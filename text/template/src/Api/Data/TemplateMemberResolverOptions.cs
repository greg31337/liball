using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace template;

/// <summary>
/// Configures CLR member access, name matching, safe methods, dictionary precedence, and the associated security policy.
/// </summary>
public class TemplateMemberResolverOptions
{
    /// <summary>
    /// Controls whether member and safe-method names use ordinal case-sensitive comparison.
    /// </summary>
    public bool CaseSensitive;
    /// <summary>
    /// Allows readable public CLR fields to be exposed after the security policy approves their names.
    /// </summary>
    public bool AllowPublicFields;
    /// <summary>
    /// Allows readable, non-indexed public CLR properties to be exposed after security checks.
    /// </summary>
    public bool AllowPublicProperties;
    /// <summary>
    /// Allows parameterless public CLR methods only when their names also appear in <c>SafeMethodNames</c>.
    /// </summary>
    public bool AllowPublicMethods;
    /// <summary>
    /// Resolves dictionary keys before CLR members when a wrapped object supports both access paths.
    /// </summary>
    public bool DictionaryFirst;
    /// <summary>
    /// Stores the ordered member naming policies.
    /// </summary>
    public List<TemplateMemberNamingPolicy> MemberNamingPolicies;
    /// <summary>
    /// Stores ordered application-specific mappings from CLR or dictionary member names to template aliases.
    /// </summary>
    public List<ITemplateMemberNameMapper> MemberNameMappers;
    /// <summary>
    /// Stores the ordered safe method names.
    /// </summary>
    public List<string> SafeMethodNames;
    /// <summary>
    /// Specifies the security policy for this member resolver options.
    /// </summary>
    public TemplateSecurityPolicy SecurityPolicy;
    /// <summary>
    /// Specifies how values returned from dictionaries, collections, and CLR members are converted.
    /// </summary>
    public TemplateValueConversionPolicy ValueConversionPolicy;

    /// <summary>
    /// Initializes a new template member resolver options and validates any supplied values.
    /// </summary>
    public TemplateMemberResolverOptions()
    {
        CaseSensitive = true;
        AllowPublicFields = true;
        AllowPublicProperties = true;
        AllowPublicMethods = false;
        DictionaryFirst = true;
        MemberNamingPolicies = new List<TemplateMemberNamingPolicy>();
        MemberNamingPolicies.Add(TemplateMemberNamingPolicy.Exact);
        MemberNameMappers = new List<ITemplateMemberNameMapper>();
        SafeMethodNames = new List<string>();
        SecurityPolicy = new TemplateSecurityPolicy();
        ValueConversionPolicy = new TemplateValueConversionPolicy();
    }

    internal void Validate()
    {
        if (MemberNamingPolicies == null)
        {
            throw new ArgumentNullException("MemberNamingPolicies");
        }

        if (MemberNameMappers == null)
        {
            throw new ArgumentNullException("MemberNameMappers");
        }

        if (SafeMethodNames == null)
        {
            throw new ArgumentNullException("SafeMethodNames");
        }

        if (SecurityPolicy == null)
        {
            throw new ArgumentNullException("SecurityPolicy");
        }

        if (ValueConversionPolicy == null)
        {
            throw new ArgumentNullException("ValueConversionPolicy");
        }

        SecurityPolicy.Validate();
        ValueConversionPolicy.Validate();
    }

    /// <summary>
    /// Adds the supplied member naming policy after validating it.
    /// </summary>
    /// <param name="policy">The policy value to add or apply.</param>
    internal void AddMemberNamingPolicy(TemplateMemberNamingPolicy policy)
    {
        if (!AllowsMemberNamingPolicy(policy))
        {
            MemberNamingPolicies.Add(policy);
        }
    }

    /// <summary>
    /// Checks whether the requested naming transformation is enabled.
    /// </summary>
    /// <param name="policy">The policy value to add or apply.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    internal bool AllowsMemberNamingPolicy(TemplateMemberNamingPolicy policy)
    {
        if (MemberNamingPolicies == null)
        {
            return false;
        }

        int index = 0;
        while (index < MemberNamingPolicies.Count)
        {
            if (MemberNamingPolicies[index] == policy)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    /// <summary>
    /// Adds the supplied safe method after validating it.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal void AddSafeMethod(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (!ContainsSafeMethod(name))
        {
            SafeMethodNames.Add(name);
        }
    }

    /// <summary>
    /// Determines whether the safe method condition is satisfied.
    /// </summary>
    /// <param name="name">The logical name used for lookup.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal bool IsSafeMethod(string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException("name");
        }

        if (!AllowPublicMethods)
        {
            return false;
        }

        return ContainsSafeMethod(name);
    }

    /// <summary>
    /// Compares two names using ordinal matching and the configured case sensitivity.
    /// </summary>
    /// <param name="left">The left used by the operation.</param>
    /// <param name="right">The right used by the operation.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal bool NamesEqual(string left, string right)
    {
        if (left == null)
        {
            throw new ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new ArgumentNullException("right");
        }

        if (CaseSensitive)
        {
            return string.Equals(left, right, StringComparison.Ordinal);
        }

        return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Compares a CLR member name with a template name after applying each configured naming policy.
    /// </summary>
    /// <param name="memberName">The member name written in the template.</param>
    /// <param name="accessName">The access name used by the operation.</param>
    /// <returns><c>true</c> when the condition is satisfied; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when a required argument is <c>null</c>.</exception>
    internal bool MemberNamesEqual(string memberName, string accessName)
    {
        if (memberName == null)
        {
            throw new ArgumentNullException("memberName");
        }

        if (accessName == null)
        {
            throw new ArgumentNullException("accessName");
        }

        if (AllowsMemberNamingPolicy(TemplateMemberNamingPolicy.Exact) && NamesEqual(memberName, accessName))
        {
            return true;
        }

        if (AllowsMemberNamingPolicy(TemplateMemberNamingPolicy.CamelCase) && NamesEqual(ToCamelCase(memberName), accessName))
        {
            return true;
        }

        if (AllowsMemberNamingPolicy(TemplateMemberNamingPolicy.SnakeCase) && NamesEqual(ToSeparatedCase(memberName, '_'), accessName))
        {
            return true;
        }

        if (AllowsMemberNamingPolicy(TemplateMemberNamingPolicy.KebabCase) && NamesEqual(ToSeparatedCase(memberName, '-'), accessName))
        {
            return true;
        }

        if (MemberNameMappers != null)
        {
            int mapperIndex = 0;
            while (mapperIndex < MemberNameMappers.Count)
            {
                ITemplateMemberNameMapper? mapper = MemberNameMappers[mapperIndex];
                if (mapper != null)
                {
                    string? mappedName = mapper.MapName(memberName);
                    if (mappedName != null && NamesEqual(mappedName, accessName))
                    {
                        return true;
                    }
                }

                mapperIndex = mapperIndex + 1;
            }
        }

        return false;
    }

    internal bool ContainsSafeMethod(string name)
    {
        int index = 0;
        while (index < SafeMethodNames.Count)
        {
            if (NamesEqual(SafeMethodNames[index], name))
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal string ToCamelCase(string name)
    {
        if (name.Length == 0)
        {
            return name;
        }

        char[] characters = name.ToCharArray();
        int index = 0;
        while (index < characters.Length)
        {
            char current = characters[index];
            if (!char.IsUpper(current))
            {
                break;
            }

            if (index > 0 && index + 1 < characters.Length && char.IsLower(characters[index + 1]))
            {
                break;
            }

            characters[index] = char.ToLowerInvariant(current);
            index = index + 1;
        }

        return new string(characters);
    }

    internal string ToSeparatedCase(string name, char separator)
    {
        StringBuilder builder = new StringBuilder();
        int index = 0;
        while (index < name.Length)
        {
            char current = name[index];
            if (current == '_' || current == '-' || current == ' ')
            {
                AppendSeparator(builder, separator);
            }
            else if (char.IsUpper(current))
            {
                if (ShouldInsertSeparator(name, index))
                {
                    AppendSeparator(builder, separator);
                }

                builder.Append(char.ToLowerInvariant(current));
            }
            else
            {
                builder.Append(current);
            }

            index = index + 1;
        }

        return builder.ToString();
    }

    internal bool ShouldInsertSeparator(string name, int index)
    {
        if (index == 0)
        {
            return false;
        }

        char previous = name[index - 1];
        if (previous == '_' || previous == '-' || previous == ' ')
        {
            return false;
        }

        if (char.IsLower(previous) || char.IsDigit(previous))
        {
            return true;
        }

        if (index + 1 < name.Length && char.IsLower(name[index + 1]))
        {
            return true;
        }

        return false;
    }

    internal void AppendSeparator(StringBuilder builder, char separator)
    {
        if (builder.Length == 0)
        {
            return;
        }

        if (builder[builder.Length - 1] == separator)
        {
            return;
        }

        builder.Append(separator);
    }
}
