# Independent-tool test adapters

The reusable adapters in `Fixtures/IndependentToolAdapters.cs` discover and
run external tools without a shell, capture stdout/stderr and exit status, use
isolated temporary workspaces, enforce a bounded timeout, and skip dependent
NUnit checks when an executable is unavailable.

| Adapter | PATH candidates | Override |
|---|---|---|
| LibRaw | `raw-identify`, `dcraw_emu` | `RAW_TEST_LIBRAW` |
| dcraw | `dcraw` | `RAW_TEST_DCRAW` |
| ExifTool | `exiftool` | `RAW_TEST_EXIFTOOL` |
| Exiv2 | `exiv2` | `RAW_TEST_EXIV2` |
| Adobe DNG Converter | none; explicit opt-in only | `RAW_TEST_ADOBE_DNG_CONVERTER` |
| Canon Digital Photo Professional | none; explicit opt-in only | `RAW_TEST_CANON_DPP` |
| Nikon NX Studio | none; explicit opt-in only | `RAW_TEST_NIKON_NX_STUDIO` |

An override must name an existing executable file. Vendor applications are
never launched merely because a conventional installation path exists; they
require explicit opt-in because GUI, licensing, and automation behavior varies
by host. Additional vendor tools use `IndependentToolAdapters.Vendor` with a
dedicated environment variable.

`RAW_TEST_TOOL_TIMEOUT_SECONDS` changes the default 60-second per-process
timeout and accepts values from 1 through 3600. A timeout returns a result with
`TimedOut` set and exit code `-1`; an unavailable adapter calls
`IgnoreIfUnavailable` to produce a clear NUnit skip reason.

Adapters expose version probing, execution against an existing file, execution
against caller bytes, and caller-managed `ExternalToolWorkspace` instances for
tools that create sidecar or decoded output files. T116 owns the corpus-level
metadata, decode, render, and writer compatibility assertions and matrix.
