using System.Text;

namespace template;

/// <summary>
/// Configures text decoding, resource-name prefixes, and input limits for template loaders.
/// </summary>
public class TemplateLoaderOptions
{
    /// <summary>
    /// Specifies the built-in .NET text encoding used to decode file and embedded-resource content.
    /// </summary>
    public Encoding Encoding;
    /// <summary>
    /// Specifies the maximum decoded template length in characters.
    /// </summary>
    public int MaximumTemplateLength;
    /// <summary>
    /// Specifies the manifest-resource prefix placed before logical embedded template names.
    /// </summary>
    public string ResourcePrefix;

    /// <summary>
    /// Initializes loader options with UTF-8 decoding, a one-megacharacter limit, and no resource prefix.
    /// </summary>
    public TemplateLoaderOptions()
    {
        Encoding = new UTF8Encoding(false, true);
        MaximumTemplateLength = 1048576;
        ResourcePrefix = "";
    }

    internal TemplateLoaderOptions Clone()
    {
        TemplateLoaderOptions clone = new TemplateLoaderOptions();
        clone.Encoding = (Encoding)Encoding.Clone();
        clone.MaximumTemplateLength = MaximumTemplateLength;
        clone.ResourcePrefix = ResourcePrefix;
        return clone;
    }

    internal void Validate()
    {
        if (Encoding == null)
        {
            throw new System.ArgumentNullException("Encoding");
        }

        if (MaximumTemplateLength <= 0)
        {
            throw new System.ArgumentOutOfRangeException("MaximumTemplateLength");
        }

        if (ResourcePrefix == null)
        {
            throw new System.ArgumentNullException("ResourcePrefix");
        }
    }
}
