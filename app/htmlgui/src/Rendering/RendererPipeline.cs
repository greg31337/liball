using System;
using System.Collections.Generic;

namespace htmlgui;

internal class RendererPreparation
{
    internal bool Success;
    internal UiApplication? Application;
    internal UiWindow? Window;
    internal UiPage? Page;
    internal UiDiagnostic[] Diagnostics = Array.Empty<UiDiagnostic>();
    internal int MaximumDiagnostics = 100;
}

internal class RendererStyleScope
{
    internal string OwnerId = string.Empty;
    internal string ScopeId = string.Empty;
}

internal class RendererStyleOutput
{
    internal bool Success;
    internal UiCssCompilation Compilation = new UiCssCompilation();
    internal UiDiagnostic[] Diagnostics = Array.Empty<UiDiagnostic>();
    internal RendererStyleScope[] Scopes = Array.Empty<RendererStyleScope>();
    internal string PageScopeId = string.Empty;
}

internal class RendererPipeline
{
    internal IUiModelValidator Validator;

    internal RendererPipeline()
    {
        Validator = new UiModelValidator();
    }

    internal UiRenderResult RenderPage(UiApplication? application, bool fullDocument)
    {
        RendererPreparation preparation = PrepareApplication(application);
        if (!preparation.Success || preparation.Application == null || preparation.Window == null || preparation.Page == null)
        {
            return CreateFailureResult(preparation.Diagnostics);
        }

        UiApplication normalized = preparation.Application;
        UiWindow window = preparation.Window;
        UiPage page = preparation.Page;
        UiRenderOptions options = CloneRenderOptions(normalized.RenderOptions, fullDocument);
        RendererStyleOutput styleOutput = CompileStyles(normalized, page, preparation.MaximumDiagnostics);
        UiDiagnostic[] diagnostics = MergeDiagnostics(preparation.Diagnostics, styleOutput.Diagnostics, preparation.MaximumDiagnostics);
        if (!styleOutput.Success || HasDiagnosticErrors(diagnostics))
        {
            UiRenderResult styleFailure = CreateFailureResult(diagnostics);
            styleFailure.Resources = CloneResources(page.Resources);
            styleFailure.Hash = CreateOutputHash(styleFailure);
            return styleFailure;
        }

        HtmlOutputComposer composer = new HtmlOutputComposer();
        UiHydrationManifest? hydration = null;
        if (options.IncludeHydration)
        {
            UiRenderOptions parityOptions = CloneRenderOptions(options, fullDocument);
            parityOptions.IncludeHydration = false;
            parityOptions.IncludeBootstrap = false;
            HtmlOutputComposition parityOutput = composer.ComposePageOutput(window, page, styleOutput.Compilation, null, parityOptions);
            ApplyStyleScopes(parityOutput.Root, styleOutput.Scopes, styleOutput.PageScopeId, fullDocument);
            RenderTreeAnnotationResult parityTree = new RenderTreeAnnotator().AnnotateTree(new DomNode[] { parityOutput.Root }, normalized.RootSeed, preparation.MaximumDiagnostics);
            diagnostics = MergeDiagnostics(diagnostics, parityTree.Diagnostics, preparation.MaximumDiagnostics);
            if (!parityTree.Success || HasDiagnosticErrors(diagnostics))
            {
                UiRenderResult parityFailure = CreateFailureResult(diagnostics);
                parityFailure.Resources = CloneResources(page.Resources);
                parityFailure.Hash = CreateOutputHash(parityFailure);
                return parityFailure;
            }

            hydration = CreateHydrationManifest(normalized, window, page, parityTree);
        }

        HtmlOutputComposition output = composer.ComposePageOutput(window, page, styleOutput.Compilation, hydration, options);
        ApplyStyleScopes(output.Root, styleOutput.Scopes, styleOutput.PageScopeId, fullDocument);
        RenderTreeAnnotationResult annotated = new RenderTreeAnnotator().AnnotateTree(new DomNode[] { output.Root }, normalized.RootSeed, preparation.MaximumDiagnostics);
        diagnostics = MergeDiagnostics(diagnostics, annotated.Diagnostics, preparation.MaximumDiagnostics);
        UiRenderResult result = new UiRenderResult();
        result.Resources = CloneResources(page.Resources);
        result.Diagnostics = diagnostics;
        if (annotated.Success && !HasDiagnosticErrors(diagnostics))
        {
            result.Html = new HtmlSerializer().SerializeNode(annotated.Roots[0]);
            result.Css = output.Css;
            result.Bootstrap = output.Bootstrap;
            result.Snapshot = CreateSnapshot(normalized, window, page, annotated);
            result.Hydration = hydration;
        }

        result.Hash = CreateOutputHash(result);
        return result;
    }

    internal UiRenderResult RenderStylesOnly(UiApplication? application)
    {
        RendererPreparation preparation = PrepareApplication(application);
        if (!preparation.Success || preparation.Application == null || preparation.Page == null)
        {
            return CreateFailureResult(preparation.Diagnostics);
        }

        RendererStyleOutput styleOutput = CompileStyles(preparation.Application, preparation.Page, preparation.MaximumDiagnostics);
        UiDiagnostic[] diagnostics = MergeDiagnostics(preparation.Diagnostics, styleOutput.Diagnostics, preparation.MaximumDiagnostics);
        UiRenderResult result = new UiRenderResult();
        result.Diagnostics = diagnostics;
        result.Resources = CloneResources(preparation.Page.Resources);
        if (styleOutput.Success && !HasDiagnosticErrors(diagnostics))
        {
            result.Css = styleOutput.Compilation.Css;
        }

        result.Hash = CreateOutputHash(result);
        return result;
    }

    internal UiRenderResult RenderBootstrapOnly()
    {
        UiRenderResult result = new UiRenderResult();
        result.Bootstrap = new HtmlOutputComposer().LoadBootstrapAsset();
        result.Hash = CreateOutputHash(result);
        return result;
    }

    internal RendererPreparation PrepareApplication(UiApplication? application)
    {
        RendererPreparation result = new RendererPreparation();
        result.MaximumDiagnostics = ResolveMaximumDiagnostics(application);
        UiResult validation = Validator.TryValidateModel(application);
        result.Diagnostics = CopyDiagnostics(validation.Diagnostics, result.MaximumDiagnostics);
        if (!validation.Success || application == null)
        {
            return result;
        }

        UiApplication normalized = new ModelNormalizer().NormalizeApplication(application);
        result.Application = normalized;
        result.Window = SelectWindow(normalized);
        if (result.Window == null)
        {
            result.Diagnostics = AppendDiagnostic(result.Diagnostics, CreateDiagnostic("render.no-window", "The application does not contain an active renderable window.", "/application/windows"), result.MaximumDiagnostics);
            return result;
        }

        result.Page = SelectPage(result.Window);
        if (result.Page == null)
        {
            string code = "render.no-page";
            string message = "The selected window does not contain a renderable page.";
            string location = "/application/windows/" + result.Window.Key + "/pages";
            if (result.Window.CurrentPageKey != null && result.Window.CurrentPageKey.Length > 0)
            {
                code = "render.current-page-not-found";
                message = "The selected window's current page key does not identify a page.";
                location = "/application/windows/" + result.Window.Key + "/currentPageKey";
            }

            result.Diagnostics = AppendDiagnostic(result.Diagnostics, CreateDiagnostic(code, message, location), result.MaximumDiagnostics);
            return result;
        }

        result.Success = !HasDiagnosticErrors(result.Diagnostics);
        return result;
    }

    internal int ResolveMaximumDiagnostics(UiApplication? application)
    {
        if (application != null && application.RenderOptions != null && application.RenderOptions.MaximumDiagnostics > 0)
        {
            return application.RenderOptions.MaximumDiagnostics;
        }

        return 100;
    }

    internal UiWindow? SelectWindow(UiApplication application)
    {
        UiWindow[] windows = application.Windows ?? Array.Empty<UiWindow>();
        int index = 0;
        while (index < windows.Length)
        {
            UiWindow? window = windows[index];
            if (window != null && window.IsActive)
            {
                return window;
            }

            index++;
        }

        return null;
    }

    internal UiPage? SelectPage(UiWindow window)
    {
        UiPage[] pages = window.Pages ?? Array.Empty<UiPage>();
        if (window.CurrentPageKey != null && window.CurrentPageKey.Length > 0)
        {
            int selectedIndex = 0;
            while (selectedIndex < pages.Length)
            {
                UiPage? selected = pages[selectedIndex];
                if (selected != null && string.Equals(selected.Key, window.CurrentPageKey, StringComparison.Ordinal))
                {
                    return selected;
                }

                selectedIndex++;
            }

            return null;
        }

        int index = 0;
        while (index < pages.Length)
        {
            if (pages[index] != null)
            {
                return pages[index];
            }

            index++;
        }

        return null;
    }

    internal UiRenderOptions CloneRenderOptions(UiRenderOptions? source, bool fullDocument)
    {
        UiRenderOptions options = source ?? new UiRenderOptions();
        return new UiRenderOptions
        {
            FullDocument = fullDocument,
            IncludeHydration = options.IncludeHydration,
            IncludeBootstrap = options.IncludeBootstrap,
            ContentSecurityPolicyNonce = options.ContentSecurityPolicyNonce,
            MaximumDiagnostics = options.MaximumDiagnostics
        };
    }

    internal RendererStyleOutput CompileStyles(UiApplication application, UiPage page, int maximumDiagnostics)
    {
        UiCssCompiler compiler = new UiCssCompiler();
        List<UiCssRule> rules = new List<UiCssRule>();
        List<UiDiagnostic> diagnostics = new List<UiDiagnostic>();
        List<RendererStyleScope> scopes = new List<RendererStyleScope>();
        RendererStyleOutput result = new RendererStyleOutput();
        if (application.Theme != null)
        {
            UiCssCompilation theme = compiler.ResolveTheme(application.Theme, UiThemeMode.Auto, null, null);
            AppendRules(rules, theme.Rules);
            AppendDiagnosticList(diagnostics, theme.Diagnostics, maximumDiagnostics);
        }

        if (page.Styles.Length > 0)
        {
            UiCssCompilation pageStyles = compiler.GenerateScopedRules(application.RootSeed, page.Id, null, null, page.Styles, null, null, null, null);
            AppendRules(rules, pageStyles.Rules);
            AppendDiagnosticList(diagnostics, pageStyles.Diagnostics, maximumDiagnostics);
            result.PageScopeId = pageStyles.ScopeId;
        }

        ModelWalker walker = new ModelWalker(maximumDiagnostics);
        walker.ValidateApplication(application);
        int nodeIndex = 0;
        while (nodeIndex < walker.Nodes.Count)
        {
            UiNode node = walker.Nodes[nodeIndex];
            if (node.Style != null)
            {
                UiCssCompilation nodeStyles = compiler.GenerateScopedRules(application.RootSeed, node.Identity.StableId, null, null, null, null, null, null, node.Style);
                AppendRules(rules, nodeStyles.Rules);
                AppendDiagnosticList(diagnostics, nodeStyles.Diagnostics, maximumDiagnostics);
                if (nodeStyles.Success)
                {
                    scopes.Add(new RendererStyleScope { OwnerId = node.Identity.StableId, ScopeId = nodeStyles.ScopeId });
                }
            }

            nodeIndex++;
        }

        if (!HasDiagnosticErrors(diagnostics.ToArray()))
        {
            result.Compilation = compiler.SerializeStylesheet(rules.ToArray());
        }

        result.Diagnostics = diagnostics.ToArray();
        result.Scopes = scopes.ToArray();
        result.Success = !HasDiagnosticErrors(result.Diagnostics);
        return result;
    }

    internal void AppendRules(List<UiCssRule> target, UiCssRule[]? source)
    {
        UiCssRule[] rules = source ?? Array.Empty<UiCssRule>();
        int index = 0;
        while (index < rules.Length)
        {
            target.Add(rules[index]);
            index++;
        }
    }

    internal void ApplyStyleScopes(DomNode root, RendererStyleScope[] scopes, string pageScopeId, bool fullDocument)
    {
        if (pageScopeId.Length > 0)
        {
            if (fullDocument)
            {
                ApplyPageScopeToBody(root, pageScopeId);
            }
            else
            {
                ApplyPageScopeToFragment(root, pageScopeId);
            }
        }

        ApplyOwnerScopes(root, scopes);
    }

    internal void ApplyPageScopeToBody(DomNode node, string scopeId)
    {
        if (node.Kind == DomNodeKind.Element && string.Equals(node.Name, HtmlElements.Body, StringComparison.Ordinal))
        {
            AddScopeClass(node, scopeId);
            return;
        }

        int index = 0;
        while (index < node.Children.Length)
        {
            ApplyPageScopeToBody(node.Children[index], scopeId);
            index++;
        }
    }

    internal void ApplyPageScopeToFragment(DomNode root, string scopeId)
    {
        int index = 0;
        while (index < root.Children.Length)
        {
            if (!IsFragmentAsset(root.Children[index]))
            {
                ApplyScopeToFirstElements(root.Children[index], scopeId);
            }

            index++;
        }
    }

    internal bool IsFragmentAsset(DomNode node)
    {
        return node.Key.StartsWith("$resource:", StringComparison.Ordinal) || node.Key == "$style" || node.Key == "$hydration" || node.Key == "$bootstrap";
    }

    internal void ApplyScopeToFirstElements(DomNode node, string scopeId)
    {
        if (node.Kind == DomNodeKind.Element)
        {
            AddScopeClass(node, scopeId);
            return;
        }

        int index = 0;
        while (index < node.Children.Length)
        {
            ApplyScopeToFirstElements(node.Children[index], scopeId);
            index++;
        }
    }

    internal void ApplyOwnerScopes(DomNode node, RendererStyleScope[] scopes)
    {
        int scopeIndex = 0;
        while (scopeIndex < scopes.Length)
        {
            if (string.Equals(scopes[scopeIndex].OwnerId, node.Id, StringComparison.Ordinal))
            {
                node.StyleScope = scopes[scopeIndex].ScopeId;
                AddScopeClass(node, scopes[scopeIndex].ScopeId);
            }

            scopeIndex++;
        }

        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            ApplyOwnerScopes(node.Children[childIndex], scopes);
            childIndex++;
        }
    }

    internal void AddScopeClass(DomNode node, string scopeId)
    {
        DomAttribute[] attributes = node.Attributes ?? Array.Empty<DomAttribute>();
        int index = 0;
        while (index < attributes.Length)
        {
            if (string.Equals(attributes[index].Name, HtmlAttributes.Class, StringComparison.Ordinal))
            {
                if (!ContainsClassToken(attributes[index].Value, scopeId))
                {
                    attributes[index].Value = attributes[index].Value.Length == 0 ? scopeId : attributes[index].Value + " " + scopeId;
                }

                return;
            }

            index++;
        }

        DomAttribute[] expanded = new DomAttribute[attributes.Length + 1];
        Array.Copy(attributes, expanded, attributes.Length);
        expanded[attributes.Length] = new DomAttribute { Name = HtmlAttributes.Class, Value = scopeId };
        node.Attributes = expanded;
    }

    internal bool ContainsClassToken(string value, string token)
    {
        string[] tokens = value.Split(new char[] { ' ', '\t', '\r', '\n', '\f' }, StringSplitOptions.RemoveEmptyEntries);
        int index = 0;
        while (index < tokens.Length)
        {
            if (string.Equals(tokens[index], token, StringComparison.Ordinal))
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal UiHydrationManifest CreateHydrationManifest(UiApplication application, UiWindow window, UiPage page, RenderTreeAnnotationResult tree)
    {
        return new HydrationPlanner().CreateHydrationManifest(application, window, page, tree.Roots, tree.ContentHash);
    }

    internal UiValueEntry[] CreateHydrationNodes(DomNode[] roots)
    {
        List<UiValueEntry> result = new List<UiValueEntry>();
        int index = 0;
        while (index < roots.Length)
        {
            AddHydrationNode(result, roots[index]);
            index++;
        }

        return result.ToArray();
    }

    internal UiRouteData? CloneRouteData(UiRouteData? source)
    {
        if (source == null)
        {
            return null;
        }

        return new UiRouteData
        {
            RouteId = source.RouteId,
            Parameters = CloneValueEntries(source.Parameters),
            Query = CloneValueEntries(source.Query),
            Fragment = source.Fragment
        };
    }

    internal UiValueEntry[] CloneValueEntries(UiValueEntry[]? source)
    {
        UiValueEntry[] values = source ?? Array.Empty<UiValueEntry>();
        UiValueEntry[] result = new UiValueEntry[values.Length];
        int index = 0;
        while (index < values.Length)
        {
            UiValueEntry entry = values[index];
            result[index] = new UiValueEntry { Key = entry.Key, Value = CloneTaggedValue(entry.Value) ?? new UiValue() };
            index++;
        }

        return result;
    }

    internal UiValue? CloneTaggedValue(UiValue? source)
    {
        if (source == null)
        {
            return null;
        }

        UiValue result = new UiValue();
        result.Kind = source.Kind;
        switch (source.Kind)
        {
            case UiValueKind.Null:
                break;
            case UiValueKind.Boolean:
                result.BooleanValue = source.BooleanValue;
                break;
            case UiValueKind.Integer:
                result.IntegerValue = source.IntegerValue;
                break;
            case UiValueKind.FloatingPoint:
                result.FloatingPointValue = source.FloatingPointValue;
                break;
            case UiValueKind.Decimal:
                result.DecimalValue = source.DecimalValue;
                break;
            case UiValueKind.String:
                result.StringValue = source.StringValue ?? string.Empty;
                break;
            case UiValueKind.DateTime:
                result.DateTimeValue = source.DateTimeValue ?? string.Empty;
                break;
            case UiValueKind.Binary:
                byte[] sourceBytes = source.BinaryValue ?? Array.Empty<byte>();
                result.BinaryValue = new byte[sourceBytes.Length];
                Array.Copy(sourceBytes, result.BinaryValue, sourceBytes.Length);
                break;
            case UiValueKind.Array:
                UiValue[] sourceItems = source.ArrayValue ?? Array.Empty<UiValue>();
                result.ArrayValue = new UiValue[sourceItems.Length];
                int itemIndex = 0;
                while (itemIndex < sourceItems.Length)
                {
                    result.ArrayValue[itemIndex] = CloneTaggedValue(sourceItems[itemIndex]) ?? new UiValue();
                    itemIndex++;
                }

                break;
            case UiValueKind.Object:
                result.ObjectValue = CloneValueEntries(source.ObjectValue);
                break;
            default:
                throw new ArgumentOutOfRangeException("source", source.Kind, "The portable value kind is not supported.");
        }

        return result;
    }

    internal UiResource[] CloneResources(UiResource[]? source)
    {
        UiResource[] resources = source ?? Array.Empty<UiResource>();
        UiResource[] result = new UiResource[resources.Length];
        int index = 0;
        while (index < resources.Length)
        {
            UiResource resource = resources[index];
            result[index] = new UiResource
            {
                Key = resource.Key,
                Url = resource.Url,
                Relationship = resource.Relationship,
                MimeType = resource.MimeType,
                Media = resource.Media,
                Integrity = resource.Integrity,
                CrossOrigin = resource.CrossOrigin,
                ReferrerPolicy = resource.ReferrerPolicy,
                IsModule = resource.IsModule,
                Attributes = CloneValueEntries(resource.Attributes)
            };
            index++;
        }

        return result;
    }

    internal void AddHydrationNode(List<UiValueEntry> entries, DomNode node)
    {
        UiValueEntry entry = new UiValueEntry
        {
            Key = node.Id,
            Value = new UiValue { Kind = UiValueKind.Integer, IntegerValue = node.Sequence }
        };
        int insertionIndex = entries.Count;
        entries.Add(entry);
        while (insertionIndex > 0 && string.CompareOrdinal(entries[insertionIndex - 1].Key, entry.Key) > 0)
        {
            entries[insertionIndex] = entries[insertionIndex - 1];
            insertionIndex--;
        }

        entries[insertionIndex] = entry;
        int childIndex = 0;
        while (childIndex < node.Children.Length)
        {
            AddHydrationNode(entries, node.Children[childIndex]);
            childIndex++;
        }
    }

    internal DomSnapshot CreateSnapshot(UiApplication application, UiWindow window, UiPage page, RenderTreeAnnotationResult tree)
    {
        int revision = 0;
        if (window.NavigationResult != null && window.NavigationResult.Generation > 0)
        {
            revision = window.NavigationResult.Generation;
        }

        string sessionHash = StableHash.HashParts(new string[] { application.RootSeed, application.Id, window.Id, page.Id });
        return new DomSnapshot
        {
            SessionId = "render-" + sessionHash.Substring(0, 20),
            RootSeed = application.RootSeed,
            Revision = revision,
            Roots = tree.Roots,
            ContentHash = tree.ContentHash
        };
    }

    internal UiRenderResult CreateFailureResult(UiDiagnostic[] diagnostics)
    {
        UiRenderResult result = new UiRenderResult();
        result.Diagnostics = diagnostics;
        result.Hash = CreateOutputHash(result);
        return result;
    }

    internal UiDiagnostic CreateDiagnostic(string code, string message, string location)
    {
        return new UiDiagnostic { Severity = UiDiagnosticSeverity.Error, Code = code, Message = message, Location = location };
    }

    internal UiDiagnostic[] AppendDiagnostic(UiDiagnostic[] existing, UiDiagnostic diagnostic, int maximumDiagnostics)
    {
        if (existing.Length >= maximumDiagnostics)
        {
            return existing;
        }

        UiDiagnostic[] result = new UiDiagnostic[existing.Length + 1];
        Array.Copy(existing, result, existing.Length);
        result[existing.Length] = diagnostic;
        return result;
    }

    internal void AppendDiagnosticList(List<UiDiagnostic> target, UiDiagnostic[]? source, int maximumDiagnostics)
    {
        UiDiagnostic[] diagnostics = source ?? Array.Empty<UiDiagnostic>();
        int index = 0;
        while (index < diagnostics.Length && target.Count < maximumDiagnostics)
        {
            target.Add(CloneDiagnostic(diagnostics[index]));
            index++;
        }
    }

    internal UiDiagnostic[] MergeDiagnostics(UiDiagnostic[] first, UiDiagnostic[] second, int maximumDiagnostics)
    {
        List<UiDiagnostic> result = new List<UiDiagnostic>();
        AppendDiagnosticList(result, first, maximumDiagnostics);
        AppendDiagnosticList(result, second, maximumDiagnostics);
        return result.ToArray();
    }

    internal UiDiagnostic[] CopyDiagnostics(UiDiagnostic[]? source, int maximumDiagnostics)
    {
        List<UiDiagnostic> result = new List<UiDiagnostic>();
        AppendDiagnosticList(result, source, maximumDiagnostics);
        return result.ToArray();
    }

    internal UiDiagnostic CloneDiagnostic(UiDiagnostic source)
    {
        return new UiDiagnostic
        {
            Severity = source.Severity,
            Code = source.Code,
            Message = source.Message,
            Location = source.Location
        };
    }

    internal bool HasDiagnosticErrors(UiDiagnostic[] diagnostics)
    {
        int index = 0;
        while (index < diagnostics.Length)
        {
            if (diagnostics[index].Severity == UiDiagnosticSeverity.Error)
            {
                return true;
            }

            index++;
        }

        return false;
    }

    internal string CreateOutputHash(UiRenderResult result)
    {
        string snapshotHash = result.Snapshot == null ? string.Empty : result.Snapshot.ContentHash;
        string hydrationHash = result.Hydration == null ? string.Empty : result.Hydration.Hash;
        string resourcesHash = CreateResourcesHash(result.Resources);
        return StableHash.HashParts(new string[]
        {
            "ui-render-result-v1",
            result.Html,
            result.Css,
            result.Bootstrap,
            snapshotHash,
            hydrationHash,
            resourcesHash
        });
    }

    internal string CreateResourcesHash(UiResource[]? resources)
    {
        UiResource[] source = resources ?? Array.Empty<UiResource>();
        List<string> parts = new List<string>();
        int index = 0;
        while (index < source.Length)
        {
            UiResource resource = source[index];
            UiValue attributes = new UiValue { Kind = UiValueKind.Object, ObjectValue = resource.Attributes };
            parts.Add(StableHash.HashParts(new string[]
            {
                resource.Key,
                resource.Url,
                resource.Relationship,
                resource.MimeType ?? string.Empty,
                resource.Media ?? string.Empty,
                resource.Integrity ?? string.Empty,
                resource.CrossOrigin ?? string.Empty,
                resource.ReferrerPolicy ?? string.Empty,
                InvariantFormatter.FormatBoolean(resource.IsModule),
                ValueJsonCodec.SerializeValue(attributes, 64, 10000, 1048576, 1048576)
            }));
            index++;
        }

        return StableHash.HashParts(parts.ToArray());
    }
}
