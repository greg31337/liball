# Specification Baseline

Retrieved on 2026-08-24. These local copies define or describe the external
formats and behavioral conventions used by the library. JSON, UTF-8, and XML
have normative specifications. INI and dotenv do not have a single universal
standard, so the referenced documents are representative dialect descriptions;
the library's accepted dialect must be fixed by its own public contract and
tests.

- `rfc8259-json.txt` — IETF JSON data interchange syntax and interoperability
  requirements. Source: https://www.rfc-editor.org/rfc/rfc8259.txt
- `rfc3629-utf8.txt` — IETF UTF-8 definition and malformed-sequence rules.
  Source: https://www.rfc-editor.org/rfc/rfc3629.txt
- `w3c-xml-1.0-fifth-edition.html` — W3C XML 1.0 grammar, well-formedness,
  encoding, element, attribute, and entity rules. Source:
  https://www.w3.org/TR/xml/
- `java-properties.html` — Java `Properties.load` line structure, separators,
  comments, continuation, escaping, and encoding behavior. Source:
  https://docs.oracle.com/en/java/javase/26/docs/api/java.base/java/util/Properties.html
- `python-configparser-ini.html` — representative INI sections, delimiters,
  comments, duplicate handling, and interpolation behavior. Source:
  https://docs.python.org/3.14/library/configparser.html
- `dotenvx-env-file.html` — representative dotenv keys, quoting, comments,
  interpolation, and multiline value behavior. Source:
  https://dotenvx.com/docs/env-file/
- `dotnet-configuration-providers.html` — .NET configuration precedence and
  file, environment, command-line, key-per-file, and memory-provider behavior.
  Source: https://learn.microsoft.com/en-us/dotnet/core/extensions/configuration-providers
- `hocon.md` — immutable snapshots, fallback merge, paths, substitutions,
  includes, duration units, and byte-size units used as configuration design
  precedents. Source:
  https://raw.githubusercontent.com/lightbend/config/main/HOCON.md
- `posix-environment-variables.html` — POSIX environment variable model and
  portable naming constraints. Source:
  https://pubs.opengroup.org/onlinepubs/9799919799/basedefs/V1_chap08.html
- `gnu-command-line-interfaces.html` — command-line short and long option
  conventions. Source:
  https://www.gnu.org/prep/standards/html_node/Command_002dLine-Interfaces.html
- `dotnet-filesystemwatcher.html` — built-in watcher semantics, duplicate event
  behavior, buffer overflow, and missed-event caveats. Source:
  https://learn.microsoft.com/en-us/dotnet/api/system.io.filesystemwatcher?view=net-10.0
- `dotnet-timespan-parse.html` — built-in invariant and culture-sensitive
  `TimeSpan` parsing rules. Source:
  https://learn.microsoft.com/en-us/dotnet/fundamentals/runtime-libraries/system-timespan-parse
