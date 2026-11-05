using System;

namespace raw;

internal class TiffProbeEvidence
{
    internal bool HeaderValid;
    internal bool Cr2;
    internal bool BigTiff;
    internal RawByteOrder ByteOrder;
    internal ulong FirstIfdOffset;
    internal bool NikonMake;
    internal bool CameraModel;
    internal bool NikonMakerNote;
    internal bool NikonCompression;
    internal bool RawStructure;
    internal bool Dng;
}

internal class TiffProbeReader
{
    internal byte[] Bytes;
    internal RawByteOrder ByteOrder;
    internal bool BigTiff;

    internal TiffProbeReader(byte[] bytes, RawByteOrder byteOrder, bool bigTiff)
    {
        Bytes = bytes;
        ByteOrder = byteOrder;
        BigTiff = bigTiff;
    }

    internal bool CanRead(ulong offset, ulong length)
    {
        return offset <= (ulong)Bytes.Length && length <= (ulong)Bytes.Length - offset;
    }

    internal ushort ReadUInt16(ulong offset)
    {
        return EndianReader.ReadUInt16(Bytes.AsSpan((int)offset, 2), ByteOrder);
    }

    internal uint ReadUInt32(ulong offset)
    {
        return EndianReader.ReadUInt32(Bytes.AsSpan((int)offset, 4), ByteOrder);
    }

    internal ulong ReadUInt64(ulong offset)
    {
        return EndianReader.ReadUInt64(Bytes.AsSpan((int)offset, 8), ByteOrder);
    }

    internal ulong ReadOffset(ulong offset)
    {
        if (BigTiff)
        {
            return ReadUInt64(offset);
        }
        return ReadUInt32(offset);
    }
}

internal static class FormatProbe
{
    internal const int PrefixLimit = 1024 * 1024;

    internal static RawProbe Probe(System.ReadOnlyMemory<byte> source, string? fileName, RawFormat expectedFormat, bool genericDescriptorProvided, RawLimits? limits, System.Threading.CancellationToken cancellationToken)
    {
        BoundedInput input = new BoundedInput(source, 0, source.Length, limits);
        return Probe(input, fileName, expectedFormat, genericDescriptorProvided, cancellationToken);
    }

    internal static RawProbe Probe(System.IO.Stream source, long offset, long length, string fileName, RawFormat expectedFormat, bool genericDescriptorProvided, RawLimits? limits, System.Threading.CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BoundedInput input = BoundedInput.CreateStream(source, offset, length, limits);
        return Probe(input, fileName, expectedFormat, genericDescriptorProvided, cancellationToken);
    }

    internal static RawProbe Probe(BoundedInput? input, string? fileName, RawFormat expectedFormat, bool genericDescriptorProvided, System.Threading.CancellationToken cancellationToken)
    {
        if (input == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "A bounded input is required for format probing.", -1);
        }
        if (!DefinedFormat(expectedFormat))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "The expected raw format is not defined.", -1);
        }
        cancellationToken.ThrowIfCancellationRequested();
        byte[] prefix = ReadPrefix(input, cancellationToken);
        TiffProbeEvidence evidence = InspectTiff(prefix, cancellationToken);
        System.Collections.Generic.List<RawDiagnostic> diagnostics = new System.Collections.Generic.List<RawDiagnostic>();
        RawProbe result = Classify(evidence, genericDescriptorProvided, diagnostics);
        AddHintDiagnostic(result, expectedFormat, "caller format", diagnostics);
        RawFormat fileFormat = FormatFromFileName(fileName);
        AddHintDiagnostic(result, fileFormat, "filename extension", diagnostics);
        result.Diagnostics = diagnostics.ToArray();
        return result;
    }

    internal static byte[] ReadPrefix(BoundedInput input, System.Threading.CancellationToken cancellationToken)
    {
        long length = input.GetLength();
        if (length > PrefixLimit)
        {
            length = PrefixLimit;
        }
        CheckedMath.AllocationSize(length, 1, input.Limits);
        byte[] bytes = new byte[CheckedMath.ToInt32(length)];
        cancellationToken.ThrowIfCancellationRequested();
        if (input.UsesMemory || input.Seekable)
        {
            input.ReadAt(0, bytes);
            if (!input.UsesMemory)
            {
                (input.Stream ?? throw new System.InvalidOperationException("A required value is unavailable.")).Position = input.GetAbsolutePosition();
            }
        }
        else
        {
            input.ReadExactly(bytes);
        }
        return bytes;
    }

    internal static TiffProbeEvidence InspectTiff(byte[] bytes, System.Threading.CancellationToken cancellationToken)
    {
        TiffProbeEvidence evidence = new TiffProbeEvidence();
        if (bytes.Length < 4)
        {
            return evidence;
        }
        if (bytes[0] == 0x49 && bytes[1] == 0x49)
        {
            evidence.ByteOrder = RawByteOrder.LittleEndian;
        }
        else if (bytes[0] == 0x4d && bytes[1] == 0x4d)
        {
            evidence.ByteOrder = RawByteOrder.BigEndian;
        }
        else
        {
            return evidence;
        }
        TiffProbeReader reader = new TiffProbeReader(bytes, evidence.ByteOrder, false);
        ushort version = reader.ReadUInt16(2);
        if (version == 42)
        {
            if (bytes.Length < 8)
            {
                return evidence;
            }
            evidence.HeaderValid = true;
            evidence.BigTiff = false;
            evidence.FirstIfdOffset = reader.ReadUInt32(4);
            if (bytes.Length >= 16 && bytes[8] == 0x43 && bytes[9] == 0x52)
            {
                evidence.Cr2 = true;
            }
        }
        else if (version == 43)
        {
            if (bytes.Length < 16 || reader.ReadUInt16(4) != 8 || reader.ReadUInt16(6) != 0)
            {
                return evidence;
            }
            evidence.HeaderValid = true;
            evidence.BigTiff = true;
            reader.BigTiff = true;
            evidence.FirstIfdOffset = reader.ReadUInt64(8);
        }
        else
        {
            return evidence;
        }
        cancellationToken.ThrowIfCancellationRequested();
        InspectDirectories(reader, evidence, cancellationToken);
        return evidence;
    }

    internal static void InspectDirectories(TiffProbeReader reader, TiffProbeEvidence evidence, System.Threading.CancellationToken cancellationToken)
    {
        System.Collections.Generic.List<ulong> pending = new System.Collections.Generic.List<ulong>();
        System.Collections.Generic.HashSet<ulong> visited = new System.Collections.Generic.HashSet<ulong>();
        if (evidence.FirstIfdOffset != 0)
        {
            pending.Add(evidence.FirstIfdOffset);
        }
        int pendingIndex = 0;
        while (pendingIndex < pending.Count && visited.Count < 32)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ulong directoryOffset = pending[pendingIndex];
            pendingIndex++;
            if (directoryOffset == 0 || visited.Contains(directoryOffset))
            {
                continue;
            }
            visited.Add(directoryOffset);
            int countSize = reader.BigTiff ? 8 : 2;
            int entrySize = reader.BigTiff ? 20 : 12;
            int nextSize = reader.BigTiff ? 8 : 4;
            if (!reader.CanRead(directoryOffset, (ulong)countSize))
            {
                continue;
            }
            ulong count = reader.BigTiff ? reader.ReadUInt64(directoryOffset) : reader.ReadUInt16(directoryOffset);
            if (count > 4096 || count > (ulong.MaxValue - (ulong)countSize - (ulong)nextSize) / (ulong)entrySize)
            {
                continue;
            }
            ulong directoryLength = (ulong)countSize + count * (ulong)entrySize + (ulong)nextSize;
            if (!reader.CanRead(directoryOffset, directoryLength))
            {
                continue;
            }
            ulong entryOffset = directoryOffset + (ulong)countSize;
            ulong entryIndex = 0;
            while (entryIndex < count)
            {
                InspectEntry(reader, entryOffset, evidence, pending);
                entryOffset += (ulong)entrySize;
                entryIndex++;
            }
            ulong nextOffset = reader.ReadOffset(entryOffset);
            if (nextOffset != 0 && pending.Count < 64)
            {
                pending.Add(nextOffset);
            }
        }
    }

    internal static void InspectEntry(TiffProbeReader reader, ulong entryOffset, TiffProbeEvidence evidence, System.Collections.Generic.List<ulong> pending)
    {
        ushort tag = reader.ReadUInt16(entryOffset);
        ushort type = reader.ReadUInt16(entryOffset + 2);
        ulong count = reader.BigTiff ? reader.ReadUInt64(entryOffset + 4) : reader.ReadUInt32(entryOffset + 4);
        int elementSize = FieldSize(type);
        if (elementSize == 0 || count > ulong.MaxValue / (ulong)elementSize)
        {
            return;
        }
        ulong length = count * (ulong)elementSize;
        int slotOffset = reader.BigTiff ? 12 : 8;
        int slotSize = reader.BigTiff ? 8 : 4;
        ulong valueOffset = entryOffset + (ulong)slotOffset;
        if (length > (ulong)slotSize)
        {
            valueOffset = reader.ReadOffset(valueOffset);
        }
        if (!reader.CanRead(valueOffset, length))
        {
            return;
        }
        if (tag == 271 && type == 2 && StartsWithAscii(reader.Bytes, valueOffset, length, "NIKON"))
        {
            evidence.NikonMake = true;
        }
        else if (tag == 272 && type == 2 && HasAsciiText(reader.Bytes, valueOffset, length))
        {
            evidence.CameraModel = true;
        }
        else if (tag == 37500 && StartsWithAscii(reader.Bytes, valueOffset, length, "Nikon"))
        {
            evidence.NikonMakerNote = true;
        }
        else if (tag == 259 && ReadUnsignedScalar(reader, type, count, valueOffset) == 34713)
        {
            evidence.NikonCompression = true;
        }
        else if (tag == 33421 || tag == 33422)
        {
            evidence.RawStructure = true;
        }
        else if (tag == 50706)
        {
            evidence.Dng = true;
        }
        if ((tag == 330 || tag == 34665) && pending.Count < 64)
        {
            AddPointerValues(reader, type, count, valueOffset, pending);
        }
    }

    internal static void AddPointerValues(TiffProbeReader reader, ushort type, ulong count, ulong valueOffset, System.Collections.Generic.List<ulong> pending)
    {
        int size = 0;
        if (type == 4 || type == 13)
        {
            size = 4;
        }
        else if (type == 16 || type == 18)
        {
            size = 8;
        }
        if (size == 0 || count > 16)
        {
            return;
        }
        ulong index = 0;
        while (index < count && pending.Count < 64)
        {
            ulong offset = size == 8 ? reader.ReadUInt64(valueOffset + index * 8) : reader.ReadUInt32(valueOffset + index * 4);
            if (offset != 0)
            {
                pending.Add(offset);
            }
            index++;
        }
    }

    internal static ulong ReadUnsignedScalar(TiffProbeReader reader, ushort type, ulong count, ulong valueOffset)
    {
        if (count != 1)
        {
            return ulong.MaxValue;
        }
        if (type == 1)
        {
            return reader.Bytes[(int)valueOffset];
        }
        if (type == 3)
        {
            return reader.ReadUInt16(valueOffset);
        }
        if (type == 4 || type == 13)
        {
            return reader.ReadUInt32(valueOffset);
        }
        if (type == 16 || type == 18)
        {
            return reader.ReadUInt64(valueOffset);
        }
        return ulong.MaxValue;
    }

    internal static RawProbe Classify(TiffProbeEvidence evidence, bool genericDescriptorProvided, System.Collections.Generic.List<RawDiagnostic> diagnostics)
    {
        RawFormat container = RawFormat.Unknown;
        int confidence = 0;
        bool weakNef = false;
        if (evidence.Cr2)
        {
            container = RawFormat.Cr2;
            confidence = 100;
        }
        else if (evidence.HeaderValid)
        {
            bool rawEvidence = evidence.NikonCompression || evidence.RawStructure;
            bool nikonIdentity = evidence.NikonMakerNote || evidence.NikonMake && (evidence.CameraModel || rawEvidence);
            bool strongNef = !evidence.Dng && nikonIdentity && rawEvidence;
            weakNef = !evidence.Dng && !strongNef && (evidence.NikonMake != evidence.NikonMakerNote);
            if (strongNef)
            {
                container = RawFormat.Nef;
                confidence = 95;
            }
            else if (!weakNef)
            {
                container = RawFormat.Tiff;
                confidence = 100;
            }
        }

        RawFormat[] candidates;
        RawFormat format;
        bool descriptorRequired = false;
        if (weakNef)
        {
            if (genericDescriptorProvided)
            {
                candidates = new RawFormat[] { RawFormat.Nef, RawFormat.Tiff, RawFormat.GenericRaw };
            }
            else
            {
                candidates = new RawFormat[] { RawFormat.Nef, RawFormat.Tiff };
            }
            format = RawFormat.Unknown;
            confidence = 50;
            diagnostics.Add(Diagnostic(RawDiagnosticCode.AmbiguousFormat, RawSeverity.Error, format, "Nikon identity evidence is insufficient to distinguish NEF from TIFF."));
        }
        else if (container != RawFormat.Unknown && genericDescriptorProvided)
        {
            candidates = new RawFormat[] { container, RawFormat.GenericRaw };
            format = RawFormat.Unknown;
            confidence = 50;
            diagnostics.Add(Diagnostic(RawDiagnosticCode.AmbiguousFormat, RawSeverity.Error, format, "Recognizable container content conflicts with a supplied generic raw descriptor."));
        }
        else if (container != RawFormat.Unknown)
        {
            candidates = new RawFormat[] { container };
            format = container;
        }
        else if (genericDescriptorProvided)
        {
            candidates = new RawFormat[] { RawFormat.GenericRaw };
            format = RawFormat.GenericRaw;
            confidence = 100;
        }
        else
        {
            candidates = System.Array.Empty<RawFormat>();
            format = RawFormat.Unknown;
            descriptorRequired = true;
            diagnostics.Add(Diagnostic(RawDiagnosticCode.UnknownFormat, RawSeverity.Error, format, "The content has no recognized container signature and generic raw data requires a descriptor."));
        }
        return new RawProbe(format, confidence, candidates, diagnostics.ToArray(), descriptorRequired, evidence.HeaderValid, evidence.ByteOrder, evidence.BigTiff);
    }

    internal static void AddHintDiagnostic(RawProbe result, RawFormat hint, string source, System.Collections.Generic.List<RawDiagnostic> diagnostics)
    {
        if (hint == RawFormat.Unknown)
        {
            return;
        }
        bool supported = result.Format == hint;
        int index = 0;
        while (!supported && index < result.Candidates.Length)
        {
            if (result.Candidates[index] == hint)
            {
                supported = true;
            }
            index++;
        }
        if (!supported)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.FormatMismatch, RawSeverity.Warning, result.Format, "Detected content does not match the " + source + " hint " + hint + "."));
        }
    }

    internal static RawFormat FormatFromFileName(string? fileName)
    {
        if (fileName == null || fileName.Length == 0)
        {
            return RawFormat.Unknown;
        }
        int index = fileName.LastIndexOf('.');
        if (index < 0)
        {
            return RawFormat.Unknown;
        }
        string extension = fileName.Substring(index);
        if (string.Equals(extension, ".raw", System.StringComparison.OrdinalIgnoreCase))
        {
            return RawFormat.GenericRaw;
        }
        if (string.Equals(extension, ".tif", System.StringComparison.OrdinalIgnoreCase) || string.Equals(extension, ".tiff", System.StringComparison.OrdinalIgnoreCase))
        {
            return RawFormat.Tiff;
        }
        if (string.Equals(extension, ".cr2", System.StringComparison.OrdinalIgnoreCase))
        {
            return RawFormat.Cr2;
        }
        if (string.Equals(extension, ".nef", System.StringComparison.OrdinalIgnoreCase))
        {
            return RawFormat.Nef;
        }
        return RawFormat.Unknown;
    }

    internal static int FieldSize(ushort type)
    {
        if (type == 1 || type == 2 || type == 6 || type == 7)
        {
            return 1;
        }
        if (type == 3 || type == 8)
        {
            return 2;
        }
        if (type == 4 || type == 9 || type == 11 || type == 13)
        {
            return 4;
        }
        if (type == 5 || type == 10 || type == 12 || type == 16 || type == 17 || type == 18)
        {
            return 8;
        }
        return 0;
    }

    internal static bool StartsWithAscii(byte[] bytes, ulong offset, ulong length, string value)
    {
        if (length < (ulong)value.Length || offset > (ulong)bytes.Length || (ulong)value.Length > (ulong)bytes.Length - offset)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            byte actual = bytes[(int)offset + index];
            byte expected = (byte)value[index];
            if (actual != expected)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool HasAsciiText(byte[] bytes, ulong offset, ulong length)
    {
        if (length == 0 || offset >= (ulong)bytes.Length)
        {
            return false;
        }
        ulong available = length;
        if (available > (ulong)bytes.Length - offset)
        {
            available = (ulong)bytes.Length - offset;
        }
        ulong index = 0;
        while (index < available)
        {
            byte value = bytes[(int)(offset + index)];
            if (value == 0)
            {
                return index > 0;
            }
            if (value < 0x20 || value > 0x7e)
            {
                return false;
            }
            index++;
        }
        return true;
    }

    internal static bool DefinedFormat(RawFormat format)
    {
        return format == RawFormat.Unknown || format == RawFormat.GenericRaw || format == RawFormat.Tiff || format == RawFormat.Cr2 || format == RawFormat.Nef;
    }

    internal static RawDiagnostic Diagnostic(RawDiagnosticCode code, RawSeverity severity, RawFormat format, string message)
    {
        return new RawDiagnostic(code, severity, format, -1, "", -1, "Probe", message, false);
    }
}
