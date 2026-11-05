using NUnit.Framework;

namespace log.Tests;

[TestFixture]
public class LogBuiltInEnricherTests
{
    [Test]
    public void SetRuntime_NormalizesAndStoresOverride()
    {
        LogBuiltInEnricher enricher = new LogBuiltInEnricher();

        enricher.SetRuntime(-1, -2, null, null);

        Assert.That(enricher.HasRuntimeOverride, Is.True);
        Assert.That(enricher.ThreadId, Is.EqualTo(0));
        Assert.That(enricher.ProcessId, Is.EqualTo(0));
        Assert.That(enricher.MachineName, Is.EqualTo(""));
        Assert.That(enricher.ApplicationName, Is.EqualTo(""));
    }

    [Test]
    public void AddStaticField_AppendsField()
    {
        LogBuiltInEnricher enricher = new LogBuiltInEnricher();

        enricher.AddStaticField("Service", "billing");
        enricher.AddStaticLogField(LogField.Int("Shard", 2));

        Assert.That(enricher.StaticFields.Length, Is.EqualTo(2));
        Assert.That(enricher.StaticFields[0].Name, Is.EqualTo("Service"));
        Assert.That(enricher.StaticFields[0].Value.TextValue, Is.EqualTo("billing"));
        Assert.That(enricher.StaticFields[1].Name, Is.EqualTo("Shard"));
        Assert.That(enricher.StaticFields[1].Value.IntegerValue, Is.EqualTo(2));
    }

    [Test]
    public void ClearMethods_ResetRuntimeOverrideAndStaticFields()
    {
        LogBuiltInEnricher enricher = new LogBuiltInEnricher();

        enricher.SetRuntime(1, 2, "machine", "app");
        enricher.AddStaticField("Service", "billing");
        enricher.ClearRuntimeOverride();
        enricher.ClearStaticFields();

        Assert.That(enricher.HasRuntimeOverride, Is.False);
        Assert.That(enricher.StaticFields.Length, Is.EqualTo(0));
    }
}
