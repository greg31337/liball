namespace asn1;

internal static class EcnEncodingObjectSetCatalog
{
    internal static readonly string[] Names =
    [
        "PER-BASIC-ALIGNED",
        "PER-BASIC-UNALIGNED",
        "PER-CANONICAL-ALIGNED",
        "PER-CANONICAL-UNALIGNED",
        "BER",
        "CER",
        "DER"
    ];

    internal static readonly string[] SharedClassNames =
    [
        "#INT",
        "#BOOL",
        "#NUL",
        "#CHARS",
        "#OCTETS",
        "#BITS",
        "#CONCATENATION"
    ];

    internal static readonly string[] ExcludedClassNames =
    [
        "#ALTERNATIVES",
        "#REPETITION",
        "#PAD",
        "#TRANSFORM",
        "#CONDITIONAL-INT",
        "#CONDITIONAL-REPETITION",
        "#OUTER"
    ];

    internal static readonly EcnPredeclaredEncodingObject[] SharedObjects =
        CreateSharedObjects();
    internal static readonly EcnPredeclaredEncodingObjectSet[] Sets =
        CreateSets();

    internal static bool IsBuiltIn(string name)
    {
        return FindSet(name) != null;
    }

    internal static EcnPredeclaredEncodingObjectSet[] GetSets()
    {
        EcnPredeclaredEncodingObjectSet[] result =
            new EcnPredeclaredEncodingObjectSet[Sets.Length];
        int index = 0;
        while (index < Sets.Length)
        {
            result[index] = Sets[index];
            index = index + 1;
        }
        return result;
    }

    internal static EcnPredeclaredEncodingObjectSet? FindSet(string name)
    {
        if (name == null)
        {
            throw new System.ArgumentNullException("name");
        }
        int index = 0;
        while (index < Sets.Length)
        {
            if (Sets[index].Name == name)
            {
                return Sets[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal static EcnPredeclaredEncodingObject? FindObject(
        string setName,
        string className)
    {
        EcnPredeclaredEncodingObjectSet? set = FindSet(setName);
        if (set == null)
        {
            return null;
        }
        int index = 0;
        while (index < set.Objects.Length)
        {
            if (set.Objects[index].ClassName == className)
            {
                return set.Objects[index];
            }
            index = index + 1;
        }
        return null;
    }

    internal static EcnPredeclaredEncodingObject? SelectObject(
        EcnPredeclaredEncodingObjectSet set,
        EcnSymbol encodingClass)
    {
        EcnPredeclaredEncodingObject? selected = FindObject(
            set.Name,
            encodingClass.Name);
        EcnSymbol? current = encodingClass;
        while (selected == null
            && current != null
            && current.ClassDefinition != null)
        {
            EcnEncodingClassDefinition definition = current.ClassDefinition;
            if (definition.AliasBuiltIn)
            {
                selected = FindObject(set.Name, definition.AliasClassReference);
                if (selected == null)
                {
                    EcnBuiltInClassDefinition? builtIn =
                        EcnBuiltInClassCatalog.FindClass(
                            definition.AliasClassReference);
                    if (builtIn != null)
                    {
                        selected = FindObject(set.Name, builtIn.PrimitiveName);
                    }
                }
                return selected;
            }
            current = definition.AliasSymbol;
            if (current != null)
            {
                selected = FindObject(set.Name, current.Name);
            }
        }
        if (selected == null && encodingClass.PrimitiveClassName.Length > 0)
        {
            selected = FindObject(set.Name, encodingClass.PrimitiveClassName);
        }
        return selected;
    }

    internal static EcnPredeclaredEncodingObject[] CreateSharedObjects()
    {
        EcnPredeclaredEncodingObject[] result =
            new EcnPredeclaredEncodingObject[SharedClassNames.Length];
        int index = 0;
        while (index < SharedClassNames.Length)
        {
            EcnBuiltInClassDefinition? builtIn =
                EcnBuiltInClassCatalog.FindClass(SharedClassNames[index]);
            result[index] = new EcnPredeclaredEncodingObject
            {
                StableId = "x692:object:shared:" + SharedClassNames[index],
                Name = "shared-" + SharedClassNames[index].Substring(1),
                ClassName = SharedClassNames[index],
                PrimitiveClassName = builtIn == null
                    ? SharedClassNames[index]
                    : builtIn.PrimitiveName,
                ClassCategory = builtIn == null
                    ? EcnClassCategory.Unknown
                    : builtIn.Category,
                Rule = EcnPredeclaredEncodingRule.SharedBasic,
                Shared = true,
                RequiresBounds = SharedClassNames[index] == "#INT",
                RequiresFixedSize = SharedClassNames[index] == "#CHARS"
                    || SharedClassNames[index] == "#OCTETS"
                    || SharedClassNames[index] == "#BITS",
                RequiresNoOptionalComponents =
                    SharedClassNames[index] == "#CONCATENATION"
            };
            index = index + 1;
        }
        return result;
    }

    internal static EcnPredeclaredEncodingObjectSet[] CreateSets()
    {
        return
        [
            CreateSet("PER-BASIC-ALIGNED", "2.1.3.0.0",
                [2, 1, 3, 0, 0], EcnPredeclaredEncodingRule.PerBasicAligned,
                true, true, false),
            CreateSet("PER-BASIC-UNALIGNED", "2.1.3.0.1",
                [2, 1, 3, 0, 1], EcnPredeclaredEncodingRule.PerBasicUnaligned,
                true, false, false),
            CreateSet("PER-CANONICAL-ALIGNED", "2.1.3.1.0",
                [2, 1, 3, 1, 0], EcnPredeclaredEncodingRule.PerCanonicalAligned,
                true, true, true),
            CreateSet("PER-CANONICAL-UNALIGNED", "2.1.3.1.1",
                [2, 1, 3, 1, 1], EcnPredeclaredEncodingRule.PerCanonicalUnaligned,
                true, false, true),
            CreateSet("BER", "2.1.1", [2, 1, 1],
                EcnPredeclaredEncodingRule.Ber, false, false, false),
            CreateSet("CER", "2.1.2.0", [2, 1, 2, 0],
                EcnPredeclaredEncodingRule.Cer, false, false, true),
            CreateSet("DER", "2.1.2.1", [2, 1, 2, 1],
                EcnPredeclaredEncodingRule.Der, false, false, true)
        ];
    }

    internal static EcnPredeclaredEncodingObjectSet CreateSet(
        string name,
        string objectIdentifier,
        int[] arcs,
        EcnPredeclaredEncodingRule rule,
        bool per,
        bool aligned,
        bool canonical)
    {
        return new EcnPredeclaredEncodingObjectSet
        {
            StableId = "x692:set:" + name,
            Name = name,
            ObjectIdentifier = objectIdentifier,
            ObjectIdentifierArcs = arcs,
            Rule = rule,
            Objects = CreateSetObjects(name, rule),
            Per = per,
            Aligned = aligned,
            Canonical = canonical
        };
    }

    internal static EcnPredeclaredEncodingObject[] CreateSetObjects(
        string setName,
        EcnPredeclaredEncodingRule rule)
    {
        System.Collections.Generic.List<EcnPredeclaredEncodingObject> objects =
            new System.Collections.Generic.List<EcnPredeclaredEncodingObject>();
        int index = 0;
        while (index < SharedObjects.Length)
        {
            objects.Add(SharedObjects[index]);
            index = index + 1;
        }
        EcnBuiltInClassDefinition[] classes = EcnBuiltInClassCatalog.GetClasses();
        index = 0;
        while (index < classes.Length)
        {
            if (!ContainsName(SharedClassNames, classes[index].Name)
                && !ContainsName(ExcludedClassNames, classes[index].Name))
            {
                objects.Add(new EcnPredeclaredEncodingObject
                {
                    StableId = "x692:object:" + setName + ":"
                        + classes[index].Name,
                    Name = setName + "-" + classes[index].Name.Substring(1),
                    ClassName = classes[index].Name,
                    PrimitiveClassName = classes[index].PrimitiveName,
                    ClassCategory = classes[index].Category,
                    Rule = rule,
                    NoAdditionalEncoding = classes[index].Name == "#OPEN-TYPE"
                        && (rule == EcnPredeclaredEncodingRule.Ber
                            || rule == EcnPredeclaredEncodingRule.Cer
                            || rule == EcnPredeclaredEncodingRule.Der)
                });
            }
            index = index + 1;
        }
        return objects.ToArray();
    }

    internal static bool ContainsName(string[] names, string name)
    {
        int index = 0;
        while (index < names.Length)
        {
            if (names[index] == name)
            {
                return true;
            }
            index = index + 1;
        }
        return false;
    }
}
