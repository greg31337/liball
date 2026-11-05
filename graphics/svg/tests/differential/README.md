# Differential renderer tests

`SvgDifferentialRendererTests` renders small, opaque, font-independent SVG cases with this library and compares their RGBA pixels with browser engines and independent SVG renderers. The cases isolate standards questions around CSS `currentColor`, even-odd clipping, transformed user-space gradients, and `use` positioning with inherited presentation properties.

The test suite discovers `rsvg-convert` and `resvg` on `PATH`. Browser executables are intentionally explicit because command-line flags and installation paths vary by platform:

```sh
SVG_DIFF_CHROMIUM=/path/to/chrome dotnet test tests/svg.Tests.csproj --filter Differential
SVG_DIFF_FIREFOX=/path/to/firefox dotnet test tests/svg.Tests.csproj --filter Differential
```

`SVG_DIFF_RSVG_CONVERT` and `SVG_DIFF_RESVG` may override the discovered external renderer executables. A missing executable skips only that renderer's cases. A configured executable that fails, times out after 30 seconds, emits a non-64-by-64 image, or emits an invalid PNG fails the test.

## Tolerances and intentional differences

Pixels are compared in premultiplied sRGB RGBA8. A pixel is different when any channel exceeds its case's channel tolerance; the test then limits both the ratio of different pixels and the mean absolute error across all channels.

| Case | Per-channel tolerance | Maximum differing pixels | Maximum mean channel error | Intentional difference |
| --- | ---: | ---: | ---: | --- |
| `css-current-color` | 10 | 3.5% | 1.5 | Rounded-corner boundary coverage may vary with the antialiasing kernel. |
| `evenodd-clip` | 12 | 5.5% | 2.0 | Circle and path boundary coverage may vary with coverage sampling. |
| `user-space-gradient` | 10 | 2.5% | 2.5 | Gradient sample position and byte rounding can cause small channel differences. |
| `use-position-and-inheritance` | 12 | 4.0% | 2.0 | Translated rounded and circular boundaries can vary by one output pixel. |

The thresholds do not exempt regions or colors and cannot hide a geometry-sized displacement. Text is deliberately excluded because installed fonts, shaping engines, and hinting are host inputs rather than SVG-only standards questions. Declarative animation is also excluded from screenshot comparison because browser capture time is not a deterministic animation sample; fixed-time internal animation tests cover that behavior. Network resources, scripts, and external fonts are not used.
