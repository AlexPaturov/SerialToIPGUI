// Decompiled with JetBrains decompiler
// Type: serialtoip.ServerMode
// Assembly: SerialToIPGUI, Version=1.9.5866.19643, Culture=neutral, PublicKeyToken=null
// MVID: B1D67A1F-B8BA-49F3-B87A-0CFB4BE0BA84
// Assembly location: C:\Users\Alex\Downloads\SerialToIPGUI_v1.9_2016-01-23\SerialToIPGUI.exe

using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace serialtoip
{
    public class ServerMode
    {
        private volatile bool _run = true;
        private Connection conn;

        public int Run(
            Dictionary<string, string> d,
            Socket moxaTC,
            CrossThreadComm.TraceCb traceFunc,
            CrossThreadComm.UpdateState updState)
        {
            return this.Run(d, moxaTC, traceFunc, updState, (CrossThreadComm.UpdateRXTX)null);
        }

        public int Run(
            Dictionary<string, string> d,
            Socket moxaTC,
            CrossThreadComm.TraceCb traceFunc,
            CrossThreadComm.UpdateState updState,
            CrossThreadComm.UpdateRXTX updRxTx)
        {
            if (traceFunc != null)
                traceFunc((object)"SOCKET SERVER MODE");
            DateTime now = DateTime.Now;
            _run = true;

            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.Bind((EndPoint)new IPEndPoint(IPAddress.Any, int.Parse(d["clientPort"].Trim())));
            socket.Listen(1);
            socket.ReceiveTimeout = 10;

            while (_run)
            {
                Socket soc = (Socket)null;
                if (!moxaTC.Connected && socket.Poll(1000, SelectMode.SelectRead))
                    soc = socket.Accept();

                if (!moxaTC.Connected && soc != null)
                {
                    traceFunc((object)"Tcp client connected");
                    this.conn = new Connection();
                    try
                    {
                        this.conn.StartConnection(soc, d, moxaTC, traceFunc, updState, updRxTx);
                    }
                    catch (Exception ex)
                    {
                        traceFunc((object)"IP-to-SERIAL connection initialization failed");
                        traceFunc((object)ex.Message);
                        this.conn = (Connection)null;
                    }
                }
                else
                {
                    if (DateTime.Now.Subtract(now).TotalSeconds > 10.0)
                    {
                        traceFunc((object)"Server active and idle");
                        now = DateTime.Now;
                    }
                    Thread.Sleep(1);
                }
            }

            traceFunc((object)"Server shutting down");
            socket.Close();
            this.conn = (Connection)null;
            if (updState != null)
                updState((object)this, CrossThreadComm.State.terminate);
            return 0;
        }

        public void StopRequest()
        {
            if (this.conn != null)
                this.conn.StopRequest();
            this._run = false;
        }
    }
}
