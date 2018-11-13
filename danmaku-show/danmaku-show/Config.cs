using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace danmaku_show
{
    internal class Config
    {
        #region Room Config
        public bool ConnectRoomOnStartup { get => getProp<bool>("autoConnectRoom"); set => setProp("autoConnectRoom", value); }
        public bool ReconnectRoom { get => getProp<bool>("reconnectRoom"); set => setProp("reconnectRoom", value); }
        public int ReconnectRoomRetryCount { get => getProp<int>("reconnectRoomRetryCount"); set => setProp("reconnectRoomRetryCount", value); }
        #endregion

        #region Internal Server Config
        public int InternalServerPort { get => getProp<int>("internalServerPort"); set => setProp("internalServerPort", value); }
        public bool RunInternalServerOnStartup { get => getProp<bool>("autoStartInternalServer"); set => setProp("autoStartInternalServer", value); }
        public bool AutoRestartInternalServer { get => getProp<bool>("restartInternalServerWhileLost"); set => setProp("restartInternalServerWhileLost", value); }
        #endregion

        #region Push Action
        public int PushDuration { get => getProp<int>("pushDuration"); set => setProp("pushDuration", value); }
        public int ServerRetryCount { get => getProp<int>("restartServerRetryCount"); set => setProp("restartServerRetryCount", value); }
        public string[] PushMessageTypeFilter { get => getProp<string>("pushMessageTypes").Split(',').Select(x => x.Trim()).ToArray(); set => setProp("pushMessageTypes", string.Join(",", value)); }
        #endregion

        #region Others
        #endregion

        private void setProp<T>(string propName, T value)
        {
            try
            {
                Properties.Settings.Default[propName] = value;
                Properties.Settings.Default.Save();
            }
            catch
            {
                // do nothing.
            }
        }

        private T getProp<T>(string propName)
        {
            try
            {
                return (T)(Properties.Settings.Default[propName]);
            }
            catch
            {
                return default(T);
            }
        }

        private static Config _current = new Config();
        public static Config Current {
            get {
                if(_current == null)
                {
                    lock (_current) {
                        if (_current == null)
                        {
                            lock (_current)
                            {
                                _current = new Config();
                            }
                        }
                    }
                    return _current;
                }
                else
                {
                    return _current;
                }
            }
            set {
                lock (_current)
                {
                    _current = value;
                }
            }
        }
    }
}
