namespace iso20022;

internal static class DerCertificateValidator
{
    internal static bool IsCertificate(byte[] data)
    {
        DerElementData certificate = ReadElement(data, 0, data.Length);
        if (!certificate.Success || certificate.Tag != 48 ||
            certificate.EndOffset != data.Length)
        {
            return false;
        }
        int position = certificate.ContentOffset;
        DerElementData tbs = ReadElement(data, position, certificate.EndOffset);
        if (!tbs.Success || tbs.Tag != 48)
        {
            return false;
        }
        position = tbs.EndOffset;
        DerElementData algorithm = ReadElement(data, position, certificate.EndOffset);
        if (!algorithm.Success || algorithm.Tag != 48)
        {
            return false;
        }
        position = algorithm.EndOffset;
        DerElementData signature = ReadElement(data, position, certificate.EndOffset);
        if (!signature.Success || signature.Tag != 3 ||
            signature.EndOffset != certificate.EndOffset ||
            signature.ContentLength < 2)
        {
            return false;
        }
        return HasRequiredTbsFields(data, tbs);
    }

    internal static bool HasRequiredTbsFields(
        byte[] data,
        DerElementData tbs)
    {
        int position = tbs.ContentOffset;
        DerElementData field = ReadElement(data, position, tbs.EndOffset);
        if (!field.Success)
        {
            return false;
        }
        if (field.Tag == 160)
        {
            position = field.EndOffset;
            field = ReadElement(data, position, tbs.EndOffset);
        }
        if (!field.Success || field.Tag != 2)
        {
            return false;
        }
        position = field.EndOffset;
        int[] tags = new int[] { 48, 48, 48, 48, 48 };
        int index = 0;
        while (index < tags.Length)
        {
            field = ReadElement(data, position, tbs.EndOffset);
            if (!field.Success || field.Tag != tags[index])
            {
                return false;
            }
            position = field.EndOffset;
            index++;
        }
        return true;
    }

    internal static DerElementData ReadElement(
        byte[] data,
        int offset,
        int limit)
    {
        DerElementData result = new DerElementData();
        if (offset < 0 || offset + 2 > limit || limit > data.Length)
        {
            return result;
        }
        result.Tag = data[offset];
        int firstLength = data[offset + 1];
        int headerLength = 2;
        int contentLength = 0;
        if ((firstLength & 128) == 0)
        {
            contentLength = firstLength;
        }
        else
        {
            int lengthBytes = firstLength & 127;
            if (lengthBytes == 0 || lengthBytes > 4 ||
                offset + 2 + lengthBytes > limit)
            {
                return result;
            }
            int index = 0;
            while (index < lengthBytes)
            {
                if (contentLength > (int.MaxValue - data[offset + 2 + index]) / 256)
                {
                    return result;
                }
                contentLength = contentLength * 256 + data[offset + 2 + index];
                index++;
            }
            if (contentLength < 128 || data[offset + 2] == 0)
            {
                return result;
            }
            headerLength = headerLength + lengthBytes;
        }
        if (contentLength < 0 || offset + headerLength > limit - contentLength)
        {
            return result;
        }
        result.ContentOffset = offset + headerLength;
        result.ContentLength = contentLength;
        result.EndOffset = result.ContentOffset + contentLength;
        result.Success = true;
        return result;
    }
}
