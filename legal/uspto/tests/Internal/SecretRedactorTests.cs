using System.Text.Json;
using uspto;

namespace uspto.Tests;

public class SecretRedactorTests
{
    [Test]
    public void HeaderCopiesMaskCredentialAndCookieValuesWithoutMutatingTheSource()
    {
        const string extraSecret = "diagnostic-secret";
        using HttpResponseMessage response = new HttpResponseMessage();
        response.Headers.TryAddWithoutValidation("X-API-Key", "odp-secret");
        response.Headers.TryAddWithoutValidation("Authorization", "Bearer bearer-secret");
        response.Headers.TryAddWithoutValidation("Set-Cookie", "sessionid=cookie-secret; Secure");
        response.Headers.TryAddWithoutValidation("X-Diagnostic", "failure=" + extraSecret);
        response.Headers.TryAddWithoutValidation("ETag", "safe-validator");

        Dictionary<string, string[]> redacted = SecretRedactor.RedactHeaders(response.Headers, new string[] { extraSecret });

        Assert.Multiple(() =>
        {
            Assert.That(redacted["X-API-Key"], Is.EqualTo(new string[] { SecretRedactor.Replacement }));
            Assert.That(redacted["Authorization"], Is.EqualTo(new string[] { SecretRedactor.Replacement }));
            Assert.That(redacted["Set-Cookie"], Is.EqualTo(new string[] { SecretRedactor.Replacement }));
            Assert.That(redacted["X-Diagnostic"].Single(), Is.EqualTo("failure=" + SecretRedactor.Replacement));
            Assert.That(redacted["ETag"].Single(), Is.EqualTo("safe-validator"));
            Assert.That(response.Headers.GetValues("X-API-Key").Single(), Is.EqualTo("odp-secret"));
            Assert.That(response.Headers.GetValues("X-Diagnostic").Single(), Does.Contain(extraSecret));
        });
    }

    [Test]
    public void UriCopiesRemoveUserInfoAndMaskCredentialsInEveryUriComponent()
    {
        const string knownSecret = "path-secret";
        Uri source = new Uri("https://user:password@example.test/files/path-secret?name=path-secret&API%5FKEY=query-secret&X-Amz-Signature=signed-secret#access_token=fragment-secret");

        Uri redacted = SecretRedactor.RedactUri(source, new string[] { knownSecret });
        string decoded = Uri.UnescapeDataString(redacted.AbsoluteUri);

        Assert.Multiple(() =>
        {
            Assert.That(redacted.UserInfo, Is.Empty);
            Assert.That(decoded, Does.Contain(SecretRedactor.Replacement));
            Assert.That(decoded, Does.Contain("name=" + SecretRedactor.Replacement));
            Assert.That(decoded, Does.Not.Contain("user:password"));
            Assert.That(decoded, Does.Not.Contain(knownSecret));
            Assert.That(decoded, Does.Not.Contain("query-secret"));
            Assert.That(decoded, Does.Not.Contain("signed-secret"));
            Assert.That(decoded, Does.Not.Contain("fragment-secret"));
            Assert.That(source.AbsoluteUri, Does.Contain("query-secret"));
        });
    }

    [Test]
    public void StructuredFieldCopiesAreSafeForErrorsCachesAndManifests()
    {
        const string knownSecret = "cache-secret";
        Dictionary<string, string?> record = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase)
        {
            ["status"] = "ready",
            ["api_key"] = "structured-secret",
            ["downloadUri"] = "https://example.test/file?sig=signed-secret&release=2026-09-02",
            ["cacheKey"] = "GET /record/" + knownSecret,
            ["optional"] = null
        };

        Dictionary<string, string?> redacted = SecretRedactor.RedactFields(record, new string[] { knownSecret });
        string persisted = JsonSerializer.Serialize(redacted);

        Assert.Multiple(() =>
        {
            Assert.That(redacted.Comparer, Is.SameAs(record.Comparer));
            Assert.That(redacted["status"], Is.EqualTo("ready"));
            Assert.That(redacted["api_key"], Is.EqualTo(SecretRedactor.Replacement));
            Assert.That(redacted["downloadUri"], Does.Contain("sig=" + SecretRedactor.Replacement));
            Assert.That(redacted["cacheKey"], Does.Not.Contain(knownSecret));
            Assert.That(redacted["optional"], Is.Null);
            Assert.That(persisted, Does.Not.Contain("structured-secret"));
            Assert.That(persisted, Does.Not.Contain("signed-secret"));
            Assert.That(persisted, Does.Not.Contain(knownSecret));
            Assert.That(record["api_key"], Is.EqualTo("structured-secret"));
        });
    }

    [Test]
    public void JsonErrorRedactionPreservesValidJsonAndNonSensitiveFields()
    {
        const string json = "{\"error\":\"denied\",\"apiKey\":\"json-secret\",\"password\":null,\"tokenCount\":3}";

        string redacted = SecretRedactor.RedactText(json, null)!;
        using JsonDocument parsed = JsonDocument.Parse(redacted);

        Assert.Multiple(() =>
        {
            Assert.That(parsed.RootElement.GetProperty("error").GetString(), Is.EqualTo("denied"));
            Assert.That(parsed.RootElement.GetProperty("apiKey").GetString(), Is.EqualTo(SecretRedactor.Replacement));
            Assert.That(parsed.RootElement.GetProperty("password").GetString(), Is.EqualTo(SecretRedactor.Replacement));
            Assert.That(parsed.RootElement.GetProperty("tokenCount").GetInt32(), Is.EqualTo(3));
            Assert.That(redacted, Does.Not.Contain("json-secret"));
        });
    }

    [Test]
    public void DiagnosticRedactionMasksHeadersAssignmentsAndEncodedKnownSecrets()
    {
        const string knownSecret = "key with spaces/+";
        string encodedSecret = Uri.EscapeDataString(knownSecret);
        string formEncodedSecret = encodedSecret.Replace("%20", "+", StringComparison.OrdinalIgnoreCase);
        string diagnostic = "request failed\nAuthorization: Bearer auth-secret\napi-key=form-secret; client_secret='quoted-secret'; uri=https://example.test/?token=query-secret&note=" + formEncodedSecret + "\nsessionId: session-secret\ntokenCount: 4";

        string redacted = SecretRedactor.RedactText(diagnostic, new string[] { knownSecret })!;

        Assert.Multiple(() =>
        {
            Assert.That(redacted, Does.Contain("Authorization: " + SecretRedactor.Replacement));
            Assert.That(redacted, Does.Contain("api-key=" + SecretRedactor.Replacement));
            Assert.That(redacted, Does.Contain("client_secret='" + SecretRedactor.Replacement + "'"));
            Assert.That(redacted, Does.Contain("token=" + SecretRedactor.Replacement));
            Assert.That(redacted, Does.Contain("sessionId: " + SecretRedactor.Replacement));
            Assert.That(redacted, Does.Contain("tokenCount: 4"));
            Assert.That(redacted, Does.Not.Contain("auth-secret"));
            Assert.That(redacted, Does.Not.Contain("form-secret"));
            Assert.That(redacted, Does.Not.Contain("quoted-secret"));
            Assert.That(redacted, Does.Not.Contain("query-secret"));
            Assert.That(redacted, Does.Not.Contain("session-secret"));
            Assert.That(redacted, Does.Not.Contain(formEncodedSecret));
        });
    }

    [Test]
    public void RelativeUrisAndNullTextAreHandledWithoutInventingAnAuthority()
    {
        Uri relative = new Uri("records/123?api_key=relative-secret&view=full", UriKind.Relative);
        Uri networkPath = new Uri("//user:password@example.test/records?view=full", UriKind.Relative);

        Uri redacted = SecretRedactor.RedactUri(relative, null);
        Uri redactedNetworkPath = SecretRedactor.RedactUri(networkPath, null);

        Assert.Multiple(() =>
        {
            Assert.That(redacted.IsAbsoluteUri, Is.False);
            Assert.That(Uri.UnescapeDataString(redacted.OriginalString), Does.Contain("api_key=" + SecretRedactor.Replacement));
            Assert.That(redacted.OriginalString, Does.Not.Contain("relative-secret"));
            Assert.That(redactedNetworkPath.OriginalString, Is.EqualTo("//example.test/records?view=full"));
            Assert.That(SecretRedactor.RedactText(null, null), Is.Null);
        });
    }
}
