using System;

namespace fake;

/// <summary>
/// Contains a generated birth date and its completed age at the captured reference date.
/// </summary>
public class BirthDateAge
{
    /// <summary>
    /// The generated birth date.
    /// </summary>
    public DateOnly BirthDate;

    /// <summary>
    /// The completed age at the captured reference date.
    /// </summary>
    public int Age;

    internal BirthDateAge(DateOnly birthDate, int age)
    {
        BirthDate = birthDate;
        Age = age;
    }
}
