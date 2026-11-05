# Optional test-font downloader

This test-side tool retrieves content-pinned fonts from Noto, Adobe, Google
Fonts, SIL, and another OFL project. It uses only the Python standard library.
It is not referenced by either project file, build targets, or test setup, so
`dotnet build` and `dotnet test` never invoke it or access the network.

The source manifest pins immutable revisions, download sizes and SHA-256 values,
extracted-member sizes and SHA-256 values, licenses, formats, scripts, and
features. ZIP downloads are verified before a single named member is extracted;
the extracted font is verified independently. Redirects must remain on HTTPS,
paths cannot escape the selected cache, and temporary files are atomically
replaced only after validation.

Validate the manifest and list all available fixtures without network access or
filesystem writes:

```sh
python3 tests/Tools/TestFonts/download-test-fonts.py --list
```

Download one fixture explicitly to a disposable or user-managed cache:

```sh
python3 tests/Tools/TestFonts/download-test-fonts.py \
  --download \
  --cache /tmp/ttf-test-fonts \
  noto-sans-arabic-regular-2-013
```

Omit fixture IDs to download every pinned source. A valid existing file is
reused without network access. An invalid existing file is replaced only in
`--download` mode. Verify a cache offline with:

```sh
python3 tests/Tools/TestFonts/download-test-fonts.py \
  --verify \
  --cache /tmp/ttf-test-fonts
```

Downloaded files are optional differential/conformance inputs and are not
copied into `src/` or automatically discovered by unit tests. A future test
that consumes one must opt in explicitly, use the same fixture ID and hash, and
skip clearly when the user-managed cache is absent.

## Updating a source

Choose an immutable upstream commit or release, retain the corresponding
license source, and calculate both the complete-download and final-font sizes
and SHA-256 values. For an archive, pin one exact member path and never select by
basename or wildcard. Run `--list`, perform an explicit download into a fresh
cache, run `--verify` without network access, and review the resulting font's
format, scripts, and relevant tables before committing the manifest change.
