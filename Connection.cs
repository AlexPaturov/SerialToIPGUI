// Decompiled with JetBrains decompiler
// Type: serialtoip.Connection
// Assembly: SerialToIPGUI, Version=1.9.5866.19643, Culture=neutral, PublicKeyToken=null
// MVID: B1D67A1F-B8BA-49F3-B87A-0CFB4BE0BA84
// Assembly location: C:\Users\Alex\Downloads\SerialToIPGUI_v1.9_2016-01-23\SerialToIPGUI.exe

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace serialtoip
{
    public class Connection
    {
        private CrossThreadComm.TraceCb _conInfoCallback;
        private CrossThreadComm.UpdateState _updState;
        private CrossThreadComm.UpdateRXTX _updRxTx;
        public Socket socket;
        private SerialPort _sp;
        private string _remotehost;
        private int _remoteport;
        private bool _isfree = true;
        private Dictionary<string, string> _d;
        private bool _keepOpen = true;

        public Connection()
        {
        }

        public Connection(
          Socket soc,
          Dictionary<string, string> d,
          SerialPort sp,
          CrossThreadComm.TraceCb conInfoCb,
          CrossThreadComm.UpdateState updState)
        {
            this.StartConnection(soc, d, sp, conInfoCb, updState);
        }

        public Connection(
          Socket soc,
          Dictionary<string, string> d,
          SerialPort sp,
          CrossThreadComm.TraceCb conInfoCb,
          CrossThreadComm.UpdateState updState,
          CrossThreadComm.UpdateRXTX updRxTx)
        {
            this.StartConnection(soc, d, sp, conInfoCb, updState, updRxTx);
        }

        public void SetConnInfoTraceCallback(CrossThreadComm.TraceCb conInfoCb) => this._conInfoCallback = conInfoCb;

        public bool IsFree() => this._isfree;

        public void TraceLine(string s)
        {
            if (this._conInfoCallback == null)
                return;
            this._conInfoCallback((object)s);
        }

        public void StopRequest() => this._keepOpen = false;

        public bool StartConnection(
          Socket soc,
          Dictionary<string, string> d,
          SerialPort sp,
          CrossThreadComm.TraceCb conInfoCb,
          CrossThreadComm.UpdateState updState)
        {
            return this.StartConnection(soc, d, sp, conInfoCb, updState, (CrossThreadComm.UpdateRXTX)null);
        }

        public bool StartConnection(
          Socket soc,
          Dictionary<string, string> d,
          SerialPort sp,
          CrossThreadComm.TraceCb conInfoCb,
          CrossThreadComm.UpdateState updState,
          CrossThreadComm.UpdateRXTX updRxTx)
        {
            this.socket = soc;
            this._sp = sp;
            this._d = d;
            this.SetConnInfoTraceCallback(conInfoCb);
            this._updState = updState;
            this._updRxTx = updRxTx;

            if (this._updState != null)
                this._updState((object)this, CrossThreadComm.State.start);

            this._remotehost = d["remotehost"];
            this._remoteport = int.Parse(d["socketport"].Trim());
            this._isfree = false;
            
            if (this._remotehost != null)
            {
                if (this._remotehost.Length > 3)
                {
                    try
                    {
                        this.TraceLine("Connecting to remote host " + this._remotehost + ":" + this._remoteport.ToString());
                        this.socket.Connect(this._remotehost, this._remoteport);
                    }
                    catch (Exception ex)
                    {
                        this.TraceLine("CLIENT SOCKET CONNECT - ERROR OCCURED:\r\n" + ex.ToString());
                        throw ex;
                    }
                }
            }

            if (!this._sp.IsOpen)
            {
                this.TraceLine("Trying to open serial port " + this._sp.PortName);
                try
                {
                    this._sp.Open();
                }
                catch (Exception ex)
                {
                    this.TraceLine("SERIAL PORT OPEN - ERROR OCCURED:\r\n" + ex.ToString());
                    if (this.socket.Connected)
                        this.socket.Close();
                    throw ex;
                }
                this.TraceLine("Serial port opened OK");
            }
            new Thread(new ThreadStart(this.Tranceiver)).Start();
            return true;
        }

        private void DoStartupActions()
        {
            if (this._d["socksend"] != null)
            {
                this.TraceLine("Sending something to socket ");
                byte[] bytes = new UTF8Encoding().GetBytes(this._d["socksend"]);
                this.socket.Send(bytes, bytes.Length, SocketFlags.None);
            }
            if (this._d["sockfilesend"] == null)
                return;
            this.TraceLine("Sending file " + this._d["sockfilesend"] + " to socket");
            byte[] buffer = File.ReadAllBytes(this._d["sockfilesend"]);
            this.socket.Send(buffer, buffer.Length, SocketFlags.None);
        }

        private int LimitTo(int i, int limit) => i > limit ? limit : i;

        private void Tranceiver()
        {
            byte[] buffer = new byte[8192];
            this._keepOpen = true;
            this.TraceLine("Client connected from " + this.socket.RemoteEndPoint.ToString());
           
            if (this._updState != null)
                this._updState((object)this, CrossThreadComm.State.connect);
            //this.DoStartupActions();
           
            while (this._keepOpen)
            {
                bool flag = false;
                int num1 = this.LimitTo(this.socket.Available, 8192);
                if (num1 > 0)
                {
                    if (this._updRxTx != null)
                        this._updRxTx((object)this, 0, num1);
                    this.TraceLine("IP-to-SERIAL " + num1.ToString());
                    this.socket.Receive(buffer, num1, SocketFlags.None);
                    this._sp.Write(buffer, 0, num1);
                    flag = true;
                }
                int num2 = this.LimitTo(this._sp.BytesToRead, 8192);
                
                if (num2 > 0)
                {
                    if (this._updRxTx != null)
                        this._updRxTx((object)this, num2, 0);
                    this.TraceLine("SERIAL-to-IP " + num2.ToString());
                    this._sp.Read(buffer, 0, num2);
                    this.socket.Send(buffer, num2, SocketFlags.None);
                    flag = true;
                }
                
                if (this.socket.Poll(3000, SelectMode.SelectRead) & this.socket.Available == 0)
                {
                    this.TraceLine("IP connection lost ");
                    this._keepOpen = false;
                }
                
                if (!flag)
                    Thread.Sleep(1);
            }

            if (this._updState != null)
                this._updState((object)this, CrossThreadComm.State.disconnect);

            this.TraceLine("Client disconnected from " + this.socket.RemoteEndPoint.ToString());
            
            if (this._sp.IsOpen)
            {
                this.TraceLine("Closing the serial port " + this._sp.PortName);
                this._sp.Close();
            }
            
            this.socket.Close();
            this._isfree = true;
        }
    }
}
