// ═══════════════════════════════════════════════════════════════════════════
// SpectrumControl.Rendering.cs  —  Configuration et rendu du graphe
//
// Responsabilités :
//   • Initialisation de MS Chart (InitChartAppearance, ApplyChartTheme)
//   • Gestion des axes X/Y (log/lin, stepHz, dialogs)
//   • Boutons Run/Stop, averaging, peak-hold
//   • Formatage des labels de fréquence et de niveau
//   • Export PNG
// ═══════════════════════════════════════════════════════════════════════════
using AudioHexViewer.Core;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Globalization;

namespace AudioHexViewer
{
    partial class SpectrumControl
    {
        private void ApplyChartTheme()
        {
            if (chartSpectrum == null || chartSpectrum.ChartAreas.Count == 0)
                return;

            // Fond du contrôle Chart (marges / zones hors "plot") : garder la couleur du parent.
            chartSpectrum.BackColor = chartSpectrum.Parent?.BackColor ?? SystemColors.Control;
            var area = chartSpectrum.ChartAreas.FindByName("SpectrumArea") ?? chartSpectrum.ChartAreas[0];
            area.BackColor = BackgroundColor;

            // Grilles X/Y (major+minor). Les intervalles sont réglés ailleurs, ici on ne fait que les couleurs.
            area.AxisX.MajorGrid.LineColor = GridMajorColor;
            area.AxisY.MajorGrid.LineColor = GridMajorColor;
            area.AxisX.MinorGrid.LineColor = GridMinorColor;
            area.AxisY.MinorGrid.LineColor = GridMinorColor;

            // Couleurs des labels. Note: le Chart WinForms n'a pas de "minor labels" texte distincts,
            // donc LabelMinor s'applique aux ticks minor.
            area.AxisX.LabelStyle.ForeColor = LabelMajorColor;
            // AxisY native labels : NE PAS remettre à LabelMajorColor ici.
            // Ils sont remplacés par le rendu PostPaint (déterministe).
            // On force Transparent une fois pour toutes afin qu'aucun paint natif
            // (qui peut avoir lieu avant ApplyChartAxes) ne les rende visibles.
            area.AxisY.LabelStyle.ForeColor  = Color.Transparent;
            area.AxisY2.LabelStyle.ForeColor = Color.Transparent;
            area.AxisX.TitleForeColor = LabelMajorColor;
            area.AxisY.TitleForeColor = LabelMajorColor;

            // Axis line + ticks = LabelMajor (minor ticks = LabelMinor)
            area.AxisX.LineColor = LabelMajorColor;
            area.AxisY.LineColor = LabelMajorColor;
            area.AxisX.MajorTickMark.LineColor = LabelMajorColor;
            //area.AxisY.MajorTickMark.LineColor = LabelMajorColor;
            area.AxisX.MinorTickMark.LineColor = LabelMinorColor;
            //area.AxisY.MinorTickMark.LineColor = LabelMinorColor;

            // --------------------------------------------------------------------
            // Ticks majeurs : réduire leur longueur pour éviter qu’ils “touchent”
            // nos labels custom (major labels dessinés à la main).
            // --------------------------------------------------------------------
            area.AxisY.MajorTickMark.Size = 0.5f;
            area.AxisY2.MajorTickMark.Size = 0.5f;

            // (Optionnel) si tu veux aussi uniformiser le style :
            area.AxisY.MajorTickMark.TickMarkStyle = TickMarkStyle.OutsideArea;
            area.AxisY2.MajorTickMark.TickMarkStyle = TickMarkStyle.OutsideArea;


            // Activer les ticks "minor" pour que LabelMinor soit visible (sinon le Chart garde ses défauts).
            area.AxisX.MinorTickMark.Enabled = false;
            area.AxisY.MinorTickMark.Enabled = false;
            area.AxisX.MinorTickMark.Size = 1f;
            area.AxisY.MinorTickMark.Size = 1f;


            // Axes secondaires (au cas où ils seraient activés)
            area.AxisX2.MajorGrid.LineColor = GridMajorColor;
            area.AxisY2.MajorGrid.LineColor = GridMajorColor;
            area.AxisX2.MinorGrid.LineColor = GridMinorColor;
            area.AxisY2.MinorGrid.LineColor = GridMinorColor;
            area.AxisX2.LabelStyle.ForeColor = LabelMajorColor;
            // AxisY2 labels : gérés par PostPaint comme AxisY → toujours Transparent
            area.AxisY2.LabelStyle.ForeColor = Color.Transparent;
            area.AxisX2.LineColor = LabelMajorColor;
            area.AxisY2.LineColor = LabelMajorColor;
            area.AxisX2.MajorTickMark.LineColor = LabelMajorColor;
            area.AxisY2.MajorTickMark.LineColor = LabelMajorColor;
            area.AxisX2.MinorTickMark.LineColor = LabelMinorColor;
            area.AxisY2.MinorTickMark.LineColor = LabelMinorColor;


            chartSpectrum.Invalidate();
        }

        private void ApplyLogXMinorGridStripLines(Axis axisX)
        {
            // Pour un axe logarithmique, MinorGrid du Chart n'est pas exploitable pour avoir les lignes 2..9 par décade.
            // On les simule avec des StripLines (1 par multiplicateur) en coordonnées log10.
            axisX.StripLines.Clear();

            for (int mult = 2; mult <= 9; mult++)
            {
                var sl = new StripLine
                {
                    Interval = 1.0, // chaque décade
                    IntervalOffset = Math.Log10(mult), // position 2..9 dans la décade en unités log
                    StripWidth = 0.0,
                    BorderColor = GridMinorColor,
                    BorderDashStyle = ChartDashStyle.Dot,
                    BorderWidth = 1
                };
                axisX.StripLines.Add(sl);
            }
        }

        private void ApplyCursorTheme()
        {
            // Labels d'info des curseurs
            if (_lblCursorLInfo != null) _lblCursorLInfo.ForeColor = FftLeftColor;
            if (_lblCursorRInfo != null) _lblCursorRInfo.ForeColor = FftRightColor;

            // Couleurs des marqueurs (triangles) selon voie trackée (valeur par défaut = FFT)
            _cursorMarkLColor = FftLeftColor;
            _cursorMarkRColor = FftRightColor;

            // Couleurs des lignes de curseur
            if (_cursorLineL != null) _cursorLineL.LineColor = Color.FromArgb(180, FftLeftColor);
            if (_cursorLineR != null) _cursorLineR.LineColor = Color.FromArgb(180, FftRightColor);
        }

        private static string FormatDbMinor(double db, double stepMinor)
        {
            if (!(stepMinor > 0))
                return db.ToString("0");

            // Si le pas mineur est entier => entier
            if (Math.Abs(stepMinor - Math.Round(stepMinor)) < 1e-9)
                return db.ToString("0");

            // Sinon => décimales cohérentes avec le pas (max 2)
            int decimals = GetStepDecimals(stepMinor);

            return db.ToString("F" + decimals);
        }

        private static int CountTrailingZeros(int value)
        {
            if (value == 0) return 9; // cas spécial (ne devrait pas arriver ici)
            int count = 0;
            while (value % 10 == 0)
            {
                count++;
                value /= 10;
            }
            return count;
        }

        private static bool TryChooseBestExactStep(int spanHz, out int segments, out int stepHz)
        {
            int[] candidates = { 3, 4, 5, 6, 7, 8, 9, 10 };

            int bestSeg = -1;
            int bestStep = -1;
            int bestZeros = -1;

            foreach (int s in candidates)
            {
                if (s <= 0) continue;
                if (spanHz % s != 0) continue;

                int step = spanHz / s;
                int zeros = CountTrailingZeros(step);

                // priorité 1 : max trailing zeros
                // tie-break  : step le plus grand (moins chargé)
                if (zeros > bestZeros || (zeros == bestZeros && step > bestStep))
                {
                    bestZeros = zeros;
                    bestStep = step;
                    bestSeg = s;
                }
            }

            if (bestSeg > 0)
            {
                segments = bestSeg;
                stepHz = bestStep;
                return true;
            }

            segments = 0;
            stepHz = 0;
            return false;
        }

        private void InitializeEmptySpectrumDisplay()
        {
            // Ne pas écraser un état déjà alimenté (cas rare : designer / reload)
            if (freqAxis != null && (currentLeftSpectrum != null || currentRightSpectrum != null))
                return;

            // Valeurs "safe" pour permettre un premier rendu (axes/grille),
            // remplacées dès la première FeedExternalSpectrum().
            if (lastFftSize <= 0)
                lastFftSize = 65536; // valeur présente dans FftSizeOptions
            if (_sampleRate <= 0)
                _sampleRate = 44100; // fallback, remplacé par le vrai sample rate ensuite

            int bins = (lastFftSize / 2) + 1;

            freqAxis = new double[bins];

            currentLeftSpectrum = new double[bins];
            currentRightSpectrum = new double[bins];

            for (int i = 0; i < bins; i++)
            {
                freqAxis[i] = i * (double)_sampleRate / lastFftSize;

                // Plancher d'affichage (les deux voies) pour éviter une zone blanche
                // si l'utilisateur démarre sur "Right" ou "L+R" avant la 1ère acquisition.
                currentLeftSpectrum[i] = -200.0;
                currentRightSpectrum[i] = -200.0;
            }

            // IMPORTANT : le tracé (Chart) utilise désormais les tableaux "PLOT" (_plot*)
            // construits par BuildPlotDataFromFull(). Au démarrage, sans première frame FFT,
            // ces tableaux n'existent pas encore => graphe vide.
            // On construit donc aussi la version décimée ici, uniquement pour forcer un rendu
            // initial (axes/grille + courbes au plancher), sans toucher au worker FFT.
            BuildPlotDataFromFull();

            UpdateWindowInfo();
            UpdateOverlayInfo();
            UpdateSpectrumChartAndStats();
        }

        //
        private void EnsureVoltLayoutInit()
        {
            if (_voltLayoutInit) return;
            // Ces contrôles existent même si la zone Stats est masquée / déplacée par le layout du host.
            _leftMeasOrigLoc = lblLeftMeas.Location;
            _leftMeasOrigSize = lblLeftMeas.Size;
            _rightMeasOrigLoc = lblRightMeas.Location;
            _rightMeasOrigSize = lblRightMeas.Size;
            _voltLayoutInit = true;
        }

        private void SetVoltLabelsVisible(bool showLeft, bool showRight)
        {
            EnsureVoltLayoutInit();

            // LEFT
            if (lblLeftRmsV != null && lblLeftPeakV != null)
            {
                lblLeftRmsV.Visible = showLeft;
                lblLeftPeakV.Visible = showLeft;
                if (showLeft)
                {
                    lblLeftMeas.Location = new System.Drawing.Point(_leftMeasOrigLoc.X, _leftMeasOrigLoc.Y + 46);
                    lblLeftMeas.Size = new System.Drawing.Size(_leftMeasOrigSize.Width, Math.Max(20, _leftMeasOrigSize.Height - 46));
                }
                else
                {
                    lblLeftMeas.Location = _leftMeasOrigLoc;
                    lblLeftMeas.Size = _leftMeasOrigSize;
                }
            }

            // RIGHT
            if (lblRightRmsV != null && lblRightPeakV != null)
            {
                lblRightRmsV.Visible = showRight;
                lblRightPeakV.Visible = showRight;
                if (showRight)
                {
                    lblRightMeas.Location = new System.Drawing.Point(_rightMeasOrigLoc.X, _rightMeasOrigLoc.Y + 46);
                    lblRightMeas.Size = new System.Drawing.Size(_rightMeasOrigSize.Width, Math.Max(20, _rightMeasOrigSize.Height - 46));
                }
                else
                {
                    lblRightMeas.Location = _rightMeasOrigLoc;
                    lblRightMeas.Size = _rightMeasOrigSize;
                }
            }
        }

        //
        private void ChartSpectrum_FormatNumber(object sender, FormatNumberEventArgs e)
        {
            // On ne touche qu’aux labels d’axes
            if (e.ElementType != ChartElementType.AxisLabels)
                return;

            // Heuristique fiable dans TON graphe :
            // - Axe Y = valeurs négatives (dBFS)
            // - Axe X = fréquences >= 0
            double v = e.Value;

            // --- IMPORTANT (FRF/Phase) -------------------------------------------------
            // Le handler FormatNumber ne sait pas sur quel axe il travaille.
            // Donc si l'axe X est en log, il masque tous les labels positifs non "decades".
            // Problème : l'axe Phase (Y2) contient des valeurs positives (+90, +180) => masquées.
            // On détecte ici les labels typiques de la phase et on bypass le filtrage log.
            // ---------------------------------------------------------------------------
            if (IsFrfMode())
            {
                // Labels phase attendus : multiples de 90° dans une plage raisonnable.
                // (évite d'impacter l'axe X : ses decades sont 1,10,100,1k,...)
                const double eps = 1e-9;
                bool isPhaseTick =
                    (v >= -360.0 && v <= 360.0) &&
                    (Math.Abs((v / 90.0) - Math.Round(v / 90.0)) < eps);

                if (isPhaseTick)
                {
                    // format simple, sans "Hz", sans "k", etc.
                    e.LocalizedValue = v.ToString("0", CultureInfo.InvariantCulture);
                    return;
                }
            }

            // --- En LOG : ne garder que les décades 1,10,100,1k,10k,100k... ---
            if (_xAxisIsLog && v > 0.0)
            {
                double p = Math.Log10(v);
                double pr = Math.Round(p);

                // tolérance : accepte uniquement v ~= 10^n
                double decade = Math.Pow(10.0, pr);
                if (Math.Abs(v - decade) > decade * 1e-6)
                {
                    e.LocalizedValue = ""; // cache les labels intermédiaires (2,3,4,... ou 19.99)
                    return;
                }

                // ici c’est une décade => on affiche proprement (1,10,100,1k,...)
                e.LocalizedValue = FormatHzK(decade);
                return;
            }

            if (v >= 1000.0)
            {
                double k = v / 1000.0;

                // 1k, 2k, 10k, 20k... (et 1.5k si un jour ça arrive)
                bool isInt = Math.Abs(k - Math.Round(k)) < 1e-9;
                e.LocalizedValue = isInt ? $"{k:0}k" : $"{k:0.#}k";
            }
            else if (v >= 0.0)
            {
                e.LocalizedValue = $"{v:0}";
            }
            else
            {
                // Axe Y (dBFS) : afficher des décimales si le pas (major) n'est pas entier
                int decimals = GetStepDecimals(_currentYMajor);
                e.LocalizedValue = v.ToString("F" + decimals);
            }

        }

        //
        private static int GetStepDecimals(double step)
        {
            if (!(step > 0))
                return 0;

            // si step est un entier (à epsilon près) -> 0 décimales
            if (Math.Abs(step - Math.Round(step)) < 1e-9)
                return 0;

            // sinon on limite à 2 décimales (puisque tu arrondis minor à 2 décimales)
            // Ex: 12.5 -> 1 décimale, 12.25 -> 2 décimales
            double s10 = step * 10.0;
            if (Math.Abs(s10 - Math.Round(s10)) < 1e-9)
                return 1;

            return 2;
        }

        //
        private void UpdateAvgProgressUi(int k, int n)
        {
            if (lblAvgProgress == null) return;

            if (n <= 1) lblAvgProgress.Text = "";
            else lblAvgProgress.Text = $"{k}/{n}";
        }

        //
        private void UpdateWindowInfo()
        {
            if (_sampleRate <= 0 || lastFftSize <= 0)
            {
                lblWindowInfo.Text = "";
                return;
            }

            double sec = (double)lastFftSize / _sampleRate;
            lblWindowInfo.Text = $"≈ {sec:F2} s";
        }

        private void BtnResetAvg_Click(object? sender, EventArgs e)
        {
            ResetAveragingState();

            // Pas de recalcul immédiat ici : on attend la prochaine frame externe
            chartSpectrum.Invalidate();
        }

        // 
        private void InitChartAppearance()
        {
            var areas = chartSpectrum.ChartAreas;
            if (areas.Count == 0)
            {
                var area = new ChartArea("SpectrumArea");
                chartSpectrum.ChartAreas.Add(area);
            }

            chartSpectrum.Series.Clear();

            var area0 = chartSpectrum.ChartAreas[0];
            area0.Name = "SpectrumArea";
            area0.AxisX.Title = "Frequency (Hz)";
            area0.AxisY.Title = "Level (dBFS)";
            area0.AxisY.Minimum = -200;
            area0.AxisY.Maximum = 0;


            ApplyChartTheme();
        }

        //
        private void UpdateLogLinButtonText()
        {
            if (chkLogX == null) return;
            // Convention : Checked = LOG, Unchecked = LIN
            chkLogX.Text = chkLogX.Checked ? "LOG" : "LIN";
        }

        //
        private void InitializeUiOptions()
        {
            //
            chkCursorL.CheckedChanged += (s, e) => UpdateCursorVisibility();
            chkCursorR.CheckedChanged += (s, e) => UpdateCursorVisibility();

            // Window
            comboWindow.Items.Clear();
            comboWindow.Items.Add("Rectangular");
            comboWindow.Items.Add("Hann");
            comboWindow.Items.Add("Hamming");
            comboWindow.Items.Add("Blackman-Harris 4");
            comboWindow.Items.Add("Blackman-Harris  7");
            comboWindow.SelectedIndex = 0; // Fênêtre par défaut : 0= Rectangulaire
            comboWindow.SelectedIndexChanged += (s, e) =>
            {
                if (_isRestoringSettings)
                    return;

                var newWin = (WindowType)comboWindow.SelectedIndex;
                if (newWin == lastWindow)
                    return;

                lastWindow = newWin;
                WindowTypeChangedForExternalFft?.Invoke(this, EventArgs.Empty);
                // IMPORTANT (anti-artifacts)
                // Changer de fenetre modifie la ponderation des bins.
                // On reinitialise la moyenne pour eviter des traces visuelles.
                ResetAveragingState();
                UpdateOverlayInfo();

            };



            // Averaging options
            comboAveraging.Items.Clear();
            comboAveraging.Items.Add("None");

            int[] av = new[] { 2, 4, 8, 16, 32, 64, 128 };
            foreach (var a in av) comboAveraging.Items.Add(a.ToString(CultureInfo.InvariantCulture));

            // Exponential options (alpha)
            comboAveraging.Items.Add("Exponential 0.5");
            comboAveraging.Items.Add("Exponential 0.75");
            comboAveraging.Items.Add("Exponential 0.88");
            comboAveraging.Items.Add("Exponential 0.94");
            comboAveraging.Items.Add("Exponential 0.97");

            // Default
            comboAveraging.SelectedIndex = 0;

            comboAveraging.SelectedIndexChanged += (s, e) =>
            {
                if (_isRestoringSettings) return;

                string txt = comboAveraging.SelectedItem as string ?? "None";

                _avgIsExponential = false;
                _avgExpAlpha = 0.0;

                if (txt.StartsWith("Exponential", StringComparison.OrdinalIgnoreCase))
                {
                    // format: "Exponential 0.94"
                    var parts = txt.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2 &&
                        double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double a))
                    {
                        _avgIsExponential = true;
                        _avgExpAlpha = a;
                    }

                    // on garde avgTargetCount à 1 (pas utilisé en mode exp)
                    avgTargetCount = 1;
                }
                else if (txt.Equals("None", StringComparison.OrdinalIgnoreCase))
                {
                    avgTargetCount = 1;
                }
                else if (int.TryParse(txt, NumberStyles.Integer, CultureInfo.InvariantCulture, out int n) && n >= 2)
                {
                    avgTargetCount = n;
                }
                else
                {
                    avgTargetCount = 1;
                }

                ResetAveragingState();
                UpdateOverlayInfo();

            };

            ////
            //// Sélection AC/DC
            //chkRemoveDC.CheckedChanged += (s, e) =>
            //{
            //    if (_isRestoringSettings)
            //        return;

            //    UpdateAcDcButtonText();
            //    UpdateOverlayInfo();
            //};

            //// Initial update
            //UpdateAcDcButtonText();
            //UpdateLogLinButtonText();

            // Checkbox DC mode
            if (chkDcMode != null)
            {
                chkDcMode.CheckedChanged += (s, e) =>
                {
                    if (_isRestoringSettings) return;

                    // Checked = DC mode (conserver DC), Unchecked = AC mode (retirer DC)
                    _removeDcStats = !chkDcMode.Checked;

                    // Notifier Form1 pour propager à SfftWorker
                    RemoveDcChanged?.Invoke(this, EventArgs.Empty);

                    UpdateOverlayInfo();
                };
            }


            // Peak-hold
            if (chkPeakHold != null)
            {
                chkPeakHold.Checked = false;
                // Initial update visuelle
                UpdateToggleButtonVisual(chkPeakHold);

                chkPeakHold.CheckedChanged += (s, e) =>
                {
                    // Quand on active : repartir de zéro pour que le peak-hold soit cohérent
                    if (chkPeakHold.Checked)
                        _post.ResetPeakHoldState();

                    // Update visuelle
                    UpdateToggleButtonVisual(chkPeakHold);
                    UpdateSpectrumChartAndStats();
                };
            }

            // Channel (affichage)
            comboChannel.Items.Clear();
            comboChannel.Items.Add("Left");
            comboChannel.Items.Add("Right");
            comboChannel.Items.Add("L+R");
            comboChannel.Items.Add("Bode Plot");
            comboChannel.SelectedIndex = 2; // Par défaut les 2 voies sont affichées

            comboChannel.SelectedIndexChanged += (s, e) =>
            {
                if (_isRestoringSettings)
                    return;

                // Mise à jour immédiate de l'UI d'unité (même en LIVE)
                UpdateLevelUnitUi();
                // F
                UpdateFrfUiLocks();   // <-- AJOUT
                // Mise à jour de l'overlay
                UpdateOverlayInfo();

                // IMPORTANT: en FRF (L/R) on masque le panneau stats -> on récupère toute la largeur
                ApplyChartStatsMargin(chartSpectrum, invalidate: true, reservedScale: 1f);

                SyncCursorUiFromModel(); // met à jour les libellés Auto/Gain/Phase ou Auto/Left/Right
                ApplyFrfPhiButtonsUi(); // montre/masque les boutons φ selon IsFrfMode()


                // Informe l'extérieur immédiatement (même en LIVE)
                // => le worker pourra éviter de calculer une voie inutile.
                ChannelModeChanged?.Invoke(SelectedChannelMode);

                // IMPORTANT :
                // En mode LIVE on ne force PAS un redraw immédiat, car la voie nouvellement choisie
                // n'a pas encore son spectre calculé (il arrive au prochain AcquireOnce()).
                // Forcer un redraw met le Chart en état transitoire (séries vides + log) => exception.
                if (isLive)
                    return;

                // Hors live (pause), on peut redessiner ce qu'on a en mémoire
                UpdateSpectrumChartAndStats();
            };



            // Bouton RUN
            btnRun.Click += (s, e) =>
            {
                if (!isLive)
                {
                    RunRequested?.Invoke();   // demande à Form1 de lancer READ si besoin
                    StartFromRun(false);
                    RunStateChanged?.Invoke(true);
                }
                else
                {
                    StopFromRun();
                    RunStateChanged?.Invoke(false);
                }
            };
            // Unité de niveau (FFT) : dBFS / dBV (si calibré)
            if (comboLevelUnit != null)
            {
                comboLevelUnit.Items.Clear();
                comboLevelUnit.Items.Add("dBFS");
                comboLevelUnit.Items.Add("dBV");
                comboLevelUnit.SelectedIndex = 0;

                comboLevelUnit.SelectedIndexChanged += (s, e) =>
                {
                    if (_isRestoringSettings)
                        return;

                    _levelUnit = (comboLevelUnit.SelectedItem as string) ?? "dBFS";

                    // Si on n'a pas la calibration pour le mode courant, on force dBFS
                    if (_levelUnit == "dBV" && !IsDbvAvailableForCurrentMode())
                    {
                        _levelUnit = "dBFS";
                        comboLevelUnit.SelectedIndex = 0;
                    }

                    // éviter des limites incohérentes après un changement d'unité
                    customYMin = null;
                    customYMax = null;

                    if (!isLive)
                        UpdateSpectrumChartAndStats();
                };
            }

            UpdateLevelUnitUi();

            // Log X axis 
            if (chkLogX != null)
            {
                chkLogX.CheckedChanged += (s, e) =>
                {
                    if (_isRestoringSettings)
                        return;

                    // Bascule LIN→LOG : réinitialiser customFMin inconditionnellement.
                    //
                    // Problème fondamental : customFMin peut contenir n'importe quelle valeur
                    // saisie en LIN (y compris 0, ou sub-Hz comme 0,001 Hz) qui est invalide
                    // pour l'axe LOG. Toutes les tentatives de "corriger conditionnellement"
                    // (si < ΔF, si <= 0…) ont échoué car fMinAuto peut lui-même être
                    // incorrect dans certains états transitoires.
                    //
                    // Solution définitive : en passant en LOG, on part toujours de l'auto
                    // (fMinAuto = ΔF courant, décade inférieure). L'utilisateur qui souhaite
                    // un Fmin custom en LOG l'entre via le dialog APRÈS avoir basculé en LOG,
                    // où la validation contre ΔF se fait correctement.
                    if (chkLogX.Checked)
                        customFMin = null;

                    // IMPORTANT (anti-artifacts)
                    // Le passage LOG <-> LIN change l'echantillonnage visuel de l'axe X
                    // et peut faire apparaitre un 'ghost' d'une moyenne precedente.
                    ResetAveragingState();

                    UpdateSpectrumChartAndStats();
                    UpdateLogLinButtonText();
                };
            }

        }

        private (bool ok, string message) CalibrateLevel(bool isRight, double knownVrms)
        {
            if (knownVrms <= 0.0 || double.IsNaN(knownVrms) || double.IsInfinity(knownVrms))
                return (false, "Valeur Vrms invalide (doit être > 0).");

            // On calibre à partir du RMS AC en FS (DC retiré) — pas depuis le dBFS.
            double? measuredRmsFs = isRight ? _lastRmsFsRight : _lastRmsFsLeft;
            if (measuredRmsFs == null || measuredRmsFs.Value <= 0.0)
                return (false, "Pas de mesure RMS (FS) disponible. Lancez d'abord une acquisition (RUN), puis réessayez.");

            // Gain linéaire : Vrms / FS_rms (AC)
            double gain = knownVrms / measuredRmsFs.Value;

            // Offset appliqué aux courbes FFT : dBV = dBFS + offset
            double offset = OffsetDbFromGain(gain);

            if (isRight)
            {
                _levelCalValidRight = true;
                _levelGainVrmsPerFsRight = gain;
                _levelCalOffsetDbRight = offset;
            }
            else
            {
                _levelCalValidLeft = true;
                _levelGainVrmsPerFsLeft = gain;
                _levelCalOffsetDbLeft = offset;
            }

            UpdateLevelUnitUi();

            if (!isLive)
                UpdateSpectrumChartAndStats();

            string chan = isRight ? "R" : "L";
            double vrmsAt0Dbfs = gain / Math.Sqrt(2.0); // 0 dBFS (peak FS=1) -> Vrms
            return (true, $"Voie {chan} : RMS_FS(ac) = {measuredRmsFs.Value:F6}. Gain = {gain:G6} Vrms/FS. " +
                          $"Offset = {offset:+0.00;-0.00;0.00} dB. => 0 dBFS ≈ {vrmsAt0Dbfs:F6} Vrms");
        }

        //      
        public void ShowLevelCalibrationDialog(IWin32Window? owner = null)
        {
            var dlg = new LevelCalibrationDialog();

            // États init
            dlg.SetCurrentCalibration(
                _levelCalValidLeft, _levelCalOffsetDbLeft,
                _levelCalValidRight, _levelCalOffsetDbRight,
                _lastRmsDbfsLeft, _lastRmsDbfsRight);

            dlg.CalibrateLeft = (vrms) => CalibrateLevel(false, vrms);
            dlg.CalibrateRight = (vrms) => CalibrateLevel(true, vrms);

            dlg.ResetCalibration = () =>
            {
                ResetLevelCalibration();
                dlg.SetCurrentCalibration(
                    _levelCalValidLeft, _levelCalOffsetDbLeft,
                    _levelCalValidRight, _levelCalOffsetDbRight,
                    _lastRmsDbfsLeft, _lastRmsDbfsRight);
            };

            dlg.ShowDialog(this);


        }

        //
        private void UpdateRunButtonAppearance()
        {
            if (isLive)
            {
                btnRun.Text = "■  STOP";
                btnRun.BackColor = Color.Firebrick;
                btnRun.ForeColor = Color.White;
            }
            else
            {
                btnRun.Text = "▶  RUN";
                btnRun.BackColor = Color.SeaGreen;
                btnRun.ForeColor = Color.White;
            }
        }

        public void StartFromRun(bool onceMode)
        {
            // SpectrumControl ne démarre plus de calcul FFT.
            // Il se contente d’afficher les frames externes reçues via FeedExternalSpectrum().
            _hasEverRun = true;
            isLive = true;
            UpdateRunButtonAppearance();
        }

        public void StopFromRun()
        {
            isLive = false;
            UpdateRunButtonAppearance();
        }

        //
        private void ResetAveragingState()
        {
            _post.ResetAveragingState();
            _post.ResetFrfState();
            _post.ResetCohAvgState();
            _cohAvgLeftDb  = null;
            _cohAvgRightDb = null;

            if (_avgIsExponential)
                UpdateAvgProgressUi(0, 0);
            else
                UpdateAvgProgressUi(0, avgTargetCount);
        }

        private void BtnResetPeakHold_Click(object? sender, EventArgs e)
        {
            _post.ResetPeakHoldState();
            UpdateSpectrumChartAndStats();
        }

        private void InitAxisButtonUi()
        {
            if (_btnAxis != null)
                return; // already initialized

            // Parent it to the chart so it stays positioned relative to the graph area.
            _btnAxis = new Button();
            _btnAxis.Name = "btnAxis";
            _btnAxis.Text = "Axis Setup";
            _btnAxis.AutoSize = true;
            _btnAxis.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            _btnAxis.Padding = new Padding(8, 2, 8, 2);
            _btnAxis.FlatStyle = FlatStyle.Flat;
            _btnAxis.FlatAppearance.BorderSize = 1;
            _btnAxis.TabStop = false;
            _btnAxis.Cursor = Cursors.Hand;

            // Keep the button attached to the bottom-left corner of its parent.
            _btnAxis.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;

            // Slightly translucent background (still readable).
            // Note: WinForms button doesn't support true alpha in all themes, but this is safe.
            _btnAxis.BackColor = Color.FromArgb(230, SystemColors.Control);

            // Open the same dialog as the existing right-click on the chart.
            _btnAxis.Click += (_, __) => ShowAxisDialog();

            // Tooltip (optional but nice UX)
            _toolTipAxis ??= new ToolTip();
            _toolTipAxis.SetToolTip(_btnAxis, "Axis Parameters");

            chartSpectrum.Controls.Add(_btnAxis);
            _btnAxis.BringToFront();

            // Initial placement + keep in place on resize.
            PositionAxisButton();
            chartSpectrum.Resize += (_, __) => PositionAxisButton();
        }

        private void PositionAxisButton()
        {
            if (_btnAxis == null)
                return;

            const int marginPx = 6;

            int x = marginPx;
            int y = Math.Max(marginPx, chartSpectrum.ClientSize.Height - _btnAxis.Height - marginPx);

            _btnAxis.Location = new Point(x, y);
            _btnAxis.BringToFront();
        }

        //
        private void ShowAxisDialog()
        {
            if (chartSpectrum.ChartAreas.Count == 0)
                return;

            var area = chartSpectrum.ChartAreas["SpectrumArea"];

            // Valeurs actuelles (si custom non null, on les utilise)
            double yMinCurrent = customYMin ?? area.AxisY.Minimum;
            double yMaxCurrent = customYMax ?? area.AxisY.Maximum;
            double fMinCurrent = customFMin ?? area.AxisX.Minimum;   // Hz
            double fMaxCurrent = customFMax ?? area.AxisX.Maximum;   // Hz

            using (var dlg = new SpectrumAxisDialog())
            {
                // Main Y axis unit must be set before injecting YMin/YMax (so the dialog can clamp/round correctly).
                string modeForUnit = (comboChannel?.SelectedItem as string) ?? "L+R";
                bool useDbvAxis = UseDbvForMode(modeForUnit);

                if (string.Equals(modeForUnit, "Bode Plot", StringComparison.OrdinalIgnoreCase))
                    dlg.MainYScale = SpectrumAxisDialog.YScaleMode.DbGain;
                else if (useDbvAxis)
                    dlg.MainYScale = SpectrumAxisDialog.YScaleMode.Dbv;
                else
                    dlg.MainYScale = SpectrumAxisDialog.YScaleMode.Dbfs;

                // IMPORTANT: set YMax first, then YMin (YMin is derived via the selected range).
                dlg.YMax = yMaxCurrent;
                dlg.YMin = yMinCurrent;
                dlg.FMinHz = fMinCurrent;   // *** en Hz ***
                dlg.FMaxHz = fMaxCurrent;   // *** en Hz ***

                // Y2 Phase (deg) — uniquement si l'affichage phase est actif
                bool phaseUiEnabled = IsFrfMode() && _frfPhaseEnabled;
                dlg.PhaseRangeEnabled = phaseUiEnabled;

                // IMPORTANT : set PhaseMax first, then PhaseMin (PhaseMin is derived via the selected range).
                dlg.PhaseMaxDeg = customPhaseMax ?? 180.0;
                dlg.PhaseMinDeg = customPhaseMin ?? -180.0;



                dlg.SampleRateHz = _sampleRate;
                // 🔧 Ajuster dynamiquement la borne max de Fmax
                double fmaxCap = SpectrumAxisDialog.GetAllowedFmax(_sampleRate);
                dlg.SetFmaxMaximum(fmaxCap);

                dlg.FitYProvider = GetCurrentYRangeGlobal;
                dlg.FitY2Provider = GetVisiblePhaseRangeDeg_ForFitY2;
                dlg.FftSizeN = lastFftSize;


                if (dlg.ShowDialog(FindForm()) == DialogResult.OK)
                {
                    customYMin = dlg.YMin;
                    customYMax = dlg.YMax;

                    customFMin = dlg.FMinHz;   // plus de *1000
                    customFMax = dlg.FMaxHz;

                    if (phaseUiEnabled)
                    {
                        customPhaseMin = dlg.PhaseMinDeg;
                        customPhaseMax = dlg.PhaseMaxDeg;
                    }
                    else
                    {
                        // Si phase non affichée, on ne force pas une plage custom
                        customPhaseMin = null;
                        customPhaseMax = null;
                    }

                    if (customYMin >= customYMax)
                        customYMin = customYMax - 10.0;

                    // Valider customFMin selon le mode actuel :
                    //   • LIN : 0 Hz valide, négatif → 0
                    //   • LOG : la valeur saisie doit être >= la décade inférieure de ΔF.
                    //           Si elle est inférieure ou nulle → null (= auto).
                    //           On utilise _sampleRate/lastFftSize directement (toujours
                    //           à jour, fiable même si freqAxis est transitoirement vide).
                    if (chkLogX?.Checked == true)
                    {
                        if (customFMin.HasValue && _sampleRate > 0 && lastFftSize > 0)
                        {
                            double deltaF    = (double)_sampleRate / lastFftSize;
                            double minAllowed = Math.Pow(10.0, Math.Floor(Math.Log10(deltaF)));
                            if (customFMin.Value <= 0 || customFMin.Value < minAllowed)
                                customFMin = null;
                        }
                        else if (customFMin.HasValue && customFMin.Value <= 0)
                        {
                            customFMin = null;
                        }
                    }
                    else
                    {
                        if (customFMin < 0.0)
                            customFMin = 0.0;
                    }
                    if (customFMax <= customFMin)
                        customFMax = customFMin + 10.0;

                    ResetAveragingState();
                    UpdateSpectrumChartAndStats();

                }
            }

        }

        private void OpenNoiseToPeakThresholdDialog()
        {
            using var dlg = new NoiseToPeakThresholdDialog(
                currentValueDb: NoiseToPeakThresholdDb,
                minDb: NoiseToPeakThresholdMinDb,
                maxDb: NoiseToPeakThresholdMaxDb);

            if (dlg.ShowDialog(this.FindForm()) == DialogResult.OK)
            {
                NoiseToPeakThresholdDb = dlg.ValueDb;
                // Pas besoin de recalcul automatique : la valeur sera prise en compte au prochain < / >
            }
        }

        //
        private static string FormatHzK(double hz)
        {
            if (hz < 1.0)
            {
                // Sous 1 Hz : affiche en mHz si < 1 Hz, ou directement si >= 0.1
                if (hz >= 0.1) return hz.ToString("0.##");
                if (hz >= 0.01) return hz.ToString("0.###");
                return hz.ToString("G3"); // ex. 1E-4 pour de très basses fréquences
            }
            if (hz >= 1000.0)
                return (hz / 1000.0).ToString("0.#") + "k";
            return hz.ToString("0");
        }

        private void ApplyAxisXModeSafe(ChartArea area, bool useLog, double fMin, double fMax)
        {
            // Normalisation des bornes
            if (double.IsNaN(fMin) || double.IsInfinity(fMin)) fMin = 0.0;
            if (double.IsNaN(fMax) || double.IsInfinity(fMax)) fMax = fMin + 1.0;

            if (useLog)
            {
                // LOG => min strictement > 0
                fMin = Math.Max(LogXAbsMinHz, fMin);

                // ── RÈGLE ABSOLUE en LOG : fMin >= décade inférieure de ΔF ────────────
                // Calcul de ΔF directement depuis les paramètres FFT instantanés
                // (_sampleRate et lastFftSize sont mis à jour AVANT ApplyChartAxes).
                // Ce calcul ne dépend ni de freqAxis (qui peut être null/vide), ni de
                // customFMin (qui peut persister depuis une autre config).
                //
                // Exemple : SR = 192 kHz, N = 32 768 → ΔF = 5,86 Hz → décade = 1 Hz
                //   → fMin sera >= 1 Hz, peu importe ce qui était passé en argument.
                //
                // C'est l'application stricte de la règle énoncée :
                //   "ΔF du SR en cours détermine la décade minimum autorisée pour LOG"
                if (_sampleRate > 0 && lastFftSize > 0)
                {
                    double deltaF       = (double)_sampleRate / lastFftSize;
                    double minDecadeFft = Math.Pow(10.0, Math.Floor(Math.Log10(deltaF)));
                    if (fMin < minDecadeFft)
                        fMin = minDecadeFft;
                }

                // Snap à la décade inférieure (sécurité — corrige le bug original
                // des labels majeurs invisibles, et idempotent si fMin est déjà une décade)
                if (fMin > 0.0)
                {
                    double decade = Math.Pow(10.0, Math.Floor(Math.Log10(fMin)));
                    fMin = Math.Max(LogXAbsMinHz, decade);
                }

                if (fMax <= fMin) fMax = fMin * 10.0;
            }
            else
            {
                // LIN => min >= 0 acceptable
                if (fMax <= fMin) fMax = fMin + 1.0;
            }

            // Application "atomique" :
            // 1) repasser en LIN (évite l’état transitoire invalide)
            // 2) poser les bornes
            // 3) repasser en LOG si besoin
            bool wasLog = area.AxisX.IsLogarithmic;
            if (wasLog == useLog)
            {
                // Mode inchangé — mettre à jour les bornes SANS toggler IsLogarithmic.
                // Ce toggle est la cause du bug WinForms.DataVisualization :
                // InnerPlotPosition.Height se recalcule à ~97-99%, repoussant les labels
                // X en dessous de la zone visible. On l'évite quand le mode ne change pas.
                area.AxisX.Minimum = fMin;
                area.AxisX.Maximum = fMax;
            }
            else
            {
                // Changement de mode LOG↔LIN : toggle atomique nécessaire.
                area.AxisX.IsLogarithmic = false;
                area.AxisX.Minimum = fMin;
                area.AxisX.Maximum = fMax;
                area.AxisX.IsLogarithmic = useLog;
            }
        }

        //
        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);

            // On laisse le live tick faire le refresh quand live,
            // sinon on peut rafraîchir directement.
            if (!isLive)
                UpdateSpectrumChartAndStats();
        }

        //
        private void UpdateToggleButtonVisual(CheckBox? b)
        {
            if (b == null) return;

            if (b.Checked)
            {
                // IMPORTANT sinon Windows peut ignorer BackColor
                b.UseVisualStyleBackColor = false;
                b.BackColor = Color.LightGreen;
                b.ForeColor = Color.Black;
            }
            else
            {
                // Revenir au rendu standard
                b.UseVisualStyleBackColor = true;
                b.BackColor = SystemColors.Control;
                b.ForeColor = SystemColors.ControlText;
            }
        }

        //
        private double GetFrequencyFromMouseX(int mouseX)
        {
            var area = chartSpectrum.ChartAreas["SpectrumArea"];
            bool useLog = (chkLogX != null && chkLogX.Checked);

            double xMinVal = area.AxisX.Minimum;
            double xMaxVal = area.AxisX.Maximum;

            if (xMaxVal <= xMinVal)
                return xMinVal;

            // Récupère les positions pixels du min/max axe X
            double pxMin = area.AxisX.ValueToPixelPosition(xMinVal);
            double pxMax = area.AxisX.ValueToPixelPosition(xMaxVal);

            // Si quelque chose est inversé ou invalide, fallback
            if (double.IsNaN(pxMin) || double.IsNaN(pxMax) || Math.Abs(pxMax - pxMin) < 1e-6)
            {
                try
                {
                    return area.AxisX.PixelPositionToValue(mouseX);
                }
                catch
                {
                    return xMinVal;
                }
            }

            // Position relative 0..1 dans la zone de tracé
            double t = (mouseX - pxMin) / (pxMax - pxMin);
            if (t < 0.0) t = 0.0;
            if (t > 1.0) t = 1.0;

            if (!useLog)
            {
                return xMinVal + t * (xMaxVal - xMinVal);
            }

            // LOG : f = min * (max/min)^t
            // (valeurs doivent être > 0)
            double min = Math.Max(1e-12, xMinVal);
            double max = Math.Max(min * 1.000001, xMaxVal);

            return min * Math.Pow(max / min, t);
        }

        //
        private void ClearSpectrum()
        {
            // Supprime les overlays dynamiques et vide les séries pré-allouées.
            // Ne jamais appeler chartSpectrum.Series.Clear() : cela supprimerait
            // les séries fixes (_serLeft etc.) et casserait EnsureSeriesInited.
            RemoveOverlaySeries();

            if (_seriesInited)
            {
                FillSeries(_serLeft!,      null, null, 0, false, 0.0);
                FillSeries(_serRight!,     null, null, 0, false, 0.0);
                FillSeries(_serPeakLeft!,  null, null, 0, false, 0.0);
                FillSeries(_serPeakRight!, null, null, 0, false, 0.0);
                FillSeries(_serPhase!,     null, null, 0, false, 0.0);
            }
            else
            {
                chartSpectrum.Series.Clear();  // safe fallback avant init
            }

            // Axe X: état safe (bornes valides, mode log préservé)
            if (chartSpectrum.ChartAreas.Count > 0)
            {
                var axArea = chartSpectrum.ChartAreas[0];
                axArea.AxisX.StripLines.Clear();
                bool useLogNow = (chkLogX != null && chkLogX.Checked);
                axArea.AxisX.IsLogarithmic = false;
                double xMaxS = (_sampleRate > 0) ? (_sampleRate / 2.0) : 1.0;
                double xMinS = useLogNow ? 1.0 : 0.0;
                if (xMaxS <= xMinS) xMaxS = xMinS * 10.0;
                axArea.AxisX.Minimum = xMinS;
                axArea.AxisX.Maximum = xMaxS;
                axArea.AxisX.IsLogarithmic = useLogNow;
                axArea.AxisX.MajorGrid.Enabled = true;
                axArea.AxisX.MinorGrid.Enabled = false;
                ApplyLogXMinorGridStripLines(axArea.AxisX);
            }

            // Labels stats
            if (lblLeftRms  != null) lblLeftRms.Text  = "RMS:";
            if (lblLeftPeak != null) lblLeftPeak.Text  = "Peak:";
            if (lblRightRms != null) lblRightRms.Text  = "";
            if (lblRightPeak!= null) lblRightPeak.Text = "";

            if (_overlayInfoAnno != null)
                _overlayInfoAnno.Visible = false;

            currentLeftSpectrum  = null;
            currentRightSpectrum = null;
            lastFftSize          = 0;
            _plotFreqAxis        = null;
            _plotLeftSpectrum    = null;
            _plotRightSpectrum   = null;
            _plotPeakLeft        = null;
            _plotPeakRight       = null;
            _plotFrfPhaseDeg     = null;
            freqAxis             = null;
            lastTimeLeft         = null;
            lastTimeRight        = null;
        }

        //
        private void LblLeftMeas_Click(object sender, EventArgs e)
        {

        }

        //
        private void LblRightMeas_Click(object sender, EventArgs e)
        {

        }

        private void ChkEnableThd_CheckedChanged(object sender, EventArgs e)
        {
            if (_suppressThdUiEvents)
                return;

            bool enabled = chkEnableThd.Checked;

            // Apply immediately (affichage + calculs)
            SetThdMeasuresEnabled(enabled);

            // Update visual (in case SetThdMeasuresEnabled modified something)
            UpdateToggleButtonVisual(chkEnableThd);

            // Let Form1 persist it (SessionSettings)
            ThdEnabledChanged?.Invoke(enabled);
        }

        private void BtnThdSettings_Click(object sender, EventArgs e)
        {
            ThdSettingsRequested?.Invoke();
        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void chkPeakHold_CheckedChanged(object sender, EventArgs e)
        {

        }


        //
        private static (int Segments, int StepHz, int SpanAdjustedHz) ChooseBestStepWithSpanAdjustment(int spanHz)
        {
            // 1) Si span exact => on prend direct
            if (TryChooseBestExactStep(spanHz, out int seg, out int step))
                return (seg, step, spanHz);

            // 2) Sinon, on ajuste span autour de spanHz (recherche locale)
            // Recherche dans une fenêtre raisonnable +/- 2000 Hz (modifiable)
            const int searchRadius = 2000;

            int bestSeg = 0;
            int bestStep = 0;
            int bestSpanAdj = spanHz;
            int bestZeros = -1;
            int bestDelta = int.MaxValue;

            for (int delta = 1; delta <= searchRadius; delta++)
            {
                // On teste spanHz - delta puis spanHz + delta (symétrique)
                int[] spansToTry = { spanHz - delta, spanHz + delta };

                foreach (int candidateSpan in spansToTry)
                {
                    if (candidateSpan <= 0) continue;

                    if (TryChooseBestExactStep(candidateSpan, out int s, out int st))
                    {
                        int zeros = CountTrailingZeros(st);

                        // priorité 1 : max trailing zeros
                        // priorité 2 : delta minimal (span ajusté le plus proche)
                        // tie-break : step le plus grand
                        if (zeros > bestZeros ||
                            (zeros == bestZeros && delta < bestDelta) ||
                            (zeros == bestZeros && delta == bestDelta && st > bestStep))
                        {
                            bestZeros = zeros;
                            bestDelta = delta;
                            bestSeg = s;
                            bestStep = st;
                            bestSpanAdj = candidateSpan;
                        }
                    }
                }

                // petit early-exit : si on trouve 4 zéros (ex 10000), c'est déjà excellent
                if (bestZeros >= 4)
                    break;
            }

            // fallback absolu (ne devrait pas arriver) : 6 segments
            if (bestSeg == 0)
            {
                bestSeg = 6;
                bestStep = Math.Max(1, spanHz / bestSeg);
                bestSpanAdj = bestStep * bestSeg;
            }

            return (bestSeg, bestStep, bestSpanAdj);
        }


    }
}
