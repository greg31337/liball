using System;
using System.Collections.Generic;

namespace markdown;

internal class MarkdownCustomContainerRegistration
{
    internal string Name;

    internal MarkdownCustomContainerRegistration()
    {
        Name = "";
    }

    internal static MarkdownCustomContainerRegistration Create(string name)
    {
        ValidateName(name);
        MarkdownCustomContainerRegistration registration = new MarkdownCustomContainerRegistration();
        registration.Name = name;
        return registration;
    }

    internal MarkdownCustomContainerRegistration Copy()
    {
        return Create(Name);
    }

    internal static void ValidateName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (!CustomContainerBlockRules.IsValidContainerName(name))
        {
            throw new ArgumentException("Custom container name is invalid.");
        }
    }
}

internal class MarkdownCustomContainerMap
{
    internal List<MarkdownCustomContainerRegistration> Registrations;

    internal MarkdownCustomContainerMap()
    {
        Registrations = new List<MarkdownCustomContainerRegistration>();
    }

    internal int GetCount()
    {
        return Registrations.Count;
    }

    internal bool IsEmpty()
    {
        return Registrations.Count == 0;
    }

    internal MarkdownCustomContainerMap Copy()
    {
        MarkdownCustomContainerMap copy = new MarkdownCustomContainerMap();
        int index = 0;
        while (index < Registrations.Count)
        {
            copy.Registrations.Add(Registrations[index].Copy());
            index = index + 1;
        }

        return copy;
    }

    internal void Set(string name)
    {
        MarkdownCustomContainerRegistration registration = MarkdownCustomContainerRegistration.Create(name);
        int index = IndexOf(name);
        if (index < 0)
        {
            Registrations.Add(registration);
        }
        else
        {
            Registrations[index] = registration;
        }
    }

    internal int IndexOf(string name)
    {
        MarkdownCustomContainerRegistration.ValidateName(name);

        int index = 0;
        while (index < Registrations.Count)
        {
            if (Registrations[index].Name == name)
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

    internal bool Remove(string name)
    {
        int index = IndexOf(name);
        if (index < 0)
        {
            return false;
        }

        Registrations.RemoveAt(index);
        return true;
    }

    internal void Clear()
    {
        Registrations.Clear();
    }
}
