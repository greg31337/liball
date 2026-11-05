using System.Collections.Generic;

namespace molecule;

internal class MaccsKeys
{
    internal List<MaccsKey> Keys;

    internal MaccsKeys()
    {
        Keys = new List<MaccsKey>();
        RegisterDefaults();
    }

    internal void RegisterDefaults()
    {
        Keys.Clear();
        AddKey(1, "carbon", "[#6]", "Contains carbon.");
        AddKey(2, "nitrogen", "[#7]", "Contains nitrogen.");
        AddKey(3, "oxygen", "[#8]", "Contains oxygen.");
        AddKey(4, "sulfur", "[#16]", "Contains sulfur.");
        AddKey(5, "phosphorus", "[#15]", "Contains phosphorus.");
        AddKey(6, "halogen", "[#9,#17,#35,#53]", "Contains a halogen.");
        AddKey(7, "hetero-n-o-p-s", "[#7,#8,#15,#16]", "Contains common hetero atom.");
        AddKey(8, "aromatic-atom", "[a]", "Contains aromatic atom.");
        AddKey(9, "ring-atom", "[R]", "Contains ring atom.");
        AddKey(10, "carbonyl", "C=O", "Contains carbonyl C=O.");
        AddKey(11, "carbon-nitrogen-single", "C-N", "Contains C-N single bond.");
        AddKey(12, "carbon-oxygen-single", "C-O", "Contains C-O single bond.");
        AddKey(13, "carbon-carbon-single", "C-C", "Contains C-C single bond.");
        AddKey(14, "carbon-carbon-double", "C=C", "Contains C=C double bond.");
        AddKey(15, "carbon-nitrogen-double", "C=N", "Contains C=N double bond.");
        AddKey(16, "carbon-nitrogen-triple", "C#N", "Contains C#N triple bond.");
    }

    internal void AddKey(int index, string name, string smarts, string description)
    {
        MaccsKey key = new MaccsKey();
        key.Index = index;
        key.Name = NormalizeString(name);
        key.Smarts = NormalizeString(smarts);
        key.Description = NormalizeString(description);
        Keys.Add(key);
    }

    internal List<MaccsKey> GetKeys()
    {
        List<MaccsKey> keys = new List<MaccsKey>();
        int index = 0;
        while (index < Keys.Count)
        {
            keys.Add(Keys[index]);
            index = index + 1;
        }

        return keys;
    }

    internal MaccsKey FindKey(string name)
    {
        string normalized = NormalizeKey(name);
        int index = 0;
        while (index < Keys.Count)
        {
            if (NormalizeKey(Keys[index].Name) == normalized)
            {
                return Keys[index];
            }

            index = index + 1;
        }

        return new MaccsKey();
    }

    internal MaccsKey FindKeyByIndex(int keyIndex)
    {
        int index = 0;
        while (index < Keys.Count)
        {
            if (Keys[index].Index == keyIndex)
            {
                return Keys[index];
            }

            index = index + 1;
        }

        return new MaccsKey();
    }

    internal bool ContainsKey(string name)
    {
        return FindKey(name).Name.Length > 0;
    }

    internal string NormalizeKey(string value)
    {
        return NormalizeString(value).Trim().ToLowerInvariant();
    }

    internal string NormalizeString(string? value)
    {
        if (value == null)
        {
            return "";
        }

        return value;
    }
}

internal class MaccsKey
{
    internal int Index;
    internal string Name;
    internal string Smarts;
    internal string Description;

    internal MaccsKey()
    {
        Index = 0;
        Name = "";
        Smarts = "";
        Description = "";
    }
}
