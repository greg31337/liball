namespace docx;

/// <summary>
/// Represents settings data used by a DOCX document.
/// </summary>
public class DocxSettings
{
    internal string Xml = "";
    /// <summary>
    /// The document view mode, or an empty string when unspecified.
    /// </summary>
    public string View = "";
    /// <summary>
    /// The zoom view mode, or an empty string when unspecified.
    /// </summary>
    public string ZoomView = "";
    /// <summary>
    /// The zoom percent value stored in the document model.
    /// </summary>
    public string ZoomPercent = "";
    /// <summary>
    /// The default tab stop value stored in the document model.
    /// </summary>
    public string DefaultTabStop = "";
    /// <summary>
    /// The hyphenation zone value stored in the document model.
    /// </summary>
    public string HyphenationZone = "";
    /// <summary>
    /// The consecutive hyphen limit, or an empty string when unspecified.
    /// </summary>
    public string ConsecutiveHyphenLimit = "";
    /// <summary>
    /// Whether capitalization hyphenation was explicitly supplied.
    /// </summary>
    public bool HasDoNotHyphenateCaps;
    /// <summary>
    /// Whether capitalized words are excluded from hyphenation.
    /// </summary>
    public bool DoNotHyphenateCaps;
    /// <summary>
    /// The spelling proof state, or an empty string when unspecified.
    /// </summary>
    public string ProofStateSpelling = "";
    /// <summary>
    /// The grammar proof state, or an empty string when unspecified.
    /// </summary>
    public string ProofStateGrammar = "";
    /// <summary>
    /// The decimal symbol value stored in the document model.
    /// </summary>
    public string DecimalSymbol = "";
    /// <summary>
    /// The list separator value stored in the document model.
    /// </summary>
    public string ListSeparator = "";
    /// <summary>
    /// Whether even and odd headers was explicitly supplied.
    /// </summary>
    public bool HasEvenAndOddHeaders;
    /// <summary>
    /// Whether the even and odd headers behavior is enabled.
    /// </summary>
    public bool EvenAndOddHeaders;
    /// <summary>
    /// Whether update fields was explicitly supplied.
    /// </summary>
    public bool HasUpdateFields;
    /// <summary>
    /// Whether the update fields behavior is enabled.
    /// </summary>
    public bool UpdateFields;
    /// <summary>
    /// Whether track revisions was explicitly supplied.
    /// </summary>
    public bool HasTrackRevisions;
    /// <summary>
    /// Whether the track revisions behavior is enabled.
    /// </summary>
    public bool TrackRevisions;
    /// <summary>
    /// Whether do not track moves was explicitly supplied.
    /// </summary>
    public bool HasDoNotTrackMoves;
    /// <summary>
    /// Whether the do not track moves behavior is enabled.
    /// </summary>
    public bool DoNotTrackMoves;
    /// <summary>
    /// Whether do not track formatting was explicitly supplied.
    /// </summary>
    public bool HasDoNotTrackFormatting;
    /// <summary>
    /// Whether the do not track formatting behavior is enabled.
    /// </summary>
    public bool DoNotTrackFormatting;
    /// <summary>
    /// Whether auto hyphenation was explicitly supplied.
    /// </summary>
    public bool HasAutoHyphenation;
    /// <summary>
    /// Whether the auto hyphenation behavior is enabled.
    /// </summary>
    public bool AutoHyphenation;
    /// <summary>
    /// The document protection edit mode, or an empty string when protection is absent.
    /// </summary>
    public string ProtectionEdit = "";
    /// <summary>
    /// Whether document protection enforcement was explicitly supplied.
    /// </summary>
    public bool HasProtectionEnforcement;
    /// <summary>
    /// Whether document protection is enforced.
    /// </summary>
    public bool ProtectionEnforcement;
    /// <summary>
    /// The default theme font language, or an empty string when unspecified.
    /// </summary>
    public string ThemeFontLanguage = "";
    /// <summary>
    /// The East Asian theme font language, or an empty string when unspecified.
    /// </summary>
    public string ThemeFontEastAsiaLanguage = "";
    /// <summary>
    /// The bidirectional theme font language, or an empty string when unspecified.
    /// </summary>
    public string ThemeFontBidiLanguage = "";
    /// <summary>
    /// Whether compatibility settings was explicitly supplied.
    /// </summary>
    public bool HasCompatibilitySettings;
    /// <summary>
    /// The compatibility settings value stored in the document model.
    /// </summary>
    public DocxCompatibilitySettings CompatibilitySettings = DocxCompatibilitySettings.Create();
    internal string ProtectionXml = "";
    internal List<DocxXmlBuffer> UnknownChildren = new List<DocxXmlBuffer>();

    /// <summary>
    /// Creates a settings instance.
    /// </summary>
    /// <param name="xml">The xml used by the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is null.</exception>
    public DocxSettings(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        this.Xml = xml;
        Initialize();
        LoadFromXml();
    }

    internal static DocxSettings Create()
    {
        return new DocxSettings("");
    }

    internal void Initialize()
    {
        this.View = "";
        this.ZoomView = "";
        this.ZoomPercent = "";
        this.DefaultTabStop = "";
        this.HyphenationZone = "";
        this.ConsecutiveHyphenLimit = "";
        this.HasDoNotHyphenateCaps = false;
        this.DoNotHyphenateCaps = false;
        this.ProofStateSpelling = "";
        this.ProofStateGrammar = "";
        this.DecimalSymbol = "";
        this.ListSeparator = "";
        this.HasEvenAndOddHeaders = false;
        this.EvenAndOddHeaders = false;
        this.HasUpdateFields = false;
        this.UpdateFields = false;
        this.HasTrackRevisions = false;
        this.TrackRevisions = false;
        this.HasDoNotTrackMoves = false;
        this.DoNotTrackMoves = false;
        this.HasDoNotTrackFormatting = false;
        this.DoNotTrackFormatting = false;
        this.HasAutoHyphenation = false;
        this.AutoHyphenation = false;
        this.ProtectionEdit = "";
        this.HasProtectionEnforcement = false;
        this.ProtectionEnforcement = false;
        this.ThemeFontLanguage = "";
        this.ThemeFontEastAsiaLanguage = "";
        this.ThemeFontBidiLanguage = "";
        this.HasCompatibilitySettings = false;
        this.CompatibilitySettings = DocxCompatibilitySettings.Create();
        this.ProtectionXml = "";
        this.UnknownChildren = new List<DocxXmlBuffer>();
    }

    internal static bool IsSupported(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        return reader.IsElement("settings", DocxConstants.NamespaceWordprocessingMl);
    }

    internal static DocxSettings Parse(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (!IsSupported(reader))
        {
            throw new InvalidOperationException("Reader is not positioned on w:settings.");
        }

        DocxXmlBuffer buffer = DocxXmlBuffer.Capture(reader);
        return new DocxSettings(buffer.GetXml());
    }

    internal static DocxSettings ParseXml(string xml)
    {
        if (xml == null)
        {
            throw new ArgumentNullException("xml");
        }

        DocxXmlReader reader = DocxXmlReader.Create(xml);
        reader.MoveToContent();
        return Parse(reader);
    }

    internal string GetXml()
    {
        return this.Xml;
    }

    internal bool HasXml()
    {
        return this.Xml != "";
    }

    internal void LoadFromXml()
    {
        Initialize();

        if (this.Xml == "")
        {
            return;
        }

        DocxXmlReader reader = DocxXmlReader.Create(this.Xml);

        if (!reader.ReadToElement("settings", DocxConstants.NamespaceWordprocessingMl))
        {
            return;
        }

        if (reader.Reader.IsEmptyElement)
        {
            return;
        }

        int settingsDepth = reader.Depth();
        bool hasNode = reader.Read();

        while (hasNode)
        {
            if (reader.Reader.NodeType == System.Xml.XmlNodeType.EndElement && reader.Depth() == settingsDepth)
            {
                return;
            }

            if (reader.Reader.NodeType == System.Xml.XmlNodeType.Element && reader.Depth() == settingsDepth + 1)
            {
                if (ReadKnownChild(reader))
                {
                    hasNode = !reader.Reader.EOF;
                    continue;
                }

                DocxXmlBuffer child = DocxXmlBuffer.Capture(reader);
                this.UnknownChildren.Add(child);
                hasNode = !reader.Reader.EOF;
                continue;
            }

            hasNode = reader.Read();
        }
    }

    internal bool ReadKnownChild(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        if (reader.NamespaceUri() != DocxConstants.NamespaceWordprocessingMl)
        {
            return false;
        }

        if (reader.LocalName() == "zoom")
        {
            this.ZoomView = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
            this.ZoomPercent = reader.GetAttribute("percent", DocxConstants.NamespaceWordprocessingMl);
            reader.ReadPastCurrentElement();
            return true;
        }

        if (reader.LocalName() == "view")
        {
            this.View = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
            reader.ReadPastCurrentElement();
            return true;
        }

        if (reader.LocalName() == "defaultTabStop")
        {
            this.DefaultTabStop = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
            reader.ReadPastCurrentElement();
            return true;
        }

        if (reader.LocalName() == "hyphenationZone")
        {
            this.HyphenationZone = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
            reader.ReadPastCurrentElement();
            return true;
        }

        if (reader.LocalName() == "consecutiveHyphenLimit")
        {
            this.ConsecutiveHyphenLimit = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
            reader.ReadPastCurrentElement();
            return true;
        }

        if (reader.LocalName() == "doNotHyphenateCaps")
        {
            this.HasDoNotHyphenateCaps = true;
            this.DoNotHyphenateCaps = ReadToggleValue(reader);
            reader.ReadPastCurrentElement();
            return true;
        }

        if (reader.LocalName() == "proofState")
        {
            this.ProofStateSpelling = reader.GetAttribute("spelling", DocxConstants.NamespaceWordprocessingMl);
            this.ProofStateGrammar = reader.GetAttribute("grammar", DocxConstants.NamespaceWordprocessingMl);
            reader.ReadPastCurrentElement();
            return true;
        }

        if (reader.LocalName() == "decimalSymbol")
        {
            this.DecimalSymbol = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
            reader.ReadPastCurrentElement();
            return true;
        }

        if (reader.LocalName() == "listSeparator")
        {
            this.ListSeparator = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
            reader.ReadPastCurrentElement();
            return true;
        }

        if (reader.LocalName() == "evenAndOddHeaders")
        {
            this.HasEvenAndOddHeaders = true;
            this.EvenAndOddHeaders = ReadToggleValue(reader);
            reader.ReadPastCurrentElement();
            return true;
        }

        if (reader.LocalName() == "updateFields")
        {
            this.HasUpdateFields = true;
            this.UpdateFields = ReadToggleValue(reader);
            reader.ReadPastCurrentElement();
            return true;
        }

        if (reader.LocalName() == "trackRevisions")
        {
            this.HasTrackRevisions = true;
            this.TrackRevisions = ReadToggleValue(reader);
            reader.ReadPastCurrentElement();
            return true;
        }

        if (reader.LocalName() == "doNotTrackMoves")
        {
            this.HasDoNotTrackMoves = true;
            this.DoNotTrackMoves = ReadToggleValue(reader);
            reader.ReadPastCurrentElement();
            return true;
        }

        if (reader.LocalName() == "doNotTrackFormatting")
        {
            this.HasDoNotTrackFormatting = true;
            this.DoNotTrackFormatting = ReadToggleValue(reader);
            reader.ReadPastCurrentElement();
            return true;
        }

        if (reader.LocalName() == "autoHyphenation")
        {
            this.HasAutoHyphenation = true;
            this.AutoHyphenation = ReadToggleValue(reader);
            reader.ReadPastCurrentElement();
            return true;
        }

        if (reader.LocalName() == "documentProtection")
        {
            DocxXmlBuffer protection = DocxXmlBuffer.Capture(reader);
            this.ProtectionXml = protection.GetXml();
            ReadProtectionXml();
            return true;
        }

        if (reader.LocalName() == "themeFontLang")
        {
            this.ThemeFontLanguage = reader.GetAttribute("val", DocxConstants.NamespaceWordprocessingMl);
            this.ThemeFontEastAsiaLanguage = reader.GetAttribute("eastAsia", DocxConstants.NamespaceWordprocessingMl);
            this.ThemeFontBidiLanguage = reader.GetAttribute("bidi", DocxConstants.NamespaceWordprocessingMl);
            reader.ReadPastCurrentElement();
            return true;
        }

        if (reader.LocalName() == "compat")
        {
            this.HasCompatibilitySettings = true;
            this.CompatibilitySettings = DocxCompatibilitySettings.Parse(reader);
            return true;
        }

        return false;
    }

    internal static bool ReadToggleValue(DocxXmlReader reader)
    {
        if (reader == null)
        {
            throw new ArgumentNullException("reader");
        }

        return DocxXmlAttributes.ReadBool(reader, "val", DocxConstants.NamespaceWordprocessingMl, true);
    }

    internal void SetZoomPercent(string value)
    {
        this.ZoomPercent = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetView(string value)
    {
        this.View = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetZoom(string view, string percent)
    {
        this.ZoomView = DocxCoreProperties.ValidateValue("view", view);
        this.ZoomPercent = DocxCoreProperties.ValidateValue("percent", percent);
    }

    internal void SetDefaultTabStop(string value)
    {
        this.DefaultTabStop = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetHyphenationZone(string value)
    {
        this.HyphenationZone = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetConsecutiveHyphenLimit(string value)
    {
        this.ConsecutiveHyphenLimit = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetDoNotHyphenateCaps(bool value)
    {
        this.HasDoNotHyphenateCaps = true;
        this.DoNotHyphenateCaps = value;
    }

    internal void SetProofState(string spelling, string grammar)
    {
        this.ProofStateSpelling = DocxCoreProperties.ValidateValue("spelling", spelling);
        this.ProofStateGrammar = DocxCoreProperties.ValidateValue("grammar", grammar);
    }

    internal void SetDecimalSymbol(string value)
    {
        this.DecimalSymbol = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetListSeparator(string value)
    {
        this.ListSeparator = DocxCoreProperties.ValidateValue("value", value);
    }

    internal void SetEvenAndOddHeaders(bool value)
    {
        this.HasEvenAndOddHeaders = true;
        this.EvenAndOddHeaders = value;
    }

    internal void ClearEvenAndOddHeaders()
    {
        this.HasEvenAndOddHeaders = false;
        this.EvenAndOddHeaders = false;
    }

    internal void SetUpdateFields(bool value)
    {
        this.HasUpdateFields = true;
        this.UpdateFields = value;
    }

    internal void SetTrackRevisions(bool value)
    {
        this.HasTrackRevisions = true;
        this.TrackRevisions = value;
    }

    internal void SetDoNotTrackMoves(bool value)
    {
        this.HasDoNotTrackMoves = true;
        this.DoNotTrackMoves = value;
    }

    internal void SetDoNotTrackFormatting(bool value)
    {
        this.HasDoNotTrackFormatting = true;
        this.DoNotTrackFormatting = value;
    }

    internal void SetAutoHyphenation(bool value)
    {
        this.HasAutoHyphenation = true;
        this.AutoHyphenation = value;
    }

    internal void SetProtection(string edit, bool enforcement)
    {
        this.ProtectionEdit = DocxCoreProperties.ValidateValue("edit", edit);
        this.HasProtectionEnforcement = true;
        this.ProtectionEnforcement = enforcement;
        DocxXmlWriter writer = DocxXmlWriter.Create();
        writer.StartElement("w", "documentProtection", DocxConstants.NamespaceWordprocessingMl);

        if (edit != "")
        {
            writer.WriteAttribute("w", "edit", DocxConstants.NamespaceWordprocessingMl, edit);
        }

        DocxXmlAttributes.WriteBool(writer, "w", "enforcement", DocxConstants.NamespaceWordprocessingMl, enforcement);
        writer.EndElement();
        this.ProtectionXml = writer.GetXml();
    }

    internal void ClearProtection()
    {
        this.ProtectionEdit = "";
        this.HasProtectionEnforcement = false;
        this.ProtectionEnforcement = false;
        this.ProtectionXml = "";
    }

    internal void SetThemeFontLanguages(string language, string eastAsiaLanguage, string bidiLanguage)
    {
        this.ThemeFontLanguage = DocxCoreProperties.ValidateValue("language", language);
        this.ThemeFontEastAsiaLanguage = DocxCoreProperties.ValidateValue("eastAsiaLanguage", eastAsiaLanguage);
        this.ThemeFontBidiLanguage = DocxCoreProperties.ValidateValue("bidiLanguage", bidiLanguage);
    }

    internal void ReadProtectionXml()
    {
        DocxXmlReader reader = DocxXmlReader.Create(this.ProtectionXml);

        if (!reader.MoveToContent())
        {
            return;
        }

        this.ProtectionEdit = reader.GetAttribute("edit", DocxConstants.NamespaceWordprocessingMl);
        string enforcement = reader.GetAttribute("enforcement", DocxConstants.NamespaceWordprocessingMl);
        this.HasProtectionEnforcement = enforcement != "";

        if (this.HasProtectionEnforcement)
        {
            this.ProtectionEnforcement = DocxXmlAttributes.ParseBool(enforcement);
        }
    }

    internal DocxCompatibilitySettings GetOrCreateCompatibilitySettings()
    {
        if (!this.HasCompatibilitySettings)
        {
            this.HasCompatibilitySettings = true;
            this.CompatibilitySettings = DocxCompatibilitySettings.Create();
        }

        return this.CompatibilitySettings;
    }

    internal void SetCompatibilitySettings(DocxCompatibilitySettings compatibilitySettings)
    {
        if (compatibilitySettings == null)
        {
            throw new ArgumentNullException("compatibilitySettings");
        }

        this.HasCompatibilitySettings = true;
        this.CompatibilitySettings = compatibilitySettings;
    }

    internal void ClearCompatibilitySettings()
    {
        this.HasCompatibilitySettings = false;
        this.CompatibilitySettings = DocxCompatibilitySettings.Create();
    }

    internal void AddUnknownChild(DocxXmlBuffer child)
    {
        if (child == null)
        {
            throw new ArgumentNullException("child");
        }

        this.UnknownChildren.Add(child);
    }

    internal int UnknownChildCount()
    {
        return this.UnknownChildren.Count;
    }

    internal string GetUnknownChildXml(int index)
    {
        return this.UnknownChildren[index].GetXml();
    }

    internal string WriteXml()
    {
        DocxXmlWriter writer = DocxXmlWriter.Create();
        WriteTo(writer);
        return writer.GetXml();
    }

    internal void WriteTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        writer.StartElement("w", "settings", DocxConstants.NamespaceWordprocessingMl);
        WriteChildrenTo(writer);
        writer.EndElement();
    }

    internal void WriteChildrenTo(DocxXmlWriter writer)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        WriteStringSetting(writer, "view", "val", this.View);
        WriteZoomSetting(writer);
        WriteStringSetting(writer, "defaultTabStop", "val", this.DefaultTabStop);
        WriteStringSetting(writer, "hyphenationZone", "val", this.HyphenationZone);
        WriteStringSetting(writer, "consecutiveHyphenLimit", "val", this.ConsecutiveHyphenLimit);
        WriteToggleSetting(writer, "doNotHyphenateCaps", this.HasDoNotHyphenateCaps, this.DoNotHyphenateCaps);
        WriteProofState(writer);
        WriteStringSetting(writer, "decimalSymbol", "val", this.DecimalSymbol);
        WriteStringSetting(writer, "listSeparator", "val", this.ListSeparator);
        WriteToggleSetting(writer, "evenAndOddHeaders", this.HasEvenAndOddHeaders, this.EvenAndOddHeaders);
        WriteToggleSetting(writer, "updateFields", this.HasUpdateFields, this.UpdateFields);
        WriteToggleSetting(writer, "trackRevisions", this.HasTrackRevisions, this.TrackRevisions);
        WriteToggleSetting(writer, "doNotTrackMoves", this.HasDoNotTrackMoves, this.DoNotTrackMoves);
        WriteToggleSetting(writer, "doNotTrackFormatting", this.HasDoNotTrackFormatting, this.DoNotTrackFormatting);
        WriteToggleSetting(writer, "autoHyphenation", this.HasAutoHyphenation, this.AutoHyphenation);

        if (this.ProtectionXml != "")
        {
            new DocxXmlBuffer(this.ProtectionXml).WriteTo(writer);
        }

        WriteThemeFontLanguages(writer);

        if (this.HasCompatibilitySettings)
        {
            this.CompatibilitySettings.WriteTo(writer);
        }

        foreach (DocxXmlBuffer child in this.UnknownChildren)
        {
            child.WriteTo(writer);
        }
    }

    internal void WriteZoomSetting(DocxXmlWriter writer)
    {
        if (this.ZoomView == "" && this.ZoomPercent == "")
        {
            return;
        }

        writer.StartElement("w", "zoom", DocxConstants.NamespaceWordprocessingMl);

        if (this.ZoomView != "")
        {
            writer.WriteAttribute("w", "val", DocxConstants.NamespaceWordprocessingMl, this.ZoomView);
        }

        if (this.ZoomPercent != "")
        {
            writer.WriteAttribute("w", "percent", DocxConstants.NamespaceWordprocessingMl, this.ZoomPercent);
        }

        writer.EndElement();
    }

    internal void WriteProofState(DocxXmlWriter writer)
    {
        if (this.ProofStateSpelling == "" && this.ProofStateGrammar == "")
        {
            return;
        }

        writer.StartElement("w", "proofState", DocxConstants.NamespaceWordprocessingMl);

        if (this.ProofStateSpelling != "")
        {
            writer.WriteAttribute("w", "spelling", DocxConstants.NamespaceWordprocessingMl, this.ProofStateSpelling);
        }

        if (this.ProofStateGrammar != "")
        {
            writer.WriteAttribute("w", "grammar", DocxConstants.NamespaceWordprocessingMl, this.ProofStateGrammar);
        }

        writer.EndElement();
    }

    internal void WriteThemeFontLanguages(DocxXmlWriter writer)
    {
        if (this.ThemeFontLanguage == "" && this.ThemeFontEastAsiaLanguage == "" && this.ThemeFontBidiLanguage == "")
        {
            return;
        }

        writer.StartElement("w", "themeFontLang", DocxConstants.NamespaceWordprocessingMl);

        if (this.ThemeFontLanguage != "")
        {
            writer.WriteAttribute("w", "val", DocxConstants.NamespaceWordprocessingMl, this.ThemeFontLanguage);
        }

        if (this.ThemeFontEastAsiaLanguage != "")
        {
            writer.WriteAttribute("w", "eastAsia", DocxConstants.NamespaceWordprocessingMl, this.ThemeFontEastAsiaLanguage);
        }

        if (this.ThemeFontBidiLanguage != "")
        {
            writer.WriteAttribute("w", "bidi", DocxConstants.NamespaceWordprocessingMl, this.ThemeFontBidiLanguage);
        }

        writer.EndElement();
    }

    internal static void WriteStringSetting(DocxXmlWriter writer, string localName, string attributeName, string value)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (attributeName == null)
        {
            throw new ArgumentNullException("attributeName");
        }

        if (value == null)
        {
            throw new ArgumentNullException("value");
        }

        if (value == "")
        {
            return;
        }

        writer.StartElement("w", localName, DocxConstants.NamespaceWordprocessingMl);
        writer.WriteAttribute("w", attributeName, DocxConstants.NamespaceWordprocessingMl, value);
        writer.EndElement();
    }

    internal static void WriteToggleSetting(DocxXmlWriter writer, string localName, bool hasValue, bool value)
    {
        if (writer == null)
        {
            throw new ArgumentNullException("writer");
        }

        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (!hasValue)
        {
            return;
        }

        writer.StartElement("w", localName, DocxConstants.NamespaceWordprocessingMl);
        DocxXmlAttributes.WriteBool(writer, "w", "val", DocxConstants.NamespaceWordprocessingMl, value);
        writer.EndElement();
    }
}
