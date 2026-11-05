using System.Collections.Generic;

namespace pptx;

/// <summary>
/// Represents template data data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxTemplateData
{
    /// <summary>
    /// Contains the keys in package or presentation order.
    /// </summary>
    public List<string> Keys;
    /// <summary>
    /// Contains the values in package or presentation order.
    /// </summary>
    public List<string> Values;

    /// <summary>
    /// Initializes a new template data with safe defaults or the supplied values.
    /// </summary>
    public PptxTemplateData()
    {
        Keys = new List<string>();
        Values = new List<string>();
    }
    internal int Count()
    {
        return Keys.Count;
    }
    internal void Add(string key, string value)
    {
        string cleanKey = CleanKey(key);
        if (cleanKey.Length == 0)
        {
            throw new System.ArgumentException("Template key is required.");
        }

        int index = IndexOf(cleanKey);
        if (index >= 0)
        {
            Values[index] = PptxDiagnostic.CleanText(value);
            return;
        }

        Keys.Add(cleanKey);
        Values.Add(PptxDiagnostic.CleanText(value));
    }
    internal bool Contains(string key)
    {
        return IndexOf(key) >= 0;
    }
    internal string Get(string key)
    {
        int index = IndexOf(key);
        if (index < 0)
        {
            return "";
        }

        return Values[index];
    }
    internal int IndexOf(string key)
    {
        string cleanKey = CleanKey(key);
        for (int index = 0; index < Keys.Count; index++)
        {
            if (Keys[index] == cleanKey)
            {
                return index;
            }
        }

        return -1;
    }
    internal static PptxTemplateData FromDictionary(Dictionary<string, string> values)
    {
        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        PptxTemplateData data = new PptxTemplateData();
        foreach (KeyValuePair<string, string> entry in values)
        {
            data.Add(entry.Key, entry.Value);
        }

        return data;
    }
    internal static PptxTemplateData FromLists(List<string> keys, List<string> values)
    {
        if (keys == null)
        {
            throw new System.ArgumentNullException("keys");
        }

        if (values == null)
        {
            throw new System.ArgumentNullException("values");
        }

        if (keys.Count != values.Count)
        {
            throw new System.ArgumentException("Template key and value counts must match.");
        }

        PptxTemplateData data = new PptxTemplateData();
        for (int index = 0; index < keys.Count; index++)
        {
            data.Add(keys[index], values[index]);
        }

        return data;
    }

    internal static string CleanKey(string key)
    {
        return PptxDiagnostic.CleanText(key).Trim();
    }
}

