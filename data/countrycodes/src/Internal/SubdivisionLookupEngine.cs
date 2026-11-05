using System.Collections.Generic;

namespace countrycodes;

internal static class SubdivisionLookupEngine
{
    internal static Subdivision[] GetAll()
    {
        return SubdivisionMapper.MapSubdivisions(RuntimeCatalog.GetState().Subdivisions);
    }

    internal static Subdivision[] GetForCountry(string? countryAlpha2)
    {
        if (countryAlpha2 == null)
        {
            return new Subdivision[0];
        }

        char[] canonical = new char[CountryCodeConstants.Alpha2Length];
        if (!AsciiCodeUtility.TryNormalizeAlpha2(countryAlpha2, canonical))
        {
            return new Subdivision[0];
        }

        GeneratedSubdivisionRow[] rows =
            SubdivisionIndex.GetSubdivisionsForCountry(new string(canonical));
        return SubdivisionMapper.MapSubdivisions(rows);
    }

    internal static Subdivision[] GetByStatus(SubdivisionStatus status)
    {
        List<Subdivision> matches = new List<Subdivision>();
        GeneratedSubdivisionRow[] rows = RuntimeCatalog.GetState().Subdivisions;
        int index = 0;
        while (index < rows.Length)
        {
            GeneratedSubdivisionRow row = rows[index];
            if (SubdivisionMapper.GetSubdivisionStatus(row) == status)
            {
                matches.Add(SubdivisionMapper.MapSubdivision(row));
            }

            index += 1;
        }

        GeneratedSubdivisionAliasRow[] aliases =
            RuntimeCatalog.GetState().SubdivisionAliases;
        index = 0;
        while (index < aliases.Length)
        {
            GeneratedSubdivisionAliasRow alias = aliases[index];
            if (!alias.IsCurrentSubdivision &&
                SubdivisionMapper.MapAliasStatus(alias.Reason) == status)
            {
                InsertSubdivisionInCodeOrder(
                    matches,
                    SubdivisionMapper.MapSubdivisionAlias(alias));
            }

            index += 1;
        }

        return matches.ToArray();
    }

    internal static SubdivisionLookupResult FindCodeText(string? value)
    {
        if (value == null)
        {
            return CreateInvalidResult();
        }

        return FindCodeCharacters(value);
    }

    internal static SubdivisionLookupResult FindCodeCharacters(
        global::System.ReadOnlySpan<char> value)
    {
        int maximumLength = CountryCodeConstants.SubdivisionCountryCodeLength +
                            1 +
                            CountryCodeConstants.SubdivisionMaximumSuffixLength;
        char[] canonical = new char[maximumLength];
        if (!AsciiCodeUtility.TryNormalizeSubdivisionCode(value, canonical, false))
        {
            return CreateInvalidResult();
        }

        string canonicalCode = new string(canonical, 0, value.Length);
        GeneratedSubdivisionRow? row =
            SubdivisionIndex.FindSubdivisionByCode(canonicalCode);
        if (row != null)
        {
            return CreateSubdivisionResult(row);
        }

        GeneratedSubdivisionAliasRow? alias =
            SubdivisionIndex.FindSubdivisionAliasByCode(canonicalCode);
        return CreateAliasResult(alias);
    }

    internal static SubdivisionLookupResult FindCountryAndSuffix(
        string? countryAlpha2,
        string? suffix)
    {
        if (countryAlpha2 == null || suffix == null ||
            countryAlpha2.Length != CountryCodeConstants.Alpha2Length ||
            suffix.Length < CountryCodeConstants.SubdivisionMinimumSuffixLength ||
            suffix.Length > CountryCodeConstants.SubdivisionMaximumSuffixLength)
        {
            return CreateInvalidResult();
        }

        string code = countryAlpha2 + CountryCodeConstants.SubdivisionSeparator + suffix;
        return FindCodeText(code);
    }

    internal static SubdivisionLookupResult FindNameText(string? name)
    {
        return FindNameCandidates(name, null, false);
    }

    internal static SubdivisionLookupResult FindNameInCountry(
        string? countryAlpha2,
        string? name)
    {
        if (countryAlpha2 == null)
        {
            return CreateInvalidResult();
        }

        char[] canonical = new char[CountryCodeConstants.Alpha2Length];
        if (!AsciiCodeUtility.TryNormalizeAlpha2(countryAlpha2, canonical))
        {
            return CreateInvalidResult();
        }

        return FindNameCandidates(name, new string(canonical), true);
    }

    internal static SubdivisionLookupResult FindNameCandidates(
        string? name,
        string? countryAlpha2,
        bool constrainCountry)
    {
        if (name == null || name.Length == 0)
        {
            return CreateInvalidResult();
        }

        string? searchKey = UnicodeNameUtility.CreateMatchKey(
            name,
            true,
            true,
            true,
            true);
        if (searchKey == null || searchKey.Length == 0)
        {
            return CreateInvalidResult();
        }

        GeneratedSubdivisionNameRow[] nameRows =
            NameIndex.FindSubdivisionNamesBySearchKey(searchKey);
        List<GeneratedSubdivisionRow> exactRows = new List<GeneratedSubdivisionRow>();
        List<GeneratedSubdivisionRow> normalizedRows =
            new List<GeneratedSubdivisionRow>();
        int index = 0;
        while (index < nameRows.Length)
        {
            GeneratedSubdivisionNameRow nameRow = nameRows[index];
            if (nameRow.IsCurrent && IsLookupNameKind(nameRow.NameKind))
            {
                GeneratedSubdivisionRow? subdivisionRow =
                    SubdivisionIndex.FindSubdivisionByCode(nameRow.SubdivisionCode);
                if (subdivisionRow != null &&
                    (!constrainCountry ||
                     AsciiCodeUtility.EqualsOrdinal(
                         subdivisionRow.CountryCode,
                         countryAlpha2)))
                {
                    AddSubdivisionInOrder(normalizedRows, subdivisionRow);
                    if (global::System.String.Equals(
                        nameRow.DisplayName,
                        name,
                        global::System.StringComparison.OrdinalIgnoreCase))
                    {
                        AddSubdivisionInOrder(exactRows, subdivisionRow);
                    }
                }
            }

            index += 1;
        }

        if (exactRows.Count > 0)
        {
            return CreateCandidateResult(exactRows.ToArray());
        }

        return CreateCandidateResult(normalizedRows.ToArray());
    }

    internal static CountryLookupResult ResolveCountry(string? code)
    {
        if (code == null)
        {
            return CountryLookupEngine.CreateInvalidResult();
        }

        int maximumLength = CountryCodeConstants.SubdivisionCountryCodeLength +
                            1 +
                            CountryCodeConstants.SubdivisionMaximumSuffixLength;
        char[] canonical = new char[maximumLength];
        if (!AsciiCodeUtility.TryNormalizeSubdivisionCode(code, canonical, false))
        {
            return CountryLookupEngine.CreateInvalidResult();
        }

        char[] countryCode = new char[CountryCodeConstants.Alpha2Length];
        countryCode[0] = canonical[0];
        countryCode[1] = canonical[1];
        return CountryLookupEngine.FindAlpha2Text(new string(countryCode));
    }

    internal static SubdivisionLookupResult FindParent(string? code)
    {
        SubdivisionLookupResult subdivisionResult = FindCodeText(code);
        if (subdivisionResult.Status != LookupStatus.Found ||
            subdivisionResult.Subdivision == null)
        {
            return subdivisionResult;
        }

        GeneratedSubdivisionRow? subdivision =
            SubdivisionIndex.FindSubdivisionByCode(subdivisionResult.Subdivision.Code);
        if (subdivision == null || !subdivision.HasParent)
        {
            return CreateNotFoundResult();
        }

        GeneratedSubdivisionRow? parent = SubdivisionHierarchy.FindParentRow(
            subdivision,
            RuntimeCatalog.GetState().Subdivisions);
        return CreateSubdivisionResult(parent);
    }

    internal static Subdivision[] GetChildren(string? code)
    {
        GeneratedSubdivisionRow? subdivision = FindHierarchyStart(code);
        if (subdivision == null)
        {
            return new Subdivision[0];
        }

        GeneratedSubdivisionRow[] children = SubdivisionHierarchy.GetChildRows(
            subdivision,
            RuntimeCatalog.GetState().Subdivisions);
        return SubdivisionMapper.MapSubdivisions(children);
    }

    internal static Subdivision[] GetAncestors(string? code)
    {
        GeneratedSubdivisionRow? subdivision = FindHierarchyStart(code);
        if (subdivision == null)
        {
            return new Subdivision[0];
        }

        GeneratedSubdivisionRow[] ancestors = SubdivisionHierarchy.GetAncestorRows(
            subdivision,
            RuntimeCatalog.GetState().Subdivisions);
        return SubdivisionMapper.MapSubdivisions(ancestors);
    }

    internal static Subdivision[] GetDescendants(string? code)
    {
        GeneratedSubdivisionRow? subdivision = FindHierarchyStart(code);
        if (subdivision == null)
        {
            return new Subdivision[0];
        }

        GeneratedSubdivisionRow[] descendants = SubdivisionHierarchy.GetDescendantRows(
            subdivision,
            RuntimeCatalog.GetState().Subdivisions);
        return SubdivisionMapper.MapSubdivisions(descendants);
    }

    internal static GeneratedSubdivisionRow? FindHierarchyStart(string? code)
    {
        if (code == null)
        {
            return null;
        }

        int maximumLength = CountryCodeConstants.SubdivisionCountryCodeLength +
                            1 +
                            CountryCodeConstants.SubdivisionMaximumSuffixLength;
        char[] canonical = new char[maximumLength];
        if (!AsciiCodeUtility.TryNormalizeSubdivisionCode(code, canonical, false))
        {
            return null;
        }

        string canonicalCode = new string(canonical, 0, code.Length);
        return SubdivisionIndex.FindSubdivisionByCode(canonicalCode);
    }

    internal static Subdivision GetRequiredByCode(string? code)
    {
        SubdivisionLookupResult result = FindCodeText(code);
        if (result.Status == LookupStatus.Found && result.Subdivision != null)
        {
            return result.Subdivision;
        }

        if (result.Status == LookupStatus.NotFound)
        {
            string notFoundInput = code == null ? string.Empty : code;
            throw new SubdivisionNotFoundException(notFoundInput);
        }

        throw new InvalidSubdivisionCodeException(code, ClassifyCodeFailure(code));
    }

    internal static SubdivisionLookupResult CreateSubdivisionResult(
        GeneratedSubdivisionRow? row)
    {
        if (row == null)
        {
            return CreateNotFoundResult();
        }

        Subdivision subdivision = SubdivisionMapper.MapSubdivision(row);
        return new SubdivisionLookupResult(
            LookupStatus.Found,
            subdivision,
            new Subdivision[0]);
    }

    internal static SubdivisionLookupResult CreateCandidateResult(
        GeneratedSubdivisionRow[] rows)
    {
        if (rows.Length == 0)
        {
            return CreateNotFoundResult();
        }

        if (rows.Length == 1)
        {
            return CreateSubdivisionResult(rows[0]);
        }

        Subdivision[] mapped = SubdivisionMapper.MapSubdivisions(rows);
        return new SubdivisionLookupResult(LookupStatus.Ambiguous, null, mapped);
    }

    internal static SubdivisionLookupResult CreateAliasResult(
        GeneratedSubdivisionAliasRow? row)
    {
        if (row == null)
        {
            return CreateNotFoundResult();
        }

        Subdivision subdivision = SubdivisionMapper.MapSubdivisionAlias(row);
        return new SubdivisionLookupResult(
            LookupStatus.Found,
            subdivision,
            new Subdivision[0]);
    }

    internal static SubdivisionLookupResult CreateInvalidResult()
    {
        return new SubdivisionLookupResult(
            LookupStatus.InvalidInput,
            null,
            new Subdivision[0]);
    }

    internal static SubdivisionLookupResult CreateNotFoundResult()
    {
        return new SubdivisionLookupResult(
            LookupStatus.NotFound,
            null,
            new Subdivision[0]);
    }

    internal static ValidationFailureKind ClassifyCodeFailure(string? value)
    {
        if (value == null || value.Length == 0)
        {
            return ValidationFailureKind.NullOrEmpty;
        }

        int minimumLength = CountryCodeConstants.SubdivisionCountryCodeLength +
                            1 +
                            CountryCodeConstants.SubdivisionMinimumSuffixLength;
        int maximumLength = CountryCodeConstants.SubdivisionCountryCodeLength +
                            1 +
                            CountryCodeConstants.SubdivisionMaximumSuffixLength;
        if (value.Length < minimumLength || value.Length > maximumLength)
        {
            return ValidationFailureKind.InvalidLength;
        }

        if (value[2] != CountryCodeConstants.SubdivisionSeparator)
        {
            return ValidationFailureKind.InvalidShape;
        }

        if (!AsciiCodeUtility.IsAsciiLetter(value[0]) ||
            !AsciiCodeUtility.IsAsciiLetter(value[1]))
        {
            return ValidationFailureKind.InvalidCharacter;
        }

        int index = 3;
        while (index < value.Length)
        {
            if (!AsciiCodeUtility.IsAsciiAlphanumeric(value[index]))
            {
                return ValidationFailureKind.InvalidCharacter;
            }

            index += 1;
        }

        return ValidationFailureKind.InvalidShape;
    }

    internal static bool IsLookupNameKind(GeneratedSubdivisionNameKind kind)
    {
        return kind == GeneratedSubdivisionNameKind.Canonical ||
               kind == GeneratedSubdivisionNameKind.Alias;
    }

    internal static void AddSubdivisionInOrder(
        List<GeneratedSubdivisionRow> rows,
        GeneratedSubdivisionRow value)
    {
        int index = 0;
        while (index < rows.Count)
        {
            int comparison =
                AsciiCodeUtility.CompareOrdinal(rows[index].Code, value.Code);
            if (comparison == 0)
            {
                return;
            }

            if (comparison > 0)
            {
                rows.Insert(index, value);
                return;
            }

            index += 1;
        }

        rows.Add(value);
    }

    internal static void InsertSubdivisionInCodeOrder(
        List<Subdivision> subdivisions,
        Subdivision value)
    {
        int index = 0;
        while (index < subdivisions.Count)
        {
            int comparison =
                AsciiCodeUtility.CompareOrdinal(subdivisions[index].Code, value.Code);
            if (comparison == 0)
            {
                return;
            }

            if (comparison > 0)
            {
                subdivisions.Insert(index, value);
                return;
            }

            index += 1;
        }

        subdivisions.Add(value);
    }
}
