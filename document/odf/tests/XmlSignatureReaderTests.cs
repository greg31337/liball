using System.Text;
using System.Xml;

namespace odf.Tests;

public class XmlSignatureReaderTests
{
    [Test]
    public void ParsesXmlSignatureReferencesAlgorithmsX509AndXadesMetadata()
    {
        string xml = ContainerStart("1.4")
            + "<ds:Signature Id='sig-1'>"
            + "<ds:SignedInfo Id='signed-info'>"
            + "<ds:CanonicalizationMethod Algorithm='"
            + OdfAlgorithmIdentifiers.ExclusiveCanonicalXml10
            + "'><ec:InclusiveNamespaces PrefixList='ds xades #default'/></ds:CanonicalizationMethod>"
            + "<ds:SignatureMethod Algorithm='"
            + OdfAlgorithmIdentifiers.RsaSha256
            + "'/>"
            + "<ds:Reference Id='content-ref' URI='content.xml' Type='urn:example:document'>"
            + "<ds:Transforms><ds:Transform Algorithm='"
            + OdfAlgorithmIdentifiers.ExclusiveCanonicalXml10
            + "'><ec:InclusiveNamespaces PrefixList='office ds'/></ds:Transform>"
            + "<ds:Transform Algorithm='"
            + OdfAlgorithmIdentifiers.Base64Transform
            + "'/></ds:Transforms>"
            + "<ds:DigestMethod Algorithm='"
            + OdfAlgorithmIdentifiers.Sha256
            + "'/><ds:DigestValue>AQIDBA==</ds:DigestValue></ds:Reference>"
            + "<ds:Reference URI='#signed-properties' Type='http://uri.etsi.org/01903#SignedProperties'>"
            + "<ds:DigestMethod Algorithm='"
            + OdfAlgorithmIdentifiers.Sha512
            + "'/><ds:DigestValue>BQYHCA==</ds:DigestValue></ds:Reference>"
            + "</ds:SignedInfo>"
            + "<ds:SignatureValue Id='signature-value'>c2lnbmF0dXJl</ds:SignatureValue>"
            + "<ds:KeyInfo Id='key-info'><ds:KeyName>Alice Example</ds:KeyName>"
            + "<ds:X509Data>"
            + "<ds:X509IssuerSerial><ds:X509IssuerName>CN=Issuer</ds:X509IssuerName>"
            + "<ds:X509SerialNumber>12345678901234567890</ds:X509SerialNumber>"
            + "</ds:X509IssuerSerial><ds:X509SubjectName>CN=Alice</ds:X509SubjectName>"
            + "<ds:X509SKI>AQID</ds:X509SKI>"
            + "<ds:X509Certificate>Y2VydA==</ds:X509Certificate>"
            + "<ds:X509CRL>Y3Js</ds:X509CRL>"
            + "<ds11:X509Digest Algorithm='"
            + OdfAlgorithmIdentifiers.Sha256
            + "'>ZGlnZXN0</ds11:X509Digest></ds:X509Data>"
            + "<ds11:DEREncodedKeyValue>a2V5</ds11:DEREncodedKeyValue>"
            + "<ds11:KeyInfoReference URI='#secondary-key'/></ds:KeyInfo>"
            + "<ds:Object Id='object-1'><ds:Manifest Id='manifest-1'>"
            + "<ds:Reference URI='Pictures/image.png'><ds:DigestMethod Algorithm='"
            + OdfAlgorithmIdentifiers.Sha256
            + "'/><ds:DigestValue>CQoLDA==</ds:DigestValue></ds:Reference>"
            + "</ds:Manifest>"
            + "<xades:QualifyingProperties Target='#sig-1' Id='qualifying'>"
            + "<xades:SignedProperties Id='signed-properties'>"
            + "<xades:SignedSignatureProperties>"
            + "<xades:SigningTime>2026-09-01T12:34:56Z</xades:SigningTime>"
            + "<xades:SigningCertificate/><xades:SigningCertificateV2/>"
            + "</xades:SignedSignatureProperties>"
            + "<xades:SignedDataObjectProperties><xades:DataObjectFormat ObjectReference='#content-ref'/>"
            + "</xades:SignedDataObjectProperties></xades:SignedProperties>"
            + "<xades:UnsignedProperties><xades:UnsignedSignatureProperties>"
            + "<xades:SignatureTimeStamp/></xades:UnsignedSignatureProperties>"
            + "<xades:UnsignedDataObjectProperties><ext:opaque>keep</ext:opaque>"
            + "</xades:UnsignedDataObjectProperties></xades:UnsignedProperties>"
            + "</xades:QualifyingProperties></ds:Object></ds:Signature>"
            + "</dsig:document-signatures>";
        byte[] source = Encoding.UTF8.GetBytes(xml);

        OdfSignatureContainer container = XmlSignatureReader.ParseBytes(
            source,
            OdfPartNames.DocumentSignatures,
            CreateLimits());
        OdfSignature signature = container.Signatures[0];
        OdfSignatureReference contentReference = signature.References[0];
        OdfX509Data x509 = signature.X509Data[0];
        OdfXadesProperties xades = signature.XadesProperties[0];
        byte originalFirstByte = container.OriginalBytes[0];
        source[0] = 0;

        Assert.Multiple(() =>
        {
            Assert.That(container.IsValid, Is.True);
            Assert.That(container.Version, Is.EqualTo("1.4"));
            Assert.That(container.PartPath, Is.EqualTo(OdfPartNames.DocumentSignatures));
            Assert.That(container.Signatures, Has.Length.EqualTo(1));
            Assert.That(container.OriginalBytes[0], Is.EqualTo(originalFirstByte));
            Assert.That(signature.Id, Is.EqualTo("sig-1"));
            Assert.That(signature.Status, Is.EqualTo(OdfSignatureStatus.NotChecked));
            Assert.That(signature.CanonicalizationMethod, Is.EqualTo(OdfAlgorithmIdentifiers.ExclusiveCanonicalXml10));
            Assert.That(signature.CanonicalizationInclusivePrefixes, Is.EqualTo("ds xades #default"));
            Assert.That(signature.SignatureMethod, Is.EqualTo(OdfAlgorithmIdentifiers.RsaSha256));
            Assert.That(signature.SignatureValue, Is.EqualTo(Encoding.UTF8.GetBytes("signature")));
            Assert.That(signature.SignatureValueId, Is.EqualTo("signature-value"));
            Assert.That(signature.References, Has.Length.EqualTo(2));
            Assert.That(contentReference.IsPackageReference, Is.True);
            Assert.That(contentReference.IsSameDocument, Is.False);
            Assert.That(contentReference.Transforms, Has.Length.EqualTo(2));
            Assert.That(contentReference.Transforms[0].InclusivePrefixes, Is.EqualTo(new[] { "office", "ds" }));
            Assert.That(contentReference.DigestValue, Is.EqualTo(new byte[] { 1, 2, 3, 4 }));
            Assert.That(signature.References[1].IsSameDocument, Is.True);
            Assert.That(signature.ManifestReferences, Has.Length.EqualTo(1));
            Assert.That(signature.ManifestReferences[0].Uri, Is.EqualTo("Pictures/image.png"));
            Assert.That(signature.KeyNames, Is.EqualTo(new[] { "Alice Example" }));
            Assert.That(signature.DerEncodedKeyValues[0], Is.EqualTo(Encoding.UTF8.GetBytes("key")));
            Assert.That(signature.KeyInfoReferenceUris, Is.EqualTo(new[] { "#secondary-key" }));
            Assert.That(x509.IsValid, Is.True);
            Assert.That(x509.IssuerNames, Is.EqualTo(new[] { "CN=Issuer" }));
            Assert.That(x509.SerialNumbers, Is.EqualTo(new[] { "12345678901234567890" }));
            Assert.That(x509.SubjectNames, Is.EqualTo(new[] { "CN=Alice" }));
            Assert.That(x509.Certificates[0], Is.EqualTo(Encoding.UTF8.GetBytes("cert")));
            Assert.That(x509.CertificateRevocationLists[0], Is.EqualTo(Encoding.UTF8.GetBytes("crl")));
            Assert.That(x509.Digests[0], Is.EqualTo(Encoding.UTF8.GetBytes("digest")));
            Assert.That(xades.IsValid, Is.True);
            Assert.That(xades.Target, Is.EqualTo("#sig-1"));
            Assert.That(xades.SignedPropertiesId, Is.EqualTo("signed-properties"));
            Assert.That(xades.SigningTime, Is.EqualTo("2026-09-01T12:34:56Z"));
            Assert.That(xades.HasSigningCertificate, Is.True);
            Assert.That(xades.HasSigningCertificateV2, Is.True);
            Assert.That(xades.HasUnsignedProperties, Is.True);
            Assert.That(xades.UnsignedDataObjectPropertiesXml, Does.Contain("ext:opaque"));
            Assert.That(signature.OriginalXml, Does.Contain("xades:QualifyingProperties"));
            Assert.That(signature.ObjectXml[0], Does.Contain("ext:opaque"));
            Assert.That(signature.ObjectXml[0], Does.Contain("keep</ext:opaque>"));
            Assert.That(container.Diagnostics, Is.Empty);
        });
    }

    [TestCase(OdfPartNames.DocumentSignatures, "1.2")]
    [TestCase(OdfPartNames.MacroSignatures, "1.3")]
    [TestCase(OdfPartNames.PackageSignatures, "1.4")]
    public void ParsesEveryStandardOdfSignatureContainerPathAndVersion(
        string partPath,
        string version)
    {
        string xml = ContainerStart(version)
            + MinimalSignature()
            + "</dsig:document-signatures>";

        OdfSignatureContainer container = Parse(xml, partPath, CreateLimits());

        Assert.Multiple(() =>
        {
            Assert.That(container.IsValid, Is.True);
            Assert.That(container.Version, Is.EqualTo(version));
            Assert.That(container.PartPath, Is.EqualTo(partPath));
            Assert.That(container.Signatures[0].IsValid, Is.True);
        });
    }

    [Test]
    public void RetainsInvalidLexicalValuesAndReportsStructuralDiagnostics()
    {
        string xml = ContainerStart("1.4")
            + "<ds:Signature><ds:SignedInfo>"
            + "<ds:CanonicalizationMethod/><ds:SignatureMethod Algorithm=''/>"
            + "<ds:Reference URI='content.xml'><ds:DigestValue>not-base64</ds:DigestValue>"
            + "</ds:Reference></ds:SignedInfo><ds:SignatureValue>bad</ds:SignatureValue>"
            + "<ds:KeyInfo><ds:X509Data><ds:X509Certificate>bad</ds:X509Certificate>"
            + "</ds:X509Data></ds:KeyInfo><ds:Object>"
            + "<xades:QualifyingProperties><xades:SignedProperties/>"
            + "</xades:QualifyingProperties></ds:Object></ds:Signature>"
            + "</dsig:document-signatures>";

        OdfSignatureContainer container = Parse(
            xml,
            OdfPartNames.DocumentSignatures,
            CreateLimits());
        OdfSignature signature = container.Signatures[0];

        Assert.Multiple(() =>
        {
            Assert.That(container.IsValid, Is.False);
            Assert.That(signature.IsValid, Is.False);
            Assert.That(signature.SignatureValueLexical, Is.EqualTo("bad"));
            Assert.That(signature.SignatureValueIsValid, Is.False);
            Assert.That(signature.References[0].DigestValueLexical, Is.EqualTo("not-base64"));
            Assert.That(signature.References[0].DigestValueIsValid, Is.False);
            Assert.That(signature.X509Data[0].CertificateLexicalValues[0], Is.EqualTo("bad"));
            Assert.That(signature.XadesProperties[0].OriginalXml, Does.Contain("SignedProperties"));
            Assert.That(signature.Diagnostics, Is.Not.Empty);
            Assert.That(container.Diagnostics, Is.Not.Empty);
            Assert.That(container.Diagnostics[0].Code, Is.EqualTo(OdfDiagnosticCodes.SignatureInvalid));
        });
    }

    [Test]
    public void RejectsInvalidContainerIdentityAndEnforcesXmlBounds()
    {
        string validXml = ContainerStart("1.4")
            + MinimalSignature()
            + "</dsig:document-signatures>";
        OdfSignatureContainer wrongPath = Parse(
            validXml,
            "META-INF/other-signatures.xml",
            CreateLimits());
        OdfSignatureContainer wrongVersion = Parse(
            ContainerStart("2.0") + MinimalSignature() + "</dsig:document-signatures>",
            OdfPartNames.DocumentSignatures,
            CreateLimits());
        OdfSecurityLimits sizeLimits = CreateLimits();
        sizeLimits.MaximumSingleEntryBytes = 10;
        OdfSecurityLimits depthLimits = CreateLimits();
        depthLimits.MaximumXmlDepth = 2;
        OdfSecurityLimits attributeLimits = CreateLimits();
        attributeLimits.MaximumXmlAttributesPerElement = 1;

        Assert.Multiple(() =>
        {
            Assert.That(wrongPath.IsValid, Is.False);
            Assert.That(wrongVersion.IsValid, Is.False);
            Assert.Throws<OdfSecurityException>(() => Parse(
                validXml,
                OdfPartNames.DocumentSignatures,
                sizeLimits));
            Assert.Throws<OdfSecurityException>(() => Parse(
                validXml,
                OdfPartNames.DocumentSignatures,
                depthLimits));
            Assert.Throws<OdfSecurityException>(() => Parse(
                validXml,
                OdfPartNames.DocumentSignatures,
                attributeLimits));
            Assert.Throws<XmlException>(() => Parse(
                "<!DOCTYPE r [<!ENTITY x 'value'>]><r>&x;</r>",
                OdfPartNames.DocumentSignatures,
                CreateLimits()));
        });
    }

    static OdfSignatureContainer Parse(
        string xml,
        string partPath,
        OdfSecurityLimits limits)
    {
        return XmlSignatureReader.ParseBytes(
            Encoding.UTF8.GetBytes(xml),
            partPath,
            limits);
    }

    static string ContainerStart(string version)
    {
        return "<dsig:document-signatures xmlns:dsig='"
            + OdfNamespaces.DigitalSignature
            + "' xmlns:ds='"
            + OdfNamespaces.XmlDigitalSignature
            + "' xmlns:ds11='http://www.w3.org/2009/xmldsig11#'"
            + " xmlns:ec='http://www.w3.org/2001/10/xml-exc-c14n#' xmlns:xades='"
            + OdfNamespaces.Xades
            + "' xmlns:office='"
            + OdfNamespaces.Office
            + "' xmlns:ext='urn:extension' dsig:version='"
            + version
            + "'>";
    }

    static string MinimalSignature()
    {
        return "<ds:Signature Id='minimal'><ds:SignedInfo>"
            + "<ds:CanonicalizationMethod Algorithm='"
            + OdfAlgorithmIdentifiers.CanonicalXml11
            + "'/><ds:SignatureMethod Algorithm='"
            + OdfAlgorithmIdentifiers.EcdsaSha256
            + "'/><ds:Reference URI='content.xml'><ds:DigestMethod Algorithm='"
            + OdfAlgorithmIdentifiers.Sha256
            + "'/><ds:DigestValue>AQIDBA==</ds:DigestValue></ds:Reference>"
            + "</ds:SignedInfo><ds:SignatureValue>c2ln</ds:SignatureValue>"
            + "</ds:Signature>";
    }

    static OdfSecurityLimits CreateLimits()
    {
        OdfSecurityLimits result = new OdfSecurityLimits();
        result.MaximumSingleEntryBytes = 1048576;
        result.MaximumXmlDepth = 64;
        result.MaximumXmlAttributesPerElement = 64;
        result.MaximumXmlTextLength = 1048576;
        return result;
    }
}
