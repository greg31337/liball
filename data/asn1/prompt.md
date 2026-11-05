create detailed development plan for universal ASN.1 schema compiler and portable runtime supporting the ITU-T X.680-X.683 language specifications and X.690-X.696 encoding-rule families., and put it in todo.md
create complete list of features,including  ASN.1 lexer/parser, modules and imports, tagging, constraints, parameterization, information object classes, open types, schema validation, intermediate representation, code generation, reflection metadata, BER, DER, CER, PER/UPER/APER, OER/COER, XER, streaming encode/decode, incremental parsing, unknown-extension preservation, canonical encoding checks, from low level to high
research existing libraries, like  asn1c, OSS Nokalva ASN.1 Tools, Objective Systems ASN1C, asn1tools, pyasn1, Bouncy Castle ASN.1, asn1scc, to figure out the set of complete features
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
