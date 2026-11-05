# Conformance vectors

This directory contains externally sourced AVIF and AV1 bitstreams used by the conformance tests.
Every imported file is pinned by SHA-256 in `manifest.json`; updates must change the upstream commit,
hash, size, provenance, and expected properties together.

## AOMedia AVIF / Link-U

The two `.avif` files are unmodified copies from the `testFiles/Link-U` directory of the Alliance for
Open Media `av1-avif` specification repository at commit
`bf4c18d1f3971069b75e87d6ee469790589f4f09`.

The `.obu` file is the byte-exact primary `av01` item payload extracted from
`fox.profile0.8bpc.yuv420.monochrome.avif`. It was not decoded, re-encoded, or otherwise changed. The
extraction separates the existing coded item from its ISO BMFF container so the AV1 low-overhead OBU
reader can consume it directly.

The upstream README identifies the Fox Parade creator as Kaede Fujisaki (`@ledyba`) and the vector
publisher as Link-U. The original image was retrieved from the creator's website. The AVIF files and
the extracted coded payload are distributed under Creative Commons Attribution-ShareAlike 4.0
International. The complete license is retained at `licenses/CC-BY-SA-4.0.txt`.

Upstream sources:

- <https://github.com/AOMediaCodec/av1-avif>
- <https://github.com/AOMediaCodec/av1-avif/tree/bf4c18d1f3971069b75e87d6ee469790589f4f09/testFiles/Link-U>
- <https://github.com/link-u/avif-sample-images>

No vector is downloaded while tests run. This keeps the suite deterministic and makes licensing and
source review possible without network access.
