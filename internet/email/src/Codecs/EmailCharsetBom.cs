namespace email;

internal class EmailCharsetBom
{
    string charset;
    int byteCount;

    internal EmailCharsetBom(string charset, int byteCount)
    {
        this.charset = charset;
        this.byteCount = byteCount;
    }

    internal string GetCharset()
    {
        return charset;
    }

    internal int GetByteCount()
    {
        return byteCount;
    }
}
