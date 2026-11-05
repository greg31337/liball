using System.Xml;

namespace pptx;

/// <summary>
/// Represents app properties data that can be read from or written to a PowerPoint presentation.
/// </summary>
public class PptxAppProperties
{
    internal PptxPart Part;
    /// <summary>
    /// Stores the part name associated with this object.
    /// </summary>
    public string PartName;
    /// <summary>
    /// Stores the relationship id used to identify or connect related presentation data.
    /// </summary>
    public string RelationshipId;
    /// <summary>
    /// Stores the application associated with this object.
    /// </summary>
    public string Application;
    /// <summary>
    /// Stores the presentation format associated with this object.
    /// </summary>
    public string PresentationFormat;
    /// <summary>
    /// Stores the slides value associated with this object.
    /// </summary>
    public int Slides;
    /// <summary>
    /// Stores the notes value associated with this object.
    /// </summary>
    public int Notes;
    /// <summary>
    /// Stores the hidden slides value associated with this object.
    /// </summary>
    public int HiddenSlides;
    /// <summary>Stores the template name.</summary>
    public string Template;
    /// <summary>Stores the manager name.</summary>
    public string Manager;
    /// <summary>Stores the company name.</summary>
    public string Company;
    /// <summary>Stores the application version.</summary>
    public string AppVersion;
    /// <summary>Stores the total editing time in minutes.</summary>
    public int TotalTime;
    /// <summary>Stores the page count.</summary>
    public int Pages;
    /// <summary>Stores the word count.</summary>
    public int Words;
    /// <summary>Stores the character count.</summary>
    public int Characters;
    /// <summary>Stores the line count.</summary>
    public int Lines;
    /// <summary>Stores the paragraph count.</summary>
    public int Paragraphs;
    /// <summary>Stores the multimedia clip count.</summary>
    public int MultimediaClips;
    /// <summary>Stores the character count including spaces.</summary>
    public int CharactersWithSpaces;
    /// <summary>Stores the document security flags.</summary>
    public int DocumentSecurity;
    /// <summary>Indicates whether thumbnail scaling crops content.</summary>
    public bool ScaleCrop;
    /// <summary>Indicates whether document links are current.</summary>
    public bool LinksUpToDate;
    /// <summary>Indicates whether the document is shared.</summary>
    public bool SharedDocument;
    /// <summary>Stores the base URI used by hyperlinks.</summary>
    public string HyperlinkBase;
    /// <summary>Indicates whether hyperlinks were changed.</summary>
    public bool HyperlinksChanged;

    internal PptxAppProperties(PptxPart part, string relationshipId)
    {
        if (part == null)
        {
            throw new System.ArgumentNullException("part");
        }

        Part = part;
        PartName = part.PartName;
        RelationshipId = PptxDiagnostic.CleanText(relationshipId);
        Application = "";
        PresentationFormat = "";
        Slides = 0;
        Notes = 0;
        HiddenSlides = 0;
        Template = "";
        Manager = "";
        Company = "";
        AppVersion = "";
        TotalTime = 0;
        Pages = 0;
        Words = 0;
        Characters = 0;
        Lines = 0;
        Paragraphs = 0;
        MultimediaClips = 0;
        CharactersWithSpaces = 0;
        DocumentSecurity = 0;
        ScaleCrop = false;
        LinksUpToDate = false;
        SharedDocument = false;
        HyperlinkBase = "";
        HyperlinksChanged = false;
        Load();
    }

    internal void Load()
    {
        XmlDocument document = PptxXml.LoadBytes(GetCurrentBytes());
        XmlElement? root = document.DocumentElement;
        if (root == null)
        {
            return;
        }

        Application = PptxCoreProperties.GetChildText(root, PptxNamespaces.ExtendedProperties, "Application");
        PresentationFormat = PptxCoreProperties.GetChildText(root, PptxNamespaces.ExtendedProperties, "PresentationFormat");
        Slides = GetIntChild(root, "Slides", 0);
        Notes = GetIntChild(root, "Notes", 0);
        HiddenSlides = GetIntChild(root, "HiddenSlides", 0);
        Template = PptxCoreProperties.GetChildText(root, PptxNamespaces.ExtendedProperties, "Template");
        Manager = PptxCoreProperties.GetChildText(root, PptxNamespaces.ExtendedProperties, "Manager");
        Company = PptxCoreProperties.GetChildText(root, PptxNamespaces.ExtendedProperties, "Company");
        AppVersion = PptxCoreProperties.GetChildText(root, PptxNamespaces.ExtendedProperties, "AppVersion");
        TotalTime = GetIntChild(root, "TotalTime", 0);
        Pages = GetIntChild(root, "Pages", 0);
        Words = GetIntChild(root, "Words", 0);
        Characters = GetIntChild(root, "Characters", 0);
        Lines = GetIntChild(root, "Lines", 0);
        Paragraphs = GetIntChild(root, "Paragraphs", 0);
        MultimediaClips = GetIntChild(root, "MMClips", 0);
        CharactersWithSpaces = GetIntChild(root, "CharactersWithSpaces", 0);
        DocumentSecurity = GetIntChild(root, "DocSecurity", 0);
        ScaleCrop = GetBoolChild(root, "ScaleCrop", false);
        LinksUpToDate = GetBoolChild(root, "LinksUpToDate", false);
        SharedDocument = GetBoolChild(root, "SharedDoc", false);
        HyperlinkBase = PptxCoreProperties.GetChildText(root, PptxNamespaces.ExtendedProperties, "HyperlinkBase");
        HyperlinksChanged = GetBoolChild(root, "HyperlinksChanged", false);
    }
    internal void Save()
    {
        XmlDocument document = PptxXml.LoadBytes(GetCurrentBytes());
        XmlElement? rootValue = document.DocumentElement;
        if (rootValue == null)
        {
            throw new System.ArgumentException("Application properties XML root is required.");
        }

        XmlElement root = rootValue;
        SetTextChild(document, root, "Application", Application);
        SetTextChild(document, root, "PresentationFormat", PresentationFormat);
        SetTextChild(document, root, "Slides", Slides.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SetTextChild(document, root, "Notes", Notes.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SetTextChild(document, root, "HiddenSlides", HiddenSlides.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SetTextChild(document, root, "Template", Template);
        SetTextChild(document, root, "Manager", Manager);
        SetTextChild(document, root, "Company", Company);
        SetTextChild(document, root, "AppVersion", AppVersion);
        SetTextChild(document, root, "TotalTime", TotalTime.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SetTextChild(document, root, "Pages", Pages.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SetTextChild(document, root, "Words", Words.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SetTextChild(document, root, "Characters", Characters.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SetTextChild(document, root, "Lines", Lines.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SetTextChild(document, root, "Paragraphs", Paragraphs.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SetTextChild(document, root, "MMClips", MultimediaClips.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SetTextChild(document, root, "CharactersWithSpaces", CharactersWithSpaces.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SetTextChild(document, root, "DocSecurity", DocumentSecurity.ToString(System.Globalization.CultureInfo.InvariantCulture));
        SetTextChild(document, root, "ScaleCrop", BoolText(ScaleCrop));
        SetTextChild(document, root, "LinksUpToDate", BoolText(LinksUpToDate));
        SetTextChild(document, root, "SharedDoc", BoolText(SharedDocument));
        SetTextChild(document, root, "HyperlinkBase", HyperlinkBase);
        SetTextChild(document, root, "HyperlinksChanged", BoolText(HyperlinksChanged));
        Part.SetXmlDocument(document);
    }

    internal byte[] GetCurrentBytes()
    {
        if (Part.HasXmlDocument)
        {
            return PptxXml.WriteBytes(Part.XmlDocument);
        }

        return Part.GetBytes();
    }

    internal static int GetIntChild(XmlElement root, string localName, int fallback)
    {
        XmlElement? child = PptxXml.FindFirstChild(root, PptxNamespaces.ExtendedProperties, localName);
        if (child == null)
        {
            return fallback;
        }

        int parsed = 0;
        if (int.TryParse(child.InnerText, out parsed))
        {
            return parsed;
        }

        return fallback;
    }

    internal static void AppendTextChild(XmlDocument document, XmlElement root, string localName, string value)
    {
        XmlElement child = PptxXml.CreateElement(document, "", localName, PptxNamespaces.ExtendedProperties);
        child.InnerText = PptxDiagnostic.CleanText(value);
        root.AppendChild(child);
    }

    internal static bool GetBoolChild(XmlElement root, string localName, bool fallback)
    {
        string value = PptxCoreProperties.GetChildText(root, PptxNamespaces.ExtendedProperties, localName);
        if (value == "true" || value == "1")
        {
            return true;
        }

        if (value == "false" || value == "0")
        {
            return false;
        }

        return fallback;
    }

    internal static void SetTextChild(XmlDocument document, XmlElement root, string localName, string value)
    {
        XmlElement? child = PptxXml.FindFirstChild(root, PptxNamespaces.ExtendedProperties, localName);
        if (child == null)
        {
            child = PptxXml.CreateElement(document, "", localName, PptxNamespaces.ExtendedProperties);
            root.AppendChild(child);
        }

        child.InnerText = PptxDiagnostic.CleanText(value);
    }

    internal static string BoolText(bool value)
    {
        if (value)
        {
            return "true";
        }

        return "false";
    }
}
