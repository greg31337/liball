# Specification source index

These files were downloaded on 2026-08-24 as local research snapshots for the
feature scope in `todo.md`. Living standards can change after this snapshot;
the source URL remains the authority when behavior is revised.

## Normative and standards-track sources

| Local file | Source | Relevant scope |
|---|---|---|
| `standards/whatwg-html.html` | <https://html.spec.whatwg.org/> | HTML syntax, parsing, tree construction, named character references, serialization rules, elements, attributes, and HTML microsyntaxes |
| `standards/whatwg-encoding.html` | <https://encoding.spec.whatwg.org/> | Encoding labels, sniffing hooks, decoder algorithms, and error handling |
| `standards/whatwg-encoding-indexes.json` | <https://encoding.spec.whatwg.org/indexes.json> | Machine-readable legacy-encoding indexes |
| `standards/whatwg-dom.html` | <https://dom.spec.whatwg.org/> | Tree relationships, traversal, mutation, cloning, normalization, and text content |
| `standards/whatwg-url.html` | <https://url.spec.whatwg.org/> | URL parsing, resolution, normalization, hosts, schemes, and security behavior |
| `standards/w3c-selectors-4.html` | <https://www.w3.org/TR/selectors-4/> | Selector grammar, matching, combinators, specificity, and pseudo-classes |
| `standards/w3c-css-syntax-3.html` | <https://www.w3.org/TR/css-syntax-3/> | CSS tokenization, parsing, escapes, error recovery, and `An+B` syntax |
| `standards/w3c-css-style-attributes.html` | <https://www.w3.org/TR/css-style-attr/> | Parsing the contents of HTML `style` attributes |
| `standards/commonmark-0.31.2.txt` | <https://spec.commonmark.org/0.31.2/spec.txt> | CommonMark block/inline constructs, escaping, and examples |
| `standards/github-flavored-markdown.html` | <https://github.github.com/gfm/> | GFM tables, task items, strikethrough, autolinks, and tag filtering |
| `standards/w3c-json-ld-1.1.html` | <https://www.w3.org/TR/json-ld11/> | JSON-LD syntax and processing constraints relevant to metadata extraction |

## RFCs

| Local file | Source | Relevant scope |
|---|---|---|
| `rfcs/rfc2397-data-urls.txt` | <https://www.rfc-editor.org/rfc/rfc2397.txt> | `data:` URL syntax used by sanitizer policy |
| `rfcs/rfc3986-uri.txt` | <https://www.rfc-editor.org/rfc/rfc3986.txt> | Generic URI syntax and reference resolution |
| `rfcs/rfc3987-iri.txt` | <https://www.rfc-editor.org/rfc/rfc3987.txt> | Internationalized resource identifiers |
| `rfcs/rfc5646-language-tags.txt` | <https://www.rfc-editor.org/rfc/rfc5646.txt> | Language-tag syntax and matching inputs |
| `rfcs/rfc8259-json.txt` | <https://www.rfc-editor.org/rfc/rfc8259.txt> | JSON grammar and interoperability constraints for JSON-LD input |
| `rfcs/rfc8288-web-linking.txt` | <https://www.rfc-editor.org/rfc/rfc8288.txt> | Link relation semantics relevant to canonical and alternate metadata |

## Metadata and security resources

| Local file | Source | Relevant scope |
|---|---|---|
| `resources/schemaorg-current-https.jsonld` | <https://schema.org/version/latest/schemaorg-current-https.jsonld> | Current Schema.org vocabulary used to recognize selected article metadata |
| `resources/open-graph-protocol.html` | <https://ogp.me/> | Open Graph metadata property definitions |
| `resources/dcmi-metadata-terms.html` | <https://www.dublincore.org/specifications/dublin-core/dcmi-terms/> | Dublin Core metadata terms |
| `resources/owasp-xss-prevention.html` | <https://cheatsheetseries.owasp.org/cheatsheets/Cross_Site_Scripting_Prevention_Cheat_Sheet.html> | HTML sanitization boundaries and safe-use guidance |
| `resources/owasp-dom-clobbering.html` | <https://cheatsheetseries.owasp.org/cheatsheets/DOM_Clobbering_Prevention_Cheat_Sheet.html> | DOM-clobbering threats and mitigations |

## Upstream feature references

These project descriptions are non-normative. They are retained because the
original product research used them to establish a practical, complete feature
surface.

| Local file | Source | Relevant scope |
|---|---|---|
| `resources/jsoup-cookbook.html` | <https://jsoup.org/cookbook/> | Cohesive parsing, DOM, querying, cleaning, and serialization workflows |
| `resources/anglesharp.md` | <https://github.com/AngleSharp/AngleSharp> | Standards-oriented .NET HTML/DOM/selectors behavior |
| `resources/html-agility-pack.md` | <https://github.com/zzzprojects/html-agility-pack> | Forgiving .NET HTML loading, traversal, and saving |
| `resources/beautiful-soup.html` | <https://www.crummy.com/software/BeautifulSoup/bs4/doc/> | Tree navigation, search, mutation, text extraction, and encoding metadata |
| `resources/parse5.md` | <https://github.com/inikulin/parse5> | WHATWG parser/tokenizer separation and source-location support |
| `resources/html5lib.html` | <https://html5lib.readthedocs.io/en/latest/> | HTML conformance behavior and serializer/filter concepts |
| `resources/dompurify.md` | <https://github.com/cure53/DOMPurify> | Sanitizer profiles, namespace handling, URI checks, and removal reporting |
| `resources/owasp-java-html-sanitizer.md` | <https://github.com/OWASP/java-html-sanitizer> | Composable sanitizer policies and security-oriented processing |
| `resources/ganss-html-sanitizer.md` | <https://github.com/mganss/HtmlSanitizer> | HTML/CSS/URL allowlists and base-URI handling |
| `resources/turndown.md` | <https://github.com/mixmark-io/turndown> | Rule-driven HTML-to-Markdown conversion |
| `resources/html-to-text.md` | <https://github.com/html-to-text/node-html-to-text> | Semantic text formatting, wrapping, selectors, links, images, and tables |
| `resources/mozilla-readability.md` | <https://github.com/mozilla/readability> | Readerability preflight, article scoring, cleanup, and result fields |
