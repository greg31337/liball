# Nikon HE/HE* codec implementation gate

Status: **decoder and encoder not cleared for implementation**  
Reviewed: 2026-08-20

This is an engineering and release-risk decision for this repository, not a
legal opinion. It is sufficient to keep the project on the conservative path
of not implementing the codec. It is not sufficient to clear an implementation;
that would require qualified patent and licensing counsel for the intended
distribution territories and business model.

## Decision

Do not schedule or implement an in-tree Nikon High Efficiency or High
Efficiency Star decoder or encoder. Keep support at recognition, inspection,
metadata/preview extraction, stable unsupported-codec diagnostics, and opaque
payload preservation.

The gate fails for two independent reasons:

1. No reviewed public source establishes the exact Nikon still-image HE/HE*
   codestream syntax, profile, parameter mapping, or NEF embedding rules needed
   for a conforming independent decoder or encoder.
2. The identified technology is expressly patent-covered, and this repository
   has no patent or SDK license reviewed as covering a dependency-free,
   redistributable .NET implementation.

The later publication of JPEG XS RAW material does not by itself change this
decision. A similarity in technology, features, or ownership is not proof that
a Nikon HE/HE* payload conforms to a particular ISO/IEC 21122 edition and
profile.

## Reviewed facts and sources

| Finding | Primary source | Consequence |
|---|---|---|
| Nikon exposes two HE modes in addition to lossless compression and describes their relative file size/quality, but does not publish a codestream. | [Nikon Z 9 RAW recording guide](https://onlinemanual.nikonimglib.com/z9/en/psm_raw_recording_94.html) | Product behavior is documented; the bitstream is not. |
| Nikon states that the Z 9 high-efficiency RAW compression uses intoPIX technology. | [Nikon Z 9 product features](https://nij.nikon.com/products/lineup/mirrorless/z9/features04.html) | The codec cannot be treated as an unencumbered Nikon-only TIFF variation. |
| intoPIX identifies the Z 9 integration as TicoRAW, says it applies to still and movie RAW, and calls the technology patented. | [intoPIX Z 9 integration announcement](https://ja.intopix.com/blogs/post/TicoRAW-technology-added-with-High-Efficiency-RAW-recording-of-Nikon-Z-9-flagship-mirrorless-camera) | TicoRAW patent and licensing questions apply to any proposed implementation. |
| intoPIX says TicoRAW and FastTicoRAW are covered by issued or pending patents and offers encoder/decoder SDKs and IP cores. | [intoPIX patents and trademarks](https://www.intopix.com/patents-trademark), [TicoRAW product page](https://www.intopix.com/tico-raw) | Public product descriptions are not a patent license or an implementation specification. |
| A JPEG XS patent pool covers declared essential patents owned by its licensors, including intoPIX; licensing models include software, products, processing instances, and services. | [Fraunhofer/intoPIX/Vectis pool announcement](https://www.iis.fraunhofer.de/en/pr/2022/20220301_jpeg_xs.html), [current pool patent list](https://www.jpegxspool.com/patent-list) | Independent code can still require patent permission; open-source authorship does not resolve that issue. |
| ISO/IEC 21122 now specifies JPEG XS and supports raw Bayer data, with normative codestream, profiles, and conformance parts. | [JPEG XS overview](https://jpeg.org/jpegxs/), [ISO/IEC 21122-1:2024 catalogue entry](https://webstore.iec.ch/en/publication/98775) | JPEG XS is a possible future specification basis only after byte-level conformance of Nikon HE/HE* is established. |
| The published JPEG XS reference software is labelled for evaluation only under a custom license. | [JPEG XS reference software page](https://jpeg.org/jpegxs/software.html) | It is not an approved source for copying or adapting production code in this repository. |
| Nikon offers a NEF/NRW Image SDK subject to the license presented at download time rather than a public bitstream specification. | [Nikon SDK FAQ](https://sdk.nikonimaging.com/information/en/) | The SDK may be used only after a separate agreement review; it is not a clean-room specification and cannot become a package dependency under the current v1 contract. |

The review found no Nikon document that assigns HE/HE* to an ISO/IEC 21122
edition/profile, no public Nikon HE/HE* conformance vectors, and no license in
this repository granting the relevant codec patent rights. Therefore the
project must not infer a specification match from the facts that intoPIX
supplies both TicoRAW and JPEG XS technology.

## Copyright, contract, and reverse-engineering boundary

Public laws contain limited interoperability provisions, but they are not a
general implementation clearance. In the United States, 17 U.S.C. §1201(f)
applies under stated conditions to circumvention for interoperability of an
independently created computer program. In the European Union, Articles 5 and
6 of Directive 2009/24/EC impose conditions on observation, study, testing,
and decompilation for interoperability. See the [official U.S. Code text](https://uscode.house.gov/view.xhtml?edition=prelim&num=0&req=granuleid%3AUSC-prelim-title17-section1201)
and the [official EU directive](https://eur-lex.europa.eu/legal-content/en/TXT/?uri=CELEX%3A32009L0024).

Those provisions are jurisdiction-specific, fact-dependent, and do not grant
a patent license. They also do not override every contract, trade-secret,
sample-redistribution, trademark, or other applicable-law issue. Consequently:

- do not accept or use a Nikon or intoPIX SDK agreement for implementation
  research without recording and reviewing its exact terms;
- do not decompile, disassemble, trace, or copy SDK/plugin binaries for this
  project without a counsel-approved clean-room plan;
- do not copy source, tables, constants, or explanatory text from reference
  implementations whose license is incompatible with this package;
- do not claim TicoRAW or JPEG XS conformance based only on successful decoding
  of samples; and
- keep acquired HE/HE* files private unless their redistribution rights and
  provenance are recorded.

## Repository-permitted work while the gate is closed

The following work does not interpret or generate compressed sensor samples
and remains within the repository's current policy:

- recognize HE and HE* from consistent TIFF and Nikon maker-note evidence;
- expose container structure, metadata, thumbnails, and previews;
- identify raw payload ranges and extract or preserve their original bytes;
- report `Recognized=true`, `Inspected=true`, and `Decoded=false`;
- return a stable `UnsupportedCodec` diagnostic/exception; and
- add structural and preservation tests that never assert guessed pixels.

The following work is not scheduled:

- entropy, transform, prediction, rate-control, reconstruction, or proxy decode;
- any HE/HE* encoder, including experimental or opt-in encoding;
- conversion of Nikon HE/HE* payloads through bundled proprietary binaries;
- a write profile that fabricates or rewrites the HE/HE* codestream; and
- a claim of mathematical, visual, Nikon, TicoRAW, or JPEG XS conformance.

Byte-exact relocation of an opaque payload may be considered later under the
normal preserving-rewrite rules, provided offsets, checksums, authentication,
maker-note references, and application compatibility are independently proven.

## Requirements to reopen the gate

All of the following evidence is required before decoder work is scheduled:

1. Identify the exact normative codestream and profile for each Nikon HE and
   HE* variant, including edition/version and every Nikon-specific parameter or
   wrapper rule.
2. Prove the identification with redistributable conformance vectors or
   privately hashed samples and at least one authorized independent decoder;
   sample similarity is insufficient.
3. Obtain a written patent analysis for the intended package license,
   territories, distribution model, decoder/encoder distinction, and users.
4. If a license is required, obtain terms that expressly cover the library and
   downstream redistribution; record the agreement owner and renewal/expiry
   obligations outside the public repository.
5. Purchase or otherwise lawfully obtain the normative specifications and
   record their copyright/use constraints without committing restricted text.
6. Approve a clean-room implementation plan that separates restricted
   materials and incompatible code from production authors and provenance.
7. Define corruption, resource-limit, conformance, pixel/error-bound, metadata,
   preview, and opaque-preservation tests before enabling a profile.

Encoder work has the same requirements plus independently confirmed Nikon NEF
container rules, rate/quality signaling, application compatibility, and an
explicit answer on whether encoding carries different royalties or field-of-use
restrictions.

## Re-review triggers

Reopen this decision only when Nikon or intoPIX publishes an exact HE/HE*
specification/profile statement, a suitable patent license is obtained, counsel
provides written clearance, or a material patent/specification status change is
documented. A new camera, an application gaining decode support, a reverse-
engineered implementation, or a patent expiring in only one jurisdiction is not
by itself sufficient.
