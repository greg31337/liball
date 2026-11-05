using System.Text;
using email;

namespace email.Tests;

public class EmailMessageClassifierTests
{
    [Test]
    public void UsesAutoSubmittedDispositionAndRetainsContradictoryEvidence()
    {
        EmailMessageClassification generated = Classify("Auto-Submitted: auto-generated\r\n\r\nbody");
        EmailMessageClassification manual = Classify("Auto-Submitted: no\r\n\r\nbody");

        Assert.That(generated.IsAutomated(), Is.True);
        Assert.That(generated.GetAutomated().GetConfidence(), Is.EqualTo(EmailMessageClassificationConfidence.High));
        Assert.That(generated.GetAutomated().ContainsReason(EmailMessageClassificationReasonCode.AutoSubmittedGenerated), Is.True);
        Assert.That(generated.GetAutomated().GetReasonAt(0).GetStandard(), Does.Contain("RFC 3834"));
        Assert.That(manual.IsAutomated(), Is.False);
        Assert.That(manual.GetAutomated().GetConfidence(), Is.EqualTo(EmailMessageClassificationConfidence.None));
        Assert.That(manual.GetAutomated().ContainsReason(EmailMessageClassificationReasonCode.AutoSubmittedNo), Is.True);
        Assert.That(manual.GetAutomated().GetReasonAt(0).GetEffect(), Is.EqualTo(EmailMessageClassificationEvidenceEffect.Contradicts));
    }

    [Test]
    public void GivesValidListIdStrongConfidenceAndListCommandsModerateConfidence()
    {
        EmailMessageClassification identified = Classify(
            "List-Id: Project discussion <discuss.lists.example.test>\r\n"
            + "List-Unsubscribe: <mailto:leave@example.test>\r\n"
            + "List-Post: <mailto:discuss@example.test>\r\n\r\nbody");
        EmailMessageClassification commandsOnly = Classify(
            "List-Unsubscribe: <https://lists.example.test/unsubscribe/7>\r\n"
            + "List-Unsubscribe-Post: List-Unsubscribe=One-Click\r\n\r\nbody");

        Assert.That(identified.IsMailingList(), Is.True);
        Assert.That(identified.GetMailingList().GetConfidence(), Is.EqualTo(EmailMessageClassificationConfidence.High));
        Assert.That(identified.GetMailingList().ContainsReason(EmailMessageClassificationReasonCode.ListIdentifier), Is.True);
        Assert.That(identified.GetMailingList().ContainsReason(EmailMessageClassificationReasonCode.ListManagementHeader), Is.True);
        Assert.That(commandsOnly.GetMailingList().GetConfidence(), Is.EqualTo(EmailMessageClassificationConfidence.Medium));
        Assert.That(commandsOnly.GetMailingList().ContainsReason(EmailMessageClassificationReasonCode.ListManagementHeader), Is.True);
    }

    [Test]
    public void DoesNotTrustMalformedOrDuplicatedListIdentifiersAsStrongEvidence()
    {
        EmailMessageClassification malformed = Classify("List-Id: <bad..identifier>\r\n\r\nbody");
        EmailMessageClassification duplicated = Classify(
            "List-Id: <one.lists.example.test>\r\n"
            + "List-Id: <two.lists.example.test>\r\n\r\nbody");

        Assert.That(malformed.IsMailingList(), Is.False);
        Assert.That(malformed.GetMailingList().ContainsReason(EmailMessageClassificationReasonCode.ListIdentifierMalformed), Is.True);
        Assert.That(duplicated.IsMailingList(), Is.True);
        Assert.That(duplicated.GetMailingList().GetConfidence(), Is.EqualTo(EmailMessageClassificationConfidence.Medium));
        Assert.That(duplicated.GetMailingList().ContainsReason(EmailMessageClassificationReasonCode.ListIdentifierDuplicated), Is.True);
    }

    [Test]
    public void ClassifiesFailedAndDelayedDsnActionsAsBounceEvidence()
    {
        EmailMessageClassification classification = Classify(DeliveryStatusReport(
            "Auto-Submitted: no\r\n",
            "Reporting-MTA: dns; mx.example.test\r\n\r\n"
            + "Final-Recipient: rfc822; missing@example.test\r\nAction: failed\r\nStatus: 5.1.1\r\n\r\n"
            + "Final-Recipient: rfc822; waiting@example.test\r\nAction: delayed\r\nStatus: 4.2.0\r\n"));

        Assert.That(classification.IsBounce(), Is.True);
        Assert.That(classification.GetBounce().GetConfidence(), Is.EqualTo(EmailMessageClassificationConfidence.High));
        Assert.That(classification.GetBounce().ContainsReason(EmailMessageClassificationReasonCode.DeliveryStatusFailure), Is.True);
        Assert.That(classification.GetBounce().ContainsReason(EmailMessageClassificationReasonCode.DeliveryStatusDelay), Is.True);
        Assert.That(classification.IsReceipt(), Is.False);
        Assert.That(classification.GetAutomated().GetConfidence(), Is.EqualTo(EmailMessageClassificationConfidence.High));
        Assert.That(classification.GetAutomated().ContainsReason(EmailMessageClassificationReasonCode.AutoSubmittedNo), Is.True);
        Assert.That(classification.GetAutomated().ContainsReason(EmailMessageClassificationReasonCode.DeliveryStatusReport), Is.True);
    }

    [Test]
    public void ClassifiesSuccessfulDsnActionsAsDeliveryReceiptsRatherThanBounces()
    {
        EmailMessageClassification classification = Classify(DeliveryStatusReport(
            "",
            "Reporting-MTA: dns; mx.example.test\r\n\r\n"
            + "Final-Recipient: rfc822; reader@example.test\r\nAction: delivered\r\nStatus: 2.0.0\r\n"));

        Assert.That(classification.IsReceipt(), Is.True);
        Assert.That(classification.GetReceipt().GetConfidence(), Is.EqualTo(EmailMessageClassificationConfidence.High));
        Assert.That(classification.GetReceipt().ContainsReason(EmailMessageClassificationReasonCode.DeliveryStatusSuccess), Is.True);
        Assert.That(classification.IsBounce(), Is.False);
        Assert.That(classification.IsAutomated(), Is.True);
    }

    [Test]
    public void DistinguishesAnMdnFromARequestForOne()
    {
        EmailMessageClassification notification = Classify(
            "Content-Type: multipart/report; boundary=r; report-type=disposition-notification\r\n\r\n"
            + "--r\r\nContent-Type: text/plain\r\n\r\nDisplayed\r\n"
            + "--r\r\nContent-Type: message/disposition-notification\r\n\r\n"
            + "Final-Recipient: rfc822; reader@example.test\r\n"
            + "Disposition: manual-action/MDN-sent-manually; displayed\r\n"
            + "--r--\r\n");
        EmailMessageClassification request = Classify(
            "Disposition-Notification-To: receipts@example.test\r\n"
            + "Subject: Ordinary message\r\n\r\nbody");

        Assert.That(notification.IsReceipt(), Is.True);
        Assert.That(notification.GetReceipt().GetConfidence(), Is.EqualTo(EmailMessageClassificationConfidence.High));
        Assert.That(notification.GetReceipt().ContainsReason(EmailMessageClassificationReasonCode.DispositionNotificationReport), Is.True);
        Assert.That(notification.IsAutomated(), Is.True);
        Assert.That(request.IsReceipt(), Is.False);
        Assert.That(request.GetReceipt().ContainsReason(EmailMessageClassificationReasonCode.DispositionNotificationRequest), Is.True);
        Assert.That(request.GetReceipt().GetReasonAt(0).IsSupporting(), Is.False);
    }

    [Test]
    public void LabelsNonstandardSubjectAndPrecedenceSignalsAsWeak()
    {
        EmailMessageClassification bounceSubject = Classify("Subject: Mail delivery failed\r\n\r\nbody");
        EmailMessageClassification legacyList = Classify("Precedence: list\r\n\r\nbody");

        Assert.That(bounceSubject.GetBounce().GetConfidence(), Is.EqualTo(EmailMessageClassificationConfidence.Low));
        Assert.That(bounceSubject.GetBounce().GetReasonAt(0).GetStrength(), Is.EqualTo(EmailMessageClassificationEvidenceStrength.Weak));
        Assert.That(bounceSubject.GetBounce().GetReasonAt(0).GetStandard(), Does.Contain("not an Internet standard"));
        Assert.That(legacyList.GetMailingList().GetConfidence(), Is.EqualTo(EmailMessageClassificationConfidence.Low));
        Assert.That(legacyList.GetMailingList().ContainsReason(EmailMessageClassificationReasonCode.PrecedenceList), Is.True);
    }

    [Test]
    public void EnforcesLimitsAndReturnsDefensiveResultLists()
    {
        EmailMessage message = ParseMessage("Auto-Submitted: auto-replied\r\nSubject: reply\r\n\r\nbody");
        EmailMessageClassificationOptions options = new EmailMessageClassificationOptions();
        options.SetMaximumHeaderCount(1);

        EmailLimitException exception = Assert.Throws<EmailLimitException>(() => new EmailMessageClassifier().ClassifyWithMessageAndOptions(message, options));
        Assert.That(exception.GetLimitName(), Is.EqualTo("MaxClassificationHeaders"));

        options.SetMaximumHeaderCount(2);
        EmailMessageClassification classification = new EmailMessageClassifier().ClassifyWithMessageAndOptions(message, options);
        classification.GetResults().Clear();
        classification.GetAutomated().GetReasons().Clear();
        Assert.That(classification.GetResults().Count, Is.EqualTo(4));
        Assert.That(classification.GetAutomated().GetReasonCount(), Is.EqualTo(1));

        EmailMessageClassificationOptions clone = options.Clone();
        options.SetMaximumHeaderValueBytes(1);
        Assert.That(clone.GetMaximumHeaderValueBytes(), Is.EqualTo(EmailMessageClassificationOptionsConstants.DefaultMaximumHeaderValueBytes));
        Assert.Throws<ArgumentNullException>(() => new EmailMessageClassifier().ClassifyWithMessage(null!));
        Assert.Throws<ArgumentNullException>(() => new EmailMessageClassifier().ClassifyWithMessageAndOptions(message, null!));
    }

    static string DeliveryStatusReport(string extraHeaders, string fields)
    {
        return extraHeaders
            + "Content-Type: multipart/report; boundary=r; report-type=delivery-status\r\n\r\n"
            + "--r\r\nContent-Type: text/plain\r\n\r\nDelivery status\r\n"
            + "--r\r\nContent-Type: message/delivery-status\r\n\r\n"
            + fields
            + "--r--\r\n";
    }

    static EmailMessageClassification Classify(string source)
    {
        return new EmailMessageClassifier().ClassifyWithMessage(ParseMessage(source));
    }

    static EmailMessage ParseMessage(string source)
    {
        EmailParseResult result = new EmailParser().ParseMessageWithInputAndOptionsUsingValueArrayAndEmailParseOptions(Encoding.UTF8.GetBytes(source), new EmailParseOptionsOperations().CreatePreserving());
        Assert.That(result.HasMessage(), Is.True);
        return result.GetMessage()!;
    }
}
