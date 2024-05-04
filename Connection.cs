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
using System.Runtime.InteropServices.ComTypes;
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
        private Socket _moxaTC;
        private bool _isfree = true;
        private Dictionary<string, string> _d;
        private bool _keepOpen = true;

        public Connection()
        {
        }

        public Connection(
            Socket soc,
            Dictionary<string, string> d,
            Socket moxaTC,
            CrossThreadComm.TraceCb conInfoCb,
            CrossThreadComm.UpdateState updState)
        {
            StartConnection(soc, d, moxaTC, conInfoCb, updState);
        }

        public Connection(
            Socket soc,
            Dictionary<string, string> d,
            Socket moxaTC,
            CrossThreadComm.TraceCb conInfoCb,
            CrossThreadComm.UpdateState updState,
            CrossThreadComm.UpdateRXTX updRxTx)
        {
            StartConnection(soc, d, moxaTC, conInfoCb, updState, updRxTx);
        }

        public void SetConnInfoTraceCallback(CrossThreadComm.TraceCb conInfoCb) => _conInfoCallback = conInfoCb;

        public bool IsFree() => _isfree;

        public void TraceLine(string s)
        {
            if (_conInfoCallback == null)
                return;
            _conInfoCallback((object)s);
        }

        public void StopRequest() => _keepOpen = false;

        public bool StartConnection(
          Socket soc,
          Dictionary<string, string> d,
          Socket moxaTC,
          CrossThreadComm.TraceCb conInfoCb,
          CrossThreadComm.UpdateState updState)
        {
            return StartConnection(soc, d, moxaTC, conInfoCb, updState, (CrossThreadComm.UpdateRXTX)null);
        }

        public bool StartConnection(
          Socket soc,
          Dictionary<string, string> d,
          Socket moxaTC,
          CrossThreadComm.TraceCb conInfoCb,
          CrossThreadComm.UpdateState updState,
          CrossThreadComm.UpdateRXTX updRxTx)
        {
            socket = soc;
            _moxaTC = moxaTC;
            _d = d;
            SetConnInfoTraceCallback(conInfoCb);
            _updState = updState;
            _updRxTx = updRxTx;

            if (_updState != null)
                _updState((object)this, CrossThreadComm.State.start);
            _isfree = false;

            if (!_moxaTC.Connected)
            {
                TraceLine("Trying to connect to client " + _d["moxaHost"] + ":" + _d["moxaPort"]);
                try
                {
                    _moxaTC.Connect(_d["moxaHost"], int.Parse(_d["moxaPort"]));
                }
                catch (Exception ex)
                {
                    TraceLine("CONNECT TO CLIENT - ERROR OCCURED:\r\n" + ex.ToString());
                    if (socket.Connected)
                        socket.Close();
                    throw ex;
                }
                TraceLine("CONNECT TO CLIENT - OK");
            }
            new Thread(new ThreadStart(Tranceiver)).Start();
            return true;
        }

        private void Tranceiver()
        {
            byte[] buffer = new byte[8192];
            _keepOpen = true;
            string cliCmd = string.Empty;


            TraceLine("Moxa connected from " + _d["moxaHost"] +":"+ int.Parse(_d["moxaPort"]));
            
            if (_updState != null)
                _updState((object)this, CrossThreadComm.State.connect);
            
            while (_keepOpen)
            {
                bool flag = false;

                int colByteClient = LimitTo(socket.Available, 8192);
                // ------------------------------ от клиента пришёл запрос begin -----------------------------------------------------------------   
                if (colByteClient > 0) // 
                {
                    if (_updRxTx != null)
                        _updRxTx((object)this, 0, colByteClient);
                    
                    // здесь нужен будет трай, который перехватит, обработает, если клиент живой - вернёт в правильном формате.
                    socket.Receive(buffer, colByteClient, SocketFlags.None);    // получил данные в буфер
                    
                    #region only for show buffer data to textbox
                    byte[] newArray = new byte[colByteClient];                  // установил размерность нового массива
                    Buffer.BlockCopy(buffer, 0, newArray, 0, colByteClient);    // копирую данные в промежуточный массив для отображения
                    TraceLine("client to moxa " + colByteClient.ToString() + "  " + Encoding.GetEncoding(1251).GetString(newArray));
                    #endregion

                    // ------- в зависимости от команды полученной от клиента отправляю в контроллер команду begin ------------------------------

                    cliCmd = Encoding.GetEncoding(1251).GetString(newArray);
                    string RLCmd = string.Empty;
                    switch (cliCmd) 
                    {
                        case "<Request method='set_mode' parameter='Static'/>" :  // 1)
                            RLCmd = "" + "\r\n";
                            break;

                        case "<Request method='checksum'/>":                      // 2)
                            RLCmd = "" + "\r\n";
                            break;

                        case "<Request method='get_static'/>":                    // 3) получить вес, взвешивание в статике
                            RLCmd = "F#1"+"\r\n";
                            break;

                        case "<Request method='set_zero' parameter='0'/>":        // 4)
                            RLCmd = "" + "\r\n";
                            break;

                        case "<Request method='restart_weight'/>":                // 5)
                            RLCmd = "" + "\r\n";
                            break;

                        default:
                            RLCmd = "" + "\r\n";
                            break;
                    }
                    // ------- end --------------------------------
                    _moxaTC.Send(Encoding.GetEncoding(1251).GetBytes(RLCmd), Encoding.GetEncoding(1251).GetBytes(RLCmd).Length, SocketFlags.None);                   
                    Thread.Sleep(200); // подождём пока данные прийдут - а надо ?
                    flag = true;
                }
                // ------------------------------ от клиента пришёл запрос end --------------------------------------------------------------

                int colByteMoxa = LimitTo(_moxaTC.Available, 8192);

                // ------------------------------ от моксы пришёл ответ -------------------------------------------------------------- 
                if (colByteMoxa > 0)
                {
                    if (_updRxTx != null)
                        _updRxTx((object)this, colByteMoxa, 0);

                    _moxaTC.Receive(buffer, colByteMoxa, SocketFlags.None);
                    
                    #region only for show buffer data to textbox
                    byte[] moxaArr = new byte[colByteMoxa];                  // установил размерность нового массива
                    Buffer.BlockCopy(buffer, 0, moxaArr, 0, colByteMoxa);    // копирую данные в промежуточный массив для отображения
                    TraceLine("moxa to client " + colByteMoxa.ToString() + "  " + Encoding.GetEncoding(1251).GetString(buffer) + "  " + Encoding.GetEncoding(1251).GetString(moxaArr));
                    #endregion
                    
                    // ------------------- здесь я буду обрабатывать и декодировать сообщение от моксы begin -------------------------------

                    // ------------------- здесь я буду обрабатывать и декодировать сообщение от моксы end ---------------------------------
                    
                    socket.Send(buffer, colByteMoxa, SocketFlags.None);
                    flag = true;
                }
                // ------------------------------ от моксы пришёл ответ --------------------------------------------------------------

                if (socket.Poll(3000, SelectMode.SelectRead) & socket.Available == 0)
                {
                    TraceLine("IP connection lost ");
                    _keepOpen = false;
                }

                if (!flag)
                    Thread.Sleep(1);
            }
            
            if (_updState != null)
                _updState((object)this, CrossThreadComm.State.disconnect);
            TraceLine("Client disconnected from " + socket.RemoteEndPoint.ToString());
            
            if (_moxaTC.Connected)
            {
                TraceLine("Closing the moxa connection " + _moxaTC.RemoteEndPoint.ToString());
                _moxaTC.Close();
            }

            socket.Close();
            _isfree = true;
        }

        private int LimitTo(int i, int limit) => i > limit ? limit : i;
    }
}
