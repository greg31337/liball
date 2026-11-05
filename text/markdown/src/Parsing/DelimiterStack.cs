using System;

namespace markdown;

internal class MarkdownDelimiter
{
    internal string Marker;
    internal int Length;
    internal int OriginalLength;
    internal int Position;
    internal bool CanOpen;
    internal bool CanClose;
    internal bool Active;
    internal MarkdownNode? Node;
    internal MarkdownDelimiter? Previous;
    internal MarkdownDelimiter? Next;

    internal MarkdownDelimiter()
    {
        Marker = "";
        Length = 0;
        OriginalLength = 0;
        Position = 0;
        CanOpen = false;
        CanClose = false;
        Active = true;
        Node = null;
        Previous = null;
        Next = null;
    }

    internal static MarkdownDelimiter Create(string marker, int length, int position, bool canOpen, bool canClose)
    {
        ValidateMarker(marker);

        if (length <= 0)
        {
            throw new ArgumentOutOfRangeException("length");
        }

        if (position < 0)
        {
            throw new ArgumentOutOfRangeException("position");
        }

        MarkdownDelimiter delimiter = new MarkdownDelimiter();
        delimiter.Marker = marker;
        delimiter.Length = length;
        delimiter.OriginalLength = length;
        delimiter.Position = position;
        delimiter.CanOpen = canOpen;
        delimiter.CanClose = canClose;
        return delimiter;
    }

    internal static MarkdownDelimiter Create(string marker, int length, int position, bool canOpen, bool canClose, MarkdownNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException("node");
        }

        MarkdownDelimiter delimiter = Create(marker, length, position, canOpen, canClose);
        delimiter.Node = node;
        return delimiter;
    }

    internal bool HasRemainingDelimiter()
    {
        return Length > 0;
    }

    internal void UseDelimiterCharacters(int count)
    {
        if (count < 0)
        {
            throw new ArgumentOutOfRangeException("count");
        }

        if (count > Length)
        {
            throw new ArgumentOutOfRangeException("count");
        }

        Length = Length - count;
    }

    internal void Deactivate()
    {
        Active = false;
    }

    internal static void ValidateMarker(string marker)
    {
        if (marker == null)
        {
            throw new ArgumentNullException("marker");
        }

        if (marker.Length == 0)
        {
            throw new ArgumentException("Delimiter marker cannot be empty.");
        }
    }
}

internal class MarkdownDelimiterStack
{
    internal MarkdownDelimiter? Top;
    internal int Count;

    internal MarkdownDelimiterStack()
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

    internal MarkdownDelimiter? Peek()
    {
        return Top;
    }

    internal void Push(MarkdownDelimiter delimiter)
    {
        if (delimiter == null)
        {
            throw new ArgumentNullException("delimiter");
        }

        if (delimiter.Previous != null || delimiter.Next != null || Top == delimiter)
        {
            throw new ArgumentException("Delimiter is already linked.");
        }

        delimiter.Previous = Top;
        delimiter.Next = null;
        if (Top != null)
        {
            Top.Next = delimiter;
        }

        Top = delimiter;
        Count = Count + 1;
    }

    internal MarkdownDelimiter PushMarker(string marker, int length, int position, bool canOpen, bool canClose)
    {
        MarkdownDelimiter delimiter = MarkdownDelimiter.Create(marker, length, position, canOpen, canClose);
        Push(delimiter);
        return delimiter;
    }

    internal MarkdownDelimiter PushNode(string marker, int length, int position, bool canOpen, bool canClose, MarkdownNode node)
    {
        MarkdownDelimiter delimiter = MarkdownDelimiter.Create(marker, length, position, canOpen, canClose, node);
        Push(delimiter);
        return delimiter;
    }

    internal MarkdownDelimiter? Pop()
    {
        if (Top == null)
        {
            return null;
        }

        MarkdownDelimiter delimiter = Top;
        Remove(delimiter);
        return delimiter;
    }

    internal bool Remove(MarkdownDelimiter delimiter)
    {
        if (delimiter == null)
        {
            throw new ArgumentNullException("delimiter");
        }

        if (!Contains(delimiter))
        {
            return false;
        }

        MarkdownDelimiter? previous = delimiter.Previous;
        MarkdownDelimiter? next = delimiter.Next;

        if (previous != null)
        {
            previous.Next = next;
        }

        if (next != null)
        {
            next.Previous = previous;
        }

        if (Top == delimiter)
        {
            Top = previous;
        }

        delimiter.Previous = null;
        delimiter.Next = null;
        Count = Count - 1;
        return true;
    }

    internal int RemoveUpTo(MarkdownDelimiter delimiter)
    {
        if (delimiter == null)
        {
            throw new ArgumentNullException("delimiter");
        }

        int removed = 0;
        while (Top != null)
        {
            MarkdownDelimiter current = Top;
            Remove(current);
            removed = removed + 1;
            if (current == delimiter)
            {
                break;
            }
        }

        return removed;
    }

    internal bool Contains(MarkdownDelimiter delimiter)
    {
        if (delimiter == null)
        {
            throw new ArgumentNullException("delimiter");
        }

        MarkdownDelimiter? current = Top;
        while (current != null)
        {
            if (current == delimiter)
            {
                return true;
            }

            current = current.Previous;
        }

        return false;
    }

    internal MarkdownDelimiter? FindMatchingOpener(MarkdownDelimiter closer)
    {
        if (closer == null)
        {
            throw new ArgumentNullException("closer");
        }

        if (!closer.CanClose)
        {
            return null;
        }

        MarkdownDelimiter? current = closer.Previous;
        while (current != null)
        {
            if (current.Active && current.CanOpen && current.Marker == closer.Marker)
            {
                return current;
            }

            current = current.Previous;
        }

        return null;
    }

    internal MarkdownDelimiter? FindMatchingOpenerByMarker(string marker)
    {
        MarkdownDelimiter.ValidateMarker(marker);

        MarkdownDelimiter? current = Top;
        while (current != null)
        {
            if (current.Active && current.CanOpen && current.Marker == marker)
            {
                return current;
            }

            current = current.Previous;
        }

        return null;
    }

    internal int DeactivateOpeners(string marker)
    {
        MarkdownDelimiter.ValidateMarker(marker);

        int deactivated = 0;
        MarkdownDelimiter? current = Top;
        while (current != null)
        {
            if (current.CanOpen && current.Marker == marker && current.Active)
            {
                current.Deactivate();
                deactivated = deactivated + 1;
            }

            current = current.Previous;
        }

        return deactivated;
    }

    internal MarkdownDelimiter[] GetDelimiters()
    {
        MarkdownDelimiter[] delimiters = new MarkdownDelimiter[Count];
        int index = Count - 1;
        MarkdownDelimiter? current = Top;
        while (current != null)
        {
            delimiters[index] = current;
            index = index - 1;
            current = current.Previous;
        }

        return delimiters;
    }

    internal void Clear()
    {
        while (Top != null)
        {
            Remove(Top);
        }
    }
}
