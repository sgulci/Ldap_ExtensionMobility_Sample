using System;
using System.Collections.Generic;
using System.DirectoryServices.Protocols;

namespace Ldap_ExtensionMobility
{
    class Program
    {
        static void Main(string[] args)
        {
            using (LdapConnection connect = CreateConnection("yourdomain.com.tr"))
            {
                using (ChangeNotifier notifier = new ChangeNotifier(connect))
                {
                    //register some objects for notifications (limit 5)
                    notifier.Register("dc=yourdomain,dc=com,dc=tr", SearchScope.Subtree);
                    //notifier.Register("ou=users,dc=dunnry,dc=net", SearchScope.Base);

                    notifier.ObjectChanged += new EventHandler<ObjectChangedEventArgs>(notifier_ObjectChanged);

                    Console.WriteLine("Waiting for changes...");
                    Console.WriteLine();
                    Console.ReadLine();
                }
            }
        }

        static void notifier_ObjectChanged(object sender, ObjectChangedEventArgs e)
        {
            string samaccountname = "";

            Console.WriteLine(e.Result.DistinguishedName);
            foreach (string attrib in e.Result.Attributes.AttributeNames)
            {
                foreach (var item in e.Result.Attributes[attrib].GetValues(typeof(string)))
                {
                    Console.WriteLine("\t{0}: {1}", attrib, item);


                    if (attrib == "samaccountname")
                    {
                        samaccountname = item.ToString();
                    }

                    if (attrib == "isdeleted" && item.ToString() == "TRUE")
                    {
                        LogoutEMUser(samaccountname);

                        Console.WriteLine(" !!!!!Watchout!!!!  !!!!!!!!!!!!!!we logout CM EM User !!!!!!!!  For --> " + samaccountname);
                    }

                }
            }
            Console.WriteLine();
            Console.WriteLine("====================");
            Console.WriteLine();
        }




        static private void LogoutEMUser(string userName)
        {

            ExtensionMobilityManager emManager = new ExtensionMobilityManager("10.10.10.10", "appuser", "12345");
            AxlManager axlManager = new AxlManager("10.10.10.10", "appuser", "12345");

            string deviceName = emManager.GetCurrentDeviceUserLoggedIn(userName);

            emManager.Logout(deviceName);
            
            // Kullanıcı sililnemediğinden pin kodu değiştirilecek
            axlManager.UpdateUserPin(userName);
        }


        static private LdapConnection CreateConnection(string server)
        {
            //var networkCredential = new System.Net.NetworkCredential("admin", "Test12345");
            LdapConnection connect = new LdapConnection(server);
            connect.SessionOptions.ProtocolVersion = 3;
            connect.AuthType = AuthType.Negotiate;  //use my current credentials
            //connect.Credential = networkCredential;


            return connect;
        }
    }



    public class ChangeNotifier : IDisposable
    {
        LdapConnection _connection;
        HashSet<IAsyncResult> _results = new HashSet<IAsyncResult>();

        public ChangeNotifier(LdapConnection connection)
        {
            _connection = connection;
            _connection.AutoBind = true;
        }

        public void Register(string dn, SearchScope scope)
        {
            SearchRequest request = new SearchRequest(
                dn, //root the search here
                "(objectClass=*)", //very inclusive
                scope, //any scope works
                null //we are interested in all attributes
                );

            //register our search
            request.Controls.Add(new DirectoryNotificationControl());

            //we will send this async and register our callback
            //note how we would like to have partial results
            IAsyncResult result = _connection.BeginSendRequest(
                request,
                TimeSpan.FromDays(1), //set timeout to a day...
                PartialResultProcessing.ReturnPartialResultsAndNotifyCallback,
                Notify,
                request
                );

            //store the hash for disposal later
            _results.Add(result);
        }

        private void Notify(IAsyncResult result)
        {
            //since our search is long running, we don't want to use EndSendRequest
            PartialResultsCollection prc = _connection.GetPartialResults(result);

            foreach (SearchResultEntry entry in prc)
            {
                OnObjectChanged(new ObjectChangedEventArgs(entry));
            }
        }

        private void OnObjectChanged(ObjectChangedEventArgs args)
        {
            if (ObjectChanged != null)
            {
                ObjectChanged(this, args);
            }
        }

        public event EventHandler<ObjectChangedEventArgs> ObjectChanged;

        #region IDisposable Members

        public void Dispose()
        {
            foreach (var result in _results)
            {
                //end each async search
                _connection.Abort(result);
            }
        }

        #endregion
    }

    public class ObjectChangedEventArgs : EventArgs
    {
        public ObjectChangedEventArgs(SearchResultEntry entry)
        {
            Result = entry;
        }

        public SearchResultEntry Result { get; set; }
    }
}
