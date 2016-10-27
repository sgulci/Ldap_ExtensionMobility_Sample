using System;
using System.IO;
using System.Net;
using System.Text;
using System.Xml;

namespace Ldap_ExtensionMobility
{
    public class ExtensionMobilityManager
    {
        private string _cucmIp, _user, _password;      

        public ExtensionMobilityManager(string ccmIp, string user, string password)
        {
            _cucmIp = ccmIp;
            _user = user;
            _password = password;
        }

        private string _appInfo;

        private string AppInfo
        {
            get
            {
                if (String.IsNullOrEmpty(_appInfo))
                {
                    _appInfo = String.Format("<appInfo><appID>{0}</appID><appCertificate>{1}</appCertificate></appInfo>", _user, _password);
                }
                return _appInfo;
            }
        }

        public void Login(string devicename, string userId, string deviceProfile, int exclusiveDuration)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("xml=<request>");
                sb.Append(AppInfo);
                sb.Append("<login>");
                sb.AppendFormat("<deviceName>{0}</deviceName>", devicename);
                sb.AppendFormat("<userID>{0}</userID>", userId);
                sb.AppendFormat("<deviceProfile>{0}</deviceProfile>", deviceProfile);
                sb.AppendFormat("<exclusiveDuration><time>{0}</time></exclusiveDuration>", exclusiveDuration);
                sb.Append("</login>");
                sb.Append("</request>");

                XmlDocument xmlDoc = ExecuteQueryOnEMAPI(sb.ToString());
                if (!xmlDoc.InnerXml.Contains("success"))
                {
                    XmlNodeList errors = xmlDoc.GetElementsByTagName("error");
                    if (errors.Count > 0)
                        throw new Exception(errors[0].InnerText);
                    else
                        throw new Exception(string.Format("{0} could not succeeded.", sb.ToString()));
                }

            }
            catch (Exception ex)
            {
                //log(string.Format("ExtensionMobilityManager.login >>> {0}", ex));
                Console.WriteLine(string.Format("ExtensionMobilityManager.login >>> {0}", ex));
            }

        }

        public void Logout(string devicename)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("xml=<request>");
            sb.Append(AppInfo);
            sb.Append("<logout>");
            sb.AppendFormat("<deviceName>{0}</deviceName>", devicename);
            sb.Append("</logout>");
            sb.Append("</request>");

            XmlDocument xmlDoc = ExecuteQueryOnEMAPI(sb.ToString());
            if (!xmlDoc.InnerXml.Contains("success"))
            {
                XmlNodeList errors = xmlDoc.GetElementsByTagName("error");
                if (errors.Count > 0)
                    throw new Exception(errors[0].InnerText);
                else
                    throw new Exception(string.Format("{0} could not succeeded.", sb.ToString()));
            }
        }

        public XmlDocument deviceUserQuery(string deviceName)
        {

            StringBuilder sb = new StringBuilder();
            sb.Append("xml=<query>");
            sb.Append(AppInfo);
            sb.AppendFormat("<deviceUserQuery><deviceName>{0}</deviceName></deviceUserQuery>", deviceName);
            sb.Append("</query>");

            return ExecuteQueryOnEMAPI(sb.ToString());

        }

        public XmlDocument userDeviceQuery(string userName)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("xml=<query>");
            sb.Append(AppInfo);
            sb.AppendFormat("<userDevicesQuery><userID>{0}</userID></userDevicesQuery>", userName);
            sb.Append("</query>");

            return ExecuteQueryOnEMAPI(sb.ToString());

        }

        public string GetCurrentDeviceUserLoggedIn(string userName, XmlDocument xmlDoc = null)
        {
            if (xmlDoc == null)
                xmlDoc = userDeviceQuery(userName);

            if (xmlDoc.GetElementsByTagName("deviceName").Count > 0)
            {
                return xmlDoc.GetElementsByTagName("deviceName")[0].InnerText;
            }
            else
            {
                return String.Empty;
            }
        }

        public string GetCurrentLoggedInUserOnDevice(string devicename, XmlDocument xmlDoc = null)
        {
            if (xmlDoc == null)
                xmlDoc = deviceUserQuery(devicename);

            if (xmlDoc.GetElementsByTagName("userID").Count > 0)
            {
                return xmlDoc.GetElementsByTagName("userID")[0].InnerText;
            }
            else
            {
                return String.Empty;
            }
        }

        public string GetLastLoggedInUserOnDevice(string devicename, XmlDocument xmlDoc = null)
        {
            if (xmlDoc == null)
                xmlDoc = deviceUserQuery(devicename);

            if (xmlDoc.GetElementsByTagName("lastlogin").Count > 0)
            {
                return xmlDoc.GetElementsByTagName("lastlogin")[0].InnerText;
            }
            else
            {
                return String.Empty;
            }
        }

        private XmlDocument ExecuteQueryOnEMAPI(string xmlQuery)
        {
            string emService = "http://" + _cucmIp + ":8080/emservice/EMServiceServlet";
            HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(emService);
            httpWebRequest.ContentType = "application/x-www-form-urlencoded";
            httpWebRequest.Accept = "text/xml";
            httpWebRequest.Method = "POST";
            httpWebRequest.Credentials = CredentialCache.DefaultCredentials;

            using (StreamWriter streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
            {
                streamWriter.Write(xmlQuery);
            }

            XmlDocument xmlDoc = new XmlDocument();

            try
            {
                using (WebResponse response = (WebResponse)httpWebRequest.GetResponse())
                {
                    xmlDoc.Load(response.GetResponseStream());
                }

                //log(string.Format("ExecuteQueryOnEMAPI >>> query={0}\nresponse={1}", xmlQuery, xmlToString(xmlDoc)));
                Console.WriteLine(string.Format("ExecuteQueryOnEMAPI >>> query={0}\nresponse={1}", xmlQuery, xmlToString(xmlDoc)));
            }
            catch (Exception ex)
            {
                //log(string.Format("ExecuteQueryOnEMAPI >>> query={0}\nexception={1}", xmlQuery, ex));
                Console.WriteLine(string.Format("ExecuteQueryOnEMAPI >>> query={0}\nexception={1}", xmlQuery, ex));
            }

            return xmlDoc;
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
    }
}
