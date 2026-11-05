namespace uri.Tests;

public class UriNormalizationGeneratedTests
{
    [Test]
    public void AuthorityComponentCombinationsComposeParseAndNormalizeStably()
    {
        string[] schemePrefixes = new string[] { "", "X:" };
        string?[] userInfos = new string?[] { null, "", "User%7e" };
        string[] hosts = new string[]
        {
            "",
            "EXAMPLE%2eCOM",
            "192.0.2.128",
            "[2001:0DB8:0:0:0:0:0:1]",
            "[Vf.A:B]"
        };
        string?[] ports = new string?[] { null, "", "080" };
        string[] paths = new string[]
        {
            "",
            "/",
            "/a//b/",
            "/a/./%7e/../b"
        };
        string?[] queries = new string?[] { null, "", "Q=%7e%2f/../" };
        string?[] fragments = new string?[] { null, "", "F=%7e%2f/./" };
        int combinations = 0;

        foreach (string schemePrefix in schemePrefixes)
        {
            foreach (string? userInfo in userInfos)
            {
                foreach (string host in hosts)
                {
                    foreach (string? port in ports)
                    {
                        foreach (string path in paths)
                        {
                            foreach (string? query in queries)
                            {
                                foreach (string? fragment in fragments)
                                {
                                    string input = schemePrefix + "//";
                                    if (userInfo != null)
                                    {
                                        input = input + userInfo + "@";
                                    }

                                    input = input + host;
                                    if (port != null)
                                    {
                                        input = input + ":" + port;
                                    }

                                    input = input + path;
                                    input = AppendOptionalComponent(input, '?', query);
                                    input = AppendOptionalComponent(input, '#', fragment);

                                    AssertSyntaxPipeline(input);
                                    combinations += 1;
                                }
                            }
                        }
                    }
                }
            }
        }

        Assert.That(combinations, Is.EqualTo(3240));
    }

    [Test]
    public void EveryNonAuthorityPathFormComposesParsesAndNormalizesStably()
    {
        string[] references = new string[]
        {
            "",
            ".",
            "..",
            "relative/./%7e/../leaf",
            "./this:that/%7e",
            "/absolute/./%7e/../leaf",
            "X:",
            "X:rootless/./%7e/../leaf",
            "X:/absolute/./%7e/../leaf"
        };
        string?[] queries = new string?[] { null, "", "Q=%7e/../" };
        string?[] fragments = new string?[] { null, "", "F=%7e/./" };
        int combinations = 0;

        foreach (string reference in references)
        {
            foreach (string? query in queries)
            {
                foreach (string? fragment in fragments)
                {
                    string input = AppendOptionalComponent(reference, '?', query);
                    input = AppendOptionalComponent(input, '#', fragment);

                    AssertSyntaxPipeline(input);
                    combinations += 1;
                }
            }
        }

        Assert.That(combinations, Is.EqualTo(81));
    }

    [Test]
    public void BuiltInProfileCombinationsNormalizeIdempotently()
    {
        string[] schemes = new string[] { "http", "https", "ws", "wss", "ftp" };
        string[] paths = new string[] { "", "/", "/a/./b" };
        string?[] queries = new string?[] { null, "", "Q=%7e" };
        string?[] fragments = new string?[] { null, "", "F=%7e" };
        int combinations = 0;

        foreach (string scheme in schemes)
        {
            string defaultPort = GetDefaultPort(scheme);
            string nondefaultPort = GetNondefaultPort(scheme);
            string?[] ports = new string?[]
            {
                null,
                "",
                defaultPort,
                "0" + defaultPort,
                nondefaultPort
            };

            foreach (string? port in ports)
            {
                foreach (string path in paths)
                {
                    foreach (string? query in queries)
                    {
                        foreach (string? fragment in fragments)
                        {
                            string input = scheme + "://EXAMPLE.COM";
                            if (port != null)
                            {
                                input = input + ":" + port;
                            }

                            input = input + path;
                            input = AppendOptionalComponent(input, '?', query);
                            input = AppendOptionalComponent(input, '#', fragment);

                            AssertProfilePipeline(input);
                            combinations += 1;
                        }
                    }
                }
            }
        }

        Assert.That(combinations, Is.EqualTo(675));
    }

    private static void AssertSyntaxPipeline(string input)
    {
        IUriParser parser = new UriParser();
        IUriComposer composer = new UriComposer();
        IUriNormalizer normalizer = new UriNormalizer();
        IUriComparer comparer = new UriComparer();
        UriReference parsed = parser.ParseReference(input, new UriParseOptions());

        string composed = composer.ComposeReference(
            parsed,
            new UriValidationOptions());
        UriReference reparsed = parser.ParseReference(
            composed,
            new UriParseOptions());
        string normalizedOnce = normalizer.NormalizeSyntax(
            composed,
            new UriNormalizationOptions());
        string normalizedTwice = normalizer.NormalizeSyntax(
            normalizedOnce,
            new UriNormalizationOptions());
        UriReference normalizedReference = parser.ParseReference(
            normalizedOnce,
            new UriParseOptions());
        string normalizedComposed = composer.ComposeReference(
            normalizedReference,
            new UriValidationOptions());
        UriComparisonResult comparison = comparer.CompareSyntax(
            input,
            normalizedOnce,
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            Assert.That(composed, Is.EqualTo(input), input);
            AssertEquivalentParsedState(reparsed, parsed, input);
            Assert.That(normalizedTwice, Is.EqualTo(normalizedOnce), input);
            Assert.That(normalizedComposed, Is.EqualTo(normalizedOnce), input);
            Assert.That(comparison.Success, Is.True, input);
            Assert.That(comparison.Equal, Is.True, input);
            Assert.That(comparison.DifferentComponent, Is.EqualTo(UriComponent.None), input);
        });
    }

    private static void AssertProfilePipeline(string input)
    {
        IUriParser parser = new UriParser();
        IUriComposer composer = new UriComposer();
        IUriNormalizer normalizer = new UriNormalizer();
        IUriComparer comparer = new UriComparer();

        string normalizedOnce = normalizer.NormalizeWithProfile(
            input,
            new UriNormalizationOptions());
        string normalizedTwice = normalizer.NormalizeWithProfile(
            normalizedOnce,
            new UriNormalizationOptions());
        UriReference parsed = parser.ParseReference(
            normalizedOnce,
            new UriParseOptions());
        string composed = composer.ComposeReference(
            parsed,
            new UriValidationOptions());
        UriComparisonResult comparison = comparer.CompareWithProfile(
            input,
            normalizedOnce,
            new UriComparisonOptions());

        Assert.Multiple(() =>
        {
            Assert.That(normalizedTwice, Is.EqualTo(normalizedOnce), input);
            Assert.That(composed, Is.EqualTo(normalizedOnce), input);
            Assert.That(comparison.Success, Is.True, input);
            Assert.That(comparison.Equal, Is.True, input);
            Assert.That(comparison.DifferentComponent, Is.EqualTo(UriComponent.None), input);
        });
    }

    private static void AssertEquivalentParsedState(
        UriReference actual,
        UriReference expected,
        string input)
    {
        Assert.That(actual.Kind, Is.EqualTo(expected.Kind), input);
        Assert.That(actual.HasScheme, Is.EqualTo(expected.HasScheme), input);
        Assert.That(actual.Scheme, Is.EqualTo(expected.Scheme), input);
        Assert.That(actual.HasAuthority, Is.EqualTo(expected.HasAuthority), input);
        Assert.That(actual.Authority.HasUserInfo, Is.EqualTo(expected.Authority.HasUserInfo), input);
        Assert.That(actual.Authority.UserInfo, Is.EqualTo(expected.Authority.UserInfo), input);
        Assert.That(actual.Authority.HostKind, Is.EqualTo(expected.Authority.HostKind), input);
        Assert.That(actual.Authority.Host, Is.EqualTo(expected.Authority.Host), input);
        Assert.That(actual.Authority.AddressBytes, Is.EqualTo(expected.Authority.AddressBytes), input);
        Assert.That(actual.Authority.HasPort, Is.EqualTo(expected.Authority.HasPort), input);
        Assert.That(actual.Authority.Port, Is.EqualTo(expected.Authority.Port), input);
        Assert.That(actual.Path, Is.EqualTo(expected.Path), input);
        Assert.That(actual.HasQuery, Is.EqualTo(expected.HasQuery), input);
        Assert.That(actual.Query, Is.EqualTo(expected.Query), input);
        Assert.That(actual.HasFragment, Is.EqualTo(expected.HasFragment), input);
        Assert.That(actual.Fragment, Is.EqualTo(expected.Fragment), input);
    }

    private static string AppendOptionalComponent(
        string text,
        char delimiter,
        string? value)
    {
        if (value == null)
        {
            return text;
        }

        return text + delimiter + value;
    }

    private static string GetDefaultPort(string scheme)
    {
        if (scheme == "http" || scheme == "ws")
        {
            return "80";
        }

        if (scheme == "https" || scheme == "wss")
        {
            return "443";
        }

        return "21";
    }

    private static string GetNondefaultPort(string scheme)
    {
        if (scheme == "http" || scheme == "ws")
        {
            return "81";
        }

        if (scheme == "https" || scheme == "wss")
        {
            return "444";
        }

        return "20";
    }
}
