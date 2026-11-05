using htmlgui;

namespace htmlgui.Tests;

public class BridgeDiagnosticRedactorTests
{
    [Test]
    public void RedactsNestedSensitiveFieldsAndDetachesDiagnosticEnvelope()
    {
        byte[] publicBytes = new byte[] { 1, 2, 3 };
        UiBridgeEnvelope envelope = new UiBridgeEnvelope
        {
            Version = BridgeProtocolVersions.Current,
            Session = "session-secret",
            Sequence = 1,
            CorrelationId = "correlation-1",
            Origin = BridgeProtocolNames.HostOrigin,
            Kind = UiBridgeMessageKind.Request,
            Request = new UiBridgeRequest
            {
                CorrelationId = "correlation-1",
                Operation = "login",
                TimeoutMilliseconds = 10,
                Arguments = new UiValueEntry[]
                {
                    Entry("accessToken", Text("token-value")),
                    Entry("profile", Object(
                        Entry("password", Text("password-value")),
                        Entry("displayName", Text("Ada")),
                        Entry("avatar", new UiValue { Kind = UiValueKind.Binary, BinaryValue = publicBytes })))
                }
            }
        };

        BridgeDiagnosticRedactionResult result = new BridgeDiagnosticRedactor().RedactBridgeDiagnostic(envelope, 4096);
        UiValueEntry[] arguments = result.Envelope!.Request!.Arguments;
        UiValueEntry[] profile = arguments.Single(item => item.Key == "profile").Value.ObjectValue!;

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Envelope, Is.Not.SameAs(envelope));
            Assert.That(result.Envelope.Session, Is.EqualTo(BridgeDiagnosticRedactor.RedactedText));
            Assert.That(arguments.Single(item => item.Key == "accessToken").Value.StringValue, Is.EqualTo(BridgeDiagnosticRedactor.RedactedText));
            Assert.That(profile.Single(item => item.Key == "password").Value.StringValue, Is.EqualTo(BridgeDiagnosticRedactor.RedactedText));
            Assert.That(profile.Single(item => item.Key == "displayName").Value.StringValue, Is.EqualTo("Ada"));
            Assert.That(profile.Single(item => item.Key == "avatar").Value.BinaryValue, Is.EqualTo(publicBytes));
            Assert.That(profile.Single(item => item.Key == "avatar").Value.BinaryValue, Is.Not.SameAs(publicBytes));
            Assert.That(envelope.Request!.Arguments[0].Value.StringValue, Is.EqualTo("token-value"));
        });
    }

    [Test]
    public void RejectsMalformedDiagnosticsAndRecognizesSensitiveFieldVariants()
    {
        UiBridgeEnvelope malformed = new UiBridgeEnvelope
        {
            Version = BridgeProtocolVersions.Current,
            Session = "session",
            Sequence = 1,
            Origin = BridgeProtocolNames.HostOrigin,
            Kind = UiBridgeMessageKind.Event,
            Payload = new UiValue { Kind = UiValueKind.String }
        };
        BridgeDiagnosticRedactor redactor = new BridgeDiagnosticRedactor();

        BridgeDiagnosticRedactionResult result = redactor.RedactBridgeDiagnostic(malformed, 4096);

        Assert.Multiple(() =>
        {
            Assert.That(result.Code, Is.EqualTo("bridge.invalid-value"));
            Assert.That(result.Envelope, Is.Null);
            Assert.That(redactor.IsSensitiveDiagnosticField("API_KEY"), Is.True);
            Assert.That(redactor.IsSensitiveDiagnosticField("client-secret"), Is.True);
            Assert.That(redactor.IsSensitiveDiagnosticField("authorization"), Is.True);
            Assert.That(redactor.IsSensitiveDiagnosticField("displayName"), Is.False);
        });
    }

    private static UiValueEntry Entry(string key, UiValue value)
    {
        return new UiValueEntry { Key = key, Value = value };
    }

    private static UiValue Text(string value)
    {
        return new UiValue { Kind = UiValueKind.String, StringValue = value };
    }

    private static UiValue Object(params UiValueEntry[] entries)
    {
        return new UiValue { Kind = UiValueKind.Object, ObjectValue = entries };
    }
}
