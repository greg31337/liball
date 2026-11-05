using NUnit.Framework;

namespace yaml.Tests;

[TestFixture]
public class YamlOptionsTests
{
    [Test]
    public void DefaultOptions_CreateEveryLayerWithConservativeDefaults()
    {
        global::yaml.YamlOptions options = global::yaml.YamlOptions.Default();

        Assert.That(options.Parser.Version, Is.EqualTo(global::yaml.YamlVersion.Version12));
        Assert.That(options.Parser.IncludeComments, Is.True);
        Assert.That(options.Parser.AllowDirectives, Is.True);
        Assert.That(options.Parser.Recover, Is.False);
        Assert.That(options.Parser.MaxDiagnostics, Is.EqualTo(100));

        Assert.That(options.Composer.DuplicateKeyPolicy, Is.EqualTo(global::yaml.YamlDuplicateKeyPolicy.Error));
        Assert.That(options.Composer.UnknownTagPolicy, Is.EqualTo(global::yaml.YamlUnknownTagPolicy.Preserve));
        Assert.That(options.Composer.ResolveAliases, Is.True);
        Assert.That(options.Composer.AllowRecursiveAliases, Is.True);
        Assert.That(options.Composer.AllowMergeKeys, Is.False);
        Assert.That(options.Composer.PreserveComments, Is.True);

        Assert.That(options.Schema.Schema, Is.EqualTo(global::yaml.YamlSchemaKind.Core));
        Assert.That(options.Schema.ResolveScalars, Is.True);
        Assert.That(options.Schema.AllowYaml11Booleans, Is.False);
        Assert.That(options.Schema.AllowTimestamp, Is.False);
        Assert.That(options.Schema.AllowBinary, Is.False);
        Assert.That(options.Schema.AllowMergeKeys, Is.False);
        Assert.That(options.Schema.UnknownTagPolicy, Is.EqualTo(global::yaml.YamlUnknownTagPolicy.Preserve));
        Assert.That(options.Schema.CustomTags.Count(), Is.EqualTo(0));

        Assert.That(options.Emitter.Indent, Is.EqualTo(2));
        Assert.That(options.Emitter.LineWidth, Is.EqualTo(80));
        Assert.That(options.Emitter.WriteDocumentStart, Is.False);
        Assert.That(options.Emitter.WriteDocumentEnd, Is.False);
        Assert.That(options.Emitter.SortStringKeys, Is.False);
        Assert.That(options.Emitter.EmitComments, Is.True);
        Assert.That(options.Emitter.DefaultScalarStyle, Is.EqualTo(global::yaml.YamlScalarStyle.Any));
        Assert.That(options.Emitter.DefaultCollectionStyle, Is.EqualTo(global::yaml.YamlCollectionStyle.Block));
        Assert.That(options.Emitter.InvalidValuePolicy, Is.EqualTo(global::yaml.YamlInvalidValuePolicy.QuoteAsString));

        Assert.That(options.Object.NamingConvention, Is.EqualTo(global::yaml.YamlNamingConvention.Exact));
        Assert.That(options.Object.IncludePublicFields, Is.True);
        Assert.That(options.Object.IgnoreNulls, Is.False);
        Assert.That(options.Object.IgnoreDefaults, Is.False);
        Assert.That(options.Object.ErrorOnUnknownMembers, Is.True);
        Assert.That(options.Object.RequireConfiguredMembers, Is.False);
        Assert.That(options.Object.UseTypeTags, Is.False);
        Assert.That(options.Object.RequiredMemberCount(), Is.EqualTo(0));
        Assert.That(options.Object.ScalarConverters.Count(), Is.EqualTo(0));
        Assert.That(options.Object.TypeConverters.Count(), Is.EqualTo(0));

        Assert.That(options.Security.EnforceLimits, Is.True);
        Assert.That(options.Security.MaxInputLength, Is.EqualTo(16777216));
        Assert.That(options.Security.MaxScalarLength, Is.EqualTo(1048576));
        Assert.That(options.Security.MaxDepth, Is.EqualTo(100));
        Assert.That(options.Security.MaxAliasCount, Is.EqualTo(100));
        Assert.That(options.Security.MaxCollectionSize, Is.EqualTo(100000));
        Assert.That(options.Security.MaxDiagnostics, Is.EqualTo(1000));
    }

    [Test]
    public void Copy_CreatesIndependentNestedOptionObjects()
    {
        global::yaml.YamlOptions options = new global::yaml.YamlOptions();
        options.Parser.Recover = true;
        options.Composer.DuplicateKeyPolicy = global::yaml.YamlDuplicateKeyPolicy.LastWins;
        options.Schema.Schema = global::yaml.YamlSchemaKind.Json;
        options.Schema.CustomTags.Register("!point", global::yaml.YamlNodeKind.Mapping);
        options.Emitter.Indent = 4;
        options.Object.NamingConvention = global::yaml.YamlNamingConvention.SnakeCase;
        options.Object.AddRequiredMember("Name");
        options.Object.ScalarConverters.Register("!upper", typeof(string));
        options.Object.TypeConverters.Register(typeof(string));
        options.Security.MaxDepth = 25;

        global::yaml.YamlOptions copy = options.Copy();

        Assert.That(copy, Is.Not.SameAs(options));
        Assert.That(copy.Parser, Is.Not.SameAs(options.Parser));
        Assert.That(copy.Composer, Is.Not.SameAs(options.Composer));
        Assert.That(copy.Schema, Is.Not.SameAs(options.Schema));
        Assert.That(copy.Emitter, Is.Not.SameAs(options.Emitter));
        Assert.That(copy.Object, Is.Not.SameAs(options.Object));
        Assert.That(copy.Security, Is.Not.SameAs(options.Security));
        Assert.That(copy.Parser.Recover, Is.True);
        Assert.That(copy.Composer.DuplicateKeyPolicy, Is.EqualTo(global::yaml.YamlDuplicateKeyPolicy.LastWins));
        Assert.That(copy.Schema.Schema, Is.EqualTo(global::yaml.YamlSchemaKind.Json));
        Assert.That(copy.Schema.CustomTags, Is.Not.SameAs(options.Schema.CustomTags));
        Assert.That(copy.Schema.CustomTags.Count(), Is.EqualTo(1));
        Assert.That(copy.Emitter.Indent, Is.EqualTo(4));
        Assert.That(copy.Object.NamingConvention, Is.EqualTo(global::yaml.YamlNamingConvention.SnakeCase));
        Assert.That(copy.Object.RequiredMemberCount(), Is.EqualTo(1));
        Assert.That(copy.Object.HasRequiredMember("Name"), Is.True);
        Assert.That(copy.Object.ScalarConverters, Is.Not.SameAs(options.Object.ScalarConverters));
        Assert.That(copy.Object.ScalarConverters.Count(), Is.EqualTo(1));
        Assert.That(copy.Object.TypeConverters, Is.Not.SameAs(options.Object.TypeConverters));
        Assert.That(copy.Object.TypeConverters.Count(), Is.EqualTo(1));
        Assert.That(copy.Security.MaxDepth, Is.EqualTo(25));

        options.ResetToDefaults();

        Assert.That(options.Parser.Recover, Is.False);
        Assert.That(copy.Parser.Recover, Is.True);
        Assert.That(options.Schema.CustomTags.Count(), Is.EqualTo(0));
        Assert.That(copy.Schema.CustomTags.Count(), Is.EqualTo(1));
        Assert.That(options.Object.RequiredMemberCount(), Is.EqualTo(0));
        Assert.That(copy.Object.RequiredMemberCount(), Is.EqualTo(1));
        Assert.That(options.Object.ScalarConverters.Count(), Is.EqualTo(0));
        Assert.That(copy.Object.ScalarConverters.Count(), Is.EqualTo(1));
        Assert.That(options.Object.TypeConverters.Count(), Is.EqualTo(0));
        Assert.That(copy.Object.TypeConverters.Count(), Is.EqualTo(1));
        Assert.That(options.Security.MaxDepth, Is.EqualTo(100));
        Assert.That(copy.Security.MaxDepth, Is.EqualTo(25));
    }

    [Test]
    public void EmitterOptions_Normalize_ClampsSupportedRanges()
    {
        global::yaml.YamlEmitterOptions low = new global::yaml.YamlEmitterOptions();
        low.Indent = 0;
        low.LineWidth = -1;

        low.Normalize();

        Assert.That(low.Indent, Is.EqualTo(1));
        Assert.That(low.LineWidth, Is.EqualTo(0));

        global::yaml.YamlEmitterOptions high = new global::yaml.YamlEmitterOptions();
        high.Indent = 99;

        high.Normalize();

        Assert.That(high.Indent, Is.EqualTo(8));
    }

    [Test]
    public void SecurityOptions_CheckConfiguredLimitsAndCanDisableEnforcement()
    {
        global::yaml.YamlSecurityOptions security = new global::yaml.YamlSecurityOptions();
        security.MaxInputLength = 3;
        security.MaxScalarLength = 4;
        security.MaxDepth = 5;
        security.MaxAliasCount = 6;
        security.MaxCollectionSize = 7;
        security.MaxDiagnostics = 8;

        Assert.That(security.AllowsInputLength(3), Is.True);
        Assert.That(security.AllowsInputLength(4), Is.False);
        Assert.That(security.AllowsScalarLength(5), Is.False);
        Assert.That(security.AllowsDepth(6), Is.False);
        Assert.That(security.AllowsAliasCount(7), Is.False);
        Assert.That(security.AllowsCollectionSize(8), Is.False);
        Assert.That(security.AllowsDiagnosticCount(9), Is.False);

        security.EnforceLimits = false;

        Assert.That(security.AllowsInputLength(400), Is.True);
        Assert.That(security.AllowsScalarLength(500), Is.True);
    }
}
