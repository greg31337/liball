using System.Text;
using System.Xml;
using iso20022;

namespace iso20022.Tests;

public class SecureBoundedXmlReaderTests
{
    [Test]
    public void SettingsProhibitDtdsAndReflectExplicitContentPolicies()
    {
        MessageReadOptions options = new MessageReadOptions
        {
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            PreserveWhitespace = false,
            Limits = new ResourceLimits { MaxTotalBytes = 4096 }
        };

        XmlReaderSettings settings = SecureXmlReaderFactory.CreateSettings(options);

        Assert.Multiple(() =>
        {
            Assert.That(settings.DtdProcessing, Is.EqualTo(DtdProcessing.Prohibit));
            Assert.That(settings.CheckCharacters, Is.True);
            Assert.That(settings.CloseInput, Is.True);
            Assert.That(settings.ConformanceLevel, Is.EqualTo(ConformanceLevel.Auto));
            Assert.That(settings.IgnoreComments, Is.False);
            Assert.That(settings.IgnoreProcessingInstructions, Is.False);
            Assert.That(settings.IgnoreWhitespace, Is.True);
            Assert.That(settings.MaxCharactersFromEntities, Is.Zero);
            Assert.That(settings.MaxCharactersInDocument, Is.EqualTo(4096));
        });
    }

    [TestCase("<!DOCTYPE root><root/>")]
    [TestCase("<!DOCTYPE root [<!ENTITY value 'expanded'>]><root>&value;</root>")]
    [TestCase("<!DOCTYPE root [<!ENTITY ext SYSTEM 'file:///etc/passwd'>]><root>&ext;</root>")]
    public void DtdAndEntityFormsAreRejectedBeforeContentCanBeConsumed(string xml)
    {
        BoundedXmlReader reader = SecureXmlReaderFactory.CreateFromText(xml, new MessageReadOptions());

        XmlException? exception = Assert.Throws<XmlException>(() => ReadToEnd(reader));

        Assert.That(exception?.Message, Does.Contain("DTD").IgnoreCase);
    }

    [Test]
    public void ReaderSurfacesOrderedNodesNamespacesAndAttributesWithoutExposingTheRawReader()
    {
        string xml = "<p:root xmlns:p='urn:test'>before<!--comment--><?target data?><p:child id='17'/><![CDATA[after]]></p:root>";
        MessageReadOptions options = new MessageReadOptions
        {
            PreserveComments = true,
            PreserveProcessingInstructions = true,
            PreserveWhitespace = true
        };
        BoundedXmlReader reader = SecureXmlReaderFactory.CreateFromText(xml, options);
        List<XmlNodeType> nodeTypes = new List<XmlNodeType>();
        string childAttribute = string.Empty;
        bool childWasEmpty = false;

        try
        {
            while (reader.ReadNext())
            {
                nodeTypes.Add(reader.GetNodeType());
                if (reader.GetNodeType() == XmlNodeType.Element && reader.GetLocalName() == "child")
                {
                    Assert.That(reader.GetQualifiedName(), Is.EqualTo("p:child"));
                    Assert.That(reader.GetNamespaceUri(), Is.EqualTo("urn:test"));
                    Assert.That(reader.GetPrefix(), Is.EqualTo("p"));
                    Assert.That(reader.GetDepth(), Is.EqualTo(1));
                    Assert.That(reader.GetAttributeCount(), Is.EqualTo(1));
                    childWasEmpty = reader.GetIsEmptyElement();
                    Assert.That(reader.MoveToFirstAttribute(), Is.True);
                    Assert.That(reader.GetLocalName(), Is.EqualTo("id"));
                    childAttribute = reader.GetValue();
                    Assert.That(reader.MoveToNextAttribute(), Is.False);
                    Assert.That(reader.MoveBackToElement(), Is.True);
                }
            }
        }
        finally
        {
            reader.DisposeReader();
        }

        Assert.Multiple(() =>
        {
            Assert.That(nodeTypes, Does.Contain(XmlNodeType.Text));
            Assert.That(nodeTypes, Does.Contain(XmlNodeType.Comment));
            Assert.That(nodeTypes, Does.Contain(XmlNodeType.ProcessingInstruction));
            Assert.That(nodeTypes, Does.Contain(XmlNodeType.CDATA));
            Assert.That(childAttribute, Is.EqualTo("17"));
            Assert.That(childWasEmpty, Is.True);
        });
    }

    [Test]
    public void IgnoredCommentsProcessingInstructionsAndWhitespaceNeverReachConsumers()
    {
        string xml = "<root> \n<!--comment--><?target data?><child/></root>";
        MessageReadOptions options = new MessageReadOptions
        {
            PreserveComments = false,
            PreserveProcessingInstructions = false,
            PreserveWhitespace = false
        };
        BoundedXmlReader reader = SecureXmlReaderFactory.CreateFromText(xml, options);
        List<XmlNodeType> nodeTypes = new List<XmlNodeType>();

        try
        {
            while (reader.ReadNext())
            {
                nodeTypes.Add(reader.GetNodeType());
            }
        }
        finally
        {
            reader.DisposeReader();
        }

        Assert.Multiple(() =>
        {
            Assert.That(nodeTypes, Does.Not.Contain(XmlNodeType.Comment));
            Assert.That(nodeTypes, Does.Not.Contain(XmlNodeType.ProcessingInstruction));
            Assert.That(nodeTypes, Does.Not.Contain(XmlNodeType.Whitespace));
            Assert.That(nodeTypes.Count(type => type == XmlNodeType.Element), Is.EqualTo(2));
        });
    }

    [Test]
    public void ByteAndTextInputsAreRejectedWhenTheirEncodedSizeExceedsTheLimit()
    {
        byte[] bytes = Encoding.UTF8.GetBytes("<root>value</root>");
        MessageReadOptions byteOptions = new MessageReadOptions();
        byteOptions.Limits.MaxTotalBytes = bytes.Length - 1;
        string unicodeText = "<root>€</root>";
        MessageReadOptions textOptions = new MessageReadOptions();
        textOptions.Limits.MaxTotalBytes = unicodeText.Length;

        XmlException? byteException = Assert.Throws<XmlException>(() => SecureXmlReaderFactory.CreateFromBytes(bytes, byteOptions));
        XmlException? textException = Assert.Throws<XmlException>(() => SecureXmlReaderFactory.CreateFromText(unicodeText, textOptions));

        Assert.Multiple(() =>
        {
            Assert.That(byteException?.Message, Does.Contain("MaxTotalBytes"));
            Assert.That(textException?.Message, Does.Contain("MaxTotalBytes"));
        });
    }

    [TestCase("<a><b><c/></b></a>", "depth")]
    [TestCase("<a first='1' second='2'/>", "attributes")]
    [TestCase("<a>four</a>", "text")]
    [TestCase("<a value='four'/>", "attribute-value")]
    [TestCase("<a><b/><c/></a>", "elements")]
    [TestCase("<a><b/><c/></a>", "collection")]
    public void ReaderEnforcesEveryXmlStructureLimit(string xml, string limitKind)
    {
        MessageReadOptions options = new MessageReadOptions();
        string expectedLimit;
        if (limitKind == "depth")
        {
            options.Limits.MaxDepth = 2;
            expectedLimit = "MaxDepth";
        }
        else if (limitKind == "attributes")
        {
            options.Limits.MaxAttributesPerElement = 1;
            expectedLimit = "MaxAttributesPerElement";
        }
        else if (limitKind == "text" || limitKind == "attribute-value")
        {
            options.Limits.MaxTextLength = 3;
            expectedLimit = "MaxTextLength";
        }
        else if (limitKind == "elements")
        {
            options.Limits.MaxElementCount = 2;
            expectedLimit = "MaxElementCount";
        }
        else
        {
            options.Limits.MaxCollectionCount = 1;
            expectedLimit = "MaxCollectionCount";
        }

        BoundedXmlReader reader = SecureXmlReaderFactory.CreateFromText(xml, options);
        XmlException? exception = Assert.Throws<XmlException>(() => ReadToEnd(reader));

        Assert.That(exception?.Message, Does.Contain(expectedLimit));
    }

    [Test]
    public void SchemaAndDiagnosticCountersEnforceTheirConfiguredLimits()
    {
        ResourceLimits limits = new ResourceLimits
        {
            MaxSchemaCount = 2,
            MaxDiagnosticCount = 2
        };
        ResourceLimitGuard schemaGuard = new ResourceLimitGuard(limits);
        ResourceLimitGuard diagnosticGuard = new ResourceLimitGuard(limits);

        schemaGuard.RecordSchema();
        schemaGuard.RecordSchema();
        diagnosticGuard.RecordDiagnostic();
        diagnosticGuard.RecordDiagnostic();

        XmlException? schemaException = Assert.Throws<XmlException>(() => schemaGuard.RecordSchema());
        XmlException? diagnosticException = Assert.Throws<XmlException>(() => diagnosticGuard.RecordDiagnostic());
        Assert.Multiple(() =>
        {
            Assert.That(schemaException?.Message, Does.Contain("MaxSchemaCount"));
            Assert.That(diagnosticException?.Message, Does.Contain("MaxDiagnosticCount"));
        });
    }

    [TestCase("MaxDepth")]
    [TestCase("MaxAttributesPerElement")]
    [TestCase("MaxTextLength")]
    [TestCase("MaxElementCount")]
    [TestCase("MaxCollectionCount")]
    [TestCase("MaxTotalBytes")]
    [TestCase("MaxSchemaCount")]
    [TestCase("MaxDiagnosticCount")]
    public void EveryConfiguredLimitMustBePositive(string limitName)
    {
        ResourceLimits limits = new ResourceLimits();
        SetLimit(limits, limitName, 0);

        ArgumentOutOfRangeException? exception = Assert.Throws<ArgumentOutOfRangeException>(() => new ResourceLimitGuard(limits));

        Assert.That(exception?.Message, Does.Contain(limitName));
    }

    [Test]
    public void ExactBoundaryValuesAreAccepted()
    {
        string xml = "<a x='123'><b>123</b><c/></a>";
        byte[] bytes = Encoding.UTF8.GetBytes(xml);
        MessageReadOptions options = new MessageReadOptions
        {
            Limits = new ResourceLimits
            {
                MaxDepth = 2,
                MaxAttributesPerElement = 1,
                MaxTextLength = 3,
                MaxElementCount = 3,
                MaxCollectionCount = 2,
                MaxTotalBytes = bytes.Length,
                MaxSchemaCount = 1,
                MaxDiagnosticCount = 1
            }
        };
        BoundedXmlReader reader = SecureXmlReaderFactory.CreateFromBytes(bytes, options);

        Assert.DoesNotThrow(() => ReadToEnd(reader));
    }

    [Test]
    public void ActiveReaderUsesTheLimitSnapshotTakenAtConstruction()
    {
        MessageReadOptions options = new MessageReadOptions();
        options.Limits.MaxElementCount = 1;
        BoundedXmlReader reader = SecureXmlReaderFactory.CreateFromText("<root><child/></root>", options);
        options.Limits.MaxElementCount = 100;

        XmlException? exception = Assert.Throws<XmlException>(() => ReadToEnd(reader));

        Assert.That(exception?.Message, Does.Contain("MaxElementCount"));
    }

    internal static void ReadToEnd(BoundedXmlReader reader)
    {
        try
        {
            while (reader.ReadNext())
            {
            }
        }
        finally
        {
            reader.DisposeReader();
        }
    }

    internal static void SetLimit(ResourceLimits limits, string name, int value)
    {
        if (name == "MaxDepth")
        {
            limits.MaxDepth = value;
        }
        else if (name == "MaxAttributesPerElement")
        {
            limits.MaxAttributesPerElement = value;
        }
        else if (name == "MaxTextLength")
        {
            limits.MaxTextLength = value;
        }
        else if (name == "MaxElementCount")
        {
            limits.MaxElementCount = value;
        }
        else if (name == "MaxCollectionCount")
        {
            limits.MaxCollectionCount = value;
        }
        else if (name == "MaxTotalBytes")
        {
            limits.MaxTotalBytes = value;
        }
        else if (name == "MaxSchemaCount")
        {
            limits.MaxSchemaCount = value;
        }
        else
        {
            limits.MaxDiagnosticCount = value;
        }
    }
}
