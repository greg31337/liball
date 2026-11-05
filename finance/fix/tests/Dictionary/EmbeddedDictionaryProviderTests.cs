using System.Collections.Concurrent;
using NUnit.Framework;

namespace fix.Tests;

public class EmbeddedDictionaryProviderTests
{
    [TestCase("begin", FixBeginStrings.Fix40, "FIX40")]
    [TestCase("begin", FixBeginStrings.Fix41, "FIX41")]
    [TestCase("begin", FixBeginStrings.Fix42, "FIX42")]
    [TestCase("begin", FixBeginStrings.Fix43, "FIX43")]
    [TestCase("begin", FixBeginStrings.Fix44, "FIX44")]
    [TestCase("begin", FixBeginStrings.Fixt11, "FIXT11")]
    [TestCase("application", FixApplicationVersions.Fix50, "FIX50")]
    [TestCase("application", FixApplicationVersions.Fix50Sp1, "FIX50SP1")]
    [TestCase("application", FixApplicationVersions.Fix50Sp2, "FIX50SP2")]
    [TestCase("application", FixApplicationVersions.FixLatest, "FIX.Latest")]
    public void ProviderResolvesEveryBuiltInIdentity(string identityKind, string value, string expectedName)
    {
        FixDictionaryIdentity identity = new FixDictionaryIdentity();
        if (identityKind == "begin")
        {
            identity.BeginString = value;
        }
        else
        {
            identity.ApplicationVersion = value;
        }

        DictionaryLoadResult result = new EmbeddedDictionaryProvider(null).ResolveDictionary(identity);

        Assert.Multiple(() =>
        {
            Assert.That(result.Success, Is.True);
            Assert.That(result.Dictionary, Is.Not.Null);
            Assert.That(result.Dictionary!.Identity.Name, Is.EqualTo(expectedName));
            Assert.That(result.Dictionary.Fields, Is.Not.Empty);
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void ProviderSupportsCanonicalNamesAndApplicationVersionAliases()
    {
        EmbeddedDictionaryProvider provider = new EmbeddedDictionaryProvider(null);

        DictionaryLoadResult classicByApplication = provider.ResolveDictionary(new FixDictionaryIdentity { ApplicationVersion = "FIX.4.4" });
        DictionaryLoadResult servicePackByName = provider.ResolveDictionary(new FixDictionaryIdentity { Name = "FIX.5.0-SP2" });
        DictionaryLoadResult servicePackByVersion = provider.ResolveDictionary(new FixDictionaryIdentity { ApplicationVersion = "FIX.5.0 SP1" });
        DictionaryLoadResult latestByGeneratedName = provider.ResolveDictionary(new FixDictionaryIdentity { Name = "FIXLatestEP309", HasExtensionPack = true, ExtensionPack = 309 });

        Assert.Multiple(() =>
        {
            Assert.That(classicByApplication.Dictionary!.Identity.Name, Is.EqualTo("FIX44"));
            Assert.That(servicePackByName.Dictionary!.Identity.Name, Is.EqualTo("FIX50SP2"));
            Assert.That(servicePackByVersion.Dictionary!.Identity.Name, Is.EqualTo("FIX50SP1"));
            Assert.That(latestByGeneratedName.Dictionary!.Identity.Name, Is.EqualTo("FIX.Latest"));
            Assert.That(latestByGeneratedName.Dictionary.Identity.ExtensionPack, Is.EqualTo(309));
        });
    }

    [Test]
    public void ProviderReturnsDetachedGraphsWithoutExposingCachedSnapshots()
    {
        EmbeddedDictionaryProvider provider = new EmbeddedDictionaryProvider(null);
        FixDictionaryIdentity identity = new FixDictionaryIdentity { BeginString = FixBeginStrings.Fix44 };

        DictionaryLoadResult first = provider.ResolveDictionary(identity);
        DictionaryLoadResult second = provider.ResolveDictionary(identity);
        string originalFieldName = second.Dictionary!.Fields[0].Name;
        first.Dictionary!.Identity.Name = "mutated";
        first.Dictionary.Fields[0].Name = "mutated";
        first.Dictionary.Fields.Clear();
        first.Dictionary.Options.MaxDefinitions = 1;
        first.Dictionary.Diagnostics.Add(new DictionaryDiagnostic { Code = "caller-change" });
        DictionaryLoadResult third = provider.ResolveDictionary(identity);

        Assert.Multiple(() =>
        {
            Assert.That(first.Dictionary, Is.Not.SameAs(second.Dictionary));
            Assert.That(second.Dictionary, Is.Not.SameAs(third.Dictionary));
            Assert.That(second.Dictionary.Identity.Name, Is.EqualTo("FIX44"));
            Assert.That(second.Dictionary.Fields[0].Name, Is.EqualTo(originalFieldName));
            Assert.That(third.Dictionary!.Identity.Name, Is.EqualTo("FIX44"));
            Assert.That(third.Dictionary.Fields, Is.Not.Empty);
            Assert.That(third.Dictionary.Fields[0].Name, Is.EqualTo(originalFieldName));
            Assert.That(third.Dictionary.Options.MaxDefinitions, Is.EqualTo(FixDefaults.MaxDictionaryDefinitions));
            Assert.That(third.Dictionary.Diagnostics, Is.Empty);
            Assert.That(EmbeddedDictionaryProvider.Cache.ContainsKey("FIX44.fixdict.gz"), Is.True);
            Assert.That(third.Dictionary, Is.Not.SameAs(EmbeddedDictionaryProvider.Cache["FIX44.fixdict.gz"]));
        });
    }

    [Test]
    public void ConcurrentReadsReturnIndependentCompleteSnapshots()
    {
        EmbeddedDictionaryProvider provider = new EmbeddedDictionaryProvider(null);
        DictionaryLoadResult[] results = new DictionaryLoadResult[64];

        Parallel.For(0, results.Length, index =>
        {
            results[index] = provider.ResolveDictionary(new FixDictionaryIdentity { ApplicationVersion = FixApplicationVersions.Fix50Sp2 });
        });

        ConcurrentDictionary<FixDictionary, bool> instances = new ConcurrentDictionary<FixDictionary, bool>();
        for (int index = 0; index < results.Length; index++)
        {
            Assert.That(results[index].Success, Is.True);
            Assert.That(results[index].Dictionary, Is.Not.Null);
            instances.TryAdd(results[index].Dictionary!, true);
        }
        results[0].Dictionary!.Fields.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(instances.Count, Is.EqualTo(results.Length));
            Assert.That(results[1].Dictionary!.Fields, Is.Not.Empty);
            Assert.That(results[1].Dictionary!.Identity.ApplicationVersion, Is.EqualTo(FixApplicationVersions.Fix50Sp2));
        });
    }

    [Test]
    public void UnknownAndNewerIdentitiesUseRuntimeProviderWithoutCachingItsResults()
    {
        RecordingDictionaryProvider fallback = new RecordingDictionaryProvider();
        EmbeddedDictionaryProvider provider = new EmbeddedDictionaryProvider(fallback);
        FixDictionaryIdentity newer = new FixDictionaryIdentity
        {
            ApplicationVersion = FixApplicationVersions.FixLatest,
            HasExtensionPack = true,
            ExtensionPack = 310
        };

        DictionaryLoadResult first = provider.ResolveDictionary(newer);
        first.Dictionary!.Fields[0].Name = "mutated";
        DictionaryLoadResult second = provider.ResolveDictionary(newer);

        Assert.Multiple(() =>
        {
            Assert.That(fallback.CallCount, Is.EqualTo(2));
            Assert.That(fallback.LastIdentity, Is.SameAs(newer));
            Assert.That(first.Success, Is.True);
            Assert.That(second.Success, Is.True);
            Assert.That(first.Dictionary, Is.Not.SameAs(fallback.Result.Dictionary));
            Assert.That(second.Dictionary, Is.Not.SameAs(first.Dictionary));
            Assert.That(second.Dictionary!.Fields[0].Name, Is.EqualTo("RuntimeField"));
            Assert.That(second.Diagnostics[0].Code, Is.EqualTo("runtime-source"));
            Assert.That(second.Diagnostics[0], Is.Not.SameAs(fallback.Result.Diagnostics[0]));
        });
    }

    [Test]
    public void MissingFallbackProducesStableResolutionDiagnostics()
    {
        EmbeddedDictionaryProvider provider = new EmbeddedDictionaryProvider(null);

        DictionaryLoadResult empty = provider.ResolveDictionary(new FixDictionaryIdentity());
        DictionaryLoadResult unknown = provider.ResolveDictionary(new FixDictionaryIdentity { ApplicationVersion = "99", Source = "request" });
        DictionaryLoadResult conflicting = provider.ResolveDictionary(new FixDictionaryIdentity { BeginString = FixBeginStrings.Fix44, ApplicationVersion = FixApplicationVersions.Fix50Sp2 });
        DictionaryLoadResult custom = provider.ResolveDictionary(new FixDictionaryIdentity { ApplicationVersion = FixApplicationVersions.Fix50Sp2, CustomVersion = "venue" });

        Assert.Multiple(() =>
        {
            Assert.That(empty.Success, Is.False);
            Assert.That(empty.Diagnostics[0].Code, Is.EqualTo("missing-dictionary-identity"));
            Assert.That(unknown.Diagnostics[0].Code, Is.EqualTo("dictionary-not-found"));
            Assert.That(unknown.Diagnostics[0].Source, Is.EqualTo("request"));
            Assert.That(conflicting.Diagnostics[0].Code, Is.EqualTo("dictionary-not-found"));
            Assert.That(custom.Diagnostics[0].Code, Is.EqualTo("dictionary-not-found"));
        });
    }
}

internal class RecordingDictionaryProvider : IDictionaryProvider
{
    internal int CallCount;
    internal FixDictionaryIdentity? LastIdentity;
    internal DictionaryLoadResult Result = new DictionaryLoadResult
    {
        Success = true,
        Dictionary = new FixDictionary
        {
            Identity = new FixDictionaryIdentity { Name = "runtime", ApplicationVersion = "10", HasExtensionPack = true, ExtensionPack = 310 },
            Fields = [new FixFieldDefinition { Tag = 9001, Name = "RuntimeField", FieldType = FixFieldType.String }]
        },
        Diagnostics = [new DictionaryDiagnostic { Severity = ValidationSeverity.Warning, Code = "runtime-source" }]
    };

    public DictionaryLoadResult ResolveDictionary(FixDictionaryIdentity identity)
    {
        CallCount++;
        LastIdentity = identity;
        return Result;
    }
}
