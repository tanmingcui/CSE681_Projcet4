using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/////////////////////////////////////////////////////////////////////
// TestRequest.cs - build and parse test requests                  //
//                                                                 //
// Author: Jim Fawcett, CST 4-187, jfawcett@twcny.rr.com           //
// Application: CSE681-Software Modeling and Analysis Demo         //
// Environment: C# console                                         //
/////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * ===================
 * Creates and parses TestRequest XML messages using XDocument
 * 
 * Required Files:
 * ---------------
 * TestRequest.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 07 Sep 2017
 * - first release
 * 
 */
using System.Xml.Linq;

namespace CSE681_Project4
{
    ///////////////////////////////////////////////////////////////////
    // TestRequest class

    public class TestReq
    {
        public string author { get; set; } = "";
        public string dateTime { get; set; } = "";
        public List<string> testDriver { get; set; } = new List<string>();
        public List<string> testedFiles { get; set; } = new List<string>();
        public XDocument doc { get; set; } = new XDocument();

        /*----< build XML document that represents a test request >----*/

        public void makeRequest()
        {
            XElement testRequestElem = new XElement("testRequest");
            doc.Add(testRequestElem);

            XElement authorElem = new XElement("author");
            authorElem.Add(author);
            testRequestElem.Add(authorElem);

            XElement dateTimeElem = new XElement("dateTime");
            dateTimeElem.Add(DateTime.Now.ToString());
            testRequestElem.Add(dateTimeElem);

            XElement testElem = new XElement("test");
            testRequestElem.Add(testElem);

            XElement driverElem = new XElement("testDriver");
            testRequestElem.Add(driverElem);

            foreach (string file in testedFiles)
            {
                XElement testedElem = new XElement("tested");
                testedElem.Add(file);
                testElem.Add(testedElem);
            }

            foreach (string file in testDriver)
            {
                XElement testdriverElem = new XElement("testDriver");
                testdriverElem.Add(file);
                testRequestElem.Add(testdriverElem);
            }
        }
        /*----< load TestRequest from XML file >-----------------------*/

        public bool loadXml(string path)
        {
            try
            {
                doc = XDocument.Load(path);
                return true;
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                return false;
            }
        }
        /*----< save TestRequest to XML file >-------------------------*/

        public bool saveXml(string path)
        {
            try
            {
                doc.Save(path);
                return true;
            }
            catch (Exception ex)
            {
                Console.Write(ex.Message);
                return false;
            }
        }
        /*----< parse document for property value >--------------------*/

        public string parse(string propertyName)
        {

            string parseStr = doc.Descendants(propertyName).First().Value;
            if (parseStr.Length > 0)
            {
                switch (propertyName)
                {
                    case "author":
                        author = parseStr;
                        break;
                    case "dateTime":
                        dateTime = parseStr;
                        break;
                    default:
                        break;
                }
                return parseStr;
            }
            return "";
        }
        /*----< parse document for property list >---------------------*/
        /*
        * - now, there is only one property list for tested files
        */
        public List<string> parseList(string propertyName)
        {
            List<string> values = new List<string>();

            IEnumerable<XElement> parseElems = doc.Descendants(propertyName);

            if (parseElems.Count() > 0)
            {
                switch (propertyName)
                {
                    case "tested":
                        foreach (XElement elem in parseElems)
                        {
                            values.Add(elem.Value);
                        }
                        testedFiles = values;
                        break;
                    default:
                        break;
                }
            }
            return values;
        }

        public List<string> parseDriverList(string propertyName)
        {
            List<string> values = new List<string>();

            IEnumerable<XElement> parseElems = doc.Descendants(propertyName);

            if (parseElems.Count() > 0)
            {
                switch (propertyName)
                {
                    case "testDriver":
                        foreach (XElement elem in parseElems)
                        {
                            values.Add(elem.Value);
                        }
                        testDriver = values;
                        break;
                    default:
                        break;
                }
            }
            return values;
        }
    }
}
