using System;
using System.Collections.Generic;
using System.Text;

namespace xml;

internal class DtdElementDeclarationParser
{
    internal DtdDeclarationReader Reader;
    internal int DeclarationOffset;
    internal List<string> PositionNames;
    internal List<string> ReferencedNames;
    internal List<List<int>> FollowPositions;
    internal DtdCompiledContentModel? CompiledModel;

    internal DtdElementDeclarationParser(DtdDeclarationReader reader, int declarationOffset)
    {
        Guard.NotNull(reader, "reader");
        Reader = reader;
        DeclarationOffset = declarationOffset;
        PositionNames = new List<string>();
        ReferencedNames = new List<string>();
        FollowPositions = new List<List<int>>();
        CompiledModel = null;
    }

    internal XmlElementDeclaration ParseDtdElementDeclaration()
    {
        RequireDtdElementWhitespace();
        string name = ReadRequiredDtdElementName("An element declaration requires an XML Name");
        RequireDtdElementWhitespace();
        DtdTokenEntry content = ReadRequiredDtdElementEntry("An element declaration requires a content specification");
        XmlElementDeclaration declaration = new XmlElementDeclaration();
        declaration.Name = CreateDtdElementName(name);
        declaration.IsEmpty = false;
        declaration.IsAny = false;
        declaration.IsMixed = false;
        declaration.Range = null;
        if (content.Token.Kind == DtdTokenKind.Name && content.Token.Name == "EMPTY")
        {
            declaration.ContentModel = "EMPTY";
            declaration.IsEmpty = true;
        }
        else if (content.Token.Kind == DtdTokenKind.Name && content.Token.Name == "ANY")
        {
            declaration.ContentModel = "ANY";
            declaration.IsAny = true;
        }
        else if (content.Token.Kind == DtdTokenKind.Symbol && content.Token.Value == "(")
        {
            Reader.ConsumeDtdDeclarationWhitespace();
            DtdTokenEntry? optionalFirst = Reader.PeekDtdDeclarationEntry();
            if (optionalFirst != null && optionalFirst.Token.Kind == DtdTokenKind.Symbol && optionalFirst.Token.Value == "#")
            {
                declaration.ContentModel = ParseDtdMixedContent();
                declaration.IsMixed = true;
            }
            else
            {
                DtdContentParticle particle = ParseDtdChildrenGroup();
                ValidateDeterministicDtdContentModel(particle);
                declaration.ContentModel = RenderDtdContentParticle(particle);
            }
        }
        else
        {
            throw CreateDtdElementError("An element content specification must be EMPTY, ANY, mixed content, or a children group", content.Token.StartUtf16Offset);
        }

        Reader.ConsumeDtdDeclarationWhitespace();
        if (!Reader.IsDtdDeclarationEnd())
        {
            DtdTokenEntry? unexpected = Reader.PeekDtdDeclarationEntry();
            throw CreateDtdElementError("An element declaration contains unexpected content", unexpected == null ? DeclarationOffset : unexpected.Token.StartUtf16Offset);
        }

        declaration.ReferencedNames = ReferencedNames.ToArray();
        if (CompiledModel == null)
        {
            CompiledModel = new DtdCompiledContentModel(name, declaration.IsEmpty, declaration.IsAny, declaration.IsMixed, declaration.IsMixed ? declaration.ReferencedNames : [], [], [], [], [], declaration.IsMixed || declaration.IsEmpty || declaration.IsAny);
        }

        else
        {
            CompiledModel.ElementName = name;
        }

        return declaration;
    }

    internal string ParseDtdMixedContent()
    {
        DtdTokenEntry hash = ReadRequiredDtdElementEntry("Mixed content must begin with #PCDATA");
        DtdTokenEntry pcdata = ReadRequiredDtdElementEntry("Mixed content must begin with #PCDATA");
        if (hash.Token.Kind != DtdTokenKind.Symbol || hash.Token.Value != "#" || pcdata.Token.Kind != DtdTokenKind.Name || pcdata.Token.Name != "PCDATA")
        {
            throw CreateDtdElementError("Mixed content must begin with #PCDATA", hash.Token.StartUtf16Offset);
        }

        List<string> names = new List<string>();
        Reader.ConsumeDtdDeclarationWhitespace();
        while (true)
        {
            DtdTokenEntry entry = ReadRequiredDtdElementEntry("Mixed content requires a closing ')'");
            if (entry.Token.Kind == DtdTokenKind.Symbol && entry.Token.Value == ")")
            {
                bool repeated = false;
                DtdTokenEntry? optionalOccurrence = Reader.PeekDtdDeclarationEntry();
                if (optionalOccurrence != null && optionalOccurrence.Token.Kind == DtdTokenKind.Symbol && optionalOccurrence.Token.Value == "*")
                {
                    Reader.ReadDtdDeclarationEntry();
                    repeated = true;
                }

                if (names.Count > 0 && !repeated)
                {
                    throw CreateDtdElementError("Mixed content with element names must end with '*'", entry.Token.StartUtf16Offset + entry.Token.Utf16Length);
                }

                StringBuilder model = new StringBuilder();
                model.Append("(#PCDATA");
                int nameIndex = 0;
                while (nameIndex < names.Count)
                {
                    model.Append('|');
                    model.Append(names[nameIndex]);
                    nameIndex = nameIndex + 1;
                }

                model.Append(')');
                if (repeated)
                {
                    model.Append('*');
                }

                return model.ToString();
            }

            if (entry.Token.Kind != DtdTokenKind.Symbol || entry.Token.Value != "|")
            {
                throw CreateDtdElementError("Mixed content permits only '|' separators and a closing ')'", entry.Token.StartUtf16Offset);
            }

            Reader.ConsumeDtdDeclarationWhitespace();
            string name = ReadRequiredDtdElementName("A mixed-content separator must be followed by an XML Name");
            if (names.Contains(name))
            {
                throw CreateDtdElementError("An element name cannot occur more than once in a mixed-content declaration", entry.Token.StartUtf16Offset);
            }

            names.Add(name);
            AddReferencedDtdElementName(name);
            Reader.ConsumeDtdDeclarationWhitespace();
        }
    }

    internal DtdContentParticle ParseDtdChildrenGroup()
    {
        List<DtdContentParticle> children = new List<DtdContentParticle>();
        children.Add(ParseDtdContentParticle());
        Reader.ConsumeDtdDeclarationWhitespace();
        DtdTokenEntry delimiter = ReadRequiredDtdElementEntry("A children content group requires a closing ')'");
        if (delimiter.Token.Kind == DtdTokenKind.Symbol && delimiter.Token.Value == ")")
        {
            DtdContentParticle single = new DtdContentParticle(DtdContentParticleKind.Sequence, null, children, DtdContentOccurrence.ExactlyOne, -1);
            ApplyDtdContentOccurrence(single);
            return single;
        }

        if (delimiter.Token.Kind != DtdTokenKind.Symbol || delimiter.Token.Value != "," && delimiter.Token.Value != "|")
        {
            throw CreateDtdElementError("A children content group requires ',', '|', or ')'", delimiter.Token.StartUtf16Offset);
        }

        string separator = delimiter.Token.Value;
        DtdContentParticleKind kind = separator == "," ? DtdContentParticleKind.Sequence : DtdContentParticleKind.Choice;
        while (true)
        {
            Reader.ConsumeDtdDeclarationWhitespace();
            children.Add(ParseDtdContentParticle());
            Reader.ConsumeDtdDeclarationWhitespace();
            DtdTokenEntry next = ReadRequiredDtdElementEntry("A children content group requires a closing ')'");
            if (next.Token.Kind == DtdTokenKind.Symbol && next.Token.Value == ")")
            {
                DtdContentParticle group = new DtdContentParticle(kind, null, children, DtdContentOccurrence.ExactlyOne, -1);
                ApplyDtdContentOccurrence(group);
                return group;
            }

            if (next.Token.Kind != DtdTokenKind.Symbol || next.Token.Value != separator)
            {
                throw CreateDtdElementError("A content group cannot mix ',' and '|' separators", next.Token.StartUtf16Offset);
            }
        }
    }

    internal DtdContentParticle ParseDtdContentParticle()
    {
        DtdTokenEntry entry = ReadRequiredDtdElementEntry("A children content group requires a content particle");
        DtdContentParticle particle;
        if (entry.Token.Kind == DtdTokenKind.Name && entry.Token.Name != null)
        {
            int position = PositionNames.Count;
            PositionNames.Add(entry.Token.Name);
            FollowPositions.Add(new List<int>());
            AddReferencedDtdElementName(entry.Token.Name);
            particle = new DtdContentParticle(DtdContentParticleKind.Name, entry.Token.Name, new List<DtdContentParticle>(), DtdContentOccurrence.ExactlyOne, position);
            ApplyDtdContentOccurrence(particle);
            return particle;
        }

        if (entry.Token.Kind == DtdTokenKind.Symbol && entry.Token.Value == "(")
        {
            Reader.ConsumeDtdDeclarationWhitespace();
            DtdTokenEntry? optionalFirst = Reader.PeekDtdDeclarationEntry();
            if (optionalFirst != null && optionalFirst.Token.Kind == DtdTokenKind.Symbol && optionalFirst.Token.Value == "#")
            {
                throw CreateDtdElementError("Mixed content cannot be nested inside a children content model", optionalFirst.Token.StartUtf16Offset);
            }

            return ParseDtdChildrenGroup();
        }

        throw CreateDtdElementError("A content particle must be an element name or a parenthesized group", entry.Token.StartUtf16Offset);
    }

    internal void ApplyDtdContentOccurrence(DtdContentParticle particle)
    {
        DtdTokenEntry? optionalOccurrence = Reader.PeekDtdDeclarationEntry();
        if (optionalOccurrence == null || optionalOccurrence.Token.Kind != DtdTokenKind.Symbol)
        {
            return;
        }

        if (optionalOccurrence.Token.Value == "?")
        {
            particle.Occurrence = DtdContentOccurrence.ZeroOrOne;
        }
        else if (optionalOccurrence.Token.Value == "*")
        {
            particle.Occurrence = DtdContentOccurrence.ZeroOrMore;
        }
        else if (optionalOccurrence.Token.Value == "+")
        {
            particle.Occurrence = DtdContentOccurrence.OneOrMore;
        }
        else
        {
            return;
        }

        Reader.ReadDtdDeclarationEntry();
    }

    internal void ValidateDeterministicDtdContentModel(DtdContentParticle particle)
    {
        DtdContentAnalysis analysis = AnalyzeDtdContentParticle(particle);
        if (HasDuplicateDtdPositionName(analysis.FirstPositions))
        {
            throw CreateDtdElementError("The children content model is not deterministic", DeclarationOffset);
        }

        int index = 0;
        while (index < FollowPositions.Count)
        {
            if (HasDuplicateDtdPositionName(FollowPositions[index]))
            {
                throw CreateDtdElementError("The children content model is not deterministic", DeclarationOffset);
            }

            index = index + 1;
        }

        int[][] compiledFollow = new int[FollowPositions.Count][];
        index = 0;
        while (index < FollowPositions.Count)
        {
            compiledFollow[index] = FollowPositions[index].ToArray();
            index = index + 1;
        }

        CompiledModel = new DtdCompiledContentModel("", false, false, false, [], PositionNames.ToArray(), analysis.FirstPositions.ToArray(), compiledFollow, analysis.LastPositions.ToArray(), analysis.Nullable);
    }

    internal DtdContentAnalysis AnalyzeDtdContentParticle(DtdContentParticle particle)
    {
        if (particle.Kind == DtdContentParticleKind.Name)
        {
            List<int> position = new List<int>();
            position.Add(particle.Position);
            bool nullableName = particle.Occurrence == DtdContentOccurrence.ZeroOrOne || particle.Occurrence == DtdContentOccurrence.ZeroOrMore;
            DtdContentAnalysis nameAnalysis = new DtdContentAnalysis(nullableName, position, new List<int>(position));
            ApplyDtdOccurrenceFollow(particle, nameAnalysis);
            return nameAnalysis;
        }

        DtdContentAnalysis[] analyses = new DtdContentAnalysis[particle.Children.Count];
        int childIndex = 0;
        while (childIndex < particle.Children.Count)
        {
            analyses[childIndex] = AnalyzeDtdContentParticle(particle.Children[childIndex]);
            childIndex = childIndex + 1;
        }

        bool nullable;
        List<int> first = new List<int>();
        List<int> last = new List<int>();
        if (particle.Kind == DtdContentParticleKind.Choice)
        {
            nullable = false;
            childIndex = 0;
            while (childIndex < analyses.Length)
            {
                nullable = nullable || analyses[childIndex].Nullable;
                AddDtdPositions(first, analyses[childIndex].FirstPositions);
                AddDtdPositions(last, analyses[childIndex].LastPositions);
                childIndex = childIndex + 1;
            }
        }
        else
        {
            nullable = true;
            childIndex = 0;
            while (childIndex < analyses.Length)
            {
                if (nullable)
                {
                    AddDtdPositions(first, analyses[childIndex].FirstPositions);
                }

                nullable = nullable && analyses[childIndex].Nullable;
                childIndex = childIndex + 1;
            }

            bool nullableSuffix = true;
            childIndex = analyses.Length - 1;
            while (childIndex >= 0)
            {
                if (nullableSuffix)
                {
                    AddDtdPositions(last, analyses[childIndex].LastPositions);
                }

                nullableSuffix = nullableSuffix && analyses[childIndex].Nullable;
                childIndex = childIndex - 1;
            }

            AddDtdSequenceFollowPositions(analyses);
        }

        if (particle.Occurrence == DtdContentOccurrence.ZeroOrOne || particle.Occurrence == DtdContentOccurrence.ZeroOrMore)
        {
            nullable = true;
        }

        DtdContentAnalysis analysis = new DtdContentAnalysis(nullable, first, last);
        ApplyDtdOccurrenceFollow(particle, analysis);
        return analysis;
    }

    internal void AddDtdSequenceFollowPositions(DtdContentAnalysis[] analyses)
    {
        int sourceIndex = 0;
        while (sourceIndex < analyses.Length - 1)
        {
            List<int> following = new List<int>();
            int targetIndex = sourceIndex + 1;
            bool continueTarget = true;
            while (targetIndex < analyses.Length && continueTarget)
            {
                AddDtdPositions(following, analyses[targetIndex].FirstPositions);
                continueTarget = analyses[targetIndex].Nullable;
                targetIndex = targetIndex + 1;
            }

            int positionIndex = 0;
            while (positionIndex < analyses[sourceIndex].LastPositions.Count)
            {
                AddDtdPositions(FollowPositions[analyses[sourceIndex].LastPositions[positionIndex]], following);
                positionIndex = positionIndex + 1;
            }

            sourceIndex = sourceIndex + 1;
        }
    }

    internal void ApplyDtdOccurrenceFollow(DtdContentParticle particle, DtdContentAnalysis analysis)
    {
        if (particle.Occurrence != DtdContentOccurrence.ZeroOrMore && particle.Occurrence != DtdContentOccurrence.OneOrMore)
        {
            return;
        }

        int index = 0;
        while (index < analysis.LastPositions.Count)
        {
            AddDtdPositions(FollowPositions[analysis.LastPositions[index]], analysis.FirstPositions);
            index = index + 1;
        }
    }

    internal bool HasDuplicateDtdPositionName(List<int> positions)
    {
        int left = 0;
        while (left < positions.Count)
        {
            int right = left + 1;
            while (right < positions.Count)
            {
                if (PositionNames[positions[left]] == PositionNames[positions[right]])
                {
                    return true;
                }

                right = right + 1;
            }

            left = left + 1;
        }

        return false;
    }

    internal void AddDtdPositions(List<int> destination, List<int> source)
    {
        int index = 0;
        while (index < source.Count)
        {
            if (!destination.Contains(source[index]))
            {
                destination.Add(source[index]);
            }

            index = index + 1;
        }
    }

    internal string RenderDtdContentParticle(DtdContentParticle particle)
    {
        StringBuilder model = new StringBuilder();
        if (particle.Kind == DtdContentParticleKind.Name)
        {
            model.Append(particle.Name);
        }
        else
        {
            model.Append('(');
            char separator = particle.Kind == DtdContentParticleKind.Sequence ? ',' : '|';
            int index = 0;
            while (index < particle.Children.Count)
            {
                if (index > 0)
                {
                    model.Append(separator);
                }

                model.Append(RenderDtdContentParticle(particle.Children[index]));
                index = index + 1;
            }

            model.Append(')');
        }

        if (particle.Occurrence == DtdContentOccurrence.ZeroOrOne)
        {
            model.Append('?');
        }
        else if (particle.Occurrence == DtdContentOccurrence.ZeroOrMore)
        {
            model.Append('*');
        }
        else if (particle.Occurrence == DtdContentOccurrence.OneOrMore)
        {
            model.Append('+');
        }

        return model.ToString();
    }

    internal void AddReferencedDtdElementName(string name)
    {
        if (!ReferencedNames.Contains(name))
        {
            ReferencedNames.Add(name);
        }
    }

    internal XmlName CreateDtdElementName(string qualifiedName)
    {
        XmlName name = new XmlName();
        name.QualifiedName = qualifiedName;
        int colon = qualifiedName.IndexOf(':');
        if (colon > 0 && colon < qualifiedName.Length - 1)
        {
            name.Prefix = qualifiedName.Substring(0, colon);
            name.LocalName = qualifiedName.Substring(colon + 1);
        }
        else
        {
            name.Prefix = null;
            name.LocalName = qualifiedName;
        }

        return name;
    }

    internal void RequireDtdElementWhitespace()
    {
        if (!Reader.ConsumeDtdDeclarationWhitespace())
        {
            DtdTokenEntry? entry = Reader.PeekDtdDeclarationEntry();
            throw CreateDtdElementError("Whitespace is required in an element declaration", entry == null ? DeclarationOffset : entry.Token.StartUtf16Offset);
        }
    }

    internal DtdTokenEntry ReadRequiredDtdElementEntry(string message)
    {
        DtdTokenEntry? entry = Reader.ReadDtdDeclarationEntry();
        if (entry == null)
        {
            throw CreateDtdElementError(message, DeclarationOffset);
        }

        return entry;
    }

    internal string ReadRequiredDtdElementName(string message)
    {
        DtdTokenEntry entry = ReadRequiredDtdElementEntry(message);
        if (entry.Token.Kind != DtdTokenKind.Name || entry.Token.Name == null)
        {
            throw CreateDtdElementError(message, entry.Token.StartUtf16Offset);
        }

        return entry.Token.Name;
    }

    internal FormatException CreateDtdElementError(string message, int offset)
    {
        return new FormatException(message + " at UTF-16 offset " + offset + ".");
    }
}
