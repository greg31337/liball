namespace raw;

/// <summary>Controls deterministic descriptor-driven generic raw encoding.</summary>
public class RawGenericEncodeOptions
{
    /// <summary>Resource bounds for input validation, working rows, and output.</summary>
    public RawLimits Limits;
    /// <summary>The token checked synchronously at plane and row boundaries.</summary>
    public System.Threading.CancellationToken CancellationToken;
    /// <summary>The normalized integer quantization rule.</summary>
    public RawSampleRoundingMode RoundingMode;
    /// <summary>The policy for normalized or half-precision values outside their permitted range.</summary>
    public RawSampleRangePolicy RangePolicy;
    /// <summary>The normalized mapping used for signed integer descriptors.</summary>
    public RawSignedEncodingPolicy SignedPolicy;
    /// <summary>The policy for NaN and infinity.</summary>
    public RawNonFinitePolicy NonFinitePolicy;
    /// <summary>The policy for bytes after required sample storage.</summary>
    public RawTrailingBytePolicy TrailingBytePolicy;
    /// <summary>An exact output-sized template used when nonsample bytes must be preserved.</summary>
    public byte[] PreservedBytes;
    /// <summary>The byte written to new data-offset, row-padding, and plane-gap storage.</summary>
    public byte PaddingByte;

    /// <summary>Creates lossless stored-sample settings with strict normalized ranges and no trailing bytes.</summary>
    public RawGenericEncodeOptions()
    {
        Limits = new RawLimits();
        CancellationToken = default(System.Threading.CancellationToken);
        RoundingMode = RawSampleRoundingMode.Nearest;
        RangePolicy = RawSampleRangePolicy.Reject;
        SignedPolicy = RawSignedEncodingPolicy.FullRange;
        NonFinitePolicy = RawNonFinitePolicy.Preserve;
        TrailingBytePolicy = RawTrailingBytePolicy.Reject;
        PreservedBytes = System.Array.Empty<byte>();
        PaddingByte = 0;
    }

    /// <summary>Validates resource limits and every generic raw encoding policy.</summary>
    /// <returns>A report containing all invalid settings.</returns>
    internal RawValidationReport Validate()
    {
        System.Collections.Generic.List<RawValidationMessage> messages = RawOptionValidation.ValidateLimits(Limits);
        if (RoundingMode < RawSampleRoundingMode.Nearest || RoundingMode > RawSampleRoundingMode.TowardZero)
        {
            RawOptionValidation.AddError(messages, "GenericEncode.RoundingMode", "The generic raw rounding mode is not defined.");
        }
        if (RangePolicy < RawSampleRangePolicy.Reject || RangePolicy > RawSampleRangePolicy.Clamp)
        {
            RawOptionValidation.AddError(messages, "GenericEncode.RangePolicy", "The generic raw range policy is not defined.");
        }
        if (SignedPolicy < RawSignedEncodingPolicy.FullRange || SignedPolicy > RawSignedEncodingPolicy.NonNegative)
        {
            RawOptionValidation.AddError(messages, "GenericEncode.SignedPolicy", "The generic raw signed policy is not defined.");
        }
        if (NonFinitePolicy < RawNonFinitePolicy.Preserve || NonFinitePolicy > RawNonFinitePolicy.Clamp)
        {
            RawOptionValidation.AddError(messages, "GenericEncode.NonFinitePolicy", "The generic raw non-finite policy is not defined.");
        }
        if (TrailingBytePolicy < RawTrailingBytePolicy.Reject || TrailingBytePolicy > RawTrailingBytePolicy.Preserve)
        {
            RawOptionValidation.AddError(messages, "GenericEncode.TrailingBytePolicy", "The generic raw trailing-byte policy is not defined.");
        }
        if (PreservedBytes == null)
        {
            RawOptionValidation.AddError(messages, "GenericEncode.PreservedBytes", "The preserved-byte template cannot be null.");
        }
        return RawOptionValidation.CreateReport(true, messages);
    }

    /// <summary>Throws when synchronous cancellation has been requested.</summary>
    /// <exception cref="System.OperationCanceledException">Cancellation has been requested.</exception>
    internal void CheckCancellation()
    {
        CancellationToken.ThrowIfCancellationRequested();
    }
}
