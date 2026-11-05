namespace ttf;

internal class FontFamilyNameCandidate
{
    FontNameValue value;
    int nameId;
    int uprightRank;
    int widthRank;
    int weightRank;

    internal FontFamilyNameCandidate(
        FontNameValue value,
        int nameId,
        FontClassification classification)
    {
        this.value = value;
        this.nameId = nameId;
        uprightRank = classification.IsItalic() || classification.IsOblique() ? 1 : 0;
        int widthClass = classification.GetWidthClass();
        int weightClass = classification.GetWeightClass();
        widthRank = widthClass == 0 ? 100 : System.Math.Abs(widthClass - 5);
        weightRank = weightClass == 0 ? 10000 : System.Math.Abs(weightClass - 400);
    }

    internal FontNameValue GetValue()
    {
        return value;
    }

    internal bool IsBetterThan(FontFamilyNameCandidate? other)
    {
        if (other == null)
        {
            return true;
        }

        int matchRank = (int)value.GetMatchKind();
        int otherMatchRank = (int)other.value.GetMatchKind();

        if (matchRank != otherMatchRank)
        {
            return matchRank < otherMatchRank;
        }

        int sourceRank = nameId == 16 ? 0 : 1;
        int otherSourceRank = other.nameId == 16 ? 0 : 1;

        if (sourceRank != otherSourceRank)
        {
            return sourceRank < otherSourceRank;
        }

        if (uprightRank != other.uprightRank)
        {
            return uprightRank < other.uprightRank;
        }

        if (widthRank != other.widthRank)
        {
            return widthRank < other.widthRank;
        }

        return weightRank < other.weightRank;
    }
}

internal class FontFamilyMetadata
{
    string[] normalizedNames;

    internal FontFamilyMetadata(FontFace face)
    {
        int nameId = FontFamilyNameResolver.GetPreferredFamilyNameId(face);
        normalizedNames = FontFamilyNameResolver.CopyNormalizedNames(face, nameId);
    }

    internal int GetNameCount()
    {
        return normalizedNames.Length;
    }

    internal string GetName(int index)
    {
        if (index < 0 || index >= normalizedNames.Length)
        {
            throw new System.ArgumentOutOfRangeException("index");
        }

        return normalizedNames[index];
    }
}

internal static class FontFamilyNameResolver
{
    internal static int GetPreferredFamilyNameId(FontFace face)
    {
        SfntNameTable? table = face.GetNameTable();

        if (HasValidName(table, 16))
        {
            return 16;
        }

        return HasValidName(table, 1) ? 1 : -1;
    }

    internal static FontNameValue? SelectFamilyName(FontFace[] faces, string? languageTag)
    {
        FontFamilyNameCandidate? best = null;
        int index = 0;

        while (index < faces.Length)
        {
            FontFace face = faces[index];
            int nameId = GetPreferredFamilyNameId(face);

            if (nameId >= 0)
            {
                FontNameValue? value = SelectName(face, nameId, languageTag);

                if (value != null)
                {
                    FontFamilyNameCandidate candidate = new FontFamilyNameCandidate(
                        value,
                        nameId,
                        face.GetClassification());

                    if (candidate.IsBetterThan(best))
                    {
                        best = candidate;
                    }
                }
            }

            index += 1;
        }

        return best == null ? null : best.GetValue();
    }

    internal static FontNameValue? SelectStyleName(FontFace face, string? languageTag)
    {
        SfntNameTable? table = face.GetNameTable();
        int nameId = HasValidName(table, 17) ? 17 : (HasValidName(table, 2) ? 2 : -1);
        return nameId < 0 ? null : SelectName(face, nameId, languageTag);
    }

    internal static string[] CopyNormalizedNames(FontFace face, int nameId)
    {
        if (nameId < 0)
        {
            return new string[0];
        }

        SfntNameTable? table = face.GetNameTable();
        System.Collections.Generic.List<string> names =
            new System.Collections.Generic.List<string>();
        int index = 0;

        System.ArgumentNullException.ThrowIfNull(table);
        while (index < table.GetRecordCount())
        {
            SfntNameRecord record = table.GetRecord(index);

            if (record.GetNameId() == nameId && record.HasValidLanguage())
            {
                SfntDecodedName decoded = table.GetDecodedName(index);

                if (decoded.IsValid())
                {
                    string normalized = NormalizeFamilyName(decoded.GetValue());

                    if (normalized.Length != 0 && !Contains(names, normalized))
                    {
                        names.Add(normalized);
                    }
                }
            }

            index += 1;
        }

        return names.ToArray();
    }

    static FontNameValue? SelectName(FontFace face, int nameId, string? languageTag)
    {
        FontNames names = languageTag == null
            ? face.GetNames()
            : face.GetLocalizedNames(languageTag);
        return names.GetName(nameId);
    }

    static bool HasValidName(SfntNameTable? table, int nameId)
    {
        if (table == null)
        {
            return false;
        }

        int index = 0;

        while (index < table.GetRecordCount())
        {
            SfntNameRecord record = table.GetRecord(index);

            if (record.GetNameId() == nameId &&
                record.HasValidLanguage() &&
                table.GetDecodedName(index).IsValid())
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static string NormalizeFamilyName(string value)
    {
        string compatible = value.Normalize(System.Text.NormalizationForm.FormKC);
        System.Text.StringBuilder builder = new System.Text.StringBuilder(compatible.Length);
        bool pendingSpace = false;
        int index = 0;

        while (index < compatible.Length)
        {
            char character = compatible[index];

            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length != 0;
            }
            else
            {
                if (pendingSpace)
                {
                    builder.Append(' ');
                    pendingSpace = false;
                }

                builder.Append(character);
            }

            index += 1;
        }

        return builder.ToString().ToUpperInvariant();
    }

    static bool Contains(System.Collections.Generic.List<string> values, string value)
    {
        int index = 0;

        while (index < values.Count)
        {
            if (string.Equals(values[index], value, System.StringComparison.Ordinal))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }
}

internal static class FontFamilyGrouper
{
    internal static FontFamily[] Create(FontFace[] faces)
    {
        FontFamilyMetadata[] metadata = new FontFamilyMetadata[faces.Length];
        int[] parents = new int[faces.Length];
        System.Collections.Generic.Dictionary<string, int> owners =
            new System.Collections.Generic.Dictionary<string, int>(System.StringComparer.Ordinal);
        int index = 0;

        while (index < faces.Length)
        {
            metadata[index] = new FontFamilyMetadata(faces[index]);
            parents[index] = index;
            int nameIndex = 0;

            while (nameIndex < metadata[index].GetNameCount())
            {
                string name = metadata[index].GetName(nameIndex);

                if (owners.ContainsKey(name))
                {
                    Union(parents, index, owners[name]);
                }
                else
                {
                    owners.Add(name, index);
                }

                nameIndex += 1;
            }

            index += 1;
        }

        System.Collections.Generic.Dictionary<int, int> groupIndexes =
            new System.Collections.Generic.Dictionary<int, int>();
        System.Collections.Generic.List<System.Collections.Generic.List<FontFace>> groups =
            new System.Collections.Generic.List<System.Collections.Generic.List<FontFace>>();
        index = 0;

        while (index < faces.Length)
        {
            int root = FindRoot(parents, index);

            if (!groupIndexes.ContainsKey(root))
            {
                groupIndexes.Add(root, groups.Count);
                groups.Add(new System.Collections.Generic.List<FontFace>());
            }

            groups[groupIndexes[root]].Add(faces[index]);
            index += 1;
        }

        FontFamily[] result = new FontFamily[groups.Count];
        index = 0;

        while (index < result.Length)
        {
            result[index] = new FontFamily(groups[index].ToArray());
            index += 1;
        }

        return result;
    }

    static void Union(int[] parents, int first, int second)
    {
        int firstRoot = FindRoot(parents, first);
        int secondRoot = FindRoot(parents, second);

        if (firstRoot == secondRoot)
        {
            return;
        }

        if (firstRoot < secondRoot)
        {
            parents[secondRoot] = firstRoot;
        }
        else
        {
            parents[firstRoot] = secondRoot;
        }
    }

    static int FindRoot(int[] parents, int index)
    {
        int root = index;

        while (parents[root] != root)
        {
            root = parents[root];
        }

        while (parents[index] != index)
        {
            int parent = parents[index];
            parents[index] = root;
            index = parent;
        }

        return root;
    }
}
