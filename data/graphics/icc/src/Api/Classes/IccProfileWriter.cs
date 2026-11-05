namespace icc;

/// <summary>Measures and writes ICC profiles.</summary>
public class IccProfileWriter : IIccProfileWriter
{
    /// <summary>Initializes a profile writer.</summary>
    public IccProfileWriter()
    {
    }

    /// <summary>Measures an encoded profile.</summary>
    /// <param name="profile">The profile to measure.</param>
    /// <param name="options">The writing options.</param>
    /// <returns>The encoded size.</returns>
    public int MeasureProfileBytes(IccProfile profile, IccWriteOptions options)
    {
        ProfileWritePlan plan = ProfileWriterCore.BuildPlan(profile, options);
        return plan.TotalSize;
    }

    /// <summary>Writes a profile to bytes.</summary>
    /// <param name="profile">The profile to write.</param>
    /// <param name="options">The writing options.</param>
    /// <returns>The encoded bytes.</returns>
    public byte[] WriteProfileBytes(IccProfile profile, IccWriteOptions options)
    {
        ProfileWritePlan plan = ProfileWriterCore.BuildPlan(profile, options);
        return ProfileWriterCore.EncodePlan(plan);
    }

    /// <summary>Attempts to write into caller-provided storage.</summary>
    /// <param name="profile">The profile to write.</param>
    /// <param name="destination">The destination storage.</param>
    /// <param name="options">The writing options.</param>
    /// <returns>The write result.</returns>
    public IccOperationResult TryWriteProfileBuffer(IccProfile profile, System.Span<byte> destination, IccWriteOptions options)
    {
        ProfileWritePlan plan = ProfileWriterCore.BuildPlan(profile, options);
        if (destination.Length < plan.TotalSize)
        {
            return ProfileWriterCore.CreateBufferResult(IccOperationStatus.ResourceLimitExceeded, $"The destination contains {destination.Length} bytes, but the preflighted profile requires {plan.TotalSize} bytes.", plan.TotalSize);
        }

        byte[] encoded = ProfileWriterCore.EncodePlan(plan);
        System.ReadOnlySpan<byte> source = new System.ReadOnlySpan<byte>(encoded);
        source.CopyTo(destination);
        return ProfileWriterCore.CreateBufferResult(IccOperationStatus.Success, $"Wrote the complete {plan.TotalSize}-byte ICC profile.", plan.TotalSize);
    }

    /// <summary>Writes a profile to a stream.</summary>
    /// <param name="profile">The profile to write.</param>
    /// <param name="destinationStream">The destination stream.</param>
    /// <param name="options">The writing options.</param>
    public void WriteProfileStream(IccProfile profile, System.IO.Stream destinationStream, IccWriteOptions options)
    {
        if (destinationStream == null || !destinationStream.CanWrite)
        {
            throw new IccProfileWriteException("The destination stream must be non-null and writable.");
        }

        ProfileWritePlan plan = ProfileWriterCore.BuildPlan(profile, options);
        byte[] encoded = ProfileWriterCore.EncodePlan(plan);
        destinationStream.Write(encoded, 0, encoded.Length);
    }
}
