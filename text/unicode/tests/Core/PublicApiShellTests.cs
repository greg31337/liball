using NUnit.Framework;

namespace unicode.Tests;

[TestFixture]
public class PublicApiShellTests
{
    [Test]
    public void RuntimeAssembly_ExposesMajorModuleShellClasses()
    {
        Type[] expectedTypes =
        [
            typeof(UnicodeVersion),
            typeof(UnicodeDataProvenance),
            typeof(UnicodeDataSource),
            typeof(UnicodeByteEncoding),
            typeof(UnicodeScalar),
            typeof(UnicodeScalarRecord),
            typeof(UnicodeScalarReader),
            typeof(UnicodeUtf16Diagnostic),
            typeof(UnicodeValidationResult),
            typeof(UnicodeRange),
            typeof(UnicodeRangeSet),
            typeof(UnicodeProperties),
            typeof(UnicodeNormalization),
            typeof(UnicodeHangul),
            typeof(UnicodeCase),
            typeof(UnicodeSegments),
            typeof(UnicodeSegmentRecord),
            typeof(UnicodeIdentifiers),
            typeof(UnicodeIdentifierDiagnostic),
            typeof(UnicodeIdentifierProfile),
            typeof(UnicodeIdentifierValidationResult),
            typeof(UnicodePrecis),
            typeof(UnicodePrecisDiagnostic),
            typeof(UnicodePrecisProfile),
            typeof(UnicodePrecisValidationResult),
            typeof(UnicodeSecurity),
            typeof(UnicodeSecurityCheckResult),
            typeof(UnicodeSecurityDiagnostic),
            typeof(UnicodePdfBidiRun),
            typeof(UnicodePdfEncoding),
            typeof(UnicodePdfFontSubset),
            typeof(UnicodePdfGlyphMap),
            typeof(UnicodePdfGlyphMapping),
            typeof(UnicodePdfScriptRun),
            typeof(UnicodePdfScriptScalarSet),
            typeof(UnicodePdfText),
            typeof(UnicodePdfCMap),
            typeof(UnicodePdfCMapDiagnostic),
            typeof(UnicodePdfCMapValidationResult),
            typeof(UnicodePdfDiagnostic),
            typeof(UnicodePdfDiagnosticResult),
            typeof(UnicodePdfDiagnostics),
        ];

        foreach (Type expectedType in expectedTypes)
        {
            Assert.That(expectedType.Namespace, Is.EqualTo("unicode"));
            Assert.That(expectedType.IsPublic, Is.True);
            Assert.That(expectedType.IsClass, Is.True);
            Assert.That(expectedType.Name, Does.StartWith("Unicode"));
        }
    }

    [Test]
    public void RuntimeAssembly_ExposesPublicEnumShells()
    {
        Type[] expectedTypes =
        [
            typeof(UnicodeNormalizationForm),
            typeof(UnicodeQuickCheckResult),
            typeof(UnicodeGeneralCategory),
            typeof(UnicodeScript),
            typeof(UnicodeBlock),
            typeof(UnicodeBidiClass),
            typeof(UnicodeEastAsianWidth),
            typeof(UnicodeBinaryProperty),
            typeof(UnicodeGraphemeBreakClass),
            typeof(UnicodeWordBreakClass),
            typeof(UnicodeSentenceBreakClass),
            typeof(UnicodeLineBreakClass),
            typeof(UnicodeCompareKeyMode),
            typeof(UnicodeIdentifierStatus),
            typeof(UnicodePrecisStatus),
            typeof(UnicodeSecurityFlag),
            typeof(UnicodeRestrictionLevel),
            typeof(UnicodePdfBidiDirection),
            typeof(UnicodePdfCMapStatus),
            typeof(UnicodePdfDiagnosticStatus),
            typeof(UnicodePdfTextEncoding),
        ];

        foreach (Type expectedType in expectedTypes)
        {
            Assert.That(expectedType.Namespace, Is.EqualTo("unicode"));
            Assert.That(expectedType.IsPublic, Is.True);
            Assert.That(expectedType.IsEnum, Is.True);
            Assert.That(expectedType.Name, Does.StartWith("Unicode"));
        }
    }

    [Test]
    public void RuntimeAssembly_ExposesConstrainedInterfaceShells()
    {
        Type[] expectedTypes =
        [
            typeof(IUnicodeIdentifierProfile),
            typeof(IUnicodePdfGlyphMap),
        ];

        foreach (Type expectedType in expectedTypes)
        {
            Assert.That(expectedType.Namespace, Is.EqualTo("unicode"));
            Assert.That(expectedType.IsPublic, Is.True);
            Assert.That(expectedType.IsInterface, Is.True);
            Assert.That(expectedType.Name, Does.Match("^I[A-Z]"));
            Assert.That(expectedType.GetInterfaces(), Is.Empty);
            Assert.That(expectedType.GetFields(), Is.Empty);

            foreach (System.Reflection.MethodInfo method in expectedType.GetMethods())
            {
                Assert.That(method.IsStatic, Is.False);
                Assert.That(method.IsGenericMethod, Is.False);
                Assert.That(method.IsAbstract, Is.True);
            }
        }

        Assert.That(typeof(IUnicodeIdentifierProfile).IsAssignableFrom(typeof(UnicodeIdentifierProfile)), Is.True);
        Assert.That(typeof(IUnicodePdfGlyphMap).IsAssignableFrom(typeof(UnicodePdfGlyphMap)), Is.True);
    }

    [Test]
    public void RuntimeAssembly_KeepsImplementationTypesInternal()
    {
        Type[] implementationTypes =
        [
            typeof(UnicodeRangeValue),
            typeof(UnicodeTableLookup),
            typeof(UnicodeCaseMappingMode),
            typeof(UnicodePdfGlyphRange),
            typeof(UnicodeCategoryTables),
            typeof(UnicodeNormalizationTables),
            typeof(UnicodeBoundaryTables),
            typeof(UnicodeSecurityTables),
            typeof(UnicodePdfEncodingTables),
        ];

        foreach (Type implementationType in implementationTypes)
        {
            Assert.That(implementationType.IsPublic, Is.False);
        }
    }
}
