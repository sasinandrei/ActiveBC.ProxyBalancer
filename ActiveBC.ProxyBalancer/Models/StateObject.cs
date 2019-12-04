using System.Net.Sockets;

namespace ActiveBC.ProxyBalancer.Models
{
    public class StateObject
    {
        public Socket ClientSocket { get; set; }

        public Socket ServerSocket { get; set; }

        public byte[] Buffer { get; set; }

        public byte[] ServerBuffer { get; set; }

        public string Url { get; set; }

        public const int BufferSize = 2048;

        public StateObject()
        {
            Buffer = new byte[BufferSize];
            ServerBuffer = new byte[BufferSize];
        }
    }
}
