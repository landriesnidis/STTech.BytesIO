using STTech.BytesIO.Modbus.Monitors;
using STTech.BytesIO.Tcp;
using STTech.BytesIO.Core;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace STTech.BytesIO.Modbus.Demo
{
    public partial class ServerForm : Form
    {
        private ModbusTcpServer tcpServer;
        private VirtualModbusDevice virtualDevice;

        private CoilRegisterValueMonitor coils;
        private DiscreteInputRegisterValueMonitor discreteInputs;
        private InputRegisterValueMonitor inputRegisters;
        private HoldingRegisterValueMonitor holdingRegisters;

        public ServerForm()
        {
            InitializeComponent();

            cmbProtocol.SelectedIndex = 0;
            btnStartStop.Click += btnStartStop_Click;
            btnCreateClient.Click += (s, e) => new ClientForm().Show();

            // 1. 初始化寄存器监视器
            coils = new CoilRegisterValueMonitor(0, 100);
            discreteInputs = new DiscreteInputRegisterValueMonitor(0, 100);
            inputRegisters = new InputRegisterValueMonitor(0, 100);
            holdingRegisters = new HoldingRegisterValueMonitor(0, 100);

            // 2. 绑定到 UI
            BindDataGridView(dgvCoils, coils.Source, "Value (Bool)");
            BindDataGridView(dgvDiscreteInputs, discreteInputs.Source, "Value (Bool)");
            BindDataGridView(dgvInputRegisters, inputRegisters.Source, "Value (U16)");
            BindDataGridView(dgvHoldingRegisters, holdingRegisters.Source, "Value (U16)");

            // 3. 注册值改变事件
            RegisterMonitorEvents(coils, "Coil");
            RegisterMonitorEvents(discreteInputs, "Discrete Input");
            RegisterMonitorEvents(inputRegisters, "Input Register");
            RegisterMonitorEvents(holdingRegisters, "Holding Register");
        }

        private void BindDataGridView<T>(DataGridView dgv, T[] source, string valueColumnName)
        {
            DataTable dt = new DataTable();
            dt.Columns.Add("Address", typeof(int));
            dt.Columns.Add(valueColumnName, typeof(T));

            for (int i = 0; i < source.Length; i++)
            {
                dt.Rows.Add(i, source[i]);
            }

            dgv.DataSource = dt;
            dgv.Columns[0].ReadOnly = true;
            dgv.Columns[0].Width = 80;

            dgv.CellValueChanged += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.ColumnIndex == 1)
                {
                    source[e.RowIndex] = (T)dgv.Rows[e.RowIndex].Cells[1].Value;
                }
            };
        }

        private void RegisterMonitorEvents<T>(RegisterValueMonitor<T> monitor, string name) where T : struct
        {
            monitor.ValueChanged += (sender, e) =>
            {
                this.SafeInvoke(() =>
                {
                    tbLog.AppendText($"[{DateTime.Now:HH:mm:ss.fff}] {name}[{e.Index}] changed: {e.OriginalValue} -> {e.NewValue} (Remote){Environment.NewLine}");
                    // 更新 UI 中的 Grid
                    DataGridView dgv = GetGridByMonitor(monitor);
                    if (dgv != null)
                    {
                        var dt = (DataTable)dgv.DataSource;
                        dt.Rows[e.Index][1] = e.NewValue;
                    }
                });
            };
        }

        private DataGridView GetGridByMonitor(object monitor)
        {
            if (monitor == coils) return dgvCoils;
            if (monitor == discreteInputs) return dgvDiscreteInputs;
            if (monitor == inputRegisters) return dgvInputRegisters;
            if (monitor == holdingRegisters) return dgvHoldingRegisters;
            return null;
        }

        private void btnStartStop_Click(object sender, EventArgs e)
        {
            if (tcpServer != null && tcpServer.IsRunning)
            {
                tcpServer.Dispose();
                tcpServer = null;
                btnStartStop.Text = "启动服务";
                btnStartStop.BackColor = Color.LightGreen;
                panel1.Enabled = true;
                btnStartStop.Enabled = true;
            }
            else
            {
                try
                {
                    // 创建并启动服务
                    var format = cmbProtocol.SelectedItem.ToString() == "ASCII" ? ModbusProtocolFormat.ASCII : ModbusProtocolFormat.RTU;
                    tcpServer = new ModbusTcpServer(format);
                    tcpServer.Port = (int)nudPort.Value;
                    tcpServer.SlaveId = (byte)nudSlaveId.Value;

                    virtualDevice = new VirtualModbusDevice(tcpServer);
                    virtualDevice.Mount(coils);
                    virtualDevice.Mount(discreteInputs);
                    virtualDevice.Mount(inputRegisters);
                    virtualDevice.Mount(holdingRegisters);

                    tcpServer.ClientConnected += (s, args) =>
                    {
                        this.SafeInvoke(() => tbLog.AppendText($"[Server] Client Connected: {args.Socket.RemoteEndPoint}{Environment.NewLine}"));
                        args.Client.OnDataReceived += Client_OnDataReceived;
                    };

                    tcpServer.Start();
                    btnStartStop.Text = "停止服务";
                    btnStartStop.BackColor = Color.Tomato;
                    panel1.Enabled = false;
                    btnStartStop.Enabled = true;

                    tbLog.AppendText($"[Server] Started on port {tcpServer.Port} (Format: {format}){Environment.NewLine}");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"启动失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    tcpServer?.Dispose();
                    tcpServer = null;
                }
            }
        }

        private void Client_OnDataReceived(object sender, DataReceivedEventArgs e)
        {
            this.SafeInvoke(() =>
            {
                tbLog.AppendText($"[Raw Data] {e.Data.ToArray().ToHexString()}{Environment.NewLine}");
            });
        }

        private void SafeInvoke(Action action)
        {
            if (this.InvokeRequired) this.Invoke(action);
            else action();
        }
    }
}
