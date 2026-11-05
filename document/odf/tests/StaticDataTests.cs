using System.Reflection;

namespace odf.Tests;

public class StaticDataTests
{
    [Test]
    public void NamespaceConstantsMatchStandardIrisAndPrefixes()
    {
        string[] actual =
        [
            OdfNamespaces.Office, OdfNamespaces.OfficePrefix,
            OdfNamespaces.Text, OdfNamespaces.TextPrefix,
            OdfNamespaces.Table, OdfNamespaces.TablePrefix,
            OdfNamespaces.Drawing, OdfNamespaces.DrawingPrefix,
            OdfNamespaces.Presentation, OdfNamespaces.PresentationPrefix,
            OdfNamespaces.Chart, OdfNamespaces.ChartPrefix,
            OdfNamespaces.Form, OdfNamespaces.FormPrefix,
            OdfNamespaces.Database, OdfNamespaces.DatabasePrefix,
            OdfNamespaces.Manifest, OdfNamespaces.ManifestPrefix,
            OdfNamespaces.DigitalSignature, OdfNamespaces.DigitalSignaturePrefix,
            OdfNamespaces.OpenFormula, OdfNamespaces.OpenFormulaPrefix,
            OdfNamespaces.Xml, OdfNamespaces.XmlPrefix,
            OdfNamespaces.XmlDigitalSignature, OdfNamespaces.XmlDigitalSignaturePrefix,
            OdfNamespaces.Rdf, OdfNamespaces.RdfPrefix,
            OdfNamespaces.OdfMetadata, OdfNamespaces.OdfMetadataPrefix,
            OdfNamespaces.PackageMetadata, OdfNamespaces.PackageMetadataPrefix
        ];

        Assert.That(actual, Is.EqualTo(new[]
        {
            "urn:oasis:names:tc:opendocument:xmlns:office:1.0", "office",
            "urn:oasis:names:tc:opendocument:xmlns:text:1.0", "text",
            "urn:oasis:names:tc:opendocument:xmlns:table:1.0", "table",
            "urn:oasis:names:tc:opendocument:xmlns:drawing:1.0", "draw",
            "urn:oasis:names:tc:opendocument:xmlns:presentation:1.0", "presentation",
            "urn:oasis:names:tc:opendocument:xmlns:chart:1.0", "chart",
            "urn:oasis:names:tc:opendocument:xmlns:form:1.0", "form",
            "urn:oasis:names:tc:opendocument:xmlns:database:1.0", "db",
            "urn:oasis:names:tc:opendocument:xmlns:manifest:1.0", "manifest",
            "urn:oasis:names:tc:opendocument:xmlns:digitalsignature:1.0", "dsig",
            "urn:oasis:names:tc:opendocument:xmlns:of:1.2", "of",
            "http://www.w3.org/XML/1998/namespace", "xml",
            "http://www.w3.org/2000/09/xmldsig#", "ds",
            "http://www.w3.org/1999/02/22-rdf-syntax-ns#", "rdf",
            "http://docs.oasis-open.org/ns/office/1.2/meta/odf#", "odf",
            "http://docs.oasis-open.org/ns/office/1.2/meta/pkg#", "pkg"
        }));
    }

    [Test]
    public void MediaTypesCoverEveryStandardDocumentAndTemplateKind()
    {
        string[] mediaTypes =
        [
            OdfMediaTypes.Base,
            OdfMediaTypes.Text,
            OdfMediaTypes.TextTemplate,
            OdfMediaTypes.TextMaster,
            OdfMediaTypes.TextMasterTemplate,
            OdfMediaTypes.Spreadsheet,
            OdfMediaTypes.SpreadsheetTemplate,
            OdfMediaTypes.Presentation,
            OdfMediaTypes.PresentationTemplate,
            OdfMediaTypes.Graphics,
            OdfMediaTypes.GraphicsTemplate,
            OdfMediaTypes.Chart,
            OdfMediaTypes.ChartTemplate,
            OdfMediaTypes.Formula,
            OdfMediaTypes.FormulaTemplate,
            OdfMediaTypes.Image,
            OdfMediaTypes.ImageTemplate
        ];

        Assert.Multiple(() =>
        {
            Assert.That(mediaTypes, Has.Length.EqualTo(17));
            Assert.That(mediaTypes, Is.Unique);
            Assert.That(mediaTypes, Has.All.StartsWith("application/vnd.oasis.opendocument."));
            Assert.That(OdfMediaTypes.Xml, Is.EqualTo("text/xml"));
            Assert.That(OdfMediaTypes.RdfXml, Is.EqualTo("application/rdf+xml"));
            Assert.That(OdfMediaTypes.Png, Is.EqualTo("image/png"));
            Assert.That(OdfMediaTypes.Svg, Is.EqualTo("image/svg+xml"));
        });
    }

    [Test]
    public void PartNamesMatchCanonicalPackagePaths()
    {
        Assert.Multiple(() =>
        {
            Assert.That(OdfPartNames.Root, Is.EqualTo("/"));
            Assert.That(OdfPartNames.Mimetype, Is.EqualTo("mimetype"));
            Assert.That(OdfPartNames.Content, Is.EqualTo("content.xml"));
            Assert.That(OdfPartNames.Styles, Is.EqualTo("styles.xml"));
            Assert.That(OdfPartNames.Metadata, Is.EqualTo("meta.xml"));
            Assert.That(OdfPartNames.Settings, Is.EqualTo("settings.xml"));
            Assert.That(OdfPartNames.Manifest, Is.EqualTo("META-INF/manifest.xml"));
            Assert.That(OdfPartNames.MetadataManifest, Is.EqualTo("manifest.rdf"));
            Assert.That(OdfPartNames.DocumentSignatures, Is.EqualTo("META-INF/documentsignatures.xml"));
            Assert.That(OdfPartNames.Thumbnail, Is.EqualTo("Thumbnails/thumbnail.png"));
        });
    }

    [Test]
    public void AlgorithmIdentifiersMatchOdfAndXmlSecurityStandards()
    {
        Assert.Multiple(() =>
        {
            Assert.That(OdfAlgorithmIdentifiers.BlowfishCfbName, Is.EqualTo("Blowfish CFB"));
            Assert.That(OdfAlgorithmIdentifiers.BlowfishCfb, Is.EqualTo("urn:oasis:names:tc:opendocument:xmlns:manifest:1.0#blowfish"));
            Assert.That(OdfAlgorithmIdentifiers.Pbkdf2, Is.EqualTo("urn:oasis:names:tc:opendocument:xmlns:manifest:1.0#pbkdf2"));
            Assert.That(OdfAlgorithmIdentifiers.Sha1OneKilobyte, Is.EqualTo("urn:oasis:names:tc:opendocument:xmlns:manifest:1.0#sha1-1k"));
            Assert.That(OdfAlgorithmIdentifiers.Sha256OneKilobyte, Is.EqualTo("urn:oasis:names:tc:opendocument:xmlns:manifest:1.0#sha256-1k"));
            Assert.That(OdfAlgorithmIdentifiers.Aes128Cbc, Is.EqualTo("http://www.w3.org/2001/04/xmlenc#aes128-cbc"));
            Assert.That(OdfAlgorithmIdentifiers.Aes256Cbc, Is.EqualTo("http://www.w3.org/2001/04/xmlenc#aes256-cbc"));
            Assert.That(OdfAlgorithmIdentifiers.RsaSha256, Is.EqualTo("http://www.w3.org/2001/04/xmldsig-more#rsa-sha256"));
            Assert.That(OdfAlgorithmIdentifiers.CanonicalXml10, Is.EqualTo("http://www.w3.org/TR/2001/REC-xml-c14n-20010315"));
            Assert.That(OdfAlgorithmIdentifiers.ExclusiveCanonicalXml10, Is.EqualTo("http://www.w3.org/2001/10/xml-exc-c14n#"));
        });
    }

    [Test]
    public void FormulaCatalogContainsEveryOdf14FunctionName()
    {
        FieldInfo[] fields = typeof(OdfFormulaNames).GetFields(BindingFlags.Public | BindingFlags.Static);
        FieldInfo[] functionFields = fields
            .Where(field => field.Name == field.Name.ToUpperInvariant())
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(functionFields, Has.Length.EqualTo(393));
            Assert.That(functionFields.Select(field => field.GetRawConstantValue()), Is.Unique);
            Assert.That(functionFields, Has.All.Matches<FieldInfo>(field => field.GetRawConstantValue()?.ToString() == field.Name));
            Assert.That(OdfFormulaNames.OpenFormulaNamespace, Is.EqualTo("urn:oasis:names:tc:opendocument:xmlns:of:1.2"));
            Assert.That(OdfFormulaNames.ErrorDivisionByZero, Is.EqualTo("#DIV/0!"));
            Assert.That(OdfFormulaNames.ErrorNotAvailable, Is.EqualTo("#N/A"));
            Assert.That(OdfFormulaNames.ABS, Is.EqualTo("ABS"));
            Assert.That(OdfFormulaNames.BITAND, Is.EqualTo("BITAND"));
            Assert.That(OdfFormulaNames.COMPLEX, Is.EqualTo("COMPLEX"));
            Assert.That(OdfFormulaNames.DSUM, Is.EqualTo("DSUM"));
            Assert.That(OdfFormulaNames.ACCRINT, Is.EqualTo("ACCRINT"));
            Assert.That(OdfFormulaNames.HLOOKUP, Is.EqualTo("HLOOKUP"));
            Assert.That(OdfFormulaNames.MULTIPLE_OPERATIONS, Is.EqualTo("MULTIPLE_OPERATIONS"));
            Assert.That(OdfFormulaNames.LEGACY_CHIDIST, Is.EqualTo("LEGACY_CHIDIST"));
        });
    }

    [Test]
    public void DiagnosticCodesAreStableAndUnique()
    {
        string[] codes =
        [
            OdfDiagnosticCodes.InvalidPackage,
            OdfDiagnosticCodes.EntryLimitExceeded,
            OdfDiagnosticCodes.ExpandedSizeLimitExceeded,
            OdfDiagnosticCodes.CompressionRatioExceeded,
            OdfDiagnosticCodes.DuplicateEntry,
            OdfDiagnosticCodes.InvalidEntryPath,
            OdfDiagnosticCodes.PathTraversal,
            OdfDiagnosticCodes.PathNormalizationCollision,
            OdfDiagnosticCodes.UnsupportedCompression,
            OdfDiagnosticCodes.CrcMismatch,
            OdfDiagnosticCodes.MissingMimetype,
            OdfDiagnosticCodes.MimetypeNotFirst,
            OdfDiagnosticCodes.MimetypeCompressed,
            OdfDiagnosticCodes.MimetypeExtraField,
            OdfDiagnosticCodes.InvalidMimetype,
            OdfDiagnosticCodes.MissingManifest,
            OdfDiagnosticCodes.ManifestEntryMissing,
            OdfDiagnosticCodes.ManifestMediaTypeMismatch,
            OdfDiagnosticCodes.MalformedXml,
            OdfDiagnosticCodes.ExternalEntityRejected,
            OdfDiagnosticCodes.XmlDepthLimitExceeded,
            OdfDiagnosticCodes.XmlAttributeLimitExceeded,
            OdfDiagnosticCodes.XmlTextLimitExceeded,
            OdfDiagnosticCodes.InvalidNamespace,
            OdfDiagnosticCodes.InvalidXmlName,
            OdfDiagnosticCodes.DuplicateXmlId,
            OdfDiagnosticCodes.BrokenReference,
            OdfDiagnosticCodes.UnexpectedElement,
            OdfDiagnosticCodes.UnexpectedAttribute,
            OdfDiagnosticCodes.MissingRequiredContent,
            OdfDiagnosticCodes.InvalidDatatype,
            OdfDiagnosticCodes.OccurrenceViolation,
            OdfDiagnosticCodes.InvalidStyleReference,
            OdfDiagnosticCodes.InvalidRangeReference,
            OdfDiagnosticCodes.InvalidResourceReference,
            OdfDiagnosticCodes.InvalidMerge,
            OdfDiagnosticCodes.ValueTypeMismatch,
            OdfDiagnosticCodes.InvalidFormula,
            OdfDiagnosticCodes.UnsupportedFunction,
            OdfDiagnosticCodes.FormulaDepthLimitExceeded,
            OdfDiagnosticCodes.CircularReference,
            OdfDiagnosticCodes.UnsupportedEncryption,
            OdfDiagnosticCodes.ChecksumMismatch,
            OdfDiagnosticCodes.SignatureInvalid,
            OdfDiagnosticCodes.SignatureAlgorithmUnsupported,
            OdfDiagnosticCodes.VersionUnsupported,
            OdfDiagnosticCodes.FeatureNotAvailable,
            OdfDiagnosticCodes.DowngradeLoss,
            OdfDiagnosticCodes.EditConflict,
            OdfDiagnosticCodes.EditValidationFailed,
            OdfDiagnosticCodes.OperationCancelled
        ];

        Assert.Multiple(() =>
        {
            Assert.That(codes, Has.Length.EqualTo(51));
            Assert.That(codes, Is.Unique);
            Assert.That(codes, Has.All.Match("^ODF-[A-Z]+-[0-9]{3}$"));
        });
    }
}
