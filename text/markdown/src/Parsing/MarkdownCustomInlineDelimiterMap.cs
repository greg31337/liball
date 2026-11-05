using System;
using System.Collections.Generic;

namespace markdown;

internal class MarkdownCustomInlineDelimiterRegistration
{
    internal string Name;
    internal string OpeningDelimiter;
    internal string ClosingDelimiter;

    internal MarkdownCustomInlineDelimiterRegistration()
    {
        Name = "";
        OpeningDelimiter = "";
        ClosingDelimiter = "";
    }

    internal static MarkdownCustomInlineDelimiterRegistration Create(string name, string openingDelimiter, string closingDelimiter)
    {
        MarkdownCustomContainerRegistration.ValidateName(name);
        ValidateDelimiter(openingDelimiter, "openingDelimiter");
        ValidateDelimiter(closingDelimiter, "closingDelimiter");
        MarkdownCustomInlineDelimiterRegistration registration = new MarkdownCustomInlineDelimiterRegistration();
        registration.Name = name;
        registration.OpeningDelimiter = openingDelimiter;
        registration.ClosingDelimiter = closingDelimiter;
        return registration;
    }

    internal MarkdownCustomInlineDelimiterRegistration Copy()
    {
        return Create(Name, OpeningDelimiter, ClosingDelimiter);
    }

    internal static void ValidateDelimiter(string delimiter, string parameterName)
    {
        if (delimiter == null)
        {
            throw new ArgumentNullException(parameterName);
        }

        if (delimiter.Length == 0)
        {
            throw new ArgumentException("Delimiter cannot be empty.");
        }
    }
}

internal class MarkdownCustomInlineDelimiterMap
{
    internal List<MarkdownCustomInlineDelimiterRegistration> Registrations;

    internal MarkdownCustomInlineDelimiterMap()
    {
        Registrations = new List<MarkdownCustomInlineDelimiterRegistration>();
    }

    internal int GetCount()
    {
        return Registrations.Count;
    }

    internal MarkdownCustomInlineDelimiterMap Copy()
    {
        MarkdownCustomInlineDelimiterMap copy = new MarkdownCustomInlineDelimiterMap();
        int index = 0;
        while (index < Registrations.Count)
        {
            copy.Registrations.Add(Registrations[index].Copy());
            index = index + 1;
        }

        return copy;
    }

    internal void Set(string name, string openingDelimiter, string closingDelimiter)
    {
        MarkdownCustomInlineDelimiterRegistration registration = MarkdownCustomInlineDelimiterRegistration.Create(name, openingDelimiter, closingDelimiter);
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

    internal MarkdownCustomInlineDelimiterRegistration[] GetRegistrations()
    {
        return Registrations.ToArray();
    }

    internal void Clear()
    {
        Registrations.Clear();
    }
}
