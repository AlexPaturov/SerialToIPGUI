// Decompiled with JetBrains decompiler
// Type: SerialToIpGUI.MainForm
// Assembly: SerialToIPGUI, Version=1.9.5866.19643, Culture=neutral, PublicKeyToken=null
// MVID: B1D67A1F-B8BA-49F3-B87A-0CFB4BE0BA84
// Assembly location: C:\Users\Alex\Downloads\SerialToIPGUI_v1.9_2016-01-23\SerialToIPGUI.exe

using serialtoip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;

namespace SerialToIpGUI
{
    public class MainForm : Form
    {
        private static CrossThreadComm.TraceCb _conInfoTrace;
        private static CrossThreadComm.UpdateState _updateState;
        private static CrossThreadComm.UpdateRXTX _updRxTx;
        private int _DiagSerportRx;
        private int _DiagSerportTx;
        private static object lck = new object();
        private static SerialPort sp = (SerialPort)null;
        protected object _traceListBoxLock = new object();
        private Dictionary<string, string> dn = new Dictionary<string, string>();
        private List<string> _items = new List<string>();
        private ClientMode cm;
        private ServerMode sm;
        private bool _running;
        private bool _shuttingdown;
        protected Thread thServiceThread;
        protected bool _connected;
        protected bool _is_shown;
        private bool drag;
        private Point start_point = new Point(0, 0);
        private bool draggable = true;
        private IContainer components;
        private Panel panel5;
        private Button buttonMinimize;
        private Label label7;
        private Label label6;
        private Label labelRxSerial;
        private Label labelSerialTX;
        private Panel panel4;
        private Panel panel3;
        private Panel panel2;
        private Panel panel1;
        private Label label5;
        private Button btnCloseForm;
        private Label label4;
        private Button buttonClearLog;
        private Button buttonRefresh;
        private ListBox listBoxInfoTrace;
        private Button buttonStop;
        private ComboBox listBoxBaudrate;
        private Label label3;
        private Button buttonStart;
        private Label labelRemoteHost;
        private TextBox textBoxRemoteHost;
        private RadioButton radioButtonServer;
        private RadioButton radioButtonClient;
        private GroupBox groupBox1;
        private TextBox textBoxSocketPort;
        private Label label2;
        private Label label1;
        private ComboBox listBoxSerialPorts;
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private void ServiceThread()
        {
            this._running = true;
            this.ConnInfoTrace((object)("Service start " + this.dn["serialport"] + " " + int.Parse(this.dn["baudrate"].Trim()).ToString()));
            logger.Info("Service start " + this.dn["serialport"] + " " + int.Parse(this.dn["baudrate"].Trim()).ToString());
            
            try
            {
                MainForm.sp = new SerialPort(this.dn["serialport"], int.Parse(this.dn["baudrate"].Trim()), Parity.None, 8, StopBits.One);
                if (this.dn["remotehost"] != null)
                {
                    this.cm = new ClientMode();
                    this.cm.Run(this.dn, MainForm.sp, MainForm._conInfoTrace, MainForm._updateState, MainForm._updRxTx);
                }
                else
                {
                    this.sm = new ServerMode();
                    this.sm.Run(this.dn, MainForm.sp, MainForm._conInfoTrace, MainForm._updateState, MainForm._updRxTx);
                }
            }
            catch (Exception ex)
            {
                this._running = false;
                this.ConnInfoTrace((object)"Mode start failed..");
                logger.Error("Mode start failed..");
                logger.Error(ex);
            }

            this.ConnInfoTrace((object)"Service stopped");
            logger.Info("Service stopped");
            this._running = false;
            this.cm = (ClientMode)null;
            this.sm = (ServerMode)null;
            this.thServiceThread = (Thread)null;
        }

        private void AddOrUpdate(string key, string value)
        {
            if (this.dn.ContainsKey(key))
                this.dn[key] = value;
            else
                this.dn.Add(key, value);
        }

        private void ButtonStartClick(object sender, EventArgs e)
        {
            this.AddOrUpdate("serialport", (string)this.listBoxSerialPorts.SelectedItem ?? this.listBoxSerialPorts.Text);
            this.AddOrUpdate("baudrate", (string)this.listBoxBaudrate.SelectedItem);
            this.AddOrUpdate("socketport", this.textBoxSocketPort.Text);
            this.listBoxBaudrate.Enabled = false;
            this.listBoxSerialPorts.Enabled = false;
            this.textBoxSocketPort.Enabled = false;
            if (!this.radioButtonServer.Checked)
                this.AddOrUpdate("remotehost", this.textBoxRemoteHost.Text);
            else
                this.AddOrUpdate("remotehost", (string)null);
            this.buttonStop.Enabled = true;
            this.buttonStart.Enabled = false;
            this.buttonRefresh.Enabled = false;
            this.thServiceThread = new Thread(new ThreadStart(this.ServiceThread));
            this._running = true;
            this.thServiceThread.Start();
            Thread.Sleep(100);
            if (!this._running)
            {
                this.groupBox1.Enabled = true;
                this.radioButtonClient.Enabled = true;
                this.radioButtonServer.Enabled = true;
                this.buttonStop.Enabled = false;
                this.buttonStart.Enabled = true;
                this.buttonRefresh.Enabled = true;
                this.panel5.BackColor = Color.Gray;
            }
            else
            {
                this.groupBox1.Enabled = false;
                this.radioButtonClient.Enabled = false;
                this.radioButtonServer.Enabled = false;
                this.panel5.BackColor = Color.Orange;
            }
        }

        private void HandleStop()
        {
            if (this.cm != null)
            {
                this.ConnInfoTrace((object)"Stop request set to Client Mode");
                logger.Info("Stop request set to Client Mode");
                this.cm.StopRequest();
            }
            if (this.sm != null)
            {
                this.ConnInfoTrace((object)"Stop request set to Server Mode");
                logger.Info("Stop request set to Server Mode");
                this.sm.StopRequest();
            }
            this.cm = (ClientMode)null;
            this.sm = (ServerMode)null;
            this.panel5.BackColor = Color.Gray;
            this.buttonStop.Enabled = false;
            this.buttonStart.Enabled = true;
            this.buttonRefresh.Enabled = true;
            this.listBoxBaudrate.Enabled = true;
            this.listBoxSerialPorts.Enabled = true;
            this.textBoxSocketPort.Enabled = true;
            this.groupBox1.Enabled = true;
            this.radioButtonClient.Enabled = true;
            this.radioButtonServer.Enabled = true;
        }

        private void ButtonStopClick(object sender, EventArgs e) => this.HandleStop();

        public void UpdateState(object obj, CrossThreadComm.State state)
        {
            if (this.InvokeRequired)
            {
                if (this._shuttingdown)
                    return;
                this.Invoke((Delegate)new CrossThreadComm.UpdateState(this.UpdateState), obj, (object)state);
            }
            else
            {
                switch (state)
                {
                    case CrossThreadComm.State.connect:
                        this.panel5.BackColor = Color.Green;
                        this._connected = true;
                        break;
                    case CrossThreadComm.State.disconnect:
                        this.panel5.BackColor = Color.Orange;
                        this._connected = false;
                        break;
                    case CrossThreadComm.State.terminate:
                        this._connected = false;
                        this.ConnInfoTrace((object)"UpdateState( ) -- service has finished");
                        logger.Info("UpdateState( ) -- service has finished");
                        break;
                }
            }
        }

        public void UpdateRxTx(object obj, int bytesFromSerial, int bytesToSerial)
        {
            if (this.InvokeRequired)
            {
                if (this._shuttingdown)
                    return;
                this.Invoke((Delegate)new CrossThreadComm.UpdateRXTX(this.UpdateRxTx), obj, (object)bytesFromSerial, (object)bytesToSerial);
            }
            else
            {
                this._DiagSerportRx += bytesFromSerial;
                this._DiagSerportTx += bytesToSerial;
            }
        }

        public void ConnInfoTrace(object obj)
        {
            if (this.InvokeRequired)
            {
                if (this._shuttingdown)
                    return;
                this.Invoke((Delegate)new CrossThreadComm.TraceCb(this.ConnInfoTrace), obj);
            }
            else
            {
                string str = (string)obj;
                lock (this._traceListBoxLock)
                {
                    this.labelRxSerial.Text = this._DiagSerportRx.ToString();
                    this.labelSerialTX.Text = this._DiagSerportTx.ToString();
                    if (str != null)
                    {
                        this.listBoxInfoTrace.Items.Add((object)(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + str));
                        logger.Info(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss ") + str);
                        if (this.listBoxInfoTrace.Items.Count > 256)
                            this.listBoxInfoTrace.Items.RemoveAt(0);
                        this.listBoxInfoTrace.SelectedIndex = this.listBoxInfoTrace.Items.Count - 1;
                        logger.Info(listBoxInfoTrace.Items.Count - 1);
                    }
                    else
                    {
                        this.listBoxInfoTrace.Items.Clear();
                        this._DiagSerportRx = 0;
                        this._DiagSerportTx = 0;
                        this.labelRxSerial.Text = this._DiagSerportRx.ToString();
                        this.labelSerialTX.Text = this._DiagSerportTx.ToString();
                    }
                }
            }
        }

        private void InitializeSerialToIPGui()
        {
            this.ConnInfoTrace((object)"GUI init with a serial port scan.");
            logger.Info("GUI init with a serial port scan.");
            this.listBoxBaudrate.DataSource = (object)this._items;
            
            try
            {
                this.listBoxBaudrate.SelectedIndex = 7;      // устанавливаем частоту 115200 
            }
            catch (Exception ex) 
            { 
                logger.Error(ex);
            }
            
            this.listBoxSerialPorts.Items.Clear();
            string[] portNames = SerialPort.GetPortNames();
            if (portNames.Length == 0)
            {
                this.listBoxSerialPorts.Items.Add((object)"error: none available!");
                logger.Info("error: none available!");
                this.ConnInfoTrace((object)"error: no serial ports found!");
                logger.Info("error: no serial ports found!");
                this.listBoxSerialPorts.ForeColor = Color.Red;
                this.buttonStart.Enabled = false;
            }
            else
            {
                this.listBoxSerialPorts.ForeColor = Color.Black;
                foreach (object obj in portNames)
                    this.listBoxSerialPorts.Items.Add(obj);
                try 
                { 
                    if (this.listBoxSerialPorts.Items.Count >= 2)
                        this.listBoxSerialPorts.SelectedIndex = 2;   // устанавливаем номер COM порта - 3
                }
                catch (Exception ex)
                {
                    logger.Error(ex);
                }
            }
            this.textBoxSocketPort.Text = this.dn["socketport"];
            this.radioButtonServer.Checked = true;
            this.labelRemoteHost.Enabled = false;
            this.textBoxRemoteHost.Enabled = false;
            this.buttonStop.Enabled = false;
        }

        public MainForm()
        {
            this.InitializeComponent();
            MainForm._conInfoTrace = new CrossThreadComm.TraceCb(this.ConnInfoTrace);
            MainForm._updateState = new CrossThreadComm.UpdateState(this.UpdateState);
            MainForm._updRxTx = new CrossThreadComm.UpdateRXTX(this.UpdateRxTx);
            this.dn.Add("serialport", "COM1");
            this.dn.Add("baudrate", "9600");
            this.dn.Add("socketport", "4001");
            this.dn.Add("remotehost", (string)null);
            this.dn.Add("socksend", (string)null);
            this.dn.Add("sockfilesend", (string)null);
            this._items.Add("1200");
            this._items.Add("2400");
            this._items.Add("4800");
            this._items.Add("9600");
            this._items.Add("19200");
            this._items.Add("38400");
            this._items.Add("57600");
            this._items.Add("115200");
            this._items.Add("230400");
            this._items.Add("460800");
            this._items.Add("921600");
            ToolTip toolTip = new ToolTip();
            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 1000;
            toolTip.ReshowDelay = 500;
            toolTip.ShowAlways = true;
            toolTip.SetToolTip((Control)this.buttonRefresh, "Refresh the serial port list");
            toolTip.SetToolTip((Control)this.buttonClearLog, "Clear the trace log");
            this.panel5.BackColor = Color.Gray;
            this.InitializeSerialToIPGui();
        }

        private void MainFormFormClosing(object sender, FormClosingEventArgs e)
        {
            this._shuttingdown = true;
            this._is_shown = false;
            this.HandleStop();
        }

        private void ButtonRefreshClick(object sender, EventArgs e) => this.InitializeSerialToIPGui();

        private void ButtonClearLogClick(object sender, EventArgs e) => this.ConnInfoTrace((object)null);

        private void CheckEnabledDisabledButtons()
        {
            string str = this.listBoxSerialPorts.SelectedItem != null ? (string)this.listBoxSerialPorts.SelectedItem : this.listBoxSerialPorts.Text;
            if (str == null)
                this.buttonStart.Enabled = false;
            else if (str.ToLower().IndexOf("select") >= 0 || str.Length < 4 || str.ToLower().IndexOf("--") >= 0)
                this.buttonStart.Enabled = false;
            else
                this.buttonStart.Enabled = true;
        }

        private void ListBoxSerialPortsSelectedIndexChanged(object sender, EventArgs e) => this.CheckEnabledDisabledButtons();

        private void ListBoxSerialPortsTextUpdate(object sender, EventArgs e) => this.CheckEnabledDisabledButtons();

        private void Button1Click(object sender, EventArgs e)
        {
            this.HandleStop();
            this.Close();
        }

        private void MainFormMouseDown(object sender, MouseEventArgs e)
        {
            Point point = new Point(e.X, e.Y);
            this.drag = true;
            this.start_point = new Point(e.X, e.Y);
            if (e.Button != MouseButtons.Right)
                return;
            ++point.X;
            ++point.Y;
        }

        private void Form_MouseUp(object sender, MouseEventArgs e) => this.drag = false;

        private void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if (!this.drag)
                return;
            Point screen = this.PointToScreen(new Point(e.X, e.Y));
            this.Location = new Point(screen.X - this.start_point.X, screen.Y - this.start_point.Y);
        }

        public bool Draggable
        {
            set => this.draggable = value;
            get => this.draggable;
        }

        private void Label5MouseDown(object sender, MouseEventArgs e)
        {
            MouseEventArgs e1 = e;
            this.MainFormMouseDown(sender, e1);
        }

        private void Label5MouseMove(object sender, MouseEventArgs e)
        {
            MouseEventArgs e1 = e;
            this.Form_MouseMove(sender, e1);
        }

        private void Label5MouseUp(object sender, MouseEventArgs e) => this.Form_MouseUp(sender, e);

        private void MainFormLoad(object sender, EventArgs e) => this._is_shown = true;

        private void ButtonMinimizeClick(object sender, EventArgs e) => this.WindowState = FormWindowState.Minimized;

        private void Button1MouseHover(object sender, EventArgs e)
        {
        }

        private void Button1MouseEnter(object sender, EventArgs e)
        {
            this.btnCloseForm.ForeColor = SystemColors.ControlLightLight;
            this.btnCloseForm.BackColor = Color.Red;
            this.btnCloseForm.Height = 26;
        }

        private void Button1MouseLeave(object sender, EventArgs e)
        {
            this.btnCloseForm.ForeColor = SystemColors.ControlLightLight;
            this.btnCloseForm.BackColor = Color.DodgerBlue;
            this.btnCloseForm.Height = 26;
        }

        private void MainFormActivated(object sender, EventArgs e)
        {
            if (!this._is_shown)
                return;
            this.Opacity = 1.0;
        }

        private void MainFormDeactivate(object sender, EventArgs e)
        {
            if (!this._is_shown)
                return;
            this.Opacity = 0.8;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && this.components != null)
                this.components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.textBoxSocketPort = new System.Windows.Forms.TextBox();
            this.radioButtonServer = new System.Windows.Forms.RadioButton();
            this.radioButtonClient = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.textBoxRemoteHost = new System.Windows.Forms.TextBox();
            this.labelRemoteHost = new System.Windows.Forms.Label();
            this.buttonStart = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.buttonStop = new System.Windows.Forms.Button();
            this.listBoxInfoTrace = new System.Windows.Forms.ListBox();
            this.buttonRefresh = new System.Windows.Forms.Button();
            this.buttonClearLog = new System.Windows.Forms.Button();
            this.listBoxSerialPorts = new System.Windows.Forms.ComboBox();
            this.listBoxBaudrate = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.btnCloseForm = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel3 = new System.Windows.Forms.Panel();
            this.panel4 = new System.Windows.Forms.Panel();
            this.labelSerialTX = new System.Windows.Forms.Label();
            this.labelRxSerial = new System.Windows.Forms.Label();
            this.label6 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.buttonMinimize = new System.Windows.Forms.Button();
            this.panel5 = new System.Windows.Forms.Panel();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(12, 42);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(104, 21);
            this.label1.TabIndex = 1;
            this.label1.Text = "Serial port:";
            // 
            // label2
            // 
            this.label2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(12, 118);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(104, 23);
            this.label2.TabIndex = 2;
            this.label2.Text = "Socket port:";
            // 
            // textBoxSocketPort
            // 
            this.textBoxSocketPort.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxSocketPort.Location = new System.Drawing.Point(185, 116);
            this.textBoxSocketPort.Name = "textBoxSocketPort";
            this.textBoxSocketPort.Size = new System.Drawing.Size(161, 24);
            this.textBoxSocketPort.TabIndex = 3;
            // 
            // radioButtonServer
            // 
            this.radioButtonServer.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.radioButtonServer.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButtonServer.Location = new System.Drawing.Point(194, 155);
            this.radioButtonServer.Name = "radioButtonServer";
            this.radioButtonServer.Size = new System.Drawing.Size(75, 24);
            this.radioButtonServer.TabIndex = 4;
            this.radioButtonServer.TabStop = true;
            this.radioButtonServer.Text = "Server";
            this.radioButtonServer.UseVisualStyleBackColor = true;
            this.radioButtonServer.CheckedChanged += new System.EventHandler(this.RadioButtonServerCheckedChanged);
            // 
            // radioButtonClient
            // 
            this.radioButtonClient.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.radioButtonClient.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.radioButtonClient.Location = new System.Drawing.Point(273, 155);
            this.radioButtonClient.Name = "radioButtonClient";
            this.radioButtonClient.Size = new System.Drawing.Size(66, 24);
            this.radioButtonClient.TabIndex = 5;
            this.radioButtonClient.TabStop = true;
            this.radioButtonClient.Text = "Client";
            this.radioButtonClient.UseVisualStyleBackColor = true;
            this.radioButtonClient.CheckedChanged += new System.EventHandler(this.RadioButtonClientCheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox1.Location = new System.Drawing.Point(185, 144);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(159, 42);
            this.groupBox1.TabIndex = 6;
            this.groupBox1.TabStop = false;
            // 
            // textBoxRemoteHost
            // 
            this.textBoxRemoteHost.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.textBoxRemoteHost.Location = new System.Drawing.Point(185, 190);
            this.textBoxRemoteHost.Name = "textBoxRemoteHost";
            this.textBoxRemoteHost.Size = new System.Drawing.Size(159, 24);
            this.textBoxRemoteHost.TabIndex = 6;
            // 
            // labelRemoteHost
            // 
            this.labelRemoteHost.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.labelRemoteHost.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelRemoteHost.Location = new System.Drawing.Point(12, 190);
            this.labelRemoteHost.Name = "labelRemoteHost";
            this.labelRemoteHost.Size = new System.Drawing.Size(152, 23);
            this.labelRemoteHost.TabIndex = 8;
            this.labelRemoteHost.Text = "Remote host:";
            // 
            // buttonStart
            // 
            this.buttonStart.BackColor = System.Drawing.SystemColors.Control;
            this.buttonStart.Enabled = false;
            this.buttonStart.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonStart.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStart.Location = new System.Drawing.Point(84, 622);
            this.buttonStart.Name = "buttonStart";
            this.buttonStart.Size = new System.Drawing.Size(128, 30);
            this.buttonStart.TabIndex = 7;
            this.buttonStart.Text = "Start";
            this.buttonStart.UseVisualStyleBackColor = true;
            this.buttonStart.Click += new System.EventHandler(this.ButtonStartClick);
            // 
            // label3
            // 
            this.label3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.label3.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label3.Location = new System.Drawing.Point(12, 80);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(104, 23);
            this.label3.TabIndex = 10;
            this.label3.Text = "Baud rate:";
            // 
            // buttonStop
            // 
            this.buttonStop.Enabled = false;
            this.buttonStop.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonStop.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonStop.Location = new System.Drawing.Point(250, 622);
            this.buttonStop.Name = "buttonStop";
            this.buttonStop.Size = new System.Drawing.Size(95, 30);
            this.buttonStop.TabIndex = 9;
            this.buttonStop.Text = "Stop";
            this.buttonStop.UseVisualStyleBackColor = true;
            this.buttonStop.Click += new System.EventHandler(this.ButtonStopClick);
            // 
            // listBoxInfoTrace
            // 
            this.listBoxInfoTrace.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.listBoxInfoTrace.FormattingEnabled = true;
            this.listBoxInfoTrace.Location = new System.Drawing.Point(12, 262);
            this.listBoxInfoTrace.Name = "listBoxInfoTrace";
            this.listBoxInfoTrace.Size = new System.Drawing.Size(332, 355);
            this.listBoxInfoTrace.TabIndex = 13;
            // 
            // buttonRefresh
            // 
            this.buttonRefresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonRefresh.Location = new System.Drawing.Point(146, 42);
            this.buttonRefresh.Name = "buttonRefresh";
            this.buttonRefresh.Size = new System.Drawing.Size(28, 28);
            this.buttonRefresh.TabIndex = 14;
            this.buttonRefresh.UseVisualStyleBackColor = true;
            this.buttonRefresh.Click += new System.EventHandler(this.ButtonRefreshClick);
            // 
            // buttonClearLog
            // 
            this.buttonClearLog.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonClearLog.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.buttonClearLog.Location = new System.Drawing.Point(17, 622);
            this.buttonClearLog.Name = "buttonClearLog";
            this.buttonClearLog.Size = new System.Drawing.Size(62, 30);
            this.buttonClearLog.TabIndex = 8;
            this.buttonClearLog.Text = "Clear";
            this.buttonClearLog.UseVisualStyleBackColor = true;
            this.buttonClearLog.Click += new System.EventHandler(this.ButtonClearLogClick);
            // 
            // listBoxSerialPorts
            // 
            this.listBoxSerialPorts.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F);
            this.listBoxSerialPorts.FormattingEnabled = true;
            this.listBoxSerialPorts.Location = new System.Drawing.Point(185, 42);
            this.listBoxSerialPorts.Name = "listBoxSerialPorts";
            this.listBoxSerialPorts.Size = new System.Drawing.Size(161, 26);
            this.listBoxSerialPorts.TabIndex = 1;
            this.listBoxSerialPorts.Text = "--select:";
            this.listBoxSerialPorts.SelectedIndexChanged += new System.EventHandler(this.ListBoxSerialPortsSelectedIndexChanged);
            this.listBoxSerialPorts.TextUpdate += new System.EventHandler(this.ListBoxSerialPortsTextUpdate);
            // 
            // listBoxBaudrate
            // 
            this.listBoxBaudrate.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F);
            this.listBoxBaudrate.FormattingEnabled = true;
            this.listBoxBaudrate.Location = new System.Drawing.Point(185, 78);
            this.listBoxBaudrate.Name = "listBoxBaudrate";
            this.listBoxBaudrate.Size = new System.Drawing.Size(161, 26);
            this.listBoxBaudrate.TabIndex = 2;
            // 
            // label4
            // 
            this.label4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.label4.Font = new System.Drawing.Font("Microsoft Sans Serif", 11F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label4.Location = new System.Drawing.Point(12, 156);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(137, 23);
            this.label4.TabIndex = 18;
            this.label4.Text = "Socket Mode:";
            // 
            // btnCloseForm
            // 
            this.btnCloseForm.BackColor = System.Drawing.Color.DodgerBlue;
            this.btnCloseForm.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnCloseForm.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this.btnCloseForm.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.btnCloseForm.Location = new System.Drawing.Point(311, -2);
            this.btnCloseForm.Name = "btnCloseForm";
            this.btnCloseForm.Size = new System.Drawing.Size(48, 26);
            this.btnCloseForm.TabIndex = 19;
            this.btnCloseForm.Text = "X";
            this.btnCloseForm.UseVisualStyleBackColor = false;
            this.btnCloseForm.Click += new System.EventHandler(this.Button1Click);
            this.btnCloseForm.MouseEnter += new System.EventHandler(this.Button1MouseEnter);
            this.btnCloseForm.MouseLeave += new System.EventHandler(this.Button1MouseLeave);
            this.btnCloseForm.MouseHover += new System.EventHandler(this.Button1MouseHover);
            // 
            // label5
            // 
            this.label5.BackColor = System.Drawing.Color.DodgerBlue;
            this.label5.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label5.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.label5.Location = new System.Drawing.Point(-1, -3);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(270, 26);
            this.label5.TabIndex = 20;
            this.label5.Text = "Serial-To-IP";
            this.label5.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.label5.MouseDown += new System.Windows.Forms.MouseEventHandler(this.Label5MouseDown);
            this.label5.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Label5MouseMove);
            this.label5.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Label5MouseUp);
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.Color.Silver;
            this.panel1.Location = new System.Drawing.Point(-1, 28);
            this.panel1.Margin = new System.Windows.Forms.Padding(0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(1, 311);
            this.panel1.TabIndex = 21;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.Color.Silver;
            this.panel2.Location = new System.Drawing.Point(355, 28);
            this.panel2.Margin = new System.Windows.Forms.Padding(0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(1, 350);
            this.panel2.TabIndex = 22;
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.Color.Silver;
            this.panel3.Location = new System.Drawing.Point(1, 656);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(358, 10);
            this.panel3.TabIndex = 23;
            // 
            // panel4
            // 
            this.panel4.BackColor = System.Drawing.Color.Silver;
            this.panel4.Location = new System.Drawing.Point(0, 30);
            this.panel4.Margin = new System.Windows.Forms.Padding(0);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(1, 344);
            this.panel4.TabIndex = 23;
            // 
            // labelSerialTX
            // 
            this.labelSerialTX.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.labelSerialTX.Location = new System.Drawing.Point(88, 230);
            this.labelSerialTX.Name = "labelSerialTX";
            this.labelSerialTX.Size = new System.Drawing.Size(90, 23);
            this.labelSerialTX.TabIndex = 24;
            this.labelSerialTX.Text = "0";
            this.labelSerialTX.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // labelRxSerial
            // 
            this.labelRxSerial.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.labelRxSerial.Location = new System.Drawing.Point(253, 229);
            this.labelRxSerial.Name = "labelRxSerial";
            this.labelRxSerial.Size = new System.Drawing.Size(91, 23);
            this.labelRxSerial.TabIndex = 25;
            this.labelRxSerial.Text = "0";
            this.labelRxSerial.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // label6
            // 
            this.label6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.label6.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label6.Location = new System.Drawing.Point(12, 230);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(66, 23);
            this.label6.TabIndex = 26;
            this.label6.Text = "SerTX:";
            // 
            // label7
            // 
            this.label7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.label7.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F);
            this.label7.Location = new System.Drawing.Point(185, 230);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(62, 23);
            this.label7.TabIndex = 27;
            this.label7.Text = "SerRX:";
            // 
            // buttonMinimize
            // 
            this.buttonMinimize.BackColor = System.Drawing.Color.DodgerBlue;
            this.buttonMinimize.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.buttonMinimize.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F);
            this.buttonMinimize.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.buttonMinimize.Location = new System.Drawing.Point(264, -2);
            this.buttonMinimize.Name = "buttonMinimize";
            this.buttonMinimize.Size = new System.Drawing.Size(48, 26);
            this.buttonMinimize.TabIndex = 28;
            this.buttonMinimize.Text = "_";
            this.buttonMinimize.UseVisualStyleBackColor = false;
            this.buttonMinimize.Click += new System.EventHandler(this.ButtonMinimizeClick);
            // 
            // panel5
            // 
            this.panel5.BackColor = System.Drawing.SystemColors.ControlLight;
            this.panel5.Location = new System.Drawing.Point(216, 621);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(31, 32);
            this.panel5.TabIndex = 29;
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(224)))), ((int)(((byte)(224)))), ((int)(((byte)(224)))));
            this.ClientSize = new System.Drawing.Size(363, 669);
            this.Controls.Add(this.panel5);
            this.Controls.Add(this.buttonMinimize);
            this.Controls.Add(this.label7);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.labelRxSerial);
            this.Controls.Add(this.labelSerialTX);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.btnCloseForm);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.listBoxBaudrate);
            this.Controls.Add(this.listBoxSerialPorts);
            this.Controls.Add(this.buttonClearLog);
            this.Controls.Add(this.buttonRefresh);
            this.Controls.Add(this.listBoxInfoTrace);
            this.Controls.Add(this.buttonStop);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.buttonStart);
            this.Controls.Add(this.labelRemoteHost);
            this.Controls.Add(this.textBoxRemoteHost);
            this.Controls.Add(this.radioButtonClient);
            this.Controls.Add(this.radioButtonServer);
            this.Controls.Add(this.textBoxSocketPort);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.label5);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "MainForm";
            this.Text = "Serial-To-IP";
            this.Activated += new System.EventHandler(this.MainFormActivated);
            this.Deactivate += new System.EventHandler(this.MainFormDeactivate);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainFormFormClosing);
            this.Load += new System.EventHandler(this.MainFormLoad);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.MainFormMouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.Form_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.Form_MouseUp);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        private void RadioButtonServerCheckedChanged(object sender, EventArgs e)
        {
            this.labelRemoteHost.Enabled = false;
            this.textBoxRemoteHost.Enabled = false;
        }

        private void RadioButtonClientCheckedChanged(object sender, EventArgs e)
        {
            this.labelRemoteHost.Enabled = true;
            this.textBoxRemoteHost.Enabled = true;
        }
    }
}
