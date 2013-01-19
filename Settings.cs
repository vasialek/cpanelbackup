using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Threading;
using Av;
using Av.Utils;

namespace cPanelBackup
{
    class Settings
    {

        #region " Name / Version "
        public static string Name = "cPanelBackup";
        public static string Version = "0.3.4";
        public static string NameVersion
        {
            get { return string.Format("{0} (v{1})", Name, Version); }
        }
        #endregion


        /// <summary>
        /// Events which need attention in whole program, i.e. Exit
        /// </summary>
        public class Events
        {
            /// <summary>
            /// How many events are in list
            /// </summary>
            public const int Count = 1;

            /// <summary>
            /// Named ID of events, to help manage them
            /// </summary>
            public class EventId
            {
                public const int Exit = 0;
            }
            //public enum EventId { Exit = 0 };

            /// <summary>
            /// Array of events which could be raised
            /// </summary>
            public static ManualResetEvent[] ArEvents = null;

            static Events()
            {
                ArEvents = new ManualResetEvent[Events.Count];
                ArEvents[EventId.Exit] = new ManualResetEvent(false);
            }

            /// <summary>
            /// Converts ID of event to string name
            /// </summary>
            public static string IdToName(int id)
            {
                switch(id)
                {
                    case EventId.Exit:
                        return "Exit";
                }

                return id.ToString();
            }
        }

        public static void Load()
        {
            Log4cs.Log("Loading settings...");
        }

        public static string ToString()
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendFormat("Settings for {0} (v{1})", Name, Version);
            sb.AppendLine();

            return sb.ToString();
        }

    }
}
