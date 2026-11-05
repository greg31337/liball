namespace ttf;

/// <summary>Accumulates one PDF font resource and finalizes it exactly once.</summary>
public class PdfFontBuilder : IPdfFontBuilder
{
    PdfFontUsage usage;
    PdfCidAllocator allocator;
    PdfFontResource? resource;
    object builderSync;

    /// <summary>Creates a document-local PDF font resource builder.</summary>
    /// <param name="instance">The immutable font instance to embed.</param>
    /// <param name="options">The resource, compatibility, and rights policy.</param>
    /// <exception cref="System.ArgumentNullException">
    /// <c>instance</c> or <c>options</c> is null.
    /// </exception>
    public PdfFontBuilder(
        FontInstance instance,
        PdfFontOptions options)
    {
        if (instance == null || options == null)
        {
            throw new System.ArgumentNullException("values");
        }

        PdfFontOptionResolver.ValidateInstance(instance, options);
        FontOperationContext.Checkpoint(options.GetOperationSettings());

        PdfFontUsageAccumulator accumulator =
            new PdfFontUsageAccumulator();
        usage = accumulator.GetOrAdd(instance, options);
        allocator = PdfCidAllocator.Create(usage);
        builderSync = new object();
    }

    /// <summary>Gets the immutable font instance bound to this builder.</summary>
    /// <returns>The constructor-supplied source instance.</returns>
    public FontInstance GetSourceInstance()
    {
        lock (builderSync)
        {
            return usage.GetInstance();
        }
    }

    /// <summary>Gets a copy of the effective PDF font options.</summary>
    /// <returns>A defensive copy of the constructor-time option snapshot.</returns>
    public PdfFontOptions GetFontOptions()
    {
        lock (builderSync)
        {
            return usage.CopyOptions();
        }
    }

    /// <summary>Adds cmap-mapped Unicode text to the resource usage.</summary>
    /// <param name="text">The strict UTF-16 source text.</param>
    public void AddText(string text)
    {
        lock (builderSync)
        {
            Checkpoint();
            EnsureMutable();
            usage.AddText(text);
            AllocatePendingUsage();
        }
    }

    /// <summary>Adds one source glyph and optional original Unicode sequence.</summary>
    /// <param name="glyphId">The source-font glyph identifier.</param>
    /// <param name="unicodeSequence">Zero or more valid Unicode scalars.</param>
    public void AddGlyph(int glyphId, int[] unicodeSequence)
    {
        lock (builderSync)
        {
            Checkpoint();
            EnsureMutable();
            usage.AddGlyph(glyphId, unicodeSequence);
            AllocatePendingUsage();
        }
    }

    /// <summary>Encodes and adds one shaped run with zero baseline shift.</summary>
    /// <param name="run">The immutable shaped glyph run.</param>
    /// <returns>The positioned codes and text-adjustment suggestions.</returns>
    public PdfEncodedRun AddShapedRun(ShapedRun run)
    {
        return AddShapedRunWithBaselineShift(run, 0);
    }

    /// <summary>Encodes and adds one shaped run with a baseline shift.</summary>
    /// <param name="run">The immutable shaped glyph run.</param>
    /// <param name="baselineShift">A finite user-space point shift.</param>
    /// <returns>The positioned codes and text-adjustment suggestions.</returns>
    public PdfEncodedRun AddShapedRunWithBaselineShift(
        ShapedRun run,
        double baselineShift)
    {
        lock (builderSync)
        {
            Checkpoint();
            EnsureMutable();
            AllocatePendingUsage();
            return PdfFontRunEncoder.Encode(
                usage,
                allocator,
                run,
                baselineShift);
        }
    }

    /// <summary>Encodes one fallback-resolved run from this builder's instance.</summary>
    /// <param name="run">The resolved shaping run.</param>
    /// <returns>The positioned codes and text-adjustment suggestions.</returns>
    /// <exception cref="System.ArgumentNullException"><c>run</c> is null.</exception>
    /// <exception cref="System.ArgumentException">
    /// The run was resolved to a different font instance.
    /// </exception>
    public PdfEncodedRun AddResolvedRun(TextShapingRun run)
    {
        return AddResolvedRunWithBaselineShift(run, 0);
    }

    /// <summary>Encodes one fallback-resolved run with a baseline shift.</summary>
    /// <param name="run">The resolved shaping run.</param>
    /// <param name="baselineShift">A finite user-space point shift.</param>
    /// <returns>The positioned codes and text-adjustment suggestions.</returns>
    /// <exception cref="System.ArgumentNullException"><c>run</c> is null.</exception>
    /// <exception cref="System.ArgumentException">
    /// The run was resolved to a different font instance.
    /// </exception>
    public PdfEncodedRun AddResolvedRunWithBaselineShift(
        TextShapingRun run,
        double baselineShift)
    {
        if (run == null)
        {
            throw new System.ArgumentNullException("run");
        }

        if (!object.ReferenceEquals(
            run.GetFontInstance(),
            usage.GetInstance()))
        {
            throw new System.ArgumentException(
                "The resolved run belongs to a different font instance.",
                "run");
        }

        return AddShapedRunWithBaselineShift(
            run.GetShapedRun(),
            baselineShift);
    }

    /// <summary>Builds and freezes the selected PDF font resource.</summary>
    /// <returns>The immutable finalized resource.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// This builder has already finalized successfully.
    /// </exception>
    public PdfFontResource FinalizeFont()
    {
        lock (builderSync)
        {
            Checkpoint();
            EnsureMutable();
            AllocatePendingUsage();
            PdfFontResource built = PdfFontResourceBuilder.Build(
                usage,
                allocator);
            Checkpoint();
            FontOperationContext.ValidateOutputLength(
                built.CopyEmbeddedFont().Length,
                GetEffectiveLimits());
            usage.Freeze();
            allocator.Freeze();
            resource = built;
            return built;
        }
    }

    /// <summary>Reports whether finalization completed successfully.</summary>
    /// <returns>True after the one successful finalization.</returns>
    public bool IsFinalized()
    {
        lock (builderSync)
        {
            return resource != null;
        }
    }

    /// <summary>Gets the successfully finalized resource.</summary>
    /// <returns>The same immutable object returned by <see cref="FinalizeFont"/>.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// Finalization has not completed successfully.
    /// </exception>
    public PdfFontResource GetFinalizedResource()
    {
        lock (builderSync)
        {
            if (resource == null)
            {
                throw new System.InvalidOperationException(
                    "The PDF font resource has not been finalized.");
            }

            return resource;
        }
    }

    /// <summary>Gets the current number of allocated character codes.</summary>
    /// <returns>The dense code count including reserved code zero.</returns>
    public int GetCharacterCodeCount()
    {
        lock (builderSync)
        {
            return allocator.GetCount();
        }
    }

    void AllocatePendingUsage()
    {
        int maximumCode = usage.CopyOptions().GetFontKind() ==
            PdfFontKind.SimpleTrueType ? byte.MaxValue : ushort.MaxValue;
        allocator.GetOrAddCids(
            usage.CopyGlyphIdsInUseOrder(),
            maximumCode);
    }

    void EnsureMutable()
    {
        if (resource != null)
        {
            throw new System.InvalidOperationException(
                "The PDF font resource has already been finalized.");
        }
    }

    void Checkpoint()
    {
        FontOperationContext.Checkpoint(
            usage.CopyOptions().GetOperationSettings());
    }

    FontLimits GetEffectiveLimits()
    {
        return FontOperationContext.CreateEffectiveLimits(
            usage.GetInstance().GetFace().GetTableCache().GetLimits(),
            usage.CopyOptions().GetOperationSettings());
    }
}
