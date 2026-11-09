using STTech.BytesIO.Bluetooth.Entity;
using System;
using System.Linq;
using Xunit;

namespace STTech.BytesIO.Bluetooth.Tests
{
    /// <summary>
    /// 针对另一 AI 审查指出的缺陷整改与产品级加固项专项单元测试
    /// </summary>
    public class BluetoothRectificationAndHardeningTests
    {
        [Theory]
        [InlineData("00001800-0000-1000-8000-00805F9B34FB", true)] // Generic Access (标准保留)
        [InlineData("00001801-0000-1000-8000-00805F9B34FB", true)] // Generic Attribute (标准保留)
        [InlineData("0000180A-0000-1000-8000-00805F9B34FB", true)] // Device Information (标准保留)
        [InlineData("0000180F-0000-1000-8000-00805F9B34FB", true)] // Battery Service (标准保留)
        [InlineData("000018FF-0000-1000-8000-00805F9B34FB", true)] // 保留上限边界
        [InlineData("000017FF-0000-1000-8000-00805F9B34FB", false)] // 超出保留下限
        [InlineData("00001900-0000-1000-8000-00805F9B34FB", false)] // 超出保留上限
        [InlineData("6E400001-B5A3-F393-E0A9-E50E24DCCA9E", false)] // Nordic UART 厂商自定义服务
        [InlineData("0000FFE0-0000-1000-8000-00805F9B34FB", false)] // TI 厂商自定义服务 (虽然基地址是 SIG，但短 ID 0xFFE0 不在保留范围)
        [InlineData("0000FFF0-0000-1000-8000-00805F9B34FB", false)] // 通用透传自定义服务
        public void IsBluetoothSigReservedService_ShouldCorrectlyIdentifyReservedUuids(string uuidStr, bool expectedIsReserved)
        {
            var guid = new Guid(uuidStr);
            bool actual = BleClient.IsBluetoothSigReservedService(guid);
            Assert.Equal(expectedIsReserved, actual);
        }

        [Fact]
        public void BleClient_WriteOption_ShouldDefaultToAuto()
        {
            var client = new BleClient();
            Assert.Equal(BleWriteOption.Auto, client.WriteOption);
        }

        [Theory]
        // Auto 模式：优先采用高速无响应；若硬件仅支持有响应则降级
        [InlineData(BleWriteOption.Auto, true, true, BleWriteOption.WriteWithoutResponse)]
        [InlineData(BleWriteOption.Auto, true, false, BleWriteOption.WriteWithoutResponse)]
        [InlineData(BleWriteOption.Auto, false, true, BleWriteOption.WriteWithResponse)]
        // WriteWithoutResponse 模式：若硬件不支持无响应，自动降级为带响应以杜绝 ArgumentException 崩溃
        [InlineData(BleWriteOption.WriteWithoutResponse, true, true, BleWriteOption.WriteWithoutResponse)]
        [InlineData(BleWriteOption.WriteWithoutResponse, true, false, BleWriteOption.WriteWithoutResponse)]
        [InlineData(BleWriteOption.WriteWithoutResponse, false, true, BleWriteOption.WriteWithResponse)]
        // WriteWithResponse 模式：若硬件不支持带响应，自动回退为无响应以保证通信可用
        [InlineData(BleWriteOption.WriteWithResponse, true, true, BleWriteOption.WriteWithResponse)]
        [InlineData(BleWriteOption.WriteWithResponse, false, true, BleWriteOption.WriteWithResponse)]
        [InlineData(BleWriteOption.WriteWithResponse, true, false, BleWriteOption.WriteWithoutResponse)]
        public void ResolveEffectiveWriteOption_ShouldAdaptivelySelectOptimalWriteOption(
            BleWriteOption userPreference,
            bool supportsWithoutResponse,
            bool supportsWithResponse,
            BleWriteOption expectedResolved)
        {
            var actual = BleClient.ResolveEffectiveWriteOption(userPreference, supportsWithoutResponse, supportsWithResponse);
            Assert.Equal(expectedResolved, actual);
        }

        [Fact]
        public void IBluetoothManager_ShouldAdhereToDependencyInversionPrinciple()
        {
            using var manager = new BluetoothManager();
            IBluetoothManager ifaceManager = manager;

            var deviceInfo = new BluetoothDeviceInfo
            {
                Address = 0x001A7DDA7113,
                AddressString = "00:1A:7D:DA:71:13",
                Name = "TestDevice",
                Type = BluetoothType.Ble
            };

            // 验证通过接口调用时返回的是抽象接口类型
            IBluetoothClient bthClient = ifaceManager.CreateBluetoothClient(deviceInfo);
            Assert.NotNull(bthClient);
            Assert.Equal(0x001A7DDA7113UL, bthClient.DeviceAddress);

            IBleClient bleClient = ifaceManager.CreateBleClient(deviceInfo);
            Assert.NotNull(bleClient);
            Assert.Equal(0x001A7DDA7113UL, bleClient.DeviceAddress);

            // 验证通过具体类调用时依然可以直接拿到具体类型（向下兼容性）
            BluetoothClient concreteBth = manager.CreateBluetoothClient(deviceInfo);
            Assert.NotNull(concreteBth);

            BleClient concreteBle = manager.CreateBleClient(deviceInfo);
            Assert.NotNull(concreteBle);
        }

        [Fact]
        public void BluetoothManager_GetDeviceAndClearDiscoveredDevices_ShouldWorkProperly()
        {
            using var manager = new BluetoothManager();
            var dummyDevice = new BluetoothDeviceInfo
            {
                Address = 0xAABBCCDDEEFF,
                AddressString = "AA:BB:CC:DD:EE:FF",
                Name = "TestDevice",
                Type = BluetoothType.Classic
            };

            // 测试空查询
            Assert.Null(manager.GetDevice(0xAABBCCDDEEFF));
            Assert.Null(manager.GetDevice("AA:BB:CC:DD:EE:FF"));

            // 测试清理
            manager.ClearDiscoveredDevices();
            Assert.Empty(manager.DiscoveredDevices);
        }

        [Fact]
        public void BluetoothScanOptions_Defaults_ShouldSupportUnpairedClassicDiscovery()
        {
            var options = new BluetoothScanOptions();
            Assert.Equal(BluetoothScanMode.All, options.Mode);
            Assert.True(options.ClassicScanUnpairedDevices, "默认应当开启经典蓝牙未配对空中发现，避免用户误解");
        }

        private class DummyDisposableNativeDevice : IDisposable
        {
            public bool IsDisposed { get; private set; }
            public void Dispose()
            {
                IsDisposed = true;
            }
        }

        [Fact]
        public void BluetoothManager_Dispose_ShouldDisposeCachedNativeDevices()
        {
            var disposable = new DummyDisposableNativeDevice();
            var manager = new BluetoothManager();

            // 模拟将含有 NativeDevice 的设备置入管理器
            var device = new BluetoothDeviceInfo
            {
                Address = 0x112233445566,
                AddressString = "11:22:33:44:55:66",
                Type = BluetoothType.Classic,
                NativeDevice = disposable
            };

            // 通过 Client 工厂自动引入缓存
            _ = manager.CreateBluetoothClient(device);

            // 执行 Dispose
            manager.Dispose();

            Assert.True(disposable.IsDisposed, "BluetoothManager.Dispose 应当遍历并释放缓存中遗留的原生 COM 资源");
            Assert.Empty(manager.DiscoveredDevices);
        }
    }
}
