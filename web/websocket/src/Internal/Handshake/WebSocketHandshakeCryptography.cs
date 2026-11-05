using System;

namespace websocket;

internal class WebSocketHandshakeValueResult
{
    internal WebSocketParseStatus Status = WebSocketParseStatus.ProtocolFailure;
    internal string Value = "";
}

internal static class WebSocketHandshakeCryptography
{
    const int ClientNonceEncodedLength = 24;
    const int AcceptEncodedLength = 28;

    internal static WebSocketHandshakeValueResult CreateClientNonce()
    {
        byte[] nonce = new byte[WebSocketProtocolConstants.ClientNonceLength];
        System.Security.Cryptography.RandomNumberGenerator.Fill(nonce);

        char[] encodedNonce = new char[ClientNonceEncodedLength];
        Base64EncodeResult encoding = Base64Codec.EncodeClientNonce(nonce, encodedNonce);
        if (encoding.Status != WebSocketParseStatus.Complete ||
            encoding.ProducedCount != encodedNonce.Length)
        {
            return new WebSocketHandshakeValueResult();
        }

        WebSocketHandshakeValueResult result = new WebSocketHandshakeValueResult();
        result.Status = WebSocketParseStatus.Complete;
        result.Value = new string(encodedNonce);
        return result;
    }

    internal static WebSocketHandshakeValueResult ComputeAccept(ReadOnlySpan<char> clientNonce)
    {
        byte[] decodedNonce = new byte[WebSocketProtocolConstants.ClientNonceLength];
        Base64DecodeResult decoding = Base64Codec.DecodeClientNonce(clientNonce, decodedNonce);
        if (decoding.Status != WebSocketParseStatus.Complete ||
            decoding.ConsumedCount != clientNonce.Length ||
            decoding.ProducedCount != decodedNonce.Length)
        {
            return new WebSocketHandshakeValueResult();
        }

        byte[] hashInput = new byte[clientNonce.Length + WebSocketProtocolConstants.AcceptGuid.Length];
        int index = 0;
        while (index < clientNonce.Length)
        {
            hashInput[index] = (byte)clientNonce[index];
            index = index + 1;
        }

        int guidIndex = 0;
        while (guidIndex < WebSocketProtocolConstants.AcceptGuid.Length)
        {
            hashInput[index] = (byte)WebSocketProtocolConstants.AcceptGuid[guidIndex];
            index = index + 1;
            guidIndex = guidIndex + 1;
        }

        byte[] digest = System.Security.Cryptography.SHA1.HashData(hashInput);
        char[] encodedAccept = new char[AcceptEncodedLength];
        Base64EncodeResult encoding = Base64Codec.EncodeAcceptDigest(digest, encodedAccept);
        if (encoding.Status != WebSocketParseStatus.Complete ||
            encoding.ProducedCount != encodedAccept.Length)
        {
            return new WebSocketHandshakeValueResult();
        }

        WebSocketHandshakeValueResult result = new WebSocketHandshakeValueResult();
        result.Status = WebSocketParseStatus.Complete;
        result.Value = new string(encodedAccept);
        return result;
    }
}
