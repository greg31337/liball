using System;

namespace html;

internal class HtmlFormattingEntry
{
    internal bool marker;
    internal HtmlNode? element;
    internal HtmlToken? token;

    internal HtmlFormattingEntry(bool marker, HtmlNode? element, HtmlToken? token)
    {
        this.marker = marker;
        this.element = element;
        this.token = token;
    }

    internal static HtmlFormattingEntry CreateMarker()
    {
        return new HtmlFormattingEntry(true, null, null);
    }

    internal static HtmlFormattingEntry Create(HtmlNode element,
        HtmlToken token)
    {
        ValidateElementAndToken(element, token);
        return new HtmlFormattingEntry(false, element, token);
    }

    internal bool IsMarker()
    {
        return this.marker;
    }

    internal HtmlNode? GetElement()
    {
        return this.element;
    }

    internal HtmlToken? GetToken()
    {
        return this.token;
    }

    internal HtmlNode GetRequiredElement()
    {
        if (this.element == null)
        {
            throw new InvalidOperationException(
                "A formatting marker has no element.");
        }

        return this.element;
    }

    internal HtmlToken GetRequiredToken()
    {
        if (this.token == null)
        {
            throw new InvalidOperationException(
                "A formatting marker has no token.");
        }

        return this.token;
    }

    internal static void ValidateElementAndToken(HtmlNode element,
        HtmlToken token)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        if (element.GetKindStorage() != HtmlNodeKind.Element
            || element.GetNamespaceStorage() != HtmlNamespace.Html
            || !HtmlNames.IsFormattingElement(
                element.GetLocalNameStorage()))
        {
            throw new ArgumentException(
                "An active formatting entry must contain an HTML formatting element.",
                "element");
        }

        if (token.GetKind() != HtmlTokenKind.StartTag
            || token.GetName() != element.GetLocalNameStorage())
        {
            throw new ArgumentException(
                "An active formatting entry requires its matching start-tag token.",
                "token");
        }

        if (!AttributesEqual(element.GetAttributesStorage(),
            token.GetAttributes()))
        {
            throw new ArgumentException(
                "The formatting element attributes must match its start-tag token.",
                "token");
        }
    }

    internal static bool AttributesEqual(HtmlAttribute[] left,
        HtmlAttribute[] right)
    {
        if (left.Length != right.Length)
        {
            return false;
        }

        bool[] matched = new bool[right.Length];
        int leftIndex = 0;
        while (leftIndex < left.Length)
        {
            HtmlAttribute leftAttribute = left[leftIndex];
            bool found = false;
            int rightIndex = 0;
            while (rightIndex < right.Length && !found)
            {
                if (!matched[rightIndex]
                    && AttributeEquals(leftAttribute, right[rightIndex]))
                {
                    matched[rightIndex] = true;
                    found = true;
                }

                rightIndex = rightIndex + 1;
            }

            if (!found)
            {
                return false;
            }

            leftIndex = leftIndex + 1;
        }

        return true;
    }

    internal static bool AttributeEquals(HtmlAttribute left, HtmlAttribute right)
    {
        return left.GetName() == right.GetName()
            && left.GetNamespaceUri() == right.GetNamespaceUri()
            && left.GetValue() == right.GetValue();
    }
}

internal class HtmlFormattingList
{
    internal HtmlFormattingEntry[] entries;
    internal int count;

    internal HtmlFormattingList()
    {
        this.entries = new HtmlFormattingEntry[16];
        this.count = 0;
    }

    internal int GetCount()
    {
        return this.count;
    }

    internal HtmlFormattingEntry[] GetEntries()
    {
        HtmlFormattingEntry[] result = new HtmlFormattingEntry[this.count];
        Array.Copy(this.entries, result, this.count);
        return result;
    }

    internal bool IsMarker(int index)
    {
        this.ValidateIndex(index);
        return this.entries[index].IsMarker();
    }

    internal HtmlNode? GetElement(int index)
    {
        this.ValidateIndex(index);
        return this.entries[index].GetElement();
    }

    internal HtmlToken? GetToken(int index)
    {
        this.ValidateIndex(index);
        return this.entries[index].GetToken();
    }

    internal void AddMarker()
    {
        this.EnsureCapacity(this.count + 1);
        this.entries[this.count] = HtmlFormattingEntry.CreateMarker();
        this.count = this.count + 1;
    }

    internal void Add(HtmlNode element, HtmlToken token)
    {
        HtmlFormattingEntry entry = HtmlFormattingEntry.Create(element, token);
        if (this.GetIndex(element) >= 0)
        {
            throw new InvalidOperationException(
                "An element cannot occur more than once in the active formatting list.");
        }

        int earliestMatch = -1;
        int matchCount = 0;
        int index = this.count - 1;
        while (index >= 0 && !this.entries[index].IsMarker())
        {
            if (AreSameFamily(this.entries[index], entry))
            {
                earliestMatch = index;
                matchCount = matchCount + 1;
            }

            index = index - 1;
        }

        if (matchCount >= 3)
        {
            this.RemoveAt(earliestMatch);
        }

        this.EnsureCapacity(this.count + 1);
        this.entries[this.count] = entry;
        this.count = this.count + 1;
    }

    internal int GetIndex(HtmlNode element)
    {
        if (element == null)
        {
            throw new ArgumentNullException("element");
        }

        int index = 0;
        while (index < this.count)
        {
            HtmlFormattingEntry entry = this.entries[index];
            if (!entry.IsMarker() && entry.GetElement() == element)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    internal bool Contains(HtmlNode element)
    {
        return this.GetIndex(element) >= 0;
    }

    internal HtmlNode? GetLastHtmlElement(string localName)
    {
        ValidateLocalName(localName);
        int index = this.count - 1;
        while (index >= 0)
        {
            HtmlFormattingEntry entry = this.entries[index];
            if (entry.IsMarker())
            {
                return null;
            }

            HtmlNode element = entry.GetRequiredElement();
            if (element.GetLocalNameStorage() == localName)
            {
                return element;
            }

            index = index - 1;
        }

        return null;
    }

    internal bool Remove(HtmlNode element)
    {
        int index = this.GetIndex(element);
        if (index < 0)
        {
            return false;
        }

        this.RemoveAt(index);
        return true;
    }

    internal void RemoveAt(int index)
    {
        this.ValidateIndex(index);
        int current = index;
        while (current + 1 < this.count)
        {
            this.entries[current] = this.entries[current + 1];
            current = current + 1;
        }

        this.count = this.count - 1;
        this.entries[this.count] = HtmlFormattingEntry.CreateMarker();
    }

    internal bool Replace(HtmlNode element, HtmlNode replacement)
    {
        if (replacement == null)
        {
            throw new ArgumentNullException("replacement");
        }

        int index = this.GetIndex(element);
        if (index < 0)
        {
            return false;
        }

        HtmlToken token = this.entries[index].GetRequiredToken();
        HtmlFormattingEntry.ValidateElementAndToken(replacement, token);
        int replacementIndex = this.GetIndex(replacement);
        if (replacementIndex >= 0 && replacement != element)
        {
            throw new InvalidOperationException(
                "An element cannot occur more than once in the active formatting list.");
        }

        this.entries[index] = HtmlFormattingEntry.Create(replacement, token);
        return true;
    }

    internal void InsertAt(int index, HtmlNode element, HtmlToken token)
    {
        if (index < 0 || index > this.count)
        {
            throw new ArgumentOutOfRangeException("index", index,
                "The formatting-list insertion index is outside the list.");
        }

        if (this.GetIndex(element) >= 0)
        {
            throw new InvalidOperationException(
                "An element cannot occur more than once in the active formatting list.");
        }

        HtmlFormattingEntry entry = HtmlFormattingEntry.Create(element, token);
        this.EnsureCapacity(this.count + 1);
        int current = this.count;
        while (current > index)
        {
            this.entries[current] = this.entries[current - 1];
            current = current - 1;
        }

        this.entries[index] = entry;
        this.count = this.count + 1;
    }

    internal int ClearToLastMarker()
    {
        int removed = 0;
        bool running = true;
        while (this.count > 0 && running)
        {
            bool marker = this.entries[this.count - 1].IsMarker();
            this.RemoveAt(this.count - 1);
            removed = removed + 1;
            if (marker)
            {
                running = false;
            }
        }

        return removed;
    }

    internal bool Reconstruct(HtmlOpenElementStack openElements,
        Func<HtmlToken, HtmlNode?> createElement)
    {
        if (openElements == null)
        {
            throw new ArgumentNullException("openElements");
        }

        if (createElement == null)
        {
            throw new ArgumentNullException("createElement");
        }

        if (this.count == 0)
        {
            return true;
        }

        int index = this.count - 1;
        HtmlFormattingEntry entry = this.entries[index];
        if (entry.IsMarker()
            || openElements.Contains(entry.GetElement()))
        {
            return true;
        }

        bool rewinding = true;
        while (index > 0 && rewinding)
        {
            HtmlFormattingEntry previous = this.entries[index - 1];
            if (previous.IsMarker()
                || openElements.Contains(previous.GetElement()))
            {
                rewinding = false;
            }
            else
            {
                index = index - 1;
            }
        }

        while (index < this.count)
        {
            if (!openElements.CanPush())
            {
                return false;
            }

            HtmlToken token = this.entries[index].GetRequiredToken();
            HtmlNode? element = createElement(token);
            if (element == null)
            {
                return false;
            }

            HtmlFormattingEntry.ValidateElementAndToken(element, token);
            if (!openElements.Push(element))
            {
                return false;
            }

            this.entries[index] = HtmlFormattingEntry.Create(element, token);
            index = index + 1;
        }

        return true;
    }

    internal bool RemoveFromListAndStack(HtmlNode element,
        HtmlOpenElementStack openElements)
    {
        if (openElements == null)
        {
            throw new ArgumentNullException("openElements");
        }

        bool listRemoved = this.Remove(element);
        bool stackRemoved = openElements.Remove(element);
        return listRemoved || stackRemoved;
    }

    internal bool ReplaceInListAndStack(HtmlNode element, HtmlNode replacement,
        HtmlOpenElementStack openElements)
    {
        if (openElements == null)
        {
            throw new ArgumentNullException("openElements");
        }

        int listIndex = this.GetIndex(element);
        int stackIndex = openElements.GetIndex(element);
        if (listIndex < 0 && stackIndex < 0)
        {
            return false;
        }

        if (replacement == null)
        {
            throw new ArgumentNullException("replacement");
        }

        int replacementListIndex = this.GetIndex(replacement);
        int replacementStackIndex = openElements.GetIndex(replacement);
        if (replacement != element
            && (replacementListIndex >= 0 || replacementStackIndex >= 0))
        {
            throw new InvalidOperationException(
                "The replacement element is already active or open.");
        }

        if (listIndex >= 0)
        {
            HtmlFormattingEntry.ValidateElementAndToken(replacement,
                this.entries[listIndex].GetRequiredToken());
        }

        if (listIndex >= 0)
        {
            this.Replace(element, replacement);
        }

        if (stackIndex >= 0)
        {
            openElements.Replace(element, replacement);
        }

        return true;
    }

    internal void ValidateIndex(int index)
    {
        if (index < 0 || index >= this.count)
        {
            throw new ArgumentOutOfRangeException("index", index,
                "The active-formatting index is outside the list.");
        }
    }

    internal void EnsureCapacity(int requiredCapacity)
    {
        if (requiredCapacity <= this.entries.Length)
        {
            return;
        }

        int newCapacity = BufferCapacity.GetNext(this.entries.Length,
            requiredCapacity, int.MaxValue);

        HtmlFormattingEntry[] result = new HtmlFormattingEntry[newCapacity];
        Array.Copy(this.entries, result, this.count);
        this.entries = result;
    }

    internal static bool AreSameFamily(HtmlFormattingEntry left,
        HtmlFormattingEntry right)
    {
        HtmlNode leftElement = left.GetRequiredElement();
        HtmlNode rightElement = right.GetRequiredElement();
        return leftElement.GetLocalNameStorage()
                == rightElement.GetLocalNameStorage()
            && leftElement.GetNamespaceStorage()
                == rightElement.GetNamespaceStorage()
            && HtmlFormattingEntry.AttributesEqual(
                left.GetRequiredToken().GetAttributes(),
                right.GetRequiredToken().GetAttributes());
    }

    internal static void ValidateLocalName(string localName)
    {
        if (localName == null)
        {
            throw new ArgumentNullException("localName");
        }

        if (localName.Length == 0)
        {
            throw new ArgumentException(
                "An element local name cannot be empty.", "localName");
        }
    }
}
