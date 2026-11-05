# HTML processing

`new EmailHtmlProcessor().ExtractDataUrisWithHtml` converts supported `data:` URL references
into detached related `MimeEntity` parts and returns HTML whose references use
`cid:` URLs. The operation does not mutate the supplied HTML entity, construct a
`multipart/related` container, fetch URLs, or sanitize unrelated markup. Callers
can place the returned HTML and ordered related parts into their desired MIME
tree.

Extraction recognizes URL attributes, `srcset` candidates, inline CSS
`url(...)` values, and CSS in `style` elements. Script text and comments are not
scanned. Unchanged source is copied exactly; an attribute containing an extracted
reference is serialized with a quoted, HTML-escaped value.

The default media-type allowlist contains `image/png`, `image/jpeg`,
`image/gif`, `image/webp`, and `image/avif`. Applications can replace or extend
that exact-type allowlist through `EmailHtmlDataUriOptions`. Both percent-encoded
and strict Base64 payloads are supported according to
[RFC 2397](https://www.rfc-editor.org/rfc/rfc2397). Malformed data URLs and media
types outside the allowlist remain unchanged and produce
`HtmlDataUriRejected`; successful extraction produces `HtmlDataUriExtracted`.

Generated content IDs are deterministic SHA-256 content addresses over the
parsed media type and decoded bytes. Repeated equivalent references in one
operation reuse one related part and one `cid:` value, following the Content-ID
URL relationship in [RFC 2392](https://www.rfc-editor.org/rfc/rfc2392). Related
parts expose decoded content and declare Base64 as their transfer encoding.

Input, output, token, nesting, attribute, URL, media-type, extracted-reference,
and decoded-byte limits are enforced before a complete result is returned. The
data-URI count applies to successfully rewritten references; the byte total
applies to unique related-part content after deduplication.
