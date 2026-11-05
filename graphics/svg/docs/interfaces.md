# Interfaces and test seams

Every public behavioral class implements an interface stored under
`src/Api/Contracts`. This keeps public operations instance-based, prevents
public static entry points, and makes the complete behavioral surface explicit.
Implementation classes live under `src/Api/Classes` and expose one constructor
plus only the instance methods declared by their contracts.

Public data remains concrete. Options, results, editable models, and snapshots
live under `src/Api/Data`, expose documented fields, do not implement interfaces,
and have no public methods. Internal helper methods can maintain invariants
without becoming part of the public contract. Constants-only types remain under
`src/Api/Static`.

## Operation contracts

Stateless parsers, geometry operations, resolvers, processors, and renderers use
a paired contract and implementation. For example, `ISvgLoader` is implemented
by `SvgLoader`, while `ISvgRenderer` is implemented by `SvgRenderer`. Callers can
construct the standard implementation directly or accept the corresponding
interface at an application boundary.

Former static overloads have distinct instance-method names because interface
members cannot be overloaded under the syntax policy. A `With...` suffix records
the differentiating parameters. Factories that require privileged construction
data are separate services; `ISvgCatalogFactory` and `SvgCatalogFactory` create
the built-in element and legacy catalogs.

## Host-service contracts

`ISvgResourceProvider` supplies synchronous approved resource requests and can
retain credentials, caches, request logs, or other host state. The standard
`SvgResourceProvider` adapts a single callback.

`ISvgDynamicSelectorState` groups link, hover, active, focus, target, language,
and revision queries. `SvgDynamicSelectorStateOptions` configures the standard
callback-backed implementation, while `ISvgMutableDynamicSelectorState` exposes
`MarkChanged` for advancing its revision after caller-managed captured state
changes without expanding the host-query contract.

One-operation predicates, notifications, and optional fallbacks remain
`Func<...>` or `Action<...>` fields on data classes. They do not require a
dedicated public interface when no reusable service state is involved.

## Testing model

Tests can implement a contract with a small deterministic fake or exercise its
standard implementation. A resource fake can return fixed bytes while recording
normalized URI, resource kind, request count, and cancellation. A selector-state
fake can expose deterministic host state and drive cache invalidation by changing
its version. Stateless operation interfaces also allow focused substitution at
application boundaries without subclassing library implementations.

Public interfaces do not inherit other interfaces and contain instance methods
only. Adding a member to a published interface remains a breaking change, so a
future capability that is not essential to an existing contract should use a
separate interface.
