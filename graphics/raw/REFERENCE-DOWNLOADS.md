# Reference downloads

The references below are linked instead of redistributed with this project because their publication notices or publisher terms restrict reproduction. Download them for local reference under the publisher's terms; a free download does not grant permission to republish it.

Paths are relative to this project. The top-level `.gitignore` excludes these local copies. Some links open a publisher page where the edition must be selected or access terms accepted.

| Local reference path | Short description | Download or publisher page |
| --- | --- | --- |
| `specs/dng-1.7.1.0.pdf` | Adobe Digital Negative 1.7.1.0 tags, profiles, and image encoding. | [Source](https://helpx.adobe.com/content/dam/help/en/camera-raw/digital-negative/jcr_content/root/content/flex/items/position/position-par/download_section_733958301/download-1/DNG_Spec_1_7_1_0.pdf) |
| `specs/exif-3.1-2026.pdf` | CIPA Exif 3.1 image metadata and tag definitions. | [Source](https://www.cipa.jp/e/std/std-sec.html) |
| `specs/exif-metadata-for-xmp-2026.pdf` | CIPA Exif-to-XMP metadata mapping and reconciliation. | [Source](https://www.cipa.jp/e/std/std-sec.html) |
| `specs/itu-t-t81-jpeg.pdf` | ITU-T T.81: JPEG coding processes and interchange syntax. | [Source](https://www.w3.org/Graphics/JPEG/itu-t81.pdf) |
| `specs/tiff-pagemaker-notes.pdf` | Adobe PageMaker TIFF technical notes and interoperability guidance. | [Source](https://helpx.adobe.com/content/dam/help/en/camera-raw/digital-negative/jcr_content/root/content/flex/items/position/position-par/download_section_1690056821/download-3/TIFFPM6.pdf) |
| `specs/xmp-part-1.pdf` | Adobe XMP Part 1: data model, serialization, and core properties. | [Source](https://raw.githubusercontent.com/adobe/XMP-Toolkit-SDK/main/docs/XMPSpecificationPart1.pdf) |
| `specs/xmp-part-2.pdf` | Adobe XMP Part 2: additional property schemas. | [Source](https://raw.githubusercontent.com/adobe/XMP-Toolkit-SDK/main/docs/XMPSpecificationPart2.pdf) |
| `specs/xmp-part-3.pdf` | Adobe XMP Part 3: packet storage and embedding in files. | [Source](https://raw.githubusercontent.com/adobe/XMP-Toolkit-SDK/main/docs/XMPSpecificationPart3.pdf) |

## Large RAW corpus fixtures

The following six files are omitted because each exceeds 20 MB (20,000,000
bytes). Their removal is a size decision, separate from the reference publication
restrictions above. The top-level `.gitignore` excludes local restored copies.

The download URLs, byte lengths, and SHA-256 hashes are retained in
[`corpus/manifest.json`](corpus/manifest.json). Download the three camera samples
to the exact paths below and verify their lengths and `hashes.fileSha256` values
against that manifest before using them.

| Local path | Size (MB) | Description and restoration source |
| --- | ---: | --- |
| `corpus/files/canon-eos-6d-mark-ii-raw-3x2-1.0.2.cr2` | 37.89 | Canon EOS 6D Mark II full RAW camera sample. [Recorded download](https://raw.pixls.us/getfile.php/1625/nice/Canon%20-%20EOS%206D%20Mark%20II%20-%20RAW%20%283%3A2%29.CR2). |
| `corpus/files/canon-eos-6d-mark-ii-mraw-3x2-1.0.2.cr2` | 28.97 | Canon EOS 6D Mark II medium RAW camera sample. [Recorded download](https://raw.pixls.us/getfile.php/1624/nice/Canon%20-%20EOS%206D%20Mark%20II%20-%20sRAW1%20%28mRAW%29%20%283%3A2%29.CR2). |
| `corpus/files/canon-eos-6d-mark-ii-sraw-3x2-1.0.2.cr2` | 20.56 | Canon EOS 6D Mark II small RAW camera sample. [Recorded download](https://raw.pixls.us/getfile.php/1623/nice/Canon%20-%20EOS%206D%20Mark%20II%20-%20sRAW2%20%28sRAW%29%20%283%3A2%29.CR2). |
| `corpus/generated/canon-eos-6d-mark-ii-raw-3x2-write-golden.cr2` | 28.52 | Synthetic Canon writer output; reproduce with the corpus generator below. |
| `corpus/generated/nikon-z-7-uncompressed-14bit-fx-write-golden.nef` | 79.54 | Synthetic Nikon Z 7 uncompressed writer output; reproduce with the corpus generator below. |
| `corpus/generated/nikon-z-8-lossless-14bit-fx-write-golden.nef` | 51.67 | Synthetic Nikon Z 8 lossless writer output; reproduce with the corpus generator below. |

After restoring the full RAW Canon source, run this command from the repository
root to regenerate all three writer outputs:

```sh
dotnet run --project graphics/raw/corpus/generator/raw.CorpusGenerator.csproj --configuration Release
```

The generator reuses the Canon source's JPEG preview and prints verification
hashes. These six fixtures must be restored before running corpus tests that
depend on them. The manifest and coverage records retain their original
acquisition and validation evidence; their historical `repository` availability
labels do not imply that these oversized files are still bundled.
