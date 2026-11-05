namespace config.Tests;

public class ConfigOptionsTests
{
    [Test]
    public void CoreEnums_ExposePlannedPublicNames()
    {
        Assert.That(Enum.GetNames<ConfigValueKind>(), Is.EqualTo(new string[] { "Missing", "Null", "String", "Number", "Boolean", "Array", "Object" }));
        Assert.That(Enum.GetNames<ConfigFormat>(), Is.EqualTo(new string[] { "Json", "Ini", "Properties", "DotEnv", "Flat", "Xml" }));
        Assert.That(Enum.GetNames<ConfigOptional>(), Is.EqualTo(new string[] { "Required", "Optional" }));
        Assert.That(Enum.GetNames<ConfigReload>(), Is.EqualTo(new string[] { "None", "OnChange" }));
        Assert.That(Enum.GetNames<ConfigMergeMode>(), Is.EqualTo(new string[] { "Override", "Strict", "Fallback" }));
        Assert.That(Enum.GetNames<ConfigArrayMergeMode>(), Is.EqualTo(new string[] { "Replace", "Append" }));
        Assert.That(Enum.GetNames<ConfigNullMergeMode>(), Is.EqualTo(new string[] { "Replace", "Ignore" }));
        Assert.That(Enum.GetNames<ConfigDuplicateKeyMode>(), Is.EqualTo(new string[] { "Error", "LastWins", "MergeObjects" }));
        Assert.That(Enum.GetNames<ConfigDotEnvExpansionMode>(), Is.EqualTo(new string[] { "Disabled", "Enabled" }));
        Assert.That(Enum.GetNames<ConfigDotEnvMissingValueMode>(), Is.EqualTo(new string[] { "Empty", "Preserve", "Error" }));
        Assert.That(Enum.GetNames<ConfigCaseMode>(), Is.EqualTo(new string[] { "Sensitive", "Insensitive" }));
        Assert.That(Enum.GetNames<ConfigEmptyEnvMode>(), Is.EqualTo(new string[] { "Keep", "Ignore" }));
        Assert.That(Enum.GetNames<ConfigErrorMode>(), Is.EqualTo(new string[] { "Collect", "FailFast" }));
    }

    [Test]
    public void CoreEnums_DefaultToConservativeV1Behavior()
    {
        Assert.That((int)ConfigValueKind.Missing, Is.EqualTo(0));
        Assert.That((int)ConfigFormat.Json, Is.EqualTo(0));
        Assert.That((int)ConfigOptional.Required, Is.EqualTo(0));
        Assert.That((int)ConfigReload.None, Is.EqualTo(0));
        Assert.That((int)ConfigMergeMode.Override, Is.EqualTo(0));
        Assert.That((int)ConfigArrayMergeMode.Replace, Is.EqualTo(0));
        Assert.That((int)ConfigNullMergeMode.Replace, Is.EqualTo(0));
        Assert.That((int)ConfigDuplicateKeyMode.Error, Is.EqualTo(0));
        Assert.That((int)ConfigDotEnvExpansionMode.Disabled, Is.EqualTo(0));
        Assert.That((int)ConfigDotEnvMissingValueMode.Empty, Is.EqualTo(0));
        Assert.That((int)ConfigCaseMode.Sensitive, Is.EqualTo(0));
        Assert.That((int)ConfigEmptyEnvMode.Keep, Is.EqualTo(0));
        Assert.That((int)ConfigErrorMode.Collect, Is.EqualTo(0));
    }
}
