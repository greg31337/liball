namespace raw;

/// <summary>Declares one bad pixel, row, or column in stored image coordinates.</summary>
public class RawDefect
{
    /// <summary>The defect geometry or detected pixel category.</summary>
    public RawDefectKind Kind;
    /// <summary>The declaration source.</summary>
    public RawDefectSource Source;
    /// <summary>The stored column, or <c>-1</c> for a row defect.</summary>
    public int X;
    /// <summary>The stored row, or <c>-1</c> for a column defect.</summary>
    public int Y;
    /// <summary>The logical channel, or <c>-1</c> to affect every channel.</summary>
    public int Channel;

    /// <summary>Creates one sensor defect declaration.</summary>
    /// <param name="kind">The defect category.</param>
    /// <param name="source">The declaration source.</param>
    /// <param name="x">The stored column, or <c>-1</c>.</param>
    /// <param name="y">The stored row, or <c>-1</c>.</param>
    /// <param name="channel">The logical channel, or <c>-1</c>.</param>
    public RawDefect(RawDefectKind kind, RawDefectSource source, int x, int y, int channel)
    {
        Kind = kind;
        Source = source;
        X = x;
        Y = y;
        Channel = channel;
    }

    /// <summary>Validates defect geometry and source against an image.</summary>
    /// <param name="width">The stored image width.</param>
    /// <param name="height">The stored image height.</param>
    /// <param name="channelCount">The logical channel count.</param>
    /// <returns>All defect validation findings.</returns>
    internal RawValidationReport Validate(int width, int height, int channelCount)
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        if (Kind <= RawDefectKind.Unknown || Kind > RawDefectKind.Column || Source <= RawDefectSource.Unknown || Source > RawDefectSource.Detected)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "Defect.Identity", "A defect requires defined kind and source values.");
        }
        if (Channel < -1 || Channel >= channelCount)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "Defect.Channel", "A defect channel must be all channels or one available logical channel.");
        }
        bool pixel = Kind == RawDefectKind.Pixel || Kind == RawDefectKind.HotPixel || Kind == RawDefectKind.DeadPixel;
        bool row = Kind == RawDefectKind.Row;
        bool column = Kind == RawDefectKind.Column;
        if (width <= 0 || height <= 0 || channelCount <= 0 || pixel && (X < 0 || X >= width || Y < 0 || Y >= height) || row && (X != -1 || Y < 0 || Y >= height) || column && (X < 0 || X >= width || Y != -1))
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "Defect.Geometry", "Defect coordinates must match their pixel, row, or column category.");
        }
        if ((Kind == RawDefectKind.HotPixel || Kind == RawDefectKind.DeadPixel) && Source != RawDefectSource.Detected)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "Defect.Source", "Hot and dead categories are reserved for detected defects.");
        }
        if ((Kind == RawDefectKind.Row || Kind == RawDefectKind.Column) && Source == RawDefectSource.Detected)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "Defect.Source", "Automatic detection does not infer whole row or column defects.");
        }
        return new RawValidationReport(true, messages.ToArray());
    }
}

/// <summary>Records repaired samples and the defect declarations applied to an image.</summary>
public class RawDefectMap
{
    /// <summary>The stored image width.</summary>
    public int Width;
    /// <summary>The stored image height.</summary>
    public int Height;
    /// <summary>The logical channel count.</summary>
    public int ChannelCount;
    /// <summary>The distance between repaired-mask row starts in samples.</summary>
    public int RowStrideSamples;
    /// <summary>One byte per sample, set to one when repair replaced that sample.</summary>
    public byte[] RepairedSamples;
    /// <summary>The ordered metadata, caller, and detected defects considered by repair.</summary>
    public RawDefect[] Defects;
    /// <summary>Whether CFA green-equilibrium scaling was applied.</summary>
    public bool GreenEquilibriumApplied;

    /// <summary>Creates a defect-repair map.</summary>
    /// <param name="width">The stored image width.</param>
    /// <param name="height">The stored image height.</param>
    /// <param name="channelCount">The logical channel count.</param>
    /// <param name="rowStrideSamples">The repaired-mask row stride.</param>
    /// <param name="repairedSamples">The repaired sample mask.</param>
    /// <param name="defects">The applied defect declarations.</param>
    /// <param name="greenEquilibriumApplied">Whether green equilibrium was applied.</param>
    public RawDefectMap(int width, int height, int channelCount, int rowStrideSamples, byte[] repairedSamples, RawDefect[] defects, bool greenEquilibriumApplied)
    {
        Width = width;
        Height = height;
        ChannelCount = channelCount;
        RowStrideSamples = rowStrideSamples;
        RepairedSamples = repairedSamples == null ? System.Array.Empty<byte>() : repairedSamples;
        Defects = defects == null ? System.Array.Empty<RawDefect>() : defects;
        GreenEquilibriumApplied = greenEquilibriumApplied;
    }

    /// <summary>Validates repaired-mask storage and every defect declaration.</summary>
    /// <returns>All defect-map validation findings.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = new System.Collections.Generic.List<RawValidationMessage>();
        long required = RawImageBufferValidation.RequiredSamples(Width, Height, ChannelCount, RowStrideSamples);
        if (required < 0 || RepairedSamples == null || RepairedSamples.LongLength < required || Defects == null)
        {
            RawModelValidation.Add(messages, RawDiagnosticCode.InvalidLength, "DefectMap.Storage", "The defect map must contain complete repaired-sample storage and a defect collection.");
            return new RawValidationReport(true, messages.ToArray());
        }
        int y = 0;
        while (y < Height)
        {
            int index = y * RowStrideSamples;
            int end = index + Width * ChannelCount;
            while (index < end)
            {
                if (RepairedSamples[index] > 1)
                {
                    RawModelValidation.Add(messages, RawDiagnosticCode.InvalidDescriptor, "DefectMap.Storage", "Repaired-sample mask values must be zero or one.");
                    break;
                }
                index++;
            }
            y++;
        }
        int defectIndex = 0;
        while (defectIndex < Defects.Length)
        {
            if (Defects[defectIndex] == null)
            {
                RawModelValidation.Add(messages, RawDiagnosticCode.InvalidArgument, "DefectMap.Defects", "Every defect-map entry must be populated.");
            }
            else
            {
                RawModelValidation.AddReport(messages, Defects[defectIndex].Validate(Width, Height, ChannelCount));
            }
            defectIndex++;
        }
        return new RawValidationReport(true, messages.ToArray());
    }
}
