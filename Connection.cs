using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace serialtoip
{
    internal enum ControllerCommand 
    {
        GetWeight,
        SetZero = 1,
        Reboot = 2
    }

    public class Connection
    {
        private CrossThreadComm.TraceCb _conInfoCallback;
        private CrossThreadComm.UpdateState _updState;
        private CrossThreadComm.UpdateRXTX _updRxTx;
        public Socket ARMsocket;
        private CancellationTokenSource _udpCts;
        private UdpClient _udpServer;
        // IPEndPoint _epContr31; // на 9.12.2024 передачи данных не ожидается

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
            UdpClient udpServer,
            CancellationTokenSource udpCts,
            CrossThreadComm.TraceCb conInfoCb,
            CrossThreadComm.UpdateState updState)
        {
            StartConnection(
                            soc,
                            d, 
                            udpServer,
                            udpCts,
                            conInfoCb, 
                            updState);
        }

        public Connection(
            Socket soc,
            Dictionary<string, string> d,
            UdpClient udpServer,
            CancellationTokenSource udpCts,
            CrossThreadComm.TraceCb conInfoCb,
            CrossThreadComm.UpdateState updState,
            CrossThreadComm.UpdateRXTX updRxTx)
        {
            StartConnection(soc, 
                            d, 
                            udpServer, 
                            udpCts,  
                            conInfoCb, 
                            updState, 
                            updRxTx);
        }

        public void SetConnInfoTraceCallback(CrossThreadComm.TraceCb conInfoCb) => _conInfoCallback = conInfoCb;
        public bool IsFree() => _isfree;
        #region TraceLine()
        public void TraceLine(string s)
        {
            if (_conInfoCallback == null)
                return;
            _conInfoCallback((object)s);
        }
        #endregion

        public void StopRequest()
        {
            _keepOpen = false;
        }

        public bool StartConnection(
          Socket soc,
          Dictionary<string, string> d,
          UdpClient udpServer,
          CancellationTokenSource udpCts,
          CrossThreadComm.TraceCb conInfoCb,
          CrossThreadComm.UpdateState updState)
        {
            return StartConnection(soc, 
                                   d, 
                                   udpServer, 
                                   udpCts, 
                                   conInfoCb, 
                                   updState, 
                                   (CrossThreadComm.UpdateRXTX)null);
        }

        public bool StartConnection(
          Socket soc,
          Dictionary<string, string> d,
          UdpClient udpServer,
          CancellationTokenSource udpCts,
          CrossThreadComm.TraceCb conInfoCb,
          CrossThreadComm.UpdateState updState,
          CrossThreadComm.UpdateRXTX updRxTx)
        {
            ARMsocket = soc;
            _udpServer = udpServer;
            _udpCts = udpCts;   
            _d = d;
            _updState = updState;
            _updRxTx = updRxTx;
            //_epContr31 = new IPEndPoint(IPAddress.Parse(_d["vesy31ip"]), Int32.Parse(_d["vesy31port"]));

            #region Обновляю текстовые данные на главной форме
            SetConnInfoTraceCallback(conInfoCb);
            if (_updState != null)
                _updState((object)this, CrossThreadComm.State.start);
            #endregion
            _isfree = false;

            if (_udpServer is null)
            {
                try
                {
                    if (_udpCts is null)
                        _udpCts = new CancellationTokenSource();
                    _udpServer = new UdpClient(Int32.Parse(_d["vesy31port"]));
                }
                catch (Exception ex)
                {
                    TraceLine("Error connect to controller:" + "\r\n" + ex.ToString());
                    logger.Error(ex);
                    
                    if (ARMsocket.Connected)
                    {
                        // ПРОВЕРИТЬ ДОСТУПНОСТЬ контроллера  - ЕСЛИ НЕ ДОСТУПЕН - СФОРМИРОВАТЬ СООБЩЕНИЕ СОГЛАСНО ФОРМАТУ В ТЕХ ТРЕБОВАНИЯХ
                        ARMsocket.Close();
                    }
                    throw;
                }
            }
            new Thread(new ThreadStart(Tranceiver)).Start();
            return true;
        }

        private async void Tranceiver()
        {
            byte[] buffer = new byte[8192];
            _keepOpen = true;
            string cliCmd = string.Empty;
            #region Write a log to main form
            if (_updState != null)
                _updState((object)this, CrossThreadComm.State.connect);
            #endregion

            while (_keepOpen)
            {
                bool transferOccured = false; // флаг свершения передачи данных клиент <-> контроллер
                
                // ------------------------------ от клиента пришёл запрос begin -----------------------------------------------------------------   
                int colByteARM = LimitTo(ARMsocket.Available, 8192);
                if (colByteARM > 0)
                {
                    #region Обновляю счётчик байтов на главной форме
                    if (_updRxTx != null)
                        _updRxTx((object)this, 0, colByteARM);
                    #endregion

                    ARMsocket.Receive(buffer, colByteARM, SocketFlags.None);
                    byte[] ARMbyteArr = new byte[colByteARM];                                               // установил размерность нового массива
                    Buffer.BlockCopy(buffer, 0, ARMbyteArr, 0, colByteARM);
                    #region Запись в лог "строка запроса прикладного ПО драйверу"
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                    logger.Info("ARM query string: " + Encoding.GetEncoding(1251).GetString(ARMbyteArr));   // строка запроса прикладного ПО драйверу
                    #endregion
                    byte[] controllerCommand = DecodeClientRequestToControllerCommand(buffer, colByteARM);  // Верну null если команда не корректная, иначе - команду для контроллера
                    if (controllerCommand != null)                                                          // команда корректна -> отправляем устройству
                    {
                        try 
                        {
                            #region Если команда отлична от "Получить вес", то отправляю запрос без ожидания ответа - закомичено
                            //System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                            //if (Encoding.GetEncoding(1251).GetString(ARMbyteArr) != "<Request method='get_static'/>") 
                            //{
                            //    _udpServer.Send(controllerCommand, controllerCommand.Length, _epContr31);    // запрос от драйвера к контроллеру
                            //}
                            #endregion

                            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                            if (Encoding.GetEncoding(1251).GetString(ARMbyteArr) == "<Request method='get_static'/>")
                            {
                                try
                                {
                                    #region Если ресурсы _udpCts, _udpServer  закрыты - возобновляю 
                                    if (_udpCts is null) 
                                    {
                                        _udpCts = new CancellationTokenSource();
                                    }

                                    if (_udpServer is null)
                                    {
                                        _udpServer = new UdpClient(Int32.Parse(_d["vesy31port"]));
                                    }
                                    #endregion

                                    while (!_udpCts.Token.IsCancellationRequested)
                                    {
                                        // Use Task.Run to allow for cancellation checks
                                        var receiveTask = _udpServer.ReceiveAsync();

                                        // Wait for data or handle timeout
                                        if (await Task.WhenAny(receiveTask, Task.Delay(5000, _udpCts.Token)) == receiveTask)
                                        {
                                            // Data received
                                            var result = receiveTask.Result;
                                            if ((result.Buffer is not null) && (result.Buffer.Length == 93))
                                            {
                                                UdpDecoder rawResultOfValue = new UdpDecoder(result);
                                                ControllerMessage preparedMess = rawResultOfValue.GetPreparedMessage();

                                                if (preparedMess.wasError == false)
                                                {
                                                    byte[] xmlByteValues = XMLFormatter.getStatic(preparedMess.setOfValues);
                                                    #region Отладочный блок, закомментирован
                                                    //string answer = string.Empty;
                                                    //foreach (var item in preparedMess.setOfValues)
                                                    //{
                                                    //    TraceLine($"{item.Value}\n");
                                                    //    logger.Info($"{item.Value}");           // ответ драйвера прикладному ПО
                                                    //    answer = $"{answer} {item.Value}\n";
                                                    //}
                                                    //byte[] arr = Encoding.GetEncoding(1251).GetBytes($"{answer}");
                                                    #endregion
                                                    ARMsocket.Send(xmlByteValues, xmlByteValues.Length, SocketFlags.None);      // Отправляем в АРМ весов XML в виде byte[].
                                                    transferOccured = true;
                                                    _udpCts.Cancel();                                                           // после однократной операции - останавливаю передачу
                                                    _udpServer.Close();
                                                }
                                                else 
                                                {
                                                    Exception exInner = new Exception("Ошибка формата документа");              // Согласно спецификации - ловить таймаут
                                                    byte[] errorByteArr = XMLFormatter.GetError(exInner, 1);                    // Отформатировал ошибку в XML формат. 
                                                    ARMsocket.Send(errorByteArr, errorByteArr.Length, SocketFlags.None);        // Отправляем в АРМ весов XML в виде byte[].
                                                    transferOccured = true;
                                                    _udpCts.Cancel();                                                           // после однократной операции - останавливаю передачу
                                                    _udpServer.Close();
                                                }
                                            }
                                            #region Write log
                                            TraceLine($"You can process the received message further here");
                                            logger.Info($"You can process the received message further here");
                                            #endregion
                                        }
                                        else
                                        {
                                            _udpCts.Cancel();     
                                            _udpServer.Close();
                                            #region Write log
                                            TraceLine($"No data received within timeout period");
                                            logger.Info($"No data received within timeout period");
                                            #endregion
                                        }
                                    }
                                }
                                catch (OperationCanceledException ex)
                                {
                                    #region Write log
                                    TraceLine($"UDP server has been stopped\n {ex.Message}\n");
                                    logger.Info($"UDP server has been stopped\n {ex.Message}");
                                    #endregion
                                    throw;
                                }
                                catch (SocketException ex)
                                {
                                    #region Write log
                                    TraceLine($"Socket error:\n {ex.Message}\n");
                                    logger.Info($"Socket error:\n {ex.Message}");
                                    #endregion
                                    throw;
                                }
                                catch (Exception ex)
                                {
                                    #region Write log
                                    TraceLine($"Unexpected error:\n {ex.Message}\n");
                                    logger.Info($"Unexpected error:\n {ex.Message}");
                                    #endregion
                                    throw;
                                }
                                finally
                                {
                                    _udpCts.Cancel();
                                    _udpServer.Close();

                                    _udpCts = null;
                                    _udpServer = null;

                                    TraceLine($"UDP server resources have been released \n");
                                    logger.Info($"UDP server resources have been released \n");
                                }

                            }
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
                    else                                                                        
                    {
                        Exception ex = new Exception("Нет ответа от устройства");              
                        byte[] errorByteArr = XMLFormatter.GetError(ex, 2);                     // Отформатировал ошибку в XML формат. 
                        ARMsocket.Send(errorByteArr, errorByteArr.Length, SocketFlags.None);    // Отправляем в АРМ весов XML в виде byte[].
                        #region Логирование текста ошибки
                        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                        logger.Error(Encoding.GetEncoding(1251).GetString(errorByteArr));       
                        #endregion
                    }
                }

                if (ARMsocket.Poll(3000, SelectMode.SelectRead) & ARMsocket.Available == 0)
                {
                    TraceLine("Lost connection to weighter ARM " + ARMsocket.RemoteEndPoint.ToString());
                    _keepOpen = false;
                }

                if (!transferOccured)
                    Thread.Sleep(1);
            }

            #region Отправка сообщения на главную форму, логирование
            if (_updState != null)
                _updState(this, CrossThreadComm.State.disconnect);
            TraceLine("Stopping UDP server...");
            logger.Info("Stopping UDP server...");
            #endregion

            if (_udpCts is not null) _udpCts.Cancel();
            if (_udpServer is not null) _udpServer.Close();

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
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            switch (Encoding.GetEncoding(1251).GetString(clientComandArr))
            {
                case "<Request method='set_mode' parameter='Static'/>":     // 1)
                    controllerCommand = null;
                    break;

                case "<Request method='checksum'/>":                        // 2)
                    controllerCommand = null;
                    break;

                case "<Request method='get_static'/>":                      // 3) получить вес, взвешивание в статике
                    controllerCommand = "getWeight";                        // пока заглушка, только чтоб не ломать общую архитектуру
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
                System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                return Encoding.GetEncoding(1251).GetBytes(controllerCommand);
            }
            else 
            { 
                return null;
            }
        }

    }
}
