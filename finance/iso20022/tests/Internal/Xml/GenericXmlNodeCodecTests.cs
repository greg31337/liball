using System.Text;
using System.Xml;
using iso20022;

namespace iso20022.Tests;

public class GenericXmlNodeCodecTests
{
    [Test]
    public void ReaderAndWriterRoundTripOrderedNamespacesAttributesMixedContentNilAndComments()
    {
        string xml = "<?xml version='1.0' encoding='utf-8'?><!--before--><?meta one?><r:root xmlns:r='urn:root' xmlns:a='urn:attributes' xmlns:xsi='http://www.w3.org/2001/XMLSchema-instance' plain='value' a:id='17'>text<![CDATA[raw<value]]><!--inside--><?go now?><r:child xsi:nil='1'/></r:root><!--after-->";
        MessageReadOptions readOptions = new MessageReadOptions
        {
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            PreserveWhitespace = true
        };
        GenericXmlNodeReader reader = new GenericXmlNodeReader(readOptions);

        DynamicXmlNode first = reader.ReadDocumentFromText(xml, "message.xml");

        Assert.That(first.Kind, Is.EqualTo(XmlNodeKind.Document));
        Assert.That(first.Children.Select(node => node.Kind), Is.EqualTo(new[] { XmlNodeKind.Comment, XmlNodeKind.ProcessingInstruction, XmlNodeKind.Element, XmlNodeKind.Comment }));
        DynamicXmlNode root = first.Children[2];
        Assert.Multiple(() =>
        {
            Assert.That(root.Name.NamespaceUri, Is.EqualTo("urn:root"));
            Assert.That(root.Name.LocalName, Is.EqualTo("root"));
            Assert.That(root.Name.Prefix, Is.EqualTo("r"));
            Assert.That(root.NamespaceDeclarations.Select(value => value.Prefix), Is.EqualTo(new[] { "r", "a", "xsi" }));
            Assert.That(root.Attributes.Select(value => value.Name.LocalName), Is.EqualTo(new[] { "plain", "id" }));
            Assert.That(root.Attributes[0].Name.NamespaceUri, Is.Empty);
            Assert.That(root.Attributes[1].Name.NamespaceUri, Is.EqualTo("urn:attributes"));
            Assert.That(root.Attributes[1].IsSpecified, Is.True);
            Assert.That(root.Children.Select(node => node.Kind), Is.EqualTo(new[] { XmlNodeKind.Text, XmlNodeKind.CData, XmlNodeKind.Comment, XmlNodeKind.ProcessingInstruction, XmlNodeKind.Element }));
            Assert.That(root.Children[0].Value, Is.EqualTo("text"));
            Assert.That(root.Children[1].Value, Is.EqualTo("raw<value"));
            Assert.That(root.Children[2].Value, Is.EqualTo("inside"));
            Assert.That(root.Children[3].Name.LocalName, Is.EqualTo("go"));
            Assert.That(root.Children[3].Value, Is.EqualTo("now"));
            Assert.That(root.Children[4].IsNil, Is.True);
            Assert.That(root.Children[4].IsEmptyElement, Is.True);
            Assert.That(root.Children[4].Attributes.Single().Value, Is.EqualTo("1"));
            Assert.That(root.Location.SourceName, Is.EqualTo("message.xml"));
            Assert.That(root.Location.HasLineInformation, Is.True);
        });

        MessageWriteOptions writeOptions = new MessageWriteOptions
        {
            FormattingMode = XmlFormattingMode.Preserve,
            WriteXmlDeclaration = true,
            PreservePrefixes = true,
            PreserveLexicalValues = true
        };
        GenericXmlNodeWriter writer = new GenericXmlNodeWriter(writeOptions);
        string written = writer.WriteText(first);
        DynamicXmlNode second = reader.ReadDocumentFromText(written, "roundtrip.xml");

        Assert.Multiple(() =>
        {
            Assert.That(written, Does.StartWith("<?xml"));
            Assert.That(written, Does.Contain("xmlns:r=\"urn:root\""));
            Assert.That(written, Does.Contain("a:id=\"17\""));
            Assert.That(written, Does.Contain("<![CDATA[raw<value]]>"));
            Assert.That(written, Does.Contain("xsi:nil=\"1\""));
            Assert.That(written, Does.Contain("<r:child xsi:nil=\"1\" />").Or.Contain("<r:child xsi:nil=\"1\"/>"));
        });
        AssertEquivalent(first, second);
    }

    [Test]
    public void ReaderAppliesCommentProcessingInstructionAndWhitespacePolicies()
    {
        string xml = "<root> \n<!--comment--><?target value?><child/> \n</root>";
        GenericXmlNodeReader preservingReader = new GenericXmlNodeReader(new MessageReadOptions
        {
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            PreserveWhitespace = true
        });
        GenericXmlNodeReader filteringReader = new GenericXmlNodeReader(new MessageReadOptions
        {
            PreserveComments = false,
            PreserveProcessingInstructions = false,
            PreserveWhitespace = false
        });

        DynamicXmlNode preservingRoot = preservingReader.ReadDocumentFromText(xml, null).Children.Single(node => node.Kind == XmlNodeKind.Element);
        DynamicXmlNode filteringRoot = filteringReader.ReadDocumentFromText(xml, null).Children.Single(node => node.Kind == XmlNodeKind.Element);

        Assert.Multiple(() =>
        {
            Assert.That(preservingRoot.Children.Select(node => node.Kind), Is.EqualTo(new[] { XmlNodeKind.Text, XmlNodeKind.Comment, XmlNodeKind.ProcessingInstruction, XmlNodeKind.Element, XmlNodeKind.Text }));
            Assert.That(filteringRoot.Children, Has.Count.EqualTo(1));
            Assert.That(filteringRoot.Children[0].Kind, Is.EqualTo(XmlNodeKind.Element));
        });
    }

    [TestCase("truth")]
    [TestCase("TRUE")]
    [TestCase("")]
    [TestCase("2")]
    public void ReaderRejectsInvalidXsiNilLexicalForms(string value)
    {
        string xml = "<root xmlns:xsi='" + Iso20022Namespaces.XmlSchemaInstance + "' xsi:nil='" + value + "'/>";
        GenericXmlNodeReader reader = new GenericXmlNodeReader(new MessageReadOptions());

        XmlException? exception = Assert.Throws<XmlException>(() => reader.ReadDocumentFromText(xml, null));

        Assert.That(exception?.Message, Does.Contain("xsi:nil"));
    }

    [Test]
    public void WriterSynthesizesRequiredBindingsNilAndDefaultNamespaceUndeclarations()
    {
        DynamicXmlNode document = new DynamicXmlNode { Kind = XmlNodeKind.Document };
        DynamicXmlNode root = new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData { LocalName = "Root", NamespaceUri = "urn:default" }
        };
        root.Attributes.Add(new XmlAttributeData
        {
            Name = new QualifiedNameData { Prefix = "a", LocalName = "id", NamespaceUri = "urn:attributes" },
            Value = "17",
            IsSpecified = true
        });
        DynamicXmlNode child = new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData { LocalName = "Child" },
            IsNil = true,
            IsEmptyElement = true
        };
        root.Children.Add(child);
        document.Children.Add(root);
        GenericXmlNodeWriter writer = new GenericXmlNodeWriter(new MessageWriteOptions { WriteXmlDeclaration = false });

        string xml = writer.WriteText(document);

        Assert.Multiple(() =>
        {
            Assert.That(xml, Does.Contain("xmlns=\"urn:default\""));
            Assert.That(xml, Does.Contain("xmlns:a=\"urn:attributes\""));
            Assert.That(xml, Does.Contain("a:id=\"17\""));
            Assert.That(xml, Does.Contain("xmlns=\"\""));
            Assert.That(xml, Does.Contain("xmlns:xsi=\"" + Iso20022Namespaces.XmlSchemaInstance + "\""));
            Assert.That(xml, Does.Contain("xsi:nil=\"true\""));
        });

        GenericXmlNodeReader reader = new GenericXmlNodeReader(new MessageReadOptions());
        DynamicXmlNode parsedRoot = reader.ReadDocumentFromText(xml, null).Children.Single();
        DynamicXmlNode parsedChild = parsedRoot.Children.Single();
        Assert.Multiple(() =>
        {
            Assert.That(parsedRoot.Name.NamespaceUri, Is.EqualTo("urn:default"));
            Assert.That(parsedChild.Name.NamespaceUri, Is.Empty);
            Assert.That(parsedChild.IsNil, Is.True);
        });
    }

    [Test]
    public void WriterRejectsNilStateConflictsAndUnprefixedNamespacedAttributes()
    {
        DynamicXmlNode conflictingNil = new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData { LocalName = "Root" },
            IsNil = false,
            Attributes = new List<XmlAttributeData>
            {
                new XmlAttributeData
                {
                    Name = new QualifiedNameData { Prefix = "xsi", LocalName = "nil", NamespaceUri = Iso20022Namespaces.XmlSchemaInstance },
                    Value = "true"
                }
            }
        };
        DynamicXmlNode invalidAttribute = new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData { LocalName = "Root" },
            Attributes = new List<XmlAttributeData>
            {
                new XmlAttributeData { Name = new QualifiedNameData { LocalName = "id", NamespaceUri = "urn:attributes" }, Value = "17" }
            }
        };
        GenericXmlNodeWriter writer = new GenericXmlNodeWriter(new MessageWriteOptions { WriteXmlDeclaration = false });

        XmlException? nilException = Assert.Throws<XmlException>(() => writer.WriteText(conflictingNil));
        XmlException? attributeException = Assert.Throws<XmlException>(() => writer.WriteText(invalidAttribute));

        Assert.Multiple(() =>
        {
            Assert.That(nilException?.Message, Does.Contain("conflicts"));
            Assert.That(attributeException?.Message, Does.Contain("explicit prefix"));
        });
    }

    [Test]
    public void EmptyElementStyleAndUtf8BomPolicyRoundTripAcrossByteInputAndOutput()
    {
        byte[] input = Encoding.UTF8.GetBytes("<root><short/><full></full></root>");
        GenericXmlNodeReader reader = new GenericXmlNodeReader(new MessageReadOptions());
        DynamicXmlNode document = reader.ReadDocumentFromBytes(input, "bytes.xml");
        DynamicXmlNode root = document.Children.Single();

        Assert.Multiple(() =>
        {
            Assert.That(root.Children[0].IsEmptyElement, Is.True);
            Assert.That(root.Children[1].IsEmptyElement, Is.False);
        });

        GenericXmlNodeWriter writer = new GenericXmlNodeWriter(new MessageWriteOptions
        {
            WriteXmlDeclaration = true,
            WriteByteOrderMark = true,
            FormattingMode = XmlFormattingMode.Preserve
        });
        byte[] output = writer.WriteBytes(document);
        string text = writer.WriteText(document);

        Assert.Multiple(() =>
        {
            Assert.That(output.Take(3), Is.EqualTo(new byte[] { 0xEF, 0xBB, 0xBF }));
            Assert.That(text[0], Is.Not.EqualTo('\uFEFF'));
            Assert.That(text, Does.Contain("<short />").Or.Contain("<short/>"));
            Assert.That(text, Does.Contain("<full></full>"));
        });

        Assert.DoesNotThrow(() => reader.ReadDocumentFromBytes(output, "output.xml"));
    }

    [Test]
    public void UnknownWriterNodeKindsFollowTheConfiguredUnknownContentPolicy()
    {
        DynamicXmlNode document = new DynamicXmlNode { Kind = XmlNodeKind.Document };
        document.Children.Add(new DynamicXmlNode { Kind = (XmlNodeKind)999, Value = "future" });
        document.Children.Add(new DynamicXmlNode { Kind = XmlNodeKind.Element, Name = new QualifiedNameData { LocalName = "Root" }, IsEmptyElement = true });
        GenericXmlNodeWriter rejectingWriter = new GenericXmlNodeWriter(new MessageWriteOptions
        {
            WriteXmlDeclaration = false,
            UnknownContentMode = UnknownContentMode.Reject
        });
        GenericXmlNodeWriter ignoringWriter = new GenericXmlNodeWriter(new MessageWriteOptions
        {
            WriteXmlDeclaration = false,
            UnknownContentMode = UnknownContentMode.Ignore
        });

        Assert.Throws<XmlException>(() => rejectingWriter.WriteText(document));
        Assert.That(ignoringWriter.WriteText(document), Does.Contain("<Root"));
    }

    [Test]
    public void IndentedWritingUsesTheCallerSelectedIndentAndNewLineText()
    {
        DynamicXmlNode document = new DynamicXmlNode { Kind = XmlNodeKind.Document };
        DynamicXmlNode root = new DynamicXmlNode { Kind = XmlNodeKind.Element, Name = new QualifiedNameData { LocalName = "Root" } };
        root.Children.Add(new DynamicXmlNode { Kind = XmlNodeKind.Element, Name = new QualifiedNameData { LocalName = "Child" }, IsEmptyElement = true });
        document.Children.Add(root);
        GenericXmlNodeWriter writer = new GenericXmlNodeWriter(new MessageWriteOptions
        {
            FormattingMode = XmlFormattingMode.Indented,
            WriteXmlDeclaration = false,
            IndentText = "\t",
            NewLineText = "\r\n"
        });

        string xml = writer.WriteText(document);

        Assert.That(xml, Does.Contain("\r\n\t<Child"));
    }

    [Test]
    public void WriterEscapesDecodedAttributeAndTextValuesWithoutChangingTheirMeaning()
    {
        DynamicXmlNode root = new DynamicXmlNode
        {
            Kind = XmlNodeKind.Element,
            Name = new QualifiedNameData { LocalName = "Root" },
            Attributes = new List<XmlAttributeData>
            {
                new XmlAttributeData { Name = new QualifiedNameData { LocalName = "value" }, Value = "a&\"<b" }
            },
            Children = new List<DynamicXmlNode>
            {
                new DynamicXmlNode { Kind = XmlNodeKind.Text, Value = "text<&value" }
            }
        };
        GenericXmlNodeWriter writer = new GenericXmlNodeWriter(new MessageWriteOptions { WriteXmlDeclaration = false });

        string xml = writer.WriteText(root);
        DynamicXmlNode parsed = new GenericXmlNodeReader(new MessageReadOptions()).ReadDocumentFromText(xml, null).Children.Single();

        Assert.Multiple(() =>
        {
            Assert.That(xml, Does.Contain("value=\"a&amp;&quot;&lt;b\""));
            Assert.That(xml, Does.Contain("text&lt;&amp;value"));
            Assert.That(parsed.Attributes[0].Value, Is.EqualTo("a&\"<b"));
            Assert.That(parsed.Children[0].Value, Is.EqualTo("text<&value"));
        });
    }

    internal static void AssertEquivalent(DynamicXmlNode expected, DynamicXmlNode actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Kind, Is.EqualTo(expected.Kind));
            Assert.That(actual.Name.NamespaceUri, Is.EqualTo(expected.Name.NamespaceUri));
            Assert.That(actual.Name.LocalName, Is.EqualTo(expected.Name.LocalName));
            Assert.That(actual.Name.Prefix, Is.EqualTo(expected.Name.Prefix));
            Assert.That(actual.Value, Is.EqualTo(expected.Value));
            Assert.That(actual.IsNil, Is.EqualTo(expected.IsNil));
            Assert.That(actual.IsEmptyElement, Is.EqualTo(expected.IsEmptyElement));
            Assert.That(actual.NamespaceDeclarations.Select(value => (value.Prefix, value.NamespaceUri)), Is.EqualTo(expected.NamespaceDeclarations.Select(value => (value.Prefix, value.NamespaceUri))));
            Assert.That(actual.Attributes.Select(value => (value.Name.NamespaceUri, value.Name.LocalName, value.Name.Prefix, value.Value)), Is.EqualTo(expected.Attributes.Select(value => (value.Name.NamespaceUri, value.Name.LocalName, value.Name.Prefix, value.Value))));
            Assert.That(actual.Children, Has.Count.EqualTo(expected.Children.Count));
        });

        int index = 0;
        while (index < expected.Children.Count)
        {
            AssertEquivalent(expected.Children[index], actual.Children[index]);
            index++;
        }
    }
}
