﻿using System;
using System.Collections.Generic;
using System.IO;
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
using BLL;
using DbModel.Location.AreaAndDev;
using Location.TModel.Location.AreaAndDev;
using LocationServer.Windows;
using LocationServices.Converters;
using LocationServices.Locations.Services;

using DbAreaEntity= DbModel.Location.AreaAndDev.Area;
using AreaEntity = Location.TModel.Location.AreaAndDev.PhysicalTopology;
//using DevEntity=DbModel.Location.AreaAndDev.DevInfo;
using DevEntity = Location.TModel.Location.AreaAndDev.DevInfo;
using PersonEntity = Location.TModel.Location.Person.Personnel;
using WPFClientControlLib.Extensions;
using System.Windows.Threading;
using LocationWCFServer;
using LocationServices.Tools;
using SignalRService.Hubs;
using DbModel.Tools;
using LocationServer.Tools;
using LocationServer.Models.EngineTool;
using DbModel.Location.Settings;
using TArchor = TModel.Location.AreaAndDev.Archor;
using Bound = Location.TModel.Location.AreaAndDev.Bound;
using LocationServer.Windows.Simple;
using Point = Location.TModel.Location.AreaAndDev.Point;
using Location.IModel;
using ArchorUDPTool;
using System.Diagnostics;
using System.Data;

namespace LocationServer
{
    /// <summary>
    /// Interaction logic for AreaCanvas.xaml
    /// </summary>
    public partial class AreaCanvasWindow : Window
    {
        private Bll bll;

        public AreaCanvasWindow()
        {
            InitializeComponent();

            InitService();
        }

        TArchor[] _archors;


        //public AreaCanvasWindow(params Archor[] archors)
        //{
        //    InitializeComponent();
        //    bll = AppContext.GetLocationBll();
        //    _archors = archors;
        //}

        public AreaCanvasWindow(params int[] archorsIds)
        {
            InitializeComponent();

            InitService();

            var ids = archorsIds.ToList();
            //_archors = bll.Archors.FindAll(i => ids.Contains(i.Id)).ToArray();
            _archors = archorService.GetList().Where(i => ids.Contains(i.Id)).ToArray();
        }

        public void LocateByIp(List<string> ips)
        {
            //_archors = bll.Archors.FindAll(i => ids.Contains(i.Id)).ToArray();
            _archors = archorService.GetList().Where(i => ips.Contains(i.Ip)).ToArray();
        }

        private AreaService areaService;
        private DepartmentService depService;
        private DeviceService devService;
        private ArchorService archorService;


        private void InitService()
        {
            bll = Bll.NewBllNoRelation();
            areaService = new AreaService(bll);
            depService = new DepartmentService(bll);
            devService = new DeviceService(bll);
            archorService = new ArchorService(bll);
        }

        private void AreaCanvasWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            

            InitAreaCanvas();
            LoadData();
            //StartPersonTimer();
            ArchorHelper.LoadArchoDevInfo();  //载入基站信息，用于ID和IP的自动匹配
        }

        private void PersonTimer_Tick(object sender, EventArgs e)
        {
            ShowPersons();
            LoadPersonTree();
        }

        DispatcherTimer personTimer;

        void StartPersonTimer()
        {
            if (personTimer == null)
            {
                personTimer = new DispatcherTimer();
                personTimer.Interval = TimeSpan.FromMilliseconds(250);
                personTimer.Tick += PersonTimer_Tick;
                personTimer.Start();
            }
        }

        void StopPersonTimer()
        {
            if (personTimer != null)
            {
                personTimer.Stop();
                personTimer = null;
            }
        }

        void InitAreaCanvas()
        {
            AreaCanvas1.Init();
            AreaCanvas1.udpArchorList = ArchorManager.LoadArchorListResult();
            ContextMenu devContextMenu = new ContextMenu();
            devContextMenu.AddMenu("设置设备", (tag) =>
            {
                SetDevInfo(AreaCanvas1.SelectedDev, AreaCanvas1.SelectedDev.Tag as DevEntity);
            });
            devContextMenu.AddMenu("删除设备", (tag) =>
            {
                var dev = AreaCanvas1.SelectedDev.Tag as DevEntity;
                RemoveDev(dev);
            });
            devContextMenu.AddMenu("复制设备", (tag) =>
            {
                var dev = AreaCanvas1.SelectedDev.Tag as DevEntity;
                dev.Pos.PosX += 5;
                dev.Pos.PosY += 5;
                dev.Name += " Copy";
                dev.Code = "";
                //dev.Ip = "";
                var dev2=devService.Post(dev);

                var archor = dev.DevDetail as TArchor;
                if (archor != null)
                {
                    archor.X += 5;
                    archor.Y += 5;
                    archor.Name += " Copy";
                    archor.Code = "";
                    archor.Ip = "";
                    archor.DevInfoId = dev2.Id;
                    var archorNew = archorService.Post(archor);
                    archorNew.Code = "Code_" + archorNew.Id;
                    archorService.Put(archorNew);
                }
                LoadData();
            });
            AreaCanvas1.DevContextMenu = devContextMenu;
            ContextMenu areaContextMenu = new ContextMenu();
            areaContextMenu.AddMenu("设置区域", (tag) =>
            {
                var area = AreaCanvas1.SelectedArea;
                ShowAreaInfo(area);
            });
            areaContextMenu.AddMenu("删除区域", (tag) =>
            {
                var area = AreaCanvas1.SelectedArea;
                RemoveArea(area);
            });
            areaContextMenu.AddMenu("删除区域内设备", (tag) =>
            {
                var area = AreaCanvas1.SelectedArea;
                RemoveAreaDevs(area);
            });
            areaContextMenu.AddMenu("添加测量点", (tag) =>
            {
                var area = AreaCanvas1.SelectedArea;
                TrackPointWindow win = new TrackPointWindow();
                if(win.Show(area.Id, AreaCanvas1.SelectedPoint2) == true)
                {
                    var newDev = win._tp;
                    area.AddLeafNode(newDev.ToTModel());
                    AreaCanvas1.Refresh();
                    var newDevRect=AreaCanvas1.GetDev(newDev.Id);
                    Window wnd=SetDevInfo(newDevRect, newDevRect.Tag as DevEntity);
                    if(wnd is RoomArchorSettingWindow)
                    {
                        (wnd as RoomArchorSettingWindow).SaveInfo(false);
                        //wnd.Close();
                    }
                }
                //RoomArchorSettingWindow win = new RoomArchorSettingWindow();
                //var dev = topoTree.SelectedObject as DevEntity;
                //SetDevInfo(null, null);
            });
            AreaCanvas1.AreaContextMenu = areaContextMenu;

            archorSettings = bll.ArchorSettings.ToList();
            AreaCanvas1.GetSettingFunc = (dev) =>
            {
                object detail = dev.DevDetail;
                if (detail is TArchor)
                {
                    var archor = detail as TArchor;
                    return archorSettings.Find(i=>i.Code== archor.Code);
                }
                return null;
            };
        }

        private bool RemoveDev(DevEntity dev)
        {
            if (MessageBox.Show("确认删除设备:" + dev.Name + "?", "警告", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                var r = devService.Delete(dev.Id + "");
                if (r == null)
                {
                    MessageBox.Show("删除失败");
                    return false;
                }
                var area = AreaCanvas1.CurrentArea;
                area.RemoveLeafNode(dev.Id);
                AreaCanvas1.RemoveDev(dev.Id);

                var topoTree = ResourceTreeView1.TopoTree;
                topoTree.RemoveDevNode(dev.Id);
                return true;
                //topoTree.RefreshNode(dev.ParentId);
                //ResourceTreeView1.TopoTree.RemoveCurrentNode();
            }
            else
            {
                return false;
            }
        }

        private void ShowAreaInfo(PhysicalTopology area)
        {
            var win = new AreaInfoWindow();
            win.Show();
            if (area.Children == null)
            {
                area.Children = areaService.GetListByPid(area.Id + "");
            }
            win.ShowInfo(area);
            win.AreaModified += Win_AreaModified;
        }

        private void Win_AreaModified(AreaEntity obj)
        {
            LoadData();
            if (obj.LeafNodes != null)
            {
                foreach (DevEntity leaf in obj.LeafNodes)
                {
                    if (leaf.TypeName == "基站")
                    {
                        var newDevRect = AreaCanvas1.GetDev(leaf.Id);
                        Window wnd = SetDevInfo(newDevRect, newDevRect.Tag as DevEntity, false);
                        if (wnd is RoomArchorSettingWindow)
                        {
                            (wnd as RoomArchorSettingWindow).SaveInfo(false);
                        }
                        if (wnd is ParkArchorSettingWindow)
                        {
                            (wnd as ParkArchorSettingWindow).SaveInfo(false);
                        }
                        //wnd.Close();
                    }
                }
            }
        }

        List<ArchorSetting> archorSettings;

        private void RemoveAreaDevs(AreaEntity area)
        {
            if (area.LeafNodes == null)
            {
                area.LeafNodes = devService.GetListByPid(area.Id + "");
            }

            List<DevEntity> devs = new List<DevEntity>();
            //List<DevEntity> devs2 = new List<DevEntity>();
            if (area.Type == AreaTypes.机房)
            {
                var parent = area.Parent;
                if (parent == null)
                {
                    parent = areaService.GetParent(area.Id+"");
                }
                var devs2 = parent.LeafNodes;
                if (devs2 == null)
                {
                    devs2 = devService.GetListByPid(parent.Id + "");
                }
                foreach (var item in devs2)
                {
                    if (area.InitBound.Contains(item.Pos.PosX, item.Pos.PosZ))
                    {
                        devs.Add(item);
                    }
                }
            }
            else if (area.Type == AreaTypes.楼层)
            {
                var devs2 = area.LeafNodes;
                if (devs2 == null)
                {
                    devs2 = devService.GetListByPid(area.Id + "");
                }
                foreach (var item in devs2)
                {
                    if (area.InitBound.Contains(item.Pos.PosX, item.Pos.PosZ))
                    {
                        devs.Add(item);
                    }
                }
            }

            //var devs=devService.GetListByBound(area.InitBound);
            AreaCanvas1.SelectDevs(devs);
            if (MessageBox.Show(string.Format("确认删除区域'{0}'内{1}个设备?",area.Name,devs.Count), "警告", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
            {
                foreach (var dev in devs)
                {
                    var r = devService.Delete(dev.Id + "");
                    if (r == null)
                    {
                        MessageBox.Show("删除失败:"+dev.Name);
                    }
                    AreaCanvas1.RemoveDev(dev.Id);
                }
            }
            else
            {
                AreaCanvas1.ClearSelect();
            }
        }

        private void RemoveArea(AreaEntity area)
        {
            if (area.Children == null)
            {
                area.Children = areaService.GetListByPid(area.Id + "");
            }
            if (area.Children != null && area.Children.Count > 0)
            {
                MessageBox.Show("存在子区域，不能删除！");
            }
            if (area.LeafNodes == null)
            {
                area.LeafNodes = devService.GetListByPid(area.Id + "");
            }
            if (area.LeafNodes != null && area.LeafNodes.Count > 0)
            {
                MessageBox.Show("存在子设备，不能删除！");
            }
            if(MessageBox.Show("确认删除区域:"+area.Name+"?","警告",MessageBoxButton.YesNo)== MessageBoxResult.Yes)
            {
                var r = areaService.Delete(area.Id + "");
                if (r == null)
                {
                    MessageBox.Show("删除失败");
                }
                else
                {
                    AreaCanvas1.RemoveArea(area.Id);
                }
            }
        }

        public void LoadData()
        {
            FilterAreaTree = null;
            LoadAreaTree();
            LoadDepTree();
            LoadPersonTree();
            //LoadAreaList();
        }

        private void LoadAreaList()
        {
                AreaListBox1.LoadData(areaService.GetList());
        }

        private Func<Area, bool> FilterAreaTree;

        private void LoadAreaTree()
        {
            DbAreaEntity tree1 = null;
            if (FilterAreaTree == null)
            {
                tree1 = bll.GetAreaTree(true, null, true);
            }
            else
            {
                tree1 = bll.GetAreaTreeEx(false, null, FilterAreaTree);
            }
            

            var tree = tree1.ToTModel();
            if (tree == null) return;
            var devList = tree.GetAllDev();
            var archorList = archorService.GetList();
            foreach (var dev in devList)
            {
                if (archorList != null)
                    dev.DevDetail = archorList.FirstOrDefault(i => i.DevInfoId == dev.Id);
            }
            var topoTree = ResourceTreeView1.TopoTree;
            var iId = topoTree.SelectedObject as IId;
            InitTopoTreeAreaMenu(topoTree);
            InitTopoTreeDevMenu(topoTree);
            topoTree.LoadDataEx<AreaEntity, DevEntity>(tree);
            topoTree.Tree.SelectedItemChanged += Tree_SelectedItemChanged;
            topoTree.ExpandLevel(2);

            if (_archors != null)
            {
                List<int> ids = new List<int>();
                foreach (var archor in _archors)
                {
                    topoTree.SelectNode(typeof(AreaEntity),(int)archor.ParentId, archor.DevInfoId);
                    ids.Add(archor.DevInfoId);
                }
                AreaCanvas1.SelectDevsById(ids);//
                //AreaCanvas1.SelectDevsByIdEx(ids);//把其他的都删除了
            }
            else
            {
                if (iId == null)
                {
                    topoTree.SelectFirst();
                }
                else
                {
                    topoTree.SelectNodeById(iId);
                }
            }
        }

        private void InitTopoTreeDevMenu(WPFClientControlLib.TopoTreeView topoTree)
        {
            topoTree.DevMenu = new ContextMenu();
            topoTree.DevMenu.AddMenu("设置设备", (tag) =>
            {
                var dev = topoTree.SelectedObject as DevEntity;
                SetDevInfo(null, dev);
            });
            topoTree.DevMenu.AddMenu("基站配置", (tag) =>
            {
                //var dev = topoTree.SelectedObject as DevEntity;
                //SetDevInfo(null, dev);
            });
            topoTree.DevMenu.AddMenu("删除设备", (tag) =>
            {
                var dev = topoTree.SelectedObject as DevEntity;
                RemoveDev(dev);
            });
        }

        private void InitTopoTreeAreaMenu(WPFClientControlLib.TopoTreeView topoTree)
        {
            topoTree.AreaMenu = new ContextMenu();
            topoTree.AreaMenu.AddMenu("添加区域", (obj) =>
            {
                var area = topoTree.SelectedObject as AreaEntity;
                var win = new NewAreaWindow(area,0);
                win.ShowPointEvent += (x, y) =>
                {
                    AreaCanvas1.ShowPoint(x, y);
                };
                if (win.ShowDialog() == true)
                {
                    var newArea = win.NewArea;
                    area.AddChild(newArea.ToTModel());
                    topoTree.RefreshCurrentNode<AreaEntity, DevEntity>(area);
                    AreaCanvas1.Refresh();
                }
            });

            topoTree.AreaMenu.AddMenu("添加区域(柱子)", (obj) =>
            {
                var area = topoTree.SelectedObject as AreaEntity;
                var win = new NewAreaWindow(area,1);
                win.ShowPointEvent += (x, y) =>
                {
                    AreaCanvas1.ShowPoint(x, y);
                };
                if (win.ShowDialog() == true)
                {
                    var newArea = win.NewArea;
                    area.AddChild(newArea.ToTModel());
                    topoTree.RefreshCurrentNode<AreaEntity, DevEntity>(area);
                    AreaCanvas1.Refresh();
                }
            });
            topoTree.AreaMenu.AddMenu("设置区域", (tag) =>
            {
                var area = topoTree.SelectedObject as AreaEntity;
                ShowAreaInfo(area);
            });
            topoTree.AreaMenu.AddMenu("删除区域", (tag) =>
            {
                var area = topoTree.SelectedObject as AreaEntity;
                RemoveArea(area);
            });
            topoTree.AreaMenu.AddMenu("添加基站", (tag) =>
            {
                var area = topoTree.SelectedObject as AreaEntity;

                var archor = new TArchor();
                archor.X = 10;
                archor.Y = 10;
                archor.Name = "NewArchor";
                archor.Code = "";
                archor.Ip = "";
                archor.ParentId = area.Id;
                var archorNew = archorService.Post(archor);
                archorNew.Code = "Code_" + archorNew.Id;
                archorService.Put(archorNew);

                area.AddLeaf(archorNew.DevInfo);

                topoTree.RefreshCurrentNode<AreaEntity, DevEntity>(area);
                AreaCanvas1.Refresh();
            });
            //topoTree.AreaMenu.AddMenu("调整子区域坐标", (tag) =>
            //{
            //    var area = topoTree.SelectedObject as AreaEntity;
            //    var bound = area.InitBound;
            //    var x = bound.MinX;
            //    var y = bound.MinY;
            //    var boundList = new List<Bound>();
            //    foreach (var subArea in area.Children)
            //    {
            //        var subBound = subArea.InitBound;
            //        if (subBound != null)
            //        {
            //            var points = subBound.Points;
            //            if (points != null)
            //            {
            //                foreach (var point in points)
            //                {
            //                    point.X -= x;
            //                    point.Y -= y;
            //                }
            //                bll.Points.EditRange(points.ToDbModel());
            //            }
            //            subBound.SetMinMaxXY();
            //            boundList.Add(subBound);
            //        }
            //    }
            //    bll.Bounds.EditRange(boundList.ToDbModel());
            //});

            topoTree.AreaMenu.AddMenu("复制结构", (tag) =>
            {
                var area = topoTree.SelectedObject as AreaEntity;

                var area1 = areaService.GetEntity("240", true);

                area.InitBound.SetMinMaxXY();
                bll.Bounds.Edit(area.InitBound.ToDbModel());

                var children = area1.Children.CloneObjectList().ToList();

                var newBounds = new List<Bound>();
                foreach (var item in children)
                {
                    if (item.InitBound.Points == null)
                    {
                        item.InitBound.Points = bll.Points.FindAll(i => i.BoundId == item.Id).ToTModel();
                    }
                    newBounds.Add(item.InitBound);
                }

                var dbBounds = newBounds.ToDbModel();
                bll.Bounds.AddRange(dbBounds);
                for (int i = 0; i < children.Count; i++)
                {
                    AreaEntity item = children[i];
                    item.ParentId = area.Id;
                    item.InitBoundId = dbBounds[i].Id;
                }
                foreach (var item in dbBounds)
                {

                    var points = item.Points.CloneObjectList();
                    foreach (var point in points)
                    {
                        point.BoundId = item.Id;
                    }
                    bll.Points.AddRange(points);
                }
                var dbChildren = children.ToDbModel();
                bll.Areas.AddRange(dbChildren);

                var devs = area1.LeafNodes.CloneObjectList().ToList(); ;
                foreach (var item in devs)
                {
                    item.ParentId = area.Id;
                    item.Code = "";
                    item.IP = "";
                    item.DevDetail = bll.Archors.Find(i => i.DevInfoId == item.Id).ToTModel();
                }
                var dbDevs = devs.ToDbModel();
                bll.DevInfos.AddRange(dbDevs);


                var archors = new List<TModel.Location.AreaAndDev.Archor>();
                for (int i = 0; i < devs.Count; i++)
                {
                    DevEntity dev = devs[i];
                    var archor = dev.DevDetail as TModel.Location.AreaAndDev.Archor;
                    archor.Code = "";
                    archor.Ip = "";
                    archor.ParentId = area.Id;
                    archor.DevInfoId = dbDevs[i].Id;
                    archors.Add(archor);
                }
                bll.Archors.AddRange(archors.ToDbModel());
                topoTree.RefreshCurrentNode<AreaEntity, DevEntity>(area);
                AreaCanvas1.Refresh();
            });
        }

        private void LoadDepTree()
        {
            var tree = depService.GetTree(1);
            var depTree = ResourceTreeView1.DepTree;
            depTree.LoadData(tree);
            depTree.ExpandLevel(2);
        }

        private void LoadPersonTree()
        {
            var tree = areaService.GetBasicTree(5);
            if (tree == null) return;
            var depTree = ResourceTreeView1.PersonTree;
            depTree.LoadData(tree);
            depTree.ExpandLevel(4);

            var persons=tree.GetAllPerson();
        }

        private AreaEntity currentArea;

        private void Tree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            currentArea = ResourceTreeView1.TopoTree.SelectedObject as AreaEntity;
            if (currentArea != null)
            {
                Bll bll = new Bll();
                var switchAreas = bll.bus_anchor_switch_area.ToList();

                AreaCanvas1.ShowDev = true;
                AreaCanvas1.ShowArea(currentArea, switchAreas);

                AreaListBox1.LoadData(currentArea.Children);
                DeviceListBox1.LoadData(currentArea.LeafNodes);

                ShowPersons();

                ArchorListExportControl1.Clear();
                TabControl1.SelectionChanged -= TabControl1_OnSelectionChanged;
                TabControl1.SelectionChanged += TabControl1_OnSelectionChanged;

                if (TabControl1.SelectedIndex == 2)
                {
                    ArchorListExportControl1.LoadData(currentArea.Id);
                    TabControl1.SelectionChanged -= TabControl1_OnSelectionChanged;
                }

                ShowRoomPictures(currentArea);
            }
            else
            {
                var dev= ResourceTreeView1.TopoTree.SelectedObject as DevEntity;
                if (dev != null)
                {
                    AreaCanvas1.SelectDevById(dev.Id);
                }
            }
        }

        private void ShowRoomPictures(AreaEntity room)
        {
            //if (room.Type == AreaTypes.机房)
            {
                using (Bll bll = new Bll())
                {
                    var areaRoom = bll.Areas.FindById(room.Id);
                    var path = areaRoom.GetPath(AreaTypes.楼层, "_");
                    var dirPath2 = AppDomain.CurrentDomain.BaseDirectory + "\\RoomPictures\\" + path;
                    DirectoryInfo dirInfo2 = new DirectoryInfo(dirPath2);
                    PictureViewer1.Show(dirInfo2);
                }
            }
        }

        private void ShowPersons()
        {
            if (currentArea == null) return;
            if (AreaCanvas1 == null) return;
            var service = new PersonService();
            //var persons = service.GetListByArea(area.Id + "");
            //if (persons == null)
            //{
            //    persons = service.GetListByArea("");
            //}
            var persons = service.GetList(true,true);
            var posService = new PosService();
            var posList = posService.GetList();//todo:实时数据以后从缓存中取
            if (posList!= null && persons != null)
            {
                foreach (var item in persons)
                {
                    if (item == null) continue;
                    if(item.Tag==null)
                    {
                        continue;
                    }
                    var pos = posList.FirstOrDefault(i => i!=null && i.Tag == item.Tag.Code);
                    item.Pos = pos;
                }
                AreaCanvas1.ShowPersons(persons);
            }
            
        }

        private void AreaListBox1_SelectedItemChanged(AreaEntity obj)
        {
            AreaCanvas1.SelectArea(obj);
        }

        private void DeviceListBox1_SelectedItemChanged(DevEntity obj)
        {
            AreaCanvas1.SelectDev(obj);
        }

        private void AreaCanvas1_DevSelected(Rectangle rect, DevEntity obj)
        {
            //SetDevInfo(rect, obj);
            if (parkArchorSettingWnd != null)
            {
                parkArchorSettingWnd.ShowInfo(rect, obj.Id);
            }
            if (roomArchorSettingWnd != null)
            {
                roomArchorSettingWnd.ShowInfo(rect, obj.Id);
            }
        }

        RoomArchorSettingWindow roomArchorSettingWnd;

        ParkArchorSettingWindow parkArchorSettingWnd;

        //private void CreateDevInfo(int areaId,Point point)
        //{
        //    //DevEntity 

        //    var area= areaId
        //}

        private Window SetDevInfo(Rectangle rect, DevEntity obj,bool isShow=true)
        {
            var parentArea = areaService.GetEntity(obj.ParentId + "");
            if (parentArea.IsPark()) //电厂
            {
                var bound = parentArea.InitBound;
                var leftBottom = bound.GetLeftBottomPoint();

                parkArchorSettingWnd = new ParkArchorSettingWindow();
                ArchorSettingContext.ZeroX = leftBottom.X;
                ArchorSettingContext.ZeroY = leftBottom.Y;
                parkArchorSettingWnd.RefreshDev += (dev) => {
                    archorSettings = bll.ArchorSettings.ToList();
                    obj.Refresh(dev);
                    AreaCanvas1.RefreshDev(dev);
                };
                parkArchorSettingWnd.ShowPointEvent += (x, y) => { AreaCanvas1.ShowPoint(x, y); };
                parkArchorSettingWnd.Closed += (sender, e) => { parkArchorSettingWnd = null; };

                if(isShow)
                    parkArchorSettingWnd.Show();

                if (parkArchorSettingWnd.ShowInfo(rect, obj.Id) == false)
                {
                    parkArchorSettingWnd.Close();
                    parkArchorSettingWnd = null;
                }
                return parkArchorSettingWnd;
            }
            else
            {
                roomArchorSettingWnd = new RoomArchorSettingWindow();
                //roomArchorSettingWnd.Owner = this;
                roomArchorSettingWnd.RefreshDev += (dev) => {
                    archorSettings = bll.ArchorSettings.ToList();
                    obj.Refresh(dev);
                    AreaCanvas1.RefreshDev(dev);
                };
                roomArchorSettingWnd.ShowPointEvent += (x, y) => { AreaCanvas1.ShowPoint(x, y); };
                roomArchorSettingWnd.Closed += (sender, e) => { roomArchorSettingWnd = null; };
                if (isShow)
                    roomArchorSettingWnd.Show();
                if (roomArchorSettingWnd.ShowInfo(rect, obj.Id) == false)
                {
                    roomArchorSettingWnd.Close();
                    roomArchorSettingWnd = null;
                }
                return roomArchorSettingWnd;
            }
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            
            
        }

        private void TabControl1_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (TabControl1.SelectedIndex == 2)
                {
                    if (currentArea != null)
                        ArchorListExportControl1.LoadData(currentArea.Id);
                    TabControl1.SelectionChanged -= TabControl1_OnSelectionChanged;
                }
            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.ToString());
            }
        }

        private void MenuRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void MenuStartTimer_Click(object sender, RoutedEventArgs e)
        {
            if(MenuStartTimer.Header.ToString()== "启动定时器")
            {
                StartPersonTimer();
                MenuStartTimer.Header = "停止定时器";
            }
            else
            {
                StopPersonTimer();
                MenuStartTimer.Header = "启动定时器";
            }
        }

        private void MenuConnectEngine_Click(object sender, RoutedEventArgs e)
        {
            if (MenuConnectEngine.Header.ToString() == "连接引擎")
            {
                StartEngine();
                MenuConnectEngine.Header = "断开引擎";
            }
            else
            {
                StopEngine();
                MenuConnectEngine.Header = "连接引擎";
            }
        }

        PositionEngineClient engineClient;

        private void StartEngine()
        {
            if (engineClient == null)
            {
                EngineLogin login = new EngineLogin();
                login.LocalIp = "127.0.0.1";
                login.LocalPort = 2323;
                login.LocalPort2 = 2324;
                login.EngineIp = "127.0.0.1";
                login.EnginePort = 3456;
                login.EnginePort2 = 1994;
                if (login.Valid() == false)
                {
                    MessageBox.Show("本地Ip和对端Ip必须是同一个Ip段的");
                    return;
                }

                engineClient = PositionEngineClient.Instance();
                //engineClient.Logs = Logs;
                engineClient.IsWriteToDb = true;
                engineClient.StartConnectEngine(login);
                engineClient.NewAlarmsFired += EngineClient_NewAlarmsFired;
            }
        }

        private void EngineClient_NewAlarmsFired(List<DbModel.Location.Alarm.LocationAlarm> obj)
        {
            var alarms = obj.ToTModel().ToArray();
            AlarmHub.SendLocationAlarms(alarms);
            AreaCanvas1.ShowLocationAlarms(alarms);
        }

        private void StopEngine()
        {
            if (engineClient != null)
            {
                engineClient.Stop();
                engineClient = null;
            }

        }

        private void MenuCreateDir_OnClick(object sender, RoutedEventArgs e)
        {
            using (Bll bll=new Bll())
            {
                var rooms = bll.Areas.FindAll(i => i.Type == AreaTypes.机房);
                var dirPath = AppDomain.CurrentDomain.BaseDirectory + "\\RoomPictures\\";
                DirectoryInfo dirInfo = new DirectoryInfo(dirPath);
                if (!dirInfo.Exists)
                {
                    dirInfo.Create();
                }
                else
                {
                    var subDirs = dirInfo.GetDirectories();
                    foreach (DirectoryInfo subDir in subDirs)
                    {
                        var dirs = subDir.GetDirectories();
                        if (dirs.Length == 0)
                        {
                            var files = subDir.GetFiles();
                            if (files.Length == 0)
                            {
                                subDir.Delete(false);
                            }
                        }
                        else
                        {

                        }
                    }
                }

                foreach (Area room in rooms)
                {
                    string roomPath = room.GetPath(AreaTypes.楼层, "_");
                    if (roomPath.Contains("走廊") || roomPath.Contains("办公室") || roomPath.Contains("会议室") || roomPath.Contains("男卫") || roomPath.Contains("女卫") || roomPath.Contains("钢梯") || roomPath.Contains("门厅") || roomPath.Contains("储藏") || roomPath.Contains("区域") || roomPath.Contains("场地") || roomPath.Contains("杂物") || roomPath.Contains("更衣") || roomPath.Contains("值班") || roomPath.Contains("饮水")) continue;
                    DirectoryInfo roomDirInfo = new DirectoryInfo(dirPath + roomPath);
                    if (!roomDirInfo.Exists)
                    {
                        roomDirInfo.Create();
                    }
                }
                Process.Start(dirPath);
            }
        }

        private void MenuOnlyShowDirNode_OnClick(object sender, RoutedEventArgs e)
        {
            FilterAreaTree = (a) =>
            {
                if (a.Type == AreaTypes.机房)
                {
                    return IsPictureExist(a);
                }
                else
                {
                    bool r = IsPictureExist(a);
                    if (r) return true;

                    if (a.Children == null || a.Children.Count == 0)
                    {
                        return false;
                    }
                    return true;
                }
            };
            LoadAreaTree();
        }

        private static bool IsPictureExist(DbAreaEntity a)
        {
            var roomPath = a.GetPath(AreaTypes.楼层, "_");
            var dirPath = AppDomain.CurrentDomain.BaseDirectory + "\\RoomPictures\\" + roomPath;
            if (Directory.Exists(dirPath))
            {
                var files = Directory.GetFiles(dirPath);
                if (files.Length > 0)
                {
                    return true;
                }
                else
                {
                    return false;
                }
                //return true;
            }
            else
            {
                return false;
            }
        }

        private void MenuMovePictures_OnClick(object sender, RoutedEventArgs e)
        {
            string tableFile1= AppDomain.CurrentDomain.BaseDirectory + "\\RoomPictures\\[四会数字电站建模]\\19082811_三维建模需求_改.xlsx";
            DataSet dataSet = ExcelLib.ExcelHelper.Load(new FileInfo(tableFile1), true);
            Dictionary<string, string> room2room1 = new Dictionary<string, string>();

            Dictionary<string, string> room2room2 = new Dictionary<string, string>();
            foreach (DataRow row in dataSet.Tables[0].Rows)
            {
                var floor1 = row[1].ToString();
                var room1 = row[2].ToString();
                var room2 = row[4].ToString();
                if (!string.IsNullOrEmpty(room2))
                {
                    room2room1.Add(room2, room1);
                }
                else
                {

                }
            }

            var rooms = bll.Areas.FindAll(i => i.Type == AreaTypes.机房);

            var dirPath1 = AppDomain.CurrentDomain.BaseDirectory + "\\RoomPictures\\[四会数字电站建模]\\";
            DirectoryInfo dirInfo1 = new DirectoryInfo(dirPath1);

            var dirPath2 = AppDomain.CurrentDomain.BaseDirectory + "\\RoomPictures\\";
            DirectoryInfo dirInfo2 = new DirectoryInfo(dirPath2);

            var subDirs1 = dirInfo1.GetDirectories();
            foreach (DirectoryInfo subDir in subDirs1)
            {
                var dirName = subDir.Name;
                if (dirName.Contains("（"))
                {
                    dirName = dirName.Split('（')[0];
                }
                if (room2room1.ContainsKey(dirName))
                {
                    var roomName = room2room1[dirName];
                    var room = rooms.FindAll(i => i.Name == roomName);
                    if (room.Count ==1)
                    {
                        var room2 = room[0];
                        var path = room2.GetPath(AreaTypes.楼层, "_");
                        DirectoryInfo dir = new DirectoryInfo(dirPath2+ path);
                        if (dir.Exists)
                        {
                            var files = subDir.GetFiles();
                            var fileList= files.ToList();
                            fileList.Sort((a, b) => { return a.LastWriteTime.CompareTo(b.LastWriteTime); });
                            foreach (FileInfo file in fileList)
                            {
                                string newPath = dir.FullName +"\\"+ file.Name;
                                file.MoveTo(newPath);
                            }

                            subDir.Delete();
                        }
                    }
                    else
                    {

                    }
                }
                else
                {

                }
            }
        }
    }
}
