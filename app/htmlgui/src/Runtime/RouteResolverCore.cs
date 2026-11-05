using System;
using System.Collections.Generic;

namespace htmlgui;

internal class ResolvedRouteDefinition
{
    internal UiRoute Route = new UiRoute();
    internal UiRoute EffectiveRoute = new UiRoute();
    internal UiRoute[] Chain = Array.Empty<UiRoute>();
    internal int Depth;
}

internal class RouteDefinitionResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal ResolvedRouteDefinition[] Definitions = Array.Empty<ResolvedRouteDefinition>();
}

internal class RouteViewResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal UiView? View;
}

internal class RouteOutletResult
{
    internal UiNode Node = new UiNode();
    internal int MatchCount;
}

internal class RouteResolutionResult
{
    internal bool Success;
    internal string Code = string.Empty;
    internal UiNavigationStatus Status;
    internal string CanonicalUrl = string.Empty;
    internal string? RedirectUrl;
    internal UiRouteData? RouteData;
    internal UiRoute[] RouteChain = Array.Empty<UiRoute>();
    internal UiView? View;
    internal UiPage? Page;
    internal string Title = string.Empty;
    internal UiDocumentMetadata? Metadata;
}

internal class RouteResolverCore
{
    internal const string DefaultRouteOutletName = "outlet";

    internal RouteCore UrlCore = new RouteCore();
    internal RouteTemplateCore TemplateCore = new RouteTemplateCore();

    internal RouteResolutionResult ResolveRouteTree(UiRoute[]? routes, UiPage[]? pages, UiDocumentMetadata? document, string? url, string? basePath)
    {
        RouteUrlResult parsedUrl = UrlCore.ParseRouteUrl(url, basePath);
        if (!parsedUrl.Success)
        {
            return CreateRouteResolutionFailure(parsedUrl.Code);
        }

        if (parsedUrl.IsExternal)
        {
            RouteResolutionResult external = new RouteResolutionResult();
            external.Success = true;
            external.Status = UiNavigationStatus.External;
            external.CanonicalUrl = parsedUrl.CanonicalUrl;
            ApplyRouteDocumentSelection(external, pages, document, Array.Empty<UiRoute>());
            return external;
        }

        RouteDefinitionResult prepared = PrepareRouteDefinitions(routes);
        if (!prepared.Success)
        {
            return CreateRouteResolutionFailure(prepared.Code);
        }

        ResolvedRouteDefinition? matchedDefinition = null;
        RouteMatchResult? matchedRoute = null;
        int definitionIndex = 0;
        while (definitionIndex < prepared.Definitions.Length)
        {
            ResolvedRouteDefinition definition = prepared.Definitions[definitionIndex];
            if (!definition.Route.IsNotFound)
            {
                RouteMatchResult candidate = TemplateCore.MatchRouteTemplate(definition.EffectiveRoute, parsedUrl);
                if (!candidate.Success)
                {
                    return CreateRouteResolutionFailure(candidate.Code);
                }

                if (candidate.Matched)
                {
                    matchedDefinition = definition;
                    matchedRoute = candidate;
                    break;
                }
            }

            definitionIndex++;
        }

        UiNavigationStatus status = UiNavigationStatus.Resolved;
        if (matchedDefinition == null || matchedRoute == null || matchedRoute.RouteData == null)
        {
            status = UiNavigationStatus.NotFound;
            matchedDefinition = FindNestedNotFoundRoute(prepared.Definitions, parsedUrl);
            if (matchedDefinition == null)
            {
                RouteResolutionResult notFound = new RouteResolutionResult();
                notFound.Success = true;
                notFound.Status = UiNavigationStatus.NotFound;
                notFound.CanonicalUrl = parsedUrl.CanonicalUrl;
                ApplyRouteDocumentSelection(notFound, pages, document, Array.Empty<UiRoute>());
                return notFound;
            }

            matchedRoute = MatchNotFoundRoute(matchedDefinition, prepared.Definitions, parsedUrl);
            if (!matchedRoute.Success || matchedRoute.RouteData == null)
            {
                return CreateRouteResolutionFailure(matchedRoute.Code);
            }
        }

        RouteResolutionResult result = new RouteResolutionResult();
        result.Success = true;
        result.Status = status;
        result.CanonicalUrl = parsedUrl.CanonicalUrl;
        result.RouteData = matchedRoute.RouteData;
        result.RouteChain = CopyResolvedRouteChain(matchedDefinition.Chain);
        if (matchedDefinition.Route.RedirectUrl != null)
        {
            RouteUrlResult redirect = UrlCore.ParseRouteUrl(matchedDefinition.Route.RedirectUrl, parsedUrl.BasePath);
            if (!redirect.Success)
            {
                return CreateRouteResolutionFailure("route.invalid-redirect");
            }

            result.Status = UiNavigationStatus.Redirected;
            result.RedirectUrl = redirect.CanonicalUrl;
        }
        else
        {
            RouteViewResult composed = ComposeRouteViews(matchedDefinition.Chain);
            if (!composed.Success)
            {
                return CreateRouteResolutionFailure(composed.Code);
            }

            result.View = composed.View;
        }

        ApplyRouteDocumentSelection(result, pages, document, matchedDefinition.Chain);
        return result;
    }

    internal RouteDefinitionResult PrepareRouteDefinitions(UiRoute[]? routes)
    {
        if (routes == null)
        {
            return CreateRouteDefinitionFailure("route.null-routes");
        }

        List<UiRoute> parsedRoutes = new List<UiRoute>();
        int index = 0;
        while (index < routes.Length)
        {
            UiRoute? source = routes[index];
            if (source == null)
            {
                return CreateRouteDefinitionFailure("route.null-route");
            }

            if (string.IsNullOrWhiteSpace(source.Id))
            {
                return CreateRouteDefinitionFailure("route.invalid-id");
            }

            if (FindParsedRouteIndex(parsedRoutes, source.Id) >= 0)
            {
                return CreateRouteDefinitionFailure("route.duplicate-id");
            }

            if (source.IsIndex && source.IsNotFound)
            {
                return CreateRouteDefinitionFailure("route.conflicting-route-kind");
            }

            if (source.OutletName != null && string.IsNullOrWhiteSpace(source.OutletName))
            {
                return CreateRouteDefinitionFailure("route.invalid-outlet-name");
            }

            if (source.RedirectUrl != null && string.IsNullOrWhiteSpace(source.RedirectUrl))
            {
                return CreateRouteDefinitionFailure("route.invalid-redirect");
            }

            if (source.RedirectUrl != null && source.View != null)
            {
                return CreateRouteDefinitionFailure("route.multiple-destinations");
            }

            RouteTemplateResult parsed = TemplateCore.ParseRouteTemplate(source.Template);
            if (!parsed.Success)
            {
                return CreateRouteDefinitionFailure(parsed.Code);
            }

            if ((source.IsIndex || source.IsNotFound) && parsed.Segments.Length != 0)
            {
                return CreateRouteDefinitionFailure(source.IsIndex ? "route.index-has-template" : "route.not-found-has-template");
            }

            if (source.IsIndex && source.ParentId == null)
            {
                return CreateRouteDefinitionFailure("route.index-without-parent");
            }

            parsedRoutes.Add(TemplateCore.CopyParsedRoute(source, parsed));
            index++;
        }

        List<ResolvedRouteDefinition> definitions = new List<ResolvedRouteDefinition>();
        index = 0;
        while (index < parsedRoutes.Count)
        {
            RouteDefinitionResult built = BuildResolvedRouteDefinition(parsedRoutes, index);
            if (!built.Success || built.Definitions.Length != 1)
            {
                return built;
            }

            ResolvedRouteDefinition definition = built.Definitions[0];
            int existingIndex = 0;
            while (existingIndex < definitions.Count)
            {
                ResolvedRouteDefinition existing = definitions[existingIndex];
                if (definition.Route.IsNotFound && existing.Route.IsNotFound && ValueComparer.TextEquals(definition.Route.ParentId, existing.Route.ParentId))
                {
                    return CreateRouteDefinitionFailure("route.duplicate-not-found");
                }

                if (!definition.Route.IsNotFound && !existing.Route.IsNotFound && TemplateCore.HaveAmbiguousRouteTemplates(definition.EffectiveRoute.Segments, existing.EffectiveRoute.Segments) && !IsAllowedIndexRouteOverlap(definition, existing))
                {
                    return CreateRouteDefinitionFailure("route.ambiguous");
                }

                existingIndex++;
            }

            InsertResolvedRouteByPrecedence(definitions, definition);
            index++;
        }

        RouteDefinitionResult result = new RouteDefinitionResult();
        result.Success = true;
        result.Definitions = definitions.ToArray();
        return result;
    }

    internal RouteDefinitionResult BuildResolvedRouteDefinition(List<UiRoute> routes, int routeIndex)
    {
        List<UiRoute> reverseChain = new List<UiRoute>();
        UiRoute current = routes[routeIndex];
        int steps = 0;
        while (true)
        {
            reverseChain.Add(current);
            if (current.ParentId == null)
            {
                break;
            }

            int parentIndex = FindParsedRouteIndex(routes, current.ParentId);
            if (parentIndex < 0)
            {
                return CreateRouteDefinitionFailure("route.unknown-parent");
            }

            UiRoute parent = routes[parentIndex];
            if (parent.IsIndex || parent.IsNotFound)
            {
                return CreateRouteDefinitionFailure("route.invalid-parent-kind");
            }

            current = parent;
            steps++;
            if (steps >= routes.Count)
            {
                return CreateRouteDefinitionFailure("route.parent-cycle");
            }
        }

        UiRoute[] chain = new UiRoute[reverseChain.Count];
        int reverseIndex = reverseChain.Count - 1;
        while (reverseIndex >= 0)
        {
            chain[reverseChain.Count - reverseIndex - 1] = reverseChain[reverseIndex];
            reverseIndex--;
        }

        List<UiRouteSegment> effectiveSegments = new List<UiRouteSegment>();
        int chainIndex = 0;
        while (chainIndex < chain.Length)
        {
            int segmentIndex = 0;
            while (segmentIndex < chain[chainIndex].Segments.Length)
            {
                UiRouteSegment segment = chain[chainIndex].Segments[segmentIndex];
                if (!ValueComparer.TextEquals(segment.Kind, RouteTemplateCore.LiteralSegment) && ContainsResolvedParameter(effectiveSegments, segment.Value))
                {
                    return CreateRouteDefinitionFailure("route.duplicate-parameter");
                }

                effectiveSegments.Add(segment);
                segmentIndex++;
            }

            chainIndex++;
        }

        UiRoute leaf = routes[routeIndex];
        UiRoute effective = new UiRoute();
        effective.Id = leaf.Id;
        effective.Segments = TemplateCore.CopyRouteTemplateSegments(effectiveSegments.ToArray());
        effective.Template = TemplateCore.BuildCanonicalRouteTemplate(effective.Segments);
        effective.ParentId = leaf.ParentId;
        effective.View = leaf.View;
        effective.OutletName = leaf.OutletName;
        effective.RedirectUrl = leaf.RedirectUrl;
        effective.IsIndex = leaf.IsIndex;
        effective.IsNotFound = leaf.IsNotFound;

        ResolvedRouteDefinition definition = new ResolvedRouteDefinition();
        definition.Route = leaf;
        definition.EffectiveRoute = effective;
        definition.Chain = chain;
        definition.Depth = chain.Length;
        RouteDefinitionResult result = new RouteDefinitionResult();
        result.Success = true;
        result.Definitions = new ResolvedRouteDefinition[] { definition };
        return result;
    }

    internal ResolvedRouteDefinition? FindNestedNotFoundRoute(ResolvedRouteDefinition[] definitions, RouteUrlResult url)
    {
        ResolvedRouteDefinition? selected = null;
        int index = 0;
        while (index < definitions.Length)
        {
            ResolvedRouteDefinition candidate = definitions[index];
            if (candidate.Route.IsNotFound && IsNotFoundParentPrefix(candidate, definitions, url) && (selected == null || candidate.Depth > selected.Depth))
            {
                selected = candidate;
            }

            index++;
        }

        return selected;
    }

    internal bool IsNotFoundParentPrefix(ResolvedRouteDefinition notFound, ResolvedRouteDefinition[] definitions, RouteUrlResult url)
    {
        if (notFound.Route.ParentId == null)
        {
            return true;
        }

        ResolvedRouteDefinition? parent = FindResolvedRouteDefinition(definitions, notFound.Route.ParentId);
        if (parent == null)
        {
            return false;
        }

        return MatchRoutePrefix(parent.EffectiveRoute, url).Matched;
    }

    internal RouteMatchResult MatchNotFoundRoute(ResolvedRouteDefinition notFound, ResolvedRouteDefinition[] definitions, RouteUrlResult url)
    {
        RouteMatchResult matched;
        if (notFound.Route.ParentId == null)
        {
            matched = new RouteMatchResult();
            matched.Success = true;
            matched.Matched = true;
            matched.RouteData = new UiRouteData();
            matched.RouteData.Query = TemplateCore.CopyRouteQueryEntries(url.Query);
            matched.RouteData.Fragment = url.Fragment;
        }
        else
        {
            ResolvedRouteDefinition? parent = FindResolvedRouteDefinition(definitions, notFound.Route.ParentId);
            if (parent == null)
            {
                return CreateRouteMatchFailureForResolver("route.unknown-parent");
            }

            matched = MatchRoutePrefix(parent.EffectiveRoute, url);
        }

        if (matched.RouteData != null)
        {
            matched.RouteData.RouteId = notFound.Route.Id;
        }

        return matched;
    }

    internal RouteMatchResult MatchRoutePrefix(UiRoute route, RouteUrlResult url)
    {
        int length = url.PathSegments.Length;
        while (length >= 0)
        {
            RouteUrlResult prefix = new RouteUrlResult();
            prefix.Success = true;
            prefix.BasePath = url.BasePath;
            prefix.PathSegments = CopyRouteSegmentPrefix(url.PathSegments, length);
            prefix.Query = url.Query;
            prefix.Fragment = url.Fragment;
            RouteMatchResult matched = TemplateCore.MatchRouteTemplate(route, prefix);
            if (!matched.Success || matched.Matched)
            {
                return matched;
            }

            length--;
        }

        return new RouteMatchResult { Success = true };
    }

    internal RouteViewResult ComposeRouteViews(UiRoute[] chain)
    {
        UiView? composed = null;
        string pendingOutlet = DefaultRouteOutletName;
        int index = chain.Length - 1;
        while (index >= 0)
        {
            UiRoute route = chain[index];
            if (route.View != null)
            {
                UiView current = CopyResolvedRouteView(route.View);
                if (composed != null)
                {
                    if (current.ContentRoot == null || composed.ContentRoot == null)
                    {
                        return CreateRouteViewFailure("route.empty-layout-view");
                    }

                    RouteOutletResult outlet = FillRouteOutletNode(current.ContentRoot, pendingOutlet, composed.ContentRoot);
                    if (outlet.MatchCount == 0)
                    {
                        return CreateRouteViewFailure("route.outlet-not-found");
                    }

                    if (outlet.MatchCount > 1)
                    {
                        return CreateRouteViewFailure("route.duplicate-outlet");
                    }

                    current.ContentRoot = outlet.Node;
                }

                composed = current;
            }

            pendingOutlet = route.OutletName ?? DefaultRouteOutletName;
            index--;
        }

        RouteViewResult result = new RouteViewResult();
        result.Success = true;
        result.View = composed;
        return result;
    }

    internal RouteOutletResult FillRouteOutletNode(UiNode node, string outletName, UiNode content)
    {
        RouteOutletResult result = new RouteOutletResult();
        if (node.Kind == UiNodeKind.Slot && ValueComparer.TextEquals(node.Name, outletName))
        {
            result.Node = content;
            result.MatchCount = 1;
            return result;
        }

        result.Node = node;
        result.MatchCount += FillRouteOutletArray(node.Children, outletName, content);
        if (node.Layout != null)
        {
            result.MatchCount += FillRouteOutletArray(node.Layout.Children, outletName, content);
        }

        if (node.Conditional != null)
        {
            result.MatchCount += FillRouteOutletArray(node.Conditional.WhenTrue, outletName, content);
            result.MatchCount += FillRouteOutletArray(node.Conditional.WhenFalse, outletName, content);
        }

        if (node.Portal != null)
        {
            result.MatchCount += FillRouteOutletArray(node.Portal.Content, outletName, content);
        }

        int slotIndex = 0;
        while (slotIndex < node.Slots.Length)
        {
            result.MatchCount += FillRouteOutletArray(node.Slots[slotIndex].Content, outletName, content);
            result.MatchCount += FillRouteOutletArray(node.Slots[slotIndex].FallbackContent, outletName, content);
            slotIndex++;
        }

        if (node.Repeat != null)
        {
            int itemIndex = 0;
            while (itemIndex < node.Repeat.Items.Length)
            {
                UiNode? itemContent = node.Repeat.Items[itemIndex].Content;
                if (itemContent != null)
                {
                    RouteOutletResult item = FillRouteOutletNode(itemContent, outletName, content);
                    node.Repeat.Items[itemIndex].Content = item.Node;
                    result.MatchCount += item.MatchCount;
                }

                itemIndex++;
            }
        }

        return result;
    }

    internal int FillRouteOutletArray(UiNode[] nodes, string outletName, UiNode content)
    {
        int matches = 0;
        int index = 0;
        while (index < nodes.Length)
        {
            RouteOutletResult result = FillRouteOutletNode(nodes[index], outletName, content);
            nodes[index] = result.Node;
            matches += result.MatchCount;
            index++;
        }

        return matches;
    }

    internal void ApplyRouteDocumentSelection(RouteResolutionResult result, UiPage[]? pages, UiDocumentMetadata? document, UiRoute[] chain)
    {
        UiDocumentMetadata inherited = CopyResolvedRouteMetadata(document ?? new UiDocumentMetadata());
        UiPage? selected = FindResolvedRoutePage(pages, chain);
        if (selected == null)
        {
            result.Metadata = inherited;
            result.Title = inherited.Title;
            return;
        }

        UiPage page = CopyResolvedRoutePage(selected);
        new ModelNormalizer().ApplyPageInheritance(inherited, page);
        result.Page = page;
        result.Metadata = page.Metadata;
        result.Title = page.Title;
    }

    internal UiPage? FindResolvedRoutePage(UiPage[]? pages, UiRoute[] chain)
    {
        UiPage[] supplied = pages ?? Array.Empty<UiPage>();
        int chainIndex = chain.Length - 1;
        while (chainIndex >= 0)
        {
            int pageIndex = 0;
            while (pageIndex < supplied.Length)
            {
                UiPage? page = supplied[pageIndex];
                if (page != null && page.RouteId != null && ValueComparer.TextEquals(page.RouteId, chain[chainIndex].Id))
                {
                    return page;
                }

                pageIndex++;
            }

            chainIndex--;
        }

        return null;
    }

    internal UiView CopyResolvedRouteView(UiView source)
    {
        object? copied = new ModelNormalizer().CopyValue(source);
        return copied == null ? new UiView() : (UiView)copied;
    }

    internal UiPage CopyResolvedRoutePage(UiPage source)
    {
        object? copied = new ModelNormalizer().CopyValue(source);
        return copied == null ? new UiPage() : (UiPage)copied;
    }

    internal UiDocumentMetadata CopyResolvedRouteMetadata(UiDocumentMetadata source)
    {
        object? copied = new ModelNormalizer().CopyValue(source);
        return copied == null ? new UiDocumentMetadata() : (UiDocumentMetadata)copied;
    }

    internal UiRoute[] CopyResolvedRouteChain(UiRoute[] source)
    {
        UiRoute[] result = new UiRoute[source.Length];
        int index = 0;
        while (index < source.Length)
        {
            RouteTemplateResult parsed = TemplateCore.ParseRouteTemplate(source[index].Template);
            result[index] = TemplateCore.CopyParsedRoute(source[index], parsed);
            index++;
        }

        return result;
    }

    internal int FindParsedRouteIndex(List<UiRoute> routes, string routeId)
    {
        int index = 0;
        while (index < routes.Count)
        {
            if (ValueComparer.TextEquals(routes[index].Id, routeId))
            {
                return index;
            }

            index++;
        }

        return -1;
    }

    internal ResolvedRouteDefinition? FindResolvedRouteDefinition(ResolvedRouteDefinition[] definitions, string routeId)
    {
        int index = 0;
        while (index < definitions.Length)
        {
            if (ValueComparer.TextEquals(definitions[index].Route.Id, routeId))
            {
                return definitions[index];
            }

            index++;
        }

        return null;
    }

    internal bool ContainsResolvedParameter(List<UiRouteSegment> segments, string parameterName)
    {
        int index = 0;
        while (index < segments.Count)
        {
            if (!ValueComparer.TextEquals(segments[index].Kind, RouteTemplateCore.LiteralSegment) && ValueComparer.TextEquals(segments[index].Value, parameterName))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal bool IsAllowedIndexRouteOverlap(ResolvedRouteDefinition first, ResolvedRouteDefinition second)
    {
        if (first.Route.IsIndex && first.Route.ParentId != null && ValueComparer.TextEquals(first.Route.ParentId, second.Route.Id))
        {
            return true;
        }

        return second.Route.IsIndex && second.Route.ParentId != null && ValueComparer.TextEquals(second.Route.ParentId, first.Route.Id);
    }

    internal int CompareResolvedRoutePrecedence(ResolvedRouteDefinition left, ResolvedRouteDefinition right)
    {
        int template = TemplateCore.CompareRoutePrecedence(left.EffectiveRoute, right.EffectiveRoute);
        if (TemplateCore.HaveAmbiguousRouteTemplates(left.EffectiveRoute.Segments, right.EffectiveRoute.Segments))
        {
            if (left.Route.IsIndex != right.Route.IsIndex)
            {
                return left.Route.IsIndex ? -1 : 1;
            }

            if (left.Depth != right.Depth)
            {
                return right.Depth - left.Depth;
            }
        }

        return template;
    }

    internal void InsertResolvedRouteByPrecedence(List<ResolvedRouteDefinition> definitions, ResolvedRouteDefinition definition)
    {
        int index = definitions.Count;
        definitions.Add(definition);
        while (index > 0 && CompareResolvedRoutePrecedence(definition, definitions[index - 1]) < 0)
        {
            definitions[index] = definitions[index - 1];
            index--;
        }

        definitions[index] = definition;
    }

    internal string[] CopyRouteSegmentPrefix(string[] source, int length)
    {
        string[] result = new string[length];
        int index = 0;
        while (index < length)
        {
            result[index] = source[index];
            index++;
        }

        return result;
    }

    internal RouteResolutionResult CreateRouteResolutionFailure(string code)
    {
        RouteResolutionResult result = new RouteResolutionResult();
        result.Code = code;
        result.Status = UiNavigationStatus.Failed;
        return result;
    }

    internal RouteDefinitionResult CreateRouteDefinitionFailure(string code)
    {
        return new RouteDefinitionResult { Code = code };
    }

    internal RouteViewResult CreateRouteViewFailure(string code)
    {
        return new RouteViewResult { Code = code };
    }

    internal RouteMatchResult CreateRouteMatchFailureForResolver(string code)
    {
        return new RouteMatchResult { Code = code };
    }
}
