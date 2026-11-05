using System;
using System.Collections.Generic;
using System.IO;

namespace fake;
/// <summary>
/// Provides explicit boundary and adversarial generation operations.
/// </summary>
public class EdgeProvider : IEdgeProvider
{
    internal GenerationContext Context;
    internal FakeRandom RandomValue;
    internal EdgeProvider(GenerationContext context)
    {
        Context = context;
        RandomValue = context.ProviderRandom("edge");
    }

    /// <summary>Selects an edge fixture according to the captured generation mode.</summary>
    /// <remarks>Valid-unusual, invalid, mixed, and adversarial modes are supported. Selection is stable for equal deterministic seeds and call sequences.</remarks>
    /// <returns>The selected edge and its rule-outcome metadata.</returns>
    /// <exception cref="System.InvalidOperationException">Thrown when the captured mode is typical or boundary.</exception>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are insufficient.</exception>
    public EdgeSelectionResult Select()
    {
        return EdgeCaseSelector.AccordingToMode(Context, RandomValue);
    }

    /// <summary>Selects a deterministic valid but unusual fixture.</summary>
    /// <returns>A valid selection with no violated rule.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are insufficient.</exception>
    public EdgeSelectionResult ValidUnusual()
    {
        return EdgeCaseSelector.ValidUnusual(Context, RandomValue, GenerationMode.ValidUnusual);
    }

    /// <summary>Selects a deterministic deliberately invalid fixture.</summary>
    /// <returns>An invalid selection naming the violated rule.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are insufficient.</exception>
    public EdgeSelectionResult Invalid()
    {
        return EdgeCaseSelector.Invalid(Context, RandomValue, GenerationMode.Invalid);
    }

    /// <summary>Selects deterministically among valid-unusual, invalid, and adversarial fixtures.</summary>
    /// <returns>A mixed-mode selection with independent validity and rule metadata.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are insufficient.</exception>
    public EdgeSelectionResult Mixed()
    {
        return EdgeCaseSelector.Mixed(Context, RandomValue);
    }

    /// <summary>Selects a deterministic inert adversarial-input fixture.</summary>
    /// <remarks>The value is returned as data only and is never interpreted or executed.</remarks>
    /// <returns>An adversarial selection naming the input-handling rule under test.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are insufficient.</exception>
    public EdgeSelectionResult Adversarial()
    {
        return EdgeCaseSelector.Adversarial(Context, RandomValue, GenerationMode.Adversarial);
    }

    /// <summary>Returns metadata for an explicit context-specific adversarial string.</summary>
    /// <remarks>The selected value is returned as inert data only, consumes no randomness, and is never selected by ordinary provider APIs.</remarks>
    /// <param name="context">The consumer context to exercise.</param>
    /// <returns>The exact adversarial fixture and its input-handling rule.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when context is unsupported or configured limits are insufficient.</exception>
    public EdgeSelectionResult AdversarialFromContext(AdversarialTextContext context)
    {
        return EdgeCaseSelector.AdversarialForContext(Context, context);
    }

    /// <summary>Returns an explicit context-specific adversarial string as inert data.</summary>
    /// <remarks>The string is not interpreted, executed, or selected by ordinary provider APIs.</remarks>
    /// <param name="context">The consumer context to exercise.</param>
    /// <returns>The exact inert string for that context.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when context is unsupported or configured limits are insufficient.</exception>
    public string AdversarialString(AdversarialTextContext context)
    {
        object? value = EdgeCaseCatalog.AdversarialText(Context, context).Value;
        if (value == null)
            throw FakeException.Create("The adversarial text fixture has no value.");
        return (string)value;
    }

    /// <summary>Returns every fixture in one explicit edge-case category.</summary>
    /// <remarks>Returned records and arrays are fresh. Injection fixtures are inert strings and are never executed.</remarks>
    /// <param name="category">The category to enumerate.</param>
    /// <returns>Fresh records in stable documented order.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when the category is unsupported or configured limits are insufficient.</exception>
    public EdgeCaseRecord[] Cases(EdgeCaseCategory category)
    {
        return EdgeCaseCatalog.Cases(Context, category);
    }

    /// <summary>Returns reference-null, empty, and whitespace-only fixtures.</summary>
    /// <returns>Fresh records in stable documented order.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are insufficient.</exception>
    public EdgeCaseRecord[] EmptyNullWhitespace()
    {
        return EdgeCaseCatalog.EmptyNullWhitespace(Context);
    }

    /// <summary>Returns numeric limits, non-finite values, and precision boundaries.</summary>
    /// <returns>Fresh records retaining their numeric runtime types.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are insufficient.</exception>
    public EdgeCaseRecord[] BoundaryNumbers()
    {
        return EdgeCaseCatalog.BoundaryNumbers(Context);
    }

    /// <summary>Returns long and repeated text fixtures with a default length of 256 UTF-16 units.</summary>
    /// <returns>Fresh records containing exact-length strings.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are insufficient.</exception>
    public EdgeCaseRecord[] LongRepeatedText()
    {
        return EdgeCaseCatalog.LongRepeatedText(Context, 256);
    }

    /// <summary>Returns long and repeated text fixtures with an explicit UTF-16 length.</summary>
    /// <param name="length">The positive exact length of each string.</param>
    /// <returns>Fresh records containing exact-length strings.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when length is not positive or exceeds configured limits.</exception>
    public EdgeCaseRecord[] LongRepeatedTextFromLength(int length)
    {
        return EdgeCaseCatalog.LongRepeatedText(Context, length);
    }

    /// <summary>Returns Unicode directionality, composition, scalar, and malformed-code-unit fixtures.</summary>
    /// <remarks>Malformed UTF-16 is represented only by a character-array value.</remarks>
    /// <returns>Fresh records in stable documented order.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are insufficient.</exception>
    public EdgeCaseRecord[] Unicode()
    {
        return EdgeCaseCatalog.Unicode(Context);
    }

    /// <summary>Returns calendar, clock, offset, epoch, and runtime temporal boundaries.</summary>
    /// <returns>Fresh records retaining their temporal runtime types.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are insufficient.</exception>
    public EdgeCaseRecord[] Temporal()
    {
        return EdgeCaseCatalog.Temporal(Context);
    }

    /// <summary>Returns malformed and unusual identifier-shaped fixtures.</summary>
    /// <remarks>Values are inert strings and make no allocation or ownership claim.</remarks>
    /// <returns>Fresh records in stable documented order.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are insufficient.</exception>
    public EdgeCaseRecord[] Identifiers()
    {
        return EdgeCaseCatalog.Identifiers(Context);
    }

    /// <summary>Returns opt-in input-injection-shaped strings.</summary>
    /// <remarks>Values are returned as inert data only and are never interpreted or executed.</remarks>
    /// <returns>Fresh records in stable documented order.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are insufficient.</exception>
    public EdgeCaseRecord[] Injections()
    {
        return EdgeCaseCatalog.Injections(Context);
    }

    /// <summary>Returns pairs that express contradictory input constraints.</summary>
    /// <returns>Fresh records whose primary and related values describe each contradiction.</returns>
    /// <exception cref="System.ArgumentOutOfRangeException">Thrown when configured limits are insufficient.</exception>
    public EdgeCaseRecord[] ContradictoryInputs()
    {
        return EdgeCaseCatalog.ContradictoryInputs(Context);
    }
}
