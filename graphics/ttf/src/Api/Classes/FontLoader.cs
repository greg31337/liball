namespace ttf;

/// <summary>Loads supported font containers from files, streams, and memory.</summary>
/// <remarks>
/// File and stream inputs are copied from the stream's current position through
/// end of stream. Caller streams are never closed, retained, rewound, or sought.
/// Array and span inputs are always copied. Read-only memory is retained only
/// when trusted retention is explicitly selected in <see cref="FontLoadOptions" />.
/// </remarks>
public class FontLoader : IFontLoader
{
    /// <summary>Creates a font loader.</summary>
    public FontLoader()
    {
    }

    /// <summary>Loads a font package from a file.</summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="options">The loading policies and limits.</param>
    /// <returns>The loaded font package.</returns>
    public FontPackage LoadFromFile(string path, FontLoadOptions options)
    {
        return LoadFile(path, options);
    }

    /// <summary>Loads a font package from a readable stream.</summary>
    /// <param name="stream">The caller-owned stream to read.</param>
    /// <param name="options">The loading policies and limits.</param>
    /// <returns>The loaded font package.</returns>
    public FontPackage LoadFromStream(
        System.IO.Stream stream,
        FontLoadOptions options)
    {
        return Load(stream, options);
    }

    /// <summary>Loads a font package after copying an array.</summary>
    /// <param name="data">The complete font container bytes.</param>
    /// <param name="options">The loading policies and limits.</param>
    /// <returns>The loaded font package.</returns>
    public FontPackage LoadFromBytes(byte[] data, FontLoadOptions options)
    {
        return Load(data, options);
    }

    /// <summary>Loads a font package from read-only memory.</summary>
    /// <param name="data">The complete font container memory.</param>
    /// <param name="options">The loading policies, limits, and ownership choice.</param>
    /// <returns>The loaded font package.</returns>
    public FontPackage LoadFromMemory(
        ReadOnlyMemory<byte> data,
        FontLoadOptions options)
    {
        return Load(data, options);
    }

    /// <summary>Loads a font package after copying a span.</summary>
    /// <param name="data">The complete font container span.</param>
    /// <param name="options">The loading policies and limits.</param>
    /// <returns>The loaded font package.</returns>
    public FontPackage LoadFromSpan(
        ReadOnlySpan<byte> data,
        FontLoadOptions options)
    {
        return Load(data, options);
    }

    /// <summary>Loads a font package from a file.</summary>
    /// <param name="path">The path of the file to read.</param>
    /// <param name="options">The loading policies and limits.</param>
    /// <returns>The loaded font package.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <c>path</c> or <c>options</c> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException"><c>path</c> is empty.</exception>
    /// <exception cref="System.IO.IOException">The file cannot be read.</exception>
    /// <exception cref="System.UnauthorizedAccessException">File access is denied.</exception>
    /// <exception cref="FontFormatException">The file is not a supported valid font.</exception>
    /// <exception cref="FontLimitException">A configured resource limit is exceeded.</exception>
    internal static FontPackage LoadFile(string path, FontLoadOptions options)
    {
        if (path == null)
        {
            throw new System.ArgumentNullException("path");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (path.Length == 0)
        {
            throw new System.ArgumentException("A font file path cannot be empty.", "path");
        }

        options.GetCancellationToken().ThrowIfCancellationRequested();

        using (System.IO.FileStream stream = System.IO.File.OpenRead(path))
        {
            return LoadStreamData(stream, options);
        }
    }

    /// <summary>Loads a font package from the current position of a readable stream.</summary>
    /// <param name="stream">The caller-owned stream to read through end of stream.</param>
    /// <param name="options">The loading policies and limits.</param>
    /// <returns>The loaded font package.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <c>stream</c> or <c>options</c> is null.
    /// </exception>
    /// <exception cref="System.ArgumentException"><c>stream</c> is not readable.</exception>
    /// <exception cref="System.IO.IOException">The stream cannot be read.</exception>
    /// <exception cref="FontFormatException">The stream does not contain a supported valid font.</exception>
    /// <exception cref="FontLimitException">A configured resource limit is exceeded.</exception>
    internal static FontPackage Load(System.IO.Stream stream, FontLoadOptions options)
    {
        if (stream == null)
        {
            throw new System.ArgumentNullException("stream");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (!stream.CanRead)
        {
            throw new System.ArgumentException("The font stream must be readable.", "stream");
        }

        return LoadStreamData(stream, options);
    }

    /// <summary>Loads a font package from an array after copying its bytes.</summary>
    /// <param name="data">The complete font container bytes.</param>
    /// <param name="options">The loading policies and limits.</param>
    /// <returns>The loaded font package.</returns>
    /// <exception cref="System.ArgumentNullException">
    /// <c>data</c> or <c>options</c> is null.
    /// </exception>
    /// <exception cref="FontFormatException">The bytes do not contain a supported valid font.</exception>
    /// <exception cref="FontLimitException">A configured resource limit is exceeded.</exception>
    internal static FontPackage Load(byte[] data, FontLoadOptions options)
    {
        if (data == null)
        {
            throw new System.ArgumentNullException("data");
        }

        ValidateOptionsAndLength(options, data.Length);
        return LoadData(FontData.Create(data), options);
    }

    /// <summary>Loads a font package from read-only memory.</summary>
    /// <param name="data">The complete font container memory.</param>
    /// <param name="options">The loading policies, limits, and ownership choice.</param>
    /// <returns>The loaded font package.</returns>
    /// <exception cref="System.ArgumentNullException"><c>options</c> is null.</exception>
    /// <exception cref="FontFormatException">The memory does not contain a supported valid font.</exception>
    /// <exception cref="FontLimitException">A configured resource limit is exceeded.</exception>
    internal static FontPackage Load(ReadOnlyMemory<byte> data, FontLoadOptions options)
    {
        ValidateOptionsAndLength(options, data.Length);
        bool retain = options.GetInputOwnership() == FontInputOwnership.RetainReadOnlyMemory;
        return LoadData(FontData.Create(data, retain), options);
    }

    /// <summary>Loads a font package from a span after copying its bytes.</summary>
    /// <param name="data">The complete font container span.</param>
    /// <param name="options">The loading policies and limits.</param>
    /// <returns>The loaded font package.</returns>
    /// <exception cref="System.ArgumentNullException"><c>options</c> is null.</exception>
    /// <exception cref="FontFormatException">The span does not contain a supported valid font.</exception>
    /// <exception cref="FontLimitException">A configured resource limit is exceeded.</exception>
    internal static FontPackage Load(ReadOnlySpan<byte> data, FontLoadOptions options)
    {
        ValidateOptionsAndLength(options, data.Length);
        return LoadData(FontData.Create(data), options);
    }

    static void ValidateOptionsAndLength(FontLoadOptions options, int length)
    {
        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        options.GetCancellationToken().ThrowIfCancellationRequested();

        if (length > options.GetLimits().GetMaxInputBytes())
        {
            throw FontLimitException.Create("The font input exceeds the configured input limit.");
        }
    }

    static FontPackage LoadStreamData(
        System.IO.Stream stream,
        FontLoadOptions options)
    {
        options.GetCancellationToken().ThrowIfCancellationRequested();
        int maximumLength = options.GetLimits().GetMaxInputBytes();
        int bufferLength = maximumLength < 81920 ? maximumLength : 81920;
        byte[] buffer = new byte[bufferLength];

        using (System.IO.MemoryStream output = new System.IO.MemoryStream())
        {
            while (true)
            {
                options.GetCancellationToken().ThrowIfCancellationRequested();
                int remaining = maximumLength - (int)output.Length;
                int requestedLength = remaining == 0
                    ? 1
                    : (remaining < buffer.Length ? remaining : buffer.Length);
                int readLength = stream.Read(buffer, 0, requestedLength);

                if (readLength == 0)
                {
                    break;
                }

                if (remaining == 0)
                {
                    throw FontLimitException.Create("The font stream exceeds the configured input limit.");
                }

                output.Write(buffer, 0, readLength);
            }

            byte[] ownedBytes = output.ToArray();
            FontData fontData = FontData.Create(new ReadOnlyMemory<byte>(ownedBytes), true);
            return LoadData(fontData, options);
        }
    }

    static FontPackage LoadData(FontData originalData, FontLoadOptions options)
    {
        options.GetCancellationToken().ThrowIfCancellationRequested();
        ContainerSignatureKind signature = ContainerDetector.Detect(
            originalData.GetMemory().Span);

        if (!ContainerDetector.IsSupported(signature))
        {
            if (signature == ContainerSignatureKind.Unknown)
            {
                throw FontFormatException.CreateDiagnostic(
                    "The input does not identify a known font container.",
                    FontDiagnosticCode.UnknownContainer);
            }

            throw FontFormatException.CreateDiagnostic(
                "The recognized font container is not supported.",
                FontDiagnosticCode.UnsupportedContainer);
        }

        FontContainerKind sourceKind = ContainerDetector.GetContainerKind(signature);
        FontData sfntData;
        Woff1Font? woff1 = null;
        Woff2Font? woff2 = null;
        System.Collections.Generic.List<FontDiagnostic> diagnostics =
            new System.Collections.Generic.List<FontDiagnostic>();

        if (sourceKind == FontContainerKind.Sfnt ||
            sourceKind == FontContainerKind.Collection)
        {
            sfntData = originalData;
        }
        else if (sourceKind == FontContainerKind.Woff1)
        {
            woff1 = Woff1Parser.Parse(originalData, options);
            sfntData = woff1.GetReconstructedSfntData();
            CopyWoff1Diagnostics(woff1, diagnostics, options.GetLimits());
        }
        else
        {
            woff2 = Woff2Parser.Parse(originalData, options);
            Woff2Reconstruction reconstruction = Woff2TransformReconstructor.Reconstruct(
                woff2,
                options);
            sfntData = reconstruction.GetData();
        }

        int[] faceOffsets = ValidateNormalizedSfnt(
            sfntData,
            options,
            diagnostics);
        FontContainerProvenance provenance = FontContainerProvenanceBuilder.Create(
            sourceKind,
            originalData,
            sfntData,
            faceOffsets,
            options,
            woff1,
            woff2);
        return new FontPackage(
            sourceKind,
            originalData,
            sfntData,
            faceOffsets,
            diagnostics.ToArray(),
            options,
            provenance);
    }

    static void CopyWoff1Diagnostics(
        Woff1Font font,
        System.Collections.Generic.List<FontDiagnostic> diagnostics,
        FontLimits limits)
    {
        int index = 0;

        while (index < font.GetDiagnosticCount() &&
            diagnostics.Count < limits.GetMaxDiagnostics())
        {
            diagnostics.Add(font.GetDiagnostic(index));
            index += 1;
        }
    }

    static int[] ValidateNormalizedSfnt(
        FontData sfntData,
        FontLoadOptions options,
        System.Collections.Generic.List<FontDiagnostic> diagnostics)
    {
        ContainerSignatureKind signature = ContainerDetector.Detect(sfntData.GetMemory().Span);

        if (signature == ContainerSignatureKind.Collection)
        {
            SfntCollection collection = SfntCollectionParser.Parse(sfntData, options);
            int[] faceOffsets = new int[collection.GetFaceCount()];
            int faceIndex = 0;

            while (faceIndex < collection.GetFaceCount())
            {
                options.GetCancellationToken().ThrowIfCancellationRequested();
                SfntCollectionFace face = collection.GetFace(faceIndex);
                SfntValidationResult validation = SfntDirectoryValidator.Validate(
                    sfntData,
                    face.GetDirectory(),
                    options,
                    faceIndex);
                HandleValidationResult(validation, diagnostics, options);
                faceOffsets[faceIndex] = (int)face.GetDirectoryOffset();
                faceIndex += 1;
            }

            return faceOffsets;
        }

        SfntDirectory directory = SfntDirectoryParser.Parse(
            sfntData.GetSlice(0, sfntData.GetLength()),
            options.GetLimits());
        SfntValidationResult scalarValidation = SfntDirectoryValidator.Validate(
            sfntData,
            directory,
            options,
            0);
        HandleValidationResult(scalarValidation, diagnostics, options);
        return new int[] { 0 };
    }

    static void HandleValidationResult(
        SfntValidationResult validation,
        System.Collections.Generic.List<FontDiagnostic> diagnostics,
        FontLoadOptions options)
    {
        int index = 0;

        while (index < validation.GetDiagnosticCount() &&
            diagnostics.Count < options.GetLimits().GetMaxDiagnostics())
        {
            diagnostics.Add(validation.GetDiagnostic(index));
            index += 1;
        }

        if (!validation.IsValid() && options.GetMode() != FontLoadMode.InspectOnly)
        {
            FontDiagnosticCode diagnosticCode =
                FindFirstFailureCode(validation);
            throw FontFormatException.CreateDiagnostic(
                "The normalized SFNT directory failed structural validation.",
                diagnosticCode);
        }
    }

    static FontDiagnosticCode FindFirstFailureCode(
        SfntValidationResult validation)
    {
        int index = 0;

        while (index < validation.GetDiagnosticCount())
        {
            FontDiagnostic diagnostic = validation.GetDiagnostic(index);

            if (diagnostic.GetSeverity() == FontDiagnosticSeverity.Error ||
                diagnostic.GetSeverity() == FontDiagnosticSeverity.Fatal)
            {
                return diagnostic.GetCode();
            }

            index += 1;
        }

        return FontDiagnosticCode.None;
    }
}
