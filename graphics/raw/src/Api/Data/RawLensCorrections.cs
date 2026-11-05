namespace raw;

/// <summary>Describes one bilinearly interpolated sensor gain grid over a stored image area.</summary>
public class RawGainMap
{
    /// <summary>The left stored coordinate covered by the map.</summary>
    public int X;
    /// <summary>The top stored coordinate covered by the map.</summary>
    public int Y;
    /// <summary>The covered stored width.</summary>
    public int Width;
    /// <summary>The covered stored height.</summary>
    public int Height;
    /// <summary>The horizontal gain-grid point count.</summary>
    public int MapWidth;
    /// <summary>The vertical gain-grid point count.</summary>
    public int MapHeight;
    /// <summary>The number of gain planes.</summary>
    public int PlaneCount;
    /// <summary>Positive gain values in grid-point-major, then plane order.</summary>
    public double[] Gains;

    /// <summary>Creates one sensor gain map.</summary>
    /// <param name="x">The left stored coordinate.</param>
    /// <param name="y">The top stored coordinate.</param>
    /// <param name="width">The covered stored width.</param>
    /// <param name="height">The covered stored height.</param>
    /// <param name="mapWidth">The horizontal grid point count.</param>
    /// <param name="mapHeight">The vertical grid point count.</param>
    /// <param name="planeCount">The gain plane count.</param>
    /// <param name="gains">Grid-point-major gain values.</param>
    public RawGainMap(int x, int y, int width, int height, int mapWidth, int mapHeight, int planeCount, double[] gains)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        MapWidth = mapWidth;
        MapHeight = mapHeight;
        PlaneCount = planeCount;
        Gains = gains == null ? System.Array.Empty<double>() : gains;
    }

    /// <summary>Returns whether a stored coordinate is covered by the map.</summary>
    /// <param name="x">The stored column.</param>
    /// <param name="y">The stored row.</param>
    /// <returns><c>true</c> when the coordinate is covered.</returns>
    internal bool Contains(int x, int y)
    {
        return x >= X && y >= Y && (long)x < (long)X + Width && (long)y < (long)Y + Height;
    }

    /// <summary>Validates geometry, plane mapping, and positive finite gains against an image.</summary>
    /// <param name="info">The sensor image metadata.</param>
    /// <returns>All gain-map validation findings.</returns>
    internal RawValidationReport Validate(RawImageInfo info)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (info == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "GainMap.Info", "Image metadata is required for gain-map validation.");
            return new RawValidationReport(true, messages.ToArray());
        }
        if (X < 0 || Y < 0 || Width <= 0 || Height <= 0 || (long)X + Width > info.Width || (long)Y + Height > info.Height || MapWidth <= 0 || MapHeight <= 0)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "GainMap.Geometry", "The gain-map area and grid must be nonempty and contained in the stored image.");
        }
        int channelCount = info.GetChannelCount();
        int cfaPlaneCount = info.Cfa == null ? 0 : info.Cfa.RepeatWidth * info.Cfa.RepeatHeight;
        if (PlaneCount <= 0 || PlaneCount != 1 && PlaneCount != channelCount && PlaneCount != cfaPlaneCount)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "GainMap.Planes", "Gain planes must be scalar, per logical channel, or per CFA phase.");
        }
        long gridPoints = MapWidth > 0 && MapHeight > 0 && MapWidth <= long.MaxValue / MapHeight ? (long)MapWidth * MapHeight : -1;
        long expected = gridPoints > 0 && PlaneCount > 0 && gridPoints <= long.MaxValue / PlaneCount ? gridPoints * PlaneCount : -1;
        if (Gains == null || expected <= 0 || expected > int.MaxValue || Gains.LongLength != expected)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "GainMap.Gains", "The gain collection must exactly match the grid and plane geometry.");
        }
        else
        {
            int index = 0;
            while (index < Gains.Length)
            {
                if (!RawModelValidation.IsFinite(Gains[index]) || Gains[index] <= 0.0)
                {
                    RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "GainMap.Gains", "Every sensor gain must be finite and positive.");
                    break;
                }
                index++;
            }
        }
        return new RawValidationReport(true, messages.ToArray());
    }
}

/// <summary>Describes radial red and blue source-coordinate scaling before demosaic.</summary>
public class RawChromaticAberration
{
    /// <summary>The optical center column in stored coordinates.</summary>
    public double CenterX;
    /// <summary>The optical center row in stored coordinates.</summary>
    public double CenterY;
    /// <summary>The red source-coordinate radius multiplier.</summary>
    public double RedScale;
    /// <summary>The blue source-coordinate radius multiplier.</summary>
    public double BlueScale;

    /// <summary>Creates radial raw chromatic-aberration calibration.</summary>
    /// <param name="centerX">The optical center column.</param>
    /// <param name="centerY">The optical center row.</param>
    /// <param name="redScale">The red source-coordinate radius multiplier.</param>
    /// <param name="blueScale">The blue source-coordinate radius multiplier.</param>
    public RawChromaticAberration(double centerX, double centerY, double redScale, double blueScale)
    {
        CenterX = centerX;
        CenterY = centerY;
        RedScale = redScale;
        BlueScale = blueScale;
    }

    /// <summary>Validates the optical center, scales, and required CFA colors.</summary>
    /// <param name="info">The sensor image metadata.</param>
    /// <returns>All chromatic-aberration validation findings.</returns>
    internal RawValidationReport Validate(RawImageInfo info)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (info == null || info.Cfa == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "ChromaticAberration.Cfa", "Raw chromatic-aberration scaling requires CFA image metadata.");
            return new RawValidationReport(true, messages.ToArray());
        }
        if (!RawModelValidation.IsFinite(CenterX) || !RawModelValidation.IsFinite(CenterY) || CenterX < 0.0 || CenterY < 0.0 || CenterX > info.Width - 1 || CenterY > info.Height - 1)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "ChromaticAberration.Center", "The optical center must be finite and contained in the stored image.");
        }
        if (!RawModelValidation.IsFinite(RedScale) || !RawModelValidation.IsFinite(BlueScale) || RedScale <= 0.0 || BlueScale <= 0.0)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "ChromaticAberration.Scales", "Red and blue radius multipliers must be finite and positive.");
        }
        bool red = false;
        bool green = false;
        bool blue = false;
        int index = 0;
        while (index < info.Cfa.Colors.Length)
        {
            RawCfaColor color = info.Cfa.Colors[index];
            red = red || color == RawCfaColor.Red;
            green = green || color == RawCfaColor.Green || color == RawCfaColor.Green2;
            blue = blue || color == RawCfaColor.Blue;
            index++;
        }
        if (!red || !green || !blue)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "ChromaticAberration.Colors", "Raw chromatic-aberration scaling requires red, green, and blue CFA phases.");
        }
        return new RawValidationReport(true, messages.ToArray());
    }
}
