# OpenType Sanitizer validation

This opt-in test tool validates generated fonts with the independent
[OpenType Sanitizer](https://github.com/khaledhosny/ots). Neither the sanitizer,
the corpus emitter, nor this script is referenced by `ttf.slnx` or the normal
test project, so ordinary builds and tests do not probe external executables or
write generated font files.

Install the pinned OTS 9.3.0 release at commit
`884861e0103b41d7e9845fdaf9fd1e3c12819103` separately, or identify its
`ots-sanitize` executable with `--ots` or the `OTS_SANITIZE` environment
variable. The runner invokes the documented
`ots-sanitize INPUT OUTPUT` form, requires a successful exit and a font-shaped
sanitized output, applies a per-file timeout, captures bounded diagnostics, and
can write a deterministic JSON report. It rejects a different tool version by
default; `--allow-version-mismatch` is reserved for exploratory comparisons.

Generate a small corpus from public production APIs:

```sh
dotnet run \
  --project tests/Tools/OpenTypeValidation/GeneratedFontCorpus/GeneratedFontCorpus.csproj \
  -- \
  /tmp/ttf-generated-fonts \
  tests/Assets/ColorFonts
```

Validate it:

```sh
python3 tests/Tools/OpenTypeValidation/validate-fonts.py \
  --report /tmp/ttf-ots-report.json \
  /tmp/ttf-generated-fonts
```

The command fails when OTS is unavailable. Optional CI jobs may pass
`--allow-missing` to produce an explicit successful skip; normal tests do not
invoke the command at all.

## Regression fixtures

`tests/Regression/Fonts/manifest.json` is the content-addressed inventory for
minimized sanitizer regressions. A retained fixture must be the smallest input
that still reproduces the independently confirmed result. Its entry records
the source operation or issue, exact minimization procedure, byte size,
SHA-256, and whether OTS must accept or reject it.

After adding or changing a fixture, validate the corpus with:

```sh
python3 tests/Tools/OpenTypeValidation/validate-fonts.py \
  --include-regressions \
  /tmp/ttf-generated-fonts
```

Do not retain user fonts, proprietary fonts, unminimized fuzz outputs, or files
without redistribution permission. An empty manifest means no independently
confirmed sanitizer regression has been discovered and minimized yet; it must
not be populated with an intentionally malformed file merely to exercise the
adapter.
