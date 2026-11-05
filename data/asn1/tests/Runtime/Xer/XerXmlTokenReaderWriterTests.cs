using asn1;

namespace asn1.Tests;

public class XerXmlTokenReaderWriterTests
{
    [Test]
    public void OneByteChunksPreserveEveryXmlLexicalConstruct()
    {
        string xml = "\uFEFF<p:\U00010000 xmlns:p=\"urn:x\" a=\"A&amp;&#x1F600;\">"
            + "Hi&lt;<![CDATA[x<y]]><!--ok--><?go data?>"
            + "</p:\U00010000>";
        byte[] data = System.Text.Encoding.UTF8.GetBytes(xml);
        XerXmlTokenReader reader = new XerXmlTokenReader(Limits());
        System.Collections.Generic.List<XerXmlToken> tokens =
            new System.Collections.Generic.List<XerXmlToken>();
        XerXmlReadResult result = new XerXmlReadResult();
        int index = 0;
        while (index < data.Length)
        {
            result = reader.ConsumeXml(new AsnIncrementalInput
            {
                Data = data,
                Offset = index,
                HasCount = true,
                Count = 1,
                IsFinalBlock = index == data.Length - 1
            });
            tokens.AddRange(result.Tokens);
            if (index < data.Length - 1)
            {
                Assert.That(result.Status,
                    Is.EqualTo(AsnDecodeStatus.NeedMoreData),
                    "byte " + index + ": " + result.ErrorCode
                        + " " + result.ErrorMessage);
            }

            index = index + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(result.Complete, Is.True);
            Assert.That(tokens.Select(item => item.Kind), Is.EqualTo(
                new[]
                {
                    XerXmlTokenKind.StartElement,
                    XerXmlTokenKind.Text,
                    XerXmlTokenKind.CData,
                    XerXmlTokenKind.Comment,
                    XerXmlTokenKind.ProcessingInstruction,
                    XerXmlTokenKind.EndElement
                }));
            Assert.That(tokens[0].Prefix, Is.EqualTo("p"));
            Assert.That(tokens[0].ByteOffset, Is.EqualTo(3));
            Assert.That(tokens[0].LocalName, Is.EqualTo("\U00010000"));
            Assert.That(tokens[0].Attributes[0].NamespaceDeclaration,
                Is.True);
            Assert.That(tokens[0].Attributes[0].NamespacePrefix,
                Is.EqualTo("p"));
            Assert.That(tokens[0].Attributes[1].Value,
                Is.EqualTo("A&\U0001F600"));
            Assert.That(tokens[1].Value, Is.EqualTo("Hi<"));
            Assert.That(tokens[2].Value, Is.EqualTo("x<y"));
            Assert.That(tokens[3].Value, Is.EqualTo("ok"));
            Assert.That(tokens[4].Name, Is.EqualTo("go"));
            Assert.That(tokens[4].Value, Is.EqualTo("data"));
            Assert.That(tokens[5].ByteOffset,
                Is.EqualTo(data.Length
                    - System.Text.Encoding.UTF8.GetByteCount("</p:\U00010000>")));
        });
    }

    [Test]
    public void FinalInputDiagnosesNamesReferencesAndUtf8Exactly()
    {
        XerXmlReadResult name = ReadFinal("<1/>", Limits());
        XerXmlReadResult entity = ReadFinal("<a>&unknown;</a>", Limits());
        XerXmlReadResult character = ReadFinal("<a>&#x0;</a>", Limits());
        XerXmlTokenReader utf8Reader = new XerXmlTokenReader(Limits());
        XerXmlReadResult utf8 = utf8Reader.ConsumeXml(
            new AsnIncrementalInput
            {
                Data = [0x3c, 0x61, 0x3e, 0xc0, 0x80],
                IsFinalBlock = true
            });

        Assert.Multiple(() =>
        {
            AssertError(name, "ASNXER0013", 1);
            AssertError(entity, "ASNXER0023", 3);
            AssertError(character, "ASNXER0025", 3);
            AssertError(utf8, "ASNXER0002", 3);
        });
    }

    [Test]
    public void CompletionDiagnosesTruncatedUtf8AndMarkup()
    {
        XerXmlTokenReader utf8Reader = new XerXmlTokenReader(Limits());
        XerXmlReadResult waitingUtf8 = utf8Reader.ConsumeXml(
            new AsnIncrementalInput { Data = [0xf0, 0x90] });
        XerXmlReadResult utf8 = utf8Reader.CompleteXml();
        XerXmlTokenReader markupReader = new XerXmlTokenReader(Limits());
        XerXmlReadResult waitingMarkup = markupReader.ConsumeXml(
            new AsnIncrementalInput
            {
                Data = System.Text.Encoding.UTF8.GetBytes("<root a=\"")
            });
        XerXmlReadResult markup = markupReader.CompleteXml();

        Assert.Multiple(() =>
        {
            Assert.That(waitingUtf8.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            AssertError(utf8, "ASNXER0003", 2);
            Assert.That(waitingMarkup.Status,
                Is.EqualTo(AsnDecodeStatus.NeedMoreData));
            AssertError(markup, "ASNXER0018", 6);
        });
    }

    [Test]
    public void ReaderEnforcesNestingAndAllocationLimits()
    {
        AsnResourceLimits depthLimits = Limits();
        depthLimits.MaximumNestingDepth = 1;
        XerXmlReadResult depth = ReadFinal(
            "<a><b></b></a>",
            depthLimits);
        AsnResourceLimits allocationLimits = Limits();
        allocationLimits.MaximumTotalAllocationBytes = 3;
        XerXmlReadResult allocation = ReadFinal("<a/>", allocationLimits);

        Assert.Multiple(() =>
        {
            AssertError(depth, "ASNR0001", 3);
            Assert.That(depth.LimitExceeded, Is.True);
            AssertError(allocation, "ASNR0005", 0);
            Assert.That(allocation.LimitExceeded, Is.True);
        });
    }

    [Test]
    public void WriterEscapesTextAttributesAndSplitCDataRoundTrips()
    {
        WriteOnlyStream output = new WriteOnlyStream();
        XerXmlTokenWriter writer = new XerXmlTokenWriter(output, Limits());
        XerXmlAttribute declaration = new XerXmlAttribute
        {
            Name = "xmlns:p",
            Prefix = "xmlns",
            LocalName = "p",
            NamespaceDeclaration = true,
            NamespacePrefix = "p",
            Value = "urn:x"
        };
        XerXmlAttribute attribute = new XerXmlAttribute
        {
            Name = "a",
            LocalName = "a",
            Value = "\"&<\n"
        };
        Assert.That(writer.WriteToken(new XerXmlToken
        {
            Kind = XerXmlTokenKind.StartElement,
            Name = "p:root",
            Attributes = [declaration, attribute]
        }).Success, Is.True);
        Assert.That(writer.WriteToken(new XerXmlToken
        {
            Kind = XerXmlTokenKind.Text,
            Value = "A<&>"
        }).Success, Is.True);
        Assert.That(writer.WriteToken(new XerXmlToken
        {
            Kind = XerXmlTokenKind.CData,
            Value = "x]]>y"
        }).Success, Is.True);
        Assert.That(writer.WriteToken(new XerXmlToken
        {
            Kind = XerXmlTokenKind.Comment,
            Value = "ok"
        }).Success, Is.True);
        Assert.That(writer.WriteToken(new XerXmlToken
        {
            Kind = XerXmlTokenKind.ProcessingInstruction,
            Name = "go",
            Value = "data"
        }).Success, Is.True);
        Assert.That(writer.WriteToken(new XerXmlToken
        {
            Kind = XerXmlTokenKind.EndElement,
            Name = "p:root"
        }).Success, Is.True);
        XerXmlWriteResult complete = writer.CompleteWriter();
        string xml = System.Text.Encoding.UTF8.GetString(output.ToArray());
        XerXmlReadResult parsed = ReadFinal(xml, Limits());

        Assert.Multiple(() =>
        {
            Assert.That(complete.Success, Is.True);
            Assert.That(complete.Complete, Is.True);
            Assert.That(complete.WrittenCount, Is.EqualTo(output.LengthValue));
            Assert.That(output.MaximumWriteCount, Is.EqualTo(1));
            Assert.That(xml, Is.EqualTo(
                "<p:root xmlns:p=\"urn:x\" a=\"&quot;&amp;&lt;&#xA;\">"
                + "A&lt;&amp;&gt;<![CDATA[x]]]]><![CDATA[>y]]>"
                + "<!--ok--><?go data?></p:root>"));
            Assert.That(parsed.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(parsed.Tokens.Where(
                    item => item.Kind == XerXmlTokenKind.CData)
                .Select(item => item.Value),
                Is.EqualTo(new[] { "x]]", ">y" }));
        });
    }

    [Test]
    public void WriterRejectsInvalidNamesCommentsAndResourceUse()
    {
        WriteOnlyStream invalidOutput = new WriteOnlyStream();
        XerXmlWriteResult invalid = new XerXmlTokenWriter(
            invalidOutput,
            Limits()).WriteToken(new XerXmlToken
            {
                Kind = XerXmlTokenKind.Comment,
                Value = "bad--comment"
            });
        WriteOnlyStream nameOutput = new WriteOnlyStream();
        XerXmlWriteResult name = new XerXmlTokenWriter(
            nameOutput,
            Limits()).WriteToken(new XerXmlToken
            {
                Kind = XerXmlTokenKind.EmptyElement,
                Name = "1bad"
            });
        AsnResourceLimits limits = Limits();
        limits.MaximumTextCharacters = 3;
        WriteOnlyStream limitedOutput = new WriteOnlyStream();
        XerXmlWriteResult limited = new XerXmlTokenWriter(
            limitedOutput,
            limits).WriteToken(new XerXmlToken
            {
                Kind = XerXmlTokenKind.Text,
                Value = "four"
            });

        Assert.Multiple(() =>
        {
            Assert.That(invalid.ErrorCode, Is.EqualTo("ASNXER0037"));
            Assert.That(invalidOutput.ToArray(), Is.Empty);
            Assert.That(name.ErrorCode, Is.EqualTo("ASNXER0033"));
            Assert.That(nameOutput.ToArray(), Is.Empty);
            Assert.That(limited.ErrorCode, Is.EqualTo("ASNR0009"));
            Assert.That(limited.LimitExceeded, Is.True);
            Assert.That(limitedOutput.ToArray(), Is.Empty);
        });
    }

    [Test]
    public void ReaderResolvesElementAttributeAndQNameNamespaces()
    {
        XerXmlReadResult result = ReadFinal(
            "<root xmlns=\"urn:root\" xmlns:p=\"urn:value\" "
                + "plain=\"x\" p:id=\"7\"><p:child/></root>",
            Limits());
        XerXmlToken root = result.Tokens[0];
        XerXmlToken child = result.Tokens[1];
        XerXmlNamespaceResolver resolver = new XerXmlNamespaceResolver();
        XerXmlQualifiedName defaultValue = resolver.ResolveQNameValue(
            "thing",
            root.NamespaceBindings);
        XerXmlQualifiedName prefixedValue = resolver.ResolveQNameValue(
            "p:thing",
            root.NamespaceBindings);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(root.NamespaceUri, Is.EqualTo("urn:root"));
            Assert.That(root.Attributes[2].NamespaceUri, Is.Empty);
            Assert.That(root.Attributes[3].NamespaceUri,
                Is.EqualTo("urn:value"));
            Assert.That(child.NamespaceUri, Is.EqualTo("urn:value"));
            Assert.That(defaultValue.Success, Is.True);
            Assert.That(defaultValue.NamespaceUri, Is.EqualTo("urn:root"));
            Assert.That(prefixedValue.Success, Is.True);
            Assert.That(prefixedValue.NamespaceUri, Is.EqualTo("urn:value"));
            Assert.That(resolver.ResolveQNameValue(
                "missing:name",
                root.NamespaceBindings).ErrorCode,
                Is.EqualTo("ASNXER0029"));
        });
    }

    [TestCase("<p:a/>", "ASNXER0029")]
    [TestCase("<a xmlns:xml=\"urn:wrong\"/>", "ASNXER0051")]
    [TestCase("<a xmlns:xmlns=\"urn:x\"/>", "ASNXER0051")]
    [TestCase("<a xmlns:p=\"urn:x\" xmlns:q=\"urn:x\" p:id=\"1\" q:id=\"2\"/>", "ASNXER0049")]
    [TestCase("<a><b></a>", "ASNXER0043")]
    [TestCase("<p:a xmlns:p=\"urn:x\" xmlns:q=\"urn:x\"></q:a>", "ASNXER0043")]
    [TestCase("<a/><b/>", "ASNXER0050")]
    [TestCase("text<a/>", "ASNXER0040")]
    public void ReaderRejectsNamespaceAndDocumentWellFormednessViolations(
        string xml,
        string code)
    {
        XerXmlReadResult result = ReadFinal(xml, Limits());

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
            Assert.That(result.ErrorCode, Is.EqualTo(code));
        });
    }

    [Test]
    public void ReaderParsesUtf8XmlDeclarationAndRejectsLateDeclaration()
    {
        XerXmlReadResult valid = ReadFinal(
            "<?xml version=\"1.0\" encoding='utf-8' standalone=\"yes\"?><a/>",
            Limits());
        XerXmlReadResult late = ReadFinal(
            "<!--before--><?xml version=\"1.0\"?><a/>",
            Limits());
        XerXmlReadResult encoding = ReadFinal(
            "<?xml version=\"1.0\" encoding=\"UTF-16\"?><a/>",
            Limits());

        Assert.Multiple(() =>
        {
            Assert.That(valid.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(valid.Tokens[0].Kind,
                Is.EqualTo(XerXmlTokenKind.XmlDeclaration));
            Assert.That(valid.Tokens[0].XmlVersion, Is.EqualTo("1.0"));
            Assert.That(valid.Tokens[0].XmlEncoding, Is.EqualTo("utf-8"));
            Assert.That(valid.Tokens[0].HasStandalone, Is.True);
            Assert.That(valid.Tokens[0].Standalone, Is.True);
            Assert.That(late.ErrorCode, Is.EqualTo("ASNXER0041"));
            Assert.That(encoding.ErrorCode, Is.EqualTo("ASNXER0046"));
        });
    }

    [Test]
    public void WriterAssignsStablePrefixesAndEmitsValidDeclaration()
    {
        WriteOnlyStream output = new WriteOnlyStream();
        XerXmlTokenWriter writer = new XerXmlTokenWriter(output, Limits());
        Assert.That(writer.WriteToken(new XerXmlToken
        {
            Kind = XerXmlTokenKind.XmlDeclaration,
            XmlVersion = "1.0",
            XmlEncoding = "UTF-8"
        }).Success, Is.True);
        Assert.That(writer.WriteToken(new XerXmlToken
        {
            Kind = XerXmlTokenKind.StartElement,
            LocalName = "root",
            NamespaceUri = "urn:root",
            Attributes =
            [
                new XerXmlAttribute
                {
                    LocalName = "id",
                    NamespaceUri = "urn:attribute",
                    Value = "7"
                }
            ]
        }).Success, Is.True);
        Assert.That(writer.WriteToken(new XerXmlToken
        {
            Kind = XerXmlTokenKind.EmptyElement,
            LocalName = "child",
            NamespaceUri = "urn:root"
        }).Success, Is.True);
        Assert.That(writer.WriteToken(new XerXmlToken
        {
            Kind = XerXmlTokenKind.EndElement,
            LocalName = "root",
            NamespaceUri = "urn:root"
        }).Success, Is.True);
        XerXmlWriteResult complete = writer.CompleteWriter();
        string xml = System.Text.Encoding.UTF8.GetString(output.ToArray());
        XerXmlReadResult read = ReadFinal(xml, Limits());

        Assert.Multiple(() =>
        {
            Assert.That(complete.Success, Is.True);
            Assert.That(xml, Is.EqualTo(
                "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                + "<ns1:root xmlns:ns1=\"urn:root\" "
                + "xmlns:ns2=\"urn:attribute\" ns2:id=\"7\">"
                + "<ns1:child/></ns1:root>"));
            Assert.That(read.Status, Is.EqualTo(AsnDecodeStatus.Done));
            Assert.That(read.Tokens[1].NamespaceUri,
                Is.EqualTo("urn:root"));
            Assert.That(read.Tokens[1].Attributes[2].NamespaceUri,
                Is.EqualTo("urn:attribute"));
        });
    }

    private static XerXmlReadResult ReadFinal(
        string xml,
        AsnResourceLimits limits)
    {
        return new XerXmlTokenReader(limits).ConsumeXml(
            new AsnIncrementalInput
            {
                Data = System.Text.Encoding.UTF8.GetBytes(xml),
                IsFinalBlock = true
            });
    }

    private static void AssertError(
        XerXmlReadResult result,
        string code,
        int offset)
    {
        Assert.That(result.Status, Is.EqualTo(AsnDecodeStatus.Error));
        Assert.That(result.ErrorCode, Is.EqualTo(code));
        Assert.That(result.ViolationOffset, Is.EqualTo(offset));
    }

    private static AsnResourceLimits Limits()
    {
        return new AsnResourceLimits
        {
            MaximumNestingDepth = 16,
            MaximumCollectionElements = 128,
            MaximumTextCharacters = 4096,
            MaximumTotalAllocationBytes = 65536
        };
    }

    private sealed class WriteOnlyStream : System.IO.Stream
    {
        private readonly System.IO.MemoryStream storage =
            new System.IO.MemoryStream();

        public int MaximumWriteCount;

        public int LengthValue => (int)this.storage.Length;

        public override bool CanRead => false;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new System.NotSupportedException();

        public override long Position
        {
            get => throw new System.NotSupportedException();
            set => throw new System.NotSupportedException();
        }

        public byte[] ToArray()
        {
            return this.storage.ToArray();
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            throw new System.NotSupportedException();
        }

        public override long Seek(long offset, System.IO.SeekOrigin origin)
        {
            throw new System.NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new System.NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            if (count > this.MaximumWriteCount)
            {
                this.MaximumWriteCount = count;
            }

            this.storage.Write(buffer, offset, count);
        }
    }
}
