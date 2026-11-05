using System;

namespace html;
/// <summary>Configures included metadata fields, candidate sources, and resource limits.</summary>
public class HtmlMetadataOptions : IHtmlMetadataOptions
{
    internal bool[] includedFields;
    internal bool[] includedSources;
    internal HtmlLimits limits;

    /// <summary>Creates metadata options that include every supported field and source.</summary>
    public HtmlMetadataOptions()
    {
        this.includedFields = new bool[(int)HtmlMetadataField.ImageUrl + 1];
        this.includedSources = new bool[(int)HtmlMetadataSource.Heuristic + 1];
        this.limits = HtmlLimits.CreateDefault();
        int index = 1;
        while (index < this.includedFields.Length)
        {
            this.includedFields[index] = true;
            index = index + 1;
        }

        index = 1;
        while (index < this.includedSources.Length)
        {
            this.includedSources[index] = true;
            index = index + 1;
        }
    }

    /// <summary>Creates an independent copy that can be changed without affecting this object.</summary>
    /// <returns>An independent metadata-options object containing the same field, source, and limit settings.</returns>
    public HtmlMetadataOptions Copy()
    {
        HtmlMetadataOptions copy = new HtmlMetadataOptions();
        Array.Copy(this.includedFields, copy.includedFields,
            this.includedFields.Length);
        Array.Copy(this.includedSources, copy.includedSources,
            this.includedSources.Length);
        copy.limits = this.limits.Copy();
        return copy;
    }

    /// <summary>Reports whether candidates for a field are included in extraction.</summary>
    /// <param name = "field">The metadata field to inspect.</param>
    /// <returns><c>true</c> when the field is included; otherwise, <c>false</c>.</returns>
    public bool GetIncludeField(HtmlMetadataField field)
    {
        HtmlMetadataValidation.ValidateField(field, "field");
        return this.includedFields[(int)field];
    }

    /// <summary>Controls whether candidates for a field are included in extraction.</summary>
    /// <param name = "field">The metadata field to configure.</param>
    /// <param name = "value"><c>true</c> to include the field; otherwise, <c>false</c>.</param>
    public void SetIncludeField(HtmlMetadataField field, bool value)
    {
        HtmlMetadataValidation.ValidateField(field, "field");
        this.includedFields[(int)field] = value;
    }

    /// <summary>Reports whether candidates from a source are included in extraction.</summary>
    /// <param name = "source">The metadata source to inspect.</param>
    /// <returns><c>true</c> when the source is included; otherwise, <c>false</c>.</returns>
    public bool GetIncludeSource(HtmlMetadataSource source)
    {
        HtmlMetadataValidation.ValidateSource(source, "source");
        return this.includedSources[(int)source];
    }

    /// <summary>Controls whether candidates from a source are included in extraction.</summary>
    /// <param name = "source">The metadata source to configure.</param>
    /// <param name = "value"><c>true</c> to include the source; otherwise, <c>false</c>.</param>
    public void SetIncludeSource(HtmlMetadataSource source, bool value)
    {
        HtmlMetadataValidation.ValidateSource(source, "source");
        this.includedSources[(int)source] = value;
    }

    /// <summary>Gets an independent copy of the configured resource limits.</summary>
    /// <returns>A mutable copy of the limits; changing it does not affect these options.</returns>
    public HtmlLimits GetLimits()
    {
        return this.limits.Copy();
    }

    /// <summary>Sets the resource limits copied by subsequent metadata operations.</summary>
    /// <param name = "value">The validated limits to copy.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required argument is null.</exception>
    public void SetLimits(HtmlLimits value)
    {
        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        value.Validate();
        this.limits = value.Copy();
    }

    internal void Validate()
    {
        if (this.includedFields == null
            || this.includedFields.Length
                != (int)HtmlMetadataField.ImageUrl + 1
            || this.includedSources == null
            || this.includedSources.Length
                != (int)HtmlMetadataSource.Heuristic + 1
            || this.limits == null)
        {
            throw new InvalidOperationException(
                "Metadata options contain a missing required value.");
        }

        this.limits.Validate();
    }
}
