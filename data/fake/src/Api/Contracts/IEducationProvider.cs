using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="EducationProvider"/>.</summary>
public interface IEducationProvider
{
    /// <summary>
    /// Generates a fictional institution name.
    /// </summary>
    /// <returns>A fictional institution name.</returns>
    string Institution();
    /// <summary>
    /// Generates a course name.
    /// </summary>
    /// <returns>A course name.</returns>
    string Course();
    /// <summary>
    /// Generates a subject name.
    /// </summary>
    /// <returns>A subject name.</returns>
    string Subject();
    /// <summary>
    /// Generates a degree name.
    /// </summary>
    /// <returns>A degree name.</returns>
    string Degree();
    /// <summary>
    /// Generates an academic term.
    /// </summary>
    /// <returns>An academic term.</returns>
    string Term();
    /// <summary>
    /// Generates a grade label.
    /// </summary>
    /// <returns>A grade label.</returns>
    string Grade();
    /// <summary>
    /// Generates a synthetic student identifier that is not issued by an institution.
    /// </summary>
    /// <returns>A synthetic student identifier.</returns>
    string StudentId();
    /// <summary>
    /// Generates a correlated synthetic student profile.
    /// </summary>
    /// <returns>A synthetic student profile.</returns>
    StudentProfile Record();
}
