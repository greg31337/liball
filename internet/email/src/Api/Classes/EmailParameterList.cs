using System;
using System.Collections.Generic;

namespace email;
/// <summary>
/// Represents a parameter list.
/// </summary>
public class EmailParameterList : IEmailParameterListContract
{
    List<EmailParameter> parameters;
    List<EmailRfc2231DecodedParameter> decodedParameters;
    /// <summary>
    /// Creates a new parameter list.
    /// </summary>
    /// <param name = "parameters">The parameters used by the operation.</param>
    /// <param name = "decodedParameters">The decoded parameters used by the operation.</param>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailParameterList(List<EmailParameter> parameters, List<EmailRfc2231DecodedParameter> decodedParameters)
    {
        if (parameters == null)
        {
            throw new ArgumentNullException("parameters");
        }

        if (decodedParameters == null)
        {
            throw new ArgumentNullException("decodedParameters");
        }

        this.parameters = new List<EmailParameter>(parameters);
        this.decodedParameters = new List<EmailRfc2231DecodedParameter>(decodedParameters);
    }

    /// <summary>
    /// Gets the count.
    /// </summary>
    /// <returns>The current count.</returns>
    public int GetCount()
    {
        return parameters.Count;
    }

    /// <summary>
    /// Gets the value at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested value.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailParameter GetAt(int index)
    {
        if (index < 0 || index >= parameters.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return parameters[index];
    }

    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <returns>The current all.</returns>
    public List<EmailParameter> GetAll()
    {
        return new List<EmailParameter>(parameters);
    }

    /// <summary>
    /// Gets the first.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current first.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailParameter? GetFirst(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        int index = 0;
        while (index < parameters.Count)
        {
            if (string.Equals(parameters[index].GetName(), name, StringComparison.OrdinalIgnoreCase))
            {
                return parameters[index];
            }

            index++;
        }

        return null;
    }

    /// <summary>
    /// Gets the all.
    /// </summary>
    /// <remarks>
    /// Collection and array results should be treated as snapshots unless the owning type explicitly documents shared mutable state.
    /// </remarks>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current all.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public List<EmailParameter> GetAllNamed(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        List<EmailParameter> matches = new List<EmailParameter>();
        int index = 0;
        while (index < parameters.Count)
        {
            if (string.Equals(parameters[index].GetName(), name, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(parameters[index]);
            }

            index++;
        }

        return matches;
    }

    /// <summary>
    /// Determines whether the duplicate is present or available.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns><c>true</c> when the duplicate is available; otherwise, <c>false</c>.</returns>
    public bool HasDuplicate(string name)
    {
        return GetAllNamed(name).Count > 1;
    }

    /// <summary>
    /// Gets the decoded count.
    /// </summary>
    /// <returns>The current decoded count.</returns>
    public int GetDecodedCount()
    {
        return decodedParameters.Count;
    }

    /// <summary>
    /// Gets the decoded at a zero-based index.
    /// </summary>
    /// <param name = "index">The zero-based index of the requested item.</param>
    /// <returns>The requested decoded.</returns>
    /// <exception cref = "ArgumentOutOfRangeException">Thrown when a numeric, index, or enum argument is outside the supported range.</exception>
    public EmailRfc2231DecodedParameter GetDecodedAt(int index)
    {
        if (index < 0 || index >= decodedParameters.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return decodedParameters[index];
    }

    /// <summary>
    /// Gets the decoded.
    /// </summary>
    /// <param name = "name">The name used by the operation.</param>
    /// <returns>The current decoded.</returns>
    /// <exception cref = "ArgumentNullException">Thrown when a required reference argument is null.</exception>
    public EmailRfc2231DecodedParameter? GetDecoded(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        int index = 0;
        while (index < decodedParameters.Count)
        {
            if (string.Equals(decodedParameters[index].GetName(), name, StringComparison.OrdinalIgnoreCase))
            {
                return decodedParameters[index];
            }

            index++;
        }

        return null;
    }
}
