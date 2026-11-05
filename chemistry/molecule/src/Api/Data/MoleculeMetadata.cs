using System.Collections.Generic;

namespace molecule;


/// <summary>
/// Provides molecule metadata data and operations for molecular graphs.
/// </summary>
public class MoleculeMetadata
{
    /// <summary>
    /// The names.
    /// </summary>
    public List<string> Names;
    /// <summary>
    /// The comments.
    /// </summary>
    public List<string> Comments;
    /// <summary>
    /// The sdf fields.
    /// </summary>
    public List<MoleculeMetadataEntry> SdfFields;
    /// <summary>
    /// The pdb fields.
    /// </summary>
    public List<MoleculeMetadataEntry> PdbFields;
    /// <summary>
    /// The vendor fields.
    /// </summary>
    public List<MoleculeMetadataEntry> VendorFields;
    /// <summary>
    /// The string values.
    /// </summary>
    public List<MoleculeMetadataEntry> StringValues;

    /// <summary>
    /// Initializes a new instance of the <see cref="MoleculeMetadata"/> class.
    /// </summary>
    public MoleculeMetadata()
    {
        Names = new List<string>();
        Comments = new List<string>();
        SdfFields = new List<MoleculeMetadataEntry>();
        PdbFields = new List<MoleculeMetadataEntry>();
        VendorFields = new List<MoleculeMetadataEntry>();
        StringValues = new List<MoleculeMetadataEntry>();
    }

    /// <summary>
    /// Adds the name.
    /// </summary>
    /// <param name="name">The name.</param>
    internal void AddName(string? name)
    {
        Names.Add(NormalizeString(name));
    }

    /// <summary>
    /// Returns the primary name.
    /// </summary>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    internal string GetPrimaryName()
    {
        if (Names.Count == 0)
        {
            return "";
        }

        return Names[0];
    }

    /// <summary>
    /// Adds the comment.
    /// </summary>
    /// <param name="comment">The comment.</param>
    internal void AddComment(string? comment)
    {
        Comments.Add(NormalizeString(comment));
    }

    /// <summary>
    /// Adds the SDF field.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    internal void AddSdfField(string? key, string? value)
    {
        AddField(SdfFields, key, value);
    }

    /// <summary>
    /// Adds the PDB field.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    internal void AddPdbField(string? key, string? value)
    {
        AddField(PdbFields, key, value);
    }

    /// <summary>
    /// Adds the vendor field.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    internal void AddVendorField(string? key, string? value)
    {
        AddField(VendorFields, key, value);
    }

    /// <summary>
    /// Sets the string value.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <param name="value">The value.</param>
    internal void SetStringValue(string? key, string? value)
    {
        SetField(StringValues, key, value);
    }

    /// <summary>
    /// Returns the string value.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    internal string GetStringValue(string? key)
    {
        return GetFirstFieldValue(StringValues, key);
    }

    /// <summary>
    /// Determines whether the value has string value.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns><c>true</c> when the documented condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool HasStringValue(string? key)
    {
        return HasField(StringValues, key);
    }

    /// <summary>
    /// Returns the SDF field.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    internal string GetSdfField(string? key)
    {
        return GetFirstFieldValue(SdfFields, key);
    }

    /// <summary>
    /// Returns the PDB field.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    internal string GetPdbField(string? key)
    {
        return GetFirstFieldValue(PdbFields, key);
    }

    /// <summary>
    /// Returns the vendor field.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The resulting text; an empty string indicates that no value could be produced.</returns>
    internal string GetVendorField(string? key)
    {
        return GetFirstFieldValue(VendorFields, key);
    }

    /// <summary>
    /// Returns the SDF field values.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    internal List<string> GetSdfFieldValues(string? key)
    {
        return GetFieldValues(SdfFields, key);
    }

    /// <summary>
    /// Returns the PDB field values.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    internal List<string> GetPdbFieldValues(string? key)
    {
        return GetFieldValues(PdbFields, key);
    }

    /// <summary>
    /// Returns the vendor field values.
    /// </summary>
    /// <param name="key">The key.</param>
    /// <returns>The resulting values in deterministic order.</returns>
    internal List<string> GetVendorFieldValues(string? key)
    {
        return GetFieldValues(VendorFields, key);
    }

    internal void AddField(List<MoleculeMetadataEntry> fields, string? key, string? value)
    {
        fields.Add(new MoleculeMetadataEntry(key, value));
    }

    internal void SetField(List<MoleculeMetadataEntry> fields, string? key, string? value)
    {
        string normalizedKey = NormalizeString(key);
        string normalizedValue = NormalizeString(value);
        int index = 0;

        while (index < fields.Count)
        {
            if (fields[index].Key == normalizedKey)
            {
                fields[index].Value = normalizedValue;
                return;
            }

            index = index + 1;
        }

        fields.Add(new MoleculeMetadataEntry(normalizedKey, normalizedValue));
    }

    internal bool HasField(List<MoleculeMetadataEntry> fields, string? key)
    {
        string normalizedKey = NormalizeString(key);
        int index = 0;

        while (index < fields.Count)
        {
            if (fields[index].Key == normalizedKey)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string GetFirstFieldValue(List<MoleculeMetadataEntry> fields, string? key)
    {
        string normalizedKey = NormalizeString(key);
        int index = 0;

        while (index < fields.Count)
        {
            if (fields[index].Key == normalizedKey)
            {
                return fields[index].Value;
            }

            index = index + 1;
        }

        return "";
    }

    internal List<string> GetFieldValues(List<MoleculeMetadataEntry> fields, string? key)
    {
        List<string> values = new List<string>();
        string normalizedKey = NormalizeString(key);
        int index = 0;

        while (index < fields.Count)
        {
            if (fields[index].Key == normalizedKey)
            {
                values.Add(fields[index].Value);
            }

            index = index + 1;
        }

        return values;
    }

    /// <summary>
    /// Creates an independent copy of the value and its mutable collections.
    /// </summary>
    /// <returns>The resulting molecule metadata.</returns>
    internal MoleculeMetadata Clone()
    {
        MoleculeMetadata metadata = new MoleculeMetadata();
        CopyStrings(Names, metadata.Names);
        CopyStrings(Comments, metadata.Comments);
        CopyFields(SdfFields, metadata.SdfFields);
        CopyFields(PdbFields, metadata.PdbFields);
        CopyFields(VendorFields, metadata.VendorFields);
        CopyFields(StringValues, metadata.StringValues);
        return metadata;
    }

    /// <summary>
    /// Resets this object to its empty state.
    /// </summary>
    internal void Clear()
    {
        Names.Clear();
        Comments.Clear();
        SdfFields.Clear();
        PdbFields.Clear();
        VendorFields.Clear();
        StringValues.Clear();
    }

    internal static string NormalizeString(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }

    internal static void CopyStrings(List<string> source, List<string> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            destination.Add(source[index]);
            index = index + 1;
        }
    }

    internal static void CopyFields(List<MoleculeMetadataEntry> source, List<MoleculeMetadataEntry> destination)
    {
        int index = 0;
        while (index < source.Count)
        {
            MoleculeMetadataEntry field = source[index];
            destination.Add(new MoleculeMetadataEntry(field.Key, field.Value));
            index = index + 1;
        }
    }
}

