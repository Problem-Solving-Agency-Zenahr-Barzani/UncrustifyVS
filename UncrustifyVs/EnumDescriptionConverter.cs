using System;
using System.Globalization;
using System.ComponentModel;

namespace UncrustifyVs
{
    /// <summary>
    /// A converter for enums to strings based on their <see cref="DescriptionAttribute"/> attribute.
    /// </summary>
    public class EnumDescriptionConverter : EnumConverter
    {
        /// <inheritdoc />
        /// <param name="type">The enum type.</param>
        public EnumDescriptionConverter(Type type)
            : base(type)
        {
        }
        /// <inheritdoc />
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destType) => destType == typeof(string);
        /// <inheritdoc />
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destType)
        {
            var fieldInfo = EnumType.GetField(Enum.GetName(EnumType, value));
            var descriptionAttribute = Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute)) as DescriptionAttribute;
            return descriptionAttribute?.Description ?? value.ToString();
        }
        /// <inheritdoc />
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type srcType) => srcType == typeof(string);
        /// <inheritdoc />
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var stringValue = (string) value;
            foreach (var fieldInfo in EnumType.GetFields())
            {
                var descriptionAttribute = Attribute.GetCustomAttribute(fieldInfo, typeof(DescriptionAttribute)) as DescriptionAttribute;
                if (stringValue == descriptionAttribute?.Description)
                {
                    return Enum.Parse(EnumType, fieldInfo.Name);
                }
            }

            return null;
        }
    }
}
