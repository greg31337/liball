using NUnit.Framework;

namespace docx.Tests;

[TestFixture]
public class DocxCompatibilitySettingsTests
{
    [Test]
    public void CompatibilitySettingsParsing_ReadsCompatSettingsAndPreservesUnknownChildren()
    {
        global::docx.DocxCompatibilitySettings compatibility = ParseCompatibility("<w:compat xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:compatSetting w:name=\"compatibilityMode\" w:uri=\"http://schemas.microsoft.com/office/word\" w:val=\"15\" /><w:compatSetting w:name=\"overrideTableStyleFontSizeAndJustification\" w:val=\"1\" /><w:useFELayout /></w:compat>");

        Assert.That(compatibility.SettingCount(), Is.EqualTo(2));
        Assert.That(compatibility.GetSetting(0).Name, Is.EqualTo("compatibilityMode"));
        Assert.That(compatibility.GetSetting(0).Uri, Is.EqualTo("http://schemas.microsoft.com/office/word"));
        Assert.That(compatibility.GetSetting(0).Value, Is.EqualTo("15"));
        Assert.That(compatibility.FindSettingByName("overrideTableStyleFontSizeAndJustification")!.Value, Is.EqualTo("1"));
        Assert.That(compatibility.UnknownChildCount(), Is.EqualTo(1));
        Assert.That(compatibility.GetUnknownChildXml(0), Does.Contain("useFELayout"));
    }

    [Test]
    public void CompatibilitySettingsWriting_WritesSettingsAndUnknownChildren()
    {
        global::docx.DocxCompatibilitySettings compatibility = global::docx.DocxCompatibilitySettings.Create();

        compatibility.AddSetting("compatibilityMode", "http://schemas.microsoft.com/office/word", "15");
        compatibility.SetSetting("overrideTableStyleFontSizeAndJustification", "", "1");
        compatibility.SetSetting("compatibilityMode", "http://schemas.microsoft.com/office/word", "16");
        compatibility.AddUnknownChild(new global::docx.DocxXmlBuffer("<w:useFELayout xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" />"));

        string xml = compatibility.WriteXml();
        global::docx.DocxCompatibilitySettings parsed = ParseCompatibility(xml);

        Assert.That(xml, Does.Contain("w:compat"));
        Assert.That(parsed.SettingCount(), Is.EqualTo(2));
        Assert.That(parsed.FindSettingByName("compatibilityMode")!.Value, Is.EqualTo("16"));
        Assert.That(parsed.UnknownChildCount(), Is.EqualTo(1));
    }

    [Test]
    public void SettingsCompatibility_CanBeCreatedAndCleared()
    {
        global::docx.DocxSettings settings = global::docx.DocxSettings.Create();

        global::docx.DocxCompatibilitySettings compatibility = settings.GetOrCreateCompatibilitySettings();
        compatibility.AddSetting("compatibilityMode", "", "15");

        global::docx.DocxSettings parsed = ParseSettings(settings.WriteXml());

        Assert.That(parsed.HasCompatibilitySettings, Is.True);
        Assert.That(parsed.CompatibilitySettings.FindSettingByName("compatibilityMode")!.Value, Is.EqualTo("15"));

        settings.ClearCompatibilitySettings();
        parsed = ParseSettings(settings.WriteXml());

        Assert.That(parsed.HasCompatibilitySettings, Is.False);
    }

    [Test]
    public void CompatibilitySettings_RejectInvalidArguments()
    {
        global::docx.DocxCompatibilitySettings compatibility = global::docx.DocxCompatibilitySettings.Create();
        global::docx.DocxCompatibilitySetting setting = global::docx.DocxCompatibilitySetting.Create("name", "", "value");
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create("<root />");

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxCompatibilitySettings(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCompatibilitySettings.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCompatibilitySettings.ParseXml(null!));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxCompatibilitySettings.ParseXml("<root />"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCompatibilitySettings.IsSupported(null!));
        Assert.Throws<ArgumentNullException>(() => compatibility.FindSettingByName(null!));
        Assert.Throws<ArgumentException>(() => compatibility.FindSettingByName(""));
        Assert.Throws<ArgumentNullException>(() => compatibility.AddSetting(null!, "", "value"));
        Assert.Throws<ArgumentNullException>(() => compatibility.AddSetting("name", null!, "value"));
        Assert.Throws<ArgumentNullException>(() => compatibility.AddSetting("name", "", null!));
        Assert.Throws<ArgumentNullException>(() => compatibility.SetSetting(null!, "", "value"));
        Assert.Throws<ArgumentNullException>(() => compatibility.SetSetting("name", null!, "value"));
        Assert.Throws<ArgumentNullException>(() => compatibility.SetSetting("name", "", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCompatibilitySettings.ValidateName(null!));
        Assert.Throws<ArgumentException>(() => global::docx.DocxCompatibilitySettings.ValidateName(""));
        Assert.Throws<ArgumentNullException>(() => compatibility.AddUnknownChild(null!));
        Assert.Throws<ArgumentNullException>(() => compatibility.WriteTo(null!));

        Assert.Throws<ArgumentNullException>(() => new global::docx.DocxCompatibilitySetting(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCompatibilitySetting.Create(null!, "", "value"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCompatibilitySetting.Create("name", null!, "value"));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCompatibilitySetting.Create("name", "", null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCompatibilitySetting.Parse(null!));
        Assert.Throws<ArgumentNullException>(() => global::docx.DocxCompatibilitySetting.ParseXml(null!));
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxCompatibilitySetting.ParseXml("<root />"));
        Assert.Throws<ArgumentNullException>(() => setting.SetName(null!));
        Assert.Throws<ArgumentException>(() => setting.SetName(""));
        Assert.Throws<ArgumentNullException>(() => setting.SetUri(null!));
        Assert.Throws<ArgumentNullException>(() => setting.SetValue(null!));
        Assert.Throws<ArgumentNullException>(() => setting.IsName(null!));
        Assert.Throws<ArgumentException>(() => setting.IsName(""));
        Assert.Throws<ArgumentNullException>(() => setting.WriteTo(null!));

        Assert.That(reader.ReadToElement("root", ""), Is.True);
        Assert.Throws<InvalidOperationException>(() => global::docx.DocxCompatibilitySettings.Parse(reader));
    }

    static global::docx.DocxCompatibilitySettings ParseCompatibility(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("compat", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxCompatibilitySettings.Parse(reader);
    }

    static global::docx.DocxSettings ParseSettings(string xml)
    {
        global::docx.DocxXmlReader reader = global::docx.DocxXmlReader.Create(xml);

        Assert.That(reader.ReadToElement("settings", global::docx.DocxConstants.NamespaceWordprocessingMl), Is.True);

        return global::docx.DocxSettings.Parse(reader);
    }
}
