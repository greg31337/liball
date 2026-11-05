using System;
using System.Collections.Generic;
using System.Text;

namespace xlsx;

internal class XlsxCompoundFileResult
{
    internal bool IsValid;
    internal XlsxValidationResult Validation;
    internal Dictionary<string, byte[]> Streams;

    internal XlsxCompoundFileResult()
    {
        IsValid = true;
        Validation = new XlsxValidationResult();
        Streams = new Dictionary<string, byte[]>(StringComparer.Ordinal);
    }

    internal void AddDiagnostic(XlsxDiagnostic diagnostic)
    {
        Validation.AddDiagnostic(diagnostic);
        IsValid = Validation.IsValid;
    }
}

internal class XlsxCompoundDirectoryEntry
{
    internal string Name;
    internal byte Type;
    internal int StartSector;
    internal long Size;

    internal XlsxCompoundDirectoryEntry()
    {
        Name = "";
        Type = 0;
        StartSector = -2;
        Size = 0;
    }
}

internal class XlsxCompoundStreamPlan
{
    internal string Name;
    internal byte[] Bytes;
    internal bool IsMini;
    internal int Start;
    internal int SectorCount;

    internal XlsxCompoundStreamPlan(string name, byte[] bytes)
    {
        Name = name;
        Bytes = bytes;
        IsMini = bytes.Length < 4096;
        Start = -2;
        SectorCount = 0;
    }
}

internal static class XlsxCompoundFile
{
    internal const int FreeSector = -1;
    internal const int EndOfChain = -2;
    internal const int FatSector = -3;
    internal const int DifatSector = -4;

    internal static XlsxCompoundFileResult Read(byte[] bytes)
    {
        XlsxCompoundFileResult result = new XlsxCompoundFileResult();
        if (bytes == null || bytes.Length < 512 || !XlsxEncryptionDetector.IsCompoundFileHeader(bytes))
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "cfb.invalidHeader", "Encrypted package must be a valid compound file."));
            return result;
        }

        int major = ReadUInt16(bytes, 26);
        int sectorShift = ReadUInt16(bytes, 30);
        int miniShift = ReadUInt16(bytes, 32);
        int sectorSize = 1 << sectorShift;
        int miniSectorSize = 1 << miniShift;
        if ((major != 3 && major != 4) || (sectorSize != 512 && sectorSize != 4096) || miniSectorSize != 64)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "cfb.unsupportedVersion", "Compound-file sector settings are not supported."));
            return result;
        }

        if ((bytes.Length - 512) % sectorSize != 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "cfb.truncated", "Compound-file bytes do not end on a sector boundary."));
            return result;
        }

        int totalSectors = (bytes.Length - 512) / sectorSize;
        int fatCount = ReadInt32(bytes, 44);
        int firstDirectory = ReadInt32(bytes, 48);
        int cutoff = ReadInt32(bytes, 56);
        int firstMiniFat = ReadInt32(bytes, 60);
        int miniFatCount = ReadInt32(bytes, 64);
        int firstDifat = ReadInt32(bytes, 68);
        int difatCount = ReadInt32(bytes, 72);
        if (fatCount < 0 || fatCount > totalSectors || miniFatCount < 0 || difatCount < 0)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "cfb.invalidAllocationTable", "Compound-file allocation-table counts are invalid."));
            return result;
        }

        List<int> fatSectors = new List<int>();
        int difatIndex = 0;
        while (difatIndex < 109 && fatSectors.Count < fatCount)
        {
            int sector = ReadInt32(bytes, 76 + difatIndex * 4);
            if (sector >= 0)
            {
                fatSectors.Add(sector);
            }
            difatIndex = difatIndex + 1;
        }

        int currentDifat = firstDifat;
        int visitedDifat = 0;
        while (fatSectors.Count < fatCount && visitedDifat < difatCount)
        {
            if (!IsSector(currentDifat, totalSectors))
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "cfb.invalidDifat", "Compound-file DIFAT chain is invalid."));
                return result;
            }

            int offset = SectorOffset(currentDifat, sectorSize);
            int entries = sectorSize / 4 - 1;
            int index = 0;
            while (index < entries && fatSectors.Count < fatCount)
            {
                int sector = ReadInt32(bytes, offset + index * 4);
                if (sector >= 0)
                {
                    fatSectors.Add(sector);
                }
                index = index + 1;
            }
            currentDifat = ReadInt32(bytes, offset + entries * 4);
            visitedDifat = visitedDifat + 1;
        }

        if (fatSectors.Count != fatCount)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "cfb.incompleteDifat", "Compound-file FAT sector references are incomplete."));
            return result;
        }

        int fatEntriesPerSector = sectorSize / 4;
        int[] fat = new int[fatCount * fatEntriesPerSector];
        int fatSectorIndex = 0;
        while (fatSectorIndex < fatSectors.Count)
        {
            int fatSectorId = fatSectors[fatSectorIndex];
            if (!IsSector(fatSectorId, totalSectors))
            {
                result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "cfb.invalidFatSector", "Compound-file FAT sector reference is invalid."));
                return result;
            }
            int sourceOffset = SectorOffset(fatSectorId, sectorSize);
            int entry = 0;
            while (entry < fatEntriesPerSector)
            {
                fat[fatSectorIndex * fatEntriesPerSector + entry] = ReadInt32(bytes, sourceOffset + entry * 4);
                entry = entry + 1;
            }
            fatSectorIndex = fatSectorIndex + 1;
        }

        byte[] directoryBytes = ReadChain(bytes, firstDirectory, fat, sectorSize, -1, totalSectors);
        if (directoryBytes.Length < 128)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "cfb.directoryMissing", "Compound-file directory stream is missing or invalid."));
            return result;
        }

        List<XlsxCompoundDirectoryEntry> entriesList = ReadDirectoryEntries(directoryBytes, major);
        XlsxCompoundDirectoryEntry? root = null;
        int directoryIndex = 0;
        while (directoryIndex < entriesList.Count)
        {
            XlsxCompoundDirectoryEntry candidate = entriesList[directoryIndex];
            if (candidate.Type == 5)
            {
                root = candidate;
            }
            directoryIndex = directoryIndex + 1;
        }
        if (root == null)
        {
            result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "cfb.rootMissing", "Compound-file root directory entry is missing."));
            return result;
        }

        byte[] miniStream = new byte[0];
        if (root.Size > 0)
        {
            miniStream = ReadChain(bytes, root.StartSector, fat, sectorSize, root.Size, totalSectors);
        }
        byte[] miniFatBytes = new byte[0];
        if (miniFatCount > 0)
        {
            miniFatBytes = ReadChain(bytes, firstMiniFat, fat, sectorSize, (long)miniFatCount * sectorSize, totalSectors);
        }
        int[] miniFat = ReadAllocationEntries(miniFatBytes);

        directoryIndex = 0;
        while (directoryIndex < entriesList.Count)
        {
            XlsxCompoundDirectoryEntry entryItem = entriesList[directoryIndex];
            if (entryItem.Type == 2 && entryItem.Name.Length > 0)
            {
                byte[] streamBytes;
                if (entryItem.Size < cutoff)
                {
                    streamBytes = ReadMiniChain(miniStream, entryItem.StartSector, miniFat, miniSectorSize, entryItem.Size);
                }
                else
                {
                    streamBytes = ReadChain(bytes, entryItem.StartSector, fat, sectorSize, entryItem.Size, totalSectors);
                }
                if (streamBytes.Length != entryItem.Size)
                {
                    result.AddDiagnostic(new XlsxDiagnostic(XlsxDiagnosticSeverity.Error, "cfb.streamTruncated", "Compound-file stream " + entryItem.Name + " is truncated."));
                }
                else
                {
                    result.Streams[entryItem.Name] = streamBytes;
                }
            }
            directoryIndex = directoryIndex + 1;
        }

        result.IsValid = result.Validation.IsValid;
        return result;
    }

    internal static byte[] Write(Dictionary<string, byte[]> streams)
    {
        int sectorSize = 512;
        int miniSectorSize = 64;
        List<XlsxCompoundStreamPlan> plans = new List<XlsxCompoundStreamPlan>();
        foreach (KeyValuePair<string, byte[]> pair in streams)
        {
            plans.Add(new XlsxCompoundStreamPlan(pair.Key, pair.Value));
        }
        bool officeEncryptionLayout = streams.ContainsKey("EncryptionInfo") && streams.ContainsKey("EncryptedPackage");
        if (officeEncryptionLayout)
        {
            plans.Add(new XlsxCompoundStreamPlan("Version", BuildVersionStream()));
            plans.Add(new XlsxCompoundStreamPlan("DataSpaceMap", BuildDataSpaceMapStream()));
            plans.Add(new XlsxCompoundStreamPlan("StrongEncryptionDataSpace", BuildDataSpaceDefinitionStream()));
            plans.Add(new XlsxCompoundStreamPlan("\x06Primary", BuildPrimaryStream()));
        }
        plans.Sort(ComparePlans);

        int nextMiniSector = 0;
        int planIndex = 0;
        while (planIndex < plans.Count)
        {
            XlsxCompoundStreamPlan plan = plans[planIndex];
            if (plan.IsMini)
            {
                plan.Start = nextMiniSector;
                plan.SectorCount = DivideRoundUp(plan.Bytes.Length, miniSectorSize);
                nextMiniSector = nextMiniSector + plan.SectorCount;
            }
            planIndex = planIndex + 1;
        }
        int miniStreamLength = nextMiniSector * miniSectorSize;
        byte[] miniStream = new byte[miniStreamLength];
        int[] miniFat = new int[nextMiniSector];
        int miniFill = 0;
        while (miniFill < miniFat.Length)
        {
            miniFat[miniFill] = FreeSector;
            miniFill = miniFill + 1;
        }
        planIndex = 0;
        while (planIndex < plans.Count)
        {
            XlsxCompoundStreamPlan plan = plans[planIndex];
            if (plan.IsMini && plan.SectorCount > 0)
            {
                Array.Copy(plan.Bytes, 0, miniStream, plan.Start * miniSectorSize, plan.Bytes.Length);
                SetChain(miniFat, plan.Start, plan.SectorCount);
            }
            planIndex = planIndex + 1;
        }

        int normalDataSectors = 0;
        planIndex = 0;
        while (planIndex < plans.Count)
        {
            if (!plans[planIndex].IsMini)
            {
                plans[planIndex].SectorCount = DivideRoundUp(plans[planIndex].Bytes.Length, sectorSize);
                normalDataSectors = normalDataSectors + plans[planIndex].SectorCount;
            }
            planIndex = planIndex + 1;
        }
        int miniStreamSectors = DivideRoundUp(miniStream.Length, sectorSize);
        int miniFatSectors = DivideRoundUp(miniFat.Length * 4, sectorSize);
        int directoryEntryCount = officeEncryptionLayout ? 11 : plans.Count + 1;
        int directorySectors = DivideRoundUp(directoryEntryCount * 128, sectorSize);
        if (directorySectors < 1)
        {
            directorySectors = 1;
        }
        int nonFatSectors = normalDataSectors + miniStreamSectors + miniFatSectors + directorySectors;
        int fatSectors = 1;
        int difatSectors = 0;
        bool allocationStable = false;
        while (!allocationStable)
        {
            int requiredDifat = fatSectors > 109 ? DivideRoundUp(fatSectors - 109, sectorSize / 4 - 1) : 0;
            int requiredFat = DivideRoundUp(nonFatSectors + fatSectors + requiredDifat, sectorSize / 4);
            allocationStable = requiredFat == fatSectors && requiredDifat == difatSectors;
            fatSectors = requiredFat;
            difatSectors = requiredDifat;
        }

        int totalSectors = nonFatSectors + difatSectors + fatSectors;
        byte[] output = new byte[512 + totalSectors * sectorSize];
        WriteHeader(output, fatSectors);
        int[] fat = new int[totalSectors];
        int fatIndex = 0;
        while (fatIndex < fat.Length)
        {
            fat[fatIndex] = FreeSector;
            fatIndex = fatIndex + 1;
        }

        int nextSector = 0;
        planIndex = 0;
        while (planIndex < plans.Count)
        {
            XlsxCompoundStreamPlan plan = plans[planIndex];
            if (!plan.IsMini)
            {
                plan.Start = nextSector;
                CopyToSectors(output, plan.Bytes, nextSector, sectorSize);
                SetChain(fat, nextSector, plan.SectorCount);
                nextSector = nextSector + plan.SectorCount;
            }
            planIndex = planIndex + 1;
        }

        int miniStreamStart = EndOfChain;
        if (miniStreamSectors > 0)
        {
            miniStreamStart = nextSector;
            CopyToSectors(output, miniStream, nextSector, sectorSize);
            SetChain(fat, nextSector, miniStreamSectors);
            nextSector = nextSector + miniStreamSectors;
        }
        int miniFatStart = EndOfChain;
        if (miniFatSectors > 0)
        {
            miniFatStart = nextSector;
            byte[] miniFatData = new byte[miniFatSectors * sectorSize];
            FillInt32(miniFatData, FreeSector);
            int miniIndex = 0;
            while (miniIndex < miniFat.Length)
            {
                WriteInt32(miniFatData, miniIndex * 4, miniFat[miniIndex]);
                miniIndex = miniIndex + 1;
            }
            CopyToSectors(output, miniFatData, nextSector, sectorSize);
            SetChain(fat, nextSector, miniFatSectors);
            nextSector = nextSector + miniFatSectors;
        }
        int directoryStart = nextSector;
        byte[] directory;
        if (officeEncryptionLayout)
        {
            directory = BuildOfficeEncryptionDirectory(plans, directorySectors * sectorSize, miniStreamStart, miniStream.Length);
        }
        else
        {
            directory = BuildDirectory(plans, directorySectors * sectorSize, miniStreamStart, miniStream.Length);
        }
        CopyToSectors(output, directory, nextSector, sectorSize);
        SetChain(fat, nextSector, directorySectors);
        nextSector = nextSector + directorySectors;

        int firstDifatSector = EndOfChain;
        if (difatSectors > 0)
        {
            firstDifatSector = nextSector;
            int difatMarker = 0;
            while (difatMarker < difatSectors)
            {
                fat[nextSector + difatMarker] = DifatSector;
                difatMarker = difatMarker + 1;
            }
            nextSector = nextSector + difatSectors;
        }
        int firstFatSector = nextSector;
        int currentFat = 0;
        while (currentFat < fatSectors)
        {
            fat[nextSector + currentFat] = FatSector;
            if (currentFat < 109)
            {
                WriteInt32(output, 76 + currentFat * 4, nextSector + currentFat);
            }
            currentFat = currentFat + 1;
        }
        WriteInt32(output, 48, directoryStart);
        WriteInt32(output, 60, miniFatStart);
        WriteInt32(output, 64, miniFatSectors);
        WriteInt32(output, 68, firstDifatSector);
        WriteInt32(output, 72, difatSectors);

        if (difatSectors > 0)
        {
            byte[] difatBytes = new byte[difatSectors * sectorSize];
            FillInt32(difatBytes, FreeSector);
            int difatIndex = 0;
            int fatReference = 109;
            int referencesPerDifat = sectorSize / 4 - 1;
            while (difatIndex < difatSectors)
            {
                int referenceIndex = 0;
                while (referenceIndex < referencesPerDifat && fatReference < fatSectors)
                {
                    WriteInt32(difatBytes, difatIndex * sectorSize + referenceIndex * 4, firstFatSector + fatReference);
                    referenceIndex = referenceIndex + 1;
                    fatReference = fatReference + 1;
                }
                int nextDifat = difatIndex + 1 < difatSectors ? firstDifatSector + difatIndex + 1 : EndOfChain;
                WriteInt32(difatBytes, difatIndex * sectorSize + referencesPerDifat * 4, nextDifat);
                difatIndex = difatIndex + 1;
            }
            CopyToSectors(output, difatBytes, firstDifatSector, sectorSize);
        }

        byte[] fatBytes = new byte[fatSectors * sectorSize];
        FillInt32(fatBytes, FreeSector);
        fatIndex = 0;
        while (fatIndex < fat.Length)
        {
            WriteInt32(fatBytes, fatIndex * 4, fat[fatIndex]);
            fatIndex = fatIndex + 1;
        }
        CopyToSectors(output, fatBytes, firstFatSector, sectorSize);
        return output;
    }

    internal static byte[] GetStream(XlsxCompoundFileResult compound, string name)
    {
        byte[]? bytes = null;
        if (compound.Streams.TryGetValue(name, out bytes))
        {
            return bytes;
        }
        return new byte[0];
    }

    internal static int ReadInt32(byte[] bytes, int offset)
    {
        uint value = ReadUInt32(bytes, offset);
        if (value <= int.MaxValue)
        {
            return (int)value;
        }
        return -((int)(uint.MaxValue - value)) - 1;
    }

    internal static uint ReadUInt32(byte[] bytes, int offset)
    {
        return (uint)bytes[offset] | ((uint)bytes[offset + 1] << 8) | ((uint)bytes[offset + 2] << 16) | ((uint)bytes[offset + 3] << 24);
    }

    internal static long ReadInt64(byte[] bytes, int offset)
    {
        uint low = ReadUInt32(bytes, offset);
        uint high = ReadUInt32(bytes, offset + 4);
        return (long)(((ulong)high << 32) | low);
    }

    internal static void WriteInt32(byte[] bytes, int offset, int value)
    {
        bytes[offset] = (byte)(value & 255);
        bytes[offset + 1] = (byte)((value >> 8) & 255);
        bytes[offset + 2] = (byte)((value >> 16) & 255);
        bytes[offset + 3] = (byte)((value >> 24) & 255);
    }

    internal static void WriteInt64(byte[] bytes, int offset, long value)
    {
        int index = 0;
        while (index < 8)
        {
            bytes[offset + index] = (byte)((value >> (index * 8)) & 255);
            index = index + 1;
        }
    }

    internal static int ReadUInt16(byte[] bytes, int offset)
    {
        return bytes[offset] | (bytes[offset + 1] << 8);
    }

    internal static bool IsSector(int sector, int totalSectors)
    {
        return sector >= 0 && sector < totalSectors;
    }

    internal static int SectorOffset(int sector, int sectorSize)
    {
        return 512 + sector * sectorSize;
    }

    internal static byte[] ReadChain(byte[] source, int start, int[] allocation, int sectorSize, long requestedSize, int totalSectors)
    {
        if (start == EndOfChain || requestedSize == 0)
        {
            return new byte[0];
        }
        if (requestedSize > int.MaxValue)
        {
            return new byte[0];
        }
        if (requestedSize >= 0)
        {
            byte[] knownOutput = new byte[(int)requestedSize];
            bool[] knownVisited = new bool[totalSectors];
            int knownCurrent = start;
            int knownOffset = 0;
            while (IsSector(knownCurrent, totalSectors) && knownCurrent < allocation.Length && !knownVisited[knownCurrent] && knownOffset < knownOutput.Length)
            {
                knownVisited[knownCurrent] = true;
                int count = knownOutput.Length - knownOffset;
                if (count > sectorSize)
                {
                    count = sectorSize;
                }
                Array.Copy(source, SectorOffset(knownCurrent, sectorSize), knownOutput, knownOffset, count);
                knownOffset = knownOffset + count;
                knownCurrent = allocation[knownCurrent];
            }
            if (knownOffset == knownOutput.Length)
            {
                return knownOutput;
            }
            byte[] truncated = new byte[knownOffset];
            Array.Copy(knownOutput, truncated, knownOffset);
            return truncated;
        }
        List<byte> output = new List<byte>();
        bool[] visited = new bool[totalSectors];
        int current = start;
        while (IsSector(current, totalSectors) && current < allocation.Length && !visited[current])
        {
            visited[current] = true;
            int offset = SectorOffset(current, sectorSize);
            int count = sectorSize;
            if (requestedSize >= 0 && output.Count + count > requestedSize)
            {
                count = (int)(requestedSize - output.Count);
            }
            int index = 0;
            while (index < count)
            {
                output.Add(source[offset + index]);
                index = index + 1;
            }
            if (requestedSize >= 0 && output.Count >= requestedSize)
            {
                break;
            }
            current = allocation[current];
        }
        return output.ToArray();
    }

    internal static byte[] ReadMiniChain(byte[] source, int start, int[] allocation, int miniSectorSize, long requestedSize)
    {
        if (start == EndOfChain || requestedSize == 0)
        {
            return new byte[0];
        }
        if (requestedSize < 0 || requestedSize > int.MaxValue)
        {
            return new byte[0];
        }
        byte[] output = new byte[(int)requestedSize];
        bool[] visited = new bool[allocation.Length];
        int current = start;
        int outputOffset = 0;
        while (current >= 0 && current < allocation.Length && !visited[current])
        {
            visited[current] = true;
            int offset = current * miniSectorSize;
            if (offset + miniSectorSize > source.Length)
            {
                break;
            }
            int count = miniSectorSize;
            if (count > requestedSize - outputOffset)
            {
                count = (int)(requestedSize - outputOffset);
            }
            Array.Copy(source, offset, output, outputOffset, count);
            outputOffset = outputOffset + count;
            if (outputOffset >= requestedSize)
            {
                break;
            }
            current = allocation[current];
        }
        if (outputOffset == output.Length)
        {
            return output;
        }
        byte[] truncated = new byte[outputOffset];
        Array.Copy(output, truncated, outputOffset);
        return truncated;
    }

    internal static List<XlsxCompoundDirectoryEntry> ReadDirectoryEntries(byte[] bytes, int major)
    {
        List<XlsxCompoundDirectoryEntry> result = new List<XlsxCompoundDirectoryEntry>();
        int offset = 0;
        while (offset + 128 <= bytes.Length)
        {
            XlsxCompoundDirectoryEntry entry = new XlsxCompoundDirectoryEntry();
            int nameLength = ReadUInt16(bytes, offset + 64);
            if (nameLength >= 2 && nameLength <= 64)
            {
                entry.Name = Encoding.Unicode.GetString(bytes, offset, nameLength - 2);
            }
            entry.Type = bytes[offset + 66];
            entry.StartSector = ReadInt32(bytes, offset + 116);
            entry.Size = ReadInt64(bytes, offset + 120);
            if (major == 3)
            {
                entry.Size = entry.Size & 0xFFFFFFFFL;
            }
            result.Add(entry);
            offset = offset + 128;
        }
        return result;
    }

    internal static int[] ReadAllocationEntries(byte[] bytes)
    {
        int[] result = new int[bytes.Length / 4];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = ReadInt32(bytes, index * 4);
            index = index + 1;
        }
        return result;
    }

    internal static int ComparePlans(XlsxCompoundStreamPlan left, XlsxCompoundStreamPlan right)
    {
        return StringComparer.Ordinal.Compare(left.Name, right.Name);
    }

    internal static int DivideRoundUp(int value, int divisor)
    {
        if (value == 0)
        {
            return 0;
        }
        return (value + divisor - 1) / divisor;
    }

    internal static void SetChain(int[] allocation, int start, int count)
    {
        int index = 0;
        while (index < count)
        {
            int current = start + index;
            if (index + 1 == count)
            {
                allocation[current] = EndOfChain;
            }
            else
            {
                allocation[current] = current + 1;
            }
            index = index + 1;
        }
    }

    internal static void WriteHeader(byte[] output, int fatSectors)
    {
        byte[] signature = new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 };
        Array.Copy(signature, output, signature.Length);
        output[24] = 0x3E;
        output[26] = 0x03;
        output[28] = 0xFE;
        output[29] = 0xFF;
        output[30] = 0x09;
        output[32] = 0x06;
        WriteInt32(output, 44, fatSectors);
        WriteInt32(output, 48, EndOfChain);
        WriteInt32(output, 56, 4096);
        WriteInt32(output, 60, EndOfChain);
        WriteInt32(output, 68, EndOfChain);
        FillInt32Range(output, 76, 109, FreeSector);
    }

    internal static void CopyToSectors(byte[] output, byte[] input, int sector, int sectorSize)
    {
        Array.Copy(input, 0, output, SectorOffset(sector, sectorSize), input.Length);
    }

    internal static byte[] BuildDirectory(List<XlsxCompoundStreamPlan> plans, int length, int miniStreamStart, int miniStreamLength)
    {
        byte[] directory = new byte[length];
        WriteDirectoryEntry(directory, 0, "Root Entry", 5, miniStreamStart, miniStreamLength, -1, -1, plans.Count > 0 ? 1 : -1);
        int index = 0;
        while (index < plans.Count)
        {
            XlsxCompoundStreamPlan plan = plans[index];
            int right = index + 1 < plans.Count ? index + 2 : -1;
            WriteDirectoryEntry(directory, (index + 1) * 128, plan.Name, 2, plan.Start, plan.Bytes.Length, -1, right, -1);
            index = index + 1;
        }
        return directory;
    }

    internal static byte[] BuildOfficeEncryptionDirectory(List<XlsxCompoundStreamPlan> plans, int length, int miniStreamStart, int miniStreamLength)
    {
        byte[] directory = new byte[length];
        XlsxCompoundStreamPlan encryptedPackage = FindPlan(plans, "EncryptedPackage");
        XlsxCompoundStreamPlan version = FindPlan(plans, "Version");
        XlsxCompoundStreamPlan dataSpaceMap = FindPlan(plans, "DataSpaceMap");
        XlsxCompoundStreamPlan dataSpaceDefinition = FindPlan(plans, "StrongEncryptionDataSpace");
        XlsxCompoundStreamPlan primary = FindPlan(plans, "\x06Primary");
        XlsxCompoundStreamPlan encryptionInfo = FindPlan(plans, "EncryptionInfo");
        WriteDirectoryEntry(directory, 0, "Root Entry", 5, miniStreamStart, miniStreamLength, -1, -1, 10);
        WriteDirectoryEntry(directory, 128, encryptedPackage.Name, 2, encryptedPackage.Start, encryptedPackage.Bytes.Length, -1, -1, -1);
        WriteDirectoryEntry(directory, 256, "\x06DataSpaces", 1, EndOfChain, 0, -1, -1, 4);
        WriteDirectoryEntry(directory, 384, version.Name, 2, version.Start, version.Bytes.Length, -1, -1, -1);
        WriteDirectoryEntry(directory, 512, dataSpaceMap.Name, 2, dataSpaceMap.Start, dataSpaceMap.Bytes.Length, 3, 5, -1);
        WriteDirectoryEntry(directory, 640, "DataSpaceInfo", 1, EndOfChain, 0, -1, 7, 6);
        WriteDirectoryEntry(directory, 768, dataSpaceDefinition.Name, 2, dataSpaceDefinition.Start, dataSpaceDefinition.Bytes.Length, -1, -1, -1);
        WriteDirectoryEntry(directory, 896, "TransformInfo", 1, EndOfChain, 0, -1, -1, 8);
        WriteDirectoryEntry(directory, 1024, "StrongEncryptionTransform", 1, EndOfChain, 0, -1, -1, 9);
        WriteDirectoryEntry(directory, 1152, primary.Name, 2, primary.Start, primary.Bytes.Length, -1, -1, -1);
        WriteDirectoryEntry(directory, 1280, encryptionInfo.Name, 2, encryptionInfo.Start, encryptionInfo.Bytes.Length, 2, 1, -1);
        directory[128 + 67] = 0;
        directory[256 + 67] = 0;
        directory[896 + 67] = 0;
        return directory;
    }

    internal static XlsxCompoundStreamPlan FindPlan(List<XlsxCompoundStreamPlan> plans, string name)
    {
        int index = 0;
        while (index < plans.Count)
        {
            if (plans[index].Name == name)
            {
                return plans[index];
            }
            index = index + 1;
        }
        throw new XlsxException("Required compound-file stream plan is missing.", null);
    }

    internal static byte[] BuildVersionStream()
    {
        List<byte> bytes = new List<byte>();
        AppendUInt16(bytes, 0x003C);
        AppendUInt16(bytes, 0);
        AppendUnicode(bytes, "Microsoft.Container.DataSpaces", false);
        AppendInt32(bytes, 1);
        AppendInt32(bytes, 1);
        AppendInt32(bytes, 1);
        return bytes.ToArray();
    }

    internal static byte[] BuildDataSpaceMapStream()
    {
        List<byte> bytes = new List<byte>();
        AppendInt32(bytes, 8);
        AppendInt32(bytes, 1);
        AppendInt32(bytes, 104);
        AppendInt32(bytes, 1);
        AppendInt32(bytes, 0);
        AppendInt32(bytes, "EncryptedPackage".Length * 2);
        AppendUnicode(bytes, "EncryptedPackage", false);
        AppendInt32(bytes, "StrongEncryptionDataSpace".Length * 2);
        AppendUnicode(bytes, "StrongEncryptionDataSpace", true);
        return bytes.ToArray();
    }

    internal static byte[] BuildDataSpaceDefinitionStream()
    {
        List<byte> bytes = new List<byte>();
        AppendInt32(bytes, 8);
        AppendInt32(bytes, 1);
        AppendInt32(bytes, "StrongEncryptionTransform".Length * 2);
        AppendUnicode(bytes, "StrongEncryptionTransform", true);
        return bytes.ToArray();
    }

    internal static byte[] BuildPrimaryStream()
    {
        string identifier = "{FF9A3F03-56EF-4613-BDD5-5A41C1D07246}";
        string name = "Microsoft.Container.EncryptionTransform";
        List<byte> bytes = new List<byte>();
        AppendInt32(bytes, identifier.Length * 2 + 12);
        AppendInt32(bytes, 1);
        AppendInt32(bytes, identifier.Length * 2);
        AppendUnicode(bytes, identifier, false);
        AppendInt32(bytes, name.Length * 2);
        AppendUnicode(bytes, name, true);
        AppendInt32(bytes, 1);
        AppendInt32(bytes, 1);
        AppendInt32(bytes, 1);
        AppendInt32(bytes, 0);
        AppendInt32(bytes, 0);
        AppendInt32(bytes, 0);
        AppendInt32(bytes, 4);
        return bytes.ToArray();
    }

    internal static void AppendUInt16(List<byte> output, int value)
    {
        output.Add((byte)(value & 255));
        output.Add((byte)((value >> 8) & 255));
    }

    internal static void AppendInt32(List<byte> output, int value)
    {
        output.Add((byte)(value & 255));
        output.Add((byte)((value >> 8) & 255));
        output.Add((byte)((value >> 16) & 255));
        output.Add((byte)((value >> 24) & 255));
    }

    internal static void AppendUnicode(List<byte> output, string value, bool terminate)
    {
        byte[] bytes = Encoding.Unicode.GetBytes(terminate ? value + "\0" : value);
        int index = 0;
        while (index < bytes.Length)
        {
            output.Add(bytes[index]);
            index = index + 1;
        }
    }

    internal static void WriteDirectoryEntry(byte[] bytes, int offset, string name, byte type, int start, long size, int left, int right, int child)
    {
        string storedName = name;
        if (storedName.Length > 31)
        {
            storedName = storedName.Substring(0, 31);
        }
        byte[] nameBytes = Encoding.Unicode.GetBytes(storedName + "\0");
        Array.Copy(nameBytes, 0, bytes, offset, nameBytes.Length);
        bytes[offset + 64] = (byte)nameBytes.Length;
        bytes[offset + 65] = (byte)(nameBytes.Length >> 8);
        bytes[offset + 66] = type;
        bytes[offset + 67] = 1;
        WriteInt32(bytes, offset + 68, left);
        WriteInt32(bytes, offset + 72, right);
        WriteInt32(bytes, offset + 76, child);
        WriteInt32(bytes, offset + 116, start);
        WriteInt64(bytes, offset + 120, size);
    }

    internal static void FillInt32(byte[] bytes, int value)
    {
        FillInt32Range(bytes, 0, bytes.Length / 4, value);
    }

    internal static void FillInt32Range(byte[] bytes, int offset, int count, int value)
    {
        int index = 0;
        while (index < count)
        {
            WriteInt32(bytes, offset + index * 4, value);
            index = index + 1;
        }
    }
}
