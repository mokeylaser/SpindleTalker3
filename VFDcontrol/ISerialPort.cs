using System;
using System.IO.Ports;

namespace VfdControl
{
    /// <summary>
    /// Abstraction over System.IO.Ports.SerialPort for testability.
    /// </summary>
    public interface ISerialPort : IDisposable
    {
        int BaudRate { get; set; }
        int DataBits { get; set; }
        StopBits StopBits { get; set; }
        Parity Parity { get; set; }
        string PortName { get; set; }
        bool IsOpen { get; }
        int BytesToRead { get; }

        void Open();
        void Close();
        void Write(byte[] buffer, int offset, int count);
        int Read(byte[] buffer, int offset, int count);
        void DiscardInBuffer();

        event SerialDataReceivedEventHandler DataReceived;
    }
}
