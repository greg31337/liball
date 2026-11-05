create detailed development plan for universal One-dimensional and two-dimensional barcode encoding, decoding, validation, and image-detection library based on ISO/IEC 15417, ISO/IEC 15420, ISO/IEC 16388, ISO/IEC 16022, ISO/IEC 15438, and GS1 barcode specifications., and put it in todo.md
create complete list of features,including  Code 128, GS1-128, EAN-8/EAN-13, UPC-A/UPC-E, Code 39, ITF/ITF-14, Codabar, Data Matrix, PDF417, checksum calculation, application identifiers, quiet zones, sizing rules, encoder layout, raster/vector rendering, scanline decoding, image localization, rotation/skew handling, error correction, validation and human-readable text, from low level to high
research existing libraries, like  ZXing, ZBar, zint, Dynamsoft Barcode Reader, Barcode4J, bwip-js, ZXing.Net, to figure out the set of complete features
list all features in todo.md
take into account simplified C# syntax to be used for implementation described in syntax.md
list proposed APIs (without fine details) and planned source files in todo.md, following public API organization rules from syntax.md: data model under src/Api/Data, operations under src/Api/Contracts, static data under src/Api/Static, enums under src/Api/Enums, exceptions under src/Api/Exceptions, public implementation classes under src/Api/Classes
list tasks that would implement the features in todo.md, in line format: "- [OPEN] T001: Task description."
do not use any external libraries under src/, only built-in .NET ones; usage of Span types is recommended if it's appropriate
usage of external libraries under tests/ is allowed
include tasks that would create complete test coverage of the functionality exposed by the src/Api layer
do not create tasks for syntax policy or source checking tests
do not create tasks for performance / benchmark tests
do not create documentation or release related tasks
download any relevant specification documents, RFCs, descriptions, and other relevant resources, and put them under specs/
