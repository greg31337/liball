# Universal Chart Library Development Plan

## Goal

Build a comprehensive C# chart generation library that renders charts to raw bitmap memory, BMP files, PNG files, and SVG strings/files without external runtime libraries. Implementation must use only built-in .NET APIs and must follow the simplified C# subset described in `syntax.md`.

## Constraints

- No external libraries in `src`.
- Use only built-in .NET APIs such as `System`, `System.IO`, `System.Text`, `System.Globalization`, and `System.IO.Compression`.
- Output formats must include raw bitmap, BMP, PNG, and SVG.
- Source must conform to simplified C# syntax:
  - file-scoped namespaces, classes, enums, fields, constructors, and block-bodied methods
  - no properties, structs, interfaces, records, attributes, inheritance outside direct exception types, `override`, LINQ, `try/catch`, named arguments, `params`, or locally defined generic types or methods
  - only `static`, `public`, `internal`, and `const` modifiers
  - `public` is reserved for the supported consumer API; implementation types and members use `internal`
  - every public type, constructor, method, field, enum, and enum member has XML documentation using only the elements allowed by `syntax.md`
- Prefer explicit classes with documented public fields for API state and internal fields for implementation state over property-heavy APIs.
- Keep renderer abstractions class-based because interfaces and inheritance are not allowed.
- Use deterministic rendering by default so tests can compare pixels and SVG text.

## Research Summary

The feature set below is derived from the public documentation of mature charting systems:

- Matplotlib documents broad static plot families: pairwise plots, statistical distributions, gridded data, irregular gridded data, and 3D/volumetric plots. Source: https://matplotlib.org/stable/plot_types/index.html
- ScottPlot emphasizes static .NET plotting, axis/tick/grid customization, multiple axes, legends, layout control, palettes, annotations, and image export. Source: https://scottplot.net/cookbook/5.0/
- Plotly organizes charts as declarative traces plus layout, with axes, subplots, color axes, annotations, shapes, images, selections, sliders, update menus, and static export to PNG/SVG. Sources: https://plotly.com/javascript/reference/ and https://plotly.com/javascript/static-image-export/
- Apache ECharts covers abundant chart types, datasets, encode mappings, visualMap, grid/polar/geo/calendar coordinates, legend, tooltip, dataZoom, brush, timeline, toolbox, large-scale rendering, and Canvas/SVG renderers. Sources: https://echarts.apache.org/en/feature.html and https://echarts.apache.org/v4/en/cheat-sheet.html
- D3 provides the low-level building blocks expected in a universal visualization library: arrays, binning, ticks, scales, axes, shapes, pies, stacks, symbols, transitions, brushing, dragging, and zooming. Source: https://d3js.org/api
- Vega-Lite contributes a grammar-style model of data, transforms, encodings, marks, scales, axes, legends, layered views, configuration defaults, and declarative visual channels. Sources: https://vega.github.io/vega-lite-v2/docs/encoding.html, https://vega.github.io/vega-lite-v3/docs/scale.html, and https://vega.github.io/vega-lite/docs/legend.html
- Highcharts demonstrates production export expectations including SVG generation, PNG conversion, data table export, print/export options, titles, axes, series, and interactive affordances. Source: https://api.highcharts.com/class-reference/Highcharts.Exporting.html

## Feature Inventory

### 1. Platform And Package Features

- Single `chart` library project targeting modern .NET.
- No dependency on `System.Drawing`, SkiaSharp, ImageSharp, Cairo, browser engines, native codecs, or platform-specific drawing stacks.
- Deterministic software rasterizer implemented in managed C#.
- Deterministic SVG writer implemented in managed C#.
- Stable public API that can be used from console apps, services, tests, and build pipelines.
- Culture-invariant defaults with optional culture-aware label formatting.
- Thread-safe independent chart instances by avoiding shared mutable render state.
- Separate document model, layout model, render model, and encoders.
- Snapshot-friendly tests for raster pixels, encoded file headers, and SVG markup.

### 2. Primitive Data Types

- Color with RGBA channels.
- Point, size, rectangle, padding, margins, angle, range, and matrix classes.
- Numeric interval and domain classes.
- String measurement result class.
- Pixel format enum for RGBA32, BGRA32, RGB24, grayscale, alpha mask, and indexed palette planning.
- Blend mode enum for source-over, source, multiply, screen, darken, lighten, and normal fallback.
- Line cap enum: butt, square, round.
- Line join enum: miter, bevel, round.
- Fill rule enum: nonzero and even-odd.
- Text alignment enum: left, center, right, start, middle, end.
- Vertical text alignment enum: top, middle, baseline, bottom.
- Orientation enum: horizontal, vertical, radial.
- Side enum: left, right, top, bottom.
- Direction enum: normal, reversed.
- Chart coordinate units: pixel, plot, paper, data, axis, fraction.

### 3. Math And Algorithms

- Numeric min/max scans with NaN and infinity policy.
- Kahan or compensated summation for aggregates.
- Linear interpolation and inverse interpolation.
- Nice number calculation for axis domains and ticks.
- Decimal tick generation.
- Logarithmic tick generation with minor ticks.
- Date/time tick generation at year, quarter, month, week, day, hour, minute, second, and millisecond levels.
- Categorical band and point positioning.
- Quantile, percentile, median, quartile, IQR, MAD, variance, standard deviation, covariance, and correlation.
- Histogram binning with fixed count, fixed width, Sturges, Scott, and Freedman-Diaconis options.
- Kernel density estimation planning with Gaussian kernel.
- Stacking for zero baseline, normalized percent, diverging positive/negative, and streamgraph offsets.
- Pie and donut angle layout.
- Treemap layout: slice, dice, slice-dice, squarified.
- Sunburst radial hierarchy layout.
- Sankey node/link layout.
- Force graph layout planning with deterministic seed.
- Contour marching squares for gridded scalar fields.
- Heatmap grid bucketing.
- Hexbin point aggregation.
- Line simplification with Douglas-Peucker and radial distance options.
- Spline interpolation: Catmull-Rom, monotone cubic, natural cubic planning.
- Step interpolation: before, after, middle.
- Polar coordinate transforms.
- Ternary coordinate transforms.
- Geographic projection planning for equirectangular, Mercator, orthographic, and Albers-like projections.

### 4. Data Model Features

- Column-oriented numeric table.
- Row-oriented object table planning without reflection dependency.
- Series-oriented arrays for x, y, z, open, high, low, close, size, color value, label, and group.
- Category table with stable category order.
- Missing value policy: skip, gap, zero, interpolate, carry forward.
- Sorting by x, y, category, value, series name, or custom order array.
- Grouping by category or key.
- Aggregation: count, sum, mean, median, min, max, first, last, variance, standard deviation, percentile.
- Binning for histograms and density plots.
- Pivoting for grouped bars, stacked bars, heatmaps, and matrices.
- Joining datasets by key planning.
- Filtering by numeric range, category inclusion, text match, and missing values.
- Calculated columns using class-based expression operations.
- Data validation reports with warnings for mismatched lengths, unsupported values, and empty series.
- Streaming append/update model for incremental charts.
- Data decimation for large line and scatter series.

### 5. Visual Encoding Features

- Position channels: x, y, x2, y2, radius, angle, latitude, longitude.
- Mark channels: color, stroke color, fill color, opacity, size, shape, stroke width, dash pattern.
- Text channels: label, tooltip text, annotation text, value label.
- Order and z-index channels.
- Detail/group channels for multi-series splitting.
- Facet channels: row, column, wrap.
- Color scales for categorical, ordinal, sequential, diverging, threshold, and binned values.
- Size scales for area-correct bubble sizing.
- Shape scales for marker symbols.
- Opacity scales.
- Conditional encodings for selected/highlighted states planning.
- VisualMap/colorbar equivalents for continuous and piecewise encoded dimensions.

### 6. Scale Features

- Linear scale.
- Log scale with configurable base.
- Symmetric log planning.
- Power scale.
- Square-root scale.
- Time scale.
- UTC time scale planning.
- Category band scale with inner and outer padding.
- Category point scale.
- Ordinal lookup scale.
- Quantile scale.
- Quantize scale.
- Threshold scale.
- Diverging scale.
- Radial scale.
- Angle scale.
- Color scale.
- Size scale.
- Reversed domains and ranges.
- Explicit domain/range.
- Automatic domain from data.
- Domain padding.
- Include-zero policy.
- Nice domain expansion.
- Clamping.
- Unknown value mapping.
- Shared scales across plots.
- Independent scales per subplot or facet.

### 7. Axes, Ticks, And Grids

- X and Y axes on bottom, top, left, and right.
- Multiple axes per side.
- Secondary axes with independent domains.
- Linked axes across subplots.
- Radial axes.
- Angular axes.
- Ternary axes planning.
- Axis title, subtitle, and unit labels.
- Major ticks.
- Minor ticks.
- Explicit tick values.
- Automatic tick count.
- Tick formatting for number, percent, currency planning, scientific notation, SI prefixes, duration, and dates.
- Multi-line tick labels.
- Rotated tick labels.
- Tick label auto-skip.
- Tick label collision detection.
- Tick label trimming and ellipsis planning.
- Axis line styling.
- Tick line styling.
- Major grid lines.
- Minor grid lines.
- Alternating grid bands.
- Zero line.
- Threshold/reference lines.
- Axis breaks planning.
- Log minor tick styling.
- Reversed axes.
- Fixed aspect ratio axes.

### 8. Layout Features

- Figure size in pixels and logical units.
- Device pixel ratio / export scale.
- Margins and padding.
- Plot area calculation.
- Auto layout for title, subtitle, caption, legends, colorbars, axes, and labels.
- Manual layout override.
- Grid layout for subplots.
- Row/column spans.
- Shared axes in subplot grids.
- Faceted layouts by row/column/wrap.
- Insets and overlay plots.
- Polar plot areas.
- Ternary plot areas planning.
- Small multiples.
- Dashboard-style multi-panel charts.
- Legend docking to any side.
- Legend overlay inside plot area.
- Colorbar docking.
- Responsive layout planning by target size.
- Clipping regions per plot area and per series.

### 9. Styling And Theming

- Global theme object.
- Built-in themes: default light, default dark, print, colorblind-safe, high-contrast, monochrome.
- Palette support for categorical colors.
- Sequential and diverging colormaps.
- Per-series style overrides.
- Per-point style overrides.
- Background color and plot area color.
- Border and frame styling.
- Line width, dash pattern, caps, joins, miter limit.
- Fill opacity and stroke opacity.
- Marker shape, size, stroke, fill.
- Font family, size, weight planning, style planning.
- Text color and outline/shadow planning.
- Grid style.
- Axis style.
- Legend style.
- Annotation style.
- Template/default cascade: library defaults, theme, chart, subplot, series, point.

### 10. Text And Labels

- Built-in simple bitmap font for raster output.
- SVG text output using font-family strings.
- Optional built-in vector font planning for consistent raster/SVG text.
- Text measurement for built-in font.
- Numeric formatting.
- Date/time formatting.
- Category labels.
- Multi-line text.
- Text wrapping.
- Text rotation.
- Text clipping.
- Label placement around points, bars, slices, and regions.
- Label collision detection.
- Label leader lines for pies/donuts.
- Value labels for bars, points, heatmap cells, pie slices, and contours.
- Rich text spans planning for SVG and limited raster support.
- Right-to-left and complex text shaping planning as an advanced limitation item.

### 11. Raster Rendering

- Raw bitmap buffer allocation.
- Pixel get/set.
- Clear rectangle.
- Alpha blending.
- Clipping rectangles.
- Anti-aliased line drawing.
- Thick line strokes.
- Polyline strokes.
- Polygon fills.
- Triangle rasterization.
- Rectangle and rounded rectangle planning.
- Circle and ellipse fills/strokes.
- Arc and wedge drawing.
- Bezier curve flattening.
- Path fill and stroke.
- Dashed lines.
- Marker symbol drawing.
- Gradient fills planning.
- Pattern fills planning.
- Image blit planning from raw bitmap.
- Supersampling option for higher quality.
- Fast integer drawing path for simple charts.

### 12. SVG Rendering

- SVG document writer.
- XML escaping.
- ViewBox and sizing.
- Groups and transforms.
- Path, line, polyline, polygon, rect, circle, ellipse, text, and image elements.
- Clip paths.
- Gradients.
- Patterns.
- Stroke/fill style serialization.
- Dash arrays, caps, joins, miter limit.
- Text anchors and baseline approximation.
- Metadata/title/desc elements for accessibility.
- CSS-free inline style output for portability.
- Optional CSS class output planning.
- Deterministic element order and ids.

### 13. Output Encoders

- Raw bitmap output as byte array with width, height, stride, and pixel format.
- BMP encoder with file header, DIB header, row padding, top-down/bottom-up option, 24-bit and 32-bit output.
- PNG encoder with signature, IHDR, IDAT, IEND chunks.
- PNG color types: truecolor, truecolor-alpha, grayscale planning, indexed planning.
- PNG filter types: none initially, sub/up/average/paeth later.
- PNG compression using built-in `DeflateStream` where valid for zlib/DEFLATE payload creation.
- PNG CRC32 and Adler32 implemented in source.
- SVG output as string, stream, and file.
- Encoder options for background, transparency, pixel format, compression level, and metadata.
- Export helpers for `ToRawBitmap`, `SaveBmp`, `SavePng`, `ToSvg`, and `SaveSvg`.

### 14. Core Chart Types

- Line chart.
- Multi-line chart.
- Step line chart.
- Spline line chart.
- Area chart.
- Stacked area chart.
- Streamgraph.
- Scatter chart.
- Bubble chart.
- Bar chart vertical/horizontal.
- Grouped bar chart.
- Stacked bar chart.
- Percent stacked bar chart.
- Range bar / Gantt-like bar.
- Histogram.
- Density plot.
- Box plot.
- Violin plot planning.
- Error bar chart.
- Stem plot.
- Lollipop chart.
- Waterfall chart.
- Pie chart.
- Donut chart.
- Rose/Nightingale chart.
- Radar/spider chart.
- Polar line/scatter/bar chart.
- Heatmap.
- Calendar heatmap.
- Contour lines.
- Filled contours.
- Image/matrix plot from raw values.
- Hexbin plot.
- 2D histogram.
- Candlestick chart.
- OHLC chart.
- Volume bars.
- Funnel chart.
- Gauge chart.
- Bullet chart.
- Treemap.
- Sunburst.
- Icicle planning.
- Sankey diagram.
- Node-link graph.
- Tree/dendrogram.
- Parallel coordinates.
- Parallel categories.
- Slope chart.
- Dumbbell chart.
- Bump chart.
- Sparklines.
- Table chart planning.

### 15. Composition And Advanced Views

- Multiple series in one plot.
- Mixed chart types in one Cartesian plot.
- Dual-axis charts.
- Shared legends.
- Shared color scales.
- Layered marks.
- Overlays: trendlines, reference bands, confidence bands, annotations, shapes.
- Subplots grid.
- Facets by category.
- Insets.
- Marginal histograms/rugs around scatter plots.
- Small multiples.
- Linked brushing planning through sidecar metadata.
- Animation/timeline planning as sequential frame export.

### 16. Statistical And Analytical Overlays

- Mean/median lines.
- Quantile bands.
- Standard deviation bands.
- Confidence interval bands from supplied data.
- Linear regression line.
- Polynomial regression planning.
- Moving average.
- Exponential moving average.
- Rolling min/max.
- Cumulative sum.
- Cumulative distribution / ECDF.
- Pareto chart.
- Control chart planning.
- Correlation matrix heatmap.
- Trendline label and equation planning.

### 17. Annotations And Decorative Layers

- Text annotations in data, pixel, or paper coordinates.
- Arrow annotations.
- Point callouts.
- Axis-aligned reference lines.
- Reference bands.
- Rectangles, circles, ellipses, polygons, and arbitrary paths.
- Background images planning from raw bitmap.
- Watermark text.
- Captions and credits.
- Custom marker symbols.
- Custom legend entries.

### 18. Accessibility And Metadata

- Chart title, subtitle, caption, and description.
- SVG `<title>` and `<desc>`.
- Series and point descriptions.
- Colorblind-safe palettes.
- High-contrast theme.
- Text alternatives emitted as sidecar text/JSON planning.
- Data table export planning.
- Semantic grouping in SVG.
- Avoid color-only encoding by supporting shape, dash, label, and pattern channels.

### 19. Performance And Large Data

- Avoid unnecessary allocations in rendering loops.
- Array-based series storage.
- Data bounds caching with explicit invalidation.
- Path simplification.
- Point decimation for lines and scatter.
- Heatmap tiling planning.
- Incremental rendering for appended data planning.
- Reuse layout and render buffers when size/options do not change.
- Benchmark suite for common chart sizes.
- Memory budget tests for large datasets.

### 20. Error Handling And Diagnostics

- Validation result class with warnings and errors.
- Deterministic handling of null/NaN/infinity.
- Clear messages for mismatched array lengths.
- Clear messages for unsupported output options.
- Debug overlay for layout boxes.
- Optional render trace log.
- Golden image comparison utilities in tests.

## Remaining Public API Completion Tasks

- [DONE] T001: Implement a concrete public chart configuration model and connect every existing configuration enum to working document, renderer, scale, series, sorting, missing-value, and output behavior.
- [DONE] T002: Complete the public foundational data layer with angle and numeric-interval values, text-measurement results, device/export scale settings, and allocation-conscious bulk access using built-in .NET span types where it improves buffer or numeric processing.
- [DONE] T003: Expand public statistics and math operations to cover compensated summation, quantiles, percentiles, quartiles, IQR, MAD, population and sample dispersion, covariance, correlation, interpolation, inverse interpolation, and nice-number calculation with explicit NaN and infinity policies.
- [DONE] T004: Add public Catmull-Rom, monotone cubic, and natural cubic interpolation choices and implement each choice consistently for line, area, and path generation.
- [DONE] T005: Add public ternary transforms and equirectangular, Mercator, orthographic, and Albers-like geographic projections, including inverse mapping where mathematically defined.
- [DONE] T006: Complete hierarchy and relationship layout choices with slice, dice, slice-dice, and squarified treemaps plus deterministic seeded force-directed graph layout.
- [DONE] T007: Add a public series-oriented data model for x, y, z, open, high, low, close, size, color, label, and group arrays, with validation and conversion to existing tables and series builders.
- [DONE] T008: Implement every missing-value policy across tables, transforms, domains, and chart builders, including skip, gap, zero, interpolation, and carry-forward behavior.
- [DONE] T009: Complete public data operations with custom-order sorting, key joins, calculated columns represented without reflection, and the full planned aggregation set.
- [DONE] T010: Add public append, update, and explicit invalidation operations for streaming tables and series, and make document re-rendering consume the updated data correctly.
- [DONE] T011: Expand visual encoding channels to radius, angle, latitude, longitude, stroke color, fill color, stroke width, dash, value label, z-index, and wrap facets, and make all declared channels affect rendered output.
- [DONE] T012: Add public conditional encodings and selection/highlight state models, including deterministic sidecar metadata for linked selections without requiring an interactive runtime.
- [DONE] T013: Implement public symmetric-log, power, square-root, UTC-time, quantile, quantize, threshold, diverging, radial, angle, opacity, and shape scales corresponding to the already declared scale kinds.
- [DONE] T014: Complete scale policy support for automatic domains, domain padding, include-zero, nice expansion, reversal, clamping, unknown values, shared scales, and independent subplot scales.
- [DONE] T015: Complete tick generation and formatting for calendar intervals, minor logarithmic ticks, currency, explicit culture selection, multiline labels, rotation, collision skipping, trimming, and ellipsis.
- [DONE] T016: Complete axis features with subtitles, units, multiple offsets per side, linked axes, ternary axes, threshold lines, breaks, fixed aspect ratios, and styling for axis lines, ticks, minor ticks, bands, and labels.
- [DONE] T017: Extend public layout to facets by row, column, and wrap; ternary regions; dashboard panels; docked and overlay legends and colorbars; responsive target sizes; device pixel ratio; and per-plot clipping.
- [DONE] T018: Complete style models and cascading for dash arrays, miter limits, independent fill and stroke opacity, borders, per-point overrides, font weight and style, text outline and shadow, and custom marker definitions.
- [DONE] T019: Complete public text layout with deterministic measurement, wrapping, multiline alignment, rotation, clipping, rich spans, Unicode fallback, right-to-left ordering, and clearly defined built-in behavior for text that cannot be shaped with built-in .NET facilities.
- [DONE] T020: Extend retained render commands with groups, transforms, clip regions, arcs and wedges, gradient and pattern paints, and raw-bitmap image placement so raster and SVG use the same public scene.
- [DONE] T021: Complete raster behavior for all declared blend modes, dashed strokes, rounded rectangles, gradients, patterns, image blits, supersampling, and consistent clipping of shapes, paths, text, and images.
- [DONE] T022: Complete raw bitmap support for RGBA32, BGRA32, RGB24, grayscale, alpha-mask, and true indexed-palette buffers, including palette storage, format conversion, row-stride handling, and span-based buffer access where appropriate.
- [DONE] T023: Complete SVG output for retained groups and transforms, clipping, gradients, patterns, embedded raw images, semantic series and point grouping, accessibility roles, deterministic identifiers, and optional CSS-class serialization.
- [DONE] T024: Complete BMP and PNG option models and encoders for all planned pixel and color types, PNG metadata and compression settings, BMP row orientation and bit depths, and byte, stream, and file destinations using only built-in .NET APIs.
- [DONE] T025: Add a unified public export workflow that dispatches raw bitmap, BMP, PNG, and SVG from scenes and documents, including background, transparency, export scale, metadata, and non-closing stream options.
- [DONE] T026: Expand declarative chart construction to all applicable planned marks, grouping, ordering, stacking, layering, facets, shared guides, and conditional channels instead of the current seven Cartesian marks.
- [DONE] T027: Complete missing chart-family builders for horizontal grouped and stacked bars, funnel charts, and any inventory chart variant that currently has only an enum, layout plan, or indirect primitive construction path.
- [DONE] T028: Add public render-to-scene operations for Sankey, node-link, force graph, tree, dendrogram, parallel-coordinate, and parallel-category layouts so every relationship layout can reach every required output backend.
- [DONE] T029: Complete statistical transforms and overlays with polynomial regression, supplied confidence and standard-deviation bands, control charts, trendline equations and labels, and configurable rolling and cumulative calculations.
- [DONE] T030: Complete annotation and label output with coordinate-unit transforms, reference shapes in data, plot, paper, and pixel coordinates, background raw images, value-label formatters, and leader-line collision handling.
- [DONE] T031: Implement the complete document composition renderer so public titles, subtitles, captions, plot backgrounds, axes, grids, series, legends, colorbars, annotations, subplots, insets, and shared guides are laid out and drawn by one public workflow.
- [DONE] T032: Add public sequential-frame export and linked-brushing sidecar models for the planned timeline and interaction metadata features while keeping core rendering deterministic and non-interactive.
- [DONE] T033: Complete accessibility output with structured text and JSON alternatives, tabular data export, semantic SVG groups, series and point descriptions, and non-color cues through shapes, dashes, labels, and patterns.
- [DONE] T034: Extend public validation and diagnostics to every new data, scale, axis, layout, style, render-command, encoder, accessibility, and export option, with stable issue codes and exception-less validation paths for expected failures.
- [DONE] T035: Add behavior tests for every public geometry, color, bitmap, path, style, theme, palette, factory, copy, mutation, and validation operation, including boundary values and documented exception cases.
- [DONE] T036: Add behavior tests for every public data table, column, matrix, grouping, joining, filtering, sorting, aggregation, binning, stacking, statistics, transform, missing-value, streaming, and reduction operation.
- [DONE] T037: Add behavior tests for every public scale, transform, tick, formatter, axis, legend, visual-map, encoding, and declarative-chart operation, covering each enum choice and invalid configuration.
- [DONE] T038: Add layout and document tests covering titles, captions, axes, legends, colorbars, grids, spans, shared axes, facets, insets, overlays, responsive sizes, device scale, clipping, themes, and complete composed rendering.
- [DONE] T039: Add chart-family tests for every direct builder and layout path, including every interpolation, orientation, stacking mode, statistical chart, grid chart, polar chart, hierarchy chart, relationship chart, compact chart, financial chart, overlay, annotation, and label-placement variant.
- [DONE] T040: Add raster conformance tests for every retained command, pixel format, blend mode, cap, join, fill rule, dash, marker, text mode, clip, transform, gradient, pattern, image blit, and quality setting.
- [DONE] T041: Add BMP and PNG conformance tests against the downloaded specifications, including headers, row order, padding, color types, filters, chunks, CRC and Adler checks, metadata, malformed options, stream behavior, and decoding with an independent test-only library.
- [DONE] T042: Add SVG and XML conformance tests for escaping, geometry, paths, text, images, groups, transforms, clips, paints, deterministic IDs, semantic structure, accessibility metadata, CSS-free output, optional CSS classes, streams, and files.
- [DONE] T043: Add public-only consumer workflow tests for every exposed constructor and interface operation without calling internal operation classes, covering raw bitmap, BMP, PNG, SVG, direct scenes, declarative charts, documents, custom renderers, and custom tick formatters.
- [DONE] T044: Add cross-backend functional tests that render representative scenes through raster and SVG, verify equivalent geometry and styling semantics, and assert deterministic repeated output for fixed inputs.
