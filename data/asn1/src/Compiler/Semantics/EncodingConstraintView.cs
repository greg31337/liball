namespace asn1;

internal class EncodingConstraintView
{
    internal IntegerConstraintResult? IntegerConstraints;
    internal SizeConstraintResult? SizeConstraints;
    internal PermittedAlphabetResult? PermittedAlphabet;
    internal NormalizedComponentConstraintSet[] ComponentConstraintSets = [];
    internal TimePropertySettings[] TimeSettings = [];
    internal bool HasEncodingEffect;
    internal bool HasValidationOnlyConstraints;
}
