# svg

A portable, managed .NET 10 library for loading, editing, resolving, processing,
generating, and rendering SVG without native graphics dependencies.

The planned public surface and its syntax/ownership rules are defined in the
[public API contract](API.md).

For an end-to-end introduction, see [Getting started](docs/getting-started.md).
For the intentionally small host-service interface design and its testing model,
see [Interfaces and test seams](docs/interfaces.md).
For standards coverage and current capability statuses, see the
[standards and support matrix](docs/support-matrix.md). For threat boundaries,
default limits, resource policy, and server-side deployment patterns, see the
[security posture and deployment guide](docs/security.md). For output stability,
processing safety, accessibility defaults, and upgrade policy, see
[serialization, processing, accessibility, and versioning](docs/serialization-and-versioning.md).

## Feature profiles

The library uses the following stable capability identifiers to group features.
They describe how a feature is handled; they are not W3C feature strings and must
not be used as aliases for SVG `requiredFeatures` values.

| Capability identifier | Profile | Contract |
| --- | --- | --- |
| `svg.profile.svg2` | SVG 2 primary | The normative target for the editable document, computed document, scene, processing, declarative animation, and rendering APIs. Individual features can still be unavailable or policy-disabled and are reported separately. |
| `svg.profile.svg11.compat` | SVG 1.1 compatibility | SVG 1.1 documents and compatibility syntax are accepted where they can be mapped to SVG 2 behavior. This includes documented precedence and recovery rules such as unnamespaced `href` taking precedence over `xlink:href`. Deprecated behavior remains identifiable in diagnostics and capability reports. |
| `svg.profile.legacy.preserve` | Preserve-only legacy | Recognized SVG 1.1 legacy, SVG Tiny, SVG 1.2, vendor, and unknown content is retained in the editable document and can be serialized without being represented as implemented rendering behavior. A preserve-only feature is reported or diagnosed instead of being silently treated as rendered. |
| `svg.profile.host.callback` | Host callback | Features that require an environment or codec outside the portable core are represented in the document and resolved only through an explicit caller callback. Examples include external resources, unsupported raster formats, advanced text shaping or font containers, and `foreignObject` content. No callback is invoked implicitly, and policy checks run before a callback. |

Profiles are not mutually exclusive. For example, an SVG 2 `image` element is in
`svg.profile.svg2`, while decoding its JPEG resource can also require
`svg.profile.host.callback`. Likewise, a feature's profile does not claim that it
is currently implemented. Capability reporting assigns the feature a separate
status such as implemented, callback-required, preserve-only, policy-disabled, or
unsupported.

Future feature identifiers use lowercase ASCII dot-separated names beginning
with `svg.feature.`. Profile and feature identifiers are permanent once published:
their meaning is not reassigned, and aliases use distinct identifiers. The
capability API will expose these exact strings as constants; document reports and
conditional-processing input use the same identifiers.
