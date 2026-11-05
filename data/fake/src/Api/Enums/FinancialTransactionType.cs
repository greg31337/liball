using System;

namespace fake;

/// <summary>Selects a synthetic financial transaction category.</summary>
public enum FinancialTransactionType
{
    /// <summary>A deposit-shaped transaction.</summary>
    Deposit = 0,

    /// <summary>A withdrawal-shaped transaction.</summary>
    Withdrawal = 1,

    /// <summary>A transfer-shaped transaction.</summary>
    Transfer = 2,

    /// <summary>A card-payment-shaped transaction.</summary>
    CardPayment = 3,

    /// <summary>A refund-shaped transaction.</summary>
    Refund = 4,

    /// <summary>A fee-shaped transaction.</summary>
    Fee = 5,

    /// <summary>An interest-shaped transaction.</summary>
    Interest = 6,

    /// <summary>A check-shaped transaction.</summary>
    Check = 7
}
