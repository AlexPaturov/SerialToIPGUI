using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace serialtoip
{
    public class Connection
    {
        private CrossThreadComm.TraceCb _conInfoCallback;
        private CrossThreadComm.UpdateState _updState;
        private CrossThreadComm.UpdateRXTX _updRxTx;
        public Socket ARMsocket;

        // добавить udp клиента 
        private CancellationTokenSource _udpCts;
        private UdpClient _udpServer;
        IPEndPoint _epContr31;

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

            // добавить udp клиента 
            _udpServer = udpServer;
            _udpCts = udpCts;   
            _d = d;
            _updState = updState;
            _updRxTx = updRxTx;
            _epContr31 = new IPEndPoint(IPAddress.Parse(_d["vesy31ip"]), Int32.Parse(_d["vesy31port"]));
            SetConnInfoTraceCallback(conInfoCb);

            if (_updState != null)
                _updState((object)this, CrossThreadComm.State.start);
            _isfree = false;

            // добавить udp клиента 
            if (_udpServer is null)
            {
                
                try
                {
                    // добавить udp клиента 
                    _udpCts = new CancellationTokenSource();
                    _udpServer = new UdpClient(Int32.Parse(_d["vesy31port"]));
                }
                catch (Exception ex)
                {
                    TraceLine("Error connect to controller:" + "\r\n" + ex.ToString());
                    logger.Error(ex);
                    
                    if (ARMsocket.Connected)
                    {
                        // ПРОВЕРИТЬ ДОСТУПНОСТЬ ХОСТА - ЕСЛИ НЕ ДОСТУПЕН - СФОРМИРОВАТЬ СООБЩЕНИЕ СОГЛАСНО ФОРМАТУ В ТЕХ ТРЕБОВАНИЯХ
                        ARMsocket.Close();
                    }
                    // throw ex;
                }

                // добавить udp клиента 
                //TraceLine("Connection to controller - OK " + _moxaTC.RemoteEndPoint.ToString()); // подключение к контроллеру - спецификация
                //logger.Info("Connection to controller - OK " + _moxaTC.RemoteEndPoint.ToString());
            }
            new Thread(new ThreadStart(Tranceiver)).Start();
            return true;
        }

        private async void Tranceiver()
        {
            byte[] buffer = new byte[8192];
            _keepOpen = true;
            string cliCmd = string.Empty;
            // vesy31ip vesy31port
            TraceLine("vesy31 controler connected from " + _d["vesy31ip"] +":"+ int.Parse(_d["vesy31port"]));
            
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
                    System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                    logger.Info("ARM query string: " + Encoding.GetEncoding(1251).GetString(ARMbyteArr));   // строка запроса прикладного ПО драйверу


                    byte[] controllerCommand = DecodeClientRequestToControllerCommand(buffer, colByteARM);  // Верну null если команда не корректная, иначе - команду для контроллера
                    if (controllerCommand != null)                                                          // команда корректна -> отправляем устройству
                    {
                        try 
                        {
                            // если не получить вес - отправляю запрос без ожидания ответа
                            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                            if (Encoding.GetEncoding(1251).GetString(ARMbyteArr) != "<Request method='get_static'/>") 
                            {
                                _udpServer.Send(controllerCommand, controllerCommand.Length, _epContr31);    // запрос драйвера контроллеру
                            }
                            else
                            {
                                try
                                {
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
                                                UdpDecoder resultOfValue = new UdpDecoder(result);
                                                ControllerMessage mess = resultOfValue.GetControllerMessage();
                                                if (mess.wasError == false)
                                                {
                                                    foreach (var item in mess.setOfValues)
                                                    {
                                                        // добавить заполнение объекта XML формата
                                                        // если данные принятые из udp корректные - возвращаю сообщение,
                                                        // нет - возвращаю сообщение об ошибке 
                                                        TraceLine($"{item.Value}\n");
                                                        logger.Info($"{item.Value}\n"); // ответ драйвера прикладному ПО
                                                    }
                                                    _udpCts.Cancel();       // сбрасываю токен 
                                                    _udpServer.Close();     // 
                                                }
                                            }

                                            TraceLine($"You can process the received message further here {DateTime.Now.ToString("HH:mm:ss.fff")}\n");
                                            logger.Info($"You can process the received message further here {DateTime.Now.ToString("HH:mm:ss.fff")}");
                                        }
                                        else
                                        {
                                            // отправить, ответ о неудачном приёме сообщения клиенту
                                            TraceLine($"No data received within timeout period {DateTime.Now.ToString("HH:mm:ss.fff")}\n");
                                            logger.Info($"No data received within timeout period {DateTime.Now.ToString("HH:mm:ss.fff")}");
                                        }
                                    }
                                }
                                catch (OperationCanceledException ex)
                                {
                                    TraceLine($"UDP server has been stopped\n {ex.Message}\n");
                                    logger.Info($"UDP server has been stopped\n {ex.Message}");
                                }
                                catch (SocketException ex)
                                {
                                    TraceLine($"Socket error:\n {ex.Message}\n");
                                    logger.Info($"Socket error:\n {ex.Message}");
                                }
                                catch (Exception ex)
                                {
                                    TraceLine($"Unexpected error:\n {ex.Message}\n");
                                    logger.Info($"Unexpected error:\n {ex.Message}");
                                }
                                finally
                                {
                                    _udpServer.Dispose();
                                    Console.WriteLine(".");
                                    TraceLine($"UDP server resources have been released \n");
                                    logger.Info($"UDP server resources have been released \n");
                                }

                            }

                            // если запрос "get_static" - просто устанавливаю коннект к контроллеру 
                            // и автоматом получаю 1-но сообщение
                            // отправляю клиенту
                            // если другая команда "set_zero" - отправляю в устройство, не ожидая ответа
                            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
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
                        System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
                        logger.Error(Encoding.GetEncoding(1251).GetString(errorByteArr));       // пишу ответ для арма весов.
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
            
            if (_updState != null)
                _updState(this, CrossThreadComm.State.disconnect);

            if (_udpServer is not null)
            {
                TraceLine("Stopping UDP server..."); 
                logger.Info("Stopping UDP server...");
                _udpCts.Cancel();
                _udpServer.Close();
            }

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
