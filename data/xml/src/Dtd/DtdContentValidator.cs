using System;
using System.Collections.Generic;

namespace xml;

internal interface IDtdContentModelSource
{
    DtdCompiledContentModel? FindDtdCompiledContentModel(string elementName);
}

internal class DtdContentValidator
{
    internal IDtdContentModelSource Source;
    internal DtdValidationContext ValidationContext;
    internal List<DtdContentValidationState> States;

    internal DtdContentValidator(IDtdContentModelSource source, DtdValidationContext validationContext)
    {
        Guard.NotNull(source, "source");
        Guard.NotNull(validationContext, "validationContext");
        Source = source;
        ValidationContext = validationContext;
        States = new List<DtdContentValidationState>();
    }

    internal void BeginDtdElementContent(string elementName, int offset, bool emptyElement)
    {
        Guard.NotNullOrEmpty(elementName, "elementName");
        if (States.Count > 0)
        {
            ConsumeDtdChildElement(elementName, offset);
        }

        DtdCompiledContentModel? model = Source.FindDtdCompiledContentModel(elementName);
        int[] initial = model == null ? [] : CopyDtdContentPositions(model.InitialPositions);
        bool accepting = model == null || model.AcceptsEmpty || model.IsAny || model.IsMixed || model.IsEmpty;
        DtdContentValidationState state = new DtdContentValidationState(elementName, model, initial, accepting);
        States.Add(state);
        if (emptyElement)
        {
            EndDtdElementContent(elementName, offset);
        }
    }

    internal void EndDtdElementContent(string elementName, int offset)
    {
        Guard.NotNullOrEmpty(elementName, "elementName");
        if (States.Count == 0)
        {
            throw new InvalidOperationException("No DTD element content validation state is open.");
        }

        int index = States.Count - 1;
        DtdContentValidationState state = States[index];
        if (state.ElementName != elementName)
        {
            throw new InvalidOperationException("The DTD element content validation state does not match the closing element.");
        }

        if (!state.Accepting)
        {
            ValidationContext.ReportDtdValidationError("Element '" + elementName + "' ended before its declared content model was satisfied", offset);
        }

        States.RemoveAt(index);
    }

    internal void ConsumeDtdChildElement(string childName, int offset)
    {
        DtdContentValidationState state = States[States.Count - 1];
        DtdCompiledContentModel? model = state.Model;
        if (model == null || model.IsAny)
        {
            return;
        }

        if (model.IsEmpty)
        {
            ValidationContext.ReportDtdValidationError("Element '" + state.ElementName + "' is declared EMPTY and cannot contain element '" + childName + "'", offset);
            return;
        }

        if (model.IsMixed)
        {
            if (!ContainsDtdContentName(model.MixedNames, childName))
            {
                ValidationContext.ReportDtdValidationError("Element '" + childName + "' is not permitted by the mixed-content model of element '" + state.ElementName + "'", offset);
            }

            return;
        }

        int matchedPosition = -1;
        int index = 0;
        while (index < state.ExpectedPositions.Length)
        {
            int position = state.ExpectedPositions[index];
            if (model.PositionNames[position] == childName)
            {
                matchedPosition = position;
                break;
            }

            index = index + 1;
        }

        if (matchedPosition < 0)
        {
            ValidationContext.ReportDtdValidationError("Element '" + childName + "' is not permitted at this position in element '" + state.ElementName + "'", offset);
            return;
        }

        state.ExpectedPositions = CopyDtdContentPositions(model.FollowPositions[matchedPosition]);
        state.Accepting = ContainsDtdContentPosition(model.FinalPositions, matchedPosition);
    }

    internal void ConsumeDtdCharacterContent(string text, int offset)
    {
        Guard.NotNull(text, "text");
        if (text.Length == 0 || States.Count == 0)
        {
            return;
        }

        DtdContentValidationState state = States[States.Count - 1];
        DtdCompiledContentModel? model = state.Model;
        if (model == null || model.IsAny || model.IsMixed)
        {
            return;
        }

        if (model.IsEmpty)
        {
            ValidationContext.ReportDtdValidationError("Element '" + state.ElementName + "' is declared EMPTY and cannot contain character content", offset);
            return;
        }

        if (!IsDtdWhitespace(text))
        {
            ValidationContext.ReportDtdValidationError("Element '" + state.ElementName + "' has character content that is not permitted by its declaration", offset);
        }
    }

    internal void ConsumeDtdReferencedCharacterContent(string text, int offset, bool syntacticCharacterReference)
    {
        Guard.NotNull(text, "text");
        if (States.Count == 0)
        {
            return;
        }

        DtdContentValidationState state = States[States.Count - 1];
        DtdCompiledContentModel? model = state.Model;
        if (model == null || model.IsAny || model.IsMixed)
        {
            return;
        }

        if (model.IsEmpty || syntacticCharacterReference && IsDtdWhitespace(text))
        {
            ValidationContext.ReportDtdValidationError("Element '" + state.ElementName + "' contains an entity or character reference that is not permitted by its declaration", offset);
        }
    }

    internal void ConsumeDtdMarkupContent(int offset)
    {
        if (States.Count == 0)
        {
            return;
        }

        DtdContentValidationState state = States[States.Count - 1];
        DtdCompiledContentModel? model = state.Model;
        if (model != null && model.IsEmpty)
        {
            ValidationContext.ReportDtdValidationError("Element '" + state.ElementName + "' is declared EMPTY and cannot contain markup", offset);
        }
    }

    internal bool IsDtdIgnorableWhitespace(string? text)
    {
        if (text == null || text.Length == 0 || States.Count == 0 || !IsDtdWhitespace(text))
        {
            return false;
        }

        DtdCompiledContentModel? model = States[States.Count - 1].Model;
        return model != null && !model.IsAny && !model.IsMixed && !model.IsEmpty;
    }

    internal bool IsDtdWhitespace(string text)
    {
        int index = 0;
        while (index < text.Length)
        {
            if (!XmlLexicalCharacterRules.IsXmlSpaceCharacter(text[index]))
            {
                return false;
            }

            index = index + 1;
        }

        return true;
    }

    internal bool ContainsDtdContentName(string[] names, string name)
    {
        int index = 0;
        while (index < names.Length)
        {
            if (names[index] == name)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal bool ContainsDtdContentPosition(int[] positions, int position)
    {
        int index = 0;
        while (index < positions.Length)
        {
            if (positions[index] == position)
            {
                return true;
            }

            index = index + 1;
        }

        return false;
    }

    internal int[] CopyDtdContentPositions(int[] positions)
    {
        int[] copy = new int[positions.Length];
        if (positions.Length > 0)
        {
            Array.Copy(positions, copy, positions.Length);
        }

        return copy;
    }

}
