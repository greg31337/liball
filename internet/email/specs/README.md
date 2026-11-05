# Specification source index

Downloaded on 2026-08-24 from authoritative publishers. RFC text files use
`https://www.rfc-editor.org/rfc/rfcNNNN.txt`.
Matching `errata/rfcNNNN-errata.html` files are snapshots of the RFC Editor's
errata search results for each downloaded RFC.

## RFCs

- Message and MIME framework: 1847, 2045, 2046, 2047, 2049, 2183, 2231,
  2369, 2387, 2392, 2397, 2557, 2919, 3156, 3274, 3462, 3463, 3464,
  3676, 3834, 4155, 4648, 5321, 5322, 6531, 6532, 6533, 6854, 7208,
  8058, and 8098.
- Calendar mail: 5545 and 6047.
- Internationalized domain names: 5890, 5891, 5892, 5893, 5894, and 5895.
- Feedback and message authentication: 5965, 6376, 6591, 6650, 8301,
  8463, 8601, 8616, and 8617.
- CMS, S/MIME, and OpenPGP: 3370, 3565, 4134, 4880, 5084, 5652, 5753,
  5754, 8017, 8550, 8551, 9580, 9688, and 9788. RFC 4880 is retained
  as a legacy interoperability reference; RFC 9580 is the current OpenPGP
  baseline.

## Live registries

The `registries/` snapshots come from IANA's assignments for message headers,
media types, character sets, content dispositions, transfer encodings, and
DKIM parameters at `https://www.iana.org/assignments/`.

## Other authoritative descriptions

- `vendor/MS-OXTNEF.pdf` is revision 15.0 of Microsoft's MS-OXTNEF Open
  Specification, published 2025-05-20.
- `vendor/maildir.html` is D. J. Bernstein's Maildir format description from
  `https://cr.yp.to/proto/maildir.html`.
- `web/whatwg-html.html` and `web/whatwg-url.html` are retrieval-date
  snapshots of the WHATWG HTML and URL Living Standards used by the bounded
  HTML-processing and URL-policy features.

## Comparative library descriptions

The `libraries/` directory contains retrieval-date snapshots of the official
MimeKit, Jakarta Mail, Python `email`, Nodemailer, Apache Mime4j, Go `net/mail`,
and .NET `MailMessage` pages used to compare feature coverage. These pages are
informative references; the RFCs and registries above remain authoritative for
protocol behavior.
