using System.Collections.Generic;

namespace odf;

internal static class TextListNavigator
{
    internal static OdfTextList? FindListByXmlId(
        OdfDocument document,
        string xmlId)
    {
        RequireDocument(document);
        OdfTextList? result = null;
        int count = 0;
        int index = 0;
        while (index < document.TextLists.Length)
        {
            OdfTextList list = document.TextLists[index];
            if (list.XmlId == xmlId)
            {
                result = list;
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    internal static OdfTextList[] FollowContinuation(
        OdfTextList list)
    {
        if (list == null)
        {
            throw new OdfDocumentException(
                "A text list is required for continuation navigation.");
        }
        if (list.HasBrokenContinuation || list.HasContinuationCycle)
        {
            return [];
        }
        List<OdfTextList> reversed = new List<OdfTextList>();
        OdfTextList? current = list;
        while (current != null)
        {
            if (current.HasBrokenContinuation
                || current.HasContinuationCycle
                || reversed.Contains(current))
            {
                return [];
            }
            reversed.Add(current);
            current = current.ContinuedList;
        }
        OdfTextList[] result = new OdfTextList[reversed.Count];
        int index = 0;
        while (index < reversed.Count)
        {
            result[index] = reversed[reversed.Count - index - 1];
            index = index + 1;
        }
        return result;
    }

    internal static void RenameListXmlId(
        OdfDocument document,
        OdfTextList list,
        string newXmlId)
    {
        RequireDocument(document);
        if (list == null || newXmlId == null || newXmlId.Length == 0)
        {
            throw new OdfEditException(
                "A text list and non-empty XML ID are required.");
        }
        if (!ContainsList(document.TextLists, list))
        {
            throw new OdfEditException(
                "The text list does not belong to the document.");
        }
        if (HasIdConflict(document.TextLists, list, newXmlId))
        {
            throw new OdfEditException(
                "A text list with the requested XML ID already exists.");
        }
        string oldXmlId = list.XmlId;
        list.XmlId = newXmlId;
        int index = 0;
        while (index < document.TextLists.Length)
        {
            OdfTextList candidate = document.TextLists[index];
            if (candidate.ContinueListId == oldXmlId)
            {
                candidate.ContinueListId = newXmlId;
            }
            index = index + 1;
        }
        TextListDocumentModel.ResolveListRelationships(document);
    }

    static bool ContainsList(OdfTextList[] lists, OdfTextList target)
    {
        int index = 0;
        while (index < lists.Length)
        {
            if (lists[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool HasIdConflict(
        OdfTextList[] lists,
        OdfTextList target,
        string xmlId)
    {
        int index = 0;
        while (index < lists.Length)
        {
            OdfTextList list = lists[index];
            if (list != target && list.XmlId == xmlId)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static void RequireDocument(OdfDocument document)
    {
        if (document == null)
        {
            throw new OdfDocumentException(
                "A document is required for list navigation.");
        }
    }
}
