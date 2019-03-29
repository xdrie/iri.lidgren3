using System.Security.Cryptography;

namespace Lidgren.Network
{
    public static class CryptoTransformExtensions
    {
        public static bool IsReusable(this ICryptoTransform transform)
        {
            return transform.CanReuseTransform && transform.CanTransformMultipleBlocks;
        }
    }
}
