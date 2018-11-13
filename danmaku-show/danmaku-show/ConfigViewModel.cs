using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace danmaku_show
{
    class ConfigViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChange(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }

        #region Room Config
        public bool ConnectRoomOnStartup {
            get => Config.Current.ConnectRoomOnStartup;
            set {
                Config.Current.ConnectRoomOnStartup = value;
                RaisePropertyChange("ConnectRoomOnStartup");
            }
        }
        public bool ReconnectRoom {
            get => Config.Current.ReconnectRoom;
            set {
                Config.Current.ReconnectRoom = value;
                RaisePropertyChange("ReconnectRoom");
            }
        }
        public int ReconnectRoomRetryCount {
            get => Config.Current.ReconnectRoomRetryCount;
            set {
                Config.Current.ReconnectRoomRetryCount = value;
                RaisePropertyChange("ReconnectRoomRetryCount");
            }
        }
        #endregion

        #region Internal Server Config
        public int InternalServerPort {
            get => Config.Current.InternalServerPort;
            set {
                Config.Current.InternalServerPort = value;
                RaisePropertyChange("InternalServerPort");
            }
        }
        public bool RunInternalServerOnStartup {
            get => Config.Current.RunInternalServerOnStartup;
            set {
                Config.Current.RunInternalServerOnStartup = value;
                RaisePropertyChange("RunInternalServerOnStartup");
            }
        }
        public bool AutoRestartInternalServer {
            get => Config.Current.AutoRestartInternalServer;
            set {
                Config.Current.AutoRestartInternalServer = value;
                RaisePropertyChange("AutoRestartInternalServer");
            }
        }
        #endregion

        #region Push Action
        public int PushDuration {
            get => Config.Current.PushDuration;
            set {
                Config.Current.PushDuration = value;
                RaisePropertyChange("PushDuration");
            }
        }
        public int ServerRetryCount {
            get => Config.Current.ServerRetryCount;
            set {
                Config.Current.ServerRetryCount = value;
                RaisePropertyChange("ServerRetryCount");
            }
        }
        #endregion
    }

    public class ObserverableListItem<T>
    : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public void RaisePropertyChange(string prop)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(prop));
        }
        private T value;
        public T Value { get => this.value; set { this.value = value; RaisePropertyChange("Value"); } }
        private bool isSelected;
        public bool IsSelected { get => this.isSelected; set { this.isSelected = value; RaisePropertyChange("IsSelected"); } }
    }
}
