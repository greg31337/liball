namespace fake;

/// <summary>Represents correlated, project-fictional publication metadata.</summary>
/// <remarks>The title, author, publisher, and series are composed fixtures. The ISBN is checksum-valid but does not claim assignment, and the record does not represent a published work.</remarks>
public class BookRecord
{
    /// <summary>The compositional fictional title.</summary>
    public string Title = string.Empty;

    /// <summary>The compositional fictional author name.</summary>
    public string Author = string.Empty;

    /// <summary>The compositional fictional publisher name.</summary>
    public string Publisher = string.Empty;

    /// <summary>The fictional series sharing its subject with <c>Title</c>.</summary>
    public string Series = string.Empty;

    /// <summary>The positive installment number within <c>Series</c>.</summary>
    public int SeriesNumber;

    /// <summary>The broad publication genre.</summary>
    public string Genre = string.Empty;

    /// <summary>The publication or media format.</summary>
    public string Format = string.Empty;

    /// <summary>The checksum-valid synthetic ISBN-13 shape.</summary>
    public string Isbn = string.Empty;

    /// <summary>The publication date at or before the captured reference time.</summary>
    public System.DateTimeOffset PublicationDate;

    /// <summary>Whether the record is synthetic, always <c>true</c>.</summary>
    public bool IsSynthetic;

    /// <summary>Whether the ISBN claims assignment, always <c>false</c>.</summary>
    public bool IsbnIsAssigned;

    /// <summary>Whether the record represents a published work, always <c>false</c>.</summary>
    public bool RepresentsPublishedWork;

    internal BookRecord()
    {
    }
}
