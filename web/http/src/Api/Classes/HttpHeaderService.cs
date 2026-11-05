using System.Collections.Generic;

namespace http;

/// <summary>
/// Provides validated, ordered operations over HTTP field collections.
/// </summary>
public class HttpHeaderService : IHttpHeaderService
{
    /// <summary>Creates an HTTP header service.</summary>
    public HttpHeaderService()
    {
    }

    /// <summary>Validates that a field name is a non-empty ASCII HTTP token.</summary>
    /// <param name="name">The field name to validate.</param>
    /// <returns><c>true</c> when the name is valid.</returns>
    public bool ValidateFieldName(string name)
    {
        return HeaderStore.IsValidName(name);
    }

    /// <summary>Validates a byte-exact field value while permitting horizontal tab and <c>obs-text</c>.</summary>
    /// <param name="value">The value octets to validate.</param>
    /// <returns><c>true</c> when the value contains no prohibited control octets.</returns>
    public bool ValidateFieldValue(byte[] value)
    {
        return HeaderStore.IsValidValue(value);
    }

    /// <summary>Adds one validated field at the end of the collection without combining duplicates.</summary>
    /// <param name="headers">The collection to modify.</param>
    /// <param name="field">The field to append.</param>
    /// <exception cref="HttpFieldException">The field name or value is invalid.</exception>
    public void AddField(HttpHeaders headers, HttpHeaderField field)
    {
        ValidateFieldForMutation(field);
        HeaderStore.AppendField(headers, field);
    }

    /// <summary>Replaces all case-insensitive matches with one validated field at the first matching position.</summary>
    /// <param name="headers">The collection to modify.</param>
    /// <param name="field">The replacement field.</param>
    /// <exception cref="HttpFieldException">The field name or value is invalid.</exception>
    public void SetField(HttpHeaders headers, HttpHeaderField field)
    {
        ValidateFieldForMutation(field);
        HeaderStore.ReplaceFields(headers, field);
    }

    /// <summary>Removes every field whose name matches using ASCII case-insensitive comparison.</summary>
    /// <param name="headers">The collection to modify.</param>
    /// <param name="name">The field name to remove.</param>
    /// <returns>The number of removed fields.</returns>
    /// <exception cref="HttpFieldException">The lookup name is invalid.</exception>
    public int RemoveFields(HttpHeaders headers, string name)
    {
        ValidateLookupName(name);
        return HeaderStore.RemoveMatchingFields(headers, name);
    }

    /// <summary>Returns matching field lines in insertion order without modifying their names or values.</summary>
    /// <param name="headers">The collection to search.</param>
    /// <param name="name">The field name to find.</param>
    /// <returns>The ordered matching field lines.</returns>
    /// <exception cref="HttpFieldException">The lookup name is invalid.</exception>
    public List<HttpHeaderField> FindFields(HttpHeaders headers, string name)
    {
        ValidateLookupName(name);
        return HeaderStore.FindMatchingFields(headers, name);
    }

    /// <summary>Combines matching list-valued field lines without changing their byte content.</summary>
    /// <param name="headers">The source field collection.</param>
    /// <param name="name">The field name to combine.</param>
    /// <returns>An owned combined value, or an empty array when no field matches.</returns>
    /// <exception cref="HttpFieldException">The name, a value, or the requested combination is invalid.</exception>
    public byte[] CombineFieldValues(HttpHeaders headers, string name)
    {
        ValidateLookupName(name);
        return HeaderStore.CombineMatchingValues(headers, name);
    }

    /// <summary>Validates field syntax and the field-name restrictions for a trailer section.</summary>
    /// <param name="trailers">The trailer fields to validate.</param>
    /// <returns><c>true</c> when every trailer field is valid and permitted.</returns>
    public bool ValidateTrailers(HttpHeaders trailers)
    {
        return HeaderStore.AreValidTrailers(trailers);
    }

    internal void ValidateFieldForMutation(HttpHeaderField field)
    {
        if (!HeaderStore.IsValidName(field.Name))
        {
            throw new HttpFieldException(
                "The field name is not a valid non-empty ASCII token.",
                "invalid-field-name",
                field.Name);
        }

        if (!HeaderStore.IsValidValue(field.Value))
        {
            throw new HttpFieldException(
                "The field value contains a prohibited control octet.",
                "invalid-field-value",
                field.Name);
        }
    }

    internal void ValidateLookupName(string name)
    {
        if (!HeaderStore.IsValidName(name))
        {
            throw new HttpFieldException(
                "The field name is not a valid non-empty ASCII token.",
                "invalid-field-name",
                name);
        }
    }
}
