using System;
using System.Collections.Generic;

namespace avif;

internal enum Av1IntraCandidateKind
{
    Ordinary = 0,
    Filter = 1,
    Cfl = 2,
    Palette = 3,
    IntraBlockCopy = 4
}

internal class Av1IntraCandidateEvaluationRequest
{
    internal PlaneBuffer? Reconstructed;
    internal int[] Source;
    internal int SourceStride;
    internal int X;
    internal int Y;
    internal int Width;
    internal int Height;
    internal int BitDepth;
    internal bool HaveAbove;
    internal bool HaveLeft;
    internal bool HaveAboveRight;
    internal bool HaveBelowLeft;
    internal bool AllowDirectional;
    internal bool AllowSmooth;
    internal bool AllowPaeth;
    internal bool AllowFilterIntra;
    internal bool AllowCfl;
    internal bool AllowPalette;
    internal bool AllowIntraBlockCopy;
    internal int[]? CflLuma;
    internal int CflLumaWidth;
    internal int CflLumaHeight;
    internal int CflLumaStride;
    internal bool SubsamplingX;
    internal bool SubsamplingY;
    internal int RateDistortionLambdaQ8;
    internal int MaximumIntraBlockCopyCandidates;

    internal Av1IntraCandidateEvaluationRequest()
    {
        Source = System.Array.Empty<int>();
        AllowDirectional = true;
        AllowSmooth = true;
        AllowPaeth = true;
        MaximumIntraBlockCopyCandidates = 32;
    }
}

internal class Av1IntraCandidate
{
    internal Av1IntraCandidateKind Kind;
    internal Av1PredictionMode Mode;
    internal int AngleDelta;
    internal Av1FilterIntraMode FilterIntraMode;
    internal int CflAlphaQ3;
    internal int[] PaletteColors;
    internal byte[] PaletteMap;
    internal int IntraBlockCopySourceX;
    internal int IntraBlockCopySourceY;
    internal int[] Prediction;
    internal long Distortion;
    internal int EstimatedRate;
    internal long RateDistortionScore;
    internal int Order;

    internal Av1IntraCandidate()
    {
        FilterIntraMode = Av1FilterIntraMode.None;
        PaletteColors = System.Array.Empty<int>();
        PaletteMap = System.Array.Empty<byte>();
        Prediction = System.Array.Empty<int>();
    }
}

internal class Av1IntraCandidateEvaluationResult
{
    internal AvifResultCode Code;
    internal Av1IntraCandidate[] Candidates;
    internal Av1IntraCandidate? BestCandidate;

    internal Av1IntraCandidateEvaluationResult(
        AvifResultCode code,
        Av1IntraCandidate[] candidates,
        Av1IntraCandidate? bestCandidate)
    {
        Code = code;
        Candidates = candidates;
        BestCandidate = bestCandidate;
    }

    internal AvifResultCode Commit(
        Av1IntraCandidateEvaluationRequest request)
    {
        if (Code != AvifResultCode.Success || BestCandidate == null
            || request == null || request.Reconstructed == null
            || request.Reconstructed.GetStateCode() != AvifResultCode.Success
            || BestCandidate.Prediction.Length != request.Width * request.Height)
        {
            return AvifResultCode.InvalidArgument;
        }

        int row = 0;
        while (row < request.Height)
        {
            Span<byte> destination = request.Reconstructed.GetRowSpan(request.Y + row);
            int column = 0;
            while (column < request.Width)
            {
                int value = BestCandidate.Prediction[row * request.Width + column];
                int offset = (request.X + column)
                    * request.Reconstructed.BytesPerSample;
                destination[offset] = IntegerConversion.ToByte(value);
                if (request.Reconstructed.BytesPerSample == 2)
                {
                    destination[offset + 1] = IntegerConversion.ToByte(value >> 8);
                }
                column++;
            }
            row++;
        }
        return AvifResultCode.Success;
    }
}

internal class Av1IntraBlockCopySearchCandidate
{
    internal int X;
    internal int Y;
    internal long Distortion;

    internal Av1IntraBlockCopySearchCandidate(int x, int y, long distortion)
    {
        X = x;
        Y = y;
        Distortion = distortion;
    }
}

// Encoder-side candidate evaluation for AV1 specification sections 7.11.2,
// 7.11.4, and 7.11.5.
internal static class Av1IntraCandidateEvaluator
{
    internal static Av1IntraCandidateEvaluationResult Evaluate(
        Av1IntraCandidateEvaluationRequest request)
    {
        AvifResultCode validation = Validate(request);
        if (validation != AvifResultCode.Success)
        {
            return Fail(validation);
        }

        List<Av1IntraCandidate> candidates = new List<Av1IntraCandidate>();
        int order = 0;
        AddOrdinaryCandidate(
            request, candidates, Av1PredictionMode.Dc, 0, order++);
        AddDirectionalCandidates(request, candidates, order);
        order += request.AllowDirectional ? 44 : 0;
        if (request.AllowSmooth)
        {
            AddOrdinaryCandidate(
                request, candidates, Av1PredictionMode.Smooth, 0, order++);
            AddOrdinaryCandidate(
                request, candidates, Av1PredictionMode.SmoothVertical, 0, order++);
            AddOrdinaryCandidate(
                request, candidates, Av1PredictionMode.SmoothHorizontal, 0, order++);
        }
        if (request.AllowPaeth)
        {
            AddOrdinaryCandidate(
                request, candidates, Av1PredictionMode.Paeth, 0, order++);
        }
        if (request.AllowFilterIntra)
        {
            int filter = (int)Av1FilterIntraMode.FilterDc;
            while (filter <= (int)Av1FilterIntraMode.FilterPaeth)
            {
                Av1IntraCandidate candidate = new Av1IntraCandidate();
                candidate.Kind = Av1IntraCandidateKind.Filter;
                candidate.Mode = Av1PredictionMode.Dc;
                candidate.FilterIntraMode = (Av1FilterIntraMode)filter;
                candidate.Order = order++;
                EvaluateCandidate(request, candidate, candidates);
                filter++;
            }
        }
        if (request.AllowCfl)
        {
            int alpha = -16;
            while (alpha <= 16)
            {
                Av1IntraCandidate candidate = new Av1IntraCandidate();
                candidate.Kind = Av1IntraCandidateKind.Cfl;
                candidate.Mode = Av1PredictionMode.Dc;
                candidate.CflAlphaQ3 = alpha;
                candidate.Order = order++;
                EvaluateCandidate(request, candidate, candidates);
                alpha++;
            }
        }
        if (request.AllowPalette)
        {
            order = AddPaletteCandidates(request, candidates, order);
        }
        if (request.AllowIntraBlockCopy)
        {
            AddIntraBlockCopyCandidates(request, candidates, order);
        }

        if (candidates.Count == 0)
        {
            return Fail(AvifResultCode.InvalidState);
        }
        Av1IntraCandidate best = candidates[0];
        int index = 1;
        while (index < candidates.Count)
        {
            if (Compare(candidates[index], best) < 0)
            {
                best = candidates[index];
            }
            index++;
        }
        return new Av1IntraCandidateEvaluationResult(
            AvifResultCode.Success,
            candidates.ToArray(),
            best);
    }

    static void AddDirectionalCandidates(
        Av1IntraCandidateEvaluationRequest request,
        List<Av1IntraCandidate> candidates,
        int order)
    {
        if (!request.AllowDirectional)
        {
            return;
        }
        AddOrdinaryCandidate(
            request, candidates, Av1PredictionMode.Vertical, 0, order++);
        AddOrdinaryCandidate(
            request, candidates, Av1PredictionMode.Horizontal, 0, order++);
        int mode = (int)Av1PredictionMode.Directional45;
        while (mode <= (int)Av1PredictionMode.Directional67)
        {
            int delta = -Av1SyntaxConstants.MaxAngleDelta;
            while (delta <= Av1SyntaxConstants.MaxAngleDelta)
            {
                AddOrdinaryCandidate(
                    request,
                    candidates,
                    (Av1PredictionMode)mode,
                    delta,
                    order++);
                delta++;
            }
            mode++;
        }
    }

    static void AddOrdinaryCandidate(
        Av1IntraCandidateEvaluationRequest request,
        List<Av1IntraCandidate> candidates,
        Av1PredictionMode mode,
        int angleDelta,
        int order)
    {
        Av1IntraCandidate candidate = new Av1IntraCandidate();
        candidate.Kind = Av1IntraCandidateKind.Ordinary;
        candidate.Mode = mode;
        candidate.AngleDelta = angleDelta;
        candidate.Order = order;
        EvaluateCandidate(request, candidate, candidates);
    }

    static int AddPaletteCandidates(
        Av1IntraCandidateEvaluationRequest request,
        List<Av1IntraCandidate> candidates,
        int order)
    {
        int[] colors = GetSortedUniqueColors(request);
        if (colors.Length < Av1SyntaxConstants.MinPaletteSize)
        {
            return order;
        }
        int maximumSize = colors.Length < Av1SyntaxConstants.MaxPaletteSize
            ? colors.Length
            : Av1SyntaxConstants.MaxPaletteSize;
        int size = Av1SyntaxConstants.MinPaletteSize;
        while (size <= maximumSize)
        {
            Av1IntraCandidate candidate = new Av1IntraCandidate();
            candidate.Kind = Av1IntraCandidateKind.Palette;
            candidate.Mode = Av1PredictionMode.Dc;
            candidate.PaletteColors = SelectPalette(colors, size);
            candidate.PaletteMap = CreatePaletteMap(request, candidate.PaletteColors);
            candidate.Order = order++;
            EvaluateCandidate(request, candidate, candidates);
            size++;
        }
        return order;
    }

    static void AddIntraBlockCopyCandidates(
        Av1IntraCandidateEvaluationRequest request,
        List<Av1IntraCandidate> candidates,
        int order)
    {
        List<Av1IntraBlockCopySearchCandidate> search =
            new List<Av1IntraBlockCopySearchCandidate>();
        int sourceY = 0;
        while (sourceY <= request.Y)
        {
            int sourceX = 0;
            while (sourceX <= (request.Reconstructed ?? throw new InvalidOperationException("Invalid internal state.")).Width - request.Width)
            {
                bool earlier = sourceY + request.Height <= request.Y
                    || sourceY == request.Y
                        && sourceX + request.Width <= request.X;
                if (earlier)
                {
                    long distortion = GetCopyDistortion(request, sourceX, sourceY);
                    InsertCopyCandidate(
                        search,
                        new Av1IntraBlockCopySearchCandidate(
                            sourceX, sourceY, distortion),
                        request.MaximumIntraBlockCopyCandidates);
                }
                sourceX += 4;
            }
            sourceY += 4;
        }
        int index = 0;
        while (index < search.Count)
        {
            Av1IntraCandidate candidate = new Av1IntraCandidate();
            candidate.Kind = Av1IntraCandidateKind.IntraBlockCopy;
            candidate.Mode = Av1PredictionMode.Dc;
            candidate.IntraBlockCopySourceX = search[index].X;
            candidate.IntraBlockCopySourceY = search[index].Y;
            candidate.Order = order++;
            EvaluateCandidate(request, candidate, candidates);
            index++;
        }
    }

    static void EvaluateCandidate(
        Av1IntraCandidateEvaluationRequest evaluation,
        Av1IntraCandidate candidate,
        List<Av1IntraCandidate> candidates)
    {
        PlaneBuffer scratch = (evaluation.Reconstructed ?? throw new InvalidOperationException("Invalid internal state.")).CreateCopy();
        if (scratch.Code != AvifResultCode.Success)
        {
            scratch.Close();
            return;
        }
        Av1IntraPredictionRequest request = CreatePredictionRequest(
            evaluation, candidate, scratch);
        AvifResultCode code = Av1IntraPredictor.Predict(request);
        if (code == AvifResultCode.Success)
        {
            candidate.Prediction = ReadPrediction(evaluation, scratch);
            candidate.Distortion = GetDistortion(evaluation, candidate.Prediction);
            candidate.EstimatedRate = EstimateRate(evaluation, candidate);
            candidate.RateDistortionScore = candidate.Distortion
                + ((long)evaluation.RateDistortionLambdaQ8
                    * candidate.EstimatedRate + 128 >> 8);
            candidates.Add(candidate);
        }
        scratch.Close();
    }

    static Av1IntraPredictionRequest CreatePredictionRequest(
        Av1IntraCandidateEvaluationRequest evaluation,
        Av1IntraCandidate candidate,
        PlaneBuffer scratch)
    {
        Av1IntraPredictionRequest request = new Av1IntraPredictionRequest();
        request.Destination = scratch;
        request.X = evaluation.X;
        request.Y = evaluation.Y;
        request.Width = evaluation.Width;
        request.Height = evaluation.Height;
        request.BitDepth = evaluation.BitDepth;
        request.Mode = candidate.Mode;
        request.AngleDelta = candidate.AngleDelta;
        request.HaveAbove = evaluation.HaveAbove;
        request.HaveLeft = evaluation.HaveLeft;
        request.HaveAboveRight = evaluation.HaveAboveRight;
        request.HaveBelowLeft = evaluation.HaveBelowLeft;
        request.UseFilterIntra = candidate.Kind == Av1IntraCandidateKind.Filter;
        request.FilterIntraMode = candidate.FilterIntraMode;
        request.UseCfl = candidate.Kind == Av1IntraCandidateKind.Cfl;
        request.CflLuma = evaluation.CflLuma;
        request.CflLumaWidth = evaluation.CflLumaWidth;
        request.CflLumaHeight = evaluation.CflLumaHeight;
        request.CflLumaStride = evaluation.CflLumaStride;
        request.SubsamplingX = evaluation.SubsamplingX;
        request.SubsamplingY = evaluation.SubsamplingY;
        request.CflAlphaQ3 = candidate.CflAlphaQ3;
        request.UsePalette = candidate.Kind == Av1IntraCandidateKind.Palette;
        request.PaletteColors = candidate.PaletteColors;
        request.PaletteMap = candidate.PaletteMap;
        request.PaletteStride = evaluation.Width;
        request.UseIntraBlockCopy =
            candidate.Kind == Av1IntraCandidateKind.IntraBlockCopy;
        request.IntraBlockCopySourceX = candidate.IntraBlockCopySourceX;
        request.IntraBlockCopySourceY = candidate.IntraBlockCopySourceY;
        return request;
    }

    static int[] ReadPrediction(
        Av1IntraCandidateEvaluationRequest request,
        PlaneBuffer scratch)
    {
        int[] prediction = new int[request.Width * request.Height];
        int row = 0;
        while (row < request.Height)
        {
            Span<byte> source = scratch.GetRowSpan(request.Y + row);
            int column = 0;
            while (column < request.Width)
            {
                int offset = (request.X + column) * scratch.BytesPerSample;
                prediction[row * request.Width + column] =
                    scratch.BytesPerSample == 1
                        ? source[offset]
                        : source[offset] | source[offset + 1] << 8;
                column++;
            }
            row++;
        }
        return prediction;
    }

    static long GetDistortion(
        Av1IntraCandidateEvaluationRequest request,
        int[] prediction)
    {
        long distortion = 0;
        int row = 0;
        while (row < request.Height)
        {
            int column = 0;
            while (column < request.Width)
            {
                long difference = request.Source[row * request.SourceStride + column]
                    - prediction[row * request.Width + column];
                distortion += difference * difference;
                column++;
            }
            row++;
        }
        return distortion;
    }

    static long GetCopyDistortion(
        Av1IntraCandidateEvaluationRequest request,
        int sourceX,
        int sourceY)
    {
        long distortion = 0;
        int row = 0;
        while (row < request.Height)
        {
            Span<byte> source = (request.Reconstructed ?? throw new InvalidOperationException("Invalid internal state.")).GetRowSpan(sourceY + row);
            int column = 0;
            while (column < request.Width)
            {
                int offset = (sourceX + column)
                    * request.Reconstructed.BytesPerSample;
                int predicted = request.Reconstructed.BytesPerSample == 1
                    ? source[offset]
                    : source[offset] | source[offset + 1] << 8;
                long difference = request.Source[row * request.SourceStride + column]
                    - predicted;
                distortion += difference * difference;
                column++;
            }
            row++;
        }
        return distortion;
    }

    static void InsertCopyCandidate(
        List<Av1IntraBlockCopySearchCandidate> candidates,
        Av1IntraBlockCopySearchCandidate candidate,
        int maximum)
    {
        int index = 0;
        while (index < candidates.Count
            && CompareCopy(candidates[index], candidate) <= 0)
        {
            index++;
        }
        candidates.Insert(index, candidate);
        if (candidates.Count > maximum)
        {
            candidates.RemoveAt(candidates.Count - 1);
        }
    }

    static int CompareCopy(
        Av1IntraBlockCopySearchCandidate first,
        Av1IntraBlockCopySearchCandidate second)
    {
        if (first.Distortion != second.Distortion)
        {
            return first.Distortion < second.Distortion ? -1 : 1;
        }
        if (first.Y != second.Y)
        {
            return first.Y < second.Y ? -1 : 1;
        }
        if (first.X != second.X)
        {
            return first.X < second.X ? -1 : 1;
        }
        return 0;
    }

    static int[] GetSortedUniqueColors(
        Av1IntraCandidateEvaluationRequest request)
    {
        List<int> colors = new List<int>();
        int row = 0;
        while (row < request.Height)
        {
            int column = 0;
            while (column < request.Width)
            {
                int value = request.Source[row * request.SourceStride + column];
                int index = 0;
                while (index < colors.Count && colors[index] < value)
                {
                    index++;
                }
                if (index == colors.Count || colors[index] != value)
                {
                    colors.Insert(index, value);
                }
                column++;
            }
            row++;
        }
        return colors.ToArray();
    }

    static int[] SelectPalette(int[] colors, int size)
    {
        int[] palette = new int[size];
        int index = 0;
        while (index < size)
        {
            int colorIndex = size == 1
                ? 0
                : (index * (colors.Length - 1) + (size - 1) / 2) / (size - 1);
            palette[index] = colors[colorIndex];
            index++;
        }
        return palette;
    }

    static byte[] CreatePaletteMap(
        Av1IntraCandidateEvaluationRequest request,
        int[] palette)
    {
        byte[] map = new byte[request.Width * request.Height];
        int row = 0;
        while (row < request.Height)
        {
            int column = 0;
            while (column < request.Width)
            {
                int value = request.Source[row * request.SourceStride + column];
                int best = 0;
                int bestDistance = Absolute(value - palette[0]);
                int index = 1;
                while (index < palette.Length)
                {
                    int distance = Absolute(value - palette[index]);
                    if (distance < bestDistance)
                    {
                        best = index;
                        bestDistance = distance;
                    }
                    index++;
                }
                map[row * request.Width + column] = (byte)best;
                column++;
            }
            row++;
        }
        return map;
    }

    static int EstimateRate(
        Av1IntraCandidateEvaluationRequest request,
        Av1IntraCandidate candidate)
    {
        if (candidate.Kind == Av1IntraCandidateKind.Filter)
        {
            return 7;
        }
        if (candidate.Kind == Av1IntraCandidateKind.Cfl)
        {
            return 7 + SignedMagnitudeBits(candidate.CflAlphaQ3);
        }
        if (candidate.Kind == Av1IntraCandidateKind.Palette)
        {
            int indexBits = CeilLog2(candidate.PaletteColors.Length);
            return 4 + candidate.PaletteColors.Length * request.BitDepth
                + request.Width * request.Height * indexBits;
        }
        if (candidate.Kind == Av1IntraCandidateKind.IntraBlockCopy)
        {
            return 4
                + SignedMagnitudeBits(candidate.IntraBlockCopySourceX - request.X)
                + SignedMagnitudeBits(candidate.IntraBlockCopySourceY - request.Y);
        }
        if (candidate.Mode >= Av1PredictionMode.Directional45
            && candidate.Mode <= Av1PredictionMode.Directional67)
        {
            return 5 + SignedMagnitudeBits(candidate.AngleDelta);
        }
        return candidate.Mode == Av1PredictionMode.Dc ? 2 : 5;
    }

    static int Compare(Av1IntraCandidate first, Av1IntraCandidate second)
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
        return first.Order < second.Order ? -1 : (first.Order > second.Order ? 1 : 0);
    }

    static AvifResultCode Validate(Av1IntraCandidateEvaluationRequest request)
    {
        if (request == null || request.Reconstructed == null
            || request.Reconstructed.GetStateCode() != AvifResultCode.Success
            || request.Source == null || request.SourceStride < request.Width
            || request.Width < 4 || request.Height < 4
            || request.Width > 64 || request.Height > 64
            || !IsPowerOfTwo(request.Width) || !IsPowerOfTwo(request.Height)
            || request.Width > request.Height * 4
            || request.Height > request.Width * 4
            || request.X < 0 || request.Y < 0
            || request.X > request.Reconstructed.Width - request.Width
            || request.Y > request.Reconstructed.Height - request.Height
            || request.Source.Length < (long)request.SourceStride * request.Height
            || request.BitDepth != 8 && request.BitDepth != 10
                && request.BitDepth != 12
            || request.Reconstructed.BytesPerSample
                != (request.BitDepth > 8 ? 2 : 1)
            || request.RateDistortionLambdaQ8 < 0
            || request.MaximumIntraBlockCopyCandidates < 1)
        {
            return AvifResultCode.InvalidArgument;
        }
        int maximum = (1 << request.BitDepth) - 1;
        int row = 0;
        while (row < request.Height)
        {
            int column = 0;
            while (column < request.Width)
            {
                int value = request.Source[row * request.SourceStride + column];
                if (value < 0 || value > maximum)
                {
                    return AvifResultCode.InvalidArgument;
                }
                column++;
            }
            row++;
        }
        if (request.AllowCfl)
        {
            int lumaWidth = request.Width << BooleanValue(request.SubsamplingX);
            int lumaHeight = request.Height << BooleanValue(request.SubsamplingY);
            if (request.CflLuma == null || request.CflLumaStride < lumaWidth
                || request.CflLumaWidth < lumaWidth
                || request.CflLumaHeight < lumaHeight
                || request.CflLuma.Length
                    < (long)request.CflLumaStride * request.CflLumaHeight)
            {
                return AvifResultCode.InvalidArgument;
            }
        }
        return AvifResultCode.Success;
    }

    static Av1IntraCandidateEvaluationResult Fail(AvifResultCode code)
    {
        return new Av1IntraCandidateEvaluationResult(
            code,
            System.Array.Empty<Av1IntraCandidate>(),
            null);
    }

    static int SignedMagnitudeBits(int value)
    {
        int magnitude = value < 0 ? -value : value;
        return magnitude == 0 ? 1 : 2 + FloorLog2(magnitude);
    }

    static int CeilLog2(int value)
    {
        int result = 0;
        int power = 1;
        while (power < value)
        {
            power <<= 1;
            result++;
        }
        return result;
    }

    static int FloorLog2(int value)
    {
        int result = -1;
        while (value != 0)
        {
            value >>= 1;
            result++;
        }
        return result;
    }

    static bool IsPowerOfTwo(int value)
    {
        return value > 0 && (value & (value - 1)) == 0;
    }

    static int BooleanValue(bool value)
    {
        return value ? 1 : 0;
    }

    static int Absolute(int value)
    {
        return value < 0 ? -value : value;
    }
}
