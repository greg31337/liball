using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using iso20022;

namespace iso20022.Tests;

public class ExternalCodeSetXlsxImporterTests
{
    [Test]
    public void RawWorkbookNormalizesSharedInlineNumericDateMappingRemovalAndBlankCells()
    {
        byte[] workbook = CreateXlsxPackage(false, false, false);
        ExternalCodeImportRequest request = CreateRequest("2026-Q1", "fixture.xlsx");
        request.Artifact.PublicationDate = new IsoPartialDateValue
        {
            Kind = PartialDateKind.YearMonth,
            Year = 2026,
            Month = 5,
            LexicalValue = "2026-05"
        };
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();

        ExternalCodeImportResult result = importer.ImportXlsxStream(
            new MemoryStream(workbook),
            request);
        ExternalCodeCatalog catalog = result.Catalog ?? throw new AssertionException(result.ErrorMessage);
        ExternalCodeSet codeSet = catalog.CodeSets.Single();
        ExternalCodeValue active = codeSet.Values[0];
        ExternalCodeValue next = codeSet.Values[1];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.SourceFormat, Is.EqualTo("XLSX"));
            Assert.That(result.SourceEntryName, Is.EqualTo("fixture.xlsx"));
            Assert.That(result.Artifact.MediaType, Is.EqualTo(Iso20022MediaTypes.OpenXmlWorkbook));
            Assert.That(result.Artifact.Hash.Value, Is.EqualTo(SHA256.HashData(workbook)));
            Assert.That(catalog.Edition, Is.EqualTo("2026-Q1"));
            Assert.That(catalog.PublicationDate?.LexicalValue, Is.EqualTo("2026-05"));
            Assert.That(catalog.SchemaIdentifier,
                Is.EqualTo("http://schemas.openxmlformats.org/spreadsheetml/2006/main"));
            Assert.That(catalog.SchemaVersion, Is.EqualTo("1"));
            Assert.That(catalog.SourceFormat, Is.EqualTo("XLSX"));
            Assert.That(codeSet.Identifier, Is.EqualTo("ExternalStatus1Code"));
            Assert.That(codeSet.Name, Is.EqualTo("ExternalStatus1Code"));
            Assert.That(codeSet.Definition, Is.EqualTo("Line 1\rLine 2"));
            Assert.That(codeSet.RegistrationStatus, Is.EqualTo("Registered"));
            Assert.That(active.Value, Is.EqualTo("ACTV"));
            Assert.That(active.Name, Is.EqualTo("Active"));
            Assert.That(active.Definition, Is.EqualTo("The code is active."));
            Assert.That(active.Requester, Is.Empty);
            Assert.That(active.RegistrationStatus, Is.EqualTo("Obsolete [2027-01-31]"));
            Assert.That(active.LastUpdatedDate?.LexicalValue, Is.EqualTo("2026-05-11"));
            Assert.That(active.CreationDate?.LexicalValue, Is.EqualTo("2026-05-01"));
            Assert.That(active.RemovalDate?.LexicalValue, Is.EqualTo("2027-01-31"));
            Assert.That(active.ReplacedBy, Is.EqualTo("NEXT"));
            Assert.That(active.MappingStandards, Is.EqualTo(new[] { "ISO 15022", "ISO 8583" }));
            Assert.That(active.MappingValues, Is.EqualTo(new[] { "A1", "B2" }));
            Assert.That(active.AdditionalInformation, Is.EqualTo("Literal _x000D_"));
            Assert.That(next.Name, Is.Empty);
            Assert.That(next.Requester, Is.Empty);
            Assert.That(next.LastUpdatedDate, Is.Null);
            Assert.That(next.CreationDate, Is.Null);
            Assert.That(next.MappingStandards, Is.Empty);
            Assert.That(result.DiagnosticCodes,
                Does.Contain("external-code.xlsx.validity_unavailable"));
        });
    }

    [Test]
    public void OuterZipSelectsWorkbookInfersQuarterAndRetainsOuterArtifactHash()
    {
        byte[] workbook = CreateXlsxPackage(false, false, false);
        byte[] archive = CreateOuterZip(new[]
        {
            ("1Q2026_externalcodesets_v1.xlsx", workbook),
            ("readme.txt", Encoding.UTF8.GetBytes("fixture"))
        });
        ExternalCodeImportRequest request = CreateRequest(string.Empty, "external-codes.zip");
        request.Artifact.Coordinate.Edition = string.Empty;
        request.Artifact.HashAlgorithm = "SHA-256";
        request.Artifact.Hash.Value = SHA256.HashData(archive);
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();

        ExternalCodeImportResult result = importer.ImportXlsxStream(
            new MemoryStream(archive),
            request);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.Catalog?.Edition, Is.EqualTo("2026-Q1"));
            Assert.That(result.SourceEntryName, Is.EqualTo("1Q2026_externalcodesets_v1.xlsx"));
            Assert.That(result.Artifact.MediaType, Is.EqualTo(Iso20022MediaTypes.Zip));
            Assert.That(result.Artifact.Hash.Value, Is.EqualTo(SHA256.HashData(archive)));
            Assert.That(result.Catalog?.Artifact, Is.SameAs(result.Artifact));
        });
    }

    [Test]
    public void WorkbookReportsAmbiguousOuterArchivesInvalidSharedIndexesAndUnsafePackagePaths()
    {
        byte[] workbook = CreateXlsxPackage(false, false, false);
        byte[] ambiguous = CreateOuterZip(new[]
        {
            ("first.xlsx", workbook),
            ("second.xlsx", workbook)
        });
        byte[] invalidSharedIndex = CreateXlsxPackage(true, false, false);
        byte[] unsafePackage = CreateXlsxPackage(false, true, false);
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();

        ExternalCodeImportResult ambiguousResult = importer.ImportXlsxStream(
            new MemoryStream(ambiguous),
            CreateRequest("2026-Q1", "ambiguous.zip"));
        ExternalCodeImportResult sharedResult = importer.ImportXlsxStream(
            new MemoryStream(invalidSharedIndex),
            CreateRequest("2026-Q1", "shared.xlsx"));
        ExternalCodeImportResult pathResult = importer.ImportXlsxStream(
            new MemoryStream(unsafePackage),
            CreateRequest("2026-Q1", "unsafe.xlsx"));

        Assert.Multiple(() =>
        {
            Assert.That(ambiguousResult.ErrorCode,
                Is.EqualTo("external-code.archive.entry_ambiguous"));
            Assert.That(sharedResult.ErrorCode,
                Is.EqualTo("external-code.xlsx.shared_string_index"));
            Assert.That(pathResult.ErrorCode,
                Is.EqualTo("external-code.xlsx.entry_path"));
            Assert.That(ambiguousResult.Catalog, Is.Null);
            Assert.That(sharedResult.Catalog, Is.Null);
            Assert.That(pathResult.Catalog, Is.Null);
        });
    }

    [Test]
    public void WorkbookHonorsPackageLimitsCancellationAndSecureXmlDefaults()
    {
        byte[] workbook = CreateXlsxPackage(false, false, false);
        byte[] dtdWorkbook = CreateXlsxPackage(false, false, true);
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();
        ExternalCodeImportRequest limitRequest = CreateRequest("2026-Q1", "limit.xlsx");
        limitRequest.MaxArchiveEntryCount = 2;
        ExternalCodeImportRequest cancellationRequest = CreateRequest("2026-Q1", "cancelled.xlsx");
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        cancellationRequest.CancellationToken = cancellation.Token;

        ExternalCodeImportResult limited = importer.ImportXlsxStream(
            new MemoryStream(workbook),
            limitRequest);

        Assert.Multiple(() =>
        {
            Assert.That(limited.ErrorCode, Is.EqualTo("external-code.xlsx.entry_count"));
            Assert.That(
                () => importer.ImportXlsxStream(new MemoryStream(workbook), cancellationRequest),
                Throws.InstanceOf<OperationCanceledException>());
            Assert.That(
                () => importer.ImportXlsxStream(
                    new MemoryStream(dtdWorkbook),
                    CreateRequest("2026-Q1", "dtd.xlsx")),
                Throws.InstanceOf<XmlException>());
        });
    }

    [Test]
    public void BundledOfficialWorkbookImportsAllDefinitionsValuesAndLifecycleMetadata()
    {
        string archivePath = Path.GetFullPath(Path.Combine(
            TestContext.CurrentContext.TestDirectory,
            "../../../../specs/iso20022/ExternalCodeSets_XLSX_2026-05.zip"));
        byte[] archive = File.ReadAllBytes(archivePath);
        ExternalCodeImportRequest request = CreateRequest("2026-Q1", archivePath);
        request.Artifact.HashAlgorithm = "SHA-256";
        request.Artifact.Hash.Value = Convert.FromHexString(
            "9ae77439784d72dcedab2d206e213080f1d153e44989d314fcc8415b73b92cd4");
        IExternalCodeSetImporter importer = new ExternalCodeSetImporter();

        ExternalCodeImportResult result = importer.ImportXlsxStream(
            new MemoryStream(archive),
            request);
        ExternalCodeCatalog catalog = result.Catalog ?? throw new AssertionException(result.ErrorMessage);
        ExternalCodeSet accepted = catalog.CodeSets.Single(
            set => set.Identifier == "ExternalAcceptedReason1Code");
        ExternalCodeSet cashClearing = catalog.CodeSets.Single(
            set => set.Identifier == "ExternalCashClearingSystem1Code");
        ExternalCodeValue canadianLvts = cashClearing.Values.Single(value => value.Value == "CAD");

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True, result.ErrorMessage);
            Assert.That(result.SourceEntryName, Is.EqualTo("1Q2026_externalcodesets_v1.xlsx"));
            Assert.That(result.Artifact.Hash.LexicalValue, Is.EqualTo(
                "9AE77439784D72DCEDAB2D206E213080F1D153E44989D314FCC8415B73B92CD4"));
            Assert.That(catalog.CodeSets, Has.Count.EqualTo(158));
            Assert.That(catalog.CodeSets.Sum(set => set.Values.Count), Is.EqualTo(3246));
            Assert.That(accepted.RegistrationStatus, Is.EqualTo("Registered"));
            Assert.That(accepted.Definition, Does.StartWith("Specifies the reason for an accepted status."));
            Assert.That(accepted.Definition, Does.Not.Contain("_x000D_"));
            Assert.That(accepted.Values.Select(value => value.Value),
                Is.EqualTo(new[] { "ADEA", "NSTP", "SMPG" }));
            Assert.That(accepted.Values[0].Name, Is.EqualTo("AccountServicerDeadlineMissed"));
            Assert.That(accepted.Values[0].CreationDate?.LexicalValue, Is.EqualTo("2016-05-01"));
            Assert.That(accepted.Values[0].LastUpdatedDate?.LexicalValue, Is.EqualTo("2016-05-11"));
            Assert.That(canadianLvts.ReplacedBy, Is.EqualTo("LVT"));
            Assert.That(canadianLvts.AdditionalInformation, Is.EqualTo("Do not use."));
            Assert.That(result.DiagnosticCodes,
                Does.Contain("external-code.xlsx.validity_unavailable"));
        });
    }

    private static ExternalCodeImportRequest CreateRequest(string edition, string sourceName)
    {
        return new ExternalCodeImportRequest
        {
            SourceName = sourceName,
            Edition = edition,
            Artifact = new ArtifactManifest
            {
                Coordinate = new ArtifactCoordinate
                {
                    Kind = ArtifactKind.ExternalCodeSet,
                    Edition = edition,
                    Identifier = "external-code-sets",
                    Version = "1"
                },
                OriginUri = "https://example.test/external-code-sets",
                IsApproved = true
            }
        };
    }

    internal static byte[] CreateXlsxPackage(
        bool invalidSharedIndex,
        bool unsafeEntry,
        bool dtdSharedStrings)
    {
        List<string> strings = new List<string>
        {
            "Code Set", "Code Value", "Code Name", "Code Definition", "Requester",
            "Status", "Last Update", "Creation Date", "Replaced By",
            "MappingStandards 1", "MappingValue 1", "MappingStandards 2",
            "MappingValue 2", "Additional Information", "Code Set Definition",
            "ExternalStatus1Code", "ACTV", "NEXT", "The code is active.",
            "The next code.", "Obsolete [2027-01-31]", "Registered", "2026-05-01",
            "ISO 15022", "A1", "ISO 8583", "B2", "Literal _x005F__x000D_",
            "Line 1_x000D_Line 2"
        };
        string sharedStrings = CreateSharedStringsXml(strings, dtdSharedStrings);
        string allCodes = CreateAllCodesWorksheet(strings, invalidSharedIndex);
        string definitions = CreateDefinitionsWorksheet(strings);
        const string workbook = """
            <?xml version="1.0" encoding="UTF-8"?>
            <workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"
              xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships">
              <workbookPr/>
              <sheets>
                <sheet name="AllCodeSets" sheetId="1" r:id="rId1"/>
                <sheet name="CodeSetsDefinition" sheetId="2" r:id="rId2"/>
              </sheets>
            </workbook>
            """;
        const string relationships = """
            <?xml version="1.0" encoding="UTF-8"?>
            <Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships">
              <Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/>
              <Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet2.xml"/>
              <Relationship Id="rId3" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings" Target="sharedStrings.xml"/>
            </Relationships>
            """;
        const string contentTypes = """
            <?xml version="1.0" encoding="UTF-8"?>
            <Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types">
              <Default Extension="xml" ContentType="application/xml"/>
            </Types>
            """;

        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            AddPackageEntry(archive, "[Content_Types].xml", contentTypes);
            AddPackageEntry(archive, "xl/workbook.xml", workbook);
            AddPackageEntry(archive, "xl/_rels/workbook.xml.rels", relationships);
            AddPackageEntry(archive, "xl/sharedStrings.xml", sharedStrings);
            AddPackageEntry(archive, "xl/worksheets/sheet1.xml", allCodes);
            AddPackageEntry(archive, "xl/worksheets/sheet2.xml", definitions);
            if (unsafeEntry)
            {
                AddPackageEntry(archive, "../unsafe.xml", "<unsafe/>");
            }
        }

        return output.ToArray();
    }

    private static string CreateSharedStringsXml(List<string> strings, bool withDtd)
    {
        StringBuilder xml = new StringBuilder();
        xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        if (withDtd)
        {
            xml.Append("<!DOCTYPE sst [<!ENTITY external SYSTEM \"file:///etc/passwd\">]>");
        }

        xml.Append("<sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">");
        foreach (string value in strings)
        {
            xml.Append("<si><t>");
            xml.Append(SecurityElement.Escape(value));
            xml.Append("</t></si>");
        }

        xml.Append("</sst>");
        return xml.ToString();
    }

    private static string CreateAllCodesWorksheet(List<string> strings, bool invalidSharedIndex)
    {
        string[] headers = new string[]
        {
            "Code Set", "Code Value", "Code Name", "Code Definition", "Requester",
            "Status", "Last Update", "Creation Date", "Replaced By",
            "MappingStandards 1", "MappingValue 1", "MappingStandards 2",
            "MappingValue 2", "Additional Information"
        };
        StringBuilder xml = new StringBuilder();
        xml.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
        xml.Append("<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>");
        xml.Append("<row r=\"1\">");
        int index = 0;
        while (index < headers.Length)
        {
            xml.Append(CreateSharedCell(GetColumnName(index) + "1", strings.IndexOf(headers[index])));
            index++;
        }

        xml.Append("</row><row r=\"2\">");
        xml.Append(CreateSharedCell("A2", strings.IndexOf("ExternalStatus1Code")));
        xml.Append(CreateSharedCell("B2", invalidSharedIndex ? 99999 : strings.IndexOf("ACTV")));
        xml.Append("<c r=\"C2\" t=\"inlineStr\"><is><r><t>Act</t></r><r><t>ive</t></r></is></c>");
        xml.Append(CreateSharedCell("D2", strings.IndexOf("The code is active.")));
        xml.Append("<c r=\"E2\"/>");
        xml.Append(CreateSharedCell("F2", strings.IndexOf("Obsolete [2027-01-31]")));
        xml.Append("<c r=\"G2\" t=\"n\"><v>46153</v></c>");
        xml.Append(CreateSharedCell("H2", strings.IndexOf("2026-05-01")));
        xml.Append(CreateSharedCell("I2", strings.IndexOf("NEXT")));
        xml.Append(CreateSharedCell("J2", strings.IndexOf("ISO 15022")));
        xml.Append(CreateSharedCell("K2", strings.IndexOf("A1")));
        xml.Append(CreateSharedCell("L2", strings.IndexOf("ISO 8583")));
        xml.Append(CreateSharedCell("M2", strings.IndexOf("B2")));
        xml.Append(CreateSharedCell("N2", strings.IndexOf("Literal _x005F__x000D_")));
        xml.Append("</row><row r=\"3\">");
        xml.Append(CreateSharedCell("A3", strings.IndexOf("ExternalStatus1Code")));
        xml.Append(CreateSharedCell("B3", strings.IndexOf("NEXT")));
        xml.Append("<c r=\"C3\"/>");
        xml.Append(CreateSharedCell("D3", strings.IndexOf("The next code.")));
        xml.Append(CreateSharedCell("F3", strings.IndexOf("Registered")));
        xml.Append("<c r=\"G3\"/><c r=\"H3\"/>");
        xml.Append("</row></sheetData></worksheet>");
        return xml.ToString();
    }

    private static string CreateDefinitionsWorksheet(List<string> strings)
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
            "<worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData>" +
            "<row r=\"1\">" +
            CreateSharedCell("A1", strings.IndexOf("Code Set")) +
            CreateSharedCell("B1", strings.IndexOf("Code Set Definition")) +
            CreateSharedCell("C1", strings.IndexOf("Status")) +
            "</row><row r=\"2\">" +
            CreateSharedCell("A2", strings.IndexOf("ExternalStatus1Code")) +
            CreateSharedCell("B2", strings.IndexOf("Line 1_x000D_Line 2")) +
            CreateSharedCell("C2", strings.IndexOf("Registered")) +
            "</row></sheetData></worksheet>";
    }

    private static string CreateSharedCell(string reference, int index)
    {
        return "<c r=\"" + reference + "\" t=\"s\"><v>" +
            index.ToString(CultureInfo.InvariantCulture) + "</v></c>";
    }

    private static string GetColumnName(int index)
    {
        return ((char)('A' + index)).ToString();
    }

    private static void AddPackageEntry(ZipArchive archive, string name, string content)
    {
        ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.NoCompression);
        using Stream stream = entry.Open();
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        stream.Write(bytes);
    }

    private static byte[] CreateOuterZip((string Name, byte[] Content)[] entries)
    {
        using MemoryStream output = new MemoryStream();
        using (ZipArchive archive = new ZipArchive(output, ZipArchiveMode.Create, true))
        {
            foreach ((string name, byte[] content) in entries)
            {
                ZipArchiveEntry entry = archive.CreateEntry(name, CompressionLevel.NoCompression);
                using Stream stream = entry.Open();
                stream.Write(content);
            }
        }

        return output.ToArray();
    }
}
