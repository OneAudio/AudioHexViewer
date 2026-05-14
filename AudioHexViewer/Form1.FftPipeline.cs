// ═══════════════════════════════════════════════════════════════════════════
// Form1.FftPipeline.cs  —  Coordination du pipeline FFT
//
// Responsabilités :
//   • Lecture des paramètres UI (taille FFT, overlap, fenêtrage)
//   • Redémarrage du pipeline (RestartSfftFromUi)
//   • Consommation des frames (UpdateAnalysisFrame → SpectrumControl)
//   • Snapshot temporel pour HistogramControl (GetAnalysisFrame)
//   • Menus de configuration : taille, overlap, buffer
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
        private int GetUiFftSize()
        {
            // N affiché/choisi dans la GUI (sample size)
            return analysisSampleSize;
        }


        private double GetUiOverlap()
        {
            // Ratio overlap (0..1) tel que géré par la GUI
            return analysisOverlap;
        }

        //
        private void ApplyActiveChannelsToWorker(string mode)
        {
            // Détermine les voies requises par le mode d'affichage.
            // IMPORTANT:
            // - "Left"  => seulement L
            // - "Right" => seulement R
            // - "L+R"   => L et R
            // - "Bode Plot"   => nécessite L et R (FRF/H1)
            bool needL = (mode == "Left" || mode == "L+R" || mode == "Bode Plot");
            bool needR = (mode == "Right" || mode == "L+R" || mode == "Bode Plot");

            _pipeline?.SetActiveChannels(needL, needR);
        }

        //
        private void RestartSfftFromUi()
        {
            // Ne rien faire si on n'est pas en acquisition
            if (currentFormat == null) return;

            // 1) Lire les paramètres UI
            int fs      = currentFormat.SampleRate;
            int fftSize = GetUiFftSize();   // puissance de 2, 8k..4M
            double overlap = GetUiOverlap(); // [0..1]

            // Sécurité
            if (fftSize  < 8192)    fftSize  = 8192;
            if (fftSize  > 4194304) fftSize  = 4194304;
            if (overlap  < 0.0)     overlap  = 0.0;
            if (overlap  > 0.99)    overlap  = 0.99;

            int hop = Math.Max(1, (int)Math.Round(fftSize * (1.0 - overlap)));

            // Arme le ticker UX (animation de progression pendant le priming)
            ArmFftTickerPriming(fs, fftSize, hop, overlap);

            // IMPORTANT: rendre la dernière frame UI avant que le worker redémarre
            _lastFrameUi?.Dispose();
            _lastFrameUi = null;

            // 2) (Re)démarrer via le contrôleur
            _pipeline ??= new FftPipelineController();

            var windowType = spectrumControl?.SelectedWindowType ?? WindowType.BlackmanHarris7;
            _pipeline.Start(fs, fftSize, hop, windowType);

            // 3) Appliquer le mode voie courant dès la création du worker
            ApplyActiveChannelsToWorker(spectrumControl?.SelectedChannelMode ?? "L+R");
        }

        // Fournit les N derniers samples int24 depuis le ring (pour HistogramControl).
        // IMPORTANT : retourne toujours un tableau de sampleCount éléments (zéros si
        // pas encore assez de données). HistogramControl.RebuildHistogram() n'est pas
        // conçu pour des tableaux vides → overflow GDI+ si on retourne Array.Empty.
        private (int[] Left, int[] Right) GetAnalysisFrame(int sampleCount)
        {
            if (sampleCount <= 0)
                return (Array.Empty<int>(), Array.Empty<int>());

            if (_pipeline != null && _pipeline.TrySnapshotAsInt24(sampleCount, out var l, out var r))
                return (l, r);

            // Fallback : zéros — même contrat qu'avant Phase 2
            return (new int[sampleCount], new int[sampleCount]);
        }

        //
        private void InitAnalysisUi()
        {
            // Init hop + timer
            RecomputeHopAndTimer();

            // Pousse les valeurs aux contrôles si déjà créés
            histogramControl?.SetGlobalSampleSize(analysisSampleSize);
            histogramControl?.UpdateHopSamples(analysisHopSamples);

            spectrumControl?.SetGlobalSampleSize(analysisSampleSize);
            spectrumControl?.UpdateHopSamples(analysisHopSamples);

        }


        private void SetAnalysisSampleSize(int nSamples)
        {
            if (nSamples < 8192) nSamples = 8192;
            if (nSamples > 4194304) nSamples = 4194304;

            analysisSampleSize = nSamples;
            //analysisCursorStart = 0; // repart sur la fin du buffer

            RecomputeHopAndTimer();

            histogramControl?.SetGlobalSampleSize(analysisSampleSize);
            histogramControl?.UpdateHopSamples(analysisHopSamples);

            spectrumControl?.SetGlobalSampleSize(analysisSampleSize);
            spectrumControl?.UpdateHopSamples(analysisHopSamples);

            // (Étape 5) Restart immédiat du worker SFFT si la capture tourne
            if (capture != null && ((capture as WasapiCapture)?.CaptureState ?? CaptureState.Capturing) == CaptureState.Capturing)
            {
                RestartSfftFromUi();
            }

        }


        private void InitSampleSizeMenu()
        {
            if (menuSampleSize == null) return;

            menuSampleSize.DropDownItems.Clear();

            foreach (int n in _sampleSizeOptions)
            {
                var it = new ToolStripMenuItem(n.ToString("N0"))
                {
                    Tag = n,
                    CheckOnClick = true
                };

                it.Click += (s, e) =>
                {
                    if (_updatingSampleSizeMenuUi) return;

                    int chosen = (int)((ToolStripMenuItem)s!).Tag!;
                    _updatingSampleSizeMenuUi = true;
                    try
                    {
                        SetAnalysisSampleSize(chosen);
                        UpdateSampleSizeMenuChecks();
                    }
                    finally
                    {
                        _updatingSampleSizeMenuUi = false;
                    }
                };

                menuSampleSize.DropDownItems.Add(it);
            }
        }

        //
        private void InitBufferMenu()
        {
            if (menuBuffer == null) return;

            menuBuffer.DropDownItems.Clear();

            foreach (int n in _bufferOptions)
            {
                string label = (n / 1024).ToString() + "k";

                var it = new ToolStripMenuItem(label)
                {
                    Tag = n,
                    CheckOnClick = true
                };

                it.Click += (s, e) =>
                {
                    if (_updatingBufferMenuUi) return;

                    int chosen = (int)((ToolStripMenuItem)s!).Tag!;
                    _updatingBufferMenuUi = true;
                    try
                    {
                        SetAnalysisBufferSize(chosen);
                        UpdateBufferMenuChecks();
                    }
                    finally
                    {
                        _updatingBufferMenuUi = false;
                    }
                };

                menuBuffer.DropDownItems.Add(it);
            }
        }

        //
        private void UpdateBufferMenuChecks()
        {
            if (menuBuffer == null) return;

            foreach (ToolStripMenuItem it in menuBuffer.DropDownItems)
            {
                int n = (int)it.Tag!;
                it.Checked = (n == analysisBufferSize);
            }
        }

        //
        private void SetAnalysisBufferSize(int bufferSize)
        {
            if (bufferSize < 16 * 1024) bufferSize = 16 * 1024;
            if (bufferSize > 512 * 1024) bufferSize = 512 * 1024;

            analysisBufferSize = bufferSize;

        }

        //
        private void ResetAnalysisPipeline()
        {
        }

        //
        private void InitOverlapMenu()
        {
            if (menuOverlap == null) return;

            menuOverlap.DropDownItems.Clear();

            foreach (var (label, value) in _overlapOptions)
            {
                var it = new ToolStripMenuItem(label)
                {
                    Tag = value,
                    CheckOnClick = true
                };

                it.Click += (s, e) =>
                {
                    if (_updatingOverlapMenuUi) return;

                    double chosen = (double)((ToolStripMenuItem)s!).Tag!;
                    _updatingOverlapMenuUi = true;
                    try
                    {
                        SetAnalysisOverlap(chosen);
                        UpdateOverlapMenuChecks();
                    }
                    finally
                    {
                        _updatingOverlapMenuUi = false;
                    }
                };

                menuOverlap.DropDownItems.Add(it);
            }
        }


        private void UpdateOverlapMenuChecks()
        {
            if (menuOverlap == null) return;

            foreach (ToolStripMenuItem it in menuOverlap.DropDownItems)
                it.Checked = (Math.Abs((double)it.Tag! - analysisOverlap) < 1e-12);
        }

        //
        private void UpdateSampleSizeMenuChecks()
        {
            if (menuSampleSize == null) return;

            foreach (ToolStripMenuItem it in menuSampleSize.DropDownItems)
                it.Checked = ((int)it.Tag! == analysisSampleSize);
        }

        //
        private void SetAnalysisOverlap(double overlap)
        {
            if (overlap < 0.0) overlap = 0.0;
            if (overlap > 0.99) overlap = 0.99;

            analysisOverlap = overlap;
            //analysisCursorStart = 0;

            RecomputeHopAndTimer();

            histogramControl?.UpdateHopSamples(analysisHopSamples);
            spectrumControl?.UpdateHopSamples(analysisHopSamples);

            // (Étape 5) Restart immédiat du worker SFFT si la capture tourne
            if (capture != null && ((capture as WasapiCapture)?.CaptureState ?? CaptureState.Capturing) == CaptureState.Capturing)
            {
                RestartSfftFromUi();
            }

        }


        private void RecomputeHopAndTimer()
        {
            analysisHopSamples = (int)Math.Round(analysisSampleSize * (1.0 - analysisOverlap));
            if (analysisHopSamples < 1) analysisHopSamples = 1;

            if (analysisTimer == null)
            {
                analysisTimer = new System.Windows.Forms.Timer();
                analysisTimer.Tick += (s, e) => UpdateAnalysisFrame();
            }

            // ROBUST FIX: timer UI = cadence d'affichage, pas cadence d'acquisition
            analysisTimer.Interval = 20; // 50 Hz UI (ajuste si besoin: 10..50 ms)

            // Le timer ne tourne que quand la capture est active
            if (capture != null && ((capture as WasapiCapture)?.CaptureState ?? CaptureState.Capturing) == CaptureState.Capturing)
                analysisTimer.Start();
            else
                analysisTimer.Stop();
        }


        private void UpdateAnalysisFrame()
        {
            if (currentFormat == null) return;

            // Le ticker doit bouger même si aucune nouvelle FFT n'arrive
            TickFftTickerUi();

            // --- Étape 3 : afficher la FFT produite par le worker (pas de FFT UI) ---
            int drained = 0;
            SpectrumFrame? last = null;

            while (_pipeline!.Frames.TryDequeue(out var f))
            {
                // On conserve uniquement la dernière frame
                if (last != null)
                    last.Dispose(); // rend au pool celle qu'on s'apprêtait à remplacer

                last = f;
                drained++;
            }


            if (drained > 0)
            {
                ResetFftTickerFromCurrentStats(); // réarme à chaque nouvelle FFT (après la 1ère, passe en hop/Fs)
                // --- Mise à jour du titre : DOIT tourner même sans nouvelle FFT ---
                // (sinon prog% ne peut pas "compter" entre 2 FFT)
                double t = _uiSw.Elapsed.TotalSeconds;
            

                if (t - _uiLastRateT > 0.2) // 5 Hz sur le titre (suffisant et stable)
                {
                    var snap = _pipeline!.Stats.Snapshot();

                    double expectedFps = (snap.Hop > 0 && snap.SampleRate > 0)
                        ? (snap.SampleRate / (double)snap.Hop)
                        : 0.0;

                    double progPct = snap.Progress01 * 100.0;

                    // Option REW-like : n'affiche le prog que si c'est lent (sinon ça va trop vite)
                    string progStr = (expectedFps < 1.0) ? $" prog~{progPct:F0}%" : "";

                    string absStr = (_lastFrameUi != null)
                        ? $" AbsEnd={_lastFrameUi.AbsEnd}"
                        : $" AbsWrite={snap.AbsWrite}";

                    // --- Barre de debug (lblDebugInfo) + titre de fenêtre ---
                    // Le titre n'affiche plus les infos de debug (évite la troncature Windows).
                    // Toutes les infos sont dans la barre interne, activée via View > Debug info.
                    string fullErr = (snap.LastError ?? "").Replace("\r", " ").Replace("\n", " ");
                    string fullErrStr = string.IsNullOrWhiteSpace(fullErr) ? "" : $" | ERR: {fullErr}";
                    _lastDebugBarText =
                        $"FFT/s act={snap.FftPerSec:F1} | FFT={snap.FftAvgMs:F3} ms{fullErrStr}";

                    // Titre de fenêtre : nom de l'appli uniquement (stable, jamais tronqué)
                    if (this.Text != "AudioHexViewer")
                        this.Text = "AudioHexViewer";

                    // Mise à jour de la barre debug interne — uniquement si le texte a changé
                    // (évite le clignotement causé par une réaffectation inutile)
                    if (panelDebugInfo.Visible && lblDebugInfo.Text != _lastDebugBarText)
                        lblDebugInfo.Text = _lastDebugBarText;

                    _uiLastRateT = t;
                }

            _uiFramesDrained += drained;
            }

            if (last != null && spectrumControl != null)
            {
                // On remplace la frame UI précédente => il faut la rendre au pool
                _lastFrameUi?.Dispose();
                _lastFrameUi = last;

                // Log de diagnostic : une ligne par FFT produite (si F8 actif)
                if (_diagLogEnabled)
                    WriteDiagLogLine(_lastFrameUi, drained);

                // Throttle: on draine toujours, mais on n'affiche qu'à 10 Hz max
                double tNow = _uiSw.Elapsed.TotalSeconds;
                bool canDisplay = (tNow - _uiLastDisplayT) >= UiDisplayPeriodSec;

                // N'afficher la FFT QUE si l'onglet Spectrum est en cours (bouton RUN spectrum actif)
                // => les 2 boutons RUN sont vraiment indépendants
                if (canDisplay && _spectrumRunning)
                {
                    spectrumControl.EnableExternalSpectrumMode(true);

                    // IMPORTANT : on envoie TOUJOURS la frame complète pour que les métriques soient justes.
                    // La décimation sera faite dans SpectrumControl uniquement pour le tracé.
                    spectrumControl.FeedExternalSpectrum(_lastFrameUi);


                }

            }


            int N = analysisSampleSize;
            if (N <= 0) return;

            int hop = analysisHopSamples > 0 ? analysisHopSamples : N;

            // ── Snapshot temporel depuis le ring (Phase 2: remplace _analysisWinL/R) ──
            // FftPipelineController.TrySnapshotAsInt24() lit les N derniers samples float
            // depuis RingBufferStereoFloat et les convertit en int24 à la volée.
            // Plus de ring buffer parallèle, plus de lock dédié, plus de double conversion.
            if (_pipeline != null && _pipeline.TrySnapshotAsInt24(N, out var left, out var right))
            {
                // Stats temporelles (VU, RMS, peak, Vdc, calibration)
                if (spectrumControl != null && _spectrumRunning)
                    spectrumControl.FeedExternalTimeStats24(left, right);
                // HistogramControl lit directement via GetAnalysisFrame() → TrySnapshotAsInt24()
            }
        }

    }
}
