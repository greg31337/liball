namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeJoiningRecord
{
    public int CodePoint;
    public string Name;
    public string JoiningType;
    public string JoiningGroup;

    public UnicodeJoiningRecord(int codePoint, string name, string joiningType, string joiningGroup)
    {
        CodePoint = codePoint;
        Name = name;
        JoiningType = joiningType;
        JoiningGroup = joiningGroup;
    }
}
