namespace fix;

internal class ScoreValue
{
    internal bool Available;
    internal ScoreValueKind Kind;
    internal bool BooleanValue;
    internal decimal NumberValue;
    internal string TextValue = "";
    internal FixFieldDefinition? Definition;
    internal string ErrorCode = "";
    internal string ErrorMessage = "";
}
