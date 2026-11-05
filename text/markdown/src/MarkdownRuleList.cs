using System;
using System.Collections.Generic;

namespace markdown;

/// <summary>
/// Stores an ordered collection of named extension registrations.
/// </summary>
internal class MarkdownRuleList
{
    internal List<MarkdownRuleRegistration> Items;

    /// <summary>
    /// Initializes an empty ordered rule list.
    /// </summary>
    internal MarkdownRuleList()
    {
        Items = new List<MarkdownRuleRegistration>();
    }

    /// <summary>
    /// Returns count.
    /// </summary>
    /// <returns>The resulting count, index, or numeric value.</returns>
    internal int GetCount()
    {
        return Items.Count;
    }

    /// <summary>
    /// Reports whether at least one rule is registered.
    /// </summary>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool HasRules()
    {
        return Items.Count > 0;
    }

    /// <summary>
    /// Creates an independent copy of the list while retaining the registered rule objects.
    /// </summary>
    /// <returns>A new list with registrations in the same order.</returns>
    internal MarkdownRuleList Copy()
    {
        MarkdownRuleList copy = new MarkdownRuleList();
        int index = 0;

        while (index < Items.Count)
        {
            copy.Items.Add(Items[index].Copy());
            index = index + 1;
        }

        return copy;
    }

    /// <summary>
    /// Returns at.
    /// </summary>
    /// <param name="index">The zero-based position in the collection.</param>
    /// <returns>A detached copy of the registration at the requested position.</returns>
    internal MarkdownRuleRegistration GetAt(int index)
    {
        if (index < 0 || index >= Items.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        return Items[index].Copy();
    }

    /// <summary>
    /// Returns registrations.
    /// </summary>
    /// <returns>A new array containing the matching values in stable order.</returns>
    internal MarkdownRuleRegistration[] GetRegistrations()
    {
        MarkdownRuleRegistration[] registrations = new MarkdownRuleRegistration[Items.Count];
        int index = 0;

        while (index < Items.Count)
        {
            registrations[index] = Items[index].Copy();
            index = index + 1;
        }

        return registrations;
    }

    /// <summary>
    /// Returns the zero-based index of the registration with the supplied name.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <returns>The resulting count, index, or numeric value.</returns>
    internal int IndexOf(string name)
    {
        MarkdownRuleRegistration.ValidateName(name);

        int index = 0;
        while (index < Items.Count)
        {
            if (Items[index].Name == name)
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    /// <summary>
    /// Reports whether a registration with the supplied name exists.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool Contains(string name)
    {
        return IndexOf(name) >= 0;
    }

    /// <summary>
    /// Returns a detached copy of the registration with the supplied name, or <c>null</c> when absent.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <returns>A detached registration copy, or <c>null</c> when the name is absent.</returns>
    internal MarkdownRuleRegistration? Find(string name)
    {
        int index = IndexOf(name);
        if (index < 0)
        {
            return null;
        }

        return Items[index].Copy();
    }

    /// <summary>
    /// Finds the rule registered with the supplied name.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <returns>The registered rule, or <c>null</c> when the name is absent.</returns>
    internal object? FindRule(string name)
    {
        int index = IndexOf(name);
        if (index < 0)
        {
            return null;
        }

        return Items[index].Rule;
    }

    /// <summary>
    /// Appends a uniquely named rule registration.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <param name="rule">The caller-supplied rule object.</param>
    internal void Add(string name, object rule)
    {
        Insert(Items.Count, name, rule);
    }

    /// <summary>
    /// Inserts a uniquely named rule registration at the requested position.
    /// </summary>
    /// <param name="index">The zero-based position in the collection.</param>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <param name="rule">The caller-supplied rule object.</param>
    internal void Insert(int index, string name, object rule)
    {
        if (index < 0 || index > Items.Count)
        {
            throw new ArgumentOutOfRangeException("index");
        }

        if (IndexOf(name) >= 0)
        {
            throw new ArgumentException("Rule registration name already exists.");
        }

        Items.Insert(index, MarkdownRuleRegistration.Create(name, rule));
    }

    /// <summary>
    /// Inserts a uniquely named registration immediately before an existing registration.
    /// </summary>
    /// <param name="existingName">The name of the registration that will follow the inserted value.</param>
    /// <param name="name">The stable name used to identify the inserted value.</param>
    /// <param name="rule">The caller-supplied rule object.</param>
    internal void InsertBefore(string existingName, string name, object rule)
    {
        int index = IndexOf(existingName);
        if (index < 0)
        {
            throw new ArgumentException("Existing rule registration was not found.");
        }

        Insert(index, name, rule);
    }

    /// <summary>
    /// Inserts a uniquely named registration immediately after an existing registration.
    /// </summary>
    /// <param name="existingName">The name of the registration that will precede the inserted value.</param>
    /// <param name="name">The stable name used to identify the inserted value.</param>
    /// <param name="rule">The caller-supplied rule object.</param>
    internal void InsertAfter(string existingName, string name, object rule)
    {
        int index = IndexOf(existingName);
        if (index < 0)
        {
            throw new ArgumentException("Existing rule registration was not found.");
        }

        Insert(index + 1, name, rule);
    }

    /// <summary>
    /// Replaces an existing registration in place and permits the replacement to use a new unique name.
    /// </summary>
    /// <param name="existingName">The name of the registration to replace.</param>
    /// <param name="name">The stable name used to identify the replacement.</param>
    /// <param name="rule">The caller-supplied replacement object.</param>
    internal void Replace(string existingName, string name, object rule)
    {
        MarkdownRuleRegistration registration = MarkdownRuleRegistration.Create(name, rule);
        int existingIndex = IndexOf(existingName);
        if (existingIndex < 0)
        {
            throw new ArgumentException("Existing rule registration was not found.");
        }

        int replacementIndex = IndexOf(name);
        if (replacementIndex >= 0 && replacementIndex != existingIndex)
        {
            throw new ArgumentException("Rule registration name already exists.");
        }

        Items[existingIndex] = registration;
    }

    /// <summary>
    /// Adds a registration or replaces the existing registration with the same name without changing its position.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <param name="rule">The caller-supplied rule object.</param>
    internal void Set(string name, object rule)
    {
        MarkdownRuleRegistration registration = MarkdownRuleRegistration.Create(name, rule);
        int index = IndexOf(name);
        if (index < 0)
        {
            Items.Add(registration);
        }
        else
        {
            Items[index] = registration;
        }
    }

    /// <summary>
    /// Removes the registration with the supplied name.
    /// </summary>
    /// <param name="name">The stable name used to identify the value.</param>
    /// <returns><c>true</c> when the condition is met; otherwise, <c>false</c>.</returns>
    internal bool Remove(string name)
    {
        int index = IndexOf(name);
        if (index < 0)
        {
            return false;
        }

        Items.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Removes every registration.
    /// </summary>
    internal void Clear()
    {
        Items.Clear();
    }
}
