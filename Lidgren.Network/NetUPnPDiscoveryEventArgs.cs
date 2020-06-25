using System;

namespace Lidgren.Network
{
    public class NetUPnPDiscoveryEventArgs : EventArgs
    {
        public TimeSpan DiscoveryStartTime { get; }
        public TimeSpan DiscoveryEndTime { get; }
        public TimeSpan DiscoveryDuration => DiscoveryEndTime - DiscoveryStartTime;

        public NetUPnPDiscoveryEventArgs(TimeSpan discoveryStartTime, TimeSpan discoveryEndTime)
        {
            DiscoveryStartTime = discoveryStartTime;
            DiscoveryEndTime = discoveryEndTime;
        }
    }
}
