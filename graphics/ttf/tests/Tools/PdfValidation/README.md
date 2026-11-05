# Independent PDF validation

These opt-in adapters consume generated PDFs independently of the production
library and normal unit tests:

- Apache PDFBox extracts Unicode text, exercising PDF parsing, font resources,
  encodings, CMaps, and embedded font programs.
- qpdf `--check` validates PDF structure, encryption, linearization, and stream
  encoding. Warnings fail by default because generated test PDFs should be
  clean; exploratory runs can use `--allow-qpdf-warnings`.
- veraPDF emits a JSON PDF/A or PDF/UA conformance report. Its compliance result
  is recorded without being treated as a failure unless
  `--verapdf-expectation` requests a specific outcome. This matters because a
  structurally valid PDF need not claim or satisfy PDF/A.

None of these tools is invoked by `ttf.slnx` or `ttf.Tests.csproj`. Normal test
runs do not download software, probe executables, or create PDF reports.

## Install the tools

PDFBox 3.0.8 is Apache-2.0 licensed and requires Java 8 or later. Download its
standalone app only when the external validation job needs it:

```sh
python3 tests/Tools/PdfValidation/download-pdfbox.py \
  --output /tmp/ttf-pdf-tools/pdfbox-app-3.0.8.jar
```

The downloader uses the immutable Apache archive URL and verifies the release
SHA-512 in `tools.json` before atomically installing the jar. The jar is not
checked into this repository.

Install qpdf using the platform package manager. Install the veraPDF Greenfield
CLI separately under either of its offered MPL-2.0 or GPL-3.0-or-later license
terms. `tools.json` records the versions used to define this adapter. A newer
qpdf or veraPDF can be evaluated explicitly with
`--allow-version-mismatch`; PDFBox always requires the pinned checksum.

## Run validation

Pass generated PDF files or directories containing them:

```sh
PDFBOX_APP_JAR=/tmp/ttf-pdf-tools/pdfbox-app-3.0.8.jar \
python3 tests/Tools/PdfValidation/validate-pdfs.py \
  --report /tmp/ttf-pdf-validation.json \
  /tmp/ttf-generated-pdfs
```

`--java`, `--qpdf`, and `--verapdf` override executable discovery. The
corresponding `JAVA`, `QPDF`, and `VERAPDF` environment variables are also
supported. Use `--use pdfbox`, `--use qpdf`, or
`--use pdfbox,qpdf` for focused jobs.

The default veraPDF flavour is `0`, which requests automatic profile detection.
For a PDF/A-producing test, select and require the intended profile, for
example:

```sh
python3 tests/Tools/PdfValidation/validate-pdfs.py \
  --use verapdf \
  --verapdf-flavour 2u \
  --verapdf-expectation compliant \
  /tmp/ttf-generated-pdfs
```

Missing requested tools fail the command. Optional matrix jobs may pass
`--allow-missing`, which records explicit skips while still running every tool
that is available. Reports contain content hashes, bounded diagnostics, qpdf
exit status, PDFBox extracted-text hashes, and veraPDF compliance results; they
do not copy extracted document text into logs.
