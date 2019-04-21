using System;
using System.ComponentModel;

namespace Common
{
    public class EnumHelper
    {
        public static string GetDescription(Enum enumeration)
        {
            string enumerationString = enumeration.ToString();
            var members = enumeration.GetType().GetMember(enumerationString);
            var attributes = members[0]?.GetCustomAttributes(typeof(DescriptionAttribute), false);
            return ((DescriptionAttribute)attributes[0])?.Description ?? enumerationString;
        }
    }
}
