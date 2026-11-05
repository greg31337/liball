namespace sdp.Tests.Unit.Validation;

public class SdpValidationIssueMetadataTests
{
    [Test]
    public void ParsedDescriptionIssues_IncludeSourceLocationsAndMediaIdentity()
    {
        string input =
            "v=0\r\n" +
            "o=- 1 1 IN IP4 192.0.2.1\r\n" +
            "s=Metadata\r\n" +
            "c=IN IP4 192.0.2.1\r\n" +
            "t=0 0\r\n" +
            "m=audio 9 RTP/AVP 0\r\n" +
            "a=mid:audio\r\n";
        SdpParseResult parsed = new SdpParser().ParseText(input, new SdpParseOptions { PreserveRawLines = true });
        Assert.That(parsed.Success, Is.True);
        Assert.That(parsed.Description, Is.Not.Null);
        parsed.Description!.ProtocolVersion = 1;
        parsed.Description.MediaDescriptions[0].Port = -1;

        SdpValidationResult result = new SdpValidator().ValidateDescription(parsed.Description, new SdpValidationOptions { Profile = SdpValidationProfile.Rfc8866 });
        SdpValidationIssue protocol = result.Issues.Single(issue => issue.Code == SdpDiagnosticCodes.ValidationProtocolVersion);
        SdpValidationIssue port = result.Issues.Single(issue => issue.Code == SdpDiagnosticCodes.ValidationPort);

        Assert.Multiple(() =>
        {
            Assert.That(protocol.Severity, Is.EqualTo(SdpValidationSeverity.Error));
            Assert.That(protocol.Scope, Is.EqualTo(SdpScope.Session));
            Assert.That(protocol.MediaIndex, Is.EqualTo(-1));
            Assert.That(protocol.FieldName, Is.EqualTo('v'));
            Assert.That(protocol.Path.Scope, Is.EqualTo(protocol.Scope));
            Assert.That(protocol.Path.MediaIndex, Is.EqualTo(protocol.MediaIndex));
            Assert.That(protocol.Path.FieldName, Is.EqualTo(protocol.FieldName));
            Assert.That(protocol.Location.LineNumber, Is.EqualTo(1));
            Assert.That(protocol.Location.ColumnNumber, Is.EqualTo(1));
            Assert.That(protocol.Detail, Is.Not.Empty);
            Assert.That(port.Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(port.MediaIndex, Is.Zero);
            Assert.That(port.FieldName, Is.EqualTo('m'));
            Assert.That(port.AttributeName, Is.Empty);
            Assert.That(port.Path.MediaId, Is.EqualTo("audio"));
            Assert.That(port.Path.Scope, Is.EqualTo(port.Scope));
            Assert.That(port.Path.MediaIndex, Is.EqualTo(port.MediaIndex));
            Assert.That(port.Location.LineNumber, Is.EqualTo(6));
            Assert.That(port.Location.ColumnNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void CompletionBoundary_NormalizesMetadataAndRedactsSensitiveValues()
    {
        string password = "password-value-that-must-not-leak";
        string fingerprint = "DE:AD:BE:EF:PRIVATE-FINGERPRINT";
        string keyMaterial = "private-inline-key-material";
        string legacyKey = "legacy-encryption-secret";
        SdpSessionDescription description = new SdpSessionDescription
        {
            EncryptionKey = new SdpEncryptionKey
            {
                Method = "clear",
                HasValue = true,
                Value = legacyKey,
                RawText = "clear:" + legacyKey
            }
        };
        description.Attributes.Add(ValueAttribute(SdpAttributeNames.IcePassword, password));
        SdpMediaDescription media = new SdpMediaDescription();
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.MediaId, "audio"));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.Fingerprint, "sha-256 " + fingerprint));
        media.Attributes.Add(ValueAttribute(SdpAttributeNames.Crypto, "1 AES_CM_128_HMAC_SHA1_80 inline:" + keyMaterial));
        media.RawLines.Add(new SdpRawLine
        {
            FieldName = 'a',
            Value = SdpAttributeNames.IcePassword + ":" + password,
            Location = new SdpSourceLocation { LineNumber = 10, ColumnNumber = 1 }
        });
        description.MediaDescriptions.Add(media);
        SdpValidationIssue issue = new SdpValidationIssue
        {
            Code = string.Empty,
            Severity = SdpValidationSeverity.Unknown,
            Scope = SdpScope.Unknown,
            AttributeName = null!,
            MediaIndex = 0,
            Detail = "Replace " + password + ", " + fingerprint + ", " + keyMaterial + ", and " + legacyKey + ".",
            Location = null!
        };
        issue.Path.Scope = SdpScope.Media;
        issue.Path.MediaIndex = 0;
        issue.Path.FieldName = 'a';
        issue.Path.AttributeName = SdpAttributeNames.IcePassword;
        issue.Path.OccurrenceIndex = 0;
        List<SdpValidationIssue> issues = new List<SdpValidationIssue> { issue };

        SdpValidationIssueMetadataCore.CompleteIssues(description, issues);

        Assert.Multiple(() =>
        {
            Assert.That(issue.Code, Is.EqualTo(SdpDiagnosticCodes.ValidationAttribute));
            Assert.That(issue.Severity, Is.EqualTo(SdpValidationSeverity.Error));
            Assert.That(issue.Scope, Is.EqualTo(SdpScope.Media));
            Assert.That(issue.MediaIndex, Is.Zero);
            Assert.That(issue.FieldName, Is.EqualTo('a'));
            Assert.That(issue.AttributeName, Is.EqualTo(SdpAttributeNames.IcePassword));
            Assert.That(issue.Path.Scope, Is.EqualTo(issue.Scope));
            Assert.That(issue.Path.MediaIndex, Is.EqualTo(issue.MediaIndex));
            Assert.That(issue.Path.FieldName, Is.EqualTo(issue.FieldName));
            Assert.That(issue.Path.AttributeName, Is.EqualTo(issue.AttributeName));
            Assert.That(issue.Path.MediaId, Is.EqualTo("audio"));
            Assert.That(issue.Location.LineNumber, Is.EqualTo(10));
            Assert.That(issue.Location.ColumnNumber, Is.EqualTo(1));
            Assert.That(issue.Detail, Does.Contain("Replace"));
            Assert.That(issue.Detail, Does.Contain("[redacted]"));
            Assert.That(issue.Detail, Does.Not.Contain(password));
            Assert.That(issue.Detail, Does.Not.Contain(fingerprint));
            Assert.That(issue.Detail, Does.Not.Contain(keyMaterial));
            Assert.That(issue.Detail, Does.Not.Contain(legacyKey));
        });
    }

    static SdpAttribute ValueAttribute(string name, string value)
    {
        return new SdpAttribute
        {
            Kind = SdpAttributeKind.Value,
            Name = name,
            Value = value,
            RawText = name + ":" + value
        };
    }
}
