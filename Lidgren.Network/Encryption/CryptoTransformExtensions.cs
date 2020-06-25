using System;
using System.Security.Cryptography;

namespace Lidgren.Network
{
    public static class CryptoTransformExtensions
    {
        public static bool IsReusable(this ICryptoTransform transform)
        {
            if (transform == null)
                throw new ArgumentNullException(nameof(transform));

            return transform.CanReuseTransform && transform.CanTransformMultipleBlocks;
        }
    }
}
