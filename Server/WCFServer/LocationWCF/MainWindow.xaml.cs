﻿using LocationWCFService.ServiceHelper;
using LocationWCFServices;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using DbModel.Tools;
using LocationServices.LocationCallbacks;
using LocationServices.Locations;
using LocationWCFService;
using System.Web.Http.SelfHost;
using WebApiService;
using LocationServices.Tools;
using Microsoft.Owin.Hosting;
using SignalRService.Hubs;

using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using BLL;
using DbModel.Location.Work;
using DbModel.LocationHistory.Data;
using EngineClient;
using Location.BLL;
using Location.TModel.Location.Alarm;
using Location.TModel.Location.AreaAndDev;
using LocationServer;
using LocationServer.Windows;
using LocationServices.Converters;
using LocationServices.Locations.Interfaces;
using TModel.Location.Data;
using TModel.Tools;
using WebNSQLib;
using LocationServer.Models.EngineTool;
using System.Text;
using LocationServer.Tools;
using LocationServer.Windows.Simple;
using NVSPlayer;
using LocationServer.Plugins.NVSPlayer.SDK;
using DbModel.Location.Alarm;
using WebApiCommunication.ExtremeVision;
using Newtonsoft.Json;
using DbModel.Location.AreaAndDev;
using Location.BLL.Tool;
using Base.Tools;
using LocationServices.Locations.Services;
using Location.TModel.Tools;
using Base.Common.Tools;
using System.Data;
using ExcelLib;

namespace LocationWCFServer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            this.Closed += MainWindow_Closed;
            this.Closing += MainWindow_Closing;
            LogEvent.InfoEvent += LogEvent_InfoEvent1;
        }

        private void LogEvent_InfoEvent1(LogEvent.LogEventInfo obj)
        {
            if (obj.Level == "Info")
            {
                Location.BLL.Tool.Log.Info(obj.Tag, obj.Msg);
            }
            else if(obj.Level == "Error")
            {
                Location.BLL.Tool.Log.Error(obj.Tag, obj.Msg);
            }
            else
            {
                Location.BLL.Tool.Log.Info(obj.Tag, obj.Msg);
            }  
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show("是否退出服务端程序", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                isCloseDaemonProcess= QuestionCloseDaemonProcess();

                var EnabelNVS = ConfigurationHelper.GetBoolValue("EnabelNVS");
                if (EnabelNVS || nginxCmdProcess!=null)
                {
                    if (nginxCmdProcess != null)
                    {
                        nginxCmdProcess.CloseMainWindow();
                    }

                    string nginx = AppDomain.CurrentDomain.BaseDirectory + "\\nginx-1.7.11.3-Gryphon\\stopq.bat";
                    if (File.Exists(nginx))
                    {
                        Process.Start(nginx); //关闭nginx-rtmp
                    }
                }
            }
            else
            {
                e.Cancel = true;
            }
        }

        private PositionEngineLog Logs = new PositionEngineLog();

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (Debugger.IsAttached)//调试模式
            {
                this.Topmost = false;//防止挡住代码
            }

            //LocationTestBox1.Logs = Logs;
            InitData();

            ServerManagerBox1.Init();

            if (EngineClientSetting.AutoStart)
            {
                ShowEngineClientWindow();
            }

            //var hex = "10 01 C0 01 02 85 A4 F4 8C C0 3A";
            //var bytes = ByteHelper.HexToBytes(hex);
            //var str = "85A4";
            //byte[] bytes1 = Encoding.UTF8.GetBytes(str);
            //byte[] bytes2 = Encoding.UTF32.GetBytes(str);
            //byte[] bytes4 = Encoding.ASCII.GetBytes(str);
            //byte[] bytes5 = Encoding.UTF7.GetBytes(str);
            //byte[] bytes6 = Encoding.Default.GetBytes(str);

            version = ConfigurationHelper.GetValue("ServerVersionCode");

            clientVersion = ConfigurationHelper.GetValue("ClientVersionCode");

            this.Title = "服务端    -v" + version+","+clientVersion;

            var isStartDaemon = ConfigurationHelper.GetBoolValue("StartDaemon");
            if (isStartDaemon && Debugger.IsAttached==false)
            {
                StartDaemon(false);
            }

            bool isAutoRun = RegeditRW.ReadIsAutoRun();

            var registerDaemonAutoRun = ConfigurationHelper.GetBoolValue("RegisterDaemonAutoRun");
            if (registerDaemonAutoRun)
            {
                if (isAutoRun == false)
                {
                    RegeditRW.SetIsAutoRun(true);
                    string path2 = System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName;
                    Log.Info(LogTags.Server, "注册开机自启动:" + path2);
                }
                else
                {
                    Log.Info(LogTags.Server, "开机自启动:" + isAutoRun);
                }
            }
            else
            {
                Log.Info(LogTags.Server, "开机自启动:" + isAutoRun);
            }

            timeTimer = new DispatcherTimer();
            timeTimer.Interval = TimeSpan.FromMilliseconds(500);
            //timer2.Interval = TimeSpan.FromSeconds(1);
            timeTimer.Tick += TimeTimer_Tick;
            timeTimer.Start();

            StaticEvents.LocateArchorByIp += StaticEvents_LocateArchorByIp;
        }

        private void StaticEvents_LocateArchorByIp(List<string> ips)
        {
            var win = new AreaCanvasWindow();
            win.LocateByIp(ips);
            win.Show();
        }

        private string version;

        private string clientVersion;

        private DispatcherTimer timeTimer;

        private DateTime startTime = DateTime.Now;

        private void TimeTimer_Tick(object sender, EventArgs e)
        {
            DateTime now = DateTime.Now;
            var time = (now - startTime);
            //this.Title = string.Format("{0}    -v({1}) [{2}][{3:dd\\.hh\\:mm\\:ss}]", "服务端", version+","+clientVersion, now.ToString("HH:mm:ss"), time);
            Status1.Content = string.Format("[{0}][{1}][{2:dd\\.hh\\:mm\\:ss}]", AppContext.ParkName,now.ToString("HH:mm:ss"), time);
        }

        private void InitData()
        {
            //StartReceiveAlarm();
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            Bll.StopThread();//关闭静态线程

            ServerManagerBox1.StopServices();
            ServerManagerBox1.StopLogTimer();
            ServerManagerBox1.StopListenLog();

            CloseDaemonProcess();
            if(Application.Current!=null)
                Application.Current.Shutdown();

            StaticEvents.LocateArchorByIp -= StaticEvents_LocateArchorByIp;
        }

        //private void BtnConnectEngine_Click(object sender, RoutedEventArgs e)
        //{
        //    if (BtnConnectEngine.Content.ToString() == "连接定位引擎")
        //    {
        //        StartConnectEngine();
        //        BtnConnectEngine.Content = "断开定位引擎";
        //    }
        //    else
        //    {
        //        StopConnectEngine();
        //        BtnConnectEngine.Content = "连接定位引擎";
        //    }
        //}

        //public void StartConnectEngine()
        //{
        //    LocationTestBox1.StartConnectEngine();
        //}

        //public void StopConnectEngine()
        //{
        //    LocationTestBox1.StopConnectEngine();
        //}

        private void BtnOpenU3D_OnClick(object sender, RoutedEventArgs e)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "Location\\Location.exe";
            if (File.Exists(path))
            {
                Process.Start(path);
            }
            else
            {
                MessageBox.Show("未找到文件:" + path);
            }
        }

        private void MenuLocationEngionTool_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new LocationEngineToolWindow();
            win.Show();
        }

        private void MenuExportArchorPosition_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new BusAnchorListWindow();
            win.Show();
        }

        private void MenuAreaCanvas_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new AreaCanvasWindow();
            win.Show();
        }

        private void MenuArchorSettingExport_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new ArchorListExportWindow();
            win.Show();
        }

        private void MenuDbExport_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new DbBrowserWindow();
            win.Show();
        }

        private void MenuDbInit_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new DbConfigureWindow();
            win.Show();
        }

        private void MenuLocationHistoryTest_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new LocationHistoryWindow();
            win.Show();
        }

        private void MenuEventSendTest_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new EventSendTestWindow();
            win.Show();
        }

        private void MenuEngineClient_OnClick(object sender, RoutedEventArgs e)
        {
            ShowEngineClientWindow();
        }

        private void ShowEngineClientWindow()
        {
            Log.Info(LogTags.Server, "打开定位引擎客户端窗口");
            var win = new EngineClientWindow();
            //win.Owner = this;
            win.Show();
        }

        private void MenuOpen3D_OnClick(object sender, RoutedEventArgs e)
        {
            string path = AppDomain.CurrentDomain.BaseDirectory + "Location\\Location.exe";
            if (File.Exists(path))
            {
                Process.Start(path);
            }
            else
            {
                MessageBox.Show("未找到文件:" + path);
            }
        }

        private void MenuCmd1_OnClick(object sender, RoutedEventArgs e)
        {
            LocationService client = new LocationService();
            var recv = client.GetAreaStatistics(1);
            int PersonNum = recv.PersonNum;
            int DevNum = recv.DevNum;
            int LocationAlarmNum = recv.LocationAlarmNum;
            int DevAlarmNum = recv.DevAlarmNum;
        }

        private void MenuCardRole_Click(object sender, RoutedEventArgs e)
        {
            var win = new CardRoleWindow();
            win.Show();
        }

        private void MenuAreaAuthorization_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new AreaAuthorizationWindow();
            win.Show();
        }

        private void MenuTag_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new TagWindow();
            win.Show();
        }

        private void MenuPerson_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new PersonWindow();
            win.Show();
        }

        private void MenuRealPos_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new RealPosWindow();
            win.Show();
        }

        private void MenuArchorScane_Click(object sender, RoutedEventArgs e)
        {

            Bll bll = Bll.NewBllNoRelation();
            var list3 = bll.Archors.GetInfoList();
            //var devs = bll.DevInfos.ToDictionary();

            var areas = bll.Areas.ToDictionary();
            if (list3 != null && areas != null)
            {
                foreach (var item in list3)
                {
                    if(item.ParentId!= null)
                    {
                        if (areas.ContainsKey((int)item.ParentId))
                        {
                            item.Parent = areas[(int)item.ParentId];
                        }
                        else
                        {

                        }
                    }
                }
            }

            var win = new ArchorConfigureWindow(list3);
            win.Show();

            //var win = new ArchorConfigureWindow();
            //win.Show();
        }

        private void MenuUDPSetting_Click(object sender, RoutedEventArgs e)
        {

        }

        private void MenuArchorCheck_Click(object sender, RoutedEventArgs e)
        {
            var win = new AnchorCheckWindow();
            win.Show();
        }

        private void MenuLocationAlarms_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new LocationAlarmWindow();
            win.Show();
        }

        private void MenuArchorSearch_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new ArchorWindow();
            win.Show();
        }

        private void MenuTrackPointList_Click(object sender, RoutedEventArgs e)
        {
            //var wind = new TrackPointListWindow();
            //wind.Show();

            var win = new ArchorListExportWindow();
            //win.CalculateAll = false;
            win.IsTrackPoint = true;
            win.Show();
        }

        private void GetAllNodeKKSData_OnClick(object sender, RoutedEventArgs e)
        {
            var window = new KKSMonitorDataWindow();
            window.Show();
            
        }

        private void MenuCache_OnClick(object sender, RoutedEventArgs e)
        {
            Bll bll = Bll.NewBllNoRelation();
            var bound1 = bll.Bounds.Find(i => i.Id == 1449);

            var bound2 = bll.Bounds.Find(i => i.Id == 1449);

            var bound3 = bll.Bounds.Find(i => i.Id == 1449);

            int nn = 0;
        }

        private void MenuClearDevAlarms_Click(object sender, RoutedEventArgs e)
        {
            Bll db = Bll.NewBllNoRelation();
            db.DevAlarms.Clear();
        }

        private void MenuGenerateDevAlarms_Click(object sender, RoutedEventArgs e)
        {
            Bll db = Bll.NewBllNoRelation();
            var devs = db.DevInfos.ToList();
            if (devs == null || devs.Count == 0) return;
            Random r = new Random((int)DateTime.Now.Ticks);
            DateTime now = DateTime.Now;
            for (int j = 0; j < 10; j++)
            {
                List<DeviceAlarm> alarms = new List<DeviceAlarm>();
                for (int i = 0; i < 10; i++)
                {
                    int devIndex = r.Next(devs.Count);
                    int month = r.Next(12) + 1;
                    int day = r.Next(28) + 1;
                    int hour = r.Next(24);
                    int m = r.Next(60);
                    int s = r.Next(60);
                    int lv = r.Next(5);
                    alarms.Add(new DeviceAlarm() { Level = (Abutment_DevAlarmLevel)lv, Title = "告警"+i, Message = "设备告警"+i, CreateTime = new DateTime(now.Year, now.Month, now.Day, hour, m, s) }.SetDev(devs[devIndex].ToTModel()));
                }
                db.DevAlarms.AddRange(alarms.ToDbModel());
            }
            MessageBox.Show("初始化成功");
        }

        private void MenuClearHisAlarms_Click(object sender, RoutedEventArgs e)
        {
            Bll db = Bll.NewBllNoRelation();
            DateTime now = DateTime.Now;
            //DateTime todayStart = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, now.Millisecond);
            DateTime todayStart = new DateTime(now.Year, now.Month, now.Day, 0, 0, 0, 0);
            DateTime todayEnd = new DateTime(now.Year, now.Month, now.Day, 23, 59, 59, 999);
            var tamp = TimeConvert.ToStamp(todayStart);
            var alarms=db.DevAlarms.Where(i => i.AlarmTimeStamp < tamp);
            bool r=db.DevAlarms.RemoveList(alarms);
            MessageBox.Show("清空成功");
        }

        private void MenuUpdatePersons_Click(object sender, RoutedEventArgs e)
        {
            UpdatePersonWindow wnd = new UpdatePersonWindow();
            wnd.Show();
        }

        private void MenuException_Click(object sender, RoutedEventArgs e)
        {
            string s = null;
            double a = 1;
            double b = 0;
            double c = a / b;
            string s2 = s.ToString();
        }

        private void SyncAllData_Click(object sender, RoutedEventArgs e)
        {
            var win = new SyncAllDataWindow();
            win.Show();
        }

        private void KKSMonitorManager_OnClick(object sender, RoutedEventArgs e)
        {
            var window = new KKSMonitorDataWindow();
            window.Show();
        }

        private void MenuInspectionTest_OnClick(object sender, RoutedEventArgs e)
        {
            var win = new InspectionChoiceWindows();
            win.Show();
        }

        private void MenuNVSPlayer_OnClick(object sender, RoutedEventArgs e)
        {
            //因为dll是64位的，程序生成也要是64位的，幸亏没有引用其他32位的dll

            //NVSPlayerForm form = new NVSPlayerForm();
            //form.Show();

            //NVSSDK.NetClient_Startup_V4(0, 0, 0);

            //NVRPlayerClientWindow window = new NVRPlayerClientWindow();
            //window.Show()

            NVSManage.RTMP_Host = ConfigurationHelper.GetValue("RTMP_Host");

            NVSManage.NVRIP = ConfigurationHelper.GetValue("NVRIP");
            NVSManage.NVRPort = ConfigurationHelper.GetValue("NVRPort");
            NVSManage.NVRUser = ConfigurationHelper.GetValue("NVRUser");
            NVSManage.NVRPass = ConfigurationHelper.GetValue("NVRPass");
            NVSManage.Init();//启动天地伟业Playback界面

            string nginx = AppDomain.CurrentDomain.BaseDirectory + "\\nginx-1.7.11.3-Gryphon\\restart-rtmp.bat";
            if (File.Exists(nginx))
            {
                nginxCmdProcess=Process.Start(nginx);//启动nginx-rtmp
            }
            else
            {
                //WriteLog("找不到nginx启动文件:" + nginx);
            }
        }

        private Process nginxCmdProcess;

        private void MenuSetting_OnClick(object sender, RoutedEventArgs e)
        {
            SettingWindow win = new SettingWindow();
            win.Show();
        }

        private void MenuStartDaemon_OnClick(object sender, RoutedEventArgs e)
        {
            StartDaemon(true);
        }


        private bool isCloseDaemonProcess = false;

        private bool QuestionCloseDaemonProcess()
        {
            var processes = GetDaemonProcessList();
            if (processes.Count > 0)
            {
                if (MessageBox.Show("是否关闭守护进程？\n不关闭则会重新启动服务端。", "确认", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    //foreach (Process process in processes)
                    //{
                    //    try
                    //    {
                    //        process.CloseMainWindow(); //关闭所有其他已经启动的守护进程
                    //    }
                    //    catch (Exception exception) //拒绝访问
                    //    {
                    //    }
                    //}
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }
        }

        private void CloseDaemonProcess()
        {
            if (isCloseDaemonProcess)
            {
                var processes = GetDaemonProcessList();
                if (processes.Count > 0)
                {
                    foreach (Process process in processes)
                    {
                        try
                        {
                            process.CloseMainWindow(); //关闭所有其他已经启动的守护进程
                        }
                        catch (Exception exception) //拒绝访问
                        {
                        }
                    }
                }
            }
        }

        private List<Process> GetDaemonProcessList()
        {
            string processName = ConfigurationHelper.GetValue("DaemonProcess");
            Process[] processes = Process.GetProcessesByName(processName);
            List<Process> list = processes.Where(i => i.HasExited == false).ToList();

            if (Debugger.IsAttached)//vs调试模式程序
            {
                list = Process.GetProcessesByName(processName + ".vshost").Where(i => i.HasExited == false).ToList();
            }

            return list;
        }

        private void StartDaemon(bool closeDaemon)
        {
            //var processName = "LocationDaemon";
            bool OnlyOneDaemon = ConfigurationHelper.GetBoolValue("OnlyOneDaemon");
            //string processName = ConfigurationHelper.GetValue("DaemonProcess");
            var process = GetDaemonProcessList();
                //string processName = "LocationDaemon";
            if (process.Count > 0)
            {
                if (closeDaemon == false)
                {
                    return;//程序启动时
                }
                else//手动启动收获进程
                {
                    if (OnlyOneDaemon)
                    {
                        foreach (Process process1 in process)
                        {
                            try
                            {
                                process1.CloseMainWindow(); //关闭所有其他已经启动的守护进程
                            }
                            catch (Exception e) //拒绝访问
                            {

                            }

                        }
                    }
                }

            }

            //string path = AppDomain.CurrentDomain.BaseDirectory + "LocationDaemon.exe";
            string path = ConfigurationHelper.GetValue("DaemonPath");

            FileInfo file = new FileInfo(path);
            if (File.Exists(file.FullName))
            {
                Process.Start(file.FullName);
            }
            else
            {
                FileInfo file2 = new FileInfo(AppDomain.CurrentDomain.BaseDirectory + path);
                if (File.Exists(file2.FullName))
                {
                    Process.Start(file2.FullName);
                }
                else
                {
                    Log.Info(LogTags.Server, "找不到文件:" + file.FullName);
                    Log.Info(LogTags.Server, "找不到文件:" + file2.FullName);
                }
            }
        }

        private void MenuOpenDir_OnClick(object sender, RoutedEventArgs e)
        {
            Process.Start(AppDomain.CurrentDomain.BaseDirectory);
        }

        private void MenuRefreshHisPosBuffer_OnClick(object sender, RoutedEventArgs e)
        {
            Worker.Run(() =>
            {
                var Phs = new LocationServices.Locations.Services.PosHistoryService();
                Phs.GetAllData(LogTags.HisPosBuffer, false);
            }, () => { });
            
        }

        private void MenuCameraAlarmManager_OnClick(object sender, RoutedEventArgs e)
        {
            CameraAlarmManagerWindow win = new CameraAlarmManagerWindow();
            win.Owner = this;
            win.Show();
        }

        private void MenuLookAlarms_Click(object sender, RoutedEventArgs e)
        {
            var win = new AlarmList();
            win.Show();
        }

        private void MenuArchorTableToXml_OnClick(object sender, RoutedEventArgs e)
        {
            Thread thread = new Thread(() =>
            {
                try
                {
                    string basePath = AppDomain.CurrentDomain.BaseDirectory;
                    string filePath = basePath + "Data\\基站信息\\基站信息.xls";

                    if (!File.Exists(filePath))
                    {
                        return;
                    }

                    string strXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>\n\r";
                    strXml += "<DepList>\n\r";

                    DataTable dt = ExcelHelper.LoadTable(new FileInfo(filePath), null, true);
                    Dictionary<string, List<string>> dict = new Dictionary<string, List<string>>();
                    foreach (DataRow row in dt.Rows)
                    {
                        string strId = row[0].ToString();
                        string strX = row[1].ToString();
                        string strY = row[3].ToString();
                        string strZ = row[2].ToString();
                        string strIp = row[4].ToString();
                        string strArea = row[5].ToString();

                        string strChildXml = "<Device Name=\"" + strId + "\" AnchorId=\"" + strId + "\" XPos=\"" + strX + "\" YPos=\"" + strY + "\" ZPos=\"" + strZ + "\" IP=\"" + strIp + "\" AbsolutePosX=\"" + strX + "\" AbsolutePosY=\"" + strY + "\" AbsolutePosZ=\"" + strZ + "\" />\n\r";
                        if (dict.ContainsKey(strArea))
                        {
                            dict[strArea].Add(strChildXml);
                        }
                        else
                        {
                            List<string> listChildXml = new List<string>();
                            listChildXml.Add(strChildXml);
                            dict.Add(strArea, listChildXml);
                        }
                    }

                    foreach (var v in dict)
                    {
                        string strKey = v.Key;
                        List<string> listChildXml = v.Value;
                        strXml += "<DeviceList Name=\"" + strKey + "\">\n\r";
                        foreach (string childXml in listChildXml)
                        {
                            strXml += childXml;
                        }

                        strXml += "</DeviceList>\n\r";
                    }

                    strXml += "</DepList>";
                    filePath = BLL.Tools.InitPaths.GetBaseStationInfo();

                    System.IO.File.WriteAllText(filePath, strXml, Encoding.UTF8);

                    MessageBox.Show("将表格转换为Xml成功");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("将表格转换为Xml失败");
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }
    }
}
