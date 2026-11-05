# Text analysis, search, and safe display

`new EmailTextProcessor().AnalyzeWithValue` separates decoded text into visible, quoted-reply,
and signature regions and produces a whitespace-collapsed snippet. Detection is
a transparent convenience heuristic, not a statement of sender intent. It
recognizes quoted `>` lines, common `On ... wrote:` and original/forwarded
message markers, mobile sign-offs, and the `-- ` signature separator defined by
[RFC 3676](https://www.rfc-editor.org/rfc/rfc3676). Options bound input size,
line count, and snippet length and determine whether quoted or signature text
may appear in the snippet.

`new EmailTextProcessor().SearchWithMessageAndQuery` searches decoded MIME text without changing the
message. Plain text is processed for `format=flowed`; HTML is converted to
visible text so script/style content is not searched. Matching is ordinal and
case-insensitive by default. Text attachments and HTML can be independently
included or excluded. Results identify the entity path and decoded character
offset and contain a bounded context. Character or match exhaustion returns a
deterministic partial result with `TextSearchLimitReached`; traversal and decode
hard limits retain their existing exception contracts.

`new EmailDisplayBuilder().BuildWithMessage` creates a read-only safe display projection. It:

- selects the preferred plain or HTML body and retains the alternatives;
- decodes plain text and `format=flowed` content;
- sanitizes selected HTML before exposing it and derives display text from the
  sanitized fragment;
- provides the snippet and text-analysis result;
- separates inline resources from downloadable attachments;
- reports unresolved related references and aggregates processing warnings;
- counts signed, encrypted, signature, encryption, and report MIME structures.

The structural security summary never claims signature verification,
decryption, authentication, or trust. Those require the separate security APIs
and caller-supplied evidence. Display construction does not mutate the source
message or fetch remote content.
