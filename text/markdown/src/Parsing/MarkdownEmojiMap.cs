using System;
using System.Collections.Generic;

namespace markdown;

internal class MarkdownEmojiMapping
{
    internal string Name;
    internal string Replacement;

    internal MarkdownEmojiMapping()
    {
        Name = "";
        Replacement = "";
    }

    internal static MarkdownEmojiMapping Create(string name, string replacement)
    {
        ValidateName(name);
        if (replacement == null)
        {
            throw new ArgumentNullException("replacement");
        }

        MarkdownEmojiMapping mapping = new MarkdownEmojiMapping();
        mapping.Name = name;
        mapping.Replacement = replacement;
        return mapping;
    }

    internal MarkdownEmojiMapping Copy()
    {
        return Create(Name, Replacement);
    }

    internal static void ValidateName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (!EmojiInlineRules.IsValidEmojiName(name))
        {
            throw new ArgumentException("Emoji shortcode name is invalid.");
        }
    }
}

internal class MarkdownEmojiMap
{
    internal List<MarkdownEmojiMapping> Mappings;

    internal MarkdownEmojiMap()
    {
        Mappings = new List<MarkdownEmojiMapping>();
    }

    internal int GetCount()
    {
        return Mappings.Count;
    }

    internal bool IsEmpty()
    {
        return Mappings.Count == 0;
    }

    internal MarkdownEmojiMap Copy()
    {
        MarkdownEmojiMap copy = new MarkdownEmojiMap();
        int index = 0;
        while (index < Mappings.Count)
        {
            copy.Mappings.Add(Mappings[index].Copy());
            index = index + 1;
        }

        return copy;
    }

    internal void Set(string name, string replacement)
    {
        MarkdownEmojiMapping mapping = MarkdownEmojiMapping.Create(name, replacement);
        int index = IndexOf(name);
        if (index < 0)
        {
            Mappings.Add(mapping);
        }
        else
        {
            Mappings[index] = mapping;
        }
    }

    internal int IndexOf(string name)
    {
        MarkdownEmojiMapping.ValidateName(name);

        int index = 0;
        while (index < Mappings.Count)
        {
            if (Mappings[index].Name == name)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool Contains(string name)
    {
        return IndexOf(name) >= 0;
    }

    internal string GetReplacement(string name)
    {
        int index = IndexOf(name);
        if (index < 0)
        {
            return "";
        }

        return Mappings[index].Replacement;
    }

    internal bool Remove(string name)
    {
        int index = IndexOf(name);
        if (index < 0)
        {
            return false;
        }

        Mappings.RemoveAt(index);
        return true;
    }

    internal void Clear()
    {
        Mappings.Clear();
    }
}
