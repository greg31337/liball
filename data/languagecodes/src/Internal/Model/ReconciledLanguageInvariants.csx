using System;
using System.Collections.Generic;

namespace languagecodes;

internal static class ReconciledLanguageInvariants
{
    internal static string[] FindViolations(ReconciledLanguageModel? model)
    {
        List<string> violations = new List<string>();
        if (model == null)
        {
            violations.Add("The reconciled model is null.");
            return violations.ToArray();
        }

        if (model.Entities == null)
        {
            violations.Add("The entity collection is null.");
            return violations.ToArray();
        }

        Dictionary<string, ReconciledLanguageEntity> entities = new Dictionary<string, ReconciledLanguageEntity>(StringComparer.Ordinal);
        Dictionary<string, string> codeOwners = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (ReconciledLanguageEntity? entity in model.Entities)
        {
            AddEntityViolations(entity, violations, entities, codeOwners);
        }

        AddRetirementTargetViolations(entities, violations);
        AddReservedRangeViolations(model.ReservedRanges, violations);
        AddRelationshipViolations(model.Relationships, entities, violations);
        AddIanaLinkViolations(model.IanaLinks, entities, violations);
        return violations.ToArray();
    }

    internal static void AddEntityViolations(
        ReconciledLanguageEntity? entity,
        List<string> violations,
        Dictionary<string, ReconciledLanguageEntity> entities,
        Dictionary<string, string> codeOwners)
    {
        if (entity == null)
        {
            violations.Add("The entity collection contains null.");
            return;
        }

        if (IsMissing(entity.Key))
        {
            violations.Add("An entity key is empty.");
            return;
        }

        string key = entity.Key ?? "";
        if (entities.ContainsKey(key))
        {
            violations.Add($"Entity key '{key}' is duplicated.");
            return;
        }

        entities.Add(key, entity);
        AddMembershipViolation(entity.InSet1, entity.Alpha2Code, "Set 1", key, violations);
        AddSet2MembershipViolation(entity, key, violations);
        AddMembershipViolation(entity.InSet3, entity.Set3Code, "Set 3", key, violations);
        AddMembershipViolation(entity.InSet5, entity.Set5Code, "Set 5", key, violations);

        if (!entity.InSet1 && !entity.InSet2 && !entity.InSet3 && !entity.InSet5)
        {
            violations.Add($"Entity '{key}' has no ISO 639 Set membership.");
        }

        AddCodeOwner(entity.Alpha2Code, "alpha-2", key, codeOwners, violations);
        AddCodeOwner(entity.Set2TerminologyCode, "Set 2 terminology", key, codeOwners, violations);
        AddCodeOwner(entity.Set2BibliographicCode, "Set 2 bibliographic", key, codeOwners, violations);
        AddCodeOwner(entity.Set3Code, "Set 3", key, codeOwners, violations);
        AddCodeOwner(entity.Set5Code, "Set 5", key, codeOwners, violations);

        if (entity.Scope == ReconciledLanguageScope.Special && entity.LanguageType != ReconciledLanguageType.Special)
        {
            violations.Add($"Special entity '{key}' does not have the special language type.");
        }

        if (entity.LanguageType == ReconciledLanguageType.Special && entity.Scope != ReconciledLanguageScope.Special)
        {
            violations.Add($"Entity '{key}' has the special language type without special scope.");
        }

        if (!entity.InSet5 && entity.GroupKind != ReconciledGroupKind.None)
        {
            violations.Add($"Entity '{key}' has a Set 5 group kind but is not in Set 5.");
        }

        if (entity.Status == ReconciledEntityStatus.Active && entity.Retirement != null)
        {
            violations.Add($"Active entity '{key}' has retirement metadata.");
        }

        if (entity.Status == ReconciledEntityStatus.Retired && entity.Retirement == null)
        {
            violations.Add($"Retired entity '{key}' has no retirement metadata.");
        }

        AddNameViolations(entity.Names, key, violations);
        AddAliasViolations(entity, key, violations);
        AddRetirementViolations(entity, key, violations);
    }

    internal static void AddMembershipViolation(
        bool membership,
        string? code,
        string setName,
        string key,
        List<string> violations)
    {
        if (membership && IsMissing(code))
        {
            violations.Add($"Entity '{key}' belongs to {setName} but has no {setName} code.");
        }

        if (!membership && !IsMissing(code))
        {
            violations.Add($"Entity '{key}' has a {setName} code but does not belong to {setName}.");
        }
    }

    internal static void AddSet2MembershipViolation(
        ReconciledLanguageEntity entity,
        string key,
        List<string> violations)
    {
        bool hasTerminology = !IsMissing(entity.Set2TerminologyCode);
        bool hasBibliographic = !IsMissing(entity.Set2BibliographicCode);
        if (entity.InSet2 && (!hasTerminology || !hasBibliographic))
        {
            violations.Add($"Entity '{key}' belongs to Set 2 but does not have both terminology and bibliographic code forms.");
        }

        if (!entity.InSet2 && (hasTerminology || hasBibliographic))
        {
            violations.Add($"Entity '{key}' has a Set 2 code but does not belong to Set 2.");
        }
    }

    internal static void AddCodeOwner(
        string? code,
        string form,
        string key,
        Dictionary<string, string> codeOwners,
        List<string> violations)
    {
        if (IsMissing(code))
        {
            return;
        }

        string ownerKey = form + ":" + code;
        if (codeOwners.ContainsKey(ownerKey))
        {
            violations.Add($"The {form} code '{code}' belongs to both '{codeOwners[ownerKey]}' and '{key}'.");
            return;
        }

        codeOwners.Add(ownerKey, key);
    }

    internal static void AddNameViolations(
        ReconciledLanguageName[]? names,
        string key,
        List<string> violations)
    {
        if (names == null || names.Length == 0)
        {
            violations.Add($"Entity '{key}' has no names.");
            return;
        }

        bool hasReferenceName = false;
        HashSet<string> seenNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (ReconciledLanguageName? name in names)
        {
            if (name == null)
            {
                violations.Add($"Entity '{key}' contains a null name.");
                continue;
            }

            if (IsMissing(name.Text))
            {
                violations.Add($"Entity '{key}' contains an empty name.");
                continue;
            }

            string nameKey = ((int)name.Source) + ":" + ((int)name.Kind) + ":" + name.NameLanguage + ":" + name.Text;
            if (!seenNames.Add(nameKey))
            {
                violations.Add($"Entity '{key}' contains a duplicate source name '{name.Text}'.");
            }

            if (name.IsReferenceName || name.Kind == ReconciledNameKind.Reference)
            {
                hasReferenceName = true;
            }
        }

        if (!hasReferenceName)
        {
            violations.Add($"Entity '{key}' has no reference name.");
        }
    }

    internal static void AddAliasViolations(
        ReconciledLanguageEntity entity,
        string key,
        List<string> violations)
    {
        if (entity.Aliases == null)
        {
            violations.Add($"Entity '{key}' has a null alias collection.");
            return;
        }

        HashSet<string> seenAliases = new HashSet<string>(StringComparer.Ordinal);
        foreach (ReconciledLanguageAlias? alias in entity.Aliases)
        {
            if (alias == null)
            {
                violations.Add($"Entity '{key}' contains a null alias.");
                continue;
            }

            if (IsMissing(alias.Value))
            {
                violations.Add($"Entity '{key}' contains an empty alias.");
                continue;
            }

            string aliasKey = ((int)alias.Kind) + ":" + alias.Value;
            if (!seenAliases.Add(aliasKey))
            {
                violations.Add($"Entity '{key}' contains a duplicate alias '{alias.Value}'.");
            }

            if (!IsMissing(alias.PreferredValue) && !IsEntityCode(entity, alias.PreferredValue ?? ""))
            {
                violations.Add($"Alias '{alias.Value}' on entity '{key}' prefers a code outside that entity.");
            }
        }
    }

    internal static void AddRetirementViolations(
        ReconciledLanguageEntity entity,
        string key,
        List<string> violations)
    {
        ReconciledLanguageRetirement? retirement = entity.Retirement;
        if (retirement == null)
        {
            return;
        }

        if (IsMissing(retirement.RetiredCode))
        {
            violations.Add($"Retirement metadata for '{key}' has no retired code.");
        }
        else if (!IsEntityCode(entity, retirement.RetiredCode ?? ""))
        {
            violations.Add($"Retirement metadata for '{key}' names a code outside that entity.");
        }

        if (IsMissing(retirement.FormerReferenceName))
        {
            violations.Add($"Retirement metadata for '{key}' has no former reference name.");
        }

        if (retirement.EffectiveDate == default(global::System.DateOnly))
        {
            violations.Add($"Retirement metadata for '{key}' has no effective date.");
        }

        if (retirement.ReplacementCodes == null)
        {
            violations.Add($"Retirement metadata for '{key}' has a null replacement collection.");
            return;
        }

        int replacementCount = retirement.ReplacementCodes.Length;
        if ((retirement.Reason == ReconciledRetirementReason.Change
                || retirement.Reason == ReconciledRetirementReason.Duplicate
                || retirement.Reason == ReconciledRetirementReason.Merge)
            && replacementCount != 1)
        {
            violations.Add($"Retirement metadata for '{key}' requires exactly one replacement.");
        }

        if (retirement.Reason == ReconciledRetirementReason.Nonexistent && replacementCount != 0)
        {
            violations.Add($"Nonexistent retirement metadata for '{key}' cannot have replacements.");
        }

        if (retirement.Reason == ReconciledRetirementReason.Split && IsMissing(retirement.Remedy))
        {
            violations.Add($"Split retirement metadata for '{key}' has no authoritative remedy.");
        }

        HashSet<string> seenReplacements = new HashSet<string>(StringComparer.Ordinal);
        foreach (string? replacementCode in retirement.ReplacementCodes)
        {
            if (IsMissing(replacementCode))
            {
                violations.Add($"Retirement metadata for '{key}' contains an empty replacement.");
            }
            else if (IsEntityCode(entity, replacementCode ?? ""))
            {
                violations.Add($"Retirement metadata for '{key}' replaces the entity with itself.");
            }
            else if (!seenReplacements.Add(replacementCode ?? ""))
            {
                violations.Add($"Retirement metadata for '{key}' contains duplicate replacement '{replacementCode}'.");
            }
        }
    }

    internal static void AddRetirementTargetViolations(
        Dictionary<string, ReconciledLanguageEntity> entities,
        List<string> violations)
    {
        foreach (KeyValuePair<string, ReconciledLanguageEntity> pair in entities)
        {
            ReconciledLanguageRetirement? retirement = pair.Value.Retirement;
            if (retirement == null || retirement.ReplacementCodes == null)
            {
                continue;
            }

            foreach (string? replacementCode in retirement.ReplacementCodes)
            {
                if (!IsMissing(replacementCode) && !ContainsEntityCode(entities, replacementCode ?? ""))
                {
                    violations.Add($"Retirement metadata for '{pair.Key}' points to unknown replacement '{replacementCode}'.");
                }
            }
        }
    }

    internal static void AddReservedRangeViolations(
        ReconciledReservedRange[]? ranges,
        List<string> violations)
    {
        if (ranges == null)
        {
            violations.Add("The reserved-range collection is null.");
            return;
        }

        foreach (ReconciledReservedRange? range in ranges)
        {
            if (range == null)
            {
                violations.Add("The reserved-range collection contains null.");
                continue;
            }

            if (IsMissing(range.FirstCode) || IsMissing(range.LastCode))
            {
                violations.Add("A reserved range has an empty boundary.");
            }
            else if (string.CompareOrdinal(range.FirstCode, range.LastCode) > 0)
            {
                violations.Add($"Reserved range '{range.FirstCode}' through '{range.LastCode}' is reversed.");
            }

            if (range.Source != ReconciledSource.Iso639Set2 && range.Source != ReconciledSource.Iana)
            {
                violations.Add("A reserved range has no Set 2 or IANA source.");
            }

            if (IsMissing(range.Purpose))
            {
                violations.Add("A reserved range has no documented purpose.");
            }
        }
    }

    internal static void AddRelationshipViolations(
        ReconciledRelationship[]? relationships,
        Dictionary<string, ReconciledLanguageEntity> entities,
        List<string> violations)
    {
        if (relationships == null)
        {
            violations.Add("The relationship collection is null.");
            return;
        }

        HashSet<string> seenRelationships = new HashSet<string>(StringComparer.Ordinal);
        foreach (ReconciledRelationship? relationship in relationships)
        {
            if (relationship == null)
            {
                violations.Add("The relationship collection contains null.");
                continue;
            }

            if (IsMissing(relationship.ParentKey) || IsMissing(relationship.ChildKey))
            {
                violations.Add("A relationship has an empty endpoint.");
                continue;
            }

            string parentKey = relationship.ParentKey ?? "";
            string childKey = relationship.ChildKey ?? "";
            if (parentKey == childKey)
            {
                violations.Add($"Relationship '{parentKey}' is self-referential.");
                continue;
            }

            string relationshipKey = ((int)relationship.Kind) + ":" + parentKey + ":" + childKey + ":" + ((int)relationship.Status);
            if (!seenRelationships.Add(relationshipKey))
            {
                violations.Add($"Relationship '{parentKey}' to '{childKey}' is duplicated.");
            }

            if (!entities.ContainsKey(parentKey) || !entities.ContainsKey(childKey))
            {
                violations.Add($"Relationship '{parentKey}' to '{childKey}' has a dangling endpoint.");
                continue;
            }

            ReconciledLanguageEntity parent = entities[parentKey];
            ReconciledLanguageEntity child = entities[childKey];
            if (relationship.Kind == ReconciledRelationshipKind.MacrolanguageMember
                && (parent.Scope != ReconciledLanguageScope.Macrolanguage || child.Scope != ReconciledLanguageScope.Individual))
            {
                violations.Add($"Macrolanguage relationship '{parentKey}' to '{childKey}' has incompatible endpoint scopes.");
            }

            if (relationship.Kind == ReconciledRelationshipKind.Set5ParentChild
                && (!parent.InSet5 || !child.InSet5))
            {
                violations.Add($"Set 5 relationship '{parentKey}' to '{childKey}' has an endpoint outside Set 5.");
            }
        }
    }

    internal static void AddIanaLinkViolations(
        ReconciledIanaLink[]? links,
        Dictionary<string, ReconciledLanguageEntity> entities,
        List<string> violations)
    {
        if (links == null)
        {
            violations.Add("The IANA-link collection is null.");
            return;
        }

        HashSet<string> seenLinks = new HashSet<string>(StringComparer.Ordinal);
        foreach (ReconciledIanaLink? link in links)
        {
            if (link == null)
            {
                violations.Add("The IANA-link collection contains null.");
                continue;
            }

            if (IsMissing(link.EntityKey) || IsMissing(link.Value))
            {
                violations.Add("An IANA link has an empty entity key or registry value.");
                continue;
            }

            string entityKey = link.EntityKey ?? "";
            if (!entities.ContainsKey(entityKey))
            {
                violations.Add($"IANA value '{link.Value}' links to unknown entity '{link.EntityKey}'.");
            }

            string linkKey = ((int)link.RecordType) + ":" + link.Value;
            if (!seenLinks.Add(linkKey))
            {
                violations.Add($"IANA value '{link.Value}' is linked more than once for its record type.");
            }

            if (link.Descriptions == null || link.Descriptions.Length == 0)
            {
                violations.Add($"IANA value '{link.Value}' has no descriptions.");
            }

            if (link.Prefixes == null)
            {
                violations.Add($"IANA value '{link.Value}' has a null prefix collection.");
            }
        }
    }

    internal static bool IsEntityCode(ReconciledLanguageEntity entity, string value)
    {
        return value == entity.Alpha2Code
            || value == entity.Set2TerminologyCode
            || value == entity.Set2BibliographicCode
            || value == entity.Set3Code
            || value == entity.Set5Code;
    }

    internal static bool ContainsEntityCode(
        Dictionary<string, ReconciledLanguageEntity> entities,
        string value)
    {
        foreach (KeyValuePair<string, ReconciledLanguageEntity> pair in entities)
        {
            if (IsEntityCode(pair.Value, value))
            {
                return true;
            }
        }

        return false;
    }

    internal static bool IsMissing(string? value)
    {
        return string.IsNullOrWhiteSpace(value);
    }
}
