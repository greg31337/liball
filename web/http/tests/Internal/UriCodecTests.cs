namespace http.Tests;

public class UriCodecTests
{
    [Test]
    public void PercentEncodingValidationAcceptsHexPairsAndRejectsIncompleteOrNonHexEscapes()
    {
        UriValidationResult valid = UriCodec.ValidatePercentEncoding("a%20b%2F%ff", 32);
        UriValidationResult incomplete = UriCodec.ValidatePercentEncoding("a%2", 32);
        UriValidationResult nonHex = UriCodec.ValidatePercentEncoding("a%XZ", 32);
        UriValidationResult nonAscii = UriCodec.ValidatePercentEncoding("caf\u00e9", 32);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(valid.Consumed, Is.EqualTo(11));
            Assert.That(incomplete.IsMalformed, Is.True);
            Assert.That(incomplete.ErrorPosition, Is.EqualTo(1));
            Assert.That(nonHex.IsMalformed, Is.True);
            Assert.That(nonHex.ErrorPosition, Is.EqualTo(1));
            Assert.That(nonAscii.IsMalformed, Is.True);
        });
    }

    [Test]
    public void SchemeValidationImplementsTheAsciiSchemeGrammar()
    {
        string[] valid = ["http", "HTTPS", "git+ssh", "web-cache.2"];
        string[] invalid = ["", "1http", "+http", "ht_tp", "caf\u00e9"];

        foreach (string value in valid)
        {
            Assert.That(UriCodec.ValidateScheme(value, 32).Status, Is.EqualTo(HttpOperationStatus.Done), value);
        }

        foreach (string value in invalid)
        {
            Assert.That(UriCodec.ValidateScheme(value, 32).IsMalformed, Is.True, value);
        }
    }

    [Test]
    public void PortValidationPreservesRfcSyntaxAndReportsNetworkRange()
    {
        UriValidationResult empty = UriCodec.ValidatePort("", 16);
        UriValidationResult normal = UriCodec.ValidatePort("00443", 16);
        UriValidationResult large = UriCodec.ValidatePort("999999999999999", 16);
        UriValidationResult invalid = UriCodec.ValidatePort("80x", 16);

        Assert.Multiple(() =>
        {
            Assert.That(empty.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(empty.HasPortValue, Is.False);
            Assert.That(normal.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(normal.HasPortValue, Is.True);
            Assert.That(normal.IsPortInRange, Is.True);
            Assert.That(normal.Port, Is.EqualTo(443));
            Assert.That(large.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(large.HasPortValue, Is.True);
            Assert.That(large.IsPortInRange, Is.False);
            Assert.That(invalid.IsMalformed, Is.True);
            Assert.That(invalid.ErrorPosition, Is.EqualTo(2));
        });
    }

    [Test]
    public void HostValidationAcceptsRegNamesIpv4AndPercentEncoding()
    {
        string[] valid =
        [
            "",
            "example.com",
            "EXAMPLE.com",
            "sub-domain.example",
            "name!$&'()*+,;=",
            "caf%C3%A9.example",
            "192.0.2.1",
            "0.0.0.0",
            "255.255.255.255",
            "256.0.0.1",
            "192.168.001.1",
            "1.2.3",
            "1.2.3.4.5"
        ];

        foreach (string value in valid)
        {
            Assert.That(UriCodec.ValidateHost(value, 64).Status, Is.EqualTo(HttpOperationStatus.Done), value);
        }
    }

    [Test]
    public void HostValidationRejectsMalformedRegNames()
    {
        string[] invalid =
        [
            "host name",
            "host/name",
            "host:80",
            "caf\u00e9.example",
            "bad%2G"
        ];

        foreach (string value in invalid)
        {
            Assert.That(UriCodec.ValidateHost(value, 64).IsMalformed, Is.True, value);
        }
    }

    [Test]
    public void IpLiteralValidationAcceptsIpv6AndIpvFutureForms()
    {
        string[] valid =
        [
            "[::]",
            "[::1]",
            "[2001:db8::1]",
            "[2001:db8:0:1:2:3:4:5]",
            "[::ffff:192.0.2.1]",
            "[v1.fe80]",
            "[VAF.a:b-._~!$&'()*+,;=:]"
        ];

        foreach (string value in valid)
        {
            UriValidationResult result = UriCodec.ValidateHost(value, 80);
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done), value);
            Assert.That(result.IsIpLiteral, Is.True, value);
        }
    }

    [Test]
    public void IpLiteralValidationRejectsMalformedForms()
    {
        string[] invalid =
        [
            "[]",
            "[:::1]",
            "[1:2:3:4:5:6:7]",
            "[1:2:3:4:5:6:7:8:9]",
            "[1::2::3]",
            "[::ffff:999.0.0.1]",
            "[fe80::1%25eth0]",
            "[v.abc]",
            "[v1.]",
            "[vG.abc]",
            "[v1.a%20b]"
        ];

        foreach (string value in invalid)
        {
            Assert.That(UriCodec.ValidateHost(value, 80).IsMalformed, Is.True, value);
        }
    }

    [Test]
    public void AuthorityValidationParsesUserInfoHostAndPortWithoutNormalization()
    {
        UriValidationResult result = UriCodec.ValidateAuthority("User:p%40ss@Example.COM:00443", 64);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.HasAuthority, Is.True);
            Assert.That(result.Authority, Is.EqualTo("User:p%40ss@Example.COM:00443"));
            Assert.That(result.HasUserInfo, Is.True);
            Assert.That(result.UserInfo, Is.EqualTo("User:p%40ss"));
            Assert.That(result.Host, Is.EqualTo("Example.COM"));
            Assert.That(result.HasPort, Is.True);
            Assert.That(result.PortText, Is.EqualTo("00443"));
            Assert.That(result.Port, Is.EqualTo(443));
        });
    }

    [Test]
    public void AuthorityValidationHandlesIpLiteralsAndEmptyComponents()
    {
        UriValidationResult literal = UriCodec.ValidateAuthority("[2001:db8::1]:8080", 64);
        UriValidationResult empty = UriCodec.ValidateAuthority("", 64);
        UriValidationResult emptyPort = UriCodec.ValidateAuthority("example.com:", 64);

        Assert.Multiple(() =>
        {
            Assert.That(literal.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(literal.Host, Is.EqualTo("[2001:db8::1]"));
            Assert.That(literal.IsIpLiteral, Is.True);
            Assert.That(literal.Port, Is.EqualTo(8080));
            Assert.That(empty.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(empty.Host, Is.Empty);
            Assert.That(emptyPort.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(emptyPort.HasPort, Is.True);
            Assert.That(emptyPort.HasPortValue, Is.False);
        });
    }

    [Test]
    public void AuthorityValidationRejectsBadUserInfoBracketsAndPorts()
    {
        string[] invalid =
        [
            "user@@example.com",
            "user name@example.com",
            "[::1",
            "[::1]extra",
            "[::1]:80:90",
            "example.com:abc",
            "example.com:80:90"
        ];

        foreach (string value in invalid)
        {
            Assert.That(UriCodec.ValidateAuthority(value, 80).IsMalformed, Is.True, value);
        }
    }

    [Test]
    public void PathAndQueryValidationApplyTheirDistinctCharacterSets()
    {
        UriValidationResult path = UriCodec.ValidatePath("/a:@!$&'()*+,;=/%2F", 64);
        UriValidationResult query = UriCodec.ValidateQuery("a/b?c=@&x=%ff", 64);
        UriValidationResult emptyPath = UriCodec.ValidatePath("", 64);

        Assert.Multiple(() =>
        {
            Assert.That(path.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(path.Path, Is.EqualTo("/a:@!$&'()*+,;=/%2F"));
            Assert.That(query.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(query.Query, Is.EqualTo("a/b?c=@&x=%ff"));
            Assert.That(emptyPath.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(UriCodec.ValidatePath("/bad path", 64).IsMalformed, Is.True);
            Assert.That(UriCodec.ValidatePath("/bad?path", 64).IsMalformed, Is.True);
            Assert.That(UriCodec.ValidateQuery("bad#query", 64).IsMalformed, Is.True);
            Assert.That(UriCodec.ValidateQuery("bad%2", 64).IsMalformed, Is.True);
        });
    }

    [Test]
    public void UriReferenceValidationParsesAbsoluteHierarchicalComponents()
    {
        UriValidationResult result = UriCodec.ValidateUriReference(
            "HtTp://User@example.com:8080/a%2Fb/c?x=1/2?3#Frag",
            128);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(result.IsAbsolute, Is.True);
            Assert.That(result.Scheme, Is.EqualTo("HtTp"));
            Assert.That(result.HasAuthority, Is.True);
            Assert.That(result.HasUserInfo, Is.True);
            Assert.That(result.Host, Is.EqualTo("example.com"));
            Assert.That(result.Port, Is.EqualTo(8080));
            Assert.That(result.Path, Is.EqualTo("/a%2Fb/c"));
            Assert.That(result.HasQuery, Is.True);
            Assert.That(result.Query, Is.EqualTo("x=1/2?3"));
            Assert.That(result.HasFragment, Is.True);
            Assert.That(result.Fragment, Is.EqualTo("Frag"));
        });
    }

    [Test]
    public void UriReferenceValidationAcceptsAbsoluteAndRelativeReferenceForms()
    {
        string[] valid =
        [
            "",
            "http://example.com",
            "file:///path",
            "mailto:User@example.com",
            "urn:isbn:0451450523",
            "//example.com/path",
            "/absolute/path",
            "relative/path",
            "a/b:c",
            "?query",
            "#fragment",
            "../up/one"
        ];

        foreach (string value in valid)
        {
            Assert.That(UriCodec.ValidateUriReference(value, 128).Status, Is.EqualTo(HttpOperationStatus.Done), value);
        }
    }

    [Test]
    public void UriReferenceValidationRejectsMalformedReferences()
    {
        string[] invalid =
        [
            "1http:value",
            ":value",
            "//bad host/path",
            "http://[:::1]/",
            "http://example.com/%2G",
            "http://example.com/path with space",
            "relative:still-scheme%",
            "path#one#two",
            "caf\u00e9/path"
        ];

        foreach (string value in invalid)
        {
            Assert.That(UriCodec.ValidateUriReference(value, 128).IsMalformed, Is.True, value);
        }
    }

    [Test]
    public void UriValidationReportsConfiguredSizeExhaustionSeparately()
    {
        UriValidationResult result = UriCodec.ValidateUriReference("/four" , 4);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(HttpOperationStatus.Rejected));
            Assert.That(result.IsLimitExceeded, Is.True);
            Assert.That(result.IsMalformed, Is.False);
        });
    }

    [Test]
    public void FragmentValidationAllowsQueryCharactersButNotRawFragmentDelimiters()
    {
        UriValidationResult valid = UriCodec.ValidateFragment("section/2?mode=full", 64);
        UriValidationResult invalid = UriCodec.ValidateFragment("one#two", 64);

        Assert.Multiple(() =>
        {
            Assert.That(valid.Status, Is.EqualTo(HttpOperationStatus.Done));
            Assert.That(valid.Fragment, Is.EqualTo("section/2?mode=full"));
            Assert.That(invalid.IsMalformed, Is.True);
        });
    }
}
