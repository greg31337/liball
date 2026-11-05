# API conventions

This document is the normative naming, null, and error-handling contract for
public APIs in the `Email` library. Where a feature-specific document is silent,
these rules apply.

## API boundary and source documentation

A `public` type or member is part of the supported API layer. Implementation
types and cross-type implementation members use `internal`; private state and
single-type helpers omit an accessibility modifier. Tests can exercise internal
behavior through the test assembly's friend access without expanding the
consumer-facing API.

Every public type, constructor, method, field, enum, and enum member has XML
documentation in source. The documentation explains the basic operation,
parameters, returned values, relevant state or ownership behavior, and directly
thrown exceptions. XML comments use only the constructs permitted by
`syntax.md`; Markdown remains the home for longer workflows, security policy,
ownership rules, and examples that span multiple API calls.

## Naming

Public identifiers use `PascalCase`. Library-owned types use the `Email` root
namespace. General email types normally use the `Email` prefix, while types
whose meaning is specifically MIME use the established `Mime` prefix. Option,
result, collection, and enum types use descriptive suffixes such as `Options`,
`Result`, `List`, `Kind`, `Mode`, and `Status`.

State is exposed by ordinary methods rather than properties or indexers:

- `Get...` reads a value, count, or snapshot;
- `Has...`, `Is...`, and `Can...` answer boolean questions;
- `Set...` changes one value;
- `Add...` and `Insert...` retain an additional ordered value;
- `Replace...` substitutes existing values using the method's documented
  matching rule;
- `Remove...` removes matching values and reports whether or how many were
  removed;
- `Clear...` removes an optional value or all members of a collection;
- `Open...`, `Copy...`, `Read...`, and `Write...` make I/O or retained-resource
  behavior explicit;
- `Parse...`, `Validate...`, and `Build...` return result objects when expected
  input failures can occur.

Collection-like classes provide `GetCount()` and `GetAt(int index)`. Methods
that expose multiple values return a new `List<T>` snapshot unless their
documentation explicitly identifies the returned object as mutable library
state. Public APIs do not use optional, named-only, `ref`, or `out` parameters;
policy choices belong in explicit option objects. Synchronous methods do not
carry an `Async` suffix.

Each source class declares at most one constructor. When a concrete type supports
more than one creation shape, its companion `...Operations` implementation
exposes instance `Create(...)` methods while the concrete type's single
constructor remains the canonical initialization path. Stateless feature
services likewise expose instance methods. This keeps defaults and validation
explicit without public static methods, optional parameters, or constructor
overloading.

Boolean methods and enum members use affirmative names. Sizes, limits, offsets,
and durations include their unit when the unit is not inherent in the type or
domain, for example `GetByteCount()` and `SetTimeoutMilliseconds(int value)`.
Abbreviations retain normal .NET casing in compound names, such as `Dkim`,
`Mime`, `Html`, and `Id`.

Every public implementation class has a matching `I...Contract` interface that
declares its public instance methods. Data models, option objects, results, and
collections remain concrete implementations so their validation, ownership,
and snapshot rules stay explicit, while consumers can depend on their declared
contracts. Narrow caller-supplied seams such as `IEmailCancellation`,
`IEmailProgress`, and `IEmailDkimKeyResolver` remain separate domain contracts.
All interface names start with `I` followed by an uppercase letter.

## Null and disabled values

Nullable type syntax is not used, but reference values can still be null. The
following contract removes ambiguity:

- A public reference parameter rejects null with `ArgumentNullException` unless
  that parameter is explicitly documented as null-disabled.
- A null-disabled option value means the associated optional feature,
  collaborator, destination, or override is disabled or not supplied. It never
  means "choose an undocumented default." Enabled defaults are represented by
  a concrete value created by the option object's constructor.
- Optional model values use `Has...()` with `Get...()`, and mutation uses
  `Set...()` plus `Clear...()`. `Get...()` returns null when `Has...()` is false;
  callers must not infer any second meaning from null.
- A failed result may have no value. `HasValue()` reports availability and
  `GetValue()` returns null when no value was produced. A partial recoverable
  result may have both a value and issues.
- Collection getters return an empty collection, not null. Byte and text getters
  return an empty array or empty string for present-but-empty data and null only
  when the value is absent under the preceding rules.

Each null-disabled setting is named in the owning type's documentation. Adding
null-disabled behavior to an existing parameter is an observable API change and
must be recorded in release notes.

## Results, issues, and exceptions

Malformed messages, unsupported encodings or algorithms, validation failures,
and other expected data-dependent outcomes return a feature-specific `...Result`
object. Result objects expose ordinary methods for:

- status or success;
- value presence and the value, including any deterministic partial value;
- an ordered `EmailIssueList`;
- consumed-byte or completion information where parsing can be incremental.

Callers determine success from the result status, not from an empty issue list.
Warnings and informational issues may accompany success. Errors may accompany a
partial value when the operation documents recovery. Issue codes are stable and
are not reused for unrelated meanings.

Exceptions are reserved for programmer mistakes and failures for which the
operation cannot return a meaningful bounded result:

- invalid API use, including forbidden null, invalid indexes, and inconsistent
  option combinations, uses standard argument or invalid-operation exceptions;
- configured hard-limit violations may throw `EmailLimitException` when the API
  cannot safely produce a partial result;
- unrecoverable I/O and cryptographic-provider failures may propagate their BCL
  exception or use the documented direct `Email` exception type.

Parsing and validation do not use exceptions merely to report malformed input.
Public diagnostics must not contain secret key material, sensitive buffer
contents, or more message content than the specific issue contract requires.

## Options and deterministic behavior

Option constructors establish a complete documented default policy. Getters
make every setting inspectable, and setters validate values immediately when
possible. An operation snapshots or reads its options before processing; later
option mutation does not retroactively alter an existing result.

Ambient culture, local time zone, process environment, and mutable global state
do not silently affect parsing or canonical writing. Any such input must be an
explicit option or caller-supplied value. APIs that use randomness or the clock
offer an explicit deterministic test policy where their feature requires one.
