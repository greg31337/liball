using System;
using System.Collections.Generic;

namespace xml;

internal enum XmlEntityDispatchKind
{
    Character = 0,
    StartEntity = 1,
    ReplacementText = 2,
    EndEntity = 3,
    SkippedEntity = 4
}

internal class XmlEntityDispatchItem
{
    internal XmlEntityDispatchKind Kind;
    internal string? Name;
    internal string? Value;
    internal int ScalarValue;
    internal bool RestrictedCharacter;

    internal XmlEntityDispatchItem(XmlEntityDispatchKind kind, string? name, string? value, int scalarValue, bool restrictedCharacter)
    {
        Kind = kind;
        Name = name;
        Value = value;
        ScalarValue = scalarValue;
        RestrictedCharacter = restrictedCharacter;
    }
}

internal class XmlEntityDispatchResult
{
    internal XmlEntityDispatchItem[] Items;
    internal bool Resolved;
    internal bool Skipped;
    internal string? Name;

    internal XmlEntityDispatchResult(XmlEntityDispatchItem[] items, bool resolved, bool skipped, string? name)
    {
        Items = items;
        Resolved = resolved;
        Skipped = skipped;
        Name = name;
    }
}

internal interface IXmlGeneralEntitySource
{
    XmlEntityDeclaration? FindXmlGeneralEntity(string name);
}

internal interface IXmlExternalGeneralEntitySource
{
    DtdLoadedSource? LoadXmlExternalGeneralEntity(XmlEntityDeclaration declaration);
}

internal interface IXmlEntityReplacementScanSource
{
    string? FindXmlEntityReplacementScanText(string name);
}

internal interface IXmlEntityReferenceObserver
{
    void ObserveXmlEntityReference(string name, int offset);
}

internal class EntityExpander
{
    internal XmlVersion Version;
    internal XmlEntityHandling Handling;
    internal bool SkipUnresolvedEntities;
    internal IXmlGeneralEntitySource? EntitySource;
    internal IXmlExternalGeneralEntitySource? ExternalEntitySource;
    internal IXmlEntityReplacementScanSource? ReplacementScanSource;
    internal IXmlEntityReferenceObserver? ReferenceObserver;
    internal ParserWorkLimiter? WorkLimiter;
    internal List<string> ActiveEntityNames;
    internal bool ExpansionSkippedEntity;
    internal bool MapWellFormednessErrors;
    internal SourceTracker? ErrorSourceTracker;
    internal IXmlErrorReporter? ErrorReporter;

    internal EntityExpander(XmlVersion version, XmlEntityHandling handling, bool skipUnresolvedEntities, IXmlGeneralEntitySource? entitySource)
    {
        if (version != XmlVersion.Xml10 && version != XmlVersion.Xml11)
        {
            throw new ArgumentOutOfRangeException("version");
        }

        if (handling != XmlEntityHandling.Expand && handling != XmlEntityHandling.PreserveReferences)
        {
            throw new ArgumentOutOfRangeException("handling");
        }

        Version = version;
        Handling = handling;
        SkipUnresolvedEntities = skipUnresolvedEntities;
        EntitySource = entitySource;
        ExternalEntitySource = null;
        ReplacementScanSource = null;
        ReferenceObserver = null;
        WorkLimiter = null;
        ActiveEntityNames = new List<string>();
        ExpansionSkippedEntity = false;
        MapWellFormednessErrors = false;
        ErrorSourceTracker = null;
        ErrorReporter = null;
    }

    internal void ConfigureXmlEntityErrors(SourceTracker tracker, IXmlErrorReporter? reporter)
    {
        Guard.NotNull(tracker, "tracker");
        MapWellFormednessErrors = true;
        ErrorSourceTracker = tracker;
        ErrorReporter = reporter;
    }

    internal void ConfigureXmlEntityWork(ParserWorkLimiter workLimiter, IXmlExternalGeneralEntitySource? externalEntitySource)
    {
        Guard.NotNull(workLimiter, "workLimiter");
        WorkLimiter = workLimiter;
        ExternalEntitySource = externalEntitySource;
    }

    internal void ConfigureXmlEntityReplacementScanSource(IXmlEntityReplacementScanSource? source)
    {
        ReplacementScanSource = source;
    }

    internal void ConfigureXmlEntityReferenceObserver(IXmlEntityReferenceObserver? observer)
    {
        ReferenceObserver = observer;
    }

    internal XmlEntityDispatchResult DispatchXmlReference(XmlToken token)
    {
        Guard.NotNull(token, "token");
        if (token.Kind == XmlTokenKind.CharacterReference)
        {
            return DispatchXmlCharacterReference(token);
        }

        if (token.Kind == XmlTokenKind.EntityReference)
        {
            return DispatchXmlEntityReference(token);
        }

        throw new ArgumentException("A character or entity reference token was expected.", "token");
    }

    internal XmlEntityDispatchResult DispatchXmlCharacterReference(XmlToken token)
    {
        string? optionalValue = token.Value;
        if (optionalValue == null)
        {
            throw CreateXmlEntityExpansionError("A character reference requires replacement text", token.StartUtf16Offset);
        }

        ValidateXmlCharacterReferenceReplacement(token.ScalarValue, optionalValue, token.RestrictedCharacter, token.StartUtf16Offset);
        XmlEntityDispatchItem item = new XmlEntityDispatchItem(XmlEntityDispatchKind.Character, null, optionalValue, token.ScalarValue, token.RestrictedCharacter);
        return new XmlEntityDispatchResult([item], true, false, null);
    }

    internal XmlEntityDispatchResult DispatchXmlEntityReference(XmlToken token)
    {
        string? optionalName = token.Name;
        if (optionalName == null || optionalName.Length == 0)
        {
            throw CreateXmlEntityExpansionError("An entity reference requires a name", token.StartUtf16Offset);
        }

        ExpansionSkippedEntity = false;
        List<XmlEntityDispatchItem> items = new List<XmlEntityDispatchItem>();
        bool resolved = ExpandXmlNamedEntity(optionalName, token.StartUtf16Offset, token.Utf16Length, true, items);
        return new XmlEntityDispatchResult(items.ToArray(), resolved && !ExpansionSkippedEntity, ExpansionSkippedEntity, optionalName);
    }

    internal bool ExpandXmlNamedEntity(string name, int offset, int referenceLength, bool documentReference, List<XmlEntityDispatchItem> items)
    {
        if (ActiveEntityNames.Contains(name))
        {
            throw CreateXmlEntityExpansionError("The general entity '" + name + "' is recursive", offset);
        }

        string? predefinedReplacement = FindPredefinedXmlEntityReplacement(name);
        IXmlGeneralEntitySource? optionalSource = EntitySource;
        XmlEntityDeclaration? declaration = predefinedReplacement == null && optionalSource != null ? optionalSource.FindXmlGeneralEntity(name) : null;
        if (predefinedReplacement == null && declaration == null)
        {
            AddUnresolvedXmlEntity(name, offset, items);
            return false;
        }

        IXmlEntityReferenceObserver? optionalObserver = ReferenceObserver;
        if (declaration != null && optionalObserver != null)
        {
            optionalObserver.ObserveXmlEntityReference(name, offset);
        }

        ParserWorkLimiter? optionalLimiter = WorkLimiter;
        if (optionalLimiter != null)
        {
            optionalLimiter.BeginXmlEntityActivation(referenceLength, documentReference);
        }

        ActiveEntityNames.Add(name);
        if (Handling == XmlEntityHandling.PreserveReferences)
        {
            items.Add(new XmlEntityDispatchItem(XmlEntityDispatchKind.StartEntity, name, null, 0, false));
        }

        if (predefinedReplacement != null)
        {
            ConsumeExpandedXmlEntityCharacters(predefinedReplacement.Length);
            items.Add(new XmlEntityDispatchItem(XmlEntityDispatchKind.Character, name, predefinedReplacement, predefinedReplacement[0], false));
        }
        else
        {
            XmlEntityDeclaration requiredDeclaration = declaration == null ? new XmlEntityDeclaration() : declaration;
            ValidateXmlEntityDeclaration(requiredDeclaration, name, offset);
            string? replacementText = requiredDeclaration.ReplacementText;
            if (replacementText == null && requiredDeclaration.IsExternal)
            {
                IXmlExternalGeneralEntitySource? optionalExternalSource = ExternalEntitySource;
                DtdLoadedSource? external = optionalExternalSource == null ? null : optionalExternalSource.LoadXmlExternalGeneralEntity(requiredDeclaration);
                if (external != null)
                {
                    replacementText = external.Text.Substring(external.ContentStartUtf16Offset);
                }
            }

            if (replacementText == null)
            {
                ActiveEntityNames.RemoveAt(ActiveEntityNames.Count - 1);
                if (optionalLimiter != null)
                {
                    optionalLimiter.EndXmlEntityActivation();
                }

                AddUnresolvedXmlEntity(name, offset, items);
                return false;
            }

            IXmlEntityReplacementScanSource? optionalScanSource = ReplacementScanSource;
            string? optionalScanText = optionalScanSource == null ? null : optionalScanSource.FindXmlEntityReplacementScanText(name);
            string scanText = optionalScanText == null ? replacementText : optionalScanText;
            ValidateXmlEntityReplacementText(scanText, offset);
            ExpandXmlEntityReplacementText(scanText, name, offset, items);
        }

        if (Handling == XmlEntityHandling.PreserveReferences)
        {
            items.Add(new XmlEntityDispatchItem(XmlEntityDispatchKind.EndEntity, name, null, 0, false));
        }

        ActiveEntityNames.RemoveAt(ActiveEntityNames.Count - 1);
        if (optionalLimiter != null)
        {
            optionalLimiter.EndXmlEntityActivation();
        }

        return true;
    }

    internal void ValidateXmlEntityDeclaration(XmlEntityDeclaration declaration, string name, int offset)
    {
        if (declaration.IsParameter)
        {
            throw CreateXmlEntityExpansionError("A parameter entity cannot be referenced as a general entity", offset);
        }

        if (!declaration.IsParsed || declaration.NotationName != null)
        {
            throw CreateXmlEntityExpansionError("An unparsed entity cannot be referenced in XML content", offset);
        }

        if (declaration.Name == null || !string.Equals(declaration.Name, name, StringComparison.Ordinal))
        {
            throw CreateXmlEntityExpansionError("The resolved entity declaration does not match the reference name", offset);
        }
    }

    internal void ExpandXmlEntityReplacementText(string replacementText, string entityName, int offset, List<XmlEntityDispatchItem> items)
    {
        XmlScanner scanner = new XmlScanner(replacementText.AsMemory(), Version, XmlEncodingEntityKind.ExternalParsedEntity);
        while (true)
        {
            XmlToken token = scanner.ScanNextXmlToken();
            if (token.Kind == XmlTokenKind.EndOfInput)
            {
                return;
            }

            if (token.Kind == XmlTokenKind.EntityReference)
            {
                ExpandXmlNamedEntity(token.Name == null ? "" : token.Name, offset + token.StartUtf16Offset, token.Utf16Length, false, items);
            }
            else if (token.Kind == XmlTokenKind.CharacterReference)
            {
                string value = token.Value == null ? "" : token.Value;
                ValidateXmlCharacterReferenceReplacement(token.ScalarValue, value, token.RestrictedCharacter, offset + token.StartUtf16Offset);
                ConsumeExpandedXmlEntityCharacters(value.Length);
                items.Add(new XmlEntityDispatchItem(XmlEntityDispatchKind.Character, null, value, token.ScalarValue, token.RestrictedCharacter));
            }
            else
            {
                AppendExpandedXmlEntityText(items, entityName, token.Lexeme);
            }
        }
    }

    internal void AppendExpandedXmlEntityText(List<XmlEntityDispatchItem> items, string entityName, string text)
    {
        if (text.Length == 0)
        {
            return;
        }

        ConsumeExpandedXmlEntityCharacters(text.Length);
        int index = items.Count - 1;
        if (index >= 0 && items[index].Kind == XmlEntityDispatchKind.ReplacementText && items[index].Name == entityName)
        {
            XmlEntityDispatchItem priorItem = items[index];
            string prior = priorItem.Value == null ? "" : priorItem.Value;
            priorItem.Value = prior + text;
            return;
        }

        items.Add(new XmlEntityDispatchItem(XmlEntityDispatchKind.ReplacementText, entityName, text, 0, false));
    }

    internal void ConsumeExpandedXmlEntityCharacters(int amount)
    {
        ParserWorkLimiter? optionalLimiter = WorkLimiter;
        if (optionalLimiter != null)
        {
            optionalLimiter.ConsumeXmlEntityCharacters(amount);
        }
    }

    internal void AddUnresolvedXmlEntity(string name, int offset, List<XmlEntityDispatchItem> items)
    {
        if (!SkipUnresolvedEntities)
        {
            throw CreateXmlEntityExpansionError("The general entity '" + name + "' is unresolved", offset);
        }

        ExpansionSkippedEntity = true;
        items.Add(new XmlEntityDispatchItem(XmlEntityDispatchKind.SkippedEntity, name, null, 0, false));
    }

    internal string? FindPredefinedXmlEntityReplacement(string name)
    {
        if (string.Equals(name, "amp", StringComparison.Ordinal))
        {
            return "&";
        }

        if (string.Equals(name, "lt", StringComparison.Ordinal))
        {
            return "<";
        }

        if (string.Equals(name, "gt", StringComparison.Ordinal))
        {
            return ">";
        }

        if (string.Equals(name, "apos", StringComparison.Ordinal))
        {
            return "'";
        }

        if (string.Equals(name, "quot", StringComparison.Ordinal))
        {
            return "\"";
        }

        return null;
    }

    internal void ValidateXmlCharacterReferenceReplacement(int scalarValue, string replacement, bool restrictedCharacter, int offset)
    {
        bool legal;
        bool expectedRestricted;
        if (Version == XmlVersion.Xml10)
        {
            legal = Xml10CharacterRules.IsLegalXml10Character(scalarValue);
            expectedRestricted = false;
        }
        else
        {
            legal = Xml11CharacterRules.IsLegalXml11Character(scalarValue);
            expectedRestricted = Xml11CharacterRules.IsRestrictedXml11Character(scalarValue);
        }

        if (!legal)
        {
            throw CreateXmlEntityExpansionError("A character reference replacement is not legal for the XML version", offset);
        }

        string expected = char.ConvertFromUtf32(scalarValue);
        if (!string.Equals(expected, replacement, StringComparison.Ordinal) || restrictedCharacter != expectedRestricted)
        {
            throw CreateXmlEntityExpansionError("Character reference replacement metadata is inconsistent", offset);
        }
    }

    internal void ValidateXmlEntityReplacementText(string replacementText, int offset)
    {
        UnicodeScalarScanner scanner = new UnicodeScalarScanner(replacementText.AsMemory());
        while (!scanner.IsUnicodeScalarEnd())
        {
            UnicodeScalar scalar = scanner.ReadUnicodeScalar();
            if (Version == XmlVersion.Xml10 && !Xml10CharacterRules.IsLegalXml10Character(scalar.Value))
            {
                throw CreateXmlEntityExpansionError("Entity replacement text contains an illegal XML 1.0 character", offset + scalar.Utf16Offset);
            }

            if (Version == XmlVersion.Xml11 && !Xml11CharacterRules.IsDirectXml11Character(scalar.Value))
            {
                throw CreateXmlEntityExpansionError("Entity replacement text contains a character that cannot appear directly in XML 1.1", offset + scalar.Utf16Offset);
            }
        }
    }

    internal Exception CreateXmlEntityExpansionError(string message, int offset)
    {
        if (MapWellFormednessErrors)
        {
            string? sourceUri = ErrorSourceTracker == null ? null : ErrorSourceTracker.SourceUri;
            return XmlParserDiagnosticFactory.CreateXmlWellFormednessError(message, offset, sourceUri, ErrorSourceTracker, ErrorReporter);
        }

        return new FormatException(message + " at UTF-16 offset " + offset + ".");
    }
}
