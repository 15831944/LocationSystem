﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Location.Model.InitInfos;
using System.IO;
using Location.Model;
using Location.BLL.Blls;
using Location.Model.LocationTables;
namespace Location.BLL.Tool
{
    public class LocationDeviceHelper
    {
        /// <summary>
        /// 基站设备TypeCode
        /// </summary>
        public static string LocationDevTypeCode = "20180821";
        /// <summary>
        /// 基站设备模型名称
        /// </summary>
        private static string LocationDevModelName = "初灵_警笛_3D";
        /// <summary>
        /// 默认基站高度
        /// </summary>
        private static float DeviceHeight = 2f;
        /// <summary>
        /// 通过文件导入基站信息
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="devBlls"></param>
        /// <param name="topoTree"></param>
        /// <returns></returns>
        public static bool ImportLocationDeviceFromFile(string filePath,ArchorBll archorBll, PhysicalTopologyBll topoTree)
        {
            if (!File.Exists(filePath)|| archorBll == null||topoTree==null)
            {
                return false;
            }
            LocationDeviceList initInfo = XmlSerializeHelper.LoadFromFile<LocationDeviceList>(filePath);
            foreach (var devArea in initInfo.DepList)
            {
                PhysicalTopology topo = topoTree.FindByName(devArea.Name);
                if (topo == null) continue;
                AddLocationDev(devArea.DevList, archorBll, topo.Id);
            }
            return true;
        }
        /// <summary>
        /// 添加基站设备
        /// </summary>
        /// <param name="devList">设备列表</param>
        /// <param name="devBlls">数据信息</param>
        /// <param name="areaId">设备所属区域ID</param>
        private static void AddLocationDev(List<LocationDevice> devList, ArchorBll archorBll, int? areaId)
        {
            foreach (var locationDev in devList)
            {                
                DevInfo devInfo = GetDevInfo(locationDev,areaId);
                DevPos devPos = GetDevPos(locationDev,devInfo.DevID);
                Archor archor = GetAnchorInfo(locationDev,devInfo.DevID);
                devInfo.Pos = devPos;
                archor.Dev = devInfo;
                archorBll.Add(archor);
                //devBlls.Add(devInfo);
            }
        }
        /// <summary>
        /// 获取基站信息
        /// </summary>
        /// <param name="locationDev"></param>
        /// <param name="devID"></param>
        /// <returns></returns>
        private static Archor GetAnchorInfo(LocationDevice locationDev,string devID)
        {
            //new Archor() { Code = "85A4", Name = "基站1", X = 3000, Y = 870, Z = 200, Type = 0, IsAutoIp = true, Ip = "", ServerIp = "", ServerPort = 0, Power = 0, AliveTime = 0, Enable = 1 };
            Archor archor = new Archor();
            archor.Code = locationDev.AnchorId;
            archor.DevId = devID;
            archor.X = TryParseFloat(locationDev.XPos);
            archor.Y = TryParseFloat(locationDev.ZPos);
            archor.Z = TryParseFloat(locationDev.YPos);
            archor.Name = locationDev.Name;
            archor.Type = 0;
            archor.IsAutoIp = true;
            archor.Ip = "";
            archor.ServerIp = "";
            archor.ServerPort = 0;
            archor.Power = 0;
            archor.AliveTime = 0;
            archor.Enable = 1;
            return archor;
        }
        /// <summary>
        /// 获取设备信息
        /// </summary>
        /// <param name="locationDev"></param>
        /// <param name="areaId"></param>
        /// <returns></returns>
        private static DevInfo GetDevInfo(LocationDevice locationDev,int? areaId)
        {
            DevInfo dev = new DevInfo();
            dev.DevID = Guid.NewGuid().ToString();
            dev.IP = "";
            dev.KKSCode = "";
            dev.CreateTime = DateTime.Now;
            dev.ModifyTime = DateTime.Now;
            dev.Name = locationDev.Name;
            dev.ModelName = LocationDevModelName;
            dev.Status = 0;
            dev.ParentId = areaId;
            try
            {
                dev.TypeCode = int.Parse(LocationDevTypeCode);
            }
            catch (Exception e)
            {
                dev.TypeCode = 0;
            }
            dev.UserName = "admin";
            return dev;
        }
        /// <summary>
        /// 获取设备位置信息
        /// </summary>
        /// <param name="locationDev"></param>
        /// <returns></returns>
        private static DevPos GetDevPos(LocationDevice locationDev,string devId)
        {
            DevPos pos = new DevPos();
            pos.DevID = devId;
            pos.PosX = TryParseFloat(locationDev.XPos);
            pos.PosZ = TryParseFloat(locationDev.YPos);
            pos.PosY = TryParseFloat(locationDev.ZPos);
            pos.RotationX = 0;
            pos.RotationY = 0;
            pos.RotationZ = 0;
            pos.ScaleX = 1;
            pos.ScaleY = 1;
            pos.ScaleZ = 1;
            return pos;
        }
        /// <summary>
        /// 字符转Float
        /// </summary>
        /// <param name="num"></param>
        /// <returns></returns>
        private static float TryParseFloat(string num)
        {
            try
            {
                if (string.IsNullOrEmpty(num))
                {
                    return DeviceHeight;
                }
                float value = float.Parse(num);
                return value;
            }
            catch (Exception e)
            {
                return 0;
            }
        }

    }
}
