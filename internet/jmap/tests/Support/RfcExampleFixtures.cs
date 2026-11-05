using System.Text;

namespace jmap.Tests;

internal static class RfcExampleFixtures
{
    public const string CoreEchoRequestJson =
        "{\"using\":[\"urn:ietf:params:jmap:core\"],\"methodCalls\":[[\"Core/echo\",{\"hello\":\"world\"},\"c1\"]]}";

    public const string CoreEchoResponseJson =
        "{\"methodResponses\":[[\"Core/echo\",{\"hello\":\"world\"},\"c1\"]],\"sessionState\":\"state-2\"}";

    public const string NotRequestProblemJson =
        "{\"type\":\"urn:ietf:params:jmap:error:notRequest\",\"status\":400,\"detail\":\"Malformed request\"}";

    public const string StateChangeJson =
        "{\"@type\":\"StateChange\",\"changed\":{\"a1\":{\"Email\":\"e2\",\"Mailbox\":\"m3\"}}}";

    public const string TextMessage =
        "From: Alice <alice@example.test>\r\n" +
        "To: Bob <bob@example.test>\r\n" +
        "Subject: Example message\r\n" +
        "Date: Thu, 10 Sep 2026 12:00:00 +0000\r\n" +
        "Message-ID: <example@example.test>\r\n" +
        "Content-Type: text/plain; charset=utf-8\r\n" +
        "\r\n" +
        "Hello from the fixture.\r\n";

    public const string MultipartMessage =
        "From: Alice <alice@example.test>\r\n" +
        "To: Bob <bob@example.test>\r\n" +
        "Subject: Multipart example\r\n" +
        "MIME-Version: 1.0\r\n" +
        "Content-Type: multipart/alternative; boundary=example-boundary\r\n" +
        "\r\n" +
        "--example-boundary\r\n" +
        "Content-Type: text/plain; charset=utf-8\r\n\r\n" +
        "Plain body.\r\n" +
        "--example-boundary\r\n" +
        "Content-Type: text/html; charset=utf-8\r\n\r\n" +
        "<p>HTML body.</p>\r\n" +
        "--example-boundary--\r\n";

    public static string SessionJson(string state)
    {
        return "{\"capabilities\":{},\"accounts\":{},\"primaryAccounts\":{}," +
            "\"username\":\"user@example.test\",\"apiUrl\":\"https://api.example.test/jmap\"," +
            "\"downloadUrl\":\"https://example.test/download/{accountId}/{blobId}/{name}\"," +
            "\"uploadUrl\":\"https://example.test/upload/{accountId}\"," +
            "\"eventSourceUrl\":\"https://example.test/events?types={types}\"," +
            "\"state\":\"" + state + "\"}";
    }

    public static byte[] TextMessageBytes()
    {
        return Encoding.UTF8.GetBytes(TextMessage);
    }

    public static byte[] MultipartMessageBytes()
    {
        return Encoding.UTF8.GetBytes(MultipartMessage);
    }
}
