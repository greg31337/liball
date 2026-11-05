create detailed development plan for universal GIF reading / writing library, and put it in todo.md
create complete list of features, from low level to high
research existing libraries to figure out the set of complete features
list all features in todo.md
list proposed APIs in todo.md, without fine details
list planned source files in todo.md
list tasks that would implement the features in todo.md, in line format: "- [OPEN] T001: Task description."
take into account simplified C# syntax to be used for implementation described in syntax.md
do not use any external libraries, only built-in .NET ones; usage of Span types is recommended if it's appropriate
reserve `public` for the high-level `Gif` facade and the data, option, metadata, diagnostic, statistics, and enum symbols it exposes; mark implementation symbols `internal`
add XML documentation to every public type, constructor, method, field, enum, and enum member, using only the constructs allowed by syntax.md
