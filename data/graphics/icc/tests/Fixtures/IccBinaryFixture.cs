namespace icc.Tests;

internal sealed class IccBinaryTagFixture
{
    internal uint Signature;
    internal byte[] Payload;

    internal IccBinaryTagFixture(uint signature, byte[] payload)
    {
        Signature = signature;
        Payload = payload.ToArray();
    }
}

internal static class IccBinaryFixture
{
    internal static byte[] CreateTagPayload(uint typeSignature, int length)
    {
        if (length < 8)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        byte[] payload = new byte[length];
        IndependentBigEndian.WriteUInt32(payload, 0, typeSignature);
        return payload;
    }

    internal static byte[] CreateProfile(uint version, uint profileClass,
        uint dataColorSpace, uint connectionSpace, int renderingIntent,
        params IccBinaryTagFixture[] tags)
    {
        int payloadOffset = 132 + (tags.Length * 12);
        int profileSize = payloadOffset;
        int index = 0;
        while (index < tags.Length)
        {
            profileSize = AlignFour(profileSize + tags[index].Payload.Length);
            index = index + 1;
        }

        byte[] bytes = new byte[profileSize];
        IndependentBigEndian.WriteUInt32(bytes, 0, (uint)profileSize);
        IndependentBigEndian.WriteUInt32(bytes, 8, version);
        IndependentBigEndian.WriteUInt32(bytes, 12, profileClass);
        IndependentBigEndian.WriteUInt32(bytes, 16, dataColorSpace);
        IndependentBigEndian.WriteUInt32(bytes, 20, connectionSpace);
        IndependentBigEndian.WriteUInt16(bytes, 24, 2026);
        IndependentBigEndian.WriteUInt16(bytes, 26, 1);
        IndependentBigEndian.WriteUInt16(bytes, 28, 1);
        IndependentBigEndian.WriteUInt32(bytes, 36, 0x61637370);
        IndependentBigEndian.WriteInt32(bytes, 64, renderingIntent);
        IndependentBigEndian.WriteUInt32(bytes, 68, 0x0000f6d6);
        IndependentBigEndian.WriteUInt32(bytes, 72, 0x00010000);
        IndependentBigEndian.WriteUInt32(bytes, 76, 0x0000d32d);
        IndependentBigEndian.WriteUInt32(bytes, 128, (uint)tags.Length);

        index = 0;
        while (index < tags.Length)
        {
            IccBinaryTagFixture tag = tags[index];
            int entryOffset = 132 + (index * 12);
            IndependentBigEndian.WriteUInt32(bytes, entryOffset,
                tag.Signature);
            IndependentBigEndian.WriteUInt32(bytes, entryOffset + 4,
                (uint)payloadOffset);
            IndependentBigEndian.WriteUInt32(bytes, entryOffset + 8,
                (uint)tag.Payload.Length);
            Array.Copy(tag.Payload, 0, bytes, payloadOffset,
                tag.Payload.Length);
            payloadOffset = AlignFour(payloadOffset + tag.Payload.Length);
            index = index + 1;
        }

        return bytes;
    }

    private static int AlignFour(int value)
    {
        return (value + 3) & ~3;
    }
}
