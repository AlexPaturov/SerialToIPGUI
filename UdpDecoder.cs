using System.Collections.Generic;
using System.Net.Sockets;
using System.Diagnostics;
using System;

namespace serialtoip;

public class UdpDecoder
{
    private readonly UdpReceiveResult _result;
    private readonly string _codeType; // тип кодировки для управления режимами перекодирования hex
    private bool _wasError = false;
    private Dictionary<string, string> _setOfValues = new Dictionary<string, string>();
    public ControllerMessage controllerMessage { get; private set; } = new ControllerMessage();

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
                _setOfValues = HexStringToHexBlocks(hexDataString);
                if (_wasError == false)
                {
                    HexBlockToSingle(_setOfValues); // перкодировал из набора hex блоков в single
                }
            }
        }
    }

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
    #region Обрезал символы начала и окончания пакета DeletePkBegPkEndSymbols(string rawString)
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
    #region Перевёл из строки в блоки данных для последующего декодирования HexStringToHexBlocks(string message)
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
    //        resultDict.Add("value02", "66 E6 B4 42"); // brutto
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
    //        resultDict.Add("value19", "71 3D 35 42"); // v13
    //        message = message.Substring(12);
    //        resultDict.Add("value20", "D7 A3 34 42"); // v24
    //        message = message.Substring(12);
    //        resultDict.Add("value21", "66 66 37 42"); // v12
    //        message = message.Substring(12);
    //        resultDict.Add("value22", "66 66 32 42"); // v34
    //        message = string.Empty;
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.WriteLine(ex.Message);
    //        _wasError = true;
    //    }
    //    return resultDict;
    //}
    //#endregion
    #endregion
    #region Преобразую HEX блоки в single HexBlockToSingle(Dictionary<string, string> massValue)
    private bool HexBlockToSingle(Dictionary<string, string> massValue)
    {
        try
        {
            foreach (var item in massValue)
            {
                byte[] bytes = HexStringToByteArray(item.Value);

                // сделать в файле конфигурации поле из которого я буду считывать режим и запускать драйвер
                if (!string.IsNullOrWhiteSpace(_codeType) && _codeType == "1") // _codeType BitConverter.IsLittleEndian
                {
                    Array.Reverse(bytes);
                }

                massValue[item.Key] = ((float)Math.Round(BitConverter.ToSingle(bytes, 0), 2)).ToString("F2");
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex.Message);
            _wasError = true;
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

    public ControllerMessage GetPreparedMessage()
    {
        controllerMessage.setOfValues = new Dictionary<string, string>(_setOfValues);
        controllerMessage.wasError = _wasError;
        return controllerMessage;
    }
}