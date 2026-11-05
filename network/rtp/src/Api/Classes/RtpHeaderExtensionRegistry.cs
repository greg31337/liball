using System;

namespace rtp;

/// <summary>
/// Stores validated signaling-supplied RTP header-extension mappings.
/// </summary>
public class RtpHeaderExtensionRegistry : IRtpHeaderExtensionRegistry
{
    internal object syncRoot = new object();
    internal RtpHeaderExtensionMapping[] mappings = [];

    /// <summary>
    /// Initializes an empty RTP header-extension mapping registry.
    /// </summary>
    public RtpHeaderExtensionRegistry()
    {
    }

    /// <summary>Adds or replaces a header-extension mapping.</summary>
    /// <param name="mapping">The mapping to validate and store independently.</param>
    public void SetHeaderExtensionMapping(RtpHeaderExtensionMapping mapping)
    {
        RtpHeaderExtensionMapping mappingCopy =
            RtpApiCopy.CopyValidatedHeaderExtensionMapping(mapping);
        lock (syncRoot)
        {
            int replacementIndex = FindStoredMappingIndex(mappingCopy.Identifier);
            int candidateLength = mappings.Length;
            if (replacementIndex < 0)
            {
                candidateLength = candidateLength + 1;
            }

            RtpHeaderExtensionMapping[] candidate =
                new RtpHeaderExtensionMapping[candidateLength];
            int mappingIndex = 0;
            while (mappingIndex < mappings.Length)
            {
                candidate[mappingIndex] = mappings[mappingIndex];
                mappingIndex = mappingIndex + 1;
            }

            if (replacementIndex >= 0)
            {
                candidate[replacementIndex] = mappingCopy;
            }
            else
            {
                candidate[candidate.Length - 1] = mappingCopy;
            }

            RtpConfigurationValidator.ValidateHeaderExtensionMappings(candidate);
            mappings = OrderMappings(candidate);
        }
    }

    /// <summary>Finds a header-extension mapping by local identifier.</summary>
    /// <param name="identifier">The locally negotiated identifier.</param>
    /// <returns>The mapping copy, or <c>null</c> when no mapping exists.</returns>
    public RtpHeaderExtensionMapping? FindHeaderExtensionMapping(int identifier)
    {
        ValidateIdentifier(identifier);
        lock (syncRoot)
        {
            int mappingIndex = FindStoredMappingIndex(identifier);
            if (mappingIndex < 0)
            {
                return null;
            }

            return mappings[mappingIndex].Copy();
        }
    }

    /// <summary>Finds a header-extension mapping by URI.</summary>
    /// <param name="uri">The registered header-extension URI.</param>
    /// <returns>The mapping copy, or <c>null</c> when no mapping exists.</returns>
    public RtpHeaderExtensionMapping? FindHeaderExtensionMappingByUri(string uri)
    {
        if (uri == null || uri.Length == 0)
        {
            throw new RtpConfigurationException(
                "The header-extension URI is required for lookup.");
        }

        lock (syncRoot)
        {
            int mappingIndex = 0;
            while (mappingIndex < mappings.Length)
            {
                if (string.Equals(mappings[mappingIndex].Uri, uri, StringComparison.Ordinal))
                {
                    return mappings[mappingIndex].Copy();
                }

                mappingIndex = mappingIndex + 1;
            }

            return null;
        }
    }

    /// <summary>Removes a header-extension mapping.</summary>
    /// <param name="identifier">The locally negotiated identifier to remove.</param>
    /// <returns><c>true</c> when a mapping was removed.</returns>
    public bool RemoveHeaderExtensionMapping(int identifier)
    {
        ValidateIdentifier(identifier);
        lock (syncRoot)
        {
            int removalIndex = FindStoredMappingIndex(identifier);
            if (removalIndex < 0)
            {
                return false;
            }

            RtpHeaderExtensionMapping[] remaining =
                new RtpHeaderExtensionMapping[mappings.Length - 1];
            int sourceIndex = 0;
            int destinationIndex = 0;
            while (sourceIndex < mappings.Length)
            {
                if (sourceIndex != removalIndex)
                {
                    remaining[destinationIndex] = mappings[sourceIndex];
                    destinationIndex = destinationIndex + 1;
                }

                sourceIndex = sourceIndex + 1;
            }

            mappings = remaining;
            return true;
        }
    }

    /// <summary>Returns independent copies of all registered mappings.</summary>
    /// <returns>The registered mappings in identifier order.</returns>
    public RtpHeaderExtensionMapping[] GetHeaderExtensionMappings()
    {
        lock (syncRoot)
        {
            RtpHeaderExtensionMapping[] copies =
                new RtpHeaderExtensionMapping[mappings.Length];
            int mappingIndex = 0;
            while (mappingIndex < mappings.Length)
            {
                copies[mappingIndex] = mappings[mappingIndex].Copy();
                mappingIndex = mappingIndex + 1;
            }

            return copies;
        }
    }

    internal int FindStoredMappingIndex(int identifier)
    {
        int mappingIndex = 0;
        while (mappingIndex < mappings.Length)
        {
            if (mappings[mappingIndex].Identifier == identifier)
            {
                return mappingIndex;
            }

            mappingIndex = mappingIndex + 1;
        }

        return -1;
    }

    internal static void ValidateIdentifier(int identifier)
    {
        if (identifier <= 0 || identifier > 255)
        {
            throw new RtpConfigurationException(
                "Header-extension identifier must be between 1 and 255.");
        }
    }

    internal static RtpHeaderExtensionMapping[] OrderMappings(
        RtpHeaderExtensionMapping[] unordered)
    {
        RtpHeaderExtensionMapping[] ordered =
            new RtpHeaderExtensionMapping[unordered.Length];
        int mappingIndex = 0;
        while (mappingIndex < unordered.Length)
        {
            ordered[mappingIndex] = unordered[mappingIndex];
            mappingIndex = mappingIndex + 1;
        }

        mappingIndex = 1;
        while (mappingIndex < ordered.Length)
        {
            RtpHeaderExtensionMapping mapping = ordered[mappingIndex];
            int insertionIndex = mappingIndex;
            while (insertionIndex > 0 &&
                ordered[insertionIndex - 1].Identifier > mapping.Identifier)
            {
                ordered[insertionIndex] = ordered[insertionIndex - 1];
                insertionIndex = insertionIndex - 1;
            }

            ordered[insertionIndex] = mapping;
            mappingIndex = mappingIndex + 1;
        }

        return ordered;
    }
}
