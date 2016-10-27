using System;
using System.Text;
using System.Xml;
using System.IO;

namespace Ldap_ExtensionMobility
{
  

    public class AxlManager
    {
        private string _callManagerIP,
           _axlUser,
           _axlPassword,
           _cucmDbVersion;


        public AxlManager(string callManagerIP, string axlUser, string axlPassword, string cucmDbVersion = "11.5")
        {
            _callManagerIP = callManagerIP;
            _axlUser = axlUser;
            _axlPassword = axlPassword;
            _cucmDbVersion = cucmDbVersion;
        }

        public bool AuthenticateUser(string userId, string pin)
        {
            string request = String.Format("<ns:doAuthenticateUser><userid>{0}</userid><pin>{1}</pin></ns:doAuthenticateUser>", userId, pin);

            XmlDocument xmlDoc = AxlHttpCaller.DoSoapRequestXml(request, "doAuthenticateUser", _callManagerIP, _axlUser, _axlPassword, _cucmDbVersion);
            var result = xmlDoc.GetElementsByTagName("userAuthenticated");

            if (result.Count > 0)
            {
                return result[0].InnerText.Equals("true");
            }

            return false;
        }


        public void UpdatePhone(RPhone rPhone)
        {
            string request = String.Format("<ns:updatePhone><name>{0}</name><callingSearchSpaceName>{1}</callingSearchSpaceName></ns:updatePhone>",
                                            rPhone.Devicename,
                                            rPhone.CallingSearchSpaceName);

            AxlHttpCaller.DoSoapRequestXml(request, "updatePhone", _callManagerIP, _axlUser, _axlPassword, _cucmDbVersion) ;
        }

        public void UpdateUserPin(string userId)
        {
            string request = String.Format("<ns:updateUser><userid>{0}</userid><pin>98</pin></ns:updateUser>",userId);
            Console.WriteLine("UpdateUserPin >>>> request " +  request);
            Console.WriteLine("UpdateUserPin >>>>>   response : "  +xmlToString(AxlHttpCaller.DoSoapRequestXml(request, "updateUser", _callManagerIP, _axlUser, _axlPassword, _cucmDbVersion)));

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


        public void ResetDevice(string devicename)
        {
            string deviceResetSoap = "<ns:doDeviceReset><deviceName>" + devicename + "</deviceName><isHardReset>false</isHardReset></ns:doDeviceReset></ns:doDeviceReset>";
            AxlHttpCaller.DoSoapRequestXml(deviceResetSoap, "doDeviceReset", _callManagerIP, _axlUser, _axlPassword, _cucmDbVersion);
        }

        public RPhone GetPhone(string devicename)
        {
            XmlDocument xmlDoc = AxlHttpCaller.DoSoapRequestXml("<ns:getPhone><name>" + devicename + "</name></ns:getPhone>", "getPhone", _callManagerIP, _axlUser, _axlPassword, _cucmDbVersion);
            XmlNodeList nl = xmlDoc.GetElementsByTagName("phone");
            if (nl.Count > 0)
            {
                RPhone rp = new RPhone();
                rp.Devicename = devicename;

                XmlNode cssn = nl[0].SelectSingleNode("callingSearchSpaceName");
                rp.CallingSearchSpaceName = cssn.InnerText;

                XmlNode lines = nl[0].SelectSingleNode("lines");
                if (lines != null)
                {
                    XmlNode line = lines.SelectSingleNode("line");
                    XmlNode label = line.SelectSingleNode("label");
                    XmlNode displayAscii = line.SelectSingleNode("displayAscii");
                    XmlNode dirn = line.SelectSingleNode("dirn");
                    XmlNode pattern = dirn.SelectSingleNode("pattern");

                    rp.ASCIILabel = displayAscii.InnerText;
                    rp.Label = label.InnerText;
                    rp.Pattern = pattern.InnerText;

                    XmlNode endUsers = line.SelectSingleNode("associatedEndusers");
                    if (endUsers != null)
                    {
                        XmlNode endUser = endUsers.SelectSingleNode("enduser");
                        if (endUser != null)
                        {
                            XmlNode userId = endUser.SelectSingleNode("userId");
                            rp.AssociatedUser = userId.InnerText;
                        }
                    }

                }

                return rp;
            }

            return null;
        }


        public RUser GetUser(string userId)
        {
            //XmlDocument xmlDoc = DoSoapRequestXml("<ns:getUser><userid>" + userId + "</userid></ns:getUser>", "getUser");
            XmlDocument xmlDoc = AxlHttpCaller.DoSoapRequestXml("<ns:getUser><userid>" + userId + "</userid></ns:getUser>", "getUser", _callManagerIP, _axlUser, _axlPassword, _cucmDbVersion);
            XmlNodeList nl = xmlDoc.GetElementsByTagName("user");
            if (nl.Count > 0)
            {
                XmlNode fName = nl[0].SelectSingleNode("firstName");
                XmlNode lName = nl[0].SelectSingleNode("lastName");
                XmlNode dProfile = nl[0].SelectSingleNode("defaultProfile");

                if (string.IsNullOrEmpty(dProfile.InnerText))
                {
                    dProfile = nl[0].SelectSingleNode("phoneProfiles");
                    if (dProfile != null)
                        dProfile = dProfile.SelectSingleNode("profileName");
                }

                return new RUser()
                {
                    Firstname = fName.InnerText,
                    Lastname = lName.InnerText,
                    DefaultProfile = dProfile.InnerText
                };
            }

            return null;
        }


        public RDeviceProfile GetDeviceProfile(string profileName)
        {
            XmlDocument xmlDoc = AxlHttpCaller.DoSoapRequestXml("<ns:getDeviceProfile><name>" + profileName + "</name></ns:getDeviceProfile>", "getDeviceProfile", _callManagerIP, _axlUser, _axlPassword, _cucmDbVersion);
            XmlNodeList profile = xmlDoc.GetElementsByTagName("deviceProfile");

            if (profile.Count > 0)
            {
                RDeviceProfile pr = new RDeviceProfile();
                pr.Name = profile[0].SelectSingleNode("name").InnerText;

                XmlNode lines = profile[0].SelectSingleNode("lines");

                if (lines != null)
                {
                    XmlNode line = lines.SelectSingleNode("line");
                    XmlNode label = line.SelectSingleNode("label");
                    XmlNode displayAscii = line.SelectSingleNode("displayAscii");
                    XmlNode dirn = line.SelectSingleNode("dirn");
                    XmlNode uuid = dirn.SelectSingleNode("uuid");

                    pr.ASCIILabel = displayAscii.InnerText;
                    pr.Label = label.InnerText;
                    pr.Dirn = uuid.InnerText;
                }

                return pr;
            }
            return null;
        }

        public void UpdateDeviceProfile(RDeviceProfile profile)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("<ns:updateDeviceProfile>");
            sb.AppendFormat("<name>{0}</name>", profile.Name);
            sb.Append("<lines>");
            sb.Append("<line>");
            sb.Append("<index>1</index>");
            sb.AppendFormat("<label>{0}</label>", profile.Label);
            sb.Append("</line>");
            sb.Append("</lines>");
            sb.Append("</ns:updateDeviceProfile>");

            AxlHttpCaller.DoSoapRequestXml(sb.ToString(), "updateDeviceProfile", _callManagerIP, _axlUser, _axlPassword, _cucmDbVersion);
        }


        public class RUser
        {
            public string Firstname { get; set; }

            public string Lastname { get; set; }

            public string DefaultProfile { get; set; }
        }


        public class RDeviceProfile
        {
            public string Name { get; set; }

            public string Label { get; set; }

            public string ASCIILabel { get; set; }

            public string Dirn { get; set; }
        }

        public class RPhone
        {
            public string Devicename { get; set; }

            public string CallingSearchSpaceName { get; set; }

            public string Label { get; set; }

            public string ASCIILabel { get; set; }

            public string Pattern { get; set; }

            public string AssociatedUser { get; set; }
        }

    }
}
