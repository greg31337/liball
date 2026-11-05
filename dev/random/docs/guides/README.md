# User guides

These guides explain the choices that affect correctness, reproducibility, and
security. The [behavioral contract](../behavioral-contract.md) is normative when
a guide and an API detail appear to conflict.

- [Engine selection](engine-selection.md): choose a deterministic engine and math mode.
- [Seed handling](seed-handling.md): encode, derive, and store seed material safely.
- [State persistence](state-persistence.md): save and resume deterministic streams.
- [Parallel spawning](parallel-spawning.md): assign stable, independent child streams.
- [Distributions](distributions.md): use parameter conventions, endpoints, and bulk APIs.
- [Custom random sources](custom-random-sources.md): inject controlled uniform bits into samplers and sampling facades.
- [QMC balance](qmc-balance.md): preserve Sobol balance and use other space-filling designs.
- [Secure-token generation](secure-token-generation.md): create tokens, strings, passwords, and UUIDs.
- [Security threat model](../security-threat-model.md): understand trust assumptions, compromise, and operational limits.

The central split is simple:

- Use `RandomGenerator` for simulation, testing, and replay.
- Use `SecureRandomGenerator` for secrets and security-sensitive identifiers.

Neither type is implicitly thread-safe. Give each owner or worker its own
instance.
