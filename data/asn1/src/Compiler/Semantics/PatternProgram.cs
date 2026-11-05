namespace asn1;

internal class PatternProgram
{
    internal string Source = "";
    internal PatternNode? Root;
    internal NormalizedConstraintNode? SourceNode;

    internal bool Matches(string text)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        return this.Root != null && new PatternMatcher().Matches(this.Root, text);
    }
}
