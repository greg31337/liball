using System;
using System.Collections.Generic;
using System.Text;

namespace email;
internal class EmailTnefConversionBudget
{
    int maximum;
    int count;

    internal EmailTnefConversionBudget(int maximum)
    {
        this.maximum = maximum;
        count = 0;
    }

    internal void AddEntity()
    {
        count++;
        if (count > maximum) throw new EmailLimitException("MaxTnefConvertedEntityCount", maximum, count);
    }
}
