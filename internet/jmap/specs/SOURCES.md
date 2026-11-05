# Specification corpus

Retrieved on 2026-08-27. RFC text and HTML are authoritative publications; errata pages must be checked for status before treating an entry as normative.

## Normative JMAP sources

- `rfc/rfc8620.txt`, `rfc/rfc8620.html`: RFC 8620, JMAP Core, from `https://www.rfc-editor.org/rfc/rfc8620.*`.
- `rfc/rfc8620-inline-errata.html`, `rfc/rfc8620-errata.html`: RFC Editor inline verified errata rendering and complete errata report.
- `rfc/rfc8621.txt`, `rfc/rfc8621.html`: RFC 8621, JMAP Mail, from `https://www.rfc-editor.org/rfc/rfc8621.*`.
- `rfc/rfc8621-errata.html`: RFC Editor complete errata report for RFC 8621.
- `iana/jmap.xml`, `iana/jmap.xhtml`: IANA JMAP capability, error-code, and data-type registries.
- `iana/imap-jmap-keywords.xml`, `iana/imap-jmap-keywords.xhtml`: IANA IMAP and JMAP keyword registry.

## Supporting specifications

- `rfc/rfc3339.txt`: date-time syntax used by Date and UTCDate.
- `rfc/rfc2782.txt`, `rfc/rfc6186.txt`, `rfc/rfc8615.txt`: DNS SRV and well-known service discovery used by Session discovery.
- `rfc/rfc4648.txt`, `rfc/rfc8030.txt`, `rfc/rfc8291.txt`: base64url, generic Web Push headers/behavior, and optional encrypted PushSubscription delivery.
- `rfc/rfc6570.txt`: level-1 URI Templates used by upload, download, and event-source URLs.
- `rfc/rfc6901.txt`: JSON Pointer syntax underlying patch paths and result-reference paths.
- `rfc/rfc7493.txt`, `rfc/rfc8259.txt`: I-JSON and JSON processing requirements.
- `rfc/rfc7807.txt`: HTTP problem details used for request-level errors.
- `rfc/rfc5322.txt`, `rfc/rfc6532.txt`: Internet Message Format and internationalised header syntax.
- `rfc/rfc2045.txt`, `rfc/rfc2047.txt`, `rfc/rfc2231.txt`: MIME body/header encoding and parameter encoding.
- `rfc/rfc2369.txt`, `rfc/rfc2392.txt`, `rfc/rfc3282.txt`: list headers, Content-ID URLs, and content-language fields used by the Email model.
- `rfc/rfc8887.txt`: optional JMAP WebSocket transport, retained as future transport context but outside the RFC 8620/8621 implementation scope.
- `rfc/rfc9404.txt`, `rfc/rfc9670.txt`: later documents that update RFC 8620, retained to avoid designing extension-hostile Core APIs; their Blob/get and sharing features are outside the present scope.

## Reference implementations

- `reference/jmap-client/*`: Stalwart Labs Rust `jmap-client` README and representative Core request/session source from `https://github.com/stalwartlabs/jmap-client`.
- `reference/jmap-client-ts/*`: Linagora TypeScript JMAP client README and public wire types from `https://github.com/linagora/jmap-client-ts`.
- `reference/apache-james/*`: Apache James JMAP configuration page and annotated Core/Mail implementation specifications from `https://james.apache.org/` and `https://github.com/apache/james-project`.
- `reference/stalwart/*`: Stalwart Mail Server overview and representative `jmap-proto` result-reference/error source from `https://github.com/stalwartlabs/mail-server`.

The reference implementation material is informative only. It is used to identify practical API layers, implementation gaps, interoperability cases, and test categories; RFC 8620, RFC 8621, applicable verified errata, and the IANA registries remain the protocol authority.
