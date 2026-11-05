using System.Reflection;

namespace ftp.Tests;

public class PublicApiCoverageInventoryTests
{
    [Test]
    public void Every_exported_API_type_and_operation_is_in_the_behavioral_inventory()
    {
        Type[] exported = typeof(FtpClient).Assembly
            .GetExportedTypes()
            .Where(type => type.Namespace == "ftp")
            .ToArray();
        Type[] contracts = exported.Where(type => type.IsInterface).ToArray();
        Type[] enums = exported.Where(type => type.IsEnum).ToArray();
        Type[] exceptions = exported.Where(type =>
            type.IsSubclassOf(typeof(Exception))).ToArray();
        Type[] staticData = exported.Where(type =>
            type.IsClass && type.IsAbstract && type.IsSealed).ToArray();
        Type[] implementations = exported.Where(type =>
            type.IsClass &&
            type.GetInterfaces().Any(IsFtpContract)).ToArray();
        Type[] data = exported.Except(contracts)
            .Except(enums)
            .Except(exceptions)
            .Except(staticData)
            .Except(implementations)
            .ToArray();

        MethodInfo[] contractMethods = contracts
            .SelectMany(contract => contract.GetMethods())
            .ToArray();
        MethodInfo[] implementationMethods = implementations
            .SelectMany(implementation => implementation.GetMethods(
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.DeclaredOnly))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(exported, Has.Length.EqualTo(130));
            Assert.That(contracts, Has.Length.EqualTo(15));
            Assert.That(enums, Has.Length.EqualTo(28));
            Assert.That(exceptions, Has.Length.EqualTo(8));
            Assert.That(staticData, Has.Length.EqualTo(6));
            Assert.That(implementations, Has.Length.EqualTo(13));
            Assert.That(data, Has.Length.EqualTo(60));
            Assert.That(contractMethods, Has.Length.EqualTo(91));
            Assert.That(implementationMethods, Has.Length.EqualTo(87));
            Assert.That(implementations.All(ImplementationMapsItsContract),
                Is.True);
            Assert.That(data.All(type =>
                type.GetConstructor(Type.EmptyTypes) != null), Is.True);
            Assert.That(exceptions.All(type =>
                type.GetConstructor(new[]
                {
                    typeof(string), typeof(Exception)
                }) != null), Is.True);
        });
    }

    [Test]
    public void Capability_independent_failure_paths_invoke_security_API_entries()
    {
        ArgumentNullException providerFailure =
            Assert.Throws<ArgumentNullException>(() =>
                new DefaultFtpCertificateProvider(null!))!;
        FtpClient client = new FtpClient(null);
        FtpProtocolException protectionFailure =
            Assert.Throws<FtpProtocolException>(() =>
                client.SetDataProtection(FtpDataProtectionLevel.Private))!;
        FtpProtocolException clearFailure =
            Assert.Throws<FtpProtocolException>(() =>
                client.ClearControlTls())!;

        Assert.Multiple(() =>
        {
            Assert.That(providerFailure.ParamName, Is.EqualTo("options"));
            Assert.That(protectionFailure.Message,
                Does.Contain("SetDataProtection"));
            Assert.That(clearFailure.Message,
                Does.Contain("ClearControlTls"));
        });
    }

    private static bool IsFtpContract(Type type)
    {
        return type.IsInterface &&
            type.Namespace == "ftp" &&
            type.Name.StartsWith("IFtp", StringComparison.Ordinal);
    }

    private static bool ImplementationMapsItsContract(Type implementation)
    {
        Type[] contracts = implementation.GetInterfaces()
            .Where(IsFtpContract)
            .ToArray();
        if (contracts.Length != 1)
        {
            return false;
        }

        InterfaceMapping mapping = implementation.GetInterfaceMap(contracts[0]);
        MethodInfo[] publicMethods = implementation.GetMethods(
            BindingFlags.Public |
            BindingFlags.Instance |
            BindingFlags.DeclaredOnly);
        return mapping.TargetMethods.Length == publicMethods.Length &&
            mapping.TargetMethods.All(publicMethods.Contains);
    }
}
