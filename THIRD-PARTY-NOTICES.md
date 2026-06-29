# Third-Party Notices

AudioHexViewer is licensed under the GNU General Public License v3.0 or later
(see [`LICENSE`](LICENSE)).

This product bundles and/or builds upon the third-party components listed
below. Each is distributed under its own license, all of which are compatible
with the GPL v3.0. The full text of the MIT License — the license used by all
bundled runtime dependencies — appears at the end of this file.

> The copyright holders listed below reflect the upstream attribution at the
> time of writing. The authoritative copyright and license text for each
> component is the `LICENSE` file in its own source repository / NuGet package;
> please refer to those for the canonical notice.

---

## Bundled runtime dependencies

These components are redistributed as part of the AudioHexViewer executable
(the published build is self-contained and includes the .NET runtime).

| Component | Version | License | Project |
|---|---|---|---|
| NAudio | 2.2.1 | MIT | https://github.com/naudio/NAudio |
| Math.NET Numerics | 5.0.0 | MIT | https://github.com/mathnet/mathnet-numerics |
| WinForms.DataVisualization | 1.10.0 | MIT | https://github.com/kirsan31/winforms-datavisualization |
| .NET Runtime (self-contained) | 10.x | MIT | https://github.com/dotnet/runtime |

**Attribution:**

- **NAudio** — Copyright © Mark Heath and NAudio contributors.
- **Math.NET Numerics** — Copyright © Math.NET Project (Christoph Rüegg, Marcus Cuda, and contributors).
- **WinForms.DataVisualization** — Copyright © the WinForms.DataVisualization maintainers and contributors; a community continuation of Microsoft's `System.Windows.Forms.DataVisualization`, Copyright © Microsoft Corporation.
- **.NET Runtime** — Copyright © .NET Foundation and Contributors.

---

## Development / test dependencies (not redistributed)

These are used only to build and test the project and are **not** included in
the released executable. Listed here for transparency.

| Component | Version | License |
|---|---|---|
| xUnit.net | 2.9.3 | Apache-2.0 |
| xunit.runner.visualstudio | 2.8.2 | Apache-2.0 |
| Microsoft.NET.Test.Sdk | 17.12.0 | MIT |
| coverlet.collector | 6.0.4 | MIT |

---

## The MIT License (MIT)

The bundled runtime dependencies above are distributed under the following
license. The copyright line applies to each respective project's holder as
listed in the Attribution section.

```
MIT License

Copyright (c) <respective copyright holders, see Attribution above>

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
