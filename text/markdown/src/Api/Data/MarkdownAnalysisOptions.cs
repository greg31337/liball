using System;

namespace markdown;

/// <summary>
/// Configures table-of-contents and reading-time analysis.
/// </summary>
public class MarkdownAnalysisOptions
{
    /// <summary>The reading speed used for estimates.</summary>
    public int WordsPerMinute;
    /// <summary>The minimum reported reading time in seconds.</summary>
    public int MinimumReadingTimeSeconds;
    /// <summary>The shallowest heading level included in the generated table of contents.</summary>
    public int TableOfContentsMinimumLevel;
    /// <summary>The deepest heading level included in the generated table of contents.</summary>
    public int TableOfContentsMaximumLevel;
    /// <summary>Whether Markdown table-of-contents text is generated.</summary>
    public bool GenerateTableOfContents;

    /// <summary>Initializes conventional analysis defaults.</summary>
    public MarkdownAnalysisOptions()
    {
        WordsPerMinute = 200;
        MinimumReadingTimeSeconds = 0;
        TableOfContentsMinimumLevel = 1;
        TableOfContentsMaximumLevel = 6;
        GenerateTableOfContents = true;
    }

    internal MarkdownAnalysisOptions CopyValidated()
    {
        Validate();
        MarkdownAnalysisOptions copy = new MarkdownAnalysisOptions();
        copy.WordsPerMinute = WordsPerMinute;
        copy.MinimumReadingTimeSeconds = MinimumReadingTimeSeconds;
        copy.TableOfContentsMinimumLevel = TableOfContentsMinimumLevel;
        copy.TableOfContentsMaximumLevel = TableOfContentsMaximumLevel;
        copy.GenerateTableOfContents = GenerateTableOfContents;
        return copy;
    }

    internal void Validate()
    {
        if (WordsPerMinute < 1)
        {
            throw new ArgumentOutOfRangeException("WordsPerMinute");
        }

        if (MinimumReadingTimeSeconds < 0)
        {
            throw new ArgumentOutOfRangeException("MinimumReadingTimeSeconds");
        }

        if (TableOfContentsMinimumLevel < 1 || TableOfContentsMinimumLevel > 6)
        {
            throw new ArgumentOutOfRangeException("TableOfContentsMinimumLevel");
        }

        if (TableOfContentsMaximumLevel < 1 || TableOfContentsMaximumLevel > 6)
        {
            throw new ArgumentOutOfRangeException("TableOfContentsMaximumLevel");
        }

        if (TableOfContentsMaximumLevel < TableOfContentsMinimumLevel)
        {
            throw new ArgumentException("The maximum table-of-contents level must not be below the minimum level.");
        }
    }
}
