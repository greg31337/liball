using System;
using System.Collections.Generic;
using System.IO;

namespace template;

/// <summary>
/// Defines member-name restrictions, loader roots, and runtime quotas used to contain untrusted templates.
/// </summary>
public class TemplateSecurityPolicy
{
    /// <summary>
    /// Allows names such as constructors and runtime type members that are blocked by default.
    /// </summary>
    public bool AllowDangerousMemberNames;
    /// <summary>
    /// Allows member names beginning with an underscore; disabled by default for safer object traversal.
    /// </summary>
    public bool AllowMemberNamesStartingWithUnderscore;
    /// <summary>
    /// Stores the ordered blocked member names.
    /// </summary>
    public List<string> BlockedMemberNames;
    /// <summary>
    /// Stores the member names that may be accessed when the list is non-empty.
    /// </summary>
    public List<string> AllowedMemberNames;
    /// <summary>
    /// Stores normalized directory roots from which file-backed templates may be loaded.
    /// </summary>
    public List<string> AllowedLoaderRoots;
    /// <summary>
    /// Specifies the maximum allowed template length.
    /// </summary>
    public int MaximumTemplateLength;
    /// <summary>
    /// Specifies the maximum allowed output length.
    /// </summary>
    public int MaximumOutputLength;
    /// <summary>
    /// Specifies the maximum allowed include depth.
    /// </summary>
    public int MaximumIncludeDepth;
    /// <summary>
    /// Specifies the maximum allowed loop iterations.
    /// </summary>
    public int MaximumLoopIterations;
    /// <summary>
    /// Specifies the maximum allowed expression depth.
    /// </summary>
    public int MaximumExpressionDepth;
    /// <summary>
    /// Specifies the maximum allowed template count.
    /// </summary>
    public int MaximumTemplateCount;
    /// <summary>
    /// Specifies the maximum allowed render milliseconds.
    /// </summary>
    public int MaximumRenderMilliseconds;
    /// <summary>
    /// Specifies the maximum allowed recursion depth.
    /// </summary>
    public int MaximumRecursionDepth;

    /// <summary>
    /// Initializes a new template security policy and validates any supplied values.
    /// </summary>
    public TemplateSecurityPolicy()
    {
        AllowDangerousMemberNames = false;
        AllowMemberNamesStartingWithUnderscore = false;
        BlockedMemberNames = new List<string>();
        AllowedMemberNames = new List<string>();
        AllowedLoaderRoots = new List<string>();
        MaximumTemplateLength = 1048576;
        MaximumOutputLength = 16777216;
        MaximumIncludeDepth = 32;
        MaximumLoopIterations = 100000;
        MaximumExpressionDepth = 64;
        MaximumTemplateCount = 1024;
        MaximumRenderMilliseconds = 0;
        MaximumRecursionDepth = 64;
        AddDefaultBlockedMemberNames();
    }

    /// <summary>
    /// Creates a security policy whose runtime limits match the supplied template options.
    /// </summary>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal static TemplateSecurityPolicy FromOptions(TemplateOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        TemplateSecurityPolicy policy = new TemplateSecurityPolicy();
        policy.ApplyOptions(options);
        return policy;
    }

    /// <summary>
    /// Copies this configuration into the supplied target after validation.
    /// </summary>
    /// <param name="options">The options that control the operation.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal void ApplyOptions(TemplateOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        MaximumTemplateLength = options.MaximumTemplateLength;
        MaximumOutputLength = options.MaximumOutputLength;
        MaximumIncludeDepth = options.MaximumIncludeDepth;
        MaximumLoopIterations = options.MaximumLoopIterations;
        MaximumExpressionDepth = options.MaximumExpressionDepth;
        MaximumTemplateCount = options.MaximumTemplateCount;
        MaximumRenderMilliseconds = options.MaximumRenderMilliseconds;
        MaximumRecursionDepth = options.MaximumRecursionDepth;
    }

    /// <summary>
    /// Adds the supplied blocked member name after validating it.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal void AddBlockedMemberName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (!IsBlockedMemberName(name))
        {
            BlockedMemberNames.Add(name);
        }
    }

    /// <summary>
    /// Checks the underscore rule and dangerous-name block list before member resolution.
    /// </summary>
    /// <param name="memberName">The member name written in the template.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal bool CanAccessMember(string memberName)
    {
        if (memberName == null)
        {
            throw new ArgumentNullException("memberName");
        }

        if (!AllowMemberNamesStartingWithUnderscore && memberName.Length > 0 && memberName[0] == '_')
        {
            return false;
        }

        if (AllowedMemberNames.Count > 0 && !ContainsMemberName(AllowedMemberNames, memberName))
        {
            return false;
        }

        if (!AllowDangerousMemberNames && IsBlockedMemberName(memberName))
        {
            return false;
        }

        return true;
    }

    internal bool ContainsMemberName(List<string> names, string name)
    {
        int index = 0;
        while (index < names.Count)
        {
            if (string.Equals(names[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Checks the blocked-name list using ordinal, case-insensitive matching.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal bool IsBlockedMemberName(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        int index = 0;
        while (index < BlockedMemberNames.Count)
        {
            if (string.Equals(BlockedMemberNames[index], name, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Adds the supplied loader root after validating it.
    /// </summary>
    /// <param name="root">The root used by the operation.</param>
    /// <exception cref="ArgumentException">Thrown when an argument is invalid for this operation.</exception>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal void AddLoaderRoot(string root)
    {
        if (root == null)
        {
            throw new ArgumentNullException("root");
        }

        if (root.Length == 0)
        {
            throw new ArgumentException("Loader root is required.", "root");
        }

        string normalized = NormalizePath(root);
        int index = 0;
        while (index < AllowedLoaderRoots.Count)
        {
            if (PathsEqual(AllowedLoaderRoots[index], normalized))
            {
                return;
            }

            index = index + 1;
        }

        AllowedLoaderRoots.Add(normalized);
    }

    /// <summary>
    /// Checks whether a normalized path is inside one of the explicitly allowed loader roots.
    /// </summary>
    /// <param name="path">The path used by the operation.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal bool IsLoaderPathAllowed(string path)
    {
        if (path == null)
        {
            throw new ArgumentNullException("path");
        }

        if (AllowedLoaderRoots.Count == 0)
        {
            return false;
        }

        string normalized = NormalizePath(path);
        int index = 0;
        while (index < AllowedLoaderRoots.Count)
        {
            string allowedRoot = AllowedLoaderRoots[index];
            if (allowedRoot != "" && IsPathInsideRoot(normalized, NormalizePath(allowedRoot)))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Determines whether the template length allowed condition is satisfied.
    /// </summary>
    /// <param name="length">The number of characters in the range or output.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsTemplateLengthAllowed(int length)
    {
        RequireNonNegative(length, "length");
        return length <= MaximumTemplateLength;
    }

    /// <summary>
    /// Determines whether the output length allowed condition is satisfied.
    /// </summary>
    /// <param name="length">The number of characters in the range or output.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsOutputLengthAllowed(int length)
    {
        RequireNonNegative(length, "length");
        return length <= MaximumOutputLength;
    }

    /// <summary>
    /// Determines whether the include depth allowed condition is satisfied.
    /// </summary>
    /// <param name="depth">The depth used by the operation.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsIncludeDepthAllowed(int depth)
    {
        RequireNonNegative(depth, "depth");
        return depth <= MaximumIncludeDepth;
    }

    /// <summary>
    /// Determines whether the loop iteration allowed condition is satisfied.
    /// </summary>
    /// <param name="iterationCount">The iteration count used by the operation.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsLoopIterationAllowed(int iterationCount)
    {
        RequireNonNegative(iterationCount, "iterationCount");
        return iterationCount <= MaximumLoopIterations;
    }

    /// <summary>
    /// Determines whether the expression depth allowed condition is satisfied.
    /// </summary>
    /// <param name="depth">The depth used by the operation.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsExpressionDepthAllowed(int depth)
    {
        RequireNonNegative(depth, "depth");
        return depth <= MaximumExpressionDepth;
    }

    /// <summary>
    /// Determines whether the template count allowed condition is satisfied.
    /// </summary>
    /// <param name="templateCount">The template count used by the operation.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsTemplateCountAllowed(int templateCount)
    {
        RequireNonNegative(templateCount, "templateCount");
        return templateCount <= MaximumTemplateCount;
    }

    /// <summary>
    /// Checks elapsed render time; a configured maximum of zero means no time limit.
    /// </summary>
    /// <param name="elapsedMilliseconds">The elapsed milliseconds used by the operation.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsRenderTimeAllowed(int elapsedMilliseconds)
    {
        RequireNonNegative(elapsedMilliseconds, "elapsedMilliseconds");
        if (MaximumRenderMilliseconds == 0)
        {
            return true;
        }

        return elapsedMilliseconds <= MaximumRenderMilliseconds;
    }

    /// <summary>
    /// Determines whether the recursion depth allowed condition is satisfied.
    /// </summary>
    /// <param name="depth">The depth used by the operation.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool IsRecursionDepthAllowed(int depth)
    {
        RequireNonNegative(depth, "depth");
        return depth <= MaximumRecursionDepth;
    }

    /// <summary>
    /// Checks the current settings for missing or contradictory values and throws when they cannot be used safely.
    /// </summary>
    internal void Validate()
    {
        if (BlockedMemberNames == null)
        {
            throw new ArgumentNullException("BlockedMemberNames");
        }

        if (AllowedMemberNames == null)
        {
            throw new ArgumentNullException("AllowedMemberNames");
        }

        if (AllowedLoaderRoots == null)
        {
            throw new ArgumentNullException("AllowedLoaderRoots");
        }

        RequirePositive(MaximumTemplateLength, "MaximumTemplateLength");
        RequirePositive(MaximumOutputLength, "MaximumOutputLength");
        RequireNonNegative(MaximumIncludeDepth, "MaximumIncludeDepth");
        RequireNonNegative(MaximumLoopIterations, "MaximumLoopIterations");
        RequirePositive(MaximumExpressionDepth, "MaximumExpressionDepth");
        RequirePositive(MaximumTemplateCount, "MaximumTemplateCount");
        RequireNonNegative(MaximumRenderMilliseconds, "MaximumRenderMilliseconds");
        RequirePositive(MaximumRecursionDepth, "MaximumRecursionDepth");
    }

    internal void AddDefaultBlockedMemberNames()
    {
        AddBlockedMemberName("__proto__");
        AddBlockedMemberName("__defineGetter__");
        AddBlockedMemberName("__defineSetter__");
        AddBlockedMemberName("__lookupGetter__");
        AddBlockedMemberName("__lookupSetter__");
        AddBlockedMemberName("constructor");
        AddBlockedMemberName("prototype");
        AddBlockedMemberName("GetType");
        AddBlockedMemberName("MemberwiseClone");
        AddBlockedMemberName("Finalize");
    }

    internal string NormalizePath(string path)
    {
        string fullPath = Path.GetFullPath(path);
        return fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    internal bool IsPathInsideRoot(string path, string root)
    {
        if (PathsEqual(path, root))
        {
            return true;
        }

        string rootPrefix = root + Path.DirectorySeparatorChar.ToString();
        return path.StartsWith(rootPrefix, StringComparison.Ordinal);
    }

    internal bool PathsEqual(string left, string right)
    {
        return string.Equals(left, right, StringComparison.Ordinal);
    }

    internal void RequirePositive(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new ArgumentOutOfRangeException(fieldName);
        }
    }

    internal void RequireNonNegative(int value, string fieldName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(fieldName);
        }
    }
}
