
namespace Lidgren.Network
{
    public interface IBitBuffer
    {
        int BitPosition { get; set; }
        int BitLength { get; set; }
    }
}
