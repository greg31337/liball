namespace json;

internal class JsonComparisonValidationFrame
{
    internal JsonValue Value;
    internal int NextChildIndex;

    internal JsonComparisonValidationFrame(JsonValue value)
    {
        Value = value;
    }
}

internal class JsonComparisonFrame
{
    internal JsonValue Left;
    internal JsonValue Right;
    internal int NextChildIndex;

    internal JsonComparisonFrame(JsonValue left, JsonValue right)
    {
        Left = left;
        Right = right;
    }
}

internal class JsonNormalizedNumber
{
    internal bool Negative;
    internal string Digits = "0";
    internal global::System.Numerics.BigInteger DecimalExponent;
}

internal static class JsonDeepComparer
{
    static readonly JsonValueFactory NumberFactory = new JsonValueFactory();

    internal static bool CompareValueGraphs(
        JsonValue left,
        JsonValue right,
        JsonComparisonMode mode)
    {
        if (left == null)
        {
            throw new global::System.ArgumentNullException("left");
        }

        if (right == null)
        {
            throw new global::System.ArgumentNullException("right");
        }

        if (mode != JsonComparisonMode.ExactModel &&
            mode != JsonComparisonMode.JsonSemantic)
        {
            throw new global::System.ArgumentOutOfRangeException("mode");
        }

        ValidateModelGraph(left);
        ValidateModelGraph(right);
        if (!CompareImmediateValues(left, right, mode))
        {
            return false;
        }

        if (!IsContainer(left))
        {
            return true;
        }

        global::System.Collections.Generic.List<JsonComparisonFrame> frames =
            new global::System.Collections.Generic.List<JsonComparisonFrame>();
        frames.Add(new JsonComparisonFrame(left, right));
        while (frames.Count > 0)
        {
            JsonComparisonFrame frame = frames[frames.Count - 1];
            JsonValue leftChild;
            JsonValue rightChild;
            if (frame.Left.Kind == JsonValueKind.Array)
            {
                global::System.Collections.Generic.List<JsonValue> leftItems =
                    frame.Left.ArrayItems
                    ?? throw new global::System.InvalidOperationException(
                        "A validated array requires an item list.");
                global::System.Collections.Generic.List<JsonValue> rightItems =
                    frame.Right.ArrayItems
                    ?? throw new global::System.InvalidOperationException(
                        "A validated array requires an item list.");
                if (frame.NextChildIndex >= leftItems.Count)
                {
                    frames.RemoveAt(frames.Count - 1);
                    continue;
                }

                leftChild = leftItems[frame.NextChildIndex];
                rightChild = rightItems[frame.NextChildIndex];
                frame.NextChildIndex++;
            }
            else
            {
                global::System.Collections.Generic.List<JsonMember> leftMembers =
                    frame.Left.ObjectMembers
                    ?? throw new global::System.InvalidOperationException(
                        "A validated object requires a member list.");
                global::System.Collections.Generic.List<JsonMember> rightMembers =
                    frame.Right.ObjectMembers
                    ?? throw new global::System.InvalidOperationException(
                        "A validated object requires a member list.");
                if (frame.NextChildIndex >= leftMembers.Count)
                {
                    frames.RemoveAt(frames.Count - 1);
                    continue;
                }

                int leftIndex = frame.NextChildIndex;
                int rightIndex = leftIndex;
                JsonMember leftMember = leftMembers[leftIndex];
                if (mode == JsonComparisonMode.JsonSemantic)
                {
                    rightIndex = FindSemanticMemberIndex(
                        leftMembers,
                        rightMembers,
                        leftIndex);
                    if (rightIndex < 0)
                    {
                        return false;
                    }
                }

                JsonMember rightMember = rightMembers[rightIndex];
                frame.NextChildIndex++;
                if (mode == JsonComparisonMode.ExactModel &&
                    !global::System.String.Equals(
                        leftMember.Name,
                        rightMember.Name,
                        global::System.StringComparison.Ordinal))
                {
                    return false;
                }

                leftChild = leftMember.Value
                    ?? throw new global::System.InvalidOperationException(
                        "A validated object member requires a value.");
                rightChild = rightMember.Value
                    ?? throw new global::System.InvalidOperationException(
                        "A validated object member requires a value.");
            }

            if (!CompareImmediateValues(leftChild, rightChild, mode))
            {
                return false;
            }

            if (IsContainer(leftChild))
            {
                frames.Add(new JsonComparisonFrame(leftChild, rightChild));
            }
        }

        return true;
    }

    static void ValidateModelGraph(JsonValue root)
    {
        ValidateValueShape(root);
        if (!IsContainer(root))
        {
            return;
        }

        global::System.Collections.Generic.List<JsonComparisonValidationFrame> frames =
            new global::System.Collections.Generic.List<JsonComparisonValidationFrame>();
        global::System.Collections.Generic.HashSet<JsonValue> active =
            new global::System.Collections.Generic.HashSet<JsonValue>();
        global::System.Collections.Generic.HashSet<JsonValue> completed =
            new global::System.Collections.Generic.HashSet<JsonValue>();
        frames.Add(new JsonComparisonValidationFrame(root));
        active.Add(root);
        while (frames.Count > 0)
        {
            JsonComparisonValidationFrame frame = frames[frames.Count - 1];
            JsonValue child;
            if (frame.Value.Kind == JsonValueKind.Array)
            {
                global::System.Collections.Generic.List<JsonValue> items =
                    frame.Value.ArrayItems
                    ?? throw new global::System.InvalidOperationException(
                        "A validated array requires an item list.");
                if (frame.NextChildIndex >= items.Count)
                {
                    active.Remove(frame.Value);
                    completed.Add(frame.Value);
                    frames.RemoveAt(frames.Count - 1);
                    continue;
                }

                child = items[frame.NextChildIndex]
                    ?? throw new global::System.InvalidOperationException(
                        "An array item cannot be null.");
                frame.NextChildIndex++;
            }
            else
            {
                global::System.Collections.Generic.List<JsonMember> members =
                    frame.Value.ObjectMembers
                    ?? throw new global::System.InvalidOperationException(
                        "A validated object requires a member list.");
                if (frame.NextChildIndex >= members.Count)
                {
                    active.Remove(frame.Value);
                    completed.Add(frame.Value);
                    frames.RemoveAt(frames.Count - 1);
                    continue;
                }

                JsonMember member = members[frame.NextChildIndex]
                    ?? throw new global::System.InvalidOperationException(
                        "An object member cannot be null.");
                frame.NextChildIndex++;
                ValidateMemberShape(member);
                child = member.Value
                    ?? throw new global::System.InvalidOperationException(
                        "A validated object member requires a value.");
            }

            ValidateValueShape(child);
            if (!IsContainer(child) || completed.Contains(child))
            {
                continue;
            }

            if (active.Contains(child))
            {
                throw new global::System.InvalidOperationException(
                    "The JSON model contains a reference cycle.");
            }

            active.Add(child);
            frames.Add(new JsonComparisonValidationFrame(child));
        }
    }

    static void ValidateValueShape(JsonValue value)
    {
        if (value.Kind == JsonValueKind.Uninitialized ||
            (value.Kind != JsonValueKind.Null &&
                value.Kind != JsonValueKind.Boolean &&
                value.Kind != JsonValueKind.Number &&
                value.Kind != JsonValueKind.String &&
                value.Kind != JsonValueKind.Array &&
                value.Kind != JsonValueKind.Object))
        {
            throw new global::System.InvalidOperationException(
                "The JSON value kind is uninitialized or undefined.");
        }

        if (value.Kind == JsonValueKind.Number)
        {
            ValidateNumberShape(value.Number);
        }

        if (value.Kind == JsonValueKind.String &&
            (value.StringValue == null || !IsValidUnicodeText(value.StringValue)))
        {
            throw new global::System.InvalidOperationException(
                "A string value requires valid Unicode text.");
        }

        if (value.Kind == JsonValueKind.Array && value.ArrayItems == null)
        {
            throw new global::System.InvalidOperationException(
                "An array value requires an item list.");
        }

        if (value.Kind == JsonValueKind.Object && value.ObjectMembers == null)
        {
            throw new global::System.InvalidOperationException(
                "An object value requires a member list.");
        }

        bool numberAllowed = value.Kind == JsonValueKind.Number;
        bool stringAllowed = value.Kind == JsonValueKind.String;
        bool arrayAllowed = value.Kind == JsonValueKind.Array;
        bool objectAllowed = value.Kind == JsonValueKind.Object;
        bool booleanAllowed = value.Kind == JsonValueKind.Boolean;
        if ((!numberAllowed && value.Number != null) ||
            (!stringAllowed && value.StringValue != null) ||
            (!arrayAllowed && value.ArrayItems != null) ||
            (!objectAllowed && value.ObjectMembers != null) ||
            (!booleanAllowed && value.BooleanValue))
        {
            throw new global::System.InvalidOperationException(
                "A JSON value contains a payload that does not match its kind.");
        }
    }

    static void ValidateNumberShape(JsonNumber? number)
    {
        if (number == null || number.Lexeme == null)
        {
            throw new global::System.InvalidOperationException(
                "A number value requires an exact lexeme.");
        }

        JsonValueResult result = NumberFactory.TryCreateNumber(number.Lexeme);
        JsonNumber? expected = result.Value?.Number;
        if (!result.Success || expected == null ||
            number.Kind != expected.Kind ||
            number.IsNegative != expected.IsNegative ||
            number.HasFraction != expected.HasFraction ||
            number.HasExponent != expected.HasExponent ||
            number.IsNegativeZero != expected.IsNegativeZero)
        {
            throw new global::System.InvalidOperationException(
                "A number payload does not match its exact JSON lexeme.");
        }
    }

    static void ValidateMemberShape(JsonMember member)
    {
        if (member.Name == null || !IsValidUnicodeText(member.Name))
        {
            throw new global::System.InvalidOperationException(
                "An object member requires a valid Unicode name.");
        }

        if (member.Value == null)
        {
            throw new global::System.InvalidOperationException(
                "An object member requires a value.");
        }

        if (member.SourceOrdinal < -1)
        {
            throw new global::System.InvalidOperationException(
                "An object member source ordinal cannot be less than -1.");
        }
    }

    static bool CompareImmediateValues(
        JsonValue left,
        JsonValue right,
        JsonComparisonMode mode)
    {
        if (left.Kind != right.Kind)
        {
            return false;
        }

        if (left.Kind == JsonValueKind.Null)
        {
            return true;
        }

        if (left.Kind == JsonValueKind.Boolean)
        {
            return left.BooleanValue == right.BooleanValue;
        }

        if (left.Kind == JsonValueKind.String)
        {
            return global::System.String.Equals(
                left.StringValue,
                right.StringValue,
                global::System.StringComparison.Ordinal);
        }

        if (left.Kind == JsonValueKind.Number)
        {
            JsonNumber leftNumber = left.Number
                ?? throw new global::System.InvalidOperationException(
                    "A validated number requires a payload.");
            JsonNumber rightNumber = right.Number
                ?? throw new global::System.InvalidOperationException(
                    "A validated number requires a payload.");
            if (mode == JsonComparisonMode.ExactModel)
            {
                return global::System.String.Equals(
                    leftNumber.Lexeme,
                    rightNumber.Lexeme,
                    global::System.StringComparison.Ordinal);
            }

            return CompareSemanticNumbers(leftNumber, rightNumber);
        }

        if (left.Kind == JsonValueKind.Array)
        {
            global::System.Collections.Generic.List<JsonValue> leftItems =
                left.ArrayItems
                ?? throw new global::System.InvalidOperationException(
                    "A validated array requires an item list.");
            global::System.Collections.Generic.List<JsonValue> rightItems =
                right.ArrayItems
                ?? throw new global::System.InvalidOperationException(
                    "A validated array requires an item list.");
            return leftItems.Count == rightItems.Count;
        }

        global::System.Collections.Generic.List<JsonMember> leftMembers =
            left.ObjectMembers
            ?? throw new global::System.InvalidOperationException(
                "A validated object requires a member list.");
        global::System.Collections.Generic.List<JsonMember> rightMembers =
            right.ObjectMembers
            ?? throw new global::System.InvalidOperationException(
                "A validated object requires a member list.");
        return leftMembers.Count == rightMembers.Count;
    }

    static int FindSemanticMemberIndex(
        global::System.Collections.Generic.List<JsonMember> leftMembers,
        global::System.Collections.Generic.List<JsonMember> rightMembers,
        int leftIndex)
    {
        string name = leftMembers[leftIndex].Name
            ?? throw new global::System.InvalidOperationException(
                "A validated object member requires a name.");
        int occurrence = 0;
        int index = 0;
        while (index < leftIndex)
        {
            if (global::System.String.Equals(
                leftMembers[index].Name,
                name,
                global::System.StringComparison.Ordinal))
            {
                occurrence++;
            }

            index++;
        }

        int rightOccurrence = 0;
        index = 0;
        while (index < rightMembers.Count)
        {
            if (global::System.String.Equals(
                rightMembers[index].Name,
                name,
                global::System.StringComparison.Ordinal))
            {
                if (rightOccurrence == occurrence)
                {
                    return index;
                }

                rightOccurrence++;
            }

            index++;
        }

        return -1;
    }

    static bool CompareSemanticNumbers(JsonNumber left, JsonNumber right)
    {
        string leftLexeme = left.Lexeme
            ?? throw new global::System.InvalidOperationException(
                "A validated number requires a lexeme.");
        string rightLexeme = right.Lexeme
            ?? throw new global::System.InvalidOperationException(
                "A validated number requires a lexeme.");
        JsonNormalizedNumber leftNormalized = NormalizeNumber(leftLexeme);
        JsonNormalizedNumber rightNormalized = NormalizeNumber(rightLexeme);
        return leftNormalized.Negative == rightNormalized.Negative &&
            global::System.String.Equals(
                leftNormalized.Digits,
                rightNormalized.Digits,
                global::System.StringComparison.Ordinal) &&
            leftNormalized.DecimalExponent == rightNormalized.DecimalExponent;
    }

    static JsonNormalizedNumber NormalizeNumber(string lexeme)
    {
        JsonNormalizedNumber normalized = new JsonNormalizedNumber();
        int index = 0;
        if (lexeme[0] == '-')
        {
            normalized.Negative = true;
            index++;
        }

        global::System.Text.StringBuilder digits =
            new global::System.Text.StringBuilder();
        int fractionalDigits = 0;
        bool afterDecimalPoint = false;
        while (index < lexeme.Length &&
            lexeme[index] != 'e' &&
            lexeme[index] != 'E')
        {
            if (lexeme[index] == '.')
            {
                afterDecimalPoint = true;
            }
            else
            {
                digits.Append(lexeme[index]);
                if (afterDecimalPoint)
                {
                    fractionalDigits++;
                }
            }

            index++;
        }

        global::System.Numerics.BigInteger exponent =
            global::System.Numerics.BigInteger.Zero;
        if (index < lexeme.Length)
        {
            index++;
            bool negativeExponent = false;
            if (lexeme[index] == '+' || lexeme[index] == '-')
            {
                negativeExponent = lexeme[index] == '-';
                index++;
            }

            while (index < lexeme.Length)
            {
                exponent = (exponent * 10) + (lexeme[index] - '0');
                index++;
            }

            if (negativeExponent)
            {
                exponent = -exponent;
            }
        }

        exponent -= fractionalDigits;
        string coefficient = digits.ToString();
        int firstNonzero = 0;
        while (firstNonzero < coefficient.Length &&
            coefficient[firstNonzero] == '0')
        {
            firstNonzero++;
        }

        if (firstNonzero == coefficient.Length)
        {
            normalized.Negative = false;
            normalized.Digits = "0";
            normalized.DecimalExponent =
                global::System.Numerics.BigInteger.Zero;
            return normalized;
        }

        int lastNonzero = coefficient.Length - 1;
        while (coefficient[lastNonzero] == '0')
        {
            lastNonzero--;
            exponent++;
        }

        normalized.Digits = coefficient.Substring(
            firstNonzero,
            lastNonzero - firstNonzero + 1);
        normalized.DecimalExponent = exponent;
        return normalized;
    }

    static bool IsContainer(JsonValue value)
    {
        return value.Kind == JsonValueKind.Array ||
            value.Kind == JsonValueKind.Object;
    }

    static bool IsValidUnicodeText(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            int character = text[index];
            if (character >= 0xD800 && character <= 0xDBFF)
            {
                if (index + 1 >= text.Length)
                {
                    return false;
                }

                int low = text[index + 1];
                if (low < 0xDC00 || low > 0xDFFF)
                {
                    return false;
                }

                index += 2;
                continue;
            }

            if (character >= 0xDC00 && character <= 0xDFFF)
            {
                return false;
            }

            index++;
        }

        return true;
    }
}
