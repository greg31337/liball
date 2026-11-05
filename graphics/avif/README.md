# avif

`avif` is a planned self-contained, managed .NET 10 library for inspecting, validating, decoding,
rendering, creating, and rewriting AVIF files. Production code uses only the .NET runtime and base
class libraries; native codecs and third-party production dependencies are outside the design.

The current implementation claim, supported-feature matrix, limitations, security guidance, and
versioning policy are documented in [Release status and policy](docs/release-status.md). The broader
frozen standards baseline, vocabulary, conformance targets, and protected-media boundary are in
[Standards and conformance](docs/conformance.md). The local source inventory and provenance are
recorded in [Development references](specs/README.md), and the ordered implementation backlog is in
[todo.md](todo.md).

See the [usage guide](docs/usage.md) for end-to-end examples covering still images, Exif and XMP
metadata, grids, HDR signaling, progressive images, and timed image sequences. Public API XML
documentation is emitted with the library build.

The repository is currently under development. In particular, the public codec currently exchanges
a private managed payload rather than interoperable normative AV1 frame data. Read the release-status
limitations before using generated files outside this library. A feature described by the broader
conformance target is not available merely because its backlog task is marked `DONE`.
