namespace version;

/// <summary>
/// Represents a cargo dependency.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class CargoDependency
{
    /// <summary>
    /// Stores the <c>packageName</c> value.
    /// </summary>
    public CargoPackageName packageName;
    /// <summary>
    /// Stores the <c>versionRequirementText</c> value.
    /// </summary>
    public string versionRequirementText;
    /// <summary>
    /// Stores the <c>versionRequirementSpecified</c> value.
    /// </summary>
    public bool versionRequirementSpecified;
    /// <summary>
    /// Stores the <c>versionRequirement</c> value.
    /// </summary>
    public VersionRange? versionRequirement;
    /// <summary>
    /// Stores the <c>dependencyKind</c> value.
    /// </summary>
    public CargoDependencyKind dependencyKind;
    /// <summary>
    /// Stores the <c>optional</c> value.
    /// </summary>
    public bool optional;
    /// <summary>
    /// Stores the <c>defaultFeatures</c> value.
    /// </summary>
    public bool defaultFeatures;
    /// <summary>
    /// Stores the <c>features</c> value.
    /// </summary>
    public string[] features;
    /// <summary>
    /// Stores the <c>registryName</c> value.
    /// </summary>
    public string registryName;
    /// <summary>
    /// Stores the <c>registrySpecified</c> value.
    /// </summary>
    public bool registrySpecified;
    /// <summary>
    /// Stores the <c>targetTriple</c> value.
    /// </summary>
    public string targetTriple;
    /// <summary>
    /// Stores the <c>targetSpecified</c> value.
    /// </summary>
    public bool targetSpecified;

    /// <summary>
    /// Initializes a new Cargo Dependency instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="packageNameValue">The package name value used by the operation.</param>
    /// <param name="versionRequirementTextValue">The version requirement text value to process.</param>
    /// <param name="dependencyKindValue">The dependency kind value used by the operation.</param>
    /// <param name="optionalValue">Whether to enable optional value.</param>
    /// <param name="defaultFeaturesValue">Whether to enable default features value.</param>
    /// <param name="featureValues">The feature values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="registryNameValue">The registry name value used by the operation.</param>
    /// <param name="targetTripleValue">The target triple value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public CargoDependency(
        CargoPackageName packageNameValue,
        string? versionRequirementTextValue,
        CargoDependencyKind dependencyKindValue,
        bool optionalValue,
        bool defaultFeaturesValue,
        string[] featureValues,
        string? registryNameValue,
        string? targetTripleValue)
    {
        System.ArgumentNullException.ThrowIfNull(packageNameValue);
        System.ArgumentNullException.ThrowIfNull(featureValues);
        ValidateDependencyKind(dependencyKindValue);

        packageName = packageNameValue;
        dependencyKind = dependencyKindValue;
        optional = optionalValue;
        defaultFeatures = defaultFeaturesValue;
        features = CopyFeatureNames(featureValues);

        if (versionRequirementTextValue == null || versionRequirementTextValue.Length == 0)
        {
            versionRequirementText = "";
            versionRequirementSpecified = false;
            versionRequirement = null;
        }
        else
        {
            VersionRangeParseResult result = CargoRangeParser.Parse(versionRequirementTextValue);
            if (result.IsFailure())
            {
                throw new System.ArgumentException("Invalid Cargo dependency version requirement.");
            }

            versionRequirementText = versionRequirementTextValue;
            versionRequirementSpecified = true;
            versionRequirement = result.GetRange();
        }

        registryName = NormalizeOptionalIdentifier(registryNameValue, "Cargo dependency registry name cannot contain invalid characters.", true);
        registrySpecified = registryName.Length > 0;
        targetTriple = NormalizeOptionalIdentifier(targetTripleValue, "Cargo dependency target triple cannot contain invalid characters.", false);
        targetSpecified = targetTriple.Length > 0;
    }

    /// <summary>
    /// Creates a new instance from the supplied identity and metadata values.
    /// </summary>
    /// <param name="packageNameText">The package name text to process.</param>
    /// <param name="versionRequirementText">The version requirement text to process.</param>
    /// <returns>The resulting cargo dependency.</returns>
    internal static CargoDependency Create(string packageNameText, string versionRequirementText)
    {
        return new CargoDependency(
            new CargoPackageName(packageNameText),
            versionRequirementText,
            CargoDependencyKind.Normal,
            false,
            true,
            new string[0],
            null,
            null);
    }

    /// <summary>
    /// Gets a package name.
    /// </summary>
    /// <returns>The resulting cargo package name.</returns>
    internal CargoPackageName GetPackageName()
    {
        return packageName;
    }

    /// <summary>
    /// Reports whether this value has version requirement.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasVersionRequirement()
    {
        return versionRequirementSpecified;
    }

    /// <summary>
    /// Gets a version requirement text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetVersionRequirementText()
    {
        return versionRequirementText;
    }

    /// <summary>
    /// Gets a version requirement.
    /// </summary>
    /// <returns>The resulting version range.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal VersionRange GetVersionRequirement()
    {
        if (versionRequirement == null)
        {
            throw new System.InvalidOperationException("Cargo dependency does not contain a version requirement.");
        }

        return versionRequirement;
    }

    /// <summary>
    /// Gets a dependency kind.
    /// </summary>
    /// <returns>The resulting cargo dependency kind.</returns>
    internal CargoDependencyKind GetDependencyKind()
    {
        return dependencyKind;
    }

    /// <summary>
    /// Reports whether optional.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsOptional()
    {
        return optional;
    }

    /// <summary>
    /// Reports whether default features.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool UsesDefaultFeatures()
    {
        return defaultFeatures;
    }

    /// <summary>
    /// Gets a feature count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetFeatureCount()
    {
        return features.Length;
    }

    /// <summary>
    /// Gets a feature.
    /// </summary>
    /// <param name="featureIndex">The zero-based feature index.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal string GetFeature(int featureIndex)
    {
        if (featureIndex < 0 || featureIndex >= features.Length)
        {
            throw new System.ArgumentOutOfRangeException("featureIndex");
        }

        return features[featureIndex];
    }

    /// <summary>
    /// Gets a features.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal string[] GetFeatures()
    {
        return CopyStrings(features);
    }

    /// <summary>
    /// Reports whether this value has registry name.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasRegistryName()
    {
        return registrySpecified;
    }

    /// <summary>
    /// Gets a registry name.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetRegistryName()
    {
        return registryName;
    }

    /// <summary>
    /// Reports whether this value has target triple.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasTargetTriple()
    {
        return targetSpecified;
    }

    /// <summary>
    /// Gets a target triple.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetTargetTriple()
    {
        return targetTriple;
    }

    /// <summary>
    /// Reports whether version text.
    /// </summary>
    /// <param name="versionText">The version text to process.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool AllowsVersionText(string versionText)
    {
        if (versionRequirement == null)
        {
            return true;
        }

        return versionRequirement.ContainsText(versionText, VersionDialect.Cargo);
    }

    internal static void ValidateDependencyKind(CargoDependencyKind dependencyKindValue)
    {
        if (dependencyKindValue == CargoDependencyKind.Normal)
        {
            return;
        }

        if (dependencyKindValue == CargoDependencyKind.Development)
        {
            return;
        }

        if (dependencyKindValue == CargoDependencyKind.Build)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("dependencyKindValue");
    }

    internal static string[] CopyFeatureNames(string[] featureValues)
    {
        string[] copy = new string[featureValues.Length];
        int index = 0;

        while (index < featureValues.Length)
        {
            CargoFeature.ValidateFeatureToken(featureValues[index], "Cargo dependency feature arrays cannot contain empty or invalid values.");
            copy[index] = featureValues[index];
            index = index + 1;
        }

        return copy;
    }

    internal static string NormalizeOptionalIdentifier(string? text, string message, bool packageIdentifier)
    {
        if (text == null || text.Length == 0)
        {
            return "";
        }

        if (packageIdentifier)
        {
            CargoPackageName.ValidateName(text, message);
        }
        else
        {
            ValidateTargetText(text, message);
        }

        return text;
    }

    /// <summary>
    /// Validates a target text.
    /// </summary>
    /// <param name="text">The text to process.</param>
    /// <param name="message">The message used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    internal static void ValidateTargetText(string text, string message)
    {
        System.ArgumentNullException.ThrowIfNull(text);

        if (text.Length == 0)
        {
            throw new System.ArgumentException(message);
        }

        int index = 0;
        while (index < text.Length)
        {
            if (!VersionChar.IsAlphaNumeric(text[index])
                && text[index] != '-'
                && text[index] != '_'
                && text[index] != '.'
                && text[index] != '/'
                && text[index] != ':'
                && text[index] != '('
                && text[index] != ')'
                && text[index] != '='
                && text[index] != '!'
                && text[index] != ','
                && text[index] != '\''
                && text[index] != '"'
                && !VersionChar.IsWhitespace(text[index]))
            {
                throw new System.ArgumentException(message);
            }

            index = index + 1;
        }
    }

    internal static string[] CopyStrings(string[] values)
    {
        string[] copy = new string[values.Length];
        int index = 0;

        while (index < values.Length)
        {
            copy[index] = values[index];
            index = index + 1;
        }

        return copy;
    }
}
