using uspto;

namespace uspto.Tests;

public class PublicImplementationConstructorTests
{
    [Test]
    public void EveryPublicImplementationClassHasAnExercisedConstructor()
    {
        IUsptoClientFactory factory = new UsptoClientFactory();
        IApiKeyProvider provider = new EnvironmentApiKeyProvider();
        IIdentifierNormalizer normalizer = new IdentifierNormalizer();
        IDataFallbackClient fallback = new DataFallbackClient(new DataFallbackPolicy());
        ICitationGraphInputBuilder citationBuilder = new CitationGraphInputBuilder();

        System.Reflection.Assembly assembly = typeof(UsptoClientFactory).Assembly;
        Type[] actual = assembly.GetExportedTypes()
            .Where(type => type.IsClass && !type.IsAbstract && type.GetInterfaces().Any(contract => contract.Assembly == assembly))
            .OrderBy(type => type.Name, StringComparer.Ordinal)
            .ToArray();
        Type[] expected = new Type[]
        {
            typeof(CitationGraphInputBuilder),
            typeof(DataFallbackClient),
            typeof(EnvironmentApiKeyProvider),
            typeof(IdentifierNormalizer),
            typeof(UsptoClientFactory)
        };

        Assert.Multiple(() =>
        {
            Assert.That(actual, Is.EqualTo(expected));
            Assert.That(factory, Is.TypeOf<UsptoClientFactory>());
            Assert.That(provider, Is.TypeOf<EnvironmentApiKeyProvider>());
            Assert.That(normalizer, Is.TypeOf<IdentifierNormalizer>());
            Assert.That(fallback, Is.TypeOf<DataFallbackClient>());
            Assert.That(citationBuilder, Is.TypeOf<CitationGraphInputBuilder>());
        });
    }

    [TestCase("null", "REQUIRED")]
    [TestCase("null-order", "REQUIRED")]
    [TestCase("empty-order", "INVALID_FALLBACK_ORDER")]
    [TestCase("long-order", "INVALID_FALLBACK_ORDER")]
    [TestCase("negative-cache-age", "INVALID_FALLBACK_FRESHNESS")]
    [TestCase("negative-local-age", "INVALID_FALLBACK_FRESHNESS")]
    [TestCase("authority", "INVALID_FALLBACK_AUTHORITY")]
    [TestCase("duplicate-online", "DUPLICATE_FALLBACK_ACCESS")]
    [TestCase("duplicate-cache", "DUPLICATE_FALLBACK_ACCESS")]
    [TestCase("duplicate-local", "DUPLICATE_FALLBACK_ACCESS")]
    [TestCase("access", "INVALID_FALLBACK_ACCESS")]
    public void DataFallbackConstructorRejectsInvalidPolicies(string scenario, string errorCode)
    {
        DataFallbackPolicy policy = InvalidFallbackPolicy(scenario);

        ValidationException failure = Assert.Throws<ValidationException>(() => new DataFallbackClient(policy))!;

        Assert.That(failure.ErrorCode, Is.EqualTo(errorCode));
    }

    private static DataFallbackPolicy InvalidFallbackPolicy(string scenario)
    {
        if (scenario == "null")
        {
            return null!;
        }

        DataFallbackPolicy policy = new DataFallbackPolicy();
        switch (scenario)
        {
            case "null-order":
                policy.AccessOrder = null!;
                break;
            case "empty-order":
                policy.AccessOrder = Array.Empty<FallbackAccess>();
                break;
            case "long-order":
                policy.AccessOrder = new[] { FallbackAccess.Online, FallbackAccess.Cache, FallbackAccess.Local, FallbackAccess.Online };
                break;
            case "negative-cache-age":
                policy.MaximumCacheAgeSeconds = -1;
                break;
            case "negative-local-age":
                policy.MaximumLocalAgeSeconds = -1;
                break;
            case "authority":
                policy.Authority = (FallbackAuthority)999;
                break;
            case "duplicate-online":
                policy.AccessOrder = new[] { FallbackAccess.Online, FallbackAccess.Online };
                break;
            case "duplicate-cache":
                policy.AccessOrder = new[] { FallbackAccess.Cache, FallbackAccess.Cache };
                break;
            case "duplicate-local":
                policy.AccessOrder = new[] { FallbackAccess.Local, FallbackAccess.Local };
                break;
            case "access":
                policy.AccessOrder = new[] { (FallbackAccess)999 };
                break;
            default:
                Assert.Fail("Unknown fallback-policy scenario: " + scenario);
                break;
        }

        return policy;
    }
}
