using System.Reflection;

namespace tiff.Tests;

[TestFixture]
public class TiffPublicApiContractCoverageTests
{
    static Assembly ApiAssembly = typeof(TiffApi).Assembly;
    static NullabilityInfoContext Nullability = new NullabilityInfoContext();

    [Test]
    public void PublicEnumValues_AreCompleteAndStable()
    {
        AssertEnum<TiffByteOrder>("Unknown=0,LittleEndian=18761,BigEndian=19789");
        AssertEnum<TiffCfaConversionMode>("Bilinear=0,Grayscale=1,Reject=2");
        AssertEnum<TiffCodecCapabilitySource>("Unavailable=0,BuiltIn=1,ApplicationProvided=2");
        AssertEnum<TiffCompression>("Unknown=0,None=1,CcittRle=2,CcittGroup3=3,CcittGroup4=4,Lzw=5,OldJpeg=6,Jpeg=7,Deflate=8,T85=9,T43=10,NeXt=32766,CcittRleW=32771,PackBits=32773,ThunderScan=32809,It8Ctpad=32895,It8Lw=32896,It8Mp=32897,It8Bl=32898,PixarFilm=32908,PixarLog=32909,AdobeDeflate=32946,Dcs=32947,Jbig=34661,Sgilog=34676,Sgilog24=34677,Jpeg2000=34712,Lzma=34925,Zstd=50000,WebP=50001,Jxl=50002");
        AssertEnum<TiffDataType>("Unknown=0,Byte=1,Ascii=2,Short=3,Long=4,Rational=5,SByte=6,Undefined=7,SShort=8,SLong=9,SRational=10,Float=11,Double=12,Ifd=13,Long8=16,SLong8=17,Ifd8=18");
        AssertEnum<TiffDiagnosticSeverity>("Warning=1,Error=2");
        AssertEnum<TiffErrorCode>("None=0,UnknownError=1,UnsupportedStream=2,NonSeekableStream=3,InvalidByteOrder=4,InvalidTiffVersion=5,InvalidBigTiffHeader=6,InvalidOffset=7,InvalidByteRange=8,IntegerOverflow=9,AllocationLimitExceeded=10,ImageDimensionLimitExceeded=11,IfdCountLimitExceeded=12,TagCountLimitExceeded=13,CyclicIfdChain=14,OverlappingDataBlock=15,DuplicateTag=16,MissingRequiredTag=17,InvalidTagType=18,InvalidTagCount=19,InvalidTagValue=20,InvalidAsciiValue=21,InvalidRationalValue=22,UnsupportedCompression=23,UnsupportedPhotometric=24,CodecExpansionLimitExceeded=25,TruncatedStream=26,PhotometricMismatch=27");
        AssertEnum<TiffExtraSample>("Unspecified=0,AssociatedAlpha=1,UnassociatedAlpha=2");
        AssertEnum<TiffFileKind>("Unknown=0,Classic=42,BigTiff=43");
        AssertEnum<TiffGeoKeyValueKind>("None=0,Short=1,Double=2,Ascii=3");
        AssertEnum<TiffGeoModelType>("Unknown=0,Projected=1,Geographic=2,Geocentric=3,UserDefined=32767");
        AssertEnum<TiffGeoRasterType>("Unknown=0,PixelIsArea=1,PixelIsPoint=2");
        AssertEnum<TiffImageJAxisKind>("Unknown=0,Channel=1,Depth=2,Time=3");
        AssertEnum<TiffJsonValueKind>("None=0,Object=1,Array=2,String=3,Number=4,Boolean=5,Null=6");
        AssertEnum<TiffOmeDimensionOrder>("Unknown=0,XYZCT=1,XYZTC=2,XYCZT=3,XYCTZ=4,XYTCZ=5,XYTZC=6");
        AssertEnum<TiffOrientation>("Unknown=0,TopLeft=1,TopRight=2,BottomRight=3,BottomLeft=4,LeftTop=5,RightTop=6,RightBottom=7,LeftBottom=8");
        AssertEnum<TiffPhotometric>("WhiteIsZero=0,BlackIsZero=1,Rgb=2,Palette=3,TransparencyMask=4,Cmyk=5,YCbCr=6,CieLab=8,IccLab=9,ItuLab=10,ColorFilterArray=32803,LogL=32844,LogLuv=32845,LinearRaw=34892");
        AssertEnum<TiffPlanarConfiguration>("Unknown=0,Contiguous=1,Separate=2");
        AssertEnum<TiffProgressOperation>("Open=0,Validation=1,Decode=2,RegionRead=3,Copy=4,Write=5");
        AssertEnum<TiffRegionOutput>("Source=0,Normalized=1");
        AssertEnum<TiffResolutionUnit>("Unknown=0,None=1,Inch=2,Centimeter=3");
        AssertEnum<TiffSampleFormat>("Unknown=0,UnsignedInteger=1,SignedInteger=2,IeeeFloatingPoint=3,Undefined=4,ComplexInteger=5,ComplexIeeeFloatingPoint=6");
        AssertEnum<TiffSourceLoadingMode>("Streaming=0,Buffered=1");
        AssertEnum<TiffUnsupportedFeaturePolicy>("Error=0,PreserveRaw=1,Skip=2,Ignore=3");
        AssertEnum<TiffValidationMode>("Strict=0,Permissive=1");
        AssertEnum<TiffWriterLayoutKind>("Strips=1,Tiles=2");

        Type[] publicEnums = ApiAssembly.GetExportedTypes().Where(type => type.IsEnum).OrderBy(type => type.Name).ToArray();
        Assert.That(publicEnums.Length, Is.EqualTo(26));
    }

    public static IEnumerable<TestCaseData> TagIdentifierCases()
    {
        foreach (FieldInfo field in typeof(TiffTagId).GetFields(BindingFlags.Public | BindingFlags.Static).OrderBy(field => field.Name))
        {
            yield return new TestCaseData(field.Name, (int)field.GetRawConstantValue()!).SetName("TagIdentifier_" + field.Name);
        }
    }

    [TestCaseSource(nameof(TagIdentifierCases))]
    public void EveryTagIdentifier_IsAUsableTiffTag(string name, int tagId)
    {
        TiffTagValue value = TiffApi.Data.CreateUnsignedScalarTag(tagId, TiffDataType.Long, 1);

        Assert.Multiple(() =>
        {
            Assert.That(name, Is.Not.Empty);
            Assert.That(tagId, Is.InRange(1, ushort.MaxValue));
            Assert.That(value.TagId, Is.EqualTo(tagId));
            Assert.That(value.DataType, Is.EqualTo(TiffDataType.Long));
            Assert.That(value.UnsignedValues, Is.EqualTo(new ulong[] { 1 }));
        });
    }

    public static IEnumerable<TestCaseData> DataClassCases()
    {
        foreach (Type type in GetDataClasses())
        {
            yield return new TestCaseData(type).SetName("DataConstructor_" + type.Name);
        }
    }

    [TestCaseSource(nameof(DataClassCases))]
    public void EveryDataClassConstructor_CreatesAnUsableInstance(Type type)
    {
        ConstructorInfo? constructor = type.GetConstructors(BindingFlags.Public | BindingFlags.Instance).SingleOrDefault();
        Assert.That(constructor, Is.Not.Null, type.Name + " must expose its data-model constructor.");

        object?[] arguments = constructor!.GetParameters().Select(parameter => CreateArgument(parameter.ParameterType)).ToArray();
        object value = constructor.Invoke(arguments);

        Assert.That(value, Is.TypeOf(type));
        foreach (FieldInfo field in GetPublicFields(type))
        {
            if (!field.FieldType.IsValueType && field.FieldType != typeof(string) && Nullability.Create(field).ReadState != NullabilityState.Nullable)
            {
                Assert.That(field.GetValue(value), Is.Not.Null, type.Name + "." + field.Name + " must have a usable default.");
            }
        }
    }

    public static IEnumerable<TestCaseData> CopyCases()
    {
        foreach (Type type in GetDataClasses())
        {
            MethodInfo? copy = GetCopyMethod(type);
            if (copy != null)
            {
                yield return new TestCaseData(type).SetName("DataCopy_" + type.Name);
            }
        }
    }

    [TestCaseSource(nameof(CopyCases))]
    public void EveryDataCopy_IsDetachedAndPreservesValues(Type type)
    {
        object original = Activator.CreateInstance(type)!;
        MethodInfo copyMethod = GetCopyMethod(type)!;
        object copy = copyMethod.Invoke(original, null)!;

        Assert.That(copy, Is.TypeOf(type));
        Assert.That(copy, Is.Not.SameAs(original));
        foreach (FieldInfo field in GetPublicFields(type))
        {
            AssertCopiedField(type, field, original, copy);
        }
    }

    [Test]
    public void ServiceFactory_ExposesAllReadyToUseServices()
    {
        Dictionary<string, Type> expected = new Dictionary<string, Type>
        {
            ["Factory"] = typeof(ITiffFactory),
            ["Data"] = typeof(ITiffDataFactory),
            ["Codec"] = typeof(ITiffCodecService),
            ["Copy"] = typeof(ITiffCopyService),
            ["DateTime"] = typeof(ITiffDateTimeService),
            ["Validation"] = typeof(ITiffValidationService),
            ["Inspection"] = typeof(ITiffInspectionService)
        };
        FieldInfo[] fields = typeof(TiffApi).GetFields(BindingFlags.Public | BindingFlags.Static);

        Assert.That(fields.Select(field => field.Name), Is.EquivalentTo(expected.Keys));
        foreach (FieldInfo field in fields)
        {
            object? service = field.GetValue(null);
            Assert.That(field.FieldType, Is.EqualTo(expected[field.Name]));
            Assert.That(service, Is.Not.Null);
            Assert.That(expected[field.Name].IsInstanceOfType(service), Is.True);
        }

        TiffTagRegistry first = TiffApi.Factory.CreateDefaultTagRegistry();
        TiffTagRegistry second = TiffApi.Factory.CreateDefaultTagRegistry();
        Assert.That(first, Is.Not.SameAs(second));
        Assert.That(first.Contains(TiffTagId.ImageWidth), Is.True);
    }

    [Test]
    public void EveryInterfaceMember_HasAConcreteImplementation()
    {
        Type[] implementations = ApiAssembly.GetTypes()
            .Where(type => type.IsClass && !type.IsAbstract)
            .Concat(new Type[] { typeof(ProgressProbe) })
            .ToArray();
        Type[] interfaces = ApiAssembly.GetExportedTypes().Where(type => type.IsInterface).OrderBy(type => type.Name).ToArray();

        foreach (Type interfaceType in interfaces)
        {
            Type[] candidates = implementations.Where(interfaceType.IsAssignableFrom).ToArray();
            Assert.That(candidates, Is.Not.Empty, interfaceType.Name + " has no concrete implementation available to exercise.");
            foreach (MethodInfo interfaceMethod in interfaceType.GetMethods())
            {
                bool mapped = candidates.Any(candidate => HasConcreteMapping(candidate, interfaceType, interfaceMethod));
                Assert.That(mapped, Is.True, interfaceType.Name + "." + interfaceMethod.Name + " is not concretely implemented.");
            }
        }
    }

    public static IEnumerable<TestCaseData> SuccessResultCases()
    {
        foreach (Type type in GetDataClasses())
        {
            if (type.GetField("Success", BindingFlags.Public | BindingFlags.Instance) != null)
            {
                yield return new TestCaseData(type).SetName("SuccessAndDiagnostics_" + type.Name);
            }
        }
    }

    [TestCaseSource(nameof(SuccessResultCases))]
    public void EverySuccessResult_ProvidesWritableSuccessAndDiagnostics(Type type)
    {
        object result = Activator.CreateInstance(type)!;
        FieldInfo successField = type.GetField("Success")!;
        FieldInfo diagnosticsField = type.GetField("Diagnostics")!;
        TiffDiagnostics diagnostics = (TiffDiagnostics)diagnosticsField.GetValue(result)!;

        successField.SetValue(result, true);
        diagnostics.AddError(TiffErrorCode.UnknownError, "failure");

        Assert.That(successField.GetValue(result), Is.True);
        Assert.That(diagnostics.Count(), Is.EqualTo(1));
        Assert.That(diagnostics.HasErrors(), Is.True);
        Assert.That(diagnostics.ContainsCode(TiffErrorCode.UnknownError), Is.True);
    }

    [Test]
    public void Services_ReturnSuccessAndDiagnosticResults()
    {
        byte[] validBytes = CreateSinglePixelFile();
        TiffFile validFile = TiffApi.Factory.OpenReadBytes(validBytes, new TiffReadOptions());
        TiffValidationResult validResult = TiffApi.Validation.ValidateBytes(validBytes, new TiffReadOptions());
        TiffInspectionResult invalidResult = TiffApi.Inspection.InspectBytes(new byte[] { 1, 2, 3 }, new TiffReadOptions());

        Assert.Multiple(() =>
        {
            Assert.That(validFile.GetSuccess(), Is.True);
            Assert.That(validResult.Success, Is.True);
            Assert.That(validResult.Diagnostics.HasErrors(), Is.False);
            Assert.That(invalidResult.Success, Is.False);
            Assert.That(invalidResult.Diagnostics.HasErrors(), Is.True);
        });

        validFile.Close();
    }

    [Test]
    public void Services_ReportInvalidArgumentsWithoutLeakingUnstructuredFailures()
    {
        TiffStreamSource source = TiffApi.Factory.CreateStreamSource(null!);
        TiffFile file = TiffApi.Factory.OpenReadBytes(null!, null!);
        TiffWriter pathWriter = TiffApi.Factory.CreatePathWriter("", null!);
        TiffWriter streamWriter = TiffApi.Factory.CreateStreamWriter(null!, null!);
        TiffDateTimeParseResult dateTime = TiffApi.DateTime.ParseTiffDateTime(null!);
        TiffMetadataCopyResult copy = TiffApi.Copy.CopyMetadata(null!);
        TiffValidationResult validation = TiffApi.Validation.ValidateBytes(null!, null!);
        TiffInspectionResult inspection = TiffApi.Inspection.InspectBytes(null!, null!);
        TiffCodecResult codec = TiffApi.Codec.Decode(null!, ReadOnlySpan<byte>.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(source.GetIsOpen(), Is.False);
            Assert.That(file.GetSuccess(), Is.False);
            Assert.That(file.GetDiagnostics().HasErrors(), Is.True);
            Assert.That(pathWriter.GetSuccess(), Is.False);
            Assert.That(streamWriter.GetSuccess(), Is.False);
            Assert.That(dateTime.Success, Is.False);
            Assert.That(dateTime.Diagnostics.HasErrors(), Is.True);
            Assert.That(copy.Success, Is.False);
            Assert.That(copy.Diagnostics.HasErrors(), Is.True);
            Assert.That(validation.Success, Is.False);
            Assert.That(validation.Diagnostics.HasErrors(), Is.True);
            Assert.That(inspection.Success, Is.False);
            Assert.That(inspection.Diagnostics.HasErrors(), Is.True);
            Assert.That(codec.Success, Is.False);
            Assert.That(codec.Diagnostics.HasErrors(), Is.True);
        });

        file.Close();
        pathWriter.Close();
        streamWriter.Close();
    }

    [Test]
    public void ClosedObjects_RejectFurtherOperationsDeterministically()
    {
        byte[] bytes = CreateSinglePixelFile();
        TiffFile file = TiffApi.Factory.OpenReadBytes(bytes, new TiffReadOptions());
        TiffImage image = file.GetImage(0);
        TiffExternalOverviewSet overviews = TiffApi.Factory.ResolveExternalOverviewStreams(image, new Stream[0], new TiffReadOptions());
        TiffStreamSource streamSource = TiffApi.Factory.CreateStreamSource(new MemoryStream(bytes));
        string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".tif");
        File.WriteAllBytes(path, bytes);
        TiffFileSource fileSource = TiffApi.Factory.CreateFileSource(path);
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());

        overviews.Close();
        streamSource.Close();
        fileSource.Close();
        writer.Close();
        file.Close();
        TiffRawStripReadResult readAfterClose = image.ReadRawStrip(0);
        int addAfterClose = writer.AddImage(TiffApi.Data.CreateImageDefinition(CreateSinglePixelTags()));

        Assert.Multiple(() =>
        {
            Assert.That(overviews.GetIsOpen(), Is.False);
            Assert.That(streamSource.GetIsOpen(), Is.False);
            Assert.That(streamSource.GetLength(), Is.EqualTo(0));
            Assert.That(streamSource.Read(0, new byte[1]), Is.EqualTo(0));
            Assert.That(fileSource.GetIsOpen(), Is.False);
            Assert.That(fileSource.GetLength(), Is.EqualTo(0));
            Assert.That(fileSource.Read(0, new byte[1]), Is.EqualTo(0));
            Assert.That(writer.GetIsOpen(), Is.False);
            Assert.That(addAfterClose, Is.EqualTo(-1));
            Assert.That(writer.GetDiagnostics().HasErrors(), Is.True);
            Assert.That(file.GetIsOpen(), Is.False);
            Assert.That(image.GetIsOpen(), Is.False);
            Assert.That(readAfterClose.Success, Is.False);
            Assert.That(readAfterClose.Diagnostics.HasErrors(), Is.True);
        });

        File.Delete(path);
    }

    static Type[] GetDataClasses()
    {
        return ApiAssembly.GetExportedTypes()
            .Where(type => type.IsClass && !type.IsAbstract && GetPublicFields(type).Length > 0)
            .OrderBy(type => type.Name)
            .ToArray();
    }

    static FieldInfo[] GetPublicFields(Type type)
    {
        return type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
    }

    static MethodInfo? GetCopyMethod(Type type)
    {
        return type.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .SingleOrDefault(method => method.Name == "Copy" && method.GetParameters().Length == 0 && method.ReturnType == type);
    }

    static object? CreateArgument(Type type)
    {
        if (type == typeof(string))
        {
            return "sample";
        }

        if (type.IsEnum)
        {
            return Enum.GetValues(type).GetValue(0);
        }

        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }

        return Activator.CreateInstance(type);
    }

    static void AssertCopiedField(Type ownerType, FieldInfo field, object original, object copy)
    {
        object? originalValue = field.GetValue(original);
        object? copiedValue = field.GetValue(copy);
        string message = ownerType.Name + "." + field.Name;

        if (originalValue == null)
        {
            Assert.That(copiedValue, Is.Null, message);
            return;
        }

        if (field.FieldType.IsValueType || field.FieldType == typeof(string))
        {
            Assert.That(copiedValue, Is.EqualTo(originalValue), message);
            return;
        }

        if (field.FieldType.IsArray)
        {
            Assert.That(copiedValue, Is.Not.SameAs(originalValue), message + " must be detached.");
            Assert.That(((Array)copiedValue!).Length, Is.EqualTo(((Array)originalValue).Length), message);
            return;
        }

        if (GetPublicFields(field.FieldType).Length > 0)
        {
            Assert.That(copiedValue, Is.Not.SameAs(originalValue), message + " must be detached.");
        }
    }

    static bool HasConcreteMapping(Type implementationType, Type interfaceType, MethodInfo interfaceMethod)
    {
        InterfaceMapping mapping = implementationType.GetInterfaceMap(interfaceType);
        int index = Array.IndexOf(mapping.InterfaceMethods, interfaceMethod);
        return index >= 0 && !mapping.TargetMethods[index].IsAbstract;
    }

    static void AssertEnum<T>(string expectedText) where T : struct, Enum
    {
        string[] expected = expectedText.Split(',');
        string[] actualNames = Enum.GetNames<T>();
        Assert.That(actualNames, Has.Length.EqualTo(expected.Length), typeof(T).Name);
        for (int index = 0; index < expected.Length; index++)
        {
            string[] pair = expected[index].Split('=');
            Assert.That(actualNames[index], Is.EqualTo(pair[0]), typeof(T).Name + " member " + index);
            T value = Enum.Parse<T>(pair[0]);
            Assert.That(Convert.ToInt32(value), Is.EqualTo(int.Parse(pair[1])), typeof(T).Name + "." + pair[0]);
        }
    }

    static byte[] CreateSinglePixelFile()
    {
        TiffWriter writer = TiffApi.Factory.CreateWriter(new TiffWriteOptions());
        int imageIndex = writer.AddImage(TiffApi.Data.CreateImageDefinition(CreateSinglePixelTags()));
        TiffRawStripWriteResult write = writer.WriteRawStrip(imageIndex, 0, new byte[] { 7 });
        TiffDiagnostics flush = writer.Flush();
        Assert.That(write.Success, Is.True);
        Assert.That(flush.HasErrors(), Is.False);
        byte[] bytes = writer.GetFileBytes();
        writer.Close();
        return bytes;
    }

    static TiffTagValue[] CreateSinglePixelTags()
    {
        return new TiffTagValue[]
        {
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageWidth, TiffDataType.Long, 1),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.ImageLength, TiffDataType.Long, 1),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.BitsPerSample, TiffDataType.Short, 8),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.Compression, TiffDataType.Short, (ulong)TiffCompression.None),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.PhotometricInterpretation, TiffDataType.Short, (ulong)TiffPhotometric.BlackIsZero),
            TiffApi.Data.CreateUnsignedScalarTag(TiffTagId.RowsPerStrip, TiffDataType.Long, 1)
        };
    }

    class ProgressProbe : ITiffProgress
    {
        public void Report(TiffProgress progress)
        {
        }
    }
}
