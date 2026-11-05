# Specification sources

Downloaded on 2026-08-25. These files are local implementation and test references. The formal CommonMark and GFM documents take precedence for their respective dialects; the extension documents describe the conventions named in `todo.md` where no universal Markdown standard exists.

## Core Markdown

- `core/commonmark-0.31.2-spec.txt` — CommonMark 0.31.2 specification and embedded examples, from https://spec.commonmark.org/0.31.2/spec.txt
- `core/commonmark-0.31.2-examples.json` — machine-readable CommonMark 0.31.2 examples, from https://spec.commonmark.org/0.31.2/spec.json
- `core/gfm-0.29-spec.html` — official GitHub Flavored Markdown 0.29-gfm specification and examples, from https://github.github.com/gfm/
- `core/html-named-character-references.json` — WHATWG named character-reference data linked by the Markdown specifications, from https://html.spec.whatwg.org/multipage/entities.json

GFM 0.29-gfm is based on CommonMark 0.29. The library's CommonMark baseline remains 0.31.2; GFM extension behavior should follow the official GFM document without regressing the newer CommonMark baseline.

## Extension conventions

- `extensions/markdown-extra-working-draft.md` — Markdown Extra draft covering attributes, footnotes, definition lists, tables, and abbreviations, from https://michelf.ca/specs/markdown-extra/index.text
- `extensions/pandoc-users-guide.html` — Pandoc Markdown descriptions for metadata, math, superscript, subscript, and other widely used extensions, from https://pandoc.org/MANUAL.html
- `extensions/python-markdown-admonition.html` — `!!!` admonition syntax, from https://python-markdown.github.io/extensions/admonition/
- `extensions/github-writing-and-formatting.html` — GitHub alert/callout and user-facing Markdown syntax, from https://docs.github.com/en/get-started/writing-on-github/getting-started-with-writing-and-formatting-on-github/basic-writing-and-formatting-syntax
- `extensions/hugo-front-matter.html` — YAML, TOML, and JSON front matter delimiter conventions, from https://gohugo.io/content-management/front-matter/
- `extensions/smartypants.html` — original SmartyPants typographic-substitution description, from https://daringfireball.net/projects/smartypants/
- `extensions/markdown-it-*.md` — primary syntax descriptions for the markdown-it abbreviation, container, definition-list, emoji, footnote, inserted-text, marked-text, subscript, and superscript plugins, downloaded from their repositories under https://github.com/markdown-it/

Features such as caller-defined fenced blocks, custom delimiters, include directives, and diagram metadata are intentionally application-defined. Their exact contracts must be fixed by this library's public API and tests rather than inferred from a nonexistent universal specification.

## Internet and Unicode references

- `rfcs/rfc2397-data-url.txt` — RFC 2397, data URLs.
- `rfcs/rfc3986-uri.txt` — RFC 3986, generic URI syntax.
- `rfcs/rfc5322-email.txt` — RFC 5322, Internet message and address syntax.
- `rfcs/rfc6068-mailto.txt` — RFC 6068, the `mailto` URI scheme.
- `unicode/uax14-unicode-line-breaking-17.0.html` — Unicode Standard Annex #14, Unicode 17.0 line-breaking behavior, from https://www.unicode.org/reports/tr14/tr14-55.html

The RFC files were downloaded from https://www.rfc-editor.org/.
