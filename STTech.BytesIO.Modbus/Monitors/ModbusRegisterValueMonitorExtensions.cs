using STTech.BytesIO.Core;
using STTech.BytesIO.Modbus.Monitors;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace STTech.BytesIO.Modbus
{
    /// <summary>
    /// ModbusClient寄存器数字监视器的方法扩展
    /// </summary>
    public static class ModbusRegisterValueMonitorExtensions
    {
        /// <summary>
        /// 内部通用的更新寄存器数值监视器处理逻辑
        /// </summary>
        /// <typeparam name="T">响应类型</typeparam>
        /// <typeparam name="TValue">数值类型</typeparam>
        /// <param name="client">Modbus 客户端</param>
        /// <param name="functionCode">功能码</param>
        /// <param name="monitor">监视器</param>
        /// <param name="slaveId">从站地址</param>
        /// <param name="timeout">超时时长</param>
        /// <returns>更新结果</returns>
        private static MonitorUpdateResult<T> UpdateRegisterValueMonitor<T, TValue>(this ModbusClient client, FunctionCode functionCode, RegisterValueMonitor<TValue> monitor, byte slaveId = 1, int timeout = 500) where T : ModbusResponse where TValue : struct
        {
            var request = new ReadRegisterRequest(functionCode)
            {
                Length = monitor.Length,
                StartAddress = monitor.StartAddress,
                SlaveId = slaveId,
            };

            var reply = client.SendModbusRequest<T>(request, timeout);

            MonitorUpdateResult<T> result = new MonitorUpdateResult<T>();
            result.Reply = reply;

            if (reply.Status == ReplyStatus.Completed)
            {
                var resp = reply.GetResponse();
                result.ErrorCode = resp.ErrorCode;

                if (resp.IsSuccess)
                {
                    ApplyResponseToMonitor(monitor, resp);
                }
            }

            return result;
        }

        /// <summary>
        /// 异步内部通用的更新寄存器数值监视器处理逻辑
        /// </summary>
        private static async Task<MonitorUpdateResult<T>> UpdateRegisterValueMonitorAsync<T, TValue>(this ModbusClient client, FunctionCode functionCode, RegisterValueMonitor<TValue> monitor, byte slaveId = 1, int timeout = 500) where T : ModbusResponse where TValue : struct
        {
            var request = new ReadRegisterRequest(functionCode)
            {
                Length = monitor.Length,
                StartAddress = monitor.StartAddress,
                SlaveId = slaveId,
            };

            var reply = await client.SendModbusRequestAsync<T>(request, timeout).ConfigureAwait(false);

            MonitorUpdateResult<T> result = new MonitorUpdateResult<T>();
            result.Reply = reply;

            if (reply.Status == ReplyStatus.Completed)
            {
                var resp = reply.GetResponse();
                result.ErrorCode = resp.ErrorCode;

                if (resp.IsSuccess)
                {
                    ApplyResponseToMonitor(monitor, resp);
                }
            }

            return result;
        }

        private static void ApplyResponseToMonitor<TValue>(RegisterValueMonitor<TValue> monitor, ModbusResponse resp) where TValue : struct
        {
            if (resp is ReadCoilRegisterResponse readCoilRegisterResponse)
            {
                var vals = readCoilRegisterResponse.Values;
                if (vals != null)
                {
                    var targetVals = vals.Length > monitor.Length ? vals.Take(monitor.Length).ToArray() : vals;
                    monitor.Update((TValue[])(object)targetVals);
                }
            }
            else if (resp is ReadDiscreteInputRegisterResponse readDiscreteInputRegisterResponse)
            {
                var vals = readDiscreteInputRegisterResponse.Values;
                if (vals != null)
                {
                    var targetVals = vals.Length > monitor.Length ? vals.Take(monitor.Length).ToArray() : vals;
                    monitor.Update((TValue[])(object)targetVals);
                }
            }
            else if (resp is ReadHoldingRegisterResponse readHoldingRegisterResponse)
            {
                monitor.Update((TValue[])(object)readHoldingRegisterResponse.GetUInt16Array());
            }
            else if (resp is ReadInputRegisterResponse readInputRegisterResponse)
            {
                monitor.Update((TValue[])(object)readInputRegisterResponse.GetUInt16Array());
            }
        }

        /// <summary>
        /// 更新保持寄存器数值监视器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="monitor">监视器对象</param>
        /// <param name="slaveId">从机ID</param>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public static MonitorUpdateResult<ReadHoldingRegisterResponse> UpdateHoldingRegisterValueMonitor(this ModbusClient client, HoldingRegisterValueMonitor monitor, byte slaveId = 1, int timeout = 500)
        {
            return client.UpdateRegisterValueMonitor<ReadHoldingRegisterResponse, ushort>(FunctionCode.ReadHoldingRegister, monitor, slaveId, timeout);
        }

        /// <summary>
        /// 异步更新保持寄存器数值监视器
        /// </summary>
        public static Task<MonitorUpdateResult<ReadHoldingRegisterResponse>> UpdateHoldingRegisterValueMonitorAsync(this ModbusClient client, HoldingRegisterValueMonitor monitor, byte slaveId = 1, int timeout = 500)
        {
            return client.UpdateRegisterValueMonitorAsync<ReadHoldingRegisterResponse, ushort>(FunctionCode.ReadHoldingRegister, monitor, slaveId, timeout);
        }

        /// <summary>
        /// 更新输入寄存器数值监视器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="monitor">监视器对象</param>
        /// <param name="slaveId">从机ID</param>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public static MonitorUpdateResult<ReadInputRegisterResponse> UpdateInputRegisterValueMonitor(this ModbusClient client, InputRegisterValueMonitor monitor, byte slaveId = 1, int timeout = 500)
        {
            return client.UpdateRegisterValueMonitor<ReadInputRegisterResponse, ushort>(FunctionCode.ReadInputRegister, monitor, slaveId, timeout);
        }

        /// <summary>
        /// 异步更新输入寄存器数值监视器
        /// </summary>
        public static Task<MonitorUpdateResult<ReadInputRegisterResponse>> UpdateInputRegisterValueMonitorAsync(this ModbusClient client, InputRegisterValueMonitor monitor, byte slaveId = 1, int timeout = 500)
        {
            return client.UpdateRegisterValueMonitorAsync<ReadInputRegisterResponse, ushort>(FunctionCode.ReadInputRegister, monitor, slaveId, timeout);
        }

        /// <summary>
        /// 更新线圈寄存器数值监视器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="monitor">监视器对象</param>
        /// <param name="slaveId">从机ID</param>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public static MonitorUpdateResult<ReadCoilRegisterResponse> UpdateCoilRegisterValueMonitor(this ModbusClient client, CoilRegisterValueMonitor monitor, byte slaveId = 1, int timeout = 500)
        {
            return client.UpdateRegisterValueMonitor<ReadCoilRegisterResponse, bool>(FunctionCode.ReadCoilRegister, monitor, slaveId, timeout);
        }

        /// <summary>
        /// 异步更新线圈寄存器数值监视器
        /// </summary>
        public static Task<MonitorUpdateResult<ReadCoilRegisterResponse>> UpdateCoilRegisterValueMonitorAsync(this ModbusClient client, CoilRegisterValueMonitor monitor, byte slaveId = 1, int timeout = 500)
        {
            return client.UpdateRegisterValueMonitorAsync<ReadCoilRegisterResponse, bool>(FunctionCode.ReadCoilRegister, monitor, slaveId, timeout);
        }

        /// <summary>
        /// 更新离散输入寄存器数值监视器
        /// </summary>
        /// <param name="client">Modbus客户端</param>
        /// <param name="monitor">监视器对象</param>
        /// <param name="slaveId">从机ID</param>
        /// <param name="timeout">超时时间</param>
        /// <returns></returns>
        public static MonitorUpdateResult<ReadDiscreteInputRegisterResponse> UpdateDiscreteInputRegisterValueMonitor(this ModbusClient client, DiscreteInputRegisterValueMonitor monitor, byte slaveId = 1, int timeout = 500)
        {
            return client.UpdateRegisterValueMonitor<ReadDiscreteInputRegisterResponse, bool>(FunctionCode.ReadDiscreteInputRegister, monitor, slaveId, timeout);
        }

        /// <summary>
        /// 异步更新离散输入寄存器数值监视器
        /// </summary>
        public static Task<MonitorUpdateResult<ReadDiscreteInputRegisterResponse>> UpdateDiscreteInputRegisterValueMonitorAsync(this ModbusClient client, DiscreteInputRegisterValueMonitor monitor, byte slaveId = 1, int timeout = 500)
        {
            return client.UpdateRegisterValueMonitorAsync<ReadDiscreteInputRegisterResponse, bool>(FunctionCode.ReadDiscreteInputRegister, monitor, slaveId, timeout);
        }
    }
}
