using System;
using System.Data;
using System.IO;
using System.Net;
using System.Xml;

namespace Ldap_ExtensionMobility
{
    public static class AxlHttpCaller
    {
        private static Object theLock = new Object();

        public static XmlDocument DoSoapRequestXml(string body, string header, string callManagerIP, string axlUser, string axlPassword, string cucmDbVersion = "11.5")
        {
            string soapEnvelope = "<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:ns=\"http://www.cisco.com/AXL/API/11.5\"><soapenv:Body>" + body + "</SOAP-ENV:Envelope>";

            ServicePointManager.ServerCertificateValidationCallback = ((sender, certificate, chain, sslPolicyErrors) => true); // Trust all certificates

            Console.WriteLine(string.Format("DoSoapRequest >>> {0}:{1}:{2}>\nheader={3}\nenvelope={4}",
                callManagerIP, cucmDbVersion, axlUser, header, soapEnvelope));

            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create("https://" + callManagerIP + ":8443/axl/");
            httpWebRequest.ContentType = "text/xml;charset=\"utf-8\"";
            httpWebRequest.ProtocolVersion = HttpVersion.Version10;
            httpWebRequest.Accept = "text/xml";
            httpWebRequest.Method = "POST";
            httpWebRequest.Headers.Add("SOAPAction: \"CUCM:DB ver=" + cucmDbVersion + " " + header + "\"");
            httpWebRequest.Credentials = new NetworkCredential(axlUser, axlPassword);

            lock (theLock)
            {
                using (StreamWriter streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    System.Text.StringBuilder soapRequest = new System.Text.StringBuilder();
                    soapRequest.Append(soapEnvelope);
                    streamWriter.Write(soapRequest.ToString());
                }

                using (WebResponse response = (WebResponse)httpWebRequest.GetResponse())
                {
                    XmlDocument xmlDocument = new XmlDocument();
                    xmlDocument.Load(response.GetResponseStream());
                    Console.WriteLine(string.Format("DoSoapRequest >>> response={0}", xmlToString(xmlDocument)));
                    return xmlDocument;
                }
            }
        }


        private static string xmlToString(XmlDocument xmlDoc)
        {
            using (StringWriter sw = new StringWriter())
            {
                using (XmlTextWriter tw = new XmlTextWriter(sw))
                {
                    xmlDoc.WriteTo(tw);
                    return sw.ToString();
                }
            }
        }

        private static string dataSetToString(DataSet ds)
        {
            using (StringWriter sw = new StringWriter())
            {
                ds.WriteXml(sw);
                return sw.ToString()
                        .Replace("  ", " ")
                        .Replace("\n", "")
                        .Replace("\t", "");
            }
        }

    }
}
