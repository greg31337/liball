using System;

namespace xml;

/// <summary>
/// Incrementally parses byte or character blocks with explicit state transitions.
/// </summary>
public interface IXmlPushParser
{
    /// <summary>
    /// Starts an incremental parse.
    /// </summary>
    /// <param name="input">The input metadata and optional initial content.</param>
    /// <param name="options">The parser options.</param>
    /// <param name="handler">The optional event handler.</param>
    /// <param name="resolver">The optional resource resolver.</param>
    /// <param name="reporter">The optional diagnostic reporter.</param>
    /// <param name="cancellation">The optional cancellation source.</param>
    /// <returns>The initial parser status.</returns>
    XmlParserStatus BeginXmlPush(XmlInput input, XmlParserOptions options, IXmlEventHandler? handler, IXmlResourceResolver? resolver, IXmlErrorReporter? reporter, IXmlCancellation? cancellation);

    /// <summary>
    /// Feeds a byte block.
    /// </summary>
    /// <param name="bytes">The next bytes.</param>
    /// <param name="isFinal">Whether this is the final block.</param>
    /// <returns>The events and resulting state.</returns>
    XmlEventBatch FeedXmlPushBytes(ReadOnlyMemory<byte> bytes, bool isFinal);

    /// <summary>
    /// Feeds a character block.
    /// </summary>
    /// <param name="characters">The next characters.</param>
    /// <param name="isFinal">Whether this is the final block.</param>
    /// <returns>The events and resulting state.</returns>
    XmlEventBatch FeedXmlPushCharacters(ReadOnlyMemory<char> characters, bool isFinal);

    /// <summary>
    /// Finishes the current incremental parse.
    /// </summary>
    /// <returns>The final events and status.</returns>
    XmlEventBatch FinishXmlPush();

    /// <summary>
    /// Pauses event production.
    /// </summary>
    /// <returns>The paused status.</returns>
    XmlParserStatus PauseXmlPush();

    /// <summary>
    /// Resumes event production.
    /// </summary>
    /// <returns>The resumed status.</returns>
    XmlParserStatus ResumeXmlPush();

    /// <summary>
    /// Aborts parsing.
    /// </summary>
    /// <returns>The aborted status.</returns>
    XmlParserStatus AbortXmlPush();

    /// <summary>
    /// Resets the parser for a new input.
    /// </summary>
    void ResetXmlPush();

    /// <summary>
    /// Closes the current input and parser.
    /// </summary>
    void CloseXmlPush();

    /// <summary>
    /// Gets the current incremental parser status.
    /// </summary>
    /// <returns>The parser status.</returns>
    XmlParserStatus InspectXmlPushStatus();
}

