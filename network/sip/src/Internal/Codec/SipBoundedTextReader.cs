namespace sip;

internal class SipBoundedTextReader
{
    string text;
    int position;

    internal SipBoundedTextReader(string text, int maximumLength)
    {
        if (text == null)
        {
            throw new System.ArgumentNullException("text");
        }

        if (maximumLength < 0)
        {
            throw new System.ArgumentOutOfRangeException("maximumLength");
        }

        if (text.Length > maximumLength)
        {
            throw new System.ArgumentException("The input exceeds the configured maximum length.", "text");
        }

        this.text = text;
        position = 0;
    }

    internal int GetPosition()
    {
        return position;
    }

    internal int GetRemainingCount()
    {
        return text.Length - position;
    }

    internal int PeekCharacter()
    {
        if (position == text.Length)
        {
            return -1;
        }

        return text[position];
    }

    internal int ReadCharacter()
    {
        int value = PeekCharacter();
        if (value >= 0)
        {
            position = position + 1;
        }

        return value;
    }

    internal string ReadText(int count)
    {
        if (count < 0)
        {
            throw new System.ArgumentOutOfRangeException("count");
        }

        if (count > text.Length - position)
        {
            throw new System.InvalidOperationException("The requested text extends beyond the bounded input.");
        }

        string result = text.Substring(position, count);
        position = position + count;
        return result;
    }

    internal bool TrySkip(int count)
    {
        if (count < 0 || count > text.Length - position)
        {
            return false;
        }

        position = position + count;
        return true;
    }
}
