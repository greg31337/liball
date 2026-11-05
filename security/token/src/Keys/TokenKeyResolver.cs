using System;
namespace token;

internal static class TokenKeyResolution
{
    internal static TokenKeyResult Resolve(
        ITokenKeyResolver resolver,
        TokenKeyResolutionContext context)
    {
        return ValidateResult(resolver.Resolve(context));
    }

    internal static TokenKeyResult ValidateResult(TokenKeyResult result)
    {
        if (result == null)
        {
            return Failure(
                "invalid_key_resolution_result",
                "The token key resolver returned no result.");
        }

        if (result.Success
            && (result.Key == null || result.Error != null))
        {
            return Failure(
                "invalid_key_resolution_result",
                "A successful token key resolution must contain exactly one key and no error.");
        }

        if (!result.Success
            && (result.Error == null || result.Key != null))
        {
            return result.Error == null
                ? Failure(
                    "key_resolution_failed",
                    "The token key resolver did not return failure details.")
                : Failure(
                    "invalid_key_resolution_result",
                    "A failed token key resolution cannot contain a key.");
        }

        return result;
    }

    static TokenKeyResult Failure(string code, string message)
    {
        TokenKeyResult result = new TokenKeyResult();
        result.Error = new TokenError(code, message, -1);
        return result;
    }
}
