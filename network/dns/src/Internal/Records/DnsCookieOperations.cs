using System;
using System.Security.Cryptography;

namespace dns;

internal static class DnsCookieOperations
{
    const int ClientCookieLength = 8;
    const int ServerCookieLength = 16;
    const int ServerSecretLength = 16;

    internal static DnsEdnsCookie CreateClientCookie()
    {
        DnsEdnsCookie cookie = new DnsEdnsCookie();
        cookie.ClientCookie = new byte[ClientCookieLength];
        RandomNumberGenerator.Fill(cookie.ClientCookie);
        return cookie;
    }

    internal static DnsEdnsCookie CreateServerCookie(
        DnsEdnsCookie requestCookie,
        byte[] clientAddress,
        byte[] serverSecret,
        uint timestamp)
    {
        ValidateServerInputs(clientAddress, serverSecret);
        if (requestCookie == null || requestCookie.ClientCookie == null ||
            requestCookie.ClientCookie.Length != ClientCookieLength)
        {
            throw new ArgumentException(
                "A DNS request Cookie requires an eight-octet Client Cookie.");
        }

        DnsEdnsCookie cookie = new DnsEdnsCookie();
        cookie.ClientCookie = CopyClientCookie(requestCookie.ClientCookie);
        cookie.ServerCookie = new byte[ServerCookieLength];
        cookie.ServerCookie[0] = 1;
        WriteNetwork32(cookie.ServerCookie, 4, timestamp);
        byte[] input = CreateHashInput(
            cookie.ClientCookie, cookie.ServerCookie, clientAddress);
        byte[] hash = ComputeSipHash(input, serverSecret);
        Array.Copy(hash, 0, cookie.ServerCookie, 8, hash.Length);
        return cookie;
    }

    internal static bool VerifyServerCookie(
        DnsEdnsCookie cookie,
        byte[] clientAddress,
        byte[] serverSecret,
        uint currentTimestamp,
        int maximumPastAgeSeconds,
        int maximumFutureSkewSeconds)
    {
        ValidateServerInputs(clientAddress, serverSecret);
        if (maximumPastAgeSeconds < 0 || maximumFutureSkewSeconds < 0)
        {
            throw new ArgumentException(
                "DNS Cookie timestamp windows cannot be negative.");
        }

        if (cookie == null || cookie.ClientCookie == null ||
            cookie.ServerCookie == null ||
            cookie.ClientCookie.Length != ClientCookieLength ||
            cookie.ServerCookie.Length != ServerCookieLength ||
            cookie.ServerCookie[0] != 1)
        {
            return false;
        }

        uint timestamp = ReadNetwork32(cookie.ServerCookie, 4);
        if (!TimestampIsValid(
                timestamp,
                currentTimestamp,
                maximumPastAgeSeconds,
                maximumFutureSkewSeconds))
        {
            return false;
        }

        byte[] input = CreateHashInput(
            cookie.ClientCookie, cookie.ServerCookie, clientAddress);
        byte[] expected = ComputeSipHash(input, serverSecret);
        return CryptographicOperations.FixedTimeEquals(
            expected, cookie.ServerCookie.AsSpan(8, 8));
    }

    static void ValidateServerInputs(
        byte[] clientAddress,
        byte[] serverSecret)
    {
        if (clientAddress == null ||
            (clientAddress.Length != 4 && clientAddress.Length != 16))
        {
            throw new ArgumentException(
                "A DNS Cookie client address must contain four or sixteen octets.");
        }

        if (serverSecret == null ||
            serverSecret.Length != ServerSecretLength)
        {
            throw new ArgumentException(
                "An RFC 9018 server secret must contain sixteen octets.");
        }
    }

    static byte[] CopyClientCookie(byte[] source)
    {
        byte[] result = new byte[ClientCookieLength];
        Array.Copy(source, 0, result, 0, result.Length);
        return result;
    }

    static byte[] CreateHashInput(
        byte[] clientCookie,
        byte[] serverCookie,
        byte[] clientAddress)
    {
        byte[] result = new byte[16 + clientAddress.Length];
        Array.Copy(clientCookie, 0, result, 0, 8);
        Array.Copy(serverCookie, 0, result, 8, 8);
        Array.Copy(clientAddress, 0, result, 16, clientAddress.Length);
        return result;
    }

    static byte[] ComputeSipHash(byte[] message, byte[] key)
    {
        ulong key0 = ReadLittle64(key, 0);
        ulong key1 = ReadLittle64(key, 8);
        ulong[] state =
        [
            0x736f6d6570736575UL ^ key0,
            0x646f72616e646f6dUL ^ key1,
            0x6c7967656e657261UL ^ key0,
            0x7465646279746573UL ^ key1
        ];
        int offset = 0;
        while (offset + 8 <= message.Length)
        {
            ulong block = ReadLittle64(message, offset);
            state[3] = state[3] ^ block;
            SipRound(state);
            SipRound(state);
            state[0] = state[0] ^ block;
            offset = offset + 8;
        }

        ulong finalBlock = (ulong)message.Length << 56;
        int remainingIndex = 0;
        while (offset + remainingIndex < message.Length)
        {
            finalBlock = finalBlock |
                ((ulong)message[offset + remainingIndex] <<
                    (remainingIndex * 8));
            remainingIndex = remainingIndex + 1;
        }

        state[3] = state[3] ^ finalBlock;
        SipRound(state);
        SipRound(state);
        state[0] = state[0] ^ finalBlock;
        state[2] = state[2] ^ 255;
        SipRound(state);
        SipRound(state);
        SipRound(state);
        SipRound(state);
        ulong hash = state[0] ^ state[1] ^ state[2] ^ state[3];
        byte[] result = new byte[8];
        WriteLittle64(result, hash);
        return result;
    }

    static void SipRound(ulong[] state)
    {
        state[0] = AddModulo64(state[0], state[1]);
        state[1] = RotateLeft(state[1], 13);
        state[1] = state[1] ^ state[0];
        state[0] = RotateLeft(state[0], 32);
        state[2] = AddModulo64(state[2], state[3]);
        state[3] = RotateLeft(state[3], 16);
        state[3] = state[3] ^ state[2];
        state[0] = AddModulo64(state[0], state[3]);
        state[3] = RotateLeft(state[3], 21);
        state[3] = state[3] ^ state[0];
        state[2] = AddModulo64(state[2], state[1]);
        state[1] = RotateLeft(state[1], 17);
        state[1] = state[1] ^ state[2];
        state[2] = RotateLeft(state[2], 32);
    }

    static ulong AddModulo64(ulong left, ulong right)
    {
        ulong lowerMask = 0x7fffffffffffffffUL;
        ulong lower = (left & lowerMask) + (right & lowerMask);
        ulong upper = (left >> 63) + (right >> 63) + (lower >> 63);
        return (lower & lowerMask) | ((upper & 1) << 63);
    }

    static ulong RotateLeft(ulong value, int bits)
    {
        return (value << bits) | (value >> (64 - bits));
    }

    static ulong ReadLittle64(byte[] source, int offset)
    {
        ulong result = 0;
        int index = 0;
        while (index < 8)
        {
            result = result | ((ulong)source[offset + index] << (index * 8));
            index = index + 1;
        }

        return result;
    }

    static void WriteLittle64(byte[] destination, ulong value)
    {
        int index = 0;
        while (index < 8)
        {
            destination[index] = (byte)((value >> (index * 8)) & 255);
            index = index + 1;
        }
    }

    static uint ReadNetwork32(byte[] source, int offset)
    {
        return ((uint)source[offset] << 24) |
            ((uint)source[offset + 1] << 16) |
            ((uint)source[offset + 2] << 8) |
            source[offset + 3];
    }

    static void WriteNetwork32(
        byte[] destination,
        int offset,
        uint value)
    {
        destination[offset] = (byte)((value >> 24) & 255);
        destination[offset + 1] = (byte)((value >> 16) & 255);
        destination[offset + 2] = (byte)((value >> 8) & 255);
        destination[offset + 3] = (byte)(value & 255);
    }

    static bool TimestampIsValid(
        uint timestamp,
        uint currentTimestamp,
        int maximumPastAgeSeconds,
        int maximumFutureSkewSeconds)
    {
        ulong cycle = 4294967296UL;
        ulong forward;
        if (timestamp >= currentTimestamp)
            forward = timestamp - currentTimestamp;
        else
            forward = cycle - currentTimestamp + timestamp;
        if (forward < 2147483648UL)
            return forward <= (ulong)maximumFutureSkewSeconds;

        ulong age;
        if (currentTimestamp >= timestamp)
            age = currentTimestamp - timestamp;
        else
            age = cycle - timestamp + currentTimestamp;
        return age <= (ulong)maximumPastAgeSeconds;
    }
}
