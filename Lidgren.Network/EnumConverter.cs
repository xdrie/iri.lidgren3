using System;
using System.Linq.Expressions;

namespace Lidgren.Network
{
    public static class EnumConverter
    {
        public static long Convert<TEnum>(TEnum value)
            where TEnum : Enum
        {
            return Helper<TEnum>.ConvertFrom(value);
        }

        public static TEnum Convert<TEnum>(long value)
            where TEnum : Enum
        {
            return Helper<TEnum>.ConvertTo(value);
        }

        [CLSCompliant(false)]
        public static TEnum Convert<TEnum>(ulong value)
            where TEnum : Enum
        {
            return Convert<TEnum>((long)value);
        }

        private static class Helper<TEnum>
        {
            public static Func<TEnum, long> ConvertFrom { get; } = GenerateFromConverter();
            public static Func<long, TEnum> ConvertTo { get; } = GenerateToConverter();

            private static Func<TEnum, long> GenerateFromConverter()
            {
                var parameter = Expression.Parameter(typeof(TEnum));
                var conversion = Expression.Convert(parameter, typeof(long));
                var method = Expression.Lambda<Func<TEnum, long>>(conversion, parameter);
                return method.Compile();
            }

            private static Func<long, TEnum> GenerateToConverter()
            {
                var parameter = Expression.Parameter(typeof(long));
                var conversion = Expression.Convert(parameter, typeof(TEnum));
                var method = Expression.Lambda<Func<long, TEnum>>(conversion, parameter);
                return method.Compile();
            }
        }
    }
}
