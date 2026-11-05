namespace asn1;

internal class LexInputBuffer
{
    internal TriviaScanner Trivia;
    internal System.Collections.Generic.List<TriviaScannerResult> PendingInputs;

    internal LexInputBuffer(TriviaScanner trivia)
    {
        this.Trivia = trivia == null ? throw new System.ArgumentNullException("trivia") : trivia;
        this.PendingInputs = new System.Collections.Generic.List<TriviaScannerResult>();
    }

    internal TriviaScannerResult ReadInput()
    {
        if (this.PendingInputs.Count == 0)
        {
            return this.Trivia.ScanNext();
        }

        TriviaScannerResult result = this.PendingInputs[0];
        this.PendingInputs.RemoveAt(0);
        return result;
    }

    internal void PushInput(TriviaScannerResult input)
    {
        if (input == null)
        {
            throw new System.ArgumentNullException("input");
        }

        this.PendingInputs.Insert(0, input);
    }
}
