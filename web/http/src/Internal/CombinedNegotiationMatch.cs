namespace http;

internal class CombinedNegotiationMatch
{
    internal bool Acceptable;
    internal long OverallQuality;
    internal long TotalSpecificity;
    internal long TotalClientOrder;
    internal MediaNegotiationMatch Media = new MediaNegotiationMatch();
    internal TokenNegotiationMatch Charset = new TokenNegotiationMatch();
    internal TokenNegotiationMatch Encoding = new TokenNegotiationMatch();
    internal LanguageNegotiationMatch Language = new LanguageNegotiationMatch();
}
