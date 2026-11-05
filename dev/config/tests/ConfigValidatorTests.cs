namespace config.Tests;

public class ConfigValidatorTests
{
    [Test]
    public void RuleFactories_CreateAllInspectableRuleKinds()
    {
        ConfigValidationRule required = ConfigValidationRule.Required("service.host");
        ConfigValidationRule type = ConfigValidationRule.Type("service.enabled", ConfigValidationValueType.Boolean);
        ConfigValidationRule range = ConfigValidationRule.Range("service.port", 1000m, 9999m);
        ConfigValidationRule length = ConfigValidationRule.Length("service.name", 3, 20);
        ConfigValidationRule arrayLength = ConfigValidationRule.ArrayLength("service.hosts", 1, 4);
        ConfigValidationRule allowed = ConfigValidationRule.Allowed("service.mode", new string[] { "dev", "prod" });
        ConfigValidationRule named = ConfigValidationRule.Named("service.host", "host-name");

        Assert.That(required.Kind, Is.EqualTo(ConfigValidationRuleKind.Required));
        Assert.That(required.Path, Is.EqualTo("service.host"));
        Assert.That(type.Kind, Is.EqualTo(ConfigValidationRuleKind.Type));
        Assert.That(type.ValueType, Is.EqualTo(ConfigValidationValueType.Boolean));
        Assert.That(range.Kind, Is.EqualTo(ConfigValidationRuleKind.NumericRange));
        Assert.That(range.MinNumber, Is.EqualTo(1000m));
        Assert.That(range.MaxNumber, Is.EqualTo(9999m));
        Assert.That(range.HasMinNumber, Is.True);
        Assert.That(range.HasMaxNumber, Is.True);
        Assert.That(length.Kind, Is.EqualTo(ConfigValidationRuleKind.StringLength));
        Assert.That(length.MinLength, Is.EqualTo(3));
        Assert.That(length.MaxLength, Is.EqualTo(20));
        Assert.That(length.HasMinLength, Is.True);
        Assert.That(length.HasMaxLength, Is.True);
        Assert.That(arrayLength.Kind, Is.EqualTo(ConfigValidationRuleKind.ArrayLength));
        Assert.That(arrayLength.Path, Is.EqualTo("service.hosts"));
        Assert.That(arrayLength.MinLength, Is.EqualTo(1));
        Assert.That(arrayLength.MaxLength, Is.EqualTo(4));
        Assert.That(allowed.Kind, Is.EqualTo(ConfigValidationRuleKind.AllowedValues));
        Assert.That(allowed.AllowedValues, Is.EqualTo(new string[] { "dev", "prod" }));
        Assert.That(named.Kind, Is.EqualTo(ConfigValidationRuleKind.Named));
        Assert.That(named.ValidatorName, Is.EqualTo("host-name"));
    }

    [Test]
    public void ValidatorMethods_AppendRulesAndCompositeRequiredTypeRules()
    {
        ConfigValidator validator = new ConfigValidator();

        validator
            .Require("service.host")
            .RequireString("service.name")
            .RequireInt("service.port")
            .Range("service.port", 1m, 65535m)
            .Length("service.name", 1, 40)
            .ArrayLength("service.hosts", 1, 4)
            .Allowed("service.mode", new string[] { "dev", "prod" })
            .Custom("service.host", "host-name");

        Assert.That(validator.RuleCount(), Is.EqualTo(10));
        Assert.That(validator.Rule(0).Kind, Is.EqualTo(ConfigValidationRuleKind.Required));
        Assert.That(validator.Rule(1).Kind, Is.EqualTo(ConfigValidationRuleKind.Required));
        Assert.That(validator.Rule(2).Kind, Is.EqualTo(ConfigValidationRuleKind.Type));
        Assert.That(validator.Rule(2).ValueType, Is.EqualTo(ConfigValidationValueType.String));
        Assert.That(validator.Rule(3).Kind, Is.EqualTo(ConfigValidationRuleKind.Required));
        Assert.That(validator.Rule(4).Kind, Is.EqualTo(ConfigValidationRuleKind.Type));
        Assert.That(validator.Rule(4).ValueType, Is.EqualTo(ConfigValidationValueType.Integer));
        Assert.That(validator.Rule(5).Kind, Is.EqualTo(ConfigValidationRuleKind.NumericRange));
        Assert.That(validator.Rule(6).Kind, Is.EqualTo(ConfigValidationRuleKind.StringLength));
        Assert.That(validator.Rule(7).Kind, Is.EqualTo(ConfigValidationRuleKind.ArrayLength));
        Assert.That(validator.Rule(8).Kind, Is.EqualTo(ConfigValidationRuleKind.AllowedValues));
        Assert.That(validator.Rule(9).Kind, Is.EqualTo(ConfigValidationRuleKind.Named));
    }

    [Test]
    public void Validator_CopiesRulesAndAllowedValues()
    {
        string[] values = new string[] { "dev", "prod" };
        ConfigValidationRule allowed = ConfigValidationRule.Allowed("service.mode", values);
        ConfigValidator validator = new ConfigValidator();

        validator.Add(allowed);
        values[0] = "changed-input";
        allowed.AllowedValues[1] = "changed-rule";
        ConfigValidationRule returned = validator.Rule(0);
        returned.AllowedValues[0] = "changed-returned";
        ConfigValidationRule[] rules = validator.ToArray();
        rules[0].AllowedValues[1] = "changed-array";

        ConfigValidationRule stored = validator.Rule(0);
        Assert.That(stored.AllowedValues, Is.EqualTo(new string[] { "dev", "prod" }));
        Assert.That(validator.Rule(-1).Path, Is.EqualTo(""));
        Assert.That(validator.Rule(99).Path, Is.EqualTo(""));
    }

    [Test]
    public void Validate_ReturnsValidResultWhenRulesPass()
    {
        Config config = Config.CreateBuilder()
            .AddString(
                "{ \"service\": { \"host\": \"localhost\", \"port\": \"5432\", \"name\": \"api\", \"mode\": \"prod\", \"enabled\": \"yes\" } }",
                ConfigFormat.Json,
                "json")
            .Build();
        ConfigValidator validator = new ConfigValidator();
        validator
            .RequireString("service.host")
            .RequireInt("service.port")
            .Range("service.port", 1m, 65535m)
            .Length("service.name", 2, 20)
            .Allowed("service.mode", new string[] { "dev", "prod" })
            .Type("service.enabled", ConfigValidationValueType.Boolean);

        ConfigValidationResult result = validator.Validate(config);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorCount, Is.EqualTo(0));
    }

    [Test]
    public void Validate_TimeSpanAndByteSizeTypesRejectOverflowingValues()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("duration.valid", ConfigValue.String("1d"));
        values.SetPathDefault("duration.overflow", ConfigValue.Number(decimal.MaxValue));
        values.SetPathDefault("size.valid", ConfigValue.String("9223372036854775807"));
        values.SetPathDefault("size.overflow", ConfigValue.String("9223372036854775808"));
        Config config = Config.CreateBuilder().AddMemory(values, "memory").Build();
        ConfigValidator validator = new ConfigValidator();
        validator
            .Type("duration.valid", ConfigValidationValueType.TimeSpan)
            .Type("duration.overflow", ConfigValidationValueType.TimeSpan)
            .Type("size.valid", ConfigValidationValueType.ByteSize)
            .Type("size.overflow", ConfigValidationValueType.ByteSize);

        ConfigValidationResult result = validator.Validate(config);

        Assert.That(result.ErrorCount, Is.EqualTo(2));
        Assert.That(result.Diagnostics.Errors[0].Path, Is.EqualTo("duration.overflow"));
        Assert.That(result.Diagnostics.Errors[1].Path, Is.EqualTo("size.overflow"));
    }

    [Test]
    public void Validate_CollectsAllRuleFailuresByDefault()
    {
        Config config = Config.CreateBuilder()
            .AddString(
                "{ \"service\": { \"port\": \"70000\", \"name\": \"a\", \"mode\": \"staging\", \"enabled\": \"maybe\" } }",
                ConfigFormat.Json,
                "json")
            .Build();
        ConfigValidator validator = new ConfigValidator();
        validator
            .Require("service.host")
            .Type("service.enabled", ConfigValidationValueType.Boolean)
            .Range("service.port", 1m, 65535m)
            .Length("service.name", 2, 20)
            .Allowed("service.mode", new string[] { "dev", "prod" });

        ConfigValidationResult result = validator.Validate(config);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorCount, Is.EqualTo(5));
        Assert.That(result.Diagnostics.Errors[0].Path, Is.EqualTo("service.host"));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("missing required value"));
        Assert.That(result.Diagnostics.Errors[1].Path, Is.EqualTo("service.enabled"));
        Assert.That(result.Diagnostics.Errors[1].Message, Is.EqualTo("expected boolean value"));
        Assert.That(result.Diagnostics.Errors[2].Path, Is.EqualTo("service.port"));
        Assert.That(result.Diagnostics.Errors[2].Message, Is.EqualTo("numeric value outside range"));
        Assert.That(result.Diagnostics.Errors[3].Path, Is.EqualTo("service.name"));
        Assert.That(result.Diagnostics.Errors[3].Message, Is.EqualTo("string length outside range"));
        Assert.That(result.Diagnostics.Errors[4].Path, Is.EqualTo("service.mode"));
        Assert.That(result.Diagnostics.Errors[4].Message, Is.EqualTo("value is not allowed"));
    }

    [Test]
    public void Validate_RequiredRulesRejectMissingAndNullValues()
    {
        ConfigMap values = new ConfigMap();
        values.SetPathDefault("present", ConfigValue.String("value"));
        values.SetPathDefault("nullValue", ConfigValue.Null());
        Config config = Config.CreateBuilder()
            .AddMemory(values, "memory")
            .Build();
        ConfigValidator validator = new ConfigValidator();
        validator
            .Require("missing")
            .Require("nullValue")
            .Require("present");

        ConfigValidationResult result = validator.Validate(config);

        Assert.That(result.ErrorCount, Is.EqualTo(2));
        Assert.That(result.Diagnostics.Errors[0].Path, Is.EqualTo("missing"));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("missing required value"));
        Assert.That(result.Diagnostics.Errors[1].Path, Is.EqualTo("nullValue"));
        Assert.That(result.Diagnostics.Errors[1].Message, Is.EqualTo("missing required value"));
    }

    [Test]
    public void Validate_RangeRulesReportBelowAboveAndNonNumericValues()
    {
        Config config = Config.CreateBuilder()
            .AddString(
                "{ \"low\": 0, \"high\": 11, \"text\": \"not-a-number\", \"ok\": \"5\" }",
                ConfigFormat.Json,
                "json")
            .Build();
        ConfigValidator validator = new ConfigValidator();
        validator
            .Range("low", 1m, 10m)
            .Range("high", 1m, 10m)
            .Range("text", 1m, 10m)
            .Range("ok", 1m, 10m);

        ConfigValidationResult result = validator.Validate(config);

        Assert.That(result.ErrorCount, Is.EqualTo(3));
        Assert.That(result.Diagnostics.Errors[0].Path, Is.EqualTo("low"));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("numeric value outside range"));
        Assert.That(result.Diagnostics.Errors[1].Path, Is.EqualTo("high"));
        Assert.That(result.Diagnostics.Errors[1].Message, Is.EqualTo("numeric value outside range"));
        Assert.That(result.Diagnostics.Errors[2].Path, Is.EqualTo("text"));
        Assert.That(result.Diagnostics.Errors[2].Message, Is.EqualTo("expected numeric value"));
    }

    [Test]
    public void Validate_LengthRulesReportShortLongAndNonStringValues()
    {
        Config config = Config.CreateBuilder()
            .AddString(
                "{ \"short\": \"a\", \"long\": \"abcd\", \"number\": 5, \"ok\": \"abc\" }",
                ConfigFormat.Json,
                "json")
            .Build();
        ConfigValidator validator = new ConfigValidator();
        validator
            .Length("short", 2, 3)
            .Length("long", 2, 3)
            .Length("number", 2, 3)
            .Length("ok", 2, 3);

        ConfigValidationResult result = validator.Validate(config);

        Assert.That(result.ErrorCount, Is.EqualTo(3));
        Assert.That(result.Diagnostics.Errors[0].Path, Is.EqualTo("short"));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("string length outside range"));
        Assert.That(result.Diagnostics.Errors[1].Path, Is.EqualTo("long"));
        Assert.That(result.Diagnostics.Errors[1].Message, Is.EqualTo("string length outside range"));
        Assert.That(result.Diagnostics.Errors[2].Path, Is.EqualTo("number"));
        Assert.That(result.Diagnostics.Errors[2].Message, Is.EqualTo("expected string value"));
    }

    [Test]
    public void Validate_AllowedRulesCompareConvertedScalarText()
    {
        Config config = Config.CreateBuilder()
            .AddString(
                "{ \"number\": 2, \"flag\": true, \"mode\": \"stage\" }",
                ConfigFormat.Json,
                "json")
            .Build();
        ConfigValidator validator = new ConfigValidator();
        validator
            .Allowed("number", new string[] { "1", "2" })
            .Allowed("flag", new string[] { "true" })
            .Allowed("mode", new string[] { "dev", "prod" });

        ConfigValidationResult result = validator.Validate(config);

        Assert.That(result.ErrorCount, Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].Path, Is.EqualTo("mode"));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("value is not allowed"));
    }

    [Test]
    public void Validate_AggregateDiagnosticsPreserveRuleOrderAndSourceMetadata()
    {
        Config config = Config.CreateBuilder()
            .AddString(
                "{ \"enabled\": \"maybe\", \"port\": \"70000\", \"mode\": \"stage\" }",
                ConfigFormat.Json,
                "json-source")
            .Build();
        ConfigValidator validator = new ConfigValidator();
        validator
            .Require("host")
            .Type("enabled", ConfigValidationValueType.Boolean)
            .Range("port", 1m, 65535m)
            .Allowed("mode", new string[] { "dev", "prod" });

        ConfigValidationResult result = validator.Validate(config);

        Assert.That(result.ErrorCount, Is.EqualTo(4));
        Assert.That(result.Diagnostics.Errors[0].Path, Is.EqualTo("host"));
        Assert.That(result.Diagnostics.Errors[0].SourceName, Is.EqualTo(""));
        Assert.That(result.Diagnostics.Errors[1].Path, Is.EqualTo("enabled"));
        Assert.That(result.Diagnostics.Errors[1].SourceName, Is.EqualTo("json-source"));
        Assert.That(result.Diagnostics.Errors[2].Path, Is.EqualTo("port"));
        Assert.That(result.Diagnostics.Errors[2].SourceName, Is.EqualTo("json-source"));
        Assert.That(result.Diagnostics.Errors[3].Path, Is.EqualTo("mode"));
        Assert.That(result.Diagnostics.Errors[3].SourceName, Is.EqualTo("json-source"));
    }

    [Test]
    public void Validate_FailFastStopsAfterFirstRuleFailure()
    {
        Config config = Config.CreateBuilder()
            .AddString(
                "{ \"service\": { \"port\": \"70000\", \"name\": \"a\" } }",
                ConfigFormat.Json,
                "json")
            .Build();
        ConfigValidationRule[] rules = new ConfigValidationRule[]
        {
            ConfigValidationRule.Required("service.host"),
            ConfigValidationRule.Range("service.port", 1m, 65535m),
            ConfigValidationRule.Length("service.name", 2, 20)
        };

        ConfigValidationResult result = ConfigValidator.Validate(config, rules, ConfigErrorMode.FailFast);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorCount, Is.EqualTo(1));
        Assert.That(result.Diagnostics.Errors[0].Path, Is.EqualTo("service.host"));
        Assert.That(result.Diagnostics.Errors[0].Message, Is.EqualTo("missing required value"));
    }

    [Test]
    public void Validate_ArrayLengthChecksNativeArraysAndSkipsMissingOrNullValues()
    {
        ConfigMap nullValues = new ConfigMap();
        nullValues.Set("nullValue", ConfigValue.Null());
        Config config = Config.CreateBuilder()
            .AddString(
                "{ \"empty\": [], \"one\": [1], \"three\": [1, 2, 3], \"scalar\": \"one,two\" }",
                ConfigFormat.Json,
                "arrays.json")
            .AddMemory(nullValues, "memory")
            .Build();
        ConfigValidator validator = new ConfigValidator();
        validator
            .ArrayLength("empty", 1, 2)
            .ArrayLength("one", 1, 2)
            .ArrayLength("three", 1, 2)
            .ArrayLength("scalar", 1, 2)
            .ArrayLength("missing", 1, 2)
            .ArrayLength("nullValue", 1, 2);

        ConfigValidationResult result = validator.Validate(config);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorCount, Is.EqualTo(3));
        Assert.That(result.Errors[0].Path, Is.EqualTo("empty"));
        Assert.That(result.Errors[0].Message, Is.EqualTo("array length outside range"));
        Assert.That(result.Errors[1].Path, Is.EqualTo("three"));
        Assert.That(result.Errors[1].Message, Is.EqualTo("array length outside range"));
        Assert.That(result.Errors[2].Path, Is.EqualTo("scalar"));
        Assert.That(result.Errors[2].Message, Is.EqualTo("expected array value"));
        Assert.That(result.Errors[2].SourceName, Is.EqualTo("arrays.json"));
    }

    [Test]
    public void Validate_NamedValidatorsRunForPresentValuesUsingIndependentCopies()
    {
        ConfigMap nullValues = new ConfigMap();
        nullValues.Set("nullValue", ConfigValue.Null());
        Config config = Config.CreateBuilder()
            .AddString("{ \"valid\": \"apple\", \"invalid\": \"pear\" }", ConfigFormat.Json, "values.json")
            .AddMemory(nullValues, "memory")
            .Build();
        PrefixNamedValidator custom = new PrefixNamedValidator("starts-with-a", "a");
        ConfigValidator validator = new ConfigValidator();
        validator
            .Register(custom)
            .Custom("valid", "starts-with-a")
            .Custom("invalid", "starts-with-a")
            .Custom("missing", "starts-with-a")
            .Custom("nullValue", "starts-with-a");

        ConfigValidationResult result = validator.Validate(config);

        Assert.That(custom.CallCount, Is.EqualTo(2));
        Assert.That(result.ErrorCount, Is.EqualTo(1));
        Assert.That(result.Errors[0].Path, Is.EqualTo("invalid"));
        Assert.That(result.Errors[0].Message, Is.EqualTo("value must start with a"));
        Assert.That(result.Errors[0].SourceName, Is.EqualTo("values.json"));
        Assert.That(config.GetString("valid"), Is.EqualTo("apple"));
        Assert.That(config.GetString("invalid"), Is.EqualTo("pear"));
    }

    [Test]
    public void Validate_NamedRegistrationReplacesByExactNameAndReportsUnknownNames()
    {
        Config config = Config.CreateBuilder()
            .AddString("{ \"value\": \"pear\" }", ConfigFormat.Json, "values.json")
            .Build();
        PrefixNamedValidator replaced = new PrefixNamedValidator("prefix", "a");
        PrefixNamedValidator replacement = new PrefixNamedValidator("prefix", "p");
        ConfigValidator validator = new ConfigValidator();
        validator
            .Register(replaced)
            .Register(replacement)
            .Custom("value", "prefix")
            .Custom("value", "Prefix");

        ConfigValidationResult result = validator.Validate(config);

        Assert.That(replaced.CallCount, Is.EqualTo(0));
        Assert.That(replacement.CallCount, Is.EqualTo(1));
        Assert.That(result.ErrorCount, Is.EqualTo(1));
        Assert.That(result.Errors[0].Message, Is.EqualTo("named validator is not registered: Prefix"));
    }

    [Test]
    public void Validate_NamedValidatorsHonorFailFastMode()
    {
        Config config = Config.CreateBuilder()
            .AddString("{ \"first\": \"bad\", \"second\": \"bad\" }", ConfigFormat.Json, "values.json")
            .Build();
        PrefixNamedValidator custom = new PrefixNamedValidator("prefix", "good");
        ConfigValidator validator = new ConfigValidator();
        validator
            .Register(custom)
            .Custom("first", "prefix")
            .Custom("second", "prefix");

        ConfigValidationResult result = validator.ValidateWithMode(config, ConfigErrorMode.FailFast);

        Assert.That(custom.CallCount, Is.EqualTo(1));
        Assert.That(result.ErrorCount, Is.EqualTo(1));
        Assert.That(result.Errors[0].Path, Is.EqualTo("first"));
    }

    [Test]
    public void ValidationResultAndRuleInspectionUseIndependentCopies()
    {
        Config config = Config.CreateBuilder().Build();
        ConfigValidator validator = new ConfigValidator();
        validator
            .Require("first")
            .ArrayLength("items", 1, 2)
            .Custom("value", "custom");

        ConfigValidationRule rule = validator.Rule(1);
        ConfigValidationRule[] rules = validator.ToArray();
        rule.Path = "changed-rule";
        rules[1].MinLength = 99;
        ConfigValidationResult result = validator.Validate(config);
        result.Errors[0].Path = "changed-error";
        result.Errors[0].Message = "changed-message";

        Assert.That(validator.Rule(1).Path, Is.EqualTo("items"));
        Assert.That(validator.Rule(1).MinLength, Is.EqualTo(1));
        Assert.That(validator.Rule(2).ValidatorName, Is.EqualTo("custom"));
        Assert.That(result.IsValid, Is.False);
        Assert.That(result.ErrorCount, Is.EqualTo(1));
        Assert.That(result.Diagnostics.Error(0).Path, Is.EqualTo("first"));
        Assert.That(result.Diagnostics.Error(0).Message, Is.EqualTo("missing required value"));
    }

    [Test]
    public void Validator_RejectsMissingValidatorInstancesAndNames()
    {
        ConfigValidator validator = new ConfigValidator();

        Assert.Throws<ArgumentNullException>(() => validator.Register(null!));
        Assert.Throws<ArgumentException>(() => validator.Register(new EmptyNameValidator()));
        Assert.Throws<ArgumentException>(() => validator.Custom("value", ""));
        Assert.Throws<ArgumentException>(() => validator.Custom("value", null!));
    }
}

public class PrefixNamedValidator : IConfigNamedValidator
{
    public string ValidatorName;
    public string Prefix;
    public int CallCount;

    public PrefixNamedValidator(string validatorName, string prefix)
    {
        ValidatorName = validatorName;
        Prefix = prefix;
        CallCount = 0;
    }

    public string Name()
    {
        return ValidatorName;
    }

    public string Validate(ConfigValue value)
    {
        CallCount = CallCount + 1;
        string text = "";
        if (value.StringValue != null)
        {
            text = value.StringValue;
        }

        value.StringValue = "mutated-validator-copy";
        if (text.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return "";
        }

        return "value must start with " + Prefix;
    }
}

public class EmptyNameValidator : IConfigNamedValidator
{
    public string Name()
    {
        return "";
    }

    public string Validate(ConfigValue value)
    {
        return "";
    }
}
