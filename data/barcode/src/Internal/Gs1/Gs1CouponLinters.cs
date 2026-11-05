namespace barcode;

internal static class Gs1CouponLinters
{
    internal static Gs1LinterResult LintPositiveOffer(string data)
    {
        int invalidOffset = FindCouponNonDigit(data);
        if (invalidOffset >= 0)
        {
            return CouponFailure(
                "GS1_LINTER_NON_DIGIT_CHARACTER",
                invalidOffset,
                1);
        }

        int offset = 0;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_FORMAT_CODE");
        }

        if (data[offset] != '0' && data[offset] != '1')
        {
            return CouponFailure(
                "GS1_LINTER_COUPON_INVALID_FORMAT_CODE",
                offset,
                1);
        }

        offset = offset + 1;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_FUNDER_VLI");
        }

        if (data[offset] > '6')
        {
            return CouponFailure(
                "GS1_LINTER_COUPON_INVALID_FUNDER_LENGTH",
                offset,
                1);
        }

        int variableLength = data[offset] - '0' + 6;
        offset = offset + 1;
        if (data.Length - offset < variableLength)
        {
            return CouponTruncated(
                data,
                offset,
                "GS1_LINTER_COUPON_TRUNCATED_FUNDER");
        }

        offset = offset + variableLength;
        if (data.Length - offset < 6)
        {
            return CouponTruncated(
                data,
                offset,
                "GS1_LINTER_COUPON_TRUNCATED_OFFER_CODE");
        }

        offset = offset + 6;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_SERIAL_NUMBER_VLI");
        }

        variableLength = data[offset] - '0' + 6;
        offset = offset + 1;
        if (data.Length - offset < variableLength)
        {
            return CouponTruncated(
                data,
                offset,
                "GS1_LINTER_COUPON_TRUNCATED_SERIAL_NUMBER");
        }

        offset = offset + variableLength;
        if (offset != data.Length)
        {
            return CouponFailure(
                "GS1_LINTER_COUPON_EXCESS_DATA",
                offset,
                data.Length - offset);
        }

        return Gs1ComponentLinters.SuccessResult();
    }

    internal static Gs1LinterResult LintCouponCode(string data)
    {
        int invalidOffset = FindCouponNonDigit(data);
        if (invalidOffset >= 0)
        {
            return CouponFailure(
                "GS1_LINTER_NON_DIGIT_CHARACTER",
                invalidOffset,
                1);
        }

        int offset = 0;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_GCP_VLI");
        }

        if (data[offset] > '6')
        {
            return CouponFailure(
                "GS1_LINTER_COUPON_INVALID_GCP_LENGTH",
                offset,
                1);
        }

        int variableLength = data[offset] - '0' + 6;
        offset = offset + 1;
        if (data.Length - offset < variableLength)
        {
            return CouponTruncated(
                data,
                offset,
                "GS1_LINTER_COUPON_TRUNCATED_GCP");
        }

        Gs1LinterResult prefix =
            Gs1ComponentLinters.LintCompanyPrefixForCoupon(
                data.Substring(offset, variableLength));
        if (!prefix.Success)
        {
            return CouponFailure(prefix.Code, offset, variableLength);
        }

        offset = offset + variableLength;
        if (data.Length - offset < 6)
        {
            return CouponTruncated(
                data,
                offset,
                "GS1_LINTER_COUPON_TRUNCATED_OFFER_CODE");
        }

        offset = offset + 6;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_SAVE_VALUE_VLI");
        }

        if (data[offset] < '1' || data[offset] > '5')
        {
            return CouponFailure(
                "GS1_LINTER_COUPON_INVALID_SAVE_VALUE_LENGTH",
                offset,
                1);
        }

        variableLength = data[offset] - '0';
        offset = offset + 1;
        if (data.Length - offset < variableLength)
        {
            return CouponTruncated(
                data,
                offset,
                "GS1_LINTER_COUPON_TRUNCATED_SAVE_VALUE");
        }

        offset = offset + variableLength;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_1ST_PURCHASE_REQUIREMENT_VLI");
        }

        if (data[offset] < '1' || data[offset] > '5')
        {
            return CouponFailure(
                "GS1_LINTER_COUPON_INVALID_1ST_PURCHASE_REQUIREMENT_LENGTH",
                offset,
                1);
        }

        variableLength = data[offset] - '0';
        offset = offset + 1;
        if (data.Length - offset < variableLength)
        {
            return CouponTruncated(
                data,
                offset,
                "GS1_LINTER_COUPON_TRUNCATED_1ST_PURCHASE_REQUIREMENT");
        }

        offset = offset + variableLength;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_1ST_PURCHASE_REQUIREMENT_CODE");
        }

        if (data[offset] > '4' && data[offset] != '9')
        {
            return CouponFailure(
                "GS1_LINTER_COUPON_INVALID_1ST_PURCHASE_REQUIREMENT_CODE",
                offset,
                1);
        }

        offset = offset + 1;
        if (data.Length - offset < 3)
        {
            return CouponTruncated(
                data,
                offset,
                "GS1_LINTER_COUPON_TRUNCATED_1ST_PURCHASE_FAMILY_CODE");
        }

        offset = offset + 3;
        Gs1LinterResult optionalOne = ValidateOptionalOne(data, offset);
        if (!optionalOne.Success)
        {
            return optionalOne;
        }

        offset = optionalOne.ErrorOffset;
        Gs1LinterResult optionalTwo = ValidateOptionalTwo(data, offset);
        if (!optionalTwo.Success)
        {
            return optionalTwo;
        }

        offset = optionalTwo.ErrorOffset;
        int expirationOffset = -1;
        if (offset < data.Length && data[offset] == '3')
        {
            offset = offset + 1;
            if (data.Length - offset < 6)
            {
                return CouponTruncated(
                    data,
                    offset,
                    "GS1_LINTER_COUPON_TOO_SHORT_FOR_EXPIRATION_DATE");
            }

            Gs1LinterResult expiration =
                Gs1ComponentLinters.LintDateForCoupon(
                    data.Substring(offset, 6));
            if (!expiration.Success)
            {
                return CouponFailure(
                    "GS1_LINTER_COUPON_INVALID_EXIPIRATION_DATE",
                    offset,
                    6);
            }

            expirationOffset = offset;
            offset = offset + 6;
        }

        if (offset < data.Length && data[offset] == '4')
        {
            offset = offset + 1;
            if (data.Length - offset < 6)
            {
                return CouponTruncated(
                    data,
                    offset,
                    "GS1_LINTER_COUPON_TOO_SHORT_FOR_START_DATE");
            }

            Gs1LinterResult start =
                Gs1ComponentLinters.LintDateForCoupon(
                    data.Substring(offset, 6));
            if (!start.Success)
            {
                return CouponFailure(
                    "GS1_LINTER_COUPON_INVALID_START_DATE",
                    offset,
                    6);
            }

            if (expirationOffset >= 0
                && CouponDateAfter(data, offset, expirationOffset))
            {
                return CouponFailure(
                    "GS1_LINTER_COUPON_EXPIRATION_BEFORE_START",
                    offset - 8,
                    14);
            }

            offset = offset + 6;
        }

        Gs1LinterResult optionalFive = ValidateOptionalFive(data, offset);
        if (!optionalFive.Success)
        {
            return optionalFive;
        }

        offset = optionalFive.ErrorOffset;
        Gs1LinterResult optionalSix = ValidateOptionalSix(data, offset);
        if (!optionalSix.Success)
        {
            return optionalSix;
        }

        offset = optionalSix.ErrorOffset;
        Gs1LinterResult optionalNine = ValidateOptionalNine(data, offset);
        if (!optionalNine.Success)
        {
            return optionalNine;
        }

        offset = optionalNine.ErrorOffset;
        if (offset != data.Length)
        {
            return CouponFailure(
                "GS1_LINTER_COUPON_EXCESS_DATA",
                offset,
                data.Length - offset);
        }

        return Gs1ComponentLinters.SuccessResult();
    }

    static Gs1LinterResult ValidateOptionalOne(string data, int offset)
    {
        if (offset >= data.Length || data[offset] != '1')
        {
            return CouponCursor(offset);
        }

        offset = offset + 1;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_ADDITIONAL_PURCHASE_RULES_CODE");
        }

        if (data[offset] > '3')
        {
            return CouponFailure(
                "GS1_LINTER_COUPON_INVALID_ADDITIONAL_PURCHASE_RULES_CODE",
                offset,
                1);
        }

        offset = offset + 1;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_2ND_PURCHASE_REQUIREMENT_VLI");
        }

        if (data[offset] < '1' || data[offset] > '5')
        {
            return CouponFailure(
                "GS1_LINTER_COUPON_INVALID_2ND_PURCHASE_REQUIREMENT_LENGTH",
                offset,
                1);
        }

        int variableLength = data[offset] - '0';
        offset = offset + 1;
        if (data.Length - offset < variableLength)
        {
            return CouponTruncated(
                data,
                offset,
                "GS1_LINTER_COUPON_TRUNCATED_2ND_PURCHASE_REQUIREMENT");
        }

        offset = offset + variableLength;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_2ND_PURCHASE_REQUIREMENT_CODE");
        }

        if (data[offset] > '4' && data[offset] != '9')
        {
            return CouponFailure(
                "GS1_LINTER_COUPON_INVALID_2ND_PURCHASE_REQUIREMENT_CODE",
                offset,
                1);
        }

        offset = offset + 1;
        if (data.Length - offset < 3)
        {
            return CouponTruncated(
                data,
                offset,
                "GS1_LINTER_COUPON_TRUNCATED_2ND_PURCHASE_FAMILY_CODE");
        }

        offset = offset + 3;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_2ND_PURCHASE_GCP_VLI");
        }

        if (data[offset] > '6' && data[offset] != '9')
        {
            return CouponFailure(
                "GS1_LINTER_COUPON_INVALID_2ND_PURCHASE_GCP_LENGTH",
                offset,
                1);
        }

        variableLength = data[offset] == '9'
            ? 0
            : data[offset] - '0' + 6;
        offset = offset + 1;
        if (data.Length - offset < variableLength)
        {
            return CouponTruncated(
                data,
                offset,
                "GS1_LINTER_COUPON_TRUNCATED_2ND_PURCHASE_GCP");
        }

        if (variableLength > 0)
        {
            Gs1LinterResult prefix =
                Gs1ComponentLinters.LintCompanyPrefixForCoupon(
                    data.Substring(offset, variableLength));
            if (!prefix.Success)
            {
                return CouponFailure(prefix.Code, offset, variableLength);
            }

            offset = offset + variableLength;
        }

        return CouponCursor(offset);
    }

    static Gs1LinterResult ValidateOptionalTwo(string data, int offset)
    {
        if (offset >= data.Length || data[offset] != '2')
        {
            return CouponCursor(offset);
        }

        offset = offset + 1;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_3RD_PURCHASE_REQUIREMENT_VLI");
        }

        if (data[offset] < '1' || data[offset] > '5')
        {
            return CouponFailure(
                "GS1_LINTER_COUPON_INVALID_3RD_PURCHASE_REQUIREMENT_LENGTH",
                offset,
                1);
        }

        int variableLength = data[offset] - '0';
        offset = offset + 1;
        if (data.Length - offset < variableLength)
        {
            return CouponTruncated(
                data,
                offset,
                "GS1_LINTER_COUPON_TRUNCATED_3RD_PURCHASE_REQUIREMENT");
        }

        offset = offset + variableLength;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_3RD_PURCHASE_REQUIREMENT_CODE");
        }

        if (data[offset] > '4' && data[offset] != '9')
        {
            return CouponFailure(
                "GS1_LINTER_COUPON_INVALID_3RD_PURCHASE_REQUIREMENT_CODE",
                offset,
                1);
        }

        offset = offset + 1;
        if (data.Length - offset < 3)
        {
            return CouponTruncated(
                data,
                offset,
                "GS1_LINTER_COUPON_TRUNCATED_3RD_PURCHASE_FAMILY_CODE");
        }

        offset = offset + 3;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_3RD_PURCHASE_GCP_VLI");
        }

        if (data[offset] > '6' && data[offset] != '9')
        {
            return CouponFailure(
                "GS1_LINTER_COUPON_INVALID_3RD_PURCHASE_GCP_LENGTH",
                offset,
                1);
        }

        variableLength = data[offset] == '9'
            ? 0
            : data[offset] - '0' + 6;
        offset = offset + 1;
        if (data.Length - offset < variableLength)
        {
            return CouponTruncated(
                data,
                offset,
                "GS1_LINTER_COUPON_TRUNCATED_3RD_PURCHASE_GCP");
        }

        if (variableLength > 0)
        {
            Gs1LinterResult prefix =
                Gs1ComponentLinters.LintCompanyPrefixForCoupon(
                    data.Substring(offset, variableLength));
            if (!prefix.Success)
            {
                return CouponFailure(prefix.Code, offset, variableLength);
            }

            offset = offset + variableLength;
        }

        return CouponCursor(offset);
    }

    static Gs1LinterResult ValidateOptionalFive(string data, int offset)
    {
        if (offset >= data.Length || data[offset] != '5')
        {
            return CouponCursor(offset);
        }

        offset = offset + 1;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_SERIAL_NUMBER_VLI");
        }

        int variableLength = data[offset] - '0' + 6;
        offset = offset + 1;
        if (data.Length - offset < variableLength)
        {
            return CouponTruncated(
                data,
                offset,
                "GS1_LINTER_COUPON_TRUNCATED_SERIAL_NUMBER");
        }

        return CouponCursor(offset + variableLength);
    }

    static Gs1LinterResult ValidateOptionalSix(string data, int offset)
    {
        if (offset >= data.Length || data[offset] != '6')
        {
            return CouponCursor(offset);
        }

        offset = offset + 1;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_RETAILER_GCP_OR_GLN_VLI");
        }

        if (data[offset] < '1' || data[offset] > '7')
        {
            return CouponFailure(
                "GS1_LINTER_COUPON_INVALID_RETAILER_GCP_OR_GLN_LENGTH",
                offset,
                1);
        }

        int variableLength = data[offset] - '0' + 6;
        offset = offset + 1;
        if (data.Length - offset < variableLength)
        {
            return CouponTruncated(
                data,
                offset,
                "GS1_LINTER_COUPON_TRUNCATED_RETAILER_GCP_OR_GLN");
        }

        Gs1LinterResult prefix =
            Gs1ComponentLinters.LintCompanyPrefixForCoupon(
                data.Substring(offset, variableLength));
        if (!prefix.Success)
        {
            return CouponFailure(prefix.Code, offset, variableLength);
        }

        return CouponCursor(offset + variableLength);
    }

    static Gs1LinterResult ValidateOptionalNine(string data, int offset)
    {
        if (offset >= data.Length || data[offset] != '9')
        {
            return CouponCursor(offset);
        }

        offset = offset + 1;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_SAVE_VALUE_CODE");
        }

        char saveValueCode = data[offset];
        if (saveValueCode != '0'
            && saveValueCode != '1'
            && saveValueCode != '2'
            && saveValueCode != '5'
            && saveValueCode != '6')
        {
            return CouponFailure(
                "GS1_LINTER_COUPON_INVALID_SAVE_VALUE_CODE",
                offset,
                1);
        }

        offset = offset + 1;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_SAVE_VALUE_APPLIES_TO_ITEM");
        }

        if (data[offset] > '2')
        {
            return CouponFailure(
                "GS1_LINTER_COUPON_INVALID_SAVE_VALUE_APPLIES_TO_ITEM",
                offset,
                1);
        }

        offset = offset + 1;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_STORE_COUPON_FLAG");
        }

        offset = offset + 1;
        if (offset == data.Length)
        {
            return CouponMissing(
                data,
                "GS1_LINTER_COUPON_MISSING_DONT_MULTIPLY_FLAG");
        }

        if (data[offset] != '0' && data[offset] != '1')
        {
            return CouponFailure(
                "GS1_LINTER_COUPON_INVALID_DONT_MULTIPLY_FLAG",
                offset,
                1);
        }

        return CouponCursor(offset + 1);
    }

    static bool CouponDateAfter(
        string data,
        int leftOffset,
        int rightOffset)
    {
        int index = 0;
        while (index < 6)
        {
            char left = data[leftOffset + index];
            char right = data[rightOffset + index];
            if (left != right)
            {
                return left > right;
            }

            index = index + 1;
        }

        return false;
    }

    static int FindCouponNonDigit(string data)
    {
        int index = 0;
        while (index < data.Length)
        {
            if (data[index] < '0' || data[index] > '9')
            {
                return index;
            }

            index = index + 1;
        }

        return -1;
    }

    static Gs1LinterResult CouponCursor(int offset)
    {
        Gs1LinterResult result = Gs1ComponentLinters.SuccessResult();
        result.ErrorOffset = offset;
        return result;
    }

    static Gs1LinterResult CouponMissing(string data, string code)
    {
        return CouponFailure(code, 0, data.Length);
    }

    static Gs1LinterResult CouponTruncated(
        string data,
        int offset,
        string code)
    {
        if (offset == data.Length)
        {
            return CouponFailure(code, 0, data.Length);
        }

        return CouponFailure(code, offset, data.Length - offset);
    }

    static Gs1LinterResult CouponFailure(
        string code,
        int offset,
        int length)
    {
        return Gs1ComponentLinters.FailureResult(code, offset, length);
    }
}
