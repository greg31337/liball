using System.Reflection;
using argon2;

namespace argon2.Tests.Api;

public class Argon2ContractTests
{
    [Test]
    public void PrimitiveExposesDestinationHashingAndRawVerification()
    {
        MethodInfo computeHash = GetRequiredMethod(typeof(IArgon2Primitive), "ComputeHash");
        MethodInfo verifyHash = GetRequiredMethod(typeof(IArgon2Primitive), "VerifyHash");
        ParameterInfo[] computeParameters = computeHash.GetParameters();
        ParameterInfo[] verifyParameters = verifyHash.GetParameters();

        Assert.Multiple(() =>
        {
            Assert.That(computeHash.ReturnType, Is.EqualTo(typeof(void)));
            Assert.That(computeParameters.Length, Is.EqualTo(7));
            Assert.That(computeParameters[0].ParameterType, Is.EqualTo(typeof(ReadOnlySpan<byte>)));
            Assert.That(computeParameters[1].ParameterType, Is.EqualTo(typeof(ReadOnlySpan<byte>)));
            Assert.That(computeParameters[2].ParameterType, Is.EqualTo(typeof(ReadOnlySpan<byte>)));
            Assert.That(computeParameters[3].ParameterType, Is.EqualTo(typeof(ReadOnlySpan<byte>)));
            Assert.That(computeParameters[4].ParameterType, Is.EqualTo(typeof(Argon2Parameters)));
            Assert.That(computeParameters[5].ParameterType, Is.EqualTo(typeof(Argon2ExecutionOptions)));
            Assert.That(computeParameters[6].ParameterType, Is.EqualTo(typeof(Span<byte>)));
            Assert.That(verifyHash.ReturnType, Is.EqualTo(typeof(bool)));
            Assert.That(verifyParameters.Length, Is.EqualTo(7));
            Assert.That(verifyParameters[6].ParameterType, Is.EqualTo(typeof(ReadOnlySpan<byte>)));
        });
    }

    [Test]
    public void KeyDeriverExposesOneShotAllocatingDerivation()
    {
        MethodInfo deriveKey = GetRequiredMethod(typeof(IArgon2KeyDeriver), "DeriveKey");
        ParameterInfo[] parameters = deriveKey.GetParameters();

        Assert.Multiple(() =>
        {
            Assert.That(deriveKey.ReturnType, Is.EqualTo(typeof(byte[])));
            Assert.That(parameters.Length, Is.EqualTo(7));
            Assert.That(parameters[0].ParameterType, Is.EqualTo(typeof(ReadOnlySpan<byte>)));
            Assert.That(parameters[1].ParameterType, Is.EqualTo(typeof(ReadOnlySpan<byte>)));
            Assert.That(parameters[2].ParameterType, Is.EqualTo(typeof(ReadOnlySpan<byte>)));
            Assert.That(parameters[3].ParameterType, Is.EqualTo(typeof(ReadOnlySpan<byte>)));
            Assert.That(parameters[4].ParameterType, Is.EqualTo(typeof(Argon2Parameters)));
            Assert.That(parameters[5].ParameterType, Is.EqualTo(typeof(Argon2ExecutionOptions)));
            Assert.That(parameters[6].ParameterType, Is.EqualTo(typeof(int)));
        });
    }

    [Test]
    public void CodecExposesCanonicalEncodeDecodeAndLengthOperations()
    {
        string[] methodNames = GetSortedMethodNames(typeof(IArgon2EncodedHashCodec));

        Assert.That(
            methodNames,
            Is.EqualTo(new[]
            {
                "DecodeHash",
                "DecodeHashWithoutException",
                "EncodeHash",
                "GetEncodedHashLength"
            }));
    }

    [Test]
    public void PasswordHasherExposesDistinctByteTextAndContextOperations()
    {
        string[] methodNames = GetSortedMethodNames(typeof(IArgon2PasswordHasher));

        Assert.That(
            methodNames,
            Is.EqualTo(new[]
            {
                "GetHashingOptions",
                "HashPasswordBytes",
                "HashPasswordBytesWithContext",
                "HashPasswordText",
                "HashPasswordTextWithContext",
                "NeedsRehash",
                "VerifyPasswordBytes",
                "VerifyPasswordBytesWithContext",
                "VerifyPasswordText",
                "VerifyPasswordTextWithContext"
            }));
    }

    static MethodInfo GetRequiredMethod(Type interfaceType, string methodName)
    {
        MethodInfo? method = interfaceType.GetMethod(methodName);
        Assert.That(method, Is.Not.Null);
        return method!;
    }

    static string[] GetSortedMethodNames(Type interfaceType)
    {
        string[] names = interfaceType.GetMethods().Select(method => method.Name).ToArray();
        Array.Sort(names, StringComparer.Ordinal);
        return names;
    }
}
