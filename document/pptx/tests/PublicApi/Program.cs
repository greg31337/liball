using System.Reflection;
using pptx;

IPptxDocumentFactory documents = new PptxDocumentFactory();
IPptxPresentationOperations presentations = new PptxPresentationOperations();
IPptxSlideOperations slides = new PptxSlideOperations();
IPptxBoundsOperations bounds = new PptxBoundsOperations();
IPptxResultOperations results = new PptxResultOperations();
IPptxTemplateDataOperations templateData = new PptxTemplateDataOperations();
IPptxTemplateFiller templateFiller = new PptxTemplateFiller();
IPptxTextExtractor textExtractor = new PptxTextExtractor();
IPptxHtmlExporter htmlExporter = new PptxHtmlExporter();
IPptxSvgExporter svgExporter = new PptxSvgExporter();
IPptxJsonSummaryExporter jsonExporter = new PptxJsonSummaryExporter();
IPptxTextOutlineExporter outlineExporter = new PptxTextOutlineExporter();
IPptxComparisonOperations comparisons = new PptxComparisonOperations();
IPptxSplitter splitter = new PptxSplitter();

PptxDocument document = documents.CreatePresentation();
IPptxDocument publicDocument = document;
IPptxPresentationSource source = document;
PptxPresentation presentation = source.LoadPresentation();
PptxSlide slide = presentations.GetSlide(presentation, 0);

slides.AddTextBox(slide, "{{title}}", bounds.FromInches(1.0, 1.0, 4.0, 1.0));
slides.AddAutoShape(slide, PptxShapeType.Rectangle, bounds.FromInches(1.0, 2.0, 2.0, 1.0));

PptxChartData chartData = new PptxChartData();
chartData.CategoryKind = PptxChartCategoryKind.Text;
chartData.Categories.Add("Q1");
PptxChartSeries series = new PptxChartSeries();
series.Name = "Revenue";
series.Values.Add("10");
chartData.Series.Add(series);
slides.AddChart(slide, PptxChartType.Column, chartData, bounds.FromInches(3.5, 2.0, 4.0, 3.0));

PptxTemplateData replacements = new PptxTemplateData();
templateData.Add(replacements, "title", "Public API");
Require(templateFiller.FillPresentation(presentation, replacements) == 1, "Template workflow did not replace its token.");
Require(textExtractor.ExtractPresentationText(presentation).Contains("Public API"), "Text extraction did not see the replacement.");
Require(htmlExporter.ExportPresentation(presentation).Contains("Public API"), "HTML export did not see the replacement.");
Require(svgExporter.ExportSlide(slide).Contains("svg"), "SVG export did not produce an SVG document.");
Require(jsonExporter.ExportPresentation(presentation).Contains("slideCount"), "JSON export did not produce its summary.");
Require(outlineExporter.ExportPresentationText(presentation).Contains("Public API"), "Outline export did not see the replacement.");

PptxResult validation = publicDocument.Validate();
Require(!results.Failed(validation), results.FormatDiagnostics(validation));
byte[] bytes = publicDocument.SaveBytes();
PptxDocument reopened = documents.OpenBytes(bytes);
Require(presentations.SlideCount(reopened.LoadPresentation()) == 1, "The public presentation did not round-trip.");
PptxPresentationComparison comparison = comparisons.CompareDocuments(document, reopened);
Require(comparison.Manifest != null && comparison.EditableModel != null, "The comparison workflow did not return both comparison views.");
Require(splitter.SplitDocument(reopened).Count == 1, "The split workflow did not return one document per slide.");

AuditPublicSurface(document);

static void AuditPublicSurface(PptxDocument workflowDocument)
{
    Assembly assembly = typeof(PptxDocument).Assembly;
    Type[] types = assembly.GetExportedTypes().Where(type => type.Namespace == "pptx").ToArray();

    Type[] interfaces = types.Where(type => type.IsInterface).ToArray();
    foreach (Type contract in interfaces)
    {
        Type[] implementations = types.Where(type => type.IsClass && !type.IsAbstract && contract.IsAssignableFrom(type)).ToArray();
        Require(implementations.Length > 0, $"No public implementation exists for {contract.Name}.");
        foreach (Type implementation in implementations)
        {
            if (implementation == typeof(PptxDocument))
            {
                Require(contract.IsInstanceOfType(workflowDocument), $"The workflow document does not implement {contract.Name}.");
            }
            else
            {
                ConstructorInfo? constructor = implementation.GetConstructor(Type.EmptyTypes);
                Require(constructor != null, $"{implementation.Name} cannot be constructed by a public consumer.");
                ConstructorInfo usableConstructor = constructor ?? throw new ArgumentException($"{implementation.Name} has no public constructor.");
                object instance = usableConstructor.Invoke(Array.Empty<object>());
                Require(contract.IsInstanceOfType(instance), $"{implementation.Name} does not satisfy {contract.Name} at runtime.");
            }
        }
    }

    Type[] enums = types.Where(type => type.IsEnum).ToArray();
    foreach (Type enumType in enums)
    {
        Array values = Enum.GetValues(enumType);
        Require(values.Length > 0, $"{enumType.Name} has no public values.");
        foreach (object value in values)
        {
            Require(Enum.IsDefined(enumType, value), $"{enumType.Name} contains an undefined value.");
        }
    }

    Type[] staticClasses = types.Where(type => type.IsClass && type.IsAbstract && type.IsSealed).ToArray();
    int constantCount = 0;
    foreach (Type staticClass in staticClasses)
    {
        FieldInfo[] constants = staticClass.GetFields(BindingFlags.Public | BindingFlags.Static);
        Require(constants.Length > 0, $"{staticClass.Name} exposes no public constants.");
        foreach (FieldInfo constant in constants)
        {
            object? value = constant.GetValue(null);
            Require(value != null, $"{staticClass.Name}.{constant.Name} is null.");
            constantCount++;
        }
    }

    Type[] dataTypes = types.Where(IsDataType).ToArray();
    int constructedDataTypes = 0;
    foreach (Type dataType in dataTypes)
    {
        ConstructorInfo[] constructors = dataType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);
        if (constructors.Length == 0)
        {
            Require(HasPublicProductionPath(dataType, types), $"No supported public workflow produces {dataType.Name}.");
            continue;
        }

        ConstructorInfo constructor = constructors.OrderBy(candidate => candidate.GetParameters().Length).First();
        object?[] arguments = constructor.GetParameters().Select(parameter => CreateArgument(parameter.ParameterType, parameter.Name)).ToArray();
        object value = constructor.Invoke(arguments);
        Require(dataType.IsInstanceOfType(value), $"The {dataType.Name} constructor returned the wrong runtime type.");
        constructedDataTypes++;
    }

    Type[] operationClasses = types.Where(type => type.IsClass && !type.IsAbstract && type.Name.EndsWith("Operations", StringComparison.Ordinal)).ToArray();
    foreach (Type operationClass in operationClasses)
    {
        object? value = Activator.CreateInstance(operationClass);
        Require(value != null, $"{operationClass.Name} could not be constructed.");
    }

    Require(interfaces.Length > 0, "No public interfaces were audited.");
    Require(enums.Length > 0, "No public enums were audited.");
    Require(constantCount > 0, "No public static constants were audited.");
    Require(constructedDataTypes > 0, "No public data types were constructed.");
    Require(operationClasses.Length > 0, "No public operation classes were constructed.");
}

static bool IsDataType(Type type)
{
    if (!type.IsClass || type.IsAbstract || typeof(Exception).IsAssignableFrom(type))
    {
        return false;
    }

    if (type.GetInterfaces().Any(contract => contract.IsPublic && contract.Namespace == "pptx"))
    {
        return false;
    }

    return type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Length > 0;
}

static bool HasPublicProductionPath(Type dataType, Type[] publicTypes)
{
    foreach (Type type in publicTypes)
    {
        foreach (MethodInfo method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (ContainsType(method.ReturnType, dataType)) return true;
        }

        foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
        {
            if (ContainsType(field.FieldType, dataType)) return true;
        }
    }

    return false;
}

static bool ContainsType(Type candidate, Type expected)
{
    if (candidate == expected) return true;
    return candidate.IsGenericType && candidate.GetGenericArguments().Contains(expected);
}

static object? CreateArgument(Type type, string? name)
{
    if (type == typeof(string))
    {
        return name switch
        {
            "extension" => "bin",
            "contentType" => "application/octet-stream",
            "partName" => "/ppt/test.xml",
            "relationshipType" => "urn:pptx:test",
            "targetUri" => "target.xml",
            "sourcePartName" => "/ppt/presentation.xml",
            _ => "value"
        };
    }

    if (type == typeof(int)) return 1;
    if (type == typeof(long)) return 1L;
    if (type == typeof(double)) return 1.0;
    if (type == typeof(bool)) return false;
    if (type.IsEnum) return Enum.GetValues(type).GetValue(0);

    ConstructorInfo? constructor = type.GetConstructor(Type.EmptyTypes);
    if (constructor != null) return constructor.Invoke(Array.Empty<object>());

    ConstructorInfo? shortest = type.GetConstructors().OrderBy(candidate => candidate.GetParameters().Length).FirstOrDefault();
    if (shortest != null)
    {
        object?[] arguments = shortest.GetParameters().Select(parameter => CreateArgument(parameter.ParameterType, parameter.Name)).ToArray();
        return shortest.Invoke(arguments);
    }

    throw new ArgumentException($"No public sample can be created for {type.Name}.");
}

static void Require(bool condition, string message)
{
    if (!condition) throw new ArgumentException(message);
}
