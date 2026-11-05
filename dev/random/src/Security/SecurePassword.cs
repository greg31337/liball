using System;
using System.Collections.Generic;

namespace random;

internal static class SecurePassword
{
    internal static string Generate(SecureRandomGenerator generator, PasswordPolicy? policy)
    {
        Guard.NotNull(generator, "generator");
        PasswordPolicySnapshot snapshot = Validate(policy);
        generator.FillBytes(Span<byte>.Empty);
        char[] characters = new char[snapshot.Length];
        if (snapshot.AllowRepeatedCharacters)
        {
            FillWithRepetition(generator, snapshot, characters);
        }
        else
        {
            FillWithoutRepetition(generator, snapshot, characters);
        }

        if (snapshot.Shuffle)
        {
            ShuffleCharacters(generator, characters);
        }

        string password = new string(characters);
        Array.Fill(characters, '\0');
        return password;
    }

    internal static PasswordPolicySnapshot Validate(PasswordPolicy? policy)
    {
        if (policy == null)
        {
            throw RandomArgumentException.Create("policy", "The value must not be null.");
        }
        if (policy.Length <= 0)
        {
            throw RandomSecurityException.Create("A password length must be positive.");
        }

        string alphabet = ValidateAlphabet(policy.Alphabet, true, null);
        if (policy.RequiredAlphabets == null)
        {
            throw RandomSecurityException.Create("Required password alphabets must not be null.");
        }

        if (policy.RequiredCounts == null)
        {
            throw RandomSecurityException.Create("Required password counts must not be null.");
        }

        if (policy.RequiredAlphabets.Length != policy.RequiredCounts.Length)
        {
            throw RandomSecurityException.Create("Required password alphabets and counts must have the same length.");
        }

        string[] requiredAlphabets = (string[])policy.RequiredAlphabets.Clone();
        int[] requiredCounts = (int[])policy.RequiredCounts.Clone();
        long totalRequired = 0L;
        int index = 0;
        while (index < requiredAlphabets.Length)
        {
            ValidateAlphabet(requiredAlphabets[index], false, alphabet);
            if (requiredCounts[index] < 0)
            {
                throw RandomSecurityException.Create("Required password counts must not be negative.");
            }

            totalRequired += requiredCounts[index];
            if (totalRequired > policy.Length)
            {
                throw RandomSecurityException.Create("Required password counts exceed the password length.");
            }

            index++;
        }

        PasswordPolicySnapshot snapshot = new PasswordPolicySnapshot(
            policy.Length,
            alphabet,
            requiredAlphabets,
            requiredCounts,
            (int)totalRequired,
            policy.AllowRepeatedCharacters,
            policy.Shuffle);
        if (!snapshot.AllowRepeatedCharacters)
        {
            if (snapshot.Alphabet.Length < snapshot.Length)
            {
                throw RandomSecurityException.Create("The alphabet is too small for a password without repeated characters.");
            }

            int[] naturalOrder = NaturalOrder(snapshot.Alphabet.Length);
            if (!CanMatchRequired(snapshot, naturalOrder))
            {
                throw RandomSecurityException.Create("The required groups cannot be satisfied without repeated characters.");
            }
        }

        return snapshot;
    }

    internal static string ValidateAlphabet(string? alphabet, bool requireTwoCharacters, string? allowedAlphabet)
    {
        if (alphabet == null)
        {
            throw RandomSecurityException.Create("A password alphabet must not be null.");
        }

        int minimumLength = requireTwoCharacters ? 2 : 1;
        if (alphabet.Length < minimumLength)
        {
            throw RandomSecurityException.Create("A password alphabet contains too few characters.");
        }

        HashSet<char> characters = new HashSet<char>();
        int index = 0;
        while (index < alphabet.Length)
        {
            char character = alphabet[index];
            if (char.IsSurrogate(character))
            {
                throw RandomSecurityException.Create("Password alphabets must not contain UTF-16 surrogate code units.");
            }

            if (!characters.Add(character))
            {
                throw RandomSecurityException.Create("Password alphabets must not contain duplicate characters.");
            }

            if (allowedAlphabet != null && allowedAlphabet.IndexOf(character) < 0)
            {
                throw RandomSecurityException.Create("Every required character must belong to the full password alphabet.");
            }

            index++;
        }

        return alphabet;
    }

    internal static void FillWithRepetition(SecureRandomGenerator generator, PasswordPolicySnapshot policy, char[] destination)
    {
        int outputIndex = 0;
        int groupIndex = 0;
        while (groupIndex < policy.RequiredAlphabets.Length)
        {
            int count = 0;
            while (count < policy.RequiredCounts[groupIndex])
            {
                string requiredAlphabet = policy.RequiredAlphabets[groupIndex];
                uint selected = generator.NextUInt32Bounded((uint)requiredAlphabet.Length);
                destination[outputIndex] = requiredAlphabet[(int)selected];
                outputIndex++;
                count++;
            }

            groupIndex++;
        }

        while (outputIndex < destination.Length)
        {
            uint selected = generator.NextUInt32Bounded((uint)policy.Alphabet.Length);
            destination[outputIndex] = policy.Alphabet[(int)selected];
            outputIndex++;
        }
    }

    internal static void FillWithoutRepetition(SecureRandomGenerator generator, PasswordPolicySnapshot policy, char[] destination)
    {
        int[] candidateOrder = NaturalOrder(policy.Alphabet.Length);
        ShuffleIndices(generator, candidateOrder);
        int[] matchedGroupByCharacter = MatchRequired(policy, candidateOrder);
        bool[] used = new bool[policy.Alphabet.Length];
        int outputIndex = 0;
        int orderIndex = 0;
        while (orderIndex < candidateOrder.Length)
        {
            int characterIndex = candidateOrder[orderIndex];
            if (matchedGroupByCharacter[characterIndex] >= 0)
            {
                destination[outputIndex] = policy.Alphabet[characterIndex];
                used[characterIndex] = true;
                outputIndex++;
            }

            orderIndex++;
        }

        orderIndex = 0;
        while (outputIndex < destination.Length)
        {
            int characterIndex = candidateOrder[orderIndex];
            if (!used[characterIndex])
            {
                destination[outputIndex] = policy.Alphabet[characterIndex];
                used[characterIndex] = true;
                outputIndex++;
            }

            orderIndex++;
        }
    }

    internal static bool CanMatchRequired(PasswordPolicySnapshot policy, int[] candidateOrder)
    {
        int[] matched = MatchRequiredOrEmpty(policy, candidateOrder);
        return matched.Length != 0 || policy.TotalRequired == 0;
    }

    internal static int[] MatchRequired(PasswordPolicySnapshot policy, int[] candidateOrder)
    {
        int[] matched = MatchRequiredOrEmpty(policy, candidateOrder);
        if (matched.Length == 0 && policy.TotalRequired != 0)
        {
            throw RandomSecurityException.Create("The required groups cannot be satisfied without repeated characters.");
        }

        return matched;
    }

    internal static int[] MatchRequiredOrEmpty(PasswordPolicySnapshot policy, int[] candidateOrder)
    {
        int[] matchedGroupByCharacter = new int[policy.Alphabet.Length];
        Array.Fill(matchedGroupByCharacter, -1);
        int groupIndex = 0;
        while (groupIndex < policy.RequiredCounts.Length)
        {
            int count = 0;
            while (count < policy.RequiredCounts[groupIndex])
            {
                bool[] visited = new bool[policy.Alphabet.Length];
                if (!TryMatch(groupIndex, policy, candidateOrder, matchedGroupByCharacter, visited))
                {
                    return new int[0];
                }

                count++;
            }

            groupIndex++;
        }

        return matchedGroupByCharacter;
    }

    internal static bool TryMatch(int groupIndex, PasswordPolicySnapshot policy, int[] candidateOrder, int[] matchedGroupByCharacter, bool[] visited)
    {
        int orderIndex = 0;
        while (orderIndex < candidateOrder.Length)
        {
            int characterIndex = candidateOrder[orderIndex];
            char character = policy.Alphabet[characterIndex];
            if (!visited[characterIndex] && policy.RequiredAlphabets[groupIndex].IndexOf(character) >= 0)
            {
                visited[characterIndex] = true;
                int previousGroup = matchedGroupByCharacter[characterIndex];
                if (previousGroup < 0 || TryMatch(previousGroup, policy, candidateOrder, matchedGroupByCharacter, visited))
                {
                    matchedGroupByCharacter[characterIndex] = groupIndex;
                    return true;
                }
            }

            orderIndex++;
        }

        return false;
    }

    internal static int[] NaturalOrder(int length)
    {
        int[] result = new int[length];
        int index = 0;
        while (index < result.Length)
        {
            result[index] = index;
            index++;
        }

        return result;
    }

    internal static void ShuffleIndices(SecureRandomGenerator generator, int[] values)
    {
        int index = values.Length - 1;
        while (index > 0)
        {
            int selected = (int)generator.NextUInt32Bounded((uint)(index + 1));
            int temporary = values[index];
            values[index] = values[selected];
            values[selected] = temporary;
            index--;
        }
    }

    internal static void ShuffleCharacters(SecureRandomGenerator generator, char[] values)
    {
        int index = values.Length - 1;
        while (index > 0)
        {
            int selected = (int)generator.NextUInt32Bounded((uint)(index + 1));
            char temporary = values[index];
            values[index] = values[selected];
            values[selected] = temporary;
            index--;
        }
    }
}

internal class PasswordPolicySnapshot
{
    internal int Length;
    internal string Alphabet;
    internal string[] RequiredAlphabets;
    internal int[] RequiredCounts;
    internal int TotalRequired;
    internal bool AllowRepeatedCharacters;
    internal bool Shuffle;

    internal PasswordPolicySnapshot(
        int length,
        string alphabet,
        string[] requiredAlphabets,
        int[] requiredCounts,
        int totalRequired,
        bool allowRepeatedCharacters,
        bool shuffle)
    {
        Length = length;
        Alphabet = alphabet;
        RequiredAlphabets = (string[])requiredAlphabets.Clone();
        RequiredCounts = (int[])requiredCounts.Clone();
        TotalRequired = totalRequired;
        AllowRepeatedCharacters = allowRepeatedCharacters;
        Shuffle = shuffle;
    }
}
