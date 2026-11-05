using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace fix;

/// <summary>Resolves detached snapshots from the built-in FIX dictionary catalog.</summary>
public class EmbeddedDictionaryProvider : IDictionaryProvider
{
    internal static readonly object CacheLock = new object();
    internal static readonly Dictionary<string, FixDictionary> Cache = new Dictionary<string, FixDictionary>();
    internal IDictionaryProvider? RuntimeProvider;

    /// <summary>Creates an embedded provider with an optional provider for unknown or newer identities.</summary>
    /// <param name="runtimeProvider">The runtime fallback provider, or <c>null</c> to return missing-dictionary diagnostics.</param>
    public EmbeddedDictionaryProvider(IDictionaryProvider? runtimeProvider)
    {
        RuntimeProvider = runtimeProvider;
    }

    /// <summary>Resolves a detached transport or application dictionary snapshot.</summary>
    /// <param name="identity">The requested dictionary identity or supported alias.</param>
    /// <returns>The detached dictionary and ordered resolution diagnostics.</returns>
    public DictionaryLoadResult ResolveDictionary(FixDictionaryIdentity identity)
    {
        string resourceName = EmbeddedDictionaryCatalog.ResolveResourceName(identity);
        if (resourceName.Length != 0)
        {
            FixDictionary snapshot = ResolveEmbeddedSnapshot(resourceName);
            return CreateSnapshotResult(snapshot);
        }
        if (RuntimeProvider != null)
        {
            DictionaryLoadResult fallback = RuntimeProvider.ResolveDictionary(identity);
            return CloneFallbackResult(fallback);
        }
        return CreateMissingResult(identity);
    }

    internal static FixDictionary ResolveEmbeddedSnapshot(string resourceName)
    {
        lock (CacheLock)
        {
            if (Cache.ContainsKey(resourceName))
            {
                return Cache[resourceName];
            }
            string manifestName = "fix.Resources.Dictionaries." + resourceName;
            Stream? possibleStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(manifestName);
            if (possibleStream == null)
            {
                throw new FixDictionaryException("The embedded dictionary resource is missing: " + resourceName);
            }
            FixDictionary dictionary;
            using (Stream resourceStream = possibleStream)
            {
                dictionary = EmbeddedDictionaryFormat.ReadResource(resourceStream);
            }
            Cache.Add(resourceName, dictionary);
            return dictionary;
        }
    }

    internal static DictionaryLoadResult CreateSnapshotResult(FixDictionary snapshot)
    {
        FixDictionary clone = DictionaryComposition.CloneDictionaryGraph(snapshot, snapshot.Options, snapshot.Identity.Source);
        DictionaryLoadResult result = new DictionaryLoadResult();
        result.Success = true;
        result.Dictionary = clone;
        result.Diagnostics = clone.Diagnostics;
        return result;
    }

    internal static DictionaryLoadResult CloneFallbackResult(DictionaryLoadResult source)
    {
        DictionaryLoadResult clone = new DictionaryLoadResult();
        clone.Success = source.Success;
        if (source.Dictionary != null)
        {
            clone.Dictionary = DictionaryComposition.CloneDictionaryGraph(source.Dictionary, source.Dictionary.Options, DictionaryComposition.ResolveDictionaryOrigin(source.Dictionary));
        }
        DictionaryComposition.CopyOverlayDiagnostics(clone.Diagnostics, source.Diagnostics);
        return clone;
    }

    internal static DictionaryLoadResult CreateMissingResult(FixDictionaryIdentity identity)
    {
        DictionaryLoadResult result = new DictionaryLoadResult();
        DictionaryDiagnostic diagnostic = new DictionaryDiagnostic();
        diagnostic.Severity = ValidationSeverity.Error;
        diagnostic.Code = EmbeddedDictionaryCatalog.HasIdentityCriteria(identity) ? "dictionary-not-found" : "missing-dictionary-identity";
        diagnostic.Message = EmbeddedDictionaryCatalog.HasIdentityCriteria(identity) ? "No embedded or runtime dictionary matches the requested identity." : "A dictionary identity must specify a name, BeginString, application version, or extension pack.";
        diagnostic.DefinitionName = identity.Name;
        diagnostic.Source = identity.Source;
        result.Diagnostics.Add(diagnostic);
        return result;
    }
}
