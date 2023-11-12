// Decompiled with JetBrains decompiler
// Type: serialtoip.ClientMode
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
  public class ClientMode
  {
    private volatile bool _run = true;
    private Connection conn;

    public void StopRequest()
    {
      if (this.conn != null)
        this.conn.StopRequest();
      this._run = false;
    }

    public int Run(
      Dictionary<string, string> d,
      SerialPort sp,
      CrossThreadComm.TraceCb traceFunc,
      CrossThreadComm.UpdateState updStat)
    {
      return this.Run(d, sp, traceFunc, updStat, (CrossThreadComm.UpdateRXTX) null);
    }

    public int Run(
      Dictionary<string, string> d,
      SerialPort sp,
      CrossThreadComm.TraceCb traceFunc,
      CrossThreadComm.UpdateState updStat,
      CrossThreadComm.UpdateRXTX updRxTx)
    {
      Console.WriteLine("SOCKET CLIENT MODE");
      this._run = true;
      IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(d["remotehost"]), int.Parse(d["socketport"].Trim()));
      Socket soc = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
      this.conn = new Connection();
      while (this._run)
      {
        if (!sp.IsOpen && this.conn.IsFree() && this.conn.StartConnection(soc, d, sp, traceFunc, updStat, updRxTx))
          soc = new Socket(ipEndPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        Thread.Sleep(1);
      }
      this.conn = (Connection) null;
      if (updStat != null)
        updStat((object) this, CrossThreadComm.State.terminate);
      return 0;
    }
  }
}
