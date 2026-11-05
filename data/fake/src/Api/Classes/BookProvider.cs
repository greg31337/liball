using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides book and media generation operations.
/// </summary>
public class BookProvider : IBookProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal BookProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("book");
    }

    /// <summary>Generates a project-fictional title from compositional vocabulary.</summary>
    /// <returns>A title that is not copied from a published work.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Title()
    {
        return BookGenerator.Title(Context, RandomValue);
    }

    /// <summary>Generates a project-fictional author name from compositional fixture parts.</summary>
    /// <returns>A fictional author name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Author()
    {
        return BookGenerator.Author(Context, RandomValue);
    }

    /// <summary>Generates a project-fictional publisher name from compositional fixture parts.</summary>
    /// <returns>A fictional publisher name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Publisher()
    {
        return BookGenerator.Publisher(Context, RandomValue);
    }

    /// <summary>Generates a project-fictional series name from compositional vocabulary.</summary>
    /// <returns>A fictional series name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Series()
    {
        return BookGenerator.Series(Context, RandomValue);
    }

    /// <summary>Selects a broad publication genre.</summary>
    /// <returns>A conventional genre name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Genre()
    {
        return BookGenerator.Genre(Context, RandomValue);
    }

    /// <summary>Selects a publication or media format.</summary>
    /// <returns>A conventional physical, electronic, or audio format.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Format()
    {
        return BookGenerator.Format(Context, RandomValue);
    }

    /// <summary>Generates a checksum-valid synthetic ISBN-13 shape.</summary>
    /// <remarks>The value does not claim assignment by an ISBN agency or publisher.</remarks>
    /// <returns>A compact 13-character ISBN shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Isbn()
    {
        return BookGenerator.Isbn(Context, RandomValue);
    }

    /// <summary>Generates a checksum-valid synthetic ISBN-13 shape.</summary>
    /// <remarks>This is an alias for <see cref="Isbn()"/>.</remarks>
    /// <returns>A compact 13-character ISBN shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public string Isbn13()
    {
        return Isbn();
    }

    /// <summary>Generates a publication date in the fifty years through the captured reference time.</summary>
    /// <returns>A publication instant expressed in UTC, clamped at the runtime minimum.</returns>
    public System.DateTimeOffset PublicationDate()
    {
        return BookGenerator.PublicationDate(Context, RandomValue);
    }

    /// <summary>Generates correlated, project-fictional publication metadata.</summary>
    /// <remarks>The title and series share a subject. The ISBN is unassigned and the metadata does not represent a published work.</remarks>
    /// <returns>A correlated fictional publication record.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public BookRecord Record()
    {
        return BookGenerator.Record(Context, RandomValue);
    }
}
