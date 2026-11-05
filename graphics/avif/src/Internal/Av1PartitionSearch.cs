using System.Collections.Generic;

namespace avif;

internal enum Av1PartitionSearchEffort
{
    Fast = 0,
    Balanced = 1,
    Thorough = 2,
    Exhaustive = 3
}

internal class Av1PartitionBlockCandidate
{
    internal int MiRow;
    internal int MiColumn;
    internal Av1BlockSize BlockSize;
    internal long Distortion;
    internal int EstimatedRate;
    internal int Order;
    internal object? Value;
}

internal class Av1PartitionSearchRequest
{
    internal int MiRow;
    internal int MiColumn;
    internal int RowEnd;
    internal int ColumnEnd;
    internal Av1BlockSize BlockSize;
    internal Av1PartitionSearchEffort Effort;
    internal int RateDistortionLambdaQ8;
    internal int[] PartitionRateCosts;
    internal Av1PartitionBlockCandidate[] BlockCandidates;
    internal bool EnablePruning;

    internal Av1PartitionSearchRequest()
    {
        Effort = Av1PartitionSearchEffort.Balanced;
        PartitionRateCosts = new int[10];
        BlockCandidates = System.Array.Empty<Av1PartitionBlockCandidate>();
        EnablePruning = true;
    }
}

internal class Av1PartitionDecision
{
    internal int MiRow;
    internal int MiColumn;
    internal Av1BlockSize BlockSize;
    internal Av1PartitionType Partition;
    internal Av1PartitionBlockCandidate[] Blocks;
    internal Av1PartitionDecision[] Children;
    internal long Distortion;
    internal long EstimatedRate;
    internal long RateDistortionScore;

    internal Av1PartitionDecision()
    {
        Blocks = System.Array.Empty<Av1PartitionBlockCandidate>();
        Children = System.Array.Empty<Av1PartitionDecision>();
    }
}

internal class Av1PartitionSearchResult
{
    internal AvifResultCode Code;
    internal Av1PartitionDecision? BestDecision;
    internal int EvaluatedPartitionCount;
    internal int PrunedPartitionCount;

    internal Av1PartitionSearchResult(
        AvifResultCode code,
        Av1PartitionDecision? bestDecision,
        int evaluatedPartitionCount,
        int prunedPartitionCount)
    {
        Code = code;
        BestDecision = bestDecision;
        EvaluatedPartitionCount = evaluatedPartitionCount;
        PrunedPartitionCount = prunedPartitionCount;
    }
}

internal class Av1PartitionBlockLocation
{
    internal int MiRow;
    internal int MiColumn;
    internal Av1BlockSize BlockSize;

    internal Av1PartitionBlockLocation(
        int miRow,
        int miColumn,
        Av1BlockSize blockSize)
    {
        MiRow = miRow;
        MiColumn = miColumn;
        BlockSize = blockSize;
    }
}

// Encoder-side partition search for AV1 specification section 6.10.3.
internal class Av1PartitionSearcher
{
    internal Av1PartitionSearchRequest Request;
    internal int EvaluatedPartitionCount;
    internal int PrunedPartitionCount;

    internal Av1PartitionSearcher(Av1PartitionSearchRequest request)
    {
        Request = request;
    }

    internal static Av1PartitionSearchResult Search(
        Av1PartitionSearchRequest request)
    {
        AvifResultCode validation = Validate(request);
        if (validation != AvifResultCode.Success)
        {
            return new Av1PartitionSearchResult(validation, null, 0, 0);
        }

        Av1PartitionSearcher searcher = new Av1PartitionSearcher(request);
        Av1PartitionDecision? best = searcher.SearchNode(
            request.MiRow,
            request.MiColumn,
            request.BlockSize);
        AvifResultCode code = best == null
            ? AvifResultCode.EncodeFailed
            : AvifResultCode.Success;
        return new Av1PartitionSearchResult(
            code,
            best,
            searcher.EvaluatedPartitionCount,
            searcher.PrunedPartitionCount);
    }

    Av1PartitionDecision? SearchNode(
        int miRow,
        int miColumn,
        Av1BlockSize blockSize)
    {
        Av1PartitionType forced = Av1PartitionRules.GetForcedPartition(
            miRow,
            miColumn,
            blockSize,
            Request.RowEnd,
            Request.ColumnEnd);
        if (forced != Av1PartitionType.Invalid)
        {
            EvaluatedPartitionCount++;
            return EvaluatePartition(miRow, miColumn, blockSize, forced, null);
        }

        Av1PartitionDecision? best = null;
        int partitionIndex = 0;
        while (partitionIndex <= (int)Av1PartitionType.Vertical4)
        {
            Av1PartitionType partition = (Av1PartitionType)partitionIndex;
            if (IsEnabled(partition, Request.Effort)
                && Av1PartitionRules.IsAllowed(blockSize, partition))
            {
                EvaluatedPartitionCount++;
                Av1PartitionDecision? candidate = EvaluatePartition(
                    miRow,
                    miColumn,
                    blockSize,
                    partition,
                    best);
                if (candidate != null
                    && (best == null || Compare(candidate, best) < 0))
                {
                    best = candidate;
                }
            }
            partitionIndex++;
        }
        return best;
    }

    Av1PartitionDecision? EvaluatePartition(
        int miRow,
        int miColumn,
        Av1BlockSize blockSize,
        Av1PartitionType partition,
        Av1PartitionDecision? incumbent)
    {
        if (partition == Av1PartitionType.Split)
        {
            return EvaluateSplit(miRow, miColumn, blockSize, incumbent);
        }

        List<Av1PartitionBlockLocation> locations = CreateBlockLocations(
            miRow,
            miColumn,
            blockSize,
            partition);
        Av1PartitionDecision decision = CreateDecision(
            miRow,
            miColumn,
            blockSize,
            partition);
        decision.EstimatedRate = Request.PartitionRateCosts[(int)partition];
        List<Av1PartitionBlockCandidate> blocks =
            new List<Av1PartitionBlockCandidate>();
        int index = 0;
        while (index < locations.Count)
        {
            Av1PartitionBlockLocation location = locations[index];
            Av1PartitionBlockCandidate? block = FindBestBlock(location);
            if (block == null)
            {
                return null;
            }
            blocks.Add(block);
            decision.Distortion = AddSaturated(
                decision.Distortion,
                block.Distortion);
            decision.EstimatedRate = AddSaturated(
                decision.EstimatedRate,
                block.EstimatedRate);
            decision.RateDistortionScore = GetScore(
                decision.Distortion,
                decision.EstimatedRate,
                Request.RateDistortionLambdaQ8);
            if (ShouldPrune(decision, incumbent))
            {
                PrunedPartitionCount++;
                return null;
            }
            index++;
        }
        decision.Blocks = blocks.ToArray();
        return decision;
    }

    Av1PartitionDecision? EvaluateSplit(
        int miRow,
        int miColumn,
        Av1BlockSize blockSize,
        Av1PartitionDecision? incumbent)
    {
        Av1BlockSize childSize = Av1ConversionTables.GetPartitionSubsize(
            Av1PartitionType.Split,
            blockSize);
        int half = Av1ConversionTables.GetBlockMiWidth(blockSize) >> 1;
        bool hasRows = miRow + half < Request.RowEnd;
        bool hasColumns = miColumn + half < Request.ColumnEnd;
        List<Av1PartitionDecision> children = new List<Av1PartitionDecision>();
        Av1PartitionDecision decision = CreateDecision(
            miRow,
            miColumn,
            blockSize,
            Av1PartitionType.Split);
        decision.EstimatedRate =
            Request.PartitionRateCosts[(int)Av1PartitionType.Split];
        if (!AddSplitChild(decision, children, miRow, miColumn, childSize, incumbent))
        {
            return null;
        }
        if (hasColumns && !AddSplitChild(
            decision,
            children,
            miRow,
            miColumn + half,
            childSize,
            incumbent))
        {
            return null;
        }
        if (hasRows && !AddSplitChild(
            decision,
            children,
            miRow + half,
            miColumn,
            childSize,
            incumbent))
        {
            return null;
        }
        if (hasRows && hasColumns && !AddSplitChild(
            decision,
            children,
            miRow + half,
            miColumn + half,
            childSize,
            incumbent))
        {
            return null;
        }
        decision.Children = children.ToArray();
        return decision;
    }

    bool AddSplitChild(
        Av1PartitionDecision decision,
        List<Av1PartitionDecision> children,
        int miRow,
        int miColumn,
        Av1BlockSize blockSize,
        Av1PartitionDecision? incumbent)
    {
        Av1PartitionDecision? child = SearchNode(miRow, miColumn, blockSize);
        if (child == null)
        {
            return false;
        }
        children.Add(child);
        decision.Distortion = AddSaturated(
            decision.Distortion,
            child.Distortion);
        decision.EstimatedRate = AddSaturated(
            decision.EstimatedRate,
            child.EstimatedRate);
        decision.RateDistortionScore = GetScore(
            decision.Distortion,
            decision.EstimatedRate,
            Request.RateDistortionLambdaQ8);
        if (ShouldPrune(decision, incumbent))
        {
            PrunedPartitionCount++;
            return false;
        }
        return true;
    }

    Av1PartitionBlockCandidate? FindBestBlock(
        Av1PartitionBlockLocation location)
    {
        Av1PartitionBlockCandidate? best = null;
        int index = 0;
        while (index < Request.BlockCandidates.Length)
        {
            Av1PartitionBlockCandidate candidate = Request.BlockCandidates[index];
            if (candidate.MiRow == location.MiRow
                && candidate.MiColumn == location.MiColumn
                && candidate.BlockSize == location.BlockSize
                && (best == null || CompareBlocks(candidate, best) < 0))
            {
                best = candidate;
            }
            index++;
        }
        return best;
    }

    int CompareBlocks(
        Av1PartitionBlockCandidate first,
        Av1PartitionBlockCandidate second)
    {
        long firstScore = GetScore(
            first.Distortion,
            first.EstimatedRate,
            Request.RateDistortionLambdaQ8);
        long secondScore = GetScore(
            second.Distortion,
            second.EstimatedRate,
            Request.RateDistortionLambdaQ8);
        if (firstScore != secondScore)
        {
            return firstScore < secondScore ? -1 : 1;
        }
        if (first.Distortion != second.Distortion)
        {
            return first.Distortion < second.Distortion ? -1 : 1;
        }
        if (first.EstimatedRate != second.EstimatedRate)
        {
            return first.EstimatedRate < second.EstimatedRate ? -1 : 1;
        }
        return first.Order < second.Order
            ? -1
            : (first.Order > second.Order ? 1 : 0);
    }

    bool ShouldPrune(
        Av1PartitionDecision partial,
        Av1PartitionDecision? incumbent)
    {
        return Request.EnablePruning
            && incumbent != null
            && partial.RateDistortionScore > incumbent.RateDistortionScore;
    }

    static Av1PartitionDecision CreateDecision(
        int miRow,
        int miColumn,
        Av1BlockSize blockSize,
        Av1PartitionType partition)
    {
        Av1PartitionDecision decision = new Av1PartitionDecision();
        decision.MiRow = miRow;
        decision.MiColumn = miColumn;
        decision.BlockSize = blockSize;
        decision.Partition = partition;
        return decision;
    }

    List<Av1PartitionBlockLocation> CreateBlockLocations(
        int miRow,
        int miColumn,
        Av1BlockSize blockSize,
        Av1PartitionType partition)
    {
        List<Av1PartitionBlockLocation> locations =
            new List<Av1PartitionBlockLocation>();
        Av1BlockSize subsize = Av1ConversionTables.GetPartitionSubsize(
            partition,
            blockSize);
        int half = Av1ConversionTables.GetBlockMiWidth(blockSize) >> 1;
        if (partition == Av1PartitionType.None)
        {
            AddLocation(locations, miRow, miColumn, subsize);
        }
        else if (partition == Av1PartitionType.Horizontal)
        {
            AddLocation(locations, miRow, miColumn, subsize);
            if (miRow + half < Request.RowEnd)
            {
                AddLocation(locations, miRow + half, miColumn, subsize);
            }
        }
        else if (partition == Av1PartitionType.Vertical)
        {
            AddLocation(locations, miRow, miColumn, subsize);
            if (miColumn + half < Request.ColumnEnd)
            {
                AddLocation(locations, miRow, miColumn + half, subsize);
            }
        }
        else if (partition == Av1PartitionType.HorizontalA)
        {
            Av1BlockSize split = Av1ConversionTables.GetPartitionSubsize(
                Av1PartitionType.Split,
                blockSize);
            AddLocation(locations, miRow, miColumn, split);
            AddLocation(locations, miRow, miColumn + half, split);
            AddLocation(locations, miRow + half, miColumn, subsize);
        }
        else if (partition == Av1PartitionType.HorizontalB)
        {
            Av1BlockSize split = Av1ConversionTables.GetPartitionSubsize(
                Av1PartitionType.Split,
                blockSize);
            AddLocation(locations, miRow, miColumn, subsize);
            AddLocation(locations, miRow + half, miColumn, split);
            AddLocation(locations, miRow + half, miColumn + half, split);
        }
        else if (partition == Av1PartitionType.VerticalA)
        {
            Av1BlockSize split = Av1ConversionTables.GetPartitionSubsize(
                Av1PartitionType.Split,
                blockSize);
            AddLocation(locations, miRow, miColumn, split);
            AddLocation(locations, miRow + half, miColumn, split);
            AddLocation(locations, miRow, miColumn + half, subsize);
        }
        else if (partition == Av1PartitionType.VerticalB)
        {
            Av1BlockSize split = Av1ConversionTables.GetPartitionSubsize(
                Av1PartitionType.Split,
                blockSize);
            AddLocation(locations, miRow, miColumn, subsize);
            AddLocation(locations, miRow, miColumn + half, split);
            AddLocation(locations, miRow + half, miColumn + half, split);
        }
        else if (partition == Av1PartitionType.Horizontal4)
        {
            int quarter = half >> 1;
            int index = 0;
            while (index < 4)
            {
                int row = miRow + index * quarter;
                if (row < Request.RowEnd)
                {
                    AddLocation(locations, row, miColumn, subsize);
                }
                index++;
            }
        }
        else if (partition == Av1PartitionType.Vertical4)
        {
            int quarter = half >> 1;
            int index = 0;
            while (index < 4)
            {
                int column = miColumn + index * quarter;
                if (column < Request.ColumnEnd)
                {
                    AddLocation(locations, miRow, column, subsize);
                }
                index++;
            }
        }
        return locations;
    }

    static void AddLocation(
        List<Av1PartitionBlockLocation> locations,
        int miRow,
        int miColumn,
        Av1BlockSize blockSize)
    {
        locations.Add(new Av1PartitionBlockLocation(
            miRow,
            miColumn,
            blockSize));
    }

    static bool IsEnabled(
        Av1PartitionType partition,
        Av1PartitionSearchEffort effort)
    {
        if (partition == Av1PartitionType.None
            || partition == Av1PartitionType.Split)
        {
            return true;
        }
        if (effort == Av1PartitionSearchEffort.Fast)
        {
            return false;
        }
        if (partition == Av1PartitionType.Horizontal
            || partition == Av1PartitionType.Vertical)
        {
            return true;
        }
        if (effort == Av1PartitionSearchEffort.Balanced)
        {
            return false;
        }
        if (partition != Av1PartitionType.Horizontal4
            && partition != Av1PartitionType.Vertical4)
        {
            return true;
        }
        return effort == Av1PartitionSearchEffort.Exhaustive;
    }

    static int Compare(
        Av1PartitionDecision first,
        Av1PartitionDecision second)
    {
        if (first.RateDistortionScore != second.RateDistortionScore)
        {
            return first.RateDistortionScore < second.RateDistortionScore ? -1 : 1;
        }
        if (first.Distortion != second.Distortion)
        {
            return first.Distortion < second.Distortion ? -1 : 1;
        }
        if (first.EstimatedRate != second.EstimatedRate)
        {
            return first.EstimatedRate < second.EstimatedRate ? -1 : 1;
        }
        return first.Partition < second.Partition
            ? -1
            : (first.Partition > second.Partition ? 1 : 0);
    }

    static long GetScore(long distortion, long rate, int lambdaQ8)
    {
        if (lambdaQ8 == 0 || rate == 0)
        {
            return distortion;
        }
        if (rate > (long.MaxValue - 128) / lambdaQ8)
        {
            return long.MaxValue;
        }
        long rateCost = (rate * lambdaQ8 + 128) >> 8;
        return AddSaturated(distortion, rateCost);
    }

    static long AddSaturated(long first, long second)
    {
        return second > long.MaxValue - first
            ? long.MaxValue
            : first + second;
    }

    static AvifResultCode Validate(Av1PartitionSearchRequest request)
    {
        if (request == null || request.BlockCandidates == null
            || request.PartitionRateCosts == null
            || request.PartitionRateCosts.Length < 10
            || request.MiRow < 0 || request.MiColumn < 0
            || request.RowEnd <= request.MiRow
            || request.ColumnEnd <= request.MiColumn
            || request.RateDistortionLambdaQ8 < 0
            || request.Effort < Av1PartitionSearchEffort.Fast
            || request.Effort > Av1PartitionSearchEffort.Exhaustive
            || Av1ConversionTables.GetSquareBlockIndex(request.BlockSize) < 0
            || request.RowEnd > request.MiRow
                + Av1ConversionTables.GetBlockMiHeight(request.BlockSize)
            || request.ColumnEnd > request.MiColumn
                + Av1ConversionTables.GetBlockMiWidth(request.BlockSize))
        {
            return AvifResultCode.InvalidArgument;
        }
        int partition = 0;
        while (partition < 10)
        {
            if (request.PartitionRateCosts[partition] < 0)
            {
                return AvifResultCode.InvalidArgument;
            }
            partition++;
        }
        int candidateIndex = 0;
        while (candidateIndex < request.BlockCandidates.Length)
        {
            Av1PartitionBlockCandidate candidate =
                request.BlockCandidates[candidateIndex];
            if (candidate == null || candidate.MiRow < request.MiRow
                || candidate.MiColumn < request.MiColumn
                || candidate.MiRow >= request.RowEnd
                || candidate.MiColumn >= request.ColumnEnd
                || candidate.Distortion < 0 || candidate.EstimatedRate < 0
                || candidate.BlockSize < Av1BlockSize.Block4x4
                || candidate.BlockSize >= Av1BlockSize.Invalid)
            {
                return AvifResultCode.InvalidArgument;
            }
            candidateIndex++;
        }
        return AvifResultCode.Success;
    }
}
