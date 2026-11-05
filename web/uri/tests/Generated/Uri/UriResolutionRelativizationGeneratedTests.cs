namespace uri.Tests;

public class UriResolutionRelativizationGeneratedTests
{
    [Test]
    public void HierarchicalComponentCombinationsRelativizeAndResolveExactly()
    {
        string[] schemes = new string[] { "http", "x" };
        string[] authorities = new string[]
        {
            "example.com",
            "user@example.com:81",
            "[2001:db8::1]"
        };
        string[] basePaths = new string[]
        {
            "",
            "/",
            "/a/b/",
            "/a/b/file"
        };
        string[] targetPaths = new string[]
        {
            "",
            "/",
            "/a/b/",
            "/a/b/file",
            "/a/b/sibling",
            "/a/b/name:part",
            "/a/b//leaf",
            "/other/deep/item"
        };
        string?[] baseQueries = new string?[] { null, "", "base=1" };
        string?[] targetQueries = new string?[]
        {
            null,
            "",
            "target=/./../"
        };
        string?[] targetFragments = new string?[]
        {
            null,
            "",
            "fragment=/./../"
        };
        int combinations = 0;

        foreach (string scheme in schemes)
        {
            foreach (string authority in authorities)
            {
                foreach (string basePath in basePaths)
                {
                    foreach (string? baseQuery in baseQueries)
                    {
                        string baseUri = BuildHierarchicalUri(
                            scheme,
                            authority,
                            basePath,
                            baseQuery,
                            null);
                        foreach (string targetPath in targetPaths)
                        {
                            foreach (string? targetQuery in targetQueries)
                            {
                                foreach (string? targetFragment in targetFragments)
                                {
                                    string targetUri = BuildHierarchicalUri(
                                        scheme,
                                        authority,
                                        targetPath,
                                        targetQuery,
                                        targetFragment);

                                    AssertExactRoundTrip(baseUri, targetUri);
                                    combinations += 1;
                                }
                            }
                        }
                    }
                }
            }
        }

        Assert.That(combinations, Is.EqualTo(5184));
    }

    [Test]
    public void AuthorityAndSchemeCombinationsUseVerifiedFallbacks()
    {
        string[] schemes = new string[] { "http", "https", "x" };
        string[] authorities = new string[]
        {
            "a.example",
            "user@b.example:81",
            "[2001:db8::2]"
        };
        string[] paths = new string[] { "", "/root", "/a//name:part" };
        string?[] queries = new string?[] { null, "", "q=1" };
        string?[] fragments = new string?[] { null, "", "f=1" };
        int combinations = 0;

        foreach (string baseScheme in schemes)
        {
            foreach (string targetScheme in schemes)
            {
                foreach (string baseAuthority in authorities)
                {
                    string baseUri = BuildHierarchicalUri(
                        baseScheme,
                        baseAuthority,
                        "/base/file",
                        "base=1",
                        null);
                    foreach (string targetAuthority in authorities)
                    {
                        foreach (string path in paths)
                        {
                            foreach (string? query in queries)
                            {
                                foreach (string? fragment in fragments)
                                {
                                    string targetUri = BuildHierarchicalUri(
                                        targetScheme,
                                        targetAuthority,
                                        path,
                                        query,
                                        fragment);

                                    AssertExactRoundTrip(baseUri, targetUri);
                                    combinations += 1;
                                }
                            }
                        }
                    }
                }
            }
        }

        Assert.That(combinations, Is.EqualTo(2187));
    }

    [Test]
    public void RootlessAbsoluteUriCombinationsRoundTripWithoutAnAuthority()
    {
        string[] basePaths = new string[] { "", "a/", "a/b/file" };
        string[] targetPaths = new string[]
        {
            "",
            "a/",
            "a/b/file",
            "a/b/sibling",
            "a/b/name:part",
            "other/item"
        };
        string?[] queries = new string?[] { null, "", "q=/./../" };
        string?[] fragments = new string?[] { null, "", "f=/./../" };
        int combinations = 0;

        foreach (string basePath in basePaths)
        {
            foreach (string? baseQuery in queries)
            {
                string baseUri = AppendOptionalComponent(
                    "x:" + basePath,
                    '?',
                    baseQuery);
                foreach (string targetPath in targetPaths)
                {
                    foreach (string? targetQuery in queries)
                    {
                        foreach (string? targetFragment in fragments)
                        {
                            string targetUri = AppendOptionalComponent(
                                "x:" + targetPath,
                                '?',
                                targetQuery);
                            targetUri = AppendOptionalComponent(
                                targetUri,
                                '#',
                                targetFragment);

                            AssertExactRoundTrip(baseUri, targetUri);
                            combinations += 1;
                        }
                    }
                }
            }
        }

        Assert.That(combinations, Is.EqualTo(486));
    }

    private static void AssertExactRoundTrip(
        string baseUri,
        string targetUri)
    {
        IUriRelativizer relativizer = new UriRelativizer();
        IUriResolver resolver = new UriResolver();
        IUriParser parser = new UriParser();
        UriTextResult relative = relativizer.TryRelativizeReference(
            baseUri,
            targetUri,
            new UriRelativizationOptions());

        Assert.That(relative.Success, Is.True, FailureMessage(baseUri, targetUri, relative));
        UriParseResult parsed = parser.TryParseReference(
            relative.Text,
            new UriParseOptions());
        Assert.That(parsed.Success, Is.True, relative.Text);

        UriTextResult resolved = resolver.TryResolveReference(
            baseUri,
            relative.Text,
            new UriResolutionOptions());
        Assert.Multiple(() =>
        {
            Assert.That(resolved.Success, Is.True, FailureMessage(baseUri, targetUri, resolved));
            Assert.That(resolved.Text, Is.EqualTo(targetUri), baseUri + " + " + relative.Text);
            Assert.That(resolved.Length, Is.EqualTo(targetUri.Length));
            Assert.That(resolved.Diagnostic, Is.Null);
        });
    }

    private static string BuildHierarchicalUri(
        string scheme,
        string authority,
        string path,
        string? query,
        string? fragment)
    {
        string text = scheme + "://" + authority + path;
        text = AppendOptionalComponent(text, '?', query);
        return AppendOptionalComponent(text, '#', fragment);
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

    private static string FailureMessage(
        string baseUri,
        string targetUri,
        UriTextResult result)
    {
        string diagnostic = "none";
        if (result.Diagnostic != null)
        {
            diagnostic = result.Diagnostic.Code + "/" + result.Diagnostic.Component;
        }

        return "base=" + baseUri + ", target=" + targetUri + ", diagnostic=" + diagnostic;
    }
}
