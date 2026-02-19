using Xunit;
using VfdControl;

namespace VfdControl.Tests
{
    public class VFDdataTests
    {
        [Fact]
        public void OnChanged_FiresWhenMaxFreqSet()
        {
            var data = new VFDdata();
            bool fired = false;
            data.OnChanged += _ => fired = true;

            data.MaxFreq = 50.0;

            Assert.True(fired);
        }

        [Fact]
        public void OnChanged_FiresWhenMaxRPMSet()
        {
            var data = new VFDdata();
            bool fired = false;
            data.OnChanged += _ => fired = true;

            data.MaxRPM = 24000;

            Assert.True(fired);
        }

        [Fact]
        public void OnChanged_FiresWhenLowerLevelFreqSet()
        {
            var data = new VFDdata();
            bool fired = false;
            data.OnChanged += _ => fired = true;

            data.LowerLevelFreq = 10.0;

            Assert.True(fired);
        }

        [Fact]
        public void OnChanged_FiresWhenBaseFreqSet()
        {
            var data = new VFDdata();
            bool fired = false;
            data.OnChanged += _ => fired = true;

            data.BaseFreq = 50.0;

            Assert.True(fired);
        }

        [Fact]
        public void OnSerialPortConnected_FiresWhenSerialConnectedSet()
        {
            var data = new VFDdata();
            bool? receivedValue = null;
            data.OnSerialPortConnected += v => receivedValue = v;

            data.SerialConnected = true;

            Assert.True(receivedValue);
        }

        [Fact]
        public void MinRPM_CalculatedCorrectly()
        {
            var data = new VFDdata();
            data.MaxFreq = 400.0;
            data.MaxRPM = 24000;
            data.LowerLevelFreq = 100.0;

            // MinRPM = (MaxRPM / MaxFreq) * LowerLevelFreq = (24000 / 400) * 100 = 6000
            Assert.Equal(6000, data.MinRPM);
        }

        [Fact]
        public void MinRPM_ReturnsZeroWhenMaxFreqIsZero()
        {
            var data = new VFDdata();
            data.MaxFreq = 0;
            data.MaxRPM = 24000;
            data.LowerLevelFreq = 100.0;

            Assert.Equal(0, data.MinRPM);
        }

        [Fact]
        public void MinRPM_ReturnsZeroWhenMaxRPMIsZero()
        {
            var data = new VFDdata();
            data.MaxFreq = 400.0;
            data.MaxRPM = 0;
            data.LowerLevelFreq = 100.0;

            Assert.Equal(0, data.MinRPM);
        }

        [Fact]
        public void InitDataOK_ReturnsTrueWhenAllValid()
        {
            var data = new VFDdata();
            data.MaxFreq = 400.0;
            data.LowerLevelFreq = 100.0;
            data.MaxRPM = 24000;
            data.OutRPM = 0;

            Assert.True(data.InitDataOK());
        }

        [Fact]
        public void InitDataOK_ReturnsFalseWhenMaxFreqNegative()
        {
            var data = new VFDdata();
            data.MaxFreq = -1;
            data.LowerLevelFreq = 100.0;
            data.MaxRPM = 24000;
            data.OutRPM = 0;

            Assert.False(data.InitDataOK());
        }

        [Fact]
        public void InitDataOK_ReturnsFalseWhenMaxRPMNegative()
        {
            var data = new VFDdata();
            data.MaxFreq = 400.0;
            data.LowerLevelFreq = 0;
            data.MaxRPM = -1;
            data.OutRPM = 0;

            Assert.False(data.InitDataOK());
        }

        [Fact]
        public void Clear_ResetsCriticalFieldsToNegativeOne()
        {
            var data = new VFDdata();
            data.MaxFreq = 400.0;
            data.LowerLevelFreq = 100.0;
            data.MaxRPM = 24000;

            data.Clear();

            Assert.Equal(-1, data.MaxFreq);
            Assert.Equal(-1, data.LowerLevelFreq);
            Assert.Equal(-1, data.MaxRPM);
        }

        [Fact]
        public void Clear_CausesInitDataOKToReturnFalse()
        {
            var data = new VFDdata();
            data.MaxFreq = 400.0;
            data.LowerLevelFreq = 100.0;
            data.MaxRPM = 24000;
            data.OutRPM = 0;
            Assert.True(data.InitDataOK());

            data.Clear();
            Assert.False(data.InitDataOK());
        }

        [Fact]
        public void OutRPM_SetsTimestamp()
        {
            var data = new VFDdata();
            var before = DateTime.UtcNow;

            data.OutRPM = 1000;

            var after = DateTime.UtcNow;
            Assert.InRange(data.TimeStamp, before, after);
        }

        [Fact]
        public void ToString_ContainsKeyValues()
        {
            var data = new VFDdata();
            data.SetFrequency = 50.0;
            data.OutFrequency = 49.5;
            data.OutRPM = 1440;
            data.OutAmp = 2.5;

            string result = data.ToString();

            Assert.Contains("Set Freq: 50", result);
            Assert.Contains("RPM: 1440", result);
        }

        [Fact]
        public void GetValues_ReturnsSeven_Values()
        {
            var data = new VFDdata();
            data.SetFrequency = 1;
            data.OutFrequency = 2;
            data.OutAmp = 3;
            data.OutRPM = 4;
            data.OutVoltDC = 5;
            data.OutVoltAC = 6;
            data.OutTemperature = 7;

            var values = data.GetValues();

            Assert.Equal(7, values.Count);
            Assert.Equal(1, values[0]);
            Assert.Equal(4, values[3]);
            Assert.Equal(7, values[6]);
        }
    }
}
