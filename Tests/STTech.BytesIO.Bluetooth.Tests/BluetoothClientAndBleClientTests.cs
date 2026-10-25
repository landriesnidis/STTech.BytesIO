using STTech.BytesIO.Bluetooth.Common;
using STTech.BytesIO.Bluetooth.Entity;
using System;
using Xunit;

namespace STTech.BytesIO.Bluetooth.Tests
{
    public class BluetoothClientAndBleClientTests
    {
        [Fact]
        public void BluetoothDeviceInfo_ShouldFormatToStringCorrectly()
        {
            var device = new BluetoothDeviceInfo
            {
                Name = "TestDevice",
                Address = 0x001A7DDA7113,
                AddressString = "00:1A:7D:DA:71:13",
                Type = BluetoothType.Ble,
                Rssi = -65
            };

            var str = device.ToString();
            Assert.Contains("[BLE]", str);
            Assert.Contains("TestDevice", str);
            Assert.Contains("00:1A:7D:DA:71:13", str);
            Assert.Contains("-65 dBm", str);
        }

        [Fact]
        public void BluetoothClient_AddressSynchronization_ShouldWorkBothWays()
        {
            var client = new BluetoothClient();

            // Set via numeric address
            client.DeviceAddress = 0x001A7DDA7113;
            Assert.Equal("00:1A:7D:DA:71:13", client.DeviceAddressString);

            // Set via string address
            client.DeviceAddressString = "11:22:33:44:55:66";
            Assert.Equal(0x112233445566UL, client.DeviceAddress);

            // Default SPP UUID
            Assert.Equal(BluetoothClient.SppServiceUuid, client.ServiceUuid);
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void BleClient_AddressSynchronization_ShouldWorkBothWays()
        {
            var client = new BleClient();

            client.DeviceAddress = 0x001A7DDA7113;
            Assert.Equal("00:1A:7D:DA:71:13", client.DeviceAddressString);

            client.DeviceAddressString = "AA:BB:CC:DD:EE:FF";
            Assert.Equal(0xAABBCCDDEEFFUL, client.DeviceAddress);
            Assert.False(client.IsConnected);
        }

        [Fact]
        public void BleClient_CustomUuids_ShouldBeRetained()
        {
            var serviceUuid = Guid.NewGuid();
            var writeUuid = Guid.NewGuid();
            var notifyUuid = Guid.NewGuid();

            var client = new BleClient("00:1A:7D:DA:71:13", serviceUuid, writeUuid, notifyUuid)
            {
                WriteOption = BleWriteOption.WriteWithResponse
            };

            Assert.Equal(serviceUuid, client.ServiceUuid);
            Assert.Equal(writeUuid, client.WriteCharacteristicUuid);
            Assert.Equal(notifyUuid, client.NotifyCharacteristicUuid);
            Assert.Equal(BleWriteOption.WriteWithResponse, client.WriteOption);
        }

        [Fact]
        public void BluetoothManager_FactoryMethods_ShouldCreateConfiguredClients()
        {
            using var manager = new BluetoothManager();

            var device = new BluetoothDeviceInfo
            {
                Name = "IndustrialSensor",
                Address = 0x001A7DDA7113,
                AddressString = "00:1A:7D:DA:71:13",
                Type = BluetoothType.Ble
            };

            var bthClient = manager.CreateBluetoothClient(device);
            Assert.NotNull(bthClient);
            Assert.Equal(device.Address, bthClient.DeviceAddress);
            Assert.Equal("00:1A:7D:DA:71:13", bthClient.DeviceAddressString);
            Assert.Same(device, bthClient.DeviceInfo);

            var bleClient = manager.CreateBleClient(device);
            Assert.NotNull(bleClient);
            Assert.Equal(device.Address, bleClient.DeviceAddress);
            Assert.Equal("00:1A:7D:DA:71:13", bleClient.DeviceAddressString);
            Assert.Same(device, bleClient.DeviceInfo);

            var directBle = manager.CreateBleClient("12:34:56:78:9A:BC");
            Assert.Equal(0x123456789ABCUL, directBle.DeviceAddress);

            var directBth = manager.CreateBluetoothClient("12:34:56:78:9A:BC");
            Assert.Equal(0x123456789ABCUL, directBth.DeviceAddress);
        }

        [Fact]
        public void BluetoothClient_Connect_WithoutAddress_ShouldFailGracefully()
        {
            var client = new BluetoothClient();
            var result = client.Connect();
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Exception);
        }

        [Fact]
        public void BleClient_Connect_WithoutAddress_ShouldFailGracefully()
        {
            var client = new BleClient();
            var result = client.Connect();
            Assert.False(result.IsSuccess);
            Assert.NotNull(result.Exception);
        }

        [Fact]
        public void BluetoothClient_ParameterlessConstructorAndPropertyDesign_ShouldBeValid()
        {
            // 验证存在公共无参构造函数
            var ctor = typeof(BluetoothClient).GetConstructor(Type.EmptyTypes);
            Assert.NotNull(ctor);
            Assert.True(ctor.IsPublic);

            // 验证核心配置属性均具备公开的 Getter 与 Setter
            var targetProperties = new[]
            {
                nameof(BluetoothClient.DeviceAddress),
                nameof(BluetoothClient.DeviceAddressString),
                nameof(BluetoothClient.DeviceName),
                nameof(BluetoothClient.ServiceUuid),
                nameof(BluetoothClient.DeviceInfo),
                nameof(BluetoothClient.ReceiveBufferSize),
                nameof(BluetoothClient.SendBufferSize)
            };

            foreach (var propName in targetProperties)
            {
                var prop = typeof(BluetoothClient).GetProperty(propName);
                Assert.NotNull(prop);
                Assert.True(prop.CanRead, $"属性 {propName} 应当具备可读能力");
                Assert.True(prop.CanWrite, $"属性 {propName} 应当具备可写能力以支持配置恢复与反序列化");
            }

            // 验证无参构造实例化与属性设值取值完整性
            var expectedUuid = Guid.NewGuid();
            var client = new BluetoothClient
            {
                DeviceAddressString = "00:1A:7D:DA:71:13",
                DeviceName = "CustomClassicDevice",
                ServiceUuid = expectedUuid,
                ReceiveBufferSize = 16384,
                SendBufferSize = 8192,
                DeviceInfo = new BluetoothDeviceInfo
                {
                    Name = "SampleClassicDevice",
                    AddressString = "00:1A:7D:DA:71:13",
                    Type = BluetoothType.Classic
                }
            };

            Assert.Equal(0x001A7DDA7113UL, client.DeviceAddress);
            Assert.Equal("00:1A:7D:DA:71:13", client.DeviceAddressString);
            Assert.Equal("CustomClassicDevice", client.DeviceName);
            Assert.Equal(expectedUuid, client.ServiceUuid);
            Assert.Equal(16384, client.ReceiveBufferSize);
            Assert.Equal(8192, client.SendBufferSize);
            Assert.False(client.IsConnected);
            Assert.NotNull(client.DeviceInfo);
        }

        [Fact]
        public void BleClient_ParameterlessConstructorAndPropertyDesign_ShouldBeValid()
        {
            // 验证存在公共无参构造函数
            var ctor = typeof(BleClient).GetConstructor(Type.EmptyTypes);
            Assert.NotNull(ctor);
            Assert.True(ctor.IsPublic);

            // 验证核心配置属性均具备公开的 Getter 与 Setter
            var targetProperties = new[]
            {
                nameof(BleClient.DeviceAddress),
                nameof(BleClient.DeviceAddressString),
                nameof(BleClient.DeviceName),
                nameof(BleClient.ServiceUuid),
                nameof(BleClient.WriteCharacteristicUuid),
                nameof(BleClient.NotifyCharacteristicUuid),
                nameof(BleClient.WriteOption),
                nameof(BleClient.DeviceInfo),
                nameof(BleClient.ReceiveBufferSize),
                nameof(BleClient.SendBufferSize)
            };

            foreach (var propName in targetProperties)
            {
                var prop = typeof(BleClient).GetProperty(propName);
                Assert.NotNull(prop);
                Assert.True(prop.CanRead, $"属性 {propName} 应当具备可读能力");
                Assert.True(prop.CanWrite, $"属性 {propName} 应当具备可写能力以支持配置恢复与反序列化");
            }

            // 验证无参构造实例化与属性设值取值完整性
            var serviceUuid = Guid.NewGuid();
            var writeUuid = Guid.NewGuid();
            var notifyUuid = Guid.NewGuid();

            var client = new BleClient
            {
                DeviceAddressString = "AA:BB:CC:DD:EE:FF",
                DeviceName = "CustomBleDevice",
                ServiceUuid = serviceUuid,
                WriteCharacteristicUuid = writeUuid,
                NotifyCharacteristicUuid = notifyUuid,
                WriteOption = BleWriteOption.WriteWithResponse,
                ReceiveBufferSize = 32768,
                SendBufferSize = 16384,
                DeviceInfo = new BluetoothDeviceInfo
                {
                    Name = "SampleBleDevice",
                    AddressString = "AA:BB:CC:DD:EE:FF",
                    Type = BluetoothType.Ble,
                    Rssi = -55
                }
            };

            Assert.Equal(0xAABBCCDDEEFFUL, client.DeviceAddress);
            Assert.Equal("AA:BB:CC:DD:EE:FF", client.DeviceAddressString);
            Assert.Equal("CustomBleDevice", client.DeviceName);
            Assert.Equal(serviceUuid, client.ServiceUuid);
            Assert.Equal(writeUuid, client.WriteCharacteristicUuid);
            Assert.Equal(notifyUuid, client.NotifyCharacteristicUuid);
            Assert.Equal(BleWriteOption.WriteWithResponse, client.WriteOption);
            Assert.Equal(32768, client.ReceiveBufferSize);
            Assert.Equal(16384, client.SendBufferSize);
            Assert.False(client.IsConnected);
            Assert.NotNull(client.DeviceInfo);
        }

        [Fact]
        public void BluetoothDeviceInfo_PropertyDesign_ShouldBeCleanAndPure()
        {
            // 验证公共无参构造函数
            var ctor = typeof(BluetoothDeviceInfo).GetConstructor(Type.EmptyTypes);
            Assert.NotNull(ctor);
            Assert.True(ctor.IsPublic);

            // 验证 NativeDevice 为 internal，不污染对外契约
            var nativeDeviceProp = typeof(BluetoothDeviceInfo).GetProperty("NativeDevice");
            Assert.Null(nativeDeviceProp); // 公共反射中不应存在

            var info = new BluetoothDeviceInfo
            {
                Address = 0x001122334455,
                AddressString = "00:11:22:33:44:55",
                Name = "IndustrialBleSensor",
                DeviceId = "DEV_001122334455",
                Type = BluetoothType.Ble,
                Rssi = -42,
                IsPaired = true,
                IsConnected = true,
                ServiceUuids = new System.Collections.Generic.List<Guid> { Guid.NewGuid(), Guid.NewGuid() }
            };

            Assert.Equal(0x001122334455UL, info.Address);
            Assert.Equal("00:11:22:33:44:55", info.AddressString);
            Assert.Equal("IndustrialBleSensor", info.Name);
            Assert.Equal("DEV_001122334455", info.DeviceId);
            Assert.Equal(BluetoothType.Ble, info.Type);
            Assert.Equal(-42, info.Rssi);
            Assert.True(info.IsPaired);
            Assert.True(info.IsConnected);
            Assert.Equal(2, info.ServiceUuids.Count);
        }
    }
}
