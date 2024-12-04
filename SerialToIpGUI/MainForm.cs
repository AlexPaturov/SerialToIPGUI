using serialtoip;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.Globalization;
using System.Net.Sockets;
using System.Reflection;
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
        protected object _traceListBoxLock = new object();
        private Dictionary<string, string> dn = new Dictionary<string, string>();
        private List<string> _items = new List<string>();
        private CancellationTokenSource _udpCts; 
        private UdpClient _udpServer;
        ServerMode sm = null;
        private bool _running = false;
        private bool _shuttingdown = false;
        protected Thread thServiceThread = null;
        protected bool _connected = false;
        protected bool _is_shown = false;
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
        private Panel panel1;
        private Label label5;
        private Button btnCloseForm;
        private Button buttonClearLog;
        private Button buttonRefresh;
        private ListBox listBoxInfoTrace;
        private Button buttonStop;
        private Label label3;
        private Button buttonStart;
        private Label labelRemoteHost;
        private Label label2;
        private TextBox tbClientPort;
        private TextBox tbVesy31ip;
        private TextBox tbVesy31port;
        private TextBox tbClientHost;
        private ContextMenuStrip contextMenuStrip1;
        private Label label1;
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private void ServiceThread()
        {
            _running = true;
            ConnInfoTrace((object)("ServiceThread start " + this.dn["clientHost"] + " " + int.Parse(this.dn["clientPort"].Trim()).ToString()));
            
            try
            {
                sm = new ServerMode();
                sm.Run(dn, 
                       _udpServer, 
                       _udpCts,  
                       MainForm._conInfoTrace, 
                       MainForm._updateState, 
                       MainForm._updRxTx);
                 
            }
            catch (Exception ex)
            {
                _running = false;
                ConnInfoTrace("ServiceThread start failed..");
                ConnInfoTrace(ex.Message);
                ConnInfoTrace(ex.StackTrace);
                logger.Error(ex);
            }

            ConnInfoTrace("ServiceThread stopped");
            _running = false;
            sm = null;

            if(_udpCts is not null)
                _udpCts.Cancel();
            _udpCts = null;

            if(_udpServer is not null)
                _udpServer.Close();
            _udpServer = null;

            thServiceThread = null;
        }

        private void ButtonStartClick(object sender, EventArgs e)
        {
            #region enable/disable input element
            this.tbVesy31ip.Enabled     = false;
            this.tbVesy31port.Enabled     = false;
            this.tbClientHost.Enabled   = false;
            this.tbClientPort.Enabled   = false;
            this.buttonStop.Enabled     = true;
            this.buttonStart.Enabled    = false; 
            this.buttonRefresh.Enabled  = false;
            this.dn["vesy31ip"]         = this.tbVesy31ip.Text.Trim();
            this.dn["vesy31port"]         = this.tbVesy31port.Text.Trim();
            this.dn["clientHost"]       = this.tbClientHost.Text.Trim();
            this.dn["clientPort"]       = this.tbClientPort.Text.Trim();
            #endregion

            thServiceThread = new Thread(ServiceThread);
            _running = true;
            thServiceThread.Start();
            Thread.Sleep(100);
            
            if (!this._running)
            {
                this.buttonStop.Enabled = false;
                this.buttonStart.Enabled = true;
                this.buttonRefresh.Enabled = true;
                this.panel5.BackColor = Color.Gray;
            }
            else
            {
                this.panel5.BackColor = Color.Orange;
            }

        }

        private void ButtonStopClick(object sender, EventArgs e) 
        {
            HandleStop();
        }

        private void HandleStop()
        {
            if (sm != null)
            {
                ConnInfoTrace((object)"Stop request ServiceThread");
                sm.StopRequest();
            }
            sm = null;

            #region работа с udp клиентом
                if(_udpCts is not null)
                    _udpCts.Cancel();

                if(_udpServer is not null)
                    _udpServer.Close();
            
                _udpServer = null;
                _udpCts = null;
            #endregion

            this.panel5.BackColor = Color.Gray;
            this.buttonStop.Enabled = false;
            this.tbVesy31ip.Enabled = true;
            this.tbVesy31port.Enabled = true;
            this.tbClientHost.Enabled = true;
            this.tbClientPort.Enabled = true;
            this.buttonStart.Enabled = true;
            this.buttonRefresh.Enabled = true;
        }

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
                        this.ConnInfoTrace((object)"UpdateState() -- ServiceThread has finished");
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
                        if (this.listBoxInfoTrace.Items.Count > 256)
                            this.listBoxInfoTrace.Items.RemoveAt(0);
                        this.listBoxInfoTrace.SelectedIndex = this.listBoxInfoTrace.Items.Count - 1;
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
            SetDefaultCulture();
            this.ConnInfoTrace((object)"GUI init");
            this.tbVesy31ip.Text = this.dn["vesy31ip"];     // на моей машине
            this.tbVesy31port.Text = this.dn["vesy31port"];
            this.tbClientHost.Text = this.dn["clientHost"];
            this.tbClientPort.Text = this.dn["clientPort"];
            this.buttonStop.Enabled = false;
        }

        public MainForm()
        {
            this.InitializeComponent();
            MainForm._conInfoTrace = new CrossThreadComm.TraceCb(this.ConnInfoTrace);
            MainForm._updateState = new CrossThreadComm.UpdateState(this.UpdateState);
            MainForm._updRxTx = new CrossThreadComm.UpdateRXTX(this.UpdateRxTx);
            dn.Add("Vesy", ConfigurationManager.AppSettings["Vesy"]);                       // настройки для подключения к 
            dn.Add("clientHost", ConfigurationManager.AppSettings["ArmAddress"]);
            dn.Add("clientPort", ConfigurationManager.AppSettings["ArmPort"]);
            dn.Add("vesy31ip", ConfigurationManager.AppSettings["ControllerAddress"]); // для контроллера 31-х весов
            dn.Add("vesy31port", ConfigurationManager.AppSettings["ControllerPort"]);       // для контроллера 31-х весов

            ToolTip toolTip = new ToolTip();
            toolTip.AutoPopDelay = 5000;
            toolTip.InitialDelay = 1000;
            toolTip.ReshowDelay = 500;
            toolTip.ShowAlways = true;
            toolTip.SetToolTip((Control)this.buttonRefresh, "Refresh the ... ");
            toolTip.SetToolTip((Control)this.buttonClearLog, "Clear the trace log");
            this.panel5.BackColor = Color.Gray;
            this.InitializeSerialToIPGui();
        }

        private void MainFormFormClosing(object sender, FormClosingEventArgs e)
        {
            this._shuttingdown = true;
            this._is_shown = false;
            this.HandleStop();
            logger.Info("Close " + System.Diagnostics.Process.GetCurrentProcess().ProcessName);  // спецификация - дата, время запуска драйвера
        }

        private void ButtonRefreshClick(object sender, EventArgs e) => this.InitializeSerialToIPGui();

        private void ButtonClearLogClick(object sender, EventArgs e) => this.ConnInfoTrace((object)null);

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

        private void MainFormLoad(object sender, EventArgs e)
        { 
            this._is_shown = true;
            logger.Info("Start " + System.Diagnostics.Process.GetCurrentProcess().ProcessName); // спецификация - дата, время запуска драйвера
        }

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
            components = new Container();
            label1 = new Label();
            label2 = new Label();
            labelRemoteHost = new Label();
            buttonStart = new Button();
            label3 = new Label();
            buttonStop = new Button();
            listBoxInfoTrace = new ListBox();
            buttonRefresh = new Button();
            buttonClearLog = new Button();
            btnCloseForm = new Button();
            label5 = new Label();
            panel1 = new Panel();
            panel4 = new Panel();
            labelSerialTX = new Label();
            labelRxSerial = new Label();
            label6 = new Label();
            label7 = new Label();
            buttonMinimize = new Button();
            panel5 = new Panel();
            tbClientPort = new TextBox();
            tbVesy31ip = new TextBox();
            tbVesy31port = new TextBox();
            tbClientHost = new TextBox();
            contextMenuStrip1 = new ContextMenuStrip(components);
            SuspendLayout();
            // 
            // label1
            // 
            label1.BackColor = Color.FromArgb(224, 224, 224);
            label1.Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label1.Location = new Point(16, 46);
            label1.Margin = new Padding(4, 0, 4, 0);
            label1.Name = "label1";
            label1.Size = new Size(120, 32);
            label1.TabIndex = 1;
            label1.Text = "vesy31 ip";
            label1.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label2
            // 
            label2.BackColor = Color.FromArgb(224, 224, 224);
            label2.Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label2.Location = new Point(521, 81);
            label2.Margin = new Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new Size(120, 32);
            label2.TabIndex = 2;
            label2.Text = "client port";
            label2.TextAlign = ContentAlignment.MiddleRight;
            // 
            // labelRemoteHost
            // 
            labelRemoteHost.BackColor = Color.FromArgb(224, 224, 224);
            labelRemoteHost.Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            labelRemoteHost.Location = new Point(521, 45);
            labelRemoteHost.Margin = new Padding(4, 0, 4, 0);
            labelRemoteHost.Name = "labelRemoteHost";
            labelRemoteHost.Size = new Size(120, 32);
            labelRemoteHost.TabIndex = 8;
            labelRemoteHost.Text = "client host";
            labelRemoteHost.TextAlign = ContentAlignment.MiddleRight;
            // 
            // buttonStart
            // 
            buttonStart.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonStart.BackColor = SystemColors.Control;
            buttonStart.FlatStyle = FlatStyle.Flat;
            buttonStart.Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            buttonStart.Location = new Point(112, 413);
            buttonStart.Margin = new Padding(4, 5, 4, 5);
            buttonStart.Name = "buttonStart";
            buttonStart.Size = new Size(171, 46);
            buttonStart.TabIndex = 7;
            buttonStart.Text = "Start";
            buttonStart.UseVisualStyleBackColor = true;
            buttonStart.Click += ButtonStartClick;
            // 
            // label3
            // 
            label3.BackColor = Color.FromArgb(224, 224, 224);
            label3.Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            label3.Location = new Point(16, 84);
            label3.Margin = new Padding(4, 0, 4, 0);
            label3.Name = "label3";
            label3.Size = new Size(120, 32);
            label3.TabIndex = 10;
            label3.Text = "vesy31 port";
            label3.TextAlign = ContentAlignment.MiddleRight;
            // 
            // buttonStop
            // 
            buttonStop.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonStop.Enabled = false;
            buttonStop.FlatStyle = FlatStyle.Flat;
            buttonStop.Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            buttonStop.Location = new Point(333, 413);
            buttonStop.Margin = new Padding(4, 5, 4, 5);
            buttonStop.Name = "buttonStop";
            buttonStop.Size = new Size(127, 46);
            buttonStop.TabIndex = 9;
            buttonStop.Text = "Stop";
            buttonStop.UseVisualStyleBackColor = true;
            buttonStop.Click += ButtonStopClick;
            // 
            // listBoxInfoTrace
            // 
            listBoxInfoTrace.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            listBoxInfoTrace.Font = new Font("Microsoft Sans Serif", 8F, FontStyle.Regular, GraphicsUnit.Point, 0);
            listBoxInfoTrace.FormattingEnabled = true;
            listBoxInfoTrace.HorizontalScrollbar = true;
            listBoxInfoTrace.ItemHeight = 16;
            listBoxInfoTrace.Location = new Point(16, 176);
            listBoxInfoTrace.Margin = new Padding(4, 5, 4, 5);
            listBoxInfoTrace.Name = "listBoxInfoTrace";
            listBoxInfoTrace.ScrollAlwaysVisible = true;
            listBoxInfoTrace.Size = new Size(875, 212);
            listBoxInfoTrace.TabIndex = 13;
            // 
            // buttonRefresh
            // 
            buttonRefresh.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonRefresh.FlatStyle = FlatStyle.Flat;
            buttonRefresh.Location = new Point(806, 413);
            buttonRefresh.Margin = new Padding(4, 5, 4, 5);
            buttonRefresh.Name = "buttonRefresh";
            buttonRefresh.Size = new Size(85, 46);
            buttonRefresh.TabIndex = 14;
            buttonRefresh.Text = "Refresh";
            buttonRefresh.UseVisualStyleBackColor = true;
            buttonRefresh.Click += ButtonRefreshClick;
            // 
            // buttonClearLog
            // 
            buttonClearLog.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            buttonClearLog.FlatStyle = FlatStyle.Flat;
            buttonClearLog.Font = new Font("Microsoft Sans Serif", 11F, FontStyle.Regular, GraphicsUnit.Point, 0);
            buttonClearLog.Location = new Point(23, 413);
            buttonClearLog.Margin = new Padding(4, 5, 4, 5);
            buttonClearLog.Name = "buttonClearLog";
            buttonClearLog.Size = new Size(83, 46);
            buttonClearLog.TabIndex = 8;
            buttonClearLog.Text = "Clear";
            buttonClearLog.UseVisualStyleBackColor = true;
            buttonClearLog.Click += ButtonClearLogClick;
            // 
            // btnCloseForm
            // 
            btnCloseForm.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnCloseForm.BackColor = Color.DodgerBlue;
            btnCloseForm.FlatStyle = FlatStyle.Flat;
            btnCloseForm.Font = new Font("Microsoft Sans Serif", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            btnCloseForm.ForeColor = SystemColors.ControlLightLight;
            btnCloseForm.Location = new Point(842, 0);
            btnCloseForm.Margin = new Padding(4, 5, 4, 5);
            btnCloseForm.Name = "btnCloseForm";
            btnCloseForm.Size = new Size(64, 43);
            btnCloseForm.TabIndex = 19;
            btnCloseForm.Text = "X";
            btnCloseForm.UseVisualStyleBackColor = false;
            btnCloseForm.Click += btnCloseFormClick;
            btnCloseForm.MouseEnter += Button1MouseEnter;
            btnCloseForm.MouseLeave += Button1MouseLeave;
            btnCloseForm.MouseHover += Button1MouseHover;
            // 
            // label5
            // 
            label5.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            label5.BackColor = Color.DodgerBlue;
            label5.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label5.ForeColor = SystemColors.ControlLightLight;
            label5.Location = new Point(1, 1);
            label5.Margin = new Padding(4, 0, 4, 0);
            label5.Name = "label5";
            label5.Size = new Size(779, 40);
            label5.TabIndex = 20;
            label5.Text = "Client controller vice versa";
            label5.TextAlign = ContentAlignment.MiddleCenter;
            label5.MouseDown += Label5MouseDown;
            label5.MouseMove += Label5MouseMove;
            label5.MouseUp += Label5MouseUp;
            // 
            // panel1
            // 
            panel1.BackColor = Color.Silver;
            panel1.Location = new Point(-1, 42);
            panel1.Margin = new Padding(0);
            panel1.Name = "panel1";
            panel1.Size = new Size(1, 479);
            panel1.TabIndex = 21;
            // 
            // panel4
            // 
            panel4.BackColor = Color.Silver;
            panel4.Location = new Point(0, 46);
            panel4.Margin = new Padding(0);
            panel4.Name = "panel4";
            panel4.Size = new Size(1, 529);
            panel4.TabIndex = 23;
            // 
            // labelSerialTX
            // 
            labelSerialTX.BackColor = SystemColors.ControlLightLight;
            labelSerialTX.Location = new Point(235, 124);
            labelSerialTX.Margin = new Padding(4, 0, 4, 0);
            labelSerialTX.Name = "labelSerialTX";
            labelSerialTX.Size = new Size(120, 35);
            labelSerialTX.TabIndex = 24;
            labelSerialTX.Text = "0";
            labelSerialTX.TextAlign = ContentAlignment.MiddleRight;
            // 
            // labelRxSerial
            // 
            labelRxSerial.BackColor = SystemColors.ControlLightLight;
            labelRxSerial.Location = new Point(671, 122);
            labelRxSerial.Margin = new Padding(4, 0, 4, 0);
            labelRxSerial.Name = "labelRxSerial";
            labelRxSerial.Size = new Size(121, 35);
            labelRxSerial.TabIndex = 25;
            labelRxSerial.Text = "0";
            labelRxSerial.TextAlign = ContentAlignment.MiddleRight;
            // 
            // label6
            // 
            label6.BackColor = Color.FromArgb(224, 224, 224);
            label6.Font = new Font("Microsoft Sans Serif", 10F);
            label6.Location = new Point(142, 124);
            label6.Margin = new Padding(4, 0, 4, 0);
            label6.Name = "label6";
            label6.Size = new Size(88, 35);
            label6.TabIndex = 26;
            label6.Text = "SerTX:";
            // 
            // label7
            // 
            label7.BackColor = Color.FromArgb(224, 224, 224);
            label7.Font = new Font("Microsoft Sans Serif", 10F);
            label7.Location = new Point(581, 124);
            label7.Margin = new Padding(4, 0, 4, 0);
            label7.Name = "label7";
            label7.Size = new Size(83, 33);
            label7.TabIndex = 27;
            label7.Text = "SerRX:";
            // 
            // buttonMinimize
            // 
            buttonMinimize.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            buttonMinimize.BackColor = Color.DodgerBlue;
            buttonMinimize.FlatStyle = FlatStyle.Flat;
            buttonMinimize.Font = new Font("Microsoft Sans Serif", 10.2F, FontStyle.Bold, GraphicsUnit.Point, 0);
            buttonMinimize.ForeColor = SystemColors.ControlLightLight;
            buttonMinimize.Location = new Point(779, 0);
            buttonMinimize.Margin = new Padding(4, 5, 4, 5);
            buttonMinimize.Name = "buttonMinimize";
            buttonMinimize.Size = new Size(64, 43);
            buttonMinimize.TabIndex = 28;
            buttonMinimize.Text = "_";
            buttonMinimize.UseVisualStyleBackColor = false;
            buttonMinimize.Click += ButtonMinimizeClick;
            // 
            // panel5
            // 
            panel5.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            panel5.BackColor = SystemColors.ControlLight;
            panel5.Location = new Point(288, 412);
            panel5.Margin = new Padding(4, 5, 4, 5);
            panel5.Name = "panel5";
            panel5.Size = new Size(41, 46);
            panel5.TabIndex = 29;
            // 
            // tbClientPort
            // 
            tbClientPort.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tbClientPort.Location = new Point(647, 84);
            tbClientPort.Margin = new Padding(4, 5, 4, 5);
            tbClientPort.Name = "tbClientPort";
            tbClientPort.Size = new Size(213, 30);
            tbClientPort.TabIndex = 3;
            // 
            // tbVesy31ip
            // 
            tbVesy31ip.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tbVesy31ip.Location = new Point(142, 51);
            tbVesy31ip.Margin = new Padding(4, 5, 4, 5);
            tbVesy31ip.Name = "tbVesy31ip";
            tbVesy31ip.Size = new Size(213, 30);
            tbVesy31ip.TabIndex = 30;
            // 
            // tbVesy31port
            // 
            tbVesy31port.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tbVesy31port.Location = new Point(142, 86);
            tbVesy31port.Margin = new Padding(4, 5, 4, 5);
            tbVesy31port.Name = "tbVesy31port";
            tbVesy31port.Size = new Size(213, 30);
            tbVesy31port.TabIndex = 31;
            // 
            // tbClientHost
            // 
            tbClientHost.Font = new Font("Microsoft Sans Serif", 12F, FontStyle.Regular, GraphicsUnit.Point, 0);
            tbClientHost.Location = new Point(647, 49);
            tbClientHost.Margin = new Padding(4, 5, 4, 5);
            tbClientHost.Name = "tbClientHost";
            tbClientHost.Size = new Size(213, 30);
            tbClientHost.TabIndex = 32;
            // 
            // contextMenuStrip1
            // 
            contextMenuStrip1.ImageScalingSize = new Size(20, 20);
            contextMenuStrip1.Name = "contextMenuStrip1";
            contextMenuStrip1.Size = new Size(61, 4);
            // 
            // MainForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(224, 224, 224);
            ClientSize = new Size(909, 472);
            Controls.Add(tbClientHost);
            Controls.Add(tbVesy31port);
            Controls.Add(tbVesy31ip);
            Controls.Add(panel5);
            Controls.Add(buttonMinimize);
            Controls.Add(label7);
            Controls.Add(label6);
            Controls.Add(labelRxSerial);
            Controls.Add(labelSerialTX);
            Controls.Add(panel4);
            Controls.Add(panel1);
            Controls.Add(btnCloseForm);
            Controls.Add(buttonClearLog);
            Controls.Add(buttonRefresh);
            Controls.Add(listBoxInfoTrace);
            Controls.Add(buttonStop);
            Controls.Add(label3);
            Controls.Add(buttonStart);
            Controls.Add(labelRemoteHost);
            Controls.Add(tbClientPort);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(label5);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(4, 5, 4, 5);
            Name = "MainForm";
            Text = "ARM to moxa bidi";
            Activated += MainFormActivated;
            Deactivate += MainFormDeactivate;
            FormClosing += MainFormFormClosing;
            Load += MainFormLoad;
            MouseDown += MainFormMouseDown;
            MouseMove += Form_MouseMove;
            MouseUp += Form_MouseUp;
            ResumeLayout(false);
            PerformLayout();
        }

        private void RadioButtonServerCheckedChanged(object sender, EventArgs e)
        {
            this.labelRemoteHost.Enabled = false;
            this.tbClientHost.Enabled = false;
        }

        private void RadioButtonClientCheckedChanged(object sender, EventArgs e)
        {
            this.labelRemoteHost.Enabled = true;
            this.tbClientHost.Enabled = true;
        }

        public static void SetDefaultCulture()
        {
            CultureInfo cultureInfo = CultureInfo.CreateSpecificCulture("en-US");
            Thread.CurrentThread.CurrentCulture = cultureInfo;
            Thread.CurrentThread.CurrentUICulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
            CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;

            Type type = typeof(CultureInfo);
            type.InvokeMember("s_userDefaultCulture",
                                BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static,
                                null,
                                cultureInfo,
                                new object[] { cultureInfo });

            type.InvokeMember("s_userDefaultUICulture",
                                BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Static,
                                null,
                                cultureInfo,
                                new object[] { cultureInfo });
        }

        private void btnCloseFormClick(object sender, EventArgs e)
        {
            this.HandleStop();
            this.Close();
        }
    }
}
