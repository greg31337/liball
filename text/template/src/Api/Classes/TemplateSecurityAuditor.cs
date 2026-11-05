using System;
using System.Collections.Generic;

namespace template;

/// <summary>
/// Examines templates and configuration for security-sensitive choices and returns warning diagnostics.
/// </summary>
public class TemplateSecurityAuditor : ITemplateSecurityAuditor
{
    /// <summary>
    /// Initializes an auditor that reports warnings without modifying templates or configuration.
    /// </summary>
    public TemplateSecurityAuditor()
    {
    }

    /// <summary>
    /// Examines the supplied template or configuration and returns security warning diagnostics.
    /// </summary>
    /// <param name="document">The parsed template document to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public List<TemplateDiagnostic> Audit(TemplateDocumentSyntax document, TemplateOptions options)
    {
        if (document == null)
        {
            throw new ArgumentNullException("document");
        }

        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        List<TemplateDiagnostic> diagnostics = AuditOptions(options);
        AddDocumentDiagnostics(diagnostics, document, options);
        return diagnostics;
    }

    /// <summary>
    /// Examines the supplied template or configuration and returns security warning diagnostics.
    /// </summary>
    /// <param name="document">The parsed template document to process.</param>
    /// <param name="options">The options that control the operation.</param>
    /// <param name="memberResolverOptions">The member resolver options used by the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public List<TemplateDiagnostic> AuditWithMemberResolverOptions(TemplateDocumentSyntax document, TemplateOptions options, TemplateMemberResolverOptions memberResolverOptions)
    {
        if (memberResolverOptions == null)
        {
            throw new ArgumentNullException("memberResolverOptions");
        }

        List<TemplateDiagnostic> diagnostics = Audit(document, options);
        AddDiagnostics(diagnostics, AuditMemberResolverOptions(memberResolverOptions));
        return diagnostics;
    }

    /// <summary>
    /// Examines the supplied template or configuration and returns security warning diagnostics.
    /// </summary>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public List<TemplateDiagnostic> AuditOptions(TemplateOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        List<TemplateDiagnostic> diagnostics = new List<TemplateDiagnostic>();
        if (options.EscapingMode == TemplateEscapingMode.None)
        {
            diagnostics.Add(CreateWarning("SECURITY001", "Template output escaping is disabled.", null, "Use an escaping mode that matches the output format."));
        }

        if (!options.AutoEscape && options.EscapingMode != TemplateEscapingMode.None)
        {
            diagnostics.Add(CreateWarning("SECURITY002", "Automatic output escaping is disabled.", null, "Enable autoescaping for templates that render untrusted values."));
        }

        if (!options.StrictUnsafeMembers && !options.StrictMode)
        {
            diagnostics.Add(CreateWarning("SECURITY003", "Strict unsafe member checks are disabled.", null, "Enable strict unsafe member checks when rendering untrusted models."));
        }

        return diagnostics;
    }

    /// <summary>
    /// Examines the supplied template or configuration and returns security warning diagnostics.
    /// </summary>
    /// <param name="policy">The policy value to add or apply.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public List<TemplateDiagnostic> AuditSecurityPolicy(TemplateSecurityPolicy policy)
    {
        if (policy == null)
        {
            throw new ArgumentNullException("policy");
        }

        List<TemplateDiagnostic> diagnostics = new List<TemplateDiagnostic>();
        if (policy.AllowDangerousMemberNames)
        {
            diagnostics.Add(CreateWarning("SECURITY004", "Dangerous member names are allowed.", null, "Keep dangerous member names blocked unless the model is fully trusted."));
        }

        if (policy.AllowMemberNamesStartingWithUnderscore)
        {
            diagnostics.Add(CreateWarning("SECURITY005", "Member names starting with underscore are allowed.", null, "Keep underscore-prefixed members blocked for untrusted templates."));
        }

        return diagnostics;
    }

    /// <summary>
    /// Examines the supplied template or configuration and returns security warning diagnostics.
    /// </summary>
    /// <param name="options">The options that control the operation.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    public List<TemplateDiagnostic> AuditMemberResolverOptions(TemplateMemberResolverOptions options)
    {
        if (options == null)
        {
            throw new ArgumentNullException("options");
        }

        List<TemplateDiagnostic> diagnostics = new List<TemplateDiagnostic>();
        if (options.AllowPublicMethods)
        {
            diagnostics.Add(CreateWarning("SECURITY006", "Public method access is enabled.", null, "Expose only explicitly safe methods to untrusted templates."));
        }

        if (options.SecurityPolicy == null)
        {
            diagnostics.Add(CreateWarning("SECURITY007", "Member access security policy is missing.", null, "Attach a security policy before resolving members from untrusted templates."));
        }
        else
        {
            AddDiagnostics(diagnostics, AuditSecurityPolicy(options.SecurityPolicy));
        }

        return diagnostics;
    }

    internal void AddDocumentDiagnostics(List<TemplateDiagnostic> diagnostics, TemplateDocumentSyntax document, TemplateOptions options)
    {
        if (!UsesAutoEscaping(options))
        {
            return;
        }

        AuditNodes(diagnostics, document.Children);
    }

    internal void AuditNodes(List<TemplateDiagnostic> diagnostics, List<TemplateSyntaxNode> nodes)
    {
        int index = 0;
        while (index < nodes.Count)
        {
            AuditNode(diagnostics, nodes[index]);
            index = index + 1;
        }
    }

    internal void AuditNode(List<TemplateDiagnostic> diagnostics, TemplateSyntaxNode node)
    {
        if (node.Kind == TemplateSyntaxKind.Output && node.Output != null)
        {
            AuditOutput(diagnostics, node.Output);
        }
        else if (node.Kind == TemplateSyntaxKind.Raw && node.Raw != null)
        {
            diagnostics.Add(CreateWarning("SECURITY010", "Raw block output bypasses autoescaping.", node.Raw.Span, "Avoid raw blocks around untrusted content."));
        }
        else if (node.Kind == TemplateSyntaxKind.Block && node.Block != null)
        {
            AuditNodes(diagnostics, node.Block.Children);
            AuditNodes(diagnostics, node.Block.ElseChildren);
        }
    }

    internal void AuditOutput(List<TemplateDiagnostic> diagnostics, TemplateOutputSyntax output)
    {
        if (output.Raw || !output.Escape)
        {
            diagnostics.Add(CreateWarning("SECURITY008", "Raw output bypasses autoescaping.", output.Span, "Render trusted safe values only, or allow the configured encoder to escape this output."));
            return;
        }

        if (ContainsSafeFilter(output.Expression))
        {
            diagnostics.Add(CreateWarning("SECURITY009", "Safe-filtered output bypasses autoescaping.", output.Expression.Span, "Use the safe filter only for already-sanitized content."));
        }
    }

    internal bool ContainsSafeFilter(TemplateExpressionSyntax expression)
    {
        if (expression.ExpressionKind == TemplateExpressionSyntaxKind.Call && expression.CallKind == TemplateExpressionCallKind.Filter && expression.Name == "safe")
        {
            return true;
        }

        int index = 0;
        while (index < expression.Children.Count)
        {
            if (ContainsSafeFilter(expression.Children[index]))
            {
                return true;
            }

            index = index + 1;
        }

        index = 0;
        while (index < expression.Arguments.Count)
        {
            if (ContainsSafeFilter(expression.Arguments[index]))
            {
                return true;
            }

            index = index + 1;
        }

        index = 0;
        while (index < expression.PathSegments.Count)
        {
            TemplatePathSegmentSyntax segment = expression.PathSegments[index];
            if (segment.Expression != null && ContainsSafeFilter(segment.Expression))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool UsesAutoEscaping(TemplateOptions options)
    {
        return options.AutoEscape && options.EscapingMode != TemplateEscapingMode.None;
    }

    internal void AddDiagnostics(List<TemplateDiagnostic> diagnostics, List<TemplateDiagnostic> additions)
    {
        int index = 0;
        while (index < additions.Count)
        {
            diagnostics.Add(additions[index]);
            index = index + 1;
        }
    }

    internal TemplateDiagnostic CreateWarning(string code, string message, TemplateSourceSpan? span, string hint)
    {
        return new TemplateDiagnostic(code, TemplateDiagnosticSeverity.Warning, message, span, hint);
    }
}
