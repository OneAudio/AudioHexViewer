function analyse_sinus_wav_ui()
  % ======================================================================
  % Analyse WAV sinus (UI):
  % - Choix fichier via uigetfile
  % - Spectre 0..Fs/2 en dBFS (0 dBFS = 1.0 crête)
  % - Fenêtre Blackman-Harris 7 termes
  % - Mesures: THD, THD+N, SINAD, SNR, SFDR + harmoniques 1..10 (dBc)
  % - Affichage: graphe à gauche, tableaux à droite (uitable)
  %
  % GNU Octave 10.3
  % ======================================================================

  close all; clc;

  % --- Dialogue choix fichier
  [fname, fpath] = uigetfile({"*.wav;*.WAV", "WAV audio (*.wav)"}, "Choisir un fichier WAV");
  if isequal(fname, 0)
    disp("Annulé.");
    return;
  end
  wav_file = fullfile(fpath, fname);

  % --- Lecture WAV
  [x, Fs] = audioread(wav_file);
  N = rows(x);
  T = N / Fs;

  % Si multi-canaux: prendre le canal RMS max (évite baisse si canaux différents)
  if columns(x) > 1
    xrms = sqrt(mean(x.^2, 1));
    [~, idx] = max(xrms);
    x = x(:, idx);
  end

  x = double(x);

  % Sécurité normalisation: audioread renvoie normalement déjà [-1,1]
  pk = max(abs(x));
  if pk > 1.00001
    % Cas rare: données non normalisées -> supposition 24-bit signé
    x = x / (2^23);
  end

  % Retire DC
  x = x - mean(x);

  % ----------------------------------------------------------------------
  % Fenêtre BH7 + FFT
  % ----------------------------------------------------------------------
  w  = blackmanharris7(N);
  xw = x .* w;
  X  = fft(xw);

  K  = floor(N/2) + 1;
  Xh = X(1:K);
  f  = (0:K-1) * (Fs / N);
  df = Fs / N;

  % ----------------------------------------------------------------------
  % Spectre puissance (pour calculs disto/bruit en énergie)
  % ----------------------------------------------------------------------
  U = sum(w.^2);
  P = (abs(Xh).^2) / U;      % puissance relative par bin (2-sided)
  if K > 2
    P(2:K-1) *= 2;           % one-sided
  end

  % ----------------------------------------------------------------------
  % Détection f0 (max hors DC) + raffinement
  % ----------------------------------------------------------------------
  [~, k0] = max(abs(Xh(2:end)));
  k0 = k0 + 1;
  f0 = refine_peak_quadratic(abs(Xh), f, k0);

  % ----------------------------------------------------------------------
  % Amplitude fondamentale en dBFS (sine-fit)
  % 0 dBFS = amplitude crête 1.0
  % ----------------------------------------------------------------------
  t = (0:N-1)'/Fs;
  s = sin(2*pi*f0*t);
  c = cos(2*pi*f0*t);
  ab = [s c] \ x;                    % moindres carrés
  Apeak = sqrt(ab(1)^2 + ab(2)^2);   % amplitude crête
  A1_dBFS = 20*log10(Apeak + eps);

  peak_dBFS = 20*log10(max(abs(x)) + eps);

  % ----------------------------------------------------------------------
  % Intégration fondamentale + harmoniques pour THD, etc.
  % ----------------------------------------------------------------------
  search_bins = max(2, round(10/df));  % ±10 Hz
  integ_bins  = max(2, round(6/df));   % ±6 Hz
  guard_bins  = integ_bins;

  Hmax = 10;
  Ph = zeros(Hmax,1);
  kh = NaN(Hmax,1);

  [Ph(1), kh(1)] = tone_power(P, f0, df, search_bins, integ_bins);
  for h = 2:Hmax
    fh = h*f0;
    if fh <= Fs/2
      [Ph(h), kh(h)] = tone_power(P, fh, df, search_bins, integ_bins);
    else
      Ph(h) = 0;
      kh(h) = NaN;
    end
  end

  P1 = Ph(1);
  if P1 <= 0
    error("Impossible de détecter la fondamentale dans le spectre.");
  end

  % THD (2..10)
  Pdist = sum(Ph(2:end));
  THD = sqrt(Pdist / P1);
  THD_dB = 20*log10(THD + eps);

  % THD+N / SINAD : tout sauf fondamentale (garde)
  mask_sig = false(K,1);
  mask_sig = mark_guard(mask_sig, kh(1), guard_bins);
  P_noise_plus_dist = sum(P(2:end) .* (~mask_sig(2:end)));
  THDN = sqrt(P_noise_plus_dist / P1);
  THDN_dB = 20*log10(THDN + eps);
  SINAD = 20*log10( sqrt(P1) / (sqrt(P_noise_plus_dist) + eps) );

  % SNR : tout sauf fondamentale + harmoniques 1..10
  mask_snr = false(K,1);
  for h = 1:Hmax
    if ~isnan(kh(h)) && kh(h) > 0
      mask_snr = mark_guard(mask_snr, kh(h), guard_bins);
    end
  end
  Pnoise = sum(P(2:end) .* (~mask_snr(2:end)));
  SNR = 20*log10( sqrt(P1) / (sqrt(Pnoise) + eps) );

  % SFDR : spur max hors fondamentale + DC
  mask_sfdr = false(K,1);
  mask_sfdr(1) = true; % DC
  mask_sfdr = mark_guard(mask_sfdr, kh(1), guard_bins);
  P_spurs = P;
  P_spurs(mask_sfdr) = 0;
  Pspur_max = max(P_spurs(2:end));
  SFDR = 10*log10(P1 / (Pspur_max + eps));

  % Harmoniques dBc (puissance relative)
  harm_dBc = 10*log10((Ph + eps) / P1);
  harm_dBc(1) = 0;

  % ----------------------------------------------------------------------
  % Spectre dBFS (amplitude crête), avec calibration sur Apeak (sine-fit)
  % ----------------------------------------------------------------------
  Wsum = sum(w);               % coherent gain
  A = abs(Xh) / Wsum;          % amplitude crête 2-sided (sans doublement)
  if K > 2
    A(2:K-1) *= 2;             % one-sided
  end

  % Calibrage: aligne le pic FFT fondamentale sur l’amplitude Apeak (sine-fit)
  kFund = kh(1);
  if ~isnan(kFund) && kFund > 0
    A_fund_fft = A(kFund);
    cal = (Apeak + eps) / (A_fund_fft + eps);
    A = A * cal;
  end

  AdBFS = 20*log10(A + eps);

  % ======================================================================
  % Affichage UI: spectre à gauche + tableaux à droite
  % ======================================================================
  fig = figure("Name", "Analyse sinus WAV", "NumberTitle", "off", ...
               "Units", "pixels", "Position", [80 80 1200 700]);

  % Axes du spectre (gauche)
  ax = axes("Parent", fig, "Units", "normalized", "Position", [0.06 0.10 0.58 0.84]);
  plot(ax, f, AdBFS, "LineWidth", 1);
  grid(ax, "on");
  xlim(ax, [0, Fs/2]);
  xlabel(ax, "Fréquence (Hz)");
  ylabel(ax, "Amplitude (dBFS re 1.0 crête)");
  title(ax, sprintf("Spectre (BH7) | f0≈%.6f Hz | Fs=%.0f Hz | T=%.2f s", f0, Fs, T), "Interpreter", "none");

  % --- Grille / ticks personnalisés
grid(ax, "on");                 % grille majeure (sur les ticks majeurs)
set(ax, "Layer", "top");        % grille au-dessus du tracé (plus lisible)

% Limites actuelles
yl = ylim(ax);
xl = xlim(ax);

% ---- Y: ticks majeurs tous les 20 dB
ymaj0 = 20 * floor(yl(1)/20);
ymaj1 = 20 * ceil (yl(2)/20);
set(ax, "YTick", ymaj0:20:ymaj1);

% Re-apply grid after YTick change (Octave parfois)
grid(ax, "on");

% ---- Y: grille mineure tous les 5 dB (sauf sur les multiples de 20)
ymin0 = 5 * floor(yl(1)/5);
ymin1 = 5 * ceil (yl(2)/5);
ymin = ymin0:5:ymin1;
ymin = ymin(mod(ymin,20) != 0);   % enlever les majeures

hold(ax, "on");
for yy = ymin
  line(ax, [xl(1) xl(2)], [yy yy], ...
       "LineStyle", ":", ...
       "Color", [0.85 0.85 0.85], ...
       "HandleVisibility", "off");
end

% ---- X: grille mineure tous les 1 kHz (sauf x=0)
xstep = 1000;  % 1 kHz
xmin0 = xstep * ceil(xl(1)/xstep);
xmin1 = xstep * floor(xl(2)/xstep);
xminor = xmin0:xstep:xmin1;
xminor = xminor(xminor != 0);

for xx = xminor
  line(ax, [xx xx], [yl(1) yl(2)], ...
       "LineStyle", ":", ...
       "Color", [0.85 0.85 0.85], ...
       "HandleVisibility", "off");
end

hold(ax, "off");





  % Panneau à droite
  panel = uipanel("Parent", fig, "Units", "normalized", "Position", [0.67 0.06 0.31 0.88], ...
                  "Title", "Mesures", "FontWeight", "bold");

  % Table principale
  main_rows = {
    "Fichier", fname;
    "Fs (Hz)", sprintf("%.0f", Fs);
    "Durée (s)", sprintf("%.3f", T);
    "N", sprintf("%d", N);
    "f0 (Hz)", sprintf("%.6f", f0);
    "A1 dBFS (sine-fit)", sprintf("%.2f", A1_dBFS);
    "Peak brut (dBFS)", sprintf("%.2f", peak_dBFS);
    "THD (2..10) (dB)", sprintf("%.3f", THD_dB);
    "THD+N (dB)", sprintf("%.3f", THDN_dB);
    "SINAD (dB)", sprintf("%.3f", SINAD);
    "SNR (dB)", sprintf("%.3f", SNR);
    "SFDR (dB)", sprintf("%.3f", SFDR);
  };

  uitable("Parent", panel, "Units", "normalized", "Position", [0.04 0.52 0.92 0.45], ...
          "Data", main_rows, ...
          "ColumnName", {"Paramètre", "Valeur"}, ...
          "ColumnWidth", {170, 180}, ...
          "RowName", []);

  % Table harmoniques
  harm_rows = cell(Hmax, 3);
  for h = 1:Hmax
    fh = h*f0;
    harm_rows{h,1} = sprintf("%d", h);
    harm_rows{h,2} = sprintf("%.1f", fh);
    if fh <= Fs/2
      harm_rows{h,3} = sprintf("%.2f", harm_dBc(h));
    else
      harm_rows{h,3} = "-";
    end
  end

  uitable("Parent", panel, "Units", "normalized", "Position", [0.04 0.04 0.92 0.44], ...
          "Data", harm_rows, ...
          "ColumnName", {"H", "f (Hz)", "dBc"}, ...
          "ColumnWidth", {45, 120, 100}, ...
          "RowName", []);

end

% ======================================================================
% Fenêtre Blackman-Harris 7 termes
% ======================================================================
function w = blackmanharris7(N)
  n = (0:N-1)';
  if N == 1
    w = 1; return;
  end
  a0 = 0.27105140069342;
  a1 = 0.43329793923448;
  a2 = 0.21812299954311;
  a3 = 0.06592544638803;
  a4 = 0.01081174209837;
  a5 = 0.00077658482522;
  a6 = 0.00001388721735;
  x = 2*pi*n/(N-1);
  w = a0 - a1*cos(1*x) + a2*cos(2*x) - a3*cos(3*x) + a4*cos(4*x) - a5*cos(5*x) + a6*cos(6*x);
end

% ======================================================================
% Raffinement f0 par interpolation quadratique sur log(|X|)
% ======================================================================
function f0 = refine_peak_quadratic(A, f, k)
  K = length(A);
  if k <= 1 || k >= K
    f0 = f(k); return;
  end
  y1 = log(A(k-1) + eps);
  y2 = log(A(k)   + eps);
  y3 = log(A(k+1) + eps);
  denom = (y1 - 2*y2 + y3);
  if abs(denom) < 1e-12
    delta = 0;
  else
    delta = 0.5*(y1 - y3) / denom;
  end
  df = f(2) - f(1);
  f0 = f(k) + delta*df;
end

% ======================================================================
% Puissance d'un ton autour de ftone: recherche pic puis intégration
% ======================================================================
function [Ptone, kpeak] = tone_power(P, ftone, df, search_bins, integ_bins)
  K = length(P);
  k_pred = round(ftone/df) + 1;

  k1 = max(2, k_pred - search_bins);
  k2 = min(K, k_pred + search_bins);

  [~, idx] = max(P(k1:k2));
  kpeak = k1 + idx - 1;

  a = max(2, kpeak - integ_bins);
  b = min(K, kpeak + integ_bins);

  Ptone = sum(P(a:b));
end

function mask = mark_guard(mask, kcenter, guard_bins)
  if isnan(kcenter) || kcenter <= 0, return; end
  K = length(mask);
  a = max(1, kcenter - guard_bins);
  b = min(K, kcenter + guard_bins);
  mask(a:b) = true;
end

