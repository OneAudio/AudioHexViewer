# Contributing to AudioHexViewer

Thanks for your interest in improving AudioHexViewer! This document covers how to
build the project, the conventions the codebase follows, and how to submit changes.

By contributing, you agree that your contributions are licensed under the project's
license, the **GNU General Public License v3.0 or later** (see [`LICENSE`](LICENSE)).

---

## Building and testing

Requirements: the **.NET 10 SDK** on **Windows** (the project targets `net10.0-windows`
and uses Windows Forms, so it builds and runs on Windows only).

```bash
# Build
dotnet build AudioHexViewer.sln -c Release

# Run the test suite (please do this before opening a pull request)
dotnet test AudioHexViewer.sln -c Release
```

The CI workflow builds and runs the tests on every push and pull request to `main`,
so a green check there mirrors what you should see locally.

---

## Coding conventions

A few conventions are applied consistently across the codebase:

- **In-app strings are in English.** User-facing text (messages, labels, tooltips,
  dialog titles) must be English. Code comments may be in any language.
- **No dead code.** Remove unused code rather than commenting it out — the Git history
  is there if you need it.
- **Keep changes focused.** Prefer small, surgical diffs that do one thing. Avoid
  unrelated reformatting or drive-by changes in the same commit.
- **License header on new source files.** Every new `.cs` file starts with:
  ```csharp
  // SPDX-License-Identifier: GPL-3.0-or-later
  // Copyright (C) <year> <your name or handle>
  // Part of AudioHexViewer - see LICENSE for details.
  ```
- **Run the tests** (`dotnet test`) before submitting, and add tests for new DSP or
  logic where it makes sense (the test project lives in `AudioHexViewer.Tests`).

---

## Submitting changes

1. Fork the repository and create a branch for your change.
2. Make your change, keeping commits focused and the build/tests green.
3. Open a pull request against `main`. Describe what the change does and why.
4. The CI must pass before a pull request can be merged.

For larger changes or new features, it's a good idea to open an issue first to discuss
the approach before investing significant effort.

---

## Reporting bugs

When reporting a crash or unexpected behaviour, please include:

- What you did and what you expected to happen;
- Your OS version and how you obtained the build (release EXE, or built from source);
- If the application crashed, the crash log. AudioHexViewer writes one to:
  ```
  %LOCALAPPDATA%\AudioHexViewer\logs\
  ```
  Attaching the most recent `crash_*.log` file makes diagnosis much easier.

---

## Security

If you believe you have found a security issue (for example, a way to crash or exploit
the application through a crafted overlay or audio file), please report it privately to
the maintainer rather than opening a public issue, so it can be addressed before
disclosure.
