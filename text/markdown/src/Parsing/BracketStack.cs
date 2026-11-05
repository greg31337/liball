using System;

namespace markdown;

internal class MarkdownBracket
{
    internal int Position;
    internal bool IsImage;
    internal bool Active;
    internal MarkdownNode? Node;
    internal MarkdownDelimiter? Delimiter;
    internal MarkdownBracket? Previous;
    internal MarkdownBracket? Next;

    internal MarkdownBracket()
    {
        Position = 0;
        IsImage = false;
        Active = true;
        Node = null;
        Delimiter = null;
        Previous = null;
        Next = null;
    }

    internal static MarkdownBracket Create(int position, bool isImage)
    {
        if (position < 0)
        {
            throw new ArgumentOutOfRangeException("position");
        }

        MarkdownBracket bracket = new MarkdownBracket();
        bracket.Position = position;
        bracket.IsImage = isImage;
        return bracket;
    }

    internal static MarkdownBracket Create(int position, bool isImage, MarkdownNode node)
    {
        if (position < 0)
        {
            throw new ArgumentOutOfRangeException("position");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        MarkdownBracket bracket = Create(position, isImage);
        bracket.Node = node;
        return bracket;
    }

    internal static MarkdownBracket Create(int position, bool isImage, MarkdownNode node, MarkdownDelimiter delimiter)
    {
        if (position < 0)
        {
            throw new ArgumentOutOfRangeException("position");
        }

        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        if (delimiter == null)
        {
            throw new ArgumentNullException("delimiter");
        }

        MarkdownBracket bracket = Create(position, isImage, node);
        bracket.Delimiter = delimiter;
        return bracket;
    }

    internal void Deactivate()
    {
        Active = false;
    }
}

internal class MarkdownBracketStack
{
    internal MarkdownBracket? Top;
    internal int Count;

    internal MarkdownBracketStack()
    {
        Top = null;
        Count = 0;
    }

    internal int GetCount()
    {
        return Count;
    }

    internal bool IsEmpty()
    {
        return Count == 0;
    }

    internal MarkdownBracket? Peek()
    {
        return Top;
    }

    internal void Push(MarkdownBracket bracket)
    {
        if (bracket == null)
        {
            throw new ArgumentNullException("bracket");
        }

        if (bracket.Previous != null || bracket.Next != null || Top == bracket)
        {
            throw new ArgumentException("Bracket is already linked.");
        }

        bracket.Previous = Top;
        bracket.Next = null;
        if (Top != null)
        {
            Top.Next = bracket;
        }

        Top = bracket;
        Count = Count + 1;
    }

    internal MarkdownBracket PushPosition(int position, bool isImage)
    {
        MarkdownBracket bracket = MarkdownBracket.Create(position, isImage);
        Push(bracket);
        return bracket;
    }

    internal MarkdownBracket PushNode(int position, bool isImage, MarkdownNode node)
    {
        MarkdownBracket bracket = MarkdownBracket.Create(position, isImage, node);
        Push(bracket);
        return bracket;
    }

    internal MarkdownBracket PushNodeWithDelimiter(int position, bool isImage, MarkdownNode node, MarkdownDelimiter delimiter)
    {
        MarkdownBracket bracket = MarkdownBracket.Create(position, isImage, node, delimiter);
        Push(bracket);
        return bracket;
    }

    internal MarkdownBracket? Pop()
    {
        if (Top == null)
        {
            return null;
        }

        MarkdownBracket bracket = Top;
        Remove(bracket);
        return bracket;
    }

    internal bool Remove(MarkdownBracket bracket)
    {
        if (bracket == null)
        {
            throw new ArgumentNullException("bracket");
        }

        if (!Contains(bracket))
        {
            return false;
        }

        MarkdownBracket? previous = bracket.Previous;
        MarkdownBracket? next = bracket.Next;

        if (previous != null)
        {
            previous.Next = next;
        }

        if (next != null)
        {
            next.Previous = previous;
        }

        if (Top == bracket)
        {
            Top = previous;
        }

        bracket.Previous = null;
        bracket.Next = null;
        Count = Count - 1;
        return true;
    }

    internal int RemoveUpTo(MarkdownBracket bracket)
    {
        if (bracket == null)
        {
            throw new ArgumentNullException("bracket");
        }

        int removed = 0;
        while (Top != null)
        {
            MarkdownBracket current = Top;
            Remove(current);
            removed = removed + 1;
            if (current == bracket)
            {
                break;
            }
        }

        return removed;
    }

    internal bool Contains(MarkdownBracket bracket)
    {
        if (bracket == null)
        {
            throw new ArgumentNullException("bracket");
        }

        MarkdownBracket? current = Top;
        while (current != null)
        {
            if (current == bracket)
            {
                return true;
            }

            current = current.Previous;
        }

        return false;
    }

    internal MarkdownBracket? FindActiveOpener()
    {
        MarkdownBracket? current = Top;
        while (current != null)
        {
            if (current.Active)
            {
                return current;
            }

            current = current.Previous;
        }

        return null;
    }

    internal MarkdownBracket? FindActiveOpenerByImageKind(bool isImage)
    {
        MarkdownBracket? current = Top;
        while (current != null)
        {
            if (current.Active && current.IsImage == isImage)
            {
                return current;
            }

            current = current.Previous;
        }

        return null;
    }

    internal int DeactivateLinkOpeners()
    {
        int deactivated = 0;
        MarkdownBracket? current = Top;
        while (current != null)
        {
            if (current.Active && !current.IsImage)
            {
                current.Deactivate();
                deactivated = deactivated + 1;
            }

            current = current.Previous;
        }

        return deactivated;
    }

    internal MarkdownBracket[] GetBrackets()
    {
        MarkdownBracket[] brackets = new MarkdownBracket[Count];
        int index = Count - 1;
        MarkdownBracket? current = Top;
        while (current != null)
        {
            brackets[index] = current;
            index = index - 1;
            current = current.Previous;
        }

        return brackets;
    }

    internal void Clear()
    {
        while (Top != null)
        {
            Remove(Top);
        }
    }
}
