﻿using Assets.z_Test.BackUpDevInfo;
using DbModel.Location.AreaAndDev;
using DbModel.Tools;
using DbModel.Tools.InitInfos;
using IModel.Enums;
using Location.BLL.Tool;
using Location.Model.InitInfos;
using Location.TModel.Location.AreaAndDev;
using LocationServices.Locations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LocationServer.Tools
{
    public class DevBackupHelper
    {
        public void BackupDevInfo(Action callBack=null)
        {            
            Thread thread = new Thread(() =>
            {
                LocationService service = new LocationService();
                var devlist = service.GetAllDevInfos().ToList();
                Log.Info(LogTags.DbInit, "Init NoramlDev,Path:"+GetSaveDevDirectory());
                DateTime recordT = DateTime.Now;
                //1.备份普通设备
                SaveNormalDev(devlist, service);
                Log.Info(LogTags.DbInit, string.Format("Init NormalDev complete,cost time: {0}s.",(DateTime.Now-recordT).TotalSeconds.ToString("f2")));
              
                Log.Info(LogTags.DbInit, "Init CameraDev... \n");
                recordT = DateTime.Now;
                //2.摄像头备份
                BackupCameraDev(service);
                Log.Info(LogTags.DbInit, string.Format("Init CameraDev complete,cost time: {0}s.", (DateTime.Now - recordT).TotalSeconds.ToString("f2")));

                Log.Info(LogTags.DbInit, "Init ArchorDev... \n");
                recordT = DateTime.Now;
                //3.备份基站信息
                BackupArchorDev(service);
                Log.Info(LogTags.DbInit, string.Format("Init ArchorDev complete,cost time: {0}s.", (DateTime.Now - recordT).TotalSeconds.ToString("f2")));

                Log.Info(LogTags.DbInit, "Init DoorAccess... \n");
                recordT = DateTime.Now;
                //4.备份门禁设备
                BackupDoorAccess(service);
                Log.Info(LogTags.DbInit, string.Format("Init DoorAccessDev complete,cost time: {0}s.", (DateTime.Now - recordT).TotalSeconds.ToString("f2")));
                if (callBack != null) callBack();
            });
            thread.IsBackground = true;
            thread.Start();          
        }

        /// <summary>
        /// 获取保存设备的文件夹
        /// </summary>
        /// <returns></returns>
        private string GetSaveDevDirectory()
        {
            return AppDomain.CurrentDomain.BaseDirectory + "Data\\设备信息\\" + DbModel.AppSetting.ParkName + "\\";
        }
        /// <summary>
        /// 获取Vs下保存目录
        /// </summary>
        /// <returns></returns>
        private string GetVsSaveDirctory()
        {
            return AppDomain.CurrentDomain.BaseDirectory + "..\\..\\"+ "Data\\设备信息\\" + DbModel.AppSetting.ParkName + "\\";
        }

        #region 普通设备备份
        /// <summary>
        /// 保存通用设备
        /// </summary>
        private void SaveNormalDev(List<Location.TModel.Location.AreaAndDev.DevInfo> devInfoList,LocationService service)
        {
            DevInfoBackupList backUpList = new DevInfoBackupList();
            backUpList.DevList = new List<DevInfoBackup>();
            foreach (var item in devInfoList)
            {
                string typeCodeT = item.TypeCode.ToString();
                if (TypeCodeHelper.IsLocationDev(typeCodeT) || TypeCodeHelper.IsCamera(typeCodeT)||TypeCodeHelper.IsDoorAccess(item.ModelName)||TypeCodeHelper.IsFireFightDevType(typeCodeT)) continue;
                DevInfoBackup dev = new DevInfoBackup();
                dev.DevId = item.DevID;
                dev.KKSCode = item.KKSCode;
                dev.Abutment_DevID = item.Abutment_DevID;
                dev.ModelName = item.ModelName;
                dev.Name = item.Name;
                dev.ParentName = GetAreaPath((int)item.ParentId,service);
                dev.TypeCode = item.TypeCode.ToString();

                DevPos pos = item.Pos;

                dev.RotationX = pos.RotationX.ToString();
                dev.RotationY = pos.RotationY.ToString();
                dev.RotationZ = pos.RotationZ.ToString();

                dev.XPos = pos.PosX.ToString();
                dev.YPos = pos.PosY.ToString();
                dev.ZPos = pos.PosZ.ToString();

                dev.ScaleX = pos.ScaleX.ToString();
                dev.ScaleY = pos.ScaleY.ToString();
                dev.ScaleZ = pos.ScaleZ.ToString();

                backUpList.DevList.Add(dev);
            }
            List<DevInfoBackup> deleteDevs = AddDeleteDev(devInfoList);
            if(devInfoList!=null&&devInfoList.Count!=0)
            {
                backUpList.DevList.AddRange(deleteDevs);
            }
            //string dirctory = GetSaveDevDirectory();
            //string initFile = dirctory+"DevInfoBackup.xml";
            //XmlSerializeHelper.Save(backUpList, initFile, Encoding.UTF8);
            SaveNormalDevXml("DevInfoBackup.xml",backUpList);
        }
        private void SaveNormalDevXml(string fileWithExtension,DevInfoBackupList backUpList)
        {
            //拷贝到Bin目录下
            string dirctory = GetSaveDevDirectory();
            string initFile = dirctory + fileWithExtension;
            XmlSerializeHelper.Save(backUpList, initFile, Encoding.UTF8);

            //直接保存到Vs目录
            string vsDirctory = GetVsSaveDirctory();
            if(Directory.Exists(vsDirctory))
            {
                string vsSaveFile = vsDirctory + fileWithExtension;
                XmlSerializeHelper.Save(backUpList, vsSaveFile, Encoding.UTF8);
            }
        }
        private static string DeleteTypeCode = "DeleteDev";
        /// <summary>
        /// 记录删除的设备（如果有设备被删除，备份时会被记录下来，TypeCode="DeleteDev",还原时这些设备不会被还原）
        /// 作用（当出现错误删除时，可以利用这些删除记录还原）
        /// </summary>
        /// <param name="devInfoList"></param>
        /// <returns></returns>
        private List<DevInfoBackup> AddDeleteDev(List<Location.TModel.Location.AreaAndDev.DevInfo> devInfoList)
        {
            //1.加载上一次的设备备份信息
            string filePath = GetSaveDevDirectory()+"DevInfoBackup.xml";
            var initInfo = XmlSerializeHelper.LoadFromFile<DevInfoBackupList>(filePath);
            List<DevInfoBackup> devInfos = initInfo.DevList;
            List<DevInfoBackup> deleteInfos = new List<DevInfoBackup>();
            Dictionary<string, string> devDic = TryGetDevDic(devInfoList);
            if (devInfos!=null)
            {
                foreach(var item in devInfos)
                {
                    if (item.TypeCode == DeleteTypeCode) continue;
                    //2.如果上一次备份的设备，在现在的数据库找不到。说明被删除了，记录下来。
                    if (!string.IsNullOrEmpty(item.DevId)&&!devDic.ContainsKey(item.DevId))
                    {                        
                        deleteInfos.Add(item);
                    }
                }
            }
            string deleteInfoPath = GetSaveDevDirectory() + "DeleteInfoBackup.xml";
            if (deleteInfos.Count != 0)
            {
                var deleteBackup = XmlSerializeHelper.LoadFromFile<DevInfoBackupList>(filePath);
                if (deleteBackup == null)
                {
                    deleteBackup = new DevInfoBackupList();
                    deleteBackup.DevList = new List<DevInfoBackup>();
                }
                deleteBackup.DevList.AddRange(deleteInfos);
                XmlSerializeHelper.Save(deleteBackup, deleteInfoPath, Encoding.UTF8);
                foreach(var item in deleteInfos)
                {
                    item.TypeCode = DeleteTypeCode;
                }
            }
            return deleteInfos;
        }
        /// <summary>
        /// 获取设备字典，主键 设备的guid
        /// </summary>
        /// <param name="devInfoList"></param>
        /// <returns></returns>
        private Dictionary<string, string>TryGetDevDic(List<Location.TModel.Location.AreaAndDev.DevInfo> devInfoList)
        {
            Dictionary<string, string> devInfoDic = new Dictionary<string, string>();
            foreach(var item in devInfoList)
            {
                if(!devInfoDic.ContainsKey(item.DevID))
                {
                    devInfoDic.Add(item.DevID,item.Name);
                }
            }
            return devInfoDic;
        }

        #endregion

        #region 摄像头备份
        public void BackupCameraDev(LocationService service)
        {
            var camList = service.GetAllCameraInfo();
            if (camList == null || camList.Count == 0) return;
            var devInfoList = service.GetAllDevInfos();
            if (devInfoList == null || devInfoList.Count == 0) return;
            List<Location.TModel.Location.AreaAndDev.DevInfo> devList = devInfoList.ToList();
            foreach (var item in camList)
            {
                Location.TModel.Location.AreaAndDev.DevInfo dev = devList.Find(i => i.Id == item.DevInfoId);
                if (dev != null)
                {
                    item.DevInfo = dev;
                }
                else
                {
                    Log.Info("CamerInfo is null:" + item.DevInfo);
                }
            }
            SaveCameraInfoToXml(camList.ToList(),service);
        }

        /// <summary>
        /// 保存设备信息至Xml文件
        /// </summary>
        private void SaveCameraInfoToXml(List<TModel.Location.AreaAndDev.Dev_CameraInfo> cameraList, LocationService service)
        {
            CameraInfoBackUpList backUpList = new CameraInfoBackUpList();
            backUpList.DevList = new List<CameraInfoBackup>();

            foreach (var item in cameraList)
            {
                if (item.DevInfo == null) continue;
                CameraInfoBackup dev = new CameraInfoBackup();
                dev.DevId = item.DevInfo.DevID;
                dev.KKSCode = item.DevInfo.KKSCode;
                dev.Abutment_DevID = item.DevInfo.Abutment_DevID;
                dev.ModelName = item.DevInfo.ModelName;
                dev.Name = item.DevInfo.Name;
                dev.ParentName = GetAreaPath((int)item.ParentId, service);
                dev.TypeCode = item.DevInfo.TypeCode.ToString();

                DevPos pos = item.DevInfo.Pos;

                dev.RotationX = pos.RotationX.ToString();
                dev.RotationY = pos.RotationY.ToString();
                dev.RotationZ = pos.RotationZ.ToString();

                dev.XPos = pos.PosX.ToString();
                dev.YPos = pos.PosY.ToString();
                dev.ZPos = pos.PosZ.ToString();

                dev.ScaleX = pos.ScaleX.ToString();
                dev.ScaleY = pos.ScaleY.ToString();
                dev.ScaleZ = pos.ScaleZ.ToString();

                dev.IP = item.Ip;
                dev.UserName = item.UserName;
                dev.PassWord = item.PassWord;
                dev.CameraIndex = item.CameraIndex.ToString();
                dev.Port = item.Port.ToString();
                dev.RtspURL = item.RtspUrl;

                backUpList.DevList.Add(dev);
            }
            //string initFile = GetSaveDevDirectory()+"CameraInfoBackup.xml";
            //XmlSerializeHelper.Save(backUpList, initFile, Encoding.UTF8);
            SaveCameraDevXml("CameraInfoBackup.xml",backUpList);
        }
        private void SaveCameraDevXml(string fileWithExtension, CameraInfoBackUpList backUpList)
        {
            //拷贝到Bin目录下
            string dirctory = GetSaveDevDirectory();
            string initFile = dirctory + fileWithExtension;
            XmlSerializeHelper.Save(backUpList, initFile, Encoding.UTF8);

            //直接保存到Vs目录
            string vsDirctory = GetVsSaveDirctory();
            if (Directory.Exists(vsDirctory))
            {
                string vsSaveFile = vsDirctory + fileWithExtension;
                XmlSerializeHelper.Save(backUpList, vsSaveFile, Encoding.UTF8);
            }
        }
        #endregion
        #region 基站设备
        /// <summary>
        /// 备份基站信息
        /// </summary>
        /// <param name="service"></param>
        private void BackupArchorDev(LocationService service)
        {
            List<TModel.Location.AreaAndDev.Archor> archorList = service.GetArchors();
            if (archorList == null || archorList.Count == 0) return;
            var devInfoList = service.GetAllDevInfos();
            if (devInfoList == null || devInfoList.Count == 0) return;
            List<Location.TModel.Location.AreaAndDev.DevInfo> devList = devInfoList.ToList();
            foreach (var item in archorList)
            {
                Location.TModel.Location.AreaAndDev.DevInfo dev = devList.Find(i => i.Id == item.DevInfoId);
                if (dev != null)
                {
                    item.DevInfo = dev;
                }
                else
                {
                    Log.Info("CamerInfo is null:" + item.DevInfo);
                }
            }
            SaveArchorInfoToXml(archorList,service);
        }
        private void SaveArchorInfoToXml(List<TModel.Location.AreaAndDev.Archor> archorList, LocationService service)
        {
            LocationDeviceList backUpList = new LocationDeviceList();
            backUpList.DepList = new List<LocationDevices>();

            foreach (var item in archorList)
            {
                if (item.DevInfo == null) continue;
                Area area = service.GetAreaById(item.ParentId);
                if(area==null)
                {
                    Log.Info(string.Format("Error: Dev {0} area not find...",item.DevInfo.Id));
                    continue;
                }
                LocationDevices areaList = backUpList.DepList.Find(i => i.Name == area.Name);
                if (areaList==null)
                {
                    areaList = new LocationDevices();
                    areaList.Name = area.Name;
                    areaList.DevList = new List<LocationDevice>();
                    backUpList.DepList.Add(areaList);
                }
                if (areaList.DevList == null) areaList.DevList = new List<LocationDevice>();
                LocationDevice dev = new LocationDevice();
                dev.Name = item.Name;
                dev.Abutment_DevID = item.DevInfo.Abutment_DevID;
                dev.AnchorId = item.Code;
                dev.IP = item.Ip;
                dev.AbsolutePosX = item.X.ToString("f2");
                dev.AbsolutePosY = item.Y.ToString("f2");
                dev.AbsolutePosZ = item.Z.ToString("f2");

                DevPos pos = item.DevInfo.Pos;
                if (pos != null)
                {
                    dev.XPos = pos.PosX.ToString("f2");
                    dev.YPos = pos.PosY.ToString("f2");
                    dev.ZPos = pos.PosZ.ToString("f2");
                }
                else
                {
                    Log.Info("Error: dev.pos is null->"+item.DevInfo.Id);
                }
                areaList.DevList.Add(dev);
            }
            //string initFile = GetSaveDevDirectory() + "基站信息.xml";
            //XmlSerializeHelper.Save(backUpList, initFile, Encoding.UTF8);
            SaveArchorDevXml("基站信息.xml",backUpList);
        }
        private void SaveArchorDevXml(string fileWithExtension, LocationDeviceList backUpList)
        {
            //拷贝到Bin目录下
            string dirctory = GetSaveDevDirectory();
            string initFile = dirctory + fileWithExtension;
            XmlSerializeHelper.Save(backUpList, initFile, Encoding.UTF8);

            //直接保存到Vs目录
            string vsDirctory = GetVsSaveDirctory();
            if (Directory.Exists(vsDirctory))
            {
                string vsSaveFile = vsDirctory + fileWithExtension;
                XmlSerializeHelper.Save(backUpList, vsSaveFile, Encoding.UTF8);
            }
        }

        #endregion
        #region 门禁设备
        /// <summary>
        /// 备份门禁设备
        /// </summary>
        /// <param name="service"></param>
        public void BackupDoorAccess(LocationService service)
        {
            var doorList = service.GetAllDoorAccessInfo();
            if (doorList == null || doorList.Count == 0)
            {
                Log.Info("DoorAccess is null...");
                return;
            }
            var devInfoList = service.GetAllDevInfos();
            if (devInfoList == null || devInfoList.Count == 0) return;
            List<Location.TModel.Location.AreaAndDev.DevInfo> devList = devInfoList.ToList();
            foreach (var item in doorList)
            {
                Location.TModel.Location.AreaAndDev.DevInfo dev = devList.Find(i => i.Id == item.DevInfoId);
                if (dev != null)
                {
                    item.DevInfo = dev;
                }
                else
                {
                    Log.Info("CamerInfo is null:" + item.DevInfo);
                }
            }
            SaveDoorAccessToXml(doorList, service);
        }
        private void SaveDoorAccessToXml(IList<Location.TModel.Location.AreaAndDev.Dev_DoorAccess> doorList, LocationService service)
        {
            DoorAccessList backUpList = new DoorAccessList();
            backUpList.DevList = new List<DoorAccess>();
            foreach (var item in doorList)
            {
                if (item.DevInfo == null) continue;
                DoorAccess dev = new DoorAccess();
                dev.DevId = item.DevInfo.DevID;
                dev.KKSCode = item.DevInfo.KKSCode;
                dev.Abutment_DevID = item.DevInfo.Abutment_DevID;
                dev.ModelName = item.DevInfo.ModelName;
                dev.Name = item.DevInfo.Name;
                dev.ParentName = GetAreaPath((int)item.ParentId, service);
                dev.TypeCode = item.DevInfo.TypeCode.ToString();

                DevPos pos = item.DevInfo.Pos;

                dev.RotationX = pos.RotationX.ToString();
                dev.RotationY = pos.RotationY.ToString();
                dev.RotationZ = pos.RotationZ.ToString();

                dev.XPos = pos.PosX.ToString();
                dev.YPos = pos.PosY.ToString();
                dev.ZPos = pos.PosZ.ToString();

                dev.ScaleX = pos.ScaleX.ToString();
                dev.ScaleY = pos.ScaleY.ToString();
                dev.ScaleZ = pos.ScaleZ.ToString();

                dev.DoorId = item.DoorId;
                dev.Local_DevId = item.DevID;
                backUpList.DevList.Add(dev);
            }
            //string initFile = GetSaveDevDirectory() + "DoorAccessBackup.xml";
            //XmlSerializeHelper.Save(backUpList, initFile, Encoding.UTF8);
            SaveDoorAccessDevXml("DoorAccessBackup.xml",backUpList);
        }
        private void SaveDoorAccessDevXml(string fileWithExtension, DoorAccessList backUpList)
        {
            //拷贝到Bin目录下
            string dirctory = GetSaveDevDirectory();
            string initFile = dirctory + fileWithExtension;
            XmlSerializeHelper.Save(backUpList, initFile, Encoding.UTF8);

            //直接保存到Vs目录
            string vsDirctory = GetVsSaveDirctory();
            if (Directory.Exists(vsDirctory))
            {
                string vsSaveFile = vsDirctory + fileWithExtension;
                XmlSerializeHelper.Save(backUpList, vsSaveFile, Encoding.UTF8);
            }
        }
        #endregion
        /// <summary>
        /// 获取区域路径 （子区域|父区域）
        /// </summary>
        /// <param name="areaId"></param>
        /// <param name="service"></param>
        /// <returns></returns>
        private string GetAreaPath(int areaId, LocationService service)
        {
            Area area = service.GetAreaById(areaId);
            string parentName = "";
            if (area != null)
            {
                Area parentArea = service.GetAreaById((int)area.ParentId);
                string grandparentName = parentArea == null ? "" : parentArea.Name;
                parentName = string.Format("{0}|{1}", area.Name, grandparentName);
            }
            return parentName;
        }
        
    }
}
