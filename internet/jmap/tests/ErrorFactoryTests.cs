using System.Text.Json;

namespace jmap.Tests;

public class ErrorFactoryTests
{
    [Test]
    public void MethodErrorsAreClassifiedWithoutLocalizingDiagnosticDescriptions()
    {
        RecordingLocalizer localizer = new RecordingLocalizer();
        ErrorFactory factory = new ErrorFactory(localizer);

        JmapMethodError error = factory.CreateMethodError(
            JmapErrorCodes.InvalidArguments,
            "The accountId argument is malformed.",
            false);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.Type, Is.EqualTo(JmapErrorCodes.InvalidArguments));
            Assert.That(error.Kind, Is.EqualTo(JmapMethodErrorKind.InvalidArguments));
            Assert.That(error.Description, Is.EqualTo("The accountId argument is malformed."));
            Assert.That(localizer.Calls, Is.Zero);
        }
    }

    [Test]
    public void UnknownMethodErrorsPreserveTheirTokenAndDetachedExtensionData()
    {
        ErrorFactory factory = new ErrorFactory(null);
        JsonDocument document = JsonDocument.Parse("{\"retryAfter\":17}");
        JmapMethodError source = new JmapMethodError();
        source.Type = "vendorTemporaryFailure";
        source.Kind = JmapMethodErrorKind.Forbidden;
        source.Description = "Vendor diagnostic.";
        source.UnknownProperties.Add("vendorData", document.RootElement);

        JmapMethodError error = factory.NormalizeMethodError(source, false);
        document.Dispose();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.Type, Is.EqualTo("vendorTemporaryFailure"));
            Assert.That(error.Kind, Is.EqualTo(JmapMethodErrorKind.Unknown));
            Assert.That(error.Description, Is.EqualTo("Vendor diagnostic."));
            Assert.That(error.UnknownProperties["vendorData"].GetProperty("retryAfter").GetInt32(), Is.EqualTo(17));
            Assert.That(factory.EffectiveMethodErrorKind(error), Is.EqualTo(JmapMethodErrorKind.ServerFail));
        }
    }

    [Test]
    public void OnlyServerPartialFailMayAccompanyAStateChange()
    {
        ErrorFactory factory = new ErrorFactory(null);

        JmapProtocolException standardError = Assert.Throws<JmapProtocolException>(
            () => factory.CreateMethodError(JmapErrorCodes.ServerFail, "Failed.", true))!;
        JmapProtocolException vendorError = Assert.Throws<JmapProtocolException>(
            () => factory.CreateMethodError("vendorFailure", "Failed.", true))!;
        JmapMethodError partial = factory.CreateMethodError(
            JmapErrorCodes.ServerPartialFail,
            "Some changes were applied.",
            true);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(standardError.Path, Is.EqualTo("type"));
            Assert.That(vendorError.Path, Is.EqualTo("type"));
            Assert.That(partial.Kind, Is.EqualTo(JmapMethodErrorKind.ServerPartialFail));
        }
    }

    [Test]
    public void SetErrorsAreReclassifiedAndAllStructuredDataIsDetached()
    {
        ErrorFactory factory = new ErrorFactory(null);
        JsonDocument document = JsonDocument.Parse("{\"policy\":\"custom\"}");
        JmapSetError source = new JmapSetError();
        source.Type = JmapErrorCodes.TooManyRecipients;
        source.Kind = JmapSetErrorKind.NotFound;
        source.Description = "Recipient limit exceeded.";
        source.Properties = new List<string> { "envelope/rcptTo" };
        source.ExistingId = "existing-1";
        source.MaxSize = 1000;
        source.HasMaxSize = true;
        source.MaxRecipients = 50;
        source.HasMaxRecipients = true;
        source.InvalidRecipients = new List<string> { "invalid@example" };
        source.NotFound = new List<string> { "blob-1" };
        source.UnknownProperties.Add("vendorData", document.RootElement);

        JmapSetError error = factory.NormalizeSetError(source);
        source.Properties[0] = "changed";
        source.InvalidRecipients[0] = "changed";
        source.NotFound[0] = "changed";
        document.Dispose();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.Type, Is.EqualTo(JmapErrorCodes.TooManyRecipients));
            Assert.That(error.Kind, Is.EqualTo(JmapSetErrorKind.TooManyRecipients));
            Assert.That(error.Properties, Is.EqualTo(new[] { "envelope/rcptTo" }));
            Assert.That(error.ExistingId, Is.EqualTo("existing-1"));
            Assert.That(error.MaxSize, Is.EqualTo(1000));
            Assert.That(error.HasMaxSize, Is.True);
            Assert.That(error.MaxRecipients, Is.EqualTo(50));
            Assert.That(error.HasMaxRecipients, Is.True);
            Assert.That(error.InvalidRecipients, Is.EqualTo(new[] { "invalid@example" }));
            Assert.That(error.NotFound, Is.EqualTo(new[] { "blob-1" }));
            Assert.That(error.UnknownProperties["vendorData"].GetProperty("policy").GetString(), Is.EqualTo("custom"));
        }
    }

    [Test]
    public void UnknownSetErrorsRemainUnknownAndPreserveTheirExactToken()
    {
        JmapSetError source = new JmapSetError();
        source.Type = "vendorMailboxPolicy";
        source.Kind = JmapSetErrorKind.Forbidden;
        source.UnknownProperties.Add("vendorCode", JsonSerializer.SerializeToElement("P7"));

        JmapSetError error = new ErrorFactory(null).NormalizeSetError(source);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.Type, Is.EqualTo("vendorMailboxPolicy"));
            Assert.That(error.Kind, Is.EqualTo(JmapSetErrorKind.Unknown));
            Assert.That(error.UnknownProperties["vendorCode"].GetString(), Is.EqualTo("P7"));
        }
    }

    [Test]
    public void OnlyForbiddenToSendUsesAcceptLanguageLocalization()
    {
        RecordingLocalizer localizer = new RecordingLocalizer();
        ErrorFactory factory = new ErrorFactory(localizer);

        JmapSetError diagnostic = factory.CreateSetError(
            JmapErrorCodes.InvalidProperties,
            "The identityId property is invalid.");
        JmapSetError visible = factory.CreateUserVisibleSetError(
            JmapErrorCodes.ForbiddenToSend,
            "submission.sendingBlocked",
            "Sending is blocked.",
            "de;q=0.9,en;q=0.8");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(diagnostic.Description, Is.EqualTo("The identityId property is invalid."));
            Assert.That(visible.Kind, Is.EqualTo(JmapSetErrorKind.ForbiddenToSend));
            Assert.That(visible.Description, Is.EqualTo("de:submission.sendingBlocked"));
            Assert.That(localizer.Calls, Is.EqualTo(1));
            Assert.That(localizer.LastKey, Is.EqualTo("submission.sendingBlocked"));
            Assert.That(localizer.LastAcceptLanguage, Is.EqualTo("de;q=0.9,en;q=0.8"));
        }
    }

    [Test]
    public void UserVisibleErrorFallsBackWithoutALocalizerAndRejectsDiagnosticTypes()
    {
        ErrorFactory factory = new ErrorFactory(null);

        JmapSetError error = factory.CreateUserVisibleSetError(
            JmapErrorCodes.ForbiddenToSend,
            "submission.sendingBlocked",
            "Sending is blocked.",
            "fr");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.Description, Is.EqualTo("Sending is blocked."));
            Assert.That(
                () => factory.CreateUserVisibleSetError(
                    JmapErrorCodes.InvalidProperties,
                    "invalid.properties",
                    "Invalid.",
                    "fr"),
                Throws.TypeOf<ArgumentException>());
        }
    }

    [Test]
    public void InvalidFactoryInputsAreRejectedBeforeErrorsAreReturned()
    {
        ErrorFactory factory = new ErrorFactory(null);
        JmapSetError invalidId = new JmapSetError();
        invalidId.Type = JmapErrorCodes.AlreadyExists;
        invalidId.ExistingId = "bad/id";
        JmapSetError invalidMaximum = new JmapSetError();
        invalidMaximum.Type = JmapErrorCodes.TooLarge;
        invalidMaximum.HasMaxSize = true;
        invalidMaximum.MaxSize = -1;
        JmapMethodError invalidExtension = new JmapMethodError();
        invalidExtension.Type = JmapErrorCodes.ServerFail;
        invalidExtension.UnknownProperties.Add("vendorData", default(JsonElement));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(
                () => factory.CreateMethodError(null, "Missing type.", false),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => factory.NormalizeSetError(invalidId),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => factory.NormalizeSetError(invalidMaximum),
                Throws.TypeOf<ArgumentException>());
            Assert.That(
                () => factory.NormalizeMethodError(invalidExtension, false),
                Throws.TypeOf<ArgumentException>());
        }
    }

    private class RecordingLocalizer : IJmapStringLocalizer
    {
        public int Calls;
        public string? LastKey;
        public string? LastAcceptLanguage;

        public string LocalizeJmapString(string key, string? acceptLanguage)
        {
            Calls = Calls + 1;
            LastKey = key;
            LastAcceptLanguage = acceptLanguage;
            return "de:" + key;
        }
    }
}
