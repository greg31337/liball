using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides education generation operations.
/// </summary>
public class EducationProvider : IEducationProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal EducationProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("education");
    }

    /// <summary>
    /// Generates a fictional institution name.
    /// </summary>
    /// <returns>A fictional institution name.</returns>
    public string Institution()
    {
        return EducationGenerator.Institution(Context, RandomValue);
    }

    /// <summary>
    /// Generates a course name.
    /// </summary>
    /// <returns>A course name.</returns>
    public string Course()
    {
        return EducationGenerator.Course(Context, RandomValue);
    }

    /// <summary>
    /// Generates a subject name.
    /// </summary>
    /// <returns>A subject name.</returns>
    public string Subject()
    {
        return EducationGenerator.Subject(Context, RandomValue);
    }

    /// <summary>
    /// Generates a degree name.
    /// </summary>
    /// <returns>A degree name.</returns>
    public string Degree()
    {
        return EducationGenerator.Degree(Context, RandomValue);
    }

    /// <summary>
    /// Generates an academic term.
    /// </summary>
    /// <returns>An academic term.</returns>
    public string Term()
    {
        return EducationGenerator.Term(Context, RandomValue);
    }

    /// <summary>
    /// Generates a grade label.
    /// </summary>
    /// <returns>A grade label.</returns>
    public string Grade()
    {
        return EducationGenerator.Grade(Context, RandomValue);
    }

    /// <summary>
    /// Generates a synthetic student identifier that is not issued by an institution.
    /// </summary>
    /// <returns>A synthetic student identifier.</returns>
    public string StudentId()
    {
        return EducationGenerator.StudentId(Context, RandomValue);
    }

    /// <summary>
    /// Generates a correlated synthetic student profile.
    /// </summary>
    /// <returns>A synthetic student profile.</returns>
    public StudentProfile Record()
    {
        return EducationGenerator.Record(Context, RandomValue);
    }
}
