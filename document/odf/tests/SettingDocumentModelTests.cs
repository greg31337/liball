using System.Text;

namespace odf.Tests;

public class SettingDocumentModelTests
{
    [Test]
    public void MaterializesTypedRecursiveSettingsAndSeparatesViewScope()
    {
        OdfDocument document = ReadFlat(SettingsXml());
        OdfSetting portable = document.PortableSettings.Single();
        OdfSetting view = document.ViewSettings.Single();
        OdfSetting namedMap = Child(portable, "Profiles");
        OdfSetting namedEntry = namedMap.Children[0];
        OdfSetting indexedMap = Child(namedEntry, "Stops");

        Assert.Multiple(() =>
        {
            Assert.That(document.ApplicationSettings, Has.Length.EqualTo(2));
            Assert.That(portable.Kind, Is.EqualTo(OdfSettingKind.Set));
            Assert.That(portable.Name,
                Is.EqualTo("ooo:configuration-settings"));
            Assert.That(portable.Scope, Is.EqualTo(OdfSettingScope.Portable));
            Assert.That(view.Scope, Is.EqualTo(OdfSettingScope.View));
            Assert.That(Child(portable, "Enabled").ValueKind,
                Is.EqualTo(OdfSettingValueKind.Boolean));
            Assert.That(Child(portable, "Enabled").BooleanValue, Is.True);
            Assert.That(Child(portable, "Small").IntegerValue,
                Is.EqualTo(-12));
            Assert.That(Child(portable, "Count").IntegerValue,
                Is.EqualTo(123456));
            Assert.That(Child(portable, "Large").IntegerValue,
                Is.EqualTo(long.MaxValue));
            Assert.That(Child(portable, "Ratio").NumericValue,
                Is.EqualTo(1.25));
            Assert.That(Child(portable, "Caption").StringValue,
                Is.EqualTo("Hello settings"));
            Assert.That(Child(portable, "Modified").ValueKind,
                Is.EqualTo(OdfSettingValueKind.DateTime));
            Assert.That(Child(portable, "Modified").IsValid, Is.True);
            Assert.That(Child(portable, "Payload").BinaryValue,
                Is.EqualTo(new byte[] { 1, 2, 3 }));
            Assert.That(namedMap.Kind,
                Is.EqualTo(OdfSettingKind.NamedMap));
            Assert.That(namedEntry.Kind,
                Is.EqualTo(OdfSettingKind.MapEntry));
            Assert.That(namedEntry.Name, Is.EqualTo("Default"));
            Assert.That(indexedMap.Kind,
                Is.EqualTo(OdfSettingKind.IndexedMap));
            Assert.That(indexedMap.Children[0].Kind,
                Is.EqualTo(OdfSettingKind.MapEntry));
            Assert.That(Child(indexedMap.Children[0], "Position").IntegerValue,
                Is.EqualTo(4));
            Assert.That(Child(view, "Zoom").Scope,
                Is.EqualTo(OdfSettingScope.View));
        });
    }

    [Test]
    public void WritesLexicalAndTypedSettingChanges()
    {
        OdfDocument document = ReadFlat(SettingsXml());
        OdfSetting portable = document.PortableSettings[0];
        Child(portable, "Enabled").BooleanValue = false;
        Child(portable, "Small").IntegerValue = 32000;
        Child(portable, "Count").IntegerValue = -42;
        Child(portable, "Large").IntegerValue = 5000000000;
        Child(portable, "Ratio").NumericValue = 2.5;
        Child(portable, "Caption").StringValue = "Changed";
        Child(portable, "Modified").StringValue =
            "2026-09-03T08:15:00Z";
        Child(portable, "Payload").BinaryValue = [4, 5, 6];
        OdfSetting position = Child(
            Child(Child(portable, "Profiles").Children[0], "Stops")
                .Children[0],
            "Position");
        position.LexicalValue = "9";
        document.ViewSettings[0].Name = "ooo:window-settings";

        OdfDocument reread = ReadFlat(Encoding.UTF8.GetString(
            Write(document)));
        OdfSetting changed = reread.PortableSettings.Single(
            item => item.Name == "ooo:configuration-settings");
        OdfSetting moved = reread.PortableSettings.Single(
            item => item.Name == "ooo:window-settings");

        Assert.Multiple(() =>
        {
            Assert.That(Child(changed, "Enabled").LexicalValue,
                Is.EqualTo("false"));
            Assert.That(Child(changed, "Small").IntegerValue,
                Is.EqualTo(32000));
            Assert.That(Child(changed, "Count").IntegerValue,
                Is.EqualTo(-42));
            Assert.That(Child(changed, "Large").IntegerValue,
                Is.EqualTo(5000000000));
            Assert.That(Child(changed, "Ratio").NumericValue,
                Is.EqualTo(2.5));
            Assert.That(Child(changed, "Caption").StringValue,
                Is.EqualTo("Changed"));
            Assert.That(Child(changed, "Modified").StringValue,
                Is.EqualTo("2026-09-03T08:15:00Z"));
            Assert.That(Child(changed, "Payload").BinaryValue,
                Is.EqualTo(new byte[] { 4, 5, 6 }));
            Assert.That(Child(
                    Child(Child(changed, "Profiles").Children[0], "Stops")
                        .Children[0],
                    "Position").IntegerValue,
                Is.EqualTo(9));
            Assert.That(moved.Scope, Is.EqualTo(OdfSettingScope.Portable));
            Assert.That(reread.ViewSettings, Is.Empty);
        });
    }

    [Test]
    public void PreservesInvalidAndUnknownLexicalValues()
    {
        string content = "<config:config-item-set "
            + "config:name='ooo:configuration-settings'>"
            + Item("BadBoolean", "boolean", "sometimes")
            + Item("BadShort", "short", "40000")
            + Item("BadInteger", "int", "3000000000")
            + Item("BadLong", "long", "9223372036854775808")
            + Item("BadDouble", "double", "1.2.3")
            + Item("BadDate", "datetime", "tomorrow")
            + Item("BadBinary", "base64Binary", "not-base64")
            + Item("Vendor", "vendor-type", "opaque")
            + "</config:config-item-set>";
        OdfDocument document = ReadFlat(FlatXml(content));
        OdfSetting set = document.PortableSettings[0];
        string output = Encoding.UTF8.GetString(Write(document));

        Assert.Multiple(() =>
        {
            Assert.That(set.Children.All(item => !item.IsValid), Is.True);
            Assert.That(Child(set, "Vendor").ValueKind,
                Is.EqualTo(OdfSettingValueKind.Unknown));
            Assert.That(output, Does.Contain("sometimes"));
            Assert.That(output, Does.Contain("9223372036854775808"));
            Assert.That(output, Does.Contain("not-base64"));
            Assert.That(output, Does.Contain("vendor-type"));
            Assert.That(output, Does.Contain("opaque"));
        });
    }

    static OdfSetting Child(OdfSetting parent, string name)
    {
        return parent.Children.Single(item => item.Name == name);
    }

    static OdfDocument ReadFlat(string xml)
    {
        return new OdfDocumentReader().ReadDocumentBytes(
            Encoding.UTF8.GetBytes(xml),
            "settings.fodt",
            new OdfLoadOptions());
    }

    static byte[] Write(OdfDocument document)
    {
        return new OdfDocumentWriter().WriteDocumentBytes(
            document,
            new OdfSaveOptions
            {
                Deterministic = true
            });
    }

    static string SettingsXml()
    {
        string portable = "<config:config-item-set "
            + "config:name='ooo:configuration-settings'>"
            + Item("Enabled", "boolean", "true")
            + Item("Small", "short", "-12")
            + Item("Count", "int", "123456")
            + Item("Large", "long", "9223372036854775807")
            + Item("Ratio", "double", "1.25")
            + Item("Caption", "string", "Hello settings")
            + Item("Modified", "datetime", "2026-09-02T12:30:00Z")
            + Item("Payload", "base64Binary", "AQID")
            + "<config:config-item-set config:name='Nested'>"
            + Item("NestedText", "string", "value")
            + "</config:config-item-set>"
            + "<config:config-item-map-named config:name='Profiles'>"
            + "<config:config-item-map-entry config:name='Default'>"
            + "<config:config-item-map-indexed config:name='Stops'>"
            + "<config:config-item-map-entry>"
            + Item("Position", "int", "4")
            + "</config:config-item-map-entry>"
            + "</config:config-item-map-indexed>"
            + "</config:config-item-map-entry>"
            + "</config:config-item-map-named>"
            + "</config:config-item-set>";
        string view = "<config:config-item-set "
            + "config:name='ooo:view-settings'>"
            + Item("Zoom", "short", "125")
            + "</config:config-item-set>";
        return FlatXml(portable + view);
    }

    static string Item(string name, string type, string value)
    {
        return "<config:config-item config:name='" + name
            + "' config:type='" + type + "'>" + value
            + "</config:config-item>";
    }

    static string FlatXml(string settings)
    {
        return "<?xml version='1.0' encoding='UTF-8'?>"
            + "<office:document xmlns:office='" + OdfNamespaces.Office
            + "' xmlns:config='" + OdfNamespaces.Config
            + "' office:version='1.4' office:mimetype='"
            + OdfMediaTypes.Text + "'><office:settings>" + settings
            + "</office:settings><office:body><office:text/>"
            + "</office:body></office:document>";
    }
}
