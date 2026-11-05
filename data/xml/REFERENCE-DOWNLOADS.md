# Reference downloads

These large references are linked instead of stored in the repository. Paths are relative to this project; the top-level `.gitignore` excludes downloaded local copies.

| Local reference path | Short description | Download |
| --- | --- | --- |
| `specs/test-suites/xslt30-test-master.zip` | W3C XSLT 3.0 conformance suite, pinned to the revision of the removed archive; save as xslt30-test-master.zip. | [Download](https://github.com/w3c/xslt30-test/archive/fddf1cf920087e791f13315d68dfbe874d97dc56.zip) |

## Running the XSLT conformance tests

`Xslt30SuiteTests` and `Xslt30SuiteAdapterTests` require the archive at the path above. From the `data/xml` directory, restore it before running those tests:

```sh
curl --fail --location \
  https://github.com/w3c/xslt30-test/archive/fddf1cf920087e791f13315d68dfbe874d97dc56.zip \
  --output specs/test-suites/xslt30-test-master.zip
```

The revision was read from the original GitHub ZIP comment. The adapter discovers the archive root, so the commit-based root directory is supported. The checksum recorded in `specs/SOURCES.md` describes the historical `master` ZIP; a commit-based download can have different ZIP bytes while retaining the same test contents.
