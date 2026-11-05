namespace svg;

internal class SvgFilterGraphState
{
    SvgSceneResource? resource;
    SvgFilterGraphOptions options;
    SvgFilterGraphPlan plan;
    SvgFilterGraphError error;
    int[] standardSlots;
    int[]? outputSlots;
    int[]? lastUses;
    SvgPixelRect[]? primitiveBounds;

    internal SvgFilterGraphState(SvgSceneResource? filter, SvgFilterGraphOptions graphOptions)
    {
        resource = filter;
        options = graphOptions;
        plan = new SvgFilterGraphPlan();
        error = SvgFilterGraphError.None;
        standardSlots = new int[6];
        outputSlots = null;
        lastUses = null;
        primitiveBounds = null;
    }

    internal SvgFilterGraphPlan Run()
    {
        InitializeSlots();
        if (!Validate()) return Failure();
        SvgMatrixOperationResult primitiveTransform = options.Transform.Multiply(((resource ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Filter ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).PrimitiveTransform);
        if (!primitiveTransform.Success)
        {
            SetError(SvgFilterGraphError.NonFiniteRegion);
            return Failure();
        }
        plan.PrimitiveTransform = (primitiveTransform.Matrix ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
        SvgPixelRect? filterBounds = TransformBounds((resource.Filter ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Region);
        if (filterBounds == null) return Failure();
        plan.FilterBounds = Intersect(filterBounds, options.Clip);
        if (!TakeArea(plan.FilterBounds)) return Failure();
        int count = resource.Filter.Primitives.Count;
        outputSlots = new int[count];
        lastUses = new int[count];
        primitiveBounds = new SvgPixelRect[count];
        int index = 0;
        while (index < count)
        {
            outputSlots[index] = -1;
            lastUses[index] = index;
            index = index + 1;
        }
        if (!ResolveLifetimes()) return Failure();
        if (!ResolveBounds()) return Failure();
        if (!CreateSnapshots()) return Failure();
        if (!CreateSteps()) return Failure();
        if (!Allocate()) return Failure();
        if (count == 0)
        {
            plan.FinalSurfaceIndex = standardSlots[(int)SvgFilterStandardInput.SourceGraphic];
            plan.FinalBounds = plan.FilterBounds;
        }
        else
        {
            plan.FinalSurfaceIndex = outputSlots[count - 1];
            plan.FinalBounds = primitiveBounds[count - 1];
        }
        plan.Success = true;
        plan.Error = SvgFilterGraphError.None;
        return plan;
    }

    void InitializeSlots()
    {
        int index = 0;
        while (index < standardSlots.Length)
        {
            standardSlots[index] = -1;
            index = index + 1;
        }
    }

    bool Validate()
    {
        if (resource == null || options == null || resource.Kind != SvgSceneResourceKind.Filter || resource.Filter == null || !resource.HasValidTag()) return SetError(SvgFilterGraphError.InvalidArgument);
        if (options.Transform == null || options.FilterLimits == null || options.RasterLimits == null || resource.Filter.Region == null || resource.Filter.PrimitiveTransform == null || resource.Filter.Primitives == null) return SetError(SvgFilterGraphError.InvalidArgument);
        if (!Known(options.DefaultColorSpace) || !ValidFilterLimits(options.FilterLimits) || !ValidRasterLimits(options.RasterLimits) || !ValidClip(options.Clip)) return SetError(SvgFilterGraphError.InvalidArgument);
        if (resource.Filter.Primitives.Count > options.FilterLimits.MaxPrimitives) return SetError(SvgFilterGraphError.PrimitiveLimitExceeded);
        int index = 0;
        while (index < resource.Filter.Primitives.Count)
        {
            SvgSceneFilterPrimitiveData primitive = resource.Filter.Primitives[index];
            if (primitive == null || primitive.Inputs == null || primitive.Index != index || !Known(primitive.ColorSpace)) return SetError(SvgFilterGraphError.InvalidArgument);
            index = index + 1;
        }
        return true;
    }

    bool ResolveLifetimes()
    {
        int index = 0;
        while (index < ((resource ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Filter ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Primitives.Count)
        {
            SvgSceneFilterPrimitiveData primitive = (resource.Filter ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Primitives[index];
            int inputIndex = 0;
            while (inputIndex < primitive.Inputs.Count)
            {
                SvgFilterInput input = primitive.Inputs[inputIndex];
                if (input == null || !Known(input.Kind)) return SetError(SvgFilterGraphError.InvalidArgument);
                if (input.Kind == SvgFilterInputKind.Standard)
                {
                    if (!Known(input.StandardInput)) return SetError(SvgFilterGraphError.InvalidArgument);
                    standardSlots[(int)input.StandardInput] = -2;
                }
                else if (input.Kind == SvgFilterInputKind.Previous || input.Kind == SvgFilterInputKind.NamedResult)
                {
                    int producer = input.Primitive == null ? -1 : input.Primitive.Index;
                    if (producer < 0 || producer >= index) return SetError(SvgFilterGraphError.InvalidGraph);
                    if (input.Kind == SvgFilterInputKind.Previous && producer != index - 1) return SetError(SvgFilterGraphError.InvalidGraph);
                    if (index > (lastUses ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[producer]) lastUses[producer] = index;
                }
                inputIndex = inputIndex + 1;
            }
            index = index + 1;
        }
        if ((resource.Filter ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Primitives.Count == 0) standardSlots[(int)SvgFilterStandardInput.SourceGraphic] = -2;
        else (lastUses ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[resource.Filter.Primitives.Count - 1] = resource.Filter.Primitives.Count;
        return true;
    }

    bool ResolveBounds()
    {
        int index = 0;
        while (index < ((resource ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Filter ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Primitives.Count)
        {
            SvgSceneFilterPrimitiveData primitive = (resource.Filter ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Primitives[index];
            SvgPixelRect bounds;
            if (primitive.Region != null)
            {
                bounds = (TransformBounds(primitive.Region) ?? throw new System.InvalidOperationException("A required SVG value is unavailable."));
                if (bounds == null) return false;
                bounds = Intersect(bounds, plan.FilterBounds);
            }
            else
            {
                bounds = PropagatedBounds(primitive);
            }
            (primitiveBounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[index] = bounds;
            index = index + 1;
        }
        return true;
    }

    SvgPixelRect PropagatedBounds(SvgSceneFilterPrimitiveData primitive)
    {
        if (primitive.ElementKind == SvgElementKind.FeTile || primitive.Inputs.Count == 0) return CopyRect(plan.FilterBounds);
        SvgPixelRect? aggregate = null;
        bool filterRegion = false;
        int index = 0;
        while (index < primitive.Inputs.Count)
        {
            SvgFilterInput input = primitive.Inputs[index];
            if (input.Kind == SvgFilterInputKind.Standard || input.Kind == SvgFilterInputKind.TransparentBlack) filterRegion = true;
            else aggregate = Union(aggregate, (primitiveBounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[(input.Primitive ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Index]);
            index = index + 1;
        }
        if (filterRegion || aggregate == null) return CopyRect(plan.FilterBounds);
        return Intersect(aggregate, plan.FilterBounds);
    }

    bool CreateSnapshots()
    {
        int standard = 0;
        while (standard < standardSlots.Length)
        {
            if (standardSlots[standard] == -2)
            {
                SvgFilterStandardInput kind = (SvgFilterStandardInput)standard;
                SvgPixelBuffer? source = Source(kind);
                SvgPixelColorSpace colorSpace = source == null ? options.DefaultColorSpace : source.ColorSpace;
                if (!Known(colorSpace)) return SetError(SvgFilterGraphError.InvalidArgument);
                if (!AddSurface(SvgFilterSurfaceKind.StandardInput, kind, colorSpace, plan.FilterBounds.Width, plan.FilterBounds.Height)) return false;
                standardSlots[standard] = plan.Surfaces.Count - 1;
                plan.SnapshotCount = plan.SnapshotCount + 1;
            }
            standard = standard + 1;
        }
        return true;
    }

    bool CreateSteps()
    {
        int index = 0;
        while (index < ((resource ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Filter ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Primitives.Count)
        {
            SvgSceneFilterPrimitiveData primitive = (resource.Filter ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Primitives[index];
            SvgFilterGraphStep step = new SvgFilterGraphStep(index, primitive);
            step.Bounds = (primitiveBounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[index];
            step.ColorSpace = OperationColorSpace(primitive.ColorSpace);
            step.LastUseStep = (lastUses ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[index];
            int inputIndex = 0;
            while (inputIndex < primitive.Inputs.Count)
            {
                SvgFilterGraphInput? input = Bind(primitive.Inputs[inputIndex], step.ColorSpace);
                if (input == null) return false;
                step.Inputs.Add(input);
                inputIndex = inputIndex + 1;
            }
            if (!step.Bounds.IsEmpty)
            {
                int slot = FindReusable(step.ColorSpace, index);
                if (slot < 0)
                {
                    if (!AddSurface(SvgFilterSurfaceKind.PrimitiveOutput, SvgFilterStandardInput.SourceGraphic, step.ColorSpace, step.Bounds.Width, step.Bounds.Height)) return false;
                    slot = plan.Surfaces.Count - 1;
                }
                else
                {
                    step.ReusesSurface = true;
                    plan.ReusedSurfaceCount = plan.ReusedSurfaceCount + 1;
                    SvgFilterSurface reused = plan.Surfaces[slot];
                    if (step.Bounds.Width > reused.Width) reused.Width = step.Bounds.Width;
                    if (step.Bounds.Height > reused.Height) reused.Height = step.Bounds.Height;
                }
                SvgFilterSurface surface = plan.Surfaces[slot];
                surface.Uses.Add(new SvgFilterSurfaceUse(index, step.Bounds, index, lastUses[index]));
                (outputSlots ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[index] = slot;
                step.OutputSurfaceIndex = slot;
            }
            plan.Steps.Add(step);
            index = index + 1;
        }
        return true;
    }

    SvgFilterGraphInput? Bind(SvgFilterInput source, SvgPixelColorSpace operationColorSpace)
    {
        SvgFilterGraphInput input = new SvgFilterGraphInput();
        input.Kind = source.Kind;
        if (source.Kind == SvgFilterInputKind.TransparentBlack)
        {
            input.Bounds = new SvgPixelRect(0, 0, 0, 0);
            input.ColorSpace = operationColorSpace;
            return input;
        }
        if (source.Kind == SvgFilterInputKind.Standard)
        {
            int slot = standardSlots[(int)source.StandardInput];
            if (slot < 0) return BindFailure();
            input.StandardInput = source.StandardInput;
            input.SurfaceIndex = slot;
            input.Bounds = CopyRect(plan.FilterBounds);
            input.ColorSpace = plan.Surfaces[slot].ColorSpace;
            input.RequiresColorConversion = input.ColorSpace != operationColorSpace;
            return input;
        }
        int producer = source.Primitive == null ? -1 : source.Primitive.Index;
        if (producer < 0 || producer >= (outputSlots ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Length) return BindFailure();
        input.ProducerIndex = producer;
        input.SurfaceIndex = outputSlots[producer];
        input.Bounds = CopyRect((primitiveBounds ?? throw new System.InvalidOperationException("A required SVG value is unavailable."))[producer]);
        if (input.SurfaceIndex < 0)
        {
            input.ColorSpace = operationColorSpace;
            return input;
        }
        input.ColorSpace = plan.Surfaces[input.SurfaceIndex].ColorSpace;
        input.RequiresColorConversion = input.ColorSpace != operationColorSpace;
        return input;
    }

    SvgFilterGraphInput? BindFailure()
    {
        SetError(SvgFilterGraphError.InvalidGraph);
        return null;
    }

    int FindReusable(SvgPixelColorSpace colorSpace, int step)
    {
        int index = 0;
        while (index < plan.Surfaces.Count)
        {
            SvgFilterSurface surface = plan.Surfaces[index];
            if (surface.Kind == SvgFilterSurfaceKind.PrimitiveOutput && surface.ColorSpace == colorSpace && surface.Uses.Count > 0)
            {
                SvgFilterSurfaceUse previous = surface.Uses[surface.Uses.Count - 1];
                if (previous.LastStep < step) return index;
            }
            index = index + 1;
        }
        return -1;
    }

    bool AddSurface(SvgFilterSurfaceKind kind, SvgFilterStandardInput standard, SvgPixelColorSpace colorSpace, int width, int height)
    {
        if (plan.Surfaces.Count >= options.FilterLimits.MaxOffscreenSurfaces) return SetError(SvgFilterGraphError.SurfaceLimitExceeded);
        if (!ValidDimensions(width, height)) return false;
        plan.Surfaces.Add(new SvgFilterSurface(plan.Surfaces.Count, kind, standard, colorSpace, width, height));
        return true;
    }

    bool Allocate()
    {
        if (!options.AllocateSurfaces) return true;
        int index = 0;
        while (index < plan.Surfaces.Count)
        {
            SvgFilterSurface surface = plan.Surfaces[index];
            SvgPixelBufferResult created = SvgPixelBuffer.Create(surface.Width, surface.Height, surface.ColorSpace, options.RasterLimits);
            if (!created.Success) return SetError(SvgFilterGraphError.AllocationFailed);
            surface.Buffer = created.Buffer;
            if (surface.Kind == SvgFilterSurfaceKind.StandardInput && !Capture(surface)) return false;
            index = index + 1;
        }
        return true;
    }

    bool Capture(SvgFilterSurface surface)
    {
        SvgPixelBuffer? source = Source(surface.StandardInput);
        if (source == null || plan.FilterBounds.IsEmpty) return true;
        bool alphaOnly = surface.StandardInput == SvgFilterStandardInput.SourceAlpha || surface.StandardInput == SvgFilterStandardInput.BackgroundAlpha;
        int left = System.Math.Max(plan.FilterBounds.X, 0);
        int top = System.Math.Max(plan.FilterBounds.Y, 0);
        int right = System.Math.Min(plan.FilterBounds.Right, source.Width);
        int bottom = System.Math.Min(plan.FilterBounds.Bottom, source.Height);
        if (right <= left || bottom <= top) return true;
        int y = top;
        while (y < bottom)
        {
            int x = left;
            while (x < right)
            {
                SvgPixelReadResult read = source.Read(x, y);
                if (!read.Success) return SetError(SvgFilterGraphError.InvalidArgument);
                SvgPremultipliedRgba8? pixel = read.Pixel;
                if (alphaOnly) pixel = new SvgPremultipliedRgba8(0, 0, 0, (pixel ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Alpha);
                SvgPixelOperationResult written = (surface.Buffer ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")).Write(x - plan.FilterBounds.X, y - plan.FilterBounds.Y, (pixel ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
                if (!written.Success) return SetError(SvgFilterGraphError.InvalidArgument);
                x = x + 1;
            }
            y = y + 1;
        }
        return true;
    }

    SvgPixelBuffer? Source(SvgFilterStandardInput input)
    {
        if (input == SvgFilterStandardInput.SourceGraphic || input == SvgFilterStandardInput.SourceAlpha) return options.SourceGraphic;
        if (input == SvgFilterStandardInput.BackgroundImage || input == SvgFilterStandardInput.BackgroundAlpha) return options.BackgroundImage;
        if (input == SvgFilterStandardInput.FillPaint) return options.FillPaint;
        if (input == SvgFilterStandardInput.StrokePaint) return options.StrokePaint;
        return null;
    }

    SvgPixelColorSpace OperationColorSpace(SvgFilterColorSpace colorSpace)
    {
        if (colorSpace == SvgFilterColorSpace.Srgb) return SvgPixelColorSpace.Srgb;
        if (colorSpace == SvgFilterColorSpace.LinearRgb) return SvgPixelColorSpace.SrgbLinear;
        return options.DefaultColorSpace;
    }

    SvgPixelRect? TransformBounds(SvgRect? region)
    {
        SvgRectTransformResult transformed = options.Transform.TransformRect((region ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        if (!transformed.Success)
        {
            SetError(SvgFilterGraphError.NonFiniteRegion);
            return null;
        }
        SvgPixelRect? bounds = SvgCompositeMath.PixelBounds(transformed.Rectangle);
        if (bounds == null) SetError(SvgFilterGraphError.NonFiniteRegion);
        return bounds;
    }

    bool TakeArea(SvgPixelRect bounds)
    {
        long area = (long)bounds.Width * bounds.Height;
        if (area > options.FilterLimits.MaxAreaPixels) return SetError(SvgFilterGraphError.AreaLimitExceeded);
        return ValidDimensions(bounds.Width, bounds.Height);
    }

    bool ValidDimensions(int width, int height)
    {
        long area = (long)width * height;
        if (width > options.RasterLimits.MaxWidth || height > options.RasterLimits.MaxHeight || area > options.RasterLimits.MaxSurfacePixels || area > options.RasterLimits.MaxPixelBytes / 4) return SetError(SvgFilterGraphError.AllocationFailed);
        return true;
    }

    bool SetError(SvgFilterGraphError value)
    {
        if (error == SvgFilterGraphError.None) error = value;
        return false;
    }

    SvgFilterGraphPlan Failure()
    {
        plan.Success = false;
        plan.Error = error == SvgFilterGraphError.None ? SvgFilterGraphError.InvalidArgument : error;
        plan.Steps.Clear();
        plan.Surfaces.Clear();
        plan.FinalSurfaceIndex = -1;
        plan.FinalBounds = new SvgPixelRect(0, 0, 0, 0);
        plan.SnapshotCount = 0;
        plan.ReusedSurfaceCount = 0;
        return plan;
    }

    static SvgPixelRect Intersect(SvgPixelRect first, SvgPixelRect? second)
    {
        if (second == null) return CopyRect(first);
        int left = System.Math.Max(first.X, second.X);
        int top = System.Math.Max(first.Y, second.Y);
        int right = System.Math.Min(first.Right, second.Right);
        int bottom = System.Math.Min(first.Bottom, second.Bottom);
        if (right <= left || bottom <= top) return new SvgPixelRect(left, top, 0, 0);
        return new SvgPixelRect(left, top, right - left, bottom - top);
    }

    static SvgPixelRect Union(SvgPixelRect? first, SvgPixelRect? second)
    {
        if (first == null || first.IsEmpty) return CopyRect((second ?? throw new System.InvalidOperationException("A required SVG value is unavailable.")));
        if (second == null || second.IsEmpty) return CopyRect(first);
        int left = System.Math.Min(first.X, second.X);
        int top = System.Math.Min(first.Y, second.Y);
        int right = System.Math.Max(first.Right, second.Right);
        int bottom = System.Math.Max(first.Bottom, second.Bottom);
        return new SvgPixelRect(left, top, right - left, bottom - top);
    }

    static SvgPixelRect CopyRect(SvgPixelRect source)
    {
        return new SvgPixelRect(source.X, source.Y, source.Width, source.Height);
    }

    static bool ValidClip(SvgPixelRect? clip)
    {
        if (clip == null) return true;
        return clip.Width >= 0 && clip.Height >= 0 && clip.Right == (long)clip.X + clip.Width && clip.Bottom == (long)clip.Y + clip.Height;
    }

    static bool ValidFilterLimits(SvgFilterLimits limits)
    {
        return limits.MaxPrimitives > 0 && limits.MaxAreaPixels > 0 && limits.MaxOffscreenSurfaces > 0;
    }

    static bool ValidRasterLimits(SvgRasterLimits limits)
    {
        return limits.MaxWidth > 0 && limits.MaxHeight > 0 && limits.MaxSurfacePixels > 0 && limits.MaxPixelBytes > 0;
    }

    static bool Known(SvgPixelColorSpace colorSpace)
    {
        return colorSpace == SvgPixelColorSpace.Srgb || colorSpace == SvgPixelColorSpace.SrgbLinear;
    }

    static bool Known(SvgFilterColorSpace colorSpace)
    {
        return colorSpace == SvgFilterColorSpace.Auto || colorSpace == SvgFilterColorSpace.Srgb || colorSpace == SvgFilterColorSpace.LinearRgb;
    }

    static bool Known(SvgFilterInputKind kind)
    {
        return kind == SvgFilterInputKind.Standard || kind == SvgFilterInputKind.Previous || kind == SvgFilterInputKind.NamedResult || kind == SvgFilterInputKind.TransparentBlack;
    }

    static bool Known(SvgFilterStandardInput input)
    {
        return input == SvgFilterStandardInput.SourceGraphic || input == SvgFilterStandardInput.SourceAlpha || input == SvgFilterStandardInput.BackgroundImage || input == SvgFilterStandardInput.BackgroundAlpha || input == SvgFilterStandardInput.FillPaint || input == SvgFilterStandardInput.StrokePaint;
    }
}
