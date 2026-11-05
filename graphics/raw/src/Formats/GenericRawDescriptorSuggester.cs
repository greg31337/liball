namespace raw;

internal class RawStorageSuggestion
{
    internal int BitDepth;
    internal int StoredWordBits;
    internal RawSampleKind SampleKind;
    internal RawPacking Packing;

    internal RawStorageSuggestion(int bitDepth, int storedWordBits, RawSampleKind sampleKind, RawPacking packing)
    {
        BitDepth = bitDepth;
        StoredWordBits = storedWordBits;
        SampleKind = sampleKind;
        Packing = packing;
    }
}

/// <summary>Produces bounded advisory generic raw descriptors from exact length and caller constraints.</summary>
internal static class GenericRawDescriptorSuggester
{
    /// <summary>Suggests exact-length descriptors without selecting or authorizing any candidate.</summary>
    /// <param name="sourceLength">The exact bounded source length.</param>
    /// <param name="hints">Caller constraints.</param>
    /// <param name="limits">Resource limits.</param>
    /// <param name="cancellationToken">The synchronous cancellation token.</param>
    /// <returns>All bounded candidates and ambiguity diagnostics.</returns>
    public static RawDescriptorSuggestionResult Suggest(long sourceLength, RawDescriptorHints? hints, RawLimits limits, System.Threading.CancellationToken cancellationToken)
    {
        ValidateInputs(sourceLength, hints, limits);
        cancellationToken.ThrowIfCancellationRequested();
        System.Collections.Generic.List<RawDescriptorCandidate> candidates = new System.Collections.Generic.List<RawDescriptorCandidate>();
        System.Collections.Generic.List<RawDiagnostic> diagnostics = new System.Collections.Generic.List<RawDiagnostic>();
        if ((hints ?? throw new System.InvalidOperationException("A required value is unavailable.")).Width == 0 && hints.Height == 0)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.InvalidDescriptor, RawSeverity.Error, "Descriptor suggestions require at least a width or height hint."));
            return new RawDescriptorSuggestionResult(candidates.ToArray(), diagnostics.ToArray(), false);
        }
        long dataOffset = hints.DataOffset < 0 ? 0 : hints.DataOffset;
        if (dataOffset >= sourceLength)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.InvalidLength, RawSeverity.Error, "The hinted data offset leaves no generic raw payload bytes."));
            return new RawDescriptorSuggestionResult(candidates.ToArray(), diagnostics.ToArray(), false);
        }
        RawStorageSuggestion[] storage = StorageSuggestions(hints);
        int[] channels = hints.ChannelCount == 0 ? new int[] { 1, 3, 4 } : new int[] { hints.ChannelCount };
        RawSampleLayout[] layouts = LayoutSuggestions(hints);
        bool truncated = false;
        int storageIndex = 0;
        while (storageIndex < storage.Length && !truncated)
        {
            int channelIndex = 0;
            while (channelIndex < channels.Length && !truncated)
            {
                int layoutIndex = 0;
                while (layoutIndex < layouts.Length && !truncated)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    truncated = SuggestConfiguration(sourceLength, dataOffset, hints, limits, storage[storageIndex], channels[channelIndex], layouts[layoutIndex], candidates);
                    layoutIndex++;
                }
                channelIndex++;
            }
            storageIndex++;
        }
        if (truncated)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.ResourceLimitExceeded, RawSeverity.Warning, "Descriptor suggestions reached the configured candidate bound; additional interpretations may exist."));
        }
        if (candidates.Count == 0)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.InvalidDescriptor, RawSeverity.Error, "No descriptor satisfying every hint matches the exact source length."));
        }
        else if (candidates.Count > 1 || truncated)
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.AmbiguousFormat, RawSeverity.Warning, "Multiple generic raw descriptors match exactly; the caller must choose an authoritative descriptor."));
        }
        else
        {
            diagnostics.Add(Diagnostic(RawDiagnosticCode.None, RawSeverity.Information, "One descriptor suggestion matches, but length and hints do not make it authoritative."));
        }
        return new RawDescriptorSuggestionResult(candidates.ToArray(), diagnostics.ToArray(), truncated);
    }

    internal static void ValidateInputs(long sourceLength, RawDescriptorHints? hints, RawLimits limits)
    {
        CheckedMath.RequireLimits(limits);
        CheckedMath.FileSize(sourceLength, limits);
        if (hints == null)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Generic raw descriptor hints are required.", -1);
        }
        if (hints.Width < 0 || hints.Height < 0 || hints.BitDepth < 0 || hints.StoredWordBits < 0 || hints.ChannelCount < 0 || hints.RowStrideBytes < 0 || hints.PlaneStrideBytes < 0 || hints.DataOffset < -1 || hints.MaxCandidates <= 0)
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Generic raw descriptor hint counts, dimensions, strides, and bounds cannot be invalid.", -1);
        }
        if (hints.Width > limits.MaxWidth || hints.Height > limits.MaxHeight || hints.Width > 0 && hints.Height > 0 && (long)hints.Width * hints.Height > limits.MaxPixelCount)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "Hinted generic raw dimensions exceed configured limits.", -1);
        }
        if (hints.BitDepth > 32 || hints.StoredWordBits > 32 || hints.ChannelCount > 64 || hints.MaxCandidates > limits.MaxTagCount)
        {
            throw new RawLimitException(RawDiagnosticCode.ResourceLimitExceeded, "Generic raw descriptor hints exceed configured candidate or descriptor bounds.", -1);
        }
        if (hints.SampleKindKnown && (hints.SampleKind < RawSampleKind.UnsignedInteger || hints.SampleKind > RawSampleKind.Single) || hints.PackingKnown && (hints.Packing < RawPacking.ByteAligned || hints.Packing > RawPacking.Raw14) || hints.ByteOrderKnown && (hints.ByteOrder < RawByteOrder.LittleEndian || hints.ByteOrder > RawByteOrder.BigEndian) || hints.BitOrderKnown && (hints.BitOrder < RawBitOrder.MostSignificantBitFirst || hints.BitOrder > RawBitOrder.LeastSignificantBitFirst) || hints.LayoutKnown && (hints.Layout < RawSampleLayout.Interleaved || hints.Layout > RawSampleLayout.Planar) || hints.PhotometricKnown && (hints.Photometric < RawPhotometricInterpretation.Unknown || hints.Photometric > RawPhotometricInterpretation.LinearRaw) || hints.CfaLayoutKnown && (hints.CfaLayout < RawCfaLayout.None || hints.CfaLayout > RawCfaLayout.Arbitrary))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "One or more generic raw descriptor hint enum values are undefined.", -1);
        }
        if (hints.CfaLayoutKnown && (hints.CfaLayout == RawCfaLayout.None || hints.CfaLayout == RawCfaLayout.Arbitrary))
        {
            throw new RawFormatException(RawDiagnosticCode.InvalidArgument, "Descriptor suggestions require a standard Bayer layout when a CFA layout hint is supplied.", -1);
        }
    }

    internal static RawStorageSuggestion[] StorageSuggestions(RawDescriptorHints hints)
    {
        System.Collections.Generic.List<RawStorageSuggestion> result = new System.Collections.Generic.List<RawStorageSuggestion>();
        RawSampleKind kind = hints.SampleKindKnown ? hints.SampleKind : RawSampleKind.UnsignedInteger;
        if (kind == RawSampleKind.Half)
        {
            AddStorage(result, hints, new RawStorageSuggestion(16, 16, kind, RawPacking.ByteAligned));
            return result.ToArray();
        }
        if (kind == RawSampleKind.Single)
        {
            AddStorage(result, hints, new RawStorageSuggestion(32, 32, kind, RawPacking.ByteAligned));
            return result.ToArray();
        }
        int[] depths = hints.BitDepth == 0 ? new int[] { 8, 10, 12, 14, 16 } : new int[] { hints.BitDepth };
        int index = 0;
        while (index < depths.Length)
        {
            int depth = depths[index];
            int word = hints.StoredWordBits == 0 ? depth <= 8 ? 8 : 16 : hints.StoredWordBits;
            AddStorage(result, hints, new RawStorageSuggestion(depth, word, kind, RawPacking.ByteAligned));
            AddStorage(result, hints, new RawStorageSuggestion(depth, depth, kind, RawPacking.PackedBits));
            if (kind == RawSampleKind.UnsignedInteger && (depth == 10 || depth == 12 || depth == 14))
            {
                RawPacking packing = depth == 10 ? RawPacking.Raw10 : depth == 12 ? RawPacking.Raw12 : RawPacking.Raw14;
                AddStorage(result, hints, new RawStorageSuggestion(depth, depth, kind, packing));
            }
            index++;
        }
        return result.ToArray();
    }

    internal static void AddStorage(System.Collections.Generic.List<RawStorageSuggestion> result, RawDescriptorHints hints, RawStorageSuggestion suggestion)
    {
        if (hints.BitDepth != 0 && hints.BitDepth != suggestion.BitDepth)
        {
            return;
        }
        if (hints.PackingKnown && hints.Packing != suggestion.Packing)
        {
            return;
        }
        if (hints.StoredWordBits != 0 && hints.StoredWordBits != suggestion.StoredWordBits)
        {
            return;
        }
        if (suggestion.Packing == RawPacking.ByteAligned && suggestion.StoredWordBits != 8 && suggestion.StoredWordBits != 16 && suggestion.StoredWordBits != 32)
        {
            return;
        }
        if (suggestion.BitDepth <= 0 || suggestion.BitDepth > suggestion.StoredWordBits)
        {
            return;
        }
        result.Add(suggestion);
    }

    internal static RawSampleLayout[] LayoutSuggestions(RawDescriptorHints hints)
    {
        if (hints.LayoutKnown)
        {
            return new RawSampleLayout[] { hints.Layout };
        }
        return new RawSampleLayout[] { RawSampleLayout.Interleaved, RawSampleLayout.Planar };
    }

    internal static bool SuggestConfiguration(long sourceLength, long dataOffset, RawDescriptorHints hints, RawLimits limits, RawStorageSuggestion storage, int channelCount, RawSampleLayout layout, System.Collections.Generic.List<RawDescriptorCandidate> candidates)
    {
        RawByteOrder[] byteOrders = ByteOrderSuggestions(hints, storage);
        RawBitOrder[] bitOrders = BitOrderSuggestions(hints, storage);
        RawCfaLayout[] cfaLayouts = CfaSuggestions(hints, channelCount);
        int byteIndex = 0;
        while (byteIndex < byteOrders.Length)
        {
            int bitIndex = 0;
            while (bitIndex < bitOrders.Length)
            {
                int cfaIndex = 0;
                while (cfaIndex < cfaLayouts.Length)
                {
                    RawDescriptor seed = CreateSeed(storage, channelCount, layout, byteOrders[byteIndex], bitOrders[bitIndex], cfaLayouts[cfaIndex], hints);
                    bool full = AddMatchingDimensions(seed, sourceLength, dataOffset, hints, limits, candidates);
                    if (full)
                    {
                        return true;
                    }
                    cfaIndex++;
                }
                bitIndex++;
            }
            byteIndex++;
        }
        return false;
    }

    internal static RawByteOrder[] ByteOrderSuggestions(RawDescriptorHints hints, RawStorageSuggestion storage)
    {
        if (hints.ByteOrderKnown)
        {
            return new RawByteOrder[] { hints.ByteOrder };
        }
        if (storage.Packing == RawPacking.ByteAligned && storage.StoredWordBits > 8)
        {
            return new RawByteOrder[] { RawByteOrder.LittleEndian, RawByteOrder.BigEndian };
        }
        return new RawByteOrder[] { RawByteOrder.LittleEndian };
    }

    internal static RawBitOrder[] BitOrderSuggestions(RawDescriptorHints hints, RawStorageSuggestion storage)
    {
        if (hints.BitOrderKnown)
        {
            return new RawBitOrder[] { hints.BitOrder };
        }
        if (storage.Packing == RawPacking.PackedBits)
        {
            return new RawBitOrder[] { RawBitOrder.MostSignificantBitFirst, RawBitOrder.LeastSignificantBitFirst };
        }
        return new RawBitOrder[] { storage.Packing == RawPacking.ByteAligned ? RawBitOrder.MostSignificantBitFirst : RawBitOrder.LeastSignificantBitFirst };
    }

    internal static RawCfaLayout[] CfaSuggestions(RawDescriptorHints hints, int channelCount)
    {
        if (hints.CfaLayoutKnown)
        {
            return channelCount == 1 ? new RawCfaLayout[] { hints.CfaLayout } : System.Array.Empty<RawCfaLayout>();
        }
        if (hints.PhotometricKnown)
        {
            if (hints.Photometric == RawPhotometricInterpretation.ColorFilterArray && channelCount == 1)
            {
                return new RawCfaLayout[] { RawCfaLayout.Rggb, RawCfaLayout.Bggr, RawCfaLayout.Grbg, RawCfaLayout.Gbrg };
            }
            return new RawCfaLayout[] { RawCfaLayout.None };
        }
        if (channelCount == 1)
        {
            return new RawCfaLayout[] { RawCfaLayout.None, RawCfaLayout.Rggb, RawCfaLayout.Bggr, RawCfaLayout.Grbg, RawCfaLayout.Gbrg };
        }
        return new RawCfaLayout[] { RawCfaLayout.None };
    }

    internal static RawDescriptor CreateSeed(RawStorageSuggestion storage, int channelCount, RawSampleLayout layout, RawByteOrder byteOrder, RawBitOrder bitOrder, RawCfaLayout cfaLayout, RawDescriptorHints hints)
    {
        RawDescriptor descriptor = new RawDescriptor();
        descriptor.BitDepth = storage.BitDepth;
        descriptor.StoredWordBits = storage.StoredWordBits;
        descriptor.SampleKind = storage.SampleKind;
        descriptor.Packing = storage.Packing;
        descriptor.ByteOrder = byteOrder;
        descriptor.BitOrder = bitOrder;
        descriptor.Layout = layout;
        descriptor.ChannelCount = channelCount;
        descriptor.Channels = Channels(channelCount, cfaLayout);
        descriptor.Photometric = hints.PhotometricKnown ? hints.Photometric : DefaultPhotometric(channelCount, cfaLayout);
        descriptor.Cfa = cfaLayout == RawCfaLayout.None ? null : RawCfaPattern.CreateBayer(cfaLayout);
        descriptor.Orientation = RawOrientation.Normal;
        return descriptor;
    }

    internal static RawChannelDescription[] Channels(int count, RawCfaLayout cfaLayout)
    {
        if (cfaLayout != RawCfaLayout.None)
        {
            return new RawChannelDescription[] { new RawChannelDescription("CFA", RawChannelKind.Custom) };
        }
        if (count == 1)
        {
            return new RawChannelDescription[] { new RawChannelDescription("Gray", RawChannelKind.Gray) };
        }
        if (count == 3)
        {
            return new RawChannelDescription[] { new RawChannelDescription("R", RawChannelKind.Red), new RawChannelDescription("G", RawChannelKind.Green), new RawChannelDescription("B", RawChannelKind.Blue) };
        }
        if (count == 4)
        {
            return new RawChannelDescription[] { new RawChannelDescription("R", RawChannelKind.Red), new RawChannelDescription("G", RawChannelKind.Green), new RawChannelDescription("B", RawChannelKind.Blue), new RawChannelDescription("A", RawChannelKind.Alpha) };
        }
        RawChannelDescription[] channels = new RawChannelDescription[count];
        int index = 0;
        while (index < count)
        {
            channels[index] = new RawChannelDescription("C" + index, RawChannelKind.Custom);
            index++;
        }
        return channels;
    }

    internal static RawPhotometricInterpretation DefaultPhotometric(int channelCount, RawCfaLayout cfaLayout)
    {
        if (cfaLayout != RawCfaLayout.None)
        {
            return RawPhotometricInterpretation.ColorFilterArray;
        }
        if (channelCount == 1)
        {
            return RawPhotometricInterpretation.BlackIsZero;
        }
        if (channelCount == 3 || channelCount == 4)
        {
            return RawPhotometricInterpretation.Rgb;
        }
        return RawPhotometricInterpretation.LinearRaw;
    }

    internal static bool AddMatchingDimensions(RawDescriptor seed, long sourceLength, long dataOffset, RawDescriptorHints hints, RawLimits limits, System.Collections.Generic.List<RawDescriptorCandidate> candidates)
    {
        if (hints.Width > 0 && hints.Height > 0)
        {
            return TryCandidate(seed, hints.Width, hints.Height, sourceLength, dataOffset, hints, limits, candidates);
        }
        if (hints.Width > 0)
        {
            int height = DeriveHeight(seed, hints.Width, sourceLength - dataOffset, hints);
            if (height > 0)
            {
                return TryCandidate(seed, hints.Width, height, sourceLength, dataOffset, hints, limits, candidates);
            }
            return false;
        }
        int width = 1;
        while (width <= limits.MaxWidth)
        {
            if (TryCandidate(seed, width, hints.Height, sourceLength, dataOffset, hints, limits, candidates))
            {
                return true;
            }
            width++;
        }
        return false;
    }

    internal static int DeriveHeight(RawDescriptor seed, int width, long available, RawDescriptorHints hints)
    {
        seed.Width = width;
        seed.Height = 1;
        long minimumRow = seed.CalculateMinimumRowBytes();
        if (minimumRow <= 0)
        {
            return 0;
        }
        long rowStride = hints.RowStrideBytes == 0 ? minimumRow : hints.RowStrideBytes;
        if (rowStride < minimumRow)
        {
            return 0;
        }
        long preceding = 0;
        long perHeight = rowStride;
        if (seed.Layout == RawSampleLayout.Planar)
        {
            if (hints.PlaneStrideBytes > 0)
            {
                preceding = RawDescriptor.MultiplyOrNegative(hints.PlaneStrideBytes, seed.ChannelCount - 1);
            }
            else
            {
                perHeight = RawDescriptor.MultiplyOrNegative(rowStride, seed.ChannelCount);
            }
        }
        if (preceding < 0 || perHeight <= 0 || available < preceding || (available - preceding) % perHeight != 0)
        {
            return 0;
        }
        long height = (available - preceding) / perHeight;
        return height > 0 && height <= int.MaxValue ? (int)height : 0;
    }

    internal static bool TryCandidate(RawDescriptor seed, int width, int height, long sourceLength, long dataOffset, RawDescriptorHints hints, RawLimits limits, System.Collections.Generic.List<RawDescriptorCandidate> candidates)
    {
        if (width <= 0 || height <= 0 || width > limits.MaxWidth || height > limits.MaxHeight || (long)width * height > limits.MaxPixelCount)
        {
            return false;
        }
        RawDescriptor descriptor = CloneSeed(seed);
        descriptor.Width = width;
        descriptor.Height = height;
        long minimumRow = descriptor.CalculateMinimumRowBytes();
        if (minimumRow <= 0)
        {
            return false;
        }
        descriptor.RowStrideBytes = hints.RowStrideBytes == 0 ? minimumRow : hints.RowStrideBytes;
        if (descriptor.Layout == RawSampleLayout.Planar)
        {
            long planeContent = RawDescriptor.MultiplyOrNegative(descriptor.RowStrideBytes, height);
            descriptor.PlaneStrideBytes = hints.PlaneStrideBytes == 0 ? planeContent : hints.PlaneStrideBytes;
        }
        else
        {
            if (hints.PlaneStrideBytes != 0)
            {
                return false;
            }
            descriptor.PlaneStrideBytes = 0;
        }
        descriptor.DataOffset = dataOffset;
        descriptor.DataLength = sourceLength - dataOffset;
        descriptor.AllowTrailingBytes = false;
        if (!descriptor.ValidateSourceLength(sourceLength, limits).IsValid())
        {
            return false;
        }
        if (Duplicate(candidates, descriptor))
        {
            return false;
        }
        int score = Score(hints);
        candidates.Add(new RawDescriptorCandidate(descriptor, score, Reasons(hints)));
        return candidates.Count >= hints.MaxCandidates;
    }

    internal static RawDescriptor CloneSeed(RawDescriptor seed)
    {
        RawDescriptor descriptor = new RawDescriptor();
        descriptor.BitDepth = seed.BitDepth;
        descriptor.StoredWordBits = seed.StoredWordBits;
        descriptor.SampleKind = seed.SampleKind;
        descriptor.Packing = seed.Packing;
        descriptor.ByteOrder = seed.ByteOrder;
        descriptor.BitOrder = seed.BitOrder;
        descriptor.Layout = seed.Layout;
        descriptor.ChannelCount = seed.ChannelCount;
        descriptor.Channels = seed.Channels;
        descriptor.Photometric = seed.Photometric;
        descriptor.Cfa = seed.Cfa;
        descriptor.Orientation = seed.Orientation;
        return descriptor;
    }

    internal static bool Duplicate(System.Collections.Generic.List<RawDescriptorCandidate> candidates, RawDescriptor descriptor)
    {
        int index = 0;
        while (index < candidates.Count)
        {
            RawDescriptor existing = candidates[index].Descriptor;
            if (existing.Width == descriptor.Width && existing.Height == descriptor.Height && existing.BitDepth == descriptor.BitDepth && existing.StoredWordBits == descriptor.StoredWordBits && existing.SampleKind == descriptor.SampleKind && existing.Packing == descriptor.Packing && existing.ByteOrder == descriptor.ByteOrder && existing.BitOrder == descriptor.BitOrder && existing.Layout == descriptor.Layout && existing.ChannelCount == descriptor.ChannelCount && existing.Photometric == descriptor.Photometric && existing.RowStrideBytes == descriptor.RowStrideBytes && existing.PlaneStrideBytes == descriptor.PlaneStrideBytes && CfaIdentity(existing.Cfa) == CfaIdentity(descriptor.Cfa))
            {
                return true;
            }
            index++;
        }
        return false;
    }

    internal static RawCfaLayout CfaIdentity(RawCfaPattern? cfa)
    {
        return cfa == null ? RawCfaLayout.None : cfa.Layout;
    }

    internal static int Score(RawDescriptorHints hints)
    {
        int score = 25;
        if (hints.Width > 0) score += 8;
        if (hints.Height > 0) score += 8;
        if (hints.BitDepth > 0) score += 7;
        if (hints.StoredWordBits > 0) score += 5;
        if (hints.SampleKindKnown) score += 5;
        if (hints.PackingKnown) score += 7;
        if (hints.ByteOrderKnown) score += 4;
        if (hints.BitOrderKnown) score += 4;
        if (hints.LayoutKnown) score += 5;
        if (hints.ChannelCount > 0) score += 6;
        if (hints.PhotometricKnown) score += 5;
        if (hints.CfaLayoutKnown) score += 5;
        if (hints.RowStrideBytes > 0) score += 3;
        if (hints.PlaneStrideBytes > 0) score += 3;
        if (hints.DataOffset >= 0) score += 2;
        return score > 100 ? 100 : score;
    }

    internal static string[] Reasons(RawDescriptorHints hints)
    {
        System.Collections.Generic.List<string> reasons = new System.Collections.Generic.List<string>();
        reasons.Add("Exact source length");
        if (hints.Width > 0) reasons.Add("Caller width");
        if (hints.Height > 0) reasons.Add("Caller height");
        if (hints.BitDepth > 0) reasons.Add("Caller bit depth");
        if (hints.PackingKnown) reasons.Add("Caller packing");
        if (hints.ChannelCount > 0) reasons.Add("Caller channel count");
        if (hints.CfaLayoutKnown) reasons.Add("Caller CFA layout");
        if (hints.RowStrideBytes > 0) reasons.Add("Caller row stride");
        return reasons.ToArray();
    }

    internal static RawDiagnostic Diagnostic(RawDiagnosticCode code, RawSeverity severity, string message)
    {
        return new RawDiagnostic(code, severity, RawFormat.GenericRaw, -1, string.Empty, -1, "DescriptorSuggestion", message, false);
    }
}
