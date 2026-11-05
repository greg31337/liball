using System;
using System.Collections;
using System.Reflection;
using System.Threading;

namespace fake;
/// <summary>Populates caller objects from deterministic ordered composition rules.</summary>
/// <remarks>Only public instance fields and non-indexed public properties are considered. Values are assigned without implicit type conversion.</remarks>
public class ObjectFiller : IObjectFiller
{
    Fake FakeValue;
    /// <summary>Initializes an object filler using the supplied fake-data context.</summary>
    /// <param name="value">The fake-data instance passed to generated rules.</param>
    /// <exception cref="ArgumentNullException">Thrown when <c>value</c> is null.</exception>
    public ObjectFiller(Fake value)
    {
        if (value == null)
            throw new ArgumentNullException("value");
        FakeValue = value;
    }

    /// <summary>Applies ordered rules to an existing caller object.</summary>
    /// <param name="target">The object whose public fields or properties are populated.</param>
    /// <param name="rules">The ordered rule set to apply.</param>
    /// <returns>The same target object after population.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="FakeException">Thrown for strict missing or read-only paths, unsupported nested construction, or incompatible values.</exception>
    public object Populate(object target, FakeRuleSet rules)
    {
        if (target == null)
            throw new ArgumentNullException("target");
        if (rules == null)
            throw new ArgumentNullException("rules");
        FakeRule[] ordered = rules.Rules();
        int index = 0;
        while (index < ordered.Length)
        {
            FakeRule rule = ordered[index];
            FakeRuleSet.ValidateMemberPath(rule.MemberPath, "memberPath");
            ObjectMemberPath path = Resolve(target, rule.MemberPath, rules.IsStrict());
            if (path.Found && rule.Kind != FakeRuleKind.Skipped)
            {
                ObjectMember? member = path.Member;
                object? memberTarget = path.Target;
                if (member == null || memberTarget == null)
                    throw FakeException.Create("A resolved member path has no target member.");
                object? value = rule.Evaluate(FakeValue, target);
                RequireAssignable(rule.MemberPath, member.ValueType, value);
                member.Write(memberTarget, value);
            }

            index = index + 1;
        }

        return target;
    }

    /// <summary>Creates and populates one caller object.</summary>
    /// <param name="factory">The callback that creates the target object.</param>
    /// <param name="rules">The ordered rule set to apply.</param>
    /// <returns>The created and populated object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="FakeException">Thrown when the factory returns null or population fails.</exception>
    public object Create(Func<object> factory, FakeRuleSet rules)
    {
        return CreateFromFactoryAndRulesAndCancellationToken(factory, rules, CancellationToken.None);
    }

    /// <summary>Creates and populates one caller object with cancellation.</summary>
    /// <param name="factory">The callback that creates the target object.</param>
    /// <param name="rules">The ordered rule set to apply.</param>
    /// <param name="cancellationToken">The token checked before invoking the factory.</param>
    /// <returns>The created and populated object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <exception cref="FakeException">Thrown when the factory returns null or population fails.</exception>
    public object CreateFromFactoryAndRulesAndCancellationToken(Func<object> factory, FakeRuleSet rules, CancellationToken cancellationToken)
    {
        RequireFactory(factory, rules);
        cancellationToken.ThrowIfCancellationRequested();
        object target = factory();
        if (target == null)
            throw FakeException.Create("The object factory returned null.");
        cancellationToken.ThrowIfCancellationRequested();
        return Populate(target, rules);
    }

    /// <summary>Creates and populates one caller object using a fake-aware factory.</summary>
    /// <param name="factory">The callback that receives the active <see cref="Fake"/> instance and creates the target.</param>
    /// <param name="rules">The ordered rule set to apply.</param>
    /// <returns>The created and populated object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="FakeException">Thrown when the factory returns null or population fails.</exception>
    public object CreateFromFactoryAndRules(Func<Fake, object> factory, FakeRuleSet rules)
    {
        return CreateFromFactoryAndRulesAndCancellationTokenUsingFuncOfFakeAndObjectAndFakeRuleSetAndCancellationToken(factory, rules, CancellationToken.None);
    }

    /// <summary>Creates and populates one caller object using a fake-aware factory and cancellation.</summary>
    /// <param name="factory">The callback that receives the active <see cref="Fake"/> instance and creates the target.</param>
    /// <param name="rules">The ordered rule set to apply.</param>
    /// <param name="cancellationToken">The token checked before invoking the factory.</param>
    /// <returns>The created and populated object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <exception cref="FakeException">Thrown when the factory returns null or population fails.</exception>
    public object CreateFromFactoryAndRulesAndCancellationTokenUsingFuncOfFakeAndObjectAndFakeRuleSetAndCancellationToken(Func<Fake, object> factory, FakeRuleSet rules, CancellationToken cancellationToken)
    {
        if (factory == null)
            throw new ArgumentNullException("factory");
        if (rules == null)
            throw new ArgumentNullException("rules");
        cancellationToken.ThrowIfCancellationRequested();
        object target = factory(FakeValue);
        if (target == null)
            throw FakeException.Create("The object factory returned null.");
        cancellationToken.ThrowIfCancellationRequested();
        return Populate(target, rules);
    }

    /// <summary>Creates and populates a bounded array of caller objects.</summary>
    /// <param name="factory">The callback invoked once for each target.</param>
    /// <param name="rules">The ordered rule set applied to every target.</param>
    /// <param name="count">The number of objects to create.</param>
    /// <returns>A new object array in callback invocation order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> exceeds configured limits.</exception>
    /// <exception cref="FakeException">Thrown when a factory invocation returns null or population fails.</exception>
    public object[] Many(Func<object> factory, FakeRuleSet rules, int count)
    {
        return ManyFromFactoryAndRulesAndCountAndCancellationToken(factory, rules, count, CancellationToken.None);
    }

    /// <summary>Creates and populates a bounded array of caller objects with cancellation.</summary>
    /// <param name="factory">The callback invoked once for each target.</param>
    /// <param name="rules">The ordered rule set applied to every target.</param>
    /// <param name="count">The number of objects to create.</param>
    /// <param name="cancellationToken">The token checked before allocation and each factory invocation.</param>
    /// <returns>A new object array in callback invocation order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> exceeds configured limits.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <exception cref="FakeException">Thrown when a factory invocation returns null or population fails.</exception>
    public object[] ManyFromFactoryAndRulesAndCountAndCancellationToken(Func<object> factory, FakeRuleSet rules, int count, CancellationToken cancellationToken)
    {
        RequireFactory(factory, rules);
        RequireCount(count, "count");
        cancellationToken.ThrowIfCancellationRequested();
        object[] result = new object[count];
        int index = 0;
        while (index < result.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result[index] = CreateFromFactoryAndRulesAndCancellationToken(factory, rules, cancellationToken);
            index = index + 1;
        }

        return result;
    }

    /// <summary>Creates and populates a bounded array using a fake-aware factory.</summary>
    /// <param name="factory">The callback that receives the active <see cref="Fake"/> instance for each target.</param>
    /// <param name="rules">The ordered rule set applied to every target.</param>
    /// <param name="count">The number of objects to create.</param>
    /// <returns>A new object array in callback invocation order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> exceeds configured limits.</exception>
    /// <exception cref="FakeException">Thrown when a factory invocation returns null or population fails.</exception>
    public object[] ManyFromFactoryAndRulesAndCount(Func<Fake, object> factory, FakeRuleSet rules, int count)
    {
        return ManyFromFactoryAndRulesAndCountAndCancellationTokenUsingFuncOfFakeAndObjectAndFakeRuleSetAndInt32AndCancellationToken(factory, rules, count, CancellationToken.None);
    }

    /// <summary>Creates and populates a bounded array using a fake-aware factory and cancellation.</summary>
    /// <param name="factory">The callback that receives the active <see cref="Fake"/> instance for each target.</param>
    /// <param name="rules">The ordered rule set applied to every target.</param>
    /// <param name="count">The number of objects to create.</param>
    /// <param name="cancellationToken">The token checked before allocation and each factory invocation.</param>
    /// <returns>A new object array in callback invocation order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> exceeds configured limits.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <exception cref="FakeException">Thrown when a factory invocation returns null or population fails.</exception>
    public object[] ManyFromFactoryAndRulesAndCountAndCancellationTokenUsingFuncOfFakeAndObjectAndFakeRuleSetAndInt32AndCancellationToken(Func<Fake, object> factory, FakeRuleSet rules, int count, CancellationToken cancellationToken)
    {
        if (factory == null)
            throw new ArgumentNullException("factory");
        if (rules == null)
            throw new ArgumentNullException("rules");
        RequireCount(count, "count");
        cancellationToken.ThrowIfCancellationRequested();
        object[] result = new object[count];
        int index = 0;
        while (index < result.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            result[index] = CreateFromFactoryAndRulesAndCancellationTokenUsingFuncOfFakeAndObjectAndFakeRuleSetAndCancellationToken(factory, rules, cancellationToken);
            index = index + 1;
        }

        return result;
    }

    /// <summary>Populates every existing object in a one-dimensional array.</summary>
    /// <param name="values">The array of non-null caller objects to populate.</param>
    /// <param name="rules">The ordered rule set applied to every object.</param>
    /// <returns>The same array after population.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the array is multidimensional or contains null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the array length exceeds configured limits.</exception>
    public Array Fill(Array values, FakeRuleSet rules)
    {
        return FillFromValuesAndRulesAndCancellationToken(values, rules, CancellationToken.None);
    }

    /// <summary>Populates every existing object in a one-dimensional array with cancellation.</summary>
    /// <param name="values">The array of non-null caller objects to populate.</param>
    /// <param name="rules">The ordered rule set applied to every object.</param>
    /// <param name="cancellationToken">The token checked before and during population.</param>
    /// <returns>The same array after population.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the array is multidimensional or contains null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the array length exceeds configured limits.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    public Array FillFromValuesAndRulesAndCancellationToken(Array values, FakeRuleSet rules, CancellationToken cancellationToken)
    {
        if (values == null)
            throw new ArgumentNullException("values");
        if (rules == null)
            throw new ArgumentNullException("rules");
        if (values.Rank != 1)
            throw new ArgumentException("values must be a one-dimensional array.", "values");
        RequireCount(values.Length, "values");
        int lowerBound = values.GetLowerBound(0);
        ValidateArray(values, lowerBound);
        cancellationToken.ThrowIfCancellationRequested();
        int index = 0;
        while (index < values.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            object? value = values.GetValue(lowerBound + index);
            if (value == null)
                throw new ArgumentException("values cannot contain null.", "values");
            Populate(value, rules);
            index = index + 1;
        }

        return values;
    }

    /// <summary>Populates every existing object in a non-generic list.</summary>
    /// <param name="values">The list of non-null caller objects to populate.</param>
    /// <param name="rules">The ordered rule set applied to every object.</param>
    /// <returns>The same list after population.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the list contains null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the list count exceeds configured limits.</exception>
    public IList FillFromValuesAndRules(IList values, FakeRuleSet rules)
    {
        return FillFromValuesAndRulesAndCancellationTokenUsingIListAndFakeRuleSetAndCancellationToken(values, rules, CancellationToken.None);
    }

    /// <summary>Populates every existing object in a non-generic list with cancellation.</summary>
    /// <param name="values">The list of non-null caller objects to populate.</param>
    /// <param name="rules">The ordered rule set applied to every object.</param>
    /// <param name="cancellationToken">The token checked before and during population.</param>
    /// <returns>The same list after population.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the list contains null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the list count exceeds configured limits.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    public IList FillFromValuesAndRulesAndCancellationTokenUsingIListAndFakeRuleSetAndCancellationToken(IList values, FakeRuleSet rules, CancellationToken cancellationToken)
    {
        if (values == null)
            throw new ArgumentNullException("values");
        if (rules == null)
            throw new ArgumentNullException("rules");
        RequireCount(values.Count, "values");
        ValidateList(values);
        cancellationToken.ThrowIfCancellationRequested();
        int index = 0;
        while (index < values.Count)
        {
            cancellationToken.ThrowIfCancellationRequested();
            object? value = values[index];
            if (value == null)
                throw new ArgumentException("values cannot contain null.", "values");
            Populate(value, rules);
            index = index + 1;
        }

        return values;
    }

    void RequireCount(int count, string parameterName)
    {
        Guard.Count(count, FakeValue.Context.CapturedOptions.MaximumCount, parameterName);
    }

    static void RequireFactory(Func<object> factory, FakeRuleSet rules)
    {
        if (factory == null)
            throw new ArgumentNullException("factory");
        if (rules == null)
            throw new ArgumentNullException("rules");
    }

    static void ValidateArray(Array values, int lowerBound)
    {
        int index = 0;
        while (index < values.Length)
        {
            if (values.GetValue(lowerBound + index) == null)
                throw new ArgumentException("values cannot contain null at index " + index + ".", "values");
            index = index + 1;
        }
    }

    static void ValidateList(IList values)
    {
        int index = 0;
        while (index < values.Count)
        {
            if (values[index] == null)
                throw new ArgumentException("values cannot contain null at index " + index + ".", "values");
            index = index + 1;
        }
    }

    static ObjectMemberPath Resolve(object root, string memberPath, bool strict)
    {
        string[] segments = memberPath.Split('.');
        ObjectMember[] members = new ObjectMember[segments.Length];
        Type currentType = root.GetType();
        int index = 0;
        while (index < segments.Length)
        {
            ObjectMember? member = FindMember(currentType, segments[index]);
            if (member == null)
                return Unavailable(memberPath, "does not contain public instance member '" + segments[index] + "'", strict);
            bool last = index == segments.Length - 1;
            if (!last && !member.CanRead())
                return Unavailable(memberPath, "cannot read intermediate member '" + segments[index] + "'", strict);
            if (last && !member.CanWrite())
                return Unavailable(memberPath, "targets read-only member '" + segments[index] + "'", strict);
            members[index] = member;
            currentType = member.ValueType;
            index = index + 1;
        }

        object current = root;
        index = 0;
        while (index < members.Length - 1)
        {
            ObjectMember member = members[index];
            object? nested = member.Read(current);
            if (nested == null)
            {
                if (!member.CanWrite())
                    return Unavailable(memberPath, "cannot initialize read-only intermediate member '" + segments[index] + "'", strict);
                Type nestedType = member.ValueType;
                if (nestedType.IsValueType || nestedType.IsAbstract || nestedType.IsInterface || nestedType.GetConstructor(Type.EmptyTypes) == null)
                    return Unavailable(memberPath, "cannot initialize intermediate member '" + segments[index] + "' because its type has no public parameterless constructor", strict);
                nested = Activator.CreateInstance(nestedType);
                if (nested == null)
                    throw FakeException.Create("A public parameterless constructor returned null.");
                member.Write(current, nested);
            }

            current = nested;
            index = index + 1;
        }

        ObjectMemberPath result = new ObjectMemberPath();
        result.Found = true;
        result.Target = current;
        result.Member = members[members.Length - 1];
        return result;
    }

    static ObjectMember? FindMember(Type type, string name)
    {
        BindingFlags flags = BindingFlags.Instance | BindingFlags.Public;
        FieldInfo? field = type.GetField(name, flags);
        if (field != null)
        {
            return ObjectMember.FromField(name, field);
        }

        PropertyInfo? property = type.GetProperty(name, flags);
        if (property == null || property.GetIndexParameters().Length != 0)
            return null;
        return ObjectMember.FromProperty(name, property);
    }

    static ObjectMemberPath Unavailable(string memberPath, string reason, bool strict)
    {
        if (strict)
            throw FakeException.Create("Member path '" + memberPath + "' " + reason + ".");
        ObjectMemberPath result = new ObjectMemberPath();
        result.Found = false;
        result.Target = null;
        result.Member = null;
        return result;
    }

    static void RequireAssignable(string memberPath, Type targetType, object? value)
    {
        if (value == null)
        {
            if (targetType.IsValueType)
                throw FakeException.Create("Rule for member path '" + memberPath + "' produced null, which cannot be assigned to " + targetType.FullName + ".");
            return;
        }

        if (!targetType.IsInstanceOfType(value))
            throw FakeException.Create("Rule for member path '" + memberPath + "' produced " + value.GetType().FullName + ", which cannot be assigned to " + targetType.FullName + ".");
    }
}
