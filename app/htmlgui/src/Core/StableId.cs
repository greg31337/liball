using System;
using System.Text;

namespace htmlgui;

internal static class StableId
{
    internal static string CreateRoot(string? rootSeed, string? nodeKind, string? siblingIdentity)
    {
        string seed = Guard.ArgumentNotWhiteSpace(rootSeed, "rootSeed");
        string kind = Guard.ArgumentNotWhiteSpace(nodeKind, "nodeKind");
        string identity = Guard.ArgumentNotWhiteSpace(siblingIdentity, "siblingIdentity");
        string token = StableHash.HashParts(new string[] { seed, kind, identity });
        return "ui-" + NormalizeKind(kind) + "-" + token.Substring(0, 20);
    }

    internal static string CreateChild(string? rootSeed, string? parentId, string? nodeKind, string? siblingIdentity)
    {
        string seed = Guard.ArgumentNotWhiteSpace(rootSeed, "rootSeed");
        string parent = Guard.ArgumentNotWhiteSpace(parentId, "parentId");
        string kind = Guard.ArgumentNotWhiteSpace(nodeKind, "nodeKind");
        string identity = Guard.ArgumentNotWhiteSpace(siblingIdentity, "siblingIdentity");
        string token = StableHash.HashParts(new string[] { seed, parent, kind, identity });
        return "ui-" + NormalizeKind(kind) + "-" + token.Substring(0, 20);
    }

    internal static string NormalizeKind(string value)
    {
        Guard.ArgumentNotWhiteSpace(value, "value");
        StringBuilder result = new StringBuilder();
        bool previousWasSeparator = false;
        int index = 0;
        while (index < value.Length)
        {
            char character = value[index];
            bool isUpper = character >= 'A' && character <= 'Z';
            bool isLower = character >= 'a' && character <= 'z';
            bool isDigit = character >= '0' && character <= '9';
            if (isUpper || isLower || isDigit)
            {
                if (isUpper)
                {
                    character = (char)(character + ('a' - 'A'));
                }

                result.Append(character);
                previousWasSeparator = false;
            }
            else if (result.Length > 0 && !previousWasSeparator)
            {
                result.Append('-');
                previousWasSeparator = true;
            }

            index++;
        }

        if (result.Length > 0 && result[result.Length - 1] == '-')
        {
            result.Length = result.Length - 1;
        }

        if (result.Length == 0)
        {
            return "node";
        }

        return result.ToString();
    }
}
