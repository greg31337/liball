namespace barcode;

internal class LinearTransitionMap
{
    internal int Width;
    internal int Height;
    internal ModuleMatrix HorizontalTransitions = new ModuleMatrix(1, 1);
    internal ModuleMatrix VerticalTransitions = new ModuleMatrix(1, 1);
}

internal class LinearOrientationHistogram
{
    internal int[] VerticalBarEvidenceByRow = new int[0];
    internal int[] HorizontalBarEvidenceByColumn = new int[0];
    internal int VerticalBarEvidence;
    internal int HorizontalBarEvidence;
    internal bool VerticalBarsDominate;
}

internal class LinearConnectedBlock
{
    internal int Left;
    internal int Top;
    internal int Width;
    internal int Height;
    internal int PixelCount;
    internal bool IsVertical;
    internal double MajorToMinorRatio;
}

internal class LinearLineGroup
{
    internal bool IsVertical;
    internal int Left;
    internal int Top;
    internal int Right;
    internal int Bottom;
    internal LinearConnectedBlock[] Blocks = new LinearConnectedBlock[0];
}

internal class LinearBarcodeCandidate
{
    internal BarcodeRectangle Bounds = new BarcodeRectangle();
    internal BarcodeQuadrilateral Location = new BarcodeQuadrilateral();
    internal bool BarsVertical;
    internal int BarCount;
    internal int TransitionCount;
    internal double OrientationScore;
    internal LinearConnectedBlock[] Blocks = new LinearConnectedBlock[0];
}

internal static class LinearTransitionAnalyzer
{
    internal static LinearTransitionMap CreateMap(ModuleMatrix matrix)
    {
        if (matrix == null)
        {
            throw new BarcodeValidationException("Linear transition source must not be null.");
        }

        ModuleMatrix horizontal = new ModuleMatrix(matrix.Width, matrix.Height);
        ModuleMatrix vertical = new ModuleMatrix(matrix.Width, matrix.Height);
        int y = 0;
        while (y < matrix.Height)
        {
            int x = 0;
            while (x < matrix.Width)
            {
                if (x > 0 && matrix.GetModule(x - 1, y) != matrix.GetModule(x, y))
                {
                    horizontal.SetModule(x, y, true);
                }

                if (y > 0 && matrix.GetModule(x, y - 1) != matrix.GetModule(x, y))
                {
                    vertical.SetModule(x, y, true);
                }

                x = x + 1;
            }

            y = y + 1;
        }

        LinearTransitionMap map = new LinearTransitionMap();
        map.Width = matrix.Width;
        map.Height = matrix.Height;
        map.HorizontalTransitions = horizontal;
        map.VerticalTransitions = vertical;
        return map;
    }

    internal static LinearOrientationHistogram CreateHistogram(LinearTransitionMap map)
    {
        ValidateMap(map);
        LinearOrientationHistogram histogram = new LinearOrientationHistogram();
        histogram.VerticalBarEvidenceByRow = new int[map.Height];
        histogram.HorizontalBarEvidenceByColumn = new int[map.Width];

        int y = 0;
        while (y < map.Height)
        {
            int x = 0;
            while (x < map.Width)
            {
                if (map.HorizontalTransitions.GetModule(x, y))
                {
                    histogram.VerticalBarEvidenceByRow[y] =
                        histogram.VerticalBarEvidenceByRow[y] + 1;
                    histogram.VerticalBarEvidence = histogram.VerticalBarEvidence + 1;
                }

                if (map.VerticalTransitions.GetModule(x, y))
                {
                    histogram.HorizontalBarEvidenceByColumn[x] =
                        histogram.HorizontalBarEvidenceByColumn[x] + 1;
                    histogram.HorizontalBarEvidence = histogram.HorizontalBarEvidence + 1;
                }

                x = x + 1;
            }

            y = y + 1;
        }

        histogram.VerticalBarsDominate =
            histogram.VerticalBarEvidence >= histogram.HorizontalBarEvidence;
        return histogram;
    }

    internal static int CountWithin(
        LinearTransitionMap map,
        int left,
        int top,
        int right,
        int bottom,
        bool horizontalTransitions)
    {
        ValidateMap(map);
        if (left < 0 || top < 0 || right < left || bottom < top ||
            right > map.Width || bottom > map.Height)
        {
            throw new BarcodeValidationException("Linear transition count bounds are invalid.");
        }

        ModuleMatrix source = horizontalTransitions
            ? map.HorizontalTransitions
            : map.VerticalTransitions;
        int count = 0;
        int y = top;
        while (y < bottom)
        {
            int x = left;
            while (x < right)
            {
                if (source.GetModule(x, y))
                {
                    count = count + 1;
                }

                x = x + 1;
            }

            y = y + 1;
        }

        return count;
    }

    static void ValidateMap(LinearTransitionMap map)
    {
        if (map == null || map.HorizontalTransitions == null || map.VerticalTransitions == null ||
            map.Width <= 0 || map.Height <= 0 ||
            map.HorizontalTransitions.Width != map.Width ||
            map.HorizontalTransitions.Height != map.Height ||
            map.VerticalTransitions.Width != map.Width ||
            map.VerticalTransitions.Height != map.Height)
        {
            throw new BarcodeValidationException("Linear transition map is invalid.");
        }
    }
}

internal static class LinearConnectedBlockFinder
{
    internal static LinearConnectedBlock[] FindDarkBlocks(ModuleMatrix matrix)
    {
        if (matrix == null)
        {
            throw new BarcodeValidationException("Linear block source must not be null.");
        }

        int pixelCount = CheckedMath.Multiply(matrix.Width, matrix.Height);
        bool[] visited = new bool[pixelCount];
        global::System.Collections.Generic.List<LinearConnectedBlock> blocks =
            new global::System.Collections.Generic.List<LinearConnectedBlock>();
        int y = 0;
        while (y < matrix.Height)
        {
            int x = 0;
            while (x < matrix.Width)
            {
                int index = CheckedMath.Add(CheckedMath.Multiply(y, matrix.Width), x);
                if (!visited[index] && matrix.GetModule(x, y))
                {
                    blocks.Add(CollectBlock(matrix, visited, x, y));
                }

                x = x + 1;
            }

            y = y + 1;
        }

        return blocks.ToArray();
    }

    static LinearConnectedBlock CollectBlock(
        ModuleMatrix matrix,
        bool[] visited,
        int startX,
        int startY)
    {
        global::System.Collections.Generic.List<int> queue =
            new global::System.Collections.Generic.List<int>();
        EnqueueDarkPixel(matrix, visited, queue, startX, startY);
        int left = startX;
        int right = startX;
        int top = startY;
        int bottom = startY;
        int queueIndex = 0;
        while (queueIndex < queue.Count)
        {
            int index = queue[queueIndex];
            int x = index % matrix.Width;
            int y = index / matrix.Width;
            if (x < left)
            {
                left = x;
            }

            if (x > right)
            {
                right = x;
            }

            if (y < top)
            {
                top = y;
            }

            if (y > bottom)
            {
                bottom = y;
            }

            EnqueueDarkPixel(matrix, visited, queue, x - 1, y);
            EnqueueDarkPixel(matrix, visited, queue, x + 1, y);
            EnqueueDarkPixel(matrix, visited, queue, x, y - 1);
            EnqueueDarkPixel(matrix, visited, queue, x, y + 1);
            queueIndex = queueIndex + 1;
        }

        LinearConnectedBlock block = new LinearConnectedBlock();
        block.Left = left;
        block.Top = top;
        block.Width = right - left + 1;
        block.Height = bottom - top + 1;
        block.PixelCount = queue.Count;
        block.IsVertical = block.Height >= block.Width;
        int major = block.IsVertical ? block.Height : block.Width;
        int minor = block.IsVertical ? block.Width : block.Height;
        block.MajorToMinorRatio = (double)major / minor;
        return block;
    }

    static void EnqueueDarkPixel(
        ModuleMatrix matrix,
        bool[] visited,
        global::System.Collections.Generic.List<int> queue,
        int x,
        int y)
    {
        if (x < 0 || x >= matrix.Width || y < 0 || y >= matrix.Height)
        {
            return;
        }

        int index = CheckedMath.Add(CheckedMath.Multiply(y, matrix.Width), x);
        if (visited[index] || !matrix.GetModule(x, y))
        {
            return;
        }

        visited[index] = true;
        queue.Add(index);
    }
}

internal static class LinearLineGrouper
{
    internal const double MinimumMajorToMinorRatio = 2.0;
    internal const double MinimumPerpendicularOverlap = 0.5;

    internal static LinearLineGroup[] Group(
        LinearConnectedBlock[] blocks,
        bool vertical,
        int maximumGap)
    {
        if (blocks == null)
        {
            throw new BarcodeValidationException("Linear blocks must not be null.");
        }

        if (maximumGap < 0)
        {
            throw new BarcodeValidationException("Linear line gap must not be negative.");
        }

        global::System.Collections.Generic.List<LinearConnectedBlock> eligible =
            new global::System.Collections.Generic.List<LinearConnectedBlock>();
        int index = 0;
        while (index < blocks.Length)
        {
            LinearConnectedBlock block = blocks[index];
            if (block == null || block.Width <= 0 || block.Height <= 0 || block.PixelCount <= 0)
            {
                throw new BarcodeValidationException("Linear connected block is invalid.");
            }

            if (block.IsVertical == vertical &&
                block.MajorToMinorRatio >= MinimumMajorToMinorRatio)
            {
                InsertInScanOrder(eligible, block, vertical);
            }

            index = index + 1;
        }

        global::System.Collections.Generic.List<LinearLineGroup> groups =
            new global::System.Collections.Generic.List<LinearLineGroup>();
        index = 0;
        while (index < eligible.Count)
        {
            LinearConnectedBlock block = eligible[index];
            LinearLineGroup? best = FindBestGroup(groups, block, vertical, maximumGap);
            if (best == null)
            {
                groups.Add(CreateGroup(block, vertical));
            }
            else
            {
                AddBlock(best, block);
            }

            index = index + 1;
        }

        return groups.ToArray();
    }

    static void InsertInScanOrder(
        global::System.Collections.Generic.List<LinearConnectedBlock> blocks,
        LinearConnectedBlock block,
        bool vertical)
    {
        int coordinate = vertical ? block.Left : block.Top;
        int index = 0;
        while (index < blocks.Count)
        {
            LinearConnectedBlock existing = blocks[index];
            int existingCoordinate = vertical ? existing.Left : existing.Top;
            if (coordinate < existingCoordinate)
            {
                blocks.Insert(index, block);
                return;
            }

            index = index + 1;
        }

        blocks.Add(block);
    }

    static LinearLineGroup? FindBestGroup(
        global::System.Collections.Generic.List<LinearLineGroup> groups,
        LinearConnectedBlock block,
        bool vertical,
        int maximumGap)
    {
        LinearLineGroup? best = null;
        int bestGap = int.MaxValue;
        int index = 0;
        while (index < groups.Count)
        {
            LinearLineGroup group = groups[index];
            int gap = vertical ? block.Left - group.Right : block.Top - group.Bottom;
            if (gap >= 0 && gap <= maximumGap && HasEnoughOverlap(group, block, vertical) &&
                gap < bestGap)
            {
                best = group;
                bestGap = gap;
            }

            index = index + 1;
        }

        return best;
    }

    static bool HasEnoughOverlap(
        LinearLineGroup group,
        LinearConnectedBlock block,
        bool vertical)
    {
        int groupStart = vertical ? group.Top : group.Left;
        int groupEnd = vertical ? group.Bottom : group.Right;
        int blockStart = vertical ? block.Top : block.Left;
        int blockEnd = blockStart + (vertical ? block.Height : block.Width);
        int overlapStart = global::System.Math.Max(groupStart, blockStart);
        int overlapEnd = global::System.Math.Min(groupEnd, blockEnd);
        int overlap = overlapEnd - overlapStart;
        if (overlap <= 0)
        {
            return false;
        }

        int groupLength = groupEnd - groupStart;
        int blockLength = blockEnd - blockStart;
        int shorter = global::System.Math.Min(groupLength, blockLength);
        return (double)overlap / shorter >= MinimumPerpendicularOverlap;
    }

    static LinearLineGroup CreateGroup(LinearConnectedBlock block, bool vertical)
    {
        LinearLineGroup group = new LinearLineGroup();
        group.IsVertical = vertical;
        group.Left = block.Left;
        group.Top = block.Top;
        group.Right = block.Left + block.Width;
        group.Bottom = block.Top + block.Height;
        group.Blocks = new LinearConnectedBlock[] { block };
        return group;
    }

    static void AddBlock(LinearLineGroup group, LinearConnectedBlock block)
    {
        LinearConnectedBlock[] expanded = new LinearConnectedBlock[group.Blocks.Length + 1];
        int index = 0;
        while (index < group.Blocks.Length)
        {
            expanded[index] = group.Blocks[index];
            index = index + 1;
        }

        expanded[expanded.Length - 1] = block;
        group.Blocks = expanded;
        group.Left = global::System.Math.Min(group.Left, block.Left);
        group.Top = global::System.Math.Min(group.Top, block.Top);
        group.Right = global::System.Math.Max(group.Right, block.Left + block.Width);
        group.Bottom = global::System.Math.Max(group.Bottom, block.Top + block.Height);
    }
}

internal static class LinearLocalizer
{
    internal const double MinimumOrientationScore = 0.6;

    internal static LinearBarcodeCandidate[] Locate(
        ModuleMatrix matrix,
        int minimumBarCount,
        int maximumGap)
    {
        if (matrix == null)
        {
            throw new BarcodeValidationException("Linear localization source must not be null.");
        }

        if (minimumBarCount < 2)
        {
            throw new BarcodeValidationException("Linear candidate minimum bar count must be at least two.");
        }

        if (maximumGap < 0)
        {
            throw new BarcodeValidationException("Linear candidate maximum gap must not be negative.");
        }

        LinearTransitionMap map = LinearTransitionAnalyzer.CreateMap(matrix);
        LinearConnectedBlock[] blocks = LinearConnectedBlockFinder.FindDarkBlocks(matrix);
        LinearLineGroup[] verticalGroups = LinearLineGrouper.Group(blocks, true, maximumGap);
        LinearLineGroup[] horizontalGroups = LinearLineGrouper.Group(blocks, false, maximumGap);
        global::System.Collections.Generic.List<LinearBarcodeCandidate> candidates =
            new global::System.Collections.Generic.List<LinearBarcodeCandidate>();
        AddCandidates(candidates, verticalGroups, map, minimumBarCount);
        AddCandidates(candidates, horizontalGroups, map, minimumBarCount);
        SortCandidates(candidates);
        return candidates.ToArray();
    }

    static void AddCandidates(
        global::System.Collections.Generic.List<LinearBarcodeCandidate> candidates,
        LinearLineGroup[] groups,
        LinearTransitionMap map,
        int minimumBarCount)
    {
        int index = 0;
        while (index < groups.Length)
        {
            LinearLineGroup group = groups[index];
            if (group.Blocks.Length >= minimumBarCount)
            {
                int horizontal = LinearTransitionAnalyzer.CountWithin(
                    map,
                    group.Left,
                    group.Top,
                    global::System.Math.Min(group.Right + 1, map.Width),
                    global::System.Math.Min(group.Bottom + 1, map.Height),
                    true);
                int vertical = LinearTransitionAnalyzer.CountWithin(
                    map,
                    group.Left,
                    group.Top,
                    global::System.Math.Min(group.Right + 1, map.Width),
                    global::System.Math.Min(group.Bottom + 1, map.Height),
                    false);
                int desired = group.IsVertical ? horizontal : vertical;
                int total = horizontal + vertical;
                double score = total == 0 ? 0.0 : (double)desired / total;
                if (score >= MinimumOrientationScore)
                {
                    candidates.Add(CreateCandidate(group, desired, score));
                }
            }

            index = index + 1;
        }
    }

    static LinearBarcodeCandidate CreateCandidate(
        LinearLineGroup group,
        int transitionCount,
        double orientationScore)
    {
        double left = group.Left;
        double top = group.Top;
        double right = group.Right;
        double bottom = group.Bottom;
        LinearBarcodeCandidate candidate = new LinearBarcodeCandidate();
        candidate.Bounds = GeometryMath.CreateRectangle(left, top, right - left, bottom - top);
        candidate.Location = GeometryMath.CreateQuadrilateral(
            GeometryMath.CreatePoint(left, top),
            GeometryMath.CreatePoint(right, top),
            GeometryMath.CreatePoint(right, bottom),
            GeometryMath.CreatePoint(left, bottom));
        candidate.BarsVertical = group.IsVertical;
        candidate.BarCount = group.Blocks.Length;
        candidate.TransitionCount = transitionCount;
        candidate.OrientationScore = orientationScore;
        candidate.Blocks = CopyBlocks(group.Blocks);
        return candidate;
    }

    static LinearConnectedBlock[] CopyBlocks(LinearConnectedBlock[] blocks)
    {
        LinearConnectedBlock[] copy = new LinearConnectedBlock[blocks.Length];
        int index = 0;
        while (index < blocks.Length)
        {
            LinearConnectedBlock source = blocks[index];
            LinearConnectedBlock target = new LinearConnectedBlock();
            target.Left = source.Left;
            target.Top = source.Top;
            target.Width = source.Width;
            target.Height = source.Height;
            target.PixelCount = source.PixelCount;
            target.IsVertical = source.IsVertical;
            target.MajorToMinorRatio = source.MajorToMinorRatio;
            copy[index] = target;
            index = index + 1;
        }

        return copy;
    }

    static void SortCandidates(
        global::System.Collections.Generic.List<LinearBarcodeCandidate> candidates)
    {
        int index = 1;
        while (index < candidates.Count)
        {
            LinearBarcodeCandidate current = candidates[index];
            int insertion = index;
            while (insertion > 0 && ComesBefore(current, candidates[insertion - 1]))
            {
                candidates[insertion] = candidates[insertion - 1];
                insertion = insertion - 1;
            }

            candidates[insertion] = current;
            index = index + 1;
        }
    }

    static bool ComesBefore(LinearBarcodeCandidate first, LinearBarcodeCandidate second)
    {
        if (first.Bounds.Y < second.Bounds.Y)
        {
            return true;
        }

        if (first.Bounds.Y > second.Bounds.Y)
        {
            return false;
        }

        return first.Bounds.X < second.Bounds.X;
    }
}
