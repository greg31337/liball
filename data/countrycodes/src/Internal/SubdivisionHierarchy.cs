using System.Collections.Generic;

namespace countrycodes;

internal static class SubdivisionHierarchy
{
    internal static GeneratedSubdivisionRow? FindParentRow(
        GeneratedSubdivisionRow subdivision,
        GeneratedSubdivisionRow[] rows)
    {
        if (!subdivision.HasParent)
        {
            return null;
        }

        return FindRowInArray(rows, subdivision.ParentCode);
    }

    internal static GeneratedSubdivisionRow[] GetChildRows(
        GeneratedSubdivisionRow subdivision,
        GeneratedSubdivisionRow[] rows)
    {
        List<GeneratedSubdivisionRow> children = new List<GeneratedSubdivisionRow>();
        int index = 0;
        while (index < rows.Length)
        {
            GeneratedSubdivisionRow row = rows[index];
            if (row.HasParent &&
                AsciiCodeUtility.EqualsOrdinal(row.ParentCode, subdivision.Code))
            {
                InsertRowInCodeOrder(children, row);
            }

            index += 1;
        }

        return children.ToArray();
    }

    internal static GeneratedSubdivisionRow[] GetAncestorRows(
        GeneratedSubdivisionRow subdivision,
        GeneratedSubdivisionRow[] rows)
    {
        List<GeneratedSubdivisionRow> ancestors = new List<GeneratedSubdivisionRow>();
        List<string> visitedCodes = new List<string>();
        visitedCodes.Add(subdivision.Code);
        GeneratedSubdivisionRow current = subdivision;
        while (current.HasParent)
        {
            if (ContainsCode(visitedCodes, current.ParentCode))
            {
                break;
            }

            GeneratedSubdivisionRow? parent = FindRowInArray(rows, current.ParentCode);
            if (parent == null)
            {
                throw new SubdivisionNotFoundException(current.ParentCode);
            }

            ancestors.Add(parent);
            visitedCodes.Add(parent.Code);
            current = parent;
        }

        return ancestors.ToArray();
    }

    internal static GeneratedSubdivisionRow[] GetDescendantRows(
        GeneratedSubdivisionRow subdivision,
        GeneratedSubdivisionRow[] rows)
    {
        List<GeneratedSubdivisionRow> descendants = new List<GeneratedSubdivisionRow>();
        List<string> visitedCodes = new List<string>();
        List<string> pendingCodes = new List<string>();
        visitedCodes.Add(subdivision.Code);
        pendingCodes.Add(subdivision.Code);
        int pendingIndex = 0;
        while (pendingIndex < pendingCodes.Count)
        {
            string parentCode = pendingCodes[pendingIndex];
            int rowIndex = 0;
            while (rowIndex < rows.Length)
            {
                GeneratedSubdivisionRow row = rows[rowIndex];
                if (row.HasParent &&
                    AsciiCodeUtility.EqualsOrdinal(row.ParentCode, parentCode) &&
                    !ContainsCode(visitedCodes, row.Code))
                {
                    visitedCodes.Add(row.Code);
                    pendingCodes.Add(row.Code);
                    InsertRowInCodeOrder(descendants, row);
                }

                rowIndex += 1;
            }

            pendingIndex += 1;
        }

        return descendants.ToArray();
    }

    internal static GeneratedSubdivisionRow? FindRowInArray(
        GeneratedSubdivisionRow[] rows,
        string code)
    {
        int index = 0;
        while (index < rows.Length)
        {
            if (AsciiCodeUtility.EqualsOrdinal(rows[index].Code, code))
            {
                return rows[index];
            }

            index += 1;
        }

        return null;
    }

    internal static bool ContainsCode(List<string> codes, string code)
    {
        int index = 0;
        while (index < codes.Count)
        {
            if (AsciiCodeUtility.EqualsOrdinal(codes[index], code))
            {
                return true;
            }

            index += 1;
        }

        return false;
    }

    internal static void InsertRowInCodeOrder(
        List<GeneratedSubdivisionRow> rows,
        GeneratedSubdivisionRow value)
    {
        int index = 0;
        while (index < rows.Count)
        {
            int comparison = AsciiCodeUtility.CompareOrdinal(rows[index].Code, value.Code);
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
}
