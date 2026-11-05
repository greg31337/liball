namespace config.Tests;

public class ConfigInterfaceSourceTests
{
    [Test]
    public void SourceInterface_RemainsFocusedAndPolicyConformant()
    {
        Type contract = typeof(IConfigSource);
        System.Reflection.MethodInfo[] methods = contract.GetMethods();

        Assert.That(contract.IsInterface, Is.True);
        Assert.That(contract.Name, Does.Match("^I[A-Z]"));
        Assert.That(contract.GetInterfaces(), Is.Empty);
        Assert.That(contract.GetFields(), Is.Empty);
        Assert.That(contract.GetProperties(), Is.Empty);
        Assert.That(contract.GetEvents(), Is.Empty);
        Assert.That(methods, Has.Length.EqualTo(1));
        Assert.That(methods[0].Name, Is.EqualTo("Load"));
        Assert.That(methods[0].IsStatic, Is.False);
        Assert.That(methods[0].IsGenericMethod, Is.False);
    }

    [Test]
    public void AddSource_LoadsInterfaceSourcesInRegistrationOrder()
    {
        TestSequenceSource first = new TestSequenceSource("first", false);
        TestSequenceSource second = new TestSequenceSource("second", false);

        Config config = Config.CreateBuilder()
            .AddSource(first)
            .AddSource(second)
            .Build();

        Assert.That(first.LastPrecedenceRank, Is.EqualTo(1));
        Assert.That(second.LastPrecedenceRank, Is.EqualTo(2));
        Assert.That(config.GetString("service.host"), Is.EqualTo("second"));
        Assert.That(config.Value("service.host").SourceName, Is.EqualTo("test-source"));
        Assert.That(config.Value("service.host").PrecedenceRank, Is.EqualTo(2));
        Assert.That(config.Diagnostics().WarningCount(), Is.EqualTo(2));
    }

    [Test]
    public void AddSource_RejectsNullSources()
    {
        Assert.That(
            () => Config.CreateBuilder().AddSource(null!),
            Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void Reload_ReevaluatesInterfaceSourceWithoutFileSystemState()
    {
        TestSequenceSource source = new TestSequenceSource("first", false);
        source.NextValue = "second";
        ConfigReloadController controller = Config.CreateBuilder()
            .AddSource(source)
            .BuildReloading();

        Config reloaded = controller.Reload();

        Assert.That(source.LoadCount, Is.EqualTo(2));
        Assert.That(reloaded.GetString("service.host"), Is.EqualTo("second"));
    }

    [Test]
    public void Reload_InterfaceSourceErrorPreservesPreviousSnapshot()
    {
        TestSequenceSource source = new TestSequenceSource("first", true);
        source.NextValue = "invalid";
        ConfigReloadController controller = Config.CreateBuilder()
            .AddSource(source)
            .BuildReloading();

        Config reloaded = controller.Reload();

        Assert.That(reloaded.GetString("service.host"), Is.EqualTo("first"));
        Assert.That(controller.LastDiagnostics().ErrorCount(), Is.EqualTo(1));
        Assert.That(controller.LastDiagnostics().Errors[0].Message, Is.EqualTo("simulated source failure"));
    }
}

public class TestSequenceSource : IConfigSource
{
    public string FirstValue;
    public string NextValue;
    public bool FailAfterFirstLoad;
    public int LoadCount;
    public int LastPrecedenceRank;

    public TestSequenceSource(string firstValue, bool failAfterFirstLoad)
    {
        FirstValue = firstValue;
        NextValue = firstValue;
        FailAfterFirstLoad = failAfterFirstLoad;
        LoadCount = 0;
        LastPrecedenceRank = 0;
    }

    public ConfigSourceResult Load(int precedenceRank)
    {
        string text = FirstValue;
        if (LoadCount > 0)
        {
            text = NextValue;
        }

        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        if (FailAfterFirstLoad && LoadCount > 0)
        {
            diagnostics.AddError("test-source", "service.host", "simulated source failure", 0, 0);
        }
        else
        {
            diagnostics.AddWarning("test-source", "service.host", "simulated source warning", 0, 0);
        }

        ConfigValue value = ConfigValue.String(text).SetSource("test-source", precedenceRank, "service.host", 0, 0);
        ConfigMap map = new ConfigMap();
        map.SetPathDefault("service.host", value);
        ConfigValue root = ConfigValue.Object(map).SetSource("test-source", precedenceRank, "", 0, 0);
        LoadCount = LoadCount + 1;
        LastPrecedenceRank = precedenceRank;
        return new ConfigSourceResult(root, diagnostics);
    }
}
