namespace random.Tests;

public class SecurePasswordTests
{
    [Test]
    public void PolicyConstructorsCopyArraysAndEnableSecureDefaults()
    {
        string[] requiredAlphabets = ["abc", "ABC"];
        int[] requiredCounts = [2, 1];
        PasswordPolicy policy = new PasswordPolicy(12, "abcABC012!@", requiredAlphabets, requiredCounts, true, true);
        requiredAlphabets[0] = "xyz";
        requiredCounts[0] = 9;

        Assert.Multiple(() =>
        {
            Assert.That(policy.Length, Is.EqualTo(12));
            Assert.That(policy.Alphabet, Is.EqualTo("abcABC012!@"));
            Assert.That(policy.RequiredAlphabets, Is.EqualTo(new[] { "abc", "ABC" }));
            Assert.That(policy.RequiredCounts, Is.EqualTo(new[] { 2, 1 }));
            Assert.That(policy.AllowRepeatedCharacters, Is.True);
            Assert.That(policy.Shuffle, Is.True);
        });
    }

    [Test]
    public void RepetitionPolicyMatchesIndependentRequiredFillAndShuffle()
    {
        PasswordPolicy policy = new PasswordPolicy(18, "abcABC012!@", ["abc", "ABC", "012", "!@"], [2, 2, 2, 1], true, true);
        SecureRandomGenerator actual = Create();
        SecureRandomGenerator reference = Create();

        string password = actual.Password(policy);
        string expected = ReferencePasswordWithRepetition(reference, policy);

        Assert.Multiple(() =>
        {
            Assert.That(password, Is.EqualTo(expected));
            Assert.That(password, Has.Length.EqualTo(18));
            Assert.That(password.Count(character => "abc".Contains(character)), Is.GreaterThanOrEqualTo(2));
            Assert.That(password.Count(character => "ABC".Contains(character)), Is.GreaterThanOrEqualTo(2));
            Assert.That(password.Count(character => "012".Contains(character)), Is.GreaterThanOrEqualTo(2));
            Assert.That(password.Count(character => "!@".Contains(character)), Is.GreaterThanOrEqualTo(1));
            Assert.That(password.All(ReflectionInfo.Required<string>(policy.Alphabet).Contains), Is.True);
        });
    }

    [Test]
    public void ShufflePolicyControlsOnlyTheFinalUnbiasedShuffle()
    {
        PasswordPolicy shuffledPolicy = new PasswordPolicy(12, "abcABC012", ["abc", "ABC"], [2, 2], true, true);
        PasswordPolicy orderedPolicy = new PasswordPolicy(12, "abcABC012", ["abc", "ABC"], [2, 2], true, false);
        SecureRandomGenerator shuffled = Create();
        SecureRandomGenerator ordered = Create();
        SecureRandomGenerator shuffledReference = Create();
        SecureRandomGenerator orderedReference = Create();

        string shuffledPassword = shuffled.Password(shuffledPolicy);
        string orderedPassword = ordered.Password(orderedPolicy);

        Assert.Multiple(() =>
        {
            Assert.That(shuffledPassword, Is.EqualTo(ReferencePasswordWithRepetition(shuffledReference, shuffledPolicy)));
            Assert.That(orderedPassword, Is.EqualTo(ReferencePasswordWithRepetition(orderedReference, orderedPolicy)));
            Assert.That(orderedPassword.Substring(0, 2).All("abc".Contains), Is.True);
            Assert.That(orderedPassword.Substring(2, 2).All("ABC".Contains), Is.True);
            Assert.That(shuffledPassword, Is.Not.EqualTo(orderedPassword));
        });
    }

    [Test]
    public void NoRepetitionUsesDistinctCharactersAndSatisfiesOverlappingGroups()
    {
        PasswordPolicy policy = new PasswordPolicy(3, "ABC", ["AB", "BC"], [2, 1], false, true);
        SecureRandomGenerator generator = Create();

        string password = generator.Password(policy);

        Assert.Multiple(() =>
        {
            Assert.That(password, Has.Length.EqualTo(3));
            Assert.That(password.Distinct().Count(), Is.EqualTo(3));
            Assert.That(password.Count("AB".Contains), Is.GreaterThanOrEqualTo(2));
            Assert.That(password.Count("BC".Contains), Is.GreaterThanOrEqualTo(1));
            Assert.That(password.Order().ToArray(), Is.EqualTo("ABC".Order().ToArray()));
        });
    }

    [Test]
    public void InvalidPoliciesAreRejectedBeforeEntropyConsumption()
    {
        SecureRandomGenerator actual = Create();
        SecureRandomGenerator reference = Create();
        object[] cases =
        [
            new object[] { (TestDelegate)(() => actual.Password(new PasswordPolicy(0, "AB", [], [], true, true))), typeof(RandomSecurityException) },
            new object[] { (TestDelegate)(() => actual.Password(new PasswordPolicy(8, null, [], [], true, true))), typeof(RandomSecurityException) },
            new object[] { (TestDelegate)(() => actual.Password(new PasswordPolicy(8, "A", [], [], true, true))), typeof(RandomSecurityException) },
            new object[] { (TestDelegate)(() => actual.Password(new PasswordPolicy(8, "AAB", [], [], true, true))), typeof(RandomSecurityException) },
            new object[] { (TestDelegate)(() => actual.Password(new PasswordPolicy(8, "A\uD800B", [], [], true, true))), typeof(RandomSecurityException) },
            new object[] { (TestDelegate)(() => actual.Password(new PasswordPolicy(8, "AB", null, [], true, true))), typeof(RandomSecurityException) },
            new object[] { (TestDelegate)(() => actual.Password(new PasswordPolicy(8, "AB", [], null, true, true))), typeof(RandomSecurityException) },
            new object[] { (TestDelegate)(() => actual.Password(new PasswordPolicy(8, "AB", ["A"], [], true, true))), typeof(RandomSecurityException) },
            new object[] { (TestDelegate)(() => actual.Password(new PasswordPolicy(8, "AB", ["A"], [-1], true, true))), typeof(RandomSecurityException) },
            new object[] { (TestDelegate)(() => actual.Password(new PasswordPolicy(2, "AB", ["A"], [3], true, true))), typeof(RandomSecurityException) },
            new object[] { (TestDelegate)(() => actual.Password(new PasswordPolicy(8, "AB", [""], [1], true, true))), typeof(RandomSecurityException) },
            new object[] { (TestDelegate)(() => actual.Password(new PasswordPolicy(8, "AB", ["AA"], [1], true, true))), typeof(RandomSecurityException) },
            new object[] { (TestDelegate)(() => actual.Password(new PasswordPolicy(8, "AB", ["C"], [1], true, true))), typeof(RandomSecurityException) },
            new object[] { (TestDelegate)(() => actual.Password(new PasswordPolicy(3, "AB", [], [], false, true))), typeof(RandomSecurityException) },
            new object[] { (TestDelegate)(() => actual.Password(new PasswordPolicy(4, "ABCD", ["AB", "CD"], [3, 1], false, true))), typeof(RandomSecurityException) }
        ];

        RandomArgumentException nullError = Assert.Throws<RandomArgumentException>(() => actual.Password(NullReference.Value<global::random.PasswordPolicy>()));
        Assert.That(nullError.ArgumentName(), Is.EqualTo("policy"));
        foreach (object[] testCase in cases)
        {
            Assert.Throws((Type)testCase[1], (TestDelegate)testCase[0]);
        }

        Assert.That(actual.NextUInt64(), Is.EqualTo(reference.NextUInt64()));
    }

    [Test]
    public void PasswordGenerationHonorsTerminalHmacState()
    {
        SecureRandomGenerator generator = Create();
        PasswordPolicy policy = new PasswordPolicy(8, "AB", [], [], true, true);
        generator.Uninstantiate();

        Assert.Throws<RandomSecurityException>(() => generator.Password(policy));
    }

    static string ReferencePasswordWithRepetition(SecureRandomGenerator generator, PasswordPolicy policy)
    {
        string[] requiredAlphabets = ReflectionInfo.Required<string[]>(policy.RequiredAlphabets);
        int[] requiredCounts = ReflectionInfo.Required<int[]>(policy.RequiredCounts);
        string policyAlphabet = ReflectionInfo.Required<string>(policy.Alphabet);
        char[] result = new char[policy.Length];
        int outputIndex = 0;
        for (int groupIndex = 0; groupIndex < requiredAlphabets.Length; groupIndex++)
        {
            for (int count = 0; count < requiredCounts[groupIndex]; count++)
            {
                string alphabet = requiredAlphabets[groupIndex];
                result[outputIndex] = alphabet[(int)generator.NextUInt32Bounded((uint)alphabet.Length)];
                outputIndex++;
            }
        }

        while (outputIndex < result.Length)
        {
            result[outputIndex] = policyAlphabet[(int)generator.NextUInt32Bounded((uint)policyAlphabet.Length)];
            outputIndex++;
        }

        if (policy.Shuffle)
        {
            for (int index = result.Length - 1; index > 0; index--)
            {
                int selected = (int)generator.NextUInt32Bounded((uint)(index + 1));
                (result[index], result[selected]) = (result[selected], result[index]);
            }
        }

        return new string(result);
    }

    static SecureRandomGenerator Create()
    {
        return SecureRandomGenerator.CreateHmacDrbg(Sequence(0x00, 32), Sequence(0x20, 16), Sequence(0x30, 11));
    }

    static byte[] Sequence(int start, int length)
    {
        byte[] result = new byte[length];
        for (int index = 0; index < length; index++)
        {
            result[index] = (byte)(start + index);
        }

        return result;
    }
}
