using System;
using System.Collections.Generic;
using System.Numerics;

namespace xml;

internal class XmlSchemaContentModelValidator
{
    internal CompiledXmlSchemaSet SchemaSet;
    internal XmlNode[] Elements;
    internal CompiledXmlSchemaParticle? RootParticle;
    internal CompiledXmlSchemaWildcard? InterleaveWildcard;
    internal int WorkCount;
    internal int MaximumWorkCount;

    internal XmlSchemaContentModelValidator(CompiledXmlSchemaSet schemaSet)
    {
        Guard.NotNull(schemaSet, "schemaSet");
        SchemaSet = schemaSet;
        Elements = [];
        RootParticle = null;
        InterleaveWildcard = null;
        WorkCount = 0;
        MaximumWorkCount = 100000;
    }

    internal XmlSchemaParticleMatchState? MatchXmlSchemaContent(CompiledXmlSchemaComplexType type, XmlNode node)
    {
        Elements = CollectXmlSchemaElementChildren(node);
        RootParticle = type.Particle;
        InterleaveWildcard = type.OpenContent != null && type.OpenContent.Mode == XmlSchemaOpenContentMode.Interleave ? type.OpenContent.Wildcard : null;
        WorkCount = 0;
        XmlSchemaParticleMatchState initial = new XmlSchemaParticleMatchState(0, Elements.Length);
        List<XmlSchemaParticleMatchState> states = new List<XmlSchemaParticleMatchState>();
        states.Add(initial);
        if (type.Particle != null)
        {
            states = MatchXmlSchemaParticle(type.Particle, states);
        }

        if (InterleaveWildcard != null)
        {
            states = ExpandXmlSchemaInterleave(states);
        }

        int index = 0;
        while (index < states.Count)
        {
            XmlSchemaParticleMatchState state = states[index];
            if (type.OpenContent != null && type.OpenContent.Mode == XmlSchemaOpenContentMode.Suffix && type.OpenContent.Wildcard != null)
            {
                state = MatchXmlSchemaWildcardSuffix(state, type.OpenContent.Wildcard);
            }

            if (state.Position == Elements.Length)
            {
                return state;
            }

            index = index + 1;
        }

        return null;
    }

    internal List<XmlSchemaParticleMatchState> MatchXmlSchemaParticle(CompiledXmlSchemaParticle particle, List<XmlSchemaParticleMatchState> starts)
    {
        List<XmlSchemaParticleMatchState> frontier = InterleaveWildcard == null ? starts : ExpandXmlSchemaInterleave(starts);
        List<XmlSchemaParticleMatchState> accepted = new List<XmlSchemaParticleMatchState>();
        BigInteger count = BigInteger.Zero;
        if (particle.MinOccurs == BigInteger.Zero)
        {
            AddDistinctXmlSchemaStates(accepted, frontier);
        }

        while (particle.MaxOccursUnbounded || count < particle.MaxOccurs)
        {
            CheckXmlSchemaContentWork();
            List<XmlSchemaParticleMatchState> next = MatchOneXmlSchemaParticle(particle, frontier);
            if (next.Count == 0)
            {
                break;
            }

            count = count + BigInteger.One;
            if (count >= particle.MinOccurs)
            {
                AddDistinctXmlSchemaStates(accepted, next);
            }

            bool progress = HasXmlSchemaPositionProgress(frontier, next);
            if (!progress)
            {
                if (count < particle.MinOccurs)
                {
                    AddDistinctXmlSchemaStates(accepted, next);
                }

                break;
            }

            frontier = next;
            if (frontier.Count == 0)
            {
                break;
            }
        }

        return InterleaveWildcard == null ? accepted : ExpandXmlSchemaInterleave(accepted);
    }

    internal List<XmlSchemaParticleMatchState> MatchOneXmlSchemaParticle(CompiledXmlSchemaParticle particle, List<XmlSchemaParticleMatchState> starts)
    {
        if (particle.Kind == XmlSchemaParticleKind.Empty)
        {
            return starts;
        }

        if (particle.Kind == XmlSchemaParticleKind.Element)
        {
            return MatchOneXmlSchemaElementParticle(particle, starts);
        }

        if (particle.Kind == XmlSchemaParticleKind.Any)
        {
            return MatchOneXmlSchemaWildcardParticle(particle, starts);
        }

        if (particle.Kind == XmlSchemaParticleKind.Choice)
        {
            return MatchOneXmlSchemaChoiceParticle(particle, starts);
        }

        if (particle.Kind == XmlSchemaParticleKind.All)
        {
            return MatchOneXmlSchemaAllParticle(particle, starts);
        }

        return MatchOneXmlSchemaSequenceParticle(particle, starts);
    }

    internal List<XmlSchemaParticleMatchState> MatchOneXmlSchemaElementParticle(CompiledXmlSchemaParticle particle, List<XmlSchemaParticleMatchState> starts)
    {
        List<XmlSchemaParticleMatchState> results = new List<XmlSchemaParticleMatchState>();
        CompiledXmlSchemaElementDeclaration? expected = particle.ElementDeclaration;
        int index = 0;
        while (index < starts.Count)
        {
            XmlSchemaParticleMatchState state = starts[index];
            if (state.Position < Elements.Length && expected != null)
            {
                XmlName? actualName = Elements[state.Position].Name;
                CompiledXmlSchemaElementDeclaration? actual = actualName == null ? null : FindXmlSchemaParticleElementDeclaration(expected, actualName);
                if (actual != null)
                {
                    XmlSchemaParticleMatchState matched = CopyXmlSchemaParticleState(state);
                    matched.Declarations[state.Position] = actual;
                    matched.Processing[state.Position] = XmlSchemaProcessContents.Strict;
                    matched.Position = state.Position + 1;
                    AddDistinctXmlSchemaState(results, matched);
                }
            }

            index = index + 1;
        }

        return results;
    }

    internal List<XmlSchemaParticleMatchState> MatchOneXmlSchemaWildcardParticle(CompiledXmlSchemaParticle particle, List<XmlSchemaParticleMatchState> starts)
    {
        List<XmlSchemaParticleMatchState> results = new List<XmlSchemaParticleMatchState>();
        CompiledXmlSchemaWildcard? wildcard = particle.Wildcard;
        if (wildcard == null)
        {
            return results;
        }

        int index = 0;
        while (index < starts.Count)
        {
            XmlSchemaParticleMatchState state = starts[index];
            if (state.Position < Elements.Length)
            {
                XmlName? name = Elements[state.Position].Name;
                if (name != null && IsXmlSchemaWildcardMatch(wildcard, name, false))
                {
                    XmlSchemaParticleMatchState matched = CopyXmlSchemaParticleState(state);
                    matched.Declarations[state.Position] = FindGlobalXmlSchemaElement(name);
                    matched.Processing[state.Position] = wildcard.ProcessContents;
                    matched.Position = state.Position + 1;
                    AddDistinctXmlSchemaState(results, matched);
                }
            }

            index = index + 1;
        }

        return results;
    }

    internal List<XmlSchemaParticleMatchState> MatchOneXmlSchemaSequenceParticle(CompiledXmlSchemaParticle particle, List<XmlSchemaParticleMatchState> starts)
    {
        List<XmlSchemaParticleMatchState> current = starts;
        int index = 0;
        while (index < particle.Children.Length)
        {
            current = MatchXmlSchemaParticle(particle.Children[index], current);
            if (current.Count == 0)
            {
                return current;
            }

            index = index + 1;
        }

        return current;
    }

    internal List<XmlSchemaParticleMatchState> MatchOneXmlSchemaChoiceParticle(CompiledXmlSchemaParticle particle, List<XmlSchemaParticleMatchState> starts)
    {
        List<XmlSchemaParticleMatchState> results = new List<XmlSchemaParticleMatchState>();
        int index = 0;
        while (index < particle.Children.Length)
        {
            AddDistinctXmlSchemaStates(results, MatchXmlSchemaParticle(particle.Children[index], starts));
            index = index + 1;
        }

        return results;
    }

    internal List<XmlSchemaParticleMatchState> MatchOneXmlSchemaAllParticle(CompiledXmlSchemaParticle particle, List<XmlSchemaParticleMatchState> starts)
    {
        List<XmlSchemaParticleMatchState> results = new List<XmlSchemaParticleMatchState>();
        int index = 0;
        while (index < starts.Count)
        {
            bool[] used = new bool[particle.Children.Length];
            MatchXmlSchemaAllChildren(particle, starts[index], used, 0, results);
            index = index + 1;
        }

        return results;
    }

    internal void MatchXmlSchemaAllChildren(CompiledXmlSchemaParticle particle, XmlSchemaParticleMatchState state, bool[] used, int usedCount, List<XmlSchemaParticleMatchState> results)
    {
        CheckXmlSchemaContentWork();
        if (usedCount == particle.Children.Length)
        {
            AddDistinctXmlSchemaState(results, state);
            return;
        }

        int index = 0;
        while (index < particle.Children.Length)
        {
            if (!used[index])
            {
                bool[] nextUsed = new bool[used.Length];
                Array.Copy(used, nextUsed, used.Length);
                nextUsed[index] = true;
                List<XmlSchemaParticleMatchState> starts = new List<XmlSchemaParticleMatchState>();
                starts.Add(state);
                List<XmlSchemaParticleMatchState> matches = MatchXmlSchemaParticle(particle.Children[index], starts);
                int matchIndex = 0;
                while (matchIndex < matches.Count)
                {
                    MatchXmlSchemaAllChildren(particle, matches[matchIndex], nextUsed, usedCount + 1, results);
                    matchIndex = matchIndex + 1;
                }
            }

            index = index + 1;
        }
    }

    internal List<XmlSchemaParticleMatchState> ExpandXmlSchemaInterleave(List<XmlSchemaParticleMatchState> starts)
    {
        List<XmlSchemaParticleMatchState> results = new List<XmlSchemaParticleMatchState>();
        AddDistinctXmlSchemaStates(results, starts);
        CompiledXmlSchemaWildcard? wildcard = InterleaveWildcard;
        if (wildcard == null)
        {
            return results;
        }

        int index = 0;
        while (index < results.Count)
        {
            XmlSchemaParticleMatchState state = results[index];
            if (state.Position < Elements.Length)
            {
                XmlName? name = Elements[state.Position].Name;
                if (name != null && IsXmlSchemaWildcardMatch(wildcard, name, false))
                {
                    XmlSchemaParticleMatchState consumed = CopyXmlSchemaParticleState(state);
                    consumed.Declarations[state.Position] = FindGlobalXmlSchemaElement(name);
                    consumed.Processing[state.Position] = wildcard.ProcessContents;
                    consumed.Position = state.Position + 1;
                    AddDistinctXmlSchemaState(results, consumed);
                }
            }

            index = index + 1;
        }

        return results;
    }

    internal XmlSchemaParticleMatchState MatchXmlSchemaWildcardSuffix(XmlSchemaParticleMatchState state, CompiledXmlSchemaWildcard wildcard)
    {
        XmlSchemaParticleMatchState result = CopyXmlSchemaParticleState(state);
        while (result.Position < Elements.Length)
        {
            XmlName? name = Elements[result.Position].Name;
            if (name == null || !IsXmlSchemaWildcardMatch(wildcard, name, false))
            {
                return result;
            }

            result.Declarations[result.Position] = FindGlobalXmlSchemaElement(name);
            result.Processing[result.Position] = wildcard.ProcessContents;
            result.Position = result.Position + 1;
        }

        return result;
    }

    internal bool IsXmlSchemaWildcardMatch(CompiledXmlSchemaWildcard wildcard, XmlName name, bool attribute)
    {
        if (wildcard.Combination != XmlSchemaWildcardCombination.None)
        {
            bool aggregate = wildcard.Combination == XmlSchemaWildcardCombination.Intersection;
            int operandIndex = 0;
            while (operandIndex < wildcard.Operands.Length)
            {
                bool operand = IsXmlSchemaWildcardMatch(wildcard.Operands[operandIndex], name, attribute);
                if (wildcard.Combination == XmlSchemaWildcardCombination.Union)
                {
                    aggregate = aggregate || operand;
                }
                else
                {
                    aggregate = aggregate && operand;
                }

                operandIndex = operandIndex + 1;
            }

            return aggregate;
        }

        string namespaceUri = name.NamespaceUri == null ? "" : name.NamespaceUri;
        bool namespaceMatch = wildcard.Kind == XmlSchemaWildcardKind.Any;
        if (wildcard.Kind == XmlSchemaWildcardKind.Other || wildcard.Kind == XmlSchemaWildcardKind.Exclusion)
        {
            namespaceMatch = !ContainsXmlSchemaNamespace(wildcard.ExcludedNamespaces, namespaceUri);
        }
        else if (wildcard.Kind == XmlSchemaWildcardKind.Enumeration)
        {
            namespaceMatch = ContainsXmlSchemaNamespace(wildcard.Namespaces, namespaceUri);
        }

        if (!namespaceMatch || ContainsXmlSchemaExcludedName(wildcard.ExcludedNames, name))
        {
            return false;
        }

        if (wildcard.ExcludesDefinedNames)
        {
            bool defined = attribute ? FindGlobalXmlSchemaAttribute(name) != null : FindGlobalXmlSchemaElement(name) != null;
            if (defined)
            {
                return false;
            }
        }

        if (!attribute && wildcard.ExcludesDefinedSiblingNames && ContainsXmlSchemaSiblingName(RootParticle, name))
        {
            return false;
        }

        return true;
    }

    internal bool ContainsXmlSchemaSiblingName(CompiledXmlSchemaParticle? particle, XmlName name)
    {
        if (particle == null)
        {
            return false;
        }

        if (particle.Kind == XmlSchemaParticleKind.Element && particle.ElementDeclaration != null && SameXmlSchemaName(particle.ElementDeclaration.Name, name))
        {
            return true;
        }

        int index = 0;
        while (index < particle.Children.Length)
        {
            if (ContainsXmlSchemaSiblingName(particle.Children[index], name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal CompiledXmlSchemaElementDeclaration? FindXmlSchemaParticleElementDeclaration(CompiledXmlSchemaElementDeclaration expected, XmlName actualName)
    {
        if (SameXmlSchemaName(expected.Name, actualName))
        {
            return expected;
        }

        if (((int)expected.Block & (int)XmlSchemaDerivationMethod.Substitution) != 0)
        {
            return null;
        }

        CompiledXmlSchemaElementDeclaration? candidate = FindGlobalXmlSchemaElement(actualName);
        CompiledXmlSchemaElementDeclaration? current = candidate;
        while (current != null)
        {
            if (current.SubstitutionGroupHead == expected)
            {
                return candidate;
            }

            current = current.SubstitutionGroupHead;
        }

        return null;
    }

    internal CompiledXmlSchemaElementDeclaration? FindGlobalXmlSchemaElement(XmlName name)
    {
        int index = SchemaSet.Elements.Length - 1;
        while (index >= 0)
        {
            CompiledXmlSchemaElementDeclaration candidate = SchemaSet.Elements[index];
            if (candidate.IsGlobal && SameXmlSchemaName(candidate.Name, name))
            {
                return candidate;
            }

            index = index - 1;
        }

        return null;
    }

    internal CompiledXmlSchemaAttributeDeclaration? FindGlobalXmlSchemaAttribute(XmlName name)
    {
        int index = SchemaSet.Attributes.Length - 1;
        while (index >= 0)
        {
            CompiledXmlSchemaAttributeDeclaration candidate = SchemaSet.Attributes[index];
            if (candidate.IsGlobal && SameXmlSchemaName(candidate.Name, name))
            {
                return candidate;
            }

            index = index - 1;
        }

        return null;
    }

    internal bool SameXmlSchemaName(XmlName left, XmlName right)
    {
        string leftNamespace = left.NamespaceUri == null ? "" : left.NamespaceUri;
        string rightNamespace = right.NamespaceUri == null ? "" : right.NamespaceUri;
        return string.Equals(leftNamespace, rightNamespace, StringComparison.Ordinal) && string.Equals(left.LocalName, right.LocalName, StringComparison.Ordinal);
    }

    internal bool ContainsXmlSchemaNamespace(string[] values, string candidate)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (string.Equals(values[index], candidate, StringComparison.Ordinal))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsXmlSchemaExcludedName(XmlName[] values, XmlName candidate)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (SameXmlSchemaName(values[index], candidate))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool HasXmlSchemaPositionProgress(List<XmlSchemaParticleMatchState> before, List<XmlSchemaParticleMatchState> after)
    {
        int afterIndex = 0;
        while (afterIndex < after.Count)
        {
            int beforeIndex = 0;
            while (beforeIndex < before.Count)
            {
                if (after[afterIndex].Position > before[beforeIndex].Position)
                {
                    return true;
                }

                beforeIndex = beforeIndex + 1;
            }

            afterIndex = afterIndex + 1;
        }

        return false;
    }

    internal XmlSchemaParticleMatchState CopyXmlSchemaParticleState(XmlSchemaParticleMatchState source)
    {
        XmlSchemaParticleMatchState result = new XmlSchemaParticleMatchState(source.Position, source.Declarations.Length);
        Array.Copy(source.Declarations, result.Declarations, source.Declarations.Length);
        Array.Copy(source.Processing, result.Processing, source.Processing.Length);
        return result;
    }

    internal void AddDistinctXmlSchemaStates(List<XmlSchemaParticleMatchState> target, List<XmlSchemaParticleMatchState> values)
    {
        int index = 0;
        while (index < values.Count)
        {
            AddDistinctXmlSchemaState(target, values[index]);
            index = index + 1;
        }
    }

    internal void AddDistinctXmlSchemaState(List<XmlSchemaParticleMatchState> target, XmlSchemaParticleMatchState value)
    {
        int index = 0;
        while (index < target.Count)
        {
            if (SameXmlSchemaParticleState(target[index], value))
            {
                return;
            }

            index = index + 1;
        }

        target.Add(value);
    }

    internal bool SameXmlSchemaParticleState(XmlSchemaParticleMatchState left, XmlSchemaParticleMatchState right)
    {
        if (left.Position != right.Position)
        {
            return false;
        }

        int index = 0;
        while (index < left.Position)
        {
            if (left.Declarations[index] != right.Declarations[index] || left.Processing[index] != right.Processing[index])
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal XmlNode[] CollectXmlSchemaElementChildren(XmlNode node)
    {
        List<XmlNode> result = new List<XmlNode>();
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].Kind == XmlNodeKind.Element)
            {
                result.Add(children[index]);
            }

            index = index + 1;
        }

        return result.ToArray();
    }

    internal void CheckXmlSchemaContentWork()
    {
        WorkCount = WorkCount + 1;
        if (WorkCount > MaximumWorkCount)
        {
            throw CreateXmlSchemaContentModelError("The schema content-model work limit was exceeded.");
        }
    }

    internal XmlLimitException CreateXmlSchemaContentModelError(string message)
    {
        XmlDiagnostic diagnostic = new XmlDiagnostic();
        diagnostic.Code = XmlErrorCodes.LimitExceeded;
        diagnostic.Category = XmlErrorCategory.Limit;
        diagnostic.Severity = XmlDiagnosticSeverity.Fatal;
        diagnostic.Message = message;
        return new XmlLimitException(message, diagnostic, null);
    }
}
