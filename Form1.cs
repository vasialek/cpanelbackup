using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Av.Utils;
using System.Text;
using System.ComponentModel;

namespace cPanelBackup
{
    public partial class Form1 : Form
    {

        /// <summary>
        /// Systray icon
        /// </summary>
        protected NotifyIcon m_icon = null;

        #region " Get/set text methods (invoked) "

        delegate void SetTextDelegate(string name, string value);
        delegate string GetTextDelegate(string name);

        /// <summary>
        /// Returns text of control.
        /// </summary>
        /// <param name="name">Name of control</param>
        /// <returns>Text of control. Returns "checked"/"" or "" for CheckBox</returns>
        private string GetText(string name)
        {
            string s = "";

            try
            {
                if(this.InvokeRequired)
                {
                    this.Invoke(new GetTextDelegate(GetText), name);
                } else
                {
                    Control[] ar = this.Controls.Find(name, false);
                    if((ar != null) && (ar.Length > 0))
                    {
                        if(ar[0].GetType() == typeof(CheckBox))
                        {
                            s = ((CheckBox)ar[0]).Checked ? "checked" : "";
                        } else
                        {
                            s = ar[0].Text;
                        }
                    }
                }
            } catch(Exception ex)
            {
            }

            return s;
        }

        /// <summary>
        /// Thread safe AddText
        /// </summary>
        /// <param name="name">Name of control to add text</param>
        /// <param name="value">Text to add</param>
        private void AddText(string name, string value)
        {
            try
            {
                if(this.InvokeRequired)
                {
                    this.Invoke(new SetTextDelegate(AddText), name, value);
                } else
                {
                    Control[] ar = this.Controls.Find(name, false);
                    if((ar != null) && (ar.Length > 0))
                    {
                        ar[0].Text += value;
                        if(ar[0].GetType() == typeof(TextBox))
                        {
                            ((TextBox)ar[0]).SelectionStart = Int32.MaxValue;
                        }
                    }
                }
            } catch(Exception)
            {
            }
        }

        /// <summary>
        /// Thread safe SetText
        /// </summary>
        /// <param name="name">Name of control to set text</param>
        /// <param name="value">Text to set</param>
        private void SetText(string name, string value)
        {
            try
            {
                if(this.InvokeRequired)
                {
                    this.Invoke(new SetTextDelegate(SetText), name, value);
                } else
                {
                    Control[] ar = this.Controls.Find(name, false);
                    if((ar != null) && (ar.Length > 0))
                    {
                        ar[0].Text = value;
                    }
                }

            } catch(Exception ex)
            {
            }
        }

        #endregion

        public Form1()
        {
            InitializeComponent();
        }

        private void OnFormClosed(object sender, FormClosedEventArgs e)
        {
            if(m_icon != null)
            {
                m_icon.Dispose();
            }
        }

        private void OnFormLoad(object sender, EventArgs e)
        {
            try
            {
                Log4cs.Log("Starting {0} (v{1})...", Settings.Name, Settings.Version);
                Debug("Starting {0}...", Settings.NameVersion);

                // In case we need only one instance of program
                EnsureSingleApplication();

                this.Text = Settings.NameVersion;

                // Creates icons using emebedded icon
                CreateFormIcons();

                lstDomains.Items.AddRange(Backup.DomainsList);

                // Create context menu for tray icon
                //MenuItem[] arMenu = this.CreateMenuItems();
                //if(arMenu != null)
                //{
                //    m_icon.ContextMenu = new ContextMenu(arMenu);
                //}

                // Hides application (form) if necessary
                //HideApplication();

            } catch(ApplicationException ex)
            {
                Debug(Importance.Error, "Application already run, check logs!");
                MessageBox.Show(ex.ToString(), string.Format("{0} (v{1})", Settings.Name, Settings.Version), MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            } catch(Exception ex)
            {
                Log4cs.Log(Importance.Error, "Error loading main form!");
                Log4cs.Log(Importance.Debug, ex.ToString());
                MessageBox.Show("Error loading application!", string.Format("{0} (v{1})", Settings.Name, Settings.Version), MessageBoxButtons.OK, MessageBoxIcon.Error);
                this.Close();
            }
        }

        /// <summary>
        /// Hides application (form) and from taskbar
        /// </summary>
        protected void HideApplication()
        {
            this.ShowInTaskbar = false;
            this.Visible = false;
        }

        /// <summary>
        /// Hides from Alt-TAB
        /// </summary>
        //protected override CreateParams CreateParams
        //{
        //    get
        //    {
        //        CreateParams cp = base.CreateParams;

        //        // Turns on WS_EX_TOOLWINDOW style bit to hide from Alt-TAB list
        //        cp.ExStyle |= 0x80;

        //        return cp;
        //    }
        //}

        /// <summary>
        /// Creates tray icon from embedded to project icon. Throws execptions
        /// </summary>
        private void CreateFormIcons()
        {
            // Create tray icon
            Assembly asm = Assembly.GetExecutingAssembly();
            FileInfo fi = new FileInfo(asm.GetName().Name);
            using(Stream s = asm.GetManifestResourceStream(string.Format("{0}.av1.ico", fi.Name)))
            {
                // Create icon to be used in Form and Tray
                Icon icon = new Icon(s);

                Icon = new Icon(icon, icon.Size);

                m_icon = new NotifyIcon();
                m_icon.Visible = true;
                m_icon.Icon = new Icon(icon, icon.Size);

                icon.Dispose();
            }
        }

        /// <summary>
        /// Ensures that application is the only. Throws ApplicationException if there is such application
        /// </summary>
        private void EnsureSingleApplication()
        {
            bool createdNew = false;
            Mutex mx = new Mutex(false, Settings.Name, out createdNew);
            Log4cs.Log(Importance.Debug, "Is mutex created: {0}", createdNew);

            // If application is already running
            if(createdNew == false)
            {
                throw new ApplicationException(String.Format("{0} application is already running!", Settings.Name));
            }
        }

        Thread _backupThread = null;
        private void OnBackupButtonClicked(object sender, EventArgs e)
        {
            backgroundWorker1.RunWorkerAsync(lstDomains.SelectedItem);
        }

        /// <summary>
        /// Work as long as you need :)
        /// </summary>
        private void DoBackupWork(object sender, DoWorkEventArgs e)
        {
            Backup b = new Backup();
            string postData = "dest=ftp&email_radio=1&email=" + b.Email + "&server=" + b.FtpHost + "&user=" + b.FtpUser + "&pass=" + b.FtpPass + "&port=" + b.FtpPort + "&rdir=" + b.FtpDir;
            Log4cs.Log("Doing backup: {0}", postData);
            Debug("Going to send backup request");
            Thread.Sleep(10000);
        }

        private void OnBackupWorkCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Debug("Backup request is completed");
        }

        #region " Debug to UI console "

        /// <summary>
        /// Outputs message to UI output console
        /// </summary>
        /// <param name="msg">Message to input, could be used like string.Format()</param>
        /// <param name="args"></param>
        protected void Debug(string msg, params object[] args)
        {
            Debug(Importance.Info, msg, args);
        }

        /// <summary>
        /// Outputs message to UI output console
        /// </summary>
        /// <param name="level">Level of imortance - Error, Info, etc...</param>
        /// <param name="msg">Message to input, could be used like string.Format()</param>
        /// <param name="args"></param>
        protected void Debug(Importance level, string msg, params object[] args)
        {
            StringBuilder sb = new StringBuilder();
            if(level != Importance.No)
            {
                sb.AppendFormat("[{0}] ", level.ToString().ToUpper());
            }

            sb.AppendFormat(msg, args);
            sb.AppendLine();
            this.AddText("txtOutput", sb.ToString());
            txtOutput.SelectionStart = int.MaxValue;
            txtOutput.ScrollToCaret();
        }

        #endregion


        #region " Context menu methods "

        /// <summary>
        /// Creates context menu for tray icon
        /// </summary>
        /// <returns></returns>
        private MenuItem[] CreateMenuItems()
        {
            MenuItem[] arMenu = null;

            try
            {
                // Got quantity of menus
                arMenu = new MenuItem[MyMenu.Size];

                for(int i = 0; i < MyMenu.Size; i++)
                {
                    arMenu[i] = new MenuItem(MyMenu.ToName(i), OnContextMenuClicked);
                }

                // By default status thread is stopped, so disable "Stop" command
                arMenu[MyMenu.Position.Stop].Enabled = false;

                // Format "Version"
                arMenu[MyMenu.Position.Version].Text = string.Format("{0} (v{1})", Settings.Name, Settings.Version);

            } catch(Exception ex)
            {
                Log4cs.Log(Importance.Error, "Error creating menu items!");
                Log4cs.Log(Importance.Debug, ex.ToString());
            }

            return arMenu;
        }

        /// <summary>
        /// Handles clicks on systray icon menu
        /// </summary>
        void OnContextMenuClicked(object sender, EventArgs e)
        {
            try
            {
                switch(((MenuItem)sender).Index)
                {
                    case MyMenu.Position.Start:
                        m_icon.ContextMenu.MenuItems[MyMenu.Position.Start].Enabled = false;
                        m_icon.ContextMenu.MenuItems[MyMenu.Position.Stop].Enabled = true;
                        OnStartStopClicked(null, null);
                        break;
                    case MyMenu.Position.Stop:
                        m_icon.ContextMenu.MenuItems[MyMenu.Position.Start].Enabled = true;
                        m_icon.ContextMenu.MenuItems[MyMenu.Position.Stop].Enabled = false;
                        OnStartStopClicked(null, null);
                        break;
                    case MyMenu.Position.Version:
                        MessageBox.Show("Simple Aiva Helper by mr. Aleksej Vasinov", Settings.NameVersion, MessageBoxButtons.OK, MessageBoxIcon.Information);
                        break;
                    case MyMenu.Position.Reload:
                        ReloadSettings();
                        break;
                    default:
                        this.Close();
                        break;
                }

            } catch(Exception ex)
            {
                Log4cs.Log(Importance.Error, "Error handling context menu click!");
                Log4cs.Log(Importance.Debug, ex.ToString());
            }
        }


        /// <summary>
        /// Reloads setting. Stops thread if necessary
        /// </summary>
        private void ReloadSettings()
        {
            Log4cs.Log("Reloading settings...");
            Settings.Load();
        }

        /// <summary>
        /// Starts/stops application if needed :)
        /// </summary>
        private void OnStartStopClicked(object sender, object e)
        {
            Log4cs.Log("Start/stop is clicked...");
        }

        #endregion

    }  // END CLASS Form1

}
