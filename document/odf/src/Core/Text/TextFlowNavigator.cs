using System.Collections.Generic;

namespace odf;

internal static class TextFlowNavigator
{
    internal static OdfTextSection? FindSection(
        OdfDocument document,
        string name)
    {
        RequireDocument(document);
        OdfTextSection? result = null;
        int count = 0;
        int index = 0;
        while (index < document.TextSections.Length)
        {
            OdfTextSection section = document.TextSections[index];
            if (section.Name == name)
            {
                result = section;
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    internal static OdfTextFrame? FindFrame(
        OdfDocument document,
        string name)
    {
        RequireDocument(document);
        OdfTextFrame? result = null;
        int count = 0;
        int index = 0;
        while (index < document.TextFrames.Length)
        {
            OdfTextFrame frame = document.TextFrames[index];
            if (frame.Name == name)
            {
                result = frame;
                count = count + 1;
            }
            index = index + 1;
        }
        return count == 1 ? result : null;
    }

    internal static OdfTextFrame[] FollowFrameChain(OdfTextFrame frame)
    {
        if (frame == null)
        {
            throw new OdfDocumentException(
                "A text frame is required for chain navigation.");
        }
        if (frame.HasBrokenChain || frame.HasChainCycle)
        {
            return [];
        }
        List<OdfTextFrame> result = new List<OdfTextFrame>();
        OdfTextFrame? current = frame;
        while (current != null)
        {
            if (current.HasBrokenChain
                || current.HasChainCycle
                || result.Contains(current))
            {
                return [];
            }
            result.Add(current);
            current = current.NextFrame;
        }
        return result.ToArray();
    }

    internal static void RenameFrame(
        OdfDocument document,
        OdfTextFrame frame,
        string newName)
    {
        RequireDocument(document);
        if (frame == null || newName == null || newName.Length == 0)
        {
            throw new OdfEditException(
                "A text frame and non-empty new name are required.");
        }
        if (!ContainsFrame(document.TextFrames, frame))
        {
            throw new OdfEditException(
                "The text frame does not belong to the document.");
        }
        if (HasFrameNameConflict(document.TextFrames, frame, newName))
        {
            throw new OdfEditException(
                "A text frame with the requested name already exists.");
        }
        string oldName = frame.Name;
        frame.Name = newName;
        int index = 0;
        while (index < document.TextFrames.Length)
        {
            OdfTextFrame candidate = document.TextFrames[index];
            if (candidate.ChainNextName == oldName)
            {
                candidate.ChainNextName = newName;
            }
            index = index + 1;
        }
        TextFlowDocumentModel.ResolveFrameChains(document.TextFrames);
    }

    internal static void RenameSection(
        OdfDocument document,
        OdfTextSection section,
        string newName)
    {
        RequireDocument(document);
        if (section == null || newName == null || newName.Length == 0)
        {
            throw new OdfEditException(
                "A text section and non-empty new name are required.");
        }
        if (!ContainsSection(document.TextSections, section))
        {
            throw new OdfEditException(
                "The text section does not belong to the document.");
        }
        if (HasSectionNameConflict(
            document.TextSections,
            section,
            newName))
        {
            throw new OdfEditException(
                "A text section with the requested name already exists.");
        }
        string oldName = section.Name;
        section.Name = newName;
        int index = 0;
        while (index < document.TextSections.Length)
        {
            OdfTextSection candidate = document.TextSections[index];
            if (candidate.SourceHref.Length == 0
                && candidate.SourceSectionName == oldName)
            {
                candidate.SourceSectionName = newName;
            }
            index = index + 1;
        }
    }

    static bool ContainsFrame(
        OdfTextFrame[] frames,
        OdfTextFrame target)
    {
        int index = 0;
        while (index < frames.Length)
        {
            if (frames[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool HasFrameNameConflict(
        OdfTextFrame[] frames,
        OdfTextFrame target,
        string name)
    {
        int index = 0;
        while (index < frames.Length)
        {
            OdfTextFrame frame = frames[index];
            if (frame != target && frame.Name == name)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool ContainsSection(
        OdfTextSection[] sections,
        OdfTextSection target)
    {
        int index = 0;
        while (index < sections.Length)
        {
            if (sections[index] == target)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }

    static bool HasSectionNameConflict(
        OdfTextSection[] sections,
        OdfTextSection target,
        string name)
    {
        int index = 0;
        while (index < sections.Length)
        {
            OdfTextSection section = sections[index];
            if (section != target && section.Name == name)
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
                "A document is required for text-flow navigation.");
        }
    }
}
