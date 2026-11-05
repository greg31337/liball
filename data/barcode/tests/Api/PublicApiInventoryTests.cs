namespace barcode.Tests;

public class PublicApiInventoryTests
{
    [Test]
    public void EveryPublicConstructorCanBeInvoked()
    {
        global::System.Type[] types = GetApiTypes();
        global::System.Collections.Generic.HashSet<global::System.Type> invoked =
            new global::System.Collections.Generic.HashSet<global::System.Type>();
        int typeIndex = 0;
        while (typeIndex < types.Length)
        {
            global::System.Type type = types[typeIndex];
            if (type.IsClass && !type.IsAbstract)
            {
                global::System.Reflection.ConstructorInfo[] constructors =
                    type.GetConstructors(
                        global::System.Reflection.BindingFlags.Public |
                        global::System.Reflection.BindingFlags.Instance);
                int constructorIndex = 0;
                while (constructorIndex < constructors.Length)
                {
                    global::System.Reflection.ConstructorInfo constructor =
                        constructors[constructorIndex];
                    object? instance = constructor.Invoke(
                        CreateConstructorArguments(constructor));
                    Assert.That(instance, Is.Not.Null, type.FullName);
                    Assert.That(instance, Is.TypeOf(type), type.FullName);
                    invoked.Add(type);
                    constructorIndex = constructorIndex + 1;
                }
            }

            typeIndex = typeIndex + 1;
        }

        global::System.Type[] concreteTypes = SelectConcreteTypes(types);
        Assert.That(invoked, Is.EquivalentTo(concreteTypes));
    }

    [Test]
    public void EveryFieldDrivenEnumOptionCanBeAssignedAndRead()
    {
        global::System.Type[] types = GetApiTypes();
        global::System.Collections.Generic.HashSet<global::System.Type> touchedEnums =
            new global::System.Collections.Generic.HashSet<global::System.Type>();
        int fieldCount = 0;
        int typeIndex = 0;
        while (typeIndex < types.Length)
        {
            global::System.Type ownerType = types[typeIndex];
            if (ownerType.IsClass && !ownerType.IsAbstract)
            {
                object owner = CreateApiInstance(ownerType);
                global::System.Reflection.FieldInfo[] fields =
                    ownerType.GetFields(
                        global::System.Reflection.BindingFlags.Public |
                        global::System.Reflection.BindingFlags.Instance |
                        global::System.Reflection.BindingFlags.DeclaredOnly);
                int fieldIndex = 0;
                while (fieldIndex < fields.Length)
                {
                    global::System.Reflection.FieldInfo field = fields[fieldIndex];
                    global::System.Type enumType = ResolveEnumFieldType(
                        field.FieldType);
                    if (enumType != typeof(void))
                    {
                        object[] values = global::System.Enum.GetValues(enumType)
                            .Cast<object>().ToArray();
                        int valueIndex = 0;
                        while (valueIndex < values.Length)
                        {
                            object assigned = field.FieldType.IsArray
                                ? CreateSingleValueArray(enumType, values[valueIndex])
                                : values[valueIndex];
                            field.SetValue(owner, assigned);
                            object? observed = field.GetValue(owner);
                            Assert.That(
                                observed,
                                Is.EqualTo(assigned),
                                ownerType.Name + "." + field.Name + "="
                                    + values[valueIndex]);
                            valueIndex = valueIndex + 1;
                        }

                        touchedEnums.Add(enumType);
                        fieldCount = fieldCount + 1;
                    }

                    fieldIndex = fieldIndex + 1;
                }
            }

            typeIndex = typeIndex + 1;
        }

        global::System.Type[] enumTypes = types
            .Where(type => type.IsEnum).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(touchedEnums, Is.EquivalentTo(enumTypes));
            Assert.That(fieldCount, Is.GreaterThan(enumTypes.Length));
        });
    }

    [Test]
    public void EveryInterfaceMethodInvokesACallableImplementation()
    {
        global::System.Type[] interfaces = GetApiTypes()
            .Where(type => type.IsInterface).ToArray();
        global::System.Collections.Generic.HashSet<string> invoked =
            new global::System.Collections.Generic.HashSet<string>();
        int interfaceIndex = 0;
        while (interfaceIndex < interfaces.Length)
        {
            global::System.Type interfaceType = interfaces[interfaceIndex];
            object target = CreateInterfaceTarget(interfaceType);
            global::System.Reflection.MethodInfo[] methods =
                interfaceType.GetMethods();
            int methodIndex = 0;
            while (methodIndex < methods.Length)
            {
                global::System.Reflection.MethodInfo method = methods[methodIndex];
                object? result = method.Invoke(
                    target,
                    CreateMethodArguments(interfaceType, method));
                Assert.That(
                    result,
                    Is.Not.Null,
                    interfaceType.Name + "." + method.Name);
                invoked.Add(interfaceType.FullName + "." + method.Name);
                methodIndex = methodIndex + 1;
            }

            interfaceIndex = interfaceIndex + 1;
        }

        int expectedMethodCount = 0;
        interfaceIndex = 0;
        while (interfaceIndex < interfaces.Length)
        {
            expectedMethodCount = expectedMethodCount
                + interfaces[interfaceIndex].GetMethods().Length;
            interfaceIndex = interfaceIndex + 1;
        }

        Assert.That(invoked, Has.Count.EqualTo(expectedMethodCount));
    }

    [Test]
    public void EveryPublicStaticFieldCanBeRead()
    {
        global::System.Type[] types = GetApiTypes();
        int fieldCount = 0;
        int typeIndex = 0;
        while (typeIndex < types.Length)
        {
            global::System.Reflection.FieldInfo[] fields =
                types[typeIndex].GetFields(
                    global::System.Reflection.BindingFlags.Public |
                    global::System.Reflection.BindingFlags.Static |
                    global::System.Reflection.BindingFlags.DeclaredOnly);
            int fieldIndex = 0;
            while (fieldIndex < fields.Length)
            {
                object? first = fields[fieldIndex].GetValue(null);
                object? second = fields[fieldIndex].GetValue(null);
                Assert.That(first, Is.Not.Null, fields[fieldIndex].Name);
                Assert.That(second, Is.EqualTo(first), fields[fieldIndex].Name);
                if (first is string text)
                {
                    Assert.That(text, Is.Not.Empty, fields[fieldIndex].Name);
                }

                fieldCount = fieldCount + 1;
                fieldIndex = fieldIndex + 1;
            }

            typeIndex = typeIndex + 1;
        }

        Assert.That(fieldCount, Is.GreaterThan(0));
    }

    [Test]
    public void EveryPublicDomainExceptionCanBeThrownAndCaught()
    {
        global::System.Type[] exceptionTypes = GetApiTypes()
            .Where(type => type.IsSubclassOf(typeof(global::System.Exception)))
            .ToArray();
        int index = 0;
        while (index < exceptionTypes.Length)
        {
            global::System.Type exceptionType = exceptionTypes[index];
            global::System.Reflection.ConstructorInfo constructor =
                exceptionType.GetConstructor(new global::System.Type[]
                {
                    typeof(string)
                })!;
            global::System.Exception exception =
                (global::System.Exception)constructor.Invoke(new object[]
                {
                    "inventory-" + exceptionType.Name
                });

            global::System.Exception caught = Assert.Throws(
                exceptionType,
                () => throw exception)!;
            Assert.That(
                caught.Message,
                Is.EqualTo("inventory-" + exceptionType.Name));
            index = index + 1;
        }

        Assert.That(exceptionTypes, Has.Length.EqualTo(7));
    }

    [Test]
    public void EveryPublicResultTypeStoresValuesInEveryPublicField()
    {
        global::System.Type[] resultTypes = GetApiTypes()
            .Where(IsResultType).ToArray();
        int fieldCount = 0;
        int typeIndex = 0;
        while (typeIndex < resultTypes.Length)
        {
            global::System.Type resultType = resultTypes[typeIndex];
            object instance = CreateApiInstance(resultType);
            global::System.Reflection.FieldInfo[] fields =
                resultType.GetFields(
                    global::System.Reflection.BindingFlags.Public |
                    global::System.Reflection.BindingFlags.Instance |
                    global::System.Reflection.BindingFlags.DeclaredOnly);
            int fieldIndex = 0;
            while (fieldIndex < fields.Length)
            {
                global::System.Reflection.FieldInfo field = fields[fieldIndex];
                object? value = CreateFieldValue(field.FieldType);
                field.SetValue(instance, value);
                object? observed = field.GetValue(instance);
                if (field.FieldType.IsValueType)
                {
                    Assert.That(observed, Is.EqualTo(value), field.Name);
                }
                else
                {
                    Assert.That(observed, Is.SameAs(value), field.Name);
                }

                fieldCount = fieldCount + 1;
                fieldIndex = fieldIndex + 1;
            }

            typeIndex = typeIndex + 1;
        }

        Assert.Multiple(() =>
        {
            Assert.That(resultTypes, Has.Length.EqualTo(9));
            Assert.That(fieldCount, Is.GreaterThan(0));
        });
    }

    static global::System.Type[] GetApiTypes()
    {
        return typeof(BarcodeEncoder).Assembly.GetExportedTypes()
            .Where(type => type.Namespace == "barcode")
            .OrderBy(type => type.FullName, global::System.StringComparer.Ordinal)
            .ToArray();
    }

    static global::System.Type[] SelectConcreteTypes(
        global::System.Type[] types)
    {
        return types.Where(type => type.IsClass && !type.IsAbstract).ToArray();
    }

    static object?[] CreateConstructorArguments(
        global::System.Reflection.ConstructorInfo constructor)
    {
        global::System.Reflection.ParameterInfo[] parameters =
            constructor.GetParameters();
        object?[] arguments = new object?[parameters.Length];
        int index = 0;
        while (index < parameters.Length)
        {
            if (parameters[index].ParameterType == typeof(string))
            {
                arguments[index] = "inventory";
            }
            else
            {
                arguments[index] = CreateFieldValue(
                    parameters[index].ParameterType);
            }

            index = index + 1;
        }

        return arguments;
    }

    static object CreateApiInstance(global::System.Type type)
    {
        global::System.Reflection.ConstructorInfo? constructor =
            type.GetConstructor(global::System.Type.EmptyTypes);
        if (constructor != null)
        {
            return constructor.Invoke(new object[0]);
        }

        return type.GetConstructor(new global::System.Type[] { typeof(string) })!
            .Invoke(new object[] { "inventory" });
    }

    static global::System.Type ResolveEnumFieldType(
        global::System.Type fieldType)
    {
        if (fieldType.IsEnum)
        {
            return fieldType;
        }

        if (fieldType.IsArray && fieldType.GetElementType()!.IsEnum)
        {
            return fieldType.GetElementType()!;
        }

        return typeof(void);
    }

    static object CreateSingleValueArray(
        global::System.Type elementType,
        object value)
    {
        global::System.Array array = global::System.Array.CreateInstance(
            elementType,
            1);
        array.SetValue(value, 0);
        return array;
    }

    static object CreateInterfaceTarget(global::System.Type interfaceType)
    {
        if (interfaceType == typeof(IBarcodeChecksumCalculator))
        {
            return new BarcodeChecksumCalculator();
        }

        if (interfaceType == typeof(IBarcodeConfigurationProvider))
        {
            return new ApiConfigurationProviderStub();
        }

        if (interfaceType == typeof(IBarcodeDecoder))
        {
            return new BarcodeDecoder();
        }

        if (interfaceType == typeof(IBarcodeDetector))
        {
            return new BarcodeDetector();
        }

        if (interfaceType == typeof(IBarcodeEncoder))
        {
            return new BarcodeEncoder();
        }

        if (interfaceType == typeof(IBarcodeRenderer))
        {
            return new BarcodeRenderer();
        }

        if (interfaceType == typeof(IBarcodeValidator))
        {
            return new BarcodeValidator();
        }

        if (interfaceType == typeof(IGs1DataProcessor))
        {
            return new Gs1DataProcessor();
        }

        if (interfaceType == typeof(IStructuredAppendAssembler))
        {
            return new StructuredAppendAssembler();
        }

        throw new global::System.InvalidOperationException(
            "The interface has no inventory target: " + interfaceType.FullName);
    }

    static object?[] CreateMethodArguments(
        global::System.Type interfaceType,
        global::System.Reflection.MethodInfo method)
    {
        if (interfaceType == typeof(IBarcodeConfigurationProvider))
        {
            return new object?[0];
        }

        if (interfaceType == typeof(IBarcodeChecksumCalculator))
        {
            BarcodeChecksumRequest request = new BarcodeChecksumRequest();
            request.Format = BarcodeFormat.Ean13;
            request.Text = "400638133393";
            request.HasObservedValue = true;
            request.ObservedValue = 1;
            return new object?[] { request };
        }

        if (interfaceType == typeof(IBarcodeEncoder))
        {
            return new object?[] { CreateEncodeRequest() };
        }

        if (interfaceType == typeof(IBarcodeDecoder))
        {
            BarcodeDecodeRequest request = new BarcodeDecodeRequest();
            request.Symbol = CreateSymbol();
            BarcodeDecodeOptions options = new BarcodeDecodeOptions();
            options.PureSymbol = true;
            options.Polarity = BarcodePolarity.Automatic;
            options.MirrorMode = BarcodeMirrorMode.Both;
            request.Options = options;
            return new object?[] { request };
        }

        if (interfaceType == typeof(IBarcodeDetector))
        {
            BarcodeImage image = BarcodeTestBuilder.CreateGrayImage(
                12,
                12,
                255);
            BarcodeDetectionRequest request = new BarcodeDetectionRequest();
            request.Image = image;
            return new object?[] { request };
        }

        if (interfaceType == typeof(IBarcodeRenderer))
        {
            BarcodeRenderOptions options = new BarcodeRenderOptions();
            options.Format = BarcodeRenderFormat.Gray8;
            BarcodeRenderRequest request = new BarcodeRenderRequest();
            request.Symbol = CreateSymbol();
            request.Options = options;
            return new object?[] { request };
        }

        if (interfaceType == typeof(IBarcodeValidator))
        {
            BarcodeValidationRequest request = new BarcodeValidationRequest();
            request.Mode = BarcodeValidationMode.Strict;
            return new object?[] { request };
        }

        if (interfaceType == typeof(IGs1DataProcessor))
        {
            if (method.Name == "LookupGs1ApplicationIdentifier")
            {
                return new object?[] { "01" };
            }

            Gs1ProcessRequest request = new Gs1ProcessRequest();
            request.SourceText = "(01)09506000134352";
            request.SourceSyntax = Gs1Syntax.Bracketed;
            request.TargetSyntax = Gs1Syntax.Bracketed;
            return new object?[] { request };
        }

        if (interfaceType == typeof(IStructuredAppendAssembler))
        {
            return new object?[] { CreateStructuredAppendRequest() };
        }

        throw new global::System.InvalidOperationException(
            "The method has no inventory arguments: " + method.Name);
    }

    static BarcodeEncodeRequest CreateEncodeRequest()
    {
        BarcodePayload payload = new BarcodePayload();
        payload.Text = "API";
        BarcodeEncodeRequest request = new BarcodeEncodeRequest();
        request.Format = BarcodeFormat.Code128;
        request.Payload = payload;
        return request;
    }

    static BarcodeSymbol CreateSymbol()
    {
        return new BarcodeEncoder().EncodeBarcode(CreateEncodeRequest());
    }

    static StructuredAppendRequest CreateStructuredAppendRequest()
    {
        BarcodeDecodeResult first = CreateStructuredAppendSegment(
            0,
            false,
            "A",
            "1");
        BarcodeDecodeResult second = CreateStructuredAppendSegment(
            1,
            true,
            "B",
            "001001");
        StructuredAppendRequest request = new StructuredAppendRequest();
        request.Format = BarcodeFormat.DataMatrix;
        request.Segments = new BarcodeDecodeResult[] { second, first };
        request.RequireComplete = true;
        return request;
    }

    static BarcodeDecodeResult CreateStructuredAppendSegment(
        int index,
        bool isLast,
        string text,
        string fileIdentifier)
    {
        StructuredAppendInfo information = new StructuredAppendInfo();
        information.Format = BarcodeFormat.DataMatrix;
        information.Index = index;
        information.Count = 2;
        information.FileIdentifier = fileIdentifier;
        information.IsLastSegment = isLast;
        BarcodePayload payload = new BarcodePayload();
        payload.Text = text;
        payload.RawBytes = global::System.Text.Encoding.ASCII.GetBytes(text);
        payload.CharacterEncoding = "ISO-8859-1";
        payload.StructuredAppend = information;
        BarcodeDecodeMetadata metadata = new BarcodeDecodeMetadata();
        metadata.StructuredAppend = information;
        BarcodeDecodeResult segment = new BarcodeDecodeResult();
        segment.Format = BarcodeFormat.DataMatrix;
        segment.Dimension = BarcodeDimension.Matrix;
        segment.Payload = payload;
        segment.Metadata = metadata;
        return segment;
    }

    static bool IsResultType(global::System.Type type)
    {
        return type.IsClass && !type.IsAbstract &&
            (type.Name.EndsWith(
                "Result",
                global::System.StringComparison.Ordinal) ||
             type.Name.EndsWith(
                "Report",
                global::System.StringComparison.Ordinal) ||
             type.Name.EndsWith(
                "Attempt",
                global::System.StringComparison.Ordinal) ||
             type.Name == "BarcodeRenderedImage" ||
             type.Name == "BarcodeCandidate");
    }

    static object? CreateFieldValue(global::System.Type type)
    {
        if (type == typeof(string))
        {
            return "inventory";
        }

        if (type == typeof(bool))
        {
            return true;
        }

        if (type == typeof(int))
        {
            return 7;
        }

        if (type == typeof(double))
        {
            return 1.25;
        }

        if (type == typeof(uint))
        {
            return 7u;
        }

        if (type == typeof(byte))
        {
            return (byte)7;
        }

        if (type.IsEnum)
        {
            global::System.Array values = global::System.Enum.GetValues(type);
            return values.GetValue(values.Length - 1);
        }

        if (type.IsArray)
        {
            global::System.Type elementType = type.GetElementType()!;
            global::System.Array array = global::System.Array.CreateInstance(
                elementType,
                1);
            array.SetValue(CreateFieldValue(elementType), 0);
            return array;
        }

        return CreateApiInstance(type);
    }
}

internal class ApiConfigurationProviderStub : IBarcodeConfigurationProvider
{
    public BarcodeConfiguration GetBarcodeConfiguration()
    {
        return new BarcodeConfiguration();
    }
}
