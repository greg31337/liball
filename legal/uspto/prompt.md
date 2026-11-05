create detailed development plan for universal USPTO patent and trademark data access library for querying and downloading Open Data Portal APIs and bulk datasets, including Patent File Wrapper, PTAB, Office Action, assignment, citation, and PatentsView-derived data., and put it in todo.md
create complete list of features,including  ODP authentication and API-key handling, patent/application/publication identifier normalization, patent application search, file-wrapper metadata and transaction history, document listing and download, continuity and priority data, assignments, attorneys, patent-term adjustment, petitions, PTAB trials and appeals, Office Action text/rejections/citations, bulk-product catalog search, resumable bulk downloads, checksums, decompression, XML/JSON parsing, pagination, rate-limit handling, retries, caching, local mirroring, incremental updates and dataset manifests, from low level to high
research existing libraries, like  patent-dev/uspto-odp, pyUSPTO, KennethThompson/uspto_odp, uspto-opendata-python, PatentsView, to figure out the set of complete features
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
