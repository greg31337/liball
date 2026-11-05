namespace asn1;

internal class EncodingEffectiveConstraints
{
    internal EncodingConstraintView Ber = new EncodingConstraintView();
    internal EncodingConstraintView Per = new EncodingConstraintView();
    internal EncodingConstraintView Oer = new EncodingConstraintView();
    internal EncodingConstraintView Xer = new EncodingConstraintView();
    internal EncodingConstraintView Other = new EncodingConstraintView();
    internal InnerCodecMetadata[] ContentsConstraints = [];
    internal NormalizedTypeConstraint[] TypeConstraints = [];

    internal EncodingConstraintView GetView(AsnEncodingRules rules)
    {
        if (rules == AsnEncodingRules.Ber
            || rules == AsnEncodingRules.Der
            || rules == AsnEncodingRules.Cer)
        {
            return this.Ber;
        }

        if (rules == AsnEncodingRules.Aper
            || rules == AsnEncodingRules.Uper
            || rules == AsnEncodingRules.CanonicalAlignedPer
            || rules == AsnEncodingRules.CanonicalUnalignedPer)
        {
            return this.Per;
        }

        if (rules == AsnEncodingRules.Oer || rules == AsnEncodingRules.Coer)
        {
            return this.Oer;
        }

        if (rules == AsnEncodingRules.BasicXer
            || rules == AsnEncodingRules.CanonicalXer
            || rules == AsnEncodingRules.ExtendedXer)
        {
            return this.Xer;
        }

        return this.Other;
    }
}
