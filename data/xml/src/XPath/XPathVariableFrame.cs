using System;
using System.Collections.Generic;

namespace xml;

internal class XPathVariableFrame
{
    internal XPathVariableFrame? Parent;
    internal List<XPathVariableBinding> Variables;

    internal XPathVariableFrame(XPathVariableFrame? parent)
    {
        Parent = parent;
        Variables = new List<XPathVariableBinding>();
    }
}

internal class XPathVariableStack
{
    internal XPathVariableFrame Current;

    internal XPathVariableStack(XPathVariableBinding[] variables)
    {
        Current = new XPathVariableFrame(null);
        int index = 0;
        while (index < variables.Length)
        {
            SetXPathVariable(variables[index]);
            index = index + 1;
        }
    }

    internal void PushXPathVariableFrame()
    {
        Current = new XPathVariableFrame(Current);
    }

    internal void PopXPathVariableFrame()
    {
        if (Current.Parent == null)
        {
            throw new InvalidOperationException("The root XPath variable frame cannot be removed.");
        }

        Current = Current.Parent;
    }

    internal void SetXPathVariable(XPathVariableBinding variable)
    {
        Guard.NotNull(variable, "variable");
        if (variable.Name == null || variable.Name.LocalName == null || variable.Name.LocalName.Length == 0)
        {
            throw new ArgumentException("An XPath variable binding requires an expanded name.", "variable");
        }

        XPathVariableBinding snapshot = new XdmFunctionOperations().FreezeXPathVariableBinding(variable);
        int index = 0;
        while (index < Current.Variables.Count)
        {
            XPathVariableBinding existing = Current.Variables[index];
            if (SameXPathVariableName(existing.Name, snapshot.Name))
            {
                Current.Variables[index] = snapshot;
                return;
            }

            index = index + 1;
        }

        Current.Variables.Add(snapshot);
    }

    internal XPathVariableBinding? FindXPathVariable(XmlName name)
    {
        Guard.NotNull(name, "name");
        XPathVariableFrame? frame = Current;
        while (frame != null)
        {
            int index = frame.Variables.Count - 1;
            while (index >= 0)
            {
                XPathVariableBinding variable = frame.Variables[index];
                if (SameXPathVariableName(variable.Name, name))
                {
                    return variable;
                }

                index = index - 1;
            }

            frame = frame.Parent;
        }

        return null;
    }

    internal bool SameXPathVariableName(XmlName? left, XmlName? right)
    {
        if (left == null || right == null)
        {
            return false;
        }

        string leftNamespace = left.NamespaceUri == null ? "" : left.NamespaceUri;
        string rightNamespace = right.NamespaceUri == null ? "" : right.NamespaceUri;
        return leftNamespace == rightNamespace && left.LocalName == right.LocalName;
    }

    internal XPathVariableBinding[] SnapshotXPathVariables()
    {
        List<XPathVariableBinding> values = new List<XPathVariableBinding>();
        XPathVariableFrame? frame = Current;
        while (frame != null)
        {
            int index = frame.Variables.Count - 1;
            while (index >= 0)
            {
                XPathVariableBinding variable = frame.Variables[index];
                if (!ContainsXPathVariableSnapshot(values, variable.Name))
                {
                    values.Add(new XdmFunctionOperations().FreezeXPathVariableBinding(variable));
                }

                index = index - 1;
            }

            frame = frame.Parent;
        }

        return values.ToArray();
    }

    internal bool ContainsXPathVariableSnapshot(List<XPathVariableBinding> variables, XmlName? name)
    {
        int index = 0;
        while (index < variables.Count)
        {
            if (SameXPathVariableName(variables[index].Name, name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }
}
