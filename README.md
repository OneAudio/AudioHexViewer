# AudioHexViewer

**A real-time audio analyzer for Windows — spectrum, oscilloscope, amplitude histogram, Bode plot, and a built-in test-signal generator — built for high-resolution audio and converter measurement.**

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)
![Platform](https://img.shields.io/badge/platform-Windows%2010%2F11%20x64-lightgrey)
![.NET](https://img.shields.io/badge/.NET-10%20(WinForms)-512BD4)
![Status](https://img.shields.io/badge/status-pre--release%200.9.x-orange)

> AudioHexViewer is a measurement tool for the DIY audio and electronics community: it turns a sound card or audio interface into a multi-instrument bench for analyzing ADCs/DACs, amplifiers, and signal chains — up to **768 kHz** sample rate via WASAPI exclusive mode.

<!-- Add a screenshot here once you have one. A spectrum view with THD/SFDR readout makes a strong first impression.
![AudioHexViewer — Spectrum view](docs/img/spectrum.png)
-->

---

## Features

- **Spectrum analyzer** — high-resolution FFT with selectable windows, dBFS scaling, peak/trace hold (MAXI / MINI), THD, SFDR and IMD readouts, and noise-floor measurement.
- **Oscilloscope** — time-domain view with configurable timebase and trigger, sharing the overlay system with the spectrum view.
- **Amplitude histogram** — sample-value distribution with Gaussian overlay, σ markers, PDF normalization and DNL measurement for converter characterization.
- **Bode plot** — gain and phase vs. frequency from an L = R loopback baseline.
- **Test-signal generator** — 11 built-in signals (pure/multi-harmonic sine, DC, square, sawtooth, two-tone IMD 19 + 20 kHz, Gaussian noise at several levels, quasi full-scale, loopback, silence) for calibration and self-test.
- **Overlays** — save and compare reference traces across sessions (per-instrument libraries).
- **File post-processing** — load a WAV file and run the full analysis chain offline.
- **Level calibration** — map dBFS to real-world units with a persistent calibration file.
- **Export** — CSV, PNG and PDF export of graphs and measurements.
- **High sample rates** — 44.1, 48, 88.2, 96, 176.4, 192, 352.8, 384, 705.6 and 768 kHz, with WASAPI exclusive mode for bit-perfect capture.

---

## Requirements

**To run** (end users): **Windows 10 or 11, 64-bit**. Nothing else to install — the release is a self-contained single-file executable that bundles the .NET runtime.

**To build** (developers): the **.NET 10 SDK** (Windows). The project targets `net10.0-windows` and uses Windows Forms, so it builds and runs on Windows only.

---

## Installation (end users)

1. Go to the [**Releases**](https://github.com/<your-username>/AudioHexViewer/releases) page.
2. Download `AudioHexViewer.exe` from the latest release.
3. (Recommended) Verify the download against the published **SHA-256** checksum.
4. Run the executable. No installer, no admin rights required.

> **SmartScreen / antivirus note.** The executable is **not code-signed** (this is a free, open-source project). On first launch, Windows SmartScreen may show *"Windows protected your PC"* — click **More info → Run anyway**. Some antivirus engines may also flag self-contained single-file executables; this is a known false positive for unsigned bundled-runtime apps. If in doubt, verify the SHA-256 checksum and/or build from source.

---

## Building from source

```bash
# Clone
git clone https://github.com/<your-username>/AudioHexViewer.git
cd AudioHexViewer

# Restore + build (Debug)
dotnet build AudioHexViewer/AudioHexViewer.csproj

# Run
dotnet run --project AudioHexViewer/AudioHexViewer.csproj
```

To produce the self-contained single-file release executable:

```bash
dotnet publish AudioHexViewer/AudioHexViewer.csproj -c Release
```

The publish settings (`win-x64`, self-contained, single file) are defined in the `.csproj`. The output executable is in `AudioHexViewer/bin/Release/net10.0-windows/win-x64/publish/`.

---

## Running the tests

The solution includes an xUnit test project (`AudioHexViewer.Tests`) covering the DSP core — **~130 test cases** across `DspMath`, `RingBufferStereoFloat`, `FftPipelineController`, `SpectrumMetrics` and `WindowFactory`.

```bash
dotnet test
```

---

## Project layout

```
AudioHexViewer/
├── Core/            # DSP core: FFT pipeline, ring buffer, window functions, stats
├── Analysis/        # Spectrum metrics, test waveforms
├── Overlays/        # Reference-trace library and management
├── Form1.*.cs       # Main window (partial classes: capture, FFT, toolbar, signals…)
├── SpectrumControl.*.cs    # Spectrum view (rendering, cursors, overlays, Bode…)
├── OscilloscopeControl.cs  # Oscilloscope view
├── HistogramControl.cs     # Amplitude histogram view
└── …                # Dialogs, export, calibration, generator
AudioHexViewer.Tests/        # xUnit test suite (DSP core)
```

User data is stored outside the application folder:

- Session settings — `%APPDATA%\AudioHexViewer\`
- Calibration — `%LOCALAPPDATA%\AudioHexViewer\`
- Overlays — `Documents\AudioHexViewer\Overlays\`

---

## Built with

- [NAudio](https://github.com/naudio/NAudio) — audio capture/playback (MIT)
- [Math.NET Numerics](https://numerics.mathdotnet.com/) — FFT and numerics (MIT)
- [WinForms.DataVisualization](https://github.com/dotnet/winforms-datavisualization) — charting

Full third-party license notices are in [`THIRD-PARTY-NOTICES.md`](THIRD-PARTY-NOTICES.md).

---

## Contributing

Contributions are welcome. A few conventions for this codebase:

- **In-app strings are in English.**
- No dead code — remove rather than comment out.
- Keep changes focused; prefer small, surgical diffs.
- Run `dotnet test` before opening a pull request.

See [`CONTRIBUTING.md`](CONTRIBUTING.md) for details (issues, build setup, coding style).

---

## License

AudioHexViewer is free software, licensed under the **GNU General Public License v3.0 or later**. See [`LICENSE`](LICENSE) for the full text.

```
Copyright (C) 2026  <Your Name> (OnE Audio Projects)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
```
