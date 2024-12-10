using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Xml;

/*
    Привожу полученные от контроллера данные к требуемому по спецификации формату.
*/

namespace serialtoip
{
    public static class XMLFormatter
    {
        #region Константы для подсчёта дельты 
        const double distanceBetweenRails = 1.52;   // расстояние между рельсами
        const double balanceKoeff = -5.24;          // корректирующий коэффициент для весов N 31
        const double fromMetersToSantim = 100;      // для перевода из сантиметров в миллиметры
        #endregion
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        #region byte[] getStatic(byte[] bInput) Получить результат статического взвешивания - для весов N 10
        public static byte[] getStatic(byte[] bInput)
        {
            if (bInput != null)
            {
                Dictionary<string, string> preparedAnswer = RawToXML(System.Text.Encoding.Default.GetString(bInput));

                XmlDocument xmlDoc = new XmlDocument();
                XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);

                XmlElement rootResponse = xmlDoc.CreateElement("Response");
                xmlDoc.AppendChild(rootResponse);

                XmlElement ch1_State = xmlDoc.CreateElement("State");
                ch1_State.InnerText = "Success";
                rootResponse.AppendChild(ch1_State);

                XmlElement ch1_CheckSumZero = xmlDoc.CreateElement("CheckSumZero");
                ch1_CheckSumZero.InnerText = "0";
                rootResponse.AppendChild(ch1_CheckSumZero);

                XmlElement ch1_CheckSumWeight = xmlDoc.CreateElement("CheckSumWeight");
                ch1_CheckSumWeight.InnerText = "0";
                rootResponse.AppendChild(ch1_CheckSumWeight);

                XmlElement ch2_StaticData = xmlDoc.CreateElement("StaticData");
                rootResponse.AppendChild(ch2_StaticData);

                XmlElement ch3_Processed = xmlDoc.CreateElement("Processed");
                ch3_Processed.InnerText = "1";
                ch2_StaticData.AppendChild(ch3_Processed);

                XmlElement ch3_Npp = xmlDoc.CreateElement("Npp");
                ch3_Npp.InnerText = "1";
                ch2_StaticData.AppendChild(ch3_Npp);

                XmlElement ch3_Number = xmlDoc.CreateElement("Number");
                ch3_Number.InnerText = "0";
                ch2_StaticData.AppendChild(ch3_Number);

                XmlElement ch3_Date = xmlDoc.CreateElement("Date");
                ch3_Date.InnerText = preparedAnswer["Date"];
                ch2_StaticData.AppendChild(ch3_Date);

                XmlElement ch3_Time = xmlDoc.CreateElement("Time");
                ch3_Time.InnerText = preparedAnswer["Time"];
                ch2_StaticData.AppendChild(ch3_Time);

                XmlElement ch3_Brutto = xmlDoc.CreateElement("Brutto");
                ch3_Brutto.InnerText = preparedAnswer["Brutto"];
                ch2_StaticData.AppendChild(ch3_Brutto);

                XmlElement ch3_KolOs = xmlDoc.CreateElement("KolOs");
                ch3_KolOs.InnerText = "0";
                ch2_StaticData.AppendChild(ch3_KolOs);

                XmlElement ch3_Speed = xmlDoc.CreateElement("Speed");
                ch3_Speed.InnerText = "0";
                ch2_StaticData.AppendChild(ch3_Speed);

                XmlElement ch3_Platform1 = xmlDoc.CreateElement("Platform1");
                ch3_Platform1.InnerText = preparedAnswer["Platform1"];
                ch2_StaticData.AppendChild(ch3_Platform1);

                XmlElement ch3_Rail1_1 = xmlDoc.CreateElement("Rail1_1");
                ch3_Rail1_1.InnerText = "0";
                ch2_StaticData.AppendChild(ch3_Rail1_1);

                XmlElement ch3_Rail1_2 = xmlDoc.CreateElement("Rail1_2");
                ch3_Rail1_2.InnerText = "0";
                ch2_StaticData.AppendChild(ch3_Rail1_2); ;

                XmlElement ch3_Rail1_3 = xmlDoc.CreateElement("Rail1_3");
                ch3_Rail1_3.InnerText = "0";
                ch2_StaticData.AppendChild(ch3_Rail1_3);

                XmlElement ch3_Rail1_4 = xmlDoc.CreateElement("Rail1_4");
                ch3_Rail1_4.InnerText = "0";
                ch2_StaticData.AppendChild(ch3_Rail1_4);

                XmlElement ch3_Platform2 = xmlDoc.CreateElement("Platform2");
                ch3_Platform2.InnerText = preparedAnswer["Platform2"];
                ch2_StaticData.AppendChild(ch3_Platform2);

                XmlElement ch3_Rail2_1 = xmlDoc.CreateElement("Rail2_1");
                ch3_Rail2_1.InnerText = "0";
                ch2_StaticData.AppendChild(ch3_Rail2_1);

                XmlElement ch3_Rail2_2 = xmlDoc.CreateElement("Rail2_2");
                ch3_Rail2_2.InnerText = "0";
                ch2_StaticData.AppendChild(ch3_Rail2_2); ;

                XmlElement ch3_Rail2_3 = xmlDoc.CreateElement("Rail2_3");
                ch3_Rail2_3.InnerText = "0";
                ch2_StaticData.AppendChild(ch3_Rail2_3);

                XmlElement ch3_Rail2_4 = xmlDoc.CreateElement("Rail2_4");
                ch3_Rail2_4.InnerText = "0";
                ch2_StaticData.AppendChild(ch3_Rail2_4);

                XmlElement ch3_ShiftPop = xmlDoc.CreateElement("ShiftPop");
                ch3_ShiftPop.InnerText = preparedAnswer["ShiftPop"];
                ch2_StaticData.AppendChild(ch3_ShiftPop);

                XmlElement ch3_ShiftPro = xmlDoc.CreateElement("ShiftPro");
                ch3_ShiftPro.InnerText = preparedAnswer["ShiftPro"];
                ch2_StaticData.AppendChild(ch3_ShiftPro);

                XmlElement ch3_pravBort1_2 = xmlDoc.CreateElement("PravBort1_2");
                ch3_pravBort1_2.InnerText = preparedAnswer["PravBort1_2"];
                ch2_StaticData.AppendChild(ch3_pravBort1_2);

                XmlElement ch3_levBort3_4 = xmlDoc.CreateElement("LevBort3_4");
                ch3_levBort3_4.InnerText = preparedAnswer["LevBort3_4"];
                ch2_StaticData.AppendChild(ch3_levBort3_4);

                XmlElement ch3_Delta = xmlDoc.CreateElement("Delta");
                ch3_Delta.InnerText = preparedAnswer["Delta"];
                ch2_StaticData.AppendChild(ch3_Delta);

                XmlElement ch3_Type = xmlDoc.CreateElement("Type");
                ch3_Type.InnerText = "V";
                ch2_StaticData.AppendChild(ch3_Type);

                return Encoding.GetEncoding(1251).GetBytes(xmlDoc.OuterXml);
            }
            else
            {
                throw new Exception("Answer from device is incorrect. getStatic input == null");
            }
        }
        #endregion

        #region byte[] getStatic(Dictionary<string, string> inputDict) Для весов N 31
        public static byte[] getStatic(Dictionary<string, string> inputDict)
        {
            Dictionary<string, string> preparedAnswer = RawToXML(inputDict);

            XmlDocument xmlDoc = new XmlDocument();
            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);

            XmlElement rootResponse = xmlDoc.CreateElement("Response");
            xmlDoc.AppendChild(rootResponse);

            XmlElement ch1_State = xmlDoc.CreateElement("State");
            ch1_State.InnerText = "Success";
            rootResponse.AppendChild(ch1_State);

            XmlElement ch1_CheckSumZero = xmlDoc.CreateElement("CheckSumZero");
            ch1_CheckSumZero.InnerText = "0";
            rootResponse.AppendChild(ch1_CheckSumZero);

            XmlElement ch1_CheckSumWeight = xmlDoc.CreateElement("CheckSumWeight");
            ch1_CheckSumWeight.InnerText = "0";
            rootResponse.AppendChild(ch1_CheckSumWeight);

            XmlElement ch2_StaticData = xmlDoc.CreateElement("StaticData");
            rootResponse.AppendChild(ch2_StaticData);

            XmlElement ch3_Processed = xmlDoc.CreateElement("Processed");
            ch3_Processed.InnerText = "1";
            ch2_StaticData.AppendChild(ch3_Processed);

            XmlElement ch3_Npp = xmlDoc.CreateElement("Npp");
            ch3_Npp.InnerText = "1";
            ch2_StaticData.AppendChild(ch3_Npp);

            XmlElement ch3_Number = xmlDoc.CreateElement("Number");
            ch3_Number.InnerText = "0";
            ch2_StaticData.AppendChild(ch3_Number);

            XmlElement ch3_Date = xmlDoc.CreateElement("Date");
            ch3_Date.InnerText = preparedAnswer["Date"];
            ch2_StaticData.AppendChild(ch3_Date);

            XmlElement ch3_Time = xmlDoc.CreateElement("Time");
            ch3_Time.InnerText = preparedAnswer["Time"];
            ch2_StaticData.AppendChild(ch3_Time);

            XmlElement ch3_Brutto = xmlDoc.CreateElement("Brutto");
            ch3_Brutto.InnerText = preparedAnswer["Brutto"];
            ch2_StaticData.AppendChild(ch3_Brutto);

            XmlElement ch3_KolOs = xmlDoc.CreateElement("KolOs");
            ch3_KolOs.InnerText = "0";
            ch2_StaticData.AppendChild(ch3_KolOs);

            XmlElement ch3_Speed = xmlDoc.CreateElement("Speed");
            ch3_Speed.InnerText = "0";
            ch2_StaticData.AppendChild(ch3_Speed);

            XmlElement ch3_Platform1 = xmlDoc.CreateElement("Platform1");
            ch3_Platform1.InnerText = preparedAnswer["Platform1"];
            ch2_StaticData.AppendChild(ch3_Platform1);

            XmlElement ch3_Rail1_1 = xmlDoc.CreateElement("Rail1_1");
            ch3_Rail1_1.InnerText = "0";
            ch2_StaticData.AppendChild(ch3_Rail1_1);

            XmlElement ch3_Rail1_2 = xmlDoc.CreateElement("Rail1_2");
            ch3_Rail1_2.InnerText = "0";
            ch2_StaticData.AppendChild(ch3_Rail1_2); ;

            XmlElement ch3_Rail1_3 = xmlDoc.CreateElement("Rail1_3");
            ch3_Rail1_3.InnerText = "0";
            ch2_StaticData.AppendChild(ch3_Rail1_3);

            XmlElement ch3_Rail1_4 = xmlDoc.CreateElement("Rail1_4");
            ch3_Rail1_4.InnerText = "0";
            ch2_StaticData.AppendChild(ch3_Rail1_4);

            XmlElement ch3_Platform2 = xmlDoc.CreateElement("Platform2");
            ch3_Platform2.InnerText = preparedAnswer["Platform2"];
            ch2_StaticData.AppendChild(ch3_Platform2);

            XmlElement ch3_Rail2_1 = xmlDoc.CreateElement("Rail2_1");
            ch3_Rail2_1.InnerText = "0";
            ch2_StaticData.AppendChild(ch3_Rail2_1);

            XmlElement ch3_Rail2_2 = xmlDoc.CreateElement("Rail2_2");
            ch3_Rail2_2.InnerText = "0";
            ch2_StaticData.AppendChild(ch3_Rail2_2); ;

            XmlElement ch3_Rail2_3 = xmlDoc.CreateElement("Rail2_3");
            ch3_Rail2_3.InnerText = "0";
            ch2_StaticData.AppendChild(ch3_Rail2_3);

            XmlElement ch3_Rail2_4 = xmlDoc.CreateElement("Rail2_4");
            ch3_Rail2_4.InnerText = "0";
            ch2_StaticData.AppendChild(ch3_Rail2_4);

            XmlElement ch3_ShiftPop = xmlDoc.CreateElement("ShiftPop");
            ch3_ShiftPop.InnerText = preparedAnswer["ShiftPop"];
            ch2_StaticData.AppendChild(ch3_ShiftPop);

            XmlElement ch3_ShiftPro = xmlDoc.CreateElement("ShiftPro");
            ch3_ShiftPro.InnerText = preparedAnswer["ShiftPro"];
            ch2_StaticData.AppendChild(ch3_ShiftPro);

            XmlElement ch3_pravBort1_2 = xmlDoc.CreateElement("PravBort1_2");
            ch3_pravBort1_2.InnerText = preparedAnswer["PravBort1_2"];
            ch2_StaticData.AppendChild(ch3_pravBort1_2);

            XmlElement ch3_levBort3_4 = xmlDoc.CreateElement("LevBort3_4");
            ch3_levBort3_4.InnerText = preparedAnswer["LevBort3_4"];
            ch2_StaticData.AppendChild(ch3_levBort3_4);

            XmlElement ch3_Delta = xmlDoc.CreateElement("Delta");
            ch3_Delta.InnerText = preparedAnswer["Delta"];
            ch2_StaticData.AppendChild(ch3_Delta);

            XmlElement ch3_Type = xmlDoc.CreateElement("Type");
            ch3_Type.InnerText = "V";
            ch2_StaticData.AppendChild(ch3_Type);

            logger.Debug(xmlDoc.OuterXml); // после отладки удалить (?)
            return Encoding.GetEncoding(1251).GetBytes(xmlDoc.OuterXml);
        }
        #endregion

        #region GetError(Exception ex, int code) Получаю стандартный Exception и код по спецификации
        public static byte[] GetError(Exception ex, int code)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);

            XmlElement rootResponse = xmlDoc.CreateElement("Response");
            xmlDoc.AppendChild(rootResponse);

            XmlElement ch1_State = xmlDoc.CreateElement("State");
            ch1_State.InnerText = "Error";
            rootResponse.AppendChild(ch1_State);

            XmlElement ch2_ErrorDescription = xmlDoc.CreateElement("ErrorDescription");
            rootResponse.AppendChild(ch2_ErrorDescription);

            XmlElement ch3_ErrorCode = xmlDoc.CreateElement("ErrorCode");
            ch3_ErrorCode.InnerText = code.ToString();
            ch2_ErrorDescription.AppendChild(ch3_ErrorCode);

            XmlElement ch3_ErrorText = xmlDoc.CreateElement("ErrorText");
            ch3_ErrorText.InnerText = ex.Message;
            ch2_ErrorDescription.AppendChild(ch3_ErrorText);

            return Encoding.GetEncoding(1251).GetBytes(xmlDoc.OuterXml);
        }
        #endregion

        #region RawToXML(string input) Разбор входной строки и приведение данных к формату в соответствии со спецификацией.
        private static Dictionary<string, string> RawToXML(string input)
        {
            Dictionary<string, string> XMLtmp = new Dictionary<string, string>();
            string workInput = input;

            if (workInput.Contains("F#1"))                                                                                                  // 1
            {
                workInput = workInput.Substring(workInput.IndexOf("F#1") + 3, (workInput.Length - (workInput.IndexOf("F#1") + 3))).Trim();
            }
            else
            {
                throw new Exception("Format of answer from device is incorrect. " + input);
            }

            XMLtmp.Add("Date", string.Format(CultureInfo.InvariantCulture, "{0:dd/MM/yyyy}",
                DateTime.ParseExact(workInput.Substring(0, workInput.IndexOf(" ")).Trim(), "dd-MM-yyyy", CultureInfo.InvariantCulture)));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 2 

            XMLtmp.Add("Time", workInput.Substring(0, workInput.IndexOf(" ")));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 3

            XMLtmp.Add("Brutto", TonnsToKilos(workInput.Substring(0, workInput.IndexOf(" "))));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 4

            XMLtmp.Add("Platform1", TonnsToKilos(workInput.Substring(0, workInput.IndexOf(" "))));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 5

            XMLtmp.Add("Platform2", TonnsToKilos(workInput.Substring(0, workInput.IndexOf(" "))));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 6

            XMLtmp.Add("PravBort1_2", TonnsToKilos(workInput.Substring(0, workInput.IndexOf(" "))));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 7 

            XMLtmp.Add("LevBort3_4", TonnsToKilos(workInput.Substring(0, workInput.IndexOf(" "))));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 8 

            XMLtmp.Add("ShiftPop", workInput.Substring(0, workInput.IndexOf(" ")));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 9

            XMLtmp.Add("ShiftPro", workInput.Substring(0, workInput.IndexOf(" ")));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 10

            XMLtmp.Add("Delta", workInput.Trim());                                                                                          // 11

            return XMLtmp;
        }
        #endregion

        #region RawToXML(RawToXML(Dictionary<string, string> inpDictOfDoublle) с подсчётом дельты (коэффициент корректировки забит константой для 31-х весов)
        private static Dictionary<string, string> RawToXML(Dictionary<string, string> inpDictOfDoublle)
        {
            Dictionary<string, string> forXMLtmp = new Dictionary<string, string>();
            forXMLtmp.Add("Date", string.Format(CultureInfo.InvariantCulture, "{0:dd/MM/yyyy}",
                        DateTime.ParseExact(DateTime.Now.ToString("dd-MM-yyyy"), "dd-MM-yyyy", CultureInfo.InvariantCulture)));                // 1
            forXMLtmp.Add("Time", DateTime.Now.ToString("HH:mm:ss"));                                                                          // 2
            forXMLtmp.Add("Brutto", TonnsToKilos(inpDictOfDoublle["value02"]));
            forXMLtmp.Add("Platform1", TonnsToKilos(inpDictOfDoublle["value19"]));
            forXMLtmp.Add("Platform2", TonnsToKilos(inpDictOfDoublle["value20"]));
            forXMLtmp.Add("PravBort1_2", TonnsToKilos(inpDictOfDoublle["value21"]));    // v12
            forXMLtmp.Add("LevBort3_4", TonnsToKilos(inpDictOfDoublle["value22"]));     // v34
            forXMLtmp.Add("ShiftPop", GetDiffer(inpDictOfDoublle["value19"], inpDictOfDoublle["value20"])); // пл1 - пл2
            forXMLtmp.Add("ShiftPro", GetDiffer(inpDictOfDoublle["value21"], inpDictOfDoublle["value22"])); // лб - пб
            forXMLtmp.Add("Delta", GetDelta(inpDictOfDoublle["value02"], 
                                            inpDictOfDoublle["value21"], 
                                            inpDictOfDoublle["value22"]));    // смещение груза относительно центра вагона Delta
            return forXMLtmp;
        }
        #endregion

        #region TonnsToKilos(string inputTonns) Перевожу тонны в киллограммы, возвращаю в строковом представлении.
        private static string TonnsToKilos(string inputTonns)
        {
            if (!string.IsNullOrEmpty(inputTonns))
            {
                double outputKilos = 0;
                if (double.TryParse(inputTonns, out outputKilos))
                {
                    return (outputKilos * 1000).ToString();
                }
                throw new Exception("Calculation mass is incorrect. | " + inputTonns + " |");
            }
            else
            {
                throw new Exception("Mass value is incorrect.");
            }
        }
        #endregion

        #region GetDiffer(string v1, string v2) получаю разность 2-х чисел -> возвращаю строку
        private static string GetDiffer(string v1, string v2)
        {
            try
            {
                // Attempt to convert v1 to double
                if (!double.TryParse(v1, out double d1))
                    throw new ArgumentException("Invalid value for v1. It must be a valid number.", nameof(v1));

                // Attempt to convert v2 to double
                if (!double.TryParse(v2, out double d2))
                    throw new ArgumentException("Invalid value for v2. It must be a valid number.", nameof(v2));

                // Perform subtraction
                double difference = d1 - d2;

                // Format the result as "0.00" without rounding
                string formattedResult = difference.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

                return formattedResult;
            }
            catch (Exception ex)
            {
                // 1 - ловлю внешнее исключение
                // 2 - записываю в лог
                // 3 - возвращаю новый ответ

                // подменять нулём ?
                throw new Exception($"{ex.Message}");
            }
        }
        #endregion

        #region GetDelta(string mass, string v12, string v34) получаю дельту с учётом коэффициента корректировки
        private static string GetDelta(string mass, string v12, string v34)
        {
            if (!double.TryParse(mass, out double massDouble))
                throw new ArgumentException("Invalid value for brutto. It must be a valid number.", nameof(mass));

            if (!double.TryParse(v12, out double v12Double))
                throw new ArgumentException("Invalid value for v12. It must be a valid number.", nameof(v12));

            if (!double.TryParse(v34, out double v34Double))
                throw new ArgumentException("Invalid value for v34. It must be a valid number.", nameof(v34));

            double deltaDouble = 0;
            try
            {
                if(massDouble > 0) // иначе летит NAN
                    deltaDouble = balanceKoeff * ((((v12Double - v34Double) / massDouble) * distanceBetweenRails) * fromMetersToSantim);
            }
            catch (Exception ex) 
            {   // ловлю ошибку, 
                logger.Error(ex.Message);
                return "0";
            }

            return deltaDouble.ToString("0", System.Globalization.CultureInfo.InvariantCulture); // округляем до целого
        }
        #endregion
    }
}
