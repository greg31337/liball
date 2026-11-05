using System.Text.Json;

namespace jmap.Tests;

public class SubmissionEnvelopeProcessorTests
{
    [Test]
    public void DeriveEnvelopePrefersSenderAndDeduplicatesRecipientsAcrossHeaders()
    {
        Email email = new Email
        {
            Sender = Addresses("sender@example.test"),
            From = Addresses("from@example.test"),
            To = Addresses("first@example.test", "second@example.test"),
            Cc = Addresses("first@EXAMPLE.TEST", "third@example.test"),
            Bcc = Addresses("second@example.test", "fourth@example.test")
        };
        Identity identity = new Identity { Email = "sender@example.test" };

        SubmissionEnvelopeResult result = new SubmissionEnvelopeProcessor().DeriveEnvelope(email, identity);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.InvalidEmailProperties, Is.Empty);
            Assert.That(result.Envelope.MailFrom?.Email, Is.EqualTo("sender@example.test"));
            Assert.That(result.Envelope.MailFrom?.Parameters, Is.Null);
            Assert.That(result.Envelope.RcptTo.Select(value => value.Email), Is.EqualTo(new[]
            {
                "first@example.test",
                "second@example.test",
                "third@example.test",
                "fourth@example.test"
            }));
            Assert.That(result.Envelope.RcptTo.All(value => value.Parameters == null), Is.True);
        }
    }

    [Test]
    public void DeriveEnvelopeFallsBackToFromAndAppliesIdentityAddressRules()
    {
        Email email = new Email
        {
            From = Addresses("alias@example.test"),
            To = Addresses("recipient@example.test")
        };
        SubmissionEnvelopeProcessor processor = new SubmissionEnvelopeProcessor();

        SubmissionEnvelopeResult replaced = processor.DeriveEnvelope(email, new Identity { Email = "owner@example.test" });
        SubmissionEnvelopeResult wildcard = processor.DeriveEnvelope(email, new Identity { Email = "*@example.test" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(replaced.InvalidEmailProperties, Is.Empty);
            Assert.That(replaced.Envelope.MailFrom?.Email, Is.EqualTo("owner@example.test"));
            Assert.That(wildcard.InvalidEmailProperties, Is.Empty);
            Assert.That(wildcard.Envelope.MailFrom?.Email, Is.EqualTo("alias@example.test"));
        }
    }

    [Test]
    public void DeriveEnvelopeReportsAmbiguousOrInvalidHeaderAddresses()
    {
        Email email = new Email
        {
            Headers = new List<EmailHeader>
            {
                new EmailHeader { Name = "Sender", Value = "one@example.test" },
                new EmailHeader { Name = "sender", Value = "two@example.test" }
            },
            Sender = Addresses("two@example.test"),
            From = Addresses("from@example.test"),
            To = Addresses("not an address"),
            Cc = new List<EmailAddress> { new EmailAddress { Email = null } }
        };

        SubmissionEnvelopeResult result = new SubmissionEnvelopeProcessor().DeriveEnvelope(email, new Identity { Email = "owner@example.test" });

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.InvalidEmailProperties, Is.EqualTo(new[] { "sender", "to/0", "cc/0" }));
            Assert.That(result.Envelope.MailFrom?.Email, Is.Null);
            Assert.That(result.Envelope.RcptTo, Is.Empty);
        }
    }

    [Test]
    public void ExplicitEnvelopeAcceptsEmptyReturnPathAndArbitraryExtensionParameters()
    {
        EmailSubmissionAddress mailFrom = SubmissionAddress("");
        mailFrom.Parameters = new Dictionary<string, JsonElement>
        {
            ["X-DELIVERY-ROUTE"] = Json("\"bulk-east\""),
            ["SMTPUTF8"] = Json("null")
        };
        EmailSubmissionAddress recipient = SubmissionAddress("\"quoted local\"@[IPv6:2001:db8::1]");
        recipient.Parameters = new Dictionary<string, JsonElement>
        {
            ["X-RCPT-TAG"] = Json("\"campaign-7\"")
        };
        EmailSubmissionEnvelope envelope = new EmailSubmissionEnvelope { MailFrom = mailFrom };
        envelope.RcptTo.Add(recipient);

        List<string> invalid = new List<string>();
        new SubmissionValidator().ValidateEnvelope(envelope, invalid);

        Assert.That(invalid, Is.Empty);
    }

    [Test]
    public void ExplicitEnvelopeReportsInvalidMailboxAndParameterSyntax()
    {
        EmailSubmissionAddress mailFrom = SubmissionAddress("Display Name <sender@example.test>");
        mailFrom.Parameters = new Dictionary<string, JsonElement>
        {
            ["-BAD"] = Json("\"value\""),
            ["SIZE"] = Json("123")
        };
        EmailSubmissionEnvelope envelope = new EmailSubmissionEnvelope { MailFrom = mailFrom };
        envelope.RcptTo.Add(SubmissionAddress("missing-domain@"));
        envelope.RcptTo.Add(SubmissionAddress("two..dots@example.test"));

        List<string> invalid = new List<string>();
        new SubmissionValidator().ValidateEnvelope(envelope, invalid);

        Assert.That(invalid, Is.EqualTo(new[]
        {
            "envelope/mailFrom/email",
            "envelope/mailFrom/parameters/-BAD",
            "envelope/mailFrom/parameters/SIZE",
            "envelope/rcptTo/0/email",
            "envelope/rcptTo/1/email"
        }));
    }

    [Test]
    public void ExtensionValidationAcceptsAdvertisedStandardAndVendorParametersOnly()
    {
        EmailSubmissionAddress mailFrom = SubmissionAddress("sender@example.test");
        mailFrom.Parameters = new Dictionary<string, JsonElement>
        {
            ["RET"] = Json("\"FULL\""),
            ["X-ROUTE"] = Json("\"east\""),
            ["UNADVERTISED"] = Json("null")
        };
        EmailSubmissionAddress recipient = SubmissionAddress("recipient@example.test");
        recipient.Parameters = new Dictionary<string, JsonElement>
        {
            ["NOTIFY"] = Json("\"FAILURE\""),
            ["RET"] = Json("\"FULL\"")
        };
        EmailSubmissionEnvelope envelope = new EmailSubmissionEnvelope { MailFrom = mailFrom };
        envelope.RcptTo.Add(recipient);
        JmapSubmissionCapabilities capabilities = new JmapSubmissionCapabilities();
        capabilities.SubmissionExtensions["DSN"] = new List<string>();
        capabilities.SubmissionExtensions["X-ROUTE"] = new List<string> { "MODE=FAST" };

        List<string> invalid = new List<string>();
        new SubmissionValidator().ValidateEnvelopeExtensions(envelope, capabilities, invalid);

        Assert.That(invalid, Is.EqualTo(new[]
        {
            "envelope/mailFrom/parameters/UNADVERTISED",
            "envelope/rcptTo/0/parameters/RET"
        }));
    }

    [Test]
    public void SubmissionCapabilityValidationAcceptsArbitrarySmtpExtensionAdvertisements()
    {
        JmapSubmissionCapabilities valid = new JmapSubmissionCapabilities();
        valid.SubmissionExtensions["FUTURERELEASE"] = new List<string> { "T", "+" };
        valid.SubmissionExtensions["X-ROUTE"] = new List<string> { "MODE=FAST" };
        JmapSubmissionCapabilities invalidCapability = new JmapSubmissionCapabilities();
        invalidCapability.SubmissionExtensions["-BAD"] = new List<string>();
        invalidCapability.SubmissionExtensions["X-ROUTE"] = new List<string> { "HAS SPACE", "" };
        SessionValidator validator = new SessionValidator();
        List<string> validPaths = new List<string>();
        List<string> invalidPaths = new List<string>();

        validator.ValidateSubmissionCapability(valid, "submission", validPaths);
        validator.ValidateSubmissionCapability(invalidCapability, "submission", invalidPaths);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(validPaths, Is.Empty);
            Assert.That(invalidPaths, Is.EqualTo(new[]
            {
                "submission/submissionExtensions/-BAD",
                "submission/submissionExtensions/X-ROUTE/0",
                "submission/submissionExtensions/X-ROUTE/1"
            }));
        }
    }

    private static List<EmailAddress> Addresses(params string[] values)
    {
        return values.Select(value => new EmailAddress { Email = value }).ToList();
    }

    private static EmailSubmissionAddress SubmissionAddress(string value)
    {
        return new EmailSubmissionAddress { Email = value };
    }

    private static JsonElement Json(string value)
    {
        using JsonDocument document = JsonDocument.Parse(value);
        return document.RootElement.Clone();
    }
}
