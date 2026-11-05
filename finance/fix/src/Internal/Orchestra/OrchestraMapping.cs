using System;
using System.Collections.Generic;

namespace fix;

internal static class OrchestraMapping
{
    internal static DictionaryLoadResult MapRepositoryToDictionary(OrchestraRepository repository, FixDictionaryOptions options)
    {
        DictionaryLoadResult result = new DictionaryLoadResult();
        if (repository == null)
        {
            AddDictionaryDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-orchestra-repository", "The Orchestra repository is missing.", "", false, 0);
            return result;
        }
        if (options == null)
        {
            AddDictionaryDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-dictionary-options", "The dictionary mapping options are missing.", "", false, 0);
            return result;
        }

        FixDictionary dictionary = new FixDictionary();
        dictionary.Options = options;
        result.Dictionary = dictionary;
        MapRepositoryIdentity(repository, dictionary);
        MapRepositoryDatatypes(repository, dictionary);
        MapRepositoryCodeSets(repository, dictionary);
        MapRepositoryFields(repository, dictionary);
        MapRepositoryComponents(repository, dictionary);
        MapRepositoryGroups(repository, dictionary);
        MapRepositoryMessages(repository, dictionary);
        MapRepositoryScenarios(repository, dictionary);
        AddRepositoryLossDiagnostics(repository, dictionary.Diagnostics);
        DictionaryNormalizer.NormalizeDictionary(dictionary, options);
        result.Diagnostics = dictionary.Diagnostics;
        result.Success = !HasDictionaryErrors(result.Diagnostics);
        return result;
    }

    internal static OrchestraReadResult MapDictionaryToRepository(FixDictionary dictionary, OrchestraVersion version)
    {
        OrchestraReadResult result = new OrchestraReadResult();
        if (dictionary == null)
        {
            AddOrchestraDiagnostic(result.Diagnostics, ValidationSeverity.Error, "missing-fix-dictionary", "The canonical dictionary is missing.", "/");
            return result;
        }
        if (version != OrchestraVersion.Version10 && version != OrchestraVersion.Version11)
        {
            AddOrchestraDiagnostic(result.Diagnostics, ValidationSeverity.Error, "unsupported-orchestra-mapping-version", "The target Orchestra version is not supported.", "/");
            return result;
        }

        OrchestraRepository repository = new OrchestraRepository();
        repository.Version = version;
        repository.Dictionary = dictionary;
        result.Repository = repository;
        MapDictionaryIdentity(dictionary, repository);
        MapDictionaryDatatypes(dictionary, repository);
        MapDictionaryCodeSets(dictionary, repository);
        MapDictionaryFields(dictionary, repository);
        int headerId = ChooseComponentId(dictionary, 1024);
        int trailerId = ChooseComponentId(dictionary, headerId + 1);
        MapDictionaryEnvelope(dictionary, repository, headerId, trailerId);
        MapDictionaryComponents(dictionary, repository);
        MapDictionaryGroups(dictionary, repository);
        MapDictionaryMessages(dictionary, repository, headerId, trailerId);
        MapDictionaryScenarios(dictionary, repository, result.Diagnostics);
        EnsureBaseScenario(repository);
        AddDictionaryLossDiagnostics(dictionary, result.Diagnostics);
        result.Success = !HasOrchestraErrors(result.Diagnostics);
        return result;
    }

    internal static void MapRepositoryIdentity(OrchestraRepository repository, FixDictionary dictionary)
    {
        OrchestraMetadata metadata = repository.Metadata;
        dictionary.Identity.Name = metadata.Name.Length == 0 ? metadata.Release : metadata.Name;
        string applicationVersion = metadata.FixVersion.Length == 0 ? metadata.Release : metadata.FixVersion;
        int marker = applicationVersion.LastIndexOf("_EP", StringComparison.OrdinalIgnoreCase);
        if (marker >= 0) applicationVersion = applicationVersion.Substring(0, marker);
        dictionary.Identity.ApplicationVersion = applicationVersion;
        if (metadata.HasExtensionPack)
        {
            dictionary.Identity.HasExtensionPack = true;
            dictionary.Identity.ExtensionPack = metadata.ExtensionPack;
        }
        else
        {
            string release = metadata.Release;
            int releaseMarker = release.LastIndexOf("_EP", StringComparison.OrdinalIgnoreCase);
            if (releaseMarker >= 0)
            {
                int extensionPack = OrchestraCommonParser.ParseNonnegativeInteger(release.Substring(releaseMarker + 3));
                if (extensionPack >= 0)
                {
                    dictionary.Identity.HasExtensionPack = true;
                    dictionary.Identity.ExtensionPack = extensionPack;
                }
            }
        }
        if (OrchestraDictionaryParser.IsClassicBeginString(applicationVersion) || applicationVersion.StartsWith("FIXT.", StringComparison.OrdinalIgnoreCase)) dictionary.Identity.BeginString = applicationVersion;
        dictionary.Identity.Scenario = ReadRepositoryScenario(repository);
    }

    internal static string ReadRepositoryScenario(OrchestraRepository repository)
    {
        if (repository.Dictionary != null) return repository.Dictionary.Identity.Scenario;
        return "";
    }

    internal static void MapRepositoryDatatypes(OrchestraRepository repository, FixDictionary dictionary)
    {
        for (int index = 0; index < repository.Datatypes.Count; index++)
        {
            OrchestraDatatypeDefinition source = repository.Datatypes[index];
            FixDatatypeDefinition target = new FixDatatypeDefinition();
            target.Name = source.Name;
            target.BaseTypeName = source.BaseType;
            target.FieldType = OrchestraDictionaryParser.ResolveOrchestraBuiltinType(source.Name);
            target.Description = ReadDescription(source.Annotations);
            dictionary.Datatypes.Add(target);
        }
    }

    internal static void MapRepositoryCodeSets(OrchestraRepository repository, FixDictionary dictionary)
    {
        for (int index = 0; index < repository.CodeSets.Count; index++)
        {
            OrchestraCodeSetDefinition source = repository.CodeSets[index];
            if (!IsBaseScenario(source.Scenario)) continue;
            FixCodeSetDefinition target = new FixCodeSetDefinition();
            target.Id = ParseDefinitionId(source.Id, source.Name, "code set", dictionary.Diagnostics, false);
            target.Name = source.Name;
            target.FieldType = ResolveRepositoryType(repository, source.Type);
            target.Description = ReadDescription(source.Annotations);
            for (int codeIndex = 0; codeIndex < source.Codes.Count; codeIndex++)
            {
                OrchestraCodeDefinition sourceCode = source.Codes[codeIndex];
                if (!IsBaseScenario(sourceCode.Scenario)) continue;
                FixCodeDefinition targetCode = new FixCodeDefinition();
                targetCode.Id = ParseOptionalId(sourceCode.Id, codeIndex);
                targetCode.Name = sourceCode.Name.Length == 0 ? sourceCode.Value : sourceCode.Name;
                targetCode.Value = sourceCode.Value;
                targetCode.SortOrder = ParseOptionalId(sourceCode.Sort, codeIndex);
                targetCode.Description = ReadDescription(sourceCode.Annotations);
                target.Codes.Add(targetCode);
            }
            dictionary.CodeSets.Add(target);
        }
    }

    internal static void MapRepositoryFields(OrchestraRepository repository, FixDictionary dictionary)
    {
        for (int index = 0; index < repository.Fields.Count; index++)
        {
            OrchestraFieldDefinition source = repository.Fields[index];
            if (!IsBaseScenario(source.Scenario)) continue;
            FixFieldDefinition target = new FixFieldDefinition();
            target.Tag = ParseDefinitionId(source.Id, source.Name, "field", dictionary.Diagnostics, true);
            target.Name = source.Name;
            target.Abbreviation = source.Abbreviation;
            target.CodeSetName = source.CodeSet;
            target.DatatypeName = source.Type;
            if (target.CodeSetName.Length != 0)
            {
                OrchestraCodeSetDefinition? codeSet = FindRepositoryCodeSet(repository, target.CodeSetName);
                if (target.DatatypeName.Length == 0 && codeSet != null) target.DatatypeName = OrchestraDictionaryParser.ResolveCanonicalTypeName(ResolveRepositoryType(repository, codeSet.Type));
            }
            target.FieldType = ResolveRepositoryType(repository, target.DatatypeName);
            if (target.FieldType == FixFieldType.Unknown && target.CodeSetName.Length != 0)
            {
                OrchestraCodeSetDefinition? codeSet = FindRepositoryCodeSet(repository, target.CodeSetName);
                if (codeSet != null) target.FieldType = ResolveRepositoryType(repository, codeSet.Type);
            }
            int discriminator = ParseOptionalId(source.DiscriminatorId, 0);
            if (discriminator > 0)
            {
                target.HasDiscriminatorTag = true;
                target.DiscriminatorTag = discriminator;
            }
            int length = ParseOptionalId(source.LengthId, 0);
            if (length > 0)
            {
                target.HasLengthTag = true;
                target.LengthTag = length;
            }
            target.Description = ReadDescription(source.Annotations);
            dictionary.Fields.Add(target);
        }
    }

    internal static void MapRepositoryComponents(OrchestraRepository repository, FixDictionary dictionary)
    {
        for (int index = 0; index < repository.Components.Count; index++)
        {
            OrchestraComponentDefinition source = repository.Components[index];
            if (!IsBaseScenario(source.Scenario)) continue;
            List<FixMemberDefinition> members = MapRepositoryMembers(source.Members, dictionary.Diagnostics);
            if (source.Name == "StandardHeader")
            {
                dictionary.Header = members;
            }
            else if (source.Name == "StandardTrailer")
            {
                dictionary.Trailer = members;
            }
            else
            {
                FixComponentDefinition target = new FixComponentDefinition();
                target.Id = ParseDefinitionId(source.Id, source.Name, "component", dictionary.Diagnostics, false);
                target.Name = source.Name;
                target.Abbreviation = source.Abbreviation;
                target.Members = members;
                target.Description = ReadDescription(source.Annotations);
                dictionary.Components.Add(target);
            }
        }
    }

    internal static void MapRepositoryGroups(OrchestraRepository repository, FixDictionary dictionary)
    {
        for (int index = 0; index < repository.Groups.Count; index++)
        {
            OrchestraGroupDefinition source = repository.Groups[index];
            if (!IsBaseScenario(source.Scenario)) continue;
            FixGroupDefinition target = new FixGroupDefinition();
            target.Id = ParseDefinitionId(source.Id, source.Name, "group", dictionary.Diagnostics, false);
            target.Name = source.Name;
            if (source.CountMember != null) target.CountTag = ParseDefinitionId(source.CountMember.ReferenceId, source.Name, "group count", dictionary.Diagnostics, true);
            target.Members = MapRepositoryMembers(source.Members, dictionary.Diagnostics);
            target.DelimiterTag = ReadDelimiterTag(target.Members);
            if (target.DelimiterTag == 0) AddDictionaryDiagnostic(dictionary.Diagnostics, ValidationSeverity.Warning, "orchestra-group-delimiter-loss", "The group delimiter could not be represented by a direct first field member.", source.Name, false, 0);
            target.Description = ReadDescription(source.Annotations);
            dictionary.Groups.Add(target);
        }
    }

    internal static void MapRepositoryMessages(OrchestraRepository repository, FixDictionary dictionary)
    {
        for (int index = 0; index < repository.Messages.Count; index++)
        {
            OrchestraMessageDefinition source = repository.Messages[index];
            FixMessageDefinition target = new FixMessageDefinition();
            target.Id = ParseDefinitionId(source.Id, source.Name, "message", dictionary.Diagnostics, false);
            target.Name = source.Name;
            target.MsgType = source.MessageType;
            target.Category = source.Category;
            target.Scenario = source.Scenario.Length == 0 ? "base" : source.Scenario;
            target.Members = MapRepositoryMessageMembers(source.Members, repository, dictionary.Diagnostics);
            target.Description = ReadDescription(source.Annotations);
            dictionary.Messages.Add(target);
        }
    }

    internal static void MapRepositoryScenarios(OrchestraRepository repository, FixDictionary dictionary)
    {
        for (int index = 0; index < repository.Scenarios.Count; index++)
        {
            OrchestraScenarioDefinition source = repository.Scenarios[index];
            FixScenarioDefinition target = new FixScenarioDefinition();
            target.Name = source.Name.Length == 0 ? "base" : source.Name;
            target.ParentName = source.ParentName;
            if (target.ParentName.Length == 0 && source.Attributes.ContainsKey("scenarioRef")) target.ParentName = source.Attributes["scenarioRef"];
            target.Description = ReadDescription(source.Annotations);
            dictionary.Scenarios.Add(target);
        }
    }

    internal static List<FixMemberDefinition> MapRepositoryMembers(List<OrchestraMemberDefinition> source, List<DictionaryDiagnostic> diagnostics)
    {
        List<FixMemberDefinition> result = [];
        for (int index = 0; index < source.Count; index++)
        {
            OrchestraMemberDefinition sourceMember = source[index];
            FixMemberDefinition target = new FixMemberDefinition();
            target.Name = sourceMember.InstanceName;
            target.Kind = ReadFixMemberKind(sourceMember.Kind);
            target.ReferenceId = ParseDefinitionId(sourceMember.ReferenceId, sourceMember.InstanceName, "member", diagnostics, true);
            target.Position = result.Count;
            target.Presence = sourceMember.Presence;
            target.Scenario = sourceMember.Scenario.Length == 0 ? "base" : sourceMember.Scenario;
            OrchestraRule? rule = FindPresenceRule(sourceMember.Rules);
            if (rule != null)
            {
                target.Presence = FixPresence.Conditional;
                target.PresenceRule = rule.Expression;
                target.RulePresence = rule.Presence;
            }
            result.Add(target);
        }
        return result;
    }

    internal static OrchestraRule? FindPresenceRule(List<OrchestraRule> rules)
    {
        for (int index = 0; index < rules.Count; index++) if (rules[index].Kind == OrchestraRuleKind.Presence) return rules[index];
        return null;
    }

    internal static List<FixMemberDefinition> MapRepositoryMessageMembers(List<OrchestraMemberDefinition> source, OrchestraRepository repository, List<DictionaryDiagnostic> diagnostics)
    {
        List<FixMemberDefinition> result = [];
        for (int index = 0; index < source.Count; index++)
        {
            OrchestraMemberDefinition member = source[index];
            if (member.Kind == "componentRef" && IsEnvelopeComponent(repository, member.ReferenceId)) continue;
            List<OrchestraMemberDefinition> one = [member];
            List<FixMemberDefinition> mapped = MapRepositoryMembers(one, diagnostics);
            if (mapped.Count != 0)
            {
                mapped[0].Position = result.Count;
                result.Add(mapped[0]);
            }
        }
        return result;
    }

    internal static bool IsEnvelopeComponent(OrchestraRepository repository, string id)
    {
        for (int index = 0; index < repository.Components.Count; index++)
        {
            OrchestraComponentDefinition component = repository.Components[index];
            if (component.Id == id && (component.Name == "StandardHeader" || component.Name == "StandardTrailer")) return true;
        }
        return false;
    }

    internal static int ReadDelimiterTag(List<FixMemberDefinition> members)
    {
        if (members.Count != 0 && members[0].Kind == FixMemberKind.Field) return members[0].ReferenceId;
        return 0;
    }

    internal static void MapDictionaryIdentity(FixDictionary dictionary, OrchestraRepository repository)
    {
        string version = dictionary.Identity.ApplicationVersion;
        if (version.Length == 0) version = dictionary.Identity.BeginString;
        repository.Metadata.Name = dictionary.Identity.Name;
        repository.Metadata.FixVersion = version;
        repository.Metadata.Release = version;
        if (dictionary.Identity.HasExtensionPack)
        {
            repository.Metadata.HasExtensionPack = true;
            repository.Metadata.ExtensionPack = dictionary.Identity.ExtensionPack;
            repository.Metadata.Release += "_EP" + dictionary.Identity.ExtensionPack.ToString(System.Globalization.CultureInfo.InvariantCulture);
        }
    }

    internal static void MapDictionaryDatatypes(FixDictionary dictionary, OrchestraRepository repository)
    {
        for (int index = 0; index < dictionary.Datatypes.Count; index++)
        {
            FixDatatypeDefinition source = dictionary.Datatypes[index];
            if (source.IsRemoved) continue;
            OrchestraDatatypeDefinition target = new OrchestraDatatypeDefinition();
            target.Name = source.Name.Length == 0 ? OrchestraDictionaryParser.ResolveCanonicalTypeName(source.FieldType) : source.Name;
            target.BaseType = source.BaseTypeName;
            AddDescription(target.Annotations, source.Description);
            repository.Datatypes.Add(target);
        }
    }

    internal static void MapDictionaryCodeSets(FixDictionary dictionary, OrchestraRepository repository)
    {
        for (int index = 0; index < dictionary.CodeSets.Count; index++)
        {
            FixCodeSetDefinition source = dictionary.CodeSets[index];
            if (source.IsRemoved) continue;
            OrchestraCodeSetDefinition target = new OrchestraCodeSetDefinition();
            target.Id = IntegerText(source.Id);
            target.Name = source.Name;
            target.Scenario = "base";
            target.Type = OrchestraDictionaryParser.ResolveCanonicalTypeName(source.FieldType);
            AddDescription(target.Annotations, source.Description);
            for (int codeIndex = 0; codeIndex < source.Codes.Count; codeIndex++)
            {
                FixCodeDefinition sourceCode = source.Codes[codeIndex];
                if (sourceCode.IsRemoved) continue;
                OrchestraCodeDefinition targetCode = new OrchestraCodeDefinition();
                targetCode.Id = IntegerText(sourceCode.Id);
                targetCode.Name = sourceCode.Name;
                targetCode.Scenario = "base";
                targetCode.Value = sourceCode.Value;
                targetCode.Sort = IntegerText(sourceCode.SortOrder);
                AddDescription(targetCode.Annotations, sourceCode.Description);
                target.Codes.Add(targetCode);
            }
            repository.CodeSets.Add(target);
        }
    }

    internal static void MapDictionaryFields(FixDictionary dictionary, OrchestraRepository repository)
    {
        for (int index = 0; index < dictionary.Fields.Count; index++)
        {
            FixFieldDefinition source = dictionary.Fields[index];
            if (source.IsRemoved) continue;
            OrchestraFieldDefinition target = new OrchestraFieldDefinition();
            target.Id = IntegerText(source.Tag);
            target.Name = source.Name;
            target.Abbreviation = source.Abbreviation;
            target.Scenario = "base";
            target.CodeSet = source.CodeSetName;
            if (target.CodeSet.Length == 0)
            {
                target.Type = source.DatatypeName;
                if (target.Type.Length == 0) target.Type = OrchestraDictionaryParser.ResolveCanonicalTypeName(source.FieldType);
            }
            if (source.HasDiscriminatorTag) target.DiscriminatorId = IntegerText(source.DiscriminatorTag);
            if (source.HasLengthTag) target.LengthId = IntegerText(source.LengthTag);
            AddDescription(target.Annotations, source.Description);
            repository.Fields.Add(target);
        }
    }

    internal static void MapDictionaryEnvelope(FixDictionary dictionary, OrchestraRepository repository, int headerId, int trailerId)
    {
        if (dictionary.Header.Count != 0)
        {
            OrchestraComponentDefinition header = new OrchestraComponentDefinition();
            header.Id = IntegerText(headerId);
            header.Name = "StandardHeader";
            header.Scenario = "base";
            header.Members = MapDictionaryMembers(dictionary.Header, repository);
            repository.Components.Add(header);
        }
        if (dictionary.Trailer.Count != 0)
        {
            OrchestraComponentDefinition trailer = new OrchestraComponentDefinition();
            trailer.Id = IntegerText(trailerId);
            trailer.Name = "StandardTrailer";
            trailer.Scenario = "base";
            trailer.Members = MapDictionaryMembers(dictionary.Trailer, repository);
            repository.Components.Add(trailer);
        }
    }

    internal static void MapDictionaryComponents(FixDictionary dictionary, OrchestraRepository repository)
    {
        for (int index = 0; index < dictionary.Components.Count; index++)
        {
            FixComponentDefinition source = dictionary.Components[index];
            if (source.IsRemoved) continue;
            OrchestraComponentDefinition target = new OrchestraComponentDefinition();
            target.Id = IntegerText(source.Id);
            target.Name = source.Name;
            target.Abbreviation = source.Abbreviation;
            target.Scenario = "base";
            target.Members = MapDictionaryMembers(source.Members, repository);
            AddDescription(target.Annotations, source.Description);
            repository.Components.Add(target);
        }
    }

    internal static void MapDictionaryGroups(FixDictionary dictionary, OrchestraRepository repository)
    {
        for (int index = 0; index < dictionary.Groups.Count; index++)
        {
            FixGroupDefinition source = dictionary.Groups[index];
            if (source.IsRemoved) continue;
            OrchestraGroupDefinition target = new OrchestraGroupDefinition();
            target.Id = IntegerText(source.Id);
            target.Name = source.Name;
            target.Scenario = "base";
            target.CountMember = new OrchestraMemberDefinition();
            target.CountMember.Kind = "fieldRef";
            target.CountMember.ReferenceId = IntegerText(source.CountTag);
            target.CountMember.Scenario = "base";
            target.Members = MapDictionaryMembers(source.Members, repository);
            AddDescription(target.Annotations, source.Description);
            repository.Groups.Add(target);
        }
    }

    internal static void MapDictionaryMessages(FixDictionary dictionary, OrchestraRepository repository, int headerId, int trailerId)
    {
        for (int index = 0; index < dictionary.Messages.Count; index++)
        {
            FixMessageDefinition source = dictionary.Messages[index];
            if (source.IsRemoved) continue;
            OrchestraMessageDefinition target = new OrchestraMessageDefinition();
            target.Id = IntegerText(source.Id);
            target.Name = source.Name;
            target.Scenario = source.Scenario.Length == 0 ? "base" : source.Scenario;
            target.MessageType = source.MsgType;
            target.Category = source.Category;
            if (dictionary.Header.Count != 0) target.Members.Add(CreateEnvelopeMember(headerId, "base", target.Members.Count));
            List<OrchestraMemberDefinition> mapped = MapDictionaryMembers(source.Members, repository);
            for (int memberIndex = 0; memberIndex < mapped.Count; memberIndex++)
            {
                mapped[memberIndex].Position = target.Members.Count;
                target.Members.Add(mapped[memberIndex]);
            }
            if (dictionary.Trailer.Count != 0) target.Members.Add(CreateEnvelopeMember(trailerId, "base", target.Members.Count));
            AddDescription(target.Annotations, source.Description);
            repository.Messages.Add(target);
        }
    }

    internal static OrchestraMemberDefinition CreateEnvelopeMember(int id, string scenario, int position)
    {
        OrchestraMemberDefinition member = new OrchestraMemberDefinition();
        member.Kind = "componentRef";
        member.ReferenceId = IntegerText(id);
        member.Scenario = scenario;
        member.Position = position;
        member.Presence = FixPresence.Required;
        member.PresenceText = "required";
        return member;
    }

    internal static List<OrchestraMemberDefinition> MapDictionaryMembers(List<FixMemberDefinition> source, OrchestraRepository repository)
    {
        List<OrchestraMemberDefinition> result = [];
        for (int index = 0; index < source.Count; index++)
        {
            FixMemberDefinition sourceMember = source[index];
            if (sourceMember.IsRemoved) continue;
            OrchestraMemberDefinition target = new OrchestraMemberDefinition();
            target.Kind = ReadOrchestraMemberKind(sourceMember.Kind);
            target.ReferenceId = IntegerText(sourceMember.ReferenceId);
            target.InstanceName = sourceMember.Name;
            target.Position = result.Count;
            target.Presence = sourceMember.Presence == FixPresence.Conditional ? FixPresence.Optional : sourceMember.Presence;
            target.PresenceText = PresenceText(target.Presence);
            target.Scenario = sourceMember.Scenario.Length == 0 ? "base" : sourceMember.Scenario;
            if (sourceMember.PresenceRule.Length != 0)
            {
                OrchestraRule rule = new OrchestraRule();
                rule.Id = repository.Rules.Count + 1;
                rule.Kind = OrchestraRuleKind.Presence;
                rule.Presence = sourceMember.RulePresence;
                rule.PresenceText = PresenceText(sourceMember.RulePresence);
                rule.Expression = sourceMember.PresenceRule;
                target.Rules.Add(rule);
                repository.Rules.Add(rule);
            }
            result.Add(target);
        }
        return result;
    }

    internal static void MapDictionaryScenarios(FixDictionary dictionary, OrchestraRepository repository, List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < dictionary.Scenarios.Count; index++)
        {
            FixScenarioDefinition source = dictionary.Scenarios[index];
            if (source.IsRemoved) continue;
            OrchestraScenarioDefinition target = new OrchestraScenarioDefinition();
            target.Name = source.Name.Length == 0 ? "base" : source.Name;
            target.ParentName = source.ParentName;
            if (target.ParentName.Length != 0) target.Attributes["scenarioRef"] = target.ParentName;
            AddDescription(target.Annotations, source.Description);
            repository.Scenarios.Add(target);
            if (source.Members.Count != 0) AddOrchestraDiagnostic(diagnostics, ValidationSeverity.Warning, "dictionary-scenario-member-loss", $"Scenario '{target.Name}' has specialized canonical members that are not attached to an Orchestra definition.", $"/scenarios/{target.Name}");
            if (source.Rules.Count != 0) AddOrchestraDiagnostic(diagnostics, ValidationSeverity.Warning, "dictionary-scenario-rule-loss", $"Scenario '{target.Name}' has canonical rules that are not attached to an Orchestra definition.", $"/scenarios/{target.Name}");
        }
    }

    internal static void EnsureBaseScenario(OrchestraRepository repository)
    {
        for (int index = 0; index < repository.Scenarios.Count; index++) if (repository.Scenarios[index].Name == "base") return;
        OrchestraScenarioDefinition scenario = new OrchestraScenarioDefinition();
        scenario.Name = "base";
        repository.Scenarios.Insert(0, scenario);
    }

    internal static int ChooseComponentId(FixDictionary dictionary, int candidate)
    {
        int selected = candidate;
        bool collision = true;
        while (collision)
        {
            collision = false;
            for (int index = 0; index < dictionary.Components.Count; index++) if (dictionary.Components[index].Id == selected) collision = true;
            if (collision) selected++;
        }
        return selected;
    }

    internal static void AddRepositoryLossDiagnostics(OrchestraRepository repository, List<DictionaryDiagnostic> diagnostics)
    {
        if (HasBehaviorMetadata(repository)) AddDictionaryDiagnostic(diagnostics, ValidationSeverity.Warning, "orchestra-behavior-loss", "Orchestra workflow, response, assignment, or rule metadata has no complete canonical dictionary representation.", repository.Metadata.Name, false, 0);
        if (HasEncodingMetadata(repository)) AddDictionaryDiagnostic(diagnostics, ValidationSeverity.Warning, "orchestra-encoding-metadata-loss", "Orchestra physical encoding metadata has no canonical dictionary representation.", repository.Metadata.Name, false, 0);
        if (repository.Interfaces.Count != 0) AddDictionaryDiagnostic(diagnostics, ValidationSeverity.Warning, "orchestra-interface-loss", "Orchestra interface and session content has no canonical dictionary representation.", repository.Metadata.Name, false, 0);
        if (HasScenarioSpecializations(repository)) AddDictionaryDiagnostic(diagnostics, ValidationSeverity.Warning, "orchestra-scenario-definition-loss", "Scenario-specialized structural definitions were omitted from the canonical dictionary.", repository.Metadata.Name, false, 0);
    }

    internal static bool HasBehaviorMetadata(OrchestraRepository repository)
    {
        if (repository.Actors.Count != 0 || repository.Flows.Count != 0 || repository.StateMachines.Count != 0 || repository.Variables.Count != 0 || repository.Timers.Count != 0) return true;
        if (repository.Rules.Count != 0) return true;
        for (int index = 0; index < repository.Fields.Count; index++) if (repository.Fields[index].Rules.Count != 0) return true;
        for (int index = 0; index < repository.Components.Count; index++) if (HasMemberRules(repository.Components[index].Members)) return true;
        for (int index = 0; index < repository.Groups.Count; index++) if (HasMemberRules(repository.Groups[index].Members)) return true;
        for (int index = 0; index < repository.Messages.Count; index++) if (repository.Messages[index].Responses.Count != 0 || repository.Messages[index].Rules.Count != 0) return true;
        for (int index = 0; index < repository.Messages.Count; index++) if (HasMemberRules(repository.Messages[index].Members)) return true;
        return false;
    }

    internal static bool HasMemberRules(List<OrchestraMemberDefinition> members)
    {
        for (int index = 0; index < members.Count; index++) if (members[index].Rules.Count != 0) return true;
        return false;
    }

    internal static bool HasEncodingMetadata(OrchestraRepository repository)
    {
        if (repository.EncodingStandards.Count != 0 || repository.CodeSetEncodings.Count != 0 || repository.ComponentEncodings.Count != 0 || repository.GroupEncodings.Count != 0 || repository.MessageEncodings.Count != 0) return true;
        for (int index = 0; index < repository.Datatypes.Count; index++) if (repository.Datatypes[index].Mappings.Count != 0) return true;
        for (int index = 0; index < repository.CodeSets.Count; index++) if (repository.CodeSets[index].Encodings.Count != 0) return true;
        for (int index = 0; index < repository.Fields.Count; index++) if (repository.Fields[index].Encodings.Count != 0) return true;
        for (int index = 0; index < repository.Components.Count; index++) if (repository.Components[index].Encodings.Count != 0 || HasMemberEncodings(repository.Components[index].Members)) return true;
        for (int index = 0; index < repository.Groups.Count; index++)
        {
            OrchestraGroupDefinition group = repository.Groups[index];
            if (group.Encodings.Count != 0 || HasMemberEncodings(group.Members)) return true;
            if (group.CountMember != null && group.CountMember.Encodings.Count != 0) return true;
        }
        for (int index = 0; index < repository.Messages.Count; index++) if (repository.Messages[index].Encodings.Count != 0 || HasMemberEncodings(repository.Messages[index].Members)) return true;
        return false;
    }

    internal static bool HasMemberEncodings(List<OrchestraMemberDefinition> members)
    {
        for (int index = 0; index < members.Count; index++) if (members[index].Encodings.Count != 0) return true;
        return false;
    }

    internal static bool HasScenarioSpecializations(OrchestraRepository repository)
    {
        for (int index = 0; index < repository.CodeSets.Count; index++)
        {
            OrchestraCodeSetDefinition codeSet = repository.CodeSets[index];
            if (!IsBaseScenario(codeSet.Scenario)) return true;
            for (int codeIndex = 0; codeIndex < codeSet.Codes.Count; codeIndex++) if (!IsBaseScenario(codeSet.Codes[codeIndex].Scenario)) return true;
        }
        for (int index = 0; index < repository.Fields.Count; index++) if (!IsBaseScenario(repository.Fields[index].Scenario)) return true;
        for (int index = 0; index < repository.Components.Count; index++) if (!IsBaseScenario(repository.Components[index].Scenario)) return true;
        for (int index = 0; index < repository.Groups.Count; index++) if (!IsBaseScenario(repository.Groups[index].Scenario)) return true;
        return false;
    }

    internal static void AddDictionaryLossDiagnostics(FixDictionary dictionary, List<OrchestraDiagnostic> diagnostics)
    {
        if (dictionary.Diagnostics.Count != 0) AddOrchestraDiagnostic(diagnostics, ValidationSeverity.Warning, "dictionary-diagnostics-loss", "Canonical dictionary diagnostics are not embedded in the Orchestra repository.", "/");
        if (HasRemovedDefinitions(dictionary)) AddOrchestraDiagnostic(diagnostics, ValidationSeverity.Warning, "dictionary-overlay-removal-loss", "Canonical overlay removal markers are not representable as Orchestra definitions and were omitted.", "/");
        if (HasSourceProvenance(dictionary)) AddOrchestraDiagnostic(diagnostics, ValidationSeverity.Warning, "dictionary-source-provenance-loss", "Canonical source provenance is not represented by Orchestra definition fields.", "/");
    }

    internal static bool HasRemovedDefinitions(FixDictionary dictionary)
    {
        for (int index = 0; index < dictionary.Datatypes.Count; index++) if (dictionary.Datatypes[index].IsRemoved) return true;
        for (int index = 0; index < dictionary.CodeSets.Count; index++)
        {
            FixCodeSetDefinition codeSet = dictionary.CodeSets[index];
            if (codeSet.IsRemoved) return true;
            for (int codeIndex = 0; codeIndex < codeSet.Codes.Count; codeIndex++) if (codeSet.Codes[codeIndex].IsRemoved) return true;
        }
        for (int index = 0; index < dictionary.Fields.Count; index++) if (dictionary.Fields[index].IsRemoved) return true;
        if (HasRemovedMembers(dictionary.Header) || HasRemovedMembers(dictionary.Trailer)) return true;
        for (int index = 0; index < dictionary.Components.Count; index++) if (dictionary.Components[index].IsRemoved || HasRemovedMembers(dictionary.Components[index].Members)) return true;
        for (int index = 0; index < dictionary.Groups.Count; index++) if (dictionary.Groups[index].IsRemoved || HasRemovedMembers(dictionary.Groups[index].Members)) return true;
        for (int index = 0; index < dictionary.Messages.Count; index++) if (dictionary.Messages[index].IsRemoved || HasRemovedMembers(dictionary.Messages[index].Members)) return true;
        for (int index = 0; index < dictionary.Scenarios.Count; index++) if (dictionary.Scenarios[index].IsRemoved || HasRemovedMembers(dictionary.Scenarios[index].Members)) return true;
        return false;
    }

    internal static bool HasRemovedMembers(List<FixMemberDefinition> members)
    {
        for (int index = 0; index < members.Count; index++) if (members[index].IsRemoved) return true;
        return false;
    }

    internal static bool HasSourceProvenance(FixDictionary dictionary)
    {
        if (dictionary.Identity.Source.Length != 0) return true;
        for (int index = 0; index < dictionary.Datatypes.Count; index++) if (dictionary.Datatypes[index].Source.Length != 0) return true;
        for (int index = 0; index < dictionary.CodeSets.Count; index++)
        {
            FixCodeSetDefinition codeSet = dictionary.CodeSets[index];
            if (codeSet.Source.Length != 0) return true;
            for (int codeIndex = 0; codeIndex < codeSet.Codes.Count; codeIndex++) if (codeSet.Codes[codeIndex].Source.Length != 0) return true;
        }
        for (int index = 0; index < dictionary.Fields.Count; index++) if (dictionary.Fields[index].Source.Length != 0) return true;
        if (HasMemberSource(dictionary.Header) || HasMemberSource(dictionary.Trailer)) return true;
        for (int index = 0; index < dictionary.Components.Count; index++) if (dictionary.Components[index].Source.Length != 0 || HasMemberSource(dictionary.Components[index].Members)) return true;
        for (int index = 0; index < dictionary.Groups.Count; index++) if (dictionary.Groups[index].Source.Length != 0 || HasMemberSource(dictionary.Groups[index].Members)) return true;
        for (int index = 0; index < dictionary.Messages.Count; index++) if (dictionary.Messages[index].Source.Length != 0 || HasMemberSource(dictionary.Messages[index].Members)) return true;
        for (int index = 0; index < dictionary.Scenarios.Count; index++) if (dictionary.Scenarios[index].Source.Length != 0 || HasMemberSource(dictionary.Scenarios[index].Members)) return true;
        return false;
    }

    internal static bool HasMemberSource(List<FixMemberDefinition> members)
    {
        for (int index = 0; index < members.Count; index++) if (members[index].Source.Length != 0) return true;
        return false;
    }

    internal static FixFieldType ResolveRepositoryType(OrchestraRepository repository, string name)
    {
        FixFieldType direct = OrchestraDictionaryParser.ResolveOrchestraBuiltinType(name);
        if (direct != FixFieldType.Unknown) return direct;
        string current = name;
        for (int depth = 0; depth <= repository.Datatypes.Count; depth++)
        {
            OrchestraDatatypeDefinition? datatype = FindRepositoryDatatype(repository, current);
            if (datatype == null) return FixFieldType.Unknown;
            direct = OrchestraDictionaryParser.ResolveOrchestraBuiltinType(datatype.Name);
            if (direct != FixFieldType.Unknown) return direct;
            direct = OrchestraDictionaryParser.ResolveOrchestraBuiltinType(datatype.BaseType);
            if (direct != FixFieldType.Unknown) return direct;
            current = datatype.BaseType;
        }
        return FixFieldType.Unknown;
    }

    internal static OrchestraDatatypeDefinition? FindRepositoryDatatype(OrchestraRepository repository, string name)
    {
        for (int index = 0; index < repository.Datatypes.Count; index++) if (repository.Datatypes[index].Name == name) return repository.Datatypes[index];
        return null;
    }

    internal static OrchestraCodeSetDefinition? FindRepositoryCodeSet(OrchestraRepository repository, string name)
    {
        for (int index = 0; index < repository.CodeSets.Count; index++) if (repository.CodeSets[index].Name == name && IsBaseScenario(repository.CodeSets[index].Scenario)) return repository.CodeSets[index];
        return null;
    }

    internal static FixMemberKind ReadFixMemberKind(string kind)
    {
        if (kind == "fieldRef") return FixMemberKind.Field;
        if (kind == "componentRef") return FixMemberKind.Component;
        if (kind == "groupRef") return FixMemberKind.Group;
        return FixMemberKind.Unknown;
    }

    internal static string ReadOrchestraMemberKind(FixMemberKind kind)
    {
        if (kind == FixMemberKind.Field) return "fieldRef";
        if (kind == FixMemberKind.Component) return "componentRef";
        if (kind == FixMemberKind.Group) return "groupRef";
        return "fieldRef";
    }

    internal static string PresenceText(FixPresence presence)
    {
        if (presence == FixPresence.Required) return "required";
        if (presence == FixPresence.Forbidden) return "forbidden";
        if (presence == FixPresence.Ignored) return "ignored";
        if (presence == FixPresence.Constant) return "constant";
        return "";
    }

    internal static int ParseDefinitionId(string value, string name, string kind, List<DictionaryDiagnostic> diagnostics, bool positive)
    {
        int parsed = OrchestraCommonParser.ParseNonnegativeInteger(value.Trim());
        if (parsed < 0 || positive && parsed == 0)
        {
            AddDictionaryDiagnostic(diagnostics, ValidationSeverity.Error, "invalid-orchestra-id", $"The Orchestra {kind} '{name}' has an invalid identifier '{value}'.", name, false, 0);
            return 0;
        }
        return parsed;
    }

    internal static int ParseOptionalId(string value, int fallback)
    {
        int parsed = OrchestraCommonParser.ParseNonnegativeInteger(value.Trim());
        return parsed < 0 ? fallback : parsed;
    }

    internal static bool IsBaseScenario(string scenario)
    {
        return scenario.Length == 0 || scenario == "base";
    }

    internal static string ReadDescription(List<OrchestraAnnotation> annotations)
    {
        for (int index = 0; index < annotations.Count; index++) if (annotations[index].Kind == "documentation") return annotations[index].Content;
        return "";
    }

    internal static void AddDescription(List<OrchestraAnnotation> annotations, string description)
    {
        if (description.Length == 0) return;
        OrchestraAnnotation annotation = new OrchestraAnnotation();
        annotation.Kind = "documentation";
        annotation.Content = description;
        annotations.Add(annotation);
    }

    internal static string IntegerText(int value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    internal static void AddDictionaryDiagnostic(List<DictionaryDiagnostic> diagnostics, ValidationSeverity severity, string code, string message, string definitionName, bool hasTag, int tag)
    {
        DictionaryDiagnostic diagnostic = new DictionaryDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.DefinitionName = definitionName;
        diagnostic.HasTag = hasTag;
        diagnostic.Tag = tag;
        diagnostics.Add(diagnostic);
    }

    internal static void AddOrchestraDiagnostic(List<OrchestraDiagnostic> diagnostics, ValidationSeverity severity, string code, string message, string path)
    {
        OrchestraDiagnostic diagnostic = new OrchestraDiagnostic();
        diagnostic.Severity = severity;
        diagnostic.Code = code;
        diagnostic.Message = message;
        diagnostic.ElementPath = path;
        diagnostics.Add(diagnostic);
    }

    internal static bool HasDictionaryErrors(List<DictionaryDiagnostic> diagnostics)
    {
        for (int index = 0; index < diagnostics.Count; index++)
        {
            if (diagnostics[index].Severity == ValidationSeverity.Error || diagnostics[index].Severity == ValidationSeverity.Fatal) return true;
        }
        return false;
    }

    internal static bool HasOrchestraErrors(List<OrchestraDiagnostic> diagnostics)
    {
        for (int index = 0; index < diagnostics.Count; index++)
        {
            if (diagnostics[index].Severity == ValidationSeverity.Error || diagnostics[index].Severity == ValidationSeverity.Fatal) return true;
        }
        return false;
    }
}
