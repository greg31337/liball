using System.Numerics;
using asn1;

namespace asn1.Tests;

public class NormativeExampleCorpusTests
{
    [Test]
    public void X680PersonnelRecordCompilesTypesTagsAndValue()
    {
        AsnCompilationResult result = Compile(
            "x680-personnel.asn",
            "PersonnelRecordModule DEFINITIONS AUTOMATIC TAGS ::= BEGIN\n"
            + "PersonnelRecord ::= [APPLICATION 0] SET {\n"
            + " name Name, title VisibleString, number EmployeeNumber,\n"
            + " dateOfHire Date, nameOfSpouse Name,\n"
            + " children SEQUENCE OF ChildInformation DEFAULT {} }\n"
            + "ChildInformation ::= SET { name Name, dateOfBirth Date }\n"
            + "Name ::= [APPLICATION 1] SEQUENCE {\n"
            + " givenName VisibleString, initial VisibleString, familyName VisibleString }\n"
            + "EmployeeNumber ::= [APPLICATION 2] INTEGER\n"
            + "Date ::= [APPLICATION 3] VisibleString\n"
            + "personnelRecord PersonnelRecord ::= {\n"
            + " name { givenName \"John\", initial \"P\", familyName \"Smith\" },\n"
            + " title \"Director\", number 51, dateOfHire \"19710917\",\n"
            + " nameOfSpouse { givenName \"Mary\", initial \"T\", familyName \"Smith\" },\n"
            + " children {\n"
            + "  { name { givenName \"Ralph\", initial \"T\", familyName \"Smith\" },\n"
            + "    dateOfBirth \"19571111\" },\n"
            + "  { name { givenName \"Susan\", initial \"B\", familyName \"Jones\" },\n"
            + "    dateOfBirth \"19590717\" } } }\n"
            + "END");

        Assert.That(result.Success, Is.True, Format(result.Diagnostics));
        Assert.That(result.Assignments.Select(item => item.Name), Is.EquivalentTo(new[]
        {
            "PersonnelRecord",
            "ChildInformation",
            "Name",
            "EmployeeNumber",
            "Date",
            "personnelRecord"
        }));
        AsnAssignment record = result.Assignments.Single(item => item.Name == "PersonnelRecord");
        Assert.That(record.Type?.Kind, Is.EqualTo(AsnTypeKind.Set));
        Assert.That(record.Type?.Tag?.Class, Is.EqualTo(AsnTagClass.Application));
        Assert.That(record.Type?.Tag?.Number, Is.EqualTo(new BigInteger(0)));
        Assert.That(record.Type?.Components, Has.Length.EqualTo(6));
        Assert.That(record.Type?.Components[5].HasDefaultValue, Is.True);
        AsnAssignment value = result.Assignments.Single(item => item.Name == "personnelRecord");
        Assert.That(value.Value?.Kind, Is.EqualTo(AsnValueKind.Set));
        Assert.That(value.Value?.NamedValues, Has.Length.EqualTo(6));
        Assert.That(
            value.Value?.NamedValues.Single(item => item.Name == "number")
                .Value?.IntegerValue,
            Is.EqualTo(new BigInteger(51)));
    }

    [Test]
    public void X681OperationExampleCompilesClassesObjectsAndExtractedSets()
    {
        AsnCompilationResult result = Compile(
            "x681-operations.asn",
            "InformationObjects DEFINITIONS ::= BEGIN\n"
            + "OPERATION ::= CLASS {\n"
            + " &ArgumentType OPTIONAL, &ResultType OPTIONAL,\n"
            + " &Errors ERROR OPTIONAL, &Linked OPERATION OPTIONAL,\n"
            + " &resultReturned BOOLEAN DEFAULT TRUE,\n"
            + " &operationCode INTEGER UNIQUE }\n"
            + "WITH SYNTAX { [ARGUMENT &ArgumentType] [RESULT &ResultType]\n"
            + " [RETURN RESULT &resultReturned] [ERRORS &Errors]\n"
            + " [LINKED &Linked] CODE &operationCode }\n"
            + "ERROR ::= CLASS { &ParameterType OPTIONAL, &errorCode INTEGER UNIQUE }\n"
            + "WITH SYNTAX { [PARAMETER &ParameterType] CODE &errorCode }\n"
            + "My-Operations OPERATION ::= { operationA | operationB }\n"
            + "operationA OPERATION ::= { ARGUMENT INTEGER\n"
            + " ERRORS { { PARAMETER INTEGER CODE 1000 } | { CODE 1001 } } CODE 1 }\n"
            + "operationB OPERATION ::= { ARGUMENT IA5String RESULT BOOLEAN\n"
            + " ERRORS { { CODE 1002 } | { PARAMETER IA5String CODE 1003 } } CODE 2 }\n"
            + "My-OperationErrors ERROR ::= { My-Operations.&Errors }\n"
            + "My-OperationErrorCodes INTEGER ::= { My-Operations.&Errors.&errorCode }\n"
            + "END");

        Assert.That(result.Success, Is.True, Format(result.Diagnostics));
        AsnAssignment operationClass = result.Assignments.Single(item => item.Name == "OPERATION");
        Assert.That(operationClass.Kind, Is.EqualTo(AsnAssignmentKind.ObjectClass));
        Assert.That(operationClass.ObjectClass?.Fields, Has.Length.EqualTo(6));
        Assert.That(
            operationClass.ObjectClass?.Fields.Single(item => item.Name == "&operationCode").Unique,
            Is.True);
        AsnInformationObject? operationA = result.Assignments
            .Single(item => item.Name == "operationA").Object;
        Assert.That(operationA?.FieldSettings, Has.Length.EqualTo(4));
        Assert.That(
            operationA?.FieldSettings.Single(item => item.FieldName == "&resultReturned")
                .Value?.Kind,
            Is.EqualTo(AsnValueKind.Boolean));
        Assert.That(
            result.Assignments.Single(item => item.Name == "My-Operations").ObjectSet?.Objects,
            Has.Length.EqualTo(2));
        Assert.That(
            result.Assignments.Single(item => item.Name == "My-OperationErrors").ObjectSet?.Objects,
            Has.Length.EqualTo(4));
    }

    [Test]
    public void X681MandatoryRecursiveLinkReportsFiniteRepresentationDiagnostic()
    {
        AsnCompilationResult result = Compile(
            "x681-mandatory-recursion.asn",
            "RecursiveClass DEFINITIONS ::= BEGIN\n"
            + "OPERATION ::= CLASS { &Linked OPERATION }\n"
            + "END");

        Assert.That(result.Success, Is.False);
        Assert.That(result.Diagnostics.Select(item => item.Code),
            Is.EqualTo(new[] { "ASNB0024" }));
        Assert.That(result.Diagnostics.Single().Message,
            Does.Contain("OPTIONAL or DEFAULT"));
    }

    [Test]
    public void X682RelationalConstraintExampleCompilesObjectSetAndConstraints()
    {
        AsnCompilationResult result = Compile(
            "x682-relations.asn",
            "RelationalConstraints DEFINITIONS ::= BEGIN\n"
            + "ERROR-CLASS ::= CLASS {\n"
            + " &category PrintableString (SIZE (1)), &code INTEGER, &Type }\n"
            + "WITH SYNTAX { &category &code &Type }\n"
            + "ErrorSet ERROR-CLASS ::= {\n"
            + " { \"A\" 1 INTEGER } | { \"A\" 2 REAL } |\n"
            + " { \"B\" 1 CHARACTER STRING } | { \"B\" 2 GeneralString } }\n"
            + "ErrorReturn ::= SEQUENCE {\n"
            + " errorCategory ERROR-CLASS.&category ({ErrorSet}) OPTIONAL,\n"
            + " errors SEQUENCE OF SEQUENCE {\n"
            + "  errorCode ERROR-CLASS.&code ({ErrorSet}{@errorCategory}),\n"
            + "  errorInfo ERROR-CLASS.&Type ({ErrorSet}{@errorCategory,@.errorCode})\n"
            + " } OPTIONAL }\n"
            + "END");

        Assert.That(result.Success, Is.True, Format(result.Diagnostics));
        AsnInformationObjectSet? errorSet = result.Assignments
            .Single(item => item.Name == "ErrorSet").ObjectSet;
        Assert.That(errorSet?.Objects, Has.Length.EqualTo(4));
        Assert.That(
            errorSet?.Objects.Select(item => item.FieldSettings[0].Value?.TextValue),
            Is.EqualTo(new[] { "A", "A", "B", "B" }));
        AsnType? errorReturn = result.Assignments
            .Single(item => item.Name == "ErrorReturn").Type;
        Assert.That(errorReturn?.Components[0].Type?.Constraints.Single().Kind,
            Is.EqualTo(AsnConstraintKind.SimpleTable));
        Assert.That(
            errorReturn?.Components[1].Type?.ElementType?.Components
                .Select(item => item.Type?.Constraints.Single().Kind),
            Is.EqualTo(new[]
            {
                AsnConstraintKind.ComponentRelation,
                AsnConstraintKind.ComponentRelation
            }));
    }

    [Test]
    public void X683SignedTemplatesCompileFormalAndActualParameters()
    {
        AsnCompilationResult result = Compile(
            "x683-signed.asn",
            "ParameterizedTypes DEFINITIONS ::= BEGIN\n"
            + "SIGNED { ToBeSigned } ::= SEQUENCE {\n"
            + " authenticated-data ToBeSigned, authenticator BIT STRING }\n"
            + "OPTIONALLY-SIGNED { ToBeSigned } ::= CHOICE {\n"
            + " unsigned-data [0] ToBeSigned,\n"
            + " signed-data [1] SIGNED { ToBeSigned } }\n"
            + "OrderInformation ::= SEQUENCE { item UTF8String, quantity INTEGER }\n"
            + "SignedOrder ::= SIGNED { OrderInformation }\n"
            + "OptionalOrder ::= OPTIONALLY-SIGNED { OrderInformation }\n"
            + "END");

        Assert.That(result.Success, Is.True, Format(result.Diagnostics));
        AsnAssignment signedTemplate = result.Assignments
            .Single(item => item.Name == "SIGNED");
        Assert.That(signedTemplate.Parameters.Single().Name, Is.EqualTo("ToBeSigned"));
        Assert.That(signedTemplate.Parameters.Single().Kind, Is.EqualTo(AsnParameterKind.Type));
        AsnAssignment signedOrder = result.Assignments
            .Single(item => item.Name == "SignedOrder");
        Assert.That(signedOrder.Type?.Kind, Is.EqualTo(AsnTypeKind.Sequence));
        Assert.That(signedOrder.Type?.Components[0].Type?.Kind,
            Is.EqualTo(AsnTypeKind.Sequence));
        Assert.That(signedOrder.Type?.Components[0].Type?.Components,
            Has.Length.EqualTo(2));
        AsnAssignment optionalOrder = result.Assignments
            .Single(item => item.Name == "OptionalOrder");
        Assert.That(optionalOrder.Type?.Kind, Is.EqualTo(AsnTypeKind.Choice));
        Assert.That(optionalOrder.Type?.Components.Select(item => item.Type?.Tag?.Number),
            Is.EqualTo(new[] { new BigInteger(0), new BigInteger(1) }));
    }

    private static AsnCompilationResult Compile(string name, string text)
    {
        return new CompilerImplementation().Compile(new AsnCompilationInput
        {
            SourceUnits = [new AsnSourceUnit { Name = name, Text = text }]
        });
    }

    private static string Format(AsnDiagnostic[] diagnostics)
    {
        return string.Join(" | ", diagnostics.Select(item => item.Code + ":" + item.Message));
    }
}
