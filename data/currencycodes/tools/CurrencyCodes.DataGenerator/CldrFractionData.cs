using System;
using System.Collections.Generic;

namespace CurrencyCodes.DataGenerator;

internal sealed class CldrFractionData
{
    internal CldrFractionData(CldrFractionRule defaultRule, IReadOnlyList<CldrFractionRule> explicitRules)
    {
        DefaultRule = defaultRule;
        ExplicitRules = explicitRules;
    }

    internal CldrFractionRule DefaultRule { get; }

    internal IReadOnlyList<CldrFractionRule> ExplicitRules { get; }

    internal CldrFractionRule ResolveRule(string currencyCode)
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
        {
            throw new ArgumentException("A currency code is required.", nameof(currencyCode));
        }

        foreach (CldrFractionRule rule in ExplicitRules)
        {
            if (string.Equals(rule.CurrencyCode, currencyCode, StringComparison.Ordinal))
            {
                return rule;
            }
        }

        return new CldrFractionRule(
            currencyCode,
            DefaultRule.Digits,
            DefaultRule.Rounding,
            DefaultRule.CashDigits,
            DefaultRule.CashRounding,
            false,
            false,
            false,
            false);
    }
}
