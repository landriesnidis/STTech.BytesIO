using STTech.BytesIO.Core;
using STTech.BytesIO.Modbus;
using STTech.BytesIO.Modbus.Monitors;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace STTech.BytesIO.Modbus.Demo
{
    public partial class ClientForm : Form
    {
        private ModbusTcpClient client;
        
        private Timer timerCoils;
        private Timer timerHolding;
        
        private CoilRegisterValueMonitor monCoils;
        private HoldingRegisterValueMonitor monHolding;

        public ClientForm()
        {
            InitializeComponent();
            cmbProtocol.SelectedIndex = 0;
            cmbFunctionCode.SelectedIndex = 2; // Default to Read Holding Registers

            btnConnect.Click += btnConnect_Click;
            btnDisconnect.Click += btnDisconnect_Click;
            btnExecute.Click += btnExecute_Click;
            
            cbMonitorCoils.CheckedChanged += cbMonitorCoils_CheckedChanged;
            cbMonitorHolding.CheckedChanged += cbMonitorHolding_CheckedChanged;
            
            cmbFunctionCode.SelectedIndexChanged += (s, e) => UpdateUIByFunctionCode();

            UpdateUIByFunctionCode();

            timerCoils = new Timer();
            timerCoils.Tick += TimerCoils_Tick;
            
            timerHolding = new Timer();
            timerHolding.Tick += TimerHolding_Tick;
        }

        private void cbMonitorCoils_CheckedChanged(object sender, EventArgs e)
        {
            if (cbMonitorCoils.Checked)
            {
                if (client == null || !client.IsConnected)
                {
                    cbMonitorCoils.Checked = false;
                    AppendLog("请先连接服务器");
                    return;
                }
                ushort addr = (ushort)nudCoilAddr.Value;
                ushort qty = (ushort)nudCoilQty.Value;
                monCoils = new CoilRegisterValueMonitor(addr, qty);
                monCoils.ValueChanged += (s, arg) => AppendLog($"[线圈监视] 地址 {arg.Index + addr} 变更: {arg.OriginalValue} -> {arg.NewValue}");
                
                timerCoils.Interval = (int)nudCoilInterval.Value;
                timerCoils.Start();
                AppendLog($"已启动线圈监视: 地址={addr}, 数量={qty}, 间隔={timerCoils.Interval}ms");
            }
            else
            {
                timerCoils.Stop();
                AppendLog("已停止线圈监视");
            }
        }

        private void cbMonitorHolding_CheckedChanged(object sender, EventArgs e)
        {
            if (cbMonitorHolding.Checked)
            {
                if (client == null || !client.IsConnected)
                {
                    cbMonitorHolding.Checked = false;
                    AppendLog("请先连接服务器");
                    return;
                }
                ushort addr = (ushort)nudHoldingAddr.Value;
                ushort qty = (ushort)nudHoldingQty.Value;
                monHolding = new HoldingRegisterValueMonitor(addr, qty);
                monHolding.ValueChanged += (s, arg) => AppendLog($"[保持寄存器监视] 地址 {arg.Index + addr} 变更: {arg.OriginalValue} -> {arg.NewValue}");
                
                timerHolding.Interval = (int)nudHoldingInterval.Value;
                timerHolding.Start();
                AppendLog($"已启动保持寄存器监视: 地址={addr}, 数量={qty}, 间隔={timerHolding.Interval}ms");
            }
            else
            {
                timerHolding.Stop();
                AppendLog("已停止保持寄存器监视");
            }
        }

        private async void TimerCoils_Tick(object sender, EventArgs e)
        {
            if (client == null || !client.IsConnected) { cbMonitorCoils.Checked = false; return; }
            byte slaveId = (byte)nudSlaveId.Value;
            await Task.Run(() => client.UpdateCoilRegisterValueMonitor(monCoils, slaveId));
        }

        private async void TimerHolding_Tick(object sender, EventArgs e)
        {
            if (client == null || !client.IsConnected) { cbMonitorHolding.Checked = false; return; }
            byte slaveId = (byte)nudSlaveId.Value;
            await Task.Run(() => client.UpdateHoldingRegisterValueMonitor(monHolding, slaveId));
        }

        private void UpdateUIByFunctionCode()
        {
            string code = cmbFunctionCode.SelectedItem?.ToString().Substring(0, 2);
            bool isRead = code == "01" || code == "02" || code == "03" || code == "04";
            bool isSingleWrite = code == "05" || code == "06";
            bool isMultipleWrite = code == "15" || code == "16";

            // If multiple write, quantity is derived from data input
            nudQuantity.Visible = lblQuantity.Visible = isRead;
            tbData.Visible = lblData.Visible = isSingleWrite || isMultipleWrite;

            if (isSingleWrite)
            {
                lblData.Text = "数据：";
            }
            else if (isMultipleWrite)
            {
                lblData.Text = "数据(逗号间隔):";
            }
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            var format = cmbProtocol.SelectedItem.ToString() == "ASCII" ? ModbusProtocolFormat.ASCII : ModbusProtocolFormat.RTU;
            client = new ModbusTcpClient(format);
            client.Host = tbHost.Text;
            client.Port = (int)nudPort.Value;

            client.OnConnectedSuccessfully += (s, args) => this.SafeInvoke(() => {
                btnConnect.Enabled = false;
                btnDisconnect.Enabled = true;
                panelTop.Enabled = false;
                panelMain.Enabled = true;
                AppendLog("已连接到服务器");
            });

            client.OnDisconnected += (s, args) => this.SafeInvoke(() => {
                btnConnect.Enabled = true;
                btnDisconnect.Enabled = false;
                panelTop.Enabled = true;
                panelMain.Enabled = false;
                cbMonitorCoils.Checked = cbMonitorHolding.Checked = false;
                AppendLog("与服务器断开连接");
            });

            client.OnExceptionOccurs += (s, args) => AppendLog($"发生异常: {args.Exception.Message}");
            client.OnDataReceived += (s, args) => AppendLog($"[Raw Data] {args.Data.ToArray().ToHexString()}");

            var result = client.Connect();
            if (!result.IsSuccess) AppendLog($"连接失败: {result.ErrorCode}");
        }

        private void btnDisconnect_Click(object sender, EventArgs e)
        {
            client?.Disconnect();
        }

        private async void btnExecute_Click(object sender, EventArgs e)
        {
            if (client == null || !client.IsConnected)
            {
                AppendLog("请先连接服务器");
                return;
            }

            byte slaveId = (byte)nudSlaveId.Value;
            ushort address = (ushort)nudAddress.Value;
            string code = cmbFunctionCode.SelectedItem.ToString().Substring(0, 2);

            try
            {
                Reply reply = null;

                switch (code)
                {
                    case "01": reply = await Task.Run(() => client.ReadCoilRegister(slaveId, address, (ushort)nudQuantity.Value)); break;
                    case "02": reply = await Task.Run(() => client.ReadDiscreteInputRegister(slaveId, address, (ushort)nudQuantity.Value)); break;
                    case "03": reply = await Task.Run(() => client.ReadHoldingRegister(slaveId, address, (ushort)nudQuantity.Value)); break;
                    case "04": reply = await Task.Run(() => client.ReadInputRegister(slaveId, address, (ushort)nudQuantity.Value)); break;
                    case "05":
                        bool coilVal = tbData.Text.Trim() == "1" || tbData.Text.ToLower().Trim() == "true";
                        reply = await Task.Run(() => client.WriteSingleCoilRegister(slaveId, address, coilVal));
                        break;
                    case "06":
                        ushort regVal = ushort.Parse(tbData.Text.Trim());
                        reply = await Task.Run(() => client.WriteSingleHoldingRegister(slaveId, address, regVal));
                        break;
                    case "15":
                        string[] coilStr = tbData.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        bool[] coils = coilStr.Select(s => s.Trim() == "1" || s.ToLower().Trim() == "true").ToArray();
                        reply = await Task.Run(() => client.WriteMultipleCoilRegisters(slaveId, address, coils));
                        break;
                    case "16":
                        string[] regStr = tbData.Text.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                        ushort[] regs = regStr.Select(s => ushort.Parse(s.Trim())).ToArray();
                        reply = await Task.Run(() => client.WriteMultipleHoldingRegisters(slaveId, address, regs));
                        break;
                }

                if (reply != null) HandleReply(reply, code, address);
            }
            catch (Exception ex)
            {
                AppendLog($"执行失败: {ex.Message}");
            }
        }

        private void HandleReply(Reply reply, string code, ushort address)
        {
            if (reply.Status == STTech.BytesIO.Core.ReplyStatus.Completed)
            {
                var response = reply.GetResponse() as ModbusResponse;
                if (response == null) return;

                if (response.IsSuccess)
                {
                    string resultStr = "";
                    if (response is ReadCoilRegisterResponse coilResp)
                    {
                        resultStr = string.Join(", ", coilResp.Values);
                    }
                    else if (response is ReadDiscreteInputRegisterResponse discResp)
                    {
                        resultStr = string.Join(", ", discResp.Values);
                    }
                    else if (response is ReadHoldingRegisterResponse holdingResp)
                    {
                        var bytes = holdingResp.Values;
                        ushort[] u16s = new ushort[bytes.Length / 2];
                        for (int i = 0; i < u16s.Length; i++)
                        {
                            u16s[i] = (ushort)(bytes[i * 2] << 8 | bytes[i * 2 + 1]);
                        }
                        resultStr = string.Join(", ", u16s);
                    }
                    else if (response is ReadInputRegisterResponse inputResp)
                    {
                        var bytes = inputResp.Values;
                        ushort[] u16s = new ushort[bytes.Length / 2];
                        for (int i = 0; i < u16s.Length; i++)
                        {
                            u16s[i] = (ushort)(bytes[i * 2] << 8 | bytes[i * 2 + 1]);
                        }
                        resultStr = string.Join(", ", u16s);
                    }
                    else
                    {
                        resultStr = "成功";
                    }
                    AppendLog($"[响应] 功能码:{code} 地址:{address} => {resultStr}");
                }
                else
                {
                    AppendLog($"[失败] 功能码:{code} 错误码:{response.ErrorCode}");
                }
            }
            else
            {
                AppendLog($"[超时/错误] 状态: {reply.Status}");
            }
        }

        private void AppendLog(string msg)
        {
            this.SafeInvoke(() => {
                rtbLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}{Environment.NewLine}");
                rtbLog.SelectionStart = rtbLog.Text.Length;
                rtbLog.ScrollToCaret();
            });
        }

        private void SafeInvoke(Action action)
        {
            if (this.InvokeRequired) this.Invoke(action);
            else action();
        }
    }
}
