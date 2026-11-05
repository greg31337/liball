namespace unicode.Tools.UnicodeDataGenerator;

public sealed class UnicodeNormalizationTestRecord
{
    public int[] Source;
    public int[] Nfc;
    public int[] Nfd;
    public int[] Nfkc;
    public int[] Nfkd;
    public string Comment;

    public UnicodeNormalizationTestRecord(
        int[] source,
        int[] nfc,
        int[] nfd,
        int[] nfkc,
        int[] nfkd,
        string comment)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(nfc);
        ArgumentNullException.ThrowIfNull(nfd);
        ArgumentNullException.ThrowIfNull(nfkc);
        ArgumentNullException.ThrowIfNull(nfkd);
        ArgumentNullException.ThrowIfNull(comment);

        Source = source;
        Nfc = nfc;
        Nfd = nfd;
        Nfkc = nfkc;
        Nfkd = nfkd;
        Comment = comment;
    }
}
