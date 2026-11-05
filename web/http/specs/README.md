# Local specification set

Retrieved on 2026-08-27. These files are research inputs and test-vector sources for the implementation plan in `../todo.md`; they are not runtime dependencies.

## Normative RFCs

- `rfcs/rfc9110.txt`: HTTP Semantics.
- `rfcs/rfc9111.txt`: HTTP Caching.
- `rfcs/rfc9112.txt`: HTTP/1.1 messaging and connection management.
- `rfcs/rfc9113.txt`: HTTP/2 framing, streams, connections, and HTTP mapping.
- `rfcs/rfc7541.txt`: HPACK.
- `rfcs/rfc9218.txt`: Extensible HTTP priorities and HTTP/2 `PRIORITY_UPDATE`.
- `rfcs/rfc8441.txt`: Extended CONNECT and `SETTINGS_ENABLE_CONNECT_PROTOCOL`.
- `rfcs/rfc8336.txt`: HTTP/2 `ORIGIN` frame.
- `rfcs/rfc9651.txt`: Structured Fields, used by extensible priorities.
- `rfcs/rfc5861.txt`: `stale-while-revalidate` and `stale-if-error` cache extensions referenced by RFC 9111.
- `rfcs/rfc3986.txt`: URI generic syntax.
- `rfcs/rfc2046.txt`: MIME multipart syntax used by `multipart/byteranges`.
- `rfcs/rfc4647.txt`: language-range matching used by content negotiation.
- `rfcs/rfc5234.txt` and `rfcs/rfc7405.txt`: ABNF syntax and case-sensitive strings.
- `rfcs/rfc1950.txt`, `rfcs/rfc1951.txt`, `rfcs/rfc1952.txt`, and `rfcs/rfc7932.txt`: zlib, DEFLATE, gzip, and Brotli content codings supported by built-in .NET codecs.

The RFC text files came from `https://www.rfc-editor.org/rfc/rfcNNNN.txt`. Corresponding RFC Editor errata search results for the primary protocol documents are saved under `errata/`.

## Current registries

The CSV snapshots under `iana/` came from the official IANA HTTP Method, HTTP Status Code, HTTP Field Name, HTTP Parameters, and HTTP/2 Parameters registries. They cover registered methods, status codes, field names, content and transfer codings, range units, HTTP/2 frame types, settings, and error codes.

## Implementation comparisons

- `references/nghttp2-README.rst`: reusable HTTP/2 framing and public HPACK encoder/decoder model.
- `references/llhttp-README.md`: incremental HTTP/1 parser callbacks, pause/resume behavior, and explicit security-sensitive leniency controls.
- `references/hyper-README.md`: low-level client/server HTTP/1 and HTTP/2 library boundary.
- `references/h2-README.md`: HTTP/2 client/server connection, stream, and flow-control model.
- `references/netty-http-package.html`: HTTP/1 message, headers, chunk, aggregation, upgrade, content coding, and limit model.
- `references/netty-http2-package.html`: frames, HPACK, connection/stream state, push, settings, and local/remote flow controllers.
- `references/h2spec-README.md`: HTTP/2 conformance-suite coverage reference.

The GitHub README snapshots correspond to repository heads observed on 2026-08-27: nghttp2 `b6729c46cf134a59940a20de534b7f44fdb0e877`, llhttp `ee51043701d8b90ef647cf04790cbb1c55889825`, hyper `37c9d0c7e018d3c218f1fc0e472207ff05ef3772`, and h2 `69a678abc731ebc9780b41b073d47cc9f154d7b9`.

## Test vectors

`test-vectors/hpack-test-case-master.tar.gz` is the complete `http2jp/hpack-test-case` interoperability corpus at commit `8a1406e7d14bfcb6c046021f13cc15cfb162726d`. Its SHA-256 is `653258cdb60790e07f30fef24aa3bdd0492565e74a3c09db706b08a0117ba0d5`. The adjacent README describes the JSON story format and shared compression contexts.
