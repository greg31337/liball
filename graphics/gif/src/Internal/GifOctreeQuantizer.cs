namespace gif;

internal class GifOctreeNode
{
    internal GifOctreeNode[] Children;
    internal bool IsLeaf;
    internal int Level;
    internal long RedSum;
    internal long GreenSum;
    internal long BlueSum;
    internal int Count;
    internal int FirstOriginalIndex;

    internal GifOctreeNode(int level)
    {
        Children = new GifOctreeNode[8];
        IsLeaf = false;
        Level = level;
        RedSum = 0;
        GreenSum = 0;
        BlueSum = 0;
        Count = 0;
        FirstOriginalIndex = 2147483647;
    }
}

internal class GifOctreeBuildState
{
    internal int LeafCount;

    internal GifOctreeBuildState()
    {
        LeafCount = 0;
    }
}

internal class GifOctreeLeafCollection
{
    internal GifOctreeNode[] Leaves;
    internal int Count;

    internal GifOctreeLeafCollection()
    {
        Leaves = new GifOctreeNode[0];
        Count = 0;
    }
}

internal class GifOctreeQuantizer : IGifQuantizationStrategy
{
    internal GifPaletteBuildResult QuantizeBitmap(GifBitmap bitmap)
    {
        return QuantizeBitmapWithOptions(bitmap, new GifQuantizeOptions());
    }

    public GifPaletteBuildResult QuantizeBitmapWithOptions(GifBitmap bitmap, GifQuantizeOptions options)
    {
        GifPaletteBuildResult result = new GifPaletteBuildResult();
        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifErrorDiagnostic validation = builder.ValidateBitmap(bitmap);
        if (validation.Code != GifErrorCode.None)
        {
            result.Error = validation;
            return result;
        }

        return QuantizeRgbaPixelsWithOptions(bitmap.RgbaPixels, options);
    }

    internal GifPaletteBuildResult QuantizeRgbaPixels(byte[] rgbaPixels)
    {
        return QuantizeRgbaPixelsWithOptions(rgbaPixels, new GifQuantizeOptions());
    }

    public GifPaletteBuildResult QuantizeRgbaPixelsWithOptions(byte[] rgbaPixels, GifQuantizeOptions options)
    {
        GifPaletteBuildResult result = new GifPaletteBuildResult();
        if (rgbaPixels == null)
        {
            rgbaPixels = new byte[0];
        }

        if (rgbaPixels.Length % 4 != 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.DecodedPixelCountMismatch, "RGBA byte count must be divisible by four for octree quantization.", new GifDiagnosticPosition(-1, -1, "Octree"));
            return result;
        }

        GifQuantizeOptions activeOptions = NormalizeOptions(options);
        GifErrorDiagnostic optionValidation = ValidateOptions(activeOptions);
        if (optionValidation.Code != GifErrorCode.None)
        {
            result.Error = optionValidation;
            return result;
        }

        GifMedianCutQuantizer shared = new GifMedianCutQuantizer();
        GifPaletteBuildResult exact = shared.TryBuildExactPalette(rgbaPixels, activeOptions);
        if (exact.Success)
        {
            return exact;
        }

        if (exact.Error.Code != GifErrorCode.InvalidColorTableSize)
        {
            return exact;
        }

        GifMedianCutSampleResult samples = shared.CollectSamples(rgbaPixels, activeOptions);
        if (!samples.Success)
        {
            result.Error = samples.Error;
            return result;
        }

        int targetColorCount = activeOptions.MaxColors;
        if (samples.HasTransparency)
        {
            targetColorCount = targetColorCount - 1;
        }

        if (targetColorCount <= 0 && samples.Count > 0)
        {
            result.Error = new GifErrorDiagnostic(GifErrorCode.InvalidColorTableSize, "Octree quantization has no palette slots for opaque colors.", new GifDiagnosticPosition(-1, -1, "Octree"));
            return result;
        }

        GifOctreeBuildState state = new GifOctreeBuildState();
        GifOctreeNode root = new GifOctreeNode(0);
        int sampleIndex = 0;
        while (sampleIndex < samples.Count)
        {
            InsertColor(root, samples.Colors[sampleIndex], 0, state);
            sampleIndex = sampleIndex + 1;
        }

        while (state.LeafCount > targetColorCount)
        {
            GifOctreeNode node = FindDeepestReducibleNode(root);
            if (node.Level < 0)
            {
                break;
            }

            ReduceNode(node, state);
        }

        GifColor[] colors = CreatePaletteColors(root, state.LeafCount, activeOptions.PreservePaletteOrder);
        int outputColorCount = colors.Length;
        if (samples.HasTransparency)
        {
            outputColorCount = outputColorCount + 1;
        }

        GifColor[] paletteColors = new GifColor[outputColorCount];
        int outputIndex = 0;
        if (samples.HasTransparency)
        {
            paletteColors[0] = new GifColor(0, 0, 0);
            result.HasTransparency = true;
            result.TransparentColorIndex = 0;
            result.TransparentPixelCount = samples.TransparentPixelCount;
            outputIndex = 1;
        }

        int colorIndex = 0;
        while (colorIndex < colors.Length)
        {
            paletteColors[outputIndex] = colors[colorIndex];
            outputIndex = outputIndex + 1;
            colorIndex = colorIndex + 1;
        }

        GifPaletteBuilder builder = new GifPaletteBuilder();
        GifPaletteCompactionResult compacted = builder.CompactToLegalColorTableSize(new GifPalette(paletteColors));
        if (!compacted.Success)
        {
            result.Error = compacted.Error;
            return result;
        }

        result.Palette = compacted.Palette;
        result.UniqueColorCount = samples.UniqueColorCount;
        if (samples.HasTransparency)
        {
            result.UniqueColorCount = result.UniqueColorCount + 1;
        }

        result.Exact = false;
        result.Success = true;
        return result;
    }

    internal void InsertColor(GifOctreeNode node, GifMedianCutColor color, int level, GifOctreeBuildState state)
    {
        if (color.OriginalIndex < node.FirstOriginalIndex)
        {
            node.FirstOriginalIndex = color.OriginalIndex;
        }

        if (node.IsLeaf || level >= 8)
        {
            if (!node.IsLeaf)
            {
                node.IsLeaf = true;
                state.LeafCount = state.LeafCount + 1;
            }

            node.RedSum = node.RedSum + color.Red;
            node.GreenSum = node.GreenSum + color.Green;
            node.BlueSum = node.BlueSum + color.Blue;
            node.Count = node.Count + 1;
            return;
        }

        int childIndex = GetChildIndex(color, level);
        if (node.Children[childIndex] == null)
        {
            node.Children[childIndex] = new GifOctreeNode(level + 1);
        }

        InsertColor(node.Children[childIndex], color, level + 1, state);
    }

    internal int GetChildIndex(GifMedianCutColor color, int level)
    {
        int shift = 7 - level;
        int index = 0;
        if ((((int)color.Red >> shift) & 1) != 0)
        {
            index = index | 4;
        }

        if ((((int)color.Green >> shift) & 1) != 0)
        {
            index = index | 2;
        }

        if ((((int)color.Blue >> shift) & 1) != 0)
        {
            index = index | 1;
        }

        return index;
    }

    internal GifOctreeNode FindDeepestReducibleNode(GifOctreeNode node)
    {
        GifOctreeNode best = new GifOctreeNode(-1);
        return FindDeepestReducibleNodeFromCandidate(node, best);
    }

    internal GifOctreeNode FindDeepestReducibleNodeFromCandidate(GifOctreeNode node, GifOctreeNode best)
    {
        if (node == null)
        {
            return best;
        }

        if (!node.IsLeaf && CountLeafDescendants(node) > 1)
        {
            if (best.Level < 0 || node.Level > best.Level)
            {
                best = node;
            }
        }

        int index = 0;
        while (index < node.Children.Length)
        {
            best = FindDeepestReducibleNodeFromCandidate(node.Children[index], best);
            index = index + 1;
        }

        return best;
    }

    internal int CountLeafDescendants(GifOctreeNode node)
    {
        if (node == null)
        {
            return 0;
        }

        if (node.IsLeaf)
        {
            return 1;
        }

        int count = 0;
        int index = 0;
        while (index < node.Children.Length)
        {
            count = count + CountLeafDescendants(node.Children[index]);
            index = index + 1;
        }

        return count;
    }

    internal void ReduceNode(GifOctreeNode node, GifOctreeBuildState state)
    {
        int leafCount = CountLeafDescendants(node);
        node.RedSum = 0;
        node.GreenSum = 0;
        node.BlueSum = 0;
        node.Count = 0;
        node.FirstOriginalIndex = 2147483647;
        AccumulateLeaves(node, node);
        node.Children = new GifOctreeNode[8];
        node.IsLeaf = true;
        state.LeafCount = state.LeafCount - leafCount + 1;
    }

    internal void AccumulateLeaves(GifOctreeNode target, GifOctreeNode node)
    {
        if (node == null)
        {
            return;
        }

        if (node.IsLeaf)
        {
            target.RedSum = target.RedSum + node.RedSum;
            target.GreenSum = target.GreenSum + node.GreenSum;
            target.BlueSum = target.BlueSum + node.BlueSum;
            target.Count = target.Count + node.Count;
            if (node.FirstOriginalIndex < target.FirstOriginalIndex)
            {
                target.FirstOriginalIndex = node.FirstOriginalIndex;
            }

            return;
        }

        int index = 0;
        while (index < node.Children.Length)
        {
            AccumulateLeaves(target, node.Children[index]);
            index = index + 1;
        }
    }

    internal GifColor[] CreatePaletteColors(GifOctreeNode root, int leafCount, bool preservePaletteOrder)
    {
        GifOctreeLeafCollection collection = new GifOctreeLeafCollection();
        collection.Leaves = new GifOctreeNode[leafCount];
        CollectLeaves(root, collection);
        if (preservePaletteOrder)
        {
            SortLeavesByFirstOriginalIndex(collection.Leaves, collection.Count);
        }

        GifColor[] colors = new GifColor[collection.Count];
        int index = 0;
        while (index < collection.Count)
        {
            colors[index] = CreateAverageColor(collection.Leaves[index]);
            index = index + 1;
        }

        return colors;
    }

    internal void CollectLeaves(GifOctreeNode node, GifOctreeLeafCollection collection)
    {
        if (node == null)
        {
            return;
        }

        if (node.IsLeaf)
        {
            collection.Leaves[collection.Count] = node;
            collection.Count = collection.Count + 1;
            return;
        }

        int index = 0;
        while (index < node.Children.Length)
        {
            CollectLeaves(node.Children[index], collection);
            index = index + 1;
        }
    }

    internal void SortLeavesByFirstOriginalIndex(GifOctreeNode[] leaves, int count)
    {
        int index = 1;
        while (index < count)
        {
            GifOctreeNode value = leaves[index];
            int insert = index - 1;
            while (insert >= 0 && leaves[insert].FirstOriginalIndex > value.FirstOriginalIndex)
            {
                leaves[insert + 1] = leaves[insert];
                insert = insert - 1;
            }

            leaves[insert + 1] = value;
            index = index + 1;
        }
    }

    internal GifColor CreateAverageColor(GifOctreeNode node)
    {
        byte red = (byte)((node.RedSum + (node.Count / 2)) / node.Count);
        byte green = (byte)((node.GreenSum + (node.Count / 2)) / node.Count);
        byte blue = (byte)((node.BlueSum + (node.Count / 2)) / node.Count);
        return new GifColor(red, green, blue);
    }

    internal GifQuantizeOptions NormalizeOptions(GifQuantizeOptions options)
    {
        if (options == null)
        {
            return new GifQuantizeOptions();
        }

        return options;
    }

    internal GifErrorDiagnostic ValidateOptions(GifQuantizeOptions options)
    {
        GifMedianCutQuantizer shared = new GifMedianCutQuantizer();
        return shared.ValidateOptions(options);
    }
}
