using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxSettingsTests
{
    [Test]
    public void SettingsParsing_ReadsKnownSettingsAndPreservesUnknownChildren()
    {
        global::docx.DocxSettings settings = ParseSettings("<w:settings xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:x=\"urn:x\"><w:zoom w:percent=\"125\" /><w:defaultTabStop w:val=\"720\" /><w:hyphenationZone w:val=\"360\" /><w:decimalSymbol w:val=\",\" /><w:listSeparator w:val=\";\" /><w:evenAndOddHeaders /><w:updateFields w:val=\"false\" /><w:trackRevisions w:val=\"1\" /><w:doNotTrackMoves w:val=\"0\" /><w:doNotTrackFormatting w:val=\"true\" /><w:autoHyphenation w:val=\"on\" /><w:compat><w:compatSetting w:name=\"compatibilityMode\" w:val=\"15\" /></w:compat><x:custom x:data=\"1\" /></w:settings>");

        Assert.That(settings.ZoomPercent, Is.EqualTo("125"));
        Assert.That(settings.DefaultTabStop, Is.EqualTo("720"));
        Assert.That(settings.HyphenationZone, Is.EqualTo("360"));
        Assert.That(settings.DecimalSymbol, Is.EqualTo(","));
        Assert.That(settings.ListSeparator, Is.EqualTo(";"));
        Assert.That(settings.HasEvenAndOddHeaders, Is.True);
        Assert.That(settings.EvenAndOddHeaders, Is.True);
        Assert.That(settings.HasUpdateFields, Is.True);
        Assert.That(settings.UpdateFields, Is.False);
        Assert.That(settings.HasTrackRevisions, Is.True);
        Assert.That(settings.TrackRevisions, Is.True);
        Assert.That(settings.HasDoNotTrackMoves, Is.True);
        Assert.That(settings.DoNotTrackMoves, Is.False);
        Assert.That(settings.HasDoNotTrackFormatting, Is.True);
        Assert.That(settings.DoNotTrackFormatting, Is.True);
        Assert.That(settings.HasAutoHyphenation, Is.True);
        Assert.That(settings.AutoHyphenation, Is.True);
        Assert.That(settings.HasCompatibilitySettings, Is.True);
        Assert.That(settings.CompatibilitySettings.SettingCount(), Is.EqualTo(1));
        Assert.That(settings.CompatibilitySettings.FindSettingByName("compatibilityMode")!.Value, Is.EqualTo("15"));
        Assert.That(settings.UnknownChildCount(), Is.EqualTo(1));
        Assert.That(settings.GetUnknownChildXml(0), Does.Contain("x:data=\"1\""));
    }

    [Test]
    public void SettingsWriting_WritesKnownSettingsAndUnknownChildren()
    {
        global::docx.DocxSettings settings = global::docx.DocxSettings.Create();

        settings.SetZoomPercent("150");
        settings.SetDefaultTabStop("720");
        settings.SetHyphenationZone("360");
        settings.SetDecimalSymbol(".");
        settings.SetListSeparator(",");
        settings.SetEvenAndOddHeaders(true);
        settings.SetUpdateFields(false);
        settings.SetTrackRevisions(true);
        settings.SetDoNotTrackMoves(false);
        settings.SetDoNotTrackFormatting(true);
        settings.SetAutoHyphenation(true);
        settings.GetOrCreateCompatibilitySettings().AddSetting("compatibilityMode", "http://schemas.microsoft.com/office/word", "15");

        string xml = settings.WriteXml();
        global::docx.DocxSettings parsed = ParseSettings(xml);

        Assert.That(xml, Does.Contain("<w:settings"));
        Assert.That(xml, Does.Contain("w:percent=\"150\""));
        Assert.That(xml, Does.Contain("w:val=\"0\""));
        Assert.That(parsed.ZoomPercent, Is.EqualTo("150"));
        Assert.That(parsed.UpdateFields, Is.False);
        Assert.That(parsed.TrackRevisions, Is.True);
        Assert.That(parsed.HasCompatibilitySettings, Is.True);
        Assert.That(parsed.CompatibilitySettings.FindSettingByName("compatibilityMode")!.Value, Is.EqualTo("15"));

        settings.ClearEvenAndOddHeaders();
        parsed = ParseSettings(settings.WriteXml());

        Assert.That(parsed.HasEvenAndOddHeaders, Is.False);
    }

    [Test]
    public void SettingsWriting_PreservesUnknownChildrenAfterMutation()
    {
        global::docx.DocxSettings settings = ParseSettings("<w:settings xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" xmlns:x=\"urn:x\"><x:custom x:data=\"1\" /><w:trackRevisions /></w:settings>");

        settings.SetZoomPercent("90");
        settings.SetTrackRevisions(false);

        string xml = settings.WriteXml();
        global::docx.DocxSettings parsed = ParseSettings(xml);

        Assert.That(parsed.ZoomPercent, Is.EqualTo("90"));
        Assert.That(parsed.HasTrackRevisions, Is.True);
        Assert.That(parsed.TrackRevisions, Is.False);
        Assert.That(parsed.UnknownChildCount(), Is.EqualTo(1));
        Assert.That(parsed.GetUnknownChildXml(0), Does.Contain("x:data=\"1\""));
    }

    [Test]
    public void DocumentSettings_ParseCreateWriteAndReopen()
    {
        global::docx.DocxPackage package = global::docx.DocxPackage.Create();
        string mainPartName = package.GetMainDocumentPartName();
        global::docx.DocxRelationshipSet relationshipSet = new global::docx.DocxRelationshipSet(mainPartName);
        relationshipSet.CreateRelationship(global::docx.DocxConstants.RelationshipTypeSettings, "settings.xml", false);
        package.RelationshipSets.Add(relationshipSet);
        package.AddXmlPartWithContent(global::docx.DocxRelationshipSet.GetRelationshipPartName(mainPartName), global::docx.DocxConstants.ContentTypeRelationships, relationshipSet.WriteXml());
        package.AddXmlPartWithContent("/word/settings.xml", global::docx.DocxConstants.ContentTypeSettings, "<w:settings xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:zoom w:percent=\"100\" /></w:settings>");

        global::docx.DocxDocument document = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(document.HasSettings(), Is.True);
        Assert.That(document.GetSettings().ZoomPercent, Is.EqualTo("100"));
        Assert.That(package.GetSettingsPartName(), Is.EqualTo("/word/settings.xml"));
        Assert.That(package.HasSettingsPart(), Is.True);

        document.GetSettings().SetZoomPercent("110");
        document.WriteSettingsPart();

        global::docx.DocxDocument reopened = global::docx.DocxDocument.OpenPackage(package);

        Assert.That(reopened.GetSettings().ZoomPercent, Is.EqualTo("110"));

        global::docx.DocxDocument created = global::docx.DocxDocument.Create();
        global::docx.DocxSettings settings = created.GetOrCreateSettings();

        settings.SetTrackRevisions(true);
        created.WriteSettingsPart();

        Assert.That(created.HasSettings(), Is.True);
        Assert.That(created.Package.GetSettingsPartName(), Is.EqualTo("/word/settings.xml"));
        Assert.That(created.Package.GetSettingsPart()!.GetText(), Does.Contain("trackRevisions"));
        Assert.That(created.Package.FindSettingsRelationship(), Is.Not.Null);
    }

    [Test]
    public void SettingsParsing_ReadsViewProofingProtectionLanguageAndHyphenationDetails()
    {
        global::docx.DocxSettings settings = ParseSettings("<w:settings xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:view w:val=\"web\" /><w:zoom w:val=\"bestFit\" w:percent=\"130\" /><w:autoHyphenation /><w:hyphenationZone w:val=\"240\" /><w:consecutiveHyphenLimit w:val=\"2\" /><w:doNotHyphenateCaps w:val=\"false\" /><w:proofState w:spelling=\"clean\" w:grammar=\"dirty\" /><w:documentProtection w:edit=\"readOnly\" w:enforcement=\"1\" w:cryptSpinCount=\"100000\" /><w:themeFontLang w:val=\"en-US\" w:eastAsia=\"ja-JP\" w:bidi=\"ar-SA\" /></w:settings>");

        Assert.That(settings.View, Is.EqualTo("web"));
        Assert.That(settings.ZoomView, Is.EqualTo("bestFit"));
        Assert.That(settings.ZoomPercent, Is.EqualTo("130"));
        Assert.That(settings.ConsecutiveHyphenLimit, Is.EqualTo("2"));
        Assert.That(settings.HasDoNotHyphenateCaps, Is.True);
        Assert.That(settings.DoNotHyphenateCaps, Is.False);
        Assert.That(settings.ProofStateSpelling, Is.EqualTo("clean"));
        Assert.That(settings.ProofStateGrammar, Is.EqualTo("dirty"));
        Assert.That(settings.ProtectionEdit, Is.EqualTo("readOnly"));
        Assert.That(settings.HasProtectionEnforcement, Is.True);
        Assert.That(settings.ProtectionEnforcement, Is.True);
        Assert.That(settings.ThemeFontLanguage, Is.EqualTo("en-US"));
        Assert.That(settings.ThemeFontEastAsiaLanguage, Is.EqualTo("ja-JP"));
        Assert.That(settings.ThemeFontBidiLanguage, Is.EqualTo("ar-SA"));
        Assert.That(settings.WriteXml(), Does.Contain("cryptSpinCount=\"100000\""));
    }

    [Test]
    public void PropertySettingsManagement_CreatesUpdatesAndRemovesDocumentProperties()
    {
        global::docx.DocxPropertySettingsManagement management = new global::docx.DocxPropertySettingsManagement();
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();

        management.SetCoreProperty(document, global::docx.DocxCorePropertyKind.Title, "Quarterly Report");
        management.SetCoreProperty(document, global::docx.DocxCorePropertyKind.Revision, "9");
        management.SetExtendedProperty(document, global::docx.DocxExtendedPropertyKind.Application, "docx");
        management.SetExtendedProperty(document, global::docx.DocxExtendedPropertyKind.Paragraphs, "12");
        global::docx.DocxCustomProperty custom = management.SetCustomProperty(document, "Project", "lpwstr", "Apollo");

        management.SetCustomPropertyMetadata(document, custom, "ProjectName", global::docx.DocxCustomProperty.DefaultFormatId, "Bookmark1");
        management.SetCustomProperty(document, "Count", "i4", "7");

        Assert.That(management.GetCoreProperties(document).Title, Is.EqualTo("Quarterly Report"));
        Assert.That(management.GetCoreProperties(document).Revision, Is.EqualTo("9"));
        Assert.That(management.GetExtendedProperties(document).Application, Is.EqualTo("docx"));
        Assert.That(management.GetExtendedProperties(document).Paragraphs, Is.EqualTo("12"));
        Assert.That(management.FindCustomProperty(document, "ProjectName")!.LinkTarget, Is.EqualTo("Bookmark1"));
        Assert.That(management.GetCustomProperties(document), Has.Count.EqualTo(2));
        Assert.That(management.RemoveCustomProperty(document, "Count"), Is.True);
        Assert.That(management.RemoveCustomProperty(document, "Count"), Is.False);

        global::docx.DocxDocument reopened = global::docx.DocxDocument.OpenPackage(document.Package);

        Assert.That(reopened.GetCoreProperties().Title, Is.EqualTo("Quarterly Report"));
        Assert.That(reopened.GetExtendedProperties().Paragraphs, Is.EqualTo("12"));
        Assert.That(management.FindCustomProperty(reopened, "ProjectName")!.Value, Is.EqualTo("Apollo"));
        Assert.That(management.FindCustomProperty(reopened, "Count"), Is.Null);
    }

    [Test]
    public void PropertySettingsManagement_UpdatesAllDocumentSettingGroups()
    {
        global::docx.DocxPropertySettingsManagement management = new global::docx.DocxPropertySettingsManagement();
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();

        management.SetDocumentView(document, "print");
        management.SetDocumentZoom(document, "fullPage", "115");
        management.SetHyphenationOptions(document, true, "360", "3", true);
        management.SetProofingState(document, "clean", "dirty");
        management.SetDefaultTabStop(document, "720");
        management.SetRevisionBehavior(document, true, true, false);
        management.SetFieldUpdateOnOpen(document, true);
        management.SetDocumentLanguage(document, "en-US", "zh-CN", "he-IL");
        management.SetDocumentReadOnly(document, true);
        global::docx.DocxCompatibilitySetting compatibility = management.SetCompatibilitySetting(document, "compatibilityMode", "http://schemas.microsoft.com/office/word", "15");

        global::docx.DocxSettings settings = management.GetDocumentSettings(document);

        Assert.That(settings.View, Is.EqualTo("print"));
        Assert.That(settings.ZoomView, Is.EqualTo("fullPage"));
        Assert.That(settings.ZoomPercent, Is.EqualTo("115"));
        Assert.That(settings.AutoHyphenation, Is.True);
        Assert.That(settings.HyphenationZone, Is.EqualTo("360"));
        Assert.That(settings.ConsecutiveHyphenLimit, Is.EqualTo("3"));
        Assert.That(settings.DoNotHyphenateCaps, Is.True);
        Assert.That(settings.ProofStateSpelling, Is.EqualTo("clean"));
        Assert.That(settings.DefaultTabStop, Is.EqualTo("720"));
        Assert.That(settings.TrackRevisions, Is.True);
        Assert.That(settings.DoNotTrackMoves, Is.True);
        Assert.That(settings.DoNotTrackFormatting, Is.False);
        Assert.That(settings.UpdateFields, Is.True);
        Assert.That(settings.ProtectionEdit, Is.EqualTo("readOnly"));
        Assert.That(settings.ProtectionEnforcement, Is.True);
        Assert.That(settings.ThemeFontEastAsiaLanguage, Is.EqualTo("zh-CN"));
        Assert.That(compatibility.Value, Is.EqualTo("15"));
        Assert.That(management.GetCompatibilitySettings(document), Has.Count.EqualTo(1));
        Assert.That(management.FindCompatibilitySetting(document, "compatibilityMode"), Is.SameAs(compatibility));

        management.SetDocumentProtection(document, "comments", true);
        Assert.That(settings.ProtectionEdit, Is.EqualTo("comments"));
        management.ClearDocumentProtection(document);
        Assert.That(settings.ProtectionEdit, Is.EqualTo(""));
        Assert.That(management.RemoveCompatibilitySetting(document, "compatibilityMode"), Is.True);
        Assert.That(management.GetCompatibilitySettings(document), Is.Empty);

        global::docx.DocxDocument reopened = global::docx.DocxDocument.OpenPackage(document.Package);
        Assert.That(reopened.GetSettings().View, Is.EqualTo("print"));
        Assert.That(reopened.GetSettings().ZoomPercent, Is.EqualTo("115"));
        Assert.That(reopened.GetSettings().ThemeFontBidiLanguage, Is.EqualTo("he-IL"));
        Assert.That(reopened.GetSettings().ProtectionEdit, Is.EqualTo(""));
    }

    [Test]
    public void PropertySettingsManagement_CreatesAndReplacesFontThemeAndWebSettingsParts()
    {
        global::docx.DocxPropertySettingsManagement management = new global::docx.DocxPropertySettingsManagement();
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        string fonts = "<w:fonts xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:font w:name=\"Aptos\" /></w:fonts>";
        string theme = "<a:theme xmlns:a=\"http://schemas.openxmlformats.org/drawingml/2006/main\" name=\"Office\"><a:themeElements /></a:theme>";
        string web = "<w:webSettings xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:optimizeForBrowser /></w:webSettings>";

        management.SetFontTableXml(document, fonts);
        management.SetThemeXml(document, theme);
        management.SetWebSettingsXml(document, web);

        Assert.That(management.GetFontTableXml(document), Does.Contain("Aptos"));
        Assert.That(management.GetThemeXml(document), Does.Contain("Office"));
        Assert.That(management.GetWebSettingsXml(document), Does.Contain("optimizeForBrowser"));
        Assert.That(document.Package.FindFontTableRelationship(), Is.Not.Null);
        Assert.That(document.Package.FindThemeRelationship(), Is.Not.Null);
        Assert.That(document.Package.FindWebSettingsRelationship(), Is.Not.Null);

        management.SetFontTableXml(document, "<w:fonts xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:font w:name=\"Calibri\" /></w:fonts>");
        Assert.That(management.GetFontTableXml(document), Does.Contain("Calibri"));
        Assert.Throws<ArgumentException>(() => management.SetThemeXml(document, "<root />"));
    }

    [Test]
    public void Settings_RejectInvalidArguments()
    {
        global::docx.DocxSettings settings = global::docx.DocxSettings.Create();
        global::docx.DocxDocument document = global::docx.DocxDocument.Create();
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");
        global::docx.DocxXmlWriter writer = global::docx.DocxXmlWriter.Create();

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxSettings(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxSettings.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxSettings.ParseXml(null!));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxSettings.ParseXml("<root />"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxSettings.IsSupported(null!));
        Assert.Throws<ArgumentNullException>(() => settings.ReadKnownChild(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxSettings.ReadToggleValue(null!));
        Assert.Throws<ArgumentNullException>(() => settings.SetZoomPercent(null!));
        Assert.Throws<ArgumentNullException>(() => settings.SetDefaultTabStop(null!));
        Assert.Throws<ArgumentNullException>(() => settings.SetHyphenationZone(null!));
        Assert.Throws<ArgumentNullException>(() => settings.SetDecimalSymbol(null!));
        Assert.Throws<ArgumentNullException>(() => settings.SetListSeparator(null!));
        Assert.Throws<ArgumentNullException>(() => settings.SetCompatibilitySettings(null!));
        Assert.Throws<ArgumentNullException>(() => settings.AddUnknownChild(null!));
        Assert.Throws<ArgumentNullException>(() => settings.WriteTo(null!));
        Assert.Throws<ArgumentNullException>(() => settings.WriteChildrenTo(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxSettings.WriteStringSetting(null!, "zoom", "percent", "100"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxSettings.WriteStringSetting(writer, null!, "percent", "100"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxSettings.WriteStringSetting(writer, "zoom", null!, "100"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxSettings.WriteStringSetting(writer, "zoom", "percent", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxSettings.WriteToggleSetting(null!, "trackRevisions", true, true));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxSettings.WriteToggleSetting(writer, null!, true, true));
        Assert.Throws<InvalidOperationException>(() => document.WriteSettingsPart());

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxSettings.Parse(reader));
    }

    static global::docx.DocxSettings ParseSettings(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("settings", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxSettings.Parse(reader);
    }
}
