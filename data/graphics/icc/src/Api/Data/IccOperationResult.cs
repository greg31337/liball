namespace icc;

/// <summary>
/// Reports an expected operation or probe outcome without requiring an exception.
/// </summary>
/// <remarks>
/// <para>Probe, capability, validation-report, and exception-less embedding operations return this type for expected unsuccessful outcomes.</para>
/// <para>Throwing operations instead use <see cref="IccProfileFormatException"/>, <see cref="IccProfileValidationException"/>, <see cref="IccProfileWriteException"/>, <see cref="IccTransformException"/>, <see cref="IccUnsupportedFeatureException"/>, or <see cref="IccEmbeddingException"/> according to the failed operation.</para>
/// </remarks>
public class IccOperationResult
{
    /// <summary>
    /// Gets or sets the operation outcome.
    /// </summary>
    public IccOperationStatus Status;

    /// <summary>
    /// Gets or sets a stable human-readable explanation of the outcome.
    /// </summary>
    public string Message;

    /// <summary>
    /// Gets or sets the input byte range associated with the outcome.
    /// </summary>
    public IccByteLocation Location;

    /// <summary>
    /// Initializes a result whose outcome has not yet been assigned.
    /// </summary>
    public IccOperationResult()
    {
        Status = IccOperationStatus.Unspecified;
        Message = string.Empty;
        Location = new IccByteLocation();
    }
}
