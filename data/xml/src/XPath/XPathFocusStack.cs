using System;
using System.Collections.Generic;

namespace xml;

internal class XPathFocusFrame
{
    internal XdmSequence? ContextItem;
    internal int Position;
    internal int Size;

    internal XPathFocusFrame(XdmSequence? contextItem, int position, int size)
    {
        ContextItem = contextItem;
        Position = position;
        Size = size;
    }
}

internal class XPathFocusStack
{
    internal List<XPathFocusFrame> Frames;

    internal XPathFocusStack(XdmSequence? contextItem, int position, int size)
    {
        Frames = new List<XPathFocusFrame>();
        PushXPathFocus(contextItem, position, size);
    }

    internal XPathFocusFrame ReadXPathFocus()
    {
        return Frames[Frames.Count - 1];
    }

    internal void PushXPathFocus(XdmSequence? contextItem, int position, int size)
    {
        ValidateXPathFocus(contextItem, position, size);
        XdmSequence? snapshot = contextItem == null ? null : new XdmSequenceOperations().FreezeXdmSequenceStorage(contextItem);
        Frames.Add(new XPathFocusFrame(snapshot, position, size));
    }

    internal void PopXPathFocus()
    {
        if (Frames.Count <= 1)
        {
            throw new InvalidOperationException("The root XPath focus frame cannot be removed.");
        }

        Frames.RemoveAt(Frames.Count - 1);
    }

    internal void ValidateXPathFocus(XdmSequence? contextItem, int position, int size)
    {
        if (contextItem == null || contextItem.Count == 0)
        {
            if (position != 0 || size != 0)
            {
                throw new ArgumentException("An absent XPath context item requires a zero position and size.", "contextItem");
            }

            return;
        }

        new XdmSequenceOperations().ValidateXdmSequenceStorage(contextItem);
        if (contextItem.Count != 1)
        {
            throw new ArgumentException("An XPath context item must be a singleton sequence.", "contextItem");
        }

        if (size <= 0 || position <= 0 || position > size)
        {
            throw new ArgumentException("An XPath focus position must be within its positive focus size.", "position");
        }
    }
}
