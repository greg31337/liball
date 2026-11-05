using asn1;

namespace asn1.Tests;

public class OpaqueConstraintCompilerTests
{
    [Test]
    public void CompilerPreservesOpaqueBodiesAndStructuredParameters()
    {
        ResolutionFixture fixture = Resolve(
            "Opaque DEFINITIONS ::= BEGIN "
                + "Encrypted ::= BIT STRING (CONSTRAINED BY { INTEGER : 7, "
                + "UTF8String, SEQUENCE { id INTEGER, text UTF8String } : "
                + "{ id 1, text \"ok\" } }) END");
        ResolvedTypeSyntax encrypted = RequireType(fixture, "Encrypted");
        UserDefinedConstraintMetadata metadata = encrypted.UserDefinedConstraints.Single();

        Assert.That(metadata.BodyTokens.Select(item => item.Text), Is.EqualTo(new[]
        {
            "INTEGER", ":", "7", ",", "UTF8String", ",", "SEQUENCE", "{",
            "id", "INTEGER", ",", "text", "UTF8String", "}", ":", "{",
            "id", "1", ",", "text", "\"ok\"", "}"
        }));
        Assert.That(metadata.Parameters.Length, Is.EqualTo(3));
        Assert.That(metadata.Parameters[0].GovernorText, Is.EqualTo("INTEGER"));
        Assert.That(metadata.Parameters[0].ActualText, Is.EqualTo("7"));
        Assert.That(metadata.Parameters[0].HasGovernor, Is.True);
        Assert.That(metadata.Parameters[1].Text, Is.EqualTo("UTF8String"));
        Assert.That(metadata.Parameters[1].HasGovernor, Is.False);
        Assert.That(metadata.Parameters[2].GovernorText,
            Is.EqualTo("SEQUENCE { id INTEGER , text UTF8String }"));
        Assert.That(metadata.Parameters[2].ActualText,
            Is.EqualTo("{ id 1 , text \"ok\" }"));
        Assert.That(metadata.HasExecutableMeaning, Is.False);
        Assert.That(metadata.RequiresApplicationValidation, Is.True);
        Assert.That(metadata.ValidationRequirement, Is.Not.Empty);
        Assert.That(encrypted.RequiresApplicationValidation, Is.True);

        AsnConstraint publicConstraint = encrypted.SourceType?.Constraints.Single()
            ?? throw new InvalidOperationException();
        Assert.That(publicConstraint.UserDefinedBody, Is.EqualTo(metadata.BodyText));
        Assert.That(publicConstraint.UserDefinedParameters,
            Is.EqualTo(metadata.Parameters.Select(item => item.Text)));
        Assert.That(publicConstraint.RequiresApplicationValidation, Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture.Diagnostics));
    }

    [Test]
    public void CompilerLinksOpaqueParametersToAssignmentFormals()
    {
        ResolutionFixture fixture = Resolve(
            "Opaque DEFINITIONS ::= BEGIN "
                + "Wrapped { TypeParam, INTEGER : limit } ::= BIT STRING "
                + "(CONSTRAINED BY { TypeParam, INTEGER : limit } ! INTEGER : 9) END");
        UserDefinedConstraintMetadata metadata = RequireType(fixture, "Wrapped")
            .UserDefinedConstraints.Single();

        Assert.That(metadata.AssignmentFormalParameters.Select(item => item.Parameter.Name),
            Is.EqualTo(new[] { "TypeParam", "limit" }));
        Assert.That(metadata.AssignmentFormalParameters.Select(item => item.Parameter.Kind),
            Is.EqualTo(new[] { AsnParameterKind.Type, AsnParameterKind.Value }));
        Assert.That(metadata.Parameters.Select(item => item.ReferencesFormalParameter),
            Is.EqualTo(new[] { true, true }));
        Assert.That(metadata.Parameters.Select(item => item.FormalParameterName),
            Is.EqualTo(new[] { "TypeParam", "limit" }));
        Assert.That(metadata.Parameters[1].GovernorText, Is.EqualTo("INTEGER"));
        Assert.That(metadata.Exception, Is.Not.Null);
        Assert.That(metadata.Exception?.UsesImplicitIntegerType, Is.False);
        Assert.That(metadata.Exception?.ExceptionValue?.EffectiveValue?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(9)));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture.Diagnostics));
    }

    [Test]
    public void CompilerResolvesDefinedAndTypedExceptionIdentifiers()
    {
        ResolutionFixture fixture = Resolve(
            "Exceptions DEFINITIONS ::= BEGIN "
                + "problemCode INTEGER ::= -17; "
                + "Reason ::= UTF8String; "
                + "Number ::= INTEGER (1..10 ! problemCode); "
                + "Text ::= UTF8String (SIZE(1..4) ! Reason : \"bad\") END");
        ResolvedTypeSyntax number = RequireType(fixture, "Number");
        ResolvedTypeSyntax text = RequireType(fixture, "Text");
        ConstraintExceptionMetadata numberException = number.ConstraintExceptions.Single();
        ConstraintExceptionMetadata textException = text.ConstraintExceptions.Single();

        Assert.That(numberException.UsesImplicitIntegerType, Is.True);
        Assert.That(numberException.DefinedIdentifier, Is.True);
        Assert.That(numberException.IdentifierText, Is.EqualTo("problemCode"));
        Assert.That(numberException.ExceptionType?.EffectiveKind,
            Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(numberException.ExceptionValue?.EffectiveValue?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(-17)));
        Assert.That(textException.UsesImplicitIntegerType, Is.False);
        Assert.That(textException.DefinedIdentifier, Is.False);
        Assert.That(textException.ExceptionType?.EffectiveKind,
            Is.EqualTo(AsnTypeKind.Utf8String));
        Assert.That(textException.ExceptionValue?.EffectiveValue?.TextValue,
            Is.EqualTo("bad"));

        AsnConstraint publicNumber = number.SourceType?.Constraints.Single()
            ?? throw new InvalidOperationException();
        AsnConstraint publicText = text.SourceType?.Constraints.Single()
            ?? throw new InvalidOperationException();
        Assert.That(publicNumber.HasException, Is.True);
        Assert.That(publicNumber.ExceptionIdentifier, Is.EqualTo("problemCode"));
        Assert.That(publicNumber.ExceptionType?.Kind, Is.EqualTo(AsnTypeKind.Integer));
        Assert.That(publicNumber.ExceptionValue?.IntegerValue,
            Is.EqualTo(new System.Numerics.BigInteger(-17)));
        Assert.That(publicText.HasException, Is.True);
        Assert.That(publicText.ExceptionIdentifier, Is.EqualTo("bad"));
        Assert.That(publicText.ExceptionType?.Kind, Is.EqualTo(AsnTypeKind.Utf8String));
        Assert.That(publicText.ExceptionValue?.TextValue, Is.EqualTo("bad"));
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture.Diagnostics));
    }

    [Test]
    public void CompilerRetainsEmptyAndInheritedOpaqueConstraintsIndependently()
    {
        ResolutionFixture fixture = Resolve(
            "Opaque DEFINITIONS ::= BEGIN "
                + "Base ::= INTEGER (CONSTRAINED BY { INTEGER : 1 } ! -1); "
                + "Alias ::= Base (CONSTRAINED BY {} ! UTF8String : \"alias\") END");
        ResolvedTypeSyntax baseType = RequireType(fixture, "Base");
        ResolvedTypeSyntax alias = RequireType(fixture, "Alias");

        Assert.That(baseType.UserDefinedConstraints.Length, Is.EqualTo(1));
        Assert.That(alias.UserDefinedConstraints.Length, Is.EqualTo(2));
        Assert.That(alias.UserDefinedConstraints.Select(item => item.BodyText),
            Is.EqualTo(new[] { "INTEGER : 1", "" }));
        Assert.That(alias.UserDefinedConstraints[1].Parameters, Is.Empty);
        Assert.That(alias.ConstraintExceptions.Length, Is.EqualTo(2));
        Assert.That(alias.ConstraintExceptions.Select(item => item.IdentifierText),
            Is.EqualTo(new[] { "-1", "alias" }));
        Assert.That(alias.UserDefinedConstraints[0].Exception,
            Is.SameAs(alias.ConstraintExceptions[0]));
        Assert.That(alias.UserDefinedConstraints[1].Exception,
            Is.SameAs(alias.ConstraintExceptions[1]));
        Assert.That(alias.RequiresApplicationValidation, Is.True);
        Assert.That(fixture.Diagnostics.GetDiagnostics(), Is.Empty,
            FormatDiagnostics(fixture.Diagnostics));
    }

    private static ResolvedTypeSyntax RequireType(
        ResolutionFixture fixture,
        string name)
    {
        return fixture.Tables[0].FindSymbol(name, AsnAssignmentKind.Type)?.ResolvedType
            ?? throw new InvalidOperationException();
    }

    private static ResolutionFixture Resolve(string source)
    {
        DiagnosticBag diagnostics = new DiagnosticBag(false, new AsnResourceLimits());
        IncrementalLexSession session = new IncrementalLexSession(
            "opaque-constraints.asn",
            false,
            new AsnResourceLimits());
        session.SupplyTextChunk(source, true);
        ModuleParseResult parsed = new ModuleParser(new TokenCursor(session, 4)).ParseModule();
        Assert.That(parsed.Status, Is.EqualTo(ModuleParseStatus.Parsed));
        Assert.That(session.GetDiagnostics(), Is.Empty);
        ModuleSyntax module = parsed.Module ?? throw new InvalidOperationException();
        SymbolTable[] tables = new SymbolTableBuilder(diagnostics).BuildTables([module]);
        new ImportResolver(diagnostics).ResolveImports(tables);
        CompilationGraph graph = new CompilationGraphBuilder(diagnostics).BuildGraph(tables);
        AssignmentClassificationResult classification = new AssignmentClassifier(diagnostics)
            .ClassifyAssignments(tables, graph);
        TypeResolutionResult types = new TypeResolver(diagnostics).ResolveTypes(tables);
        Assert.That(classification.Success, Is.True, FormatDiagnostics(diagnostics));
        Assert.That(types.Success, Is.True, FormatDiagnostics(diagnostics));
        ValueResolutionResult values = new ValueResolver(diagnostics).ResolveValues(tables);
        Assert.That(values.Success, Is.True, FormatDiagnostics(diagnostics));
        return new ResolutionFixture
        {
            Diagnostics = diagnostics,
            Tables = tables
        };
    }

    private static string FormatDiagnostics(DiagnosticBag diagnostics)
    {
        return string.Join(" | ", diagnostics.GetDiagnostics().Select(item => item.Code
            + ":" + item.Message));
    }

    private class ResolutionFixture
    {
        internal DiagnosticBag Diagnostics = null!;
        internal SymbolTable[] Tables = [];
    }
}
