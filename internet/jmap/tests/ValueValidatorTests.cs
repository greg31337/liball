namespace jmap.Tests;

public class ValueValidatorTests
{
    [Test]
    public void IdsUseTheUrlSafeBase64AlphabetAndRfcLengthBounds()
    {
        ValueValidator validator = new ValueValidator();
        string maximum = new string('a', JmapProtocolLimits.MaximumIdLength);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.IsId("A-z_09"), Is.True);
            Assert.That(validator.IsId("0"), Is.True);
            Assert.That(validator.IsId(maximum), Is.True);
            Assert.That(validator.IsId(null), Is.False);
            Assert.That(validator.IsId(string.Empty), Is.False);
            Assert.That(validator.IsId(maximum + "a"), Is.False);
            Assert.That(validator.IsId("not/padded="), Is.False);
            Assert.That(validator.IsId("café"), Is.False);
        }
    }

    [Test]
    public void CreationIdsAndReferencesHaveDistinctHashPrefixRules()
    {
        ValueValidator validator = new ValueValidator();
        string maximum = new string('c', JmapProtocolLimits.MaximumIdLength);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.IsCreationId("temporary-1"), Is.True);
            Assert.That(validator.IsCreationId("#temporary-1"), Is.False);
            Assert.That(validator.IsCreationIdReference("#temporary-1"), Is.True);
            Assert.That(validator.IsCreationIdReference("#" + maximum), Is.True);
            Assert.That(validator.IsCreationIdReference("temporary-1"), Is.False);
            Assert.That(validator.IsCreationIdReference("#"), Is.False);
            Assert.That(validator.IsCreationIdReference("##temporary-1"), Is.False);
            Assert.That(validator.IsCreationIdReference("#" + maximum + "c"), Is.False);
        }
    }

    [Test]
    public void IntegerValidatorsEnforceTheInteroperableJsonRange()
    {
        ValueValidator validator = new ValueValidator();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.IsInt(JmapProtocolLimits.MinimumInt), Is.True);
            Assert.That(validator.IsInt(JmapProtocolLimits.MaximumInt), Is.True);
            Assert.That(validator.IsInt(JmapProtocolLimits.MinimumInt - 1L), Is.False);
            Assert.That(validator.IsInt(JmapProtocolLimits.MaximumInt + 1L), Is.False);
            Assert.That(validator.IsUnsignedInt(JmapProtocolLimits.MinimumUnsignedInt), Is.True);
            Assert.That(validator.IsUnsignedInt(JmapProtocolLimits.MaximumUnsignedInt), Is.True);
            Assert.That(validator.IsUnsignedInt(-1L), Is.False);
            Assert.That(validator.IsUnsignedInt(JmapProtocolLimits.MaximumUnsignedInt + 1L), Is.False);
        }
    }

    [TestCase("2014-10-30T14:12:00+08:00")]
    [TestCase("2014-10-30T06:12:00Z")]
    [TestCase("2000-02-29T23:59:60.01-00:00")]
    [TestCase("0000-01-01T00:00:00Z")]
    public void DateAcceptsNormalizedRfc3339Values(string value)
    {
        ValueValidator validator = new ValueValidator();

        Assert.That(validator.IsDate(value), Is.True);
    }

    [TestCase("2019-02-29T00:00:00Z")]
    [TestCase("2020-04-31T00:00:00Z")]
    [TestCase("2020-01-01t00:00:00Z")]
    [TestCase("2020-01-01T00:00:00z")]
    [TestCase("2020-01-01T24:00:00Z")]
    [TestCase("2020-01-01T00:60:00Z")]
    [TestCase("2020-01-01T00:00:61Z")]
    [TestCase("2020-01-01T00:00:00.0Z")]
    [TestCase("2020-01-01T00:00:00.Z")]
    [TestCase("2020-01-01T00:00:00+24:00")]
    [TestCase("2020-01-01T00:00:00+00:60")]
    [TestCase("2020-01-01T00:00:00")]
    public void DateRejectsInvalidOrNonNormalizedValues(string value)
    {
        ValueValidator validator = new ValueValidator();

        Assert.That(validator.IsDate(value), Is.False);
    }

    [Test]
    public void UtcDateRequiresTheUppercaseUtcDesignator()
    {
        ValueValidator validator = new ValueValidator();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.IsUtcDate("2024-01-02T03:04:05Z"), Is.True);
            Assert.That(validator.IsUtcDate("2024-01-02T03:04:05.25Z"), Is.True);
            Assert.That(validator.IsUtcDate("2024-01-02T03:04:05+00:00"), Is.False);
            Assert.That(validator.IsUtcDate(null), Is.False);
        }
    }

    [Test]
    public void CapabilitiesMustBeAbsoluteWellFormedUris()
    {
        ValueValidator validator = new ValueValidator();
        string unpairedSurrogate = new string(new[] { '\uD800' });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.IsCapability(JmapCapabilities.Core), Is.True);
            Assert.That(validator.IsCapability("https://api.example.test/jmap/capability"), Is.True);
            Assert.That(validator.IsCapability("relative/capability"), Is.False);
            Assert.That(validator.IsCapability("https://example.test/has space"), Is.False);
            Assert.That(validator.IsCapability(string.Empty), Is.False);
            Assert.That(validator.IsCapability(unpairedSurrogate), Is.False);
        }
    }

    [Test]
    public void MethodNamesContainOneVisibleAsciiSeparator()
    {
        ValueValidator validator = new ValueValidator();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.IsMethodName(JmapMethods.EmailQueryChanges), Is.True);
            Assert.That(validator.IsMethodName("com.example.Widget/get"), Is.True);
            Assert.That(validator.IsMethodName("Email"), Is.False);
            Assert.That(validator.IsMethodName("/get"), Is.False);
            Assert.That(validator.IsMethodName("Email/"), Is.False);
            Assert.That(validator.IsMethodName("Email/get/more"), Is.False);
            Assert.That(validator.IsMethodName("Email/query changes"), Is.False);
            Assert.That(validator.IsMethodName("Email/quéry"), Is.False);
        }
    }

    [Test]
    public void KeywordsUseLowercasePrintableImapAtomCharacters()
    {
        ValueValidator validator = new ValueValidator();
        string maximum = new string('k', JmapProtocolLimits.MaximumKeywordLength);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.IsKeyword(JmapMailKeywords.Seen), Is.True);
            Assert.That(validator.IsKeyword("project-label"), Is.True);
            Assert.That(validator.IsKeyword(maximum), Is.True);
            Assert.That(validator.IsKeyword("$Seen"), Is.False);
            Assert.That(validator.IsKeyword("Project-label"), Is.False);
            Assert.That(validator.IsKeyword("two words"), Is.False);
            Assert.That(validator.IsKeyword("invalid]keyword"), Is.False);
            Assert.That(validator.IsKeyword("invalid%keyword"), Is.False);
            Assert.That(validator.IsKeyword(maximum + "k"), Is.False);
            Assert.That(validator.IsKeyword(null), Is.False);
        }
    }

    [Test]
    public void MailboxRolesAcceptLowercaseRegistryStyleTokens()
    {
        ValueValidator validator = new ValueValidator();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.IsMailboxRole(JmapMailboxRoles.Inbox), Is.True);
            Assert.That(validator.IsMailboxRole("x-vendor-role"), Is.True);
            Assert.That(validator.IsMailboxRole("Inbox"), Is.False);
            Assert.That(validator.IsMailboxRole("vendor role"), Is.False);
            Assert.That(validator.IsMailboxRole("rôle"), Is.False);
            Assert.That(validator.IsMailboxRole(string.Empty), Is.False);
            Assert.That(validator.IsMailboxRole(null), Is.False);
        }
    }

    [Test]
    public void MediaTypesFollowTheRegisteredTypeAndSubtypeGrammar()
    {
        ValueValidator validator = new ValueValidator();
        string maximumPart = new string('a', 127);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.IsMediaType(JmapMediaTypes.Json), Is.True);
            Assert.That(validator.IsMediaType("application/vnd.example+json"), Is.True);
            Assert.That(validator.IsMediaType("TEXT/PLAIN"), Is.True);
            Assert.That(validator.IsMediaType(maximumPart + "/" + maximumPart), Is.True);
            Assert.That(validator.IsMediaType("application"), Is.False);
            Assert.That(validator.IsMediaType("/json"), Is.False);
            Assert.That(validator.IsMediaType("application/"), Is.False);
            Assert.That(validator.IsMediaType("application/problem/json"), Is.False);
            Assert.That(validator.IsMediaType("application/json; charset=utf-8"), Is.False);
            Assert.That(validator.IsMediaType("application/@json"), Is.False);
            Assert.That(validator.IsMediaType(new string('a', 128) + "/json"), Is.False);
        }
    }

    [Test]
    public void Utf8SizeCountsOctetsAndRejectsInvalidIJsonScalars()
    {
        ValueValidator validator = new ValueValidator();
        string unpairedHigh = new string(new[] { '\uD800' });
        string unpairedLow = new string(new[] { '\uDC00' });
        string noncharacter = new string(new[] { '\uFDD0' });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validator.GetUtf8Size("Aé水👋"), Is.EqualTo(10L));
            Assert.That(validator.IsUtf8WithinSize("é👋", 6L), Is.True);
            Assert.That(validator.IsUtf8WithinSize("é👋", 5L), Is.False);
            Assert.That(validator.IsUtf8WithinSize(string.Empty, 0L), Is.True);
            Assert.That(validator.GetUtf8Size(null), Is.EqualTo(-1L));
            Assert.That(validator.GetUtf8Size(unpairedHigh), Is.EqualTo(-1L));
            Assert.That(validator.GetUtf8Size(unpairedLow), Is.EqualTo(-1L));
            Assert.That(validator.GetUtf8Size(noncharacter), Is.EqualTo(-1L));
            Assert.That(validator.IsUtf8WithinSize("valid", -1L), Is.False);
        }
    }
}
