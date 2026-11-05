namespace gif;

internal class GifIdentifier
{
    internal GifInfo IdentifyBytes(byte[] data)
    {
        return IdentifyBytesWithOptions(data, new GifDecodeOptions());
    }

    internal GifInfo IdentifyBytesWithOptions(byte[] data, GifDecodeOptions? options)
    {
        GifDecodeOptions activeOptions = CreateMetadataOnlyOptions(options);
        GifBlockReader reader = new GifBlockReader();
        GifBlockReadResult result = reader.ReadBytesWithOptions(data, activeOptions);
        return CreateInfo(result);
    }

    internal GifInfo IdentifyMemoryWithOptions(System.ReadOnlyMemory<byte> data, GifDecodeOptions? options)
    {
        GifDecodeOptions activeOptions = CreateMetadataOnlyOptions(options);
        GifBlockReader reader = new GifBlockReader();
        GifBlockReadResult result = reader.ReadMemoryWithOptions(data, activeOptions);
        return CreateInfo(result);
    }

    internal GifInfo IdentifyStream(System.IO.Stream stream)
    {
        return IdentifyStreamWithOptions(stream, new GifDecodeOptions());
    }

    internal GifInfo IdentifyStreamWithOptions(System.IO.Stream stream, GifDecodeOptions? options)
    {
        GifDecodeOptions activeOptions = CreateMetadataOnlyOptions(options);
        GifBlockReader reader = new GifBlockReader();
        GifBlockReadResult result = reader.ReadStreamWithOptions(stream, activeOptions);
        return CreateInfo(result);
    }

    internal GifInfo CreateInfo(GifBlockReadResult result)
    {
        GifInfo info = new GifInfo();
        if (result == null)
        {
            info.Error = new GifErrorDiagnostic(GifErrorCode.UnexpectedEndOfStream, "Missing GIF identification result.", new GifDiagnosticPosition(-1, -1, "Identify"));
            return info;
        }

        info.Success = result.Success;
        info.Error = result.Error;
        if (result.Document == null)
        {
            return info;
        }

        GifDocument document = result.Document;
        info.Version = document.Version;
        info.Width = document.LogicalScreen.Width;
        info.Height = document.LogicalScreen.Height;
        info.FrameCount = result.FrameCount;
        info.HasAnimation = result.FrameCount > 1;
        info.HasTransparency = HasTransparentFrame(document.Frames);
        info.LoopCount = document.LoopCount;
        info.HasComments = document.Comments.Length > 0;
        info.ApplicationExtensionIdentifiers = CreateApplicationIdentifiers(document.ApplicationExtensions);
        info.EstimatedDecodedBytes = CalculateEstimatedDecodedBytes(document.Frames);
        info.Warnings = document.Warnings;
        return info;
    }

    internal GifDecodeOptions CreateMetadataOnlyOptions(GifDecodeOptions? options)
    {
        GifDecodeOptions? source = options;
        if (source == null)
        {
            source = new GifDecodeOptions();
        }

        GifDecodeOptions copy = new GifDecodeOptions();
        copy.Strictness = source.Strictness;
        copy.MaxWidth = source.MaxWidth;
        copy.MaxHeight = source.MaxHeight;
        copy.MaxFrames = source.MaxFrames;
        copy.MaxDecodedPixels = source.MaxDecodedPixels;
        copy.MaxBytes = source.MaxBytes;
        copy.MaxCompressedBytes = source.MaxCompressedBytes;
        copy.MaxSubBlocks = source.MaxSubBlocks;
        copy.MaxSubBlockBytes = source.MaxSubBlockBytes;
        copy.MaxMemoryBytes = source.MaxMemoryBytes;
        copy.MetadataOnly = true;
        copy.FirstFrameOnly = source.FirstFrameOnly;
        copy.PreserveUnknownExtensions = source.PreserveUnknownExtensions;
        copy.PlainTextExtensionPolicy = source.PlainTextExtensionPolicy;
        copy.TolerateMalformedStreams = source.TolerateMalformedStreams;
        copy.DefaultGlobalColorTable = CopyBytes(source.DefaultGlobalColorTable);
        copy.DisposalFallback = source.DisposalFallback;
        copy.MinimumFrameDelayMilliseconds = source.MinimumFrameDelayMilliseconds;
        copy.BackgroundMode = source.BackgroundMode;
        copy.SolidBackgroundColor = CopyRgbaColor(source.SolidBackgroundColor);
        return copy;
    }

    internal GifRgbaColor CopyRgbaColor(GifRgbaColor color)
    {
        if (color == null)
        {
            return new GifRgbaColor(0, 0, 0, 255);
        }

        return new GifRgbaColor(color.Red, color.Green, color.Blue, color.Alpha);
    }

    internal byte[] CopyBytes(byte[] data)
    {
        if (data == null)
        {
            return new byte[0];
        }

        byte[] copy = new byte[data.Length];
        int index = 0;
        while (index < data.Length)
        {
            copy[index] = data[index];
            index = index + 1;
        }

        return copy;
    }

    internal string[] CreateApplicationIdentifiers(GifApplicationExtension[] applications)
    {
        if (applications == null)
        {
            return new string[0];
        }

        int count = 0;
        int index = 0;
        while (index < applications.Length)
        {
            if (HasApplicationIdentifier(applications[index]))
            {
                count = count + 1;
            }

            index = index + 1;
        }

        string[] identifiers = new string[count];
        int outputIndex = 0;
        index = 0;
        while (index < applications.Length)
        {
            if (HasApplicationIdentifier(applications[index]))
            {
                identifiers[outputIndex] = applications[index].Identifier;
                outputIndex = outputIndex + 1;
            }

            index = index + 1;
        }

        return identifiers;
    }

    internal bool HasApplicationIdentifier(GifApplicationExtension application)
    {
        if (application == null)
        {
            return false;
        }

        if (application.Identifier == null)
        {
            return false;
        }

        return application.Identifier.Length > 0;
    }

    internal bool HasTransparentFrame(GifFrame[] frames)
    {
        if (frames == null)
        {
            return false;
        }

        int index = 0;
        while (index < frames.Length)
        {
            if (frames[index] != null)
            {
                if (frames[index].HasTransparency)
                {
                    return true;
                }
            }

            index = index + 1;
        }

        return false;
    }

    internal long CalculateEstimatedDecodedBytes(GifFrame[] frames)
    {
        if (frames == null)
        {
            return 0;
        }

        long total = 0;
        int index = 0;
        while (index < frames.Length)
        {
            if (frames[index] != null)
            {
                long framePixels = (long)frames[index].Width * (long)frames[index].Height;
                long frameBytes = GifMath.MultiplySaturating(framePixels, 4);
                total = GifMath.AddSaturating(total, frameBytes);
            }

            index = index + 1;
        }

        return total;
    }
}
