using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace template.Tests;

[TestFixture]
public class TemplateSecurityAuditorTests
{
    [Test]
    public void AuditOptions_ReportsUnsafeConfiguration()
    {
        TemplateOptions options = new TemplateOptions();
        options.EscapingMode = TemplateEscapingMode.Html;
        options.AutoEscape = false;
        options.StrictUnsafeMembers = false;
        TemplateSecurityAuditor auditor = new TemplateSecurityAuditor();

        List<TemplateDiagnostic> diagnostics = auditor.AuditOptions(options);

        AssertCode(diagnostics, "SECURITY002");
        AssertCode(diagnostics, "SECURITY003");
    }

    [Test]
    public void AuditSecurityPolicy_ReportsRelaxedMemberGuards()
    {
        TemplateSecurityPolicy policy = new TemplateSecurityPolicy();
        policy.AllowDangerousMemberNames = true;
        policy.AllowMemberNamesStartingWithUnderscore = true;
        TemplateSecurityAuditor auditor = new TemplateSecurityAuditor();

        List<TemplateDiagnostic> diagnostics = auditor.AuditSecurityPolicy(policy);

        AssertCode(diagnostics, "SECURITY004");
        AssertCode(diagnostics, "SECURITY005");
    }

    [Test]
    public void AuditMemberResolverOptions_ReportsPublicMethodAccessAndMissingPolicy()
    {
        TemplateMemberResolverOptions options = new TemplateMemberResolverOptions();
        options.AllowPublicMethods = true;
        options.SecurityPolicy = null!;
        TemplateSecurityAuditor auditor = new TemplateSecurityAuditor();

        List<TemplateDiagnostic> diagnostics = auditor.AuditMemberResolverOptions(options);

        AssertCode(diagnostics, "SECURITY006");
        AssertCode(diagnostics, "SECURITY007");
    }

    [Test]
    public void AuditDocument_ReportsRawOutputInAutoescapedProfiles()
    {
        TemplateOptions options = CreateAutoEscapingOptions();
        TemplateParseResult parseResult = TemplateParser.ParseNative("{{ name | safe }}{% raw %}<b>{{ name }}</b>{% endraw %}");
        TemplateDocumentSyntax document = parseResult.Document;
        document.AddChild(CreateRawOutputNode());
        TemplateSecurityAuditor auditor = new TemplateSecurityAuditor();

        List<TemplateDiagnostic> diagnostics = auditor.Audit(document, options);

        AssertCode(diagnostics, "SECURITY009");
        AssertCode(diagnostics, "SECURITY010");
        AssertCode(diagnostics, "SECURITY008");
        Assert.That(FindDiagnostic(diagnostics, "SECURITY009").Span, Is.Not.Null);
        Assert.That(FindDiagnostic(diagnostics, "SECURITY010").Span, Is.Not.Null);
        Assert.That(FindDiagnostic(diagnostics, "SECURITY008").Span, Is.Not.Null);
    }

    [Test]
    public void AuditDocument_DoesNotReportRawOutputWhenAutoEscapeIsDisabled()
    {
        TemplateOptions options = new TemplateOptions();
        options.EscapingMode = TemplateEscapingMode.Html;
        options.AutoEscape = false;
        TemplateParseResult parseResult = TemplateParser.ParseNative("{{ name | safe }}{% raw %}<b>{{ name }}</b>{% endraw %}");
        TemplateDocumentSyntax document = parseResult.Document;
        document.AddChild(CreateRawOutputNode());
        TemplateSecurityAuditor auditor = new TemplateSecurityAuditor();

        List<TemplateDiagnostic> diagnostics = auditor.Audit(document, options);

        AssertNoCode(diagnostics, "SECURITY008");
        AssertNoCode(diagnostics, "SECURITY009");
        AssertNoCode(diagnostics, "SECURITY010");
    }

    [Test]
    public void Audit_RejectsNullArguments()
    {
        TemplateSecurityAuditor auditor = new TemplateSecurityAuditor();
        TemplateOptions options = new TemplateOptions();
        TemplateDocumentSyntax document = TemplateParser.ParseNative("x").Document;
        TemplateMemberResolverOptions memberOptions = new TemplateMemberResolverOptions();

        Assert.Throws<ArgumentNullException>(() => auditor.Audit(null!, options));
        Assert.Throws<ArgumentNullException>(() => auditor.Audit(document, null!));
        Assert.Throws<ArgumentNullException>(() => auditor.AuditWithMemberResolverOptions(document, options, null!));
        Assert.Throws<ArgumentNullException>(() => auditor.AuditOptions(null!));
        Assert.Throws<ArgumentNullException>(() => auditor.AuditSecurityPolicy(null!));
        Assert.Throws<ArgumentNullException>(() => auditor.AuditMemberResolverOptions(null!));
    }

    static TemplateOptions CreateAutoEscapingOptions()
    {
        TemplateOptions options = new TemplateOptions();
        options.EscapingMode = TemplateEscapingMode.Html;
        options.AutoEscape = true;
        options.StrictUnsafeMembers = true;
        return options;
    }

    static TemplateSyntaxNode CreateRawOutputNode()
    {
        TemplateSource source = TemplateSource.FromString("x");
        TemplateExpressionSyntax expression = new TemplateExpressionSyntax(source.CreateSpan(0, 1), TemplateExpressionSyntaxKind.Variable, "name");
        TemplateOutputSyntax output = new TemplateOutputSyntax(source.CreateSpan(0, 1), expression, true, false);
        return TemplateSyntaxNode.FromOutput(output);
    }

    static void AssertCode(List<TemplateDiagnostic> diagnostics, string code)
    {
        Assert.That(HasDiagnostic(diagnostics, code), Is.True);
    }

    static void AssertNoCode(List<TemplateDiagnostic> diagnostics, string code)
    {
        Assert.That(HasDiagnostic(diagnostics, code), Is.False);
    }

    static TemplateDiagnostic FindDiagnostic(List<TemplateDiagnostic> diagnostics, string code)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index].Code == code)
            {
                return diagnostics[index];
            }

            index = index + 1;
        }

        return TemplateDiagnostic.Create("", TemplateDiagnosticSeverity.Information, "");
    }

    static bool HasDiagnostic(List<TemplateDiagnostic> diagnostics, string code)
    {
        int index = 0;
        while (index < diagnostics.Count)
        {
            if (diagnostics[index].Code == code)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
