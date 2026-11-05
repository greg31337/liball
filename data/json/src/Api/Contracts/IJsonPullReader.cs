namespace json;

/// <summary>Accepts incremental UTF-8 input for pull-based token reading.</summary>
public interface IJsonPullReader
{
    /// <summary>Copies and processes the supplied UTF-8 input.</summary>
    /// <param name="utf8">The next input bytes.</param>
    /// <returns>The current incremental operation state.</returns>
    JsonFeedResult FeedUtf8(global::System.ReadOnlySpan<byte> utf8);

    /// <summary>Attaches and begins reading a caller-owned UTF-8 input stream.</summary>
    /// <param name="stream">The caller-owned input stream.</param>
    /// <returns>The current incremental operation state.</returns>
    JsonFeedResult FeedStream(global::System.IO.Stream stream);

    /// <summary>Signals that no additional input bytes will be supplied.</summary>
    /// <returns>The current incremental operation state.</returns>
    JsonFeedResult CompleteInput();

    /// <summary>Peeks at the next available token without advancing the reader.</summary>
    /// <returns>The token or current read state.</returns>
    JsonTokenResult PeekToken();

    /// <summary>Reads the next available token.</summary>
    /// <returns>The token or current read state.</returns>
    JsonTokenResult ReadToken();

    /// <summary>Skips the current scalar or complete subtree after fully validating it.</summary>
    /// <returns>The skip outcome and current read state.</returns>
    JsonOperationResult SkipValue();

    /// <summary>Captures the current scalar or complete subtree.</summary>
    /// <returns>The captured value or current read state.</returns>
    JsonValueResult CaptureValue();

    /// <summary>Gets the total number of input bytes accepted by the reader.</summary>
    /// <returns>The accepted byte count.</returns>
    long GetBytesAccepted();

    /// <summary>Gets the total number of accepted input bytes consumed by the reader.</summary>
    /// <returns>The consumed byte count.</returns>
    long GetBytesConsumed();

    /// <summary>Resets the reader for independent reuse.</summary>
    void ResetReader();
}
