namespace cbor;

/// <summary>Provides the normative RFC 8610 standard CDDL prelude as source data.</summary>
public static class CddlPrelude
{
    /// <summary>The complete standard prelude text automatically available to CDDL documents.</summary>
    public const string Standard =
        "any = #\n" +
        "\n" +
        "uint = #0\n" +
        "nint = #1\n" +
        "int = uint / nint\n" +
        "\n" +
        "bstr = #2\n" +
        "bytes = bstr\n" +
        "tstr = #3\n" +
        "text = tstr\n" +
        "\n" +
        "tdate = #6.0(tstr)\n" +
        "time = #6.1(number)\n" +
        "number = int / float\n" +
        "biguint = #6.2(bstr)\n" +
        "bignint = #6.3(bstr)\n" +
        "bigint = biguint / bignint\n" +
        "integer = int / bigint\n" +
        "unsigned = uint / biguint\n" +
        "decfrac = #6.4([e10: int, m: integer])\n" +
        "bigfloat = #6.5([e2: int, m: integer])\n" +
        "eb64url = #6.21(any)\n" +
        "eb64legacy = #6.22(any)\n" +
        "eb16 = #6.23(any)\n" +
        "encoded-cbor = #6.24(bstr)\n" +
        "uri = #6.32(tstr)\n" +
        "b64url = #6.33(tstr)\n" +
        "b64legacy = #6.34(tstr)\n" +
        "regexp = #6.35(tstr)\n" +
        "mime-message = #6.36(tstr)\n" +
        "cbor-any = #6.55799(any)\n" +
        "\n" +
        "float16 = #7.25\n" +
        "float32 = #7.26\n" +
        "float64 = #7.27\n" +
        "float16-32 = float16 / float32\n" +
        "float32-64 = float32 / float64\n" +
        "float = float16-32 / float64\n" +
        "\n" +
        "false = #7.20\n" +
        "true = #7.21\n" +
        "bool = false / true\n" +
        "nil = #7.22\n" +
        "null = nil\n" +
        "undefined = #7.23\n";

    /// <summary>The standard rule names in normative prelude order.</summary>
    public static readonly string[] RuleNames =
    [
        "any", "uint", "nint", "int", "bstr", "bytes", "tstr", "text",
        "tdate", "time", "number", "biguint", "bignint", "bigint", "integer",
        "unsigned", "decfrac", "bigfloat", "eb64url", "eb64legacy", "eb16",
        "encoded-cbor", "uri", "b64url", "b64legacy", "regexp", "mime-message",
        "cbor-any", "float16", "float32", "float64", "float16-32", "float32-64",
        "float", "false", "true", "bool", "nil", "null", "undefined"
    ];
}
