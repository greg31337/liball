namespace json;

/// <summary>Publishes incrementally parsed JSON tokens to a configured handler.</summary>
public interface IJsonPushParser
{
    /// <summary>Copies and processes the supplied UTF-8 input.</summary>
    /// <param name="utf8">The next input bytes.</param>
    /// <returns>The current incremental operation state.</returns>
    JsonFeedResult FeedPushUtf8(global::System.ReadOnlySpan<byte> utf8);

    /// <summary>Attaches and processes a caller-owned UTF-8 input stream.</summary>
    /// <param name="stream">The caller-owned input stream.</param>
    /// <returns>The current incremental operation state.</returns>
    JsonFeedResult FeedPushStream(global::System.IO.Stream stream);

    /// <summary>Signals that no additional input bytes will be supplied.</summary>
    /// <returns>The current incremental operation state.</returns>
    JsonFeedResult CompletePushInput();

    /// <summary>Resumes token publication after a handler-requested pause.</summary>
    /// <returns>The current incremental operation state.</returns>
    JsonFeedResult ResumePushParser();

    /// <summary>Gets the total number of input bytes accepted by the parser.</summary>
    /// <returns>The accepted byte count.</returns>
    long GetPushBytesAccepted();

    /// <summary>Gets the total number of accepted input bytes consumed by the parser.</summary>
    /// <returns>The consumed byte count.</returns>
    long GetPushBytesConsumed();

    /// <summary>Resets the parser for independent reuse.</summary>
    void ResetPushParser();
}
