namespace asn1;

internal class RecoveryPoint
{
    internal AsnTokenKind Kind;
    internal string Text;

    internal RecoveryPoint(AsnTokenKind kind, string text)
    {
        this.Kind = kind;
        this.Text = text == null ? throw new System.ArgumentNullException("text") : text;
    }

    internal bool Matches(AsnToken token)
    {
        if (token.Kind != this.Kind)
        {
            return false;
        }

        return this.Text.Length == 0 || token.Text == this.Text;
    }
}
