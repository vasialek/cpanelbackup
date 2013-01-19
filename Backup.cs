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

        private string _domain = "";
        private string _email = "";

        private string _ftpHost = "";
        private int _ftpPort = 21;
        private string _ftpDir = "";

        private string _loginUri;
        private string post_login_uri;

        static List<string> _domainsList = null;

        string cookieHeader;

        public string Domain()
        {
            return this._domain;
        }

        public string Username { get; set; }

        public string Password { get; set; }

        public string Email
        {
            get
            {
                return _email;
            }
            set
            {
                _email = value;
            }
        }

        public string getPostLoginUri()
        {
            return this.post_login_uri;
        }

        public void setPostLoginUri(string post_login)
        {
            this.post_login_uri = post_login;
        }

        public static string[] DomainsList
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
        }

        public string getCookie()
        {
            return this.cookieHeader;
        }

        public void setCookie(string cookieHeader)
        {
            this.cookieHeader = cookieHeader;
        }

        public string FtpHost
        {
            get
            {
                return _ftpHost;
            }
            set
            {
                _ftpHost = value;
            }
        }

        public string FtpUser { get; set; }

        public string FtpPass { get; set; }

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

        public string FtpDir
        {
            get
            {
                return _ftpDir;
            }
            set
            {
                _ftpDir = value;
            }
        }

        public void sendPostRequest(string uri)
        {
            sendPostRequest(uri, 80, "");
        }

        public void sendPostRequest(string uri, int port, string postData)
        {
            try
            {
                string result = "";
                _loginUri = String.Format("http://{0}:{1}/frontend/x3/backup/dofullbackup.html", uri, port);
                Log4cs.Log("Sending backup request: {0}", _loginUri);
                MessageBox.Show(_loginUri);
                var cookies = new CookieContainer();
                var request = (HttpWebRequest)WebRequest.Create(_loginUri);
                request.CookieContainer = cookies;
                request.Method = "POST";
                request.ContentType = "application/x-www-form-urlencoded";
                request.AllowAutoRedirect = false;

                string authInfo = String.Format("{0}:{1}", Username, Password);
                authInfo = Convert.ToBase64String(Encoding.Default.GetBytes(authInfo));
                request.Headers["Authorization"] = "Basic " + authInfo;

                using(var requestStream = request.GetRequestStream())
                using(var writer = new StreamWriter(requestStream))
                {
                    writer.Write(postData);
                }

                using(var responseStream = request.GetResponse().GetResponseStream())
                using(var reader = new StreamReader(responseStream))
                {
                    result = reader.ReadToEnd();
                    int start = result.IndexOf("<h1>Full Backup");
                    int end = result.IndexOf("</h1>");

                    // save needed string
                    MessageBox.Show(result.Substring(start + 4, end - start - 4));
                }
            } catch(Exception ex)
            {
                Log4cs.Log(Importance.Error, "Error sending backup request!");
                Log4cs.Log(Importance.Debug, ex.ToString());
                MessageBox.Show(ex.ToString());
            }
        }
    }
}
