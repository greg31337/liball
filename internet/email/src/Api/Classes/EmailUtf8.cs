namespace email;
/// <summary>
/// Provides operations for working with UTF-8.
/// </summary>
public class EmailUtf8 : IEmailUtf8Contract
{
    internal static bool IsValidImplementation(byte[] value)
    {
        if (value == null)
            return false;
        int index = 0;
        while (index < value.Length)
        {
            int first = value[index];
            if (first < 128)
            {
                index++;
                continue;
            }

            int count;
            int minimum;
            int scalar;
            if (first >= 194 && first <= 223)
            {
                count = 1;
                minimum = 128;
                scalar = first & 31;
            }
            else if (first >= 224 && first <= 239)
            {
                count = 2;
                minimum = 2048;
                scalar = first & 15;
            }
            else if (first >= 240 && first <= 244)
            {
                count = 3;
                minimum = 65536;
                scalar = first & 7;
            }
            else
                return false;
            if (index + count >= value.Length)
                return false;
            int continuation = 0;
            while (continuation < count)
            {
                int next = value[index + continuation + 1];
                if ((next & 192) != 128)
                    return false;
                scalar = (scalar << 6) | (next & 63);
                continuation++;
            }

            if (scalar < minimum || scalar > 1114111 || (scalar >= 55296 && scalar <= 57343))
                return false;
            index += count + 1;
        }

        return true;
    }

    /// <summary>
    /// Determines whether the current value or state is valid.
    /// </summary>
    /// <param name = "value">The value to store or process.</param>
    /// <returns><c>true</c> when the value or state is valid; otherwise, <c>false</c>.</returns>
    public bool IsValid(byte[] value)
    {
        return EmailUtf8.IsValidImplementation(value);
    }
}
