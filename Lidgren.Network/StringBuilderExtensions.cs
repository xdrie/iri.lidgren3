using System;
using System.Text;

namespace Lidgren.Network
{
    internal static class StringBuilderExtensions
    {
        public static StringBuilder AppendFormatLine(
            this StringBuilder builder, IFormatProvider? provider, string format, object arg0)
        {
            return builder.AppendFormat(provider, format, arg0).AppendLine();
        }

        public static StringBuilder AppendFormatLine(
            this StringBuilder builder, IFormatProvider? provider, string format, object arg0, object arg1)
        {
            return builder.AppendFormat(provider, format, arg0, arg1).AppendLine();
        }

        public static StringBuilder AppendFormatLine(
            this StringBuilder builder, IFormatProvider? provider, string format, object arg0, object arg1, object arg2)
        {
            return builder.AppendFormat(provider, format, arg0, arg1, arg2).AppendLine();
        }

        public static StringBuilder AppendFormatLine(
            this StringBuilder builder, IFormatProvider? provider, string format, params object[] args)
        {
            return builder.AppendFormat(provider, format, args).AppendLine();
        }

        public static StringBuilder AppendFormatLine(
            this StringBuilder builder, string format, object arg0)
        {
            return builder.AppendFormatLine(null, format, arg0);
        }

        public static StringBuilder AppendFormatLine(
            this StringBuilder builder, string format, object arg0, object arg1)
        {
            return builder.AppendFormatLine(null, format, arg0, arg1);
        }

        public static StringBuilder AppendFormatLine(
            this StringBuilder builder, string format, object arg0, object arg1, object arg2)
        {
            return builder.AppendFormatLine(null, format, arg0, arg1, arg2);
        }

        public static StringBuilder AppendFormatLine(
            this StringBuilder builder, string format, params object[] args)
        {
            return builder.AppendFormatLine(null, format, args);
        }
    }
}
