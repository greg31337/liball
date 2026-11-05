namespace whois;

/// <summary>
/// Reports response decoding or generic parsing failure with lossless response context.
/// </summary>
public class WhoisResponseException : System.Exception
{
    /// <summary>
    /// Identifies whether decoding or parsing failed.
    /// </summary>
    public WhoisOperationStage Stage;

    /// <summary>
    /// Contains the raw response whose bytes remain the source of truth.
    /// </summary>
    public WhoisRawResponse RawResponse;

    /// <summary>
    /// Contains the partial decoded response, or <c>null</c> when decoding produced no view.
    /// </summary>
    public WhoisTextResponse? TextResponse;

    /// <summary>
    /// Initializes a response exception with raw and optional decoded response context.
    /// </summary>
    /// <param name="message">The decoding or parsing failure message.</param>
    /// <param name="stage">The response-processing stage that failed.</param>
    /// <param name="rawResponse">The complete raw response context.</param>
    /// <param name="textResponse">The partial decoded response, or <c>null</c>.</param>
    /// <param name="innerException">The underlying response exception, or <c>null</c>.</param>
    public WhoisResponseException(string message, WhoisOperationStage stage, WhoisRawResponse rawResponse, WhoisTextResponse? textResponse, System.Exception? innerException) : base(message, innerException)
    {
        Stage = stage;
        RawResponse = rawResponse;
        TextResponse = textResponse;
    }
}
