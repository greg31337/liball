using iso20022;

namespace iso20022.Tests;

public class FoundationDataTests
{
    [Test]
    public void PrimitiveModelsRetainValueAndLexicalState()
    {
        IsoDecimalValue number = new IsoDecimalValue
        {
            Value = decimal.Zero,
            Scale = 4,
            IsNegativeZero = true,
            LexicalValue = "-0.0000"
        };
        IsoBinaryValue binary = new IsoBinaryValue
        {
            Value = new byte[] { 0, 127, 255 },
            Encoding = BinaryEncodingKind.Hexadecimal,
            LexicalValue = "007FFF"
        };
        IsoDateTimeValue dateTime = new IsoDateTimeValue
        {
            Date = new IsoDateValue
            {
                Year = 2026,
                Month = 8,
                Day = 31,
                HasTimezone = true,
                TimezoneOffsetMinutes = -420,
                IsUtc = false,
                LexicalMode = DateTimeLexicalMode.Preserve,
                LexicalValue = "2026-08-31-07:00"
            },
            Time = new IsoTimeValue
            {
                Hour = 9,
                Minute = 5,
                Second = 7,
                FractionalSecondDigits = "1200",
                HasTimezone = true,
                TimezoneOffsetMinutes = -420,
                IsUtc = false,
                LexicalMode = DateTimeLexicalMode.Preserve,
                LexicalValue = "09:05:07.1200-07:00"
            },
            LexicalMode = DateTimeLexicalMode.Preserve,
            LexicalValue = "2026-08-31T09:05:07.1200-07:00"
        };
        IsoAmountValue amount = new IsoAmountValue
        {
            Amount = number,
            Currency = "USD"
        };

        Assert.Multiple(() =>
        {
            Assert.That(amount.Amount, Is.SameAs(number));
            Assert.That(amount.Amount.Scale, Is.EqualTo(4));
            Assert.That(amount.Amount.IsNegativeZero, Is.True);
            Assert.That(amount.Amount.LexicalValue, Is.EqualTo("-0.0000"));
            Assert.That(binary.Value, Is.EqualTo(new byte[] { 0, 127, 255 }));
            Assert.That(binary.Encoding, Is.EqualTo(BinaryEncodingKind.Hexadecimal));
            Assert.That(dateTime.Date.TimezoneOffsetMinutes, Is.EqualTo(-420));
            Assert.That(dateTime.Time.FractionalSecondDigits, Is.EqualTo("1200"));
            Assert.That(dateTime.LexicalValue, Is.EqualTo("2026-08-31T09:05:07.1200-07:00"));
            Assert.That(amount.Currency, Is.EqualTo("USD"));
        });
    }

    [Test]
    public void RemainingPrimitiveModelsRetainTheirDistinctState()
    {
        IsoTextValue text = new IsoTextValue { Value = "A B", LexicalValue = "  A B  " };
        IsoBooleanValue boolean = new IsoBooleanValue { Value = true, LexicalValue = "1" };
        IsoDurationValue duration = new IsoDurationValue
        {
            IsNegative = true,
            Years = 1,
            Months = 2,
            Days = 3,
            Hours = 4,
            Minutes = 5,
            Seconds = 6,
            FractionalSecondDigits = "700",
            LexicalValue = "-P1Y2M3DT4H5M6.700S"
        };
        IsoPartialDateValue partialDate = new IsoPartialDateValue
        {
            Kind = PartialDateKind.MonthDay,
            Month = 2,
            Day = 29,
            HasTimezone = true,
            IsUtc = true,
            LexicalValue = "--02-29Z"
        };
        IsoCodeValue code = new IsoCodeValue
        {
            Value = "ABCD",
            CodeSetName = "ExampleCodeSet",
            LexicalValue = "ABCD"
        };

        Assert.Multiple(() =>
        {
            Assert.That(text.Value, Is.EqualTo("A B"));
            Assert.That(text.LexicalValue, Is.EqualTo("  A B  "));
            Assert.That(boolean.Value, Is.True);
            Assert.That(boolean.LexicalValue, Is.EqualTo("1"));
            Assert.That(duration.IsNegative, Is.True);
            Assert.That(duration.FractionalSecondDigits, Is.EqualTo("700"));
            Assert.That(duration.LexicalValue, Is.EqualTo("-P1Y2M3DT4H5M6.700S"));
            Assert.That(partialDate.Kind, Is.EqualTo(PartialDateKind.MonthDay));
            Assert.That(partialDate.IsUtc, Is.True);
            Assert.That(code.CodeSetName, Is.EqualTo("ExampleCodeSet"));
        });
    }

    [Test]
    public void IdentityModelsPreserveLeadingZeroesAndQualifiedNames()
    {
        MessageIdentifier identifier = new MessageIdentifier
        {
            BusinessArea = "pacs",
            MessageFunctionality = "008",
            Variant = "001",
            Version = "10",
            Value = "pacs.008.001.10"
        };
        NamespaceDescriptor descriptor = new NamespaceDescriptor
        {
            Uri = Iso20022Namespaces.SchemaUrnPrefix + identifier.Value,
            Kind = NamespaceKind.Message,
            Mode = NamespaceMode.Strict,
            MessageIdentifier = identifier
        };
        QualifiedNameData documentName = new QualifiedNameData
        {
            NamespaceUri = descriptor.Uri,
            LocalName = "Document",
            Prefix = "iso"
        };
        XmlPathData path = new XmlPathData
        {
            Segments = new List<QualifiedNameData> { documentName },
            Value = "/iso:Document"
        };
        NamespaceDeclarationData declaration = new NamespaceDeclarationData
        {
            Prefix = "iso",
            NamespaceUri = descriptor.Uri
        };

        Assert.Multiple(() =>
        {
            Assert.That(identifier.MessageFunctionality, Is.EqualTo("008"));
            Assert.That(identifier.Variant, Is.EqualTo("001"));
            Assert.That(descriptor.MessageIdentifier, Is.SameAs(identifier));
            Assert.That(descriptor.Uri, Is.EqualTo("urn:iso:std:iso:20022:tech:xsd:pacs.008.001.10"));
            Assert.That(path.Segments, Has.Count.EqualTo(1));
            Assert.That(path.Segments[0], Is.SameAs(documentName));
            Assert.That(declaration.Prefix, Is.EqualTo("iso"));
        });
    }

    [Test]
    public void ArtifactSelectorAndSourceLocationRetainCallerData()
    {
        MessageIdentifier baseline = new MessageIdentifier { Value = "pacs.008.001.10" };
        VersionSelector selector = new VersionSelector
        {
            Kind = VersionSelectionKind.Range,
            BaselineIdentifier = baseline,
            MinimumVersion = "08",
            MaximumVersion = "10"
        };
        SourceLocation location = new SourceLocation
        {
            SourceName = "repository.iso20022",
            HasLineInformation = true,
            LineNumber = 17,
            ColumnNumber = 9,
            HasCharacterOffset = true,
            CharacterOffset = 2048
        };
        ArtifactManifest manifest = new ArtifactManifest
        {
            Coordinate = new ArtifactCoordinate
            {
                Kind = ArtifactKind.Repository,
                Edition = "2026-06-26",
                Identifier = "eRepository",
                Version = "2013-ecore"
            },
            OriginUri = "https://example.test/repository.zip",
            PublicationDate = new IsoPartialDateValue
            {
                Kind = PartialDateKind.YearMonth,
                Year = 2026,
                Month = 6,
                LexicalValue = "2026-06"
            },
            Hash = new IsoBinaryValue
            {
                Value = new byte[] { 1, 2, 3 },
                Encoding = BinaryEncodingKind.Hexadecimal,
                LexicalValue = "010203"
            },
            HashAlgorithm = "SHA-256",
            MediaType = Iso20022MediaTypes.Zip,
            LicenseNotice = "Example notice",
            IsApproved = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(selector.BaselineIdentifier, Is.SameAs(baseline));
            Assert.That(selector.MinimumVersion, Is.EqualTo("08"));
            Assert.That(selector.MaximumVersion, Is.EqualTo("10"));
            Assert.That(location.LineNumber, Is.EqualTo(17));
            Assert.That(location.CharacterOffset, Is.EqualTo(2048));
            Assert.That(manifest.Coordinate.Kind, Is.EqualTo(ArtifactKind.Repository));
            Assert.That(manifest.PublicationDate?.LexicalValue, Is.EqualTo("2026-06"));
            Assert.That(manifest.Hash.Value, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(manifest.IsApproved, Is.True);
        });
    }

    [Test]
    public void MutableReferenceDefaultsAreIndependentBetweenInstances()
    {
        IsoDateTimeValue firstDateTime = new IsoDateTimeValue();
        IsoDateTimeValue secondDateTime = new IsoDateTimeValue();
        XmlPathData firstPath = new XmlPathData();
        XmlPathData secondPath = new XmlPathData();
        ArtifactManifest firstManifest = new ArtifactManifest();
        ArtifactManifest secondManifest = new ArtifactManifest();

        firstDateTime.Date.Year = 2026;
        firstPath.Segments.Add(new QualifiedNameData { LocalName = "Document" });
        firstManifest.Hash.Value = new byte[] { 1 };

        Assert.Multiple(() =>
        {
            Assert.That(secondDateTime.Date.Year, Is.Zero);
            Assert.That(secondPath.Segments, Is.Empty);
            Assert.That(secondManifest.Hash.Value, Is.Empty);
            Assert.That(firstDateTime.Date, Is.Not.SameAs(secondDateTime.Date));
            Assert.That(firstPath.Segments, Is.Not.SameAs(secondPath.Segments));
            Assert.That(firstManifest.Hash, Is.Not.SameAs(secondManifest.Hash));
        });
    }
}
