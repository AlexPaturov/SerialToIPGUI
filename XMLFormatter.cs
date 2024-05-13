using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace serialtoip
{
    public static class XMLFormatter
    {
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

                        XmlElement ch3_Type = xmlDoc.CreateElement("Type");
                        ch3_Type.InnerText = "V";
                        ch2_StaticData.AppendChild(ch3_Type);

                return Encoding.GetEncoding(1251).GetBytes(xmlDoc.OuterXml);
            }
            else 
            {
                return null; // вернуть ошибку
            }
        }

        public static byte[] GetError(Exception ex, int code) 
        {
            XmlDocument xmlDoc = new XmlDocument();                                                         // Create the XML declaration
            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);

            XmlElement rootResponse = xmlDoc.CreateElement("Response");                                     // Create the root element
            xmlDoc.AppendChild(rootResponse);

            XmlElement ch1_State = xmlDoc.CreateElement("State");                                           // Create first child elements
            ch1_State.InnerText = "Error";
            rootResponse.AppendChild(ch1_State);

                XmlElement ch2_ErrorDescription = xmlDoc.CreateElement("ErrorDescription");                 // Create second child elements
                rootResponse.AppendChild(ch2_ErrorDescription);

                XmlElement ch3_ErrorCode = xmlDoc.CreateElement("ErrorCode");                               
                ch3_ErrorCode.InnerText = code.ToString();
                ch2_ErrorDescription.AppendChild(ch3_ErrorCode);

                XmlElement ch3_ErrorText = xmlDoc.CreateElement("ErrorText");
                ch3_ErrorText.InnerText = ex.Message;
                ch2_ErrorDescription.AppendChild(ch3_ErrorText);

            return Encoding.GetEncoding(1251).GetBytes(xmlDoc.ToString());
        }

        // -----  ТЕСТОВАЯ ЗАГЛУШКА ------------------ по умолчанию, обязательное создание XML документа со строго описанной структурой --------------------------------------
        //public static string getTestNode() 
        //{
        //    XmlDocument xmlDoc = new XmlDocument();

        //    // Create the XML declaration
        //    XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            
        //    XmlElement rootElement = xmlDoc.CreateElement("Root");          // Create the root element
        //    xmlDoc.AppendChild(rootElement);                                // Append the root element to the document
        //    XmlElement childElement1 = xmlDoc.CreateElement("Child1");      // Create some child elements
        //    childElement1.InnerText = "Value1";
        //    rootElement.AppendChild(childElement1);

        //    XmlElement childElement2 = xmlDoc.CreateElement("Child2");
        //    childElement2.InnerText = "Value2";
        //    rootElement.AppendChild(childElement2);

        //    // конвертирование хмл в строку
        //    StringWriter stringWriter = new StringWriter();
        //    XmlTextWriter xmlTextWriter = new XmlTextWriter(stringWriter);

        //    xmlDoc.WriteTo(xmlTextWriter);
        //    return stringWriter.ToString();
        //    // конвертирование хмл в строку
        //}

        private static Dictionary<string, string> RawToXML(string input)
        {
            Dictionary<string, string> XMLtmp = new Dictionary<string, string>();
            string workInput = input;
            string prt = string.Empty;

            if (workInput.Contains("F#1"))                                                                                             // 1
            {
                //prt = workInput.Substring(workInput.IndexOf("F#1"), workInput.IndexOf("F#1")+3).Trim();
                //if (prt != "F#1")
                //{
                //    throw new Exception("Answer from device is incorrect." + input);
                //}
                workInput = workInput.Substring(workInput.IndexOf("F#1") + 3, (workInput.Length - (workInput.IndexOf("F#1") + 3))).Trim();  // F#1
            }
            else 
            {
                throw new Exception("Answer from device is incorrect." + input);
            }

            // Проверить на длину строки, если меньше -> бросаю исключение 

            //string data = workInput.Substring(0, workInput.IndexOf(" "));
            XMLtmp.Add("Date", workInput.Substring(0, workInput.IndexOf(" ")));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 2 брутто

            //string time = workInput.Substring(0, workInput.IndexOf(" "));
            XMLtmp.Add("Time", workInput.Substring(0, workInput.IndexOf(" ")));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 3

            //string brutto = workInput.Substring(0, workInput.IndexOf(" "));
            XMLtmp.Add("Brutto", workInput.Substring(0, workInput.IndexOf(" ")));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 4

            //string platforma1 = workInput.Substring(0, workInput.IndexOf(" "));
            XMLtmp.Add("Platform1", workInput.Substring(0, workInput.IndexOf(" ")));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 5

            //string platforma2 = workInput.Substring(0, workInput.IndexOf(" "));
            XMLtmp.Add("Platform2", workInput.Substring(0, workInput.IndexOf(" ")));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 6

            //string pravBort1_2 = workInput.Substring(0, workInput.IndexOf(" "));
            //XMLtmp.Add("pravBort1_2", workInput.Substring(0, workInput.IndexOf(" ")));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 7 не требуется -> пропускаем

            //string levBort3_4 = workInput.Substring(0, workInput.IndexOf(" "));
            //XMLtmp.Add("levBort3_4", workInput.Substring(0, workInput.IndexOf(" ")));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 8 не требуется -> пропускаем

            string Pp = workInput.Substring(0, workInput.IndexOf(" "));
            XMLtmp.Add("ShiftPop", workInput.Substring(0, workInput.IndexOf(" ")));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 9

            string Pr = workInput.Substring(0, workInput.IndexOf(" "));
            XMLtmp.Add("ShiftPro", workInput.Substring(0, workInput.IndexOf(" ")));
            workInput = workInput.Substring(workInput.IndexOf(" ") + 1, (workInput.Length - (workInput.IndexOf(" ") + 1)));                 // 10

            string delta = workInput.Trim();
            XMLtmp.Add("Delta", workInput);

            return XMLtmp;
        }
    }
}
