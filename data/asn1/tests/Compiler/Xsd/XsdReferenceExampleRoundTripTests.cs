using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Xml.Schema;
using asn1;

namespace asn1.Tests;

public class XsdReferenceExampleRoundTripTests
{
    [Test]
    public void CompilesDownloadedXsdV2ReferenceModule()
    {
        string source = ExtractReferenceModule(
            FindSpecification("ITU-T-X.694-XSDv2.html"));
        AsnCompilationResult result = new CompilerImplementation().Compile(
            new AsnCompilationInput
            {
                SourceUnits =
                [
                    new AsnSourceUnit
                    {
                        Name = "XSDv2.asn1",
                        Text = source
                    }
                ]
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.Success,
                Is.True, Format(result.Diagnostics));
            Assert.That(result.Modules.Single().Name, Is.EqualTo("XSDv2"));
            Assert.That(result.Assignments.Select(value => value.Name),
                Does.Contain("AnyType"));
            Assert.That(result.Assignments.Select(value => value.Name),
                Does.Contain("QName"));
            Assert.That(result.Assignments.Select(value => value.Name),
                Does.Contain("DateTime"));
            Assert.That(result.Assignments.Select(value => value.Name),
                Does.Contain("GenericTimeTypeChoice"));
            Assert.That(result.ReflectionCatalog?.Assignments.Any(value =>
                value.QualifiedName == "XSDv2.AnyType"), Is.True);
        });
    }

    [Test]
    public void MapsAndCompilesAnnexDReferenceSchema()
    {
        AsnCompilationResult result = CompileReferenceSchema();
        string[] mappedAssignments =
        [
            "EXAMPLES",
            "PersonnelRecord",
            "NameOfSpouse",
            "Name-1",
            "Children",
            "ChildInformation",
            "ListOfDays",
            "Day-1",
            "NamesOfMemberNations",
            "FileIdentifier",
            "Unidentified"
        ];

        Assert.Multiple(() =>
        {
            Assert.That(result.Success,
                Is.True, Format(result.Diagnostics));
            Assert.That(result.Modules.Select(value => value.Name),
                Does.Contain("X694-Examples"));
            Assert.That(result.Modules.Select(value => value.Name),
                Does.Contain("XSDv2"));
            Assert.That(result.ReflectionCatalog?.Assignments
                .Where(value => value.QualifiedName.StartsWith(
                    "X694-Examples.",
                    System.StringComparison.Ordinal))
                .Select(value => value.Name),
                Is.EquivalentTo(mappedAssignments));
        });
    }

    [Test]
    public void RoundTripsRepresentativeAnnexDInstancesWithEquivalentInfosets()
    {
        string schema = ReferenceSchema();
        AsnCompilationResult compilation = CompileReferenceSchema();
        AsnReflectionCatalog catalog = compilation.ReflectionCatalog
            ?? throw new AssertionException(
                "The reference schema reflection catalog is missing. "
                    + Format(compilation.Diagnostics));
        string[][] examples =
        [
            [
                "X694-Examples.NamesOfMemberNations",
                "<namesOfMemberNations>France Japan Canada</namesOfMemberNations>"
            ],
            [
                "X694-Examples.FileIdentifier",
                "<fileIdentifier><serialNumber>314</serialNumber></fileIdentifier>"
            ],
            [
                "X694-Examples.PersonnelRecord",
                "<personnelRecord><name><givenName>Ada</givenName>"
                    + "<initial>L</initial><familyName>Lovelace</familyName></name>"
                    + "<title>Analyst</title><decimal>42</decimal>"
                    + "<dateOfHire>1843-01-01</dateOfHire><nameOfSpouse>"
                    + "<givenName>William</givenName><initial>K</initial>"
                    + "<familyName>King</familyName></nameOfSpouse><children>"
                    + "<ChildInformation><name><givenName>Anne</givenName>"
                    + "<initial>B</initial><familyName>Blunt</familyName></name>"
                    + "<dateOfBirth>1837-09-22</dateOfBirth>"
                    + "</ChildInformation></children></personnelRecord>"
            ]
        ];
        XerSchemaCodec codec = new XerSchemaCodec(
            catalog,
            AsnEncodingRules.ExtendedXer);

        Assert.That(compilation.Success,
            Is.True, Format(compilation.Diagnostics));
        int index = 0;
        while (index < examples.Length)
        {
            string typeName = examples[index][0];
            string original = examples[index][1];
            ValidateXml(schema, original, typeName + " input");
            AsnDecodeResult decoded = codec.DecodeRequest(
                new AsnDecodeRequest
                {
                    TypeName = typeName,
                    Data = Encoding.UTF8.GetBytes(original),
                    Options = Options()
                });
            Assert.That(decoded.Status,
                Is.EqualTo(AsnDecodeStatus.Done),
                typeName + ": " + Format(decoded.Diagnostics));
            AsnRuntimeValue value = decoded.Value
                ?? throw new AssertionException(
                    typeName + " decoded without a value.");
            AsnEncodeResult encoded = codec.EncodeRequest(
                new AsnEncodeRequest
                {
                    TypeName = typeName,
                    Value = value,
                    Options = Options()
                });
            Assert.That(encoded.Success,
                Is.True, typeName + ": " + Format(encoded.Diagnostics));
            string reproduced = Encoding.UTF8.GetString(encoded.Data);
            ValidateXml(schema, reproduced, typeName + " output");
            Assert.That(Infoset(reproduced),
                Is.EqualTo(Infoset(original)),
                typeName + " changed the XML information set.");
            index = index + 1;
        }
    }

    private static AsnCompilationResult CompileReferenceSchema()
    {
        return new CompilerImplementation().Compile(new AsnCompilationInput
        {
            XsdInputs =
            [
                new AsnXsdInput
                {
                    Name = "X694-Annex-D.xsd",
                    HasBaseUri = true,
                    BaseUri = "urn:itu:x694:annex-d",
                    Text = ReferenceSchema()
                }
            ],
            XsdImportOptions = new AsnXsdImportOptions
            {
                HasModuleName = true,
                ModuleName = "X694-Examples"
            }
        });
    }

    private static AsnCodecOptions Options()
    {
        return new AsnCodecOptions
        {
            EncodingRules = AsnEncodingRules.ExtendedXer
        };
    }

    private static void ValidateXml(
        string schema,
        string xml,
        string description)
    {
        XmlSchemaSet schemas = new XmlSchemaSet();
        using (StringReader schemaText = new StringReader(schema))
        using (XmlReader schemaReader = XmlReader.Create(schemaText))
        {
            schemas.Add("", schemaReader);
        }
        schemas.Compile();
        List<string> errors = new List<string>();
        XmlReaderSettings settings = new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            Schemas = schemas,
            ValidationType = ValidationType.Schema,
            XmlResolver = null
        };
        settings.ValidationEventHandler += (_, eventArgs) =>
            errors.Add(eventArgs.Severity + ": " + eventArgs.Message);
        using (StringReader documentText = new StringReader(xml))
        using (XmlReader reader = XmlReader.Create(documentText, settings))
        {
            while (reader.Read())
            {
            }
        }
        Assert.That(errors,
            Is.Empty,
            description + " is not valid against the Annex D schema: "
                + string.Join(" | ", errors));
    }

    private static string Infoset(string xml)
    {
        XmlDocument document = new XmlDocument
        {
            PreserveWhitespace = false,
            XmlResolver = null
        };
        document.LoadXml(xml);
        StringBuilder result = new StringBuilder();
        AppendInfoset(document.DocumentElement
            ?? throw new AssertionException("XML has no document element."),
            result);
        return result.ToString();
    }

    private static void AppendInfoset(XmlNode node, StringBuilder result)
    {
        if (node.NodeType == XmlNodeType.Element)
        {
            result.Append("E{").Append(node.NamespaceURI).Append('}')
                .Append(node.LocalName).Append('[');
            XmlAttribute[] attributes = node.Attributes == null
                ? []
                : node.Attributes.Cast<XmlAttribute>()
                    .Where(value => value.Prefix != "xmlns"
                        && value.Name != "xmlns")
                    .OrderBy(value => value.NamespaceURI)
                    .ThenBy(value => value.LocalName)
                    .ToArray();
            int attributeIndex = 0;
            while (attributeIndex < attributes.Length)
            {
                XmlAttribute attribute = attributes[attributeIndex];
                result.Append('{').Append(attribute.NamespaceURI).Append('}')
                    .Append(attribute.LocalName).Append('=')
                    .Append(attribute.Value).Append(';');
                attributeIndex = attributeIndex + 1;
            }
            result.Append(']');
        }
        else if (node.NodeType == XmlNodeType.Text
            || node.NodeType == XmlNodeType.CDATA)
        {
            result.Append("T[").Append(node.Value).Append(']');
        }

        XmlNode? child = node.FirstChild;
        while (child != null)
        {
            AppendInfoset(child, result);
            child = child.NextSibling;
        }
        if (node.NodeType == XmlNodeType.Element)
        {
            result.Append("/E");
        }
    }

    private static string ReferenceSchema()
    {
        return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
            + "<xsd:schema xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" "
            + "elementFormDefault=\"unqualified\">"
            + "<xsd:element name=\"EXAMPLES\"><xsd:complexType><xsd:sequence>"
            + "<xsd:element ref=\"personnelRecord\"/>"
            + "<xsd:element name=\"decimal\" type=\"xsd:decimal\"/>"
            + "<xsd:element name=\"daysOfTheWeek\" type=\"ListOfDays\"/>"
            + "<xsd:element ref=\"namesOfMemberNations\"/>"
            + "<xsd:element ref=\"fileIdentifier\" maxOccurs=\"unbounded\"/>"
            + "</xsd:sequence></xsd:complexType></xsd:element>"
            + "<xsd:element name=\"personnelRecord\"><xsd:complexType>"
            + "<xsd:sequence><xsd:element name=\"name\" type=\"name\"/>"
            + "<xsd:element name=\"title\" type=\"xsd:string\"/>"
            + "<xsd:element name=\"decimal\" type=\"xsd:integer\"/>"
            + "<xsd:element name=\"dateOfHire\" type=\"xsd:date\"/>"
            + "<xsd:element ref=\"nameOfSpouse\"/>"
            + "<xsd:element ref=\"children\"/>"
            + "</xsd:sequence></xsd:complexType></xsd:element>"
            + "<xsd:element name=\"nameOfSpouse\" type=\"name\"/>"
            + "<xsd:complexType name=\"name\"><xsd:sequence>"
            + "<xsd:element name=\"givenName\" type=\"xsd:string\"/>"
            + "<xsd:element name=\"initial\" type=\"xsd:string\"/>"
            + "<xsd:element name=\"familyName\" type=\"xsd:string\"/>"
            + "</xsd:sequence></xsd:complexType>"
            + "<xsd:element name=\"children\"><xsd:complexType><xsd:sequence>"
            + "<xsd:element ref=\"ChildInformation\" minOccurs=\"0\" "
            + "maxOccurs=\"unbounded\"/>"
            + "</xsd:sequence></xsd:complexType></xsd:element>"
            + "<xsd:element name=\"ChildInformation\"><xsd:complexType>"
            + "<xsd:sequence><xsd:element name=\"name\" type=\"name\"/>"
            + "<xsd:element name=\"dateOfBirth\" type=\"xsd:date\"/>"
            + "</xsd:sequence></xsd:complexType></xsd:element>"
            + "<xsd:simpleType name=\"ListOfDays\">"
            + "<xsd:list itemType=\"Day\"/></xsd:simpleType>"
            + "<xsd:simpleType name=\"Day\"><xsd:restriction base=\"xsd:string\">"
            + "<xsd:enumeration value=\"monday\"/>"
            + "<xsd:enumeration value=\"tuesday\"/>"
            + "<xsd:enumeration value=\"wednesday\"/>"
            + "<xsd:enumeration value=\"thursday\"/>"
            + "<xsd:enumeration value=\"friday\"/>"
            + "<xsd:enumeration value=\"saturday\"/>"
            + "<xsd:enumeration value=\"sunday\"/>"
            + "</xsd:restriction></xsd:simpleType>"
            + "<xsd:element name=\"namesOfMemberNations\"><xsd:simpleType>"
            + "<xsd:list itemType=\"xsd:string\"/>"
            + "</xsd:simpleType></xsd:element>"
            + "<xsd:element name=\"fileIdentifier\"><xsd:complexType>"
            + "<xsd:choice><xsd:element name=\"serialNumber\" type=\"xsd:int\"/>"
            + "<xsd:element name=\"relativeName\" type=\"xsd:string\"/>"
            + "<xsd:element ref=\"unidentified\"/>"
            + "</xsd:choice></xsd:complexType></xsd:element>"
            + "<xsd:element name=\"unidentified\" type=\"xsd:anyType\"/>"
            + "</xsd:schema>";
    }

    private static string ExtractReferenceModule(string path)
    {
        string html = File.ReadAllText(path);
        int start = html.IndexOf("<pre>",
            System.StringComparison.OrdinalIgnoreCase);
        int end = html.LastIndexOf("</pre>",
            System.StringComparison.OrdinalIgnoreCase);
        if (start < 0 || end <= start)
        {
            throw new AssertionException(
                "The vendored XSDv2 reference module has no preformatted body.");
        }
        string body = html.Substring(start + 5, end - start - 5);
        body = Regex.Replace(
            body,
            "<i>[\\s\\S]*?</i><br><br>",
            "",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        body = Regex.Replace(
            body,
            "<[^>]+>",
            "",
            RegexOptions.CultureInvariant);
        return System.Net.WebUtility.HtmlDecode(body);
    }

    private static string FindSpecification(string fileName)
    {
        DirectoryInfo? directory = new DirectoryInfo(
            TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            string candidate = Path.Combine(
                directory.FullName,
                "specs",
                fileName);
            if (File.Exists(candidate))
            {
                return candidate;
            }
            directory = directory.Parent;
        }
        throw new FileNotFoundException(
            "Cannot locate vendored specification.",
            fileName);
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(value =>
            value.Code + ":" + value.Span?.Start.Line + ":"
                + value.Span?.Start.Column + ":" + value.Message));
    }
}
