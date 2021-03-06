﻿using LocationWCFServices;
using System;
using System.Collections.Generic;
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
using System.Web.Http.SelfHost;
using WebApiService;
using Microsoft.Owin.Hosting;
using SignalRService.Hubs;

using System.ServiceModel.Channels;
using System.ServiceModel.Web;
using System.Windows.Controls;
using BLL;
using EngineClient;
using LocationServices.Converters;
using LocationServices.Locations.Interfaces;
using WebNSQLib;
using LocationServer.Tools;
using Location.BLL.Tool;
using WebApiLib.Clients;
using NsqSharp.Utils;
using NVSPlayer;
using System.Collections.Concurrent;
using DbModel;
using Location.TModel.Tools;
using LocationServer.Threads;
using Base.Common.Threads;
using ArchorUDPTool.Tools;

namespace LocationServer.Controls
{
    /// <summary>
    /// Interaction logic for ServerManagerBox.xaml
    /// </summary>
    public partial class ServerManagerBox : UserControl
    {
        public ServerManagerBox()
        {
            InitializeComponent();
            Location.BLL.Tool.Log.NewLogEvent += ListenToLog;
            GoFunc.InfoEvent += GoFunc_InfoEvent;

            TbPort.Text = ConfigurationHelper.GetValue("Port");
            TbHost.Text = ConfigurationHelper.GetValue("Host");
        }

        private void GoFunc_InfoEvent(string obj)
        {
            Log.Info(obj);
        }

        public void StopListenLog()
        {
            Location.BLL.Tool.Log.NewLogEvent -= ListenToLog;
            GoFunc.InfoEvent -= GoFunc_InfoEvent;
        }

        public void StopServices()
        {
            try
            {
                WriteLog("停止服务");
                //StopConnectEngine();
                if (LocationService.u3dositionSP != null)
                {
                    LocationService.u3dositionSP.Stop();
                    LocationService.u3dositionSP = null;
                    WriteLog("停止LocationService.u3dositionSP");
                }
                if (locationServiceHost != null)
                {
                    locationServiceHost.Close();
                    locationServiceHost = null;
                    WriteLog("停止locationServiceHost");
                }
                if (httpHost != null)
                {
                    httpHost.CloseAsync();
                    httpHost = null;
                    WriteLog("停止httpHost");
                }
                if (SignalR != null)
                {
                    SignalR.Dispose();
                    SignalR = null;
                    WriteLog("停止SignalR");
                }

                if (wcfApiHost != null)
                {
                    wcfApiHost.Close();
                    wcfApiHost = null;
                    WriteLog("停止wcfApiHost");
                }
                
                StopReceiveAlarm();
                StopRemoveAlarm();
                StopLocationAlarmService();
                StopGetInspectionTrack();
                StopExtremeVisionListener();
                StopGetHistoryPositon();

                WriteLog("SisDataSaveClient.Stop");
                SisDataSaveClient.Stop();

                WriteLog("NVSManage.Stop");
                NVSManage.Stop();
                if (nginxCmdProcess != null)
                {
                    nginxCmdProcess.CloseMainWindow();
                }

                if (filterErrorPointThread != null)
                {
                    filterErrorPointThread.Abort();
                    filterErrorPointThread = null;
                }

                StopAnchorScan();
                StopCheckRepeatDev();
                StopDBBackupThread();
            }
            catch (Exception ex)
            {
                WriteLog(ex.ToString());
            }
        }

        public void StopAnchorScan()
        {
            if (anchorScanThread != null)
            {
                anchorScanThread.Abort();
                anchorScanThread = null;
            }
        }

        RepeatDevInfoCheckThread repeatDevInfoCheckThread;

        private void StartCheckRepeatDev()
        {
            if (AppContext.RepeatDevInfoCheckInterval > 0)
            {
                if(repeatDevInfoCheckThread== null)
                {
                    repeatDevInfoCheckThread = new RepeatDevInfoCheckThread(AppContext.RepeatDevInfoCheckInterval);
                    repeatDevInfoCheckThread.Start();
                }
            }
        }

        private void StopCheckRepeatDev()
        {
            if(repeatDevInfoCheckThread!= null)
            {
                repeatDevInfoCheckThread.Abort();
                repeatDevInfoCheckThread = null;
            }
        }


        /// <summary>
        /// 设备告警对接服务 基于NSQ消息队列获取第三方告警信息 项目：WebNSQLib
        /// </summary>
        private void StartReceiveAlarm()
        {
            bool enableAlarmRecieve = ConfigurationHelper.GetBoolValue("EnableAlarmRecieve");
            if (enableAlarmRecieve)
            {
                RealAlarm ra = new RealAlarm(Mh_DevAlarmReceived);
                if (alarmReceiveThread == null)
                {
                    alarmReceiveThread = new Thread(ra.ReceiveRealAlarmInfo);
                    alarmReceiveThread.IsBackground = true;
                    alarmReceiveThread.Start();
                }
            }

            //RecvBaseCommunication Rbc = new RecvBaseCommunication();
            //Rbc.RvoPa.DevAlarmReceived += Mh_DevAlarmReceived;
            //Rbc.Raca.DevAlarmReceived += Mh_DevAlarmReceived;
            //Rbc.Rfa.DevAlarmReceived += Mh_DevAlarmReceived;
            //if (alarmReceiveThread == null)
            //{
            //    alarmReceiveThread = new Thread(Rbc.StartConnectTopic);
            //    alarmReceiveThread.IsBackground = true;
            //    alarmReceiveThread.Start();
            //}

            DevAlarmKeepDays = ConfigurationHelper.GetIntValue("DevAlarmKeepDays");
            if (DevAlarmKeepDays == 0)
            {
                //MessageBox.Show("默认不清除！");
                Log.Info(LogTags.Server, "不清除历史设备告警");
            }
            else
            {
                Log.Info(LogTags.Server, "历史设备告警保留时间:" + DevAlarmKeepDays + "天");
                if (alarmRemoveThread == null)
                {
                    alarmRemoveThread = new AlarmRemoveThread(DevAlarmKeepDays);
                    alarmRemoveThread.Start();
                }
            }
        }

        public List<FunctionThread> FuncThreads = new List<FunctionThread>();

        private int DevAlarmKeepDays = 0;

        private void RemoveOldDevAlarms()
        {
            while (true)
            {
                try
                {
                    //清除某一个时间之前的所有告警
                    Bll db = Bll.NewBllNoRelation();
                    DateTime nowTime = DateTime.Now;
                    DateTime starttime = DateTime.Now.AddDays(-DevAlarmKeepDays);
                    var starttimeStamp = TimeConvert.ToStamp(starttime);
                    var query = db.DevAlarms.DbSet.Where(i => i.AlarmTimeStamp < starttimeStamp);
                    var count = query.Count();
                    if (count > 0)
                    {
                        query.DeleteFromQuery();//这样删除效率高

                        //var alarms = db.DevAlarms.Where(i => i.AlarmTimeStamp < starttimeStamp);
                        //bool r = db.DevAlarms.RemoveList(alarms);
                        //MessageBox.Show("清空成功");
                        Log.Info("RemoveAlarm", "清除历史设备告警,数量:" + count);
                    }
                    Thread.Sleep(1000 * 3600);//一小时检查一次
                }
                catch (Exception ex)
                {
                    Log.Error("RemoveAlarm", ex.ToString());
                }
            }
        }

        private void Mh_DevAlarmReceived(DbModel.Location.Alarm.DevAlarm obj)
        {
            AlarmHub.SendDeviceAlarms(obj.ToTModel());
        }

        private void StopReceiveAlarm()
        {
            if (alarmReceiveThread != null)
            {
                alarmReceiveThread.Abort();
                alarmReceiveThread = null;
            }
        }

        private void StopRemoveAlarm()
        {
            if (alarmRemoveThread != null)
            {
                alarmRemoveThread.Abort();
                alarmRemoveThread = null;
            }
        }

        private Thread alarmReceiveThread;
        /// <summary>
        /// 告警清除线程
        /// </summary>
        private AlarmRemoveThread alarmRemoveThread;

        private void BtnStartService_Click(object sender, RoutedEventArgs e)
        {
            ClickStart();
        }

        public void Init()
        {
            Log.Info(LogTags.Server,"调试模式:"+ AppContext.DebugMode);

            InitLogTimer();

            if (AppContext.AutoStartServer)
            {
                ClickStart();
            }

            StartAnchorScan();

            StartReceiveAlarm();//设备告警对接服务 基于NSQ消息队列获取第三方告警信息 项目：WebNSQLib

            StartCheckRepeatDev();
        }

        public void ClickStart()
        {
            if (BtnStartService.Content.ToString() == "启动服务")
            {
                string host = TbHost.Text;
                string port = TbPort.Text;

                //StartService(host, port);
                //BtnStartService.Content = "停止服务";

                

                Worker.Run(() =>
                {
                    StartService(host, port);
                }, () =>
                {
                    StartPlayBackManage();//不能放到线程里面运行

                    BtnStartService.Content = "停止服务";
                });
            }
            else
            {
                StopServices();
                BtnStartService.Content = "启动服务";
            }
        }

        public void InitLogTimer()
        {
            if (logTimer == null)
            {
                logTimer = new DispatcherTimer();
                logTimer.Interval = TimeSpan.FromMilliseconds(300);
                logTimer.Tick += LogTimer_Tick;
            }
            logTimer.Start();
        }

        public void StopLogTimer()
        {
            if (logTimer != null)
            {
                logTimer.Stop();
            }
        }

        private void CheckCardRole()
        {
            try
            {
                var bll = Bll.NewBllNoRelation();
                var cards = bll.LocationCards.ToList();
                var roles = bll.Roles.ToList();
                //var noRoleCards = cards.Where(i => i.CardRoleId == null);
            }
            catch (Exception e)
            {
                Log.Error(LogTags.Server, "CheckCardRole！：" + e.Message);
            }
            
        }

        private void StartService(string host, string port)
        {
            try
            {
                AppContext.CurrentHost = host;
                AppContext.CurrentPort = port;

                WriteLog("启动服务");

                //WCF服务 项目：Location.Service
                StartLocationService(host, port);

                //基于WCF接口同时兼容的WebApi服务 http://{0}:8733/LocationService/api WebServiceHost 项目：Location.Service
                StartLocationServiceApi(host, port);

                //WebApi服务 主要是和这个 //http://{0}:{1}/ HttpSelfHostServer
                StartWebApiService(host, port);
                //用来代替StartLocationAlarmService发送信息给unity，推送消息给客户端。项目：SignalRService
                StartSignalRService(host, port);

                CheckDatacaseWebApiUrl(host, port);

                //移动巡检信息获取客户端 轮询获取 WebApi 项目：WebApiClients
                StartGetInspectionTrack();

                //上海宝信项目对接视频行为告警 基于HttpListener 端口8736
                StartExtremeVisionListener();//端口要不同


                StartGetHistoryPositon();//将定位历史数据保存到缓存中

                ////定位告警回调服务（没用）, 基于 LocationCallbackService，在unity中不支持，无法使用。 项目：Location.Service 端口8734
                //StartLocationAlarmService();


                //StartReceiveAlarm();//设备告警对接服务 基于NSQ消息队列获取第三方告警信息 项目：WebNSQLib

                var EnableDevAlarmBuffer = ConfigurationHelper.GetBoolValue("EnableDevAlarmBuffer");


                Worker.Run(() =>
                {
                    CheckCardRole();//检查人员角色，发现有些定位卡没有绑定卡角色
                    try
                    {
                        Bll bll = Bll.NewBllNoRelation();
                        var count = bll.Areas.DbSet.Count(); //用于做数据迁移用，查询一下
                        if (count == 0)
                        {
                            Log.Error(LogTags.Server, bll.Areas.ErrorMessage);
                        }
                    }
                    catch (Exception e)
                    {
                        string msg = e.Message;
                        if (msg == "An error occurred while executing the command definition. See the inner exception for details.")
                        {
                            msg = e.InnerException.Message;
                        }
                        Log.Error(LogTags.Server, "数据迁移出错！！：" + msg);
                    }

                    if (EnableDevAlarmBuffer)
                    {
                        LocationService.RefreshDeviceAlarmBuffer(LogTags.Server);//实现加载全部设备告警到内存中
                    }
                }, null);

                if (filterErrorPointThread == null && !string.IsNullOrEmpty(AppContext.FilterMoreThanMaxSpeedTimer))
                {
                    filterErrorPointThread = new FilterErrorPointThread(AppContext.FilterMoreThanMaxSpeedTimer);
                    filterErrorPointThread.Start();
                }

                //StartAnchorScan();

                //StartCheckRepeatDev();
                StartDBBackupThread();
                //获取sis数据线程
                StartSaveDevMonitorNodeThread();
                //启动四会操作票刷新数据
                StartSaveOperationTicketSH();
                //保存实时测点数据
                StartSavePointHistory();
                //启动四会工作票刷新数据
                StartSaveWorkTicketSH();
                //启动四会签到告警产生及处理
                StartProduceSignInAlarm();
                //四会保存门禁历史信息
                StartCardActionsThread();
                //四会门禁卡信息
                StartSaveEntranceguardCards();
                //清除历史数据（需在线程中添加需要清除的表）
                DeleteHistoryData();
            }
            catch (Exception ex)
            {
                WriteLog(ex.ToString());
            }
        }

        private void CheckDatacaseWebApiUrl(string host, string port)
        {
            var apiUrl = AppContext.DatacaseWebApiUrl;
            if (!string.IsNullOrEmpty(apiUrl))
            {
                WriteLog("配置对接WebApi地址:" + apiUrl);
                
                if (apiUrl == "simulate")
                {
                    AppContext.DatacaseWebApiUrl = string.Format("{0}:{1}/datacase", host, port);
                }
                else
                {
                    var r = PingEx.Send(apiUrl);
                    if (r == false)
                    {
                        WriteLog("无法Ping通该地址:" + apiUrl);
                        AppContext.DatacaseWebApiUrl = string.Format("{0}:{1}/datacase", host, port);
                    }
                }
                WriteLog("最终对接WebApi地址:" + AppContext.DatacaseWebApiUrl);
            }
        }

        public void StartAnchorScan()
        {
            if (AppContext.AnchorScanInterval > 0)
            {
                Log.Info(LogTags.Server, string.Format("开启基站扫描,扫描间隔:{0}(s),一轮扫描次数:{1},告警发送模式:{2}", AppContext.AnchorScanInterval, AppContext.AnchorScanResetCount, AppContext.AnchorScanSendMode));

                if (IpHelper.GetLocalIpList().Contains(EngineClientSetting.LocalIp))
                {
                    anchorScanThread = new AnchorScanThread(AppContext.AnchorScanInterval);
                    anchorScanThread.Start();
                }
                else
                {
                    Log.Info(LogTags.Server, string.Format("配置的本地IP在本机电脑上不存在,不启动基站扫描.IP:{0}", EngineClientSetting.LocalIp));
                }
                
            }
        }

        AnchorScanThread anchorScanThread;

        FilterErrorPointThread filterErrorPointThread = null;

        public void StartPlayBackManage()
        {
            var EnabelNVS = ConfigurationHelper.GetBoolValue("EnabelNVS");
            if (EnabelNVS)
            {
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
                    WriteLog("找不到nginx启动文件:"+ nginx);
                }
            }
        }

        private Process nginxCmdProcess;

        CameraAlarmListener cameraAlarmListener;

        private void StartExtremeVisionListener()
        {
            try
            {
                bool enableVisionListener = ConfigurationHelper.GetBoolValue("EnableVisionListener");
                if (!enableVisionListener) return;
                string port = ConfigurationHelper.GetValue("ExtremeVisionListenerPort");
                string host = ConfigurationHelper.GetValue("ExtremeVisionListenerIP");
                int saveMode = ConfigurationHelper.GetIntValue("CameraAlarmPicSaveMode");
                string picDir = ConfigurationHelper.GetValue("CameraAlarmPicSaveDir");
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                DirectoryInfo dir = new DirectoryInfo(baseDir);
                picDir = dir.Root.FullName + picDir;
                AppSetting.CameraAlarmPicSaveMode = saveMode;
                AppSetting.CameraAlarmPicSaveDir = picDir;
                AppSetting.CameraAlarmKeepDay = ConfigurationHelper.GetIntValue("CameraAlarmKeepDay");
                AppSetting.DeleteAlarmKeepPictureFile = ConfigurationHelper.GetBoolValue("DeleteAlarmKeepPictureFile");
                if (cameraAlarmListener == null)
                {
                    string url = string.Format("http://{0}:{1}/listener/ExtremeVision/callback/", host, port);
                    cameraAlarmListener = new CameraAlarmListener(url, saveMode, picDir);
                    bool r = cameraAlarmListener.Start();
                    WriteLog("HttpListener: " + url + " [" + r + "]");
                }
            }catch(Exception e)
            {
                WriteLog("ServerManagerBox.StartExtremeVisionListener.Error: " + e.ToString());
            }
           
        }

        private void StopExtremeVisionListener()
        {
            WriteLog("StopExtremeVisionListener");
            if (cameraAlarmListener != null)
            {
                cameraAlarmListener.Stop();
                cameraAlarmListener = null;
            }
        }
        /// <summary>
        /// 数据库备份线程
        /// </summary>
        private DBBackupThread dbBackupThread;
        /// <summary>
        /// 启动数据库备份线程
        /// </summary>
        private void StartDBBackupThread()
        {
            bool openDBBackup = ConfigurationHelper.GetBoolValue("EnabelDBBackup");
            if (!openDBBackup) return;
            float backupDelayDays= ConfigurationHelper.GetFloatValue("BackupDelayDays");
            if (dbBackupThread == null)
            {
                dbBackupThread = new DBBackupThread(backupDelayDays);
                dbBackupThread.Start();
            }
        }

        private DevMonitorNodeThread nodeThread;
        /// <summary>
        /// 启动保存sis数据线程（实时更新）
        /// </summary>
        private void StartSaveDevMonitorNodeThread()
        {
            bool isSaveMonitor = ConfigurationHelper.GetBoolValue("EnableSaveDevMonitorNode");
            if (!isSaveMonitor) return;
            nodeThread = new DevMonitorNodeThread();
            nodeThread.Start();
        }
        private OperationTicketSHThread ticketSHThread;
        /// <summary>
        /// 启动四会操作票刷新数据
        /// </summary>
        private void StartSaveOperationTicketSH()
        {
            bool isSaveTicketSH = ConfigurationHelper.GetBoolValue("EnableOperationTicketSH");
            if (!isSaveTicketSH) return;
            string strIp = AppContext.DatacaseWebApiUrl;
            string port = AppContext.DatacaseWebApiPort;
            ticketSHThread = new OperationTicketSHThread(strIp,port);
            ticketSHThread.Start();
        }
        private WorkTicketSHThread workTicketSHThread;
        /// <summary>
        /// 启动四会工作票刷新数据
        /// </summary>
        private void StartSaveWorkTicketSH()
        {
            // EnableSaveWorkTicketSH
            bool isSaveWorkTicket = ConfigurationHelper.GetBoolValue("EnableSaveWorkTicketSH");
            if (!isSaveWorkTicket) return;
            string strIp = AppContext.DatacaseWebApiUrl;
            string port = AppContext.DatacaseWebApiPort;
            workTicketSHThread = new WorkTicketSHThread(strIp,port);
            workTicketSHThread.Start();
        }

        private SignInAlarmThread signInAlarmThread;
        /// <summary>
        /// 启动四会签到告警产生及处理
        /// </summary>
        private void StartProduceSignInAlarm()
        {
            bool isProduceAlarm = ConfigurationHelper.GetBoolValue("EnableProduceSignInAlarm");
            if (!isProduceAlarm) return;
            signInAlarmThread = new SignInAlarmThread();
            signInAlarmThread.Start();
        }

        private CardsActionsThread cardActionsThread;
        /// <summary>
        /// 启动四会保存门禁历史信息
        /// </summary>
        private void StartCardActionsThread()
        {
            bool isStart = ConfigurationHelper.GetBoolValue("EnableCardsActions");
            if (!isStart) return;
            string strIp = AppContext.DatacaseWebApiUrl;
            string port = AppContext.DatacaseWebApiPort;
            cardActionsThread = new CardsActionsThread(strIp,port);
            cardActionsThread.Start();
        }
        private EntranceguardCardsThread entranceguardCardsThread;
        /// <summary>
        /// 启动四会门禁卡信息
        /// </summary>
        private void StartSaveEntranceguardCards()
        {
            bool isStart = ConfigurationHelper.GetBoolValue("EnableCardsActions");  //跟获取门禁历史信息同步
            if (!isStart) return;
            string strIp = AppContext.DatacaseWebApiUrl;
            string port = AppContext.DatacaseWebApiPort;
            entranceguardCardsThread = new EntranceguardCardsThread(strIp,port);
            entranceguardCardsThread.Start();
        }

        private DeleteHistoryDataThread deleteHistoryDataThread;
        /// <summary>
        /// 清除历史数据
        /// </summary>
        private void DeleteHistoryData()
        {
            bool isStart = ConfigurationHelper.GetBoolValue("EnableDeleteHistoryData");
            if (!isStart) return;
            deleteHistoryDataThread = new DeleteHistoryDataThread();
            deleteHistoryDataThread.Start();
        }




        private PointHistoryThread pointHistoryThread;
        /// <summary>
        /// 保存实时测点数据（嘉明）
        /// </summary>
        private void StartSavePointHistory()
        {
            bool isSavePoint = ConfigurationHelper.GetBoolValue("EnableSavePoint");
            if (!isSavePoint) return;
            pointHistoryThread = new PointHistoryThread();
            pointHistoryThread.Start();
        }


       

        /// <summary>
        /// 停止数据库备份线程
        /// </summary>
        private void StopDBBackupThread()
        {
            if (dbBackupThread != null)
            {
                dbBackupThread.Abort();
                dbBackupThread = null;
            }
        }

        public void ShowTest(string str)
        {
            //textBox_U3DTEST.Text = str;
            //textBox_U3DTEST.AppendText( str);
        }

        private IDisposable SignalR;

        private void StartSignalRService(string host, string port)
        {
            //端口和主服务器(8733)一致的情况下，2D和3D无法连接SignalR服务器
            port = "8735";
            //string ServerURI = string.Format("http://{0}:{1}/", host,port);
            string ServerURI = string.Format("http://{0}:{1}/", "*", port);
            try
            {
                SignalR = WebApp.Start(ServerURI);
                WriteLog("SiganlR: " + ServerURI + "realtime");
            }
            catch (Exception ex)
            {
                //WriteToConsole("A server is already running at " + ServerURI);
                //this.Dispatcher.Invoke(() => ButtonStart.IsEnabled = true);
                //return;
                WriteLog(ex.ToString());
            }
        }

        private string serverLogs = "";

        private bool serverLogsChanged = false;

        private string clientLogs = "";

        private ConcurrentBag<LogInfo> clientLogInfos = new ConcurrentBag<LogInfo>();

        private bool clientLogsChanged = false;

        private void WriteLog(string txt)
        {
            //string log = string.Format("[{0}][{1}]", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.ffff"), txt);
            Location.BLL.Tool.Log.Info(LogTags.Server,txt);
        }

        private int MaxLength = 100000;
        private int MaxLength2 = 50000;

        private DispatcherTimer logTimer;

        private void ListenToLog(LogInfo info)
        {
            try
            {
                string tag = info.Tag;
                if (serverLogs.Length > MaxLength)
                {
                    serverLogs = serverLogs.Substring(0, MaxLength2);
                }

                if (clientLogs.Length > MaxLength)
                {
                    clientLogs = clientLogs.Substring(0, MaxLength2);
                }


                //string[] parts = log.Split('|');
                if (tag == LogTags.Server 
                    //|| tag == LogTags.ExtremeVision
                    )
                {
                    serverLogs = info.Log + "\n" + serverLogs;
                    serverLogsChanged = true;
                }
                else
                {
                    if (tag != LogTags.Engine && tag != LogTags.BaseData)
                    {
                        //clientLogs = log + "\n" + clientLogs;
                        clientLogInfos.Add(info);

                        clientLogsChanged = true;
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception.ToString());
            }
        }

        private void LogTimer_Tick(object sender, EventArgs e)
        {
            if (serverLogsChanged)
            {
                TbServerLog.Text = serverLogs;
                serverLogsChanged = false;
            }

            if (clientLogsChanged)
            {
                //TbClientLog.Text = clientLogs;

                var temp = new List<LogInfo>();
                lock (clientLogInfos)
                {
                    temp.AddRange(clientLogInfos);
                    clientLogInfos = new ConcurrentBag<LogInfo>();
                }
                while (temp.Count > 0)
                {
                    var log = temp.First();
                    temp.RemoveAt(0);
                    LogTabControl1.AddLog(log);
                }
                clientLogsChanged = false;
            }
        }

        HttpSelfHostServer httpHost;

        private void StartWebApiService(string host, string port)
        {
            string path = string.Format("http://{0}:{1}/", host, port);
            var config = new HttpSelfHostConfiguration(path);
            config.MaxBufferSize = int.MaxValue;
            config.MaxReceivedMessageSize = int.MaxValue;//收取数据，如果很大会出异常。设置不限制传输大小 wk 2020.5.19
            WebApiConfiguration.Configure(config);
            httpHost = new HttpSelfHostServer(config);
            httpHost.OpenAsync().Wait();

            WriteLog("WebApiService: " + path + "api");


        }

        private ServiceHost locationServiceHost;

        private void StartLocationService(string host, string port)
        {
            //1.配置方式启动服务
            //locationServiceHost = new ServiceHost(typeof(LocationService));
            //locationServiceHost.SetProxyDataContractResolver();
            //locationServiceHost.Open();

            //2.编程方式启动服务
            string url = string.Format("http://{0}:{1}/LocationService", host, port);
            Uri baseAddres = new Uri(url);
            locationServiceHost = new ServiceHost(typeof(LocationService), baseAddres);
            BasicHttpBinding httpBinding = new BasicHttpBinding();
            //httpBinding.MaxReceivedMessageSize = 2147483647;
            //httpBinding.MaxBufferPoolSize = 2147483647;

            //Binding httpBinding = new WSHttpBinding();
            locationServiceHost.AddServiceEndpoint(typeof(ILocationService), httpBinding, baseAddres);
            Binding binding = MetadataExchangeBindings.CreateMexHttpBinding();
            locationServiceHost.AddServiceEndpoint(typeof(IMetadataExchange), binding, "MEX");
            //开放数据交付终结点，客户端才能添加/更新服务引用。
            ServiceThrottlingBehavior throttle = locationServiceHost.Description.Behaviors.Find<ServiceThrottlingBehavior>();
            if (throttle == null)
            {
                throttle = new ServiceThrottlingBehavior();
                locationServiceHost.Description.Behaviors.Add(throttle);
            }
            throttle.MaxConcurrentCalls = ConfigurationHelper.GetIntValue("MaxConcurrentCalls");
            throttle.MaxConcurrentSessions = ConfigurationHelper.GetIntValue("MaxConcurrentSessions");
            throttle.MaxConcurrentInstances = ConfigurationHelper.GetIntValue("MaxConcurrentInstances");

            locationServiceHost.Open();
            WriteLog("LocationService: " + locationServiceHost.BaseAddresses[0]);
        }

        private WebServiceHost wcfApiHost;

        private void StartLocationServiceApi(string host, string port)
        {
            string path = string.Format("http://{0}:{1}/LocationService/api", host, port);
            //LocationService demoServices = new LocationService();
            wcfApiHost = new WebServiceHost(typeof(LocationService));
            WebHttpBinding httpBinding = new WebHttpBinding();
            wcfApiHost.AddServiceEndpoint(typeof(ITestService), httpBinding, new Uri(path + "/test"));//不能是相同的地址，不同的地址的话可以有多个。
            //wcfApiHost.AddServiceEndpoint(typeof(IDevService), httpBinding, new Uri(path));
            wcfApiHost.AddServiceEndpoint(typeof(IPhysicalTopologyService), httpBinding, new Uri(path));
            wcfApiHost.Open();
            WriteLog("LocationService: " + path);
        }

        private ServiceHost locationAlarmServiceHost;
        private void StartLocationAlarmService()
        {
            locationAlarmServiceHost = new ServiceHost(typeof(LocationCallbackService));
            locationAlarmServiceHost.SetProxyDataContractResolver();

            locationAlarmServiceHost.Open();

            WriteLog("LocationAlarmService: " + locationAlarmServiceHost.BaseAddresses[0]);
        }

        private void StopLocationAlarmService()
        {
            if (locationAlarmServiceHost!=null)
            {
                locationAlarmServiceHost.Close();
                WriteLog("StopLocationAlarmService: " + locationAlarmServiceHost.BaseAddresses[0]);
            }
        }


        InspectionTrackClient trackClient;//移动巡检

        private void StopGetInspectionTrack()
        {
            WriteLog("StopGetInspectionTrack");
            if (trackClient != null)
            {
                trackClient.Stop();
                trackClient = null;
            }
        }

        private void StartGetInspectionTrack()
        {
            bool EnableInspectionTrack= ConfigurationHelper.GetBoolValue("EnableInspectionTrack");

            if (EnableInspectionTrack && trackClient == null)
            {
                //Ping.
                string strIp = AppContext.DatacaseWebApiUrl;
                string port = AppContext.DatacaseWebApiPort;
                trackClient = new InspectionTrackClient(strIp,port);
                trackClient.ListGot += (TrackList) =>
                {
                    InspectionTrackHub.SendInspectionTracks(TrackList.ToTModel());//发送给客户端
                };
                trackClient.Start();

                WriteLog("StartGetInspectionTrack:"+ strIp);
            }
        }

        //private void GetInspectionTrack()
        //{
        //    string strIp = AppContext.DatacaseWebApiUrl;
        //    InspectionTrackClient client = new InspectionTrackClient(strIp);
        //    client.ListGot += (list) =>
        //    {
        //        SignalRService.Hubs.InspectionTrackHub.SendInspectionTracks(list.ToWcfModelList().ToArray());
        //    };
        //    client.Start();
        //}

        private Thread HPThread;
        private void StartGetHistoryPositon()
        {
            if (AppContext.EnableHistoryBuffer)
            {
                WriteLog("启动定位历史数据缓存");

                if (HPThread == null)
                {
                    var Phs = new LocationServices.Locations.Services.PosHistoryService();
                    HPThread = new Thread(Phs.GetHistoryPositonThread);
                    HPThread.IsBackground = true;
                    HPThread.Start();
                }
            }
        }
  
        private void StopGetHistoryPositon()
        {
            WriteLog("StopGetHistoryPositon");
            if (HPThread != null)
            {
                HPThread.Abort();
                HPThread = null;
            }
        }

        private void BtnClearServerLogs_OnClick(object sender, RoutedEventArgs e)
        {
            serverLogs = "";
            serverLogsChanged = true;
        }

        private void BtnClearClientLogs_OnClick(object sender, RoutedEventArgs e)
        {
            clientLogs = "";
            clientLogsChanged = true;
        }
    }
}
