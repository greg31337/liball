using System;
using System.Collections.Generic;

namespace fix;

internal static class EmbeddedDictionaryCatalog
{
    internal static readonly List<EmbeddedDictionaryDescriptor> Descriptors = CreateDescriptors();

    internal static string ResolveResourceName(FixDictionaryIdentity identity)
    {
        if (!HasIdentityCriteria(identity))
        {
            return "";
        }
        for (int index = 0; index < Descriptors.Count; index++)
        {
            if (MatchesIdentity(Descriptors[index], identity))
            {
                return Descriptors[index].ResourceName;
            }
        }
        return "";
    }

    internal static bool HasIdentityCriteria(FixDictionaryIdentity identity)
    {
        return identity.Name.Length != 0 || identity.BeginString.Length != 0 || identity.ApplicationVersion.Length != 0 || identity.HasExtensionPack;
    }

    internal static bool MatchesIdentity(EmbeddedDictionaryDescriptor descriptor, FixDictionaryIdentity identity)
    {
        if (identity.CustomVersion.Length != 0 || identity.Scenario.Length != 0)
        {
            return false;
        }
        if (identity.Name.Length != 0 && !NameMatches(descriptor, identity.Name))
        {
            return false;
        }
        if (identity.BeginString.Length != 0 && descriptor.BeginString != identity.BeginString)
        {
            return false;
        }
        if (identity.ApplicationVersion.Length != 0 && !ApplicationVersionMatches(descriptor, identity.ApplicationVersion))
        {
            return false;
        }
        if (identity.HasExtensionPack && (!descriptor.HasExtensionPack || descriptor.ExtensionPack != identity.ExtensionPack))
        {
            return false;
        }
        return true;
    }

    internal static bool NameMatches(EmbeddedDictionaryDescriptor descriptor, string requestedName)
    {
        if (string.Equals(descriptor.Name, requestedName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        string compact = requestedName.Replace(".", "").Replace("_", "").Replace("-", "");
        string descriptorCompact = descriptor.Name.Replace(".", "").Replace("_", "").Replace("-", "");
        if (string.Equals(descriptorCompact, compact, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        return descriptor.Name == "FIX.Latest" && string.Equals(compact, "FIXLatestEP309", StringComparison.OrdinalIgnoreCase);
    }

    internal static bool ApplicationVersionMatches(EmbeddedDictionaryDescriptor descriptor, string requestedVersion)
    {
        if (descriptor.ApplicationVersion == requestedVersion)
        {
            return true;
        }
        if (descriptor.BeginString.Length != 0 && descriptor.BeginString != FixBeginStrings.Fixt11 && string.Equals(descriptor.BeginString, requestedVersion, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (descriptor.Name == "FIX50" && string.Equals(requestedVersion, "FIX.5.0", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }
        if (descriptor.Name == "FIX50SP1" && (string.Equals(requestedVersion, "FIX.5.0SP1", StringComparison.OrdinalIgnoreCase) || string.Equals(requestedVersion, "FIX.5.0 SP1", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }
        if (descriptor.Name == "FIX50SP2" && (string.Equals(requestedVersion, "FIX.5.0SP2", StringComparison.OrdinalIgnoreCase) || string.Equals(requestedVersion, "FIX.5.0 SP2", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }
        return descriptor.Name == "FIX.Latest" && string.Equals(requestedVersion, "FIX.Latest", StringComparison.OrdinalIgnoreCase);
    }

    internal static List<EmbeddedDictionaryDescriptor> CreateDescriptors()
    {
        List<EmbeddedDictionaryDescriptor> descriptors = [];
        descriptors.Add(CreateClassicDescriptor("FIX40", FixBeginStrings.Fix40, FixApplicationVersions.Fix40));
        descriptors.Add(CreateClassicDescriptor("FIX41", FixBeginStrings.Fix41, FixApplicationVersions.Fix41));
        descriptors.Add(CreateClassicDescriptor("FIX42", FixBeginStrings.Fix42, FixApplicationVersions.Fix42));
        descriptors.Add(CreateClassicDescriptor("FIX43", FixBeginStrings.Fix43, FixApplicationVersions.Fix43));
        descriptors.Add(CreateClassicDescriptor("FIX44", FixBeginStrings.Fix44, FixApplicationVersions.Fix44));
        EmbeddedDictionaryDescriptor fixt = new EmbeddedDictionaryDescriptor();
        fixt.ResourceName = "FIXT11.fixdict.gz";
        fixt.Name = "FIXT11";
        fixt.BeginString = FixBeginStrings.Fixt11;
        descriptors.Add(fixt);
        descriptors.Add(CreateApplicationDescriptor("FIX50", FixApplicationVersions.Fix50));
        descriptors.Add(CreateApplicationDescriptor("FIX50SP1", FixApplicationVersions.Fix50Sp1));
        descriptors.Add(CreateApplicationDescriptor("FIX50SP2", FixApplicationVersions.Fix50Sp2));
        EmbeddedDictionaryDescriptor latest = CreateApplicationDescriptor("FIX.Latest", FixApplicationVersions.FixLatest);
        latest.ResourceName = "FIXLatestEP309.fixdict.gz";
        latest.HasExtensionPack = true;
        latest.ExtensionPack = 309;
        descriptors.Add(latest);
        return descriptors;
    }

    internal static EmbeddedDictionaryDescriptor CreateClassicDescriptor(string name, string beginString, string applicationVersion)
    {
        EmbeddedDictionaryDescriptor descriptor = new EmbeddedDictionaryDescriptor();
        descriptor.ResourceName = name + ".fixdict.gz";
        descriptor.Name = name;
        descriptor.BeginString = beginString;
        descriptor.ApplicationVersion = applicationVersion;
        return descriptor;
    }

    internal static EmbeddedDictionaryDescriptor CreateApplicationDescriptor(string name, string applicationVersion)
    {
        EmbeddedDictionaryDescriptor descriptor = new EmbeddedDictionaryDescriptor();
        descriptor.ResourceName = name + ".fixdict.gz";
        descriptor.Name = name;
        descriptor.ApplicationVersion = applicationVersion;
        return descriptor;
    }
}
