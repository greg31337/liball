# C# Syntax Rules

This is the feature policy for C# files under `src/` and `tests/`.

*** Do not modify! ***

## Source files (`src/**/*.cs`)

### Allowed

- Compilation units with `using` directives, one file-scoped namespace, classes, and default integer type enums.
- Classes with fields, at most one constructor, ordinary block-bodied methods, and simple base lists for exception types that derive directly from `System.Exception`.
- Interfaces with instance methods only, no fields, no default implementations, no static abstract members, no generic methods, no interface inheritance. Interfaces must be named starting with "I" followed by upper case letter. Classes can inherit from interfaces.
- Enum members must have constant int32 values.
- Predefined, named, qualified, `global::`-qualified, generic reference, and array types.
- Field, local variable, and enum member initializers.
- Plain value parameters in ordinary parenthesized parameter lists.
- Blocks, empty statements, local declarations, expression statements, `if`/`else`, constant/default `switch` on (bool, integer, enum, string) without fallthroughs, loops, `foreach`, `break`, `continue`, `return`, `throw`, `using`, and `lock`.
- Literals, `this`, parentheses, ordinary unary/binary operators, assignments, conditional expressions, casts, `default(T)`, throw expressions, calls, member access, conditional member access, element access, lambdas, object creation, array creation, initializers, collection expressions without spread, and interpolated strings.
- Nullable reference types like `string?`, `MyType?`, `List<T>?`.
- Positional invocation arguments.
- Only `static`, `public`, `internal`, `const`, and static field `readonly` modifiers. Public classes, methods, fields, enums, form the defined API layer.
- `static` fields must be `const` or `readonly`.
- XML documentation, required for public type, constructor, method, field, enum, and enum member, with the only allowed constructs: <summary>, <remarks>, <param>, <returns>, <exception>, <example>, <see>, <seealso>, <para>, <c>.
- Generic types from .NET libraries and span types when appropriate.

### Not allowed

- Block-scoped namespaces, global statements, structs, records, nested types, delegates, extension blocks, attributes, preprocessor directives.
- Nullable type forms for value types like `int?`, `bool?`, etc., generic `T?`, `Nullable<T>`. Null forgiving operator `!` and `#nullable` directives.
- Native integer types `nint`, `nuint`, `IntPtr`, and `UIntPtr`.
- Primary constructors, explicit base constructor initializers outside exception types, generic type or method declarations, and generic constraints.
- Pointer, function pointer, tuple, `ref`, and `scoped` type forms.
- `ref`, `out`, `in`, `params`, default parameter values, and function pointer parameters.
- Properties, indexers, events, operators, conversion operators, destructors, local functions, and incomplete members.
- `try`/`catch`/`finally`, `goto`, labels, `fixed`, `unsafe`, iterator `yield`, and deconstructing `foreach`.
- `checked` and `unchecked` statements and expressions.
- Named arguments and conditional element access.
- Patterns, switch expressions, tuple expressions, anonymous methods, anonymous objects, `await`, `ref` expressions, `typeof`, `sizeof`, unsafe expressions, stack allocation, ranges, spreads, `with` expressions, typed references, and `base` member access.
- Pointer/address-of unary forms and pattern-oriented binary forms, even though their broad Roslyn node types may otherwise be allowed.
- Inheritance outside exception types or outside interfaces inherited by classes, `override`, inherited member hiding, and LINQ.
- Usage of IEquatable.
- Method overloading; instance member names must be unique within an interface and within the combined member set of all interfaces implemented by a class.

## Test files (`tests/**/*.cs`)

Any valid C# syntax is allowed except nullable type forms for value types like `int?`, `bool?`, etc., generic `T?`, `Nullable<T>`. C# parse errors are still reported.

## Public API organization

- All public types must be declared under src/Api.
- No non-public types may be declared under src/Api.
- No public types may be declared outside src/Api.
- No public types can have public static methods.

Public API types must belong to exactly one of the following categories:
- interfaces; stored under src/Api/Contracts;
- data-holding classes, which must contain at least one public non-static, non-const field, may contain only `internal` helper methods, and may not implement interfaces; stored under src/Api/Data
- static data classes containing no methods or delegates in any form, but only permitted static/const fields; stored under src/Api/Static
- enums; stored under src/Api/Enums
- custom exception classes based on `System.Exception`; stored under src/Api/Exceptions
- classes implementing one or more interfaces; stored under src/Api/Classes.

A public implementation class may expose only one constructor and instance members declared by its implemented interfaces, but it may contain internal fields or methods.

Data-holding class must be used at least once in an interface or in another data-holding class.
Enum must be used at least once either in a data-holding class or an interface.

## Coding conventions

- Use signed integers for counts and sizes, unsigned only for bit and binary related values.
- Use exceptions as the general mechanism for reporting operation failure. When failures are expected to be frequent, add dedicated exception-less API.
- Existing code is not authoritative evidence that a construct is permitted.
- When several valid C# implementations are possible, choose the implementation using the smallest and most portable subset of the language.
