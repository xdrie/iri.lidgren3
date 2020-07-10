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

namespace Lidgren.Network
{
    /// <summary>
    /// Behaviour of unreliable sends above MTU.
    /// </summary>
    public enum NetUnreliableSizeBehaviour
    {
        /// <summary>
        /// Sending an unreliable message will ignore MTU and send
        /// everything in a single packet.
        /// </summary>
        IgnoreMTU = 0,

        /// <summary>
        /// Use normal fragmentation for unreliable messages.
        /// If a fragment is dropped, memory for received fragments is never reclaimed.
        /// </summary>
        NormalFragmentation = 1,

        /// <summary>
        /// Alternate behaviour; just drops unreliable messages above MTU.
        /// </summary>
        DropAboveMTU = 2,
    }
}
