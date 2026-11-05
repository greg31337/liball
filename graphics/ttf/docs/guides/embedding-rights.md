# Embedding rights

OpenType `OS/2.fsType` flags are technical permissions supplied by the font,
not legal advice. Inspect them before subsetting, PDF assembly, document editing,
or distributing generated artifacts. Preserve the font's license records and
your own provenance separately.

```csharp
FontEmbeddingRights rights = face.GetEmbeddingRights();

System.Console.WriteLine(rights.GetPermission());
System.Console.WriteLine(rights.GetRecommendedAction());
System.Console.WriteLine("Embedding: " + rights.AllowsEmbedding());
System.Console.WriteLine("Subsetting: " + rights.AllowsSubsetting());
System.Console.WriteLine("Outline data: " + rights.AllowsOutlineEmbedding());
System.Console.WriteLine("Bitmap data: " + rights.AllowsBitmapEmbedding());
```

The main outcomes are:

- `Installable` permits embedding and permanent installation.
- `Restricted` requires explicit permission from the rights holder.
- `PreviewAndPrint` requires temporary loading and read-only document use.
- `Editable` permits temporary embedding in editable documents.

Independent flags can prohibit subsetting or permit only bitmap embedding. Use
the boolean accessors and `GetRecommendedAction()` rather than interpreting the
raw bits yourself. `GetRawFlags()` remains available for auditing.

## Enforce a policy at output time

Rights inspection does not itself authorize output. Select an explicit policy
for every builder:

```csharp
FontSubsetOptions subsetOptions =
    new FontApiFactory().CreateStrictSubsetOptions();
subsetOptions.SetRightsPolicy(FontSubsetRightsPolicy.Reject);

PdfFontOptions pdfOptions =
    new FontApiFactory().CreateStrictPdfOptions();
pdfOptions.SetRightsPolicy(PdfFontRightsPolicy.Reject);
```

`Reject` fails a conflict. `Report` records the conflict but does not change the
requested mode; use it only when a higher layer has an independently established
authorization and will enforce the result. `EmbedFullFont` changes subset output
to full-font output when *only* the no-subsetting flag conflicts. It never
overrides restricted embedding or a bitmap-only restriction.

After output, audit the effective decision rather than assuming the request was
used:

```csharp
FontSubsetEmbeddingDecision decision = resource.GetEmbeddingDecision();
System.Console.WriteLine(decision.GetRequestedEmbeddingMode());
System.Console.WriteLine(decision.GetEffectiveEmbeddingMode());
System.Console.WriteLine(decision.HasPolicyConflict());
System.Console.WriteLine(decision.WasFullFontForced());
```

Also inspect the decision diagnostics and the PDF resource compatibility report.
For preview-and-print fonts, the application must enforce the read-only usage
constraint at the document and workflow level. For bitmap-only fonts, an
outline-based PDF font resource is not permitted merely because a separate
color bitmap appearance can be drawn.
