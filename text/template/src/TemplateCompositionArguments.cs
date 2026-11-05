using System;
using System.Collections.Generic;

namespace template;

internal class TemplateCompositionArguments
{
    internal TemplateValue Model;
    internal Dictionary<string, TemplateValue> Parameters;
    internal List<string> ContextAliases;

    internal TemplateCompositionArguments(TemplateValue model)
    {
        if (model == null)
        {
            throw new ArgumentNullException("model");
        }

        Model = model;
        Parameters = new Dictionary<string, TemplateValue>();
        ContextAliases = new List<string>();
    }
}
