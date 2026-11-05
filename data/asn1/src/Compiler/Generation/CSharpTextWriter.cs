namespace asn1;

internal class CSharpTextWriter
{
    internal System.Text.StringBuilder Text = new System.Text.StringBuilder();
    internal int Indentation;

    internal void WriteLine(string text)
    {
        int index = 0;
        while (index < this.Indentation)
        {
            this.Text.Append("    ");
            index = index + 1;
        }

        this.Text.Append(text);
        this.Text.Append('\n');
    }

    internal void IncreaseIndentation()
    {
        this.Indentation = this.Indentation + 1;
    }

    internal void DecreaseIndentation()
    {
        if (this.Indentation == 0)
        {
            throw new System.InvalidOperationException(
                "Generated text indentation cannot be negative.");
        }

        this.Indentation = this.Indentation - 1;
    }

    internal string GetText()
    {
        if (this.Indentation != 0)
        {
            throw new System.InvalidOperationException(
                "Generated text contains an unclosed indentation scope.");
        }

        return this.Text.ToString();
    }
}
