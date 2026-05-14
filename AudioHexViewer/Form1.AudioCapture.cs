// ═══════════════════════════════════════════════════════════════════════════
// Form1.AudioCapture.cs  —  Capture audio WASAPI
//
// Responsabilités :
//   • Démarrage / arrêt de la capture (StartCapture / StopCapture)
//   • Découplage thread capture → ProcessBuffer via Channel<byte[]>
//   • Conversion byte[] → float32 → FftPipelineController.PushAudio()
//   • VU-mètres, saturation, détection de bits
//   • Enregistrement 1s (btnRecord_Click / SaveRecordingToCsv)
// ═══════════════════════════════════════════════════════════════════════════
using AudioHexViewer.Core;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Channels;

namespace AudioHexViewer
{
    partial class Form1
    {
        private void StartCapture()
        {
            if (capture != null)
                return; // déjà démarré

            if (comboDevices.SelectedItem is not MMDevice dev)
            {
                MessageBox.Show("Selectionne un priphrique audio.");
                return;
            }

            selectedDevice = dev;

            try
            {
                // === Choix IN / OUT + Exclusif si SR explicite demandé ===
                if (!captureOutput)
                {
                    if (_requestedCaptureFormat != null)
                    {
                        // Négocie le format réel supporté par ce driver pour ce SR
                        int requestedSr = _requestedCaptureFormat.SampleRate;
                        WaveFormat? actualFormat = FindSupportedExclusiveFormat(selectedDevice, requestedSr);

                        if (actualFormat == null)
                        {
                            MessageBox.Show(
                                $"Le SR {requestedSr / 1000.0:0.###} kHz n'est pas supporté en mode\n" +
                                "Exclusif par ce périphérique (aucun format 32/24/16 bits accepté).\n\n" +
                                "Choisis 'Auto (Windows)' dans le menu Sample Rate.",
                                "Format non supporté",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Warning);
                            capture = null;
                            return;
                        }

                        capture = new ExclusiveWasapiCapture(selectedDevice, actualFormat);
                    }
                    else
                    {
                        // Mode Auto : Shared Mode standard, SR imposé par Windows
                        capture = new WasapiCapture(selectedDevice, useEventSync: false, audioBufferMillisecondsLength: 100);
                    }
                }
                else
                {
                    // Mode OUT : loopback — toujours Shared, SR du renderer Windows
                    capture = new WasapiLoopbackCapture(selectedDevice);
                }
                // ============================================================

                currentFormat = capture.WaveFormat;

                if (histogramControl != null && currentFormat != null)
                {
                    histogramControl.UpdateSampleRate(currentFormat.SampleRate);
                    histogramControl.UpdateHopSamples(analysisHopSamples);
                }

                capture.DataAvailable += Capture_DataAvailable;
                capture.RecordingStopped += Capture_RecordingStopped;

                // Met à jour le sample rate des modules d'analyse
                if (currentFormat != null)
                {
                    if (histogramControl != null)
                    {
                        histogramControl.UpdateSampleRate(currentFormat.SampleRate);
                        histogramControl.UpdateHopSamples(analysisHopSamples);
                    }

                    if (spectrumControl != null)
                    {
                        spectrumControl.UpdateSampleRate(currentFormat.SampleRate);
                        spectrumControl.UpdateHopSamples(analysisHopSamples);
                    }
                }

                // ── Mode GC bas-latence pendant la capture ────────────────────────
                // SustainedLowLatency interdit les GC Gen2 sauf urgence mémoire.
                // Sans ça, les allocations LOH répétées (buffers snapshot, etc.)
                // déclenchent des pauses STW de 0.7-2s qui suspendent SfftWorker
                // pendant plus d'un hop → TryReadWindow miss → sauts de FFT.
                System.Runtime.GCSettings.LatencyMode =
                    System.Runtime.GCLatencyMode.SustainedLowLatency;
                // ─────────────────────────────────────────────────────────────────

                // Démarrage capture audio
                capture.StartRecording();

                // ── Canal de découplage capture → ProcessBuffer ──────────────────
                // Créé APRÈS StartRecording pour ne pas perdre les premiers callbacks.
                // BoundedCapacity=8 → ~80ms de tampon.
                // DropNewest (≠ DropOldest) : quand le canal est plein, on rejette
                // le NOUVEAU paquet entrant (TryWrite retourne false) plutôt que
                // d'éjecter l'ancien. Avantage : on peut COMPTER les drops via la
                // valeur de retour de TryWrite → visible dans le diagnostic titre.
                _processCts?.Cancel();
                _processCts?.Dispose();
                _processCts = new CancellationTokenSource();
                _captureChannel = Channel.CreateBounded<(byte[], int)>(
                    new BoundedChannelOptions(8)
                    {
                        FullMode     = BoundedChannelFullMode.DropNewest,
                        SingleReader = true,
                        SingleWriter = true
                    });

                var procToken = _processCts.Token;
                _processTask = Task.Run(async () =>
                {
                    await foreach (var (buf, count) in
                        _captureChannel.Reader.ReadAllAsync(procToken))
                    {
                        try   { ProcessBuffer(buf, count); }
                        finally { ArrayPool<byte>.Shared.Return(buf); }
                    }
                }, procToken);
                // ─────────────────────────────────────────────────────────────────

                // Réinitialisation pipeline d'analyse
                ResetAnalysisPipeline();

                // Démarre pipeline SFFT (ring + worker) avec UI courants
                RestartSfftFromUi();

                // --- START test source ---
                if (currentFormat != null)
                {
                    if (_useMetricsRefTest)
                        StartMetricsReferenceTestSource(currentFormat.SampleRate);
                    else if (_useSineTest)
                        StartSineTestSource(currentFormat.SampleRate, 1000.0, 0.25);
                }

                // Démarre le générateur de fenêtres d'analyse (Histogram + Spectrum)
                RecomputeHopAndTimer();
                analysisTimer?.Start();

                UpdateDeviceInfo();
                UpdateDeviceSummary();
            }
            catch (Exception ex)
            {
                // Décodage des erreurs WASAPI les plus fréquentes
                string errMsg;
                switch (unchecked((uint)ex.HResult))
                {
                    case 0x88890008: // AUDCLNT_E_UNSUPPORTED_FORMAT
                        errMsg = $"Format non supporté par le driver en mode Exclusif.\n" +
                                 $"({_requestedCaptureFormat?.SampleRate / 1000.0:0.###} kHz / 32 bits Float)\n\n" +
                                 "Solution : choisir 'Auto' dans le menu Sample Rate, ou essayer un autre SR.";
                        break;
                    case 0x88890001: // AUDCLNT_E_ALREADY_INITIALIZED
                    case 0x88890014: // AUDCLNT_E_DEVICE_IN_USE
                        errMsg = "Le périphérique est déjà utilisé par une autre application en mode Exclusif.\n\n" +
                                 "Ferme REW, le panneau Son de Windows, ou toute autre appli audio, puis réessaie.";
                        break;
                    case 0x88890004: // AUDCLNT_E_BUFFER_SIZE_NOT_ALIGNED
                        errMsg = "Taille de buffer incompatible avec ce driver en mode Exclusif.\n" +
                                 "Essaie un autre SR ou repasse en mode Auto.";
                        break;
                    default:
                        errMsg = $"Erreur lors du démarrage de la capture :\n{ex.Message}\n(HRESULT: 0x{ex.HResult:X8})";
                        break;
                }
                MessageBox.Show(errMsg, "Erreur capture", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                capture = null;
                currentFormat = null;

                UpdateDeviceInfo();
                UpdateDeviceSummary();
            }
        }

        private void StopCapture()
        {
            if (capture == null)
                return; // déjà arrêté

            try
            {
                capture.DataAvailable -= Capture_DataAvailable;
                capture.RecordingStopped -= Capture_RecordingStopped;
                capture.StopRecording();

                // ── Arrêt propre du canal de découplage ──────────────────────────
                // 1. Signaler au Writer qu'aucun nouveau paquet n'arrivera.
                //    ReadAllAsync() dans _processTask se terminera naturellement.
                // 2. Attendre max 2s que _processTask vide les paquets en attente.
                // 3. Libérer CancellationTokenSource.
                _captureChannel?.Writer.TryComplete();
                try { _processTask?.Wait(2000); } catch { /* task déjà terminée ou annulée */ }
                _captureChannel = null;
                _processTask    = null;
                _processCts?.Cancel();
                _processCts?.Dispose();
                _processCts = null;
                // ─────────────────────────────────────────────────────────────────

                analysisTimer?.Stop();

                // Stop pipeline SFFT
                if (_pipeline != null && _pipeline.IsRunning)
                {
                    _pipeline!.Stop();
                }
                while (_pipeline!.Frames.TryDequeue(out _)) { }
                _lastFrameUi = null;
                StopFftTicker();

                // Phase 2: lastAnalysisLeft/Right supprimés — données dans _pipeline.Ring
            }
            catch
            {
                // ignore erreurs StopRecording
            }

            // Arrête aussi histogram + spectre s'ils tournent
            // On remet les flags à false AVANT StopFromRun pour éviter toute réentrance
            _histogramRunning = false;
            _spectrumRunning  = false;

            if (histogramControl != null)
                histogramControl.StopFromRun();

            if (spectrumControl != null)
                spectrumControl.StopFromRun();

            capture.Dispose();
            capture = null;
            currentFormat = null;

            // STOP ring float
            // _pipeline?.Ring lifecycle managed by FftPipelineController;
            if (_pipeline != null && _pipeline.IsRunning)
            {
                _pipeline!.Stop();
            }

            _pipeline!.Stats.SetWorkerState("STOP");

            StopSineTestSource();

            // Fermeture propre du log de diagnostic si actif
            CloseDiagLog();

            // Restauration du mode GC normal (Interactive) après la capture
            System.Runtime.GCSettings.LatencyMode =
                System.Runtime.GCLatencyMode.Interactive;

            UpdateDeviceInfo();
            UpdateDeviceSummary();
        }

        //
        private void btnRecord_Click(object sender, EventArgs e)
        {
            if (currentFormat == null)
            {
                MessageBox.Show("Dmarre la lecture (READ) avant d'enregistrer.");
                return;
            }

            lock (_recordLock)
            {
                if (isRecordingOneSecond)
                {
                    MessageBox.Show("Un enregistrement est dj en cours.");
                    return;
                }

                // Détermine le nombre de samples à enregistrer
                // => tu veux N = sample size (FFT + histogram) = valeur unique
                int windowSamples = analysisSampleSize;

                // fallback sécurité si valeur invalide
                if (windowSamples <= 0 && currentFormat != null)
                    windowSamples = currentFormat.SampleRate;

                recordSamplesNeeded = windowSamples;

                // Initialise le buffer
                recordSamplesCollected = 0;
                recordBuffer.Clear();
                recordBuffer.Capacity = Math.Max(recordBuffer.Capacity, recordSamplesNeeded);

                isRecordingOneSecond = true;
            }
        }


        private void SaveRecordingToCsv(IReadOnlyList<RecordFrame> frames)
        {
            try
            {
                string folder = GetRecordsFolderPath();
                string file = Path.Combine(folder, $"audio_record_{DateTime.Now:yyyyMMdd_HHmmss}.csv");

                using (var sw = new StreamWriter(file))
                {
                    sw.WriteLine("Index;Left_decimal;Right_decimal;Left_hex;Right_hex");

                    for (int i = 0; i < frames.Count; i++)
                    {
                        var f = frames[i];
                        uint hl = (uint)(f.Left & 0xFFFFFF);
                        uint hr = (uint)(f.Right & 0xFFFFFF);
                        sw.WriteLine($"{i};{f.Left};{f.Right};0x{hl:X6};0x{hr:X6}");
                    }
                }

                MessageBox.Show($"Fichier enregistr :\n{file}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur CSV : " + ex.Message);
            }
        }

        //
        private void Capture_DataAvailable(object? sender, WaveInEventArgs e)
        {
            // En mode test, la carte son est ignorée.
            if (_useSineTest || _useMetricsRefTest) return;

            // ── DÉCOUPLAGE : thread de capture → canal ───────────────────────
            // Ce handler s’exécute sur le thread de capture WASAPI (période 10 ms).
            // Il ne doit faire QUE copier les bytes et retourner immédiatement.
            //
            // ExclusiveWasapiCapture réutilise _outputBytes au prochain callback :
            // on loue un buffer ArrayPool (zéro GC), puis on le rend dans
            // la Task consommatrice (bloc finally dans _processTask).
            byte[] rented = ArrayPool<byte>.Shared.Rent(e.BytesRecorded);
            Buffer.BlockCopy(e.Buffer, 0, rented, 0, e.BytesRecorded);

            if (_captureChannel?.Writer.TryWrite((rented, e.BytesRecorded)) != true)
            {
                // Canal plein (ProcessBuffer trop lent) ou fermé → paquet perdu
                Interlocked.Increment(ref _diagDropCount);
                Interlocked.Increment(ref _diagDropForLog); // compteur séparé pour le log FFT
                ArrayPool<byte>.Shared.Return(rented);
            }
            // ─────────────────────────────────────────────────────────────────
        }

        private void ProcessBuffer(byte[] buffer, int bytesRecorded)
        {
            if (currentFormat == null)
                return;

            // On s'attend  du float 32 bits stro
            if (currentFormat.Encoding != WaveFormatEncoding.IeeeFloat ||
                currentFormat.BitsPerSample != 32)
            {
                return;
            }

            int channels = currentFormat.Channels;
            if (channels < 2)
                return;

            int bytesPerSample = 4; // 32 bits float
            int blockAlign = bytesPerSample * channels;

            if (bytesRecorded < blockAlign)
                return;

            // ---------- Calcul du dernier frame pour affichage dtaill ----------
            int lastFrameIndex = bytesRecorded - blockAlign;

            float leftF = BitConverter.ToSingle(buffer, lastFrameIndex);
            float rightF = BitConverter.ToSingle(buffer, lastFrameIndex + bytesPerSample);

            const int Max24 = 1 << 23;      // 8388608
            const int Max24Pos = Max24 - 1; // +0x7FFFFF
            const int Min24Neg = -Max24;    // -0x800000

            int left24 = (int)Math.Truncate(leftF * Max24);
            int right24 = (int)Math.Truncate(rightF * Max24);

            // Mmoriser le dernier code 24 bits
            lastCodeLeft = left24;
            lastCodeRight = right24;

            double instVoltsLeft = 0.0;
            double instVoltsRight = 0.0;

            if (calibValidLeft)
                instVoltsLeft = calibGainLeft * left24 + calibOffsetLeft;

            if (calibValidRight)
                instVoltsRight = calibGainRight * right24 + calibOffsetRight;

            // Filtre daffichage 4 Hz
            var now = DateTime.Now;
            if (now - lastVoltageUpdate >= voltageUpdateInterval)
            {
                lastVoltageUpdate = now;
                displayVoltsLeft = instVoltsLeft;
                displayVoltsRight = instVoltsRight;
            }

            if (left24 > Max24Pos) left24 = Max24Pos;
            if (left24 < Min24Neg) left24 = Min24Neg;
            if (right24 > Max24Pos) right24 = Max24Pos;
            if (right24 < Min24Neg) right24 = Min24Neg;

            // ---- SATURATION PAR VOIE ----
            bool sampleSatLeft = (left24 == Max24Pos || left24 == Min24Neg);
            bool sampleSatRight = (right24 == Max24Pos || right24 == Min24Neg);

            var nowSat = DateTime.Now;

            // Voie gauche : on ne date QUE le dbut de saturation
            if (sampleSatLeft && !isCurrentlySatLeft)
            {
                isCurrentlySatLeft = true;
                lastSatLeft = nowSat;  // dbut de la saturation
            }
            else if (!sampleSatLeft && isCurrentlySatLeft)
            {
                // fin de la saturation continue
                isCurrentlySatLeft = false;
            }

            // Voie droite
            if (sampleSatRight && !isCurrentlySatRight)
            {
                isCurrentlySatRight = true;
                lastSatRight = nowSat;
            }
            else if (!sampleSatRight && isCurrentlySatRight)
            {
                isCurrentlySatRight = false;
            }

            uint left24u = unchecked((uint)(left24 & 0xFFFFFF));
            uint right24u = unchecked((uint)(right24 & 0xFFFFFF));

            // Nombre de frames dans ce callback
            int bytesPerFrame = currentFormat.Channels * 4;
            int frameCount = bytesRecorded / bytesPerFrame;

            // --- NOUVEAU: push du bloc float stéréo dans _pipeline?.Ring (étape 1) ---
            // Ici on lit 1 seule fois le buffer en float32 stéréo et on pousse dans le ring.
            // Aucun impact sur l'ancien pipeline (int24), il reste en place.
            if (!(_useSineTest || _useMetricsRefTest) && _pipeline?.Ring != null && frameCount > 0) // pas de carte son si test actif
            {
                float[]? tmpL = null;
                float[]? tmpR = null;

                try
                {
                    tmpL = ArrayPool<float>.Shared.Rent(frameCount);
                    tmpR = ArrayPool<float>.Shared.Rent(frameCount);

                    int p = 0;
                    for (int i = 0; i < frameCount; i++)
                    {
                        // buffer = float32 interleaved: L,R,L,R...
                        tmpL[i] = BitConverter.ToSingle(buffer, p); p += 4;
                        tmpR[i] = BitConverter.ToSingle(buffer, p); p += 4;
                    }

                    //for (int i = 0; i < frameCount; i++)
                    //{
                    //    int idxFrame = i * bytesPerFrame;   // bytesPerFrame = currentFormat.Channels * 4

                    //    tmpL[i] = BitConverter.ToSingle(buffer, idxFrame);       // ch0
                    //    tmpR[i] = BitConverter.ToSingle(buffer, idxFrame + 4);   // ch1
                    //}


                    // Push uniquement la partie utile [0..frameCount)
                    _pipeline!.PushAudio(tmpL.AsSpan(0, frameCount), tmpR.AsSpan(0, frameCount));
                    _pipeline!.Stats.SetAbsWrite(_pipeline!.Ring!.AbsWrite);

                    // ── Diagnostic débit ──────────────────────────────────────────
                    var diagNow = DateTime.UtcNow;
                    if (_diagLastTime == DateTime.MinValue) _diagLastTime = diagNow;

                    _diagSamplesReceived += frameCount;
                    double diagElapsed = (diagNow - _diagLastTime).TotalSeconds;

                    if (diagElapsed >= 2.0 && currentFormat != null)
                    {
                        _diagSamplesExpected = (long)(currentFormat.SampleRate * diagElapsed);
                        long deficit = _diagSamplesExpected - _diagSamplesReceived;
                        _diagDeficitMs = deficit * 1000.0 / currentFormat.SampleRate;

                        // Drops dans cette fenêtre de 2s : Exchange remet le compteur à zéro
                        // atomiquement, évitant toute perte de compte entre lecture et reset.
                        long dropsWindow = Interlocked.Exchange(ref _diagDropCount, 0);

                        string lockInfo = _pipeline?.Ring != null
                            ? $" | LockWait max={_pipeline?.Ring.LockWaitMaxMs:F2}ms avg={_pipeline?.Ring.LockWaitAvgMs:F3}ms n={_pipeline?.Ring.LockWaitCount}"
                            : "";

                        string dropInfo = dropsWindow > 0
                            ? $" | DROP={dropsWindow} pkt ⚠"
                            : " | DROP=0 ✓";

                        string diagMsg = $"DIAG: reçu={_diagSamplesReceived} attendu={_diagSamplesExpected} " +
                                         $"déficit={deficit} ({_diagDeficitMs:F1}ms){lockInfo}{dropInfo}";

                        System.Diagnostics.Debug.WriteLine(diagMsg);
                        _pipeline!.Stats.SetLastError(diagMsg);  // visible dans la barre de titre

                        // Reset pour la prochaine fenêtre de 2s
                        _diagSamplesReceived = 0;
                        _diagLastTime = diagNow;
                        _pipeline?.ResetLockStats();
                    }
                    // ─────────────────────────────────────────────────────────────
                }
                finally
                {
                    if (tmpL != null) ArrayPool<float>.Shared.Return(tmpL);
                    if (tmpR != null) ArrayPool<float>.Shared.Return(tmpR);
                }


                _pipeline!.Stats.SetAbsWrite(_pipeline!.Ring!.AbsWrite);
                                
            }

            // ── Passe unique : fenêtre d'analyse + VU peak + REC ────────────────────
            // Supprimé : _audioRing (StereoRingBuffer int24) — jamais lu hors de ProcessBuffer.
            // Optimisé : _analysisWindowLock acquis UNE fois (et non N fois per-sample).
            //            _recordLock acquis UNE fois après la boucle (flush en batch).
            // ───────────────────────────────────────────────────────────────────────────
            List<RecordFrame>? recordSnapshotToSave = null;
            int peakLeftAbs  = 0;
            int peakRightAbs = 0;

            // Collecte REC locale sans lock dans la boucle
            List<RecordFrame>? localRec = null;
            bool recWasActive;
            lock (_recordLock) { recWasActive = isRecordingOneSecond; }

            // ── VU peak + REC (Phase 2 : boucle extraite du lock _analysisWindowLock supprimé) ──
            // Note : la conversion float→int24 ici sert uniquement au VU-mètre et à l'enregistrement.
            // La source de vérité audio reste _pipeline.Ring (float32).
            for (int i = 0; i < frameCount; i++)
            {
                int idxFrame = i * bytesPerFrame;
                float lf = BitConverter.ToSingle(buffer, idxFrame);
                float rf = BitConverter.ToSingle(buffer, idxFrame + 4);

                int L = (int)Math.Truncate(lf * Max24);
                int R = (int)Math.Truncate(rf * Max24);

                if (L > Max24Pos) L = Max24Pos;
                if (L < Min24Neg) L = Min24Neg;
                if (R > Max24Pos) R = Max24Pos;
                if (R < Min24Neg) R = Min24Neg;

                int absL = L < 0 ? -L : L;
                int absR = R < 0 ? -R : R;
                if (absL > peakLeftAbs)  peakLeftAbs  = absL;
                if (absR > peakRightAbs) peakRightAbs = absR;

                if (recWasActive)
                {
                    localRec ??= new List<RecordFrame>(frameCount);
                    localRec.Add(new RecordFrame { Left = L, Right = R });
                }
            }
            // ─────────────────────────────────────────────────────────────────────────

            // Flush REC avec une seule acquisition de _recordLock
            if (localRec != null)
            {
                lock (_recordLock)
                {
                    if (isRecordingOneSecond)
                    {
                        foreach (var recFr in localRec)
                        {
                            if (recordSamplesCollected >= recordSamplesNeeded) break;
                            recordBuffer.Add(recFr);
                            recordSamplesCollected++;
                        }
                        if (recordSamplesCollected >= recordSamplesNeeded)
                        {
                            isRecordingOneSecond = false;
                            recordSnapshotToSave = new List<RecordFrame>(recordBuffer);
                        }
                    }
                }
            }

            // Sauvegarde CSV déclenchée côté UI
            if (recordSnapshotToSave != null)
                BeginInvoke((Action)(() => SaveRecordingToCsv(recordSnapshotToSave)));

            // Conversion en dBFS
            double dbLeft, dbRight;
            const double fullScale = Max24Pos; // 0x7FFFFF

            if (peakLeftAbs == 0)
                dbLeft = double.NegativeInfinity;
            else
                dbLeft = 20.0 * Math.Log10(peakLeftAbs / fullScale);

            if (peakRightAbs == 0)
                dbRight = double.NegativeInfinity;
            else
                dbRight = 20.0 * Math.Log10(peakRightAbs / fullScale);

            // On clip  -120 dBFS pour l'affichage
            double dbLeftClamped = double.IsNegativeInfinity(dbLeft) ? -120.0 : Math.Max(dbLeft, -120.0);
            double dbRightClamped = double.IsNegativeInfinity(dbRight) ? -120.0 : Math.Max(dbRight, -120.0);

            // Mapping [-120, 0] dB -> [0, 100]
            int levelLeft = (int)Math.Round((dbLeftClamped + 120.0) / 120.0 * 100.0);
            int levelRight = (int)Math.Round((dbRightClamped + 120.0) / 120.0 * 100.0);

            if (levelLeft < 0) levelLeft = 0;
            if (levelLeft > 100) levelLeft = 100;
            if (levelRight < 0) levelRight = 0;
            if (levelRight > 100) levelRight = 100;

            // ---------- Texte existant ----------
            string leftHex24 = "0x" + left24u.ToString("X6");
            string rightHex24 = "0x" + right24u.ToString("X6");

            string leftBits = FormatBitsWithStability(left24u, lastBitsLeft, lastChangeLeft);
            string rightBits = FormatBitsWithStability(right24u, lastBitsRight, lastChangeRight);

            var leftText = new StringBuilder();
            leftText.AppendLine(FormatRow("---LEFT---", ""));
            leftText.AppendLine(FormatRow("Float", leftF.ToString("G9")));
            leftText.AppendLine(FormatRow("Decimal (24b)", left24.ToString()));
            leftText.AppendLine(FormatRow("Hex (24 bits)", leftHex24));
            leftText.AppendLine(FormatRow("Binary (24b)", leftBits));
            // Mesures en volts (issues de SpectrumControl / calibration unifiée)
            double? vrmsL = null, vpkL = null, vdcL = null;
            double? vrmsR = null, vpkR = null, vdcR = null;
            bool calL = false, calR = false;

            if (spectrumControl != null)
            {
                calL = spectrumControl.VoltageCalibrationValidLeft;
                calR = spectrumControl.VoltageCalibrationValidRight;
                spectrumControl.GetLastVoltages(out vrmsL, out vpkL, out vdcL, out vrmsR, out vpkR, out vdcR);
            }

            leftText.AppendLine(FormatRow("Voltage (Vrms)", (calL && vrmsL.HasValue) ? FormatVoltageWithUnit(vrmsL.Value) : "Uncalibrated"));
            leftText.AppendLine(FormatRow("Voltage (Vpeak)", (calL && vpkL.HasValue) ? FormatVoltageWithUnit(vpkL.Value) : "Uncalibrated"));
            leftText.AppendLine(FormatRow("Voltage (Vdc)", (calL && vdcL.HasValue) ? FormatVoltageWithUnit(vdcL.Value) : "Uncalibrated"));    
            var rightText = new StringBuilder();
            rightText.AppendLine(FormatRow("---RIGHT---", ""));
            rightText.AppendLine(FormatRow("Float", rightF.ToString("G9")));
            rightText.AppendLine(FormatRow("Decimal (24b)", right24.ToString()));
            rightText.AppendLine(FormatRow("Hex (24 bits)", rightHex24));
            rightText.AppendLine(FormatRow("Binary (24b)", rightBits));
            
            rightText.AppendLine(FormatRow("Voltage (Vrms)", (calR && vrmsR.HasValue) ? FormatVoltageWithUnit(vrmsR.Value) : "Uncalibrated"));
            rightText.AppendLine(FormatRow("Voltage (Vpeak)", (calR && vpkR.HasValue) ? FormatVoltageWithUnit(vpkR.Value) : "Uncalibrated"));
            rightText.AppendLine(FormatRow("Voltage (Vdc)", (calR && vdcR.HasValue) ? FormatVoltageWithUnit(vdcR.Value) : "Uncalibrated"));
            // Sauvegarde des dernires valeurs (si utilis ailleurs)
            lastLeftFloat = leftF;
            lastRightFloat = rightF;

            lastLeftScaled = (int)Math.Round(leftF * (1 << 23));
            lastRightScaled = (int)Math.Round(rightF * (1 << 23));

            lastLeft24 = left24;
            lastRight24 = right24;

            lastLeftHex24 = leftHex24;
            lastRightHex24 = rightHex24;

            lastLeftBits = leftBits;
            lastRightBits = rightBits;

            lastLeftVolts = displayVoltsLeft;
            lastRightVolts = displayVoltsRight;

            // ---------- Limitation frquence UI texte ----------
            var nowUi = DateTime.Now;
            bool updateText;

            if (nowUi - lastTextDump >= textDumpInterval)
            {
                updateText = true;
                lastTextDump = nowUi;
            }
            else
            {
                updateText = false;
            }

            // ---------- Mise  jour UI ----------
            BeginInvoke((Action)(() =>
            {
                // On ne met  jour le texte que si l'intervalle est coul
                if (updateText)
                {
                    var newLeft = leftText.ToString();
                    if (!string.Equals(newLeft, _lastAllFormatsLeft, StringComparison.Ordinal))
                    {
                        _lastAllFormatsLeft = newLeft;
                        txtLeft.Text = newLeft;
                        ColorBinaryXs(txtLeft, "Binary (24b)");
                    }

                    var newRight = rightText.ToString();
                    if (!string.Equals(newRight, _lastAllFormatsRight, StringComparison.Ordinal))
                    {
                        _lastAllFormatsRight = newRight;
                        txtRight.Text = newRight;
                        ColorBinaryXs(txtRight, "Binary (24b)");
                    }

                }

                // Bargraphs dBFS
                UpdateLevelBar(barLeftLevel, levelLeft);
                UpdateLevelBar(barRightLevel, levelRight);

                // SAT + dBFS
                bool satLActive =
                    isCurrentlySatLeft ||
                    (lastSatLeft.HasValue && (nowUi - lastSatLeft.Value) < satDisplayTime);

                if (satLActive)
                {
                    var elapsed = nowUi - lastSatLeft.Value;
                    bool blinkOn = ((int)(elapsed.TotalMilliseconds / (satBlinkPeriod.TotalMilliseconds / 2))) % 2 == 0;

                    if (blinkOn)
                    {
                        ApplyLeftLevelDisplay("      SAT", Color.Red, levelFontBold);

                    }
                    else
                    {
                        ApplyLeftLevelDisplay("", Color.Red, levelFontBold);

                    }
                }
                else
                {
                    ApplyLeftLevelDisplay(
                        double.IsNegativeInfinity(dbLeft) ? "- dBFS" : $"{dbLeft:F1} dBFS",
                        SystemColors.HotTrack,
                        levelFontRegular
                    );

                }

                bool satRActive =
                    isCurrentlySatRight ||
                    (lastSatRight.HasValue && (nowUi - lastSatRight.Value) < satDisplayTime);

                if (satRActive)
                {
                    var elapsed = nowUi - lastSatRight.Value;
                    bool blinkOn = ((int)(elapsed.TotalMilliseconds / (satBlinkPeriod.TotalMilliseconds / 2))) % 2 == 0;

                    if (blinkOn)
                    {
                        ApplyRightLevelDisplay("SAT", Color.Red, levelFontBold);

                    }
                    else
                    {
                        ApplyRightLevelDisplay("", Color.Red, levelFontBold);

                    }
                }
                else
                {
                    ApplyRightLevelDisplay(
                        double.IsNegativeInfinity(dbRight) ? "- dBFS" : $"{dbRight:F1} dBFS",
                        SystemColors.HotTrack,
                        levelFontRegular
                    );

                }

                UpdateMainDisplayFromLastValues();
            }));
        }

        //
        private void Capture_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                MessageBox.Show("Erreur pendant la capture : " + e.Exception.Message);
            }
        }

        //
        private void EnsureReadRunning()
        {
            if (capture != null) return; // déjà démarré
            StartCapture();
        }


        private void EnsureReadStoppedIfNoTabRunning()
        {
            if (capture == null) return; // déjà arrêté
            if (_spectrumRunning || _histogramRunning) return;

            StopCapture();
        }

    }
}
