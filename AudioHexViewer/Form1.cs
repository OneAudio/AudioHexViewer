using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Globalization;
using System.Text;
// ...


namespace AudioHexViewer
{
    public partial class Form1 : Form

    {
        // --- CHAMPS PRIVÉS ---
        private WasapiCapture? capture;   // pour la capture audio
        private MMDevice? selectedDevice; // périphérique audio choisi
        private WaveFormat? currentFormat;

        // Suivi de stabilité des bits (24 bits) sur 1 seconde
        private readonly bool[] lastBitsLeft = new bool[24];
        private readonly bool[] lastBitsRight = new bool[24];
        private readonly DateTime[] lastChangeLeft = new DateTime[24];
        private readonly DateTime[] lastChangeRight = new DateTime[24];
        private readonly TimeSpan bitStabilityThreshold = TimeSpan.FromSeconds(0.4);

        // Saturation par voie
        private DateTime? lastSatLeft = null;
        private DateTime? lastSatRight = null;
        private readonly TimeSpan satDisplayTime = TimeSpan.FromSeconds(1.5);   // durée totale d’affichage SAT
        private readonly TimeSpan satBlinkPeriod = TimeSpan.FromMilliseconds(400); // période du clignotement
        private bool isCurrentlySatLeft = false;
        private bool isCurrentlySatRight = false;


        private bool captureOutput = false; // false = IN (capture), true = OUT (loopback)


        // --- Enregistrement 1 seconde ---
        private bool isRecordingOneSecond = false;
        private int recordSamplesNeeded = 0;
        private int recordSamplesCollected = 0;

        // Seuils dBFS pour le bargraphe
        private const double DbMin = -80.0;      // plancher d'affichage
        private const double DbBlueToGreen = -36.0;
        private const double DbGreenToOrange = -12.0;
        private const double DbOrangeToRed = -6.0;
        private const int LedCount = 40; // nombre de segments "LED" par barre

        // --- Calibration tension ---
        // G et D séparées
        private double calibGainLeft = 1.0;
        private double calibOffsetLeft = 0.0;
        private bool calibValidLeft = false;

        private double calibGainRight = 1.0;
        private double calibOffsetRight = 0.0;
        private bool calibValidRight = false;

        // Codes 24 bits mémorisés en continu
        private int lastCodeLeft;
        private int lastCodeRight;

        // Timer de calibration (moyenne)
        private readonly System.Windows.Forms.Timer calibTimer;
        private int calibTicks = 0;
        private int calibTicksTarget = 0;
        private long calibAccum = 0;

        private enum CalStage
        {
            None,
            Positive,
            Negative
        }

        private enum CalChannel
        {
            None,
            Left,
            Right
        }

        private CalStage calibStage = CalStage.None;
        private CalChannel calibChannel = CalChannel.None;

        // Flags pour savoir quoi calibrer
        private bool calibDoLeft = false;
        private bool calibDoRight = false;

        // Variables intermédiaires
        private double calibCodePos = 0.0;
        private double calibCodeNeg = 0.0;
        private double calibVPos = 0.0;
        private double calibVNeg = 0.0;

        // Rafraîchissement tension max
        private readonly TimeSpan voltageUpdateInterval = TimeSpan.FromMilliseconds(250);
        private DateTime lastVoltageUpdate = DateTime.MinValue;
        private double displayVoltsLeft = 0.0;
        private double displayVoltsRight = 0.0;

        private double DbToPercent(double db)
        {
            // mappe [DbMin..0] vers [0..100]
            if (db <= DbMin) return 0.0;
            if (db >= 0.0) return 100.0;
            return (db - DbMin) / (0.0 - DbMin) * 100.0;
        }

        private class RecordFrame
        {
            public int Left;
            public int Right;
        }

        private List<RecordFrame> recordBuffer = new List<RecordFrame>();


        public Form1()
        {
            InitializeComponent();


            // Init des horodatages de changements de bits
            var now = DateTime.Now;
            for (int i = 0; i < 24; i++)
            {
                lastChangeLeft[i] = now;
                lastChangeRight[i] = now;
                lastBitsLeft[i] = false;
                lastBitsRight[i] = false;
            }

            comboDevices.DisplayMember = "FriendlyName";
            LoadDevices();

            UpdateStartButtonAppearance();

            comboDevices.SelectedIndexChanged += comboDevices_SelectedIndexChanged;

            // Timer calibration
            calibTimer = new System.Windows.Forms.Timer();
            calibTimer.Interval = 50;
            calibTimer.Tick += CalibTimer_Tick;

            // Charger la calibration existante
            LoadCalibration();

            // Créer les repères dBFS au-dessus des barres
            CreateDbMarkers(
                barLeftLevel,
                new[] { "-80", "-36", "-12", "-6", "0" },
                new[] { -80, DbBlueToGreen, DbGreenToOrange, DbOrangeToRed, 0.0 }
            );

            CreateDbMarkers(
                barRightLevel,
                new[] { "-80", "-36", "-12", "-6", "0" },
                new[] { -80, DbBlueToGreen, DbGreenToOrange, DbOrangeToRed, 0.0 }
            );

        }


        private string GroupBits(string bits, int groupSize = 4)
        {
            var sb = new StringBuilder(bits.Length + bits.Length / groupSize);

            for (int i = 0; i < bits.Length; i++)
            {
                if (i > 0 && i % groupSize == 0)
                    sb.Append(' ');

                sb.Append(bits[i]);
            }

            return sb.ToString();
        }

        private string FormatRow(string label, string value)
        {
            // 14 caractères pour la colonne "label", à ajuster si besoin
            const int labelWidth = 14;
            if (label.Length > labelWidth)
                label = label.Substring(0, labelWidth);

            string paddedLabel = label.PadRight(labelWidth);

            return $"{paddedLabel} : {value}";
        }
        //
        //
        private void menuDeviceInfo_Click(object? sender, EventArgs e)
        {
            string text = BuildDeviceInfoText();

            using (var dlg = new Form())
            {
                dlg.Text = "Audio device info";
                dlg.StartPosition = FormStartPosition.CenterParent;
                dlg.Size = new Size(500, 350);
                dlg.MinimizeBox = false;
                dlg.MaximizeBox = false;
                dlg.ShowIcon = false;

                var box = new RichTextBox();
                box.Dock = DockStyle.Fill;
                box.ReadOnly = true;
                box.BorderStyle = BorderStyle.None;
                box.Font = new Font(FontFamily.GenericMonospace, 9f);
                box.Text = text;
                box.BackColor = SystemColors.Window;

                dlg.Controls.Add(box);

                dlg.ShowDialog(this);
            }
        }

        //
        //
        private void menuAbout_Click(object? sender, EventArgs e)
        {
            MessageBox.Show(
                "AudioHexViewer\n\n" +
                "Affichage des données audio 24 bits, bargraph dBFS, saturation,\n" +
                "enregistrement CSV et calibration tension.\n\n" +
                "OnE Audio Projects",
                "About",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        //
        private void UpdateStartButtonAppearance()
        {
            if (capture == null)
            {
                // Mode arrêt --> bouton READ vert
                btnStart.Text = "READ";
                btnStart.BackColor = Color.SeaGreen;
                btnStart.ForeColor = Color.White;
            }
            else
            {
                // Mode en cours de capture --> bouton STOP rouge
                btnStart.Text = "STOP";
                btnStart.BackColor = Color.Firebrick;
                btnStart.ForeColor = Color.White;
            }
        }
        //

        private void menuOpenCal_Click(object? sender, EventArgs e)
        {
            string path = GetCalibrationFilePath();
            if (!File.Exists(path))
            {
                MessageBox.Show("Aucun fichier de calibration trouvé.",
                                "Open calibration file",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Information);
                return;
            }

            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show("Impossible d'ouvrir le fichier de calibration : " + ex.Message,
                                "Open calibration file",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        //
        private void LoadDevices()
        {
            comboDevices.Items.Clear();

            var enumerator = new MMDeviceEnumerator();

            // IN -> périphériques de capture, OUT -> périphériques de rendu
            var flow = captureOutput ? DataFlow.Render : DataFlow.Capture;

            var devices = enumerator.EnumerateAudioEndPoints(flow, DeviceState.Active);
            foreach (var dev in devices)
            {
                // Tu avais déjà un DeviceItem qui expose FriendlyName
                comboDevices.Items.Add(dev);
            }

            if (comboDevices.Items.Count > 0)
                comboDevices.SelectedIndex = 0;
        }

        private void comboDevices_SelectedIndexChanged(object? sender, EventArgs e)
        {
            // On n’est plus en train de capturer si on change de device
            currentFormat = null;
            UpdateDeviceInfo();
        }


        private void btnStart_Click(object sender, EventArgs e)
        {
            // Si on n'est PAS en train de capturer -> on démarre (mode READ)
            if (capture == null)
            {
                if (comboDevices.SelectedItem is not MMDevice dev)
                {
                    MessageBox.Show("Sélectionne un périphérique audio.");
                    return;
                }

                selectedDevice = dev;

                try
                {
                    // === ICI : on choisit IN ou OUT ===
                    if (!captureOutput)
                    {
                        // Mode IN : périphérique d'entrée (micro / ADC)
                        capture = new WasapiCapture(selectedDevice);
                    }
                    else
                    {
                        // Mode OUT : périphérique de sortie (loopback)
                        capture = new WasapiLoopbackCapture(selectedDevice);
                    }
                    // ================================

                    currentFormat = capture.WaveFormat;

                    capture.DataAvailable += Capture_DataAvailable;
                    capture.RecordingStopped += Capture_RecordingStopped;

                    capture.StartRecording();

                    UpdateStartButtonAppearance();
                    UpdateDeviceInfo();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur lors du démarrage de la capture : " + ex.Message);
                    capture = null;
                    //btnStart.Text = "READ";
                    currentFormat = null;
                    UpdateStartButtonAppearance();   // remet en mode READ vert
                    UpdateDeviceInfo();
                }
            }
            else
            {
                // Si on est déjà en train de capturer -> clique = STOP

                try
                {
                    capture.DataAvailable -= Capture_DataAvailable;
                    capture.RecordingStopped -= Capture_RecordingStopped;
                    capture.StopRecording();
                }
                catch
                {
                    // on ignore les erreurs de StopRecording
                }

                capture.Dispose();
                capture = null;
                currentFormat = null;

                UpdateStartButtonAppearance();       // remet READ vert
                UpdateDeviceInfo();
            }
        }

        private void rbInput_CheckedChanged(object? sender, EventArgs e)
        {
            if (rbInput.Checked)
            {
                captureOutput = false;  // mode IN
                LoadDevices();
            }
        }

        private void rbOutput_CheckedChanged(object? sender, EventArgs e)
        {
            if (rbOutput.Checked)
            {
                captureOutput = true;   // mode OUT
                LoadDevices();
            }
        }

        // ajouté à cause des erreurs enntre ICI ...

        private void LblSatLeft_Click(object sender, EventArgs e)
        {
        }

        private void barLeftLevel_Click(object sender, EventArgs e)
        {
        }
        private void lblLeftLevel_Click(object sender, EventArgs e)
        {
        }

        private void barRightLevel_Click(object sender, EventArgs e)
        {
        }

        private void label2_Click(object sender, EventArgs e)
        {
            // Handler laissé vide pour satisfaire le Designer
        }

        // ... et là
        //
        //
        //
        private void btnCalV_Click(object sender, EventArgs e)
        {
            if (capture == null || currentFormat == null)
            {
                MessageBox.Show("Démarre la capture (READ) avant de lancer la calibration.",
                                "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (calibTimer.Enabled || calibStage != CalStage.None)
            {
                MessageBox.Show("Une calibration est déjà en cours.",
                                "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Demander quelles voies calibrer
            var rLeft = MessageBox.Show("Calibrer la voie GAUCHE ?", "Calibration",
                                        MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            calibDoLeft = (rLeft == DialogResult.Yes);

            var rRight = MessageBox.Show("Calibrer la voie DROITE ?", "Calibration",
                                         MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            calibDoRight = (rRight == DialogResult.Yes);

            if (!calibDoLeft && !calibDoRight)
            {
                MessageBox.Show("Aucune voie à calibrer. Calibration annulée.",
                                "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            // Avertir si calibration existante
            if ((calibDoLeft && calibValidLeft) || (calibDoRight && calibValidRight))
            {
                var r = MessageBox.Show(
                    "Une calibration existe déjà pour au moins une voie. Voulez-vous la remplacer ?",
                    "Calibration",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (r != DialogResult.Yes)
                    return;
            }

            // On commence par la voie gauche si demandée, sinon la droite
            if (calibDoLeft)
            {
                StartChannelCalibration(CalChannel.Left);
            }
            else
            {
                StartChannelCalibration(CalChannel.Right);
            }
        }
        //
        //
        private void StartChannelCalibration(CalChannel channel)
        {
            calibChannel = channel;

            string chanName = (channel == CalChannel.Left) ? "GAUCHE" : "DROITE";

            MessageBox.Show(
                $"Voie {chanName} :\r\n" +
                "Applique une tension POSITIVE (1 à 5 V),\r\n" +
                "puis clique sur OK pour démarrer la mesure sur ~1 s.",
                "Calibration +V",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            calibStage = CalStage.Positive;
            calibAccum = 0;
            calibTicks = 0;
            double durationSeconds = 1.0;  // facilement modifiable
            calibTicksTarget = (int)(durationSeconds * 1000.0 / calibTimer.Interval);

            calibTimer.Start();
        }

        //
        //
        private void CalibTimer_Tick(object? sender, EventArgs e)
        {
            int code = 0;

            if (calibChannel == CalChannel.Left)
                code = lastCodeLeft;
            else if (calibChannel == CalChannel.Right)
                code = lastCodeRight;
            else
                return;

            calibAccum += code;
            calibTicks++;

            if (calibTicks >= calibTicksTarget)
            {
                calibTimer.Stop();

                double avgCode = calibTicks > 0 ? (double)calibAccum / calibTicks : 0.0;

                if (calibStage == CalStage.Positive)
                {
                    calibCodePos = avgCode;
                    calibStage = CalStage.None;
                    FinishPositiveCalibration();
                }
                else if (calibStage == CalStage.Negative)
                {
                    calibCodeNeg = avgCode;
                    calibStage = CalStage.None;
                    FinishNegativeCalibration();
                }
            }
        }

        //
        private void FinishPositiveCalibration()
        {
            string chanName = (calibChannel == CalChannel.Left) ? "GAUCHE" : "DROITE";

            string? input = Microsoft.VisualBasic.Interaction.InputBox(
                $"Voie {chanName} :\r\n" +
                "Mesure terminée.\r\n" +
                "Saisis la valeur de la tension POSITIVE appliquée (en volts, ex: 2.500) :",
                "Calibration +V",
                "2.500");

            if (!double.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out calibVPos))
            {
                MessageBox.Show("Valeur non valide, calibration annulée.",
                                "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                calibStage = CalStage.None;
                calibChannel = CalChannel.None;
                return;
            }

            MessageBox.Show(
                $"Voie {chanName} :\r\n" +
                "Maintenant, applique une tension NÉGATIVE (1 à 5 V),\r\n" +
                "puis clique sur OK pour démarrer la mesure sur ~1 s.",
                "Calibration -V",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            calibStage = CalStage.Negative;
            calibAccum = 0;
            calibTicks = 0;
            double durationSeconds = 1.0;
            calibTicksTarget = (int)(durationSeconds * 1000.0 / calibTimer.Interval);
            calibTimer.Start();
        }

        //
        private void FinishNegativeCalibration()
        {
            string chanName = (calibChannel == CalChannel.Left) ? "GAUCHE" : "DROITE";

            string? input = Microsoft.VisualBasic.Interaction.InputBox(
                $"Voie {chanName} :\r\n" +
                "Mesure terminée.\r\n" +
                "Saisis la valeur de la tension NÉGATIVE appliquée (en volts, ex: -2.500) :",
                "Calibration -V",
                "-2.500");

            if (!double.TryParse(input.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out calibVNeg))
            {
                MessageBox.Show("Valeur non valide, calibration annulée.",
                                "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                calibStage = CalStage.None;
                calibChannel = CalChannel.None;
                return;
            }

            if (Math.Abs(calibCodePos - calibCodeNeg) < 1.0)
            {
                MessageBox.Show("Codes de calibration trop proches, impossible de calculer gain/offset.",
                                "Calibration", MessageBoxButtons.OK, MessageBoxIcon.Error);
                calibStage = CalStage.None;
                calibChannel = CalChannel.None;
                return;
            }

            double gain = (calibVPos - calibVNeg) / (calibCodePos - calibCodeNeg);
            double offset = calibVPos - gain * calibCodePos;

            if (calibChannel == CalChannel.Left)
            {
                calibGainLeft = gain;
                calibOffsetLeft = offset;
                calibValidLeft = true;
            }
            else if (calibChannel == CalChannel.Right)
            {
                calibGainRight = gain;
                calibOffsetRight = offset;
                calibValidRight = true;
            }

            MessageBox.Show(
                $"Voie {chanName} : calibration terminée.\r\n\r\n" +
                $"Gain   = {gain:F8} V/code\r\n" +
                $"Offset = {offset:F8} V",
                "Calibration",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);

            // Passer à l'autre voie si demandé
            if (calibChannel == CalChannel.Left && calibDoRight)
            {
                // On a fini gauche, on passe à droite
                StartChannelCalibration(CalChannel.Right);
            }
            else if (calibChannel == CalChannel.Right && calibDoLeft)
            {
                // (si un jour tu veux inverser l'ordre)
                StartChannelCalibration(CalChannel.Left);
            }
            else
            {
                // fin totale
                calibStage = CalStage.None;
                calibChannel = CalChannel.None;
                SaveCalibration();
            }
        }

        //
        //
        private string GetCalibrationFilePath()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(baseDir, "AudioHexViewer.cal");
        }

        private void SaveCalibration()
        {
            string path = GetCalibrationFilePath();

            try
            {
                using (var sw = new StreamWriter(path, false))
                {
                    sw.WriteLine("GainLeft=" + calibGainLeft.ToString("R", CultureInfo.InvariantCulture));
                    sw.WriteLine("OffsetLeft=" + calibOffsetLeft.ToString("R", CultureInfo.InvariantCulture));
                    sw.WriteLine("ValidLeft=" + calibValidLeft);

                    sw.WriteLine("GainRight=" + calibGainRight.ToString("R", CultureInfo.InvariantCulture));
                    sw.WriteLine("OffsetRight=" + calibOffsetRight.ToString("R", CultureInfo.InvariantCulture));
                    sw.WriteLine("ValidRight=" + calibValidRight);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'écriture de la calibration : " + ex.Message,
                                "Calibration",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }

        private void LoadCalibration()
        {
            string path = GetCalibrationFilePath();
            if (!File.Exists(path))
            {
                calibValidLeft = false;
                calibValidRight = false;
                return;
            }

            try
            {
                string[] lines = File.ReadAllLines(path);

                double gL = 1.0, oL = 0.0;
                double gR = 1.0, oR = 0.0;
                bool vL = false, vR = false;
                bool gLOk = false, oLOk = false, gROk = false, oROk = false;

                foreach (var line in lines)
                {
                    var parts = line.Split('=', 2);
                    if (parts.Length != 2) continue;

                    string key = parts[0].Trim();
                    string val = parts[1].Trim();

                    if (string.Equals(key, "GainLeft", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out gL))
                            gLOk = true;
                    }
                    else if (string.Equals(key, "OffsetLeft", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out oL))
                            oLOk = true;
                    }
                    else if (string.Equals(key, "ValidLeft", StringComparison.OrdinalIgnoreCase))
                    {
                        bool.TryParse(val, out vL);
                    }
                    else if (string.Equals(key, "GainRight", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out gR))
                            gROk = true;
                    }
                    else if (string.Equals(key, "OffsetRight", StringComparison.OrdinalIgnoreCase))
                    {
                        if (double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out oR))
                            oROk = true;
                    }
                    else if (string.Equals(key, "ValidRight", StringComparison.OrdinalIgnoreCase))
                    {
                        bool.TryParse(val, out vR);
                    }
                }

                if (gLOk && oLOk)
                {
                    calibGainLeft = gL;
                    calibOffsetLeft = oL;
                    calibValidLeft = vL;
                }
                else
                {
                    calibValidLeft = false;
                }

                if (gROk && oROk)
                {
                    calibGainRight = gR;
                    calibOffsetRight = oR;
                    calibValidRight = vR;
                }
                else
                {
                    calibValidRight = false;
                }
            }
            catch (Exception ex)
            {
                calibValidLeft = false;
                calibValidRight = false;
                MessageBox.Show("Erreur lors de la lecture de la calibration : " + ex.Message,
                                "Calibration",
                                MessageBoxButtons.OK,
                                MessageBoxIcon.Error);
            }
        }


        //
        //
        private void btnRecord_Click(object sender, EventArgs e)
        {
            if (currentFormat == null)
            {
                MessageBox.Show("Démarre la lecture (READ) avant d'enregistrer.");
                return;
            }

            if (isRecordingOneSecond)
            {
                MessageBox.Show("Un enregistrement est déjà en cours.");
                return;
            }

            // On démarre l'enregistrement
            isRecordingOneSecond = true;
            recordSamplesCollected = 0;
            recordBuffer.Clear();

            recordSamplesNeeded = currentFormat.SampleRate;  // exactement 1 seconde

            MessageBox.Show($"Enregistrement 1 seconde démarré ({recordSamplesNeeded} échantillons).");
        }

        private void SaveRecordingToCsv()
        {
            try
            {
                string desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string file = Path.Combine(desktop, $"audio_record_{DateTime.Now:HHmmss}.csv");

                using (var sw = new StreamWriter(file))
                {
                    sw.WriteLine("Index;Left_decimal;Right_decimal;Left_hex;Right_hex");
                    int index = 0;

                    foreach (var f in recordBuffer)
                    {
                        uint hl = (uint)(f.Left & 0xFFFFFF);
                        uint hr = (uint)(f.Right & 0xFFFFFF);

                        sw.WriteLine($"{index};{f.Left};{f.Right};0x{hl:X6};0x{hr:X6}");
                        index++;
                    }
                }

                MessageBox.Show($"Fichier enregistré :\n{file}");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur CSV : " + ex.Message);
            }
        }


        private void btnStop_Click(object sender, EventArgs e)
        {

        }

        private void Capture_DataAvailable(object? sender, WaveInEventArgs e)
        {
            ProcessBuffer(e.Buffer, e.BytesRecorded);
        }

        //débute de ProcessBuffer
        private void ProcessBuffer(byte[] buffer, int bytesRecorded)
        {
            if (currentFormat == null)
                return;

            // On s'attend à du float 32 bits stéréo
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
            // ---------- Calcul du dernier frame pour affichage détaillé ----------
            int lastFrameIndex = bytesRecorded - blockAlign;

            float leftF = BitConverter.ToSingle(buffer, lastFrameIndex);
            float rightF = BitConverter.ToSingle(buffer, lastFrameIndex + bytesPerSample);

            const int Max24 = 1 << 23;      // 8388608
            const int Max24Pos = Max24 - 1; // +0x7FFFFF
            const int Min24Neg = -Max24;    // -0x800000

            float scaledL = leftF * Max24;
            float scaledR = rightF * Max24;

            int left24 = (int)Math.Truncate(scaledL);
            int right24 = (int)Math.Truncate(scaledR);

            // Mémoriser le dernier code 24 bits
            lastCodeLeft = left24;
            lastCodeRight = right24;

            double instVoltsLeft = 0.0;
            double instVoltsRight = 0.0;

            if (calibValidLeft)
                instVoltsLeft = calibGainLeft * left24 + calibOffsetLeft;

            if (calibValidRight)
                instVoltsRight = calibGainRight * right24 + calibOffsetRight;

            // Filtre d’affichage 4 Hz
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

            // Voie gauche : on ne date QUE le début de saturation
            if (sampleSatLeft && !isCurrentlySatLeft)
            {
                isCurrentlySatLeft = true;
                lastSatLeft = nowSat;  // début de la saturation
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
            // ---------- RECORD 1 SECONDE ----------
            if (isRecordingOneSecond)
            {
                int bytesPerFrame = currentFormat.Channels * 4;
                int frameCount = bytesRecorded / bytesPerFrame;

                for (int i = 0; i < frameCount; i++)
                {
                    int idx = i * bytesPerFrame;

                    float lf = BitConverter.ToSingle(buffer, idx);
                    float rf = BitConverter.ToSingle(buffer, idx + 4);

                    float sL = lf * Max24;
                    float sR = rf * Max24;

                    int L = (int)Math.Truncate(sL);
                    int R = (int)Math.Truncate(sR);

                    if (L > Max24Pos) L = Max24Pos;
                    if (L < Min24Neg) L = Min24Neg;
                    if (R > Max24Pos) R = Max24Pos;
                    if (R < Min24Neg) R = Min24Neg;

                    recordBuffer.Add(new RecordFrame() { Left = L, Right = R });
                    recordSamplesCollected++;

                    if (recordSamplesCollected >= recordSamplesNeeded)
                    {
                        isRecordingOneSecond = false;
                        SaveRecordingToCsv();
                        MessageBox.Show("Enregistrement terminé (1 seconde).");
                        break;
                    }
                }
            }
            // ---------- Calcul du niveau crête sur ce buffer (pour les VU) ----------
            int bytesPerFrameAll = currentFormat.Channels * 4;
            int totalFrames = bytesRecorded / bytesPerFrameAll;

            int peakLeftAbs = 0;
            int peakRightAbs = 0;

            for (int i = 0; i < totalFrames; i++)
            {
                int idx = i * bytesPerFrameAll;

                float lf = BitConverter.ToSingle(buffer, idx);
                float rf = BitConverter.ToSingle(buffer, idx + 4);

                float sL = lf * Max24;
                float sR = rf * Max24;

                int L = (int)Math.Truncate(sL);
                int R = (int)Math.Truncate(sR);

                if (L > Max24Pos) L = Max24Pos;
                if (L < Min24Neg) L = Min24Neg;
                if (R > Max24Pos) R = Max24Pos;
                if (R < Min24Neg) R = Min24Neg;

                int absL = Math.Abs(L);
                int absR = Math.Abs(R);

                if (absL > peakLeftAbs) peakLeftAbs = absL;
                if (absR > peakRightAbs) peakRightAbs = absR;
            }
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

            // On clip à -120 dBFS pour l'affichage
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
            leftText.AppendLine(FormatRow("Float", leftF.ToString("G9")));
            leftText.AppendLine(FormatRow("Scaled x2^23", scaledL.ToString("G9")));
            leftText.AppendLine(FormatRow("Decimal (24b)", left24.ToString()));
            leftText.AppendLine(FormatRow("Hex (24 bits)", leftHex24));
            leftText.AppendLine(FormatRow("Binary (24b)", leftBits));
            if (calibValidLeft)
                leftText.AppendLine(FormatRow("Voltage (V)", displayVoltsLeft.ToString("F4", CultureInfo.InvariantCulture)));
            else
                leftText.AppendLine(FormatRow("Voltage (V)", "N/A"));

            var rightText = new StringBuilder();
            rightText.AppendLine(FormatRow("Float", rightF.ToString("G9")));
            rightText.AppendLine(FormatRow("Scaled x2^23", scaledR.ToString("G9")));
            rightText.AppendLine(FormatRow("Decimal (24b)", right24.ToString()));
            rightText.AppendLine(FormatRow("Hex (24 bits)", rightHex24));
            rightText.AppendLine(FormatRow("Binary (24b)", rightBits));
            if (calibValidRight)
                rightText.AppendLine(FormatRow("Voltage (V)", displayVoltsRight.ToString("F4", CultureInfo.InvariantCulture)));
            else
                rightText.AppendLine(FormatRow("Voltage (V)", "N/A"));

            // ---------- Mise à jour UI ----------
            BeginInvoke((Action)(() =>
            {
                txtLeft.Text = leftText.ToString();
                txtRight.Text = rightText.ToString();

                ColorBinaryXs(txtLeft, "Binary (24b)");
                ColorBinaryXs(txtRight, "Binary (24b)");

                // SAT LEDs
                //bool satL = lastSatLeft.HasValue && (DateTime.Now - lastSatLeft.Value) < satHoldTime;
                //bool satR = lastSatRight.HasValue && (DateTime.Now - lastSatRight.Value) < satHoldTime;

                //ledSatLeft.BackColor = satL ? Color.Red : Color.Gray;
                //ledSatRight.BackColor = satR ? Color.Red : Color.Gray;

                // Bargraphs dBFS (Panel custom)
                UpdateLevelBar(barLeftLevel, levelLeft);
                UpdateLevelBar(barRightLevel, levelRight);

                var nowUi = DateTime.Now;

                // --- Voie gauche ---
                bool satLActive =
                    isCurrentlySatLeft || (lastSatLeft.HasValue && (nowUi - lastSatLeft.Value) < satDisplayTime);

                if (satLActive)
                {
                    // temps écoulé depuis la saturation
                    var elapsed = nowUi - lastSatLeft.Value;
                    // on alterne ON / OFF toutes satBlinkPeriod/2
                    bool blinkOn = ((int)(elapsed.TotalMilliseconds / (satBlinkPeriod.TotalMilliseconds / 2))) % 2 == 0;

                    if (blinkOn)
                    {
                        lblLeftLevel.Text = "      SAT";
                        lblLeftLevel.ForeColor = Color.Red;
                        lblLeftLevel.Font = new Font(lblLeftLevel.Font, FontStyle.Bold);

                    }
                    else
                    {
                        lblLeftLevel.Text = ""; // ou "- dBFS" si tu veux quelque chose quand c’est “off”
                        lblLeftLevel.ForeColor = Color.Red;

                    }
                }
                else
                {
                    // Affichage normal dBFS
                    lblLeftLevel.Text = double.IsNegativeInfinity(dbLeft) ? "- dBFS" : $"{dbLeft:F1} dBFS";
                    lblLeftLevel.ForeColor = Color.Black;
                    lblLeftLevel.Font = new Font(lblLeftLevel.Font, FontStyle.Regular);

                }

                // ---Voie droite-- -
                bool satRActive =
                    isCurrentlySatRight ||
                    (lastSatRight.HasValue && (nowUi - lastSatRight.Value) < satDisplayTime);

                if (satRActive)
                {
                    var elapsed = nowUi - lastSatRight.Value;
                    bool blinkOn = ((int)(elapsed.TotalMilliseconds / (satBlinkPeriod.TotalMilliseconds / 2))) % 2 == 0;

                    if (blinkOn)
                    {
                        lblRightLevel.Text = "      SAT";
                        lblRightLevel.ForeColor = Color.Red;
                        lblRightLevel.Font = new Font(lblRightLevel.Font, FontStyle.Bold);
                    }
                    else
                    {
                        lblRightLevel.Text = "";
                        lblRightLevel.ForeColor = Color.Red;
                    }
                }
                else
                {
                    lblRightLevel.Text = double.IsNegativeInfinity(dbRight) ? "- dBFS" : $"{dbRight:F1} dBFS";
                    lblRightLevel.ForeColor = Color.Black;
                    lblRightLevel.Font = new Font(lblRightLevel.Font, FontStyle.Regular);
                }



            }));
        }

        private void Capture_RecordingStopped(object? sender, StoppedEventArgs e)
        {
            if (e.Exception != null)
            {
                MessageBox.Show("Erreur pendant la capture : " + e.Exception.Message);
            }
        }


        private void ColorBinaryXs(RichTextBox rtb, string binaryLabel)
        {
            string fullText = rtb.Text;
            int labelPos = fullText.IndexOf(binaryLabel, StringComparison.Ordinal);
            if (labelPos < 0)
                return;

            int lineEnd = fullText.IndexOf('\n', labelPos);
            if (lineEnd < 0)
                lineEnd = fullText.Length;

            int bitsStart = fullText.IndexOf(':', labelPos);
            if (bitsStart < 0 || bitsStart + 1 >= lineEnd)
                return;

            bitsStart += 2; // on saute ": "

            // On sauvegarde la sélection actuelle
            int oldStart = rtb.SelectionStart;
            int oldLength = rtb.SelectionLength;
            Color oldColor = rtb.SelectionColor;

            rtb.SuspendLayout();

            for (int i = bitsStart; i < lineEnd; i++)
            {
                rtb.SelectionStart = i;
                rtb.SelectionLength = 1;

                char c = fullText[i];
                if (c == 'x')
                {
                    rtb.SelectionColor = Color.Red;
                }
                else
                {
                    rtb.SelectionColor = rtb.ForeColor;
                }
            }

            rtb.ResumeLayout();

            // On restaure la sélection comme si de rien n’était
            rtb.SelectionStart = oldStart;
            rtb.SelectionLength = oldLength;
            rtb.SelectionColor = oldColor;
        }



        private string FormatBitsWithStability(uint value24, bool[] lastBits, DateTime[] lastChange)
        {
            var now = DateTime.Now;
            char[] bits = new char[24];

            // On parcourt du MSB (bit 23) au LSB (bit 0)
            for (int i = 0; i < 24; i++)
            {
                int bitIndex = 23 - i; // i=0 => bit 23 (MSB), i=23 => bit 0 (LSB)
                bool bitValue = ((value24 >> bitIndex) & 1) != 0;

                // Si le bit a changé depuis la dernière fois, on note le moment
                if (bitValue != lastBits[bitIndex])
                {
                    lastBits[bitIndex] = bitValue;
                    lastChange[bitIndex] = now;
                }

                // Si le bit a changé dans la dernière seconde -> on affiche 'x'
                if (now - lastChange[bitIndex] < bitStabilityThreshold)
                {
                    bits[i] = 'x';
                }
                else
                {
                    bits[i] = bitValue ? '1' : '0';
                }
            }

            string raw = new string(bits);
            return GroupBits(raw, 4); // On réutilise ta fonction de groupement par 4 bits
        }
        //
        //
        private string BuildDeviceInfoText()
        {
            var sb = new StringBuilder();

            if (selectedDevice == null)
            {
                sb.AppendLine("Selected device : (none)");
                sb.AppendLine();
                sb.AppendLine("Capture not started.");
                return sb.ToString();
            }

            var dev = selectedDevice;

            WaveFormat? fmt = currentFormat;
            if (fmt == null)
            {
                try
                {
                    fmt = dev.AudioClient.MixFormat;
                }
                catch
                {
                    fmt = null;
                }
            }

            sb.AppendLine("Selected device");
            sb.AppendLine(new string('-', 40));

            sb.AppendLine($"Name   : {dev.FriendlyName}");
            sb.AppendLine($"ID     : {dev.ID}");
            sb.AppendLine();

            sb.AppendLine("Endpoint");
            sb.AppendLine($"  Type : {(captureOutput ? "Output (Render)" : "Input  (Capture)")}");
            sb.AppendLine($"  State: {dev.State}");
            sb.AppendLine();

            sb.AppendLine("Format (Windows)");
            if (fmt != null)
            {
                string enc;
                if (fmt.Encoding == WaveFormatEncoding.IeeeFloat)
                    enc = "IEEE Float";
                else if (fmt.Encoding == WaveFormatEncoding.Pcm)
                    enc = "PCM";
                else
                    enc = fmt.Encoding.ToString();

                sb.AppendLine($"  {fmt.Channels} ch, {fmt.BitsPerSample} bits, {fmt.SampleRate} Hz");
                sb.AppendLine($"  {enc}");
            }
            else
            {
                sb.AppendLine("  (unknown)");
            }

            sb.AppendLine();

            sb.AppendLine("Capture status");
            if (capture == null)
            {
                sb.AppendLine("  Idle (not capturing)");
            }
            else
            {
                sb.AppendLine("  Active");
                sb.AppendLine($"  Mode : {(captureOutput ? "OUT (loopback on output)" : "IN (device input)")}");
            }

            return sb.ToString();
        }
        /// <summary>
        /// 
        //
        /// </summary>
        private void UpdateDeviceInfo()
        {
            string text = BuildDeviceInfoText();
    
        }



        private void CreateDbMarkers(Panel bar, string[] labels, double[] dbValues)
        {
            if (bar == null || labels == null || dbValues == null)
                return;
            if (labels.Length != dbValues.Length)
                return;

            int n = labels.Length;

            for (int i = 0; i < n; i++)
            {
                double db = dbValues[i];
                double percent = DbToPercent(db); // 0..100

                // Position X relative à la barre
                int x = bar.Left + (int)(bar.Width * percent / 100.0);

                var lbl = new Label();
                lbl.AutoSize = true;
                lbl.Text = labels[i];
                lbl.Font = new Font(Font.FontFamily, 7); // petite police
                lbl.Top = bar.Top - lbl.Height + 10;      // juste au-dessus de la barre
                lbl.Left = x - lbl.PreferredWidth / 2;   // centré sur la position

                Controls.Add(lbl);
                lbl.BringToFront();
            }
        }


        private void UpdateLevelBar(Panel panel, int levelPercent)
        {
            if (panel == null || panel.Width <= 0 || panel.Height <= 0)
                return;

            // Clamp
            if (levelPercent < 0) levelPercent = 0;
            if (levelPercent > 100) levelPercent = 100;

            // Récupérer / créer les LEDs
            Panel[] leds;
            if (panel.Tag is Panel[] arr && arr.Length == LedCount && panel.Controls.Count == LedCount)
            {
                leds = arr;
            }
            else
            {
                leds = new Panel[LedCount];
                panel.Controls.Clear();

                int totalWidth = panel.ClientSize.Width;
                int totalHeight = panel.ClientSize.Height;

                // petites marges verticales pour ne pas coller au bord
                int vMargin = Math.Max(1, totalHeight / 6);
                int ledHeight = Math.Max(2, totalHeight - 2 * vMargin);

                int spacing = 2; // espace horizontal entre LEDs

                for (int i = 0; i < LedCount; i++)
                {
                    var led = new Panel();

                    // découpe en bandes égales sur toute la largeur
                    double xStart = (double)i / LedCount * totalWidth;
                    double xEnd = (double)(i + 1) / LedCount * totalWidth;

                    int left = (int)Math.Round(xStart);
                    int right = (int)Math.Round(xEnd);
                    int width = right - left;

                    if (width > spacing)
                        width -= spacing;

                    led.Left = left;
                    led.Width = Math.Max(1, width);
                    led.Top = vMargin;
                    led.Height = ledHeight;

                    // couleur LED éteinte
                    led.BackColor = Color.FromArgb(64, 64, 64);
                    led.BorderStyle = BorderStyle.None;

                    panel.Controls.Add(led);
                    leds[i] = led;
                }

                panel.Tag = leds;
            }

            // Pourcentage des frontières de couleur
            double pBlueEnd = DbToPercent(DbBlueToGreen);
            double pGreenEnd = DbToPercent(DbGreenToOrange);
            double pOrangeEnd = DbToPercent(DbOrangeToRed);
            double pRedEnd = 100.0;

            for (int i = 0; i < LedCount; i++)
            {
                var led = leds[i];

                // Position de cette LED en % de la barre
                double ledCenterPercent = ((i + 0.5) / LedCount) * 100.0;

                bool isOn = ledCenterPercent <= levelPercent;
                if (!isOn)
                {
                    // LED éteinte
                    led.BackColor = Color.FromArgb(64, 64, 64);
                    continue;
                }

                // Couleur en fonction de la zone de dB
                if (ledCenterPercent <= pBlueEnd)
                {
                    led.BackColor = Color.FromArgb(0, 120, 215);      // bleu "Windows"
                }
                else if (ledCenterPercent <= pGreenEnd)
                {
                    led.BackColor = Color.Lime;                       // vert vif
                }
                else if (ledCenterPercent <= pOrangeEnd)
                {
                    led.BackColor = Color.Gold;                       // jaune/orange
                }
                else // zone rouge
                {
                    led.BackColor = Color.Red;
                }
            }
        }




        private void ledSatLeft_Click(object sender, EventArgs e)
        {

        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label6_Click(object sender, EventArgs e)
        {

        }

        private void label7_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click_1(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }
    }

}
