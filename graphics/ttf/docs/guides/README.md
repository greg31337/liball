# User guides

These guides describe the supported workflows and the boundary between `ttf`
and a consuming renderer or PDF library. Examples assume `using ttf;` and a
reference to `src/ttf.csproj`.

Before choosing a workflow, consult the [support matrix](../support-matrix.md)
for format, feature, and table-level limitations. Applications that persist
generated artifacts or diagnostic data should also follow the
[versioning policy](../versioning.md).

- [Loading and inspection](loading-and-inspection.md) explains policy profiles,
  input ownership, collections, diagnostics, metadata, coverage, tables, and
  outlines.
- [Shaping, fallback, and vertical text](text-and-fallback.md) explains the text
  pipeline, cluster-safe fallback, the current public-API boundary, and vertical
  writing requirements.
- [Variable instances and subsetting](instances-and-subsets.md) shows coordinate
  selection, static serialization, glyph closure, output policies, and result
  maps.
- [PDF artifacts and color appearance](pdf-and-color.md) shows how to turn font
  usage into library-neutral PDF resource data and how to layer visible color
  artwork over semantic PDF text.
- [Embedding rights](embedding-rights.md) explains how to inspect `OS/2`
  permissions, select an enforcement policy, and audit the effective output
  decision.

The factories named `CreateStrict()` reject policy conflicts and malformed
input. The compatible factories accept only bounded, deterministic repairs and
report them. Use strict profiles for authoring and security-sensitive ingestion;
use compatible profiles when accepting real-world fonts is more important than
rejecting every specification defect.
