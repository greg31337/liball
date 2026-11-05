# pptx

`pptx` is a C# library for reading, editing, generating, validating, and round-tripping `.pptx` Open XML packages using only built-in .NET libraries in `src`.

## Public API

The supported API is the set of `public` types and members in `src`. Every public symbol has XML documentation describing its behavior, parameters, return value, and relevant conventions. Symbols marked `internal` are implementation details and may change without compatibility guarantees; tests can access them through `InternalsVisibleTo`, but applications should not depend on them.

The model uses public fields instead of properties to comply with the repository's restricted C# syntax. Data types live under `src/Api/Data`; behavior that updates XML, relationships, content types, or dirty state is exposed by interface-backed operation classes under `src/Api/Classes`.

Nullable reference analysis is enabled across the library. A `?` annotation marks a normal absence case, such as an optional package part or an unsuccessful `Find`/`Resolve` operation. Non-nullable parameters and return values are expected to be present; text APIs generally use an empty string, and collection APIs generally use an empty collection, instead of `null`.

Import, merge, and split workflows accept the narrow `IPptxPresentationSource` interface in addition to `PptxDocument`. `PptxDocument` implements this interface, while applications can implement it for lazy loading, alternate storage, or lightweight test doubles. Mutable models remain concrete data values; operation classes implement contracts and can be replaced or mocked independently.

## Getting Started

### Create a deck

```csharp
using pptx;

PptxDocumentFactory documents = new PptxDocumentFactory();
PptxPresentationOperations presentations = new PptxPresentationOperations();
PptxSlideOperations slides = new PptxSlideOperations();
PptxResultOperations results = new PptxResultOperations();
PptxDocument document = documents.CreatePresentation();
PptxPresentation presentation = document.LoadPresentation();
PptxSlide slide = presentations.GetSlide(presentation, 0);

slides.AddTextBox(slide, "Quarterly update", new PptxBounds(914400L, 914400L, 5486400L, 914400L));
slides.AddAutoShape(slide, PptxShapeType.Rectangle, new PptxBounds(914400L, 2286000L, 1828800L, 914400L));

PptxResult validation = document.Validate();
if (results.Failed(validation))
{
    throw new System.ArgumentException(results.FormatDiagnostics(validation));
}

document.SaveFile("created.pptx");
```

### Edit text

```csharp
using pptx;

PptxDocumentFactory documents = new PptxDocumentFactory();
PptxPresentationOperations presentations = new PptxPresentationOperations();
PptxDocument document = documents.OpenFile("template.pptx");
PptxPresentation presentation = document.LoadPresentation();

int changed = presentations.ReplaceText(presentation, "{{CustomerName}}", "Contoso");
if (changed == 0)
{
    throw new System.ArgumentException("The placeholder was not found.");
}

document.SaveFile("edited.pptx");
```

### Replace images

```csharp
using System.Collections.Generic;
using System.IO;
using pptx;

PptxDocumentFactory documents = new PptxDocumentFactory();
PptxPresentationOperations presentations = new PptxPresentationOperations();
PptxSlideOperations slides = new PptxSlideOperations();
PptxShapeTreeOperations shapeTrees = new PptxShapeTreeOperations();
PptxShapeOperations shapesApi = new PptxShapeOperations();
PptxDocument document = documents.OpenFile("deck.pptx");
PptxPresentation presentation = document.LoadPresentation();
PptxSlide slide = presentations.GetSlide(presentation, 0);
List<PptxShape> shapes = shapeTrees.GetAllShapes(slides.GetShapeTree(slide));
byte[] replacement = File.ReadAllBytes("replacement.png");

for (int index = 0; index < shapes.Count; index++)
{
    if (shapesApi.IsPicture(shapes[index]))
    {
        shapesApi.ReplaceImage(shapes[index], replacement);
        break;
    }
}

document.SaveFile("image-replaced.pptx");
```

### Add a chart

```csharp
using pptx;

PptxDocumentFactory documents = new PptxDocumentFactory();
PptxPresentationOperations presentations = new PptxPresentationOperations();
PptxSlideOperations slides = new PptxSlideOperations();
PptxDocument document = documents.CreatePresentation();
PptxPresentation presentation = document.LoadPresentation();
PptxChartData data = new PptxChartData();
data.Categories.Add("Q1");
data.Categories.Add("Q2");

PptxChartSeries revenue = new PptxChartSeries();
revenue.Name = "Revenue";
revenue.Values.Add("10");
revenue.Values.Add("20");
data.Series.Add(revenue);

slides.AddChart(presentations.GetSlide(presentation, 0), PptxChartType.Column, data, new PptxBounds(914400L, 914400L, 5486400L, 3200400L));
document.SaveFile("chart.pptx");
```

### Merge decks

```csharp
using pptx;

PptxDocumentFactory documents = new PptxDocumentFactory();
PptxPresentationOperations presentations = new PptxPresentationOperations();
PptxResultOperations results = new PptxResultOperations();
PptxDocument targetDocument = documents.OpenFile("base.pptx");
PptxDocument sourceDocument = documents.OpenFile("appendix.pptx");
PptxPresentation target = targetDocument.LoadPresentation();

presentations.MergeDocument(target, sourceDocument);

PptxResult validation = targetDocument.Validate();
if (results.Failed(validation))
{
    throw new System.ArgumentException(results.FormatDiagnostics(validation));
}

targetDocument.SaveFile("merged.pptx");
```

## Feature Support Levels

Support levels describe what the public API is intended to do for each feature area:

| Level | Meaning |
| --- | --- |
| `Edit` | The library can create or modify the feature with public APIs, then save and reopen it. |
| `Read` | The library can parse the feature into a model, but editing support is limited or unavailable. |
| `Preserve` | The library keeps the feature's package parts, relationships, and XML intact during round-trip saves. |
| `Inspect` | The library can identify, list, extract, summarize, or scan the feature without full editing support. |
| `Out of scope` | The feature is intentionally not implemented by this library. |

## Feature Matrix

| Feature area | Level | Notes |
| --- | --- | --- |
| Package open/save/validation | `Edit` | Create, open, repair, validate, clean up, save, clone, and round-trip package parts and relationships. |
| Slide creation, ordering, cloning, merging, and splitting | `Edit` | Add slides, clone slides, import slides, merge decks, split decks, and preserve related parts. |
| Text boxes, paragraphs, runs, bullets, and replacement | `Edit` | Add text boxes, edit text, replace text, fill simple templates, and export text or outlines. |
| Images | `Edit` | Add, replace, deduplicate, crop, tile/stretch, inspect, and extract image parts. |
| Tables | `Edit` | Add tables, edit cells, row heights, column widths, and merge or split cells. |
| Charts and chart data | `Edit` | Add and edit common chart families, cached data, titles, legends, axes, gridlines, markers, data labels, and embedded workbooks. |
| Auto-shapes, connectors, fills, lines, effects, and geometry | `Edit` | Add and edit common preset shapes, connectors, fill styles, line styles, effects, and custom geometry. |
| Hyperlinks and actions | `Edit` | Read and write external URLs, files, internal slide links, custom shows, and action settings. |
| Notes, comments, tags, custom XML, and document properties | `Edit` | Read, add, edit, remove, and preserve common metadata and collaboration parts. |
| Masters, layouts, placeholders, themes, and style resolution | `Read` | Load and inspect master/layout/theme structure, resolve inherited style defaults, and create slides from known layouts. |
| Transitions and animations | `Edit` | Parse common timing and transition data, add or edit common animation effects, and preserve unknown timing XML. |
| Media, embedded objects, VBA, diagrams, and security scanning | `Inspect` | Detect, list, extract, scan, and replace selected payloads while preserving unsupported package content. |
| Unknown Open XML parts, extension lists, and markup compatibility | `Preserve` | Unknown parts, relationships, namespaces, and extension XML are retained when possible. |
| Structural HTML, SVG, JSON, text, and outline export | `Inspect` | Export structural summaries and placeholders; these are not full-fidelity renderers. |
| Full-fidelity slide rendering, layout engine parity, and PowerPoint UI automation | `Out of scope` | Rendering pixels, matching PowerPoint's layout engine, and automating desktop applications are outside the library scope. |
