using token;

namespace token.Tests;

public class JoseCriticalHeaderValidatorTests
{
    [Test]
    public void HeaderWithoutCritIgnoresUnrecognizedNonCriticalExtensions()
    {
        JoseHeader protectedHeader = Header(
            "{\"alg\":\"HS256\",\"urn:example:ignored\":true}");

        TokenResult result = JoseCriticalHeaderValidator.Validate(
            protectedHeader,
            Array.Empty<string>());

        AssertSuccess(result);
    }

    [Test]
    public void UnderstoodProtectedExtensionsAreAccepted()
    {
        JoseHeader protectedHeader = Header(
            "{\"alg\":\"HS256\",\"crit\":[\"urn:example:flag\",\"b64\"],"
            + "\"urn:example:flag\":{\"enabled\":true},\"b64\":false}");

        TokenResult result = JoseCriticalHeaderValidator.Validate(
            protectedHeader,
            new string[] { "b64", "urn:example:flag" });

        AssertSuccess(result);
    }

    [Test]
    public void UnknownCriticalExtensionIsRejectedCaseSensitively()
    {
        JoseHeader protectedHeader = Header(
            "{\"crit\":[\"Custom\"],\"Custom\":true}");

        TokenResult result = JoseCriticalHeaderValidator.Validate(
            protectedHeader,
            new string[] { "custom" });

        AssertFailure(result, "unknown_critical_header", 0);
    }

    [Test]
    public void EmptyDuplicateAndMissingCriticalNamesAreRejected()
    {
        JoseHeader empty = Header("{\"crit\":[]}");
        JoseHeader duplicate = Header(
            "{\"crit\":[\"custom\",\"custom\"],\"custom\":true}");
        JoseHeader missing = Header("{\"crit\":[\"custom\"]}");

        TokenResult emptyResult = JoseCriticalHeaderValidator.Validate(
            empty,
            new string[] { "custom" });
        TokenResult duplicateResult = JoseCriticalHeaderValidator.Validate(
            duplicate,
            new string[] { "custom" });
        TokenResult missingResult = JoseCriticalHeaderValidator.Validate(
            missing,
            new string[] { "custom" });

        Assert.Multiple(() =>
        {
            AssertFailure(emptyResult, "invalid_critical_header", -1);
            AssertFailure(duplicateResult, "duplicate_critical_header", 1);
            AssertFailure(missingResult, "missing_critical_header", 0);
        });
    }

    [TestCase("alg", "\"HS256\"")]
    [TestCase("enc", "\"A128GCM\"")]
    [TestCase("epk", "{}")]
    [TestCase("p2c", "4096")]
    public void ParametersDefinedByJoseOrJwaCannotBeCritical(
        string name,
        string value)
    {
        JoseHeader protectedHeader = Header(
            "{\"crit\":[\"" + name + "\"],\"" + name + "\":" + value + "}");

        TokenResult result = JoseCriticalHeaderValidator.Validate(
            protectedHeader,
            new string[] { name });

        AssertFailure(result, "invalid_critical_header", 0);
    }

    [Test]
    public void CritCannotListItself()
    {
        JoseHeader protectedHeader = Header("{\"crit\":[\"crit\"]}");

        TokenResult result = JoseCriticalHeaderValidator.Validate(
            protectedHeader,
            new string[] { "crit" });

        AssertFailure(result, "invalid_critical_header", 0);
    }

    [Test]
    public void CritInAnUnprotectedHeaderIsRejected()
    {
        JoseHeader protectedHeader = Header("{\"alg\":\"HS256\"}");
        JoseHeader unprotectedHeader = Header(
            "{\"crit\":[\"custom\"],\"custom\":true}");

        TokenResult result = JoseCriticalHeaderValidator.Validate(
            protectedHeader,
            new JoseHeader[] { unprotectedHeader },
            new string[] { "custom" });

        AssertFailure(result, "critical_header_not_protected", 0);
    }

    [Test]
    public void CriticalParameterMustItselfBeProtected()
    {
        JoseHeader protectedHeader = Header("{\"crit\":[\"custom\"]}");
        JoseHeader unprotectedHeader = Header("{\"custom\":true}");

        TokenResult result = JoseCriticalHeaderValidator.Validate(
            protectedHeader,
            new JoseHeader[] { unprotectedHeader },
            new string[] { "custom" });

        AssertFailure(result, "missing_critical_header", 0);
    }

    [Test]
    public void HeaderParametersMustBeUniqueAcrossAllLocations()
    {
        JoseHeader protectedHeader = Header(
            "{\"alg\":\"HS256\",\"custom\":true}");
        JoseHeader sharedUnprotected = Header("{\"kid\":\"one\"}");
        JoseHeader recipientUnprotected = Header(
            "{\"kid\":\"two\",\"typ\":\"JWT\"}");

        TokenResult result = JoseCriticalHeaderValidator.Validate(
            protectedHeader,
            new JoseHeader[] { sharedUnprotected, recipientUnprotected },
            Array.Empty<string>());

        AssertFailure(result, "duplicate_header_parameter", 1);
    }

    [Test]
    public void DistinctProtectedAndUnprotectedParametersAreAccepted()
    {
        JoseHeader protectedHeader = Header(
            "{\"alg\":\"HS256\",\"crit\":[\"custom\"],\"custom\":true}");
        JoseHeader sharedUnprotected = Header("{\"kid\":\"one\"}");
        JoseHeader recipientUnprotected = Header("{\"typ\":\"JWT\"}");

        TokenResult result = JoseCriticalHeaderValidator.Validate(
            protectedHeader,
            new JoseHeader[] { sharedUnprotected, recipientUnprotected },
            new string[] { "custom" });

        AssertSuccess(result);
    }

    [Test]
    public void MutatedInvalidCriticalValueReturnsAStableFailure()
    {
        JoseHeader protectedHeader = Header("{\"crit\":[\"custom\"],\"custom\":true}");
        protectedHeader.Get(JoseHeaderNames.Critical).Kind = TokenJsonValueKind.String;
        protectedHeader.Get(JoseHeaderNames.Critical).StringValue = "custom";

        TokenResult result = JoseCriticalHeaderValidator.Validate(
            protectedHeader,
            new string[] { "custom" });

        AssertFailure(result, "invalid_critical_header", -1);
    }

    [Test]
    public void InvalidMutableHeaderStructuresReturnStableFailures()
    {
        JoseHeader missingParameters = new JoseHeader();
        missingParameters.Parameters = null;
        JoseHeader missingMember = new JoseHeader();
        missingMember.Parameters.Members = new JsonMember[] { null };

        TokenResult missingParametersResult = JoseCriticalHeaderValidator.Validate(
            missingParameters,
            Array.Empty<string>());
        TokenResult missingMemberResult = JoseCriticalHeaderValidator.Validate(
            new JoseHeader(),
            new JoseHeader[] { missingMember },
            Array.Empty<string>());

        Assert.Multiple(() =>
        {
            AssertFailure(missingParametersResult, "invalid_header", -1);
            AssertFailure(missingMemberResult, "invalid_header", 0);
        });
    }

    [Test]
    public void PublicOperationsRejectNullArguments()
    {
        JoseHeader header = new JoseHeader();

        Assert.Multiple(() =>
        {
            Assert.That(
                () => JoseCriticalHeaderValidator.Validate(
                    null,
                    Array.Empty<string>()),
                Throws.ArgumentNullException);
            Assert.That(
                () => JoseCriticalHeaderValidator.Validate(header, null),
                Throws.ArgumentNullException);
            Assert.That(
                () => JoseCriticalHeaderValidator.Validate(
                    header,
                    null,
                    Array.Empty<string>()),
                Throws.ArgumentNullException);
            Assert.That(
                () => JoseCriticalHeaderValidator.Validate(
                    header,
                    Array.Empty<JoseHeader>(),
                    null),
                Throws.ArgumentNullException);
        });
    }

    static JoseHeader Header(string json)
    {
        JoseHeaderResult result = JoseHeader.Read(json);
        Assert.That(result.Success, Is.True);
        return result.Header;
    }

    static void AssertSuccess(TokenResult result)
    {
        Assert.That(result.Success, Is.True);
        Assert.That(result.Error, Is.Null);
    }

    static void AssertFailure(TokenResult result, string code, int position)
    {
        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.Not.Null);
        Assert.That(result.Error.Code, Is.EqualTo(code));
        Assert.That(result.Error.Position, Is.EqualTo(position));
    }
}
