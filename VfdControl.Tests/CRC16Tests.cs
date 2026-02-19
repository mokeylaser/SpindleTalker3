using Xunit;
using VfdControl;

namespace VfdControl.Tests
{
    public class CRC16Tests
    {
        private HYmodbus CreateModbus() => new HYmodbus("COM1");

        [Fact]
        public void Crc16byte_AppendsTwo_CRC_Bytes()
        {
            var modbus = CreateModbus();
            byte[] input = new byte[] { 0x01, 0x03, 0x01, 0x00, 0x00, 0x00 };
            byte[] result = modbus.crc16byte(input);

            Assert.Equal(input.Length + 2, result.Length);
            // Original bytes are preserved
            for (int i = 0; i < input.Length; i++)
                Assert.Equal(input[i], result[i]);
        }

        [Fact]
        public void Crc16byte_KnownModbusFrame_ProducesExpectedCRC()
        {
            var modbus = CreateModbus();

            // Standard Modbus RTU request: Read Holding Register
            // Slave 1, Function 0x03, Register 0, Count 1
            // Expected CRC for [01 03 00 00 00 01] is 84 0A
            byte[] input = new byte[] { 0x01, 0x03, 0x00, 0x00, 0x00, 0x01 };
            byte[] result = modbus.crc16byte(input);

            Assert.Equal(8, result.Length);
            Assert.Equal(0x84, result[6]);
            Assert.Equal(0x0A, result[7]);
        }

        [Fact]
        public void CRCCheck_ValidPacket_ReturnsTrue()
        {
            var modbus = CreateModbus();

            // Create a valid packet by signing it first
            byte[] raw = new byte[] { 0x01, 0x03, 0x01, 0x05, 0x00, 0x00 };
            byte[] signed = modbus.crc16byte(raw);

            Assert.True(modbus.CRCCheck(signed));
        }

        [Fact]
        public void CRCCheck_CorruptedPacket_ReturnsFalse()
        {
            var modbus = CreateModbus();

            byte[] raw = new byte[] { 0x01, 0x03, 0x01, 0x05, 0x00, 0x00 };
            byte[] signed = modbus.crc16byte(raw);

            // Corrupt a byte
            signed[3] = 0xFF;

            Assert.False(modbus.CRCCheck(signed));
        }

        [Fact]
        public void CRCCheck_RoundTrip_SignThenVerify()
        {
            var modbus = CreateModbus();

            // Test various frame sizes
            byte[][] frames = new byte[][]
            {
                new byte[] { 0x01, 0x04, 0x01, 0x00 },
                new byte[] { 0x01, 0x03, 0x03, 0x0A, 0x00, 0x64 },
                new byte[] { 0x02, 0x05, 0x02, 0x03, 0xE8 },
            };

            foreach (var frame in frames)
            {
                byte[] signed = modbus.crc16byte(frame);
                Assert.True(modbus.CRCCheck(signed), $"CRC round-trip failed for frame of length {frame.Length}");
            }
        }

        [Fact]
        public void Crc16byte_SingleByte_Works()
        {
            var modbus = CreateModbus();
            byte[] input = new byte[] { 0x01 };
            byte[] result = modbus.crc16byte(input);

            Assert.Equal(3, result.Length);
            Assert.Equal(0x01, result[0]);
            // CRC bytes should be non-zero for this input
            Assert.True(modbus.CRCCheck(result));
        }

        [Fact]
        public void Crc16byte_DifferentInputs_ProduceDifferentCRCs()
        {
            var modbus = CreateModbus();

            byte[] input1 = new byte[] { 0x01, 0x03, 0x01, 0x00 };
            byte[] input2 = new byte[] { 0x01, 0x03, 0x01, 0x01 };

            byte[] result1 = modbus.crc16byte(input1);
            byte[] result2 = modbus.crc16byte(input2);

            // CRC bytes should differ
            Assert.False(result1[^2] == result2[^2] && result1[^1] == result2[^1],
                "Different inputs should produce different CRCs");
        }
    }
}
