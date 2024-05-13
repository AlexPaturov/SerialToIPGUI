using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

/*
    Привожу к требуемому по спецификации формату данные для ответа АРМ(у) клиента.
 */

namespace serialtoip
{
    public static class XMLFormatter
    {

        // Получить результат статического взвешивания.
        public static byte[] getStatic(byte[] bInput) 
        {
            if (bInput != null) 
            { 
                Dictionary<string, string> preparedAnswer = RawToXML(System.Text.Encoding.Default.GetString(bInput) );

                XmlDocument xmlDoc = new XmlDocument();                                                     // Create the XML declaration
                XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            
                XmlElement rootResponse = xmlDoc.CreateElement("Response");                                 // Create the root element
                xmlDoc.AppendChild(rootResponse);                                                           // Append the root element to the document

                    XmlElement ch1_State = xmlDoc.CreateElement("State");                                   // Create some child elements
                    ch1_State.InnerText = "Success";
                    rootResponse.AppendChild(ch1_State);

                    XmlElement ch1_CheckSumZero = xmlDoc.CreateElement("CheckSumZero");                     // Create some child elements
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

        // Получить ошибку в установленном спецификацией формате.
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

        // Разбор входной строки и приведение данных к формату в соответствии со спецификацией.
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
                throw new Exception("Answer from device is incorrect." + input);
            }

            XMLtmp.Add("Date", workInput.Substring(0, workInput.IndexOf(" ")));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 2 

            XMLtmp.Add("Time", workInput.Substring(0, workInput.IndexOf(" ")));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 3

            XMLtmp.Add("Brutto", TonnsToKilos(workInput.Substring(0, workInput.IndexOf(" "))));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 4

            XMLtmp.Add("Platform1", TonnsToKilos(workInput.Substring(0, workInput.IndexOf(" "))));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 5

            XMLtmp.Add("Platform2", TonnsToKilos(workInput.Substring(0, workInput.IndexOf(" "))));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 6

            #region Правый борт (есть в ответе, но не требуется по спецификации), обрезаю
            //XMLtmp.Add("pravBort1_2", workInput.Substring(0, workInput.IndexOf(" ")));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 7 не требуется -> пропускаем
            #endregion

            #region  Левый борт (есть в ответе, но не требуется по спецификации), обрезаю
            //XMLtmp.Add("levBort3_4", workInput.Substring(0, workInput.IndexOf(" ")));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 8 не требуется -> пропускаем
            #endregion

            string Pp = workInput.Substring(0, workInput.IndexOf(" "));
            XMLtmp.Add("ShiftPop", workInput.Substring(0, workInput.IndexOf(" ")));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 9

            string Pr = workInput.Substring(0, workInput.IndexOf(" "));
            XMLtmp.Add("ShiftPro", workInput.Substring(0, workInput.IndexOf(" ")));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 10

            string delta = workInput.Trim();                                                                                                // 11
            XMLtmp.Add("Delta", workInput);

            return XMLtmp;
        }

        // Перевожу тонны в киллограммы, возвращаю в строковом представлении.
        private static string TonnsToKilos(string inputTonns) 
        {
            if (!string.IsNullOrEmpty(inputTonns)) 
            {
                double outputKilos = 0;
                if (double.TryParse(inputTonns, out outputKilos))
                {
                    return (outputKilos * 1000).ToString();
                }
                throw new Exception("Calculation mass is incorrect. | " + inputTonns +" |");
            }
            else
            {
                throw new Exception("Mass value is incorrect.");
            }
            
        }
    }
}
