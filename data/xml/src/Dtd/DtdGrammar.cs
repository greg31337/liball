using System.Collections.Generic;

namespace xml;

internal enum DtdSourceKind
{
    InternalSubset = 0,
    ExternalSubset = 1,
    InternalParameterEntity = 2,
    ExternalParameterEntity = 3
}

internal enum DtdConditionalKind
{
    Include = 0,
    Ignore = 1
}

internal enum DtdContentParticleKind
{
    Name = 0,
    Sequence = 1,
    Choice = 2
}

internal enum DtdContentOccurrence
{
    ExactlyOne = 0,
    ZeroOrOne = 1,
    ZeroOrMore = 2,
    OneOrMore = 3
}

internal class DtdContentParticle
{
    internal DtdContentParticleKind Kind;
    internal string? Name;
    internal List<DtdContentParticle> Children;
    internal DtdContentOccurrence Occurrence;
    internal int Position;

    internal DtdContentParticle(DtdContentParticleKind kind, string? name, List<DtdContentParticle> children, DtdContentOccurrence occurrence, int position)
    {
        Kind = kind;
        Name = name;
        Children = children;
        Occurrence = occurrence;
        Position = position;
    }
}

internal class DtdContentAnalysis
{
    internal bool Nullable;
    internal List<int> FirstPositions;
    internal List<int> LastPositions;

    internal DtdContentAnalysis(bool nullable, List<int> firstPositions, List<int> lastPositions)
    {
        Nullable = nullable;
        FirstPositions = firstPositions;
        LastPositions = lastPositions;
    }
}

internal class DtdCompiledContentModel
{
    internal string ElementName;
    internal bool IsEmpty;
    internal bool IsAny;
    internal bool IsMixed;
    internal string[] MixedNames;
    internal string[] PositionNames;
    internal int[] InitialPositions;
    internal int[][] FollowPositions;
    internal int[] FinalPositions;
    internal bool AcceptsEmpty;

    internal DtdCompiledContentModel(string elementName, bool isEmpty, bool isAny, bool isMixed, string[] mixedNames, string[] positionNames, int[] initialPositions, int[][] followPositions, int[] finalPositions, bool acceptsEmpty)
    {
        ElementName = elementName;
        IsEmpty = isEmpty;
        IsAny = isAny;
        IsMixed = isMixed;
        MixedNames = mixedNames;
        PositionNames = positionNames;
        InitialPositions = initialPositions;
        FollowPositions = followPositions;
        FinalPositions = finalPositions;
        AcceptsEmpty = acceptsEmpty;
    }
}

internal class DtdContentValidationState
{
    internal string ElementName;
    internal DtdCompiledContentModel? Model;
    internal int[] ExpectedPositions;
    internal bool Accepting;

    internal DtdContentValidationState(string elementName, DtdCompiledContentModel? model, int[] expectedPositions, bool accepting)
    {
        ElementName = elementName;
        Model = model;
        ExpectedPositions = expectedPositions;
        Accepting = accepting;
    }
}

internal class DtdParameterEntityDefinition
{
    internal string Name;
    internal string? ReplacementText;
    internal string? PublicIdentifier;
    internal string? SystemIdentifier;
    internal string? BaseUri;
    internal bool IsExternal;

    internal DtdParameterEntityDefinition(string name, string? replacementText, string? publicIdentifier, string? systemIdentifier, string? baseUri, bool isExternal)
    {
        Name = name;
        ReplacementText = replacementText;
        PublicIdentifier = publicIdentifier;
        SystemIdentifier = systemIdentifier;
        BaseUri = baseUri;
        IsExternal = isExternal;
    }
}

internal class DtdTokenEntry
{
    internal DtdToken Token;
    internal DtdSourceKind SourceKind;
    internal string? SourceUri;
    internal string? ParameterEntityName;
    internal int EntityDepth;
    internal bool Synthetic;

    internal DtdTokenEntry(DtdToken token, DtdSourceKind sourceKind, string? sourceUri, string? parameterEntityName, int entityDepth, bool synthetic)
    {
        Token = token;
        SourceKind = sourceKind;
        SourceUri = sourceUri;
        ParameterEntityName = parameterEntityName;
        EntityDepth = entityDepth;
        Synthetic = synthetic;
    }
}

internal class DtdDeclarationReader
{
    internal List<DtdTokenEntry> Entries;
    internal int Position;
    internal int End;

    internal DtdDeclarationReader(List<DtdTokenEntry> entries, int start, int end)
    {
        Entries = entries;
        Position = start;
        End = end;
    }

    internal bool IsDtdDeclarationEnd()
    {
        return Position >= End;
    }

    internal DtdTokenEntry? PeekDtdDeclarationEntry()
    {
        if (Position >= End)
        {
            return null;
        }

        return Entries[Position];
    }

    internal DtdTokenEntry? ReadDtdDeclarationEntry()
    {
        if (Position >= End)
        {
            return null;
        }

        DtdTokenEntry entry = Entries[Position];
        Position = Position + 1;
        return entry;
    }

    internal bool ConsumeDtdDeclarationWhitespace()
    {
        bool consumed = false;
        while (Position < End && Entries[Position].Token.Kind == DtdTokenKind.Whitespace)
        {
            consumed = true;
            Position = Position + 1;
        }

        return consumed;
    }
}

internal class DtdLoadedSource
{
    internal string Text;
    internal long ResourceByteCount;
    internal int ContentStartUtf16Offset;
    internal string? SourceUri;
    internal XmlDeclarationInfo TextDeclaration;

    internal DtdLoadedSource(string text, long resourceByteCount, int contentStartUtf16Offset, string? sourceUri, XmlDeclarationInfo textDeclaration)
    {
        Text = text;
        ResourceByteCount = resourceByteCount;
        ContentStartUtf16Offset = contentStartUtf16Offset;
        SourceUri = sourceUri;
        TextDeclaration = textDeclaration;
    }
}

internal class DtdSubset
{
    internal DtdTokenEntry[] Tokens;
    internal XmlElementDeclaration[] ElementDeclarations;
    internal XmlAttributeDeclaration[] AttributeDeclarations;
    internal XmlEntityDeclaration[] EntityDeclarations;
    internal XmlNotationDeclaration[] NotationDeclarations;
    internal XmlEvent[] DeclarationEvents;
    internal bool HasInternalSubset;
    internal bool ExternalSubsetLoaded;
    internal string? ExternalSubsetUri;
    internal XmlDeclarationInfo? ExternalSubsetTextDeclaration;

    internal DtdSubset(DtdTokenEntry[] tokens, XmlElementDeclaration[] elementDeclarations, XmlAttributeDeclaration[] attributeDeclarations, XmlEntityDeclaration[] entityDeclarations, XmlNotationDeclaration[] notationDeclarations, XmlEvent[] declarationEvents, bool hasInternalSubset, bool externalSubsetLoaded, string? externalSubsetUri, XmlDeclarationInfo? externalSubsetTextDeclaration)
    {
        Tokens = tokens;
        ElementDeclarations = elementDeclarations;
        AttributeDeclarations = attributeDeclarations;
        EntityDeclarations = entityDeclarations;
        NotationDeclarations = notationDeclarations;
        DeclarationEvents = declarationEvents;
        HasInternalSubset = hasInternalSubset;
        ExternalSubsetLoaded = externalSubsetLoaded;
        ExternalSubsetUri = externalSubsetUri;
        ExternalSubsetTextDeclaration = externalSubsetTextDeclaration;
    }
}
