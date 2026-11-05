using System;

namespace html;

internal static class HtmlAdoptionAgency
{
    internal const int MaximumOuterIterations = 8;
    internal const int MaximumInnerActiveEntries = 3;

    internal static HtmlTreeBuilderAction Run(HtmlTreeBuilder builder,
        HtmlToken token)
    {
        if (builder == null)
        {
            throw new ArgumentNullException("builder");
        }

        if (token == null)
        {
            throw new ArgumentNullException("token");
        }

        string subject = token.GetName();
        HtmlOpenElementStack stack = builder.GetOpenElements();
        HtmlFormattingList formatting = builder.GetFormattingElements();
        HtmlNode? current = stack.GetCurrentNode();
        if (current != null && IsHtmlElement(current, subject)
            && !formatting.Contains(current))
        {
            stack.Pop();
            return HtmlTreeBuilderAction.Consume();
        }

        int outerLoopCounter = 0;
        while (outerLoopCounter < MaximumOuterIterations)
        {
            outerLoopCounter = outerLoopCounter + 1;
            HtmlNode? formattingElement = formatting.GetLastHtmlElement(
                subject);
            if (formattingElement == null)
            {
                return HtmlTreeBuilderInBodyMode.ProcessAnyOtherEndTag(
                    builder, token);
            }

            if (!stack.Contains(formattingElement))
            {
                builder.ReportParseError(token,
                    "The active formatting element was no longer open.");
                formatting.Remove(formattingElement);
                return HtmlTreeBuilderAction.Consume();
            }

            if (!stack.HasInScope(formattingElement))
            {
                builder.ReportParseError(token,
                    "The active formatting element was not in scope.");
                return HtmlTreeBuilderAction.Consume();
            }

            if (formattingElement != stack.GetCurrentNode())
            {
                builder.ReportParseError(token,
                    "The formatting end tag was misnested.");
            }

            HtmlNode? furthestBlock = stack.GetFirstSpecialElementAfter(
                formattingElement);
            if (furthestBlock == null)
            {
                stack.PopUntil(formattingElement);
                formatting.Remove(formattingElement);
                return HtmlTreeBuilderAction.Consume();
            }

            int formattingStackIndex = stack.GetIndex(formattingElement);
            HtmlNode? commonAncestor = stack.GetNode(
                formattingStackIndex - 1);
            int bookmark = formatting.GetIndex(formattingElement);
            HtmlToken? formattingToken = formatting.GetToken(bookmark);
            HtmlNode lastNode = furthestBlock;
            HtmlNode[] originalStack = stack.GetNodes();
            int traversalIndex = stack.GetIndex(furthestBlock) - 1;
            int innerLoopCounter = 0;
            bool traversing = true;
            while (traversing && traversalIndex >= 0)
            {
                innerLoopCounter = innerLoopCounter + 1;
                HtmlNode node = originalStack[traversalIndex];
                traversalIndex = traversalIndex - 1;
                if (node == formattingElement)
                {
                    traversing = false;
                }
                else
                {
                    int formattingIndex = formatting.GetIndex(node);
                    if (innerLoopCounter > MaximumInnerActiveEntries
                        && formattingIndex >= 0)
                    {
                        formatting.RemoveAt(formattingIndex);
                        if (formattingIndex < bookmark)
                        {
                            bookmark = bookmark - 1;
                        }

                        formattingIndex = -1;
                    }

                    if (formattingIndex < 0)
                    {
                        stack.Remove(node);
                    }
                    else
                    {
                        HtmlToken? nodeToken = formatting.GetToken(
                            formattingIndex);
                        if (nodeToken == null)
                        {
                            throw new InvalidOperationException(
                                "A formatting entry must retain its token.");
                        }

                        HtmlNode? replacement = builder.CreateHtmlElement(
                            nodeToken);
                        if (replacement == null)
                        {
                            return HtmlTreeBuilderAction.Consume();
                        }

                        formatting.Replace(node, replacement);
                        stack.Replace(node, replacement);
                        if (lastNode == furthestBlock)
                        {
                            bookmark = formatting.GetIndex(replacement) + 1;
                        }

                        replacement.ReparentChildStorage(lastNode, null);
                        lastNode = replacement;
                    }
                }
            }

            HtmlInsertionLocation location =
                builder.GetAdjustedInsertionLocationForTarget(commonAncestor);
            builder.MoveNodeAtLocation(lastNode, location);
            if (formattingToken == null)
            {
                throw new InvalidOperationException(
                    "A formatting entry must retain its token.");
            }

            HtmlNode? newFormattingElement = builder.CreateHtmlElement(
                formattingToken);
            if (newFormattingElement == null)
            {
                return HtmlTreeBuilderAction.Consume();
            }

            HtmlNode[] children = furthestBlock.GetChildrenStorage();
            int childIndex = 0;
            while (childIndex < children.Length)
            {
                newFormattingElement.ReparentChildStorage(
                    children[childIndex], null);
                childIndex = childIndex + 1;
            }

            furthestBlock.ReparentChildStorage(newFormattingElement, null);
            int oldFormattingIndex = formatting.GetIndex(formattingElement);
            formatting.RemoveAt(oldFormattingIndex);
            if (oldFormattingIndex < bookmark)
            {
                bookmark = bookmark - 1;
            }

            if (bookmark < 0)
            {
                bookmark = 0;
            }
            else if (bookmark > formatting.GetCount())
            {
                bookmark = formatting.GetCount();
            }

            formatting.InsertAt(bookmark, newFormattingElement,
                formattingToken);
            stack.Remove(formattingElement);
            if (!stack.InsertAfter(furthestBlock, newFormattingElement))
            {
                builder.ReportParseError(token,
                    "The adoption agency could not update the open-element stack.");
                return HtmlTreeBuilderAction.Consume();
            }
        }

        return HtmlTreeBuilderAction.Consume();
    }

    internal static bool IsHtmlElement(HtmlNode node, string name)
    {
        return node.GetNamespaceStorage() == HtmlNamespace.Html
            && node.GetLocalNameStorage() == name;
    }
}
