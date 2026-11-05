using System.Text;

namespace config.Tests;

public class ConfigEncodingSourceTests
{
    [Test]
    public void GeneralFileEntryPoints_LoadExplicitDetectedAndFlatFormats()
    {
        string directory = NewDirectory("general-files");
        string explicitPath = Path.Combine(directory, "explicit.custom");
        string detectedPath = Path.Combine(directory, "detected.flat");
        string flatPath = Path.Combine(directory, "convenience.txt");
        File.WriteAllText(explicitPath, "explicit.value=one\n", Encoding.UTF8);
        File.WriteAllText(detectedPath, "detected.value=two\n", Encoding.UTF8);
        File.WriteAllText(flatPath, "flat.value=three\n", Encoding.UTF8);

        Config config = new ConfigBuilder()
            .AddFile(explicitPath, ConfigFormat.Properties, ConfigOptional.Required, ConfigReload.None)
            .AddFileByExtension(detectedPath, ConfigOptional.Required, ConfigReload.None)
            .AddFlatFile(flatPath, ConfigOptional.Required, ConfigReload.None)
            .Build();

        Assert.That(config.Diagnostics().HasErrors(), Is.False);
        Assert.That(config.GetString("explicit.value"), Is.EqualTo("one"));
        Assert.That(config.GetString("detected.value"), Is.EqualTo("two"));
        Assert.That(config.GetString("flat.value"), Is.EqualTo("three"));
    }

    [Test]
    public void DefaultFileLikeSources_ReadUtf8Text()
    {
        string directory = NewDirectory("utf8-defaults");
        string filePath = Path.Combine(directory, "file.flat");
        string directoryPath = Path.Combine(directory, "files");
        string secretsPath = Path.Combine(directory, "secrets");
        Directory.CreateDirectory(directoryPath);
        Directory.CreateDirectory(secretsPath);
        File.WriteAllText(filePath, "file.message=café\n", Encoding.UTF8);
        File.WriteAllText(Path.Combine(directoryPath, "values.flat"), "directory.message=東京\n", Encoding.UTF8);
        File.WriteAllText(Path.Combine(secretsPath, "secret"), "påssword", Encoding.UTF8);
        MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes("stream.message=γειά\n"));

        Config config = new ConfigBuilder()
            .AddFile(filePath, ConfigFormat.Flat, ConfigOptional.Required, ConfigReload.None)
            .AddStream(stream, ConfigFormat.Flat, "stream")
            .AddDirectory(directoryPath, "*.flat", ConfigOptional.Required)
            .AddKeyPerFileDirectory(secretsPath, ConfigOptional.Required)
            .Build();

        Assert.That(config.Diagnostics().HasErrors(), Is.False);
        Assert.That(config.GetString("file.message"), Is.EqualTo("café"));
        Assert.That(config.GetString("stream.message"), Is.EqualTo("γειά"));
        Assert.That(config.GetString("directory.message"), Is.EqualTo("東京"));
        Assert.That(config.GetString("secret"), Is.EqualTo("påssword"));
    }

    [Test]
    public void EncodedFileLikeSources_ReadCallerSelectedEncoding()
    {
        string directory = NewDirectory("selected-encoding");
        string explicitPath = Path.Combine(directory, "explicit.custom");
        string detectedPath = Path.Combine(directory, "detected.flat");
        string directoryPath = Path.Combine(directory, "files");
        string secretsPath = Path.Combine(directory, "secrets");
        Directory.CreateDirectory(directoryPath);
        Directory.CreateDirectory(secretsPath);
        WriteWithoutPreamble(explicitPath, "explicit.message=français\n", Encoding.Unicode);
        WriteWithoutPreamble(detectedPath, "detected.message=español\n", Encoding.Unicode);
        WriteWithoutPreamble(Path.Combine(directoryPath, "values.flat"), "directory.message=日本語\n", Encoding.Unicode);
        WriteWithoutPreamble(Path.Combine(secretsPath, "token"), "sëcret", Encoding.Unicode);
        MemoryStream stream = new MemoryStream(Encoding.Unicode.GetBytes("stream.message=привет\n"));

        Config config = new ConfigBuilder()
            .AddFileWithEncoding(explicitPath, ConfigFormat.Flat, ConfigOptional.Required, ConfigReload.None, Encoding.Unicode)
            .AddFileByExtensionWithEncoding(detectedPath, ConfigOptional.Required, ConfigReload.None, Encoding.Unicode)
            .AddStreamWithEncoding(stream, ConfigFormat.Flat, "stream", Encoding.Unicode)
            .AddDirectoryWithEncoding(directoryPath, "*.flat", ConfigOptional.Required, Encoding.Unicode)
            .AddKeyPerFileDirectoryWithEncoding(secretsPath, ConfigOptional.Required, Encoding.Unicode)
            .Build();

        Assert.That(config.Diagnostics().HasErrors(), Is.False);
        Assert.That(config.GetString("explicit.message"), Is.EqualTo("français"));
        Assert.That(config.GetString("detected.message"), Is.EqualTo("español"));
        Assert.That(config.GetString("stream.message"), Is.EqualTo("привет"));
        Assert.That(config.GetString("directory.message"), Is.EqualTo("日本語"));
        Assert.That(config.GetString("token"), Is.EqualTo("sëcret"));
    }

    [Test]
    public void EncodedDetectedFile_PropagatesEncodingThroughRecursiveIncludes()
    {
        string directory = NewDirectory("encoded-includes");
        string mainPath = Path.Combine(directory, "main.json");
        string firstPath = Path.Combine(directory, "first.json");
        string secondPath = Path.Combine(directory, "second.flat");
        WriteWithoutPreamble(secondPath, "included.message=olá\n", Encoding.Unicode);
        WriteWithoutPreamble(firstPath, "{\"include\":\"second.flat\",\"first\":\"première\"}", Encoding.Unicode);
        WriteWithoutPreamble(mainPath, "{\"include\":\"first.json\",\"main\":\"münchen\"}", Encoding.Unicode);

        Config config = new ConfigBuilder()
            .AddFileByExtensionWithEncoding(mainPath, ConfigOptional.Required, ConfigReload.None, Encoding.Unicode)
            .Build();

        Assert.That(config.Diagnostics().HasErrors(), Is.False);
        Assert.That(config.GetString("main"), Is.EqualTo("münchen"));
        Assert.That(config.GetString("first"), Is.EqualTo("première"));
        Assert.That(config.GetString("included.message"), Is.EqualTo("olá"));
        Assert.That(config.Value("included.message").SourceName, Is.EqualTo(secondPath));
    }

    [Test]
    public void ReloadController_PreservesSelectedFileEncoding()
    {
        string directory = NewDirectory("encoded-reload");
        string path = Path.Combine(directory, "settings.flat");
        WriteWithoutPreamble(path, "message=first café\n", Encoding.Unicode);
        ConfigReloadController controller = new ConfigBuilder()
            .AddFileWithEncoding(path, ConfigFormat.Flat, ConfigOptional.Required, ConfigReload.None, Encoding.Unicode)
            .BuildReloading();

        WriteWithoutPreamble(path, "message=second café\n", Encoding.Unicode);
        Config reloaded = controller.Reload();

        Assert.That(reloaded.Diagnostics().HasErrors(), Is.False);
        Assert.That(reloaded.GetString("message"), Is.EqualTo("second café"));
    }

    [Test]
    public void EncodedEntryPoints_RejectNullEncoding()
    {
        MemoryStream stream = new MemoryStream();

        Assert.Throws<ArgumentNullException>(() => new ConfigBuilder().AddFileWithEncoding("file", ConfigFormat.Json, ConfigOptional.Required, ConfigReload.None, null!));
        Assert.Throws<ArgumentNullException>(() => new ConfigBuilder().AddFileByExtensionWithEncoding("file.json", ConfigOptional.Required, ConfigReload.None, null!));
        Assert.Throws<ArgumentNullException>(() => new ConfigBuilder().AddStreamWithEncoding(stream, ConfigFormat.Json, "stream", null!));
        Assert.Throws<ArgumentNullException>(() => new ConfigBuilder().AddDirectoryWithEncoding("files", "*", ConfigOptional.Required, null!));
        Assert.Throws<ArgumentNullException>(() => new ConfigBuilder().AddKeyPerFileDirectoryWithEncoding("files", ConfigOptional.Required, null!));
    }

    private static string NewDirectory(string label)
    {
        string path = Path.Combine(TestContext.CurrentContext.WorkDirectory, "config-" + label + "-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        return path;
    }

    private static void WriteWithoutPreamble(string path, string text, Encoding encoding)
    {
        File.WriteAllBytes(path, encoding.GetBytes(text));
    }
}
