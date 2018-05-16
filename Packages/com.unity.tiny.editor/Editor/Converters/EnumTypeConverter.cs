#if NET_4_6
using System;
using System.Collections.Generic;

using Unity.Properties;
using UnityEngine;

namespace Unity.Tiny.Conversions
{
    public class EnumTypeConverter<TEnum> : IConverterTo<TEnum>, IConverterFrom<TEnum>
        where TEnum : struct, IConvertible
    {
        private static readonly Dictionary<TEnum, int> s_ConvertedValueFrom = new Dictionary<TEnum, int>();
        private static readonly Dictionary<int, TEnum> s_ConvertedValueTo = new Dictionary<int, TEnum>();

        public EnumTypeConverter()
        {
            PopulateConversionValues();
        }

        public UTinyObject ConvertFrom(UTinyObject obj, TEnum value)
        {
            throw new InvalidOperationException($"{UTinyConstants.ApplicationName}: Cannot convert an enum from a UTinyObject.");
        }

        public object ConvertFrom(object obj, TEnum value)
        {
            var enumRef = (UTinyEnum.Reference)obj;
            ValidateType(enumRef);

            var enumType = enumRef.Type.Dereference(UTinyEditorApplication.Registry ?? ObjectConverter<TEnum>.TestRegistry);

            var defaultValue = enumType.DefaultValue as UTinyObject;
            var defaultContainer = defaultValue.Properties as IPropertyContainer;
            
            // Try to match by name.
            var prop = defaultContainer.PropertyBag.FindProperty(value.ToString());
            if (null == prop)
            {
                // Or by value.
                foreach(var property in defaultContainer.PropertyBag.Properties)
                {
                    int intValue;
                    if (s_ConvertedValueFrom.TryGetValue(value, out intValue) && (int)property.GetObjectValue(defaultContainer) == intValue)
                    {
                        prop = property;
                        break;
                    }
                }
            }

            if (null == prop)
            {
                throw new InvalidOperationException($"{UTinyConstants.ApplicationName}: Could not convert from type '{typeof(TEnum).Name}', no mapping from value {value} found.");
            }
            obj = new UTinyEnum.Reference(enumType, (int)prop.GetObjectValue(defaultContainer));
            return obj;
        }

        public TEnum ConvertTo(object obj)
        {
            var enumRef = (UTinyEnum.Reference)obj;
            ValidateType(enumRef);
            TEnum value;
            if (s_ConvertedValueTo.TryGetValue(enumRef.Value, out value))
            {
                return (TEnum)(object)value;
            }
            throw new InvalidOperationException($"{UTinyConstants.ApplicationName}: Could not convert to type '{typeof(TEnum).Name}', no mapping to value {enumRef.Value} found.");
        }

        protected virtual void PopulateConversionValues()
        {
            foreach(var value in EnumUtility.EnumValues<TEnum>())
            {
                Remap(value, (int)(object)value);
            }
        }

        protected void Remap(TEnum enumValue, int intValue)
        {
            s_ConvertedValueFrom[enumValue] = intValue;
            s_ConvertedValueTo[intValue] = enumValue;
        }

        private void ValidateType(UTinyEnum.Reference reference)
        {
            ValidateTypeEnumType();
            
            if (null == reference.Type.Dereference(UTinyEditorApplication.Registry ?? ObjectConverter<TEnum>.TestRegistry))
            {
                throw new ArgumentNullException("reference");
            }
        }

        private void ValidateTypeEnumType()
        {
            if (!typeof(TEnum).IsEnum)
            {
                throw new InvalidOperationException($"{UTinyConstants.ApplicationName}: TEnum must be an Enum type.");
            }
        }
    }

    public class GradientModeConverter : EnumTypeConverter<GradientMode> { }

    public class CameraClearFlagsConverter : EnumTypeConverter<CameraClearFlags>
    {
        protected override void PopulateConversionValues()
        {
            Remap(CameraClearFlags.Depth, 0);
            Remap(CameraClearFlags.Nothing, 0);
            Remap(CameraClearFlags.Skybox, 1);
            Remap(CameraClearFlags.Color, 1);
            Remap(CameraClearFlags.SolidColor, 1);
        }
    }

    public class TileModeConverter : EnumTypeConverter<TileMode> { }

    public class TextAnchorConverter : EnumTypeConverter<TextAnchor> { }
}
#endif // NET_4_6
