# Reference material

This directory contains the specifications and implementation descriptions used to prepare `todo.md`. The snapshots were retrieved on 2026-08-27.

## Protocol and identifier specifications

- `rfcs/rfc3912.txt` — [RFC 3912](https://www.rfc-editor.org/rfc/rfc3912.txt), the normative WHOIS wire protocol.
- `rfcs/rfc954.txt` — [RFC 954](https://www.rfc-editor.org/rfc/rfc954.txt), the historical WHOIS specification obsoleted by RFC 3912.
- `rfcs/rfc2167.txt` — [RFC 2167](https://www.rfc-editor.org/rfc/rfc2167.txt), the separate RWhois protocol, used to define the boundary around `rwhois://` referrals.
- `rfcs/rfc5890.txt` and `rfcs/rfc5891.txt` — [RFC 5890](https://www.rfc-editor.org/rfc/rfc5890.txt) and [RFC 5891](https://www.rfc-editor.org/rfc/rfc5891.txt), IDNA terminology and protocol relevant to converting domain queries to ASCII form.

## Operational descriptions

- `iana/whois-service.html` — [IANA's WHOIS service description](https://www.iana.org/help/whois), covering domain, IP, prefix, and AS queries, UTF-8 responses, colon-delimited fields, comments, and `refer:` referrals.
- `iana/service-names-port-numbers.xml` — [IANA's service-name and port-number registry](https://www.iana.org/assignments/service-names-port-numbers/service-names-port-numbers.xml), including TCP port 43 for WHOIS and TCP port 4321 for RWhois.
- `icann/rdds-consistent-labeling.html` — [ICANN's RDDS consistent labeling and display policy](https://www.icann.org/en/contracted-parties/consensus-policies/registry-registration-data-directory-services-consistent-labeling-and-display-policy/registry-registration-data-directory-services-consistent-labeling-and-display-policy-01-02-2017-en), including gTLD WHOIS labels, repeated fields, record boundaries, and registrar WHOIS referrals.

## Surveyed library descriptions

- `research/python-whois-readme.md` — [`richardpenman/whois`](https://github.com/richardpenman/whois) (`python-whois`).
- `research/likexian-whois-readme.md` — [`likexian/whois`](https://github.com/likexian/whois) for Go.
- `research/domainr-whois-readme.md` — [`domainr/whois`](https://github.com/domainr/whois) for Go.
- `research/whois-rust-readme.md` — [`magiclen/whois-rust`](https://github.com/magiclen/whois-rust) for Rust.

The research also consulted the current source/API documentation linked from `todo.md`; those links remain external so their live API details and provenance are visible.
