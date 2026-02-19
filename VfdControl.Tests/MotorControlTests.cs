using Xunit;
using VfdControl;

namespace VfdControl.Tests
{
    public class MotorControlTests
    {
        [Fact]
        public void Constructor_ValidParameters_DoesNotThrow()
        {
            var mc = new MotorControl("COM1", 38400, 8, 0, 1, 1, 100);
            Assert.NotNull(mc._hyModbus);
        }

        [Fact]
        public void Constructor_InvalidParity_TooLow_Throws()
        {
            var ex = Assert.Throws<Exception>(() =>
                new MotorControl("COM1", 38400, 8, -1, 1, 1, 100));
            Assert.Contains("Parity", ex.Message);
        }

        [Fact]
        public void Constructor_InvalidParity_TooHigh_Throws()
        {
            var ex = Assert.Throws<Exception>(() =>
                new MotorControl("COM1", 38400, 8, 5, 1, 1, 100));
            Assert.Contains("Parity", ex.Message);
        }

        [Fact]
        public void Constructor_ParityBoundary_FourIsValid()
        {
            // Parity 4 = Space, which is valid
            var mc = new MotorControl("COM1", 38400, 8, 4, 1, 1, 100);
            Assert.NotNull(mc._hyModbus);
        }

        [Fact]
        public void Constructor_InvalidStopBits_TooLow_Throws()
        {
            var ex = Assert.Throws<Exception>(() =>
                new MotorControl("COM1", 38400, 8, 0, -1, 1, 100));
            Assert.Contains("StopBits", ex.Message);
        }

        [Fact]
        public void Constructor_InvalidStopBits_TooHigh_Throws()
        {
            // This test validates the bug fix: previously parity > 3 was checked
            // instead of stopBits > 3. With the fix, stopBits = 4 should throw.
            var ex = Assert.Throws<Exception>(() =>
                new MotorControl("COM1", 38400, 8, 0, 4, 1, 100));
            Assert.Contains("StopBits", ex.Message);
        }

        [Fact]
        public void Constructor_StopBitsBoundary_ThreeIsValid()
        {
            var mc = new MotorControl("COM1", 38400, 8, 0, 3, 1, 100);
            Assert.NotNull(mc._hyModbus);
        }

        [Fact]
        public void Constructor_StopBitsZero_IsValid()
        {
            var mc = new MotorControl("COM1", 38400, 8, 0, 0, 1, 100);
            Assert.NotNull(mc._hyModbus);
        }

        [Fact]
        public void ParityNames_ReturnsValidArray()
        {
            var mc = new MotorControl("COM1");
            var names = mc.ParityNames;

            Assert.NotNull(names);
            Assert.Contains("None", names);
            Assert.Contains("Even", names);
            Assert.Contains("Odd", names);
        }

        [Fact]
        public void StopBitNames_ReturnsValidArray()
        {
            var mc = new MotorControl("COM1");
            var names = mc.StopBitNames;

            Assert.NotNull(names);
            Assert.Contains("One", names);
            Assert.Contains("Two", names);
        }

        [Fact]
        public void GetLocalComPorts_ReturnsArray()
        {
            var mc = new MotorControl("COM1");
            var ports = mc.GetLocalComPorts();

            // May return empty on CI/test machines, but should not throw
            Assert.NotNull(ports);
        }

        [Fact]
        public void Constructor_SetsHYmodbusProperties()
        {
            var mc = new MotorControl("COM3", 9600, 7, 2, 2, 5, 200);

            Assert.Equal("COM3", mc._hyModbus.PortName);
            Assert.Equal(9600, mc._hyModbus.BaudRate);
            Assert.Equal(7, mc._hyModbus.DataBits);
            Assert.Equal(2, mc._hyModbus.Parity);
            Assert.Equal(2, mc._hyModbus.StopBits);
            Assert.Equal(5, mc._hyModbus.ModBusID);
            Assert.Equal(200, mc._hyModbus.ResponseWaitTimeout);
        }

        [Fact]
        public void OnSpindleShuttingDown_EventCanBeSubscribed()
        {
            var mc = new MotorControl("COM1");
            bool eventFired = false;
            mc.OnSpindleShuttingDown += (stop) => eventFired = true;

            // We can't easily trigger Stop() without a connected port,
            // but we can verify the event subscription doesn't throw
            Assert.False(eventFired);
        }
    }
}
