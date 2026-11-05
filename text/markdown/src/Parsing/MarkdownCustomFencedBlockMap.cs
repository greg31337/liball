using System;
using System.Collections.Generic;

namespace markdown;

internal class MarkdownCustomFencedBlockRegistration
{
    internal string InfoKey;

    internal MarkdownCustomFencedBlockRegistration()
    {
        InfoKey = "";
    }

    internal static MarkdownCustomFencedBlockRegistration Create(string infoKey)
    {
        ValidateInfoKey(infoKey);
        MarkdownCustomFencedBlockRegistration registration = new MarkdownCustomFencedBlockRegistration();
        registration.InfoKey = NormalizeInfoKey(infoKey);
        return registration;
    }

    internal MarkdownCustomFencedBlockRegistration Copy()
    {
        return Create(InfoKey);
    }

    internal static void ValidateInfoKey(string infoKey)
    {
        if (infoKey == null)
        {
            throw new ArgumentNullException("infoKey");
        }

        if (!CustomFencedBlockRules.IsValidInfoKey(infoKey))
        {
            throw new ArgumentException("Custom fenced block info key is invalid.");
        }
    }

    internal static string NormalizeInfoKey(string infoKey)
    {
        ValidateInfoKey(infoKey);
        return infoKey.ToLowerInvariant();
    }
}

internal class MarkdownCustomFencedBlockMap
{
    internal List<MarkdownCustomFencedBlockRegistration> Registrations;

    internal MarkdownCustomFencedBlockMap()
    {
        Registrations = new List<MarkdownCustomFencedBlockRegistration>();
    }

    internal int GetCount()
    {
        return Registrations.Count;
    }

    internal MarkdownCustomFencedBlockMap Copy()
    {
        MarkdownCustomFencedBlockMap copy = new MarkdownCustomFencedBlockMap();
        int index = 0;
        while (index < Registrations.Count)
        {
            copy.Registrations.Add(Registrations[index].Copy());
            index = index + 1;
        }

        return copy;
    }

    internal void Set(string infoKey)
    {
        MarkdownCustomFencedBlockRegistration registration = MarkdownCustomFencedBlockRegistration.Create(infoKey);
        int index = IndexOf(infoKey);
        if (index < 0)
        {
            Registrations.Add(registration);
        }
        else
        {
            Registrations[index] = registration;
        }
    }

    internal int IndexOf(string infoKey)
    {
        string normalized = MarkdownCustomFencedBlockRegistration.NormalizeInfoKey(infoKey);
        int index = 0;
        while (index < Registrations.Count)
        {
            if (Registrations[index].InfoKey == normalized)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool Contains(string infoKey)
    {
        return IndexOf(infoKey) >= 0;
    }

    internal void Clear()
    {
        Registrations.Clear();
    }
}
