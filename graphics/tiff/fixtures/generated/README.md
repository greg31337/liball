# Generated TIFF Fixtures

This directory contains a small checked-in corpus of legal TIFF files generated
with this library. The fixtures are intentionally tiny so they can be used in
unit tests, examples, and compatibility checks without adding large binary data.

## Fixtures

- `classic-stripped-gray-2x2.tif`: classic TIFF, one 2x2 8-bit grayscale image,
  stripped storage, one raw strip.
- `classic-tiled-gray-2x2.tif`: classic TIFF, one 2x2 8-bit grayscale image,
  tiled storage, four 1x1 raw tiles.
- `bigtiff-stripped-gray-2x2.tif`: BigTIFF, one 2x2 8-bit grayscale image,
  stripped storage, one raw strip.

The tests in `TiffGeneratedFixtureCorpusTests` open and validate every `.tif`
file in this directory. `TiffGeneratedFixtureFactory` also regenerates the
expected bytes with this library and built-in .NET APIs so tests can compare the
checked-in corpus against deterministic helper output.
