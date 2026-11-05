using System.Collections.Generic;

namespace email;

internal class EmailRfc2231Group
{
    string name;
    List<EmailRfc2231Name> names;

    internal EmailRfc2231Group(string name)
    {
        this.name = name;
        names = new List<EmailRfc2231Name>();
    }

    internal string GetName()
    {
        return name;
    }

    internal void Add(EmailRfc2231Name name)
    {
        names.Add(name);
    }

    internal List<EmailRfc2231Name> GetAll()
    {
        return new List<EmailRfc2231Name>(names);
    }

    internal List<EmailRfc2231Parameter> GetSourceParameters()
    {
        List<EmailRfc2231Parameter> result = new List<EmailRfc2231Parameter>();
        int index = 0;
        while (index < names.Count)
        {
            result.Add(names[index].GetParameter());
            index++;
        }

        return result;
    }

    internal int GetCount(EmailRfc2231NameKind kind)
    {
        int count = 0;
        int index = 0;
        while (index < names.Count)
        {
            if (names[index].GetKind() == kind)
            {
                count++;
            }

            index++;
        }

        return count;
    }

    internal EmailRfc2231Name? GetFirst(EmailRfc2231NameKind kind)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (names[index].GetKind() == kind)
            {
                return names[index];
            }

            index++;
        }

        return null;
    }

    internal EmailRfc2231Name? GetContinuation(int section)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (names[index].GetKind() == EmailRfc2231NameKind.Continuation && names[index].GetSection() == section)
            {
                return names[index];
            }

            index++;
        }

        return null;
    }

    internal int GetContinuationCount(int section)
    {
        int count = 0;
        int index = 0;
        while (index < names.Count)
        {
            if (names[index].GetKind() == EmailRfc2231NameKind.Continuation && names[index].GetSection() == section)
            {
                count++;
            }

            index++;
        }

        return count;
    }

    internal int GetLowestContinuationSection()
    {
        int lowest = int.MaxValue;
        int index = 0;
        while (index < names.Count)
        {
            if (names[index].GetKind() == EmailRfc2231NameKind.Continuation && names[index].GetSection() < lowest)
            {
                lowest = names[index].GetSection();
            }

            index++;
        }

        if (lowest == int.MaxValue)
        {
            return -1;
        }

        return lowest;
    }

    internal int GetHighestContinuationSection()
    {
        int highest = -1;
        int index = 0;
        while (index < names.Count)
        {
            if (names[index].GetKind() == EmailRfc2231NameKind.Continuation && names[index].GetSection() > highest)
            {
                highest = names[index].GetSection();
            }

            index++;
        }

        return highest;
    }
}
