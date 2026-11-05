using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides correlated profile generation operations.
/// </summary>
public class ProfileProvider : IProfileProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal Fake FakeValue;
    internal ProfileProvider(GenerationContext context, Fake value)
    {
        Context = context;
        RandomValue = context.ProviderRandom("profile");
        FakeValue = value;
    }

    /// <summary>Generates a correlated synthetic person profile.</summary>
    /// <returns>A person profile with correlated identity, contact, locale, and age fields.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public PersonProfile Person()
    {
        return PersonProfileGenerator.Person(Context, RandomValue);
    }

    /// <summary>Generates a correlated synthetic household profile.</summary>
    /// <returns>A household whose members share its address, phone, locale, and time zone.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public HouseholdProfile Household()
    {
        return PersonProfileGenerator.Household(Context, RandomValue);
    }

    /// <summary>Generates a correlated synthetic company profile.</summary>
    /// <returns>A fictional company with reserved contact data and synthetic identifiers.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public CompanyProfile Company()
    {
        return CompanyGenerator.Profile(Context, RandomValue);
    }

    /// <summary>Generates a correlated synthetic product profile.</summary>
    /// <returns>A fictional product with consistent descriptive, identifier, price, and dimension fields.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public ProductProfile Product()
    {
        return ProductGenerator.Profile(Context, RandomValue);
    }

    /// <summary>Generates a correlated synthetic order profile.</summary>
    /// <returns>An order with consistent lines, totals, addresses, shipment, and lifecycle times.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    /// <exception cref="FakeException">Thrown when the captured reference time cannot support an order timeline.</exception>
    public OrderProfile Order()
    {
        CommerceOrder value = CommerceRecordGenerator.Order(Context, RandomValue);
        return OrderProfile.FromCommerceOrder(value);
    }

    /// <summary>Generates a correlated synthetic payment profile in US dollars.</summary>
    /// <returns>A payment calculation with consistent invoice, amount, and balance fields.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public PaymentProfile Payment()
    {
        PaymentRecord value = FinancialGenerator.Payment(Context, RandomValue, "USD");
        return PaymentProfile.FromPaymentRecord(value);
    }

    /// <summary>Generates a correlated synthetic vehicle profile.</summary>
    /// <returns>A fictional vehicle with correlated model, VIN, registration, and transport fields.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public VehicleProfile Vehicle()
    {
        return VehicleGenerator.Record(Context, RandomValue);
    }

    /// <summary>Generates a correlated synthetic flight profile.</summary>
    /// <returns>A fictional flight with correlated codes, route, seat, and schedule.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public FlightProfile Flight()
    {
        return AirlineGenerator.Record(Context, RandomValue);
    }

    /// <summary>Generates a correlated synthetic device profile.</summary>
    /// <returns>Project-fictional device hardware and software metadata with unregistered token shapes.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public DeviceProfile Device()
    {
        DeviceRecord value = DeviceGenerator.Record(RandomValue, Context.CapturedOptions.MaximumOutputSize);
        return DeviceProfile.FromDeviceRecord(value);
    }

    /// <summary>Generates a correlated synthetic student profile.</summary>
    /// <returns>A student profile with course and degree fields correlated to its subject.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when a configured limit is insufficient.</exception>
    public StudentProfile Student()
    {
        return EducationGenerator.Record(Context, RandomValue);
    }

    /// <summary>Returns a named dataset sequence starting at zero and incrementing by one.</summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <returns>The sequence shared with schema and random generation.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    /// <exception cref="FakeException">Thrown when the name already has different sequence configuration.</exception>
    public SequenceState Sequence(string name)
    {
        return Context.GetSequenceScope(name, 0L, 1L);
    }

    /// <summary>Returns a named arithmetic dataset sequence.</summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <param name="start">The first value returned after creation or reset.</param>
    /// <param name="increment">The nonzero amount added after each value.</param>
    /// <returns>The sequence shared with schema and random generation.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when <c>increment</c> is zero.</exception>
    /// <exception cref="FakeException">Thrown when the name already has different sequence configuration.</exception>
    public SequenceState SequenceFromNameAndStartAndIncrement(string name, long start, long increment)
    {
        return Context.GetSequenceScope(name, start, increment);
    }

    /// <summary>Returns a named dataset uniqueness scope.</summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <returns>The uniqueness scope shared with schema and random generation.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    public UniqueScope Unique(string name)
    {
        return Context.GetUniqueScope(name);
    }

    /// <summary>Generates a string unique within a named dataset scope.</summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <param name="generate">The callback used to generate candidate strings.</param>
    /// <returns>The first unused candidate.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    /// <exception cref="FakeException">Thrown when the retry budget is exhausted.</exception>
    public string UniqueFromNameAndGenerate(string name, System.Func<string> generate)
    {
        return Context.GetUniqueScope(name).Next(generate);
    }

    /// <summary>Generates an object whose composite key is unique within a named dataset scope.</summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <param name="generate">The callback used to generate candidate objects.</param>
    /// <param name="compositeKey">The callback selecting ordered string key parts.</param>
    /// <returns>The first candidate with an unused composite key.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    /// <exception cref="FakeException">Thrown when key selection fails or the retry budget is exhausted.</exception>
    public object UniqueFromNameAndGenerateAndCompositeKey(string name, System.Func<object> generate, System.Func<object, string[]> compositeKey)
    {
        return Context.GetUniqueScope(name).NextFromGenerateAndCompositeKey(generate, compositeKey);
    }

    /// <summary>Resets a named dataset sequence if it has been created.</summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    public void ResetSequence(string name)
    {
        Context.ResetSequence(name);
    }

    /// <summary>Resets all named dataset sequences.</summary>
    public void ResetSequenceFromNoArguments()
    {
        Context.ResetSequenceFromNoArguments();
    }

    /// <summary>Resets a named dataset uniqueness scope.</summary>
    /// <param name="name">The nonempty, case-sensitive scope name.</param>
    /// <exception cref="System.ArgumentNullException">Thrown when <c>name</c> is null.</exception>
    /// <exception cref="System.ArgumentException">Thrown when <c>name</c> is empty or whitespace.</exception>
    public void ResetUnique(string name)
    {
        Context.ResetUnique(name);
    }

    /// <summary>Resets all named dataset uniqueness scopes.</summary>
    public void ResetUniqueFromNoArguments()
    {
        Context.ResetUniqueFromNoArguments();
    }

    /// <summary>Creates and populates one caller-defined profile.</summary>
    /// <param name="factory">The callback that creates the target object.</param>
    /// <param name="rules">The ordered rules used to populate the target.</param>
    /// <returns>The created and populated caller object.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="FakeException">Thrown when the factory returns null or population fails.</exception>
    public object Custom(System.Func<object> factory, FakeRuleSet rules)
    {
        return new ObjectFiller(FakeValue).Create(factory, rules);
    }

    /// <summary>Creates and populates one caller-defined profile using a fake-aware factory.</summary>
    /// <param name="factory">The callback that receives this provider's owning <see cref="Fake"/> instance.</param>
    /// <param name="rules">The ordered rules used to populate the target.</param>
    /// <returns>The created and populated caller object.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="FakeException">Thrown when the factory returns null or population fails.</exception>
    public object CustomFromFactoryAndRules(System.Func<Fake, object> factory, FakeRuleSet rules)
    {
        return new ObjectFiller(FakeValue).CreateFromFactoryAndRules(factory, rules);
    }

    /// <summary>Creates and populates one caller-defined profile with cancellation.</summary>
    /// <param name="factory">The callback that creates the target object.</param>
    /// <param name="rules">The ordered rules used to populate the target.</param>
    /// <param name="cancellationToken">The token checked before invoking the factory.</param>
    /// <returns>The created and populated caller object.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <exception cref="FakeException">Thrown when the factory returns null or population fails.</exception>
    public object CustomFromFactoryAndRulesAndCancellationToken(System.Func<object> factory, FakeRuleSet rules, System.Threading.CancellationToken cancellationToken)
    {
        return new ObjectFiller(FakeValue).CreateFromFactoryAndRulesAndCancellationToken(factory, rules, cancellationToken);
    }

    /// <summary>Creates and populates one caller-defined profile using a fake-aware factory with cancellation.</summary>
    /// <param name="factory">The callback that receives this provider's owning <see cref="Fake"/> instance.</param>
    /// <param name="rules">The ordered rules used to populate the target.</param>
    /// <param name="cancellationToken">The token checked before invoking the factory.</param>
    /// <returns>The created and populated caller object.</returns>
    /// <exception cref="System.ArgumentNullException">Thrown when an argument is null.</exception>
    /// <exception cref="System.OperationCanceledException">Thrown when cancellation is requested.</exception>
    /// <exception cref="FakeException">Thrown when the factory returns null or population fails.</exception>
    public object CustomFromFactoryAndRulesAndCancellationTokenUsingFuncOfFakeAndObjectAndFakeRuleSetAndCancellationToken(System.Func<Fake, object> factory, FakeRuleSet rules, System.Threading.CancellationToken cancellationToken)
    {
        return new ObjectFiller(FakeValue).CreateFromFactoryAndRulesAndCancellationTokenUsingFuncOfFakeAndObjectAndFakeRuleSetAndCancellationToken(factory, rules, cancellationToken);
    }
}
