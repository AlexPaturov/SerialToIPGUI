using System.Collections.Generic;
using System.Net.Sockets;
using System.Diagnostics;
using System;

namespace serialtoip;

public class UdpDecoder
{
    private const string codeEndian = "LittleEndian";                                           // для управления направлением кодировки
    private readonly UdpReceiveResult _result;
    private readonly string _codeType;                                                          // тип кодировки для управления режимами перекодирования hex (little indian / big indian)
    private bool _wasError = false;                                                             // флаг появления ошибки при разборе строки
    private Dictionary<string, string> _setOfValues = new Dictionary<string, string>();         // набор ответа от контроллера в числовом представлении
    public ControllerMessage controllerMessage { get; private set; } = new ControllerMessage();

    #region UdpDecoder(UdpReceiveResult result, string codeType = "") конструктор, и вызов методов обработки входящей строки по цепочке
    public UdpDecoder(UdpReceiveResult result, string codeType = "")
    {
        _result = result;
        _codeType = codeType;

        string hexDataString = BitConvertToHexString(_result);
        if (_wasError == false)
        {
            hexDataString = DeletePkBegPkEndSymbols(hexDataString); // Обрезал символы начала и окончания пакета
            if (_wasError == false)
            {
                _setOfValues = HexStringToHexBlocks(hexDataString); // Нарезал по блокам HEX 4 пары, для перекодировки в double
                if (_wasError == false)
                {
                    HexBlockToSingle(_setOfValues); // перкодировал из набора hex блоков в single
                }
            }
        }
    }
    #endregion

    #region BitConvertToHexString(UdpReceiveResult result)
    private string BitConvertToHexString(UdpReceiveResult result)
    {
        try
        {
            return BitConverter.ToString(result.Buffer).Replace("-", " ");
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            _wasError = true;
            return string.Empty;
        }
    }
    #endregion
    
    #region DeletePkBegPkEndSymbols(string rawString) Обрезал символы начала и окончания пакета 
    private string DeletePkBegPkEndSymbols(string rawString)
    {
        try
        {
            return rawString.Substring(6, rawString.Length - 15);
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            _wasError = true;
            return string.Empty;
        }
    }
    #endregion
    
    #region HexStringToHexBlocks(string message) Перевёл из строки в блоки данных для последующего декодирования 
    private Dictionary<string, string> HexStringToHexBlocks(string message)
    {
        Dictionary<string, string> resultDict = new Dictionary<string, string>();
        try
        {
            #region not used
            resultDict.Add("value01", message.Substring(0, 11));
            message = message.Substring(12);
            #endregion
            resultDict.Add("value02", message.Substring(0, 11)); // brutto
            message = message.Substring(12);
            #region нагрузки по датчикам - 8 штук (не используется)
            resultDict.Add("value03", message.Substring(0, 11));
            message = message.Substring(12);
            resultDict.Add("value04", message.Substring(0, 11));
            message = message.Substring(12);
            resultDict.Add("value05", message.Substring(0, 11));
            message = message.Substring(12);
            resultDict.Add("value06", message.Substring(0, 11));
            message = message.Substring(12);
            resultDict.Add("value07", message.Substring(0, 11));
            message = message.Substring(12);
            resultDict.Add("value08", message.Substring(0, 11));
            message = message.Substring(12);
            resultDict.Add("value09", message.Substring(0, 11));
            message = message.Substring(12);
            resultDict.Add("value10", message.Substring(0, 11));
            message = message.Substring(12);
            #endregion
            #region not used
            resultDict.Add("value11", message.Substring(0, 11));
            message = message.Substring(12);
            resultDict.Add("value12", message.Substring(0, 11));
            message = message.Substring(12);
            resultDict.Add("value13", message.Substring(0, 11));
            message = message.Substring(12);
            resultDict.Add("value14", message.Substring(0, 11));
            message = message.Substring(12);
            resultDict.Add("value15", message.Substring(0, 11));
            message = message.Substring(12);
            resultDict.Add("value16", message.Substring(0, 11));
            message = message.Substring(12);
            resultDict.Add("value17", message.Substring(0, 11));
            message = message.Substring(12);
            resultDict.Add("value18", message.Substring(0, 11));
            message = message.Substring(12);
            #endregion
            resultDict.Add("value19", message.Substring(0, 11)); // v13
            message = message.Substring(12);
            resultDict.Add("value20", message.Substring(0, 11)); // v24
            message = message.Substring(12);
            resultDict.Add("value21", message.Substring(0, 11)); // v12
            message = message.Substring(12);
            resultDict.Add("value22", message.Substring(0, 11)); // v34
            message = string.Empty;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            _wasError = true;
        }

        return resultDict;
    }
    #endregion

    #region HexStringToHexBlocks -> для тестов подсчёта дельты и конвертации HEX кода - закомичено
    //private Dictionary<string, string> HexStringToHexBlocks(string message)
    //{
    //    Dictionary<string, string> resultDict = new Dictionary<string, string>();
    //    try
    //    {
    //        #region not used
    //        resultDict.Add("value01", message.Substring(0, 11));
    //        message = message.Substring(12);
    //        #endregion
    //        resultDict.Add("value02", "00 00 00 00"); // brutto
    //        message = message.Substring(12);
    //        #region нагрузки по датчикам - 8 штук (не используется)
    //        resultDict.Add("value03", message.Substring(0, 11));
    //        message = message.Substring(12);
    //        resultDict.Add("value04", message.Substring(0, 11));
    //        message = message.Substring(12);
    //        resultDict.Add("value05", message.Substring(0, 11));
    //        message = message.Substring(12);
    //        resultDict.Add("value06", message.Substring(0, 11));
    //        message = message.Substring(12);
    //        resultDict.Add("value07", message.Substring(0, 11));
    //        message = message.Substring(12);
    //        resultDict.Add("value08", message.Substring(0, 11));
    //        message = message.Substring(12);
    //        resultDict.Add("value09", message.Substring(0, 11));
    //        message = message.Substring(12);
    //        resultDict.Add("value10", message.Substring(0, 11));
    //        message = message.Substring(12);
    //        #endregion
    //        #region not used
    //        resultDict.Add("value11", message.Substring(0, 11));
    //        message = message.Substring(12);
    //        resultDict.Add("value12", message.Substring(0, 11));
    //        message = message.Substring(12);
    //        resultDict.Add("value13", message.Substring(0, 11));
    //        message = message.Substring(12);
    //        resultDict.Add("value14", message.Substring(0, 11));
    //        message = message.Substring(12);
    //        resultDict.Add("value15", message.Substring(0, 11));
    //        message = message.Substring(12);
    //        resultDict.Add("value16", message.Substring(0, 11));
    //        message = message.Substring(12);
    //        resultDict.Add("value17", message.Substring(0, 11));
    //        message = message.Substring(12);
    //        resultDict.Add("value18", message.Substring(0, 11));
    //        message = message.Substring(12);
    //        #endregion
    //        resultDict.Add("value19", "00 00 00 00"); // v13
    //        message = message.Substring(12);
    //        resultDict.Add("value20", "00 00 00 00"); // v24
    //        message = message.Substring(12);
    //        resultDict.Add("value21", "00 00 00 00"); // v12
    //        message = message.Substring(12);
    //        resultDict.Add("value22", "00 00 00 00"); // v34
    //        message = string.Empty;
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine(ex.Message);
    //        _wasError = true;
    //    }
    //    return resultDict;
    //}
    #endregion

    #region HexBlockToSingle(Dictionary<string, string> massValue) Преобразую HEX блоки в single 
    private bool HexBlockToSingle(Dictionary<string, string> massValue)
    {
        Dictionary<string, string> temp = new Dictionary<string, string>();
        try
        {
            foreach (var item in massValue)
            {
                byte[] bytes = HexStringToByteArray(item.Value);

                // сделать в файле конфигурации поле из которого я буду считывать режим и запускать драйвер
                if (!string.IsNullOrWhiteSpace(_codeType) && _codeType == codeEndian)
                {
                    Array.Reverse(bytes); // при необходимости перевернуть кодировку
                }

                temp.Add(item.Key, ((float)Math.Round(BitConverter.ToSingle(bytes, 0), 2)).ToString("F2"));
            }

            // копирую, по уродски, пока...
            foreach (var item in temp)
                massValue[item.Key] = item.Value;
        }
        catch (Exception ex)
        {
            _wasError = true;
            //return false; // не имеет смысла 
            throw;
        }

        return true;
    }
    #endregion

    #region HexStringToByteArray Ошибка - что будем делать?
    private byte[] HexStringToByteArray(string hex)
    {
        string[] hexParts = hex.Split(' ');
        byte[] bytes = new byte[hexParts.Length];

        for (int i = 0; i < hexParts.Length; i++)
        {
            bytes[i] = Convert.ToByte(hexParts[i], 16);
        }

        return bytes;
    }
    #endregion

    #region GetPreparedMessage() return ControllerMessage
    public ControllerMessage GetPreparedMessage()
    {
        controllerMessage.setOfValues = new Dictionary<string, string>(_setOfValues);
        controllerMessage.wasError = _wasError;
        return controllerMessage;
    }
    #endregion
}