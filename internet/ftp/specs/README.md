# FTP/FTPS planning sources

Retrieved on 2026-08-27 from the authoritative locations linked below. These
files are design inputs for `todo.md`; they are not runtime dependencies.

## Protocol specifications

- `rfcs/rfc854.txt`: [Telnet Protocol](https://www.rfc-editor.org/rfc/rfc854.txt), needed for FTP control-channel framing and Telnet synchronization.
- `rfcs/rfc775.txt`, `rfc1545.txt`, and `rfc1639.txt`: historical specifications for the deployed X-command aliases and obsolete LPRT/LPSV commands catalogued by the IANA registry.
- `rfcs/rfc959.txt`: [File Transfer Protocol](https://www.rfc-editor.org/rfc/rfc959.txt), the base FTP standard.
- `rfcs/rfc1123.txt`: [Requirements for Internet Hosts — Application and Support](https://www.rfc-editor.org/rfc/rfc1123.txt), including the modernized FTP requirements in section 4.1.
- `rfcs/rfc1579.txt`: [Firewall-Friendly FTP](https://www.rfc-editor.org/rfc/rfc1579.txt), passive-mode guidance.
- `rfcs/rfc2228.txt`: [FTP Security Extensions](https://www.rfc-editor.org/rfc/rfc2228.txt), the security-command framework used by FTP-TLS.
- `rfcs/rfc2389.txt`: [FTP feature negotiation](https://www.rfc-editor.org/rfc/rfc2389.txt), defining FEAT and OPTS.
- `rfcs/rfc2428.txt`: [FTP Extensions for IPv6 and NATs](https://www.rfc-editor.org/rfc/rfc2428.txt), defining EPRT and EPSV.
- `rfcs/rfc2577.txt`: [FTP Security Considerations](https://www.rfc-editor.org/rfc/rfc2577.txt), covering bounce, port theft, password, privacy, and denial-of-service risks.
- `rfcs/rfc2640.txt`: [Internationalization of FTP](https://www.rfc-editor.org/rfc/rfc2640.txt), defining UTF-8 and LANG behavior.
- `rfcs/rfc3659.txt`: [Extensions to FTP](https://www.rfc-editor.org/rfc/rfc3659.txt), defining MDTM, SIZE, REST STREAM, TVFS, MLST, and MLSD.
- `rfcs/rfc4217.txt`: [Securing FTP with TLS](https://www.rfc-editor.org/rfc/rfc4217.txt), defining explicit FTP-TLS behavior and its interaction with AUTH, PBSZ, PROT, and CCC.
- `rfcs/rfc5797.txt`: [FTP Command and Extension Registry](https://www.rfc-editor.org/rfc/rfc5797.txt), defining the command registry and conformance fields.
- `rfcs/rfc6384.txt`: [An FTP Application Layer Gateway for IPv6-to-IPv4 Translation](https://www.rfc-editor.org/rfc/rfc6384.txt), retained for the registry's optional ALGS command.
- `rfcs/rfc7151.txt`: [FTP HOST Command for Virtual Hosts](https://www.rfc-editor.org/rfc/rfc7151.txt), defining virtual-host selection before authentication.
- `errata/`: downloaded RFC Editor errata pages for the principal protocol RFCs.
- `drafts/draft-dd-pret-00.txt`: [PRET command Internet-Draft](https://www.ietf.org/archive/id/draft-dd-pret-00.txt), retained only as a deployed compatibility extension; it is not an RFC.
- `drafts/draft-preston-ftpext-deflate-04.txt`, `draft-bryan-ftpext-hash-02.txt`, and `draft-somers-ftp-mfxx-04.txt`: expired MODE Z, HASH, and MFMT-family drafts retained to define opt-in interoperability behavior, not standards claims.

## Registries

- `registries/ftp-commands-extensions.txt` and `.xml`: [IANA FTP Commands and Extensions registry](https://www.iana.org/assignments/ftp-commands-extensions/), used as the command inventory and conformance source.

## Implementation feature references

- `references/libcurl-ftp.html`, `libcurl-ftps.html`, and `libcurl-options.html`: official curl/libcurl FTP, FTPS, and option references.
- `references/apache-ftpclient.html` and `apache-ftpsclient.html`: official Apache Commons Net FTPClient and FTPSClient API references.
- `references/python-ftplib.html`: official Python `ftplib` and `FTP_TLS` API reference.
- `references/fluentftp-readme.md` and `fluentftp-support.md`: official FluentFTP overview and command-support table.
- `references/apache-ftpserver-features.html`: official Apache FtpServer feature overview.
- `references/pyftpdlib-api.html`: official pyftpdlib server API reference.
- `references/fubar-ftpserver-readme.md`: official FubarDevelopment/FtpServer overview.
- `references/dotnet-sslstream.html` and `dotnet-sslstream-best-practices.html`: official .NET `SslStream` API and TLS guidance.
