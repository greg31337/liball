namespace jmap;

internal class SecurityLimits
{
    internal const int MaximumJsonDepth = 256;
    internal const int MaximumMimeMessageBytes = 50000000;
    internal const int MaximumMimeDepth = 32;
    internal const int MaximumMimeParts = 10000;
    internal const int MaximumDecodedMimePartBytes = 50000000;
    internal const int MaximumTotalDecodedMimeBytes = 200000000;
    internal const int MaximumHeaderLineBytes = 998;
    internal const int MaximumHeaderCount = 1000;
    internal const int MaximumHeaderBytes = 1048576;
    internal const int MaximumFilterNodes = 4096;
    internal const int MaximumJsonPointerCharacters = 4096;
    internal const int MaximumJsonPointerTokens = 256;
    internal const int MaximumResultReferences = 256;
    internal const int MaximumPatchPaths = 1024;
    internal const int MaximumEventSourceSubscribers = 1024;
    internal const int MaximumEventSourceStateEntries = 16384;
    internal const int MaximumEventSourceTypes = 256;
    internal const int MaximumEventSourceStateBytes = 4096;
    internal const int MaximumEventSourcePublicationBytes = 1048576;
    internal const int MaximumFileNameBytes = 255;
    internal const int MaximumUriTemplateCharacters = 8192;
    internal const int MaximumUriVariableBytes = 4096;
    internal const int MaximumExpandedUriCharacters = 16384;
    internal const int MaximumPushUrlCharacters = 2048;
}

internal class FilterTraversalBudget
{
    internal int Nodes;
}
