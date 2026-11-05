namespace version;

/// <summary>
/// Represents a python version.
/// </summary>
/// <remarks>
/// State is exposed through public fields in accordance with the library's data-oriented API model.
/// </remarks>
public class PythonVersion
{
    /// <summary>
    /// Stores the <c>originalText</c> value.
    /// </summary>
    public string originalText;
    /// <summary>
    /// Stores the <c>epoch</c> value.
    /// </summary>
    public string epoch;
    /// <summary>
    /// Stores the <c>releaseSegments</c> value.
    /// </summary>
    public string[] releaseSegments;
    /// <summary>
    /// Stores the <c>preReleasePresent</c> value.
    /// </summary>
    public bool preReleasePresent;
    /// <summary>
    /// Stores the <c>preReleaseLabel</c> value.
    /// </summary>
    public string preReleaseLabel;
    /// <summary>
    /// Stores the <c>preReleaseNumber</c> value.
    /// </summary>
    public string preReleaseNumber;
    /// <summary>
    /// Stores the <c>postReleasePresent</c> value.
    /// </summary>
    public bool postReleasePresent;
    /// <summary>
    /// Stores the <c>postReleaseNumber</c> value.
    /// </summary>
    public string postReleaseNumber;
    /// <summary>
    /// Stores the <c>developmentReleasePresent</c> value.
    /// </summary>
    public bool developmentReleasePresent;
    /// <summary>
    /// Stores the <c>developmentReleaseNumber</c> value.
    /// </summary>
    public string developmentReleaseNumber;
    /// <summary>
    /// Stores the <c>localSegments</c> value.
    /// </summary>
    public PythonLocalVersionSegment[] localSegments;

    /// <summary>
    /// Initializes a new Python Version instance from the supplied values.
    /// </summary>
    /// <remarks>
    /// The constructor validates the supplied state before the instance is made available to callers.
    /// </remarks>
    /// <param name="originalTextValue">The original text value to process.</param>
    /// <param name="epochValue">The epoch value used by the operation.</param>
    /// <param name="releaseSegmentValues">The release segment values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <param name="preReleasePresentValue">Whether to enable pre release present value.</param>
    /// <param name="preReleaseLabelValue">The pre release label value used by the operation.</param>
    /// <param name="preReleaseNumberValue">The pre release number value used by the operation.</param>
    /// <param name="postReleasePresentValue">Whether to enable post release present value.</param>
    /// <param name="postReleaseNumberValue">The post release number value used by the operation.</param>
    /// <param name="developmentReleasePresentValue">Whether to enable development release present value.</param>
    /// <param name="developmentReleaseNumberValue">The development release number value used by the operation.</param>
    /// <param name="localSegmentValues">The local segment values to examine; entries are processed in array order unless the method states otherwise.</param>
    /// <exception cref="System.ArgumentNullException">A required argument is null.</exception>
    public PythonVersion(
        string originalTextValue,
        string epochValue,
        string[] releaseSegmentValues,
        bool preReleasePresentValue,
        string preReleaseLabelValue,
        string preReleaseNumberValue,
        bool postReleasePresentValue,
        string postReleaseNumberValue,
        bool developmentReleasePresentValue,
        string developmentReleaseNumberValue,
        PythonLocalVersionSegment[] localSegmentValues)
    {
        System.ArgumentNullException.ThrowIfNull(originalTextValue);
        System.ArgumentNullException.ThrowIfNull(epochValue);
        System.ArgumentNullException.ThrowIfNull(releaseSegmentValues);
        System.ArgumentNullException.ThrowIfNull(preReleaseLabelValue);
        System.ArgumentNullException.ThrowIfNull(preReleaseNumberValue);
        System.ArgumentNullException.ThrowIfNull(postReleaseNumberValue);
        System.ArgumentNullException.ThrowIfNull(developmentReleaseNumberValue);
        System.ArgumentNullException.ThrowIfNull(localSegmentValues);

        ValidateEpoch(epochValue);
        ValidateReleaseSegments(releaseSegmentValues);
        ValidatePreRelease(preReleasePresentValue, preReleaseLabelValue, preReleaseNumberValue);
        ValidateNumberPresence(postReleasePresentValue, postReleaseNumberValue, "Post-release number");
        ValidateNumberPresence(developmentReleasePresentValue, developmentReleaseNumberValue, "Development release number");

        originalText = originalTextValue;
        epoch = epochValue;
        releaseSegments = CopyReleaseSegments(releaseSegmentValues);
        preReleasePresent = preReleasePresentValue;
        preReleaseLabel = preReleaseLabelValue;
        preReleaseNumber = preReleaseNumberValue;
        postReleasePresent = postReleasePresentValue;
        postReleaseNumber = postReleaseNumberValue;
        developmentReleasePresent = developmentReleasePresentValue;
        developmentReleaseNumber = developmentReleaseNumberValue;
        localSegments = CopyLocalSegments(localSegmentValues);
    }

    /// <summary>
    /// Gets an original text.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetOriginalText()
    {
        return originalText;
    }

    /// <summary>
    /// Gets an epoch.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetEpoch()
    {
        return epoch;
    }

    /// <summary>
    /// Gets a release segment count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetReleaseSegmentCount()
    {
        return releaseSegments.Length;
    }

    /// <summary>
    /// Gets a release segment.
    /// </summary>
    /// <param name="segmentIndex">The zero-based segment index.</param>
    /// <returns>The resulting text.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal string GetReleaseSegment(int segmentIndex)
    {
        if (segmentIndex < 0 || segmentIndex >= releaseSegments.Length)
        {
            throw new System.ArgumentOutOfRangeException("segmentIndex");
        }

        return releaseSegments[segmentIndex];
    }

    /// <summary>
    /// Gets a release segments.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal string[] GetReleaseSegments()
    {
        return CopyReleaseSegments(releaseSegments);
    }

    /// <summary>
    /// Reports whether this value has pre release.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasPreRelease()
    {
        return preReleasePresent;
    }

    /// <summary>
    /// Gets a pre release label.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetPreReleaseLabel()
    {
        return preReleaseLabel;
    }

    /// <summary>
    /// Gets a pre release number.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetPreReleaseNumber()
    {
        return preReleaseNumber;
    }

    /// <summary>
    /// Reports whether this value has post release.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasPostRelease()
    {
        return postReleasePresent;
    }

    /// <summary>
    /// Gets a post release number.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetPostReleaseNumber()
    {
        return postReleaseNumber;
    }

    /// <summary>
    /// Reports whether this value has development release.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasDevelopmentRelease()
    {
        return developmentReleasePresent;
    }

    /// <summary>
    /// Gets a development release number.
    /// </summary>
    /// <returns>The resulting text.</returns>
    internal string GetDevelopmentReleaseNumber()
    {
        return developmentReleaseNumber;
    }

    /// <summary>
    /// Reports whether this value has local version.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasLocalVersion()
    {
        return localSegments.Length > 0;
    }

    /// <summary>
    /// Gets a local segment count.
    /// </summary>
    /// <returns>The requested numeric value.</returns>
    internal int GetLocalSegmentCount()
    {
        return localSegments.Length;
    }

    /// <summary>
    /// Gets a local segment.
    /// </summary>
    /// <param name="segmentIndex">The zero-based segment index.</param>
    /// <returns>The resulting python local version segment.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">An index, limit, or enum value is outside the supported range.</exception>
    internal PythonLocalVersionSegment GetLocalSegment(int segmentIndex)
    {
        if (segmentIndex < 0 || segmentIndex >= localSegments.Length)
        {
            throw new System.ArgumentOutOfRangeException("segmentIndex");
        }

        return localSegments[segmentIndex];
    }

    /// <summary>
    /// Gets a local segments.
    /// </summary>
    /// <remarks>
    /// The returned array is a copy, so changing it does not change this instance.
    /// </remarks>
    /// <returns>A new array containing the matching or represented values.</returns>
    internal PythonLocalVersionSegment[] GetLocalSegments()
    {
        return CopyLocalSegments(localSegments);
    }

    /// <summary>
    /// Reports whether the version is a prerelease.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsPrerelease()
    {
        return preReleasePresent || developmentReleasePresent;
    }

    /// <summary>
    /// Reports whether final release.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool IsFinalRelease()
    {
        return !preReleasePresent && !postReleasePresent && !developmentReleasePresent;
    }

    /// <summary>
    /// Formats this value as original string.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <returns>The resulting text.</returns>
    internal string ToOriginalString()
    {
        return PythonVersionFormatter.FormatOriginal(this);
    }

    /// <summary>
    /// Formats this value as normalized string.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <returns>The resulting text.</returns>
    internal string ToNormalizedString()
    {
        return PythonVersionFormatter.FormatNormalized(this);
    }

    /// <summary>
    /// Formats this value as canonical string.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <returns>The resulting text.</returns>
    internal string ToCanonicalString()
    {
        return PythonVersionFormatter.FormatCanonical(this);
    }

    /// <summary>
    /// Formats this value as display string.
    /// </summary>
    /// <remarks>
    /// Formatting does not change the represented version; the selected form controls which original or normalized details are emitted.
    /// </remarks>
    /// <returns>The resulting text.</returns>
    internal string ToDisplayString()
    {
        return PythonVersionFormatter.FormatDisplay(this);
    }

    internal static void ValidateEpoch(string epochValue)
    {
        if (epochValue.Length == 0)
        {
            throw new System.ArgumentException("Python epoch cannot be empty.");
        }

        if (!ContainsOnlyDigits(epochValue))
        {
            throw new System.ArgumentException("Python epoch must contain only digits.");
        }
    }

    internal static void ValidateReleaseSegments(string[] segmentValues)
    {
        if (segmentValues.Length == 0)
        {
            throw new System.ArgumentException("At least one Python release segment is required.");
        }

        int index = 0;
        while (index < segmentValues.Length)
        {
            if (segmentValues[index] == null)
            {
                throw new System.ArgumentException("Python release segments cannot contain null values.");
            }

            if (segmentValues[index].Length == 0)
            {
                throw new System.ArgumentException("Python release segments cannot be empty.");
            }

            if (!ContainsOnlyDigits(segmentValues[index]))
            {
                throw new System.ArgumentException("Python release segments must contain only digits.");
            }

            index = index + 1;
        }
    }

    internal static void ValidatePreRelease(bool present, string label, string number)
    {
        if (!present)
        {
            if (label.Length != 0 || number.Length != 0)
            {
                throw new System.ArgumentException("Absent Python pre-release must have empty label and number.");
            }

            return;
        }

        if (!IsPreReleaseLabel(label))
        {
            throw new System.ArgumentException("Python pre-release label must be a, b, or rc.");
        }

        if (number.Length == 0 || !ContainsOnlyDigits(number))
        {
            throw new System.ArgumentException("Python pre-release number must contain only digits.");
        }
    }

    internal static void ValidateNumberPresence(bool present, string number, string name)
    {
        if (!present)
        {
            if (number.Length != 0)
            {
                throw new System.ArgumentException(name + " must be empty when absent.");
            }

            return;
        }

        if (number.Length == 0 || !ContainsOnlyDigits(number))
        {
            throw new System.ArgumentException(name + " must contain only digits.");
        }
    }

    internal static bool IsPreReleaseLabel(string label)
    {
        if (label == "a")
        {
            return true;
        }

        if (label == "b")
        {
            return true;
        }

        if (label == "rc")
        {
            return true;
        }

        return false;
    }

    internal static string[] CopyReleaseSegments(string[] segmentValues)
    {
        string[] copy = new string[segmentValues.Length];
        int index = 0;

        while (index < segmentValues.Length)
        {
            copy[index] = segmentValues[index];
            index = index + 1;
        }

        return copy;
    }

    internal static PythonLocalVersionSegment[] CopyLocalSegments(PythonLocalVersionSegment[] segmentValues)
    {
        PythonLocalVersionSegment[] copy = new PythonLocalVersionSegment[segmentValues.Length];
        int index = 0;

        while (index < segmentValues.Length)
        {
            if (segmentValues[index] == null)
            {
                throw new System.ArgumentException("Python local segments cannot contain null values.");
            }

            copy[index] = segmentValues[index];
            index = index + 1;
        }

        return copy;
    }

    internal static bool ContainsOnlyDigits(string text)
    {
        int index = 0;

        while (index < text.Length)
        {
            if (!VersionChar.IsDigit(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }
}
