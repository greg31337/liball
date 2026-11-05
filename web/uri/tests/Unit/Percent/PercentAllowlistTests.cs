namespace uri.Tests;

public class PercentAllowlistTests
{
    private const string Unreserved = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~";
    private const string SubDelimiters = "!$&'()*+,;=";

    [TestCase(PercentEncodeSet.Unreserved, Unreserved)]
    [TestCase(PercentEncodeSet.UserInfo, Unreserved + SubDelimiters + ":")]
    [TestCase(PercentEncodeSet.RegisteredName, Unreserved + SubDelimiters)]
    [TestCase(PercentEncodeSet.PathSegment, Unreserved + SubDelimiters + ":@")]
    [TestCase(PercentEncodeSet.Path, Unreserved + SubDelimiters + ":@/")]
    [TestCase(PercentEncodeSet.Query, Unreserved + SubDelimiters + ":@/?")]
    [TestCase(PercentEncodeSet.Fragment, Unreserved + SubDelimiters + ":@/?")]
    [TestCase(PercentEncodeSet.UrnNamespaceSpecificString, Unreserved + SubDelimiters + ":@/")]
    [TestCase(PercentEncodeSet.UrnRComponent, Unreserved + SubDelimiters + ":@/?")]
    [TestCase(PercentEncodeSet.UrnQComponent, Unreserved + SubDelimiters + ":@/?")]
    [TestCase(PercentEncodeSet.UriTemplateSimpleExpansion, Unreserved)]
    [TestCase(
        PercentEncodeSet.UriTemplateReservedExpansion,
        Unreserved + ":/?#[]@!$&'()*+,;=")]
    [TestCase(
        PercentEncodeSet.WebLinkExtendedValue,
        "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!#$&+-.^_`|~")]
    public void EncodeSetMatchesExactAsciiProduction(PercentEncodeSet encodeSet, string expectedCharacters)
    {
        for (int code = 0; code <= 255; code++)
        {
            char value = (char)code;

            Assert.That(
                PercentAllowlist.IsAllowed(value, encodeSet),
                Is.EqualTo(expectedCharacters.Contains(value)),
                $"{encodeSet} U+{code:X4}");
        }
    }

    [Test]
    public void ComponentHelpersMatchSelectorDispatch()
    {
        for (int code = 0; code <= 127; code++)
        {
            char value = (char)code;

            Assert.Multiple(() =>
            {
                Assert.That(
                    PercentAllowlist.IsUserInfoCharacter(value),
                    Is.EqualTo(PercentAllowlist.IsAllowed(value, PercentEncodeSet.UserInfo)));
                Assert.That(
                    PercentAllowlist.IsRegisteredNameCharacter(value),
                    Is.EqualTo(PercentAllowlist.IsAllowed(value, PercentEncodeSet.RegisteredName)));
                Assert.That(
                    PercentAllowlist.IsPathSegmentCharacter(value),
                    Is.EqualTo(PercentAllowlist.IsAllowed(value, PercentEncodeSet.PathSegment)));
                Assert.That(
                    PercentAllowlist.IsPathCharacter(value),
                    Is.EqualTo(PercentAllowlist.IsAllowed(value, PercentEncodeSet.Path)));
                Assert.That(
                    PercentAllowlist.IsQueryCharacter(value),
                    Is.EqualTo(PercentAllowlist.IsAllowed(value, PercentEncodeSet.Query)));
                Assert.That(
                    PercentAllowlist.IsFragmentCharacter(value),
                    Is.EqualTo(PercentAllowlist.IsAllowed(value, PercentEncodeSet.Fragment)));
                Assert.That(
                    PercentAllowlist.IsUrnNamespaceSpecificStringCharacter(value),
                    Is.EqualTo(PercentAllowlist.IsAllowed(value, PercentEncodeSet.UrnNamespaceSpecificString)));
                Assert.That(
                    PercentAllowlist.IsUrnRComponentCharacter(value),
                    Is.EqualTo(PercentAllowlist.IsAllowed(value, PercentEncodeSet.UrnRComponent)));
                Assert.That(
                    PercentAllowlist.IsUrnQComponentCharacter(value),
                    Is.EqualTo(PercentAllowlist.IsAllowed(value, PercentEncodeSet.UrnQComponent)));
                Assert.That(
                    PercentAllowlist.IsUriTemplateSimpleExpansionCharacter(value),
                    Is.EqualTo(PercentAllowlist.IsAllowed(value, PercentEncodeSet.UriTemplateSimpleExpansion)));
                Assert.That(
                    PercentAllowlist.IsUriTemplateReservedExpansionCharacter(value),
                    Is.EqualTo(PercentAllowlist.IsAllowed(value, PercentEncodeSet.UriTemplateReservedExpansion)));
                Assert.That(
                    PercentAllowlist.IsWebLinkExtendedValueCharacter(value),
                    Is.EqualTo(PercentAllowlist.IsAllowed(value, PercentEncodeSet.WebLinkExtendedValue)));
            });
        }
    }

    [Test]
    public void RawPercentAndNonAsciiAreNeverPreservedByAnAllowlist()
    {
        foreach (PercentEncodeSet encodeSet in Enum.GetValues<PercentEncodeSet>())
        {
            Assert.That(PercentAllowlist.IsAllowed('%', encodeSet), Is.False, $"percent in {encodeSet}");
            Assert.That(PercentAllowlist.IsAllowed('\u0080', encodeSet), Is.False, $"non-ASCII in {encodeSet}");
            Assert.That(PercentAllowlist.IsAllowed('\ud83d', encodeSet), Is.False, $"surrogate in {encodeSet}");
        }
    }

    [Test]
    public void WebLinkExtendedValueExcludesRfc8187Exceptions()
    {
        Assert.Multiple(() =>
        {
            Assert.That(PercentAllowlist.IsWebLinkExtendedValueCharacter('*'), Is.False);
            Assert.That(PercentAllowlist.IsWebLinkExtendedValueCharacter('\''), Is.False);
            Assert.That(PercentAllowlist.IsWebLinkExtendedValueCharacter('%'), Is.False);
            Assert.That(PercentAllowlist.IsWebLinkExtendedValueCharacter('#'), Is.True);
            Assert.That(PercentAllowlist.IsWebLinkExtendedValueCharacter('`'), Is.True);
            Assert.That(PercentAllowlist.IsWebLinkExtendedValueCharacter('|'), Is.True);
        });
    }

    [Test]
    public void InvalidSelectorDoesNotPreserveInput()
    {
        Assert.That(PercentAllowlist.IsAllowed('A', (PercentEncodeSet)999), Is.False);
    }

    [Test]
    public void PercentCodecOptionsDefaultToUnreservedAndRemainConfigurable()
    {
        PercentCodecOptions options = new PercentCodecOptions();

        Assert.That(options.EncodeSet, Is.EqualTo(PercentEncodeSet.Unreserved));

        options.EncodeSet = PercentEncodeSet.Query;

        Assert.That(options.EncodeSet, Is.EqualTo(PercentEncodeSet.Query));
    }
}
