using System;
using System.IO;
using System.Text;
using System.Threading;

namespace html;
/// <summary>Defines the incremental operations exposed by an HTML parser.</summary>
public interface IHtmlParser
{
    /// <summary>Adds the next input segment and records whether it is the final segment.</summary>
    /// <param name = "input">The input data to process.</param>
    /// <param name = "isFinal">Whether no more input will be supplied after this chunk.</param>
    public void Feed(ReadOnlySpan<char> input, bool isFinal);
    /// <summary>Attaches a text reader that subsequent reads pull from as input is needed.</summary>
    /// <param name = "reader">The decoded character source to pull from.</param>
    /// <param name = "leaveOpen">Whether reset or terminal completion leaves the reader open.</param>
    public void SetReader(TextReader reader, bool leaveOpen);
    /// <summary>Sets the token observed by subsequent incremental parser work.</summary>
    /// <param name = "cancellationToken">The cancellation token to observe.</param>
    public void SetCancellationToken(CancellationToken cancellationToken);
    /// <summary>Initializes this parser for a context-sensitive fragment.</summary>
    /// <param name = "context">The element or template-content context that controls parsing.</param>
    public void InitializeFragment(HtmlFragmentContext context);
    /// <summary>Reads the next available parser step.</summary>
    /// <returns>The parser progress status and processed token count.</returns>
    public HtmlParseProgress Read();
    /// <summary>Finishes processing and returns the final bounded result.</summary>
    /// <returns>The completed document or fragment and its status information.</returns>
    public HtmlParseResult Complete();
    /// <summary>Clears accumulated state so the parser can process new input.</summary>
    public void Reset();
}
