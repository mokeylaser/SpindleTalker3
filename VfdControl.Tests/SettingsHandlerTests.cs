using Xunit;
using VfdControl;

namespace VfdControl.Tests
{
    public class SettingsHandlerTests
    {
        [Fact]
        public void Convert_ValidLines_ReturnsRegisterValues()
        {
            var handler = new SettingsHandler();
            var lines = new List<string>
            {
                "0,0,0,byte,\"Parameter read only mode\",enum",
                "4,50,50,intX100,\"Base Frequency\",Hz"
            };

            var result = handler.Convert(lines, ',');

            Assert.NotNull(result);
            Assert.Equal(2, result.Count);
            Assert.Equal(0, result[0].ID);
            Assert.Equal("0", result[0].Value);
            Assert.Equal(4, result[1].ID);
            Assert.Equal("50", result[1].Value);
        }

        [Fact]
        public void Convert_InvalidColumnCount_ReturnsNull()
        {
            var handler = new SettingsHandler();
            var lines = new List<string>
            {
                "0,0,0" // Only 3 columns instead of 6
            };

            var result = handler.Convert(lines, ',');

            Assert.Null(result);
        }

        [Fact]
        public void Convert_EmptyList_ReturnsEmptyList()
        {
            var handler = new SettingsHandler();
            var lines = new List<string>();

            var result = handler.Convert(lines, ',');

            Assert.NotNull(result);
            Assert.Empty(result);
        }

        [Fact]
        public void Convert_DifferentSeparator_Works()
        {
            var handler = new SettingsHandler();
            var lines = new List<string>
            {
                "0;0;0;byte;\"Parameter read only mode\";enum"
            };

            var result = handler.Convert(lines, ';');

            Assert.NotNull(result);
            Assert.Single(result);
            Assert.Equal(0, result[0].ID);
        }

        [Fact]
        public void Convert_SecondLineInvalid_ReturnsNull()
        {
            var handler = new SettingsHandler();
            var lines = new List<string>
            {
                "0,0,0,byte,\"Parameter read only mode\",enum",
                "invalid,line" // Only 2 columns
            };

            var result = handler.Convert(lines, ',');

            Assert.Null(result);
        }
    }
}
