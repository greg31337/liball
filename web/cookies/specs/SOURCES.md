# Research sources

Retrieved on 2026-08-27. These files are design and test references; they are not runtime dependencies and nothing under `src/` may depend on them.

## Normative and standards-track material

| Local file | Upstream source | Purpose |
|---|---|---|
| `rfc/rfc6265.txt` | https://www.rfc-editor.org/rfc/rfc6265.txt | Required HTTP cookie baseline. |
| `rfc/rfc6265-errata.html` | https://www.rfc-editor.org/errata/rfc6265 | Published RFC 6265 errata. |
| `drafts/draft-ietf-httpbis-rfc6265bis-22.txt` | https://www.ietf.org/archive/id/draft-ietf-httpbis-rfc6265bis-22.txt | IESG-approved successor in the RFC Editor queue; SameSite, prefixes, limits, modern parsing and retrieval. |
| `rfc/rfc9110.txt` | https://www.rfc-editor.org/rfc/rfc9110.txt | HTTP semantics, methods, field handling, and HTTP-date context. |
| `rfc/rfc9113.txt` | https://www.rfc-editor.org/rfc/rfc9113.txt | HTTP/2 field-section rules relevant to optional Cookie field splitting. |
| `rfc/rfc9114.txt` | https://www.rfc-editor.org/rfc/rfc9114.txt | HTTP/3 field-section rules relevant to optional Cookie field splitting. |
| `rfc/rfc3986.txt` | https://www.rfc-editor.org/rfc/rfc3986.txt | URI host and path model. |
| `rfc/rfc5890.txt` | https://www.rfc-editor.org/rfc/rfc5890.txt | IDNA definitions used by host canonicalization. |
| `rfc/rfc5891.txt` | https://www.rfc-editor.org/rfc/rfc5891.txt | IDNA protocol and U-label/A-label conversion requirements. |
| `rfc/rfc3492.txt` | https://www.rfc-editor.org/rfc/rfc3492.txt | Punycode encoding reference. |
| `rfc/rfc6761.txt` | https://www.rfc-editor.org/rfc/rfc6761.txt | Special-use names, including localhost handling. |
| `drafts/draft-cutler-httpbis-partitioned-cookies-00.txt` | https://www.ietf.org/archive/id/draft-cutler-httpbis-partitioned-cookies-00.txt | Informational CHIPS/`Partitioned` proposal; optional extension only. |

The 6265bis file is a dated draft snapshot, not yet an RFC as of retrieval. The implementation plan therefore keeps RFC 6265 as the baseline and places changed or added behavior behind an explicit compliance policy.

## Public Suffix List

| Local file | Upstream source | Purpose |
|---|---|---|
| `public-suffix/public_suffix_list.dat` | https://publicsuffix.org/list/public_suffix_list.dat | PSL syntax/examples and integration test fixture. Snapshot version `2026-08-19_19-18-48_UTC`, commit `e8c9a2b2b2856b6449999dd0ec0d118f364ed0cd`. |
| `public-suffix/list-format.html` | https://publicsuffix.org/list/ | Official PSL format and update instructions. |

The PSL snapshot is licensed under MPL-2.0 as stated in the file. It is not embedded in the production assembly; the public API exposes provider hooks so applications can choose and update their own PSL implementation.

## Implementation comparisons

| Local files | Upstream source | Design lessons examined |
|---|---|---|
| `research/tough-cookie/README.md`, `research/tough-cookie/api.md` | https://github.com/salesforce/tough-cookie | Liberal/strict parsing, low-level match helpers, pluggable stores, memory jar, serialization, SameSite context, prefix policy, and trustworthy-localhost policy. |
| `research/python/http.cookiejar.html`, `cookiejar.py`, `test_http_cookiejar.py`, `test_http_cookies.py` | https://docs.python.org/3/library/http.cookiejar.html and https://github.com/python/cpython | Separate acceptance/return policy, allow/block controls, session clearing, persistence abstraction, Netscape file compatibility, and malformed-input coverage. Legacy RFC 2109/2965 behavior is research-only and is not planned as a production protocol. |
| `research/go/cookie.go`, `cookie_test.go`, `jar.go`, `jar_test.go` | https://github.com/golang/go | Small transport-neutral jar contract, public-suffix hook, host/IP canonicalization, Max-Age precedence, deterministic replacement identity, stable selection ordering, concurrency, and parse/validation behavior. |
| `research/curl/http-cookies.html`, `cookie.c`, `cookie-jar.md` | https://curl.se/docs/http-cookies.html and https://github.com/curl/curl | Netscape cookie-file interoperability, `#HttpOnly_`, PSL rejection, prefix checks, localhost security treatment, secure-cookie overlay protection, 400-day cap, session handling, and robust malformed-input rejection. |
| `research/wpt-cookies-readme.md` | https://github.com/web-platform-tests/wpt/tree/master/cookies | Location and organization of browser cookie interoperability tests for selecting portable conformance cases. |
| `research/secure-contexts.html` | https://www.w3.org/TR/secure-contexts/ | Potentially trustworthy origin algorithm referenced by modern cookie behavior. |

Third-party source and test snapshots retain their upstream notices. They are reference material only; implementation code must be original and use only built-in .NET APIs.
