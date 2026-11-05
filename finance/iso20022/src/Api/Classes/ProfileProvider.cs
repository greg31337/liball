using System;
using System.Collections.Generic;

namespace iso20022;

/// <summary>Provides deterministic lookup over an immutable registration list of validation profiles.</summary>
public class ProfileProvider : IProfileProvider
{
    internal List<ProfileDefinition> profiles = new List<ProfileDefinition>();

    /// <summary>Initializes a provider from caller-owned profile definitions without mutating the supplied list.</summary>
    /// <param name="profileDefinitions">The profiles available for exact or latest-version lookup.</param>
    /// <exception cref="ArgumentNullException">Thrown when the profile list is null.</exception>
    /// <exception cref="ArgumentException">Thrown when a profile is null, incomplete, or duplicates an exact identity and version.</exception>
    public ProfileProvider(List<ProfileDefinition>? profileDefinitions)
    {
        if (profileDefinitions == null)
        {
            throw new ArgumentNullException("profileDefinitions");
        }
        int index = 0;
        while (index < profileDefinitions.Count)
        {
            ProfileDefinition? profile = profileDefinitions[index];
            if (profile == null || profile.Identifier == null ||
                profile.Identifier.Length == 0 || profile.Version == null ||
                profile.Version.Length == 0)
            {
                throw new ArgumentException(
                    "Every registered profile requires a stable identifier and exact version.",
                    "profileDefinitions");
            }
            if (FindExactRegisteredProfile(
                profiles,
                profile.Identifier,
                profile.Version) != null)
            {
                throw new ArgumentException(
                    "Profile identifiers and versions must be unique.",
                    "profileDefinitions");
            }
            profiles.Add(profile);
            index++;
        }
    }

    /// <summary>Finds one exact profile version or the latest registered version when no version is supplied.</summary>
    /// <param name="identifier">The stable profile identifier.</param>
    /// <param name="version">The exact profile version, or an empty value for the latest registered version.</param>
    /// <returns>The selected profile, or <c>null</c> when no profile matches.</returns>
    public ProfileDefinition? FindProfile(string? identifier, string? version)
    {
        if (identifier == null || identifier.Length == 0 || version == null)
        {
            return null;
        }
        if (version.Length > 0)
        {
            return FindExactRegisteredProfile(profiles, identifier, version);
        }
        ProfileDefinition? selected = null;
        int index = 0;
        while (index < profiles.Count)
        {
            ProfileDefinition profile = profiles[index];
            if (profile.Identifier == identifier &&
                (selected == null || CompareProfileVersions(
                    profile.Version,
                    selected.Version) > 0))
            {
                selected = profile;
            }
            index++;
        }
        return selected;
    }

    internal static ProfileDefinition? FindExactRegisteredProfile(
        List<ProfileDefinition> values,
        string identifier,
        string version)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index].Identifier == identifier &&
                values[index].Version == version)
            {
                return values[index];
            }
            index++;
        }
        return null;
    }

    internal static int CompareProfileVersions(string left, string right)
    {
        string[] leftParts = left.Split('.');
        string[] rightParts = right.Split('.');
        int count = leftParts.Length;
        if (rightParts.Length > count)
        {
            count = rightParts.Length;
        }
        int index = 0;
        while (index < count)
        {
            string leftPart = "0";
            string rightPart = "0";
            if (index < leftParts.Length)
            {
                leftPart = leftParts[index];
            }
            if (index < rightParts.Length)
            {
                rightPart = rightParts[index];
            }
            int comparison = CompareProfileVersionPart(leftPart, rightPart);
            if (comparison != 0)
            {
                return comparison;
            }
            index++;
        }
        return string.CompareOrdinal(left, right);
    }

    internal static int CompareProfileVersionPart(string left, string right)
    {
        if (!ProfileVersionPartIsNumeric(left) ||
            !ProfileVersionPartIsNumeric(right))
        {
            return string.CompareOrdinal(left, right);
        }
        int leftIndex = 0;
        while (leftIndex + 1 < left.Length && left[leftIndex] == '0')
        {
            leftIndex++;
        }
        int rightIndex = 0;
        while (rightIndex + 1 < right.Length && right[rightIndex] == '0')
        {
            rightIndex++;
        }
        int leftLength = left.Length - leftIndex;
        int rightLength = right.Length - rightIndex;
        if (leftLength != rightLength)
        {
            return leftLength < rightLength ? -1 : 1;
        }
        return string.CompareOrdinal(
            left.Substring(leftIndex),
            right.Substring(rightIndex));
    }

    internal static bool ProfileVersionPartIsNumeric(string value)
    {
        if (value.Length == 0)
        {
            return false;
        }
        int index = 0;
        while (index < value.Length)
        {
            if (value[index] < '0' || value[index] > '9')
            {
                return false;
            }
            index++;
        }
        return true;
    }
}
