create detailed development plan for universal Session Description Protocol parsing, validation, and generation library, based on RFC8866, RFC8122, RFC8839, and put it in todo.md
create complete list of features,including  session/media descriptions, attributes, codecs, connection data, timing, ICE/DTLS attributes, extmaps, bundle/group helpers, from low level to high
research existing libraries, like  libsdptransform, sdp-transform, PJSIP SDP, GStreamer SDP, to figure out the set of complete features
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
