# Interface boundaries

Every public implementation class has a contract under `src/Api/Contracts`.
The concrete class retains validation, ownership, and lifecycle invariants, and
its matching `I...Contract` lists exactly the instance operations it exposes.
Stateless services are constructed and invoked as instances; alternative
creation shapes for stateful models are exposed by companion `...Operations`
implementations. Public constants live in method-free `...Constants` classes.

`IEmailCancellation` supplies one cancellation check. `EmailCancellation` is
the standard thread-safe implementation. Tests can instead use a deterministic
implementation that reports cancellation after a known number of checks.

`IEmailProgress` receives counter increments. `EmailProgress` is the standard
thread-safe accumulator and snapshot provider. Applications may implement the
interface to connect operations directly to another metrics sink without
copying from an intermediate `EmailProgress` object.

`IEmailDkimKeyResolver` resolves a parsed `EmailDkimVerificationKey` for the
validated domain and selector in a DKIM signature or ARC set. DKIM and ARC
verification retain their list-based overloads for simple in-memory use; the
resolver overloads avoid preloading unrelated keys and provide a natural seam
for a caller-owned DNS/cache layer. The library itself never performs DNS or
network access. A resolver returns null when no key is available and must not
return a binding for a different domain or selector.

Resource-retaining classes implement `IDisposable` in addition to their
explicit `Close()` method where applicable. This supports `using` statements
without changing the documented ownership rules: owned resources are released,
borrowed resources remain caller-owned, and repeated disposal is harmless.

Contracts such as `IEmailMessageContract`, `IMimeEntityContract`, and
`IEmailContentContract` describe public operations without weakening the
invariants enforced by their concrete implementations. Applications normally
construct and pass the concrete models; the contracts provide an explicit API
boundary and are not an invitation for operations to accept arbitrary model
implementations where their signatures still require concrete validated data.
