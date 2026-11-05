using System.Collections.Generic;

namespace ftp;

internal class FtpPageStructureState
{
    internal int LogicalByteSize;
    internal int StorageByteCount;
    internal int UnusedHighBits;
    internal bool EndOfFileReached;
    internal long NextLogicalByteOffset;
    internal int OperationKind;
    internal List<FtpPageRestartLocation> RestartLocations;

    internal FtpPageStructureState(int logicalByteSize)
    {
        if (logicalByteSize < 3 || logicalByteSize > 255)
        {
            throw new FtpConfigurationException("Page structure requires a logical byte size from 3 through 255 bits.", null);
        }

        LogicalByteSize = logicalByteSize;
        StorageByteCount = (logicalByteSize + 7) / 8;
        UnusedHighBits = StorageByteCount * 8 - logicalByteSize;
        EndOfFileReached = false;
        NextLogicalByteOffset = 0;
        OperationKind = 0;
        RestartLocations = new List<FtpPageRestartLocation>();
    }

    internal FtpPageFrame EncodePage(FtpPageRequest? request)
    {
        if (OperationKind == 2)
        {
            throw new FtpTransferException("A page-structure state cannot switch from decoding to encoding.", null);
        }

        OperationKind = 1;
        if (EndOfFileReached)
        {
            throw new FtpTransferException("A page cannot follow the last-page marker.", null);
        }

        if (request == null || request.PageIndex == null ||
            request.OptionalHeaderFields == null || request.Data == null)
        {
            throw new FtpConfigurationException("A complete page request is required.", null);
        }

        ValidateLogicalUnits(request.PageIndex, true);
        ValidateLogicalUnits(request.OptionalHeaderFields, false);
        ValidateLogicalUnits(request.Data, false);
        int optionalFieldCount = request.OptionalHeaderFields.Length / StorageByteCount;
        int dataLogicalByteCount = request.Data.Length / StorageByteCount;
        int headerLogicalByteCount = 4 + optionalFieldCount;
        ValidatePageDescriptor(headerLogicalByteCount, dataLogicalByteCount, request.PageType, optionalFieldCount);
        ValidateLengthValue(headerLogicalByteCount, "page header length");
        ValidateLengthValue(dataLogicalByteCount, "page data length");

        int totalStorageBytes = GetTotalStorageBytes(headerLogicalByteCount, dataLogicalByteCount);
        byte[] serialized = new byte[totalStorageBytes];
        WriteInteger(headerLogicalByteCount, serialized, 0);
        System.Array.Copy(request.PageIndex, 0, serialized, StorageByteCount, StorageByteCount);
        WriteInteger(dataLogicalByteCount, serialized, 2);
        WriteInteger(request.PageType, serialized, 3);
        if (request.OptionalHeaderFields.Length != 0)
        {
            System.Array.Copy(request.OptionalHeaderFields, 0, serialized,
                4 * StorageByteCount, request.OptionalHeaderFields.Length);
        }

        if (request.Data.Length != 0)
        {
            System.Array.Copy(request.Data, 0, serialized,
                headerLogicalByteCount * StorageByteCount, request.Data.Length);
        }

        FtpPageFrame frame = CreatePageFrame(
            headerLogicalByteCount,
            dataLogicalByteCount,
            request.PageIndex,
            request.PageType,
            request.OptionalHeaderFields,
            request.Data,
            serialized);
        RegisterPage(frame);
        return frame;
    }

    internal FtpPageFrame DecodePage(byte[]? source, int offset, int count)
    {
        if (OperationKind == 1)
        {
            throw new FtpTransferException("A page-structure state cannot switch from encoding to decoding.", null);
        }

        OperationKind = 2;
        if (EndOfFileReached)
        {
            throw new FtpTransferException("A page cannot follow the last-page marker.", null);
        }

        if (source == null)
        {
            throw new FtpTransferException("Page-structure source data is required.", null);
        }

        if (offset < 0 || count < 0 || offset > source.Length - count)
        {
            throw new FtpTransferException("The page-structure source slice is invalid.", null);
        }

        int minimumHeaderBytes = 4 * StorageByteCount;
        if (count < minimumHeaderBytes || count % StorageByteCount != 0)
        {
            throw new FtpTransferException("A page frame is shorter than its mandatory logical-byte header.", null);
        }

        byte[] serialized = new byte[count];
        System.Array.Copy(source, offset, serialized, 0, count);
        ValidateLogicalUnits(serialized, false);
        long headerValue = ReadInteger(serialized, 0);
        long dataValue = ReadInteger(serialized, 2);
        long pageTypeValue = ReadInteger(serialized, 3);
        if (headerValue > int.MaxValue || dataValue > int.MaxValue || pageTypeValue > int.MaxValue)
        {
            throw new FtpTransferException("A page header value exceeds the supported Int32 range.", null);
        }

        int headerLogicalByteCount = (int)headerValue;
        int dataLogicalByteCount = (int)dataValue;
        int pageType = (int)pageTypeValue;
        int optionalFieldCount = headerLogicalByteCount - 4;
        ValidatePageDescriptor(headerLogicalByteCount, dataLogicalByteCount, pageType, optionalFieldCount);
        int expectedStorageBytes = GetTotalStorageBytes(headerLogicalByteCount, dataLogicalByteCount);
        if (count != expectedStorageBytes)
        {
            throw new FtpTransferException("The page frame length does not match its header and data lengths.", null);
        }

        byte[] pageIndex = new byte[StorageByteCount];
        System.Array.Copy(serialized, StorageByteCount, pageIndex, 0, StorageByteCount);
        byte[] optionalFields = new byte[optionalFieldCount * StorageByteCount];
        if (optionalFields.Length != 0)
        {
            System.Array.Copy(serialized, 4 * StorageByteCount,
                optionalFields, 0, optionalFields.Length);
        }

        byte[] data = new byte[dataLogicalByteCount * StorageByteCount];
        if (data.Length != 0)
        {
            System.Array.Copy(serialized, headerLogicalByteCount * StorageByteCount,
                data, 0, data.Length);
        }

        FtpPageFrame frame = CreatePageFrame(
            headerLogicalByteCount,
            dataLogicalByteCount,
            pageIndex,
            pageType,
            optionalFields,
            data,
            serialized);
        RegisterPage(frame);
        return frame;
    }

    internal void RequireEndOfFile()
    {
        if (!EndOfFileReached)
        {
            throw new FtpTransferException("A page-structured transfer must end with a last-page header.", null);
        }
    }

    internal FtpPageRestartLocation? FindRestartLocation(byte[]? pageIndex)
    {
        if (pageIndex == null || pageIndex.Length != StorageByteCount)
        {
            throw new FtpConfigurationException("A complete page index is required for restart lookup.", null);
        }

        int index = 0;
        while (index < RestartLocations.Count)
        {
            FtpPageRestartLocation location = RestartLocations[index];
            if (IndexesEqual(location.PageIndex, pageIndex))
            {
                return location;
            }

            index++;
        }

        return null;
    }

    internal FtpPageFrame CreatePageFrame(
        int headerLogicalByteCount,
        int dataLogicalByteCount,
        byte[] pageIndex,
        int pageType,
        byte[] optionalFields,
        byte[] data,
        byte[] serialized)
    {
        FtpPageFrame frame = new FtpPageFrame();
        frame.HeaderLogicalByteCount = headerLogicalByteCount;
        frame.DataLogicalByteCount = dataLogicalByteCount;
        frame.PageIndex = CopyBytes(pageIndex);
        frame.PageType = pageType;
        frame.OptionalHeaderFields = CopyBytes(optionalFields);
        frame.Data = CopyBytes(data);
        frame.SerializedLogicalBytes = CopyBytes(serialized);
        frame.LastPage = pageType == 0;
        return frame;
    }

    internal void RegisterPage(FtpPageFrame frame)
    {
        long pageLogicalByteCount = frame.HeaderLogicalByteCount + (long)frame.DataLogicalByteCount;
        if (NextLogicalByteOffset > long.MaxValue - pageLogicalByteCount)
        {
            throw new FtpTransferException("The page restart location exceeded Int64 capacity.", null);
        }

        if (!frame.LastPage)
        {
            if (FindExistingRestartLocation(frame.PageIndex) != null)
            {
                throw new FtpTransferException("A page index may occur only once in a paged transfer.", null);
            }

            FtpPageRestartLocation location = new FtpPageRestartLocation(
                CopyBytes(frame.PageIndex), NextLogicalByteOffset);
            RestartLocations.Add(location);
            frame.RestartLocation = location;
        }

        NextLogicalByteOffset += pageLogicalByteCount;
        if (frame.LastPage)
        {
            EndOfFileReached = true;
        }
    }

    internal FtpPageRestartLocation? FindExistingRestartLocation(byte[] pageIndex)
    {
        int index = 0;
        while (index < RestartLocations.Count)
        {
            FtpPageRestartLocation location = RestartLocations[index];
            if (IndexesEqual(location.PageIndex, pageIndex))
            {
                return location;
            }

            index++;
        }

        return null;
    }

    internal void ValidatePageDescriptor(
        int headerLogicalByteCount,
        int dataLogicalByteCount,
        int pageType,
        int optionalFieldCount)
    {
        if (headerLogicalByteCount < 4 || optionalFieldCount < 0 ||
            headerLogicalByteCount != 4 + optionalFieldCount)
        {
            throw new FtpTransferException("A page header length must include four mandatory logical bytes.", null);
        }

        if (dataLogicalByteCount < 0)
        {
            throw new FtpTransferException("A page data length cannot be negative.", null);
        }

        if (pageType == 0)
        {
            if (headerLogicalByteCount != 4 || dataLogicalByteCount != 0 || optionalFieldCount != 0)
            {
                throw new FtpTransferException("A last-page header must have length four and no data.", null);
            }
        }
        else if (pageType == 1)
        {
            if (headerLogicalByteCount != 4 || optionalFieldCount != 0)
            {
                throw new FtpTransferException("A simple-page header must have length four.", null);
            }
        }
        else if (pageType == 2)
        {
            return;
        }
        else if (pageType == 3)
        {
            if (headerLogicalByteCount != 5 || optionalFieldCount != 1)
            {
                throw new FtpTransferException("An access-controlled page header must have length five.", null);
            }
        }
        else
        {
            throw new FtpTransferException("The page type is not defined by RFC 959.", null);
        }
    }

    internal void ValidateLogicalUnits(byte[] values, bool requireOne)
    {
        if (values.Length % StorageByteCount != 0 || (requireOne && values.Length != StorageByteCount))
        {
            throw new FtpConfigurationException("Page values must contain complete logical-byte storage units.", null);
        }

        if (UnusedHighBits == 0)
        {
            return;
        }

        int unusedMask = 0xFF << (8 - UnusedHighBits);
        int index = 0;
        while (index < values.Length)
        {
            if ((values[index] & unusedMask) != 0)
            {
                throw new FtpTransferException("A page logical byte has non-zero high padding bits.", null);
            }

            index += StorageByteCount;
        }
    }

    internal void ValidateLengthValue(long value, string valueName)
    {
        if (LogicalByteSize < 63)
        {
            long limit = 1L << LogicalByteSize;
            if (value >= limit)
            {
                throw new FtpTransferException($"The {valueName} does not fit the selected logical-byte size.", null);
            }
        }
    }

    internal int GetTotalStorageBytes(int headerLogicalByteCount, int dataLogicalByteCount)
    {
        if (headerLogicalByteCount > int.MaxValue - dataLogicalByteCount)
        {
            throw new FtpTransferException("The serialized page exceeds supported buffer capacity.", null);
        }

        int totalLogicalByteCount = headerLogicalByteCount + dataLogicalByteCount;
        if (totalLogicalByteCount > int.MaxValue / StorageByteCount)
        {
            throw new FtpTransferException("The serialized page exceeds supported buffer capacity.", null);
        }

        return totalLogicalByteCount * StorageByteCount;
    }

    internal void WriteInteger(long value, byte[] destination, int logicalByteIndex)
    {
        int destinationIndex = (logicalByteIndex + 1) * StorageByteCount - 1;
        long remaining = value;
        int bytesWritten = 0;
        while (bytesWritten < StorageByteCount && remaining != 0)
        {
            destination[destinationIndex - bytesWritten] = (byte)(remaining & 0xFF);
            remaining >>= 8;
            bytesWritten++;
        }
    }

    internal long ReadInteger(byte[] source, int logicalByteIndex)
    {
        int sourceIndex = logicalByteIndex * StorageByteCount;
        int ignoredHighBytes = StorageByteCount - 8;
        int index = 0;
        while (index < ignoredHighBytes)
        {
            if (source[sourceIndex + index] != 0)
            {
                throw new FtpTransferException("A page header integer exceeds Int64 capacity.", null);
            }

            index++;
        }

        long value = 0;
        while (index < StorageByteCount)
        {
            if (value > (long.MaxValue - source[sourceIndex + index]) / 256)
            {
                throw new FtpTransferException("A page header integer exceeds Int64 capacity.", null);
            }

            value = value * 256 + source[sourceIndex + index];
            index++;
        }

        return value;
    }

    internal bool IndexesEqual(byte[] left, byte[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        int index = 0;
        while (index < left.Length)
        {
            if (left[index] != right[index])
            {
                return false;
            }

            index++;
        }

        return true;
    }

    internal byte[] CopyBytes(byte[] source)
    {
        byte[] copy = new byte[source.Length];
        if (source.Length != 0)
        {
            System.Array.Copy(source, 0, copy, 0, source.Length);
        }

        return copy;
    }
}
