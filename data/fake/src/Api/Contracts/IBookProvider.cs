using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="BookProvider"/>.</summary>
public interface IBookProvider
{
    /// <summary>Generates a project-fictional title from compositional vocabulary.</summary>
    /// <returns>A title that is not copied from a published work.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Title();
    /// <summary>Generates a project-fictional author name from compositional fixture parts.</summary>
    /// <returns>A fictional author name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Author();
    /// <summary>Generates a project-fictional publisher name from compositional fixture parts.</summary>
    /// <returns>A fictional publisher name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Publisher();
    /// <summary>Generates a project-fictional series name from compositional vocabulary.</summary>
    /// <returns>A fictional series name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Series();
    /// <summary>Selects a broad publication genre.</summary>
    /// <returns>A conventional genre name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Genre();
    /// <summary>Selects a publication or media format.</summary>
    /// <returns>A conventional physical, electronic, or audio format.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Format();
    /// <summary>Generates a checksum-valid synthetic ISBN-13 shape.</summary>
    /// <remarks>The value does not claim assignment by an ISBN agency or publisher.</remarks>
    /// <returns>A compact 13-character ISBN shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Isbn();
    /// <summary>Generates a checksum-valid synthetic ISBN-13 shape.</summary>
    /// <remarks>This is an alias for <see cref="Isbn()"/>.</remarks>
    /// <returns>A compact 13-character ISBN shape.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Isbn13();
    /// <summary>Generates a publication date in the fifty years through the captured reference time.</summary>
    /// <returns>A publication instant expressed in UTC, clamped at the runtime minimum.</returns>
    System.DateTimeOffset PublicationDate();
    /// <summary>Generates correlated, project-fictional publication metadata.</summary>
    /// <remarks>The title and series share a subject. The ISBN is unassigned and the metadata does not represent a published work.</remarks>
    /// <returns>A correlated fictional publication record.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    BookRecord Record();
}
