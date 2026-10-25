using STTech.BytesIO.Bluetooth.Common;
using System;
using Xunit;

namespace STTech.BytesIO.Bluetooth.Tests
{
    public class BluetoothAddressHelperTests
    {
        [Fact]
        public void Format_ShouldFormatNumericAddressCorrectly()
        {
            ulong address = 0x001A7DDA7113;
            var formatted = BluetoothAddressHelper.Format(address);
            Assert.Equal("00:1A:7D:DA:71:13", formatted);

            var hyphenFormatted = BluetoothAddressHelper.Format(address, "-");
            Assert.Equal("00-1A-7D-DA-71-13", hyphenFormatted);
        }

        [Theory]
        [InlineData("00:1A:7D:DA:71:13", 0x001A7DDA7113UL)]
        [InlineData("00-1A-7D-DA-71-13", 0x001A7DDA7113UL)]
        [InlineData("001A7DDA7113", 0x001A7DDA7113UL)]
        [InlineData("FF:EE:DD:CC:BB:AA", 0xFFEEDDCCBBAAUL)]
        public void Parse_ShouldParseValidMacStrings(string macString, ulong expected)
        {
            var parsed = BluetoothAddressHelper.Parse(macString);
            Assert.Equal(expected, parsed);

            var success = BluetoothAddressHelper.TryParse(macString, out var tryParsed);
            Assert.True(success);
            Assert.Equal(expected, tryParsed);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData(null)]
        [InlineData("invalid-mac")]
        [InlineData("00:1A:7D:DA:71:13:99")] // too long
        public void TryParse_ShouldReturnFalseForInvalidInput(string? invalidString)
        {
            var success = BluetoothAddressHelper.TryParse(invalidString, out var address);
            Assert.False(success);
            Assert.Equal(0UL, address);
        }

        [Fact]
        public void Parse_ShouldThrowFormatExceptionOnInvalidInput()
        {
            Assert.Throws<FormatException>(() => BluetoothAddressHelper.Parse("invalid"));
        }
    }
}
