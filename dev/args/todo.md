# Universal CLI Argument Parsing Library Plan

## Goals

- Build a small, universal command-line argument parsing and handling library for .NET.
- Use only built-in .NET APIs. Do not depend on external libraries, source generators, reflection packages, or console rendering packages.
- Keep implementation compatible with the simplified C# subset in `syntax.md`.
- Support both low-level parsing use cases and higher-level command application use cases.
- Make behavior deterministic, testable, and suitable for native AOT-style applications.

## Research Notes

Established libraries converge on the same critical capabilities:

- Commander.js emphasizes fluent command and option definitions, required options, defaults, variadic options, custom option processing, choices, hidden options, conflicts, implied options, version output, generated help, and help subcommands: https://www.npmjs.com/package/commander
- picocli emphasizes strongly typed options and positional parameters, arity, default and fallback values, required parameters, option groups, subcommands, help/version short-circuiting, configurable parser behavior, POSIX/GNU/MS-DOS styles, unmatched input handling, ANSI-aware help, and shell completion: https://picocli.info/
- Click emphasizes clear separation between options and arguments, type conversion, defaults, required values, repeated and multi-value options, short option stacking, environment variable value sources, prompts, callbacks, help generation, and shell completion: https://click.palletsprojects.com/
- Cobra emphasizes nested commands, local and persistent/global flags, aliases, automatic help commands and help flags, custom help templates, command groups, required flags, validation, and shell completion generation: https://cobra.dev/
- System.CommandLine emphasizes commands, subcommands, options, arguments, aliases, recursive/global options, typed values, custom parsing and validation, parse results, invocation, help, response files, and tab completion: https://learn.microsoft.com/en-us/dotnet/standard/commandline/
- argparse and docopt show that usage syntax, positional argument rules, optional/required/repeating groups, choices, defaults, `--` end-of-options handling, and generated help are long-standing expectations: https://docs.python.org/3/library/argparse.html and https://docopt.org/
- clap shows expectations for polished CLI UX: suggested fixes, colored output where appropriate, shell completions, value enums, command/argument groups, builder and declarative APIs, and fast parsing: https://docs.rs/clap/latest/clap/
- Spectre.Console.Cli shows useful lifecycle boundaries: parse, bind, validate, intercept, execute, return exit code, plus diagnostics and machine-readable command metadata: https://spectreconsole.net/cli/

## Design Constraints From `syntax.md`

- Use file-scoped namespaces, classes, enums, fields, constructors, and block-bodied methods.
- Do not use attributes, properties, records, structs, custom delegate declarations, extension methods, default parameter values, `params`, LINQ, `var`, `try`/`catch`, pattern matching, tuple syntax, async/await, or class-inheritance-oriented APIs.
- Use interfaces only for behavior-only boundaries that benefit from substitution; the supported subset excludes interface fields, default implementations, generic methods, static abstract members, and interface inheritance.
- Do not define generic classes or generic methods.
- Use ordinary overloads instead of optional parameters.
- Use arrays and built-in generic collection types such as `List<T>` and `Dictionary<TKey,TValue>`.
- Prefer explicit result objects and built-in delegate types over custom delegate declarations.
- Keep parser and model APIs usable without reflection.
- Use only `static`, `public`, `internal`, and `const` modifiers. A `public` declaration is part of the supported consumer API; implementation types, methods, and fields must be `internal`.
- Add XML documentation to every public type, constructor, method, field, enum, and enum member. Documentation may use only the constructs allowed by `syntax.md`.

## Feature List From Low Level To High Level

### Token And Syntax Layer

- Preserve raw argv token order.
- Distinguish raw token text, logical token kind, index, and whether token was consumed.
- Recognize long options: `--name`, `--name value`, `--name=value`.
- Recognize short options: `-n`, `-n value`, `-nvalue` when configured.
- Recognize POSIX short option clusters such as `-abc`.
- Recognize short option clusters where the final option consumes a value, such as `-vn5`.
- Recognize Windows-style slash options such as `/name` and `/name:value` when enabled.
- Recognize end-of-options delimiter `--`.
- Treat tokens after `--` as positional input.
- Support a lone `-` as positional input.
- Preserve quoted values as provided by the process argv.
- Support case-sensitive matching by default.
- Allow optional case-insensitive command matching.
- Allow optional case-insensitive option matching.
- Report unknown options.
- Optionally collect unmatched tokens instead of failing.
- Optionally stop parsing at first positional token.
- Optionally stop parsing at first unmatched token.

### Definition Model

- Define an application/root command.
- Define commands and nested subcommands.
- Define command aliases.
- Define hidden commands.
- Define command summaries, descriptions, examples, and usage text.
- Define command groups for help organization.
- Define branch commands that only group subcommands.
- Define default commands.
- Define required subcommands.
- Define options with one or more aliases.
- Define long, short, and slash-style option aliases.
- Define local options for one command.
- Define recursive/global options inherited by child commands.
- Define hidden options.
- Define required options.
- Define boolean flags.
- Define negatable boolean flags such as `--no-color`.
- Define options with required values.
- Define options with optional values.
- Define options with fixed arity.
- Define options with bounded arity ranges.
- Define variadic options.
- Define repeatable options.
- Define count options such as `-vvv`.
- Define default option values.
- Define fallback values for present-but-valueless options.
- Define environment variable fallback names.
- Define positional arguments by position.
- Define required positional arguments.
- Define optional positional arguments.
- Define variadic positional arguments.
- Define choices for option and argument values.
- Define mutually exclusive groups.
- Define at-least-one-required groups.
- Define dependent values where one option implies or requires another.
- Define conflicts between options.
- Define deprecated commands, options, and arguments.
- Define completion candidates for commands, options, and values.

### Parsing And Resolution

- Resolve the deepest matching command path.
- Parse command-local options at the correct command level.
- Parse inherited/global options across command paths.
- Avoid leaking non-recursive parent options into subcommands.
- Parse positional arguments in declared order.
- Parse variadic positional arguments without swallowing subcommands incorrectly.
- Bind repeated values in encounter order.
- Track value source: command line, default, environment, fallback, or implied.
- Track whether each option or argument was explicitly supplied.
- Preserve remaining arguments for pass-through scenarios.
- Produce a parse result even when invocation is not requested.
- Produce structured parse errors with token index and symbol name where possible.
- Support parse-only workflows for tests and tools.

### Type Conversion

- Convert strings to `string`, `bool`, `int`, `long`, `double`, `decimal`, `DateTime`, `TimeSpan`, `FileInfo`, `DirectoryInfo`, and enum values.
- Support custom value parser functions registered by logical type name.
- Support culture-invariant parsing by default.
- Support optional culture-specific parsing through parser settings.
- Report conversion failures with expected type and offending value.
- Support value normalization before conversion.
- Support value choice validation before or after conversion.

### Validation

- Validate missing required options.
- Validate missing required arguments.
- Validate too few or too many values for arity.
- Validate unknown commands.
- Validate unknown options.
- Validate duplicate non-repeatable options.
- Validate option conflicts.
- Validate option dependencies.
- Validate choice sets.
- Validate command-specific custom rules.
- Validate cross-option custom rules.
- Allow help and version options to short-circuit validation.
- Return all useful validation errors when possible instead of failing at the first one.

### Binding

- Bind parse results into a simple `ArgValues` object.
- Retrieve values by canonical symbol name.
- Retrieve option and argument values as strings.
- Retrieve converted scalar values.
- Retrieve converted array values.
- Retrieve boolean flag values.
- Retrieve count flag values.
- Retrieve supplied/default/source metadata.
- Keep binding independent from command execution.
- Avoid reflection-based binding in the first implementation.

### Invocation

- Define command handlers through built-in `Func<ArgContext, int>` values.
- Execute the resolved command handler.
- Pass command context and parsed values to handlers.
- Return integer exit codes.
- Support parse-only, validate-only, and invoke modes.
- Allow a root default handler.
- Allow branch commands to show help when invoked without a handler.
- Support pre-execution and post-execution hooks through built-in `Func<ArgContext, int>` values.
- Keep invocation synchronous for the first implementation.

### Help, Usage, And Version Output

- Generate usage lines.
- Generate command help.
- Generate root help.
- Generate subcommand listings.
- Generate option listings.
- Generate positional argument listings.
- Show default values when configured.
- Show environment variable names when configured.
- Show required markers when configured.
- Show choices when configured.
- Show examples.
- Hide hidden commands and options unless detailed help is requested.
- Support custom help option aliases.
- Support help command such as `app help command`.
- Support version option.
- Support custom version text provider.
- Wrap help to configurable width.
- Keep ANSI styling optional and disabled by default.

### Completion And Tooling

- Generate completion suggestions for command names.
- Generate completion suggestions for option names.
- Generate completion suggestions for option and argument values.
- Filter completions by current command path.
- Support shell completion script generation later for bash, zsh, fish, and PowerShell.
- Expose command model diagnostics for tests and tooling.
- Emit machine-readable command metadata later as JSON.

### Configuration And Quality

- Support parser settings object.
- Support POSIX, GNU, and Windows-friendly option modes through settings.
- Support deterministic error messages.
- Support localization-ready message lookup later without external packages.
- Keep allocations reasonable for common parse paths.
- Keep all public behavior covered by tests.
- Keep all source files conforming to `syntax.md`.

## Critical Must-Have Features For Version 1

- Command tree with nested subcommands.
- Options, flags, arguments, aliases, defaults, required values, and repeatable values.
- GNU/POSIX option syntax: `--name`, `--name=value`, `-n`, `-abc`, and `--`.
- Global/inherited options.
- Parse result with structured errors.
- Type conversion for common .NET scalar types and enums.
- Validation for required, unknown, arity, choices, conflicts, and duplicates.
- Generated help and usage.
- Version option.
- Handler invocation with integer exit code.
- Completion candidate API for commands, options, and static values.
- No external dependencies.
- Simplified C# syntax conformance.

## Deferred Features

- Attribute-based APIs, because attributes are not allowed by `syntax.md`.
- Reflection binding, because explicit binding is simpler, AOT-friendly, and compatible with the syntax policy.
- Async handlers.
- Dependency injection integration.
- Rich console rendering.
- Source generators.
- Full docopt-style usage-pattern parser.
- Localization resource system.
- Response files, unless a later phase confirms exact escaping and recursion rules.

## Public API Completion Tasks

- [DONE] T001: Add public command configuration for custom usage text, help group, explicit branch behavior, default-command selection, and command deprecation, and expose each capability through `IArgCommand`.
- [DONE] T002: Resolve configured default commands when no explicit child command is supplied while preserving command paths, local/global option scope, required-subcommand rules, help behavior, and handler selection.
- [DONE] T003: Make option parsing collect fixed, bounded, and unbounded arities across consecutive tokens without consuming option tokens, end-of-options delimiters, or tokens owned by subcommands and positional arguments.
- [DONE] T004: Add a public option-group model and command APIs for mutually exclusive groups and at-least-one-required groups, including structured validation errors and help representation.
- [DONE] T005: Add public option implication configuration that can supply another option and value with `ArgValueSource.Implied`, and define deterministic precedence against command-line, environment, fallback, and default values.
- [DONE] T006: Surface command, option, and argument deprecation as structured non-error diagnostics when deprecated symbols are used, without preventing parsing, validation, or invocation.
- [DONE] T007: Add parser settings for invariant-by-default or caller-selected `System.Globalization.CultureInfo` conversion and apply the selected culture consistently to all culture-sensitive built-in conversions.
- [DONE] T008: Add value-normalization configuration for options and arguments and support deterministic choice validation against raw, normalized, or converted values at a caller-selected stage.
- [DONE] T009: Add public command-specific and cross-option validation callbacks, aggregate their structured errors with built-in validation, and preserve help/version short-circuit behavior.
- [DONE] T010: Add converted-array retrieval for every built-in scalar conversion supported by `ArgValues`, preserving encounter order and recording per-value conversion failures.
- [DONE] T011: Add public count-option retrieval to `ArgValues` and `IArgValues`, including canonical-name and alias lookup and zero for absent count options.
- [DONE] T012: Add public result-status queries for parse, validation, conversion, and run outcomes so consumers can inspect failures without relying on internal methods or mutable collection counts.
- [DONE] T013: Add a built-in-delegate version text provider to the public application API and use it consistently in validation short-circuit results, run results, help output, and completion.
- [DONE] T014: Add detailed-help configuration that includes hidden commands and options on request while keeping ordinary help and completion filtered by default.
- [DONE] T015: Add optional ANSI help styling controlled by parser/help settings, disabled by default, with plain-text width calculations unaffected by escape sequences.
- [DONE] T016: Add explicit POSIX, GNU-friendly, and Windows-friendly parser mode configuration through `ArgParserSettings`, with deterministic settings for clusters, attached values, slash aliases, matching, and option ordering.
- [DONE] T017: Add shell completion script generation for bash, zsh, fish, and PowerShell using only built-in .NET APIs and the existing command/completion model.
- [DONE] T018: Add machine-readable command metadata output using built-in `System.Text.Json`, covering command paths, aliases, options, arguments, arity, defaults, groups, deprecations, and completion metadata in a deterministic schema.
- [DONE] T019: Add a replaceable message lookup contract for parser, validator, invocation, help, and completion text, with a complete built-in English provider and deterministic fallback behavior.
- [DONE] T020: Add public-API-only model tests for every application, command, option, argument, settings, contract, fluent configuration method, accessor, invalid input, and incompatible configuration combination.
- [DONE] T021: Add public-API-only parsing tests covering every long, short, clustered, attached, slash, delimiter, lone-dash, case, stopping, unmatched, default-command, local/global scope, positional, fixed-arity, bounded-arity, variadic, repeatable, and count behavior.
- [DONE] T022: Add public-API-only resolution and binding tests covering command paths, occurrences, remaining tokens, all value sources and precedence rules, canonical and alias lookup, supplied/defaulted/count metadata, converted scalars and arrays, cultures, normalization, enums, files, directories, and custom converters.
- [DONE] T023: Add public-API-only validation tests covering required symbols and subcommands, arity, unknown input, duplicates, choices, conflicts, dependencies, implications, option groups, custom rules, deprecation diagnostics, multiple-error aggregation, and help/version short-circuiting.
- [DONE] T024: Add public-API-only help and version tests covering usage, descriptions, summaries, examples, command groups, options, arguments, defaults, environment names, required markers, choices, custom aliases, help commands, hidden and detailed views, wrapping, ANSI styling, localization, and dynamic version text.
- [DONE] T025: Add public-API-only invocation tests covering parse-only, validate-only, run, selected and default handlers, explicit branch behavior, before/after hook order and exit codes, validation failures, and help/version short circuits.
- [DONE] T026: Add public-API-only completion tests covering commands, aliases, local and inherited options, option and argument values, hidden filtering, command-path context, case settings, partial input, and generated scripts for every supported shell.
- [DONE] T027: Add public-API-only diagnostics and metadata tests covering every public model field, deterministic text output, deterministic JSON output, schema conformance, escaping, and empty or deeply nested command models.
- [DONE] T028: Add end-to-end tests for representative valid and invalid CLIs that exercise the complete public surface without accessing internals and verify deterministic parse, validation, help, completion, diagnostic, and invocation results.
