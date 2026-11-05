# POP3 research corpus

Retrieved on 2026-08-27. These files are inputs to the implementation plan in
`../todo.md`; the RFC Editor and IANA copies are authoritative protocol
references, while the library pages are feature-set comparisons only.

## Standards

- `rfcs/rfc1939.txt` — POP3 base protocol, state machine, commands, mailbox
  transaction rules, multiline framing, APOP, TOP, and UIDL.
- `rfcs/rfc1957.txt` — interoperability observations for deployed POP3
  implementations.
- `rfcs/rfc2449.txt` — extension mechanism, CAPA, formal grammar, pipelining,
  response codes, and the initial capability set.
- `rfcs/rfc2595.txt` — STLS and TLS requirements for POP3.
- `rfcs/rfc3206.txt` — AUTH, SYS/TEMP, and SYS/PERM response codes.
- `rfcs/rfc4013.txt` — historical SASLprep profile named by RFCs 4616 and
  5034.
- `rfcs/rfc4422.txt` — SASL framework used by RFC 5034 hooks.
- `rfcs/rfc4616.txt` — the mandatory-to-implement SASL PLAIN mechanism.
- `rfcs/rfc4648.txt` — strict Base64 encoding used by POP3 AUTH exchanges.
- `rfcs/rfc5034.txt` — POP3 SASL profile and AUTH command.
- `rfcs/rfc5322.txt` — Internet Message Format carried as raw POP3 content.
- `rfcs/rfc6186.txt` — POP3/POP3S service discovery and service naming.
- `rfcs/rfc6856.txt` — UTF8, UTF8=USER, LANG, and the UTF8 response code.
- `rfcs/rfc7817.txt` — email access service identity checks referenced by RFC
  8314.
- `rfcs/rfc8264.txt` and `rfcs/rfc8265.txt` — current PRECIS framework and
  username/password profiles that replace Stringprep/SASLprep for protocols
  that adopt them.
- `rfcs/rfc8314.txt` — current use of implicit TLS for mail access and updated
  security guidance for RFCs 1939 and 2595.
- `rfcs/rfc8996.txt` — deprecation of TLS 1.0 and TLS 1.1.
- `rfcs/rfc9525.txt` — current service identity verification guidance,
  superseding the generic rules on which earlier mail guidance depended.

All RFC text files came from `https://www.rfc-editor.org/rfc/rfcNNNN.txt`.

## Registries and implementation comparisons

- `references/iana-pop3-extension-mechanism.xml` — current IANA POP3
  capabilities and response-code registries, from
  `https://www.iana.org/assignments/pop3-extension-mechanism/pop3-extension-mechanism.xml`.
- `references/mailkit-readme.md` — MailKit feature overview, from its official
  GitHub repository.
- `references/mailkit-pop3-client.html` — MailKit Pop3Client API overview, from
  the official MimeKit/MailKit API site.
- `references/mailkit-starttls-buffer-advisory.html` — MailKit's 2026 advisory
  describing why plaintext input buffers must be discarded at a TLS upgrade
  boundary.
- `references/libetpan.html` — libEtPan overview and API reference.
- `references/libetpan-mailpop3.h` — libEtPan's public low-level POP3 API,
  retained to compare command, TLS, progress, timeout, and logging coverage.
- `references/angus-mail-pop3-package.html` — Angus Mail, the Jakarta Mail
  implementation, POP3 provider documentation and configuration surface.

The comparison artifacts remain subject to their respective upstream licenses;
they are research references and are not source inputs to the library.
