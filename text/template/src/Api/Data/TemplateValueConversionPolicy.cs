using System;
using System.Collections.Generic;
using System.Globalization;

namespace template;

/// <summary>
/// Configures culture-aware map-key conversion and ordered application-specific value converters.
/// </summary>
public class TemplateValueConversionPolicy
{
    /// <summary>
    /// Supplies culture-specific formatting when non-string dictionary keys become template map keys.
    /// </summary>
    public CultureInfo Culture;
    /// <summary>
    /// Stores application-specific converters in the order in which they are queried.
    /// </summary>
    public List<ICustomTemplateValueConverter> Converters;

    /// <summary>
    /// Initializes a conversion policy with invariant culture and no custom converters.
    /// </summary>
    public TemplateValueConversionPolicy()
    {
        Culture = CultureInfo.InvariantCulture;
        Converters = new List<ICustomTemplateValueConverter>();
    }

    internal void Validate()
    {
        if (Culture == null)
        {
            throw new ArgumentNullException("Culture");
        }

        if (Converters == null)
        {
            throw new ArgumentNullException("Converters");
        }

        int index = 0;
        while (index < Converters.Count)
        {
            if (Converters[index] == null)
            {
                throw new ArgumentException("Custom converters cannot contain null.", "Converters");
            }

            index = index + 1;
        }
    }
}
