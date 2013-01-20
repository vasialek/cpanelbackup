using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
//using System.Threading.Tasks;
using System.Windows.Forms;
using Av.Utils;
using System.Configuration;
using System.Collections.Specialized;

namespace cPanelBackup
{
    class Backup
    {
        public string Domain { get; private set; }
        public string Username { get; private set; }
        public string Password { get; private set; }
        public string Email { get; private set; }
        public string PostLoginUri { get; private set; }
        public static string[] getDomainList { get; private set; }
        public string Cookie { get; private set; }
        public string FtpHost { get; private set; }
        public string FtpUser { get; private set; }
        public string FtpPass { get; private set; }
        public string FtpDir { get; private set; }
        public string LoginUri { get; private set; }
        public static string[,] ServerList { get; private set; }

        private int _ftpPort = 21;

//      
        static List<string> _domainsList = null;

        /*public static string[] DomainsList
        {
            get
            {
                if( _domainsList == null )
                {
                    Log4cs.Log("Loading domains from application configurations:");
                    _domainsList = new List<string>();
                    NameValueCollection nvc = ConfigurationManager.GetSection("DomainsList") as NameValueCollection;

                    foreach(string item in nvc)
                    {
                        Log4cs.Log("  {0}", nvc[item]);
                        _domainsList.Add(nvc[item]);

                    }
                }
                return _domainsList.ToArray();
            }
        }*/

        /// <summary>
        /// Gets/sets port for FTP backup sending. Throws ArgumentOutOfRangeException on bad port!
        /// </summary>
        public int FtpPort
        {
            get
            {
                return _ftpPort;
            }
            set
            {
                if( value < 1 || value > 65535 )
                {
                    throw new ArgumentOutOfRangeException();
                }
                _ftpPort = value;
            }
        }

        public static string[,] InitializeLoginData()
        {
            string[,] loginData = new string[,]
                                    {
                                        { "server", "login", "pass", "ftp_dir" }
                                        , { "server", "login", "pass", "ftp_dir" }
                                    };
            ServerList = loginData;

            return loginData;
        }

        public void InitializeFtpData(string ftpHost, string ftpUser, string ftpPass, int ftpPort)
        {
            this.FtpHost = ftpHost;
            this.FtpUser = ftpUser;
            this.FtpPass = ftpPass;
            this.FtpPort = ftpPort;

        }

        public void sendPostRequest(string uri)
        {
            sendPostRequest(uri, 80, "");
        }

        public void sendPostRequest(string uri, int port = 80, string postData = "", int server = 0)
        {
            try
            {
                if (server == -1)
                {
                    MessageBox.Show("Error: Please select server!");
                }
                else
                {
                    Domain = "domain.com";
                    string[,] serverData = ServerList;
                    Username = serverData[server, 1];
                    Password = serverData[server, 2];
                    FtpDir = serverData[server, 3];
                    postData += "&rdir=" + FtpDir;

                    if (uri == null)
                    {
                        uri = Domain;
                    }
                    LoginUri = "http://" + uri + ":" + port + "/frontend/x3/backup/dofullbackup.html";

                    var cookies = new CookieContainer();
                    var request = (HttpWebRequest)WebRequest.Create(LoginUri);
                    request.CookieContainer = cookies;
                    request.Method = "POST";
                    request.ContentType = "application/x-www-form-urlencoded";
                    request.AllowAutoRedirect = false;

                    string authInfo = Username + ":" + Password;
                    authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                    request.Headers["Authorization"] = "Basic " + authInfo;

                    using (var requestStream = request.GetRequestStream())
                    using (var writer = new StreamWriter(requestStream))
                    {
                        writer.Write(postData);
                    }

                    using (var responseStream = request.GetResponse().GetResponseStream())
                    using (var reader = new StreamReader(responseStream))
                    {
                        string result = reader.ReadToEnd();
                        int start = result.IndexOf("<h1>Full Backup");
                        int end = result.IndexOf("</h1>");

                        // save needed string
                        MessageBox.Show(result.Substring(start + 4, end - start - 4));
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
