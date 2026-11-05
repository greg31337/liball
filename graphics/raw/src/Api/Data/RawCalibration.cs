namespace raw;

/// <summary>Describes absolute and row or column relative black-level calibration.</summary>
public class RawBlackLevel
{
    /// <summary>The horizontal repeat size of absolute values.</summary>
    public int RepeatWidth;
    /// <summary>The vertical repeat size of absolute values.</summary>
    public int RepeatHeight;
    /// <summary>The number of calibrated logical channels.</summary>
    public int ChannelCount;
    /// <summary>Absolute values ordered by repeat row, repeat column, then channel.</summary>
    public double[] Values;
    /// <summary>Optional additive delta for every stored row.</summary>
    public double[] RowDeltas;
    /// <summary>Optional additive delta for every stored column.</summary>
    public double[] ColumnDeltas;

    /// <summary>Creates black-level calibration.</summary>
    /// <param name="repeatWidth">The horizontal repeat size.</param>
    /// <param name="repeatHeight">The vertical repeat size.</param>
    /// <param name="channelCount">The calibrated channel count.</param>
    /// <param name="values">Absolute repeated values.</param>
    /// <param name="rowDeltas">Optional row deltas.</param>
    /// <param name="columnDeltas">Optional column deltas.</param>
    public RawBlackLevel(int repeatWidth, int repeatHeight, int channelCount, double[] values, double[]? rowDeltas, double[]? columnDeltas)
    {
        RepeatWidth = repeatWidth;
        RepeatHeight = repeatHeight;
        ChannelCount = channelCount;
        Values = values == null ? System.Array.Empty<double>() : values;
        RowDeltas = rowDeltas == null ? System.Array.Empty<double>() : rowDeltas;
        ColumnDeltas = columnDeltas == null ? System.Array.Empty<double>() : columnDeltas;
    }

    /// <summary>Creates one scalar black level shared by all pixels and channels.</summary>
    /// <param name="value">The scalar black level.</param>
    /// <param name="channelCount">The image channel count.</param>
    /// <returns>Scalar calibration expanded across logical channels.</returns>
    internal static RawBlackLevel CreateScalar(double value, int channelCount)
    {
        if (channelCount < 1 || channelCount > 64)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Scalar black-level channel count must be between one and sixty-four.", -1);
        }
        double[] values = new double[channelCount];
        int index = 0;
        while (index < values.Length)
        {
            values[index] = value;
            index++;
        }
        return new RawBlackLevel(1, 1, channelCount, values, null, null);
    }

    /// <summary>Creates black levels that differ only by logical channel.</summary>
    /// <param name="values">One value per logical channel.</param>
    /// <returns>Per-channel black-level calibration.</returns>
    internal static RawBlackLevel CreatePerChannel(double[] values)
    {
        double[] retained = values == null ? System.Array.Empty<double>() : values;
        return new RawBlackLevel(1, 1, retained.Length, retained, null, null);
    }

    /// <summary>Returns the calibrated black level at one stored sample coordinate.</summary>
    /// <param name="x">The stored column.</param>
    /// <param name="y">The stored row.</param>
    /// <param name="channel">The logical channel.</param>
    /// <returns>The absolute repeated value plus row and column deltas.</returns>
    /// <exception cref="RawFormatException">The calibration or coordinate is invalid.</exception>
    internal double GetValue(int x, int y, int channel)
    {
        long expected = -1;
        if (RepeatWidth > 0 && RepeatHeight > 0 && ChannelCount > 0 && RepeatWidth <= long.MaxValue / RepeatHeight && (long)RepeatWidth * RepeatHeight <= long.MaxValue / ChannelCount)
        {
            expected = (long)RepeatWidth * RepeatHeight * ChannelCount;
        }
        if (x < 0 || y < 0 || channel < 0 || channel >= ChannelCount || expected < 0 || Values == null || Values.LongLength != expected || !AllFinite(Values) || RowDeltas == null || ColumnDeltas == null || RowDeltas.Length != 0 && y >= RowDeltas.Length || ColumnDeltas.Length != 0 && x >= ColumnDeltas.Length || !AllFinite(RowDeltas) || !AllFinite(ColumnDeltas))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "Valid black-level calibration and sample coordinates are required.", -1);
        }
        return GetValueUnchecked(x, y, channel);
    }

    internal double GetValueUnchecked(int x, int y, int channel)
    {
        int repeatX = x % RepeatWidth;
        int repeatY = y % RepeatHeight;
        double value = Values[(repeatY * RepeatWidth + repeatX) * ChannelCount + channel];
        if (RowDeltas.Length != 0)
        {
            value += RowDeltas[y];
        }
        if (ColumnDeltas.Length != 0)
        {
            value += ColumnDeltas[x];
        }
        return value;
    }

    /// <summary>Validates calibration against stored image geometry and channels.</summary>
    /// <param name="imageWidth">The stored image width.</param>
    /// <param name="imageHeight">The stored image height.</param>
    /// <param name="channelCount">The image channel count.</param>
    /// <returns>All black-level validation findings.</returns>
    internal RawValidationReport Validate(int imageWidth, int imageHeight, int channelCount)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        long expected = -1;
        if (RepeatWidth > 0 && RepeatHeight > 0 && ChannelCount > 0 && RepeatWidth <= long.MaxValue / RepeatHeight && (long)RepeatWidth * RepeatHeight <= long.MaxValue / ChannelCount)
        {
            expected = (long)RepeatWidth * RepeatHeight * ChannelCount;
        }
        if (imageWidth <= 0 || imageHeight <= 0 || channelCount <= 0 || ChannelCount != channelCount || RepeatWidth > imageWidth || RepeatHeight > imageHeight)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "BlackLevel.Geometry", "Black-level repeat and channel geometry must fit the image.");
        }
        if (Values == null || expected < 0 || Values.LongLength != expected || !AllFinite(Values))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "BlackLevel.Values", "Black-level values must be finite and match repeat and channel geometry.");
        }
        if (RowDeltas == null || RowDeltas.Length != 0 && RowDeltas.Length != imageHeight || RowDeltas != null && !AllFinite(RowDeltas))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "BlackLevel.RowDeltas", "Row deltas must be empty or contain one finite value per image row.");
        }
        if (ColumnDeltas == null || ColumnDeltas.Length != 0 && ColumnDeltas.Length != imageWidth || ColumnDeltas != null && !AllFinite(ColumnDeltas))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "BlackLevel.ColumnDeltas", "Column deltas must be empty or contain one finite value per image column.");
        }
        return new RawValidationReport(true, messages.ToArray());
    }

    internal static bool AllFinite(double[] values)
    {
        if (values == null)
        {
            return false;
        }
        int index = 0;
        while (index < values.Length)
        {
            if (!RawModelValidation.IsFinite(values[index]))
            {
                return false;
            }
            index++;
        }
        return true;
    }
}

/// <summary>Describes one set of white-balance multipliers or temperature and tint coordinates.</summary>
public class RawWhiteBalance
{
    /// <summary>The semantic source of the white balance.</summary>
    public RawWhiteBalanceMode Source;
    /// <summary>Positive logical-channel multipliers in image channel order.</summary>
    public double[] Multipliers;
    /// <summary>The correlated color temperature in kelvin, or zero when not supplied.</summary>
    public double TemperatureKelvin;
    /// <summary>The green-axis tint adjustment in exposure stops; positive values increase the green multiplier.</summary>
    public double Tint;

    /// <summary>Creates white-balance calibration.</summary>
    /// <param name="source">The semantic white-balance source.</param>
    /// <param name="multipliers">Logical-channel multipliers.</param>
    /// <param name="temperatureKelvin">The correlated color temperature, or zero.</param>
    /// <param name="tint">The green-axis tint adjustment in exposure stops.</param>
    public RawWhiteBalance(RawWhiteBalanceMode source, double[]? multipliers, double temperatureKelvin, double tint)
    {
        Source = source;
        Multipliers = multipliers == null ? System.Array.Empty<double>() : multipliers;
        TemperatureKelvin = temperatureKelvin;
        Tint = tint;
    }

    /// <summary>Returns an independent multiplier array normalized so its smallest value is one.</summary>
    /// <returns>Normalized multipliers, or an empty array when none are present.</returns>
    /// <exception cref="RawFormatException">A multiplier is non-finite or not positive.</exception>
    internal double[] GetNormalizedMultipliers()
    {
        if (Multipliers == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "White-balance multipliers cannot be null.", -1);
        }
        if (Multipliers.Length == 0)
        {
            return System.Array.Empty<double>();
        }
        double minimum = double.MaxValue;
        int index = 0;
        while (index < Multipliers.Length)
        {
            if (!RawModelValidation.IsFinite(Multipliers[index]) || Multipliers[index] <= 0.0)
            {
                throw new RawFormatException(RawDiagnosticCode.InvalidDescriptor, "White-balance multipliers must be finite and positive.", -1);
            }
            if (Multipliers[index] < minimum)
            {
                minimum = Multipliers[index];
            }
            index++;
        }
        double[] normalized = new double[Multipliers.Length];
        index = 0;
        while (index < Multipliers.Length)
        {
            normalized[index] = Multipliers[index] / minimum;
            index++;
        }
        return normalized;
    }

    /// <summary>Validates white-balance data against a logical channel count.</summary>
    /// <param name="channelCount">The image channel count.</param>
    /// <returns>All white-balance validation findings.</returns>
    internal RawValidationReport Validate(int channelCount)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (Source < RawWhiteBalanceMode.None || Source > RawWhiteBalanceMode.TemperatureAndTint)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "WhiteBalance.Source", "The white-balance source is not defined.");
        }
        if (Multipliers == null || Multipliers.Length != 0 && Multipliers.Length != channelCount)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "WhiteBalance.Multipliers", "White-balance multipliers must be empty or match the image channel count.");
        }
        else
        {
            int index = 0;
            while (index < Multipliers.Length)
            {
                if (!RawModelValidation.IsFinite(Multipliers[index]) || Multipliers[index] <= 0.0)
                {
                    RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "WhiteBalance.Multipliers", "Every white-balance multiplier must be finite and positive.");
                    break;
                }
                index++;
            }
        }
        if (Source == RawWhiteBalanceMode.UserMultipliers && (Multipliers == null || Multipliers.Length != channelCount))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "WhiteBalance.Multipliers", "User-multiplier white balance requires one multiplier per channel.");
        }
        if (!RawModelValidation.IsFinite(TemperatureKelvin) || !RawModelValidation.IsFinite(Tint) || TemperatureKelvin < 0.0 || TemperatureKelvin != 0.0 && (TemperatureKelvin < 1000.0 || TemperatureKelvin > 50000.0))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "WhiteBalance.Temperature", "White-balance temperature and tint must be finite and a supplied temperature must be between 1000 and 50000 kelvin.");
        }
        if (Source == RawWhiteBalanceMode.TemperatureAndTint && TemperatureKelvin == 0.0)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "WhiteBalance.Temperature", "Temperature-and-tint white balance requires a correlated color temperature.");
        }
        return new RawValidationReport(true, messages.ToArray());
    }
}

/// <summary>Describes one matrix or embedded-ICC camera color calibration.</summary>
public class RawColorProfile
{
    /// <summary>The stable profile name.</summary>
    public string Name;
    /// <summary>The calibration illuminant identity.</summary>
    public RawColorIlluminant Illuminant;
    /// <summary>The custom illuminant temperature in kelvin, or zero.</summary>
    public double IlluminantTemperatureKelvin;
    /// <summary>A row-major three-by-three camera-to-XYZ color matrix, or an empty array.</summary>
    public double[] ColorMatrix;
    /// <summary>A row-major three-by-three XYZ-to-camera reduction matrix, or an empty array.</summary>
    public double[] ReductionMatrix;
    /// <summary>A row-major three-by-three camera calibration matrix, or an empty array.</summary>
    public double[] CameraCalibration;
    /// <summary>A row-major three-by-three camera-to-XYZ forward matrix, or an empty array.</summary>
    public double[] ForwardMatrix;
    /// <summary>The XYZ profile white point, or an empty array.</summary>
    public double[] WhitePoint;
    /// <summary>An exact embedded ICC profile, or an empty array.</summary>
    public byte[] IccProfile;

    /// <summary>Creates camera color calibration.</summary>
    /// <param name="name">The stable profile name.</param>
    /// <param name="illuminant">The calibration illuminant.</param>
    /// <param name="illuminantTemperatureKelvin">The custom illuminant temperature, or zero.</param>
    /// <param name="colorMatrix">The camera-to-XYZ matrix.</param>
    /// <param name="reductionMatrix">The reduction matrix.</param>
    /// <param name="cameraCalibration">The camera calibration matrix.</param>
    /// <param name="forwardMatrix">The forward matrix.</param>
    /// <param name="whitePoint">The XYZ white point.</param>
    /// <param name="iccProfile">The exact embedded ICC profile.</param>
    public RawColorProfile(string name, RawColorIlluminant illuminant, double illuminantTemperatureKelvin, double[]? colorMatrix, double[]? reductionMatrix, double[]? cameraCalibration, double[]? forwardMatrix, double[]? whitePoint, byte[]? iccProfile)
    {
        Name = name == null ? string.Empty : name;
        Illuminant = illuminant;
        IlluminantTemperatureKelvin = illuminantTemperatureKelvin;
        ColorMatrix = colorMatrix == null ? System.Array.Empty<double>() : colorMatrix;
        ReductionMatrix = reductionMatrix == null ? System.Array.Empty<double>() : reductionMatrix;
        CameraCalibration = cameraCalibration == null ? System.Array.Empty<double>() : cameraCalibration;
        ForwardMatrix = forwardMatrix == null ? System.Array.Empty<double>() : forwardMatrix;
        WhitePoint = whitePoint == null ? System.Array.Empty<double>() : whitePoint;
        IccProfile = iccProfile == null ? System.Array.Empty<byte>() : iccProfile;
    }

    /// <summary>Returns whether at least one camera color matrix is present.</summary>
    /// <returns><c>true</c> when matrix calibration is available.</returns>
    internal bool HasMatrixProfile()
    {
        return ColorMatrix != null && ColorMatrix.Length == 9 || ForwardMatrix != null && ForwardMatrix.Length == 9 || ReductionMatrix != null && ReductionMatrix.Length == 9;
    }

    /// <summary>Returns whether embedded ICC bytes are present.</summary>
    /// <returns><c>true</c> when an ICC profile is available.</returns>
    internal bool HasIccProfile()
    {
        return IccProfile != null && IccProfile.Length != 0;
    }

    /// <summary>Validates profile identity, illuminant, matrices, white point, and ICC bytes.</summary>
    /// <returns>All color-profile validation findings.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (Name == null || Name.Length == 0)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidProfile, "ColorProfile.Name", "A color profile requires a stable name.");
        }
        if (Illuminant <= RawColorIlluminant.Unknown || Illuminant > RawColorIlluminant.D60 || !RawModelValidation.IsFinite(IlluminantTemperatureKelvin) || IlluminantTemperatureKelvin < 0.0 || Illuminant == RawColorIlluminant.Custom && (IlluminantTemperatureKelvin < 1000.0 || IlluminantTemperatureKelvin > 50000.0))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidProfile, "ColorProfile.Illuminant", "The profile illuminant and optional custom temperature are invalid.");
        }
        ValidateMatrix(messages, ColorMatrix, "ColorProfile.ColorMatrix");
        ValidateMatrix(messages, ReductionMatrix, "ColorProfile.ReductionMatrix");
        ValidateMatrix(messages, CameraCalibration, "ColorProfile.CameraCalibration");
        ValidateMatrix(messages, ForwardMatrix, "ColorProfile.ForwardMatrix");
        if (WhitePoint == null || WhitePoint.Length != 0 && WhitePoint.Length != 3 || WhitePoint != null && !RawBlackLevel.AllFinite(WhitePoint))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidProfile, "ColorProfile.WhitePoint", "The white point must be empty or contain three finite XYZ values.");
        }
        if (IccProfile == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidProfile, "ColorProfile.Icc", "The ICC profile byte collection cannot be null.");
        }
        if (!HasMatrixProfile() && !HasIccProfile())
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidProfile, "ColorProfile.Data", "A color profile requires matrix calibration or embedded ICC bytes.");
        }
        return new RawValidationReport(true, messages.ToArray());
    }

    internal static void ValidateMatrix(System.Collections.Generic.List<RawValidationMessage> messages, double[] matrix, string context)
    {
        if (matrix == null || matrix.Length != 0 && matrix.Length != 9 || matrix != null && !RawBlackLevel.AllFinite(matrix))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidProfile, context, "A color matrix must be empty or contain nine finite row-major values.");
        }
    }
}
