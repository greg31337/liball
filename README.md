# liball

A collection of **67 C#/.NET library projects** for data formats, documents,
graphics, protocols, and application utilities, with **50 console examples**.
Follow the library links below for their documentation, APIs, and supported features.

## Libraries

| Area | Libraries |
| --- | --- |
| Applications | [htmlgui](app/htmlgui/) |
| Chemistry | [molecule](chemistry/molecule/) |
| Data | [asn1](data/asn1/), [barcode](data/barcode/), [cbor](data/cbor/), [countrycodes](data/countrycodes/), [csv](data/csv/), [currencycodes](data/currencycodes/), [fake](data/fake/), [geo](data/geo/), [json](data/json/), [languagecodes](data/languagecodes/), [qrcode](data/qrcode/), [time](data/time/), [units](data/units/), [xml](data/xml/), [yaml](data/yaml/) |
| Graphics data | [gltf](data/graphics/gltf/), [icc](data/graphics/icc/) |
| Development | [args](dev/args/), [cache](dev/cache/), [config](dev/config/), [log](dev/log/), [random](dev/random/), [sql](dev/sql/), [uuid](dev/uuid/), [version](dev/version/) |
| Documents | [docx](document/docx/), [odf](document/odf/), [pdf](document/pdf/), [pptx](document/pptx/), [xlsx](document/xlsx/) |
| Finance | [fix](finance/fix/), [iso20022](finance/iso20022/) |
| Graphics | [avif](graphics/avif/), [chart](graphics/chart/), [gif](graphics/gif/), [image](graphics/image/), [jpeg](graphics/jpeg/), [png](graphics/png/), [raw](graphics/raw/), [svg](graphics/svg/), [tiff](graphics/tiff/), [ttf](graphics/ttf/), [webp](graphics/webp/) |
| Internet | [email](internet/email/), [ftp](internet/ftp/), [jmap](internet/jmap/), [pop3](internet/pop3/), [tftp](internet/tftp/), [whois](internet/whois/) |
| Legal | [uspto](legal/uspto/) |
| Networking | [diameter](network/diameter/), [dns](network/dns/), [rtp](network/rtp/), [sdp](network/sdp/), [sip](network/sip/) |
| Security | [argon2](security/argon2/), [token](security/token/) |
| Text | [markdown](text/markdown/), [template](text/template/), [unicode](text/unicode/) |
| Web | [cookies](web/cookies/), [html](web/html/), [http](web/http/), [uri](web/uri/), [websocket](web/websocket/) |

## Examples

The examples reference local library source and include [sample data](examples/data/README.md)
such as a menu PNG, QR code, animated GIF, sales CSV, and expense records.
See the [example guide](examples/README.md) for descriptions and expected output.

| Area | Console apps |
| --- | --- |
| Data formats | [BarcodeChecksums](examples/BarcodeChecksums/), [CborDiagnostics](examples/CborDiagnostics/), [CborMessages](examples/CborMessages/), [CsvJson](examples/CsvJson/), [CsvWriting](examples/CsvWriting/), [JsonPointers](examples/JsonPointers/), [XmlDocuments](examples/XmlDocuments/), [YamlSettings](examples/YamlSettings/) |
| Catalogs and units | [CountryCurrencies](examples/CountryCurrencies/), [LanguageTags](examples/LanguageTags/), [UnitConversions](examples/UnitConversions/) |
| Dates and times | [CalendarSchedule](examples/CalendarSchedule/), [CalendarTimeZones](examples/CalendarTimeZones/) |
| Development | [CacheDemo](examples/CacheDemo/), [CommandLine](examples/CommandLine/), [FakeCustomers](examples/FakeCustomers/), [Identifiers](examples/Identifiers/), [LayeredConfig](examples/LayeredConfig/), [RandomSampling](examples/RandomSampling/), [SqlQueries](examples/SqlQueries/), [StructuredLogging](examples/StructuredLogging/), [VersionRanges](examples/VersionRanges/), [WeightedSampling](examples/WeightedSampling/) |
| Documents and text | [HtmlExtraction](examples/HtmlExtraction/), [MarkdownReport](examples/MarkdownReport/), [Spreadsheet](examples/Spreadsheet/), [TemplateReport](examples/TemplateReport/), [UnicodeText](examples/UnicodeText/) |
| Graphics and geometry | [GeoJsonRoutes](examples/GeoJsonRoutes/), [Geometry](examples/Geometry/), [GifAnimation](examples/GifAnimation/), [ImageFormats](examples/ImageFormats/), [PngImages](examples/PngImages/), [QrCodes](examples/QrCodes/), [SvgPaths](examples/SvgPaths/) |
| Protocols and web | [CookieHeaders](examples/CookieHeaders/), [DnsPackets](examples/DnsPackets/), [EmailAddresses](examples/EmailAddresses/), [HttpMessages](examples/HttpMessages/), [RtpPackets](examples/RtpPackets/), [SessionDescriptions](examples/SessionDescriptions/), [SipMessages](examples/SipMessages/), [TftpPackets](examples/TftpPackets/), [UriWorkbench](examples/UriWorkbench/), [WebSocketFrames](examples/WebSocketFrames/), [WhoisRecords](examples/WhoisRecords/) |
| Security | [PasswordHashing](examples/PasswordHashing/), [TokenEncoding](examples/TokenEncoding/) |
| Chemistry and finance | [Molecules](examples/Molecules/), [FixFieldValues](examples/FixFieldValues/) |

Additional TIFF samples: [TiffMetadataDump](graphics/tiff/samples/TiffMetadataDump/),
[TiffTileExtract](graphics/tiff/samples/TiffTileExtract/), and
[TiffTranscode](graphics/tiff/samples/TiffTranscode/).

With the **.NET 10 SDK**, run all 50 console examples from the repository root:

```sh
bash examples/run-all.sh
```

Or run one example:

```sh
dotnet run --project examples/QrCodes --configuration Release
```
