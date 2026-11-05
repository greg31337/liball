# Contributing

UniversalHtml welcomes focused changes that follow the ordered work in [`todo.md`](todo.md). Before editing, read [`syntax.md`](syntax.md); it is authoritative and must not be modified as part of an implementation task.

## Workflow

1. Select the first applicable OPEN task unless maintainers have agreed to another scope.
2. Keep production changes within that task and preserve unrelated worktree changes.
3. Add behavioral tests under `tests/` for changed code under `src/`. Do not add tests solely for documentation, project-file contents, or syntax-policy conformance.
4. Use pinned local inputs for generated standards data. Normal builds must remain offline.
5. Run `dotnet test html.slnx --configuration Release` and, when generated files are involved, `dotnet run --project tools/GenerateTables -- --check`.
6. Mark a task DONE only after its required behavior and tests succeed.

## Production C# checklist

The following rules apply to every `src/**/*.cs` file, including generated source.

### File and declaration forms

- A compilation unit may contain `using` directives, exactly one file-scoped namespace, classes, interfaces, and enums.
- A class may contain fields, constructors, and ordinary block-bodied methods. A class may implement interfaces. Only an exception class may otherwise have a base list, and it must derive directly from `System.Exception`.
- An interface may contain instance methods only. It has no fields, default implementations, static abstract members, generic methods, or interface inheritance. Its name starts with `I` followed by an uppercase letter.
- Enum members may have explicit constant values.
- Allowed types are predefined, named, qualified, `global::`-qualified, generic reference types supplied by .NET, and arrays. Reference types may carry nullable annotations. Span types are allowed where their lifetime is appropriate.
- Library code must not declare a generic class or method, but it may use framework generic types such as `List<string>` and `Dictionary<string, string>`.
- Fields, local variables, and enum members may have initializers.
- Parameters must be plain value parameters in an ordinary parenthesized list.
- The only modifiers are `public`, `internal`, `static`, and `const`. Every production type, constructor, method, and field declares either `public` or `internal` visibility explicitly. `public` is reserved for the supported package API; implementation declarations are `internal`.
- Keep constructors and factories for result objects, compiled configurations, parser-produced tokens, diagnostics, exceptions, inspection-only rule values, and other library-produced values `internal`. A public return or exception type does not imply that callers should be able to manufacture it. Expose public construction only for supported caller-authored inputs such as options, policies, fragment contexts, and DOM values.
- When named public factories represent every supported construction state, keep the duplicate constructor `internal`; the factories are the canonical public construction path.
- Keep defensive-copy helpers on publicly immutable values `internal`; callers need public `Copy()` only when the returned type exposes supported mutations.
- Keep same-value aliases introduced only to make internal call sites more descriptive `internal`; public members must add a distinct supported operation or observation.
- Keep compiled snapshots `internal` when no public operation accepts them; a public compile method must produce a configuration that callers can actually reuse in a supported workflow.
- Keep subsystem stages that are unsafe or incomplete when called alone `internal`. In particular, individual sanitizer element, node, attribute, URL, CSS, nested-content, template-marker, and clobbering helpers are implementation details behind the complete sanitizer API.

Do not use block-scoped namespaces, global statements, structs, records, delegates, extension blocks, attributes, or preprocessor directives. Do not use primary constructors, library-defined generic declarations, generic constraints, or an explicit base-constructor initializer except on a directly derived exception class.

Every public type, constructor, method, field, enum, and enum member requires XML documentation. Documentation may use only `<summary>`, `<remarks>`, `<param>`, `<returns>`, `<exception>`, `<example>`, `<see>`, `<seealso>`, `<para>`, and `<c>`; keep implementation-only `internal` declarations free of XML documentation.

Class inheritance is forbidden outside those exception classes. Do not use `override`; use a narrow interface when substitutable behavior is a supported boundary.

### Type and parameter forms

Nullable reference annotations such as `string?`, `MyType?`, and `List<T>?` are allowed. Nullable value-type forms such as `int?`, generic `T?`, `Nullable<T>`, and `System.Nullable<T>` remain forbidden, as do the null-forgiving operator, nullable directives, pointer, function-pointer, tuple, `ref`, and `scoped` type forms.

Do not use `ref`, `out`, `in`, `params`, default parameter values, or function-pointer parameters. Expected negative results use concrete result classes, and overloads replace default parameter values.

### Members

Do not declare properties, indexers, events, operators, conversion operators, destructors, local functions, or incomplete members. Expose state through ordinary methods such as `Get...`, `Set...`, `Add...`, and `Remove...`.

### Statements

Allowed statements are blocks, empty statements, local declarations, expression statements, `if`/`else`, constant/default `switch`, loops, `foreach`, `break`, `continue`, `return`, `throw`, `using`, `checked`, and `lock`.

Do not use `try`, `catch`, `finally`, `goto`, labels, `fixed`, `unsafe`, iterator `yield`, or a deconstructing `foreach`. Resource ownership must be structured so cleanup does not require a forbidden `finally` in production code.

### Expressions and calls

Allowed expressions are literals, `this`, parentheses, ordinary unary and binary operators, assignments, conditional expressions, casts, `default(T)`, checked expressions, throw expressions, ordinary calls and member access, conditional member access, element access, lambdas, object and array creation, initializers, collection expressions without spread, and interpolated strings.

Invocation arguments must be positional. Named arguments and conditional element access are forbidden.

Do not use patterns, pattern-oriented binary forms, switch expressions, tuple expressions, anonymous methods, anonymous objects, `await`, `ref` expressions, `typeof`, `sizeof`, unsafe expressions, stack allocation, ranges, spread elements, `with` expressions, typed references, or `base` member access. Pointer and address-of unary forms are forbidden. LINQ is forbidden; use explicit loops.

## Design patterns required by the subset

- Represent related mutable state with concrete classes and fields rather than records, structs, or class inheritance. Use interfaces only for narrow behavioral boundaries, not as state containers.
- Use ordinary getter and mutation methods instead of properties, indexers, and events.
- Return concrete arrays as snapshots. Return an empty array rather than `null` for an empty collection.
- A public method that may return `null` uses a nullable reference return type and must include the exact sentence `Returns null when ...` in its external API documentation.
- A `Try...` API returns a concrete result object with success and value methods; it never uses `out`, tuples, or exceptions for an expected miss.
- Keep spans within a method call or temporary view. Never retain caller spans in objects.
- Use explicit loops and state machines in place of LINQ, iterators, async methods, or pattern matching.
- Reserve exceptions for invalid arguments, programmer errors, invariant failures, and unrecoverable exhaustion. Report malformed input through bounded diagnostics.

## Test C# policy

Files under `tests/**/*.cs` may use any valid C# syntax except nullable value-type forms such as `int?`, generic `T?`, `Nullable<T>`, or `System.Nullable<T>`, the null-forgiving operator, and nullable directives. Parse errors are never permitted. Test compilation enables nullable annotations without flow warnings because negative tests intentionally pass null to non-null contracts and the syntax policy forbids null-forgiving suppressions.

Tests should assert observable behavior, boundary conditions, failure atomicity, deterministic results, limits, and cancellation as appropriate. A generator freshness test may protect generated `src/` behavior, but documentation and project-file wording should not be frozen by unit tests.

## Generated data

Edit pinned files under `tools/GenerateTables/inputs/`, not the generated `src/Core` file. Update the input revision and SHA-256 in the manifest, run the generator in `--write` mode, inspect the generated diff, and then run both the full suite and `--check`. Generator code is tooling and is not shipped, but its generated C# must satisfy every production rule above.

## Documentation

Public XML documentation must explain behavior in plain English, including important ownership, snapshot, null, limit, cancellation, mutation, and security details an ordinary caller needs. Keep architecture boundaries and non-goals in [`docs/architecture.md`](docs/architecture.md), conformance versions in [`docs/supported-standards.md`](docs/supported-standards.md), and diagnostic compatibility rules in [`docs/diagnostics.md`](docs/diagnostics.md). Do not claim standards coverage or performance that has not been measured by committed fixtures or benchmarks.
