# Maintenance checklist

Use `syntax.md` as the authoritative language policy for every change under
`src/`.

- Treat each `public` class, enum, constructor, method, and field as supported
  API. Make implementation-only symbols explicitly `internal`.
- Use only the `public`, `internal`, `static`, and `const` modifiers.
- Document every public type, constructor, method, field, enum, and enum member
  in plain English. Explain observable behavior, units, ordering, ownership,
  validation, and side effects when they matter to a caller.
- Restrict XML documentation to `summary`, `remarks`, `param`, `returns`,
  `exception`, `example`, `see`, `seealso`, `para`, and `c` elements.
- Update tests and Markdown whenever public behavior or syntax policy changes,
  then run the complete solution test suite.
