namespace ttf;

/// <summary>Represents one loaded scalar font or font collection package.</summary>
/// <remarks>
/// Original container bytes and normalized SFNT data are immutable package
/// state. Faces are created lazily and exactly once per index. Concurrent calls
/// for the same face return the same immutable <see cref="FontFace" /> instance.
/// </remarks>
public class FontPackage : IFontFaceCollection, IFontPackage
{
    FontContainerKind sourceKind;
    FontData originalData;
    FontData sfntData;
    int[] faceOffsets;
    FontDiagnostic[] diagnostics;
    FontLoadOptions options;
    FontContainerProvenance provenance;
    FontFace[] faces;
    object[] faceLocks;
    object diagnosticLock;
    object creationCountLock;
    FontDiagnostic[]? aggregatedDiagnostics;
    int createdFaceCount;

    internal FontPackage(
        FontContainerKind sourceKind,
        FontData originalData,
        FontData sfntData,
        int[] faceOffsets,
        FontDiagnostic[] diagnostics,
        FontLoadOptions options,
        FontContainerProvenance provenance)
    {
        if (originalData == null)
        {
            throw new System.ArgumentNullException("originalData");
        }

        if (sfntData == null)
        {
            throw new System.ArgumentNullException("sfntData");
        }

        if (faceOffsets == null)
        {
            throw new System.ArgumentNullException("faceOffsets");
        }

        if (diagnostics == null)
        {
            throw new System.ArgumentNullException("diagnostics");
        }

        if (options == null)
        {
            throw new System.ArgumentNullException("options");
        }

        if (provenance == null)
        {
            throw new System.ArgumentNullException("provenance");
        }

        this.sourceKind = sourceKind;
        this.originalData = originalData;
        this.sfntData = sfntData;
        this.faceOffsets = faceOffsets.AsSpan().ToArray();
        this.diagnostics = diagnostics.AsSpan().ToArray();
        this.options = options;
        this.provenance = provenance;
        faces = new FontFace[faceOffsets.Length];
        faceLocks = new object[faceOffsets.Length];
        diagnosticLock = new object();
        creationCountLock = new object();
        aggregatedDiagnostics = null;
        createdFaceCount = 0;
        int index = 0;

        while (index < faceLocks.Length)
        {
            faceLocks[index] = new object();
            index += 1;
        }
    }

    /// <summary>Gets the kind of the original loaded container.</summary>
    /// <returns>The original container kind.</returns>
    public FontContainerKind GetSourceKind()
    {
        return sourceKind;
    }

    /// <summary>Gets immutable provenance for the original and reconstructed containers.</summary>
    /// <returns>The package provenance.</returns>
    public FontContainerProvenance GetProvenance()
    {
        return provenance;
    }

    /// <summary>Gets the number of faces in header order.</summary>
    /// <returns>The face count.</returns>
    public int GetFaceCount()
    {
        return faceOffsets.Length;
    }

    /// <summary>Gets one lazily created face in header order.</summary>
    /// <param name="index">The zero-based face index.</param>
    /// <returns>The stable face instance for the index.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>index</c> is outside the package face range.
    /// </exception>
    public FontFace GetFace(int index)
    {
        ValidateFaceIndex(index);

        lock (faceLocks[index])
        {
            if (faces[index] == null)
            {
                faces[index] = CreateFace(index);

                lock (creationCountLock)
                {
                    createdFaceCount += 1;
                }
            }

            return faces[index];
        }
    }

    /// <summary>Gets all retained diagnostics in deterministic package and face order.</summary>
    /// <returns>A new array containing the retained diagnostics.</returns>
    public FontDiagnostic[] GetDiagnostics()
    {
        lock (diagnosticLock)
        {
            if (aggregatedDiagnostics == null)
            {
                aggregatedDiagnostics = CreateAggregatedDiagnostics();
            }

            return aggregatedDiagnostics.AsSpan().ToArray();
        }
    }

    /// <summary>Copies the complete original input container bytes.</summary>
    /// <returns>A new array containing the original bytes.</returns>
    public byte[] CopyOriginalBytes()
    {
        return originalData.CopyBytes();
    }

    /// <summary>Copies one face as a standalone SFNT font.</summary>
    /// <param name="faceIndex">The zero-based face index.</param>
    /// <returns>A new array containing the standalone SFNT bytes.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">
    /// <c>faceIndex</c> is outside the package face range.
    /// </exception>
    /// <exception cref="System.InvalidOperationException">
    /// The selected inspect-only face is not structurally valid for reconstruction.
    /// </exception>
    public byte[] CopySfntBytes(int faceIndex)
    {
        ValidateFaceIndex(faceIndex);
        FontFace face = GetFace(faceIndex);

        if (!face.IsStructurallyValid())
        {
            throw new System.InvalidOperationException(
                "A structurally invalid inspect-only face cannot be reconstructed.");
        }

        if (faceOffsets.Length == 1 &&
            ContainerDetector.Detect(sfntData.GetMemory().Span) != ContainerSignatureKind.Collection)
        {
            return sfntData.CopyBytes();
        }

        return SfntFaceWriter.Write(face, options.GetLimits());
    }

    internal FontContainerKind GetLoadedSourceKind()
    {
        return GetSourceKind();
    }

    internal int GetLoadedFaceCount()
    {
        return GetFaceCount();
    }

    internal int GetLoadedFaceOffset(int index)
    {
        ValidateFaceIndex(index);
        return faceOffsets[index];
    }

    internal int GetOriginalDataLength()
    {
        return originalData.GetLength();
    }

    internal byte[] CopyOriginalData()
    {
        return CopyOriginalBytes();
    }

    internal FontData GetOriginalData()
    {
        return originalData;
    }

    internal int GetSfntDataLength()
    {
        return sfntData.GetLength();
    }

    internal byte[] CopySfntData()
    {
        return sfntData.CopyBytes();
    }

    internal FontData GetSfntData()
    {
        return sfntData;
    }

    internal int GetLoadedDiagnosticCount()
    {
        return diagnostics.Length;
    }

    internal FontDiagnostic GetLoadedDiagnostic(int index)
    {
        if (index < 0 || index >= diagnostics.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return diagnostics[index];
    }

    internal int GetCreatedFaceCount()
    {
        lock (creationCountLock)
        {
            return createdFaceCount;
        }
    }

    FontFace CreateFace(int index)
    {
        options.GetCancellationToken().ThrowIfCancellationRequested();
        int faceOffset = faceOffsets[index];
        SfntDirectory directory = SfntDirectoryParser.Parse(
            sfntData.GetSlice(faceOffset, sfntData.GetLength() - faceOffset),
            options.GetLimits());
        SfntValidationResult validation = SfntDirectoryValidator.Validate(
            sfntData,
            directory,
            options,
            index);
        SfntTableStore tableStore = SfntTableStoreBuilder.Create(
            sfntData,
            directory,
            options,
            index);
        SfntTableCache tableCache = SfntTableCache.Create(
            tableStore,
            options.GetMode(),
            options.GetLimits(),
            validation.IsValid());
        return new FontFace(
            index,
            directory.GetScalerType(),
            validation,
            tableCache);
    }

    FontDiagnostic[] CreateAggregatedDiagnostics()
    {
        System.Collections.Generic.List<FontDiagnostic> result =
            new System.Collections.Generic.List<FontDiagnostic>();
        int index = 0;

        while (index < diagnostics.Length &&
            result.Count < options.GetLimits().GetMaxDiagnostics())
        {
            result.Add(diagnostics[index]);
            index += 1;
        }

        int faceIndex = 0;

        while (faceIndex < faceOffsets.Length &&
            result.Count < options.GetLimits().GetMaxDiagnostics())
        {
            FontFace face = GetFace(faceIndex);
            int diagnosticIndex = 0;

            while (diagnosticIndex < face.GetTableDiagnosticCount() &&
                result.Count < options.GetLimits().GetMaxDiagnostics())
            {
                result.Add(face.GetTableDiagnostic(diagnosticIndex));
                diagnosticIndex += 1;
            }

            faceIndex += 1;
        }

        return result.ToArray();
    }

    void ValidateFaceIndex(int index)
    {
        if (index < 0 || index >= faceOffsets.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }
    }
}
