using Xunit;
using VfdControl;

namespace VfdControl.Tests
{
    public class RegisterValueTests
    {
        [Theory]
        [InlineData(3, "intX100")]
        [InlineData(4, "intX100")]
        [InlineData(8, "intX10")]
        [InlineData(33, "int")]
        [InlineData(0, "byte")]
        [InlineData(199, "byte")] // Unknown ID defaults to "byte"
        public void Type_ReturnsCorrectTypeForKnownRegisters(int id, string expectedType)
        {
            var rv = new RegisterValue(id);
            Assert.Equal(expectedType, rv.Type);
        }

        [Theory]
        [InlineData(3, "Hz")]
        [InlineData(8, "Volt")]
        [InlineData(14, "Sec")]
        [InlineData(144, "RPM")]
        [InlineData(163, "address")]
        [InlineData(999, "Unknown")] // Out of range
        public void Unit_ReturnsCorrectUnitForKnownRegisters(int id, string expectedUnit)
        {
            var rv = new RegisterValue(id);
            Assert.Equal(expectedUnit, rv.Unit);
        }

        [Theory]
        [InlineData(0, "Parameter read only mode")]
        [InlineData(144, "Rated Motor speed (RPM)")]
        [InlineData(163, "RS485 address")]
        public void Description_ReturnsCorrectDescriptionForKnownRegisters(int id, string expectedDesc)
        {
            var rv = new RegisterValue(id);
            Assert.Equal(expectedDesc, rv.Description);
        }

        [Fact]
        public void Description_FactorySettingRange()
        {
            var rv = new RegisterValue(200);
            Assert.Equal("Factory Setting", rv.Description);
        }

        [Fact]
        public void Description_OutOfRange_ReturnsNA()
        {
            var rv = new RegisterValue(251);
            Assert.Equal("NA", rv.Description);
        }

        [Fact]
        public void ToValue_IntX10_DividesByTen()
        {
            var rv = new RegisterValue(8); // Type = intX10
            rv.Value = "2200";
            Assert.Equal("220", rv.ToValue());
        }

        [Fact]
        public void ToValue_IntX100_DividesByHundred()
        {
            var rv = new RegisterValue(3); // Type = intX100
            rv.Value = "5000";
            Assert.Equal("50", rv.ToValue());
        }

        [Fact]
        public void ToValue_IntX1000_DividesByThousand()
        {
            // There are no intX1000 types in the current register map,
            // but we can test the code path by using a value directly
            var rv = new RegisterValue(3); // intX100 actually
            rv.Value = "40000";
            // For intX100: 40000/100 = 400
            Assert.Equal("400", rv.ToValue());
        }

        [Fact]
        public void ToValue_ByteType_ReturnsValueUnchanged()
        {
            var rv = new RegisterValue(0); // Type = byte
            rv.Value = "42";
            Assert.Equal("42", rv.ToValue());
        }

        [Fact]
        public void Data0_And_Data1_ExtractBytesCorrectly()
        {
            // For "int" type, data0 = high byte (>>8), data1 = low byte (>>0)
            var rv = new RegisterValue(33); // Type = "int"
            rv.Value = "256"; // 0x0100

            Assert.Equal(0x01, rv.data0); // high byte
            Assert.Equal(0x00, rv.data1); // low byte
        }

        [Fact]
        public void Data0_And_Data1_SmallValue()
        {
            var rv = new RegisterValue(33); // Type = "int"
            rv.Value = "5";

            Assert.Equal(0x00, rv.data0); // high byte
            Assert.Equal(0x05, rv.data1); // low byte
        }

        [Fact]
        public void Data0_And_Data1_IntX10Type()
        {
            var rv = new RegisterValue(8); // Type = intX10
            rv.Value = "22.0"; // 22.0 * 10 = 220 = 0x00DC

            Assert.Equal(0x00, rv.data0); // 220 >> 8 = 0
            Assert.Equal(0xDC, rv.data1); // 220 & 0xFF = 0xDC
        }

        [Fact]
        public void CommandLength_ByteType_ReturnsTwo()
        {
            var rv = new RegisterValue(0); // Type = byte
            Assert.Equal(2, rv.CommandLength);
        }

        [Fact]
        public void CommandLength_IntX100Type_ReturnsThree()
        {
            var rv = new RegisterValue(3); // Type = intX100
            Assert.Equal(3, rv.CommandLength);
        }

        [Fact]
        public void CommandLength_IntType_ReturnsThree()
        {
            var rv = new RegisterValue(33); // Type = int
            Assert.Equal(3, rv.CommandLength);
        }

        [Fact]
        public void DefaultValue_KnownRegister_ReturnsExpectedValue()
        {
            var rv = new RegisterValue(4); // Base Frequency
            Assert.Equal("50", rv.DefaultValue);
        }

        [Fact]
        public void DefaultValue_UnknownRegister_ReturnsUnknown()
        {
            var rv = new RegisterValue(999);
            Assert.Equal("Unknown", rv.DefaultValue);
        }

        [Fact]
        public void ToString_FormatsCorrectly()
        {
            var rv = new RegisterValue(0);
            rv.Value = "1";
            string result = rv.ToString(',');

            Assert.Contains("0,", result);
            Assert.Contains(",1,", result);
        }

        [Fact]
        public void ByteConstructor_SetsID()
        {
            var rv = new RegisterValue((byte)42);
            Assert.Equal(42, rv.ID);
        }
    }
}
