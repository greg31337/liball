# Maintenance Brief

Maintain the dependency-free configuration loading and merging library described
in `README.md` and tracked in `todo.md`.

Production code must follow `syntax.md`. Treat `public` as an explicit API
decision: consumer-facing classes, methods, fields, enums, and enum members may
be public, while parser, loader, merge, conversion, binding, resolution, and
other implementation mechanics remain internal. Document every public symbol
with the permitted XML documentation constructs and do not add XML documentation
to internal implementation symbols.

Use only built-in .NET runtime APIs. Keep source precedence deterministic and
run the full build, test suite, syntax-policy audit, and public-documentation
audit after changes.
