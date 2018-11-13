using BilibiliDM_PluginFramework;
using BiliDMLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Net;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace danmaku_show
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        private readonly DanmakuLoader loader = new DanmakuLoader();
        private readonly List<object> queue = new List<object>();
        private Process ServerProcess = new Process();
        private Thread PushThread;
        private readonly object giftCountLock = new object();
        private readonly object dmCountLock = new object();
        private readonly ConfigViewModel confVM= new ConfigViewModel();
        int giftCount = 0;
        int dmCount = 0;
        int port = 0;
        const string pingPongToken = "bilibili-show-pong#Akaishi";
        public Dictionary<string, string> MessageEnum { get =>
            new Dictionary<string, string>{
                { "DANMAKU", "弹幕"},
                { "GIFT", "礼物"},
                { "SYSTEM", "系统消息"},
                { "WELCOME", "欢迎信息"},
            };
        }
        Logger logger;

        #region FLAG

        #endregion

        private string ServerRoot { get => "http://localhost:" + port; }

        public MainWindow()
        {
            InitializeComponent();
            logger = LogManager.GetCurrentClassLogger();
            logger.Trace("初始化窗体结束");

            try
            {
                this.inpRoomid.Text = Properties.Settings.Default.roomid.ToString();
                this.inpPort.Text = Properties.Settings.Default.port.ToString();
                port = Properties.Settings.Default.port;
                logger.Trace("从 App Settings 中读取讯息");
            }
            catch
            {
                this.inpRoomid.Text = "";
                this.inpPort.Text = "0";
                this.port = 0;
                logger.Error("无法读取 App Settings， 设置为默认值。");
            }
            loader.Disconnected += Loader_Disconnected;
            loader.ReceivedDanmaku += Loader_ReceivedDanmaku;

            // 启用推送服务
            try
            {
                PushThread = new Thread(() => {
                    while (true)
                    {
                        SendMessage();
                        Thread.Sleep(200);
                    }
                });
            }
            catch(Exception exc)
            {
                MessageBox.Show("无法启用推送服务，程序准备退出。");
                logger.Error("无法启用推送服务，程序准备退出。");
                logger.Error(exc.Message);
                logger.Error(exc.StackTrace);
                this.Close();
            }


            InitServer();
        }

        private void Loader_ReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
            logger.Trace("收到一条弹幕");
            if (e.Danmaku.MsgType == MsgTypeEnum.Comment)
            {
                lock (dmCountLock)
                {
                    dmCount++;
                }
                try
                {
                    DispatcherUIUpdate(() => {
                        lbDanmakuCount.Text = dmCount.ToString();
                    });
                }
                catch {
                    // do nothing.
                }
                var item = new
                {
                    type = "DANMAKU",
                    data = new
                    {
                        user = e.Danmaku.UserName,
                        isAdmin = e.Danmaku.isAdmin,
                        isVIP = e.Danmaku.isVIP,
                        text = e.Danmaku.CommentText
                    }
                };
                lock (queue)
                {
                    queue.Add(item);
                }
                logger.Trace("队列一条弹幕");
                logger.Info(JsonConvert.SerializeObject(item));
            }
            else if (e.Danmaku.MsgType == MsgTypeEnum.GiftSend)
            {
                lock (giftCountLock)
                {
                    giftCount+=e.Danmaku.GiftCount;
                }
                try
                {
                    DispatcherUIUpdate(() => {
                        lbGiftCount.Text = giftCount.ToString();
                    });
                }
                catch
                {
                    // do nothing.
                }
                var item = new
                {
                    type = "GIFT",
                    data = new
                    {
                        user = e.Danmaku.UserName,
                        gift = e.Danmaku.GiftName,
                        count = e.Danmaku.GiftCount
                    }
                };
                lock (queue)
                {
                    queue.Add(item);
                }
                logger.Trace("队列一条礼物");
                logger.Info(JsonConvert.SerializeObject(item));
            }
            else if(e.Danmaku.MsgType == MsgTypeEnum.LiveStart)
            {
                var item = new
                {
                    type = "SYTEM",
                    msg = "直播开始啦~"
                };
                lock (queue)
                {
                    queue.Add(item);
                }
                logger.Trace("队列一条消息");
                logger.Info(JsonConvert.SerializeObject(item));

            }
            else if (e.Danmaku.MsgType == MsgTypeEnum.LiveEnd)
            {
                var item = new
                {
                    type = "SYTEM",
                    msg = "直播结束咯~"
                };
                lock (queue)
                {
                    queue.Add(item);
                }
                logger.Trace("队列一条消息");
                logger.Info(JsonConvert.SerializeObject(item));
            }
            else if(e.Danmaku.MsgType == MsgTypeEnum.Welcome || e.Danmaku.MsgType == MsgTypeEnum.WelcomeGuard)
            {
                var item = new
                {
                    type = "WELCOME",
                    data = new
                    {
                        user = e.Danmaku.UserName,
                        isAdmin = e.Danmaku.isAdmin,
                        isVIP = e.Danmaku.isVIP
                    }
                };
                lock (queue)
                {
                    queue.Add(item);
                }
                logger.Trace("队列一条欢迎");
                logger.Info(JsonConvert.SerializeObject(item));
            }
        }

        private void Loader_Disconnected(object sender, DisconnectEvtArgs e)
        {
            DispatcherUIUpdate(() => {
                lbConnectionState.Text = "BAD";
                lbConnectionState.Foreground = new SolidColorBrush(Color.FromRgb(200, 0, 0));
            });
        }

        private void SaveRoomId(int roomId)
        {
            try
            {
                Properties.Settings.Default.roomid = roomId;
                Properties.Settings.Default.Save();
                logger.Trace("设置当前直播间id: "+roomId.ToString());
            }
            catch (Exception exc)
            {
                logger.Error("无法保存设定。");
                logger.Error(exc.Message);
                logger.Error(exc.StackTrace);
            }
            //Do whatever you want here..
        }

        private void SavePort(int port)
        {
            try
            {
                Properties.Settings.Default.port = port;
                this.port = port;
                Properties.Settings.Default.Save();
                logger.Trace("设置当前服务器端口: " + port.ToString());
            }
            catch (Exception exc)
            {
                logger.Error("无法保存设定。");
                logger.Error(exc.Message);
                logger.Error(exc.StackTrace);
            }
            //Do whatever you want here..
        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            var roomid = Convert.ToInt32(inpRoomid.Text);
            if (roomid > 0)
            {
                SaveRoomId(roomid);
                btnConnect.IsEnabled = false;
                loader.Disconnect();
                var connectresult = await loader.ConnectAsync(roomid);
                btnConnect.IsEnabled = true;
                if(!connectresult && loader.Error != null)
                {
                    MessageBox.Show("发生错误", loader.Error.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
                    logger.Fatal("发生错误，无法继续");
                    logger.Fatal(JsonConvert.SerializeObject(loader.Error));
                }

                if (connectresult)
                {
                    lbConnectionState.Text = "OK";
                    lbConnectionState.Foreground = new SolidColorBrush(Color.FromRgb(0, 200, 0));
                    btnConnect.Content = "重连";
                }
            }
        }

        private void btnConfig_Click(object sender, RoutedEventArgs e)
        {
            var port = Convert.ToInt32(inpPort.Text);
            if (port > 0)
            {
                SavePort(port);
            }
            else
            {
                inpPort.Text = "0";
                SavePort(0);
            }
        }

        private void SendMessage()
        {
            if(queue.Count == 0)
            {
                return;
            }
            List<object> ds;
            lock (queue)
            {
                ds = new List<object>(queue);
                logger.Trace("准备推送 " + ds.Count + "条消息。");
                logger.Info(JsonConvert.SerializeObject(ds));
                queue.Clear();
            }
            var body = JsonConvert.SerializeObject(new { count = ds.Count, ds = ds });
            var req = WebRequest.CreateHttp("http://localhost:" + port +"/danmaku");
            req.Method = "POST";
            req.ContentType = "application/json";
            using(var stream = req.GetRequestStream())
            {
                using(var writer = new StreamWriter(stream, Encoding.UTF8))
                {
                    writer.Write(body);
                    writer.Flush();
                }
            }
            try
            {
                using (var resp = req.GetResponse())
                {
                    using(var stream = resp.GetResponseStream())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            var str = reader.ReadToEnd();
                            var json = JObject.Parse(str);
                            if (!json["msg"].ToString().Equals("OK"))
                            {
                                throw new Exception();
                            }
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                logger.Fatal("推送失败，回滚操作。");
                logger.Fatal(exc.Message);
                logger.Fatal(exc.StackTrace);
                DispatcherUIUpdate(() => {
                    lbServerState.Text = "BAD";
                    lbServerState.Foreground = new SolidColorBrush(Color.FromRgb(200, 0, 0));
                });
                lock (queue)
                {
                    queue.InsertRange(0, ds);
                }
            }
        }

        private async Task<string> DownloadString(string url)
        {
            string result;
            using (var client = new WebClient())
            {
                result = await client.DownloadStringTaskAsync(url);
            }
            return result;
        }

        private async void InitServer()
        {
            logger.Trace("准备初始化服务器");
            try
            {
                if (await PingServer())
                {
                    DispatcherUIUpdate(() =>
                    {
                        lbServerState.Text = "OK (外部)";
                        lbServerState.Foreground = new SolidColorBrush(Color.FromRgb(0, 200, 0));
                    });
                    logger.Trace("获取到外部服务器，将直接开启推送服务。");
                    PushThread.Start();
                }
                else
                {
                    logger.Trace("启动服务器中");
                    await RunServer();
                }
            }catch(WebException webx)
            {
                logger.Fatal("发生网络错误。");
                if (webx.Response == null || (webx.Response as HttpWebResponse).StatusCode == HttpStatusCode.ServiceUnavailable)
                {
                    logger.Fatal("服务不存在，尝试启动服务器。");
                    await RunServer();
                }
                else
                {
                    DispatcherUIUpdate(() =>
                    {
                        lbServerState.Text = "端口被占用";
                        lbServerState.Foreground = new SolidColorBrush(Color.FromRgb(200, 0, 0));
                    });
                    logger.Fatal("端口被占用");
                }
            }
        }

        private async Task RunServer()
        {
            // 尝试在后台启用node服务器
            try
            {
                var psi = new ProcessStartInfo();
                psi.UseShellExecute = false;
#if DEBUG
                psi.CreateNoWindow = false;
#elif RELEASE
                psi.CreateNoWindow = true;
#endif
                psi.FileName = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\start_server.cmd";
                Trace.TraceInformation(psi.FileName);
                ServerProcess = Process.Start(psi);

            }
            catch (Exception exc)
            {
                DispatcherUIUpdate(() =>
                {
                    lbServerState.Text = "脚本运行错误";
                    lbServerState.Foreground = new SolidColorBrush(Color.FromRgb(200, 0, 0));
                });
                logger.Fatal("脚本运行错误");
                logger.Error(exc.Message);
                logger.Error(exc.StackTrace);
            }

            DispatcherUIUpdate(() =>
            {
                lbServerState.Text = "启动中...";
                lbServerState.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 200));
            });
            while (!ServerProcess.HasExited)
            {
                logger.Trace("持续检查服务器状态。");
                bool isServerAvaliable;
                try
                {
                    isServerAvaliable = await PingServer();
                }
                catch
                {
                    isServerAvaliable = false;
                }
                if (isServerAvaliable)
                {
                    DispatcherUIUpdate(() =>
                    {
                        lbServerState.Text = "OK (内部)";
                        lbServerState.Foreground = new SolidColorBrush(Color.FromRgb(0, 200, 0));
                    });
                    logger.Trace("内部服务器开启成功，开启推送服务。");
                    PushThread.Start();
                    return;
                }
                await Task.Delay(1000);
            }
            DispatcherUIUpdate(() =>
            {
                lbServerState.Text = "启动失败";
                lbServerState.Foreground = new SolidColorBrush(Color.FromRgb(200, 0, 0));
            });
            logger.Fatal("内部服务器开启失败。");
        }

        private async Task<bool> PingServer()
        {
            var str = await DownloadString(ServerRoot + "/ping");
            return str.Equals(pingPongToken, StringComparison.Ordinal);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!ServerProcess.HasExited)
            {
                var pid = ServerProcess.Id;
                Utils.KillProcessAndChildren(pid);
            }
            PushThread.Abort();
            Application.Current.Shutdown();
        }

        private void DispatcherUIUpdate(Action action) {
            this.Dispatcher.Invoke(action);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            pushTypeSelection.ItemsSource = MessageEnum;
            tabConfig.DataContext = confVM;
            LoadPushMessageTypeFilter();
        }

        private void pushTypeSelection_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SavePushMessageTypeFilter();
        }

        private void SavePushMessageTypeFilter()
        {
            if(pushTypeSelection.SelectedItems == null)
            {
                Config.Current.PushMessageTypeFilter = new string[0];
            }
            var list = new List<string>();
            foreach (KeyValuePair<string, string> i in pushTypeSelection.SelectedItems)
            {
                list.Add(i.Key);
            }
            Config.Current.PushMessageTypeFilter = list.ToArray();
        }

        private void LoadPushMessageTypeFilter()
        {
            if (pushTypeSelection.SelectedItems == null)
            {
                return;
            }
            var list = Config.Current.PushMessageTypeFilter;
            foreach (var f in list) {
                foreach (KeyValuePair<string, string> i in pushTypeSelection.Items)
                {
                    if (i.Key.Equals(f))
                    {
                        pushTypeSelection.SelectedItems.Add(i);
                    }
                }
            }
        }
    }
}
