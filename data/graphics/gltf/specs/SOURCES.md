# Downloaded reference sources

Retrieved 2026-08-27. These files are implementation inputs and test-planning references, not runtime dependencies. No file under `src/` may depend on third-party code.

## Normative format and extension material

- `khronos-gltf/glTF-2.0.1.pdf`: Khronos glTF 2.0.1 registry PDF, <https://registry.khronos.org/glTF/specs/2.0/glTF-2.0.pdf>.
- `khronos-gltf/specification-2.0/`: glTF 2.0 AsciiDoc specification, normative JSON Schemas, and figures from KhronosGroup/glTF commit `fdb8ce0e2e0b7ecf3466f8dacb9f1385257b8276`, <https://github.com/KhronosGroup/glTF>.
- `khronos-gltf/extensions/`: the glTF 2.0 extension registry, ratified and in-progress Khronos specifications, multi-vendor and vendor specifications, archived extension specifications, schemas, and explanatory figures from the same commit. The unrelated 0.8 and 1.0 extension trees were not retained.
- `khronos-gltf/LICENSE.adoc` and `khronos-gltf/COPYING.adoc`: licensing terms accompanying the Khronos material.
- `khronos-gltf-external-reference/`: glTF External References proposal, explainer, and licensing terms from KhronosGroup/glTF-External-Reference commit `190ea4ecce4baa630ac0259c8b664d2148436190`, <https://github.com/KhronosGroup/glTF-External-Reference>.

## Internet and JSON standards

- `rfcs/rfc2397-data-url.txt`: RFC 2397, The `data` URL scheme, <https://www.rfc-editor.org/rfc/rfc2397>.
- `rfcs/rfc3986-uri.txt`: RFC 3986, URI generic syntax and reference resolution, <https://www.rfc-editor.org/rfc/rfc3986>.
- `rfcs/rfc4648-base-encodings.txt`: RFC 4648, Base64 and related encodings, <https://www.rfc-editor.org/rfc/rfc4648>.
- `rfcs/rfc6901-json-pointer.txt`: RFC 6901, JSON Pointer, <https://www.rfc-editor.org/rfc/rfc6901>.
- `rfcs/rfc8259-json.txt`: RFC 8259, JSON data interchange syntax, <https://www.rfc-editor.org/rfc/rfc8259>.
- `rfcs/rfc8785-json-canonicalization.txt`: RFC 8785, JSON Canonicalization Scheme, used as design input for deterministic output, <https://www.rfc-editor.org/rfc/rfc8785>.
- `rfcs/rfc8089-file-uri.txt`: RFC 8089, the `file` URI scheme, <https://www.rfc-editor.org/rfc/rfc8089>.
- `json-schema-draft-04/core.html` and `json-schema-draft-04/validation.html`: JSON Schema draft-04 core and validation specifications, matching the dialect used by the glTF core schemas, <https://json-schema.org/specification-links#draft-4>.
- `image-formats/KTX-2.0.html`: Khronos KTX 2.0 container specification referenced by `KHR_texture_basisu`, <https://registry.khronos.org/KTX/specs/2.0/ktxspec.v2.html>.
- `image-formats/PNG-3.html`: W3C Portable Network Graphics third edition specification, <https://www.w3.org/TR/png-3/>.

## Library feature references

- `reference-libraries/cgltf-README.md`: cgltf core/extension/accessor/writer feature description at commit `85cd62382dfea638278962690cf515023f33ed00`, <https://github.com/jkuhlmann/cgltf>.
- `reference-libraries/tinygltf-README.md`: TinyGLTF container, URI, sparse-accessor, callback, image, and extension feature description at commit `0d9ce1a9561688c6c6e7471430d15a50a0b15f1a`, <https://github.com/syoyo/tinygltf>.
- `reference-libraries/SharpGLTF-README.md`: SharpGLTF's low-level, runtime, and toolkit layering description at commit `0a00735b32a6f3be262037c9f82a63011ca840bf`, <https://github.com/vpenades/SharpGLTF>.
- `reference-libraries/glTF-Transform-README.md`: glTF-Transform's read/edit/write, graph management, extension, and composable transform description at commit `e9feb829f071f6febfb68707ffc3146502325b34`, <https://github.com/donmccurdy/glTF-Transform>.
- `reference-libraries/threejs-GLTFLoader.html`: three.js GLTFLoader loader and supported-extension reference, <https://threejs.org/docs/pages/GLTFLoader.html>.
- `reference-libraries/glTF-Validator-README.md`: Khronos validator validation-surface description at commit `434283be08a668a8fb4e437145630ddbf93b0686`, <https://github.com/KhronosGroup/glTF-Validator>.

## Conformance and fixture references

- `testing-references/glTF-Validator-ISSUES.md`: complete Khronos validator issue catalog from commit `434283be08a668a8fb4e437145630ddbf93b0686`.
- `testing-references/glTF-Asset-Generator-README.md`: generated conformance-asset catalog description from commit `3d99767e9a67fbfe109f0d298c1e8d909bcac9db`, <https://github.com/KhronosGroup/glTF-Asset-Generator>.
- `testing-references/glTF-Sample-Assets-README.md`: Khronos sample-asset corpus description from commit `2d97dcc2463db123ed5203598cffedf8b6cf1683`, <https://github.com/KhronosGroup/glTF-Sample-Assets>.
