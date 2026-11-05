create high level development plan for universal CLI argument parsing and handling library, and put it in todo.md
create a list of necessary features, from low level to high
research existing libraries, like Commander.js, picocli, Click, Cobra, System.CommandLine, and others, to figure out a set of critical must have features
list all features in todo.md
list proposed APIs in todo.md, without fine details
list planned source files in todo.md
list tasks that would implement the features in todo.md, in line format: "- [OPEN] T001: Task description."
take into account simplified C# syntax to be used for implementation described in syntax.md
do not use any external libraries, only built-in .NET ones
treat `public` declarations as the supported API layer and keep implementation-only declarations `internal`
require XML documentation on every public type, constructor, method, field, enum, and enum member, using only the XML constructs allowed by syntax.md
