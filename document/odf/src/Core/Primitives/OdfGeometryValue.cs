namespace odf;

internal class OdfPointValue
{
    internal string LexicalValue = "";
    internal long X;
    internal long Y;
    internal bool IsValid;
}

internal class OdfVectorValue
{
    internal string LexicalValue = "";
    internal double X;
    internal double Y;
    internal double Z;
    internal bool IsValid;
}

internal class OdfViewBoxValue
{
    internal string LexicalValue = "";
    internal long Left;
    internal long Top;
    internal long Width;
    internal long Height;
    internal bool IsValid;
}
