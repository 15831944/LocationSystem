﻿using BLL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DbModel.Location.Work;
using SignalRService.Hubs;
using LocationServices.Converters;
using WebApiLib.Clients;
using Location.BLL.Tool;
using WPFClientControlLib;
using CommunicationClass.SihuiThermalPowerPlant.Models;
using DbModel.LocationHistory.Work;
using LocationServer.Tools;
using DbModel.Location.AreaAndDev;
using Location.TModel.Tools;
using WebApiLib;
using Newtonsoft.Json;
using DbModel.Tools;
using LocationServices.Locations.Services;
using TModel.Location.Work;
using DbModel.Converters;
using TModel.Tools;
using System.Data.Entity.Infrastructure;

namespace LocationServer.Windows
{
    /// <summary>
    /// InspectionChoiceWindows.xaml 的交互逻辑
    /// </summary>
    public partial class InspectionChoiceWindows : Window
    {
        public InspectionChoiceWindows()
        {
            InitializeComponent();
        }

        private List<DbModel.Location.Work.InspectionTrack> trackList;

        private void SendAllItem_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (trackList == null || trackList.Count == 0) return;
            DbModel.Location.Work.InspectionTrackList TrackList2 = new DbModel.Location.Work.InspectionTrackList();
            TrackList2.ReviseTrack = trackList;

            InspectionTrackHub.SendInspectionTracks(TrackList2.ToTModel());//发送给客户端
        }

        private void MenuGetAllInspectionTrack_Click(object sender, RoutedEventArgs e)
        {
            if (trackList == null)
            {
                Bll bll = Bll.Instance();
                trackList = bll.InspectionTracks.ToList();//从数据库取

                if (trackList == null || trackList.Count() == 0)
                {
                    return;
                }
            }

            SendAddByItem.ItemsSource = trackList;
            SendReviseByItem.ItemsSource = trackList;
            SendDeleteByItem.ItemsSource = trackList;
            SendAllItem.ItemsSource = trackList;
        }

        private void SendAddByItem_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DbModel.Location.Work.InspectionTrack it = SendAddByItem.SelectedItem as DbModel.Location.Work.InspectionTrack;
            if (it == null) return;
            DbModel.Location.Work.InspectionTrackList TrackList2 = new DbModel.Location.Work.InspectionTrackList();
            TrackList2.AddTrack.Add(it);

            InspectionTrackHub.SendInspectionTracks(TrackList2.ToTModel());//发送给客户端
        }

        private void SendReviseByItem_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DbModel.Location.Work.InspectionTrack it = SendReviseByItem.SelectedItem as DbModel.Location.Work.InspectionTrack;
            if (it == null) return;

            it.Name += "这是在测试";
            DbModel.Location.Work.InspectionTrackList TrackList2 = new DbModel.Location.Work.InspectionTrackList();
            TrackList2.ReviseTrack.Add(it);

            InspectionTrackHub.SendInspectionTracks(TrackList2.ToTModel());//发送给客户端
        }

        private void SendDeleteByItem_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DbModel.Location.Work.InspectionTrack it = SendDeleteByItem.SelectedItem as DbModel.Location.Work.InspectionTrack;
            if (it == null) return;
            DbModel.Location.Work.InspectionTrackList TrackList2 = new DbModel.Location.Work.InspectionTrackList();
            TrackList2.DeleteTrack.Add(it);

            InspectionTrackHub.SendInspectionTracks(TrackList2.ToTModel());//发送给客户端
        }

        InspectionTrackClient trackClient;//移动巡检

        private void MenuStartReceive_Click(object sender, RoutedEventArgs e)
        {
            trackClient.days = 365;
            trackClient.Start();

            Log.Info(LogTags.Inspection, "StartGetInspectionTrack:" + strIp);
        }

        LogTextBoxController controller;
        string strIp;
        string port;
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            strIp = AppContext.DatacaseWebApiUrl;
            port = AppContext.DatacaseWebApiPort;
            trackClient = new InspectionTrackClient(strIp,port);
            trackClient.ListGot += (TrackList) =>
            {
                InspectionTrackHub.SendInspectionTracks(TrackList.ToTModel());//发送给客户端
            };

            controller = new LogTextBoxController(TbLogs, LogTags.Inspection);
            var now = DateTime.Now;
            StartTime.SelectedDate = new DateTime(now.Year, 1, 1, 0, 0, 0);
            EndTime.SelectedDate = now;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if(trackClient!= null)
            {
                trackClient.Stop();
            }
        }

        //List<DbModel.Location.AreaAndDev.DevInfo> devList = null;

        //List<device> devList2 = new List<device>();

        public List<KKSCode> kksList = new List<KKSCode>();

        private void BtnGetList_Click(object sender, RoutedEventArgs e)
        {

            var start = (DateTime)StartTime.SelectedDate;
            var end = (DateTime)EndTime.SelectedDate;
            var list=trackClient.GetPatrolList(start, end);
            if (list == null)
            {
                MessageBox.Show("无数据！");
                list = new List<patrols>();
            }
            TbCount.Text = list.Count+"";
            DataGridPatrolList.ItemsSource = list;

            Bll bll = Bll.NewBllNoRelation();
            //devList = bll.DevInfos.ToList();

            //devList2 = trackClient.client.GetDeviceList("", "", "");

            kksList = bll.KKSCodes.ToList();
            //bll.DevMonitorNodes
        }


        private void BtnGetListAll_Click(object sender, RoutedEventArgs e)
        {
            var list = trackClient.GetPatrolList();
            TbCount.Text = list.Count + "";
            DataGridPatrolList.ItemsSource = list;

            Bll bll = Bll.NewBllNoRelation();
            ////devList = bll.DevInfos.ToList();

            ////devList2 = trackClient.client.GetDeviceList("", "", "");

            //kksList=bll.KKSCodes.ToList();

            Worker.Run(() =>
            {
                var newList = new List<DbModel.Location.Work.InspectionTrack>();
                var newHisList = new List<DbModel.LocationHistory.Work.InspectionTrackHistory>();

                long lNow = InspectionTrackClient.GetNowDateStamp();
                for (int i = 0; i < list.Count; i++)
                {
                    var item = list[i];
                    string progress1 = string.Format("Progress:{0}/{1}", i + 1, list.Count);
                    Log.Info(LogTags.Inspection, progress1);

                    int parentId = 0;
                    if (item.endTime > lNow)
                    {
                        var now = InspectionTrackClient.CreateInspectionTrack(item);
                        newList.Add(now);

                        bll.InspectionTracks.Add(now);//添加
                    }
                    else
                    {
                        var history = InspectionTrackClient.CreateInspectionTrackHistory(item);
                        newHisList.Add(history);

                        bll.InspectionTrackHistorys.Add(history);//历史轨迹
                    }

                    
                    
                }



            }, () =>
             {
                 MessageBox.Show("完成");
             });


        }

        patrols pd;

        private void DataGridPatrolList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            patrols p = DataGridPatrolList.SelectedItem as patrols;
            if (p == null) return;
            pd = trackClient.GetPatrolDetail(p.id);
            PatrolDetail.SelectedObject = pd;

            foreach (var item in pd.route)
            {
                if (item.deviceName == "")
                {
                    var d1 = kksList.FindAll(i => i.Code.Contains(item.kksCode));
                    if (d1.Count > 0)
                    {
                        item.deviceName = d1[0].Name;

                    }
                   
                }
            }

            DataGridRouteList.ItemsSource = pd.route;
        }

        private void DataGridRouteList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pd == null) return;
            checkpoints route = DataGridRouteList.SelectedItem as checkpoints;
            if (route == null) return;
            var checkResult=trackClient.Getcheckresults(pd.id,route.deviceId);
            foreach (var item in checkResult.checks)
            {
                var d1 = kksList.FindAll(i => i.Code.Contains(item.kksCode));
                if (d1.Count > 0)
                {
                    //item.deviceName = d1[0].Name;
                }
            }
            DataGridCheckList.ItemsSource = checkResult.checks;
        }

        private void DataGridCheckList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void BtnGetList2_Click(object sender, RoutedEventArgs e)
        {
            //Bll bll = Bll.Instance();
            Bll bll = Bll.NewBllNoRelation();
            trackList = bll.InspectionTracks.ToList();//从数据库取
            DataGridPatrolList2.ItemsSource = trackList;

            TbCount2.Text = trackList.Count+"";
            bll.Dispose();
        }

        private void DataGridPatrolList2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DbModel.Location.Work.InspectionTrack item = DataGridPatrolList2.SelectedItem as DbModel.Location.Work.InspectionTrack;
            if (item == null) return;
            Bll bll = Bll.NewBllNoRelation();
            var route = bll.PatrolPoints.FindAll(i => i.ParentId == item.Id);
            PatrolDetail2.SelectedObject = item;
            DataGridRouteList2.ItemsSource = route;
            bll.Dispose();
        }

        private void DataGridRouteList2_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            DbModel.Location.Work.PatrolPoint p = DataGridRouteList2.SelectedItem as DbModel.Location.Work.PatrolPoint;
            if (p == null) return;
            Bll bll = Bll.NewBllNoRelation();
            var checks = bll.PatrolPointItems.FindAll(i => i.ParentId == p.Id);

            DataGridCheckList2.ItemsSource = checks;
        }

        private void DataGridCheckList_SelectionChanged2(object sender, SelectionChangedEventArgs e)
        {
            
        }

        private void BtnGetList3_Click(object sender, RoutedEventArgs e)
        {
            GetHistoryList();
        }

        private void GetHistoryList()
        {
            Bll bll = Bll.NewBllNoRelation();
            var list = bll.InspectionTrackHistorys.ToList();//从数据库取
            DataGridPatrolList3.ItemsSource = list;

            TbCount3.Text = list.Count + "";
            bll.Dispose();
        }

        private void DataGridPatrolList3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            InspectionTrackHistory item = DataGridPatrolList3.SelectedItem as InspectionTrackHistory;
            if (item == null) return;
            Bll bll = Bll.NewBllNoRelation();
            var route = bll.PatrolPointHistorys.FindAll(i => i.ParentId == item.Id);
            PatrolDetail3.SelectedObject = item;
            DataGridRouteList3.ItemsSource = route;
            bll.Dispose();
        }

        private void DataGridRouteList3_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PatrolPointHistory p = DataGridRouteList3.SelectedItem as PatrolPointHistory;
            if (p == null) return;
            Bll bll = Bll.NewBllNoRelation();
            var checks = bll.PatrolPointItemHistorys.FindAll(i => i.ParentId == p.Id);

            DataGridCheckList3.ItemsSource = checks;
        }

        private void DataGridCheckList3_SelectionChanged2(object sender, SelectionChangedEventArgs e)
        {

        }

        List<PatrolPointItemHistory> removeList1 = new List<PatrolPointItemHistory>();
        List<PatrolPointHistory> removeList2 = new List<PatrolPointHistory>();
        List<InspectionTrackHistory> removeList3 = new List<InspectionTrackHistory>();

        private void BtnClearHistory_Click(object sender, RoutedEventArgs e)
        {
            Worker.Run(() =>
            {
                DateTime now = DateTime.Now.Date;

                Bll bll = Bll.Instance();
                Log.Info(LogTags.Inspection, "GetHistory Start");
                var list = bll.InspectionTrackHistorys.ToList();//从数据库取
                Log.Info(LogTags.Inspection, "GetHistory End:"+list.Count);

                for (int i = 0; i < list.Count; i++)
                {
                    Log.Info(LogTags.Inspection, string.Format("Progress {0}/{1}", (i + 1), list.Count));
                    var item = list[i];
                    var list2 = list.FindAll(j => j.Abutment_Id == item.Abutment_Id && j.Id != item.Id);
                    if (list2.Count > 0)
                    {
                        RemoveHistoryList(bll, list2);

                        foreach (var item2 in list2)
                        {
                            list.Remove(item2);
                        }
                    }
                }

                FlushRemove(bll);

            }, () =>
             {
                 GetHistoryList();
             });
        }

        private void RemoveHistoryList(Bll bll, List<InspectionTrackHistory> list2)
        {
            Log.Info(LogTags.Inspection, string.Format("Clear {0}", list2.Count));
            //foreach (var item2 in list2)
            for(int i=0;i<list2.Count;i++)
            {
                var item2 = list2[i];
                Log.Info(LogTags.Inspection, string.Format("RemoveHistoryList {0}/{1}", i+1,list2.Count));
                var routes = item2.Route;
                foreach (var route in routes)
                {
                    //var r1=bll.PatrolPointItemHistorys.RemoveList(route.Checks);
                    removeList1.AddRange(route.Checks);
                }
                if (removeList1.Count > 1000)
                {
                    FlushRemove(bll);
                }
                //var r2=bll.PatrolPointHistorys.RemoveList(routes);
                removeList2.AddRange(routes);
            }
            //var r3=bll.InspectionTrackHistorys.RemoveList(list2);
            if (removeList2.Count > 1000)
            {
                FlushRemove(bll);
            }
            removeList3.AddRange(list2);
            if (removeList3.Count > 1000)
            {
                FlushRemove(bll);
            }
        }

        private void FlushRemove(Bll bll)
        {
            {
                var r1 = bll.PatrolPointItemHistorys.RemoveList(removeList1);
                removeList1.Clear();
                var r2 = bll.PatrolPointHistorys.RemoveList(removeList2);
                removeList2.Clear();
                var r3 = bll.InspectionTrackHistorys.RemoveList(removeList3);
                removeList3.Clear();
            }
        }

        private void MenuClear_Click(object sender, RoutedEventArgs e)
        {
            Worker.Run(() =>
            {
                Log.Info(LogTags.Inspection, "Clear Start");
                Bll bll = Bll.NewBllNoRelation();

                Log.Info(LogTags.Inspection, "PatrolPointItemHistorys");
                bll.PatrolPointItemHistorys.Clear();
                Log.Info(LogTags.Inspection, "PatrolPointHistorys");
                bll.PatrolPointHistorys.Clear();
                Log.Info(LogTags.Inspection, "InspectionTrackHistorys");
                bll.InspectionTrackHistorys.Clear();

                Log.Info(LogTags.Inspection, "PatrolPointItems");
                bll.PatrolPointItems.Clear();
                Log.Info(LogTags.Inspection, "PatrolPoints");
                bll.PatrolPoints.Clear();
                Log.Info(LogTags.Inspection, "InspectionTracks");
                bll.InspectionTracks.Clear();

                trackClient.isAllNew = true;

                Log.Info(LogTags.Inspection, "Clear End");
            }, () =>
             {
                 MessageBox.Show("完成");
             });
        }

        private void MenuClearRecent_Click(object sender, RoutedEventArgs e)
        {

            Worker.Run(() =>
            {

                Log.Info(LogTags.Inspection, "ClearRecent Start");

                Bll bll = Bll.NewBllNoRelation();

                DateTime now = DateTime.Now.Date;
                DateTime start = now.AddDays(-5);
                long startStamp = TimeConvert.ToStamp(start);

                var list=bll.InspectionTrackHistorys.FindAll(i => i.CreateTime >= startStamp);
                Log.Info(LogTags.Inspection, "InspectionTrackHistorys:"+ list.Count);
                RemoveHistoryList(bll, list);
                FlushRemove(bll);

                Log.Info(LogTags.Inspection, "ClearRecent End");
            }, () =>
            {
                MessageBox.Show("完成");
            });
        }

        private void MenuZhongShan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //string result = WebApiHelper.GetString("http://127.0.0.1:8080/zhongshan/login");
                // Log.Info(result);


                //List<sis> list=WebApiHelper.GetEntity<List<sis>>("http://127.0.0.1:8080/zhongshan/login/Sis");
                //sis ss = list[0];
                //检查项列表
                // List<results> resultList = WebApiHelper.GetEntity<List<results>>("http://127.0.0.1:8080/zhongshan/Results/list");
                //results result = resultList[0];

                //两票列表
                //List<tickets> ticketsList = WebApiHelper.GetEntity<List<tickets>>("http://127.0.0.1:8080/zhongshan/Tickets/list");
                //int count = ticketsList.Count;
                //if (count > 0)
                //{
                //    tickets ticket = ticketsList[0];
                //}
                //Sis列表
                //string kks = "kks001";
                // List<sis>  sisList= WebApiHelper.GetEntity<List<sis>>("http://127.0.0.1:8080/zhongshan/Sis/list/kks/"+kks);
                //if (sisList.Count > 0)
                //{
                //    sis sis = sisList[0];
                //}

                //两票数据
                string result = WebApiHelper.GetString("http://120.25.195.214:18000/api/tickets?type=1");

                JsonSerializerSettings setting = new JsonSerializerSettings();
                setting.NullValueHandling = NullValueHandling.Ignore;
                Message<TwoTickets> message = JsonConvert.DeserializeObject<Message<TwoTickets>>(result,setting);
                int total = message.total;
                string msg = message.msg;
                List<TwoTickets> list = message.data;


                //循环获取DetailsSet
                if (list != null)
                {
                    foreach (TwoTickets ticket in list)
                    {
                            string results = ticket.detail;
                            Details details = JsonConvert.DeserializeObject<Details>(results, setting);
                            ticket.detail = "";
                            DetailsSet detalsSet = new DetailsSet();
                            detalsSet.optTicket = details.optTicket;
                            List<LinesSet> lineSet = new List<LinesSet>();
                            List<LinesGet> lineList = details.lines;
                           
                            //循环，给LineSet赋值
                            if (lineList != null)
                            {
                                foreach (LinesGet line in lineList)
                                {
                                    LinesSet setline = new LinesSet();
                                    setline.name = line.name;
                                    List<Dictionary<string, string>> dicList = line.lineContentList;
                                    List<LineContent> contentList = new List<LineContent>();
                                        if (dicList != null)
                                        {
                                            foreach (Dictionary<string, string> dic in dicList)
                                            {
                                                LineContent linecontent = new LineContent();
                                                List<KeyValue> keyList = new List<KeyValue>();
                                                if(dic!=null)
                                                   { 
                                                foreach (KeyValuePair<string, string> kv in dic)
                                                {
                                                    KeyValue keyValue = new KeyValue();
                                                    keyValue.key = kv.Key;
                                                    keyValue.value = kv.Value;
                                                    keyList.Add(keyValue);
                                                }
                                                linecontent.Content = keyList;
                                                contentList.Add(linecontent);
                                                    }
                                            }
                                            setline.lineContentList = contentList;
                                            lineSet.Add(setline);
                                        }
                                }
                                detalsSet.lines = lineSet;
                                ticket.detailsSet = detalsSet;
                            }
                        
                    }

                    Message<TwoTickets> aa = message;
                     var xml = XmlSerializeHelper.GetXmlText(aa);

                }
            }
            catch (Exception ex)
            {
                //错误
                Log.Error(ex.ToString());
            }

        }

        private void MenuOPC_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //OPCClient1.GetOPCServers();
                //  //Kepware.KEPServerEX.V6
                //  OPCClient1.ConnectServer("");

                //  //OPCClient1.WriteValue("Simulator.Test.k0", "121");

                //  OPCClient1.ReadValue("Simulator.Test.k0",true);
                //  object aa=OPCClient1.ReadValue("Simulator.Test.k0");
                string aa = "";
                DevService devservice = new DevService();
                Log.Info("a");
                // Dev_Monitor dev = dev = devservice.GetDevMonitorInfoByKKS("3号机组凝汽器", false);
                //string opcServerIp = AppContext.OPCServerIP;
                //OPCReadAuto opc = new OPCReadAuto(opcServerIp);
                //string tagNameValue = opc.getOPC("30PAB13AA001CD");
                Bll db = Bll.NewBllNoRelation();
                List<DbModel.Location.AreaAndDev.DevMonitorNode> nodeList = db.DevMonitorNodes.ToList();
                string tags = "";
                //foreach (DbModel.Location.AreaAndDev.DevMonitorNode node in nodeList)
                //{
                //    tags += node.TagName + ",";
                //}
                tags = string.Format("NCS_34_AI67,NCS_34_AI71,2NCS_AI190,2NCS_AI198,2NCS_AI230,2NCS_AI238,2NCS_AI266,2NCS_AI274");
                tags = tags.Substring(0, tags.Length - 1);
                // string result = WebApiHelper.GetString("http://10.146.33.9:20080/MIS/GetRtMonTagValues?tagNames=COM-LA-006");
                // List<SisData> sisList = WebApiHelper.GetEntity<List<SisData>>("http://10.146.33.9:20080/MIS/GetRtMonTagInfosByNames?tagNames="+tags);

                
                    DoorClickService drLockService = new DoorClickService();
                //List<DoorClick> list = db.DoorClicks.ToList();
                // List<DoorClick> list = drLockService.GetListByJson();
                // bool result=db.DoorClicks.AddRange(list);
                DateTime startTime = new DateTime(2020,5,16,12,00,00);
                DateTime endTime = new DateTime(2020, 5, 20, 12, 00, 00);
                string[] personids = { "a0d1304a3aa04cec86132e5a8f15d3e9", "576fa82f4aae429d8463046a34453ed5" };
                string[] doorindexCodes = null;
               //  List<DbModel.LocationHistory.Door.DoorClick> list = drLockService.GetListByCondition(startTime,endTime,"1","1000", "198914",personids,doorindexCodes);
                    Log.Info("b");

                //OPCReadAuto opc = new OPCReadAuto(opcServerIp);
                // if (opc.IsConnected == true)
                // {
                //     aa = opc.GetOpcValueOne("30MAG10CT301");
                // }


            }
            catch (Exception ex)
            {
                Log.Info("连接opc:"+ex.ToString());
            }
        }

        private void TicketIn_Click(object sender, RoutedEventArgs e)
        {
            TicketsService service = new TicketsService();
            //List<TwoTickets> list = service.ListAll();
            //List<OperationTicketSH> dbList = new List<OperationTicketSH>();
            //foreach (TwoTickets ticket in list)
            //{
            //    OperationTicketSH dbTable = ticket.ToDbModel();
            //    dbList.Add(dbTable);
            //}
            Bll bll = Bll.NewBllNoRelation();
            //List<OperationTicketHistorySH> OperationHisList = bll.OperationTicketHistorySHs.ToList();
            //List<OperationTicketHistorySH> newList = new List<OperationTicketHistorySH>();
            //OperationHisList.Sort((a, b) => { return (int)a.Abutment_Id - (int)b.Abutment_Id; });
            //int id = (int)OperationHisList.Last().Abutment_Id + 1;
            //for (int i = 0; i < OperationHisList.Count; i++)
            //{
            //    OperationTicketHistorySH ticket = OperationHisList[i];
            //    ticket.Abutment_Id = id;
            //    newList.Add(ticket);
            //    id = id + 1;
            //}
            //bool result = bll.OperationTicketHistorySHs.AddRange(newList);
            //    string sql = "delete from  aa where id  in(99,100)";
            //  string result= bll.WorkTicketHistorySHes.AddorEditBySql(sql);
            DateTime now = DateTime.Now;
            long a= TimeConvert.ToStamp(now);
         
        }
    }

    public class person
    {
        public int id { get; set; }
        public string Name { get; set; }
    }
}
