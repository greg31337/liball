# Internal source boundaries

Production internals use the `xml` namespace. Source directories define
dependency layers without creating nested C# namespaces.

Internal layers may reference `System.*`, the public API types that they consume,
and only the internal namespaces listed below. A listed namespace also grants
access to its child namespaces. Public data, enum, static, exception, and
contract namespaces must not reference internal namespaces. Public classes in
`Api/Classes/` are adapters and may reference the internal layers they expose.

| Internal namespace | Allowed internal dependencies |
| --- | --- |
| `Core/` | none |
| `Text/` | `Core/` |
| `Diagnostics/` | `Core/` |
| `Events/` | `Core/`, `Diagnostics/` |
| `Resolution/` | `Core/`, `Text/`, `Diagnostics/` |
| `Namespaces/` | `Core/`, `Text/`, `Diagnostics/` |
| `Dtd/` | `Core/`, `Text/`, `Diagnostics/`, `Resolution/` |
| `Parsing/` | `Core/`, `Text/`, `Diagnostics/`, `Events/`, `Resolution/`, `Namespaces/`, `Dtd/` |
| `Tree/` | `Core/`, `Diagnostics/`, `Events/`, `Namespaces/`, `Dtd/` |
| `Escaping/` | `Core/`, `Text/`, `Diagnostics/` |
| `Include/` | `Core/`, `Text/`, `Diagnostics/`, `Parsing/`, `Resolution/`, `Tree/` |
| `Xdm/` | `Core/`, `Diagnostics/`, `Namespaces/`, `Tree/` |
| `Schema/` | `Core/`, `Text/`, `Diagnostics/`, `Parsing/`, `Resolution/`, `Tree/`, `Xdm/` |
| `XPath/` | `Core/`, `Text/`, `Diagnostics/`, `Resolution/`, `Tree/`, `Xdm/`, `Schema/` |
| `Serialization/` | `Core/`, `Text/`, `Diagnostics/`, `Namespaces/`, `Tree/`, `Escaping/`, `Xdm/` |
| `Canonicalization/` | `Core/`, `Text/`, `Diagnostics/`, `Events/`, `Namespaces/`, `Tree/`, `Escaping/` |
| `Xslt/` | all preceding internal layers except `Canonicalization/` |

Cross-layer callbacks use API contracts; they do not create
reverse internal references. In particular, schema code does not reference the
XPath implementation, and XPath code does not reference the XSLT implementation.
This keeps the dependency graph acyclic as those integrations are added.
