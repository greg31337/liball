using System.Net.Mail;
using System.Text;
using fake;

namespace fake.Tests;

[TestFixture]
public class InternetEmailTests
{
    [Test]
    public void TypicalLocalPartsAreDeterministicDotAtomsWithinTheSmtpLimit()
    {
        InternetProvider first = Fake.Create(8101).Internet();
        InternetProvider second = Fake.Create(8101).Internet();

        int index = 0;
        while (index < 512)
        {
            string value = first.EmailLocalPart();
            Assert.That(value, Is.EqualTo(second.EmailLocalPart()));
            Assert.That(value, Does.Match("^[a-z][a-z0-9]{3,9}\\.[a-z][a-z0-9]{3,9}$"));
            Assert.That(Encoding.ASCII.GetByteCount(value), Is.LessThanOrEqualTo(64));
            index++;
        }
    }

    [Test]
    public void ExplicitLocalPartCasesCoverDotAtomQuotedAndBoundaryForms()
    {
        InternetProvider internet = Fake.Create(8102).Internet();
        string typical = internet.EmailLocalPartFromKind(EmailLocalPartCase.Typical);
        string plus = internet.EmailLocalPartFromKind(EmailLocalPartCase.PlusTagged);
        string apostrophe = internet.EmailLocalPartFromKind(EmailLocalPartCase.Apostrophe);
        string quotedSpace = internet.EmailLocalPartFromKind(EmailLocalPartCase.QuotedSpace);
        string quotedSpecial = internet.EmailLocalPartFromKind(EmailLocalPartCase.QuotedSpecial);
        string maximum = internet.EmailLocalPartFromKind(EmailLocalPartCase.MaximumLength);

        Assert.Multiple(() =>
        {
            Assert.That(typical, Does.Match("^[a-z][a-z0-9]+\\.[a-z][a-z0-9]+$"));
            Assert.That(plus, Does.Match("^[a-z][a-z0-9]+\\+tag[0-9]{2}$"));
            Assert.That(apostrophe, Does.Match("^[a-z][a-z0-9]+'[a-z][a-z0-9]+$"));
            Assert.That(quotedSpace, Does.Match("^\"local part [0-9]{2}\"$"));
            Assert.That(quotedSpecial, Does.StartWith("\"quoted\\\"value\\\\part"));
            Assert.That(quotedSpecial, Does.EndWith("\""));
            Assert.That(maximum, Is.EqualTo(new string('a', 64)));
        });
    }

    [Test]
    public void EveryAsciiLocalPartCaseFormsAParseableAddress()
    {
        InternetProvider internet = Fake.Create(8103).Internet();

        foreach (EmailLocalPartCase kind in Enum.GetValues<EmailLocalPartCase>())
        {
            string address = internet.ExampleEmailFromKind(kind);
            MailAddress parsed = new MailAddress(address);
            string localPart = LocalPart(address);

            Assert.That(parsed.Address, Is.EqualTo(address));
            Assert.That(address.All(character => character <= 127), Is.True);
            Assert.That(Encoding.ASCII.GetByteCount(localPart), Is.LessThanOrEqualTo(64));
            Assert.That(IsExampleDomain(Domain(address)), Is.True);
        }
    }

    [Test]
    public void DefaultEmailAndExplicitExampleEmailAreReservedAndDeterministic()
    {
        InternetProvider first = Fake.Create(8104).Internet();
        InternetProvider second = Fake.Create(8104).Internet();

        int index = 0;
        while (index < 512)
        {
            string email = first.Email();
            string example = first.ExampleEmail();

            Assert.That(email, Is.EqualTo(second.Email()));
            Assert.That(example, Is.EqualTo(second.ExampleEmail()));
            Assert.That(IsExampleDomain(Domain(email)), Is.True);
            Assert.That(IsExampleDomain(Domain(example)), Is.True);
            Assert.That(email.All(character => character <= 127), Is.True);
            Assert.That(example.All(character => character <= 127), Is.True);
            Assert.That(new MailAddress(email).Address, Is.EqualTo(email));
            Assert.That(new MailAddress(example).Address, Is.EqualTo(example));
            Assert.That(Encoding.ASCII.GetByteCount(email), Is.LessThanOrEqualTo(254));
            index++;
        }
    }

    [Test]
    public void ExampleEmailRemainsReservedOutsideReservedSafetyMode()
    {
        FakeOptions options = FakeOptions.Create(8105);
        options.SafetyMode = SafetyMode.UnrestrictedShape;
        InternetProvider internet = Fake.Create(options).Internet();

        int index = 0;
        while (index < 256)
        {
            string email = internet.Email();
            string example = internet.ExampleEmail();
            string domain = Domain(email);
            string suffix = domain.Substring(domain.LastIndexOf('.') + 1);

            Assert.That(internet.IsDomainSuffix(suffix), Is.True);
            Assert.That(IsExampleDomain(domain), Is.False);
            Assert.That(IsExampleDomain(Domain(example)), Is.True);
            index++;
        }
    }

    [Test]
    public void InternationalEmailUsesUtf8LocalPartIdnAndReservedDomainTree()
    {
        InternetProvider first = Fake.Create(8106).Internet();
        InternetProvider second = Fake.Create(8106).Internet();

        int index = 0;
        while (index < 256)
        {
            string email = first.InternationalEmail();
            string localPart = LocalPart(email);
            string domain = Domain(email);
            string asciiDomain = first.ToAsciiDomain(domain);

            Assert.That(email, Is.EqualTo(second.InternationalEmail()));
            Assert.That(localPart.Any(character => character > 127), Is.True);
            Assert.That(domain.Any(character => character > 127), Is.True);
            Assert.That(Encoding.UTF8.GetByteCount(localPart), Is.LessThanOrEqualTo(64));
            Assert.That(Encoding.UTF8.GetByteCount(email), Is.LessThanOrEqualTo(254));
            Assert.That(IsReservedSubdomain(asciiDomain), Is.True);
            Assert.That(first.ToAsciiDomain(first.ToUnicodeDomain(asciiDomain)), Is.EqualTo(asciiDomain));
            index++;
        }
    }

    [Test]
    public void DisplayNamesAreDeterministicAndInternationalVariantContainsUnicode()
    {
        InternetProvider first = Fake.Create(8107).Internet();
        InternetProvider second = Fake.Create(8107).Internet();

        int index = 0;
        while (index < 256)
        {
            string ascii = first.DisplayName();
            string international = first.InternationalDisplayName();

            Assert.That(ascii, Is.EqualTo(second.DisplayName()));
            Assert.That(international, Is.EqualTo(second.InternationalDisplayName()));
            Assert.That(ascii, Does.Match("^(Fixture User|Sample Person|Example Contact) [0-9]{3}$"));
            Assert.That(ascii.All(character => character <= 127), Is.True);
            Assert.That(international.Any(character => character > 127), Is.True);
            index++;
        }
    }

    [Test]
    public void MailboxQuotesEscapesAndRoundTripsDisplayNameAndAddress()
    {
        InternetProvider generated = Fake.Create(8108).Internet();
        InternetProvider replay = Fake.Create(8108).Internet();

        string mailbox = generated.Mailbox();
        string expectedName = replay.DisplayName();
        string expectedAddress = replay.Email();
        MailAddress parsed = new MailAddress(mailbox);
        const string callerName = "A \"Quoted\", \\ Name";
        string callerMailbox = generated.MailboxFromDisplayName(callerName);
        MailAddress callerParsed = new MailAddress(callerMailbox);

        Assert.Multiple(() =>
        {
            Assert.That(mailbox, Does.StartWith("\""));
            Assert.That(parsed.DisplayName, Is.EqualTo(expectedName));
            Assert.That(parsed.Address, Is.EqualTo(expectedAddress));
            Assert.That(callerMailbox, Does.StartWith("\"A \\\"Quoted\\\", \\\\ Name\" <"));
            Assert.That(callerParsed.DisplayName, Is.EqualTo("A \\\"Quoted\\\", \\\\ Name"));
            Assert.That(IsExampleDomain(Domain(callerParsed.Address)), Is.True);
        });
    }

    [Test]
    public void InternationalMailboxAndSmtpUtf8AliasesReplayAtEqualSeeds()
    {
        InternetProvider international = Fake.Create(8109).Internet();
        InternetProvider smtp = Fake.Create(8109).Internet();
        InternetProvider mailbox = Fake.Create(8110).Internet();
        InternetProvider smtpMailbox = Fake.Create(8110).Internet();

        string internationalEmail = international.InternationalEmail();
        string smtpEmail = smtp.SmtpUtf8Email();
        string internationalMailbox = mailbox.InternationalMailbox();
        string aliasMailbox = smtpMailbox.SmtpUtf8Mailbox();
        MailAddress parsed = new MailAddress(internationalMailbox);

        Assert.Multiple(() =>
        {
            Assert.That(smtpEmail, Is.EqualTo(internationalEmail));
            Assert.That(aliasMailbox, Is.EqualTo(internationalMailbox));
            Assert.That(parsed.DisplayName.Any(character => character > 127), Is.True);
            Assert.That(parsed.Address.Any(character => character > 127), Is.True);
            Assert.That(IsReservedSubdomain(mailbox.ToAsciiDomain(Domain(parsed.Address))), Is.True);
        });
    }

    [Test]
    public void CallerDisplayNamesRejectHeaderInjectionAndWrongCharacterMode()
    {
        InternetProvider internet = Fake.Create(8111).Internet();

        Assert.That(
            Assert.Throws<ArgumentNullException>(() => internet.MailboxFromDisplayName(null)).ParamName,
            Is.EqualTo("displayName"));
        Assert.That(
            Assert.Throws<ArgumentException>(() => internet.MailboxFromDisplayName("")).ParamName,
            Is.EqualTo("displayName"));
        Assert.That(
            Assert.Throws<ArgumentException>(() => internet.MailboxFromDisplayName("   ")).ParamName,
            Is.EqualTo("displayName"));
        Assert.That(
            Assert.Throws<ArgumentException>(() => internet.MailboxFromDisplayName("Line\r\nBcc: target@example.com")).ParamName,
            Is.EqualTo("displayName"));
        Assert.That(
            Assert.Throws<ArgumentException>(() => internet.MailboxFromDisplayName("Tab\tName")).ParamName,
            Is.EqualTo("displayName"));
        Assert.That(
            Assert.Throws<ArgumentException>(() => internet.MailboxFromDisplayName("Élodie")).ParamName,
            Is.EqualTo("displayName"));
        Assert.DoesNotThrow(() => internet.InternationalMailboxFromDisplayName("Élodie"));
        Assert.That(
            Assert.Throws<ArgumentException>(() => internet.InternationalMailboxFromDisplayName("bad\ud800")).ParamName,
            Is.EqualTo("displayName"));
    }

    [Test]
    public void LocalPartCasesAndEmailOutputsHonorConfiguredLimits()
    {
        InternetProvider internet = Fake.Create(8112).Internet();
        FakeOptions exactOptions = FakeOptions.Create(8112);
        exactOptions.MaximumOutputSize = 64;
        FakeOptions shortOptions = FakeOptions.Create(8112);
        shortOptions.MaximumOutputSize = 63;

        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(
                () => internet.EmailLocalPartFromKind((EmailLocalPartCase)999)).ParamName,
            Is.EqualTo("kind"));
        Assert.That(
            Fake.Create(exactOptions).Internet().EmailLocalPartFromKind(EmailLocalPartCase.MaximumLength),
            Has.Length.EqualTo(64));
        Assert.That(
            Assert.Throws<ArgumentOutOfRangeException>(
                () => Fake.Create(shortOptions).Internet().EmailLocalPartFromKind(EmailLocalPartCase.MaximumLength)).ParamName,
            Is.EqualTo("localPart"));
        FakeOptions tinyOptions = FakeOptions.Create(8112);
        tinyOptions.MaximumOutputSize = 10;
        Assert.Throws<ArgumentOutOfRangeException>(() => Fake.Create(tinyOptions).Internet().Email());
    }

    static string LocalPart(string address)
    {
        return address.Substring(0, address.LastIndexOf('@'));
    }

    static string Domain(string address)
    {
        return address.Substring(address.LastIndexOf('@') + 1);
    }

    static bool IsExampleDomain(string value)
    {
        return value == "example.com" ||
            value == "example.net" ||
            value == "example.org";
    }

    static bool IsReservedSubdomain(string value)
    {
        return value.EndsWith(".example.com", StringComparison.Ordinal) ||
            value.EndsWith(".example.net", StringComparison.Ordinal) ||
            value.EndsWith(".example.org", StringComparison.Ordinal);
    }
}
