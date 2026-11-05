using System;

namespace xml;

internal class XPathRuntimeContext
{
    internal XPathDynamicContext Snapshot;
    internal XmlSecurityLimits Limits;
    internal XmlResolverPolicy ResourcePolicy;
    internal int MaximumDepth;
    internal int MaximumResources;
    internal int CancellationPollingInterval;
    internal long MaximumOperations;
    internal bool HasExplicitResolverPolicy;

    internal XPathRuntimeContext(XPathDynamicContext context)
    {
        Guard.NotNull(context, "context");
        if (context.ImplicitTimezoneMinutes < -840 || context.ImplicitTimezoneMinutes > 840)
        {
            throw new ArgumentOutOfRangeException("context.ImplicitTimezoneMinutes");
        }

        Snapshot = new XPathDynamicContext();
        Snapshot.ContextItem = context.ContextItem == null ? null : new XdmSequenceOperations().FreezeXdmSequenceStorage(context.ContextItem);
        NormalizeXPathFocus(context.Position, context.Size);
        Snapshot.Variables = context.Variables == null ? [] : new XdmFunctionOperations().FreezeXPathClosureVariables(context.Variables);
        Snapshot.VariableResolver = context.VariableResolver;
        Snapshot.BaseUri = context.BaseUri;
        Snapshot.DefaultCollectionUri = context.DefaultCollectionUri;
        Snapshot.ImplicitTimezoneMinutes = context.ImplicitTimezoneMinutes;
        Snapshot.DefaultLanguage = context.DefaultLanguage;
        Snapshot.CurrentDateTime = SnapshotXPathCurrentDateTime(context.CurrentDateTime, context.ImplicitTimezoneMinutes);
        Snapshot.EnvironmentNames = CopyXPathContextStrings(context.EnvironmentNames);
        Snapshot.EnvironmentValues = CopyXPathContextStrings(context.EnvironmentValues);
        ValidateXPathEnvironmentSnapshot(Snapshot.EnvironmentNames, Snapshot.EnvironmentValues);
        HasExplicitResolverPolicy = context.ResolverPolicy != null;
        ResourcePolicy = CopyXPathResolverPolicy(context.ResolverPolicy);
        Snapshot.ResolverPolicy = ResourcePolicy;
        Snapshot.CollectionResolver = context.CollectionResolver;
        Snapshot.ResourceResolver = context.ResourceResolver;
        Snapshot.TraceReceiver = context.TraceReceiver;
        Snapshot.Cancellation = context.Cancellation;
        Limits = CopyXPathSecurityLimits(context.SecurityLimits);
        Snapshot.SecurityLimits = Limits;
        Snapshot.MaximumOperations = context.MaximumOperations;
        Snapshot.XsltCurrentGroupAvailable = context.XsltCurrentGroupAvailable;
        Snapshot.XsltCurrentGroup = context.XsltCurrentGroup == null ? null : new XdmSequenceOperations().FreezeXdmSequenceStorage(context.XsltCurrentGroup);
        Snapshot.XsltCurrentGroupingKeyAvailable = context.XsltCurrentGroupingKeyAvailable;
        Snapshot.XsltCurrentGroupingKey = context.XsltCurrentGroupingKey == null ? null : new XdmSequenceOperations().FreezeXdmSequenceStorage(context.XsltCurrentGroupingKey);
        Snapshot.XsltCurrentMergeGroupAvailable = context.XsltCurrentMergeGroupAvailable;
        Snapshot.XsltCurrentMergeGroup = context.XsltCurrentMergeGroup == null ? null : new XdmSequenceOperations().FreezeXdmSequenceStorage(context.XsltCurrentMergeGroup);
        Snapshot.XsltCurrentMergeSourceNames = context.XsltCurrentMergeSourceNames == null ? null : (string[])context.XsltCurrentMergeSourceNames.Clone();
        Snapshot.XsltCurrentMergeSourceGroups = CopyXPathMergeGroups(context.XsltCurrentMergeSourceGroups);
        Snapshot.XsltCurrentMergeKeyAvailable = context.XsltCurrentMergeKeyAvailable;
        Snapshot.XsltCurrentMergeKey = context.XsltCurrentMergeKey == null ? null : new XdmSequenceOperations().FreezeXdmSequenceStorage(context.XsltCurrentMergeKey);
        Snapshot.XsltKeys = context.XsltKeys;
        Snapshot.XsltAccumulators = context.XsltAccumulators;
        Snapshot.XsltRegexGroups = context.XsltRegexGroups == null ? null : (string?[])context.XsltRegexGroups.Clone();
        Snapshot.XsltFunctions = context.XsltFunctions;
        Snapshot.XsltCurrentComponent = context.XsltCurrentComponent;
        Snapshot.XsltCurrentItem = context.XsltCurrentItem == null ? null : new XdmSequenceOperations().FreezeXdmSequenceStorage(context.XsltCurrentItem);
        Snapshot.XsltCurrentOutputUri = context.XsltCurrentOutputUri;
        Snapshot.XsltDocuments = context.XsltDocuments;
        Snapshot.XsltStreamedNodes = context.XsltStreamedNodes;
        Snapshot.ExtensionState = context.ExtensionState == null ? new XmlExtensionRuntimeState() : context.ExtensionState;
        MaximumDepth = Limits.MaximumDepth > 0 ? Limits.MaximumDepth : 256;
        MaximumResources = Limits.MaximumExternalResources > 0 ? Limits.MaximumExternalResources : 64;
        CancellationPollingInterval = Limits.CancellationPollingInterval > 0 ? Limits.CancellationPollingInterval : 1024;
        MaximumOperations = context.MaximumOperations > 0 ? context.MaximumOperations : long.MaxValue;
    }

    internal XdmSequence[]? CopyXPathMergeGroups(XdmSequence[]? groups)
    {
        if (groups == null)
        {
            return null;
        }

        XdmSequence[] result = new XdmSequence[groups.Length];
        int index = 0;
        while (index < groups.Length)
        {
            result[index] = new XdmSequenceOperations().FreezeXdmSequenceStorage(groups[index]);
            index = index + 1;
        }

        return result;
    }

    internal void NormalizeXPathFocus(int position, int size)
    {
        XdmSequence? item = Snapshot.ContextItem;
        if (item == null || item.Count == 0)
        {
            if (position != 0 || size != 0)
            {
                throw new ArgumentException("An absent XPath context item requires a zero position and size.", "context");
            }

            Snapshot.Position = 0;
            Snapshot.Size = 0;
            return;
        }

        if (item.Count != 1)
        {
            throw new ArgumentException("The XPath dynamic context item must be empty or a singleton.", "context");
        }

        if (position == 0 && size == 0)
        {
            Snapshot.Position = 1;
            Snapshot.Size = 1;
            return;
        }

        if (size <= 0 || position <= 0 || position > size)
        {
            throw new ArgumentException("The XPath dynamic focus position must be within its positive size.", "context");
        }

        Snapshot.Position = position;
        Snapshot.Size = size;
    }

    internal DateTimeOffset SnapshotXPathCurrentDateTime(DateTimeOffset supplied, int implicitTimezoneMinutes)
    {
        if (supplied != default(DateTimeOffset))
        {
            return supplied;
        }

        TimeSpan offset = TimeSpan.FromMinutes(implicitTimezoneMinutes);
        return DateTimeOffset.UtcNow.ToOffset(offset);
    }

    internal void ValidateXPathEnvironmentSnapshot(string[]? names, string[]? values)
    {
        string[] actualNames = names == null ? [] : names;
        string[] actualValues = values == null ? [] : values;
        if (actualNames.Length != actualValues.Length)
        {
            throw new ArgumentException("XPath environment names and values must have equal lengths.", "context");
        }

        int index = 0;
        while (index < actualNames.Length)
        {
            if (actualNames[index] == null || actualValues[index] == null)
            {
                throw new ArgumentException("XPath environment names and values cannot contain null entries.", "context");
            }

            int earlier = 0;
            while (earlier < index)
            {
                if (actualNames[earlier] == actualNames[index])
                {
                    throw new ArgumentException("XPath environment names must be unique.", "context");
                }

                earlier = earlier + 1;
            }

            index = index + 1;
        }
    }

    internal string[]? CopyXPathContextStrings(string[]? values)
    {
        if (values == null)
        {
            return null;
        }

        string[] result = new string[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            string? value = values[index];
            if (value == null)
            {
                throw new ArgumentException("An XPath context string array cannot contain null entries.", "context");
            }

            result[index] = value;
            index = index + 1;
        }

        return result;
    }

    internal XmlSecurityLimits CopyXPathSecurityLimits(XmlSecurityLimits? supplied)
    {
        XmlSecurityLimits source = supplied == null ? CreateDefaultXPathSecurityLimits() : supplied;
        XmlSecurityLimits result = new XmlSecurityLimits();
        result.MaximumDepth = source.MaximumDepth;
        result.MaximumNodeCount = source.MaximumNodeCount;
        result.MaximumAttributesPerElement = source.MaximumAttributesPerElement;
        result.MaximumNameLength = source.MaximumNameLength;
        result.MaximumTextLength = source.MaximumTextLength;
        result.MaximumTokenLength = source.MaximumTokenLength;
        result.MaximumInputCharacters = source.MaximumInputCharacters;
        result.MaximumInputBytes = source.MaximumInputBytes;
        result.MaximumDtdCharacters = source.MaximumDtdCharacters;
        result.MaximumEntityDepth = source.MaximumEntityDepth;
        result.MaximumEntityCount = source.MaximumEntityCount;
        result.MaximumEntityCharacters = source.MaximumEntityCharacters;
        result.MaximumEntityAmplification = source.MaximumEntityAmplification;
        result.MaximumExternalResources = source.MaximumExternalResources;
        result.MaximumResourceDepth = source.MaximumResourceDepth;
        result.MaximumResourceBytes = source.MaximumResourceBytes;
        result.CancellationPollingInterval = source.CancellationPollingInterval;
        return result;
    }

    internal XmlSecurityLimits CreateDefaultXPathSecurityLimits()
    {
        XmlSecurityLimits limits = new XmlSecurityLimits();
        limits.MaximumDepth = 256;
        limits.MaximumNodeCount = 1000000;
        limits.MaximumAttributesPerElement = 10000;
        limits.MaximumNameLength = 16384;
        limits.MaximumTextLength = 67108864;
        limits.MaximumTokenLength = 67108864;
        limits.MaximumInputCharacters = 67108864;
        limits.MaximumInputBytes = 134217728;
        limits.MaximumDtdCharacters = 16777216;
        limits.MaximumEntityDepth = 32;
        limits.MaximumEntityCount = 100000;
        limits.MaximumEntityCharacters = 67108864;
        limits.MaximumEntityAmplification = 100;
        limits.MaximumExternalResources = 64;
        limits.MaximumResourceDepth = 16;
        limits.MaximumResourceBytes = 67108864;
        limits.CancellationPollingInterval = 1024;
        return limits;
    }

    internal XmlResolverPolicy CopyXPathResolverPolicy(XmlResolverPolicy? policy)
    {
        XmlResolverPolicy result = new XmlResolverPolicy();
        if (policy == null)
        {
            result.AllowedSchemes = [];
            result.AllowedRoots = [];
            result.CatalogEntries = [];
            return result;
        }

        result.Access = policy.Access;
        result.AllowedSchemes = CopyXPathContextStrings(policy.AllowedSchemes);
        result.AllowedRoots = CopyXPathContextStrings(policy.AllowedRoots);
        result.AllowRedirects = policy.AllowRedirects;
        result.MaximumRedirects = policy.MaximumRedirects;
        result.MaximumDepth = policy.MaximumDepth;
        result.MaximumResourceBytes = policy.MaximumResourceBytes;
        result.MaximumWorkUnits = policy.MaximumWorkUnits;
        result.CacheResources = policy.CacheResources;
        result.CatalogEntries = CopyXPathCatalogEntries(policy.CatalogEntries);
        result.Accept = policy.Accept;
        result.AcceptLanguage = policy.AcceptLanguage;
        return result;
    }

    internal XmlCatalogEntry[]? CopyXPathCatalogEntries(XmlCatalogEntry[]? entries)
    {
        if (entries == null)
        {
            return null;
        }

        XmlCatalogEntry[] result = new XmlCatalogEntry[entries.Length];
        int index = 0;
        while (index < entries.Length)
        {
            XmlCatalogEntry? entry = entries[index];
            if (entry == null)
            {
                throw new ArgumentException("An XPath resolver catalog cannot contain null entries.", "context");
            }

            result[index] = CopyXPathCatalogEntry(entry);
            index = index + 1;
        }

        return result;
    }

    internal XmlCatalogEntry CopyXPathCatalogEntry(XmlCatalogEntry entry)
    {
        XmlCatalogEntry result = new XmlCatalogEntry();
        result.Kind = entry.Kind;
        result.PublicIdentifier = entry.PublicIdentifier;
        result.SystemIdentifier = entry.SystemIdentifier;
        result.Uri = entry.Uri;
        result.TargetUri = entry.TargetUri;
        result.RewritePrefix = entry.RewritePrefix;
        result.Suffix = entry.Suffix;
        result.CatalogUri = entry.CatalogUri;
        result.BaseUri = entry.BaseUri;
        result.PreferPublic = entry.PreferPublic;
        result.Entries = CopyXPathCatalogEntries(entry.Entries);
        result.Range = CopyXPathContextRange(entry.Range);
        return result;
    }

    internal SourceRange? CopyXPathContextRange(SourceRange? range)
    {
        if (range == null)
        {
            return null;
        }

        SourceRange result = new SourceRange();
        result.Start = CopyXPathContextLocation(range.Start);
        result.End = CopyXPathContextLocation(range.End);
        return result;
    }

    internal SourceLocation? CopyXPathContextLocation(SourceLocation? location)
    {
        if (location == null)
        {
            return null;
        }

        SourceLocation result = new SourceLocation();
        result.SourceUri = location.SourceUri;
        result.CharacterOffset = location.CharacterOffset;
        result.ByteOffset = location.ByteOffset;
        result.Line = location.Line;
        result.Column = location.Column;
        return result;
    }
}

internal class XPathRuntimeLimiter
{
    internal long MaximumOperations;
    internal long Operations;
    internal int MaximumDepth;
    internal int Depth;
    internal int CancellationPollingInterval;
    internal int WorkSinceCancellationPoll;
    internal IXmlCancellation? Cancellation;

    internal XPathRuntimeLimiter(long maximumOperations, int maximumDepth, int cancellationPollingInterval, IXmlCancellation? cancellation)
    {
        MaximumOperations = maximumOperations;
        Operations = 0;
        MaximumDepth = maximumDepth;
        Depth = 0;
        CancellationPollingInterval = cancellationPollingInterval;
        WorkSinceCancellationPoll = 0;
        Cancellation = cancellation;
    }

    internal void EnterXPathInstruction(SourceRange range)
    {
        if (Operations >= MaximumOperations)
        {
            throw CreateXPathRuntimeLimit("operation count", MaximumOperations, range);
        }

        if (Depth >= MaximumDepth)
        {
            throw CreateXPathRuntimeLimit("instruction depth", MaximumDepth, range);
        }

        Operations = Operations + 1;
        Depth = Depth + 1;
        WorkSinceCancellationPoll = WorkSinceCancellationPoll + 1;
        if (WorkSinceCancellationPoll >= CancellationPollingInterval)
        {
            WorkSinceCancellationPoll = 0;
            PollXPathCancellation();
        }
    }

    internal void ExitXPathInstruction()
    {
        Depth = Depth - 1;
    }

    internal void CountXPathWork(SourceRange range)
    {
        if (Operations >= MaximumOperations)
        {
            throw CreateXPathRuntimeLimit("operation count", MaximumOperations, range);
        }

        Operations = Operations + 1;
        WorkSinceCancellationPoll = WorkSinceCancellationPoll + 1;
        if (WorkSinceCancellationPoll >= CancellationPollingInterval)
        {
            WorkSinceCancellationPoll = 0;
            PollXPathCancellation();
        }
    }

    internal void PollXPathCancellation()
    {
        IXmlCancellation? cancellation = Cancellation;
        if (cancellation != null && cancellation.IsXmlCancellationRequested())
        {
            throw new OperationCanceledException("XPath evaluation was canceled.");
        }
    }

    internal XmlLimitException CreateXPathRuntimeLimit(string limitName, long maximum, SourceRange range)
    {
        string message = "The configured XPath " + limitName + " limit of " + maximum + " was exceeded.";
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.LimitExceeded;
        diagnostic.Category = XmlErrorCategory.Limit;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        diagnostic.Range = CopyXPathLimitRange(range);
        return new XmlLimitException(message, diagnostic, diagnostic.Range.Start);
    }

    internal SourceRange CopyXPathLimitRange(SourceRange range)
    {
        SourceRange result = new SourceRange();
        result.Start = CopyXPathLimitLocation(range.Start);
        result.End = CopyXPathLimitLocation(range.End);
        return result;
    }

    internal SourceLocation? CopyXPathLimitLocation(SourceLocation? location)
    {
        if (location == null)
        {
            return null;
        }

        SourceLocation result = new SourceLocation();
        result.SourceUri = location.SourceUri;
        result.CharacterOffset = location.CharacterOffset;
        result.ByteOffset = location.ByteOffset;
        result.Line = location.Line;
        result.Column = location.Column;
        return result;
    }
}
