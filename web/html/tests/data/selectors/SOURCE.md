# Pinned WPT selector subset

- Upstream: https://github.com/web-platform-tests/wpt
- Revision: `9bc6e2404bff5349e48d7962b0a495582bc5ade8`
- License: BSD 3-Clause; see `LICENSE.md`
- Vendored checksum: see `../fixture-manifest.json`

`wpt-selector-subset.tsv` is a manually normalized subset of these upstream
files. Selectors and expected IDs are preserved; browser DOM construction is
represented by named fixtures in the test adapter.

| Upstream file | SHA-256 |
| --- | --- |
| `dom/nodes/selectors.js` | `cffc3f46deb933d63d4cb2cfd811d3ec21ec7804faab4826c8aba1868459e8d1` |
| `dom/nodes/ParentNode-querySelector-scope.html` | `c4e9845879103662af728a7165ee303c1cb53ffbef9ec32d04a28084ce7773bc` |
| `dom/nodes/ParentNode-querySelectors-namespaces.html` | `1366718eae7a46e8df819c62a0fd40061aa4d12d65cc70b31f1231a32fe77a64` |
| `dom/nodes/ParentNode-querySelector-case-insensitive.html` | `dba508eb8c717cb39f8bba9da67237aca67ba0fa503b4cd3d0e6aa8777b07574` |
| `dom/nodes/ParentNode-querySelector-escapes.html` | `fa1e02eec0cc18cd335c07e25a8ee83dc14e091fcfd93e99bdfc8c774001d915` |
| `dom/nodes/ParentNode-querySelectorAll-removed-elements.html` | `312262f5497da7e53922e1e198196d80eb9a6fe2d4558ea2ae91e650e227b898` |
| `dom/nodes/DocumentFragment-querySelectorAll-after-modification.html` | `22d51b1306a2f32023a01f9905af80d5945e4b8ac2a6420c14e457d369485263` |

The subset includes deterministic tree-observable selectors supported by the
library. It excludes layout, user interaction, shadow DOM, pseudo-elements,
and browser state. Namespace-prefix declarations are excluded because the DOM
Selectors API has no namespace resolver; undeclared prefixes remain invalid.
The reproducible update procedure is in `../UPDATING.md`.
