create detailed development plan for universal HTML parsing, sanitization, conversion to text or markdown library, and put it in todo.md
create complete list of features, from low level to high
research existing libraries, like jsoup, AngleSharp, Html Agility Pack, Beautiful Soup, DOMPurify, and others, to figure out the set of complete features
list all features in todo.md
list the public API design baseline in todo.md, without duplicating the exact signatures and XML documentation that define the current API in src/
list planned source files in todo.md
list tasks that would implement the features in todo.md, in line format: "- [OPEN] T001: Task description."
take into account simplified C# syntax to be used for implementation described in syntax.md
keep the shipped `src/` library dependency-free and use only built-in .NET runtime APIs there; test and repository tooling dependencies do not become package runtime dependencies. Use span types when they improve bounded scanning without retaining caller-owned storage
