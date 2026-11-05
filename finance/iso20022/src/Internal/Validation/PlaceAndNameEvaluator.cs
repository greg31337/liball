using System.Collections.Generic;
using System.Text;

namespace iso20022;

internal class PlaceAndNameEvaluator
{
    internal static PlaceAndNameEvaluationResult EvaluatePath(
        PlaceAndNameParseResult expression,
        DynamicXmlNode tree,
        DynamicXmlNode parent,
        XmlPathData parentPath,
        List<NamespaceDeclarationData> declarations,
        ResourceLimits limits)
    {
        PlaceAndNameEvaluationResult result = new PlaceAndNameEvaluationResult();
        List<PlaceAndNameMatch> current = new List<PlaceAndNameMatch>();
        PlaceAndNameMatch initial = new PlaceAndNameMatch();
        if (expression.Absolute)
        {
            initial.Node = tree;
        }
        else
        {
            initial.Node = parent;
            initial.Path = CopyPath(parentPath);
        }
        current.Add(initial);

        int stepIndex = 0;
        while (stepIndex < expression.Steps.Count)
        {
            PlaceAndNameStep step = expression.Steps[stepIndex];
            string? namespaceUri = ResolvePrefix(step.Prefix, declarations);
            if (namespaceUri == null)
            {
                result.Success = false;
                result.ErrorCode = "supplementary.place_and_name_prefix_unresolved";
                result.ErrorMessage = "The PlaceAndName prefix '" + step.Prefix +
                    "' is not declared at the supplementary-data location.";
                return result;
            }

            List<PlaceAndNameMatch> next = new List<PlaceAndNameMatch>();
            int currentIndex = 0;
            while (currentIndex < current.Count)
            {
                AddChildMatches(
                    current[currentIndex],
                    step,
                    namespaceUri,
                    next,
                    limits,
                    result);
                if (!result.Success)
                {
                    return result;
                }
                currentIndex++;
            }
            current = next;
            if (current.Count == 0)
            {
                break;
            }
            stepIndex++;
        }

        int matchIndex = 0;
        while (matchIndex < current.Count)
        {
            result.Paths.Add(CopyPath(current[matchIndex].Path));
            matchIndex++;
        }
        return result;
    }

    internal static void AddChildMatches(
        PlaceAndNameMatch context,
        PlaceAndNameStep step,
        string namespaceUri,
        List<PlaceAndNameMatch> matches,
        ResourceLimits limits,
        PlaceAndNameEvaluationResult result)
    {
        int matchingCount = CountMatchingChildren(context.Node, step, namespaceUri);
        int matchingPosition = 0;
        int index = 0;
        while (index < context.Node.Children.Count)
        {
            DynamicXmlNode child = context.Node.Children[index];
            if (IsMatch(child, step, namespaceUri))
            {
                matchingPosition++;
                if (!step.HasPosition || matchingPosition == step.Position)
                {
                    if (matches.Count >= limits.MaxCollectionCount)
                    {
                        result.Success = false;
                        result.ErrorCode = "supplementary.location_match_limit";
                        result.ErrorMessage = "The resolved PlaceAndName targets exceed the configured collection limit.";
                        return;
                    }
                    PlaceAndNameMatch match = new PlaceAndNameMatch();
                    match.Node = child;
                    match.Path = AppendPath(
                        context.Path,
                        child.Name,
                        matchingCount > 1,
                        matchingPosition);
                    matches.Add(match);
                }
            }
            index++;
        }
    }

    internal static int CountMatchingChildren(
        DynamicXmlNode parent,
        PlaceAndNameStep step,
        string namespaceUri)
    {
        int count = 0;
        int index = 0;
        while (index < parent.Children.Count)
        {
            if (IsMatch(parent.Children[index], step, namespaceUri))
            {
                count++;
            }
            index++;
        }
        return count;
    }

    internal static bool IsMatch(
        DynamicXmlNode node,
        PlaceAndNameStep step,
        string namespaceUri)
    {
        return node.Kind == XmlNodeKind.Element &&
            node.Name.LocalName == step.LocalName &&
            node.Name.NamespaceUri == namespaceUri;
    }

    internal static string? ResolvePrefix(
        string prefix,
        List<NamespaceDeclarationData> declarations)
    {
        if (prefix == "xml")
        {
            return Iso20022Namespaces.Xml;
        }
        int index = declarations.Count - 1;
        while (index >= 0)
        {
            if (declarations[index].Prefix == prefix)
            {
                return declarations[index].NamespaceUri;
            }
            index--;
        }
        if (prefix.Length == 0)
        {
            return string.Empty;
        }
        return null;
    }

    internal static XmlPathData AppendPath(
        XmlPathData source,
        QualifiedNameData name,
        bool includePosition,
        int position)
    {
        XmlPathData path = CopyPath(source);
        path.Segments.Add(XmlPathTracker.CopyName(name));
        StringBuilder display = new StringBuilder(path.Value);
        display.Append('/');
        if (name.NamespaceUri.Length > 0)
        {
            display.Append("Q{");
            display.Append(name.NamespaceUri);
            display.Append('}');
        }
        display.Append(name.LocalName);
        if (includePosition)
        {
            display.Append('[');
            display.Append(position);
            display.Append(']');
        }
        path.Value = display.ToString();
        return path;
    }

    internal static XmlPathData CopyPath(XmlPathData source)
    {
        XmlPathData path = new XmlPathData();
        path.Value = source.Value;
        int index = 0;
        while (index < source.Segments.Count)
        {
            path.Segments.Add(XmlPathTracker.CopyName(source.Segments[index]));
            index++;
        }
        return path;
    }
}
