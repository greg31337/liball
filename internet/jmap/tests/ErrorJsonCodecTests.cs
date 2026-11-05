using System.Text;
using System.Text.Json;

namespace jmap.Tests;

public class ErrorJsonCodecTests
{
    [Test]
    public void ProblemCodecPreservesEveryStandardAndUnknownField()
    {
        ProtocolJsonCodec codec = Codec();
        JmapProblem problem;

        using (JsonDocument document = JsonDocument.Parse(
            "{\"type\":\"urn:ietf:params:jmap:error:limit\",\"title\":\"Limit exceeded\"," +
            "\"status\":400,\"detail\":\"The request is too large.\",\"instance\":\"urn:request:17\"," +
            "\"limit\":\"maxSizeRequest\",\"vendorData\":{\"retryAfter\":30}}"))
        {
            problem = codec.DecodeProblem(document.RootElement);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem.Type, Is.EqualTo(JmapErrorCodes.LimitProblemType));
            Assert.That(problem.Kind, Is.EqualTo(JmapProblemKind.Limit));
            Assert.That(problem.Title, Is.EqualTo("Limit exceeded"));
            Assert.That(problem.Status, Is.EqualTo(400));
            Assert.That(problem.HasStatus, Is.True);
            Assert.That(problem.Detail, Is.EqualTo("The request is too large."));
            Assert.That(problem.Instance, Is.EqualTo("urn:request:17"));
            Assert.That(problem.Limit, Is.EqualTo(JmapProperties.MaxSizeRequest));
            Assert.That(problem.UnknownProperties["vendorData"].GetProperty("retryAfter").GetInt32(), Is.EqualTo(30));
        }

        JmapProblem roundTrip = DecodeProblem(Encoding.UTF8.GetString(EncodeProblem(problem)));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(roundTrip.Kind, Is.EqualTo(JmapProblemKind.Limit));
            Assert.That(roundTrip.HasStatus, Is.True);
            Assert.That(roundTrip.UnknownProperties["vendorData"].GetProperty("retryAfter").GetInt32(), Is.EqualTo(30));
        }
    }

    [Test]
    public void ProblemCodecDistinguishesMissingStatusAndPreservesUnknownType()
    {
        JmapProblem missing = DecodeProblem("{}");
        JmapProblem unknown = DecodeProblem("{\"type\":\"https://example.test/problems/vendor\",\"status\":599}");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(missing.Type, Is.Null);
            Assert.That(missing.Kind, Is.EqualTo(JmapProblemKind.Unknown));
            Assert.That(missing.HasStatus, Is.False);
            Assert.That(Encoding.UTF8.GetString(EncodeProblem(missing)), Is.EqualTo("{}"));
            Assert.That(unknown.Type, Is.EqualTo("https://example.test/problems/vendor"));
            Assert.That(unknown.Kind, Is.EqualTo(JmapProblemKind.Unknown));
            Assert.That(unknown.Status, Is.EqualTo(599));
            Assert.That(unknown.HasStatus, Is.True);
        }
    }

    [TestCase(JmapErrorCodes.UnknownCapabilityProblemType, JmapProblemKind.UnknownCapability)]
    [TestCase(JmapErrorCodes.NotJsonProblemType, JmapProblemKind.NotJson)]
    [TestCase(JmapErrorCodes.NotRequestProblemType, JmapProblemKind.NotRequest)]
    [TestCase(JmapErrorCodes.LimitProblemType, JmapProblemKind.Limit)]
    [TestCase("urn:example:unknown", JmapProblemKind.Unknown)]
    public void ProblemTypesAreClassifiedWithoutChangingTheWireValue(string type, JmapProblemKind expected)
    {
        JmapProblem problem = DecodeProblem("{\"type\":\"" + type + "\"}");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(problem.Kind, Is.EqualTo(expected));
            Assert.That(problem.Type, Is.EqualTo(type));
        }
    }

    [Test]
    public void MethodErrorCodecPreservesDescriptionVendorDataAndUnknownToken()
    {
        ProtocolJsonCodec codec = Codec();
        JmapMethodError error;

        using (JsonDocument document = JsonDocument.Parse(
            "{\"type\":\"vendorTemporaryFailure\",\"description\":\"Try later.\",\"retryAfter\":15,\"details\":{\"node\":\"n1\"}}"))
        {
            error = codec.DecodeMethodError(document.RootElement);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.Type, Is.EqualTo("vendorTemporaryFailure"));
            Assert.That(error.Kind, Is.EqualTo(JmapMethodErrorKind.Unknown));
            Assert.That(error.Description, Is.EqualTo("Try later."));
            Assert.That(error.UnknownProperties["retryAfter"].GetInt32(), Is.EqualTo(15));
            Assert.That(error.UnknownProperties["details"].GetProperty("node").GetString(), Is.EqualTo("n1"));
        }

        JmapMethodError roundTrip = DecodeMethodError(Encoding.UTF8.GetString(EncodeMethodError(error)));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(roundTrip.Type, Is.EqualTo("vendorTemporaryFailure"));
            Assert.That(roundTrip.Kind, Is.EqualTo(JmapMethodErrorKind.Unknown));
            Assert.That(roundTrip.UnknownProperties["details"].GetProperty("node").GetString(), Is.EqualTo("n1"));
        }
    }

    [TestCase(JmapErrorCodes.ServerUnavailable, JmapMethodErrorKind.ServerUnavailable)]
    [TestCase(JmapErrorCodes.ServerFail, JmapMethodErrorKind.ServerFail)]
    [TestCase(JmapErrorCodes.ServerPartialFail, JmapMethodErrorKind.ServerPartialFail)]
    [TestCase(JmapErrorCodes.UnknownMethod, JmapMethodErrorKind.UnknownMethod)]
    [TestCase(JmapErrorCodes.InvalidArguments, JmapMethodErrorKind.InvalidArguments)]
    [TestCase(JmapErrorCodes.InvalidResultReference, JmapMethodErrorKind.InvalidResultReference)]
    [TestCase(JmapErrorCodes.Forbidden, JmapMethodErrorKind.Forbidden)]
    [TestCase(JmapErrorCodes.AccountNotFound, JmapMethodErrorKind.AccountNotFound)]
    [TestCase(JmapErrorCodes.AccountNotSupportedByMethod, JmapMethodErrorKind.AccountNotSupportedByMethod)]
    [TestCase(JmapErrorCodes.AccountReadOnly, JmapMethodErrorKind.AccountReadOnly)]
    [TestCase(JmapErrorCodes.RequestTooLarge, JmapMethodErrorKind.RequestTooLarge)]
    [TestCase(JmapErrorCodes.CannotCalculateChanges, JmapMethodErrorKind.CannotCalculateChanges)]
    [TestCase(JmapErrorCodes.StateMismatch, JmapMethodErrorKind.StateMismatch)]
    [TestCase(JmapErrorCodes.AnchorNotFound, JmapMethodErrorKind.AnchorNotFound)]
    [TestCase(JmapErrorCodes.UnsupportedFilter, JmapMethodErrorKind.UnsupportedFilter)]
    [TestCase(JmapErrorCodes.UnsupportedSort, JmapMethodErrorKind.UnsupportedSort)]
    [TestCase(JmapErrorCodes.TooManyChanges, JmapMethodErrorKind.TooManyChanges)]
    [TestCase(JmapErrorCodes.FromAccountNotFound, JmapMethodErrorKind.FromAccountNotFound)]
    [TestCase(JmapErrorCodes.FromAccountNotSupportedByMethod, JmapMethodErrorKind.FromAccountNotSupportedByMethod)]
    [TestCase("vendorError", JmapMethodErrorKind.Unknown)]
    public void MethodErrorTypesAreClassifiedWithoutChangingTheWireValue(string type, JmapMethodErrorKind expected)
    {
        JmapMethodError error = DecodeMethodError("{\"type\":\"" + type + "\"}");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.Kind, Is.EqualTo(expected));
            Assert.That(error.Type, Is.EqualTo(type));
        }
    }

    [Test]
    public void SetErrorCodecPreservesEveryStructuredAndUnknownField()
    {
        ProtocolJsonCodec codec = Codec();
        JmapSetError error;

        using (JsonDocument document = JsonDocument.Parse(
            "{\"type\":\"invalidRecipients\",\"description\":\"Some recipients are invalid.\"," +
            "\"properties\":[\"emailId\",\"identityId\"],\"existingId\":\"existing-1\"," +
            "\"maxSize\":50000000,\"maxRecipients\":100,\"invalidRecipients\":[\"bad-1\",\"bad-2\"]," +
            "\"notFound\":[\"blob-1\",\"blob-2\"],\"vendorDetail\":{\"smtp\":550}}"))
        {
            error = codec.DecodeSetError(document.RootElement);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.Type, Is.EqualTo(JmapErrorCodes.InvalidRecipients));
            Assert.That(error.Kind, Is.EqualTo(JmapSetErrorKind.InvalidRecipients));
            Assert.That(error.Description, Is.EqualTo("Some recipients are invalid."));
            Assert.That(error.Properties, Is.EqualTo(new[] { "emailId", "identityId" }));
            Assert.That(error.ExistingId, Is.EqualTo("existing-1"));
            Assert.That(error.MaxSize, Is.EqualTo(50000000));
            Assert.That(error.HasMaxSize, Is.True);
            Assert.That(error.MaxRecipients, Is.EqualTo(100));
            Assert.That(error.HasMaxRecipients, Is.True);
            Assert.That(error.InvalidRecipients, Is.EqualTo(new[] { "bad-1", "bad-2" }));
            Assert.That(error.NotFound, Is.EqualTo(new[] { "blob-1", "blob-2" }));
            Assert.That(error.UnknownProperties["vendorDetail"].GetProperty("smtp").GetInt32(), Is.EqualTo(550));
        }

        JmapSetError roundTrip = DecodeSetError(Encoding.UTF8.GetString(EncodeSetError(error)));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(roundTrip.Kind, Is.EqualTo(JmapSetErrorKind.InvalidRecipients));
            Assert.That(roundTrip.Properties, Is.EqualTo(error.Properties));
            Assert.That(roundTrip.HasMaxSize, Is.True);
            Assert.That(roundTrip.HasMaxRecipients, Is.True);
            Assert.That(roundTrip.InvalidRecipients, Is.EqualTo(error.InvalidRecipients));
            Assert.That(roundTrip.NotFound, Is.EqualTo(error.NotFound));
            Assert.That(roundTrip.UnknownProperties["vendorDetail"].GetProperty("smtp").GetInt32(), Is.EqualTo(550));
        }
    }

    [Test]
    public void SetErrorCodecPreservesAbsentVersusPresentZeroLimitsAndEmptyLists()
    {
        JmapSetError absent = DecodeSetError("{\"type\":\"tooLarge\"}");
        JmapSetError present = DecodeSetError("{\"type\":\"tooLarge\",\"maxSize\":0,\"maxRecipients\":0,\"properties\":[],\"invalidRecipients\":[],\"notFound\":[]}");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(absent.HasMaxSize, Is.False);
            Assert.That(absent.HasMaxRecipients, Is.False);
            Assert.That(absent.Properties, Is.Null);
            Assert.That(absent.InvalidRecipients, Is.Null);
            Assert.That(absent.NotFound, Is.Null);
            Assert.That(present.HasMaxSize, Is.True);
            Assert.That(present.HasMaxRecipients, Is.True);
            Assert.That(present.Properties, Is.Empty);
            Assert.That(present.InvalidRecipients, Is.Empty);
            Assert.That(present.NotFound, Is.Empty);
        }

        string json = Encoding.UTF8.GetString(EncodeSetError(present));
        using JsonDocument document = JsonDocument.Parse(json);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(document.RootElement.GetProperty(JmapProperties.MaxSize).GetInt64(), Is.Zero);
            Assert.That(document.RootElement.GetProperty(JmapProperties.MaxRecipients).GetInt64(), Is.Zero);
            Assert.That(document.RootElement.GetProperty(JmapProperties.Properties).GetArrayLength(), Is.Zero);
        }
    }

    [TestCase(JmapErrorCodes.Forbidden, JmapSetErrorKind.Forbidden)]
    [TestCase(JmapErrorCodes.OverQuota, JmapSetErrorKind.OverQuota)]
    [TestCase(JmapErrorCodes.TooLarge, JmapSetErrorKind.TooLarge)]
    [TestCase(JmapErrorCodes.RateLimit, JmapSetErrorKind.RateLimit)]
    [TestCase(JmapErrorCodes.NotFound, JmapSetErrorKind.NotFound)]
    [TestCase(JmapErrorCodes.InvalidPatch, JmapSetErrorKind.InvalidPatch)]
    [TestCase(JmapErrorCodes.WillDestroy, JmapSetErrorKind.WillDestroy)]
    [TestCase(JmapErrorCodes.InvalidProperties, JmapSetErrorKind.InvalidProperties)]
    [TestCase(JmapErrorCodes.Singleton, JmapSetErrorKind.Singleton)]
    [TestCase(JmapErrorCodes.AlreadyExists, JmapSetErrorKind.AlreadyExists)]
    [TestCase(JmapErrorCodes.MailboxHasChild, JmapSetErrorKind.MailboxHasChild)]
    [TestCase(JmapErrorCodes.MailboxHasEmail, JmapSetErrorKind.MailboxHasEmail)]
    [TestCase(JmapErrorCodes.BlobNotFound, JmapSetErrorKind.BlobNotFound)]
    [TestCase(JmapErrorCodes.TooManyKeywords, JmapSetErrorKind.TooManyKeywords)]
    [TestCase(JmapErrorCodes.TooManyMailboxes, JmapSetErrorKind.TooManyMailboxes)]
    [TestCase(JmapErrorCodes.ForbiddenFrom, JmapSetErrorKind.ForbiddenFrom)]
    [TestCase(JmapErrorCodes.InvalidEmail, JmapSetErrorKind.InvalidEmail)]
    [TestCase(JmapErrorCodes.TooManyRecipients, JmapSetErrorKind.TooManyRecipients)]
    [TestCase(JmapErrorCodes.NoRecipients, JmapSetErrorKind.NoRecipients)]
    [TestCase(JmapErrorCodes.InvalidRecipients, JmapSetErrorKind.InvalidRecipients)]
    [TestCase(JmapErrorCodes.ForbiddenMailFrom, JmapSetErrorKind.ForbiddenMailFrom)]
    [TestCase(JmapErrorCodes.ForbiddenToSend, JmapSetErrorKind.ForbiddenToSend)]
    [TestCase(JmapErrorCodes.CannotUnsend, JmapSetErrorKind.CannotUnsend)]
    [TestCase("vendorSetError", JmapSetErrorKind.Unknown)]
    public void SetErrorTypesAreClassifiedWithoutChangingTheWireValue(string type, JmapSetErrorKind expected)
    {
        JmapSetError error = DecodeSetError("{\"type\":\"" + type + "\"}");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(error.Kind, Is.EqualTo(expected));
            Assert.That(error.Type, Is.EqualTo(type));
        }
    }

    [Test]
    public void CodecsRejectWrongFieldShapesAndMissingRequiredErrorTypes()
    {
        JmapMethodError missingMethodType = new JmapMethodError();
        JmapSetError missingSetType = new JmapSetError();

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => DecodeProblem("{\"status\":\"400\"}"), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => DecodeMethodError("{\"type\":null}"), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => DecodeSetError("{\"type\":\"invalidProperties\",\"properties\":{}}"), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => DecodeSetError("{\"type\":\"tooLarge\",\"maxSize\":-1}"), Throws.TypeOf<JmapProtocolException>());
            Assert.That(() => EncodeMethodError(missingMethodType), Throws.TypeOf<JmapValidationException>());
            Assert.That(() => EncodeSetError(missingSetType), Throws.TypeOf<JmapValidationException>());
        }
    }

    private static ProtocolJsonCodec Codec()
    {
        return new ProtocolJsonCodec(new JmapJsonReader(100000, 32));
    }

    private static JmapProblem DecodeProblem(string json)
    {
        JmapJsonReader reader = new JmapJsonReader(100000, 32);
        return new ProtocolJsonCodec(reader).DecodeProblem(reader.ReadUtf8Memory(Encoding.UTF8.GetBytes(json)));
    }

    private static JmapMethodError DecodeMethodError(string json)
    {
        JmapJsonReader reader = new JmapJsonReader(100000, 32);
        return new ProtocolJsonCodec(reader).DecodeMethodError(reader.ReadUtf8Memory(Encoding.UTF8.GetBytes(json)));
    }

    private static JmapSetError DecodeSetError(string json)
    {
        JmapJsonReader reader = new JmapJsonReader(100000, 32);
        return new ProtocolJsonCodec(reader).DecodeSetError(reader.ReadUtf8Memory(Encoding.UTF8.GetBytes(json)));
    }

    private static byte[] EncodeProblem(JmapProblem problem)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec().EncodeProblem(problem, writer);
        writer.DisposeWriter();
        return stream.ToArray();
    }

    private static byte[] EncodeMethodError(JmapMethodError error)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec().EncodeMethodError(error, writer);
        writer.DisposeWriter();
        return stream.ToArray();
    }

    private static byte[] EncodeSetError(JmapSetError error)
    {
        using MemoryStream stream = new MemoryStream();
        JmapJsonWriter writer = new JmapJsonWriter(stream);
        Codec().EncodeSetError(error, writer);
        writer.DisposeWriter();
        return stream.ToArray();
    }
}
