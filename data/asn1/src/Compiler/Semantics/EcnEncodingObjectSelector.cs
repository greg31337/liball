namespace asn1;

internal static class EcnEncodingObjectSelector
{
    internal static EcnEncodingSelection SelectEncoding(
        EcnEncodingObjectSetDefinition definition,
        EcnSymbol encodingClass)
    {
        EcnSymbol[] userObjects = SelectObjects(definition, encodingClass);
        if (userObjects.Length > 0)
        {
            EcnSymbol userObject = userObjects[0];
            return new EcnEncodingSelection
            {
                StableId = "ecn:object:" + userObject.QualifiedName,
                UserObject = userObject,
                Found = true
            };
        }

        EcnPredeclaredEncodingObject? predeclared =
            SelectPredeclaredObject(definition, encodingClass);
        return new EcnEncodingSelection
        {
            StableId = predeclared == null ? "" : predeclared.StableId,
            PredeclaredObject = predeclared,
            Found = predeclared != null
        };
    }

    internal static EcnPredeclaredEncodingObject? SelectPredeclaredObject(
        EcnEncodingObjectSetDefinition definition,
        EcnSymbol encodingClass)
    {
        EcnPredeclaredEncodingObject? direct =
            SelectPredeclaredMember(definition.PredeclaredObjects, encodingClass);
        if (direct != null)
        {
            return direct;
        }

        int index = 0;
        while (index < definition.PredeclaredSets.Length)
        {
            EcnPredeclaredEncodingObject? selected =
                EcnEncodingObjectSetCatalog.SelectObject(
                    definition.PredeclaredSets[index],
                    encodingClass);
            if (selected != null)
            {
                return selected;
            }
            index = index + 1;
        }

        if (definition.PredeclaredCompletion != null)
        {
            return EcnEncodingObjectSetCatalog.SelectObject(
                definition.PredeclaredCompletion,
                encodingClass);
        }

        if (definition.CompletionSymbol != null
            && definition.CompletionSymbol.ObjectSetDefinition != null)
        {
            return SelectPredeclaredObject(
                definition.CompletionSymbol.ObjectSetDefinition,
                encodingClass);
        }

        return null;
    }

    internal static EcnPredeclaredEncodingObject? SelectPredeclaredMember(
        EcnPredeclaredEncodingObject[] objects,
        EcnSymbol encodingClass)
    {
        EcnPredeclaredEncodingObject? primitive = null;
        int index = 0;
        while (index < objects.Length)
        {
            if (objects[index].ClassName == encodingClass.Name)
            {
                return objects[index];
            }
            if (encodingClass.PrimitiveClassName.Length > 0
                && objects[index].ClassName == encodingClass.PrimitiveClassName)
            {
                primitive = objects[index];
            }
            index = index + 1;
        }
        if (primitive != null)
        {
            return primitive;
        }
        EcnSymbol? current = encodingClass;
        while (current != null && current.ClassDefinition != null)
        {
            EcnEncodingClassDefinition classDefinition = current.ClassDefinition;
            if (classDefinition.AliasBuiltIn)
            {
                index = 0;
                while (index < objects.Length)
                {
                    if (objects[index].ClassName
                        == classDefinition.AliasClassReference)
                    {
                        return objects[index];
                    }
                    index = index + 1;
                }
                return null;
            }
            current = classDefinition.AliasSymbol;
        }
        return null;
    }

    internal static EcnSymbol[] SelectObjects(
        EcnEncodingObjectSetDefinition definition,
        EcnSymbol encodingClass)
    {
        if (definition == null)
        {
            throw new System.ArgumentNullException("definition");
        }

        if (encodingClass == null)
        {
            throw new System.ArgumentNullException("encodingClass");
        }

        EcnSymbol[] selected = SelectExactObjects(definition, encodingClass);
        if (selected.Length > 0)
        {
            return selected;
        }

        EcnEncodingClassDefinition? classDefinition =
            encodingClass.ClassDefinition;
        if (classDefinition != null && classDefinition.AliasSymbol != null)
        {
            return SelectObjects(definition, classDefinition.AliasSymbol);
        }

        if (classDefinition != null && classDefinition.AliasBuiltIn)
        {
            EcnBuiltInClassDefinition? builtIn =
                EcnBuiltInClassCatalog.FindClass(
                    classDefinition.AliasClassReference);
            if (builtIn != null)
            {
                return SelectBuiltInObjects(definition, builtIn.PrimitiveName);
            }
        }

        return [];
    }

    internal static EcnSymbol[] SelectExactObjects(
        EcnEncodingObjectSetDefinition definition,
        EcnSymbol encodingClass)
    {
        System.Collections.Generic.List<EcnSymbol> selected =
            new System.Collections.Generic.List<EcnSymbol>();
        int index = 0;
        while (index < definition.Objects.Length)
        {
            EcnSymbol candidate = definition.Objects[index];
            EcnSymbol? governor = candidate.GovernorSymbol;
            EcnSymbol? governorTarget = governor == null
                ? null
                : governor.ImportedTarget == null
                    ? governor
                    : governor.ImportedTarget;
            EcnSymbol classTarget = encodingClass.ImportedTarget == null
                ? encodingClass
                : encodingClass.ImportedTarget;
            if (governorTarget == classTarget)
            {
                selected.Add(candidate);
            }

            index = index + 1;
        }

        if (selected.Count == 0
            && definition.CompletionSymbol != null
            && definition.CompletionSymbol.ObjectSetDefinition != null)
        {
            return SelectExactObjects(
                definition.CompletionSymbol.ObjectSetDefinition,
                encodingClass);
        }

        return selected.ToArray();
    }

    internal static EcnSymbol[] SelectBuiltInObjects(
        EcnEncodingObjectSetDefinition definition,
        string primitiveName)
    {
        System.Collections.Generic.List<EcnSymbol> selected =
            new System.Collections.Generic.List<EcnSymbol>();
        int index = 0;
        while (index < definition.Objects.Length)
        {
            EcnSymbol candidate = definition.Objects[index];
            if (candidate.GovernorBuiltIn)
            {
                EcnBuiltInClassDefinition? builtIn =
                    EcnBuiltInClassCatalog.FindClass(candidate.GovernorName);
                if (builtIn != null && builtIn.PrimitiveName == primitiveName)
                {
                    selected.Add(candidate);
                }
            }

            index = index + 1;
        }

        if (selected.Count == 0
            && definition.CompletionSymbol != null
            && definition.CompletionSymbol.ObjectSetDefinition != null)
        {
            return SelectBuiltInObjects(
                definition.CompletionSymbol.ObjectSetDefinition,
                primitiveName);
        }

        return selected.ToArray();
    }

    internal static bool AreClassesCompatible(
        EcnSymbol encodingObject,
        EcnSymbol encodingClass)
    {
        EcnSymbol? governor = encodingObject.GovernorSymbol;
        if (governor != null)
        {
            EcnSymbol actualGovernor = governor.ImportedTarget == null
                ? governor
                : governor.ImportedTarget;
            EcnSymbol actualClass = encodingClass.ImportedTarget == null
                ? encodingClass
                : encodingClass.ImportedTarget;
            if (actualGovernor == actualClass)
            {
                return true;
            }

            EcnSymbol? current = actualClass;
            while (current != null && current.ClassDefinition != null)
            {
                EcnEncodingClassDefinition definition = current.ClassDefinition;
                if (definition.AliasSymbol == actualGovernor)
                {
                    return true;
                }

                current = definition.AliasSymbol;
            }
        }

        if (encodingObject.GovernorBuiltIn)
        {
            EcnBuiltInClassDefinition? builtIn =
                EcnBuiltInClassCatalog.FindClass(encodingObject.GovernorName);
            return builtIn != null
                && builtIn.Category == encodingClass.ClassCategory
                && builtIn.PrimitiveName == encodingClass.PrimitiveClassName;
        }

        return encodingObject.ClassCategory != EcnClassCategory.Unknown
            && encodingObject.ClassCategory == encodingClass.ClassCategory
            && encodingObject.PrimitiveClassName
                == encodingClass.PrimitiveClassName;
    }
}
