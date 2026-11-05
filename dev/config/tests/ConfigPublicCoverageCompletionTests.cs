using System;

namespace config.Tests;

public class ConfigPublicCoverageCompletionTests
{
    [Test]
    public void PublicBindingAndDefaultReads_CoverSuccessfulRootMappedAndPresentValuePaths()
    {
        Config config = new ConfigBuilder()
            .AddString("{\"Name\":\"direct\",\"alias\":\"mapped\",\"Count\":42,\"Csv\":\"one,two\"}", ConfigFormat.Json, "coverage.json")
            .Build();
        ConfigFieldMap fieldMap = new ConfigFieldMap().Add("Name", "alias");

        PublicCoverageBindingTarget direct = (PublicCoverageBindingTarget)config.Bind(typeof(PublicCoverageBindingTarget));
        PublicCoverageBindingTarget mapped = (PublicCoverageBindingTarget)config.BindMapped(typeof(PublicCoverageBindingTarget), fieldMap);
        PublicCoverageBindingTarget existing = new PublicCoverageBindingTarget();
        config.BindMappedInstance(existing, fieldMap);

        Assert.Multiple(() =>
        {
            Assert.That(direct.Name, Is.EqualTo("direct"));
            Assert.That(direct.Count, Is.EqualTo(42));
            Assert.That(mapped.Name, Is.EqualTo("mapped"));
            Assert.That(existing.Name, Is.EqualTo("mapped"));
            Assert.That(fieldMap.Get("Name"), Is.EqualTo("alias"));
            Assert.That(fieldMap.Has("missing"), Is.False);
            Assert.That(fieldMap.Get("missing"), Is.Empty);
            Assert.That(config.GetStringOrDefault("Count", "fallback"), Is.EqualTo("42"));
            Assert.That(config.GetStringArrayOrDefault("Csv", new string[] { "fallback" }), Is.EqualTo(new string[] { "one", "two" }));
        });
    }

    [Test]
    public void PublicBuilderContracts_RejectNullOptionsAndExerciseCurrentDirectoryConventionalOverload()
    {
        Assert.Throws<ArgumentNullException>(() => new ConfigBuilder().SetSourceGroupLabels(null!));
        Assert.Throws<ArgumentNullException>(() => new ConfigBuilder().SetResolutionOptions(null!));

        string key = "coverage" + Guid.NewGuid().ToString("N");
        ConfigMap defaults = new ConfigMap();
        defaults.Set(key, new ConfigValues().String("from-defaults"));

        Config config = new ConfigBuilder()
            .AddDefaultAppSourcesWithDefaults(defaults, "", new string[0])
            .Build();

        Assert.That(config.GetString(key), Is.EqualTo("from-defaults"));
    }

    [Test]
    public void PublicEnvironmentMap_ConvertsStringAndNonStringDataFields()
    {
        ConfigValues values = new ConfigValues();
        ConfigMap environment = new ConfigMap();
        environment.Set("APP_name", values.String("service"));
        environment.Set("APP_count", values.Number(12m));

        Config config = new ConfigBuilder()
            .AddEnvironmentMap(environment, "APP_", "__")
            .Build();

        Assert.Multiple(() =>
        {
            Assert.That(config.GetString("name"), Is.EqualTo("service"));
            Assert.That(config.GetInt("count"), Is.EqualTo(12));
        });
    }

    [Test]
    public void PublicMapsPathsAndValues_HandleEmptyPathsShorterComparandsAndInvalidContainers()
    {
        ConfigValues values = new ConfigValues();
        ConfigMap map = new ConfigMap();
        ConfigValue invalidObject = values.Object(new ConfigMap());
        invalidObject.ObjectValue = null;
        ConfigValue invalidArray = values.Array(new ConfigArray());
        invalidArray.ArrayValue = null;
        ConfigValue invalidString = values.String("value");
        invalidString.StringValue = null;
        map.Set("broken", invalidObject);
        map.SetPath("", values.String("ignored"), ".");
        IConfigPath paths = new ConfigPath();

        Assert.Multiple(() =>
        {
            Assert.That(map.Count(), Is.EqualTo(1));
            Assert.That(map.GetPath("broken.child", ".").Kind, Is.EqualTo(ConfigValueKind.Missing));
            Assert.That(map.ChildKeys("broken", "."), Is.Empty);
            Assert.That(paths.Compare("service.host", "service", ".", ConfigCaseMode.Sensitive), Is.GreaterThan(0));
            Assert.That(values.ToDisplayString(invalidObject), Is.EqualTo("(invalid)"));
            Assert.That(values.ToDisplayString(invalidArray), Is.EqualTo("(invalid)"));
            Assert.That(values.ToDisplayString(invalidString), Is.EqualTo("\"\""));
        });
    }

    [Test]
    public void PublicCustomSource_CanExposeAnInvalidObjectWithoutCrashingSnapshotReads()
    {
        Config config = new ConfigBuilder()
            .AddSource(new PublicCoverageInvalidObjectSource())
            .Build();

        Assert.Multiple(() =>
        {
            Assert.That(config.Value("").Kind, Is.EqualTo(ConfigValueKind.Object));
            Assert.That(config.Value("child").Kind, Is.EqualTo(ConfigValueKind.Missing));
            Assert.That(config.Children(""), Is.Empty);
            Assert.That(config.Keys(), Is.Empty);
            Assert.That(config.Section("child").Count(), Is.EqualTo(0));
        });
    }

    [Test]
    public void PublicDiagnostics_FormatUnknownNamedAndLocatedErrorsAndWarnings()
    {
        ConfigDiagnostics diagnostics = new ConfigDiagnostics();
        diagnostics.AddError("named", "", "plain error", 0, 0);
        diagnostics.AddWarning("", "", "unknown warning", 0, 0);
        diagnostics.AddWarning("located", "path", "located warning", 2, 3);

        Assert.That(
            diagnostics.ToDisplayString(),
            Is.EqualTo("error named (root): plain error\nwarning (unknown) (root): unknown warning\nwarning located:2:3 path: located warning"));
    }

    [Test]
    public void PublicValidation_AcceptsEveryConvertibleShapeAndNumericBoundaryForm()
    {
        ConfigValues values = new ConfigValues();
        ConfigMap map = new ConfigMap();
        ConfigArray array = new ConfigArray();
        array.Add(values.String("item"));
        map.Set("boolean", values.Boolean(true));
        map.Set("number", values.Number(1m));
        map.Set("false", values.Boolean(false));
        map.Set("integerFraction", values.Number(1.5m));
        map.Set("integerOverflow", values.Number(2147483648m));
        map.Set("longFraction", values.Number(1.5m));
        map.Set("longOverflow", values.Number(9223372036854775808m));
        map.Set("array", values.Array(array));
        map.Set("string", values.String("item"));
        Config config = new ConfigBuilder().AddMemory(map, "validation-memory").Build();

        ConfigValidationResult accepted = new ConfigValidator()
            .Type("boolean", ConfigValidationValueType.Boolean)
            .Type("number", ConfigValidationValueType.Boolean)
            .Type("number", ConfigValidationValueType.Integer)
            .Type("number", ConfigValidationValueType.Long)
            .Type("number", ConfigValidationValueType.Double)
            .Type("boolean", ConfigValidationValueType.Double)
            .Type("array", ConfigValidationValueType.StringArray)
            .Type("string", ConfigValidationValueType.StringArray)
            .Type("number", ConfigValidationValueType.StringArray)
            .Type("boolean", ConfigValidationValueType.StringArray)
            .Range("boolean", 1m, 1m)
            .Range("false", 0m, 0m)
            .Validate(config);
        ConfigValidationResult rejected = new ConfigValidator()
            .Type("integerFraction", ConfigValidationValueType.Integer)
            .Type("integerOverflow", ConfigValidationValueType.Integer)
            .Type("longFraction", ConfigValidationValueType.Long)
            .Type("longOverflow", ConfigValidationValueType.Long)
            .Validate(config);

        Assert.Multiple(() =>
        {
            Assert.That(accepted.IsValid, Is.True);
            Assert.That(accepted.ErrorCount, Is.EqualTo(0));
            Assert.That(rejected.IsValid, Is.False);
            Assert.That(rejected.ErrorCount, Is.EqualTo(4));
        });
    }

    [Test]
    public void PublicValidationRuleFields_CanDisableEachRangeAndLengthBoundary()
    {
        Config config = new ConfigBuilder()
            .AddString("{\"number\":100,\"text\":\"long\",\"items\":[1,2,3]}", ConfigFormat.Json, "inactive-boundaries.json")
            .Build();
        ConfigValidationRule range = new ConfigValidator().Range("number", 0m, 1m).Rule(0);
        range.HasMinNumber = false;
        range.HasMaxNumber = false;
        ConfigValidationRule length = new ConfigValidator().Length("text", 0, 1).Rule(0);
        length.HasMinLength = false;
        length.HasMaxLength = false;
        ConfigValidationRule arrayLength = new ConfigValidator().ArrayLength("items", 0, 1).Rule(0);
        arrayLength.HasMinLength = false;
        arrayLength.HasMaxLength = false;

        ConfigValidationResult result = new ConfigValidator()
            .Add(range)
            .Add(length)
            .Add(arrayLength)
            .Validate(config);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.ErrorCount, Is.EqualTo(0));
        });
    }

    [Test]
    public void PublicValidation_ReportsEveryTypeNameInvalidEnumAndNullableValidatorResult()
    {
        Config config = new ConfigBuilder()
            .AddString("{\"object\":{},\"scalar\":1}", ConfigFormat.Json, "validation-types.json")
            .Build();
        ConfigValidator validator = new ConfigValidator()
            .Register(new PublicCoverageNullResultValidator())
            .Custom("scalar", "null-result")
            .Type("object", ConfigValidationValueType.Any)
            .Type("object", ConfigValidationValueType.String)
            .Type("object", ConfigValidationValueType.Boolean)
            .Type("object", ConfigValidationValueType.Integer)
            .Type("object", ConfigValidationValueType.Long)
            .Type("object", ConfigValidationValueType.Decimal)
            .Type("object", ConfigValidationValueType.Double)
            .Type("object", ConfigValidationValueType.TimeSpan)
            .Type("object", ConfigValidationValueType.ByteSize)
            .Type("object", ConfigValidationValueType.StringArray)
            .Type("object", ConfigValidationValueType.Array)
            .Type("object", ConfigValidationValueType.Object)
            .Type("scalar", ConfigValidationValueType.Object)
            .Type("scalar", (ConfigValidationValueType)999);

        ConfigValidationResult result = validator.Validate(config);
        ArgumentException? nullName = Assert.Throws<ArgumentException>(() => new ConfigValidator().Register(new PublicCoverageNullNameValidator()));
        Assert.Throws<ArgumentNullException>(() => new ConfigValidator().Register(null!));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorCount, Is.EqualTo(12));
            Assert.That(result.Errors[0].Message, Is.EqualTo("expected string value"));
            Assert.That(result.Errors[4].Message, Is.EqualTo("expected decimal value"));
            Assert.That(result.Errors[7].Message, Is.EqualTo("expected byte-size value"));
            Assert.That(result.Errors[8].Message, Is.EqualTo("expected string array value"));
            Assert.That(result.Errors[9].Message, Is.EqualTo("expected array value"));
            Assert.That(result.Errors[10].Message, Is.EqualTo("expected object value"));
            Assert.That(result.Errors[11].Message, Is.EqualTo("expected configured value"));
            Assert.That(nullName, Is.Not.Null);
            Assert.That(nullName!.ParamName, Is.EqualTo("validator"));
        });
    }

    [Test]
    public void PublicReloadStart_IgnoresMemoryAndCustomSourcesWithoutCreatingWatchers()
    {
        ConfigMap memory = new ConfigMap();
        memory.Set("memory", new ConfigValues().String("value"));
        ConfigReloadController controller = new ConfigBuilder()
            .AddMemory(memory, "memory")
            .AddSource(new PublicCoverageStableSource())
            .BuildReloading();

        controller.Start();
        Assert.That(controller.IsWatching(), Is.True);
        controller.Stop();

        Assert.That(controller.IsWatching(), Is.False);
        Assert.That(controller.Current().GetString("custom"), Is.EqualTo("value"));
    }
}

public class PublicCoverageBindingTarget
{
    public string Name = "initial";
    public int Count;
}

public class PublicCoverageInvalidObjectSource : IConfigSource
{
    public ConfigSourceResult Load(int precedenceRank)
    {
        ConfigValues values = new ConfigValues();
        ConfigValue root = values.Object(new ConfigMap());
        root.ObjectValue = null;
        return new ConfigSourceResult(root, new ConfigDiagnostics());
    }
}

public class PublicCoverageStableSource : IConfigSource
{
    public ConfigSourceResult Load(int precedenceRank)
    {
        ConfigValues values = new ConfigValues();
        ConfigMap map = new ConfigMap();
        map.Set("custom", values.String("value"));
        return new ConfigSourceResult(values.Object(map), new ConfigDiagnostics());
    }
}

public class PublicCoverageNullNameValidator : IConfigNamedValidator
{
    public string Name()
    {
        return null!;
    }

    public string Validate(ConfigValue value)
    {
        return "unused";
    }
}

public class PublicCoverageNullResultValidator : IConfigNamedValidator
{
    public string Name()
    {
        return "null-result";
    }

    public string Validate(ConfigValue value)
    {
        return null!;
    }
}
