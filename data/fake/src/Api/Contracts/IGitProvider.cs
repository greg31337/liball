using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="GitProvider"/>.</summary>
public interface IGitProvider
{
    /// <summary>Generates a valid fictional Git branch name.</summary>
    /// <returns>A reviewed branch name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Branch();
    /// <summary>Generates a semantic-version-shaped Git tag.</summary>
    /// <returns>A tag beginning with <c>v</c>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    string Tag();
    /// <summary>Generates a lowercase hexadecimal Git commit identifier shape.</summary>
    /// <remarks>The value is synthetic and does not identify a real commit.</remarks>
    /// <returns>A 40-character SHA-1-shaped value.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the configured output limit is insufficient.</exception>
    string CommitSha();
    /// <summary>Generates a fictional Git commit message.</summary>
    /// <returns>A reviewed project-authored message.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string CommitMessage();
    /// <summary>Generates a recent Git commit date.</summary>
    /// <remarks>The date is no later than the captured reference time and is within the preceding 365 days.</remarks>
    /// <returns>A commit date expressed as a UTC instant.</returns>
    System.DateTimeOffset CommitDate();
    /// <summary>Generates a fictional formatted Git commit entry.</summary>
    /// <remarks>The SHA, author, date, and message are synthetic and correlated within the returned entry.</remarks>
    /// <returns>A multi-line Git log entry.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string CommitEntry();
    /// <summary>Generates a fictional repository slug.</summary>
    /// <returns>An owner and repository name separated by a slash.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Repository();
    /// <summary>Generates a fictional repository issue reference.</summary>
    /// <returns>A repository slug and positive issue number separated by <c>#</c>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string Issue();
    /// <summary>Generates a fictional repository pull-request reference.</summary>
    /// <returns>A repository slug and positive pull-request number separated by <c>!</c>.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string PullRequest();
    /// <summary>Generates a fictional package name.</summary>
    /// <returns>A lowercase hyphenated package name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string PackageName();
    /// <summary>Generates a programming-language name.</summary>
    /// <returns>A reviewed programming-language name.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string ProgrammingLanguage();
    /// <summary>Generates a license identifier.</summary>
    /// <remarks>The value is selected from a reviewed subset of SPDX license identifiers.</remarks>
    /// <returns>An SPDX license identifier.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    string License();
}
