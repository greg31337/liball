using System.Text;

namespace http.Tests;

public class AuthenticationFieldCodecTests
{
    [Test]
    public void CredentialsParserAcceptsAndPreservesTheFullToken68Alphabet()
    {
        byte[] input = "bAsIc AZaz09-._~+/=="u8.ToArray();

        AuthenticationParseResult result = FieldValueParser.ParseAuthenticationCredentials(
            input,
            64,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Consumed, Is.EqualTo(input.Length));
            Assert.That(result.Value.Scheme, Is.EqualTo("bAsIc"));
            Assert.That(result.Value.Token68, Is.EqualTo("AZaz09-._~+/=="u8.ToArray()));
            Assert.That(result.Value.Parameters, Is.Empty);
        });
    }

    [Test]
    public void CredentialsParserHandlesBadWhitespaceTokensAndQuotedParameters()
    {
        byte[] prefix = "Digest realm \t=\t \"apps\", nonce=abc, title=\"Login to \\\"apps\\\"\", opaque=\""u8.ToArray();
        byte[] suffix = "\""u8.ToArray();
        byte[] input = new byte[prefix.Length + 1 + suffix.Length];
        Array.Copy(prefix, input, prefix.Length);
        input[prefix.Length] = 0x80;
        Array.Copy(suffix, 0, input, prefix.Length + 1, suffix.Length);

        AuthenticationParseResult result = FieldValueParser.ParseAuthenticationCredentials(
            input,
            160,
            8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Value.Token68, Is.Null);
            Assert.That(result.Value.Parameters, Has.Count.EqualTo(4));
            Assert.That(result.Value.Parameters[0].Name, Is.EqualTo("realm"));
            Assert.That(result.Value.Parameters[0].Value, Is.EqualTo("apps"u8.ToArray()));
            Assert.That(result.Value.Parameters[0].IsQuoted, Is.True);
            Assert.That(result.Value.Parameters[1].Value, Is.EqualTo("abc"u8.ToArray()));
            Assert.That(result.Value.Parameters[1].IsQuoted, Is.False);
            Assert.That(result.Value.Parameters[2].Value, Is.EqualTo("Login to \"apps\""u8.ToArray()));
            Assert.That(result.Value.Parameters[3].Value, Is.EqualTo(new byte[] { 0x80 }));
        });
    }

    [Test]
    public void ParserResolvesToken68AndParameterAmbiguityByCompleteSyntax()
    {
        AuthenticationParseResult padded = FieldValueParser.ParseAuthenticationCredentials(
            "Basic abc=="u8.ToArray(),
            32,
            4);
        AuthenticationParseResult singlePadding = FieldValueParser.ParseAuthenticationCredentials(
            "Basic abc="u8.ToArray(),
            32,
            4);
        AuthenticationParseResult parameter = FieldValueParser.ParseAuthenticationCredentials(
            "Custom abc=def"u8.ToArray(),
            32,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(padded.Value.Token68, Is.EqualTo("abc=="u8.ToArray()));
            Assert.That(singlePadding.Value.Token68, Is.EqualTo("abc="u8.ToArray()));
            Assert.That(parameter.Value.Token68, Is.Null);
            Assert.That(parameter.Value.Parameters, Has.Count.EqualTo(1));
            Assert.That(parameter.Value.Parameters[0].Name, Is.EqualTo("abc"));
            Assert.That(parameter.Value.Parameters[0].Value, Is.EqualTo("def"u8.ToArray()));
        });
    }

    [Test]
    public void ChallengeParserGroupsParametersAndSeparatesSchemesPerRfcExample()
    {
        byte[] input = "Basic realm=\"simple\", Newauth realm=\"apps\", type=1, title=\"Login to \\\"apps\\\"\""u8.ToArray();

        AuthenticationParseResult result = FieldValueParser.ParseAuthenticationValues(
            input,
            160,
            8,
            8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Values, Has.Count.EqualTo(2));
            Assert.That(result.Values[0].Scheme, Is.EqualTo("Basic"));
            Assert.That(result.Values[0].Parameters, Has.Count.EqualTo(1));
            Assert.That(result.Values[0].Parameters[0].Value, Is.EqualTo("simple"u8.ToArray()));
            Assert.That(result.Values[1].Scheme, Is.EqualTo("Newauth"));
            Assert.That(result.Values[1].Parameters, Has.Count.EqualTo(3));
            Assert.That(result.Values[1].Parameters[0].Name, Is.EqualTo("realm"));
            Assert.That(result.Values[1].Parameters[1].Name, Is.EqualTo("type"));
            Assert.That(result.Values[1].Parameters[2].Value,
                Is.EqualTo("Login to \"apps\""u8.ToArray()));
        });
    }

    [Test]
    public void ChallengeParserHandlesSchemeOnlyToken68AndEmptyListMembers()
    {
        AuthenticationParseResult result = FieldValueParser.ParseAuthenticationValues(
            ", Basic, , Bearer mF_9.B5f-4.1JqM==, Negotiate,"u8.ToArray(),
            128,
            8,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Values, Has.Count.EqualTo(3));
            Assert.That(result.EmptyElements, Is.EqualTo(3));
            Assert.That(result.Values[0].Scheme, Is.EqualTo("Basic"));
            Assert.That(result.Values[0].Token68, Is.Null);
            Assert.That(result.Values[1].Scheme, Is.EqualTo("Bearer"));
            Assert.That(result.Values[1].Token68, Is.EqualTo("mF_9.B5f-4.1JqM=="u8.ToArray()));
            Assert.That(result.Values[2].Scheme, Is.EqualTo("Negotiate"));
        });
    }

    [Test]
    public void EmptyAuthParameterMembersCanRemainWithinAChallenge()
    {
        AuthenticationParseResult result = FieldValueParser.ParseAuthenticationValues(
            "Digest realm=\"apps\", , nonce=abc, Basic realm=\"simple\""u8.ToArray(),
            128,
            8,
            8);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.Values, Has.Count.EqualTo(2));
            Assert.That(result.Values[0].Parameters, Has.Count.EqualTo(2));
            Assert.That(result.Values[0].Parameters[1].Name, Is.EqualTo("nonce"));
            Assert.That(result.Values[1].Scheme, Is.EqualTo("Basic"));
        });
    }

    [Test]
    public void DuplicateAuthenticationParameterNamesAreRejectedCaseInsensitively()
    {
        AuthenticationParseResult duplicate = FieldValueParser.ParseAuthenticationValues(
            "Digest realm=a, Realm=b"u8.ToArray(),
            64,
            4,
            4);
        AuthenticationParseResult separate = FieldValueParser.ParseAuthenticationValues(
            "First realm=a, Second REALM=b"u8.ToArray(),
            64,
            4,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(duplicate.IsMalformed, Is.True);
            Assert.That(duplicate.IsDuplicateParameter, Is.True);
            Assert.That(separate.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(separate.Values, Has.Count.EqualTo(2));
        });
    }

    [Test]
    public void AuthenticationParserRejectsMalformedCredentialsAndChallengeLists()
    {
        string[] credentials =
        [
            "",
            " Basic abc",
            "Basic\tabc",
            "Basic abc def",
            "Basic abc=def==",
            "Digest realm =",
            "Basic abc, Bearer def",
            "Bad/Scheme abc"
        ];

        foreach (string input in credentials)
        {
            AuthenticationParseResult result = FieldValueParser.ParseAuthenticationCredentials(
                Encoding.ASCII.GetBytes(input),
                96,
                8);
            Assert.That(result.IsMalformed, Is.True, input);
        }

        string[] challenges =
        [
            "Basic realm=a Digest realm=b",
            "Basic realm=\"unterminated",
            "Basic abc=def==",
            "Basic, Bad/Scheme abc"
        ];

        foreach (string input in challenges)
        {
            AuthenticationParseResult result = FieldValueParser.ParseAuthenticationValues(
                Encoding.ASCII.GetBytes(input),
                96,
                8,
                8);
            Assert.That(result.IsMalformed, Is.True, input);
        }
    }

    [Test]
    public void CredentialsSerializationCanonicalizesParameterWhitespaceAndQuoting()
    {
        HttpAuthenticationValue value = new HttpAuthenticationValue();
        value.Scheme = "Digest";
        value.Parameters.Add(new HttpParameter
        {
            Name = "realm",
            HasValue = true,
            Value = "apps"u8.ToArray(),
            IsQuoted = true
        });
        value.Parameters.Add(new HttpParameter
        {
            Name = "title",
            HasValue = true,
            Value = "Login to \"apps\""u8.ToArray(),
            IsQuoted = true
        });
        value.Parameters.Add(new HttpParameter
        {
            Name = "nonce",
            HasValue = true,
            Value = "abc"u8.ToArray()
        });

        FieldValueWriteResult written = FieldValueWriter.SerializeAuthenticationCredentials(
            value,
            128,
            8);
        AuthenticationParseResult parsed = FieldValueParser.ParseAuthenticationCredentials(
            written.Bytes,
            128,
            8);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(written.Bytes),
                Is.EqualTo("Digest realm=\"apps\", title=\"Login to \\\"apps\\\"\", nonce=abc"));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Value.Parameters[1].Value, Is.EqualTo("Login to \"apps\""u8.ToArray()));
        });
    }

    [Test]
    public void Token68AndMultipleChallengeSerializationRoundTrip()
    {
        HttpAuthenticationValue basic = new HttpAuthenticationValue();
        basic.Scheme = "Basic";
        basic.Token68 = "QWxhZGRpbjpvcGVuIHNlc2FtZQ=="u8.ToArray();
        HttpAuthenticationValue digest = new HttpAuthenticationValue();
        digest.Scheme = "Digest";
        digest.Parameters.Add(new HttpParameter
        {
            Name = "realm",
            HasValue = true,
            Value = "proxy"u8.ToArray(),
            IsQuoted = true
        });

        FieldValueWriteResult written = FieldValueWriter.SerializeAuthenticationValues(
            new List<HttpAuthenticationValue> { basic, digest },
            128,
            4,
            4);
        AuthenticationParseResult parsed = FieldValueParser.ParseAuthenticationValues(
            written.Bytes,
            128,
            4,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(Encoding.ASCII.GetString(written.Bytes),
                Is.EqualTo("Basic QWxhZGRpbjpvcGVuIHNlc2FtZQ==, Digest realm=\"proxy\""));
            Assert.That(parsed.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(parsed.Values, Has.Count.EqualTo(2));
            Assert.That(parsed.Values[0].Token68, Is.EqualTo(basic.Token68));
            Assert.That(parsed.Values[1].Parameters[0].Value, Is.EqualTo("proxy"u8.ToArray()));
        });
    }

    [Test]
    public void GenericCodecAppliesTheSameGrammarToProxyAuthenticationValues()
    {
        AuthenticationParseResult proxyChallenge = FieldValueParser.ParseAuthenticationValues(
            "Basic realm=\"proxy\""u8.ToArray(),
            64,
            4,
            4);
        AuthenticationParseResult proxyCredentials = FieldValueParser.ParseAuthenticationCredentials(
            "Basic cHJveHk6c2VjcmV0"u8.ToArray(),
            64,
            4);

        Assert.Multiple(() =>
        {
            Assert.That(proxyChallenge.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(proxyChallenge.Values[0].Parameters[0].Value, Is.EqualTo("proxy"u8.ToArray()));
            Assert.That(proxyCredentials.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(proxyCredentials.Value.Token68, Is.EqualTo("cHJveHk6c2VjcmV0"u8.ToArray()));
        });
    }

    [Test]
    public void AuthenticationSerializationRejectsAmbiguousOrMalformedModelState()
    {
        HttpAuthenticationValue mixed = new HttpAuthenticationValue();
        mixed.Scheme = "Basic";
        mixed.Token68 = "abc=="u8.ToArray();
        mixed.Parameters.Add(new HttpParameter
        {
            Name = "realm",
            HasValue = true,
            Value = "apps"u8.ToArray()
        });

        HttpAuthenticationValue badToken = new HttpAuthenticationValue();
        badToken.Scheme = "Basic";
        badToken.Token68 = "ab=c"u8.ToArray();

        HttpAuthenticationValue duplicate = new HttpAuthenticationValue();
        duplicate.Scheme = "Digest";
        duplicate.Parameters.Add(new HttpParameter
        {
            Name = "realm",
            HasValue = true,
            Value = "a"u8.ToArray()
        });
        duplicate.Parameters.Add(new HttpParameter
        {
            Name = "REALM",
            HasValue = true,
            Value = "b"u8.ToArray()
        });

        HttpAuthenticationValue unquotedSpace = new HttpAuthenticationValue();
        unquotedSpace.Scheme = "Digest";
        unquotedSpace.Parameters.Add(new HttpParameter
        {
            Name = "title",
            HasValue = true,
            Value = "not token"u8.ToArray()
        });

        Assert.Multiple(() =>
        {
            Assert.That(FieldValueWriter.SerializeAuthenticationCredentials(mixed, 64, 4).IsMalformed,
                Is.True);
            Assert.That(FieldValueWriter.SerializeAuthenticationCredentials(badToken, 64, 4).IsMalformed,
                Is.True);
            Assert.That(FieldValueWriter.SerializeAuthenticationCredentials(duplicate, 64, 4).IsMalformed,
                Is.True);
            Assert.That(FieldValueWriter.SerializeAuthenticationCredentials(unquotedSpace, 64, 4).IsMalformed,
                Is.True);
        });
    }

    [Test]
    public void AuthenticationOperationsDistinguishConfiguredLimits()
    {
        AuthenticationParseResult size = FieldValueParser.ParseAuthenticationCredentials(
            "Basic abc"u8.ToArray(),
            8,
            4);
        AuthenticationParseResult challenges = FieldValueParser.ParseAuthenticationValues(
            "Basic, Bearer"u8.ToArray(),
            64,
            1,
            4);
        AuthenticationParseResult parameters = FieldValueParser.ParseAuthenticationCredentials(
            "Digest a=1, b=2"u8.ToArray(),
            64,
            1);
        AuthenticationParseResult emptyParameters = FieldValueParser.ParseAuthenticationCredentials(
            "Digest a=1,,"u8.ToArray(),
            64,
            1);

        HttpAuthenticationValue token = new HttpAuthenticationValue();
        token.Scheme = "Basic";
        token.Token68 = "abc"u8.ToArray();
        FieldValueWriteResult output = FieldValueWriter.SerializeAuthenticationCredentials(token, 8, 4);

        Assert.Multiple(() =>
        {
            Assert.That(size.IsLimitExceeded, Is.True);
            Assert.That(size.IsMalformed, Is.False);
            Assert.That(challenges.IsLimitExceeded, Is.True);
            Assert.That(parameters.IsLimitExceeded, Is.True);
            Assert.That(emptyParameters.IsLimitExceeded, Is.True);
            Assert.That(output.IsLimitExceeded, Is.True);
        });
    }
}
