using BilibiliDM_PluginFramework;
using BiliDMLib;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private List<object> queue = new List<object>();
        private Process ServerProcess = new Process();
        private Thread PushThread;
        private object giftCountLock = new object();
        private object dmCountLock = new object();
        int giftCount = 0;
        int dmCount = 0;
        int port = 0;
        public MainWindow()
        {
            InitializeComponent();
            try
            {
                this.inpRoomid.Text = Properties.Settings.Default.roomid.ToString();
                this.inpPort.Text = Properties.Settings.Default.port.ToString();
                port = Properties.Settings.Default.port;
            }
            catch
            {
                this.inpRoomid.Text = "";
            }
            loader.Disconnected += Loader_Disconnected;
            loader.ReceivedDanmaku += Loader_ReceivedDanmaku;

            try
            {
                var psi = new ProcessStartInfo();
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.FileName = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\start_server.cmd";
                Trace.TraceInformation(psi.FileName);
                ServerProcess = Process.Start(psi);

                PushThread = new Thread(() => {
                    while (true)
                    {
                        SendMessage();
                        Thread.Sleep(200);
                    }
                });
                PushThread.Start();

                lbServerState.Text = "OK";
                lbServerState.Foreground = new SolidColorBrush(Color.FromRgb(0, 200, 0));
            }
            catch(Exception exc)
            {

            }
        }

        private void Loader_ReceivedDanmaku(object sender, ReceivedDanmakuArgs e)
        {
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
                lock (queue)
                {
                    queue.Add(new
                    {
                        type = "DANMAKU",
                        data = new
                        {
                            user = e.Danmaku.UserName,
                            isAdmin = e.Danmaku.isAdmin,
                            isVIP = e.Danmaku.isVIP,
                            text = e.Danmaku.CommentText
                        }
                    });
                }
            }else if (e.Danmaku.MsgType == MsgTypeEnum.GiftSend)
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
                lock (queue)
                {
                    queue.Add(new
                    {
                        type = "GIFT",
                        data = new
                        {
                            user = e.Danmaku.UserName,
                            gift = e.Danmaku.GiftName,
                            count = e.Danmaku.GiftCount
                        }
                    });
                }
            }else if(e.Danmaku.MsgType == MsgTypeEnum.LiveStart)
            {
                lock (queue)
                {
                    queue.Add(new
                    {
                        type = "SYTEM",
                        msg = "直播开始啦~"
                    });
                }
            }
            else if (e.Danmaku.MsgType == MsgTypeEnum.LiveEnd)
            {
                lock (queue)
                {
                    queue.Add(new
                    {
                        type = "SYTEM",
                        msg = "直播结束咯~"
                    });
                }
            }else if(e.Danmaku.MsgType == MsgTypeEnum.Welcome || e.Danmaku.MsgType == MsgTypeEnum.WelcomeGuard)
            {
                lock (queue)
                {
                    queue.Add(new
                    {
                        type = "WELCOME",
                        data = new
                        {
                            user = e.Danmaku.UserName,
                            isAdmin = e.Danmaku.isAdmin,
                            isVIP = e.Danmaku.isVIP
                        }
                    });
                }
            }
        }

        private void Loader_Disconnected(object sender, DisconnectEvtArgs e)
        {

        }

        private void SaveRoomId(int roomId)
        {
            try
            {
                Properties.Settings.Default.roomid = roomId;
                Properties.Settings.Default.Save();
            }
            catch (Exception)
            {
                // ignored
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
            }
            catch (Exception)
            {
                // ignored
            }
            //Do whatever you want here..
        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            var roomid = Convert.ToInt32(inpRoomid.Text);
            if (roomid > 0)
            {
                var connectresult = await loader.ConnectAsync(roomid);
                if(!connectresult && loader.Error != null)
                {
                    MessageBox.Show("发生错误无法继续", loader.Error.ToString(), MessageBoxButton.OK, MessageBoxImage.Error);
                }

                if (connectresult)
                {
                    lbConnectionState.Text = "OK";
                    lbConnectionState.Foreground = new SolidColorBrush(Color.FromRgb(0, 200, 0));
                    SaveRoomId(roomid);
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
                inpPort.Text = "";
            }
        }

        private void QueueMessage(object obj)
        {
            lock (queue)
            {
                queue.Add(obj);
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
            catch (Exception)
            {
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!ServerProcess.HasExited)
            {
                ServerProcess.Dispose();
            }
        }

        private void DispatcherUIUpdate(Action action) {
            this.Dispatcher.Invoke(action);
        }
    }
}
