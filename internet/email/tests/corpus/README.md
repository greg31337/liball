# Test corpus

Store larger standards, interoperability, malformed-message, archive, and
security samples here. A corpus entry must be redistributable, stable, useful
for more than a single small assertion, and free of personal data. Keep required
license and attribution notices beside the relevant sample or corpus subset.

All data files require an entry in `MANIFEST.tsv` and must follow
`tests/TEST_DATA.md`.

## Included subsets

- `standards/` contains original, synthetic messages shaped to exercise
  published RFC formats without copying correspondence or RFC prose.
- `libraries/` contains deterministic output from major email libraries using
  fixed synthetic inputs.
- `malformed/` contains deliberately damaged, inert messages for recovery and
  preservation work.
- `international/` contains UTF-8 headers, addresses, parameters, and bodies
  using reserved domains.
- `reports/` contains synthetic DSN and ARF multipart/report messages.
- `archives/` contains synthetic mbox and Maildir layouts.
- `security/` contains inert CMS/S/MIME and PGP/MIME structures made only with
  test material.

Every entry marked `CC0-1.0` in the manifest is original synthetic material
dedicated under [CC0 1.0](https://creativecommons.org/publicdomain/zero/1.0/).
Standards URLs identify the format being exercised, not copied message text.
