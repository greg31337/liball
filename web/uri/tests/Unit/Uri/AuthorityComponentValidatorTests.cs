namespace uri.Tests;

public class AuthorityComponentValidatorTests
{
    [Test]
    public void EmptyAndEveryAllowedLiteralCharacterAreAccepted()
    {
        string unreservedAndSubDelimiters =
            "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-._~!$&'()*+,;=";

        Assert.Multiple(() =>
        {
            Assert.That(AuthorityParser.ValidateUserInfo("".AsSpan(), 0), Is.Null);
            Assert.That(AuthorityParser.ValidateRegisteredName("".AsSpan(), 0), Is.Null);
            Assert.That(
                AuthorityParser.ValidateUserInfo(
                    (unreservedAndSubDelimiters + ":").AsSpan(),
                    0),
                Is.Null);
            Assert.That(
                AuthorityParser.ValidateRegisteredName(
                    unreservedAndSubDelimiters.AsSpan(),
                    0),
                Is.Null);
        });
    }

    [Test]
    public void EveryPercentEncodedByteIsAcceptedWithoutDecoding()
    {
        int value = 0;
        while (value <= 255)
        {
            string upper = "%" + value.ToString("X2");
            string lower = "%" + value.ToString("x2");

            Assert.Multiple(() =>
            {
                Assert.That(AuthorityParser.ValidateUserInfo(upper.AsSpan(), 0), Is.Null, upper);
                Assert.That(AuthorityParser.ValidateUserInfo(lower.AsSpan(), 0), Is.Null, lower);
                Assert.That(AuthorityParser.ValidateRegisteredName(upper.AsSpan(), 0), Is.Null, upper);
                Assert.That(AuthorityParser.ValidateRegisteredName(lower.AsSpan(), 0), Is.Null, lower);
            });

            value = value + 1;
        }
    }

    [TestCase("%", 10)]
    [TestCase("%0", 10)]
    [TestCase("%G0", 11)]
    [TestCase("%0G", 12)]
    [TestCase("ok%G0", 13)]
    public void MalformedUserInfoPercentTripletsReturnExactUriDiagnostics(
        string text,
        int expectedOffset)
    {
        UriDiagnostic? diagnostic = AuthorityParser.ValidateUserInfo(text.AsSpan(), 10);

        AssertDiagnostic(
            diagnostic,
            UriDiagnosticCode.InvalidPercentEncoding,
            UriComponent.UserInfo,
            expectedOffset,
            DiagnosticFactory.InvalidPercentEncodingMessage);
    }

    [TestCase("%", 20)]
    [TestCase("%0", 20)]
    [TestCase("%G0", 21)]
    [TestCase("%0G", 22)]
    [TestCase("ok%0G", 24)]
    public void MalformedRegisteredNamePercentTripletsReturnExactUriDiagnostics(
        string text,
        int expectedOffset)
    {
        UriDiagnostic? diagnostic = AuthorityParser.ValidateRegisteredName(text.AsSpan(), 20);

        AssertDiagnostic(
            diagnostic,
            UriDiagnosticCode.InvalidPercentEncoding,
            UriComponent.Host,
            expectedOffset,
            DiagnosticFactory.InvalidPercentEncodingMessage);
    }

    [Test]
    public void UserInfoRejectsEveryCharacterOutsideItsLiteralGrammar()
    {
        string invalidCharacters = "@/?#[] \\|`\"<>\u0000\u007f\u0080\u00e9";
        int index = 0;
        while (index < invalidCharacters.Length)
        {
            string text = "ok" + invalidCharacters[index] + "tail";
            UriDiagnostic? diagnostic = AuthorityParser.ValidateUserInfo(text.AsSpan(), 30);

            AssertDiagnostic(
                diagnostic,
                UriDiagnosticCode.InvalidUserInfo,
                UriComponent.UserInfo,
                32,
                DiagnosticFactory.InvalidUserInfoMessage);
            index = index + 1;
        }
    }

    [Test]
    public void RegisteredNameRejectsEveryCharacterOutsideItsLiteralGrammar()
    {
        string invalidCharacters = ":@/?#[] \\|`\"<>\u0000\u007f\u0080\u00e9";
        int index = 0;
        while (index < invalidCharacters.Length)
        {
            string text = "ok" + invalidCharacters[index] + "tail";
            UriDiagnostic? diagnostic = AuthorityParser.ValidateRegisteredName(text.AsSpan(), 40);

            AssertDiagnostic(
                diagnostic,
                UriDiagnosticCode.InvalidHost,
                UriComponent.Host,
                42,
                DiagnosticFactory.InvalidHostMessage);
            index = index + 1;
        }
    }

    [Test]
    public void FirstInvalidCharacterWinsAndOffsetsRemainStableAtBoundaries()
    {
        UriDiagnostic? first = AuthorityParser.ValidateUserInfo("a@%GG".AsSpan(), 50);
        UriDiagnostic? relative = AuthorityParser.ValidateRegisteredName("ab:cd".AsSpan(), -1);
        UriDiagnostic? saturated = AuthorityParser.ValidateRegisteredName(
            "ab:cd".AsSpan(),
            int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.Null);
            Assert.That(first!.Code, Is.EqualTo(UriDiagnosticCode.InvalidUserInfo));
            Assert.That(first.Range.Offset, Is.EqualTo(51));
            Assert.That(relative, Is.Not.Null);
            Assert.That(relative!.Range.Offset, Is.EqualTo(2));
            Assert.That(saturated, Is.Not.Null);
            Assert.That(saturated!.Range.Offset, Is.EqualTo(int.MaxValue));
        });
    }

    [TestCase("Example.COM", "example.com", true)]
    [TestCase("A-B_c~!$&'()*+,;=", "a-b_C~!$&'()*+,;=", true)]
    [TestCase("%aF", "%Af", true)]
    [TestCase("", "", true)]
    [TestCase("example", "examples", false)]
    [TestCase("%41", "A", false)]
    [TestCase("%2F", "/", false)]
    [TestCase("%41", "%42", false)]
    [TestCase("\u00c4", "\u00e4", false)]
    public void RegisteredNameComparisonUsesAsciiCaseWithoutDecoding(
        string left,
        string right,
        bool expected)
    {
        string originalLeft = left;
        string originalRight = right;

        bool equal = AuthorityParser.AreRegisteredNamesEqual(
            left.AsSpan(),
            right.AsSpan());

        Assert.Multiple(() =>
        {
            Assert.That(equal, Is.EqualTo(expected));
            Assert.That(left, Is.EqualTo(originalLeft));
            Assert.That(right, Is.EqualTo(originalRight));
        });
    }

    private static void AssertDiagnostic(
        UriDiagnostic? diagnostic,
        UriDiagnosticCode code,
        UriComponent component,
        int offset,
        string message)
    {
        Assert.That(diagnostic, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(diagnostic!.Domain, Is.EqualTo(DiagnosticDomain.Uri));
            Assert.That(diagnostic.Code, Is.EqualTo(code));
            Assert.That(diagnostic.Severity, Is.EqualTo(DiagnosticSeverity.Error));
            Assert.That(diagnostic.Component, Is.EqualTo(component));
            Assert.That(diagnostic.Range.Offset, Is.EqualTo(offset));
            Assert.That(diagnostic.Range.Length, Is.EqualTo(1));
            Assert.That(diagnostic.Message, Is.EqualTo(message));
        });
    }
}
