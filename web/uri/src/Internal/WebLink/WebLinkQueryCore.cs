using System;
using System.Collections.Generic;

namespace uri;

internal static class WebLinkQueryCore
{
    internal const string InvalidQueryInputMessage = "A valid Web Link set and filter are required.";

    internal static WebLinkSet FilterWebLinks(
        WebLinkSet? linkSet,
        WebLinkFilter? filter)
    {
        ValidateQueryInputs(linkSet, filter);
        if (linkSet == null || filter == null)
        {
            throw new ArgumentException(InvalidQueryInputMessage);
        }

        List<WebLink> matches = new List<WebLink>();
        int index = 0;
        while (index < linkSet.Links.Length)
        {
            WebLink link = linkSet.Links[index];
            if (MatchesFilter(link, filter))
            {
                matches.Add(link);
            }

            index = index + 1;
        }

        return CreateFilteredSet(linkSet, matches);
    }

    internal static WebLinkSet FindLinksByRelation(
        WebLinkSet? linkSet,
        string? relation)
    {
        if (relation == null)
        {
            throw new ArgumentException(InvalidQueryInputMessage);
        }

        WebLinkFilter filter = new WebLinkFilter();
        filter.HasRelation = true;
        filter.Relation = relation;
        return FilterWebLinks(linkSet, filter);
    }

    internal static WebLinkSet FindLinksByTarget(
        WebLinkSet? linkSet,
        string? target)
    {
        if (target == null)
        {
            throw new ArgumentException(InvalidQueryInputMessage);
        }

        WebLinkFilter filter = new WebLinkFilter();
        filter.HasTarget = true;
        filter.Target = target;
        return FilterWebLinks(linkSet, filter);
    }

    internal static WebLinkSet FindLinksByContext(
        WebLinkSet? linkSet,
        string? context)
    {
        if (context == null)
        {
            throw new ArgumentException(InvalidQueryInputMessage);
        }

        WebLinkFilter filter = new WebLinkFilter();
        filter.HasContext = true;
        filter.Context = context;
        return FilterWebLinks(linkSet, filter);
    }

    internal static bool MatchesFilter(
        WebLink link,
        WebLinkFilter filter)
    {
        if (filter.HasRelation &&
            !MatchesRelation(link, filter.Relation))
        {
            return false;
        }

        if (filter.HasTarget &&
            !MatchesTarget(link, filter.Target))
        {
            return false;
        }

        if (filter.HasContext &&
            !MatchesContext(link, filter.Context))
        {
            return false;
        }

        if (filter.HasMediaType &&
            !MatchesMediaType(link, filter.MediaType))
        {
            return false;
        }

        if (filter.HasLanguage &&
            !MatchesLanguage(link, filter.Language))
        {
            return false;
        }

        if (filter.HasExtensionAttribute &&
            !MatchesExtensionAttribute(link, filter))
        {
            return false;
        }

        return true;
    }

    internal static bool MatchesRelation(WebLink link, string relation)
    {
        bool registered = WebLinkRelationCore.IsRegisteredRelation(relation);
        if (!registered)
        {
            UriParseResult parsed = UriParserCore.ParseAbsoluteUri(
                relation.AsSpan());
            if (!parsed.Success)
            {
                return false;
            }
        }

        int index = 0;
        while (index < link.Relations.Length)
        {
            WebLinkRelationKind expectedKind =
                WebLinkRelationKind.Extension;
            if (registered)
            {
                expectedKind = WebLinkRelationKind.Registered;
            }

            if (link.RelationKinds[index] == expectedKind &&
                AsciiCase.EqualsIgnoreCase(
                    link.Relations[index],
                    relation))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool MatchesTarget(WebLink link, string target)
    {
        return AreUriReferencesEquivalent(link.Target, target);
    }

    internal static bool MatchesContext(WebLink link, string context)
    {
        return link.HasAnchor &&
            AreUriReferencesEquivalent(link.Anchor, context);
    }

    internal static bool MatchesMediaType(
        WebLink link,
        string mediaType)
    {
        return link.HasType &&
            AsciiCase.EqualsIgnoreCase(link.Type, mediaType);
    }

    internal static bool MatchesLanguage(
        WebLink link,
        string language)
    {
        int index = 0;
        while (index < link.HrefLanguages.Length)
        {
            if (AsciiCase.EqualsIgnoreCase(
                link.HrefLanguages[index],
                language))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool MatchesExtensionAttribute(
        WebLink link,
        WebLinkFilter filter)
    {
        int index = 0;
        while (index < link.Parameters.Length)
        {
            WebLinkParameter parameter = link.Parameters[index];
            if (!parameter.Ignored &&
                parameter.Kind == WebLinkParameterKind.Extension &&
                AsciiCase.EqualsIgnoreCase(
                    parameter.Name,
                    filter.ExtensionAttributeName))
            {
                if (!filter.ExtensionAttributeHasValue)
                {
                    return true;
                }

                if (parameter.HasValue &&
                    parameter.Value == filter.ExtensionAttributeValue)
                {
                    return true;
                }
            }

            index = index + 1;
        }

        return false;
    }

    internal static bool AreUriReferencesEquivalent(
        string left,
        string right)
    {
        UriComparisonResult comparison = IriComparerCore.CompareAsUri(
            left.AsSpan(),
            right.AsSpan(),
            new OperationLimits());
        return comparison.Success && comparison.Equal;
    }

    internal static WebLinkSet CreateFilteredSet(
        WebLinkSet source,
        List<WebLink> links)
    {
        WebLinkSet result = new WebLinkSet();
        result.Links = links.ToArray();
        result.OriginalFieldValues = new string[
            source.OriginalFieldValues.Length];
        Array.Copy(
            source.OriginalFieldValues,
            result.OriginalFieldValues,
            source.OriginalFieldValues.Length);
        result.Diagnostics = new UriDiagnostic[source.Diagnostics.Length];
        Array.Copy(
            source.Diagnostics,
            result.Diagnostics,
            source.Diagnostics.Length);
        return result;
    }

    internal static void ValidateQueryInputs(
        WebLinkSet? linkSet,
        WebLinkFilter? filter)
    {
        if (linkSet == null || filter == null ||
            linkSet.Links == null ||
            linkSet.OriginalFieldValues == null ||
            linkSet.Diagnostics == null ||
            filter.Relation == null || filter.Target == null ||
            filter.Context == null || filter.MediaType == null ||
            filter.Language == null ||
            filter.ExtensionAttributeName == null ||
            filter.ExtensionAttributeValue == null)
        {
            throw new ArgumentException(InvalidQueryInputMessage);
        }

        int index = 0;
        while (index < linkSet.Links.Length)
        {
            ValidateLinkModel(linkSet.Links[index]);
            index = index + 1;
        }
    }

    internal static void ValidateLinkModel(WebLink? link)
    {
        if (link == null || link.Target == null || link.Anchor == null ||
            link.Relations == null || link.RelationKinds == null ||
            link.Relations.Length != link.RelationKinds.Length ||
            link.ReverseRelations == null ||
            link.ReverseRelationKinds == null ||
            link.ReverseRelations.Length !=
                link.ReverseRelationKinds.Length ||
            link.Parameters == null || link.HrefLanguages == null ||
            link.Type == null)
        {
            throw new ArgumentException(InvalidQueryInputMessage);
        }

        int index = 0;
        while (index < link.Relations.Length)
        {
            if (link.Relations[index] == null)
            {
                throw new ArgumentException(InvalidQueryInputMessage);
            }

            index = index + 1;
        }

        index = 0;
        while (index < link.HrefLanguages.Length)
        {
            if (link.HrefLanguages[index] == null)
            {
                throw new ArgumentException(InvalidQueryInputMessage);
            }

            index = index + 1;
        }

        index = 0;
        while (index < link.Parameters.Length)
        {
            WebLinkParameter? parameter = link.Parameters[index];
            if (parameter == null || parameter.Name == null ||
                parameter.Value == null)
            {
                throw new ArgumentException(InvalidQueryInputMessage);
            }

            index = index + 1;
        }
    }
}
