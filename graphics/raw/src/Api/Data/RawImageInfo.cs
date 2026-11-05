namespace raw;

/// <summary>Describes a non-destructive rectangular region in stored image coordinates.</summary>
public class RawArea
{
    /// <summary>The semantic purpose of the area.</summary>
    public RawAreaKind Kind;
    /// <summary>The zero-based left coordinate.</summary>
    public int X;
    /// <summary>The zero-based top coordinate.</summary>
    public int Y;
    /// <summary>The area width.</summary>
    public int Width;
    /// <summary>The area height.</summary>
    public int Height;

    /// <summary>Creates an image area.</summary>
    /// <param name="kind">The semantic area kind.</param>
    /// <param name="x">The left coordinate.</param>
    /// <param name="y">The top coordinate.</param>
    /// <param name="width">The area width.</param>
    /// <param name="height">The area height.</param>
    public RawArea(RawAreaKind kind, int x, int y, int width, int height)
    {
        Kind = kind;
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>Returns whether a stored coordinate lies inside this area.</summary>
    /// <param name="x">The stored column.</param>
    /// <param name="y">The stored row.</param>
    /// <returns><c>true</c> when the coordinate is contained.</returns>
    internal bool Contains(int x, int y)
    {
        if (X < 0 || Y < 0 || Width <= 0 || Height <= 0)
        {
            return false;
        }
        return x >= X && y >= Y && (long)x < (long)X + Width && (long)y < (long)Y + Height;
    }

    /// <summary>Validates the area against complete stored image bounds.</summary>
    /// <param name="imageWidth">The stored image width.</param>
    /// <param name="imageHeight">The stored image height.</param>
    /// <returns>All area validation findings.</returns>
    internal RawValidationReport Validate(int imageWidth, int imageHeight)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (Kind <= RawAreaKind.Unknown || Kind > RawAreaKind.OpticalBlack)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "Area.Kind", "An image area requires a defined semantic kind.");
        }
        if (imageWidth <= 0 || imageHeight <= 0 || X < 0 || Y < 0 || Width <= 0 || Height <= 0 || (long)X + Width > imageWidth || (long)Y + Height > imageHeight)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "Area.Geometry", "The image area must be nonempty and contained in the stored image.");
        }
        return new RawValidationReport(true, messages.ToArray());
    }
}

/// <summary>Describes one logical sample plane of an image.</summary>
public class RawPlaneInfo
{
    /// <summary>The zero-based plane index.</summary>
    public int Index;
    /// <summary>The stable plane name.</summary>
    public string Name;
    /// <summary>The semantic channel role.</summary>
    public RawChannelKind Kind;
    /// <summary>The plane width in samples.</summary>
    public int Width;
    /// <summary>The plane height in samples.</summary>
    public int Height;
    /// <summary>The meaningful precision in bits.</summary>
    public int BitDepth;
    /// <summary>The distance between row starts in samples.</summary>
    public int RowStrideSamples;

    /// <summary>Creates a logical image plane description.</summary>
    /// <param name="index">The plane index.</param>
    /// <param name="name">The stable plane name.</param>
    /// <param name="kind">The semantic channel role.</param>
    /// <param name="width">The plane width.</param>
    /// <param name="height">The plane height.</param>
    /// <param name="bitDepth">The meaningful precision.</param>
    /// <param name="rowStrideSamples">The row stride in samples.</param>
    public RawPlaneInfo(int index, string name, RawChannelKind kind, int width, int height, int bitDepth, int rowStrideSamples)
    {
        Index = index;
        Name = name == null ? string.Empty : name;
        Kind = kind;
        Width = width;
        Height = height;
        BitDepth = bitDepth;
        RowStrideSamples = rowStrideSamples;
    }

    /// <summary>Returns the number of sample slots required through the last row.</summary>
    /// <returns>The checked required sample count.</returns>
    /// <exception cref="RawFormatException">The plane geometry is invalid or overflows.</exception>
    internal long GetRequiredSampleCount()
    {
        if (Width <= 0 || Height <= 0 || RowStrideSamples < Width)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "A valid plane geometry is required.", -1);
        }
        long preceding = (long)(Height - 1) * RowStrideSamples;
        if (preceding > long.MaxValue - Width)
        {
            throw new RawFormatException(RawDiagnosticCode.ArithmeticOverflow, "The plane sample count overflows.", -1);
        }
        return preceding + Width;
    }

    /// <summary>Validates plane identity and geometry.</summary>
    /// <returns>All plane validation findings.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (Index < 0 || Name == null || Name.Length == 0 || Kind < RawChannelKind.Unknown || Kind > RawChannelKind.Custom)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "Plane.Identity", "A plane requires a nonnegative index, name, and defined channel kind.");
        }
        if (Width <= 0 || Height <= 0 || BitDepth < 1 || BitDepth > 32 || RowStrideSamples < Width)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "Plane.Geometry", "Plane dimensions, precision, and row stride are invalid.");
        }
        return new RawValidationReport(true, messages.ToArray());
    }
}

/// <summary>Describes image identity, storage meaning, planes, and non-destructive geometry.</summary>
public class RawImageInfo
{
    /// <summary>The zero-based document image index.</summary>
    public int Index;
    /// <summary>The semantic image role.</summary>
    public RawImageKind Kind;
    /// <summary>The stored image width.</summary>
    public int Width;
    /// <summary>The stored image height.</summary>
    public int Height;
    /// <summary>The meaningful sample precision.</summary>
    public int BitDepth;
    /// <summary>The stored numeric representation.</summary>
    public RawSampleKind SampleKind;
    /// <summary>The sample photometric interpretation.</summary>
    public RawPhotometricInterpretation Photometric;
    /// <summary>The payload compression.</summary>
    public RawCompression Compression;
    /// <summary>The declared image orientation.</summary>
    public RawOrientation Orientation;
    /// <summary>Logical channels in pixel-interleaved order.</summary>
    public RawChannelDescription[] Channels;
    /// <summary>Logical plane descriptions.</summary>
    public RawPlaneInfo[] Planes;
    /// <summary>The CFA pattern, or <c>null</c> for a non-CFA image.</summary>
    public RawCfaPattern? Cfa;
    /// <summary>The complete stored bounds.</summary>
    public RawArea StoredArea;
    /// <summary>The usable active area, or <c>null</c>.</summary>
    public RawArea? ActiveArea;
    /// <summary>The camera-recommended default crop, or <c>null</c>.</summary>
    public RawArea? DefaultCrop;
    /// <summary>The mapped encoded payload, or <c>null</c>.</summary>
    public RawPayloadBlock? Payload;

    /// <summary>Creates image metadata.</summary>
    /// <param name="index">The document image index.</param>
    /// <param name="kind">The semantic image role.</param>
    /// <param name="width">The stored width.</param>
    /// <param name="height">The stored height.</param>
    /// <param name="bitDepth">The meaningful precision.</param>
    /// <param name="sampleKind">The stored numeric representation.</param>
    /// <param name="photometric">The photometric interpretation.</param>
    /// <param name="compression">The payload compression.</param>
    /// <param name="orientation">The image orientation.</param>
    /// <param name="channels">The logical channels.</param>
    /// <param name="planes">The logical planes.</param>
    /// <param name="cfa">The CFA pattern, or <c>null</c>.</param>
    /// <param name="storedArea">The complete stored area.</param>
    /// <param name="activeArea">The active area, or <c>null</c>.</param>
    /// <param name="defaultCrop">The default crop, or <c>null</c>.</param>
    /// <param name="payload">The mapped payload, or <c>null</c>.</param>
    public RawImageInfo(int index, RawImageKind kind, int width, int height, int bitDepth, RawSampleKind sampleKind, RawPhotometricInterpretation photometric, RawCompression compression, RawOrientation orientation, RawChannelDescription[] channels, RawPlaneInfo[] planes, RawCfaPattern? cfa, RawArea storedArea, RawArea? activeArea, RawArea? defaultCrop, RawPayloadBlock? payload)
    {
        Index = index;
        Kind = kind;
        Width = width;
        Height = height;
        BitDepth = bitDepth;
        SampleKind = sampleKind;
        Photometric = photometric;
        Compression = compression;
        Orientation = orientation;
        Channels = channels == null ? System.Array.Empty<RawChannelDescription>() : channels;
        Planes = planes == null ? System.Array.Empty<RawPlaneInfo>() : planes;
        Cfa = cfa;
        StoredArea = storedArea;
        ActiveArea = activeArea;
        DefaultCrop = defaultCrop;
        Payload = payload;
    }

    /// <summary>Returns the logical channel count.</summary>
    /// <returns>The channel collection length, or zero when missing.</returns>
    internal int GetChannelCount()
    {
        return Channels == null ? 0 : Channels.Length;
    }

    /// <summary>Validates image metadata and geometry against resource limits.</summary>
    /// <param name="limits">Resource limits for the image.</param>
    /// <returns>All image-info validation findings.</returns>
    internal RawValidationReport Validate(RawLimits? limits)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (limits == null || !limits.Validate().IsValid())
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "ImageInfo.Limits", "Valid resource limits are required.");
        }
        if (Index < 0 || Kind <= RawImageKind.Unknown || Kind > RawImageKind.Mask)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "ImageInfo.Identity", "An image requires a nonnegative index and defined role.");
        }
        if (!RawModelValidation.ValidImageGeometry(Width, Height, limits) || BitDepth < 1 || BitDepth > 32)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "ImageInfo.Geometry", "Image dimensions or sample precision are invalid or exceed limits.");
        }
        if (SampleKind < RawSampleKind.UnsignedInteger || SampleKind > RawSampleKind.Single || Photometric <= RawPhotometricInterpretation.Unknown || Photometric > RawPhotometricInterpretation.LinearRaw || Compression < RawCompression.Unknown || Compression > RawCompression.NikonHeStar || Orientation < RawOrientation.Unknown || Orientation > RawOrientation.Rotate270Clockwise)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "ImageInfo.Enums", "One or more image metadata values are not defined.");
        }
        ValidateChannels(messages);
        ValidatePlanes(messages);
        ValidateAreas(messages);
        int channelCount = GetChannelCount();
        if ((Photometric == RawPhotometricInterpretation.WhiteIsZero || Photometric == RawPhotometricInterpretation.BlackIsZero) && channelCount != 1 || Photometric == RawPhotometricInterpretation.Rgb && channelCount != 3 && channelCount != 4 || Photometric == RawPhotometricInterpretation.YCbCr && channelCount != 3)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "ImageInfo.Photometric", "The logical channel count does not match the photometric interpretation.");
        }
        if (SampleKind == RawSampleKind.Half && BitDepth != 16 || SampleKind == RawSampleKind.Single && BitDepth != 32)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "ImageInfo.SampleKind", "Floating-point image precision must match its numeric representation.");
        }
        if (Photometric == RawPhotometricInterpretation.ColorFilterArray)
        {
            if (Cfa == null || GetChannelCount() != 1)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "ImageInfo.Cfa", "A CFA image requires one channel and a valid pattern.");
            }
            else
            {
                RawModelValidation.AddReport(messages, Cfa.Validate());
            }
        }
        else if (Cfa != null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "ImageInfo.Cfa", "A non-CFA image cannot carry a CFA pattern.");
        }
        if (Payload != null)
        {
            long sourceLength = Payload.Offset >= 0 && Payload.Length > 0 && Payload.Offset <= long.MaxValue - Payload.Length ? Payload.Offset + Payload.Length : -1;
            RawModelValidation.AddReport(messages, Payload.Validate(sourceLength));
        }
        return new RawValidationReport(true, messages.ToArray());
    }

    internal void ValidateChannels(System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (Channels == null || Channels.Length == 0 || Channels.Length > 64)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "ImageInfo.Channels", "An image requires one through sixty-four logical channels.");
            return;
        }
        int index = 0;
        while (index < Channels.Length)
        {
            RawChannelDescription channel = Channels[index];
            if (channel == null || channel.Name == null || channel.Name.Length == 0 || channel.Kind < RawChannelKind.Unknown || channel.Kind > RawChannelKind.Custom)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "ImageInfo.Channels", "Every logical channel requires a name and defined role.");
                return;
            }
            index++;
        }
    }

    internal void ValidatePlanes(System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (Planes == null || Channels == null || Planes.Length != Channels.Length)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "ImageInfo.Planes", "Image planes must correspond one-to-one with logical channels.");
            return;
        }
        int index = 0;
        while (index < Planes.Length)
        {
            RawPlaneInfo plane = Planes[index];
            if (plane == null)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "ImageInfo.Planes", "Every logical plane must be populated.");
            }
            else
            {
                RawModelValidation.AddReport(messages, plane.Validate());
                if (plane.Index != index || plane.Width != Width || plane.Height != Height || plane.BitDepth != BitDepth)
                {
                    RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "ImageInfo.Planes", "Plane identity and geometry must match the image.");
                }
            }
            index++;
        }
    }

    internal void ValidateAreas(System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (StoredArea == null || StoredArea.Kind != RawAreaKind.StoredImage || StoredArea.X != 0 || StoredArea.Y != 0 || StoredArea.Width != Width || StoredArea.Height != Height)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "ImageInfo.StoredArea", "The stored area must exactly describe the complete image bounds.");
        }
        else
        {
            RawModelValidation.AddReport(messages, StoredArea.Validate(Width, Height));
        }
        if (ActiveArea != null)
        {
            if (ActiveArea.Kind != RawAreaKind.Active)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "ImageInfo.ActiveArea", "The active area has the wrong semantic kind.");
            }
            RawModelValidation.AddReport(messages, ActiveArea.Validate(Width, Height));
        }
        if (DefaultCrop != null)
        {
            if (DefaultCrop.Kind != RawAreaKind.DefaultCrop)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "ImageInfo.DefaultCrop", "The default crop has the wrong semantic kind.");
            }
            RawModelValidation.AddReport(messages, DefaultCrop.Validate(Width, Height));
        }
    }
}
