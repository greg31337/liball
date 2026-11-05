using System.Collections.Generic;

namespace xml;

internal class XmlSchemaIdentityFieldValue
{
    internal XdmAtomicValue[] AtomicValues = [];
}

internal class XmlSchemaIdentityTuple
{
    internal XmlSchemaIdentityFieldValue[] Fields = [];
    internal XmlNode? SelectedNode;
}

internal class XmlSchemaIdentityBinding
{
    internal CompiledXmlSchemaIdentityConstraint? Constraint;
    internal List<XmlSchemaIdentityTuple> Tuples = new List<XmlSchemaIdentityTuple>();
    internal List<XmlSchemaIdentityTuple> Conflicts = new List<XmlSchemaIdentityTuple>();
}

internal class XmlSchemaIdentityScope
{
    internal XmlNode? Node;
    internal List<XmlSchemaIdentityBinding> Bindings = new List<XmlSchemaIdentityBinding>();
}

internal class XmlSchemaIdentityConstraintValidator
{
    internal XmlSchemaAssessmentEngine Engine;
    internal XmlSchemaSimpleTypeValidator SimpleTypeValidator;
    internal List<XmlSchemaIdentityScope> Scopes;
    internal bool FieldSelectionError;

    internal XmlSchemaIdentityConstraintValidator(XmlSchemaAssessmentEngine engine)
    {
        Guard.NotNull(engine, "engine");
        Engine = engine;
        SimpleTypeValidator = new XmlSchemaSimpleTypeValidator();
        Scopes = new List<XmlSchemaIdentityScope>();
        FieldSelectionError = false;
    }

    internal void ValidateXmlSchemaIdentityConstraints()
    {
        Scopes.Clear();
        int assessmentIndex = Engine.Assessments.Count - 1;
        while (assessmentIndex >= 0)
        {
            XmlSchemaNodeAssessment assessment = Engine.Assessments[assessmentIndex];
            if (assessment.Node.Kind == XmlNodeKind.Element && assessment.ElementDeclaration != null && assessment.ProcessContents != XmlSchemaProcessContents.Skip)
            {
                ValidateXmlSchemaIdentityScope(assessment, assessment.ElementDeclaration);
            }

            assessmentIndex = assessmentIndex - 1;
        }
    }

    internal void ValidateXmlSchemaIdentityScope(XmlSchemaNodeAssessment assessment, CompiledXmlSchemaElementDeclaration declaration)
    {
        XmlSchemaIdentityScope scope = new XmlSchemaIdentityScope();
        scope.Node = assessment.Node;
        MergeXmlSchemaChildIdentityScopes(scope);
        int index = 0;
        while (index < declaration.IdentityConstraints.Length)
        {
            CompiledXmlSchemaIdentityConstraint constraint = declaration.IdentityConstraints[index];
            if (constraint.Kind != XmlSchemaIdentityConstraintKind.KeyReference)
            {
                XmlSchemaIdentityBinding? binding = EvaluateXmlSchemaKeyOrUniqueConstraint(assessment, constraint);
                if (binding != null)
                {
                    MergeXmlSchemaIdentityBinding(scope, binding);
                }
            }

            index = index + 1;
        }

        index = 0;
        while (index < declaration.IdentityConstraints.Length)
        {
            CompiledXmlSchemaIdentityConstraint constraint = declaration.IdentityConstraints[index];
            if (constraint.Kind == XmlSchemaIdentityConstraintKind.KeyReference)
            {
                EvaluateXmlSchemaKeyReferenceConstraint(assessment, scope, constraint);
            }

            index = index + 1;
        }

        Scopes.Add(scope);
    }

    internal void MergeXmlSchemaChildIdentityScopes(XmlSchemaIdentityScope target)
    {
        XmlNode? targetNode = target.Node;
        int scopeIndex = 0;
        while (scopeIndex < Scopes.Count)
        {
            XmlSchemaIdentityScope child = Scopes[scopeIndex];
            XmlNode? childNode = child.Node;
            if (targetNode != null && childNode != null && childNode.Parent == targetNode)
            {
                int bindingIndex = 0;
                while (bindingIndex < child.Bindings.Count)
                {
                    MergeXmlSchemaIdentityBinding(target, child.Bindings[bindingIndex]);
                    bindingIndex = bindingIndex + 1;
                }
            }

            scopeIndex = scopeIndex + 1;
        }
    }

    internal XmlSchemaIdentityBinding? EvaluateXmlSchemaKeyOrUniqueConstraint(XmlSchemaNodeAssessment assessment, CompiledXmlSchemaIdentityConstraint constraint)
    {
        XmlSchemaIdentityXPathExpression? selector = constraint.CompiledSelector;
        if (selector == null)
        {
            return null;
        }

        XmlNode[] selected = SelectXmlSchemaIdentityNodes(selector, assessment.Node);
        XmlSchemaIdentityBinding binding = new XmlSchemaIdentityBinding();
        binding.Constraint = constraint;
        bool valid = true;
        int index = 0;
        while (index < selected.Length)
        {
            if (!IsSkippedXmlSchemaIdentityNode(selected[index]))
            {
                XmlSchemaIdentityTuple? tuple = ReadXmlSchemaIdentityTuple(assessment, constraint, selected[index]);
                if (FieldSelectionError)
                {
                    valid = false;
                }

                if (tuple == null)
                {
                    if (constraint.Kind == XmlSchemaIdentityConstraintKind.Key)
                    {
                        valid = false;
                        ReportXmlSchemaIdentityError(assessment, selected[index], "A selected key value is missing, nilled, invalid, or not simple-typed.");
                    }
                }
                else if (FindEqualXmlSchemaIdentityTuple(binding.Tuples, tuple) != null)
                {
                    valid = false;
                    ReportXmlSchemaIdentityError(assessment, selected[index], "An XML Schema key or unique constraint contains a duplicate value.");
                }
                else
                {
                    binding.Tuples.Add(tuple);
                }
            }

            index = index + 1;
        }

        return valid ? binding : null;
    }

    internal void EvaluateXmlSchemaKeyReferenceConstraint(XmlSchemaNodeAssessment assessment, XmlSchemaIdentityScope scope, CompiledXmlSchemaIdentityConstraint constraint)
    {
        XmlSchemaIdentityXPathExpression? selector = constraint.CompiledSelector;
        CompiledXmlSchemaIdentityConstraint? referenced = constraint.ReferencedConstraint;
        if (selector == null || referenced == null)
        {
            return;
        }

        XmlSchemaIdentityBinding? target = FindXmlSchemaIdentityBinding(scope, referenced);
        XmlNode[] selected = SelectXmlSchemaIdentityNodes(selector, assessment.Node);
        int index = 0;
        while (index < selected.Length)
        {
            if (!IsSkippedXmlSchemaIdentityNode(selected[index]))
            {
                XmlSchemaIdentityTuple? tuple = ReadXmlSchemaIdentityTuple(assessment, constraint, selected[index]);
                if (tuple != null && (target == null || FindEqualXmlSchemaIdentityTuple(target.Tuples, tuple) == null))
                {
                    ReportXmlSchemaIdentityError(assessment, selected[index], "An XML Schema key reference does not match any key or unique value in scope.");
                }
            }

            index = index + 1;
        }
    }

    internal XmlSchemaIdentityTuple? ReadXmlSchemaIdentityTuple(XmlSchemaNodeAssessment scopeAssessment, CompiledXmlSchemaIdentityConstraint constraint, XmlNode selected)
    {
        FieldSelectionError = false;
        List<XmlSchemaIdentityFieldValue> values = new List<XmlSchemaIdentityFieldValue>();
        int fieldIndex = 0;
        while (fieldIndex < constraint.CompiledFields.Length)
        {
            XmlNode[] nodes = SelectXmlSchemaIdentityNodes(constraint.CompiledFields[fieldIndex], selected);
            if (nodes.Length != 1)
            {
                if (nodes.Length > 1)
                {
                    FieldSelectionError = true;
                    ReportXmlSchemaIdentityError(scopeAssessment, selected, "An XML Schema identity-constraint field selected more than one node.");
                }

                return null;
            }

            XmlSchemaNodeAssessment? fieldAssessment = FindXmlSchemaIdentityAssessment(nodes[0]);
            XmlSchemaSimpleTypeValidationResult? validation = fieldAssessment == null ? null : fieldAssessment.SimpleTypeValidation;
            if (fieldAssessment == null || fieldAssessment.Validity == XmlSchemaValidity.Invalid || validation == null || !validation.Success)
            {
                return null;
            }

            if (constraint.Kind == XmlSchemaIdentityConstraintKind.Key && nodes[0].Kind == XmlNodeKind.Element && fieldAssessment.ElementDeclaration != null && fieldAssessment.ElementDeclaration.IsNillable)
            {
                return null;
            }

            XmlSchemaIdentityFieldValue fieldValue = new XmlSchemaIdentityFieldValue();
            fieldValue.AtomicValues = validation.AtomicValues;
            values.Add(fieldValue);
            fieldIndex = fieldIndex + 1;
        }

        XmlSchemaIdentityTuple result = new XmlSchemaIdentityTuple();
        result.Fields = values.ToArray();
        result.SelectedNode = selected;
        return result;
    }

    internal void MergeXmlSchemaIdentityBinding(XmlSchemaIdentityScope scope, XmlSchemaIdentityBinding source)
    {
        CompiledXmlSchemaIdentityConstraint? constraint = source.Constraint;
        if (constraint == null)
        {
            return;
        }

        XmlSchemaIdentityBinding? target = FindXmlSchemaIdentityBinding(scope, constraint);
        if (target == null)
        {
            target = new XmlSchemaIdentityBinding();
            target.Constraint = constraint;
            scope.Bindings.Add(target);
        }

        int conflictIndex = 0;
        while (conflictIndex < source.Conflicts.Count)
        {
            AddXmlSchemaIdentityConflict(target, source.Conflicts[conflictIndex]);
            conflictIndex = conflictIndex + 1;
        }

        int tupleIndex = 0;
        while (tupleIndex < source.Tuples.Count)
        {
            AddXmlSchemaIdentityBindingTuple(target, source.Tuples[tupleIndex]);
            tupleIndex = tupleIndex + 1;
        }
    }

    internal void AddXmlSchemaIdentityBindingTuple(XmlSchemaIdentityBinding binding, XmlSchemaIdentityTuple tuple)
    {
        if (FindEqualXmlSchemaIdentityTuple(binding.Conflicts, tuple) != null)
        {
            return;
        }

        XmlSchemaIdentityTuple? existing = FindEqualXmlSchemaIdentityTuple(binding.Tuples, tuple);
        if (existing == null)
        {
            binding.Tuples.Add(tuple);
        }
        else if (existing.SelectedNode != tuple.SelectedNode)
        {
            binding.Tuples.Remove(existing);
            binding.Conflicts.Add(tuple);
        }
    }

    internal void AddXmlSchemaIdentityConflict(XmlSchemaIdentityBinding binding, XmlSchemaIdentityTuple tuple)
    {
        XmlSchemaIdentityTuple? existing = FindEqualXmlSchemaIdentityTuple(binding.Tuples, tuple);
        if (existing != null)
        {
            binding.Tuples.Remove(existing);
        }

        if (FindEqualXmlSchemaIdentityTuple(binding.Conflicts, tuple) == null)
        {
            binding.Conflicts.Add(tuple);
        }
    }

    internal XmlSchemaIdentityBinding? FindXmlSchemaIdentityBinding(XmlSchemaIdentityScope scope, CompiledXmlSchemaIdentityConstraint constraint)
    {
        int index = 0;
        while (index < scope.Bindings.Count)
        {
            if (scope.Bindings[index].Constraint == constraint)
            {
                return scope.Bindings[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal XmlSchemaIdentityTuple? FindEqualXmlSchemaIdentityTuple(List<XmlSchemaIdentityTuple> tuples, XmlSchemaIdentityTuple candidate)
    {
        int index = 0;
        while (index < tuples.Count)
        {
            if (EqualXmlSchemaIdentityTuples(tuples[index], candidate))
            {
                return tuples[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal bool EqualXmlSchemaIdentityTuples(XmlSchemaIdentityTuple left, XmlSchemaIdentityTuple right)
    {
        if (left.Fields.Length != right.Fields.Length)
        {
            return false;
        }

        int fieldIndex = 0;
        while (fieldIndex < left.Fields.Length)
        {
            XdmAtomicValue[] leftValues = left.Fields[fieldIndex].AtomicValues;
            XdmAtomicValue[] rightValues = right.Fields[fieldIndex].AtomicValues;
            if (leftValues.Length != rightValues.Length)
            {
                return false;
            }

            int valueIndex = 0;
            while (valueIndex < leftValues.Length)
            {
                if (!SimpleTypeValidator.EqualXmlSchemaAtomicValues(leftValues[valueIndex], rightValues[valueIndex]))
                {
                    return false;
                }

                valueIndex = valueIndex + 1;
            }

            fieldIndex = fieldIndex + 1;
        }

        return true;
    }

    internal XmlSchemaNodeAssessment? FindXmlSchemaIdentityAssessment(XmlNode node)
    {
        int index = 0;
        while (index < Engine.Assessments.Count)
        {
            if (Engine.Assessments[index].Node == node)
            {
                return Engine.Assessments[index];
            }

            index = index + 1;
        }

        return null;
    }

    internal void ReportXmlSchemaIdentityError(XmlSchemaNodeAssessment scopeAssessment, XmlNode node, string message)
    {
        scopeAssessment.Validity = XmlSchemaValidity.Invalid;
        Engine.ReportXmlSchemaAssessmentError(node, message);
    }

    internal bool IsSkippedXmlSchemaIdentityNode(XmlNode node)
    {
        XmlNode? current = node;
        while (current != null)
        {
            XmlSchemaNodeAssessment? assessment = FindXmlSchemaIdentityAssessment(current);
            if (assessment != null && assessment.ProcessContents == XmlSchemaProcessContents.Skip)
            {
                return true;
            }

            current = current.Parent;
        }

        return false;
    }

    internal XmlNode[] SelectXmlSchemaIdentityNodes(XmlSchemaIdentityXPathExpression expression, XmlNode context)
    {
        List<XmlNode> result = new List<XmlNode>();
        int pathIndex = 0;
        while (pathIndex < expression.Paths.Length)
        {
            XmlNode[] selected = SelectXmlSchemaIdentityPath(expression.Paths[pathIndex], context);
            int selectedIndex = 0;
            while (selectedIndex < selected.Length)
            {
                if (!result.Contains(selected[selectedIndex]))
                {
                    result.Add(selected[selectedIndex]);
                }

                selectedIndex = selectedIndex + 1;
            }

            pathIndex = pathIndex + 1;
        }

        return result.ToArray();
    }

    internal XmlNode[] SelectXmlSchemaIdentityPath(XmlSchemaIdentityXPathPath path, XmlNode context)
    {
        List<XmlNode> current = new List<XmlNode>();
        current.Add(context);
        if (path.DescendantStart)
        {
            AddXmlSchemaIdentityDescendants(context, current);
        }

        int stepIndex = 0;
        while (stepIndex < path.Steps.Length)
        {
            current = ApplyXmlSchemaIdentityStep(current, path.Steps[stepIndex]);
            stepIndex = stepIndex + 1;
        }

        return current.ToArray();
    }

    internal List<XmlNode> ApplyXmlSchemaIdentityStep(List<XmlNode> current, XmlSchemaIdentityXPathStep step)
    {
        if (step.Axis == XmlSchemaIdentityXPathAxis.Self)
        {
            return current;
        }

        List<XmlNode> result = new List<XmlNode>();
        int currentIndex = 0;
        while (currentIndex < current.Count)
        {
            XmlNode node = current[currentIndex];
            XmlNode[] candidates = step.Axis == XmlSchemaIdentityXPathAxis.Attribute ? node.Attributes == null ? [] : node.Attributes : node.Children == null ? [] : node.Children;
            int candidateIndex = 0;
            while (candidateIndex < candidates.Length)
            {
                XmlNode candidate = candidates[candidateIndex];
                XmlNodeKind requiredKind = step.Axis == XmlSchemaIdentityXPathAxis.Attribute ? XmlNodeKind.Attribute : XmlNodeKind.Element;
                if (candidate.Kind == requiredKind && MatchXmlSchemaIdentityName(step.NameTest, candidate.Name) && !result.Contains(candidate))
                {
                    result.Add(candidate);
                }

                candidateIndex = candidateIndex + 1;
            }

            currentIndex = currentIndex + 1;
        }

        return result;
    }

    internal void AddXmlSchemaIdentityDescendants(XmlNode node, List<XmlNode> result)
    {
        XmlNode[] children = node.Children == null ? [] : node.Children;
        int index = 0;
        while (index < children.Length)
        {
            if (children[index].Kind == XmlNodeKind.Element)
            {
                result.Add(children[index]);
                AddXmlSchemaIdentityDescendants(children[index], result);
            }

            index = index + 1;
        }
    }

    internal bool MatchXmlSchemaIdentityName(XmlSchemaIdentityNameTest? test, XmlName? name)
    {
        if (test == null || name == null)
        {
            return false;
        }

        string namespaceUri = name.NamespaceUri == null ? "" : name.NamespaceUri;
        bool namespaceMatches = test.AnyNamespace || namespaceUri == test.NamespaceUri;
        return namespaceMatches && (test.LocalName == null || test.LocalName == name.LocalName);
    }
}
