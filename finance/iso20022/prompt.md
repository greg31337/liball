create detailed development plan for universal ISO 20022 financial-message model, parsing, generation, validation, and repository-tooling library based on the ISO 20022 series and official ISO 20022 repository artifacts., and put it in todo.md
create complete list of features,including  message-definition metadata, business components, data types, XML message parsing/writing, XSD validation, generated strongly typed models, namespace/version handling, message identifiers, semantic validation hooks, code sets, supplementary data, repository import, schema/code generation, version comparison, migration helpers, streaming processing, from low level to high
research existing libraries, like  Prowide ISO 20022, iso20022.js, moov-io/iso20022, IBM Financial Transaction Manager ISO 20022 tooling, to figure out the set of complete features
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
