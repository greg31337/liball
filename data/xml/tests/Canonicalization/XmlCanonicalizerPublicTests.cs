using System.Text;
using xml;

namespace xml.Tests;

[TestFixture]
public class XmlCanonicalizerPublicTests
{
    [Test]
    public void CollectorExposesBoundedSnapshotCountClearAndResetOperations()
    {
        XmlByteCollector collector = new XmlByteCollector();
        IXmlByteSink sink = collector;
        IXmlByteCollector collection = collector;
        collection.ConfigureXmlByteCollection(4);
        byte[] source = [1, 2];

        sink.WriteXmlBytes(source);
        byte[] snapshot = collection.SnapshotCollectedXmlBytes();
        source[0] = 9;
        snapshot[1] = 8;

        Assert.Multiple(() =>
        {
            Assert.That(collection.CountCollectedXmlBytes(), Is.EqualTo(2));
            Assert.That(collection.SnapshotCollectedXmlBytes(), Is.EqualTo(new byte[] { 1, 2 }));
        });

        XmlLimitException exception = Assert.Throws<XmlLimitException>(() => sink.WriteXmlBytes(new byte[] { 3, 4, 5 }))!;
        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.LimitExceeded));
            Assert.That(exception.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Limit));
            Assert.That(collection.CountCollectedXmlBytes(), Is.EqualTo(2));
            Assert.That(collection.SnapshotCollectedXmlBytes(), Is.EqualTo(new byte[] { 1, 2 }));
        });

        collection.ClearCollectedXmlBytes();
        Assert.That(collection.CountCollectedXmlBytes(), Is.Zero);
        Assert.Throws<XmlLimitException>(() => sink.WriteXmlBytes(new byte[] { 1, 2, 3, 4, 5 }));

        collection.ResetXmlByteCollection();
        sink.WriteXmlBytes(new byte[] { 6, 7, 8, 9, 10 });
        Assert.That(collection.SnapshotCollectedXmlBytes(), Is.EqualTo(new byte[] { 6, 7, 8, 9, 10 }));
    }

    [Test]
    public void CanonicalizerWritesRawCanonicalBytesWithoutMutatingInputs()
    {
        XmlDocument document = Parse("<root b=\"2\" a=\"1\"><empty/></root>");
        long mutationVersion = document.MutationVersion;
        XmlCanonicalizationOptions options = new XmlCanonicalizationOptions
        {
            Method = XmlCanonicalizationMethod.CanonicalXml11,
            IncludeComments = true
        };
        XmlByteCollector collector = new XmlByteCollector();
        XmlOutput output = new XmlOutput
        {
            Kind = XmlOutputKind.ByteSink,
            ByteSink = collector,
            EncodingName = "utf-16"
        };

        new XmlCanonicalizer().CanonicalizeXml(document, output, options);

        Assert.Multiple(() =>
        {
            Assert.That(collector.SnapshotCollectedXmlBytes(), Is.EqualTo(Encoding.UTF8.GetBytes("<root a=\"1\" b=\"2\"><empty></empty></root>")));
            Assert.That(document.MutationVersion, Is.EqualTo(mutationVersion));
            Assert.That(options.Method, Is.EqualTo(XmlCanonicalizationMethod.CanonicalXml11));
            Assert.That(options.IncludeComments, Is.True);
            Assert.That(output.EncodingName, Is.EqualTo("utf-16"));
        });
    }

    [Test]
    public void CanonicalizerRejectsUnsupportedMethodsBeforeWritingOutput()
    {
        XmlDocument document = Parse("<root/>");
        XmlByteCollector collector = new XmlByteCollector();
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.ByteSink, ByteSink = collector };
        XmlCanonicalizationOptions options = new XmlCanonicalizationOptions { Method = (XmlCanonicalizationMethod)99 };

        XmlCanonicalizationException exception = Assert.Throws<XmlCanonicalizationException>(() => new XmlCanonicalizer().CanonicalizeXml(document, output, options))!;

        Assert.Multiple(() =>
        {
            Assert.That(exception.Diagnostic!.Code, Is.EqualTo(XmlErrorCodes.CanonicalizationFailure));
            Assert.That(exception.Diagnostic.Category, Is.EqualTo(XmlErrorCategory.Canonicalization));
            Assert.That(collector.CountCollectedXmlBytes(), Is.Zero);
        });
    }

    [Test]
    public void CanonicalizerValidatesRequiredPublicArguments()
    {
        XmlCanonicalizer canonicalizer = new XmlCanonicalizer();
        XmlDocument document = Parse("<root/>");
        XmlOutput output = new XmlOutput { Kind = XmlOutputKind.ByteSink, ByteSink = new XmlByteCollector() };
        XmlCanonicalizationOptions options = new XmlCanonicalizationOptions { Method = XmlCanonicalizationMethod.CanonicalXml11 };

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => canonicalizer.CanonicalizeXml(null!, output, options));
            Assert.Throws<ArgumentNullException>(() => canonicalizer.CanonicalizeXml(document, null!, options));
            Assert.Throws<ArgumentNullException>(() => canonicalizer.CanonicalizeXml(document, output, null!));
        });
    }

    private static XmlDocument Parse(string source)
    {
        XmlParserOptions options = new XmlParserOptions
        {
            Version = XmlVersion.Xml10,
            NamespaceHandling = XmlNamespaceHandling.Namespaces10,
            DtdProcessing = XmlDtdProcessing.Prohibit,
            EntityHandling = XmlEntityHandling.Expand,
            ValidationMode = XmlValidationMode.None,
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            PreservePrefixes = true,
            RetainNamespaceDeclarations = true
        };
        return new XmlParser().ParseXmlDocument(new XmlInput { Kind = XmlInputKind.String, Text = source }, options, null, null, null);
    }
}
