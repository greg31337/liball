using System;

namespace raw;

internal class CanonEarlyRawHeader
{
    internal long Offset;
    internal int Length;
    internal int CodedWidth;
    internal int CodedHeight;
    internal int Precision;
    internal int ComponentCount;
    internal int StoredWidth;
    internal int StoredHeight;
}

internal static class CanonEarlyRawLayout
{
    internal const string RawOffsetQuirk = "early-raw-offset";
    internal const string DimensionlessIfdQuirk = "dimensionless-raw-ifd";
    internal const string HalfHeightDoubleWidthQuirk = "half-height-double-width";

    internal static CanonEarlyRawHeader Inspect(System.ReadOnlyMemory<byte> source, long offset, long maximumLength, RawReadProfile profile, RawLimits? limits, System.Threading.CancellationToken cancellationToken)
    {
        if (profile == null || !HasQuirk(profile.Codec, RawOffsetQuirk) && !HasQuirk(profile.Codec, DimensionlessIfdQuirk))
        {
            throw new RawProfileException(RawDiagnosticCode.UnsupportedProfile, "An explicit early Canon raw-layout profile is required.", offset);
        }
        if (offset < 0 || maximumLength <= 0 || offset > source.Length || maximumLength > source.Length - offset || offset > int.MaxValue || maximumLength > int.MaxValue)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "The early Canon raw JPEG range is outside the bounded input.", offset);
        }
        int length = FindJpegLength(source, CheckedMath.ToInt32(offset), CheckedMath.ToInt32(maximumLength), cancellationToken);
        JpegDocument jpeg = JpegParser.Parse(source.Slice(CheckedMath.ToInt32(offset), length), limits, cancellationToken);
        if (jpeg.Frames.Count != 1 || jpeg.Frames[0].ProcessMarker != 0xc3 || jpeg.Scans.Count != 1)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "The early Canon raw payload requires one lossless JPEG frame and scan.", offset);
        }
        JpegFrameHeader frame = jpeg.Frames[0];
        if (frame.Components.Count != 2 || frame.Width <= 0 || frame.Height <= 0 || frame.Precision != profile.Codec.StoredWordBits)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The early Canon lossless JPEG dimensions, precision, or components do not match its profile.", offset);
        }
        int component = 0;
        while (component < frame.Components.Count)
        {
            if (frame.Components[component].HorizontalSampling != 1 || frame.Components[component].VerticalSampling != 1)
            {
                throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The early Canon profile requires one-by-one JPEG component sampling.", offset);
            }
            component++;
        }
        CanonEarlyRawHeader result = new CanonEarlyRawHeader();
        result.Offset = offset;
        result.Length = length;
        result.CodedWidth = frame.Width;
        result.CodedHeight = frame.Height;
        result.Precision = frame.Precision;
        result.ComponentCount = frame.Components.Count;
        ResolveGeometry(result, profile);
        return result;
    }

    internal static void ResolveGeometry(CanonEarlyRawHeader header, RawReadProfile profile)
    {
        if (header == null || profile == null || profile.Codec == null || profile.Geometry == null || header.CodedWidth <= 0 || header.CodedHeight <= 0 || header.ComponentCount <= 0)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "Complete early Canon coded and profile geometry is required.", -1);
        }
        long width = CheckedMath.Multiply(header.CodedWidth, header.ComponentCount);
        long height = header.CodedHeight;
        if (HasQuirk(profile.Codec, HalfHeightDoubleWidthQuirk))
        {
            if (header.ComponentCount != 2 || (header.CodedWidth & 1) != 0 || header.CodedWidth <= (long)header.CodedHeight * 2)
            {
                throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The half-height/double-width Canon quirk does not match the coded frame.", header.Offset);
            }
            width = header.CodedWidth;
            height = CheckedMath.Multiply(header.CodedHeight, 2);
        }
        header.StoredWidth = CheckedMath.ToInt32(width);
        header.StoredHeight = CheckedMath.ToInt32(height);
        if (header.StoredWidth != profile.Geometry.StoredWidth || header.StoredHeight != profile.Geometry.StoredHeight)
        {
            throw new RawProfileException(RawDiagnosticCode.InvalidProfile, "The early Canon decoded geometry does not match the selected profile.", header.Offset);
        }
    }

    internal static int FindJpegLength(System.ReadOnlyMemory<byte> source, int offset, int maximumLength, System.Threading.CancellationToken cancellationToken)
    {
        if (offset < 0 || maximumLength < 2 || offset > source.Length || maximumLength > source.Length - offset)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidOffset, "The early Canon JPEG search range is invalid.", offset);
        }
        System.ReadOnlySpan<byte> bytes = source.Span;
        int end = offset + maximumLength;
        if (bytes[offset] != 0xff || bytes[offset + 1] != JpegParser.StartOfImage)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidSignature, "The early Canon raw offset does not identify a JPEG stream.", offset);
        }
        int position = offset + 2;
        bool entropy = false;
        while (position < end)
        {
            if ((position & 65535) == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }
            if (bytes[position] != 0xff)
            {
                if (!entropy)
                {
                    throw new RawFormatException(RawDiagnosticCode.InvalidCodecData, "The early Canon JPEG marker sequence is invalid.", position);
                }
                position++;
                continue;
            }
            int markerOffset = position;
            position++;
            while (position < end && bytes[position] == 0xff)
            {
                position++;
            }
            if (position >= end)
            {
                break;
            }
            byte marker = bytes[position];
            position++;
            if (entropy && (marker == 0x00 || marker >= 0xd0 && marker <= 0xd7))
            {
                continue;
            }
            entropy = false;
            if (marker == JpegParser.EndOfImage)
            {
                return position - offset;
            }
            if (marker == JpegParser.StartOfImage || marker == 0x01)
            {
                continue;
            }
            if (position + 2 > end)
            {
                break;
            }
            int segmentLength = bytes[position] * 256 + bytes[position + 1];
            if (segmentLength < 2 || segmentLength > end - position)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidLength, "An early Canon JPEG segment exceeds its bounded payload.", markerOffset);
            }
            position += segmentLength;
            if (marker == JpegParser.StartOfScan)
            {
                entropy = true;
            }
        }
        throw new RawFormatException(RawDiagnosticCode.TruncatedInput, "The early Canon raw JPEG ends before EOI.", end);
    }

    internal static bool HasQuirk(RawProfileCodec codec, string quirk)
    {
        if (codec == null || codec.Quirks == null)
        {
            return false;
        }
        int index = 0;
        while (index < codec.Quirks.Length)
        {
            if (codec.Quirks[index] == quirk)
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static RawCanonMakerNoteTag? FindTag(RawMakerNote? note, ushort requested)
    {
        if (note == null || note.RawBytes == null || note.RawBytes.Length < 6 || note.Signature != "Canon" && note.Signature != "CanonIfd")
        {
            return null;
        }
        ushort count = MakerNoteCodec.ReadUInt16(note.RawBytes, 0, note.ByteOrder);
        long entriesEnd = 2L + (long)count * 12 + 4;
        if (entriesEnd > note.RawBytes.Length)
        {
            return null;
        }
        RawCanonMakerNoteTag? match = null;
        int index = 0;
        while (index < count)
        {
            int entry = 2 + index * 12;
            ushort id = MakerNoteCodec.ReadUInt16(note.RawBytes, entry, note.ByteOrder);
            if (id == requested)
            {
                if (match != null)
                {
                    return null;
                }
                ushort typeCode = MakerNoteCodec.ReadUInt16(note.RawBytes, entry + 2, note.ByteOrder);
                int elementSize = MakerNoteCodec.ElementSize(typeCode);
                uint valueCount = MakerNoteCodec.ReadUInt32(note.RawBytes, entry + 4, note.ByteOrder);
                long valueLength = CheckedMath.Multiply(valueCount, elementSize);
                int valuePosition = entry + 8;
                if (valueLength > 4)
                {
                    uint storedOffset = MakerNoteCodec.ReadUInt32(note.RawBytes, entry + 8, note.ByteOrder);
                    long relative = (long)storedOffset - note.Offset;
                    if (relative < 0 || relative > int.MaxValue)
                    {
                        return null;
                    }
                    valuePosition = (int)relative;
                }
                if (valueLength < 0 || valueLength > int.MaxValue || valuePosition < 0 || valueLength > note.RawBytes.Length - valuePosition)
                {
                    return null;
                }
                byte[] valueBytes = note.RawBytes.AsSpan(valuePosition, (int)valueLength).ToArray();
                RawTagValue value = new RawTagValue((RawTagType)typeCode, valueCount, note.ByteOrder, valueBytes);
                match = new RawCanonMakerNoteTag(id, "Tag0x" + id.ToString("X4", System.Globalization.CultureInfo.InvariantCulture), RawCanonMakerNoteVersion.Unknown, value, entry, valuePosition, false);
            }
            index++;
        }
        return match;
    }
}
