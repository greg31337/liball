# Console examples

Fifty small .NET 10 console apps demonstrating 46 libraries from this collection.
Each app references the local library source through `ProjectReference`; no external
NuGet packages, downloaded fixtures, credentials, or network services are needed.

From the repository root, build and run everything with:

```sh
bash examples/run-all.sh
```

Or build the solution and run an individual app:

```sh
dotnet build examples/Examples.slnx --configuration Release
dotnet run --project examples/CsvJson --configuration Release --no-build
```

The examples use fixed sample inputs, including the viewable images and editable
documents in [data/](data/README.md). Each prints the results of real library
operations. Examples that perform a round trip check that the relevant values
were preserved and stop on failure. There are no separate test projects or test
files.

| App | Local libraries | Demonstration and results to look for |
| --- | --- | --- |
| [BarcodeChecksums](BarcodeChecksums/Program.cs) | `data/barcode` | Calculate EAN/UPC check digits and detect a changed digit. |
| [CacheDemo](CacheDemo/Program.cs) | `dev/cache` | A two-entry LRU cache evicts coffee; repeated lookup calls the loader once. |
| [CalendarSchedule](CalendarSchedule/Program.cs) | `data/time` | Month arithmetic across leap day, a 90-minute duration, and six Monday/Wednesday/Friday meetings. |
| [CalendarTimeZones](CalendarTimeZones/Program.cs) | `data/time` | Show one UTC instant in three explicit fixed-offset time zones. |
| [CborDiagnostics](CborDiagnostics/Program.cs) | `data/cbor` | Encode binary data and integer map keys using CBOR diagnostic notation. |
| [CborMessages](CborMessages/Program.cs) | `data/cbor` | Convert a JSON sensor message to compact binary CBOR, show its bytes, and decode it. |
| [CommandLine](CommandLine/Program.cs) | `dev/args` | Parse aliases, typed values, flags, defaults, and help; greet Ada twice by default. |
| [CookieHeaders](CookieHeaders/Program.cs) | `web/cookies` | Parse Set-Cookie attributes and serialize request cookie pairs. |
| [CountryCurrencies](CountryCurrencies/Program.cs) | `data/countrycodes`, `data/currencycodes` | Look up country codes and currencies, convert minor units, and round CHF cash to 12.45. |
| [CsvJson](CsvJson/Program.cs) | `data/csv`, `data/json` | Read six café sales from CSV, calculate a 40.60 total, and serialize a JSON report. |
| [CsvWriting](CsvWriting/Program.cs) | `data/csv` | Write commas, quotes, and multiline CSV fields and read them back. |
| [DnsPackets](DnsPackets/Program.cs) | `network/dns` | Round-trip a DNS question and construct an IPv4 reverse lookup name. |
| [EmailAddresses](EmailAddresses/Program.cs) | `internet/email` | Parse quoted mailbox names and format an address list. |
| [FakeCustomers](FakeCustomers/Program.cs) | `data/fake` | Generate repeatable synthetic names and reserved-domain email addresses. |
| [FixFieldValues](FixFieldValues/Program.cs) | `finance/fix` | Validate and round-trip FIX quantities, prices, booleans, and timestamps. |
| [GeoJsonRoutes](GeoJsonRoutes/Program.cs) | `data/geo` | Read a GeoJSON route, retain its properties, and export its WKT geometry. |
| [Geometry](Geometry/Program.cs) | `data/geo` | Read WKT for a garden: area 40, perimeter 26, centroid (4, 2.5), and point containment. |
| [GifAnimation](GifAnimation/Program.cs) | `graphics/gif` | Read a viewable progress animation; preserve three frames and their 1,400 ms cycle when saving. |
| [HtmlExtraction](HtmlExtraction/Program.cs) | `web/html` | Convert an HTML article to readable text and Markdown. |
| [HttpMessages](HttpMessages/Program.cs) | `web/http` | Parse and serialize an HTTP/1.1 request without opening a connection. |
| [Identifiers](Identifiers/Program.cs) | `dev/uuid` | Reproducible UUIDv5 names, UUIDv7/ULID creation, parsing, and timestamp extraction. |
| [ImageFormats](ImageFormats/Program.cs) | `graphics/image` | Identify the menu PNG, animated GIF, and QR PNG by their signatures and MIME types. |
| [JsonPointers](JsonPointers/Program.cs) | `data/json` | Navigate arrays and escaped property names using JSON Pointer. |
| [LanguageTags](LanguageTags/Program.cs) | `data/languagecodes` | Look up ISO language codes and inspect BCP 47 tags. |
| [LayeredConfig](LayeredConfig/Program.cs) | `dev/config` | Merge defaults, JSON, a supplied environment map, and an override; final port 9443. |
| [MarkdownReport](MarkdownReport/Program.cs) | `text/markdown` | Render a GitHub-flavored Markdown table, task list, and link to HTML. |
| [Molecules](Molecules/Program.cs) | `chemistry/molecule` | Calculate molecular formulas and masses from small SMILES strings. |
| [PasswordHashing](PasswordHashing/Program.cs) | `security/argon2` | Hash a synthetic password using library defaults; show Match and Mismatch verification results. |
| [PngImages](PngImages/Program.cs) | `graphics/png` | Read a 720 × 480 menu PNG, inspect RGBA pixels, and save it with all pixels preserved. |
| [QrCodes](QrCodes/Program.cs) | `data/qrcode`, `graphics/png` | Decode the workshop URL from a real QR PNG; render the symbol in the terminal and save a scannable PNG. |
| [RandomSampling](RandomSampling/Program.cs) | `dev/random` | Seeded dice histogram, normal samples, saved generator state, and a shuffled team. |
| [RtpPackets](RtpPackets/Program.cs) | `network/rtp` | Encode an RTP payload and inspect its header after decoding. |
| [SessionDescriptions](SessionDescriptions/Program.cs) | `network/sdp` | Read an audio session offer and inspect its effective direction. |
| [SipMessages](SipMessages/Program.cs) | `network/sip` | Parse and serialize a SIP OPTIONS request. |
| [Spreadsheet](Spreadsheet/Program.cs) | `document/xlsx` | Load typed expense records from JSON, create and reopen a workbook, and calculate `SUM(B2:B4)` as 73.50. |
| [SqlQueries](SqlQueries/Program.cs) | `dev/sql` | Parse and format a grouped sales query without connecting to a database. |
| [StructuredLogging](StructuredLogging/Program.cs) | `dev/log` | Three structured log messages with an order ID; filter the debug message. |
| [SvgPaths](SvgPaths/Program.cs) | `graphics/svg` | Measure an SVG rectangle path and locate points along its perimeter. |
| [TemplateReport](TemplateReport/Program.cs) | `text/template` | Load a typed JSON checklist and render a template file with a filter, a loop, conditionals, and an included footer. |
| [TftpPackets](TftpPackets/Program.cs) | `internet/tftp` | Round-trip TFTP read, data, and acknowledgement packets. |
| [TokenEncoding](TokenEncoding/Program.cs) | `security/token` | Encode UTF-8 as Base64URL, hexadecimal, and PEM; verify the Base64URL round trip. |
| [UnicodeText](UnicodeText/Program.cs) | `text/unicode` | Normalize accents, fold case, and split four grapheme clusters without breaking emoji. |
| [UnitConversions](UnitConversions/Program.cs) | `data/units` | Convert miles, temperatures, and bytes; derive speed and a 784.80 N force. |
| [UriWorkbench](UriWorkbench/Program.cs) | `web/uri` | Resolve references and encode query parameters. |
| [VersionRanges](VersionRanges/Program.cs) | `dev/version` | Sort semantic versions, apply `^1.3.0`, and select 1.10.0 while excluding prereleases. |
| [WebSocketFrames](WebSocketFrames/Program.cs) | `web/websocket` | Encode a masked client text frame and decode it at the server. |
| [WeightedSampling](WeightedSampling/Program.cs) | `dev/random` | Sample weighted categories reproducibly and update their probabilities. |
| [WhoisRecords](WhoisRecords/Program.cs) | `internet/whois` | Extract named fields from a synthetic WHOIS response. |
| [XmlDocuments](XmlDocuments/Program.cs) | `data/xml` | Read an XML catalog and preserve escaped text during serialization. |
| [YamlSettings](YamlSettings/Program.cs) | `data/yaml` | Load service settings, change replicas from two to three, emit YAML, and reload it. |

## Inputs and generated files

Try the command-line example with your own arguments:

```sh
dotnet run --project examples/CommandLine -- --name Grace --repeat 3
dotnet run --project examples/CommandLine -- --help
```

Shared [sample data](data/README.md) is copied beside each executable during build
and publish. Input paths use the executable directory, so the apps also work when
launched from a different working directory. Edit files under `examples/data/`
and rebuild to try your own inputs.

Four apps write files you can open:

| App | Generated file, relative to the working directory |
| --- | --- |
| `PngImages` | `artifacts/PngImages/cafe-menu.png` |
| `GifAnimation` | `artifacts/GifAnimation/loading-progress.gif` |
| `QrCodes` | `artifacts/QrCodes/workshop-qr.png` |
| `Spreadsheet` | `artifacts/Spreadsheet/expenses.xlsx` |

The runner uses `examples/` as its working directory, so these outputs are under
`examples/artifacts/`. Re-running replaces the generated files. `Spreadsheet`
also accepts a different output directory as its first argument:

```sh
dotnet run --project examples/Spreadsheet -- artifacts/my-workbooks
```

The remaining apps only print output. The existing repository `.gitignore` excludes
`bin/`, `obj/`, and `artifacts/` at every depth.

Protocol examples encode and decode synthetic messages, including the editable
files in `data/protocols/`. They do not open sockets, send email, or query
DNS/WHOIS servers. Image examples read actual PNG and GIF files and print their
dimensions, pixels, or animation timing.
`TokenEncoding` demonstrates reversible encodings, not token authentication.
`CalendarTimeZones` uses explicit fixed offsets; it does not apply daylight-saving
rules. `GeoJsonRoutes` preserves longitude/latitude coordinates without treating
degrees as planar distance units.

UUIDv7/ULID random portions, password salts and hashes, and log timestamps vary
between runs. Random sampling and synthetic customer generation use fixed seeds.
The country, currency, and language examples use the libraries' bundled catalog
data. The QR display needs a monospaced terminal with Unicode block-character
support; image decoding and PNG export work independently of the
terminal rendering. Calendar recurrences use local dates without a time zone;
the separate meeting-duration example uses UTC. Geometry coordinates are planar
metres. Password hashing uses the library's default memory and iteration settings,
so it may take a few seconds.

## Source layout

Each app keeps its demonstration in `Program.cs`, with explicit blocks, readable
initializers, and short comments explaining the important library behavior.
`Shared/SampleFiles.cs` handles the common input and output paths, and
`.editorconfig` records the formatting conventions.
