using System;
using System.Collections.Generic;

namespace fake;

internal class GenerationContext
{
    internal FakeOptions CapturedOptions;
    internal GenerationMode GenerationModeValue;
    internal SafetyMode SafetyModeValue;
    internal StrictnessMode StrictnessModeValue;
    internal NormalizationMode NormalizationModeValue;
    internal DateTimeOffset ReferenceTimeValue;
    internal LocaleResourceStore LocaleResources;
    internal LocaleRegistrationStore LocaleRegistrations;
    Dictionary<string, UniqueScope> UniqueScopes;
    Dictionary<string, NamedSequenceScope> SequenceScopes;
    object UniqueSync;
    object SequenceSync;

    internal GenerationContext(FakeOptions options)
    {
        CapturedOptions = options.Copy();
        GenerationModeValue = options.GenerationMode;
        SafetyModeValue = options.SafetyMode;
        StrictnessModeValue = options.StrictnessMode;
        NormalizationModeValue = options.NormalizationMode;
        ReferenceTimeValue = options.ReferenceTime;
        LocaleResources = LocaleResourceStore.Product();
        LocaleRegistrations = new LocaleRegistrationStore(
            options.DataVersion,
            options.MaximumOutputSize,
            options.MaximumCount);
        UniqueScopes = new Dictionary<string, UniqueScope>(StringComparer.Ordinal);
        SequenceScopes = new Dictionary<string, NamedSequenceScope>(
            StringComparer.Ordinal);
        UniqueSync = new object();
        SequenceSync = new object();
    }

    internal void AttachModes(GenerationMetadata metadata)
    {
        Guard.NotNull(metadata, "metadata");
        metadata.GenerationMode = GenerationModeValue;
        metadata.SafetyMode = SafetyModeValue;
        metadata.StrictnessMode = StrictnessModeValue;
        metadata.NormalizationMode = NormalizationModeValue;
    }

    internal void AttachModesFromMetadata(EdgeSelectionResult metadata)
    {
        Guard.NotNull(metadata, "metadata");
        metadata.SafetyMode = SafetyModeValue;
        metadata.StrictnessMode = StrictnessModeValue;
        metadata.NormalizationMode = NormalizationModeValue;
    }

    internal DateTimeOffset ReferenceTime()
    {
        return ReferenceTimeValue;
    }

    internal FakeOptions OptionsSnapshot()
    {
        return CapturedOptions.Copy();
    }

    internal FakeRandom ProviderRandom(string name)
    {
        ulong seed = StableSeed.FromLong(CapturedOptions.Seed);
        ulong providerSeed = StableSeed.Derive(seed, "provider", name);
        return FakeRandom.FromSeed(this, providerSeed);
    }

    internal LocaleFieldValue[] LocaleValues()
    {
        LocaleFieldValue[] embedded;
        if (LocaleRegistrations.RegistrationCount() == 0)
        {
            embedded = LocaleResources.Values(CapturedOptions.DataVersion);
        }
        else
        {
            embedded = LocaleResources.ValuesOrEmpty(CapturedOptions.DataVersion);
        }
        return LocaleRegistrations.Merge(embedded);
    }

    internal UniqueScope GetUniqueScope(string name)
    {
        Guard.NotNullOrWhiteSpace(name, "name");
        lock (UniqueSync)
        {
            if (!UniqueScopes.ContainsKey(name))
            {
                UniqueScopes[name] = new UniqueScope(
                    name,
                    CapturedOptions.UniquenessRetryLimit,
                    CapturedOptions.MaximumCount,
                    CapturedOptions.MaximumOutputSize);
            }
            return UniqueScopes[name];
        }
    }

    internal void ResetUnique(string name)
    {
        GetUniqueScope(name).Reset();
    }

    internal void ResetUniqueFromNoArguments()
    {
        UniqueScope[] scopes;
        lock (UniqueSync)
        {
            scopes = new UniqueScope[UniqueScopes.Count];
            UniqueScopes.Values.CopyTo(scopes, 0);
        }
        int index = 0;
        while (index < scopes.Length)
        {
            scopes[index].Reset();
            index = index + 1;
        }
    }

    internal SequenceState GetSequenceScope(
        string name,
        long start,
        long increment)
    {
        Guard.NotNullOrWhiteSpace(name, "name");
        if (increment == 0L)
            throw new ArgumentOutOfRangeException(
                "increment",
                increment,
                "increment must not be zero.");
        NamedSequenceScope scope;
        lock (SequenceSync)
        {
            if (!SequenceScopes.ContainsKey(name))
            {
                SequenceScopes[name] = new NamedSequenceScope(
                    start,
                    increment,
                    CapturedOptions.MaximumCount);
            }
            scope = SequenceScopes[name];
        }
        scope.RequireConfiguration(name, start, increment);
        return scope.State;
    }

    internal void ResetSequence(string name)
    {
        Guard.NotNullOrWhiteSpace(name, "name");
        SequenceState? state = null;
        lock (SequenceSync)
        {
            if (SequenceScopes.ContainsKey(name))
                state = SequenceScopes[name].State;
        }
        if (state != null)
            state.Reset();
    }

    internal void ResetSequenceFromNoArguments()
    {
        NamedSequenceScope[] scopes;
        lock (SequenceSync)
        {
            scopes = new NamedSequenceScope[SequenceScopes.Count];
            SequenceScopes.Values.CopyTo(scopes, 0);
        }
        int index = 0;
        while (index < scopes.Length)
        {
            scopes[index].State.Reset();
            index = index + 1;
        }
    }
}
