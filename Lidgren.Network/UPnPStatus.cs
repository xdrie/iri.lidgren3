namespace Lidgren.Network
{
    /// <summary>
    /// Status of the UPnP capabilities
    /// </summary>
    public enum UPnPStatus
    {
        /// <summary>
        /// Still discovering UPnP capabilities
        /// </summary>
        Discovering,

        /// <summary>
        /// UPnP is not available
        /// </summary>
        NotAvailable,

        /// <summary>
        /// UPnP is available and ready to use
        /// </summary>
        Available
    }
}
