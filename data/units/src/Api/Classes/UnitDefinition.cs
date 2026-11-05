namespace units;

/// <summary>
/// Describes a unit's identity, dimension, conversion scale, offset, and aliases.
/// </summary>
/// <remarks>
/// For scale units, a system value is the input multiplied by the scale. Affine units additionally use the stored offset. Array metadata returned by this type is copied so callers cannot change the definition.
/// </remarks>
public class UnitDefinition : IUnitDefinition
{
    internal string _id;
    internal string _name;
    internal string _pluralName;
    internal string _symbol;
    internal string[] _aliases;
    internal DimensionVector _dimension;
    internal RationalNumber _scaleToSystemUnit;
    internal RationalNumber _offsetToSystemUnit;
    internal bool _prefixable;
    internal UnitConversionKind _conversionKind;
    internal string _canonicalExpression;

    /// <summary>
    /// Initializes a new unit definition from the supplied metadata.
    /// </summary>
    /// <param name="id">The stable identifier.</param>
    /// <param name="name">The display name.</param>
    /// <param name="pluralName">The plural display name.</param>
    /// <param name="symbol">The unit or metadata symbol.</param>
    /// <param name="aliases">The alternate lookup texts.</param>
    /// <param name="dimension">The dimension vector to use.</param>
    /// <param name="scaleToSystemUnit">The exact multiplier from this unit to its system unit.</param>
    /// <param name="offsetToSystemUnit">The exact offset used after scaling to the system unit.</param>
    /// <param name="prefixable">Whether registered prefixes may be applied to the unit.</param>
    /// <param name="conversionKind">The conversion mechanics used by the unit.</param>
    public UnitDefinition(string id, string name, string pluralName, string symbol, string[]? aliases, DimensionVector dimension, RationalNumber scaleToSystemUnit, RationalNumber offsetToSystemUnit, bool prefixable, UnitConversionKind conversionKind)
    {
        ValidateRequiredText(id, "id");
        ValidateRequiredText(name, "name");
        ValidateRequiredText(pluralName, "pluralName");
        ValidateRequiredText(symbol, "symbol");
        ValidateRequiredValue(dimension, "dimension");
        ValidateRequiredValue(scaleToSystemUnit, "scaleToSystemUnit");
        ValidateRequiredValue(offsetToSystemUnit, "offsetToSystemUnit");

        if (scaleToSystemUnit.IsZero())
        {
            throw new System.ArgumentException("Scale to system unit must not be zero.");
        }

        if (!IsValidConversionKind(conversionKind))
        {
            throw new System.ArgumentException("Unit conversion kind is not supported.");
        }

        _id = id;
        _name = name;
        _pluralName = pluralName;
        _symbol = symbol;
        _aliases = CopyAliases(aliases);
        _dimension = dimension;
        _scaleToSystemUnit = scaleToSystemUnit;
        _offsetToSystemUnit = offsetToSystemUnit;
        _prefixable = prefixable;
        _conversionKind = conversionKind;
        _canonicalExpression = EncodeCanonicalAtom(id);
    }

    /// <summary>
    /// Gets the stable identifier.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetId()
    {
        return _id;
    }

    /// <summary>
    /// Gets the display name.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetName()
    {
        return _name;
    }

    /// <summary>
    /// Gets the plural name.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetPluralName()
    {
        return _pluralName;
    }

    /// <summary>
    /// Gets the symbol.
    /// </summary>
    /// <returns>The requested text.</returns>
    public string GetSymbol()
    {
        return _symbol;
    }

    /// <summary>
    /// Returns a copy of the aliases.
    /// </summary>
    /// <returns>A copy of the matching or stored values.</returns>
    public string[] GetAliases()
    {
        return CopyAliases(_aliases);
    }

    /// <summary>
    /// Gets the dimension.
    /// </summary>
    /// <returns>The resulting dimension vector.</returns>
    public DimensionVector GetDimension()
    {
        return _dimension;
    }

    /// <summary>
    /// Gets the scale to system unit.
    /// </summary>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber GetScaleToSystemUnit()
    {
        return _scaleToSystemUnit;
    }

    /// <summary>
    /// Gets the offset to system unit.
    /// </summary>
    /// <returns>The resulting rational number.</returns>
    public RationalNumber GetOffsetToSystemUnit()
    {
        return _offsetToSystemUnit;
    }

    /// <summary>
    /// Reports whether registered prefixes may be applied to this unit.
    /// </summary>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsPrefixable()
    {
        return _prefixable;
    }

    /// <summary>
    /// Gets the conversion kind.
    /// </summary>
    /// <returns>The resulting unit conversion kind.</returns>
    public UnitConversionKind GetConversionKind()
    {
        return _conversionKind;
    }

    /// <summary>
    /// Reports whether the supplied unit has the same dimension vector.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsCompatibleWith(UnitDefinition? unit)
    {
        if (unit == null)
        {
            return false;
        }

        return _dimension.EqualsDimension(unit.GetDimension());
    }

    /// <summary>
    /// Reports whether the supplied unit has the same dimension, scale, offset, and conversion kind.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>True when the condition is met; otherwise, false.</returns>
    public bool IsEquivalentTo(UnitDefinition? unit)
    {
        if (unit == null)
        {
            return false;
        }

        return IsCompatibleWith(unit)
            && _scaleToSystemUnit.EqualsValue(unit.GetScaleToSystemUnit())
            && _offsetToSystemUnit.EqualsValue(unit.GetOffsetToSystemUnit())
            && _conversionKind == unit.GetConversionKind();
    }

    /// <summary>
    /// Multiplies the supplied unit definitions and adds their dimension exponents and exact scales.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Multiply(UnitDefinition unit)
    {
        RequireUnit(unit);
        RequireScaleOnly(this);
        RequireScaleOnly(unit);

        return CreateCompoundUnit(
            "(" + _id + "*" + unit.GetId() + ")",
            "(" + _canonicalExpression + "*" + unit.GetCanonicalExpression() + ")",
            _name + " " + unit.GetName(),
            _symbol + "*" + unit.GetSymbol(),
            _dimension.Multiply(unit.GetDimension()),
            _scaleToSystemUnit.Multiply(unit.GetScaleToSystemUnit()));
    }

    /// <summary>
    /// Divides by the supplied unit definition and subtracts its dimension exponents and exact scale.
    /// </summary>
    /// <param name="unit">The unit definition to use.</param>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Divide(UnitDefinition unit)
    {
        RequireUnit(unit);
        RequireScaleOnly(this);
        RequireScaleOnly(unit);

        return CreateCompoundUnit(
            "(" + _id + "/" + unit.GetId() + ")",
            "(" + _canonicalExpression + "/" + unit.GetCanonicalExpression() + ")",
            _name + " per " + unit.GetName(),
            _symbol + "/" + unit.GetSymbol(),
            _dimension.Divide(unit.GetDimension()),
            _scaleToSystemUnit.Divide(unit.GetScaleToSystemUnit()));
    }

    /// <summary>
    /// Creates the reciprocal unit by negating its dimension exponents and inverting its scale.
    /// </summary>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Inverse()
    {
        RequireScaleOnly(this);

        return CreateCompoundUnit(
            "(1/" + _id + ")",
            "(1/" + _canonicalExpression + ")",
            "inverse " + _name,
            "1/" + _symbol,
            _dimension.Pow(-1),
            new RationalNumber(1, 1).Divide(_scaleToSystemUnit));
    }

    /// <summary>
    /// Raises the exact scale and every dimension exponent to the supplied integer power.
    /// </summary>
    /// <param name="power">The integer exponent to apply.</param>
    /// <returns>The resulting unit definition.</returns>
    public UnitDefinition Pow(int power)
    {
        RequireScaleOnly(this);

        return CreateCompoundUnit(
            "(" + _id + "^" + FormatInt(power) + ")",
            "(" + _canonicalExpression + "^" + FormatInt(power) + ")",
            _name + "^" + FormatInt(power),
            _symbol + "^" + FormatInt(power),
            _dimension.Pow(power),
            PowerRational(_scaleToSystemUnit, power));
    }

    /// <summary>
    /// Attempts to take an exact root of both the rational scale and every dimension exponent.
    /// </summary>
    /// <param name="root">The positive integer root to calculate.</param>
    /// <returns>A result that describes success or failure.</returns>
    public UnitRootResult Root(int root)
    {
        if (root <= 0)
        {
            return UnitRootResult.CreateFailure(UnitErrorCode.ArithmeticInvalidOperation, "Root must be greater than zero.");
        }

        if (!CanUseInCompound())
        {
            return UnitRootResult.CreateFailure(UnitErrorCode.ArithmeticInvalidOperation, "Only scale-only units can be rooted.");
        }

        DimensionRootResult dimensionRoot = _dimension.Root(root);
        if (dimensionRoot.IsFailure())
        {
            return UnitRootResult.CreateFailure(dimensionRoot.GetErrorCode(), dimensionRoot.GetMessage());
        }

        RationalNumber? scaleRoot = ExactRationalRoot(_scaleToSystemUnit, root);
        if (scaleRoot == null)
        {
            return UnitRootResult.CreateFailure(UnitErrorCode.ArithmeticInvalidOperation, "Unit scale must have an exact rational root.");
        }

        return UnitRootResult.CreateSuccess(
            CreateCompoundUnit(
                "(" + _id + "^1/" + FormatInt(root) + ")",
                "(" + _canonicalExpression + "^1/" + FormatInt(root) + ")",
                _name + "^1/" + FormatInt(root),
                _symbol + "^1/" + FormatInt(root),
                dimensionRoot.GetDimension(),
                scaleRoot));
    }

    internal static void ValidateRequiredText(string value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new System.ArgumentException("Unit " + fieldName + " must not be empty.");
        }
    }

    internal static void ValidateRequiredValue(object value, string fieldName)
    {
        if (value == null)
        {
            throw new System.ArgumentNullException(fieldName);
        }
    }

    internal static string[] CopyAliases(string[]? aliases)
    {
        if (aliases == null)
        {
            return new string[0];
        }

        string[] copy = new string[aliases.Length];
        int index = 0;

        while (index < aliases.Length)
        {
            ValidateRequiredText(aliases[index], "alias");
            copy[index] = aliases[index];
            index = index + 1;
        }

        return copy;
    }

    internal bool CanUseInCompound()
    {
        return _conversionKind == UnitConversionKind.Scale && _offsetToSystemUnit.IsZero();
    }

    internal static void RequireUnit(UnitDefinition unit)
    {
        if (unit == null)
        {
            throw new System.ArgumentNullException("unit");
        }
    }

    internal static void RequireScaleOnly(UnitDefinition unit)
    {
        if (!unit.CanUseInCompound())
        {
            throw new System.InvalidOperationException("Only scale-only units can be used in compound unit algebra.");
        }
    }

    internal string GetCanonicalExpression()
    {
        return _canonicalExpression;
    }

    internal static UnitDefinition CreateCompoundUnit(string id, string canonicalExpression, string name, string symbol, DimensionVector dimension, RationalNumber scale)
    {
        UnitDefinition result = new UnitDefinition(
            "compound",
            name,
            name,
            symbol,
            new string[0],
            dimension,
            scale,
            new RationalNumber(0, 1),
            false,
            UnitConversionKind.Scale);
        result._id = id;
        result._canonicalExpression = canonicalExpression;
        return result;
    }

    internal static string EncodeCanonicalAtom(string id)
    {
        if (IsCanonicalIdentifier(id))
        {
            return id;
        }

        string text = "[";
        int index = 0;
        while (index < id.Length)
        {
            int value = id[index];
            text = text + HexDigit(value / 4096);
            text = text + HexDigit(value / 256 % 16);
            text = text + HexDigit(value / 16 % 16);
            text = text + HexDigit(value % 16);
            index = index + 1;
        }

        return text + "]";
    }

    internal static bool IsCanonicalIdentifier(string text)
    {
        if (text.Length == 0 || !IsCanonicalIdentifierStart(text[0]))
        {
            return false;
        }

        int index = 1;
        while (index < text.Length)
        {
            if (!IsCanonicalIdentifierPart(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal static bool IsCanonicalIdentifierStart(char value)
    {
        return value == '_' || value >= 'A' && value <= 'Z' || value >= 'a' && value <= 'z';
    }

    internal static bool IsCanonicalIdentifierPart(char value)
    {
        return IsCanonicalIdentifierStart(value);
    }

    internal static string HexDigit(int value)
    {
        if (value < 10)
        {
            return ((char)('0' + value)).ToString();
        }

        return ((char)('A' + value - 10)).ToString();
    }

    internal static RationalNumber PowerRational(RationalNumber value, int power)
    {
        if (power == 0)
        {
            return new RationalNumber(1, 1);
        }

        if (power < 0)
        {
            if (power == int.MinValue)
            {
                throw new System.OverflowException("Power is too small to negate.");
            }

            return new RationalNumber(1, 1).Divide(PowerRational(value, 0 - power));
        }

        RationalNumber result = new RationalNumber(1, 1);
        RationalNumber factor = value;
        int remaining = power;

        while (remaining > 0)
        {
            if (remaining % 2 == 1)
            {
                result = result.Multiply(factor);
            }

            remaining = remaining / 2;
            if (remaining > 0)
            {
                factor = factor.Multiply(factor);
            }
        }

        return result;
    }

    internal static RationalNumber? ExactRationalRoot(RationalNumber value, int root)
    {
        System.Numerics.BigInteger sourceNumerator = value.GetNumeratorAsBigInteger();
        bool negative = sourceNumerator < new System.Numerics.BigInteger(0);
        if (negative && root % 2 == 0)
        {
            return null;
        }

        if (negative)
        {
            sourceNumerator = new System.Numerics.BigInteger(0) - sourceNumerator;
        }

        System.Numerics.BigInteger numerator = ExactIntegerRoot(sourceNumerator, root);
        System.Numerics.BigInteger denominator = ExactIntegerRoot(value.GetDenominatorAsBigInteger(), root);

        if (numerator < new System.Numerics.BigInteger(0) || denominator < new System.Numerics.BigInteger(0))
        {
            return null;
        }

        if (negative)
        {
            numerator = new System.Numerics.BigInteger(0) - numerator;
        }

        return RationalNumber.FromBigIntegers(numerator, denominator);
    }

    internal static System.Numerics.BigInteger ExactIntegerRoot(System.Numerics.BigInteger value, int root)
    {
        if (value < new System.Numerics.BigInteger(0))
        {
            return new System.Numerics.BigInteger(-1);
        }

        if (value == new System.Numerics.BigInteger(0) || value == new System.Numerics.BigInteger(1))
        {
            return value;
        }

        System.Numerics.BigInteger low = new System.Numerics.BigInteger(1);
        long bitLength = value.GetBitLength();
        if ((long)root >= bitLength)
        {
            return new System.Numerics.BigInteger(-1);
        }

        long highBitLength = (bitLength - 1L) / root + 1L;
        if (highBitLength > int.MaxValue)
        {
            return new System.Numerics.BigInteger(-1);
        }

        System.Numerics.BigInteger high = System.Numerics.BigInteger.Pow(new System.Numerics.BigInteger(2), (int)highBitLength);
        System.Numerics.BigInteger target = value;

        while (low <= high)
        {
            System.Numerics.BigInteger mid = low + (high - low) / new System.Numerics.BigInteger(2);
            System.Numerics.BigInteger powered = System.Numerics.BigInteger.Pow(mid, root);
            int comparison = powered.CompareTo(target);

            if (comparison == 0)
            {
                return mid;
            }

            if (comparison < 0)
            {
                low = mid + new System.Numerics.BigInteger(1);
            }
            else
            {
                high = mid - new System.Numerics.BigInteger(1);
            }
        }

        return new System.Numerics.BigInteger(-1);
    }

    internal static bool IsValidConversionKind(UnitConversionKind conversionKind)
    {
        return conversionKind == UnitConversionKind.Unsupported
            || conversionKind == UnitConversionKind.Scale
            || conversionKind == UnitConversionKind.Affine
            || conversionKind == UnitConversionKind.Special;
    }

    internal static string FormatInt(int value)
    {
        return value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}
