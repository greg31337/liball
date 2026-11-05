namespace version;

/// <summary>
/// Represents a nuget package deprecation metadata.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class NuGetPackageDeprecationMetadata
{
    /// <summary>
    /// Stores the <c>deprecated</c> value.
    /// </summary>
    public bool deprecated;
    /// <summary>
    /// Stores the <c>reasons</c> value.
    /// </summary>
    public NuGetPackageDeprecationReason[] reasons;
    /// <summary>
    /// Stores the <c>message</c> value.
    /// </summary>
    public string message;
    /// <summary>
    /// Stores the <c>alternatePackage</c> value.
    /// </summary>
    public NuGetPackageVersionIdentity? alternatePackage;

    /// <summary>
    /// Initializes a new NuGet Package Deprecation Metadata instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="deprecatedValue">Whether to enable deprecated value.</param>
    /// <param name="reasonValues">The reason values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="messageValue">The message value used by the operation.</param>
    /// <param name="alternatePackageValue">The alternate package value used by the operation.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    /// <exception cref="System.ArgumentException">An argument is malformed or inconsistent with the requested operation.</exception>
    public NuGetPackageDeprecationMetadata(
        bool deprecatedValue,
        NuGetPackageDeprecationReason[] reasonValues,
        string messageValue,
        NuGetPackageVersionIdentity? alternatePackageValue)
    {
        System.ArgumentNullException.ThrowIfNull(reasonValues);
        System.ArgumentNullException.ThrowIfNull(messageValue);

        if (!deprecatedValue && reasonValues.Length > 0)
        {
            throw new System.ArgumentException("Non-deprecated NuGet metadata cannot contain deprecation reasons.");
        }

        if (!deprecatedValue && messageValue.Length > 0)
        {
            throw new System.ArgumentException("Non-deprecated NuGet metadata cannot contain a deprecation message.");
        }

        if (!deprecatedValue && alternatePackageValue != null)
        {
            throw new System.ArgumentException("Non-deprecated NuGet metadata cannot contain an alternate package.");
        }

        deprecated = deprecatedValue;
        reasons = CopyReasons(reasonValues);
        message = messageValue;
        alternatePackage = alternatePackageValue;
    }

    /// <summary>
    /// Creates a not deprecated.
    /// </summary>
    /// <returns>The resulting nuget package deprecation metadata.</returns>
    internal static NuGetPackageDeprecationMetadata CreateNotDeprecated()
    {
        return new NuGetPackageDeprecationMetadata(false, new NuGetPackageDeprecationReason[0], "", null);
    }

    /// <summary>
    /// Creates a deprecated.
    /// </summary>
    /// <param name="reasons">The reasons to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="message">The message used by the operation.</param>
    /// <param name="alternatePackage">The alternate package used by the operation.</param>
    /// <returns>The resulting nuget package deprecation metadata.</returns>
    internal static NuGetPackageDeprecationMetadata CreateDeprecated(
        NuGetPackageDeprecationReason[] reasons,
        string message,
        NuGetPackageVersionIdentity? alternatePackage)
    {
        return new NuGetPackageDeprecationMetadata(true, reasons, message, alternatePackage);
    }

    /// <summary>
    /// Reports whether deprecated.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsDeprecated()
    {
        return deprecated;
    }

    /// <summary>
    /// Gets a reason count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetReasonCount()
    {
        return reasons.Length;
    }

    /// <summary>
    /// Gets a reason.
    /// </summary>
    /// <param name="reasonIndex">The zero-based reason index.</param>
    /// <returns>The resulting nuget package deprecation reason.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal NuGetPackageDeprecationReason GetReason(int reasonIndex)
    {
        if (reasonIndex < 0 || reasonIndex >= reasons.Length)
        {
            throw new System.ArgumentOutOfRangeException("reasonIndex");
        }

        return reasons[reasonIndex];
    }

    /// <summary>
    /// Gets a reasons.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal NuGetPackageDeprecationReason[] GetReasons()
    {
        return CopyReasons(reasons);
    }

    /// <summary>
    /// Reports whether this value has message.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasMessage()
    {
        return message.Length > 0;
    }

    /// <summary>
    /// Gets a message.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetMessage()
    {
        return message;
    }

    /// <summary>
    /// Reports whether this value has alternate package.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasAlternatePackage()
    {
        return alternatePackage != null;
    }

    /// <summary>
    /// Gets an alternate package.
    /// </summary>
    /// <returns>The resulting nuget package version identity.</returns>
    /// <exception cref="System.InvalidOperationException">The requested value is unavailable because the operation did not complete successfully.</exception>
    internal NuGetPackageVersionIdentity GetAlternatePackage()
    {
        if (alternatePackage == null)
        {
            throw new System.InvalidOperationException("Deprecation metadata does not contain an alternate package.");
        }

        return alternatePackage;
    }

    internal static NuGetPackageDeprecationReason[] CopyReasons(NuGetPackageDeprecationReason[] reasonValues)
    {
        NuGetPackageDeprecationReason[] copy = new NuGetPackageDeprecationReason[reasonValues.Length];
        int index = 0;

        while (index < reasonValues.Length)
        {
            ValidateReason(reasonValues[index]);
            copy[index] = reasonValues[index];
            index = index + 1;
        }

        return copy;
    }

    internal static void ValidateReason(NuGetPackageDeprecationReason reason)
    {
        if (reason == NuGetPackageDeprecationReason.Other)
        {
            return;
        }

        if (reason == NuGetPackageDeprecationReason.Legacy)
        {
            return;
        }

        if (reason == NuGetPackageDeprecationReason.CriticalBugs)
        {
            return;
        }

        throw new System.ArgumentOutOfRangeException("reason");
    }
}
