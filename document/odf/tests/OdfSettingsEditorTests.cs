namespace odf.Tests;

public class OdfSettingsEditorTests
{
    [Test]
    public void CreatesEverySettingContainerAndValueKind()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreateFlatDocument(OdfDocumentKind.Text);
        IOdfSettingsEditor editor = new OdfSettingsEditor();
        OdfSetting portable = editor.CreatePortableSettingSet(
            document,
            "ooo:configuration-settings");
        OdfSetting nestedSet = editor.CreateSetting(
            document,
            portable,
            OdfSettingKind.Set,
            "Nested",
            "",
            "");
        CreateValue(editor, document, nestedSet,
            "Boolean", "boolean", "true");
        CreateValue(editor, document, nestedSet,
            "Short", "short", "-12");
        CreateValue(editor, document, nestedSet,
            "Integer", "int", "123456");
        CreateValue(editor, document, nestedSet,
            "Long", "long", "5000000000");
        CreateValue(editor, document, nestedSet,
            "Double", "double", "1.25");
        CreateValue(editor, document, nestedSet,
            "String", "string", "settings text");
        CreateValue(editor, document, nestedSet,
            "DateTime", "datetime", "2026-09-02T12:30:00Z");
        CreateValue(editor, document, nestedSet,
            "Binary", "base64Binary", "AQID");

        OdfSetting namedMap = editor.CreateSetting(
            document,
            portable,
            OdfSettingKind.NamedMap,
            "Profiles",
            "",
            "");
        OdfSetting namedEntry = editor.CreateSetting(
            document,
            namedMap,
            OdfSettingKind.MapEntry,
            "Default",
            "",
            "");
        CreateValue(editor, document, namedEntry,
            "Caption", "string", "default profile");
        OdfSetting indexedMap = editor.CreateSetting(
            document,
            portable,
            OdfSettingKind.IndexedMap,
            "Stops",
            "",
            "");
        OdfSetting indexedEntry = editor.CreateSetting(
            document,
            indexedMap,
            OdfSettingKind.MapEntry,
            "",
            "",
            "");
        CreateValue(editor, document, indexedEntry,
            "Position", "int", "4");
        editor.CreateViewSettingSet(document, "ooo:view-settings");
        editor.ApplySettingChanges(document);

        OdfDocument reread = Read(Write(document));
        IOdfSettingsEditor rereadEditor = new OdfSettingsEditor();
        OdfSetting changedSet = rereadEditor.FindSetting(
            reread, "Nested")!;
        OdfSetting changedNamed = rereadEditor.FindSetting(
            reread, "Profiles")!;
        OdfSetting changedIndexed = rereadEditor.FindSetting(
            reread, "Stops")!;

        Assert.Multiple(() =>
        {
            Assert.That(changedSet.Kind, Is.EqualTo(OdfSettingKind.Set));
            Assert.That(changedSet.Children.Select(item => item.ValueKind),
                Is.EqualTo(new[]
                {
                    OdfSettingValueKind.Boolean,
                    OdfSettingValueKind.Short,
                    OdfSettingValueKind.Integer,
                    OdfSettingValueKind.Long,
                    OdfSettingValueKind.Double,
                    OdfSettingValueKind.String,
                    OdfSettingValueKind.DateTime,
                    OdfSettingValueKind.Base64Binary
                }));
            Assert.That(rereadEditor.FindSetting(reread, "Boolean")
                ?.BooleanValue, Is.True);
            Assert.That(rereadEditor.FindSetting(reread, "Short")
                ?.IntegerValue, Is.EqualTo(-12));
            Assert.That(rereadEditor.FindSetting(reread, "Integer")
                ?.IntegerValue, Is.EqualTo(123456));
            Assert.That(rereadEditor.FindSetting(reread, "Long")
                ?.IntegerValue, Is.EqualTo(5000000000));
            Assert.That(rereadEditor.FindSetting(reread, "Double")
                ?.NumericValue, Is.EqualTo(1.25));
            Assert.That(rereadEditor.FindSetting(reread, "String")
                ?.StringValue, Is.EqualTo("settings text"));
            Assert.That(rereadEditor.FindSetting(reread, "DateTime")
                ?.StringValue, Is.EqualTo("2026-09-02T12:30:00Z"));
            Assert.That(rereadEditor.FindSetting(reread, "Binary")
                ?.BinaryValue, Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(changedNamed.Kind,
                Is.EqualTo(OdfSettingKind.NamedMap));
            Assert.That(changedNamed.Children[0].Kind,
                Is.EqualTo(OdfSettingKind.MapEntry));
            Assert.That(changedNamed.Children[0].Name,
                Is.EqualTo("Default"));
            Assert.That(changedIndexed.Kind,
                Is.EqualTo(OdfSettingKind.IndexedMap));
            Assert.That(changedIndexed.Children[0].Kind,
                Is.EqualTo(OdfSettingKind.MapEntry));
            Assert.That(changedIndexed.Children[0].Name, Is.Empty);
            Assert.That(rereadEditor.GetPortableSettings(reread),
                Has.Length.EqualTo(1));
            Assert.That(rereadEditor.GetViewSettings(reread),
                Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void CreatesEditsFindsAndDeletesSettingTrees()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreateFlatDocument(OdfDocumentKind.Text);
        IOdfSettingsEditor editor = new OdfSettingsEditor();
        OdfSetting[] emptySnapshot = editor.GetApplicationSettings(document);
        OdfSetting portable = editor.CreatePortableSettingSet(
            document,
            "ooo:configuration-settings");
        OdfSetting view = editor.CreateViewSettingSet(
            document,
            "ooo:view-settings");
        OdfSetting enabled = editor.CreateSetting(
            document,
            portable,
            OdfSettingKind.Item,
            "Enabled",
            "boolean",
            "true");
        OdfSetting profiles = editor.CreateSetting(
            document,
            portable,
            OdfSettingKind.NamedMap,
            "Profiles",
            "",
            "");
        OdfSetting profile = editor.CreateSetting(
            document,
            profiles,
            OdfSettingKind.MapEntry,
            "Default",
            "",
            "");
        editor.CreateSetting(
            document,
            profile,
            OdfSettingKind.Item,
            "Caption",
            "string",
            "Original");
        editor.CreateSetting(
            document,
            view,
            OdfSettingKind.Item,
            "Zoom",
            "short",
            "125");
        enabled.BooleanValue = false;
        editor.FindSetting(document, "Caption")!.StringValue = "Changed";
        editor.ApplySettingChanges(document);

        OdfDocument reread = Read(Write(document));
        IOdfSettingsEditor rereadEditor = new OdfSettingsEditor();
        Assert.Multiple(() =>
        {
            Assert.That(emptySnapshot, Is.Empty);
            Assert.That(editor.GetApplicationSettings(document),
                Is.Not.SameAs(document.ApplicationSettings));
            Assert.That(rereadEditor.GetApplicationSettings(reread),
                Has.Length.EqualTo(2));
            Assert.That(rereadEditor.GetPortableSettings(reread),
                Has.Length.EqualTo(1));
            Assert.That(rereadEditor.GetViewSettings(reread),
                Has.Length.EqualTo(1));
            Assert.That(rereadEditor.FindSetting(reread, "Enabled")
                ?.BooleanValue, Is.False);
            Assert.That(rereadEditor.FindSetting(reread, "Caption")
                ?.StringValue, Is.EqualTo("Changed"));
            Assert.That(rereadEditor.FindSetting(reread, "Caption")
                ?.Scope, Is.EqualTo(OdfSettingScope.Portable));
        });

        OdfSetting rereadProfiles = rereadEditor.FindSetting(
            reread,
            "Profiles")!;
        OdfSetting rereadView = rereadEditor.GetViewSettings(reread)[0];
        rereadEditor.DeleteSetting(reread, rereadProfiles);
        rereadEditor.DeleteSetting(reread, rereadView);
        OdfDocument deleted = Read(Write(reread));
        Assert.Multiple(() =>
        {
            Assert.That(rereadEditor.FindSetting(reread, "Caption"), Is.Null);
            Assert.That(deleted.ApplicationSettings, Has.Length.EqualTo(1));
            Assert.That(deleted.ViewSettings, Is.Empty);
            Assert.That(deleted.PortableSettings[0].Children,
                Has.Length.EqualTo(1));
        });
    }

    [Test]
    public void CreatesMissingPackageSettingsPart()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreatePackageDocument(OdfDocumentKind.Text);
        IOdfSettingsEditor editor = new OdfSettingsEditor();
        OdfSetting settings = editor.CreatePortableSettingSet(
            document,
            "ooo:configuration-settings");
        editor.CreateSetting(
            document,
            settings,
            OdfSettingKind.Item,
            "Name",
            "string",
            "Package value");

        OdfDocument reread = Read(Write(document));
        Assert.Multiple(() =>
        {
            Assert.That(document.Parts.Any(part =>
                part.Path == OdfPartNames.Settings), Is.True);
            Assert.That(reread.ApplicationSettings, Has.Length.EqualTo(1));
            Assert.That(new OdfSettingsEditor().FindSetting(reread, "Name")
                ?.StringValue, Is.EqualTo("Package value"));
        });
    }

    [Test]
    public void RejectsInvalidSettingStructuresAndAmbiguousNames()
    {
        OdfDocument document = new OdfDocumentFactory()
            .CreateFlatDocument(OdfDocumentKind.Text);
        IOdfSettingsEditor editor = new OdfSettingsEditor();
        OdfSetting settings = editor.CreatePortableSettingSet(
            document,
            "ooo:configuration-settings");
        editor.CreateSetting(
            document,
            settings,
            OdfSettingKind.Item,
            "Duplicate",
            "string",
            "first");
        editor.CreateSetting(
            document,
            settings,
            OdfSettingKind.Item,
            "Duplicate",
            "string",
            "second");

        Assert.Multiple(() =>
        {
            Assert.That(editor.FindSetting(document, "Duplicate"), Is.Null);
            Assert.That(() => editor.CreateViewSettingSet(
                    document,
                    "ordinary-settings"),
                Throws.TypeOf<OdfEditException>());
            Assert.That(() => editor.CreateSetting(
                    document,
                    settings.Children[0],
                    OdfSettingKind.Item,
                    "Nested",
                    "string",
                    "bad"),
                Throws.TypeOf<OdfEditException>());
            Assert.That(() => editor.CreateSetting(
                    document,
                    settings,
                    OdfSettingKind.Item,
                    "BadType",
                    "vendor-type",
                    "bad"),
                Throws.TypeOf<OdfEditException>());
        });
    }

    static OdfDocument Read(byte[] data)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            data,
            "settings.odf",
            new OdfLoadOptions());
    }

    static OdfSetting CreateValue(
        IOdfSettingsEditor editor,
        OdfDocument document,
        OdfSetting parent,
        string name,
        string valueType,
        string lexicalValue)
    {
        return editor.CreateSetting(
            document,
            parent,
            OdfSettingKind.Item,
            name,
            valueType,
            lexicalValue);
    }

    static byte[] Write(OdfDocument document)
    {
        return new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions { Deterministic = true });
    }
}
