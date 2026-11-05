using System.Collections.Generic;
using System.Xml;

namespace pptx;

/// <summary>
/// Represents hyperlink data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxHyperlink
{
    internal PptxShape? Shape;
    internal PptxRun? Run;
    internal XmlElement? Element;
    internal PptxRelationship? Relationship;
    /// <summary>
    /// Stores the relationship id used to identify or connect related presentation data.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the relationship type associated with this object.
    /// </summary>
    public string RelationshipType;
    /// <summary>
    /// Stores the target associated with this object.
    /// </summary>
    public string Target;
    /// <summary>
    /// Stores the target part name associated with this object.
    /// </summary>
    public string TargetPartName;
    /// <summary>
    /// Stores the action associated with this object.
    /// </summary>
    public string Action;
    /// <summary>
    /// Stores the type associated with this object.
    /// </summary>
    public string Type;
    /// <summary>
    /// Stores the slide index value associated with this object.
    /// </summary>
    public int SlideIndex;
    /// <summary>
    /// Stores the custom show name associated with this object.
    /// </summary>
    public string CustomShowName;
    /// <summary>
    /// Stores <c>shape</c> or <c>run</c> to identify the hyperlink owner.
    /// </summary>
    public string OwnerType;
    /// <summary>
    /// Stores the owning shape identifier.
    /// </summary>
    public int ShapeId;
    /// <summary>
    /// Stores the owning run text, or an empty string for shape-level hyperlinks.
    /// </summary>
    public string RunText;
    /// <summary>
    /// Stores the macro name parsed from a preserved macro action.
    /// </summary>
    public string MacroName;

    internal PptxHyperlink(PptxShape? shape, PptxRun? run)
    {
        if (shape == null && run == null)
        {
            throw new System.ArgumentException("A hyperlink owner is required.");
        }

        if (shape != null && run != null)
        {
            throw new System.ArgumentException("A hyperlink can have only one owner.");
        }

        Shape = shape;
        Run = run;
        Element = null;
        Relationship = null;
        RelationshipId = "";
        RelationshipType = "";
        Target = "";
        TargetPartName = "";
        Action = "";
        Type = "";
        SlideIndex = -1;
        CustomShowName = "";
        OwnerType = "shape";
        ShapeId = 0;
        RunText = "";
        MacroName = "";
        if (Run != null)
        {
            OwnerType = "run";
            RunText = Run.Text;
            ShapeId = Run.Paragraph.TextFrame.Shape.Id;
        }
        else if (Shape != null)
        {
            ShapeId = Shape.Id;
        }

        Load();
    }

    internal void Load()
    {
        Element = GetHyperlinkElement();
        Relationship = null;
        RelationshipId = "";
        RelationshipType = "";
        Target = "";
        TargetPartName = "";
        Action = "";
        Type = "";
        SlideIndex = -1;
        CustomShowName = "";
        MacroName = "";
        if (Element == null)
        {
            return;
        }

        RelationshipId = PptxXml.GetAttributeValue(Element, PptxNamespaces.Relationships, "id");
        Action = PptxXml.GetAttributeValue(Element, "", "action");
        if (RelationshipId.Length > 0)
        {
            Relationship = GetShapeTree().FindRelationshipById(RelationshipId);
            if (Relationship != null)
            {
                RelationshipType = Relationship.Type;
                Target = Relationship.Target;
                TargetPartName = Relationship.TargetPartName;
            }
        }

        Type = Classify();
    }
    internal bool HasLink()
    {
        return Element != null;
    }

    internal string Classify()
    {
        if (Relationship != null && Relationship.Type == PptxRelationshipTypes.Slide)
        {
            SlideIndex = FindSlideIndex(Relationship.TargetPartName);
            return "slide";
        }

        string actionType = ClassifyAction(Action);
        if (actionType.Length > 0)
        {
            return actionType;
        }

        if (Relationship != null && Relationship.Type == PptxRelationshipTypes.Hyperlink)
        {
            if (StartsWithText(Relationship.Target, "file:") || StartsWithText(Relationship.Target, "\\\\") || StartsWithText(Relationship.Target, "//"))
            {
                return "file";
            }

            return "url";
        }

        if (Relationship != null)
        {
            return "relationship";
        }

        return "";
    }

    internal string ClassifyAction(string action)
    {
        string clean = PptxDiagnostic.CleanText(action);
        if (clean.Length == 0)
        {
            return "";
        }

        if (ContainsText(clean, "jump=nextslide"))
        {
            return "next-slide";
        }

        if (ContainsText(clean, "jump=previousslide"))
        {
            return "previous-slide";
        }

        if (ContainsText(clean, "jump=firstslide"))
        {
            return "first-slide";
        }

        if (ContainsText(clean, "jump=lastslide"))
        {
            return "last-slide";
        }

        if (StartsWithText(clean, "ppaction://customshow?id="))
        {
            CustomShowName = clean.Substring("ppaction://customshow?id=".Length);
            return "custom-show";
        }

        if (StartsWithText(clean, "ppaction://macro?name="))
        {
            MacroName = clean.Substring("ppaction://macro?name=".Length);
            return "macro";
        }

        if (StartsWithText(clean, "ppaction://macro?id="))
        {
            MacroName = clean.Substring("ppaction://macro?id=".Length);
            return "macro";
        }

        if (StartsWithText(clean, "ppaction://program"))
        {
            return "program";
        }

        return "action";
    }

    internal int FindSlideIndex(string partName)
    {
        PptxShapeTree tree = GetShapeTree();
        if (tree.Slide.Presentation == null)
        {
            return -1;
        }

        string normalized = PptxUriCore.NormalizePartName(partName);
        for (int index = 0; index < tree.Slide.Presentation.SlideCount(); index++)
        {
            if (tree.Slide.Presentation.GetSlide(index).PartName == normalized)
            {
                return index;
            }
        }

        return -1;
    }

    internal XmlElement? GetHyperlinkElement()
    {
        if (Shape != null)
        {
            return Shape.GetHyperlinkClickElement();
        }

        if (Run != null)
        {
            return Run.GetHyperlinkClickElement();
        }

        return null;
    }

    internal PptxShapeTree GetShapeTree()
    {
        if (Shape != null)
        {
            return Shape.Tree;
        }

        if (Run != null)
        {
            return Run.Paragraph.TextFrame.Shape.Tree;
        }

        throw new System.ArgumentException("Hyperlink owner was not found.");
    }

    internal void SetUrl(string url)
    {
        if (Shape != null)
        {
            Shape.SetExternalHyperlink(url);
        }
        else if (Run != null)
        {
            PptxRunFormatCore.SetExternalHyperlink(Run, url);
        }

        Load();
    }

    internal void SetFile(string fileUri)
    {
        if (Shape != null)
        {
            Shape.SetFileHyperlink(fileUri);
        }
        else if (Run != null)
        {
            PptxRunFormatCore.SetFileHyperlink(Run, fileUri);
        }

        Load();
    }

    internal void SetSlide(int slideIndex)
    {
        if (Shape != null)
        {
            Shape.SetSlideHyperlink(slideIndex);
        }
        else if (Run != null)
        {
            PptxRunFormatCore.SetSlideHyperlink(Run, slideIndex);
        }

        Load();
    }

    internal void SetNavigation(string navigation)
    {
        string action = NavigationAction(navigation);
        SetAction(action);
    }

    internal void SetCustomShow(string customShowName)
    {
        string clean = PptxDiagnostic.CleanText(customShowName);
        if (clean.Length == 0)
        {
            throw new System.ArgumentException("Custom show name is required.");
        }

        SetAction("ppaction://customshow?id=" + clean);
    }

    internal void SetAction(string action)
    {
        if (Shape != null)
        {
            Shape.SetAction(action);
        }
        else if (Run != null)
        {
            PptxRunFormatCore.SetAction(Run, action);
        }

        Load();
    }

    internal void Remove()
    {
        if (Shape != null)
        {
            Shape.RemoveHyperlink();
        }
        else if (Run != null)
        {
            PptxRunFormatCore.RemoveHyperlink(Run);
        }

        Load();
    }

    internal PptxResult AuditSafety()
    {
        PptxResult result = new PptxResult();
        string partName = GetShapeTree().Slide.PartName;
        if (!HasLink())
        {
            return result;
        }

        if (Type == "file")
        {
            result.AddWarning("HLK002", "File hyperlink can open a local or network resource: " + Target, partName, RelationshipId, OwnerType);
        }
        else if (Type == "macro")
        {
            result.AddWarning("HLK004", "Preserved macro action requires explicit trust: " + Action, partName, RelationshipId, OwnerType);
        }
        else if (Type == "program")
        {
            result.AddWarning("HLK005", "Program action can launch external code: " + Action, partName, RelationshipId, OwnerType);
        }
        else if (Type == "action")
        {
            result.AddWarning("HLK006", "Unrecognized action was preserved: " + Action, partName, RelationshipId, OwnerType);
        }
        else if (Type == "url" && IsUnsafeExternalTarget(Target))
        {
            result.AddWarning("HLK003", "External hyperlink uses a potentially unsafe scheme: " + Target, partName, RelationshipId, OwnerType);
        }

        if (RelationshipId.Length > 0 && Relationship == null)
        {
            result.AddWarning("HLK007", "Hyperlink relationship could not be resolved.", partName, RelationshipId, OwnerType);
        }

        return result;
    }

    internal static PptxResult AuditAll(List<PptxHyperlink> hyperlinks)
    {
        if (hyperlinks == null)
        {
            throw new System.ArgumentNullException("hyperlinks");
        }

        PptxResult result = new PptxResult();
        for (int hyperlinkIndex = 0; hyperlinkIndex < hyperlinks.Count; hyperlinkIndex++)
        {
            PptxResult hyperlinkResult = hyperlinks[hyperlinkIndex].AuditSafety();
            for (int diagnosticIndex = 0; diagnosticIndex < hyperlinkResult.Diagnostics.Count; diagnosticIndex++)
            {
                result.Add(hyperlinkResult.Diagnostics[diagnosticIndex]);
            }
        }

        return result;
    }

    internal static string NavigationAction(string navigation)
    {
        string clean = PptxDiagnostic.CleanText(navigation);
        if (clean == "next-slide")
        {
            return "ppaction://hlinkshowjump?jump=nextslide";
        }

        if (clean == "previous-slide")
        {
            return "ppaction://hlinkshowjump?jump=previousslide";
        }

        if (clean == "first-slide")
        {
            return "ppaction://hlinkshowjump?jump=firstslide";
        }

        if (clean == "last-slide")
        {
            return "ppaction://hlinkshowjump?jump=lastslide";
        }

        throw new System.ArgumentException("Navigation must be next-slide, previous-slide, first-slide, or last-slide.");
    }

    internal static bool IsUnsafeExternalTarget(string target)
    {
        return StartsWithText(target, "javascript:") || StartsWithText(target, "vbscript:") || StartsWithText(target, "data:") || StartsWithText(target, "ms-msdt:");
    }

    internal static bool ContainsText(string text, string value)
    {
        if (text.IndexOf(value, System.StringComparison.OrdinalIgnoreCase) >= 0)
        {
            return true;
        }

        return false;
    }

    internal static bool StartsWithText(string text, string value)
    {
        return text.StartsWith(value, System.StringComparison.OrdinalIgnoreCase);
    }
}
