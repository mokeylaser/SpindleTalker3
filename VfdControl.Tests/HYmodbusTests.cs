using Xunit;
using VfdControl;

namespace VfdControl.Tests
{
    public class HYmodbusTests
    {
        private HYmodbus CreateModbus(int modBusID = 1) =>
            new HYmodbus("COM1", modBusID: modBusID);

        [Theory]
        [InlineData(0x03, 6)]  // ReadControlData response
        [InlineData(0x05, 7)]  // WriteControlData response
        [InlineData(0x01, 8)]  // FunctionRead response (default)
        [InlineData(0x02, 8)]  // FunctionWrite response (default)
        [InlineData(0xFF, 8)]  // Unknown (default)
        public void GetResponseLength_ReturnsCorrectLength(byte commandByte, int expectedLength)
        {
            var modbus = CreateModbus();
            Assert.Equal(expectedLength, modbus.GetResponseLength(commandByte));
        }

        [Fact]
        public void Constructor_SetsDefaultProperties()
        {
            var modbus = new HYmodbus("COM3", 9600, 7, 2, 2, 5, 200);

            Assert.Equal("COM3", modbus.PortName);
            Assert.Equal(9600, modbus.BaudRate);
            Assert.Equal(7, modbus.DataBits);
            Assert.Equal(2, modbus.Parity);
            Assert.Equal(2, modbus.StopBits);
            Assert.Equal(5, modbus.ModBusID);
            Assert.Equal(200, modbus.ResponseWaitTimeout);
            Assert.False(modbus.ComOpen);
        }

        [Fact]
        public void Constructor_DefaultRetryConfiguration()
        {
            var modbus = CreateModbus();

            Assert.Equal(3, modbus.MaxRetries);
            Assert.Equal(100, modbus.RetryBaseDelayMs);
            Assert.Equal(2000, modbus.RetryMaxDelayMs);
        }

        [Fact]
        public void RetryConfiguration_CanBeModified()
        {
            var modbus = CreateModbus();
            modbus.MaxRetries = 5;
            modbus.RetryBaseDelayMs = 200;
            modbus.RetryMaxDelayMs = 5000;

            Assert.Equal(5, modbus.MaxRetries);
            Assert.Equal(200, modbus.RetryBaseDelayMs);
            Assert.Equal(5000, modbus.RetryMaxDelayMs);
        }

        [Fact]
        public void VFDData_IsInitialized()
        {
            var modbus = CreateModbus();
            Assert.NotNull(modbus.VFDData);
        }

        [Fact]
        public void ComOpen_InitiallyFalse()
        {
            var modbus = CreateModbus();
            Assert.False(modbus.ComOpen);
        }

        [Fact]
        public void DownloadUploadMode_InitiallyFalse()
        {
            var modbus = CreateModbus();
            Assert.False(modbus.DownloadUploadMode);
        }

        [Fact]
        public void ISerialPort_Constructor_SetsPort()
        {
            // Just verify the constructor overload works
            var modbus = new HYmodbus((ISerialPort?)null, modBusID: 2, responseWaitTimeout: 500);

            Assert.Equal(2, modbus.ModBusID);
            Assert.Equal(500, modbus.ResponseWaitTimeout);
        }

        [Fact]
        public void SendDataAsync_DoesNotThrowWithoutConnection()
        {
            var modbus = CreateModbus();
            byte[] packet = new byte[] { 0x01, 0x03, 0x01, 0x00, 0x00, 0x00 };

            // SendDataAsync adds CRC and enqueues — should not throw even without connection
            modbus.SendDataAsync(packet);
        }

        [Fact]
        public void SendData_ReturnsZeroWhenNoWorkerRunning()
        {
            var modbus = CreateModbus();
            byte[] packet = new byte[] { 0x01, 0x03, 0x01, 0x00, 0x00, 0x00 };

            // Without a connected worker, SendData should time out and return 0
            int result = modbus.SendData(packet);
            Assert.Equal(0, result);
        }

        [Fact]
        public void StartPolling_SetsDownloadUploadModeFalse()
        {
            var modbus = CreateModbus();
            modbus.DownloadUploadMode = true;

            modbus.StartPolling();

            Assert.False(modbus.DownloadUploadMode);
        }

        [Fact]
        public void OnWriteLog_CanBeSubscribed()
        {
            var modbus = CreateModbus();
            string? logMessage = null;
            bool? wasError = null;

            modbus.OnWriteLog += (msg, err) =>
            {
                logMessage = msg;
                wasError = err;
            };

            // Event subscribed successfully — we can't trigger log easily
            // without a connection, but the subscription shouldn't throw
            Assert.Null(logMessage);
        }

        [Fact]
        public void OnWriteTerminalForm_CanBeSubscribed()
        {
            var modbus = CreateModbus();
            modbus.OnWriteTerminalForm += (msg, send) => { };
            // Should not throw
        }

        [Fact]
        public void OnProcessPollPacket_CanBeSubscribed()
        {
            var modbus = CreateModbus();
            modbus.OnProcessPollPacket += (data) => { };
            // Should not throw
        }

        [Fact]
        public void SendCommand_WithCommandType_CreatesRegisterValue()
        {
            var modbus = CreateModbus();

            // This will time out on SendData (no worker) and return value=0
            var result = modbus.SendCommand(CommandType.FunctionRead, 0x03, 100);

            Assert.NotNull(result);
            Assert.Equal(0x03, result.ID);
            Assert.Equal("0", result.Value);
        }

        [Fact]
        public void CRCCheck_OnHYmodbusInstance_WorksCorrectly()
        {
            var modbus = CreateModbus();

            byte[] raw = new byte[] { 0x01, 0x04, 0x01, 0x00, 0x00, 0x00 };
            byte[] signed = modbus.crc16byte(raw);
            Assert.True(modbus.CRCCheck(signed));

            // Tamper with a byte
            signed[2] ^= 0xFF;
            Assert.False(modbus.CRCCheck(signed));
        }
    }
}
