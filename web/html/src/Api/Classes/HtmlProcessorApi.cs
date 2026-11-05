using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

namespace html;
/// <summary>Provides instance-based access to the HtmlProcessor operations.</summary>
public class HtmlProcessorApi : IHtmlProcessorApi
{
    /// <summary>Executes the ProcessString operation.</summary>
    public HtmlProcessingResult ProcessString(string html, IHtmlProcessingConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlProcessor.Process(html, options, cancellationToken);
    }

    /// <summary>Executes the ProcessCharacters operation.</summary>
    public HtmlProcessingResult ProcessCharacters(ReadOnlySpan<char> html, IHtmlProcessingConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlProcessor.ProcessCharacters(html, options,
            cancellationToken);
    }

    /// <summary>Executes the ProcessBytes operation.</summary>
    public HtmlProcessingResult ProcessBytes(byte[] html, IHtmlProcessingConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlProcessor.Process(html, options, cancellationToken);
    }

    /// <summary>Executes the ProcessByteSpan operation.</summary>
    public HtmlProcessingResult ProcessByteSpan(ReadOnlySpan<byte> html, IHtmlProcessingConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlProcessor.ProcessByteSpan(html, options,
            cancellationToken);
    }

    /// <summary>Executes the ProcessStream operation.</summary>
    public HtmlProcessingResult ProcessStream(Stream stream, IHtmlProcessingConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlProcessor.ProcessStream(stream, options,
            cancellationToken);
    }

    /// <summary>Executes the ProcessReader operation.</summary>
    public HtmlProcessingResult ProcessReader(TextReader reader, IHtmlProcessingConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlProcessor.ProcessReader(reader, options,
            cancellationToken);
    }

    /// <summary>Executes the ProcessDocument operation.</summary>
    public HtmlProcessingResult ProcessDocument(HtmlDocument document, IHtmlProcessingConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlProcessor.ProcessDocument(document, options,
            cancellationToken);
    }

    /// <summary>Executes the ProcessFragment operation.</summary>
    public HtmlProcessingResult ProcessFragment(HtmlNode fragment, IHtmlProcessingConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlProcessor.ProcessFragment(fragment, options,
            cancellationToken);
    }

    /// <summary>Executes the ProcessNode operation.</summary>
    public HtmlProcessingResult ProcessNode(HtmlNode node, IHtmlProcessingConfiguration options, CancellationToken cancellationToken)
    {
        return HtmlProcessor.ProcessNode(node, options,
            cancellationToken);
    }
}
