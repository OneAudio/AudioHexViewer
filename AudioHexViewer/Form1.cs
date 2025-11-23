using System;
using System.Text;
using System.Windows.Forms;
using NAudio.CoreAudioApi;
using NAudio.Wave;
using System.Drawing;


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

        // --- Indicateur de saturation ---
        private DateTime? lastSaturationTime = null;
        private readonly TimeSpan saturationHoldTime = TimeSpan.FromSeconds(1);

        // Saturation par voie
        private DateTime? lastSatLeft = null;
        private DateTime? lastSatRight = null;
        private readonly TimeSpan satHoldTime = TimeSpan.FromSeconds(1);


        // --- Enregistrement 1 seconde ---
        private bool isRecordingOneSecond = false;
        private int recordSamplesNeeded = 0;
        private int recordSamplesCollected = 0;

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
            LoadInputDevices();

            comboDevices.SelectedIndexChanged += comboDevices_SelectedIndexChanged;

            // Remplissage de la liste des sample rates
            comboSampleRate.Items.Clear();
            comboSampleRate.Items.Add("Auto (mix format)");
            comboSampleRate.Items.Add("44100");
            comboSampleRate.Items.Add("88200");
            comboSampleRate.Items.Add("176400");
            comboSampleRate.Items.Add("352800");
            comboSampleRate.Items.Add("48000");
            comboSampleRate.Items.Add("96000");
            comboSampleRate.Items.Add("192000");
            comboSampleRate.Items.Add("384000");
            comboSampleRate.SelectedIndex = 0; // Auto par défaut


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


        private void LoadInputDevices()
        {
            // Gestionnaire de périphériques audio
            var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active);

            comboDevices.Items.Clear();

            foreach (var dev in devices)
            {
                // On associe l'objet MMDevice directement à l'item
                comboDevices.Items.Add(dev);
            }

            if (comboDevices.Items.Count > 0)
            {
                comboDevices.SelectedIndex = 0;
            }

            UpdateDeviceInfo();
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
                    MessageBox.Show("Sélectionne un périphérique d'entrée audio.");
                    return;
                }

                selectedDevice = dev;

                try
                {
                    // 1) On démarre la capture comme avant (mode partagé, mix format)
                    capture = new WasapiCapture(selectedDevice);
                    currentFormat = capture.WaveFormat;

                    capture.DataAvailable += Capture_DataAvailable;
                    capture.RecordingStopped += Capture_RecordingStopped;

                    capture.StartRecording();

                    // 2) On vérifie le sample rate demandé vs celui réellement utilisé
                    string srText = comboSampleRate.SelectedItem as string ?? "Auto (mix format)";
                    bool isAuto = srText.StartsWith("Auto", StringComparison.OrdinalIgnoreCase);

                    if (!isAuto)
                    {
                        if (int.TryParse(srText, out int wantedRate))
                        {
                            if (currentFormat != null && currentFormat.SampleRate != wantedRate)
                            {
                                MessageBox.Show(
                                    $"Tu as choisi {wantedRate} Hz, " +
                                    $"mais le périphérique (en mode partagé) est actuellement à {currentFormat.SampleRate} Hz.\n\n" +
                                    "Pour utiliser exactement ce taux, change le réglage dans les propriétés audio Windows (onglet Statistiques avancées).",
                                    "Sample rate différent",
                                    MessageBoxButtons.OK,
                                    MessageBoxIcon.Warning
                                );
                            }
                        }
                    }

                    // 3) UI
                    btnStart.Text = "STOP";
                    UpdateDeviceInfo();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur lors du démarrage de la capture : " + ex.Message);
                    capture = null;
                    btnStart.Text = "READ";
                    UpdateDeviceInfo();
                }
            }
            else
            {
                // Si on est déjà en train de capturer -> clique = STOP

                try
                {
                    capture.DataAvailable -= Capture_DataAvailable;
                    capture.StopRecording();
                }
                catch
                {
                    // on ignore les erreurs de StopRecording
                }

                capture.Dispose();
                capture = null;
                currentFormat = null;

                btnStart.Text = "READ";
                UpdateDeviceInfo();
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

        if (left24 > Max24Pos) left24 = Max24Pos;
        if (left24 < Min24Neg) left24 = Min24Neg;
        if (right24 > Max24Pos) right24 = Max24Pos;
        if (right24 < Min24Neg) right24 = Min24Neg;

        // ---- SATURATION PAR VOIE ----
        if (left24 == Max24Pos || left24 == Min24Neg)
            lastSatLeft = DateTime.Now;
        if (right24 == Max24Pos || right24 == Min24Neg)
            lastSatRight = DateTime.Now;

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

            var rightText = new StringBuilder();
            rightText.AppendLine(FormatRow("Float", rightF.ToString("G9")));
            rightText.AppendLine(FormatRow("Scaled x2^23", scaledR.ToString("G9")));
            rightText.AppendLine(FormatRow("Decimal (24b)", right24.ToString()));
            rightText.AppendLine(FormatRow("Hex (24 bits)", rightHex24));
            rightText.AppendLine(FormatRow("Binary (24b)", rightBits));

            // ---------- Mise à jour UI ----------
            BeginInvoke((Action)(() =>
            {
            txtLeft.Text = leftText.ToString();
            txtRight.Text = rightText.ToString();

            ColorBinaryXs(txtLeft, "Binary (24b)");
            ColorBinaryXs(txtRight, "Binary (24b)");

            // SAT LEDs
            bool satL = lastSatLeft.HasValue && (DateTime.Now - lastSatLeft.Value) < satHoldTime;
            bool satR = lastSatRight.HasValue && (DateTime.Now - lastSatRight.Value) < satHoldTime;

            ledSatLeft.BackColor = satL ? Color.Red : Color.Gray;
            ledSatRight.BackColor = satR ? Color.Red : Color.Gray;

            // Bargraphs dBFS
            barLeftLevel.Value = levelLeft;
            barRightLevel.Value = levelRight;

                lblLeftLevel.Text = double.IsNegativeInfinity(dbLeft) ? "- dBFS" : $"{dbLeft:F1} dBFS";
                lblRightLevel.Text = double.IsNegativeInfinity(dbRight) ? "- dBFS" : $"{dbRight:F1} dBFS";
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
        private void UpdateDeviceInfo()
        {
            if (DeviceDatabox == null)
                return;

            if (comboDevices.SelectedItem is not MMDevice dev)
            {
                DeviceDatabox.Clear();
                DeviceDatabox.AppendText("No device selected.");
                return;
            }

            var sb = new StringBuilder();

            // Infos de base sur le device
            sb.AppendLine("Selected device");
            sb.AppendLine("----------------");
            sb.AppendLine($"Name        : {dev.FriendlyName}");
            sb.AppendLine($"ID          : {dev.ID}");
            sb.AppendLine($"Data flow   : {dev.DataFlow}");
            sb.AppendLine($"State       : {dev.State}");
            sb.AppendLine();

            // Format mixeur Windows (MixFormat)
            var mix = dev.AudioClient.MixFormat;
            sb.AppendLine("Mix format (Windows)");
            sb.AppendLine("--------------------");
            sb.AppendLine($"Encoding    : {mix.Encoding}");
            sb.AppendLine($"Sample rate : {mix.SampleRate} Hz");
            sb.AppendLine($"Bits/sample : {mix.BitsPerSample}");
            sb.AppendLine($"Channels    : {mix.Channels}");
            sb.AppendLine();

            // Format réellement utilisé par la capture (si en cours)
            if (currentFormat != null)
            {
                sb.AppendLine("Capture format (app)");
                sb.AppendLine("--------------------");
                sb.AppendLine($"Encoding    : {currentFormat.Encoding}");
                sb.AppendLine($"Sample rate : {currentFormat.SampleRate} Hz");
                sb.AppendLine($"Bits/sample : {currentFormat.BitsPerSample}");
                sb.AppendLine($"Channels    : {currentFormat.Channels}");
            }

            DeviceDatabox.Clear();
            DeviceDatabox.Text = sb.ToString();
        }

        private void ledSatLeft_Click(object sender, EventArgs e)
        {

        }
    }

}
