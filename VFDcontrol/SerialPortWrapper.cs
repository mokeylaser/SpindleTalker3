using System;
using System.IO.Ports;

namespace VfdControl
{
    /// <summary>
    /// Thin wrapper around System.IO.Ports.SerialPort that implements ISerialPort.
    /// Used in production; allows mocking in tests.
    /// </summary>
    public class SerialPortWrapper : ISerialPort
    {
        private readonly SerialPort _port;

        public SerialPortWrapper()
        {
            _port = new SerialPort();
        }

        public int BaudRate
        {
            get => _port.BaudRate;
            set => _port.BaudRate = value;
        }

        public int DataBits
        {
            get => _port.DataBits;
            set => _port.DataBits = value;
        }

        public StopBits StopBits
        {
            get => _port.StopBits;
            set => _port.StopBits = value;
        }

        public Parity Parity
        {
            get => _port.Parity;
            set => _port.Parity = value;
        }

        public string PortName
        {
            get => _port.PortName;
            set => _port.PortName = value;
        }

        public bool IsOpen => _port.IsOpen;

        public int BytesToRead => _port.BytesToRead;

        public void Open() => _port.Open();
        public void Close() => _port.Close();
        public void Write(byte[] buffer, int offset, int count) => _port.Write(buffer, offset, count);
        public int Read(byte[] buffer, int offset, int count) => _port.Read(buffer, offset, count);
        public void DiscardInBuffer() => _port.DiscardInBuffer();

        public event SerialDataReceivedEventHandler DataReceived
        {
            add => _port.DataReceived += value;
            remove => _port.DataReceived -= value;
        }

        public void Dispose()
        {
            _port.Dispose();
        }
    }
}
