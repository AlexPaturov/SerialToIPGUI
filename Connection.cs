using System;
using System.Collections.Generic;
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
        public Socket ARMsocket;
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
            ARMsocket = soc;
            _moxaTC = moxaTC;
            _d = d;
            _updState = updState;
            _updRxTx = updRxTx;
            SetConnInfoTraceCallback(conInfoCb);

            if (_updState != null)
                _updState((object)this, CrossThreadComm.State.start);
            _isfree = false;

            if (!_moxaTC.Connected)
            {
                TraceLine("Trying to connect to weighter ARM " + _d["moxaHost"] + ":" + _d["moxaPort"]);
                try
                {
                    _moxaTC = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // так себе решение
                    _moxaTC.ReceiveTimeout = 500;
                    _moxaTC.SendTimeout = 500;  
                    _moxaTC.Connect(_d["moxaHost"], int.Parse(_d["moxaPort"]));
                }
                catch (Exception ex)
                {
                    TraceLine("Error connect to weighter ARM:" + "\r\n" + ex.ToString());
                    if (ARMsocket.Connected)
                    {
                        ARMsocket.Shutdown(SocketShutdown.Both); // не тестировал
                        ARMsocket.Close();
                    }
                    throw ex;
                }
                TraceLine("Connect to weighter ARM success. " + _d["moxaHost"] + ":" + _d["moxaPort"]); // подключение к АРМ весов - спецификация
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
                bool flag = false; // флаг передачи данных клиент <-> контроллер
                int colByteClient = LimitTo(ARMsocket.Available, 8192);
                
                // ------------------------------ от клиента пришёл запрос begin -----------------------------------------------------------------   
                if (colByteClient > 0) // 
                {
                    if (_updRxTx != null)
                        _updRxTx((object)this, 0, colByteClient);
                    
                    ARMsocket.Receive(buffer, colByteClient, SocketFlags.None); 
                    
                    byte[] controllerCommand = DecodeClientRequestToControllerCommand(buffer, colByteClient); // Верну null если команда не корректная, иначе - команду для контроллера
                    if (controllerCommand != null)                                                          // команда корректна -> отправляем устройству
                    {
                        try 
                        { 
                            _moxaTC.Send(controllerCommand, controllerCommand.Length, SocketFlags.None);
                            Thread.Sleep(600);                                                              // подождём пока данные прийдут. На 200 - сыплет ошибки.
                            flag = true;                                                                    // передача данных контроллеру была
                        } 
                        catch (SocketException ex) 
                        {
                            if (ex.SocketErrorCode == SocketError.TimedOut)
                            {
                                Exception exInner = new Exception("Прибор не ответил на запрос");           // Согласно спецификации.
                                byte[] errorByteArr = XMLFormatter.GetError(exInner, 1);                    // Отформатировал ошибку в XML формат. 
                                ARMsocket.Send(errorByteArr, errorByteArr.Length, SocketFlags.None);        // Отправляем в АРМ весов XML в виде byte[].
                            }
                            throw ex;
                        }
                        catch (Exception ex) 
                        {
                            throw ex;
                        }
                    }
                    else                                                                        // формирование для клиента сообщения об ошибке
                    {
                        Exception ex = new Exception("Ошибка обработки запроса");               // Согласно спецификации.
                        byte[] errorByteArr = XMLFormatter.GetError(ex, 2);                     // Отформатировал ошибку в XML формат. 
                        ARMsocket.Send(errorByteArr, errorByteArr.Length, SocketFlags.None);    // Отправляем в АРМ весов XML в виде byte[].
                    }
                }
                // ------------------------------ от клиента пришёл запрос end --------------------------------------------------------------

                // ------------------------------ от моксы пришёл ответ --------------------------------------------------------------------- 
                int colByteFromMoxa = LimitTo(_moxaTC.Available, 8192);
                if (colByteFromMoxa > 0)
                {
                    if (_updRxTx != null)
                        _updRxTx((object)this, colByteFromMoxa, 0);

                    try
                    {
                        _moxaTC.Receive(buffer, colByteFromMoxa, SocketFlags.None);
                    }
                    catch (SocketException ex)
                    {
                        if (ex.SocketErrorCode == SocketError.TimedOut)                                 // по таймауту - делаю "своё" исключение
                        {
                            Exception exInner = new Exception("Прибор не ответил на запрос");           // Согласно спецификации.
                            byte[] errorByteArr = XMLFormatter.GetError(exInner, 1);                    // Отформатировал ошибку в XML формат. 
                            ARMsocket.Send(errorByteArr, errorByteArr.Length, SocketFlags.None);        // Отправляем в АРМ весов XML в виде byte[].
                        }
                        throw ex;
                    }
                    catch (Exception ex)
                    {
                        throw ex;
                    }

                    #region only for show buffer data to textbox
                    byte[] moxaArr = new byte[colByteFromMoxa];                                         // установил размерность нового массива
                    Buffer.BlockCopy(buffer, 0, moxaArr, 0, colByteFromMoxa);                           // копирую данные в промежуточный массив для отображения
                    TraceLine("moxa to client " + colByteFromMoxa.ToString() + "  " + Encoding.GetEncoding(1251).GetString(buffer) + "  " + Encoding.GetEncoding(1251).GetString(moxaArr));
                    #endregion
                    
                    byte[] btArr = null;
                    try
                    {
                        btArr = XMLFormatter.getStatic(moxaArr);                                        // Если возникла ошибка при разборе сообщения - кидаю исключение.
                    } 
                    catch (Exception ex) 
                    {
                        btArr = XMLFormatter.GetError(ex, 100);                                         // Отформатировал ошибку в XML формат, перевёл в byte[]
                    }

                    ARMsocket.Send(btArr, btArr.Length, SocketFlags.None);
                    flag = true;
                }
                // ------------------------------ от моксы пришёл ответ ---------------------------------------------------------------------

                if (ARMsocket.Poll(3000, SelectMode.SelectRead) & ARMsocket.Available == 0)
                {
                    TraceLine("Lost connection to " + ARMsocket.RemoteEndPoint.ToString());
                    _keepOpen = false;
                }

                if (!flag)
                    Thread.Sleep(1);
            }
            
            if (_updState != null)
                _updState((object)this, CrossThreadComm.State.disconnect);
            TraceLine("Client disconnected from " + ARMsocket.RemoteEndPoint.ToString());
            
            if (_moxaTC.Connected)
            {
                TraceLine("Closing the moxa connection " + _moxaTC.RemoteEndPoint.ToString());  // подключение к АРМ весов
                _moxaTC.Shutdown(SocketShutdown.Both);
                _moxaTC.Disconnect(true);
                _moxaTC.Close();
            }

            ARMsocket.Shutdown(SocketShutdown.Both);
            ARMsocket.Close();
            _isfree = true;
        }

        private int LimitTo(int i, int limit) => i > limit ? limit : i;

        // Перевожу команду полученную от клиента в команду для исполнения контроллером
        private byte[] DecodeClientRequestToControllerCommand(byte[] cliBuffer, int dataLength)
        {
            string controllerCommand = string.Empty;
            byte[] clientComandArr = new byte[dataLength];                      // установил размерность массива для команды клиента
            Buffer.BlockCopy(cliBuffer, 0, clientComandArr, 0, dataLength);     // копирую данные в промежуточный массив 
            
            switch (Encoding.GetEncoding(1251).GetString(clientComandArr))
            {
                case "<Request method='set_mode' parameter='Static'/>":         // 1)
                    //controllerCommand = "" + "\r\n";
                    controllerCommand = null;
                    break;

                case "<Request method='checksum'/>":                            // 2)
                    //controllerCommand = "" + "\r\n";
                    controllerCommand = null;
                    break;

                case "<Request method='get_static'/>":                          // 3) получить вес, взвешивание в статике
                    controllerCommand = "F#1" + "\r\n";
                    break;

                case "<Request method='set_zero' parameter='0'/>":              // 4)
                    //controllerCommand = "" + "\r\n";
                    controllerCommand = null;
                    break;

                case "<Request method='restart_weight'/>":                      // 5)
                    //controllerCommand = "" + "\r\n";
                    controllerCommand = null;
                    break;

                default:                                                        // 6) Команда полученная от клиента - не распознана.
                    //controllerCommand = "" + "\r\n";
                    controllerCommand = null;
                    break;
            }

            // Если команда распознана - перекодирую в байтовый массив и возвращаю, иначе верну null.
            if (controllerCommand.Length > 0)
            {
                return Encoding.GetEncoding(1251).GetBytes(controllerCommand);
            }
            else 
            { 
                return null;
            }
        }

    }
}
