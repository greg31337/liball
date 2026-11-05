using System.Collections.Generic;

namespace xml;

internal class XPathLocalVariable
{
    internal XmlName Name;
    internal XPathBinding? Binding;
    internal XPathParameter? Parameter;

    internal XPathLocalVariable(XmlName name, XPathBinding? binding, XPathParameter? parameter)
    {
        Name = name;
        Binding = binding;
        Parameter = parameter;
    }
}

internal class XPathStaticBinder
{
    internal XPathStaticEnvironment Environment;
    internal List<XPathLocalVariable> LocalVariables;

    internal XPathStaticBinder(XPathStaticContext context)
    {
        Environment = new XPathStaticEnvironment(context);
        LocalVariables = new List<XPathLocalVariable>();
    }

    internal XPathAstNode BindXPathExpression(XPathAstNode expression)
    {
        Guard.NotNull(expression, "expression");
        LocalVariables.Clear();
        BindXPathNode(expression);
        return expression;
    }

    internal void BindXPathNode(XPathAstNode node)
    {
        if (node.Kind == XPathAstKind.For || node.Kind == XPathAstKind.Let || node.Kind == XPathAstKind.Quantified)
        {
            BindXPathBindings(node);
            return;
        }

        if (node.Kind == XPathAstKind.InlineFunction)
        {
            BindXPathInlineFunction(node);
            return;
        }

        if (node.Kind == XPathAstKind.Arrow)
        {
            BindXPathArrow(node);
            return;
        }

        if (node.Kind == XPathAstKind.Variable)
        {
            BindXPathVariableReference(node);
            return;
        }

        if (node.Kind == XPathAstKind.StaticFunctionCall)
        {
            BindXPathFunctionReference(node, node.Children.Length);
        }
        else if (node.Kind == XPathAstKind.NamedFunctionReference)
        {
            BindXPathFunctionReference(node, node.Arity);
        }

        if (node.NodeTest != null)
        {
            Environment.ResolveXPathNodeTest(node.NodeTest, node.Axis);
            node.ResolvedName = node.NodeTest.ResolvedName;
            node.NamespaceUri = node.NodeTest.NamespaceUri;
        }

        if (node.Type != null)
        {
            Environment.ResolveXPathType(node.Type);
        }

        BindXPathOptionalNode(node.Left);
        BindXPathOptionalNode(node.Right);
        BindXPathOptionalNode(node.Operand);
        BindXPathOptionalNode(node.Condition);
        BindXPathOptionalNode(node.ThenExpression);
        BindXPathOptionalNode(node.ElseExpression);
        BindXPathOptionalNode(node.Body);
        BindXPathChildren(node.Children);
        BindXPathMapEntries(node.MapEntries);
    }

    internal void BindXPathBindings(XPathAstNode node)
    {
        int initialCount = LocalVariables.Count;
        int index = 0;
        while (index < node.Bindings.Length)
        {
            XPathBinding binding = node.Bindings[index];
            BindXPathNode(binding.Expression);
            XmlName name = Environment.ResolveXPathName(binding.Prefix, binding.LocalName, binding.NamespaceUri, XPathNameUse.Variable, binding.Range);
            binding.NamespaceUri = name.NamespaceUri;
            LocalVariables.Add(new XPathLocalVariable(name, binding, null));
            index = index + 1;
        }

        BindXPathOptionalNode(node.Body);
        RemoveXPathLocalVariables(initialCount);
    }

    internal void BindXPathInlineFunction(XPathAstNode node)
    {
        int initialCount = LocalVariables.Count;
        int index = 0;
        while (index < node.Parameters.Length)
        {
            XPathParameter parameter = node.Parameters[index];
            XmlName name = Environment.ResolveXPathName(parameter.Prefix, parameter.LocalName, parameter.NamespaceUri, XPathNameUse.Variable, parameter.Range);
            parameter.NamespaceUri = name.NamespaceUri;
            if (parameter.Type != null)
            {
                Environment.ResolveXPathType(parameter.Type);
            }

            LocalVariables.Add(new XPathLocalVariable(name, null, parameter));
            index = index + 1;
        }

        if (node.Type != null)
        {
            Environment.ResolveXPathType(node.Type);
        }

        BindXPathOptionalNode(node.Body);
        RemoveXPathLocalVariables(initialCount);
    }

    internal void BindXPathArrow(XPathAstNode node)
    {
        BindXPathOptionalNode(node.Left);
        BindXPathChildren(node.Children);
        XPathAstNode? function = node.Right;
        if (function == null)
        {
            return;
        }

        if (function.Kind == XPathAstKind.Name)
        {
            BindXPathFunctionReference(function, node.Children.Length + 1);
        }
        else
        {
            BindXPathNode(function);
        }
    }

    internal void BindXPathVariableReference(XPathAstNode node)
    {
        XmlName name = Environment.ResolveXPathName(node.Prefix, node.LocalName, node.NamespaceUri, XPathNameUse.Variable, node.Range);
        node.ResolvedName = name;
        node.NamespaceUri = name.NamespaceUri;
        int index = LocalVariables.Count - 1;
        while (index >= 0)
        {
            XPathLocalVariable local = LocalVariables[index];
            if (Environment.SameXPathName(local.Name, name))
            {
                node.LocalBinding = local.Binding;
                node.ParameterBinding = local.Parameter;
                return;
            }

            index = index - 1;
        }

        node.VariableBinding = Environment.FindXPathVariable(name);
    }

    internal void BindXPathFunctionReference(XPathAstNode node, int arity)
    {
        XmlName name = Environment.ResolveXPathName(node.Prefix, node.LocalName, node.NamespaceUri, XPathNameUse.Function, node.Range);
        node.ResolvedName = name;
        node.NamespaceUri = name.NamespaceUri;
        node.FunctionBinding = Environment.FindXPathFunction(name, arity);
    }

    internal void BindXPathOptionalNode(XPathAstNode? node)
    {
        if (node != null)
        {
            BindXPathNode(node);
        }
    }

    internal void BindXPathChildren(XPathAstNode[] children)
    {
        int index = 0;
        while (index < children.Length)
        {
            BindXPathNode(children[index]);
            index = index + 1;
        }
    }

    internal void BindXPathMapEntries(XPathMapEntry[] entries)
    {
        int index = 0;
        while (index < entries.Length)
        {
            BindXPathNode(entries[index].Key);
            BindXPathNode(entries[index].Value);
            index = index + 1;
        }
    }

    internal void RemoveXPathLocalVariables(int count)
    {
        while (LocalVariables.Count > count)
        {
            LocalVariables.RemoveAt(LocalVariables.Count - 1);
        }
    }
}
