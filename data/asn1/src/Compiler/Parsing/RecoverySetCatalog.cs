namespace asn1;

internal class RecoverySetCatalog
{
    internal RecoverySet ModuleBoundary;
    internal RecoverySet AssignmentBoundary;
    internal RecoverySet ComponentBoundary;
    internal RecoverySet ConstraintBoundary;
    internal RecoverySet ObjectSyntaxBoundary;
    internal RecoverySet ParameterListBoundary;
    internal RecoverySet EncodingControlBoundary;

    internal RecoverySetCatalog()
    {
        RecoveryPoint end = new RecoveryPoint(AsnTokenKind.ReservedWord, "END");
        RecoveryPoint encodingControl = new RecoveryPoint(
            AsnTokenKind.ReservedWord,
            "ENCODING-CONTROL");
        RecoveryPoint endOfInput = new RecoveryPoint(AsnTokenKind.EndOfInput, "");

        this.ModuleBoundary = new RecoverySet(
            "module",
            [end, endOfInput]);
        this.AssignmentBoundary = new RecoverySet(
            "assignment",
            [
                new RecoveryPoint(AsnTokenKind.TypeReference, ""),
                new RecoveryPoint(AsnTokenKind.ValueReference, ""),
                new RecoveryPoint(AsnTokenKind.ObjectClassReference, ""),
                new RecoveryPoint(AsnTokenKind.ObjectReference, ""),
                new RecoveryPoint(AsnTokenKind.ObjectSetReference, ""),
                new RecoveryPoint(AsnTokenKind.Identifier, ""),
                end,
                encodingControl,
                endOfInput
            ]);
        this.ComponentBoundary = new RecoverySet(
            "component",
            [
                new RecoveryPoint(AsnTokenKind.Comma, ""),
                new RecoveryPoint(AsnTokenKind.RightBrace, ""),
                new RecoveryPoint(AsnTokenKind.Ellipsis, ""),
                endOfInput
            ]);
        this.ConstraintBoundary = new RecoverySet(
            "constraint",
            [
                new RecoveryPoint(AsnTokenKind.RightParenthesis, ""),
                new RecoveryPoint(AsnTokenKind.Comma, ""),
                new RecoveryPoint(AsnTokenKind.RightBrace, ""),
                endOfInput
            ]);
        this.ObjectSyntaxBoundary = new RecoverySet(
            "object syntax",
            [
                new RecoveryPoint(AsnTokenKind.Comma, ""),
                new RecoveryPoint(AsnTokenKind.RightBrace, ""),
                new RecoveryPoint(AsnTokenKind.RightBracket, ""),
                endOfInput
            ]);
        this.ParameterListBoundary = new RecoverySet(
            "parameter list",
            [
                new RecoveryPoint(AsnTokenKind.Comma, ""),
                new RecoveryPoint(AsnTokenKind.RightBrace, ""),
                new RecoveryPoint(AsnTokenKind.Assignment, ""),
                endOfInput
            ]);
        this.EncodingControlBoundary = new RecoverySet(
            "encoding control",
            [end, encodingControl, endOfInput]);
    }
}
