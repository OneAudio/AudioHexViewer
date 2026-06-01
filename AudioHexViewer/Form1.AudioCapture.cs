// ═══════════════════════════════════════════════════════════════════════════
// Form1.AudioCapture.cs  —  Capture audio WASAPI
//
// Responsabilités :
//   • Démarrage / arrêt de la capture (StartCapture / StopCapture)
//   • Découplage thread capture → ProcessBuffer via Channel<byte[]>
//   • Conversion byte[] → float32 via MemoryMarshal.Cast (zéro copie)
//     Passe unique : déinterleave + VU peak + REC + pipeline push
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
            _vuInTimer?.Stop();
            if (capture != null)
                return; // déjà démarré

            // ── Mode loopback : ne nécessite pas de périphérique physique ─────
            // La création de LoopbackWaveIn se fait dans le bloc try ci-dessous
            // pour que tout le pipeline (Channel, SfftWorker, analysisTimer...) soit
            // correctement initialisé — comme pour une capture physique normale.

            if (_loopbackSource == null && _selectedInputDevice is not MMDevice)
            {
                MessageBox.Show("Selectionne un priphrique audio.");
                return;
            }

            var dev = _selectedInputDevice as MMDevice;
            selectedDevice = dev;   // null si loopback, MMDevice sinon

            try
            {
                // ── Sélection du mode de capture ─────────────────────────────
                if (_loopbackSource != null)
                {
                    // Mode loopback logiciel : pas de périphérique physique
                    capture = new LoopbackWaveIn(_loopbackSource);
                }
                else
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
                }   // fin else (mode physique)
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

                    oscilloscopeControl?.UpdateSampleRate(currentFormat.SampleRate);
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
                UpdateVuInTimer();   // démarre le VU si un onglet analyse est actif

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
            _vuInTimer?.Stop();
            _vuInPctL = _vuInPctR = 0;
            _vuInStrL = _vuInStrR = "-∞ dBFS";
            if (pnlInputVu?.IsHandleCreated == true) pnlInputVu.BeginInvoke(() => pnlInputVu.Invalidate());
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

            if (currentFormat.Encoding != WaveFormatEncoding.IeeeFloat ||
                currentFormat.BitsPerSample != 32)
                return;

            int channels = currentFormat.Channels;
            if (channels < 2)
                return;

            int bytesPerFrame = channels * 4;
            if (bytesRecorded < bytesPerFrame)
                return;

            int frameCount = bytesRecorded / bytesPerFrame;

            // ── Cast zéro-copie byte[] → float (MemoryMarshal, zéro alloc, zéro bounds-check) ─
            // Le buffer provient d'ArrayPool<byte> (aligné ≥8 octets) ; float = 4 octets → OK.
            // Sur Windows x64/x86/ARM le format est little-endian, identique à la mémoire native.
            var floatBuf = System.Runtime.InteropServices.MemoryMarshal.Cast<byte, float>(
                buffer.AsSpan(0, bytesRecorded));

            // ── Constantes de conversion float→int24 ─────────────────────────────────────────
            const int Max24    =  1 << 23;        // 8 388 608
            const int Max24Pos =  Max24 - 1;      // +0x7FFFFF
            const int Min24Neg = -Max24;           // -0x800000

            // ── Dernier frame : affichage hex/bits + saturation ───────────────────────────────
            // On utilise directement le span casté (zéro BitConverter supplémentaire).
            float leftF  = floatBuf[(frameCount - 1) * channels];
            float rightF = floatBuf[(frameCount - 1) * channels + 1];

            int left24  = (int)Math.Truncate(leftF  * Max24);
            int right24 = (int)Math.Truncate(rightF * Max24);

            lastCodeLeft  = left24;
            lastCodeRight = right24;

            double instVoltsLeft  = calibValidLeft  ? calibGainLeft  * left24  + calibOffsetLeft  : 0.0;
            double instVoltsRight = calibValidRight ? calibGainRight * right24 + calibOffsetRight : 0.0;

            var now = DateTime.Now;
            if (now - lastVoltageUpdate >= voltageUpdateInterval)
            {
                lastVoltageUpdate = now;
                displayVoltsLeft  = instVoltsLeft;
                displayVoltsRight = instVoltsRight;
            }

            if (left24  > Max24Pos) left24  = Max24Pos;
            if (left24  < Min24Neg) left24  = Min24Neg;
            if (right24 > Max24Pos) right24 = Max24Pos;
            if (right24 < Min24Neg) right24 = Min24Neg;

            bool sampleSatLeft  = left24  == Max24Pos || left24  == Min24Neg;
            bool sampleSatRight = right24 == Max24Pos || right24 == Min24Neg;

            if (sampleSatLeft && !isCurrentlySatLeft)
            { isCurrentlySatLeft = true;  lastSatLeft  = now; }
            else if (!sampleSatLeft && isCurrentlySatLeft)
                isCurrentlySatLeft = false;

            if (sampleSatRight && !isCurrentlySatRight)
            { isCurrentlySatRight = true; lastSatRight = now; }
            else if (!sampleSatRight && isCurrentlySatRight)
                isCurrentlySatRight = false;

            uint left24u  = unchecked((uint)(left24  & 0xFFFFFF));
            uint right24u = unchecked((uint)(right24 & 0xFFFFFF));

            // ── Passe UNIQUE : déinterleave → tmpL/tmpR + VU peak + REC ──────────────────────
            // Avant : 2 parcours complets du buffer (push pipeline PUIS VU peak).
            // Maintenant : 1 seul parcours ; tmpL/tmpR sont réutilisés directement
            // pour PushAudio, éliminant le second balayage et un double-parse float.
            List<RecordFrame>? recordSnapshotToSave = null;
            int peakLeftAbs  = 0;
            int peakRightAbs = 0;

            bool recWasActive;
            lock (_recordLock) { recWasActive = isRecordingOneSecond; }
            List<RecordFrame>? localRec = null;

            float[]? tmpL = null;
            float[]? tmpR = null;
            try
            {
                tmpL = ArrayPool<float>.Shared.Rent(frameCount);
                tmpR = ArrayPool<float>.Shared.Rent(frameCount);

                for (int i = 0; i < frameCount; i++)
                {
                    float lf = floatBuf[i * channels];
                    float rf = floatBuf[i * channels + 1];

                    tmpL[i] = lf;
                    tmpR[i] = rf;

                    int L = (int)(lf * Max24);
                    int R = (int)(rf * Max24);

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

                // ── Pipeline FFT ──────────────────────────────────────────────────────────────
                if (_pipeline?.Ring != null)
                {
                    _pipeline!.PushAudio(tmpL.AsSpan(0, frameCount), tmpR.AsSpan(0, frameCount));
                    _pipeline!.Stats.SetAbsWrite(_pipeline!.Ring.AbsWrite);

                    // ── Diagnostic débit ─────────────────────────────────────────────────────
                    var diagNow = DateTime.UtcNow;
                    if (_diagLastTime == DateTime.MinValue) _diagLastTime = diagNow;

                    _diagSamplesReceived += frameCount;
                    double diagElapsed = (diagNow - _diagLastTime).TotalSeconds;

                    if (diagElapsed >= 2.0)
                    {
                        _diagSamplesExpected = (long)(currentFormat.SampleRate * diagElapsed);
                        long deficit  = _diagSamplesExpected - _diagSamplesReceived;
                        _diagDeficitMs = deficit * 1000.0 / currentFormat.SampleRate;

                        long dropsWindow = Interlocked.Exchange(ref _diagDropCount, 0);

                        var ring = _pipeline.Ring;
                        string lockInfo = $" | LockWait max={ring.LockWaitMaxMs:F2}ms avg={ring.LockWaitAvgMs:F3}ms n={ring.LockWaitCount}";
                        string dropInfo = dropsWindow > 0
                            ? $" | DROP={dropsWindow} pkt ⚠"
                            : " | DROP=0 ✓";

                        string diagMsg = $"DIAG: reçu={_diagSamplesReceived} attendu={_diagSamplesExpected} " +
                                         $"déficit={deficit} ({_diagDeficitMs:F1}ms){lockInfo}{dropInfo}";

                        System.Diagnostics.Debug.WriteLine(diagMsg);
                        _pipeline!.Stats.SetLastError(diagMsg);

                        _diagSamplesReceived = 0;
                        _diagLastTime        = diagNow;
                        _pipeline.ResetLockStats();
                    }
                    // ─────────────────────────────────────────────────────────────────────────
                }
            }
            finally
            {
                if (tmpL != null) ArrayPool<float>.Shared.Return(tmpL);
                if (tmpR != null) ArrayPool<float>.Shared.Return(tmpR);
            }

            // ── Flush REC en batch (une seule acquisition de _recordLock) ────────────────────
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

            if (recordSnapshotToSave != null)
                BeginInvoke((Action)(() => SaveRecordingToCsv(recordSnapshotToSave)));

            // ── Conversion VU en dBFS ─────────────────────────────────────────────────────────
            const double fullScale = Max24Pos;

            double dbLeft  = peakLeftAbs  == 0 ? double.NegativeInfinity : 20.0 * Math.Log10(peakLeftAbs  / fullScale);
            double dbRight = peakRightAbs == 0 ? double.NegativeInfinity : 20.0 * Math.Log10(peakRightAbs / fullScale);

            double dbLeftClamped  = double.IsNegativeInfinity(dbLeft)  ? -120.0 : Math.Max(dbLeft,  -120.0);
            double dbRightClamped = double.IsNegativeInfinity(dbRight) ? -120.0 : Math.Max(dbRight, -120.0);

            int levelLeft  = Math.Clamp((int)Math.Round((dbLeftClamped  + 120.0) / 120.0 * 100.0), 0, 100);
            int levelRight = Math.Clamp((int)Math.Round((dbRightClamped + 120.0) / 120.0 * 100.0), 0, 100);

            // ── Texte détaillé (dernier frame) ───────────────────────────────────────────────
            string leftHex24  = "0x" + left24u.ToString("X6");
            string rightHex24 = "0x" + right24u.ToString("X6");

            string leftBits  = FormatBitsWithStability(left24u,  lastBitsLeft,  lastChangeLeft);
            string rightBits = FormatBitsWithStability(right24u, lastBitsRight, lastChangeRight);

            var leftText = new StringBuilder();
            leftText.AppendLine(FormatRow("---LEFT---", ""));
            leftText.AppendLine(FormatRow("Float",          leftF.ToString("G9")));
            leftText.AppendLine(FormatRow("Decimal (24b)",  left24.ToString()));
            leftText.AppendLine(FormatRow("Hex (24 bits)",  leftHex24));
            leftText.AppendLine(FormatRow("Binary (24b)",   leftBits));

            double? vrmsL = null, vpkL = null, vdcL = null;
            double? vrmsR = null, vpkR = null, vdcR = null;
            bool calL = false, calR = false;

            if (spectrumControl != null)
            {
                calL = spectrumControl.VoltageCalibrationValidLeft;
                calR = spectrumControl.VoltageCalibrationValidRight;
                spectrumControl.GetLastVoltages(out vrmsL, out vpkL, out vdcL, out vrmsR, out vpkR, out vdcR);
            }

            leftText.AppendLine(FormatRow("Voltage (Vrms)",  (calL && vrmsL.HasValue) ? FormatVoltageWithUnit(vrmsL.Value) : "Uncalibrated"));
            leftText.AppendLine(FormatRow("Voltage (Vpeak)", (calL && vpkL.HasValue)  ? FormatVoltageWithUnit(vpkL.Value)  : "Uncalibrated"));
            leftText.AppendLine(FormatRow("Voltage (Vdc)",   (calL && vdcL.HasValue)  ? FormatVoltageWithUnit(vdcL.Value)  : "Uncalibrated"));

            var rightText = new StringBuilder();
            rightText.AppendLine(FormatRow("---RIGHT---", ""));
            rightText.AppendLine(FormatRow("Float",          rightF.ToString("G9")));
            rightText.AppendLine(FormatRow("Decimal (24b)",  right24.ToString()));
            rightText.AppendLine(FormatRow("Hex (24 bits)",  rightHex24));
            rightText.AppendLine(FormatRow("Binary (24b)",   rightBits));
            rightText.AppendLine(FormatRow("Voltage (Vrms)",  (calR && vrmsR.HasValue) ? FormatVoltageWithUnit(vrmsR.Value) : "Uncalibrated"));
            rightText.AppendLine(FormatRow("Voltage (Vpeak)", (calR && vpkR.HasValue)  ? FormatVoltageWithUnit(vpkR.Value)  : "Uncalibrated"));
            rightText.AppendLine(FormatRow("Voltage (Vdc)",   (calR && vdcR.HasValue)  ? FormatVoltageWithUnit(vdcR.Value)  : "Uncalibrated"));

            // ── Cache des dernières valeurs ───────────────────────────────────────────────────
            lastLeftFloat   = leftF;
            lastRightFloat  = rightF;
            lastLeftScaled  = (int)Math.Round(leftF  * (1 << 23));
            lastRightScaled = (int)Math.Round(rightF * (1 << 23));
            lastLeft24      = left24;
            lastRight24     = right24;
            lastLeftHex24   = leftHex24;
            lastRightHex24  = rightHex24;
            lastLeftBits    = leftBits;
            lastRightBits   = rightBits;
            lastLeftVolts   = displayVoltsLeft;
            lastRightVolts  = displayVoltsRight;

            // ── Throttle affichage texte (4 Hz) ──────────────────────────────────────────────
            bool updateText = (now - lastTextDump) >= textDumpInterval;
            if (updateText) lastTextDump = now;

            // ── Mise à jour UI (BeginInvoke → thread UI) ─────────────────────────────────────
            BeginInvoke((Action)(() =>
            {
                if (updateText)
                {
                    var newLeft = leftText.ToString();
                    if (!string.Equals(newLeft, _lastAllFormatsLeft, StringComparison.Ordinal))
                    {
                        _lastAllFormatsLeft = newLeft;
                        txtLeft.Text        = newLeft;
                        ColorBinaryXs(txtLeft, "Binary (24b)");
                    }

                    var newRight = rightText.ToString();
                    if (!string.Equals(newRight, _lastAllFormatsRight, StringComparison.Ordinal))
                    {
                        _lastAllFormatsRight = newRight;
                        txtRight.Text        = newRight;
                        ColorBinaryXs(txtRight, "Binary (24b)");
                    }
                }

                // Bargraphs dBFS (conservé pour DataViewForm)
                UpdateLevelBar(barLeftLevel,  levelLeft);
                UpdateLevelBar(barRightLevel, levelRight);
                // Nouveau VU-mètre GDI+ entrée
                SetVuInLevels(dbLeft, dbRight);

                // SAT + dBFS (gauche)
                bool satLActive = isCurrentlySatLeft ||
                    (lastSatLeft.HasValue && (now - lastSatLeft.Value) < satDisplayTime);

                if (satLActive)
                {
                    var elapsed = now - lastSatLeft!.Value;
                    bool blinkOn = ((int)(elapsed.TotalMilliseconds / (satBlinkPeriod.TotalMilliseconds / 2))) % 2 == 0;
                    ApplyLeftLevelDisplay(blinkOn ? "      SAT" : "", Color.Red, levelFontBold);
                }
                else
                {
                    ApplyLeftLevelDisplay(
                        double.IsNegativeInfinity(dbLeft) ? "- dBFS" : $"{dbLeft:F1} dBFS",
                        SystemColors.HotTrack, levelFontRegular);
                }

                // SAT + dBFS (droite)
                bool satRActive = isCurrentlySatRight ||
                    (lastSatRight.HasValue && (now - lastSatRight.Value) < satDisplayTime);

                if (satRActive)
                {
                    var elapsed = now - lastSatRight!.Value;
                    bool blinkOn = ((int)(elapsed.TotalMilliseconds / (satBlinkPeriod.TotalMilliseconds / 2))) % 2 == 0;
                    ApplyRightLevelDisplay(blinkOn ? "SAT" : "", Color.Red, levelFontBold);
                }
                else
                {
                    ApplyRightLevelDisplay(
                        double.IsNegativeInfinity(dbRight) ? "- dBFS" : $"{dbRight:F1} dBFS",
                        SystemColors.HotTrack, levelFontRegular);
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
            if (_spectrumRunning || _histogramRunning || _oscilloscopeRunning) return;

            StopCapture();
        }

    }
}
