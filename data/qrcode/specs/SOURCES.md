# Reference sources

Retrieved 2026-08-27 and audited 2026-09-16. These files are research inputs,
not source-library dependencies. Code under `src/` must not depend on them at
run time. See `CONFORMANCE_SOURCES.md` for the authority rules, edition and
corrigendum audit, and the implementation-constant traceability gate.

## Normative and industry references

- ISO/IEC 18004:2024 official catalogue: <https://www.iso.org/standard/83389.html>. The complete standard is a paid publication and was not copied from an unofficial mirror.
- ISO/IEC 23941:2022 official catalogue: <https://www.iso.org/standard/77404.html>. The complete standard is a paid publication and was not copied from an unofficial mirror.
- ISO/IEC 15424:2025 official catalogue: <https://www.iso.org/standard/82433.html>. The complete standard is a paid publication and was not copied from an unofficial mirror.
- ISO/IEC 15415:2024 official catalogue: <https://www.iso.org/standard/76876.html>. The complete standard is a paid publication and was not copied from an unofficial mirror.
- `gs1/GS1-General-Specifications-26.0.0.pdf`: <https://ref.gs1.org/standards/genspecs/26.0.0/>.
- `gs1/GS1-2D-Barcodes-at-Retail-POS-1.0.pdf`: <https://ref.gs1.org/guidelines/2d-in-retail/1.0.0/GS1-2DRetailPOS-Guideline-i1.0-r-2024-05-28>.
- `ietf/RFC-3629-UTF-8.txt`: <https://www.rfc-editor.org/rfc/rfc3629.txt>.
- `unicode/SHIFTJIS.TXT`: <https://www.unicode.org/Public/MAPPINGS/OBSOLETE/EASTASIA/JIS/SHIFTJIS.TXT>. This is an obsolete historical mapping and must be checked against the licensed QR and JIS requirements before generating implementation tables.
- `unicode/GB2312.TXT`: Unicode's historical GB2312-80 mapping, preserved by CPython at <https://github.com/python/cpython/blob/main/Tools/unicode/python-mappings/GB2312.TXT> after removal from Unicode's current public mapping tree. Its header identifies Unicode table version 1.0 dated 1999-10-08 and grants redistribution of the mapping data.
- `unicode/CP932.TXT`: Microsoft CP932 mapping published in Unicode's vendor mapping directory at <https://www.unicode.org/Public/MAPPINGS/VENDORS/MICSFT/WINDOWS/CP932.TXT>. It is used only to identify vendor additions and conflicts relative to the standard Shift JIS layer.
- `unicode/CP936.TXT`: Microsoft CP936 mapping published in Unicode's vendor mapping directory at <https://www.unicode.org/Public/MAPPINGS/VENDORS/MICSFT/WINDOWS/CP936.TXT>. It is used only to identify vendor additions and conflicts relative to the GB2312 layer.
- `tests/Fixtures/Golden/sources/*.svg`: CC0 legacy Model 1 encoded and boundary-layout references from Wikimedia Commons. Source pages, derivation, and limitations are recorded in `tests/Fixtures/README.md`; exact hashes are in both checksum manifests.
- `denso/*.html`: public QR Code descriptions from DENSO WAVE at <https://www.qrcode.com/en/>.
- `aim/AIM-ECI-Part-1-catalog.html`: AIM catalogue description at <https://web.aimglobal.org/external/wcpages/wcecommerce/eComItemDetailsPage.aspx?Category=4&ItemID=44>; the licensed publication itself is not included.
- `aim/AIM-ECI-Part-3-catalog.html`: archived catalogue description of the
  expired 2021 public review for Version 2 at
  <https://web.aimglobal.org/external/wcpages/wcecommerce/eComItemDetailsPage.aspx?ItemID=776>.
  It is provenance evidence only and must not be used as the current register.
  AIM announced the completed revision on 2022-02-03 at
  <https://web.aimglobal.org/news/newsarticledisplay.aspx?ArticleID=111>; the
  current licensed product is store item 805 at
  <https://web.aimglobal.org/external/wcpages/wcecommerce/eComItemDetailsPage.aspx?ItemID=805>.

## Reference implementations

The saved README and license files describe feature scope and licensing. They are not copied into `src/` and do not authorize copying implementation code without preserving the applicable license.

- ZXing, commit `ba9e91fb31206f726ceb7694fa562c94d30f9ce9`: <https://github.com/zxing/zxing>.
- ZBar, commit `2ea2ca585b10a07e528af08ddf8890756e059b5b`: <https://github.com/mchehab/zbar>.
- quirc, commit `927d680904dc95fdff4cd9d022eb374b438ff8f2`: <https://github.com/dlbeer/quirc>.
- libqrencode, commit `715e29fd4cd71b6e452ae0f4e36d917b43122ce8`: <https://github.com/fukuchi/libqrencode>.
- Project Nayuki QR Code generator, commit `993ccbb6765327f5c49939ba77736098c04ab81d`: <https://github.com/nayuki/QR-Code-generator>.
- Segno, commit `b11dc2913c22b22b3bc0a6efaa989904c44977bf`: <https://github.com/heuer/segno>.
- Zint, commit `baa91878f488f472fee78585184ffcee07c1dd51`: <https://github.com/zint/zint>.

## Integrity

Run `shasum -a 256 -c specs/SHA256SUMS` from the repository root before
deriving constant tables or test vectors. The manifest covers acquired source
artifacts; the project-authored Markdown control files are reviewed through
version control instead.
