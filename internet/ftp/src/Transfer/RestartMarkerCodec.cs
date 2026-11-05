using System.Globalization;

namespace ftp;

internal class RestartMarkerCodec
{
    internal string? LatestMarker;
    internal FtpRestartCheckpoint? LatestCheckpoint;

    internal RestartMarkerCodec()
    {
        LatestMarker = null;
        LatestCheckpoint = null;
    }

    internal string GenerateMarker(FtpRestartCheckpoint? checkpoint)
    {
        ValidateCheckpoint(checkpoint);
        if (checkpoint == null)
        {
            throw new FtpConfigurationException("A restart checkpoint is required.", null);
        }

        string pendingCarriageReturn = checkpoint.PendingCarriageReturn ? "1" : "0";
        string marker = "F1:" +
            checkpoint.LogicalByteOffset.ToString(CultureInfo.InvariantCulture) + ":" +
            checkpoint.WireByteOffset.ToString(CultureInfo.InvariantCulture) + ":" +
            checkpoint.StructureUnitCount.ToString(CultureInfo.InvariantCulture) + ":" +
            checkpoint.PendingBitCount.ToString(CultureInfo.InvariantCulture) + ":" +
            pendingCarriageReturn;
        LatestMarker = marker;
        LatestCheckpoint = CopyCheckpoint(checkpoint);
        return marker;
    }

    internal FtpRestartCheckpoint ParseMarker(string? marker)
    {
        string checkedMarker = ValidateOpaqueMarker(marker, 127);
        string[] fields = checkedMarker.Split(new char[] { ':' });
        if (fields.Length != 6 || fields[0] != "F1")
        {
            throw new FtpTransferException("The restart marker does not use the supported F1 checkpoint format.", null);
        }

        long logicalByteOffset = ParseDecimal(fields[1], "logical byte offset");
        long wireByteOffset = ParseDecimal(fields[2], "wire byte offset");
        long structureUnitCount = ParseDecimal(fields[3], "structure unit count");
        long pendingBitCount = ParseDecimal(fields[4], "pending bit count");
        long pendingCarriageReturn = ParseDecimal(fields[5], "pending carriage-return state");
        if (pendingBitCount > 7)
        {
            throw new FtpTransferException("A restart marker pending bit count must be from zero through seven.", null);
        }

        if (pendingCarriageReturn > 1)
        {
            throw new FtpTransferException("A restart marker carriage-return state must be zero or one.", null);
        }

        FtpRestartCheckpoint checkpoint = new FtpRestartCheckpoint();
        checkpoint.LogicalByteOffset = logicalByteOffset;
        checkpoint.WireByteOffset = wireByteOffset;
        checkpoint.StructureUnitCount = structureUnitCount;
        checkpoint.PendingBitCount = (int)pendingBitCount;
        checkpoint.PendingCarriageReturn = pendingCarriageReturn == 1;
        LatestMarker = checkedMarker;
        LatestCheckpoint = CopyCheckpoint(checkpoint);
        return checkpoint;
    }

    internal string AcceptOpaqueMarker(string? marker, int maximumLength)
    {
        string checkedMarker = ValidateOpaqueMarker(marker, maximumLength);
        LatestMarker = checkedMarker;
        LatestCheckpoint = null;
        return checkedMarker;
    }

    internal string ValidateOpaqueMarker(string? marker, int maximumLength)
    {
        if (maximumLength <= 0)
        {
            throw new FtpConfigurationException("A positive restart marker length limit is required.", null);
        }

        if (string.IsNullOrEmpty(marker) || marker.Length > maximumLength)
        {
            throw new FtpTransferException("A restart marker is empty or exceeds its transfer-mode limit.", null);
        }

        int index = 0;
        while (index < marker.Length)
        {
            char value = marker[index];
            if (value < 33 || value > 126)
            {
                throw new FtpTransferException("A restart marker must be printable ASCII without spaces.", null);
            }

            index++;
        }

        return marker;
    }

    internal string? GetLatestMarker()
    {
        return LatestMarker;
    }

    internal FtpRestartCheckpoint? GetLatestCheckpoint()
    {
        if (LatestCheckpoint == null)
        {
            return null;
        }

        return CopyCheckpoint(LatestCheckpoint);
    }

    internal void ValidateCheckpoint(FtpRestartCheckpoint? checkpoint)
    {
        if (checkpoint == null)
        {
            throw new FtpConfigurationException("A restart checkpoint is required.", null);
        }

        if (checkpoint.LogicalByteOffset < 0 ||
            checkpoint.WireByteOffset < 0 ||
            checkpoint.StructureUnitCount < 0)
        {
            throw new FtpConfigurationException("Restart checkpoint offsets and counts cannot be negative.", null);
        }

        if (checkpoint.PendingBitCount < 0 || checkpoint.PendingBitCount > 7)
        {
            throw new FtpConfigurationException("A restart checkpoint pending bit count must be from zero through seven.", null);
        }
    }

    internal long ParseDecimal(string text, string fieldName)
    {
        if (text.Length == 0)
        {
            throw new FtpTransferException($"The restart marker {fieldName} is empty.", null);
        }

        long value = 0;
        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character < '0' || character > '9')
            {
                throw new FtpTransferException($"The restart marker {fieldName} is not decimal.", null);
            }

            int digit = character - '0';
            if (value > (long.MaxValue - digit) / 10)
            {
                throw new FtpTransferException($"The restart marker {fieldName} exceeds Int64 capacity.", null);
            }

            value = value * 10 + digit;
            index++;
        }

        return value;
    }

    internal FtpRestartCheckpoint CopyCheckpoint(FtpRestartCheckpoint source)
    {
        FtpRestartCheckpoint copy = new FtpRestartCheckpoint();
        copy.LogicalByteOffset = source.LogicalByteOffset;
        copy.WireByteOffset = source.WireByteOffset;
        copy.StructureUnitCount = source.StructureUnitCount;
        copy.PendingBitCount = source.PendingBitCount;
        copy.PendingCarriageReturn = source.PendingCarriageReturn;
        return copy;
    }
}
