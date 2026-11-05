namespace raw;

/// <summary>Contains exact decoded stored samples with explicit buffer ownership.</summary>
public class RawStoredImage
{
    /// <summary>Image identity and geometry.</summary>
    public RawImageInfo Info;
    /// <summary>Unsigned stored samples, or an empty array.</summary>
    public uint[] UnsignedSamples;
    /// <summary>Signed stored samples, or an empty array.</summary>
    public int[] SignedSamples;
    /// <summary>Half- or single-precision values expanded to <c>float</c>, or an empty array.</summary>
    public float[] FloatingSamples;
    /// <summary>The distance between pixel-interleaved row starts in samples.</summary>
    public int RowStrideSamples;
    /// <summary>Whether this model owns and may replace its sample array.</summary>
    public bool OwnsSamples;
    /// <summary>Diagnostics produced while reconstructing stored samples.</summary>
    public RawDiagnostic[] Diagnostics;

    /// <summary>Creates an exact stored-sample image.</summary>
    /// <param name="info">Image identity and geometry.</param>
    /// <param name="unsignedSamples">Unsigned samples, or <c>null</c>.</param>
    /// <param name="signedSamples">Signed samples, or <c>null</c>.</param>
    /// <param name="floatingSamples">Floating samples, or <c>null</c>.</param>
    /// <param name="rowStrideSamples">The pixel-interleaved row stride.</param>
    /// <param name="ownsSamples">Whether the model owns its sample storage.</param>
    /// <param name="diagnostics">Decode diagnostics.</param>
    public RawStoredImage(RawImageInfo info, uint[]? unsignedSamples, int[]? signedSamples, float[]? floatingSamples, int rowStrideSamples, bool ownsSamples, RawDiagnostic[]? diagnostics)
    {
        Info = info;
        UnsignedSamples = unsignedSamples == null ? System.Array.Empty<uint>() : unsignedSamples;
        SignedSamples = signedSamples == null ? System.Array.Empty<int>() : signedSamples;
        FloatingSamples = floatingSamples == null ? System.Array.Empty<float>() : floatingSamples;
        RowStrideSamples = rowStrideSamples;
        OwnsSamples = ownsSamples;
        Diagnostics = diagnostics == null ? System.Array.Empty<RawDiagnostic>() : diagnostics;
    }

    /// <summary>Returns the populated stored sample array.</summary>
    /// <returns>The unsigned, signed, or floating sample array as an object.</returns>
    internal object GetSamples()
    {
        if (UnsignedSamples != null && UnsignedSamples.Length != 0)
        {
            return UnsignedSamples;
        }
        if (SignedSamples != null && SignedSamples.Length != 0)
        {
            return SignedSamples;
        }
        return FloatingSamples;
    }

    /// <summary>Validates buffer selection, geometry, sample domains, limits, and diagnostics.</summary>
    /// <param name="limits">Resource limits for the decoded image.</param>
    /// <returns>All stored-image validation findings.</returns>
    internal RawValidationReport Validate(RawLimits? limits)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (Info == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "StoredImage.Info", "Stored image metadata is required.");
            return new RawValidationReport(true, messages.ToArray());
        }
        RawModelValidation.AddReport(messages, Info.Validate(limits));
        int channelCount = Info.GetChannelCount();
        long required = RawImageBufferValidation.RequiredSamples(Info.Width, Info.Height, channelCount, RowStrideSamples);
        bool unsigned = UnsignedSamples != null && UnsignedSamples.Length != 0;
        bool signed = SignedSamples != null && SignedSamples.Length != 0;
        bool floating = FloatingSamples != null && FloatingSamples.Length != 0;
        int selected = (unsigned ? 1 : 0) + (signed ? 1 : 0) + (floating ? 1 : 0);
        if (required < 0 || selected != 1 || unsigned && (UnsignedSamples ?? throw new System.InvalidOperationException("A required value is unavailable.")).LongLength < required || signed && (SignedSamples ?? throw new System.InvalidOperationException("A required value is unavailable.")).LongLength < required || floating && (FloatingSamples ?? throw new System.InvalidOperationException("A required value is unavailable.")).LongLength < required)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "StoredImage.Samples", "Exactly one sample array must contain the complete pixel-interleaved image geometry.");
        }
        if (unsigned != (Info.SampleKind == RawSampleKind.UnsignedInteger) || signed != (Info.SampleKind == RawSampleKind.SignedInteger) || floating != (Info.SampleKind == RawSampleKind.Half || Info.SampleKind == RawSampleKind.Single))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "StoredImage.SampleKind", "The populated sample array must match the declared numeric representation.");
        }
        bool enough = unsigned && required >= 0 && (UnsignedSamples ?? throw new System.InvalidOperationException("A required value is unavailable.")).LongLength >= required || signed && required >= 0 && (SignedSamples ?? throw new System.InvalidOperationException("A required value is unavailable.")).LongLength >= required || floating && required >= 0 && (FloatingSamples ?? throw new System.InvalidOperationException("A required value is unavailable.")).LongLength >= required;
        if (selected == 1 && enough)
        {
            ValidateIntegerDomain(messages, required);
        }
        RawImageBufferValidation.ValidateDiagnostics(messages, Diagnostics, "StoredImage.Diagnostics");
        return new RawValidationReport(true, messages.ToArray());
    }

    internal void ValidateIntegerDomain(System.Collections.Generic.List<RawValidationMessage> messages, long required)
    {
        if (required < 0 || Info.BitDepth < 1 || Info.BitDepth > 32)
        {
            return;
        }
        if (UnsignedSamples != null && UnsignedSamples.Length != 0)
        {
            uint maximum = Info.BitDepth == 32 ? uint.MaxValue : (1U << Info.BitDepth) - 1U;
            int row = 0;
            int rowSamples = Info.Width * Info.GetChannelCount();
            while (row < Info.Height)
            {
                int index = row * RowStrideSamples;
                int end = index + rowSamples;
                while (index < end)
                {
                    if (UnsignedSamples[index] > maximum)
                    {
                        RawModelValidation.Add(messages, RawDiagnosticCode.InvalidCodecData, "StoredImage.Samples", "An unsigned stored sample exceeds the declared precision.");
                        return;
                    }
                    index++;
                }
                row++;
            }
        }
        if (SignedSamples != null && SignedSamples.Length != 0)
        {
            long minimum = Info.BitDepth == 32 ? int.MinValue : -(1L << (Info.BitDepth - 1));
            long maximum = Info.BitDepth == 32 ? int.MaxValue : (1L << (Info.BitDepth - 1)) - 1L;
            int row = 0;
            int rowSamples = Info.Width * Info.GetChannelCount();
            while (row < Info.Height)
            {
                int index = row * RowStrideSamples;
                int end = index + rowSamples;
                while (index < end)
                {
                    if (SignedSamples[index] < minimum || SignedSamples[index] > maximum)
                    {
                        RawModelValidation.Add(messages, RawDiagnosticCode.InvalidCodecData, "StoredImage.Samples", "A signed stored sample exceeds the declared precision.");
                        return;
                    }
                    index++;
                }
                row++;
            }
        }
    }
}

/// <summary>Contains normalized sensor samples, non-destructive areas, and calibration data.</summary>
public class RawSensorImage
{
    /// <summary>Image identity and geometry.</summary>
    public RawImageInfo Info;
    /// <summary>Normalized unsigned sixteen-bit sensor samples, or an empty array.</summary>
    public ushort[] IntegerSamples;
    /// <summary>Normalized floating sensor samples, or an empty array.</summary>
    public float[] FloatingSamples;
    /// <summary>The distance between pixel-interleaved row starts in samples.</summary>
    public int RowStrideSamples;
    /// <summary>Whether this model owns and may replace its normalized sample array.</summary>
    public bool OwnsSamples;
    /// <summary>Optional exact stored values retained alongside normalized data.</summary>
    public RawStoredImage? StoredImage;
    /// <summary>An optional caller-selected non-destructive crop.</summary>
    public RawArea? UserCrop;
    /// <summary>The resolved stored, active, default, and user crop hierarchy, or <c>null</c>.</summary>
    public RawCropModel? CropModel;
    /// <summary>Masked and optical-black areas.</summary>
    public RawArea[] MaskedAreas;
    /// <summary>Black-level calibration, or <c>null</c>.</summary>
    public RawBlackLevel? BlackLevel;
    /// <summary>One white level or one value per logical channel.</summary>
    public double[] WhiteLevels;
    /// <summary>As-shot or selected white-balance calibration, or <c>null</c>.</summary>
    public RawWhiteBalance? WhiteBalance;
    /// <summary>Available color calibrations.</summary>
    public RawColorProfile[] ColorProfiles;
    /// <summary>Black- and white-clipped sample locations, or <c>null</c>.</summary>
    public RawClippingMask? ClippingMask;
    /// <summary>Defect-repair results, or <c>null</c> before repair.</summary>
    public RawDefectMap? DefectMap;
    /// <summary>Masked-sample banding correction applied during preprocessing, or <c>null</c>.</summary>
    public RawBandingCorrectionResult? BandingCorrection;
    /// <summary>Embedded sensor gain maps.</summary>
    public RawGainMap[] GainMaps;
    /// <summary>Embedded raw chromatic-aberration calibration, or <c>null</c>.</summary>
    public RawChromaticAberration? ChromaticAberration;
    /// <summary>Whether embedded gain maps have been applied to these samples.</summary>
    public bool GainMapsApplied;
    /// <summary>Whether raw chromatic-aberration scaling has been applied to these samples.</summary>
    public bool ChromaticAberrationApplied;
    /// <summary>Diagnostics produced during decode and normalization.</summary>
    public RawDiagnostic[] Diagnostics;

    /// <summary>Creates a normalized sensor image.</summary>
    /// <param name="info">Image identity and geometry.</param>
    /// <param name="integerSamples">Normalized unsigned sixteen-bit samples, or <c>null</c>.</param>
    /// <param name="floatingSamples">Normalized floating samples, or <c>null</c>.</param>
    /// <param name="rowStrideSamples">The pixel-interleaved row stride.</param>
    /// <param name="ownsSamples">Whether the model owns its normalized sample storage.</param>
    /// <param name="storedImage">Optional exact stored samples.</param>
    /// <param name="userCrop">Optional caller crop.</param>
    /// <param name="maskedAreas">Masked and optical-black areas.</param>
    /// <param name="blackLevel">Optional black-level calibration.</param>
    /// <param name="whiteLevels">White levels.</param>
    /// <param name="whiteBalance">Optional white-balance calibration.</param>
    /// <param name="colorProfiles">Available color profiles.</param>
    /// <param name="diagnostics">Decode diagnostics.</param>
    public RawSensorImage(RawImageInfo info, ushort[]? integerSamples, float[]? floatingSamples, int rowStrideSamples, bool ownsSamples, RawStoredImage? storedImage, RawArea? userCrop, RawArea[]? maskedAreas, RawBlackLevel? blackLevel, double[]? whiteLevels, RawWhiteBalance? whiteBalance, RawColorProfile[]? colorProfiles, RawDiagnostic[]? diagnostics)
    {
        Info = info;
        IntegerSamples = integerSamples == null ? System.Array.Empty<ushort>() : integerSamples;
        FloatingSamples = floatingSamples == null ? System.Array.Empty<float>() : floatingSamples;
        RowStrideSamples = rowStrideSamples;
        OwnsSamples = ownsSamples;
        StoredImage = storedImage;
        UserCrop = userCrop;
        CropModel = null;
        MaskedAreas = maskedAreas == null ? System.Array.Empty<RawArea>() : maskedAreas;
        BlackLevel = blackLevel;
        WhiteLevels = whiteLevels == null ? System.Array.Empty<double>() : whiteLevels;
        WhiteBalance = whiteBalance;
        ColorProfiles = colorProfiles == null ? System.Array.Empty<RawColorProfile>() : colorProfiles;
        ClippingMask = null;
        DefectMap = null;
        BandingCorrection = null;
        GainMaps = System.Array.Empty<RawGainMap>();
        ChromaticAberration = null;
        GainMapsApplied = false;
        ChromaticAberrationApplied = false;
        Diagnostics = diagnostics == null ? System.Array.Empty<RawDiagnostic>() : diagnostics;
    }

    /// <summary>Returns the populated normalized sample array.</summary>
    /// <returns>The unsigned sixteen-bit or floating sample array as an object.</returns>
    internal object GetSamples()
    {
        if (IntegerSamples != null && IntegerSamples.Length != 0)
        {
            return IntegerSamples;
        }
        return FloatingSamples;
    }

    /// <summary>Validates normalized storage, areas, calibration, limits, and diagnostics.</summary>
    /// <param name="limits">Resource limits for the decoded image.</param>
    /// <returns>All sensor-image validation findings.</returns>
    internal RawValidationReport Validate(RawLimits? limits)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (Info == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "SensorImage.Info", "Sensor image metadata is required.");
            return new RawValidationReport(true, messages.ToArray());
        }
        RawModelValidation.AddReport(messages, Info.Validate(limits));
        int channelCount = Info.GetChannelCount();
        long required = RawImageBufferValidation.RequiredSamples(Info.Width, Info.Height, channelCount, RowStrideSamples);
        bool integers = IntegerSamples != null && IntegerSamples.Length != 0;
        bool floating = FloatingSamples != null && FloatingSamples.Length != 0;
        if (required < 0 || integers == floating || integers && (IntegerSamples ?? throw new System.InvalidOperationException("A required value is unavailable.")).LongLength < required || floating && (FloatingSamples ?? throw new System.InvalidOperationException("A required value is unavailable.")).LongLength < required)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "SensorImage.Samples", "Exactly one normalized sample array must contain the complete pixel-interleaved image geometry.");
        }
        if (StoredImage != null)
        {
            RawModelValidation.AddReport(messages, StoredImage.Validate(limits));
            if (StoredImage.Info != Info)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "SensorImage.StoredImage", "Retained stored samples must reference the same image metadata.");
            }
        }
        ValidateAreas(messages);
        ValidateCalibration(messages, channelCount);
        if (CropModel != null)
        {
            RawModelValidation.AddReport(messages, CropModel.Validate(Info.Width, Info.Height));
            if (!RawCropModel.Same(UserCrop, CropModel.UserCrop))
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "SensorImage.CropModel", "The resolved crop model must contain the sensor image user crop.");
            }
        }
        if (ClippingMask != null)
        {
            RawModelValidation.AddReport(messages, ClippingMask.Validate());
            if (ClippingMask.Width != Info.Width || ClippingMask.Height != Info.Height || ClippingMask.ChannelCount != channelCount || ClippingMask.RowStrideSamples != RowStrideSamples)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "SensorImage.ClippingMask", "The clipping mask must match the normalized sample geometry.");
            }
        }
        if (DefectMap != null)
        {
            RawModelValidation.AddReport(messages, DefectMap.Validate());
            if (DefectMap.Width != Info.Width || DefectMap.Height != Info.Height || DefectMap.ChannelCount != channelCount || DefectMap.RowStrideSamples != RowStrideSamples)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "SensorImage.DefectMap", "The defect map must match the normalized sample geometry.");
            }
        }
        if (BandingCorrection != null)
        {
            RawModelValidation.AddReport(messages, BandingCorrection.Validate(Info.Width, Info.Height));
        }
        ValidateLensCorrections(messages);
        RawImageBufferValidation.ValidateDiagnostics(messages, Diagnostics, "SensorImage.Diagnostics");
        return new RawValidationReport(true, messages.ToArray());
    }

    internal void ValidateLensCorrections(System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (GainMaps == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "SensorImage.GainMaps", "The gain-map collection cannot be null.");
        }
        else
        {
            int index = 0;
            while (index < GainMaps.Length)
            {
                if (GainMaps[index] == null)
                {
                    RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "SensorImage.GainMaps", "Every gain-map entry must be populated.");
                }
                else
                {
                    RawModelValidation.AddReport(messages, GainMaps[index].Validate(Info));
                }
                index++;
            }
        }
        if (ChromaticAberration != null)
        {
            RawModelValidation.AddReport(messages, ChromaticAberration.Validate(Info));
        }
        if (GainMapsApplied && (GainMaps == null || GainMaps.Length == 0))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "SensorImage.GainMapsApplied", "Applied gain-map state requires embedded gain metadata.");
        }
        if (ChromaticAberrationApplied && ChromaticAberration == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "SensorImage.ChromaticAberrationApplied", "Applied chromatic-aberration state requires embedded calibration.");
        }
    }

    internal void ValidateAreas(System.Collections.Generic.List<RawValidationMessage> messages)
    {
        if (UserCrop != null)
        {
            if (UserCrop.Kind != RawAreaKind.UserCrop)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "SensorImage.UserCrop", "The caller crop has the wrong semantic kind.");
            }
            RawModelValidation.AddReport(messages, UserCrop.Validate(Info.Width, Info.Height));
        }
        if (MaskedAreas == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "SensorImage.MaskedAreas", "The masked-area collection cannot be null.");
            return;
        }
        int index = 0;
        while (index < MaskedAreas.Length)
        {
            RawArea area = MaskedAreas[index];
            if (area == null || area.Kind != RawAreaKind.Masked && area.Kind != RawAreaKind.OpticalBlack)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "SensorImage.MaskedAreas", "Every masked area must be populated and have masked or optical-black semantics.");
            }
            else
            {
                RawModelValidation.AddReport(messages, area.Validate(Info.Width, Info.Height));
            }
            index++;
        }
    }

    internal void ValidateCalibration(System.Collections.Generic.List<RawValidationMessage> messages, int channelCount)
    {
        if (BlackLevel != null)
        {
            RawModelValidation.AddReport(messages, BlackLevel.Validate(Info.Width, Info.Height, channelCount));
        }
        if (WhiteLevels == null || WhiteLevels.Length != 0 && WhiteLevels.Length != 1 && WhiteLevels.Length != channelCount)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "SensorImage.WhiteLevels", "White levels must be empty, scalar, or contain one finite value per channel.");
        }
        else
        {
            int index = 0;
            while (index < WhiteLevels.Length)
            {
                if (!RawModelValidation.IsFinite(WhiteLevels[index]) || Info.SampleKind != RawSampleKind.SignedInteger && WhiteLevels[index] <= 0.0)
                {
                    RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "SensorImage.WhiteLevels", "Every white level must be finite and unsigned or floating white levels must be positive.");
                    break;
                }
                index++;
            }
        }
        if (WhiteBalance != null)
        {
            RawModelValidation.AddReport(messages, WhiteBalance.Validate(channelCount));
        }
        if (ColorProfiles == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "SensorImage.ColorProfiles", "The color-profile collection cannot be null.");
            return;
        }
        int profileIndex = 0;
        while (profileIndex < ColorProfiles.Length)
        {
            if (ColorProfiles[profileIndex] == null)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "SensorImage.ColorProfiles", "Every color-profile entry must be populated.");
            }
            else
            {
                RawModelValidation.AddReport(messages, ColorProfiles[profileIndex].Validate());
            }
            profileIndex++;
        }
    }
}

/// <summary>Contains deterministic gray, RGB, or RGBA render output with explicit buffer ownership.</summary>
public class RawRgbImage
{
    /// <summary>The output width.</summary>
    public int Width;
    /// <summary>The output height.</summary>
    public int Height;
    /// <summary>The number of pixel-interleaved output channels.</summary>
    public int ChannelCount;
    /// <summary>The distance between row starts in samples.</summary>
    public int RowStrideSamples;
    /// <summary>Eight-bit samples, or an empty array.</summary>
    public byte[] ByteSamples;
    /// <summary>Unsigned sixteen-bit samples, or an empty array.</summary>
    public ushort[] UShortSamples;
    /// <summary>Floating-point samples, or an empty array.</summary>
    public float[] FloatingSamples;
    /// <summary>The output channel order.</summary>
    public RawChannelDescription[] Channels;
    /// <summary>The output color space.</summary>
    public RawColorSpace ColorSpace;
    /// <summary>The applied transfer function.</summary>
    public RawTransferFunction TransferFunction;
    /// <summary>The retained orientation metadata.</summary>
    public RawOrientation Orientation;
    /// <summary>Whether orientation has already been applied to pixel positions.</summary>
    public bool OrientationApplied;
    /// <summary>Whether this model owns and may replace its sample array.</summary>
    public bool OwnsSamples;
    /// <summary>Diagnostics produced during rendering or thumbnail decode.</summary>
    public RawDiagnostic[] Diagnostics;

    /// <summary>Creates a rendered image.</summary>
    /// <param name="width">The output width.</param>
    /// <param name="height">The output height.</param>
    /// <param name="channelCount">The output channel count.</param>
    /// <param name="rowStrideSamples">The row stride in samples.</param>
    /// <param name="byteSamples">Eight-bit samples, or <c>null</c>.</param>
    /// <param name="ushortSamples">Unsigned sixteen-bit samples, or <c>null</c>.</param>
    /// <param name="floatingSamples">Floating samples, or <c>null</c>.</param>
    /// <param name="channels">The output channel order.</param>
    /// <param name="colorSpace">The output color space.</param>
    /// <param name="transferFunction">The applied transfer function.</param>
    /// <param name="orientation">The retained orientation.</param>
    /// <param name="orientationApplied">Whether orientation is baked into pixels.</param>
    /// <param name="ownsSamples">Whether the model owns its sample storage.</param>
    /// <param name="diagnostics">Render diagnostics.</param>
    public RawRgbImage(int width, int height, int channelCount, int rowStrideSamples, byte[]? byteSamples, ushort[]? ushortSamples, float[]? floatingSamples, RawChannelDescription[] channels, RawColorSpace colorSpace, RawTransferFunction transferFunction, RawOrientation orientation, bool orientationApplied, bool ownsSamples, RawDiagnostic[]? diagnostics)
    {
        Width = width;
        Height = height;
        ChannelCount = channelCount;
        RowStrideSamples = rowStrideSamples;
        ByteSamples = byteSamples == null ? System.Array.Empty<byte>() : byteSamples;
        UShortSamples = ushortSamples == null ? System.Array.Empty<ushort>() : ushortSamples;
        FloatingSamples = floatingSamples == null ? System.Array.Empty<float>() : floatingSamples;
        Channels = channels == null ? System.Array.Empty<RawChannelDescription>() : channels;
        ColorSpace = colorSpace;
        TransferFunction = transferFunction;
        Orientation = orientation;
        OrientationApplied = orientationApplied;
        OwnsSamples = ownsSamples;
        Diagnostics = diagnostics == null ? System.Array.Empty<RawDiagnostic>() : diagnostics;
    }

    /// <summary>Returns the populated rendered sample array.</summary>
    /// <returns>The byte, unsigned sixteen-bit, or floating sample array as an object.</returns>
    internal object GetSamples()
    {
        if (ByteSamples != null && ByteSamples.Length != 0)
        {
            return ByteSamples;
        }
        if (UShortSamples != null && UShortSamples.Length != 0)
        {
            return UShortSamples;
        }
        return FloatingSamples;
    }

    /// <summary>Returns the precision of the populated output buffer.</summary>
    /// <returns>Eight, sixteen, or thirty-two bits; otherwise zero.</returns>
    internal int GetBitsPerChannel()
    {
        if (ByteSamples != null && ByteSamples.Length != 0)
        {
            return 8;
        }
        if (UShortSamples != null && UShortSamples.Length != 0)
        {
            return 16;
        }
        if (FloatingSamples != null && FloatingSamples.Length != 0)
        {
            return 32;
        }
        return 0;
    }

    /// <summary>Validates output geometry, channel order, buffer selection, limits, and diagnostics.</summary>
    /// <param name="limits">Resource limits for the rendered image.</param>
    /// <returns>All rendered-image validation findings.</returns>
    internal RawValidationReport Validate(RawLimits? limits)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (!RawModelValidation.ValidImageGeometry(Width, Height, limits) || ChannelCount != 1 && ChannelCount != 3 && ChannelCount != 4)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "RgbImage.Geometry", "Rendered image dimensions or channel count are invalid or exceed limits.");
        }
        long required = RawImageBufferValidation.RequiredSamples(Width, Height, ChannelCount, RowStrideSamples);
        bool bytes = ByteSamples != null && ByteSamples.Length != 0;
        bool words = UShortSamples != null && UShortSamples.Length != 0;
        bool floats = FloatingSamples != null && FloatingSamples.Length != 0;
        int selected = (bytes ? 1 : 0) + (words ? 1 : 0) + (floats ? 1 : 0);
        if (required < 0 || selected != 1 || bytes && (ByteSamples ?? throw new System.InvalidOperationException("A required value is unavailable.")).LongLength < required || words && (UShortSamples ?? throw new System.InvalidOperationException("A required value is unavailable.")).LongLength < required || floats && (FloatingSamples ?? throw new System.InvalidOperationException("A required value is unavailable.")).LongLength < required)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "RgbImage.Samples", "Exactly one output sample array must contain the complete pixel-interleaved image geometry.");
        }
        if (Channels == null || Channels.Length != ChannelCount)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "RgbImage.Channels", "The output channel descriptions must match the channel count.");
        }
        else
        {
            int index = 0;
            while (index < Channels.Length)
            {
                if (Channels[index] == null || Channels[index].Name == null || Channels[index].Name.Length == 0)
                {
                    RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "RgbImage.Channels", "Every output channel requires a name.");
                    break;
                }
                index++;
            }
        }
        if (ColorSpace < RawColorSpace.RawCamera || ColorSpace > RawColorSpace.IccRgb || TransferFunction < RawTransferFunction.Linear || TransferFunction > RawTransferFunction.Icc || Orientation < RawOrientation.Unknown || Orientation > RawOrientation.Rotate270Clockwise)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "RgbImage.Enums", "One or more rendered-image values are not defined.");
        }
        RawImageBufferValidation.ValidateDiagnostics(messages, Diagnostics, "RgbImage.Diagnostics");
        return new RawValidationReport(true, messages.ToArray());
    }
}
