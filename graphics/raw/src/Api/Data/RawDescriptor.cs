namespace raw;

/// <summary>Describes one channel in stored generic raw sample order.</summary>
public class RawChannelDescription
{
    /// <summary>The stable caller-facing channel name.</summary>
    public string Name;
    /// <summary>The semantic channel role.</summary>
    public RawChannelKind Kind;

    /// <summary>Creates a channel description.</summary>
    /// <param name="name">The stable channel name.</param>
    /// <param name="kind">The semantic role.</param>
    public RawChannelDescription(string name, RawChannelKind kind)
    {
        Name = name == null ? string.Empty : name;
        Kind = kind;
    }
}

/// <summary>Completely describes the storage geometry of a headerless generic raw payload.</summary>
public class RawDescriptor
{
    /// <summary>The image width in pixels.</summary>
    public int Width;
    /// <summary>The image height in pixels.</summary>
    public int Height;
    /// <summary>The meaningful sample precision in bits.</summary>
    public int BitDepth;
    /// <summary>The stored word width for byte-aligned samples, or the packed precision otherwise.</summary>
    public int StoredWordBits;
    /// <summary>The numeric representation of each sample.</summary>
    public RawSampleKind SampleKind;
    /// <summary>The sample packing scheme.</summary>
    public RawPacking Packing;
    /// <summary>The byte order of byte-aligned multi-byte words.</summary>
    public RawByteOrder ByteOrder;
    /// <summary>The bit order of generic packed-bit samples.</summary>
    public RawBitOrder BitOrder;
    /// <summary>The arrangement of multiple channels.</summary>
    public RawSampleLayout Layout;
    /// <summary>The number of stored channels.</summary>
    public int ChannelCount;
    /// <summary>Channels in interleaved order or planar order.</summary>
    public RawChannelDescription[] Channels;
    /// <summary>The interpretation of the stored samples.</summary>
    public RawPhotometricInterpretation Photometric;
    /// <summary>The CFA grid for mosaic samples, or <c>null</c>.</summary>
    public RawCfaPattern? Cfa;
    /// <summary>The declared image orientation.</summary>
    public RawOrientation Orientation;
    /// <summary>The byte distance between consecutive stored rows.</summary>
    public long RowStrideBytes;
    /// <summary>The byte distance between planar channel starts, or zero for interleaved data.</summary>
    public long PlaneStrideBytes;
    /// <summary>The byte offset of the first sample from the input window start.</summary>
    public long DataOffset;
    /// <summary>The declared payload length after <see cref="DataOffset"/>.</summary>
    public long DataLength;
    /// <summary>Whether <see cref="DataLength"/> may exceed the required geometry length.</summary>
    public bool AllowTrailingBytes;

    /// <summary>Creates an empty descriptor whose fields must be filled or replaced by a preset.</summary>
    public RawDescriptor()
    {
        Width = 0;
        Height = 0;
        BitDepth = 0;
        StoredWordBits = 0;
        SampleKind = RawSampleKind.UnsignedInteger;
        Packing = RawPacking.ByteAligned;
        ByteOrder = RawByteOrder.LittleEndian;
        BitOrder = RawBitOrder.MostSignificantBitFirst;
        Layout = RawSampleLayout.Interleaved;
        ChannelCount = 0;
        Channels = System.Array.Empty<RawChannelDescription>();
        Photometric = RawPhotometricInterpretation.Unknown;
        Cfa = null;
        Orientation = RawOrientation.Normal;
        RowStrideBytes = 0;
        PlaneStrideBytes = 0;
        DataOffset = 0;
        DataLength = 0;
        AllowTrailingBytes = false;
    }

    /// <summary>Creates a tightly stored grayscale descriptor.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="bitDepth">The meaningful precision.</param>
    /// <param name="storedWordBits">The byte-aligned stored word width.</param>
    /// <param name="byteOrder">The stored word byte order.</param>
    /// <returns>The grayscale descriptor.</returns>
    internal static RawDescriptor CreateGray(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder)
    {
        return CreateColor(width, height, bitDepth, storedWordBits, byteOrder, new RawChannelDescription[] { Channel("Gray", RawChannelKind.Gray) }, RawPhotometricInterpretation.BlackIsZero);
    }

    /// <summary>Creates a tightly interleaved RGB descriptor.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="bitDepth">The meaningful precision.</param>
    /// <param name="storedWordBits">The byte-aligned stored word width.</param>
    /// <param name="byteOrder">The stored word byte order.</param>
    /// <returns>The RGB descriptor.</returns>
    internal static RawDescriptor CreateRgb(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder)
    {
        return CreateColor(width, height, bitDepth, storedWordBits, byteOrder, RgbChannels(false, false), RawPhotometricInterpretation.Rgb);
    }

    /// <summary>Creates a tightly interleaved BGR descriptor.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="bitDepth">The meaningful precision.</param>
    /// <param name="storedWordBits">The byte-aligned stored word width.</param>
    /// <param name="byteOrder">The stored word byte order.</param>
    /// <returns>The BGR descriptor.</returns>
    internal static RawDescriptor CreateBgr(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder)
    {
        return CreateColor(width, height, bitDepth, storedWordBits, byteOrder, RgbChannels(true, false), RawPhotometricInterpretation.Rgb);
    }

    /// <summary>Creates a tightly interleaved RGBA descriptor.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="bitDepth">The meaningful precision.</param>
    /// <param name="storedWordBits">The byte-aligned stored word width.</param>
    /// <param name="byteOrder">The stored word byte order.</param>
    /// <returns>The RGBA descriptor.</returns>
    internal static RawDescriptor CreateRgba(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder)
    {
        return CreateColor(width, height, bitDepth, storedWordBits, byteOrder, RgbChannels(false, true), RawPhotometricInterpretation.Rgb);
    }

    /// <summary>Creates a tightly interleaved BGRA descriptor.</summary>
    /// <param name="width">The image width.</param>
    /// <param name="height">The image height.</param>
    /// <param name="bitDepth">The meaningful precision.</param>
    /// <param name="storedWordBits">The byte-aligned stored word width.</param>
    /// <param name="byteOrder">The stored word byte order.</param>
    /// <returns>The BGRA descriptor.</returns>
    internal static RawDescriptor CreateBgra(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder)
    {
        return CreateColor(width, height, bitDepth, storedWordBits, byteOrder, RgbChannels(true, true), RawPhotometricInterpretation.Rgb);
    }

    /// <summary>Creates a tightly stored byte-aligned Bayer descriptor.</summary>
    /// <param name="width">The sensor width.</param>
    /// <param name="height">The sensor height.</param>
    /// <param name="bitDepth">The meaningful precision.</param>
    /// <param name="storedWordBits">The byte-aligned stored word width.</param>
    /// <param name="byteOrder">The stored word byte order.</param>
    /// <param name="layout">The standard Bayer phase.</param>
    /// <returns>The Bayer descriptor.</returns>
    internal static RawDescriptor CreateBayer(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder, RawCfaLayout layout)
    {
        RawDescriptor descriptor = CreateColor(width, height, bitDepth, storedWordBits, byteOrder, new RawChannelDescription[] { Channel("CFA", RawChannelKind.Custom) }, RawPhotometricInterpretation.ColorFilterArray);
        descriptor.Cfa = RawCfaPattern.CreateBayer(layout);
        return descriptor;
    }

    /// <summary>Creates a tightly stored arbitrary byte-aligned CFA descriptor.</summary>
    /// <param name="width">The sensor width.</param>
    /// <param name="height">The sensor height.</param>
    /// <param name="bitDepth">The meaningful precision.</param>
    /// <param name="storedWordBits">The byte-aligned stored word width.</param>
    /// <param name="byteOrder">The stored word byte order.</param>
    /// <param name="repeatWidth">The CFA repeat width.</param>
    /// <param name="repeatHeight">The CFA repeat height.</param>
    /// <param name="colors">Row-major CFA colors.</param>
    /// <returns>The arbitrary CFA descriptor.</returns>
    internal static RawDescriptor CreateArbitraryCfa(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder, int repeatWidth, int repeatHeight, RawCfaColor[] colors)
    {
        RawDescriptor descriptor = CreateColor(width, height, bitDepth, storedWordBits, byteOrder, new RawChannelDescription[] { Channel("CFA", RawChannelKind.Custom) }, RawPhotometricInterpretation.ColorFilterArray);
        descriptor.Cfa = RawCfaPattern.CreateArbitrary(repeatWidth, repeatHeight, colors);
        return descriptor;
    }

    /// <summary>Creates a tightly stored Bayer descriptor using RAW10, RAW12, or RAW14 group packing.</summary>
    /// <param name="width">The sensor width.</param>
    /// <param name="height">The sensor height.</param>
    /// <param name="packing">The named RAW10, RAW12, or RAW14 packing.</param>
    /// <param name="layout">The standard Bayer phase.</param>
    /// <returns>The named-packed Bayer descriptor.</returns>
    internal static RawDescriptor CreatePackedBayer(int width, int height, RawPacking packing, RawCfaLayout layout)
    {
        int depth = NamedDepth(packing);
        RawDescriptor descriptor = CreateBayer(width, height, depth, 16, RawByteOrder.LittleEndian, layout);
        descriptor.Packing = packing;
        descriptor.StoredWordBits = depth;
        descriptor.BitOrder = RawBitOrder.LeastSignificantBitFirst;
        SetTightGeometry(descriptor);
        return descriptor;
    }

    /// <summary>Returns the minimum bytes occupied by one row without stride padding.</summary>
    /// <returns>The minimum row byte count.</returns>
    /// <exception cref="RawFormatException">The descriptor packing geometry is invalid or overflows.</exception>
    internal long GetMinimumRowBytes()
    {
        long result = CalculateMinimumRowBytes();
        if (result < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "The descriptor row geometry is invalid or overflows.", -1);
        }
        return result;
    }

    /// <summary>Returns the bytes required through the last stored row of the last channel plane.</summary>
    /// <returns>The required payload byte count.</returns>
    /// <exception cref="RawFormatException">The descriptor stride geometry is invalid or overflows.</exception>
    internal long GetRequiredDataLength()
    {
        long result = CalculateRequiredDataLength();
        if (result < 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "The descriptor payload geometry is invalid or overflows.", -1);
        }
        return result;
    }

    /// <summary>Returns intentional trailing bytes after the required sample geometry.</summary>
    /// <returns>The nonnegative trailing byte count.</returns>
    /// <exception cref="RawFormatException">The declared data length is shorter than required.</exception>
    internal long GetTrailingByteCount()
    {
        long required = GetRequiredDataLength();
        if (DataLength < required)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidLength, "The declared generic raw data is shorter than its geometry.", DataOffset);
        }
        return DataLength - required;
    }

    /// <summary>Validates descriptor fields and storage geometry against resource limits.</summary>
    /// <param name="limits">The resource limits.</param>
    /// <returns>All validation findings in deterministic order.</returns>
    internal RawValidationReport Validate(RawLimits limits)
    {
        return ValidateInternal(-1, false, limits);
    }

    /// <summary>Validates the descriptor and its exact bounded input length.</summary>
    /// <param name="sourceLength">The bounded input length.</param>
    /// <param name="limits">The resource limits.</param>
    /// <returns>All validation findings in deterministic order.</returns>
    internal RawValidationReport ValidateSourceLength(long sourceLength, RawLimits? limits)
    {
        return ValidateInternal(sourceLength, true, limits);
    }

    internal RawValidationReport ValidateInternal(long sourceLength, bool checkSourceLength, RawLimits? limits)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (limits == null)
        {
            Add(messages, RawDiagnosticCode.InvalidArgument, "Descriptor.Limits", "Resource limits are required.");
            return new RawValidationReport(true, messages.ToArray());
        }
        if (Width <= 0 || Height <= 0)
        {
            Add(messages, RawDiagnosticCode.InvalidLength, "Descriptor.Geometry", "Width and height must be greater than zero.");
        }
        else
        {
            if (Width > limits.MaxWidth || Height > limits.MaxHeight || (long)Width * Height > limits.MaxPixelCount)
            {
                Add(messages, RawDiagnosticCode.ResourceLimitExceeded, "Descriptor.Geometry", "Image geometry exceeds configured limits.");
            }
        }
        ValidateEnums(messages);
        ValidateChannels(messages);
        ValidateSampleStorage(messages);
        ValidatePhotometric(messages);
        long minimumRow = CalculateMinimumRowBytes();
        if (minimumRow < 0)
        {
            Add(messages, RawDiagnosticCode.ArithmeticOverflow, "Descriptor.RowStrideBytes", "Minimum row storage is invalid or overflows.");
        }
        else if (RowStrideBytes < minimumRow)
        {
            Add(messages, RawDiagnosticCode.InvalidLength, "Descriptor.RowStrideBytes", "Row stride is shorter than packed row storage.");
        }
        long required = CalculateRequiredDataLength();
        if (required < 0)
        {
            Add(messages, RawDiagnosticCode.ArithmeticOverflow, "Descriptor.DataLength", "Payload geometry is invalid or overflows.");
        }
        if (DataOffset < 0 || DataLength < 0)
        {
            Add(messages, RawDiagnosticCode.InvalidLength, "Descriptor.DataRange", "Data offset and length cannot be negative.");
        }
        else
        {
            if (DataOffset > long.MaxValue - DataLength)
            {
                Add(messages, RawDiagnosticCode.ArithmeticOverflow, "Descriptor.DataRange", "Data offset and length overflow.");
            }
            if (DataLength > limits.MaxFileSizeBytes)
            {
                Add(messages, RawDiagnosticCode.ResourceLimitExceeded, "Descriptor.DataLength", "Declared data exceeds the configured file-size limit.");
            }
            if (required >= 0 && (DataLength < required || !AllowTrailingBytes && DataLength != required))
            {
                Add(messages, RawDiagnosticCode.InvalidLength, "Descriptor.DataLength", "Declared data length must exactly match geometry unless trailing bytes are explicitly allowed.");
            }
        }
        if (checkSourceLength)
        {
            if (sourceLength < 0)
            {
                Add(messages, RawDiagnosticCode.InvalidLength, "Descriptor.SourceLength", "The bounded source length cannot be negative.");
            }
            else if (sourceLength > limits.MaxFileSizeBytes)
            {
                Add(messages, RawDiagnosticCode.ResourceLimitExceeded, "Descriptor.SourceLength", "The bounded source exceeds the configured file-size limit.");
            }
            else if (DataOffset >= 0 && DataLength >= 0 && DataOffset <= long.MaxValue - DataLength && sourceLength != DataOffset + DataLength)
            {
                Add(messages, RawDiagnosticCode.InvalidLength, "Descriptor.SourceLength", "The bounded source length must equal data offset plus declared data length.");
            }
        }
        return new RawValidationReport(true, messages.ToArray());
    }

    internal void ValidateEnums(System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (SampleKind < RawSampleKind.UnsignedInteger || SampleKind > RawSampleKind.Single || Packing < RawPacking.ByteAligned || Packing > RawPacking.Raw14 || ByteOrder < RawByteOrder.LittleEndian || ByteOrder > RawByteOrder.BigEndian || BitOrder < RawBitOrder.MostSignificantBitFirst || BitOrder > RawBitOrder.LeastSignificantBitFirst || Layout < RawSampleLayout.Interleaved || Layout > RawSampleLayout.Planar || Photometric < RawPhotometricInterpretation.Unknown || Photometric > RawPhotometricInterpretation.LinearRaw || Orientation < RawOrientation.Unknown || Orientation > RawOrientation.Rotate270Clockwise)
        {
            Add(messages, RawDiagnosticCode.InvalidDescriptor, "Descriptor.Enums", "One or more descriptor enum values are undefined.");
        }
    }

    internal void ValidateChannels(System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (ChannelCount <= 0 || ChannelCount > 64 || Channels == null || Channels.Length != ChannelCount)
        {
            Add(messages, RawDiagnosticCode.InvalidLength, "Descriptor.Channels", "Channel count and channel descriptions must agree and contain one through sixty-four channels.");
            return;
        }
        int index = 0;
        while (index < Channels.Length)
        {
            RawChannelDescription channel = Channels[index];
            if (channel == null || channel.Name == null || channel.Name.Length == 0 || channel.Kind < RawChannelKind.Unknown || channel.Kind > RawChannelKind.Custom)
            {
                Add(messages, RawDiagnosticCode.InvalidDescriptor, "Descriptor.Channels", "Every channel requires a name and defined semantic role.");
                break;
            }
            int earlier = 0;
            while (earlier < index)
            {
                if (Channels[earlier] != null && Channels[earlier].Name == channel.Name)
                {
                    Add(messages, RawDiagnosticCode.InvalidDescriptor, "Descriptor.Channels", "Channel names must be unique and preserve explicit order.");
                    return;
                }
                earlier++;
            }
            index++;
        }
    }

    internal void ValidateSampleStorage(System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (BitDepth < 1 || BitDepth > 32)
        {
            Add(messages, RawDiagnosticCode.InvalidLength, "Descriptor.BitDepth", "Sample precision must be between one and thirty-two bits.");
        }
        if (Packing == RawPacking.ByteAligned)
        {
            if (StoredWordBits != 8 && StoredWordBits != 16 && StoredWordBits != 32 || BitDepth > StoredWordBits)
            {
                Add(messages, RawDiagnosticCode.InvalidDescriptor, "Descriptor.StoredWordBits", "Byte-aligned words must be 8, 16, or 32 bits and hold the meaningful precision.");
            }
        }
        else if (Packing == RawPacking.PackedBits)
        {
            if (StoredWordBits != BitDepth || SampleKind == RawSampleKind.Half || SampleKind == RawSampleKind.Single)
            {
                Add(messages, RawDiagnosticCode.InvalidDescriptor, "Descriptor.Packing", "Generic packed bits require integer samples and stored width equal to precision.");
            }
        }
        else if (Packing == RawPacking.Raw10 || Packing == RawPacking.Raw12 || Packing == RawPacking.Raw14)
        {
            int namedDepth = NamedDepthOrZero(Packing);
            if (BitDepth != namedDepth || StoredWordBits != namedDepth || SampleKind != RawSampleKind.UnsignedInteger)
            {
                Add(messages, RawDiagnosticCode.InvalidDescriptor, "Descriptor.Packing", "Named raw packing requires its exact unsigned integer precision.");
            }
        }
        if (SampleKind == RawSampleKind.Half && (Packing != RawPacking.ByteAligned || BitDepth != 16 || StoredWordBits != 16))
        {
            Add(messages, RawDiagnosticCode.InvalidDescriptor, "Descriptor.SampleKind", "Half samples require byte-aligned sixteen-bit storage.");
        }
        if (SampleKind == RawSampleKind.Single && (Packing != RawPacking.ByteAligned || BitDepth != 32 || StoredWordBits != 32))
        {
            Add(messages, RawDiagnosticCode.InvalidDescriptor, "Descriptor.SampleKind", "Single samples require byte-aligned thirty-two-bit storage.");
        }
    }

    internal void ValidatePhotometric(System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (Photometric == RawPhotometricInterpretation.Unknown || Photometric == RawPhotometricInterpretation.Palette)
        {
            Add(messages, RawDiagnosticCode.InvalidDescriptor, "Descriptor.Photometric", "Generic raw data requires an explicit supported photometric interpretation.");
        }
        if ((Photometric == RawPhotometricInterpretation.WhiteIsZero || Photometric == RawPhotometricInterpretation.BlackIsZero) && ChannelCount != 1)
        {
            Add(messages, RawDiagnosticCode.InvalidDescriptor, "Descriptor.Photometric", "Grayscale interpretation requires one channel.");
        }
        if (Photometric == RawPhotometricInterpretation.Rgb && (ChannelCount != 3 && ChannelCount != 4))
        {
            Add(messages, RawDiagnosticCode.InvalidDescriptor, "Descriptor.Photometric", "RGB interpretation requires three or four channels.");
        }
        if (Photometric == RawPhotometricInterpretation.YCbCr && ChannelCount != 3)
        {
            Add(messages, RawDiagnosticCode.InvalidDescriptor, "Descriptor.Photometric", "YCbCr interpretation requires three channels.");
        }
        if (Photometric == RawPhotometricInterpretation.ColorFilterArray)
        {
            if (ChannelCount != 1 || Cfa == null)
            {
                Add(messages, RawDiagnosticCode.InvalidDescriptor, "Descriptor.Cfa", "CFA interpretation requires one stored mosaic channel and a pattern.");
            }
            else
            {
                RawValidationReport report = Cfa.Validate();
                int index = 0;
                while (index < (report.Messages ?? throw new System.InvalidOperationException("A required value is unavailable.")).Length)
                {
                    messages.Add(report.Messages[index]);
                    index++;
                }
            }
        }
        else if (Cfa != null)
        {
            Add(messages, RawDiagnosticCode.InvalidDescriptor, "Descriptor.Cfa", "A CFA pattern cannot accompany a non-CFA interpretation.");
        }
    }

    internal long CalculateMinimumRowBytes()
    {
        if (Width <= 0 || ChannelCount <= 0 || BitDepth <= 0 || BitDepth > 32)
        {
            return -1;
        }
        long samples = Width;
        if (Layout == RawSampleLayout.Interleaved)
        {
            samples = MultiplyOrNegative(samples, ChannelCount);
        }
        else if (Layout != RawSampleLayout.Planar)
        {
            return -1;
        }
        if (samples < 0)
        {
            return -1;
        }
        if (Packing == RawPacking.ByteAligned)
        {
            if (StoredWordBits != 8 && StoredWordBits != 16 && StoredWordBits != 32)
            {
                return -1;
            }
            return MultiplyOrNegative(samples, StoredWordBits / 8);
        }
        if (Packing == RawPacking.PackedBits)
        {
            return PackedBytes(samples, BitDepth);
        }
        int depth = NamedDepthOrZero(Packing);
        if (depth == 0)
        {
            return -1;
        }
        int group = Packing == RawPacking.Raw12 ? 2 : 4;
        int groupBytes = Packing == RawPacking.Raw10 ? 5 : Packing == RawPacking.Raw12 ? 3 : 7;
        long full = samples / group;
        long remainder = samples % group;
        long result = MultiplyOrNegative(full, groupBytes);
        if (result < 0)
        {
            return -1;
        }
        if (remainder != 0)
        {
            long lowBits = MultiplyOrNegative(remainder, depth - 8);
            if (lowBits < 0 || lowBits > long.MaxValue - 7 || result > long.MaxValue - remainder - (lowBits + 7) / 8)
            {
                return -1;
            }
            result += remainder + (lowBits + 7) / 8;
        }
        return result;
    }

    internal long CalculateRequiredDataLength()
    {
        long minimum = CalculateMinimumRowBytes();
        if (minimum < 0 || RowStrideBytes < minimum || Height <= 0 || ChannelCount <= 0)
        {
            return -1;
        }
        long planeContent = MultiplyOrNegative(RowStrideBytes, Height);
        if (planeContent < 0)
        {
            return -1;
        }
        if (Layout == RawSampleLayout.Interleaved)
        {
            if (PlaneStrideBytes != 0)
            {
                return -1;
            }
            return planeContent;
        }
        if (Layout != RawSampleLayout.Planar || PlaneStrideBytes < planeContent)
        {
            return -1;
        }
        long preceding = MultiplyOrNegative(PlaneStrideBytes, ChannelCount - 1);
        if (preceding < 0 || preceding > long.MaxValue - planeContent)
        {
            return -1;
        }
        return preceding + planeContent;
    }

    internal static RawDescriptor CreateColor(int width, int height, int bitDepth, int storedWordBits, RawByteOrder byteOrder, RawChannelDescription[] channels, RawPhotometricInterpretation photometric)
    {
        RawDescriptor descriptor = new RawDescriptor();
        descriptor.Width = width;
        descriptor.Height = height;
        descriptor.BitDepth = bitDepth;
        descriptor.StoredWordBits = storedWordBits;
        descriptor.SampleKind = RawSampleKind.UnsignedInteger;
        descriptor.Packing = RawPacking.ByteAligned;
        descriptor.ByteOrder = byteOrder;
        descriptor.BitOrder = RawBitOrder.MostSignificantBitFirst;
        descriptor.Layout = RawSampleLayout.Interleaved;
        descriptor.ChannelCount = channels.Length;
        descriptor.Channels = channels;
        descriptor.Photometric = photometric;
        descriptor.Orientation = RawOrientation.Normal;
        SetTightGeometry(descriptor);
        return descriptor;
    }

    internal static void SetTightGeometry(RawDescriptor descriptor)
    {
        descriptor.RowStrideBytes = descriptor.GetMinimumRowBytes();
        descriptor.PlaneStrideBytes = 0;
        descriptor.DataOffset = 0;
        descriptor.DataLength = descriptor.GetRequiredDataLength();
        descriptor.AllowTrailingBytes = false;
    }

    internal static RawChannelDescription[] RgbChannels(bool blueFirst, bool alpha)
    {
        RawChannelDescription red = Channel("R", RawChannelKind.Red);
        RawChannelDescription green = Channel("G", RawChannelKind.Green);
        RawChannelDescription blue = Channel("B", RawChannelKind.Blue);
        RawChannelDescription opacity = Channel("A", RawChannelKind.Alpha);
        if (blueFirst && alpha)
        {
            return new RawChannelDescription[] { blue, green, red, opacity };
        }
        if (blueFirst)
        {
            return new RawChannelDescription[] { blue, green, red };
        }
        if (alpha)
        {
            return new RawChannelDescription[] { red, green, blue, opacity };
        }
        return new RawChannelDescription[] { red, green, blue };
    }

    internal static RawChannelDescription Channel(string name, RawChannelKind kind)
    {
        return new RawChannelDescription(name, kind);
    }

    internal static int NamedDepth(RawPacking packing)
    {
        int depth = NamedDepthOrZero(packing);
        if (depth == 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Named Bayer packing must be RAW10, RAW12, or RAW14.", -1);
        }
        return depth;
    }

    internal static int NamedDepthOrZero(RawPacking packing)
    {
        if (packing == RawPacking.Raw10)
        {
            return 10;
        }
        if (packing == RawPacking.Raw12)
        {
            return 12;
        }
        if (packing == RawPacking.Raw14)
        {
            return 14;
        }
        return 0;
    }

    internal static long PackedBytes(long samples, int bitDepth)
    {
        long bits = MultiplyOrNegative(samples, bitDepth);
        if (bits < 0 || bits > long.MaxValue - 7)
        {
            return -1;
        }
        return (bits + 7) / 8;
    }

    internal static long MultiplyOrNegative(long first, long second)
    {
        if (first < 0 || second < 0 || first != 0 && second > long.MaxValue / first)
        {
            return -1;
        }
        return first * second;
    }

    internal static void Add(System.Collections.Generic.List<RawValidationMessage> messages, RawDiagnosticCode code, string context, string message)
    {
        messages.Add(new RawValidationMessage(RawValidationCategory.Semantic, code, RawSeverity.Error, RawFormat.GenericRaw, -1, context, message, false));
    }
}
