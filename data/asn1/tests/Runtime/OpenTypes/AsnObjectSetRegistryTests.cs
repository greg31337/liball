using asn1;

namespace asn1.Tests;

public class AsnObjectSetRegistryTests
{
    [Test]
    public void RegistryCombinesStaticAndDynamicEntriesWithoutMutatingSchema()
    {
        AsnInformationObject root = CreateObject("Catalog.root");
        AsnInformationObject knownExtension = CreateObject("Catalog.known");
        AsnInformationObject dynamicExtension = CreateObject("Catalog.dynamic");
        AsnOpenTypeDispatchEntry rootDispatch = CreateDispatch("root", 1);
        AsnOpenTypeDispatchEntry dynamicDispatch = CreateDispatch("dynamic", 2);
        AsnInformationObjectSet objectSet = new AsnInformationObjectSet
        {
            StableId = "Catalog.Messages",
            Extensible = true,
            RootObjects = [root],
            AdditionalObjects = [knownExtension],
            Objects = [root, knownExtension],
            DispatchEntries = [rootDispatch]
        };
        AsnObjectSetRegistry registry = new AsnObjectSetRegistry();

        registry.RegisterObject(objectSet, dynamicExtension, dynamicDispatch);
        AsnInformationObject[] effectiveObjects = registry.GetEffectiveObjects(objectSet);
        AsnOpenTypeDispatchEntry[] effectiveDispatch =
            registry.GetEffectiveDispatchEntries(objectSet);

        Assert.That(effectiveObjects.Select(item => item.StableId), Is.EqualTo(new[]
        {
            "Catalog.root",
            "Catalog.known",
            "Catalog.dynamic"
        }));
        Assert.That(effectiveDispatch, Is.EqualTo(new[] { rootDispatch, dynamicDispatch }));
        Assert.That(registry.ResolveDispatchEntry(objectSet, IntegerValue(1)),
            Is.SameAs(rootDispatch));
        Assert.That(registry.ResolveDispatchEntry(objectSet, IntegerValue(2)),
            Is.SameAs(dynamicDispatch));
        Assert.That(registry.ResolveDispatchEntry(objectSet, IntegerValue(3)), Is.Null);
        Assert.That(objectSet.Objects, Is.EqualTo(new[] { root, knownExtension }));
        Assert.That(objectSet.DispatchEntries, Is.EqualTo(new[] { rootDispatch }));

        effectiveObjects[0] = dynamicExtension;
        effectiveDispatch[0] = dynamicDispatch;
        Assert.That(registry.GetEffectiveObjects(objectSet)[0], Is.SameAs(root));
        Assert.That(registry.GetEffectiveDispatchEntries(objectSet)[0],
            Is.SameAs(rootDispatch));
    }

    [Test]
    public void RegistriesAreIsolatedPerCatalogAndCodecOptions()
    {
        AsnInformationObjectSet objectSet = new AsnInformationObjectSet
        {
            StableId = "Catalog.Messages",
            Extensible = true
        };
        AsnReflectionCatalog firstCatalog = new AsnReflectionCatalog();
        AsnReflectionCatalog secondCatalog = new AsnReflectionCatalog();
        AsnObjectSetRegistry codecRegistry = new AsnObjectSetRegistry();
        AsnCodecOptions options = new AsnCodecOptions
        {
            ObjectSetRegistry = codecRegistry
        };

        firstCatalog.ObjectSetRegistry.RegisterObject(
            objectSet,
            CreateObject("Catalog.first"),
            null);
        codecRegistry.RegisterObject(
            objectSet,
            CreateObject("Codec.only"),
            null);

        Assert.That(firstCatalog.ObjectSetRegistry.GetEffectiveObjects(objectSet)
            .Select(item => item.StableId), Is.EqualTo(new[] { "Catalog.first" }));
        Assert.That(secondCatalog.ObjectSetRegistry.GetEffectiveObjects(objectSet), Is.Empty);
        Assert.That(options.ObjectSetRegistry?.GetEffectiveObjects(objectSet)
            .Select(item => item.StableId), Is.EqualTo(new[] { "Codec.only" }));
    }

    [Test]
    public void RegistryRejectsClosedSetsAndDuplicateIdentitiesOrKeys()
    {
        AsnObjectSetRegistry registry = new AsnObjectSetRegistry();
        AsnInformationObjectSet closed = new AsnInformationObjectSet();
        AsnInformationObjectSet extensible = new AsnInformationObjectSet
        {
            Extensible = true,
            Objects = [CreateObject("Catalog.root")],
            DispatchEntries = [CreateDispatch("root", 1)]
        };

        Assert.That(
            () => registry.RegisterObject(closed, CreateObject("Catalog.closed"), null),
            Throws.ArgumentException);
        Assert.That(
            () => registry.RegisterObject(
                extensible,
                CreateObject("Catalog.root"),
                CreateDispatch("other", 2)),
            Throws.ArgumentException);
        Assert.That(
            () => registry.RegisterObject(
                extensible,
                CreateObject("Catalog.other"),
                CreateDispatch("duplicate", 1)),
            Throws.ArgumentException);
        Assert.That(
            () => registry.RegisterObject(
                extensible,
                CreateObject("Catalog.invalid"),
                new AsnOpenTypeDispatchEntry()),
            Throws.ArgumentException);
        Assert.That(registry.GetEffectiveObjects(extensible).Length, Is.EqualTo(1));
    }

    [Test]
    public void RegistryRejectsNullArgumentsAndDuplicateObjectReferences()
    {
        AsnObjectSetRegistry registry = new AsnObjectSetRegistry();
        AsnInformationObject item = CreateObject("Catalog.dynamic");
        AsnInformationObjectSet objectSet = new AsnInformationObjectSet
        {
            Extensible = true
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                () => registry.RegisterObject(null!, item, null),
                Throws.ArgumentNullException);
            Assert.That(
                () => registry.RegisterObject(objectSet, null!, null),
                Throws.ArgumentNullException);
            Assert.That(
                () => registry.GetEffectiveObjects(null!),
                Throws.ArgumentNullException);
            Assert.That(
                () => registry.GetEffectiveDispatchEntries(null!),
                Throws.ArgumentNullException);
            Assert.That(
                () => registry.ResolveDispatchEntry(null!, IntegerValue(1)),
                Throws.ArgumentNullException);
            Assert.That(
                () => registry.ResolveDispatchEntry(objectSet, null!),
                Throws.ArgumentNullException);
        });

        registry.RegisterObject(objectSet, item, null);
        Assert.That(
            () => registry.RegisterObject(objectSet, item, null),
            Throws.ArgumentException);
    }

    [Test]
    public void RegistryDistinguishesMissingAndUnequalStableIdentitiesAndInvalidKeys()
    {
        AsnObjectSetRegistry registry = new AsnObjectSetRegistry();
        AsnInformationObjectSet namedStatic = new AsnInformationObjectSet
        {
            Extensible = true,
            Objects = [CreateObject("Catalog.named")]
        };
        AsnInformationObjectSet unnamedStatic = new AsnInformationObjectSet
        {
            Extensible = true,
            Objects = [CreateObject("")]
        };
        AsnInformationObjectSet invalidDispatch = new AsnInformationObjectSet
        {
            Extensible = true,
            DispatchEntries = [new AsnOpenTypeDispatchEntry()]
        };

        registry.RegisterObject(namedStatic, CreateObject(""), null);
        registry.RegisterObject(unnamedStatic, CreateObject(""), null);

        Assert.That(registry.GetEffectiveObjects(namedStatic).Length, Is.EqualTo(2));
        Assert.That(registry.GetEffectiveObjects(unnamedStatic).Length, Is.EqualTo(2));
        Assert.That(
            registry.ResolveDispatchEntry(invalidDispatch, IntegerValue(1)),
            Is.Null);
    }

    [Test]
    public void ConcurrentRegistrationProducesCompleteSnapshots()
    {
        const int registrationCount = 128;
        AsnInformationObjectSet objectSet = new AsnInformationObjectSet
        {
            StableId = "Catalog.Concurrent",
            Extensible = true
        };
        AsnObjectSetRegistry registry = new AsnObjectSetRegistry();

        Parallel.For(0, registrationCount, index =>
        {
            registry.RegisterObject(
                objectSet,
                CreateObject("Catalog.dynamic." + index.ToString()),
                CreateDispatch("dynamic." + index.ToString(), index));
        });

        AsnInformationObject[] objects = registry.GetEffectiveObjects(objectSet);
        AsnOpenTypeDispatchEntry[] entries =
            registry.GetEffectiveDispatchEntries(objectSet);
        Assert.That(objects.Length, Is.EqualTo(registrationCount));
        Assert.That(objects.Select(item => item.StableId).Distinct().Count(),
            Is.EqualTo(registrationCount));
        Assert.That(entries.Length, Is.EqualTo(registrationCount));
        Assert.That(registry.ResolveDispatchEntry(
            objectSet,
            IntegerValue(registrationCount - 1)), Is.Not.Null);
    }

    private static AsnInformationObject CreateObject(string stableId)
    {
        return new AsnInformationObject { StableId = stableId };
    }

    private static AsnOpenTypeDispatchEntry CreateDispatch(string stableId, int key)
    {
        return new AsnOpenTypeDispatchEntry
        {
            StableId = stableId,
            Key = IntegerValue(key),
            Type = new AsnType { Kind = AsnTypeKind.Integer }
        };
    }

    private static AsnValue IntegerValue(int value)
    {
        return new AsnValue
        {
            Kind = AsnValueKind.Integer,
            HasIntegerValue = true,
            IntegerValue = value
        };
    }
}
