namespace fake;

/// <summary>
/// Represents a synthetic student profile assembled from education locale data.
/// The values are fictional and do not identify an enrolled student.
/// </summary>
public class StudentProfile
{
    /// <summary>
    /// Gets the fictional institution name.
    /// </summary>
    public string Institution = string.Empty;

    /// <summary>
    /// Gets the course name.
    /// </summary>
    public string Course = string.Empty;

    /// <summary>
    /// Gets the subject name associated with the course and degree.
    /// </summary>
    public string Subject = string.Empty;

    /// <summary>
    /// Gets the degree name associated with the subject.
    /// </summary>
    public string Degree = string.Empty;

    /// <summary>
    /// Gets the academic term.
    /// </summary>
    public string Term = string.Empty;

    /// <summary>
    /// Gets the grade label.
    /// </summary>
    public string Grade = string.Empty;

    /// <summary>
    /// Gets a synthetic student identifier.
    /// </summary>
    public string StudentId = string.Empty;

    internal StudentProfile(
        string institution,
        string course,
        string subject,
        string degree,
        string term,
        string grade,
        string studentId)
    {
        Institution = institution;
        Course = course;
        Subject = subject;
        Degree = degree;
        Term = term;
        Grade = grade;
        StudentId = studentId;
    }
}
