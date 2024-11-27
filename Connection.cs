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

        // добавить udp клиента 
        public Socket _moxaTC;
        private bool _isfree = true;
        private Dictionary<string, string> _d;
        private bool _keepOpen = true;
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

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

        public void StopRequest()
        {
            _keepOpen = false;
        }

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

            // добавить udp клиента 
            _moxaTC = moxaTC;
            _d = d;
            _updState = updState;
            _updRxTx = updRxTx;
            SetConnInfoTraceCallback(conInfoCb);

            if (_updState != null)
                _updState((object)this, CrossThreadComm.State.start);
            _isfree = false;

            // добавить udp клиента 
            if (!_moxaTC.Connected)
            {
                TraceLine("Trying to connect to weighter ARM " + _d["moxaHost"] + ":" + _d["moxaPort"]);
                try
                {
                    // добавить udp клиента 
                    _moxaTC = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp); // так себе решение
                    _moxaTC.ReceiveTimeout = 500;
                    _moxaTC.SendTimeout = 500;  
                    _moxaTC.Connect(_d["moxaHost"], int.Parse(_d["moxaPort"]));
                }
                catch (Exception ex)
                {
                    TraceLine("Error connect to weighter ARM:" + "\r\n" + ex.ToString());
                    logger.Error(ex);
                    
                    if (ARMsocket.Connected)
                    {
                        // протестировать throw exception 20.05.2024
                        // ARMsocket.Shutdown(SocketShutdown.Both); // не тестировал
                        ARMsocket.Close();
                    }
                    // throw ex;
                }

                // добавить udp клиента 
                TraceLine("Connection to controller - OK " + _moxaTC.RemoteEndPoint.ToString()); // подключение к контроллеру - спецификация
                logger.Info("Connection to controller - OK " + _moxaTC.RemoteEndPoint.ToString());
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
                bool transferOccured = false; // флаг свершения передачи данных клиент <-> контроллер
                
                // ------------------------------ от клиента пришёл запрос begin -----------------------------------------------------------------   
                int colByteARM = LimitTo(ARMsocket.Available, 8192);
                if (colByteARM > 0) // 
                {
                    if (_updRxTx != null)
                        _updRxTx((object)this, 0, colByteARM);
                    
                    ARMsocket.Receive(buffer, colByteARM, SocketFlags.None);
                    byte[] ARMbyteArr = new byte[colByteARM];                                               // установил размерность нового массива
                    Buffer.BlockCopy(buffer, 0, ARMbyteArr, 0, colByteARM);
                    logger.Info("ARM query string: " + Encoding.GetEncoding(1251).GetString(ARMbyteArr));   // строка запроса прикладного ПО драйверу

                    byte[] controllerCommand = DecodeClientRequestToControllerCommand(buffer, colByteARM);  // Верну null если команда не корректная, иначе - команду для контроллера
                    if (controllerCommand != null)                                                          // команда корректна -> отправляем устройству
                    {
                        try 
                        {
                            // добавить udp клиента 
                            _moxaTC.Send(controllerCommand, controllerCommand.Length, SocketFlags.None);    // запрос драйвера контроллеру
                            logger.Info(Encoding.GetEncoding(1251).GetString(controllerCommand));           // команда контроллеру
                            Thread.Sleep(600);                                                              // подождём пока данные прийдут. На 200 - сыплет ошибки.
                            transferOccured = true;                                                                    // передача данных контроллеру была
                        } 
                        catch (SocketException ex) 
                        {
                            if (ex.SocketErrorCode == SocketError.TimedOut)
                            {
                                Exception exInner = new Exception("Прибор не ответил на запрос");           // Согласно спецификации - ловить таймаут
                                byte[] errorByteArr = XMLFormatter.GetError(exInner, 1);                    // Отформатировал ошибку в XML формат. 
                                ARMsocket.Send(errorByteArr, errorByteArr.Length, SocketFlags.None);        // Отправляем в АРМ весов XML в виде byte[].
                            }
                            TraceLine(ex.StackTrace + " " + ex.Message);
                            logger.Error(ex);
                        }
                        
                    }
                    else                                                                        // формирование для клиента сообщения об ошибке в его запросе.
                    {
                        Exception ex = new Exception("Ошибка обработки запроса");               // Согласно спецификации.
                        byte[] errorByteArr = XMLFormatter.GetError(ex, 2);                     // Отформатировал ошибку в XML формат. 
                        ARMsocket.Send(errorByteArr, errorByteArr.Length, SocketFlags.None);    // Отправляем в АРМ весов XML в виде byte[].
                        logger.Error(Encoding.GetEncoding(1251).GetString(errorByteArr));       // пишу ответ для арма весов.
                    }
                }
                // ------------------------------ от клиента пришёл запрос end --------------------------------------------------------------

                // ------------------------------ от моксы пришёл ответ начало --------------------------------------------------------------------- 
                // добавить udp клиента 
                int colByteFromMoxa = LimitTo(_moxaTC.Available, 8192);
                if (colByteFromMoxa > 0)
                {
                    if (_updRxTx != null)
                        _updRxTx((object)this, colByteFromMoxa, 0);

                    try
                    {
                        // добавить udp клиента 
                        _moxaTC.Receive(buffer, colByteFromMoxa, SocketFlags.None);
                    }
                    catch (SocketException ex)
                    {
                        logger.Error(ex);

                        if (ex.SocketErrorCode == SocketError.TimedOut)                                 // по таймауту - делаю "своё" исключение
                        {
                            Exception exInner = new Exception("Прибор не ответил на запрос");           // Согласно спецификации.
                            byte[] errorByteArr = XMLFormatter.GetError(exInner, 1);                    // Отформатировал ошибку в XML формат. 
                            ARMsocket.Send(errorByteArr, errorByteArr.Length, SocketFlags.None);        // Отправляем в АРМ весов XML в виде byte[].
                        }
                    }    

                    #region only for show buffer data to textbox
                    byte[] moxaArr = new byte[colByteFromMoxa];                                         // установил размерность нового массива
                    Buffer.BlockCopy(buffer, 0, moxaArr, 0, colByteFromMoxa);                           // копирую данные в промежуточный массив для отображения
                    TraceLine("moxa to client " + colByteFromMoxa.ToString() + "  " + Encoding.GetEncoding(1251).GetString(buffer) + "  " + Encoding.GetEncoding(1251).GetString(moxaArr));
                    logger.Info(Encoding.GetEncoding(1251).GetString(moxaArr)); // ответ драйвера прикладному ПО
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
                    
                    transferOccured = true;
                }
                // ------------------------------ от моксы пришёл ответ окончание ---------------------------------------------------------------------

                if (ARMsocket.Poll(3000, SelectMode.SelectRead) & ARMsocket.Available == 0)
                {
                    TraceLine("Lost connection to weighter ARM " + ARMsocket.RemoteEndPoint.ToString());
                    _keepOpen = false;
                }

                if (!transferOccured)
                    Thread.Sleep(1);
            }
            
            if (_updState != null)
                _updState(this, CrossThreadComm.State.disconnect);
            //TraceLine("Client disconnected from " + ARMsocket.RemoteEndPoint.ToString());

            // добавить udp клиента 
            if (_moxaTC.Connected)
            {
                // добавить udp клиента 
                TraceLine("Connect to controller is closed " + _moxaTC.RemoteEndPoint.ToString());  // отключение от контроллера
                logger.Info("Connect to controller is closed " + _moxaTC.RemoteEndPoint.ToString());
                //_moxaTC.Shutdown(SocketShutdown.Both);
                //_moxaTC.Disconnect(true);

                // добавить udp клиента 
                _moxaTC.Close();
            }

            //ARMsocket.Shutdown(SocketShutdown.Both);
            logger.Info("Connect to weighter ARM is closed " + ARMsocket.RemoteEndPoint.ToString()); // отключение от ARM весов
            ARMsocket.Close();
            _isfree = true;
        }

        private int LimitTo(int i, int limit) => i > limit ? limit : i;

        // Перевожу команду полученную от клиента в команду для исполнения контроллером
        private byte[] DecodeClientRequestToControllerCommand(byte[] cliBuffer, int dataLength)
        {
            string controllerCommand = string.Empty;
            byte[] clientComandArr = new byte[dataLength];                     // установил размерность массива для команды клиента
            Buffer.BlockCopy(cliBuffer, 0, clientComandArr, 0, dataLength);    // копирую данные в промежуточный массив 
            
            switch (Encoding.GetEncoding(1251).GetString(clientComandArr))
            {
                case "<Request method='set_mode' parameter='Static'/>":     // 1)
                    controllerCommand = null;
                    break;

                case "<Request method='checksum'/>":                        // 2)
                    controllerCommand = null;
                    break;

                case "<Request method='get_static'/>":                      // 3) получить вес, взвешивание в статике
                    controllerCommand = "F#1" + "\r\n";
                    break;

                case "<Request method='set_zero' parameter='0'/>":          // 4)
                    controllerCommand = null;
                    break;

                case "<Request method='restart_weight'/>":                  // 5)
                    controllerCommand = null;
                    break;

                default:                                                    // 6) Команда полученная от клиента - не распознана.
                    controllerCommand = null;
                    break;
            }

            // Если команда распознана - перекодирую в байтовый массив и возвращаю, иначе верну null.
            if (!string.IsNullOrEmpty(controllerCommand) && controllerCommand.Length > 0)
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
