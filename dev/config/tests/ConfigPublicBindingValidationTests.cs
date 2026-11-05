using System;
using System.Collections.Generic;

namespace config.Tests;

public class ConfigPublicBindingValidationTests
{
    [Test]
    public void PublicBinding_CreatesAndUpdatesNestedTargetsWithArraysListsEnumsAndCaseMatching()
    {
        Config config = new ConfigBuilder()
            .SetCaseMode(ConfigCaseMode.Insensitive)
            .AddString("{\"service\":{\"name\":\"api\",\"port\":\"8080\",\"enabled\":\"yes\",\"timeout\":\"2s\",\"mode\":\"fAsT\",\"credentials\":{\"token\":\"secret\"},\"ports\":[80,\"443\"],\"workers\":[{\"name\":\"one\",\"mode\":\"slow\"},{\"name\":\"two\",\"mode\":\"FAST\"}]}}", ConfigFormat.Json, "binding.json")
            .Build();

        PublicServiceBindingTarget created = (PublicServiceBindingTarget)config.BindSection("SERVICE", typeof(PublicServiceBindingTarget));
        PublicServiceBindingTarget existing = new PublicServiceBindingTarget();
        PublicCredentialsBindingTarget existingCredentials = existing.Credentials;
        config.BindSectionInstance("SERVICE", existing);

        AssertServiceBinding(created);
        AssertServiceBinding(existing);
        Assert.That(existing.Credentials, Is.SameAs(existingCredentials));
    }

    [Test]
    public void PublicBinding_AppliesFieldMapsToNewAndExistingSectionTargets()
    {
        Config config = new ConfigBuilder()
            .AddString("{\"service\":{\"display-name\":\"mapped-api\",\"auth\":{\"api-key\":\"mapped-secret\"},\"public-ports\":[8080,8443],\"workers\":[{\"worker-name\":\"mapped-worker\",\"worker-mode\":\"Slow\"}]}}", ConfigFormat.Json, "mapped.json")
            .Build();
        ConfigFieldMap fields = new ConfigFieldMap()
            .Add("Name", "display-name")
            .Add("Credentials", "auth")
            .Add("Credentials.Token", "auth.api-key")
            .Add("Ports", "public-ports")
            .Add("Workers", "workers")
            .Add("Workers.Name", "workers.worker-name")
            .Add("Workers.Mode", "workers.worker-mode");

        PublicServiceBindingTarget created = (PublicServiceBindingTarget)config.BindMappedSection("service", typeof(PublicServiceBindingTarget), fields);
        PublicServiceBindingTarget existing = new PublicServiceBindingTarget();
        config.BindMappedSectionInstance("service", existing, fields);

        Assert.Multiple(() =>
        {
            Assert.That(fields.Has("Credentials.Token"), Is.True);
            Assert.That(fields.Get("Credentials.Token"), Is.EqualTo("auth.api-key"));
            Assert.That(created.Name, Is.EqualTo("mapped-api"));
            Assert.That(created.Credentials.Token, Is.EqualTo("mapped-secret"));
            Assert.That(created.Ports, Is.EqualTo(new int[] { 8080, 8443 }));
            Assert.That(created.Workers.Count, Is.EqualTo(1));
            Assert.That(created.Workers[0].Name, Is.EqualTo("mapped-worker"));
            Assert.That(created.Workers[0].Mode, Is.EqualTo(PublicBindingMode.Slow));
            Assert.That(existing.Name, Is.EqualTo("mapped-api"));
            Assert.That(existing.Credentials.Token, Is.EqualTo("mapped-secret"));
            Assert.That(existing.Workers[0].Name, Is.EqualTo("mapped-worker"));
        });
    }

    [Test]
    public void PublicBinding_ReportsUnsupportedTargetsAndConversionFailuresWithContext()
    {
        Config malformed = new ConfigBuilder()
            .AddString("Port=not-an-integer", ConfigFormat.Flat, "binding.flat")
            .Build();
        Config unsupported = new ConfigBuilder()
            .AddString("Timestamp=2026-08-25", ConfigFormat.Flat, "unsupported.flat")
            .Build();

        ConfigConversionException? conversion = Assert.Throws<ConfigConversionException>(() => malformed.BindInstance(new PublicBindingFailureTarget()));
        ConfigConversionException? root = Assert.Throws<ConfigConversionException>(() => malformed.Bind(typeof(string)));
        ConfigConversionException? field = Assert.Throws<ConfigConversionException>(() => unsupported.Bind(typeof(PublicUnsupportedBindingTarget)));

        Assert.Multiple(() =>
        {
            Assert.That(conversion, Is.Not.Null);
            Assert.That(conversion!.Message, Does.Contain("field Port"));
            Assert.That(conversion.Message, Does.Contain("System.Int32"));
            Assert.That(conversion.SourceName, Is.EqualTo("binding.flat"));
            Assert.That(conversion.Path, Is.EqualTo("Port"));
            Assert.That(conversion.LineNumber, Is.EqualTo(1));
            Assert.That(conversion.ColumnNumber, Is.EqualTo(1));
            Assert.That(root!.Message, Does.Contain("unsupported config binding target type"));
            Assert.That(field!.Message, Does.Contain("field Timestamp"));
            Assert.That(field.Message, Does.Contain("System.DateTime"));
        });
    }

    [Test]
    public void PublicValidation_AcceptsRequiredTypeRangeLengthArrayAndAllowedRules()
    {
        Config config = BuildValidationConfig();
        ConfigValidator validator = new ConfigValidator()
            .RequireString("name")
            .RequireInt("integer")
            .Type("object", ConfigValidationValueType.Any)
            .Type("name", ConfigValidationValueType.String)
            .Type("boolean", ConfigValidationValueType.Boolean)
            .Type("integer", ConfigValidationValueType.Integer)
            .Type("long", ConfigValidationValueType.Long)
            .Type("decimal", ConfigValidationValueType.Decimal)
            .Type("double", ConfigValidationValueType.Double)
            .Type("duration", ConfigValidationValueType.TimeSpan)
            .Type("size", ConfigValidationValueType.ByteSize)
            .Type("csv", ConfigValidationValueType.StringArray)
            .Type("items", ConfigValidationValueType.Array)
            .Type("object", ConfigValidationValueType.Object)
            .Range("integer", 42m, 42m)
            .Length("name", 3, 3)
            .ArrayLength("items", 2, 2)
            .Allowed("mode", new string[] { "prod", "stage" });

        ConfigValidationResult result = validator.Validate(config);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.ErrorCount, Is.EqualTo(0));
            Assert.That(result.Errors, Is.Empty);
            Assert.That(result.Diagnostics.HasErrors(), Is.False);
            Assert.That(validator.RuleCount(), Is.EqualTo(20));
        });
    }

    [Test]
    public void PublicValidation_CollectsAllBuiltInRuleFailuresInRuleOrder()
    {
        Config config = new ConfigBuilder()
            .AddString("{\"nullValue\":null,\"badBoolean\":\"maybe\",\"low\":0,\"high\":11,\"nonNumeric\":\"abc\",\"short\":\"a\",\"long\":\"abcd\",\"number\":5,\"empty\":[],\"many\":[1,2,3],\"scalar\":\"one,two\",\"mode\":\"stage\"}", ConfigFormat.Json, "validation.json")
            .Build();
        ConfigValidator validator = new ConfigValidator()
            .Require("missing")
            .Require("nullValue")
            .Type("badBoolean", ConfigValidationValueType.Boolean)
            .Range("low", 1m, 10m)
            .Range("high", 1m, 10m)
            .Range("nonNumeric", 1m, 10m)
            .Length("short", 2, 3)
            .Length("long", 2, 3)
            .Length("number", 2, 3)
            .ArrayLength("empty", 1, 2)
            .ArrayLength("many", 1, 2)
            .ArrayLength("scalar", 1, 2)
            .Allowed("mode", new string[] { "dev", "prod" });

        ConfigValidationResult result = validator.ValidateWithMode(config, ConfigErrorMode.Collect);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorCount, Is.EqualTo(13));
            Assert.That(result.Diagnostics.ErrorCount(), Is.EqualTo(13));
        });
        AssertValidationError(result, 0, "missing", "missing required value", "");
        AssertValidationError(result, 1, "nullValue", "missing required value", "validation.json");
        AssertValidationError(result, 2, "badBoolean", "expected boolean value", "validation.json");
        AssertValidationError(result, 3, "low", "numeric value outside range", "validation.json");
        AssertValidationError(result, 4, "high", "numeric value outside range", "validation.json");
        AssertValidationError(result, 5, "nonNumeric", "expected numeric value", "validation.json");
        AssertValidationError(result, 6, "short", "string length outside range", "validation.json");
        AssertValidationError(result, 7, "long", "string length outside range", "validation.json");
        AssertValidationError(result, 8, "number", "expected string value", "validation.json");
        AssertValidationError(result, 9, "empty", "array length outside range", "validation.json");
        AssertValidationError(result, 10, "many", "array length outside range", "validation.json");
        AssertValidationError(result, 11, "scalar", "expected array value", "validation.json");
        AssertValidationError(result, 12, "mode", "value is not allowed", "validation.json");
    }

    [Test]
    public void PublicValidation_FailFastAndThrowingValidationPreserveFirstFailure()
    {
        Config config = new ConfigBuilder()
            .AddString("port=not-an-integer\nname=x", ConfigFormat.Flat, "validation.flat")
            .Build();
        ConfigValidator validator = new ConfigValidator()
            .RequireInt("port")
            .Length("name", 2, 10)
            .Require("missing");

        ConfigValidationResult result = validator.ValidateWithMode(config, ConfigErrorMode.FailFast);
        ConfigValidationException? exception = Assert.Throws<ConfigValidationException>(() => validator.ValidateOrThrow(config));

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Errors[0].Path, Is.EqualTo("port"));
            Assert.That(result.Errors[0].Message, Is.EqualTo("expected integer value"));
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Is.EqualTo("expected integer value"));
            Assert.That(exception.SourceName, Is.EqualTo("validation.flat"));
            Assert.That(exception.Path, Is.EqualTo("port"));
            Assert.That(exception.LineNumber, Is.EqualTo(1));
            Assert.That(exception.ColumnNumber, Is.EqualTo(1));
        });
    }

    [Test]
    public void PublicValidation_CustomValidatorsReceiveCopiesAndResultsRemainInspectable()
    {
        Config config = new ConfigBuilder()
            .AddString("{\"valid\":\"apple\",\"invalid\":\"pear\"}", ConfigFormat.Json, "custom.json")
            .Build();
        PublicPrefixValidator custom = new PublicPrefixValidator("starts-with-a", "a");
        ConfigValidator validator = new ConfigValidator()
            .Register(custom)
            .Custom("valid", "starts-with-a")
            .Custom("invalid", "starts-with-a");

        ConfigValidationResult result = validator.Validate(config);
        ConfigValidationRule copiedRule = validator.Rule(1);
        ConfigValidationRule[] copiedRules = validator.ToArray();
        copiedRule.Path = "changed-rule";
        copiedRules[1].ValidatorName = "changed-validator";
        result.Errors[0].Path = "changed-error";

        Assert.Multiple(() =>
        {
            Assert.That(custom.CallCount, Is.EqualTo(2));
            Assert.That(config.GetString("valid"), Is.EqualTo("apple"));
            Assert.That(config.GetString("invalid"), Is.EqualTo("pear"));
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.ErrorCount, Is.EqualTo(1));
            Assert.That(result.Diagnostics.Error(0).Path, Is.EqualTo("invalid"));
            Assert.That(result.Diagnostics.Error(0).Message, Is.EqualTo("value must start with a"));
            Assert.That(result.Diagnostics.Error(0).SourceName, Is.EqualTo("custom.json"));
            Assert.That(validator.Rule(1).Path, Is.EqualTo("invalid"));
            Assert.That(validator.Rule(1).ValidatorName, Is.EqualTo("starts-with-a"));
        });
    }

    private static Config BuildValidationConfig()
    {
        return new ConfigBuilder()
            .AddString("{\"name\":\"api\",\"boolean\":\"yes\",\"integer\":\"42\",\"long\":\"1234567890123\",\"decimal\":\"12.5\",\"double\":\"1.5\",\"duration\":\"2s\",\"size\":\"2kb\",\"csv\":\"one,two\",\"items\":[1,2],\"object\":{\"key\":\"value\"},\"mode\":\"prod\"}", ConfigFormat.Json, "valid.json")
            .Build();
    }

    private static void AssertServiceBinding(PublicServiceBindingTarget target)
    {
        Assert.Multiple(() =>
        {
            Assert.That(target.Name, Is.EqualTo("api"));
            Assert.That(target.Port, Is.EqualTo(8080));
            Assert.That(target.Enabled, Is.True);
            Assert.That(target.Timeout, Is.EqualTo(TimeSpan.FromSeconds(2)));
            Assert.That(target.Mode, Is.EqualTo(PublicBindingMode.Fast));
            Assert.That(target.Credentials.Token, Is.EqualTo("secret"));
            Assert.That(target.Ports, Is.EqualTo(new int[] { 80, 443 }));
            Assert.That(target.Workers.Count, Is.EqualTo(2));
            Assert.That(target.Workers[0].Name, Is.EqualTo("one"));
            Assert.That(target.Workers[0].Mode, Is.EqualTo(PublicBindingMode.Slow));
            Assert.That(target.Workers[1].Name, Is.EqualTo("two"));
            Assert.That(target.Workers[1].Mode, Is.EqualTo(PublicBindingMode.Fast));
        });
    }

    private static void AssertValidationError(ConfigValidationResult result, int index, string path, string message, string sourceName)
    {
        ConfigError error = result.Errors[index];
        Assert.Multiple(() =>
        {
            Assert.That(error.Path, Is.EqualTo(path));
            Assert.That(error.Message, Is.EqualTo(message));
            Assert.That(error.SourceName, Is.EqualTo(sourceName));
        });
    }
}

public enum PublicBindingMode
{
    Unknown = 0,
    Fast = 1,
    Slow = 2
}

public class PublicServiceBindingTarget
{
    public string Name = "default";
    public int Port = 1;
    public bool Enabled;
    public TimeSpan Timeout;
    public PublicBindingMode Mode;
    public PublicCredentialsBindingTarget Credentials = new PublicCredentialsBindingTarget();
    public int[] Ports = new int[0];
    public List<PublicWorkerBindingTarget> Workers = new List<PublicWorkerBindingTarget>();
}

public class PublicCredentialsBindingTarget
{
    public string Token = "default-token";
}

public class PublicWorkerBindingTarget
{
    public string Name = "default-worker";
    public PublicBindingMode Mode;
}

public class PublicBindingFailureTarget
{
    public int Port;
}

public class PublicUnsupportedBindingTarget
{
    public DateTime Timestamp;
}

public class PublicPrefixValidator : IConfigNamedValidator
{
    public string ValidatorName;
    public string Prefix;
    public int CallCount;

    public PublicPrefixValidator(string validatorName, string prefix)
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

        value.StringValue = "mutated-copy";
        if (text.StartsWith(Prefix, StringComparison.Ordinal))
        {
            return "";
        }

        return "value must start with " + Prefix;
    }
}
