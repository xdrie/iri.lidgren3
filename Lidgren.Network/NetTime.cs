/* Copyright (c) 2010 Michael Lidgren

Permission is hereby granted, free of charge, to any person obtaining a copy of this software
and associated documentation files (the "Software"), to deal in the Software without
restriction, including without limitation the rights to use, copy, modify, merge, publish,
distribute, sublicense, and/or sell copies of the Software, and to permit persons to whom
the Software is furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all copies or
substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE
USE OR OTHER DEALINGS IN THE SOFTWARE.

*/

using System;
using System.Diagnostics;
using System.Globalization;

namespace Lidgren.Network
{
    /// <summary>
    /// Time service
    /// </summary>
    public static class NetTime
    {
        private static readonly long _timeInitialized = Stopwatch.GetTimestamp();

        /// <summary>
        /// Get the amount of time elapsed since the application started.
        /// </summary>
        public static TimeSpan Now => TimeSpan.FromTicks(Stopwatch.GetTimestamp() - _timeInitialized);

        /// <summary>
        /// Given seconds it will output a human friendly readable string
        /// (milliseconds if less than 10 seconds).
        /// </summary>
        public static string ToReadable(double seconds)
        {
            var culture = CultureInfo.CurrentCulture;

            if (seconds >= 10)
                return TimeSpan.FromSeconds(seconds).ToString(null, culture);

            return (seconds * 1000.0).ToString("N2", culture) + " ms";
        }

        /// <summary>
        /// Given time it will output a human friendly readable string
        /// (milliseconds if less than 10 seconds).
        /// </summary>
        public static string ToReadable(TimeSpan time)
        {
            var culture = CultureInfo.CurrentCulture;

            if (time.TotalSeconds >= 10)
                return time.ToString(null, culture);

            return time.TotalMilliseconds.ToString("N2", culture) + " ms";
        }
    }
}