using System;
using System.Collections;
using System.Reflection;
using System.Threading;

namespace fake;
/// <summary>Defines the public operations exposed by <see cref="ObjectFiller"/>.</summary>
public interface IObjectFiller
{
    /// <summary>Applies ordered rules to an existing caller object.</summary>
    /// <param name="target">The object whose public fields or properties are populated.</param>
    /// <param name="rules">The ordered rule set to apply.</param>
    /// <returns>The same target object after population.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="FakeException">Thrown for strict missing or read-only paths, unsupported nested construction, or incompatible values.</exception>
    object Populate(object target, FakeRuleSet rules);
    /// <summary>Creates and populates one caller object.</summary>
    /// <param name="factory">The callback that creates the target object.</param>
    /// <param name="rules">The ordered rule set to apply.</param>
    /// <returns>The created and populated object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="FakeException">Thrown when the factory returns null or population fails.</exception>
    object Create(Func<object> factory, FakeRuleSet rules);
    /// <summary>Creates and populates one caller object with cancellation.</summary>
    /// <param name="factory">The callback that creates the target object.</param>
    /// <param name="rules">The ordered rule set to apply.</param>
    /// <param name="cancellationToken">The token checked before invoking the factory.</param>
    /// <returns>The created and populated object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <exception cref="FakeException">Thrown when the factory returns null or population fails.</exception>
    object CreateFromFactoryAndRulesAndCancellationToken(Func<object> factory, FakeRuleSet rules, CancellationToken cancellationToken);
    /// <summary>Creates and populates one caller object using a fake-aware factory.</summary>
    /// <param name="factory">The callback that receives the active <see cref="Fake"/> instance and creates the target.</param>
    /// <param name="rules">The ordered rule set to apply.</param>
    /// <returns>The created and populated object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="FakeException">Thrown when the factory returns null or population fails.</exception>
    object CreateFromFactoryAndRules(Func<Fake, object> factory, FakeRuleSet rules);
    /// <summary>Creates and populates one caller object using a fake-aware factory and cancellation.</summary>
    /// <param name="factory">The callback that receives the active <see cref="Fake"/> instance and creates the target.</param>
    /// <param name="rules">The ordered rule set to apply.</param>
    /// <param name="cancellationToken">The token checked before invoking the factory.</param>
    /// <returns>The created and populated object.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <exception cref="FakeException">Thrown when the factory returns null or population fails.</exception>
    object CreateFromFactoryAndRulesAndCancellationTokenUsingFuncOfFakeAndObjectAndFakeRuleSetAndCancellationToken(Func<Fake, object> factory, FakeRuleSet rules, CancellationToken cancellationToken);
    /// <summary>Creates and populates a bounded array of caller objects.</summary>
    /// <param name="factory">The callback invoked once for each target.</param>
    /// <param name="rules">The ordered rule set applied to every target.</param>
    /// <param name="count">The number of objects to create.</param>
    /// <returns>A new object array in callback invocation order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> exceeds configured limits.</exception>
    /// <exception cref="FakeException">Thrown when a factory invocation returns null or population fails.</exception>
    object[] Many(Func<object> factory, FakeRuleSet rules, int count);
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
    object[] ManyFromFactoryAndRulesAndCountAndCancellationToken(Func<object> factory, FakeRuleSet rules, int count, CancellationToken cancellationToken);
    /// <summary>Creates and populates a bounded array using a fake-aware factory.</summary>
    /// <param name="factory">The callback that receives the active <see cref="Fake"/> instance for each target.</param>
    /// <param name="rules">The ordered rule set applied to every target.</param>
    /// <param name="count">The number of objects to create.</param>
    /// <returns>A new object array in callback invocation order.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <c>count</c> exceeds configured limits.</exception>
    /// <exception cref="FakeException">Thrown when a factory invocation returns null or population fails.</exception>
    object[] ManyFromFactoryAndRulesAndCount(Func<Fake, object> factory, FakeRuleSet rules, int count);
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
    object[] ManyFromFactoryAndRulesAndCountAndCancellationTokenUsingFuncOfFakeAndObjectAndFakeRuleSetAndInt32AndCancellationToken(Func<Fake, object> factory, FakeRuleSet rules, int count, CancellationToken cancellationToken);
    /// <summary>Populates every existing object in a one-dimensional array.</summary>
    /// <param name="values">The array of non-null caller objects to populate.</param>
    /// <param name="rules">The ordered rule set applied to every object.</param>
    /// <returns>The same array after population.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the array is multidimensional or contains null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the array length exceeds configured limits.</exception>
    Array Fill(Array values, FakeRuleSet rules);
    /// <summary>Populates every existing object in a one-dimensional array with cancellation.</summary>
    /// <param name="values">The array of non-null caller objects to populate.</param>
    /// <param name="rules">The ordered rule set applied to every object.</param>
    /// <param name="cancellationToken">The token checked before and during population.</param>
    /// <returns>The same array after population.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the array is multidimensional or contains null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the array length exceeds configured limits.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    Array FillFromValuesAndRulesAndCancellationToken(Array values, FakeRuleSet rules, CancellationToken cancellationToken);
    /// <summary>Populates every existing object in a non-generic list.</summary>
    /// <param name="values">The list of non-null caller objects to populate.</param>
    /// <param name="rules">The ordered rule set applied to every object.</param>
    /// <returns>The same list after population.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the list contains null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the list count exceeds configured limits.</exception>
    IList FillFromValuesAndRules(IList values, FakeRuleSet rules);
    /// <summary>Populates every existing object in a non-generic list with cancellation.</summary>
    /// <param name="values">The list of non-null caller objects to populate.</param>
    /// <param name="rules">The ordered rule set applied to every object.</param>
    /// <param name="cancellationToken">The token checked before and during population.</param>
    /// <returns>The same list after population.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the list contains null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the list count exceeds configured limits.</exception>
    /// <exception cref="OperationCanceledException">Thrown when cancellation is requested.</exception>
    IList FillFromValuesAndRulesAndCancellationTokenUsingIListAndFakeRuleSetAndCancellationToken(IList values, FakeRuleSet rules, CancellationToken cancellationToken);
}
