namespace raw;

/// <summary>Supplies optional constraints for non-authoritative generic raw descriptor suggestions.</summary>
public class RawDescriptorHints
{
    /// <summary>The hinted width, or zero when unspecified.</summary>
    public int Width;
    /// <summary>The hinted height, or zero when unspecified.</summary>
    public int Height;
    /// <summary>The hinted meaningful sample depth, or zero when unspecified.</summary>
    public int BitDepth;
    /// <summary>The hinted stored word width, or zero when unspecified.</summary>
    public int StoredWordBits;
    /// <summary>Whether <see cref="SampleKind"/> is constrained.</summary>
    public bool SampleKindKnown;
    /// <summary>The hinted numeric sample representation.</summary>
    public RawSampleKind SampleKind;
    /// <summary>Whether <see cref="Packing"/> is constrained.</summary>
    public bool PackingKnown;
    /// <summary>The hinted sample packing.</summary>
    public RawPacking Packing;
    /// <summary>Whether <see cref="ByteOrder"/> is constrained.</summary>
    public bool ByteOrderKnown;
    /// <summary>The hinted multi-byte word order.</summary>
    public RawByteOrder ByteOrder;
    /// <summary>Whether <see cref="BitOrder"/> is constrained.</summary>
    public bool BitOrderKnown;
    /// <summary>The hinted generic packed-bit order.</summary>
    public RawBitOrder BitOrder;
    /// <summary>Whether <see cref="Layout"/> is constrained.</summary>
    public bool LayoutKnown;
    /// <summary>The hinted planar or interleaved sample layout.</summary>
    public RawSampleLayout Layout;
    /// <summary>The hinted channel count, or zero when unspecified.</summary>
    public int ChannelCount;
    /// <summary>Whether <see cref="Photometric"/> is constrained.</summary>
    public bool PhotometricKnown;
    /// <summary>The hinted sample interpretation.</summary>
    public RawPhotometricInterpretation Photometric;
    /// <summary>Whether <see cref="CfaLayout"/> is constrained.</summary>
    public bool CfaLayoutKnown;
    /// <summary>The hinted standard Bayer layout.</summary>
    public RawCfaLayout CfaLayout;
    /// <summary>The hinted row stride, or zero for tightly stored rows.</summary>
    public long RowStrideBytes;
    /// <summary>The hinted planar stride, or zero for tightly adjacent planes.</summary>
    public long PlaneStrideBytes;
    /// <summary>The hinted payload offset, or negative one when unspecified.</summary>
    public long DataOffset;
    /// <summary>The maximum number of returned candidates.</summary>
    public int MaxCandidates;

    /// <summary>Creates unconstrained storage hints with a bounded candidate count.</summary>
    public RawDescriptorHints()
    {
        Width = 0;
        Height = 0;
        BitDepth = 0;
        StoredWordBits = 0;
        SampleKindKnown = false;
        SampleKind = RawSampleKind.UnsignedInteger;
        PackingKnown = false;
        Packing = RawPacking.ByteAligned;
        ByteOrderKnown = false;
        ByteOrder = RawByteOrder.LittleEndian;
        BitOrderKnown = false;
        BitOrder = RawBitOrder.MostSignificantBitFirst;
        LayoutKnown = false;
        Layout = RawSampleLayout.Interleaved;
        ChannelCount = 0;
        PhotometricKnown = false;
        Photometric = RawPhotometricInterpretation.Unknown;
        CfaLayoutKnown = false;
        CfaLayout = RawCfaLayout.None;
        RowStrideBytes = 0;
        PlaneStrideBytes = 0;
        DataOffset = -1;
        MaxCandidates = 64;
    }
}

/// <summary>Describes one exact-length generic raw descriptor suggestion.</summary>
public class RawDescriptorCandidate
{
    /// <summary>The suggested descriptor.</summary>
    public RawDescriptor Descriptor;
    /// <summary>A deterministic relative score from zero through one hundred.</summary>
    public int Score;
    /// <summary>The evidence used to construct the suggestion.</summary>
    public string[] Reasons;

    /// <summary>Creates a descriptor suggestion.</summary>
    /// <param name="descriptor">The suggested descriptor.</param>
    /// <param name="score">The relative score.</param>
    /// <param name="reasons">The suggestion evidence.</param>
    public RawDescriptorCandidate(RawDescriptor descriptor, int score, string[] reasons)
    {
        Descriptor = descriptor;
        Score = score;
        Reasons = reasons == null ? System.Array.Empty<string>() : reasons;
    }
}

/// <summary>Contains advisory generic raw descriptor candidates without selecting one.</summary>
public class RawDescriptorSuggestionResult
{
    /// <summary>Every exact-length candidate in deterministic preference order.</summary>
    public RawDescriptorCandidate[] Candidates;
    /// <summary>Diagnostics describing ambiguity, missing constraints, or candidate truncation.</summary>
    public RawDiagnostic[] Diagnostics;
    /// <summary>Whether the result is authoritative, which is always false for descriptor suggestions.</summary>
    public bool Authoritative;
    /// <summary>Whether more matching candidates existed than the configured return bound.</summary>
    public bool Truncated;

    /// <summary>Creates a non-authoritative descriptor suggestion result.</summary>
    /// <param name="candidates">The exact-length candidates.</param>
    /// <param name="diagnostics">Suggestion diagnostics.</param>
    /// <param name="truncated">Whether candidate generation reached its configured bound.</param>
    public RawDescriptorSuggestionResult(RawDescriptorCandidate[] candidates, RawDiagnostic[] diagnostics, bool truncated)
    {
        Candidates = candidates == null ? System.Array.Empty<RawDescriptorCandidate>() : candidates;
        Diagnostics = diagnostics == null ? System.Array.Empty<RawDiagnostic>() : diagnostics;
        Authoritative = false;
        Truncated = truncated;
    }

    /// <summary>Returns whether multiple exact-length interpretations remain.</summary>
    /// <returns><c>true</c> when multiple candidates are present or generation was truncated.</returns>
    internal bool IsAmbiguous()
    {
        return Candidates.Length > 1 || Truncated;
    }
}
