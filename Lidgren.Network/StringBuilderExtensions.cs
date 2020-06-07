using System.Text;

namespace Lidgren.Network
{
    internal static class StringBuilderExtensions
    {
        public static StringBuilder AppendFormatLine(
               this StringBuilder builder, string format, object arg0)
        {
            builder.AppendFormat(format, arg0);
            builder.AppendLine();
            return builder;
        }

        public static StringBuilder AppendFormatLine(
            this StringBuilder builder, string format, params object[] args)
        {
            builder.AppendFormat(format, args);
            builder.AppendLine();
            return builder;
        }
    }
}
