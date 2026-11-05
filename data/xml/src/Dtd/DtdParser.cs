using System;
using System.Collections.Generic;
using System.Text;

namespace xml;

internal class DtdParser : IXmlGeneralEntitySource, IXmlExternalGeneralEntitySource, IXmlEntityReplacementScanSource, IDtdContentModelSource, IDtdAttributeDeclarationSource, IDtdDocumentDeclarationSource
{
    internal XmlVersion Version;
    internal IXmlResourceResolver? Resolver;
    internal XmlResolverPolicy? ResolverPolicy;
    internal Dictionary<string, DtdParameterEntityDefinition> ParameterEntities;
    internal List<string> ActiveParameterEntities;
    internal List<DtdTokenEntry> ExpandedTokens;
    internal Dictionary<string, XmlElementDeclaration> ElementDeclarations;
    internal Dictionary<string, DtdCompiledContentModel> CompiledContentModels;
    internal Dictionary<string, bool> ExternalElementDeclarations;
    internal Dictionary<string, XmlEntityDeclaration> GeneralEntityDeclarations;
    internal Dictionary<string, XmlEntityDeclaration> ParameterEntityDeclarations;
    internal Dictionary<string, XmlNotationDeclaration> NotationDeclarations;
    internal Dictionary<string, XmlAttributeDeclaration> AttributeDeclarations;
    internal Dictionary<string, List<XmlAttributeDeclaration>> AttributeDeclarationsByElement;
    internal Dictionary<string, bool> ExternalAttributeDeclarations;
    internal Dictionary<string, bool> ExternalAttributeDefaultEntityDependencies;
    internal Dictionary<string, bool> ExternalGeneralEntityDeclarations;
    internal List<XmlEntityDeclaration> RetainedEntityDeclarations;
    internal List<XmlNotationDeclaration> RetainedNotationDeclarations;
    internal List<XmlElementDeclaration> RetainedElementDeclarations;
    internal List<XmlAttributeDeclaration> RetainedAttributeDeclarations;
    internal List<XmlEvent> RetainedDeclarationEvents;
    internal List<string> DuplicateElementDeclarationNames;
    internal List<string> DuplicateNotationDeclarationNames;
    internal List<string> DeclarationValidityErrors;
    internal ParserWorkLimiter? WorkLimiter;
    internal bool Loaded;
    internal XmlVersion DocumentEntityVersion;
    internal bool HasExternalSubsetIdentifier;
    internal bool SawParameterEntityReference;
    internal Dictionary<string, string> EntityReplacementScanTexts;

    internal DtdParser(XmlVersion version, IXmlResourceResolver? resolver, XmlResolverPolicy? resolverPolicy)
    {
        if (version != XmlVersion.Xml10 && version != XmlVersion.Xml11)
        {
            throw new ArgumentOutOfRangeException("version");
        }

        Version = version;
        XmlCatalogEntry[]? catalogEntries = resolverPolicy == null ? null : resolverPolicy.CatalogEntries;
        Resolver = resolver == null || resolverPolicy == null || catalogEntries == null || catalogEntries.Length == 0 ? resolver : new ResourceManager(resolver, resolverPolicy);
        ResolverPolicy = resolverPolicy;
        ParameterEntities = new Dictionary<string, DtdParameterEntityDefinition>(StringComparer.Ordinal);
        ActiveParameterEntities = new List<string>();
        ExpandedTokens = new List<DtdTokenEntry>();
        ElementDeclarations = new Dictionary<string, XmlElementDeclaration>(StringComparer.Ordinal);
        CompiledContentModels = new Dictionary<string, DtdCompiledContentModel>(StringComparer.Ordinal);
        ExternalElementDeclarations = new Dictionary<string, bool>(StringComparer.Ordinal);
        GeneralEntityDeclarations = new Dictionary<string, XmlEntityDeclaration>(StringComparer.Ordinal);
        ParameterEntityDeclarations = new Dictionary<string, XmlEntityDeclaration>(StringComparer.Ordinal);
        NotationDeclarations = new Dictionary<string, XmlNotationDeclaration>(StringComparer.Ordinal);
        AttributeDeclarations = new Dictionary<string, XmlAttributeDeclaration>(StringComparer.Ordinal);
        AttributeDeclarationsByElement = new Dictionary<string, List<XmlAttributeDeclaration>>(StringComparer.Ordinal);
        ExternalAttributeDeclarations = new Dictionary<string, bool>(StringComparer.Ordinal);
        ExternalAttributeDefaultEntityDependencies = new Dictionary<string, bool>(StringComparer.Ordinal);
        ExternalGeneralEntityDeclarations = new Dictionary<string, bool>(StringComparer.Ordinal);
        RetainedEntityDeclarations = new List<XmlEntityDeclaration>();
        RetainedNotationDeclarations = new List<XmlNotationDeclaration>();
        RetainedElementDeclarations = new List<XmlElementDeclaration>();
        RetainedAttributeDeclarations = new List<XmlAttributeDeclaration>();
        RetainedDeclarationEvents = new List<XmlEvent>();
        DuplicateElementDeclarationNames = new List<string>();
        DuplicateNotationDeclarationNames = new List<string>();
        DeclarationValidityErrors = new List<string>();
        WorkLimiter = null;
        Loaded = false;
        DocumentEntityVersion = version;
        HasExternalSubsetIdentifier = false;
        SawParameterEntityReference = false;
        EntityReplacementScanTexts = new Dictionary<string, string>(StringComparer.Ordinal);
    }

    internal void RegisterInternalDtdParameterEntity(string name, string replacementText, string? baseUri)
    {
        Guard.NotNull(replacementText, "replacementText");
        ValidateDtdParameterEntityName(name);
        if (!ParameterEntities.ContainsKey(name))
        {
            ParameterEntities.Add(name, new DtdParameterEntityDefinition(name, replacementText, null, null, baseUri, false));
        }
    }

    internal void ConfigureDtdParserWork(ParserWorkLimiter workLimiter)
    {
        Guard.NotNull(workLimiter, "workLimiter");
        WorkLimiter = workLimiter;
    }

    internal void ConfigureDtdDocumentEntityVersion(XmlVersion version)
    {
        DocumentEntityVersion = version;
    }

    internal void RegisterExternalDtdParameterEntity(string name, string? publicIdentifier, string systemIdentifier, string? baseUri)
    {
        Guard.NotNull(systemIdentifier, "systemIdentifier");
        ValidateDtdParameterEntityName(name);
        if (!ParameterEntities.ContainsKey(name))
        {
            ParameterEntities.Add(name, new DtdParameterEntityDefinition(name, null, publicIdentifier, systemIdentifier, baseUri, true));
        }
    }

    internal DtdSubset LoadDtdSubsets(string? internalSubset, string? publicIdentifier, string? systemIdentifier, string? baseUri)
    {
        if (Loaded)
        {
            throw new InvalidOperationException("The DTD subsets have already been loaded.");
        }

        Loaded = true;
        HasExternalSubsetIdentifier = systemIdentifier != null;
        bool hasInternalSubset = internalSubset != null;
        if (internalSubset != null)
        {
            ProcessDtdSource(internalSubset, 0, DtdSourceKind.InternalSubset, baseUri, null, 0, false);
        }

        bool externalSubsetLoaded = false;
        string? externalSubsetUri = null;
        XmlDeclarationInfo? externalTextDeclaration = null;
        if (systemIdentifier != null)
        {
            DtdLoadedSource? external = ResolveDtdSource(XmlResourceKind.ExternalSubset, publicIdentifier, systemIdentifier, baseUri);
            if (external != null)
            {
                externalSubsetLoaded = true;
                externalSubsetUri = external.SourceUri;
                externalTextDeclaration = external.TextDeclaration.HasDeclaration ? external.TextDeclaration : null;
                ProcessDtdSource(external.Text, external.ContentStartUtf16Offset, DtdSourceKind.ExternalSubset, external.SourceUri, null, 0, false);
            }
        }

        NormalizeRetainedDtdAttributeDefaults();

        return new DtdSubset(ExpandedTokens.ToArray(), RetainedElementDeclarations.ToArray(), RetainedAttributeDeclarations.ToArray(), RetainedEntityDeclarations.ToArray(), RetainedNotationDeclarations.ToArray(), RetainedDeclarationEvents.ToArray(), hasInternalSubset, externalSubsetLoaded, externalSubsetUri, externalTextDeclaration);
    }

    internal void ProcessDtdSource(string text, int contentStart, DtdSourceKind sourceKind, string? sourceUri, string? parameterEntityName, int entityDepth, bool declarationContext)
    {
        ParserWorkLimiter? optionalLimiter = WorkLimiter;
        if (optionalLimiter != null)
        {
            optionalLimiter.ObserveXmlDtdCharacters(text.Length - contentStart);
        }

        DtdScanner scanner = new DtdScanner(text.AsMemory(), Version);
        scanner.ResetDtdScannerAt(contentStart);
        ProcessDtdTokenStream(scanner, sourceKind, sourceUri, parameterEntityName, entityDepth, declarationContext, false);
    }

    internal void ProcessDtdTokenStream(DtdScanner scanner, DtdSourceKind sourceKind, string? sourceUri, string? parameterEntityName, int entityDepth, bool declarationContext, bool conditionalBody)
    {
        int declarationBalance = 0;
        int declarationStart = -1;
        string? declarationName = null;
        while (true)
        {
            DtdToken token = scanner.ScanNextDtdToken();
            if (token.Kind == DtdTokenKind.EndOfInput)
            {
                if (conditionalBody)
                {
                    throw CreateDtdError("A conditional section is not terminated", token.StartUtf16Offset);
                }

                if (declarationBalance != 0)
                {
                    throw CreateDtdError("A markup declaration must begin and end in the same parameter entity replacement text", token.StartUtf16Offset);
                }

                return;
            }

            if (token.Kind == DtdTokenKind.ConditionalSectionClose)
            {
                if (!conditionalBody)
                {
                    throw CreateDtdError("A conditional section closing delimiter has no matching opening delimiter", token.StartUtf16Offset);
                }

                if (declarationBalance != 0)
                {
                    throw CreateDtdError("A markup declaration cannot cross a conditional section boundary", token.StartUtf16Offset);
                }

                return;
            }

            if (token.Kind == DtdTokenKind.ConditionalSectionOpen)
            {
                if (sourceKind != DtdSourceKind.ExternalSubset && sourceKind != DtdSourceKind.ExternalParameterEntity)
                {
                    throw CreateDtdError("Conditional sections are permitted only in an external subset or external parameter entity", token.StartUtf16Offset);
                }

                if (declarationBalance != 0 || declarationContext)
                {
                    throw CreateDtdError("A conditional section cannot occur within a markup declaration", token.StartUtf16Offset);
                }

                ProcessDtdConditionalSection(scanner, sourceKind, sourceUri, parameterEntityName, entityDepth);
                continue;
            }

            if (token.Kind == DtdTokenKind.ParameterEntityReference)
            {
                SawParameterEntityReference = true;
                if (sourceKind == DtdSourceKind.InternalSubset && declarationBalance > 0)
                {
                    throw CreateDtdError("A parameter entity reference cannot occur within a markup declaration in the internal subset", token.StartUtf16Offset);
                }

                ExpandDtdParameterEntity(token, sourceUri, entityDepth, declarationBalance > 0 || declarationContext);
                continue;
            }

            if (token.Kind == DtdTokenKind.MarkupDeclarationOpen)
            {
                declarationBalance = declarationBalance + 1;
                if (declarationBalance == 1)
                {
                    declarationStart = ExpandedTokens.Count;
                    declarationName = null;
                }
            }
            else if (token.Kind == DtdTokenKind.DeclarationClose)
            {
                if (declarationBalance == 0)
                {
                    throw CreateDtdError("A markup declaration closing delimiter begins outside its physical entity", token.StartUtf16Offset);
                }

                declarationBalance = declarationBalance - 1;
            }
            else if (token.Kind == DtdTokenKind.ProcessingInstruction && token.Name != null && string.Equals(token.Name, "xml", StringComparison.OrdinalIgnoreCase))
            {
                throw CreateDtdError("A text declaration may appear only at the beginning of an external parsed entity", token.StartUtf16Offset);
            }
            else if (declarationBalance > 0 && declarationName == null && token.Kind == DtdTokenKind.Name)
            {
                declarationName = token.Name;
            }

            if (sourceKind == DtdSourceKind.InternalSubset && declarationBalance > 0 && declarationName == "ENTITY" && token.Kind == DtdTokenKind.QuotedLiteral && token.Value != null)
            {
                int parameterMarker = token.Value.IndexOf('%');
                if (parameterMarker >= 0)
                {
                    throw CreateDtdError("A parameter entity reference cannot occur within an entity declaration in the internal subset", token.StartUtf16Offset + 1 + parameterMarker);
                }
            }

            if (!declarationContext && declarationBalance == 0 && token.Kind != DtdTokenKind.Whitespace && token.Kind != DtdTokenKind.Comment && token.Kind != DtdTokenKind.ProcessingInstruction && token.Kind != DtdTokenKind.DeclarationClose && token.Kind != DtdTokenKind.ConditionalSectionOpen && token.Kind != DtdTokenKind.ConditionalSectionClose)
            {
                if (token.Kind != DtdTokenKind.MarkupDeclarationOpen)
                {
                    throw CreateDtdError("A DTD subset must contain complete markup declarations separated by whitespace or parameter entities", token.StartUtf16Offset);
                }
            }

            ExpandedTokens.Add(new DtdTokenEntry(token, sourceKind, sourceUri, parameterEntityName, entityDepth, false));
            if (token.Kind == DtdTokenKind.DeclarationClose && declarationBalance == 0)
            {
                ParseRetainedDtdDeclaration(declarationStart, ExpandedTokens.Count);
                declarationStart = -1;
                declarationName = null;
            }
        }
    }

    internal void ProcessDtdConditionalSection(DtdScanner scanner, DtdSourceKind sourceKind, string? sourceUri, string? parameterEntityName, int entityDepth)
    {
        DtdToken token = scanner.ScanNextDtdToken();
        if (token.Kind == DtdTokenKind.Whitespace)
        {
            token = scanner.ScanNextDtdToken();
        }

        DtdConditionalKind kind;
        if (token.Kind == DtdTokenKind.Name && token.Name == "INCLUDE")
        {
            kind = DtdConditionalKind.Include;
        }
        else if (token.Kind == DtdTokenKind.Name && token.Name == "IGNORE")
        {
            kind = DtdConditionalKind.Ignore;
        }
        else if (token.Kind == DtdTokenKind.ParameterEntityReference)
        {
            kind = ResolveDtdConditionalKeyword(token, sourceUri);
        }
        else
        {
            throw CreateDtdError("A conditional section requires an INCLUDE or IGNORE keyword", token.StartUtf16Offset);
        }

        token = scanner.ScanNextDtdToken();
        if (token.Kind == DtdTokenKind.Whitespace)
        {
            token = scanner.ScanNextDtdToken();
        }

        if (token.Kind != DtdTokenKind.Symbol || token.Value != "[")
        {
            throw CreateDtdError("A conditional section keyword must be followed by '['", token.StartUtf16Offset);
        }

        if (kind == DtdConditionalKind.Ignore)
        {
            scanner.SkipDtdIgnoredSection();
            return;
        }

        ProcessDtdTokenStream(scanner, sourceKind, sourceUri, parameterEntityName, entityDepth, false, true);
    }

    internal DtdConditionalKind ResolveDtdConditionalKeyword(DtdToken reference, string? referencingSourceUri)
    {
        string name = reference.Name == null ? "" : reference.Name;
        if (!ParameterEntities.ContainsKey(name))
        {
            throw CreateDtdError("The parameter entity '" + name + "' is not declared", reference.StartUtf16Offset);
        }

        if (ActiveParameterEntities.Contains(name))
        {
            throw CreateDtdError("The parameter entity '" + name + "' is recursive", reference.StartUtf16Offset);
        }

        DtdParameterEntityDefinition definition = ParameterEntities[name];
        ParserWorkLimiter? optionalLimiter = WorkLimiter;
        if (optionalLimiter != null)
        {
            optionalLimiter.BeginXmlEntityActivation(reference.Utf16Length, ActiveParameterEntities.Count == 0);
        }

        ActiveParameterEntities.Add(name);
        string replacementText;
        int contentStart = 0;
        string? sourceUri = definition.BaseUri;
        if (definition.IsExternal)
        {
            string systemIdentifier = definition.SystemIdentifier == null ? "" : definition.SystemIdentifier;
            string? entityBaseUri = definition.BaseUri == null ? referencingSourceUri : definition.BaseUri;
            DtdLoadedSource? external = ResolveDtdSource(XmlResourceKind.ParameterEntity, definition.PublicIdentifier, systemIdentifier, entityBaseUri);
            if (external == null)
            {
                throw CreateDtdError("The external parameter entity '" + name + "' could not be loaded", reference.StartUtf16Offset);
            }

            replacementText = external.Text;
            contentStart = external.ContentStartUtf16Offset;
            sourceUri = external.SourceUri;
        }
        else
        {
            replacementText = definition.ReplacementText == null ? "" : definition.ReplacementText;
        }

        DtdConditionalKind kind = ParseDtdConditionalKeywordText(replacementText, contentStart, sourceUri);
        ActiveParameterEntities.RemoveAt(ActiveParameterEntities.Count - 1);
        if (optionalLimiter != null)
        {
            optionalLimiter.ConsumeXmlEntityCharacters(replacementText.Length - contentStart);
            optionalLimiter.EndXmlEntityActivation();
        }

        return kind;
    }

    internal DtdConditionalKind ParseDtdConditionalKeywordText(string text, int contentStart, string? sourceUri)
    {
        DtdScanner scanner = new DtdScanner(text.AsMemory(), Version);
        scanner.ResetDtdScannerAt(contentStart);
        DtdConditionalKind kind = DtdConditionalKind.Include;
        bool found = false;
        while (true)
        {
            DtdToken token = scanner.ScanNextDtdToken();
            if (token.Kind == DtdTokenKind.EndOfInput)
            {
                if (!found)
                {
                    throw CreateDtdError("A conditional section keyword parameter entity is empty", contentStart);
                }

                return kind;
            }

            if (token.Kind == DtdTokenKind.Whitespace)
            {
                continue;
            }

            if (token.Kind == DtdTokenKind.ParameterEntityReference && !found)
            {
                kind = ResolveDtdConditionalKeyword(token, sourceUri);
                found = true;
                continue;
            }

            if (token.Kind == DtdTokenKind.Name && !found && token.Name == "INCLUDE")
            {
                kind = DtdConditionalKind.Include;
                found = true;
                continue;
            }

            if (token.Kind == DtdTokenKind.Name && !found && token.Name == "IGNORE")
            {
                kind = DtdConditionalKind.Ignore;
                found = true;
                continue;
            }

            if (found && token.Kind == DtdTokenKind.Symbol && token.Value == "[")
            {
                throw CreateDtdError("A conditional section opening delimiter must not be completed inside parameter entity replacement text", token.StartUtf16Offset);
            }

            throw CreateDtdError("A conditional section keyword parameter entity must expand to only INCLUDE or IGNORE", token.StartUtf16Offset);
        }
    }

    internal void ExpandDtdParameterEntity(DtdToken reference, string? referencingSourceUri, int entityDepth, bool declarationContext)
    {
        string name = reference.Name == null ? "" : reference.Name;
        if (!ParameterEntities.ContainsKey(name))
        {
            throw CreateDtdError("The parameter entity '" + name + "' is not declared", reference.StartUtf16Offset);
        }

        if (ActiveParameterEntities.Contains(name))
        {
            throw CreateDtdError("The parameter entity '" + name + "' is recursive", reference.StartUtf16Offset);
        }

        DtdParameterEntityDefinition definition = ParameterEntities[name];
        ParserWorkLimiter? optionalLimiter = WorkLimiter;
        if (optionalLimiter != null)
        {
            optionalLimiter.BeginXmlEntityActivation(reference.Utf16Length, entityDepth == 0);
        }

        ActiveParameterEntities.Add(name);
        AddSyntheticDtdSpace(reference, definition, entityDepth + 1);
        int expansionCharacterCount;
        if (definition.IsExternal)
        {
            string systemIdentifier = definition.SystemIdentifier == null ? "" : definition.SystemIdentifier;
            string? entityBaseUri = definition.BaseUri == null ? referencingSourceUri : definition.BaseUri;
            DtdLoadedSource? external = ResolveDtdSource(XmlResourceKind.ParameterEntity, definition.PublicIdentifier, systemIdentifier, entityBaseUri);
            if (external == null)
            {
                throw CreateDtdError("The external parameter entity '" + name + "' could not be loaded", reference.StartUtf16Offset);
            }

            expansionCharacterCount = external.Text.Length - external.ContentStartUtf16Offset;
            ProcessDtdSource(external.Text, external.ContentStartUtf16Offset, DtdSourceKind.ExternalParameterEntity, external.SourceUri, name, entityDepth + 1, declarationContext);
        }
        else
        {
            string replacementText = definition.ReplacementText == null ? "" : definition.ReplacementText;
            expansionCharacterCount = replacementText.Length;
            ProcessDtdSource(replacementText, 0, DtdSourceKind.InternalParameterEntity, definition.BaseUri, name, entityDepth + 1, declarationContext);
        }

        AddSyntheticDtdSpace(reference, definition, entityDepth + 1);
        ActiveParameterEntities.RemoveAt(ActiveParameterEntities.Count - 1);
        if (optionalLimiter != null)
        {
            optionalLimiter.ConsumeXmlEntityCharacters(expansionCharacterCount);
            optionalLimiter.EndXmlEntityActivation();
        }
    }

    internal void AddSyntheticDtdSpace(DtdToken reference, DtdParameterEntityDefinition definition, int entityDepth)
    {
        DtdSourceKind sourceKind = definition.IsExternal ? DtdSourceKind.ExternalParameterEntity : DtdSourceKind.InternalParameterEntity;
        DtdToken space = new DtdToken(DtdTokenKind.Whitespace, null, " ", " ", reference.StartUtf16Offset, 1, '\0', false);
        ExpandedTokens.Add(new DtdTokenEntry(space, sourceKind, definition.BaseUri, definition.Name, entityDepth, true));
    }

    internal void ParseRetainedDtdDeclaration(int start, int end)
    {
        if (start < 0 || end - start < 3)
        {
            return;
        }

        DtdDeclarationReader reader = new DtdDeclarationReader(ExpandedTokens, start + 1, end - 1);
        DtdTokenEntry? optionalKeyword = reader.ReadDtdDeclarationEntry();
        if (optionalKeyword == null || optionalKeyword.Token.Kind != DtdTokenKind.Name)
        {
            throw CreateDtdError("A DTD markup declaration requires a declaration name immediately after '<!'", ExpandedTokens[start].Token.StartUtf16Offset);
        }

        string? keyword = optionalKeyword.Token.Name;
        string? declarationBaseUri = ExpandedTokens[start].SourceUri;
        bool declarationExternal = ExpandedTokens[start].SourceKind != DtdSourceKind.InternalSubset;
        if (keyword == "ENTITY")
        {
            ParseDtdEntityDeclaration(reader, declarationBaseUri, optionalKeyword.Token.StartUtf16Offset, declarationExternal);
        }
        else if (keyword == "ELEMENT")
        {
            ValidateDtdElementParameterEntityNesting(start, end);
            DtdElementDeclarationParser elementParser = new DtdElementDeclarationParser(reader, optionalKeyword.Token.StartUtf16Offset);
            XmlElementDeclaration declaration = elementParser.ParseDtdElementDeclaration();
            string name = declaration.Name == null || declaration.Name.QualifiedName == null ? "" : declaration.Name.QualifiedName;
            if (!ElementDeclarations.ContainsKey(name))
            {
                ElementDeclarations.Add(name, declaration);
                ExternalElementDeclarations.Add(name, declarationExternal);
                RetainedElementDeclarations.Add(declaration);
                XmlEvent item = new XmlEvent();
                item.Kind = XmlEventKind.ElementDeclaration;
                item.NodeKind = XmlNodeKind.DocumentType;
                item.Name = declaration.Name;
                item.Value = declaration.ContentModel;
                item.Range = declaration.Range;
                item.ElementDeclaration = declaration;
                RetainedDeclarationEvents.Add(item);
                if (elementParser.CompiledModel != null)
                {
                    CompiledContentModels.Add(name, elementParser.CompiledModel);
                }
            }
            else
            {
                DuplicateElementDeclarationNames.Add(name);
            }
        }
        else if (keyword == "NOTATION")
        {
            ParseDtdNotationDeclaration(reader, declarationBaseUri, optionalKeyword.Token.StartUtf16Offset);
        }
        else if (keyword == "ATTLIST")
        {
            DtdAttributeListDeclarationParser attributeParser = new DtdAttributeListDeclarationParser(reader, Version, optionalKeyword.Token.StartUtf16Offset);
            XmlAttributeDeclaration[] declarations = attributeParser.ParseDtdAttributeListDeclaration();
            int index = 0;
            while (index < declarations.Length)
            {
                RetainDtdAttributeDeclaration(declarations[index], declarationExternal);
                index = index + 1;
            }
        }
        else
        {
            throw CreateDtdError("The DTD markup declaration name is not recognized", optionalKeyword.Token.StartUtf16Offset);
        }
    }

    internal void ValidateDtdElementParameterEntityNesting(int start, int end)
    {
        List<DtdTokenEntry> openings = new List<DtdTokenEntry>();
        int index = start;
        while (index < end)
        {
            DtdTokenEntry entry = ExpandedTokens[index];
            if (entry.Token.Kind == DtdTokenKind.Symbol && entry.Token.Value == "(")
            {
                openings.Add(entry);
            }
            else if (entry.Token.Kind == DtdTokenKind.Symbol && entry.Token.Value == ")" && openings.Count > 0)
            {
                int openingIndex = openings.Count - 1;
                DtdTokenEntry opening = openings[openingIndex];
                openings.RemoveAt(openingIndex);
                if (opening.SourceKind != entry.SourceKind || opening.EntityDepth != entry.EntityDepth || opening.ParameterEntityName != entry.ParameterEntityName || opening.SourceUri != entry.SourceUri)
                {
                    DeclarationValidityErrors.Add("An element content group must begin and end in the same parameter entity replacement text");
                }
            }

            index = index + 1;
        }
    }

    internal void RetainDtdAttributeDeclaration(XmlAttributeDeclaration declaration, bool declaredExternally)
    {
        ValidateDtdAttributeDefaultEntityDeclarations(declaration);
        string elementName = declaration.ElementName == null || declaration.ElementName.QualifiedName == null ? "" : declaration.ElementName.QualifiedName;
        string attributeName = declaration.Name == null || declaration.Name.QualifiedName == null ? "" : declaration.Name.QualifiedName;
        string key = elementName + "\0" + attributeName;
        if (AttributeDeclarations.ContainsKey(key))
        {
            return;
        }

        AttributeDeclarations.Add(key, declaration);
        ExternalAttributeDeclarations.Add(key, declaredExternally);
        RetainedAttributeDeclarations.Add(declaration);
        XmlEvent item = new XmlEvent();
        item.Kind = XmlEventKind.AttributeDeclaration;
        item.NodeKind = XmlNodeKind.DocumentType;
        item.Name = declaration.Name;
        item.Value = declaration.DefaultValue;
        item.Range = declaration.Range;
        item.AttributeDeclaration = declaration;
        RetainedDeclarationEvents.Add(item);
        if (!AttributeDeclarationsByElement.ContainsKey(elementName))
        {
            AttributeDeclarationsByElement.Add(elementName, new List<XmlAttributeDeclaration>());
        }

        AttributeDeclarationsByElement[elementName].Add(declaration);
    }

    internal void ValidateDtdAttributeDefaultEntityDeclarations(XmlAttributeDeclaration declaration)
    {
        string? optionalValue = declaration.DefaultValue;
        if (optionalValue == null || HasExternalSubsetIdentifier || SawParameterEntityReference)
        {
            return;
        }

        string value = optionalValue;
        int position = 0;
        while (position < value.Length)
        {
            int referenceStart = value.IndexOf('&', position);
            if (referenceStart < 0)
            {
                return;
            }

            int referenceEnd = value.IndexOf(';', referenceStart + 1);
            if (referenceEnd < 0)
            {
                return;
            }

            if (referenceStart + 1 < value.Length && value[referenceStart + 1] != '#')
            {
                string name = value.Substring(referenceStart + 1, referenceEnd - referenceStart - 1);
                bool predefined = name == "amp" || name == "lt" || name == "gt" || name == "apos" || name == "quot";
                if (!predefined && !GeneralEntityDeclarations.ContainsKey(name))
                {
                    throw CreateDtdError("The general entity '" + name + "' must be declared before it is referenced in an attribute default", referenceStart);
                }
            }

            position = referenceEnd + 1;
        }
    }

    internal void ParseDtdEntityDeclaration(DtdDeclarationReader reader, string? declarationBaseUri, int declarationOffset, bool declaredExternally)
    {
        RequireDtdDeclarationWhitespace(reader, declarationOffset);
        bool parameter = false;
        DtdTokenEntry? optionalEntry = reader.PeekDtdDeclarationEntry();
        if (optionalEntry != null && optionalEntry.Token.Kind == DtdTokenKind.Symbol && optionalEntry.Token.Value == "%")
        {
            parameter = true;
            reader.ReadDtdDeclarationEntry();
            RequireDtdDeclarationWhitespace(reader, optionalEntry.Token.StartUtf16Offset);
        }

        string name = ReadRequiredDtdName(reader, "An entity declaration requires an XML Name", declarationOffset);
        RequireDtdDeclarationWhitespace(reader, declarationOffset);
        DtdTokenEntry definition = ReadRequiredDtdDeclarationEntry(reader, "An entity declaration requires an entity definition", declarationOffset);
        string? replacementText = null;
        string? publicIdentifier = null;
        string? systemIdentifier = null;
        string? notationName = null;
        bool external = false;
        bool parsed = true;
        if (IsDtdLiteralToken(definition.Token))
        {
            replacementText = ConstructDtdEntityReplacementText(definition.Token.Value == null ? "" : definition.Token.Value, declarationBaseUri, definition.Token.StartUtf16Offset + 1);
        }
        else if (definition.Token.Kind == DtdTokenKind.Name && definition.Token.Name == "SYSTEM")
        {
            external = true;
            RequireDtdDeclarationWhitespace(reader, definition.Token.StartUtf16Offset);
            DtdTokenEntry systemLiteral = ReadRequiredDtdLiteral(reader, "A SYSTEM entity identifier requires a system literal", definition.Token.StartUtf16Offset);
            systemIdentifier = systemLiteral.Token.Value;
            ValidateDtdSystemIdentifier(systemIdentifier == null ? "" : systemIdentifier, systemLiteral.Token.StartUtf16Offset);
        }
        else if (definition.Token.Kind == DtdTokenKind.Name && definition.Token.Name == "PUBLIC")
        {
            external = true;
            RequireDtdDeclarationWhitespace(reader, definition.Token.StartUtf16Offset);
            DtdTokenEntry publicLiteral = ReadRequiredDtdLiteral(reader, "A PUBLIC entity identifier requires a public identifier literal", definition.Token.StartUtf16Offset);
            publicIdentifier = publicLiteral.Token.Value;
            ValidateDtdPublicIdentifier(publicIdentifier == null ? "" : publicIdentifier, publicLiteral.Token.StartUtf16Offset);
            RequireDtdDeclarationWhitespace(reader, publicLiteral.Token.StartUtf16Offset);
            DtdTokenEntry systemLiteral = ReadRequiredDtdLiteral(reader, "A PUBLIC entity identifier requires a system literal", publicLiteral.Token.StartUtf16Offset);
            systemIdentifier = systemLiteral.Token.Value;
            ValidateDtdSystemIdentifier(systemIdentifier == null ? "" : systemIdentifier, systemLiteral.Token.StartUtf16Offset);
        }
        else
        {
            throw CreateDtdError("An entity declaration requires an entity value or external identifier", definition.Token.StartUtf16Offset);
        }

        bool hadTrailingWhitespace = reader.ConsumeDtdDeclarationWhitespace();
        optionalEntry = reader.PeekDtdDeclarationEntry();
        if (optionalEntry != null && optionalEntry.Token.Kind == DtdTokenKind.Name && optionalEntry.Token.Name == "NDATA")
        {
            if (parameter || !external)
            {
                throw CreateDtdError("NDATA is permitted only on an external general entity", optionalEntry.Token.StartUtf16Offset);
            }

            if (!hadTrailingWhitespace)
            {
                throw CreateDtdError("Whitespace is required before NDATA", optionalEntry.Token.StartUtf16Offset);
            }

            reader.ReadDtdDeclarationEntry();
            RequireDtdDeclarationWhitespace(reader, optionalEntry.Token.StartUtf16Offset);
            notationName = ReadRequiredDtdName(reader, "NDATA requires a notation name", optionalEntry.Token.StartUtf16Offset);
            parsed = false;
            reader.ConsumeDtdDeclarationWhitespace();
        }

        EnsureDtdDeclarationComplete(reader, declarationOffset);
        XmlEntityDeclaration declaration = new XmlEntityDeclaration();
        declaration.Name = name;
        declaration.ReplacementText = replacementText;
        declaration.PublicIdentifier = publicIdentifier;
        declaration.SystemIdentifier = systemIdentifier;
        declaration.NotationName = notationName;
        declaration.BaseUri = declarationBaseUri;
        declaration.IsParameter = parameter;
        declaration.IsExternal = external;
        declaration.IsParsed = parsed;
        declaration.Range = null;
        if (!parameter && !external && replacementText != null)
        {
            string scanText = EscapeRestrictedXml11EntityReplacement(replacementText);
            if (scanText != replacementText)
            {
                EntityReplacementScanTexts[name] = scanText;
            }
        }

        RetainDtdEntityDeclaration(declaration, declaredExternally);
    }

    internal string EscapeRestrictedXml11EntityReplacement(string replacementText)
    {
        if (Version != XmlVersion.Xml11)
        {
            return replacementText;
        }

        UnicodeScalarScanner scanner = new UnicodeScalarScanner(replacementText.AsMemory());
        StringBuilder result = new StringBuilder();
        while (!scanner.IsUnicodeScalarEnd())
        {
            UnicodeScalar scalar = scanner.ReadUnicodeScalar();
            if (Xml11CharacterRules.IsRestrictedXml11Character(scalar.Value))
            {
                result.Append("&#x");
                result.Append(scalar.Value.ToString("X"));
                result.Append(';');
            }
            else
            {
                result.Append(char.ConvertFromUtf32(scalar.Value));
            }
        }

        return result.ToString();
    }

    internal void RetainDtdEntityDeclaration(XmlEntityDeclaration declaration, bool declaredExternally)
    {
        string name = declaration.Name == null ? "" : declaration.Name;
        Dictionary<string, XmlEntityDeclaration> declarations = declaration.IsParameter ? ParameterEntityDeclarations : GeneralEntityDeclarations;
        if (declarations.ContainsKey(name))
        {
            return;
        }

        declarations.Add(name, declaration);
        if (!declaration.IsParameter)
        {
            ExternalGeneralEntityDeclarations.Add(name, declaredExternally);
        }

        RetainedEntityDeclarations.Add(declaration);
        XmlEvent item = new XmlEvent();
        item.Kind = XmlEventKind.EntityDeclaration;
        item.NodeKind = XmlNodeKind.DocumentType;
        item.Name = CreateDtdEventName(name);
        item.Value = declaration.ReplacementText;
        item.PublicIdentifier = declaration.PublicIdentifier;
        item.SystemIdentifier = declaration.SystemIdentifier;
        item.Range = declaration.Range;
        item.EntityDeclaration = declaration;
        RetainedDeclarationEvents.Add(item);
        if (declaration.IsParameter)
        {
            if (declaration.IsExternal)
            {
                RegisterExternalDtdParameterEntity(name, declaration.PublicIdentifier, declaration.SystemIdentifier == null ? "" : declaration.SystemIdentifier, declaration.BaseUri);
            }
            else
            {
                RegisterInternalDtdParameterEntity(name, declaration.ReplacementText == null ? "" : declaration.ReplacementText, declaration.BaseUri);
            }
        }
    }

    internal void NormalizeRetainedDtdAttributeDefaults()
    {
        XmlAttributeValueNormalizer normalizer = new XmlAttributeValueNormalizer(Version);
        normalizer.ConfigureXmlAttributeEntities(this, WorkLimiter, null, null);
        DtdAttributeDefaultEntityObserver observer = new DtdAttributeDefaultEntityObserver(this);
        normalizer.ConfigureXmlAttributeEntityObserver(observer);
        int index = 0;
        while (index < RetainedAttributeDeclarations.Count)
        {
            XmlAttributeDeclaration declaration = RetainedAttributeDeclarations[index];
            if (declaration.DefaultValue != null)
            {
                string elementName = declaration.ElementName == null || declaration.ElementName.QualifiedName == null ? "" : declaration.ElementName.QualifiedName;
                string attributeName = declaration.Name == null || declaration.Name.QualifiedName == null ? "" : declaration.Name.QualifiedName;
                observer.AttributeKey = elementName + "\0" + attributeName;
                declaration.DefaultValue = normalizer.NormalizeDtdAttributeValue(declaration.DefaultValue, declaration.TypeName == null ? "CDATA" : declaration.TypeName);
            }

            index = index + 1;
        }
    }

    internal void ParseDtdNotationDeclaration(DtdDeclarationReader reader, string? declarationBaseUri, int declarationOffset)
    {
        RequireDtdDeclarationWhitespace(reader, declarationOffset);
        string name = ReadRequiredDtdName(reader, "A notation declaration requires an XML Name", declarationOffset);
        RequireDtdDeclarationWhitespace(reader, declarationOffset);
        DtdTokenEntry identifier = ReadRequiredDtdDeclarationEntry(reader, "A notation declaration requires an external or public identifier", declarationOffset);
        string? publicIdentifier = null;
        string? systemIdentifier = null;
        if (identifier.Token.Kind == DtdTokenKind.Name && identifier.Token.Name == "SYSTEM")
        {
            RequireDtdDeclarationWhitespace(reader, identifier.Token.StartUtf16Offset);
            DtdTokenEntry systemLiteral = ReadRequiredDtdLiteral(reader, "A SYSTEM notation identifier requires a system literal", identifier.Token.StartUtf16Offset);
            systemIdentifier = systemLiteral.Token.Value;
            ValidateDtdSystemIdentifier(systemIdentifier == null ? "" : systemIdentifier, systemLiteral.Token.StartUtf16Offset);
            reader.ConsumeDtdDeclarationWhitespace();
        }
        else if (identifier.Token.Kind == DtdTokenKind.Name && identifier.Token.Name == "PUBLIC")
        {
            RequireDtdDeclarationWhitespace(reader, identifier.Token.StartUtf16Offset);
            DtdTokenEntry publicLiteral = ReadRequiredDtdLiteral(reader, "A PUBLIC notation identifier requires a public identifier literal", identifier.Token.StartUtf16Offset);
            publicIdentifier = publicLiteral.Token.Value;
            ValidateDtdPublicIdentifier(publicIdentifier == null ? "" : publicIdentifier, publicLiteral.Token.StartUtf16Offset);
            bool separated = reader.ConsumeDtdDeclarationWhitespace();
            DtdTokenEntry? optionalSystem = reader.PeekDtdDeclarationEntry();
            if (optionalSystem != null && IsDtdLiteralToken(optionalSystem.Token))
            {
                if (!separated)
                {
                    throw CreateDtdError("Whitespace is required before a notation system literal", optionalSystem.Token.StartUtf16Offset);
                }

                reader.ReadDtdDeclarationEntry();
                systemIdentifier = optionalSystem.Token.Value;
                ValidateDtdSystemIdentifier(systemIdentifier == null ? "" : systemIdentifier, optionalSystem.Token.StartUtf16Offset);
                reader.ConsumeDtdDeclarationWhitespace();
            }
        }
        else
        {
            throw CreateDtdError("A notation declaration requires SYSTEM or PUBLIC", identifier.Token.StartUtf16Offset);
        }

        EnsureDtdDeclarationComplete(reader, declarationOffset);
        if (!NotationDeclarations.ContainsKey(name))
        {
            XmlNotationDeclaration declaration = new XmlNotationDeclaration();
            declaration.Name = name;
            declaration.PublicIdentifier = publicIdentifier;
            declaration.SystemIdentifier = systemIdentifier;
            declaration.BaseUri = declarationBaseUri;
            declaration.Range = null;
            NotationDeclarations.Add(name, declaration);
            RetainedNotationDeclarations.Add(declaration);
            XmlEvent item = new XmlEvent();
            item.Kind = XmlEventKind.NotationDeclaration;
            item.NodeKind = XmlNodeKind.DocumentType;
            item.Name = CreateDtdEventName(name);
            item.PublicIdentifier = declaration.PublicIdentifier;
            item.SystemIdentifier = declaration.SystemIdentifier;
            item.Range = declaration.Range;
            item.NotationDeclaration = declaration;
            RetainedDeclarationEvents.Add(item);
        }
        else
        {
            DuplicateNotationDeclarationNames.Add(name);
        }
    }

    internal XmlName CreateDtdEventName(string qualifiedName)
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
            name.LocalName = qualifiedName;
        }

        return name;
    }

    internal string ConstructDtdEntityReplacementText(string value, string? referencingSourceUri, int sourceOffset)
    {
        StringBuilder replacement = new StringBuilder();
        int position = 0;
        while (position < value.Length)
        {
            if (value[position] == '&')
            {
                XmlLexicalScanner scanner = new XmlLexicalScanner(value.AsMemory(), Version);
                scanner.ResetXmlLexicalOffset(position);
                if (position + 1 < value.Length && value[position + 1] == '#')
                {
                    XmlCharacterReference reference = scanner.ScanXmlCharacterReference();
                    replacement.Append(reference.ReplacementText);
                    position = scanner.CurrentXmlLexicalOffset();
                }
                else
                {
                    scanner.ResetXmlLexicalOffset(position + 1);
                    XmlLexicalToken name = scanner.ScanXmlName();
                    position = scanner.CurrentXmlLexicalOffset();
                    if (position >= value.Length || value[position] != ';')
                    {
                        throw CreateDtdError("A general entity reference in an entity value must end with ';'", sourceOffset + position);
                    }

                    replacement.Append('&');
                    replacement.Append(name.Value);
                    replacement.Append(';');
                    position = position + 1;
                }
            }
            else if (value[position] == '%')
            {
                XmlLexicalScanner scanner = new XmlLexicalScanner(value.AsMemory(), Version);
                scanner.ResetXmlLexicalOffset(position + 1);
                XmlLexicalToken name = scanner.ScanXmlName();
                position = scanner.CurrentXmlLexicalOffset();
                if (position >= value.Length || value[position] != ';')
                {
                    throw CreateDtdError("A parameter entity reference in an entity value must end with ';'", sourceOffset + position);
                }

                replacement.Append(ExpandDtdParameterEntityValue(name.Value, referencingSourceUri, sourceOffset + position));
                position = position + 1;
            }
            else
            {
                replacement.Append(value[position]);
                position = position + 1;
            }
        }

        return replacement.ToString();
    }

    internal string ExpandDtdParameterEntityValue(string name, string? referencingSourceUri, int sourceOffset)
    {
        if (!ParameterEntities.ContainsKey(name))
        {
            throw CreateDtdError("The parameter entity '" + name + "' is not declared", sourceOffset);
        }

        if (ActiveParameterEntities.Contains(name))
        {
            throw CreateDtdError("The parameter entity '" + name + "' is recursive", sourceOffset);
        }

        DtdParameterEntityDefinition definition = ParameterEntities[name];
        ParserWorkLimiter? optionalLimiter = WorkLimiter;
        if (optionalLimiter != null)
        {
            optionalLimiter.BeginXmlEntityActivation(name.Length + 2, ActiveParameterEntities.Count == 0);
        }

        ActiveParameterEntities.Add(name);
        string value;
        string? sourceUri = definition.BaseUri;
        if (definition.IsExternal)
        {
            DtdLoadedSource? external = ResolveDtdSource(XmlResourceKind.ParameterEntity, definition.PublicIdentifier, definition.SystemIdentifier == null ? "" : definition.SystemIdentifier, definition.BaseUri == null ? referencingSourceUri : definition.BaseUri);
            if (external == null)
            {
                throw CreateDtdError("The external parameter entity '" + name + "' could not be loaded", sourceOffset);
            }

            value = external.Text.Substring(external.ContentStartUtf16Offset);
            sourceUri = external.SourceUri;
        }
        else
        {
            value = definition.ReplacementText == null ? "" : definition.ReplacementText;
        }

        string expanded = ConstructDtdEntityReplacementText(value, sourceUri, 0);
        ActiveParameterEntities.RemoveAt(ActiveParameterEntities.Count - 1);
        if (optionalLimiter != null)
        {
            optionalLimiter.ConsumeXmlEntityCharacters(expanded.Length);
            optionalLimiter.EndXmlEntityActivation();
        }

        return expanded;
    }

    internal void RequireDtdDeclarationWhitespace(DtdDeclarationReader reader, int offset)
    {
        if (!reader.ConsumeDtdDeclarationWhitespace())
        {
            DtdTokenEntry? entry = reader.PeekDtdDeclarationEntry();
            throw CreateDtdError("Whitespace is required in the DTD declaration", entry == null ? offset : entry.Token.StartUtf16Offset);
        }
    }

    internal DtdTokenEntry ReadRequiredDtdDeclarationEntry(DtdDeclarationReader reader, string message, int offset)
    {
        DtdTokenEntry? entry = reader.ReadDtdDeclarationEntry();
        if (entry == null)
        {
            throw CreateDtdError(message, offset);
        }

        return entry;
    }

    internal string ReadRequiredDtdName(DtdDeclarationReader reader, string message, int offset)
    {
        DtdTokenEntry entry = ReadRequiredDtdDeclarationEntry(reader, message, offset);
        if (entry.Token.Kind != DtdTokenKind.Name || entry.Token.Name == null)
        {
            throw CreateDtdError(message, entry.Token.StartUtf16Offset);
        }

        return entry.Token.Name;
    }

    internal DtdTokenEntry ReadRequiredDtdLiteral(DtdDeclarationReader reader, string message, int offset)
    {
        DtdTokenEntry entry = ReadRequiredDtdDeclarationEntry(reader, message, offset);
        if (!IsDtdLiteralToken(entry.Token))
        {
            throw CreateDtdError(message, entry.Token.StartUtf16Offset);
        }

        return entry;
    }

    internal bool IsDtdLiteralToken(DtdToken token)
    {
        return token.Kind == DtdTokenKind.QuotedLiteral || token.Kind == DtdTokenKind.PublicIdentifierLiteral || token.Kind == DtdTokenKind.SystemLiteral;
    }

    internal void EnsureDtdDeclarationComplete(DtdDeclarationReader reader, int offset)
    {
        if (!reader.IsDtdDeclarationEnd())
        {
            DtdTokenEntry? entry = reader.PeekDtdDeclarationEntry();
            throw CreateDtdError("The DTD declaration contains unexpected content", entry == null ? offset : entry.Token.StartUtf16Offset);
        }
    }

    internal void ValidateDtdPublicIdentifier(string value, int offset)
    {
        int index = 0;
        while (index < value.Length)
        {
            if (!XmlLexicalCharacterRules.IsXmlPublicIdentifierCharacter(value[index]))
            {
                throw CreateDtdError("A public identifier contains a character outside PubidChar", offset + 1 + index);
            }

            index = index + 1;
        }
    }

    internal void ValidateDtdSystemIdentifier(string value, int offset)
    {
        int fragment = value.IndexOf('#');
        if (fragment >= 0)
        {
            throw CreateDtdError("A system identifier must not contain a URI fragment", offset + 1 + fragment);
        }
    }

    public XmlEntityDeclaration? FindXmlGeneralEntity(string name)
    {
        if (GeneralEntityDeclarations.ContainsKey(name))
        {
            return GeneralEntityDeclarations[name];
        }

        return null;
    }

    public DtdLoadedSource? LoadXmlExternalGeneralEntity(XmlEntityDeclaration declaration)
    {
        Guard.NotNull(declaration, "declaration");
        if (!declaration.IsExternal || !declaration.IsParsed || declaration.NotationName != null || declaration.SystemIdentifier == null)
        {
            return null;
        }

        return ResolveDtdSource(XmlResourceKind.GeneralEntity, declaration.PublicIdentifier, declaration.SystemIdentifier, declaration.BaseUri);
    }

    public string? FindXmlEntityReplacementScanText(string name)
    {
        return EntityReplacementScanTexts.ContainsKey(name) ? EntityReplacementScanTexts[name] : null;
    }

    public DtdCompiledContentModel? FindDtdCompiledContentModel(string elementName)
    {
        if (CompiledContentModels.ContainsKey(elementName))
        {
            return CompiledContentModels[elementName];
        }

        return null;
    }

    public XmlAttributeDeclaration? FindDtdAttributeDeclaration(string elementName, string attributeName)
    {
        string key = elementName + "\0" + attributeName;
        if (AttributeDeclarations.ContainsKey(key))
        {
            return AttributeDeclarations[key];
        }

        return null;
    }

    public XmlAttributeDeclaration[] FindDtdAttributeDeclarationsForElement(string elementName)
    {
        if (AttributeDeclarationsByElement.ContainsKey(elementName))
        {
            return AttributeDeclarationsByElement[elementName].ToArray();
        }

        return [];
    }

    public XmlAttributeDeclaration[] FindAllDtdAttributeDeclarations()
    {
        return RetainedAttributeDeclarations.ToArray();
    }

    public bool IsDtdAttributeDeclarationExternal(string elementName, string attributeName)
    {
        string key = elementName + "\0" + attributeName;
        if (ExternalAttributeDeclarations.ContainsKey(key))
        {
            return ExternalAttributeDeclarations[key];
        }

        return false;
    }

    public XmlEntityDeclaration? FindDtdAttributeEntity(string name)
    {
        return FindXmlGeneralEntity(name);
    }

    public bool DoesDtdAttributeDefaultUseExternalEntity(string elementName, string attributeName)
    {
        return ExternalAttributeDefaultEntityDependencies.ContainsKey(elementName + "\0" + attributeName);
    }

    public bool HasDtdAttributeNotationDeclaration(string name)
    {
        return NotationDeclarations.ContainsKey(name);
    }

    public XmlElementDeclaration? FindDtdAttributeElementDeclaration(string name)
    {
        return FindDtdDocumentElementDeclaration(name);
    }

    public XmlElementDeclaration? FindDtdDocumentElementDeclaration(string name)
    {
        if (ElementDeclarations.ContainsKey(name))
        {
            return ElementDeclarations[name];
        }

        return null;
    }

    public string[] FindDtdDuplicateElementDeclarationNames()
    {
        return DuplicateElementDeclarationNames.ToArray();
    }

    public string[] FindDtdDuplicateNotationDeclarationNames()
    {
        return DuplicateNotationDeclarationNames.ToArray();
    }

    public string[] FindDtdDeclarationValidityErrors()
    {
        return DeclarationValidityErrors.ToArray();
    }

    public XmlEntityDeclaration[] FindAllDtdDocumentEntityDeclarations()
    {
        return RetainedEntityDeclarations.ToArray();
    }

    public XmlNotationDeclaration[] FindAllDtdDocumentNotationDeclarations()
    {
        return RetainedNotationDeclarations.ToArray();
    }

    public bool HasDtdDocumentNotationDeclaration(string name)
    {
        return NotationDeclarations.ContainsKey(name);
    }

    public bool IsDtdDocumentElementDeclarationExternal(string name)
    {
        return ExternalElementDeclarations.ContainsKey(name) && ExternalElementDeclarations[name];
    }

    public bool IsDtdDocumentEntityDeclarationExternal(string name)
    {
        return ExternalGeneralEntityDeclarations.ContainsKey(name) && ExternalGeneralEntityDeclarations[name];
    }

    internal DtdLoadedSource? ResolveDtdSource(XmlResourceKind kind, string? publicIdentifier, string systemIdentifier, string? baseUri)
    {
        IXmlResourceResolver? optionalResolver = Resolver;
        XmlResolverPolicy? optionalPolicy = ResolverPolicy;
        if (optionalResolver == null || optionalPolicy == null || optionalPolicy.Access != XmlResourceAccess.Allowed)
        {
            return null;
        }

        XmlResolvedResource resource = optionalResolver.ResolveXmlResource(kind, publicIdentifier, systemIdentifier, baseUri, optionalPolicy);
        Guard.NotNull(resource, "resolvedResource");
        if (resource.Access != XmlResourceAccess.Allowed)
        {
            return null;
        }

        if (resource.Kind != kind)
        {
            throw CreateDtdError("The resolver returned a resource of the wrong kind", 0);
        }

        XmlInput? optionalInput = resource.Input;
        if (optionalInput == null)
        {
            throw CreateDtdError("The resolver allowed a DTD resource without supplying input", 0);
        }

        if (resource.OwnsResource)
        {
            optionalInput.OwnsResource = true;
        }

        if (optionalInput.BaseUri == null)
        {
            optionalInput.BaseUri = resource.AbsoluteUri;
        }

        if (optionalInput.MediaType == null)
        {
            optionalInput.MediaType = resource.MediaType;
        }

        if (optionalInput.EncodingName == null)
        {
            optionalInput.EncodingName = resource.EncodingName;
        }

        ParserWorkLimiter? optionalLimiter = WorkLimiter;
        if (optionalLimiter != null)
        {
            optionalLimiter.BeginXmlExternalResource();
            long knownResourceBytes = KnownXmlResourceBytes(optionalInput);
            if (knownResourceBytes >= 0)
            {
                optionalLimiter.CheckXmlExternalResourceBytes(knownResourceBytes);
            }
        }

        string? sourceUri = resource.AbsoluteUri == null ? optionalInput.BaseUri : resource.AbsoluteUri;
        XmlInputDecoder decoder = new XmlInputDecoder(optionalInput, XmlEncodingEntityKind.ExternalParsedEntity, EncodingPolicyKind.BuiltInText, 4096);
        using (decoder)
        {
            XmlDecodedInput decoded = decoder.DecodeXmlInput();
            long resourceByteCount = decoded.IsCharacterInput ? decoded.Text.Length : decoded.RawByteOffsets[decoded.RawByteOffsets.Length - 1];
            if (optionalLimiter != null)
            {
                optionalLimiter.CheckXmlExternalResourceBytes(resourceByteCount);
            }

            XmlLineNormalizationResult normalized = LineNormalizer.NormalizeXmlLineEndings(decoded, Version, XmlEncodingEntityKind.ExternalParsedEntity);
            XmlDeclarationParser declarationParser = new XmlDeclarationParser(normalized.Text.AsMemory(), XmlEncodingEntityKind.ExternalParsedEntity, 0);
            XmlDeclarationInfo declaration = declarationParser.ParseXmlOrTextDeclaration();
            if (DocumentEntityVersion == XmlVersion.Xml10 && declaration.HasVersion && declaration.Version == XmlVersion.Xml11)
            {
                throw CreateDtdError("An XML 1.1 external entity cannot be included in an XML 1.0 document", 0);
            }

            return new DtdLoadedSource(normalized.Text, resourceByteCount, declaration.CharacterCount, sourceUri, declaration);
        }
    }

    internal long KnownXmlResourceBytes(XmlInput input)
    {
        if (input.Kind == XmlInputKind.String)
        {
            return input.Text == null ? 0 : input.Text.Length;
        }

        if (input.Kind == XmlInputKind.CharacterMemory)
        {
            return input.Characters.Length;
        }

        if (input.Kind == XmlInputKind.ByteMemory)
        {
            return input.Bytes.Length;
        }

        return -1;
    }

    internal void ValidateDtdParameterEntityName(string name)
    {
        Guard.NotNullOrEmpty(name, "name");
        XmlLexicalScanner scanner = new XmlLexicalScanner(name.AsMemory(), Version);
        XmlLexicalToken token = scanner.ScanXmlName();
        if (token.Utf16Length != name.Length)
        {
            throw new ArgumentException("A parameter entity name must be exactly one XML Name.", "name");
        }
    }

    internal FormatException CreateDtdError(string message, int offset)
    {
        return new FormatException(message + " at UTF-16 offset " + offset + ".");
    }
}
