using System.Collections.Generic;

namespace http;

/// <summary>
/// Validates and manipulates ordered HTTP field collections.
/// </summary>
public interface IHttpHeaderService
{
    /// <summary>Validates an HTTP field name.</summary>
    /// <param name="name">The field name to validate.</param>
    /// <returns><c>true</c> when the name is valid.</returns>
    bool ValidateFieldName(string name);

    /// <summary>Validates byte-exact HTTP field value data.</summary>
    /// <param name="value">The value octets to validate.</param>
    /// <returns><c>true</c> when the value is valid.</returns>
    bool ValidateFieldValue(byte[] value);

    /// <summary>Adds one field without combining existing lines.</summary>
    /// <param name="headers">The collection to modify.</param>
    /// <param name="field">The field to append.</param>
    void AddField(HttpHeaders headers, HttpHeaderField field);

    /// <summary>Replaces all matching fields with one field.</summary>
    /// <param name="headers">The collection to modify.</param>
    /// <param name="field">The replacement field.</param>
    void SetField(HttpHeaders headers, HttpHeaderField field);

    /// <summary>Removes all fields with an ASCII case-insensitive matching name.</summary>
    /// <param name="headers">The collection to modify.</param>
    /// <param name="name">The field name to remove.</param>
    /// <returns>The number of removed fields.</returns>
    int RemoveFields(HttpHeaders headers, string name);

    /// <summary>Finds all matching field lines in insertion order.</summary>
    /// <param name="headers">The collection to search.</param>
    /// <param name="name">The field name to find.</param>
    /// <returns>The matching field lines.</returns>
    List<HttpHeaderField> FindFields(HttpHeaders headers, string name);

    /// <summary>Combines field values only when the field grammar permits combination.</summary>
    /// <param name="headers">The source field collection.</param>
    /// <param name="name">The field name to combine.</param>
    /// <returns>The combined byte-exact value.</returns>
    byte[] CombineFieldValues(HttpHeaders headers, string name);

    /// <summary>Validates a trailer field section.</summary>
    /// <param name="trailers">The trailer fields to validate.</param>
    /// <returns><c>true</c> when every trailer field is permitted.</returns>
    bool ValidateTrailers(HttpHeaders trailers);
}
