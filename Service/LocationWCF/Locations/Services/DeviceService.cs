﻿using BLL;
using BLL.Blls.Location;
using DbModel.Tools;
using IModel.Enums;
using Location.BLL.Tool;
using Location.TModel.Location.AreaAndDev;
using Location.TModel.Tools;
using LocationServices.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using TModel.Location.AreaAndDev;
using TModel.LocationHistory.AreaAndDev;
using TModel.Tools;
using TEntity = Location.TModel.Location.AreaAndDev.DevInfo;
using TPEntity = Location.TModel.Location.AreaAndDev.PhysicalTopology;

namespace LocationServices.Locations.Services
{
    public interface IDeviceService : ILeafEntityService<TEntity, TPEntity>
    {
        IList<TEntity> GetListByTypes(int[] typeList);
        TEntity GetEntityByDevId(string devId);
        TEntity GetEntityById(int id);
        TEntity GetDevByGameName(string nameName);


    }
    public class DeviceService : IDeviceService
    {
        private Bll db;
        private DevInfoBll dbSet;

        public DeviceService()
        {
            db = Bll.NewBllNoRelation();
            dbSet = db.DevInfos;
        }

        public DeviceService(Bll bll)
        {
            this.db = bll;
            dbSet = db.DevInfos;
        }

        public TEntity Delete(string id)
        {
            var devId = id.ToInt();
            var dev = dbSet.Find(devId);
            if (dev != null)
            {
                if (dev.Local_TypeCode == TypeCodes.Archor)
                {
                    var archor = db.Archors.DeleteByDev(devId);
                    if (archor != null)
                    {
                        var item = dbSet.DeleteById(id.ToInt());
                        return item.ToTModel();
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    var item = dbSet.DeleteById(id.ToInt());
                    return item.ToTModel();
                }
            }
            else
            {
                return null;
            }
        }

        public TEntity GetEntity(string id)
        {
            var item = dbSet.Find(id.ToInt());
            return item.ToTModel();
        }

        public TPEntity GetParent(string id)
        {
            var item = dbSet.Find(id.ToInt());
            if (item == null) return null;
            return new AreaService(db).GetEntity(item.ParentId+"");
        }
        
        /// <summary>
        /// 字符串Id,GUID那部分
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TEntity GetEntityByDevId(string id)
        {
            List<TEntity> devInfo = dbSet.DbSet.Where(item => item.Local_DevID == id).ToList().ToTModel();
            if (devInfo != null && devInfo.Count != 0) return devInfo[0];
            else return null;
        }

        /// <summary>
        /// 数字Id,主键
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TEntity GetEntityById(int id)
        {
            List<TEntity> devInfo = dbSet.DbSet.Where(item => item.Id == id).ToList().ToTModel();
            if (devInfo != null && devInfo.Count != 0) return devInfo[0];
            else return null;
        }

        public TEntity GetDevByGameName(string nameName)
        {
            List<TEntity> devInfo = dbSet.DbSet.Where(item => item.ModelName == nameName).ToList().ToTModel();
            if (devInfo != null && devInfo.Count != 0) return devInfo[0];
            else return null;
        }

        public List<TEntity> GetList()
        {
            var devInfoList = dbSet.ToList().ToTModel();
            BindingDev(devInfoList);
            return devInfoList.ToWCFList();
        }

        public IList<TEntity> GetListByName(string name)
        {          
            var devInfoList = dbSet.GetListByName(name).ToTModel();
            BindingDev(devInfoList);
            return devInfoList.ToWCFList();
        }

        public TEntity Post(TEntity item)
        {
            var dbItem = item.ToDbModel();
            var result = dbSet.Add(dbItem);
            return result ? dbItem.ToTModel() : null;
        }

        public List<TEntity> PostRange(List<TEntity>itemList)
        {
            List<TEntity> entityList = new List<TEntity>();
            foreach(var item in itemList)
            {
                var dbItem = item.ToDbModel();
                var result = dbSet.Add(dbItem);
                TEntity value = result ? dbItem.ToTModel() : null;
                if (value != null) entityList.Add(value);
            }
            return entityList;
        }
        public TEntity Post(string pid, TEntity item)
        {
            if (item == null) return null;
            item.ParentId = pid.ToInt();
            var dbItem = item.ToDbModel();
            var result = dbSet.Add(dbItem);
            return result ? dbItem.ToTModel() : null;
        }

        public TEntity Put(TEntity item)
        {
            if (item == null) return null;
            var dbItem = item.ToDbModel();
            dbItem.ModifyTime = DateTime.Now;
            dbItem.ModifyTimeStamp = TimeConvert.ToStamp(dbItem.ModifyTime);
            var result = dbSet.Edit(dbItem);
            return result ? dbItem.ToTModel() : null;
        }

        /// <summary>
        /// 获取所有的设备基本信息
        /// </summary>
        /// <returns></returns>
        public IList<TEntity> GetListByTypes(int[] types)
        {
            List<TEntity> devInfoList = null;
            if (types == null || types.Length == 0)
            {
                devInfoList = dbSet.ToList().ToTModel();
            }
            else
            {
                devInfoList = dbSet.DbSet.Where(i => types.Contains(i.Local_TypeCode)).ToList().ToTModel();
            }

            BindingDev(devInfoList);
            return devInfoList.ToWCFList();
        }

        /// <summary>
        /// 通过区域ID,获取区域下所有设备
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        public List<TEntity> GetListByPids(int[] pidArray)
        {
            try
            {
                List<int> pidList = pidArray.ToList();
                List<TEntity> devInfoList = new List<TEntity>();
                //DateTime recordT = DateTime.Now;
                //string value = "";
                //foreach (var pid in pidList)
                //{
                //    var dbDevList = dbSet.DbSet.Where(item => item.ParentId == pid && item.Local_TypeCode != TypeCodes.TrackPoint).ToList();
                //    devInfoList.AddRange(dbDevList.ToTModel());
                //    value += string.Format("Find dev by id,id:{0} cost time:{1}ms \n",pid,(DateTime.Now-recordT).TotalMilliseconds);
                //    recordT = DateTime.Now;
                //    //BindingDev(devInfoList);
                //}
                //Log.Info(value);
                //return devInfoList.ToWCFList();

                var dbDevs = dbSet.DbSet.Where(item => item.ParentId != null && pidList.Contains((int)item.ParentId) && item.Local_TypeCode != TypeCodes.TrackPoint).ToList();
                devInfoList = dbDevs.ToTModel();
                var list= devInfoList.ToWCFList();
                //string xml = XmlSerializeHelper.GetXmlText(list);
                return list;
            }catch(Exception e)
            {
                Log.Error("DeviceService.GetListByPids.Exception:"+e.ToString());
                return null;
            }
        }

        /// <summary>
        /// 通过区域ID,获取区域下所有设备
        /// </summary>
        /// <param name="pid"></param>
        /// <returns></returns>
        public List<TEntity> GetListByPid(string pids)
        {
            var list = pids.Split(',').Select(i => i.ToInt()).ToList();

            return dbSet.GetListByPid(list).ToWcfModelList();
        }


        public IList<TEntity> DeleteListByPid(string pids)
        {
            var list = pids.Split(',').Select(i => i.ToInt()).ToList();
            return dbSet.DeleteListByPid(list).ToWcfModelList();
        }


        private void BindingDev(List<TEntity> devInfoList)
        {
            BindingDevParent(devInfoList, db.Areas.ToList().ToTModel());           
            foreach (var item in devInfoList)
            {
                item.TypeName = TypeCodeHelper.GetTypeName(item.TypeCode+"");
            }
        }

        #region static 
        //public static bool IsBindingPos;

        public static void BindingDevPos(List<TEntity> devInfoList, List<DevPos> devPosList)
        {
            //if(IsBindingPos==true)return;
            //IsBindingPos = true;
            if (devInfoList == null || devInfoList.Count == 0)
            {
                Log.Info("DevInfoList is null");
                return;
            }
            foreach (var item in devInfoList)
            {
                DevPos pos = devPosList.Find(o => o.DevID == item.DevID);
                if (pos == null)
                {
                    Log.Info("设备：{0} 加载位置信息失败.", item.DevID);
                }
                else
                {
                    item.SetPos(pos);
                }
            }
        }

        public static void BindingDevParent(List<TEntity> devInfoList, List<TPEntity> nodeList)
        {
            //if(IsBindingPos==true)return;
            //IsBindingPos = true;
            if (devInfoList == null || devInfoList.Count == 0)
            {
                Log.Info("DevInfoList is null");
                return;
            }          
            if (nodeList != null)
            {
                foreach (PhysicalTopology node in nodeList)
                {
                    node.Parent = nodeList.Find(i => i.Id == node.ParentId);
                }
            }  
            foreach (var item in devInfoList)
            {

                //if(string.IsNullOrEmpty(item.IP)&&TypeCodeHelper.IsCamera(item.TypeCode.ToString()))
                //{
                //    var camTemp = cameraList.Find(i=>i.DevInfoId==item.Id);
                //    if (camTemp != null) item.IP = camTemp.Ip;
                //}

                PhysicalTopology node = nodeList.Find(o => o.Id == item.ParentId);
                if (node == null)
                {
                    Log.Info("设备：{0} 加载位置信息失败.", item.DevID);
                }
                else
                {
                    //item.Parent = node;
                    item.Path = GetPath(node);
                    //Log.Info("path：{0} ", item.Path);
                }
            }
        }

        public static string GetPath(PhysicalTopology node)
        {
            if (node.Parent == null)
            {
                //return node.Name;
                return "";
            }
            else
            {
                string path = GetPath(node.Parent);
                if (string.IsNullOrEmpty(path)) return node.Name;
                else
                {
                    return path + "->" + node.Name;
                }
            }
        }

        #endregion

        public List<NearbyDev> GetNearbyDev_Currency(int id, float fDis, int nFlag)
        {
            List<NearbyDev> lst = new List<NearbyDev>();
            List<NearbyDev> lst2 = new List<NearbyDev>();
            DbModel.Location.Data.LocationCardPosition lcp = db.LocationCardPositions.DbSet.Where(p => p.PersonId == id).FirstOrDefault();
            if (lcp == null || lcp.AreaId == null)
            {
                return lst;
            }

            int? AreadId = lcp.AreaId;

            float PosX = lcp.X;
            float PosY = lcp.Y;
            float PosZ = lcp.Z;

            float PosX2 = 0;
            float PosY2 = 0;
            float PosZ2 = 0;

            float sqrtDistance = 0;
            float Distance = 0;

            //根据区域ID和标志位获取摄像头
            GetCameraDevByAreaId(AreadId, nFlag, lst2);
            if (lst2.Count <= 0)
            {
                DbModel.Location.AreaAndDev.Area area = db.Areas.Find(p => p.Id == AreadId);
                GetCameraDevByAreaId(area.ParentId, nFlag, lst2);
            }

            //var query = from t1 in db.DevInfos.DbSet
            //            join t2 in db.DevTypes.DbSet on t1.Local_TypeCode equals t2.TypeCode
            //            join t3 in db.Areas.DbSet on t1.ParentId equals t3.Id
            //            where t1.ParentId == AreadId && (nFlag == 0 ? true : (t1.Local_TypeCode == 3000201 || t1.Local_TypeCode == 14 || t1.Local_TypeCode == 3000610 || t1.Local_TypeCode == 1000102))
            //            select new NearbyDev { id = t1.Id, Name = t1.Name, TypeCode = t1.Local_TypeCode, TypeName = t2.TypeName, Area = t3.Name, X = t1.PosX, Y = t1.PosY, Z = t1.PosZ };

            //if (query != null)
            //{
            //    lst2 = query.ToList();
            //}

            var areas = db.Areas.ToDictionary();
            var bounds = db.Bounds.ToDictionary();

            foreach (var item in areas)
            {
                var area = item.Value;
                if (area.InitBoundId != null)
                {
                    area.InitBound = bounds[(int)area.InitBoundId];
                }
            }

            foreach (NearbyDev item in lst2)
            {
                float x = item.X;
                float y = item.Y;
                float z = item.Z;

                if (areas.ContainsKey(item.AreaId))
                {
                    var area = areas[item.AreaId];//所在区域
                   

                    if (area.Type == AreaTypes.机房)
                    {
                        var floor = areas[(int)area.ParentId];
                        var building = areas[(int)floor.ParentId];//
                        var minX = area.InitBound.GetZeroX()+floor.InitBound.GetZeroX() + building.InitBound.GetZeroX();
                        var minY = area.InitBound.GetZeroY() + floor.InitBound.GetZeroY() + building.InitBound.GetZeroY();
                        x += minX;
                        z += minY;
                    }
                    else if (area.Type == AreaTypes.楼层)
                    {
                        var floor = area;
                        var building = areas[(int)floor.ParentId];//
                        var minX = floor.InitBound.GetZeroX() + building.InitBound.GetZeroX();
                        var minY = floor.InitBound.GetZeroY() + building.InitBound.GetZeroY();
                        x += minX;
                        z += minY;
                    }
                }
                
                PosX2 = x - PosX;
                PosY2 = y - PosY;
                PosZ2 = z - PosZ;

                sqrtDistance = PosX2 * PosX2 + PosY2 * PosY2 + PosZ2 * PosZ2;
                Distance = (float)System.Math.Sqrt(sqrtDistance);
                item.Distance = Distance;
                if (Distance <= fDis)
                {
                    NearbyDev item2 = item.Clone();
                    if (item2 != null)
                    {
                        item2.X = PosX2;
                        item2.Y = PosY2;
                        item2.Z = PosZ2;
                        lst.Add(item2);
                    }
                }

                PosX2 = 0;
                PosY2 = 0;
                PosZ2 = 0;
                sqrtDistance = 0;
                Distance = 0;
            }

            lst.Sort(new DevDistanceCompare());

            if (lst.Count == 0)
            {
                lst = null;
            }

            return lst;
        }

        private void GetCameraDevByAreaId(int? AreadId, int nFlag, List<NearbyDev> lst2)
        {
            List<NearbyDev> Result = new List<NearbyDev>();

            var query = from t1 in db.DevInfos.DbSet
                        join t2 in db.DevTypes.DbSet on t1.Local_TypeCode equals t2.TypeCode
                        join t3 in db.Areas.DbSet on t1.ParentId equals t3.Id
                        where t1.ParentId == AreadId && (nFlag == 0 ? true : (t1.Local_TypeCode == 3000201 || t1.Local_TypeCode == 14 || t1.Local_TypeCode == 3000610 || t1.Local_TypeCode == 1000102))
                        select new NearbyDev { id = t1.Id, Name = t1.Name, TypeCode = t1.Local_TypeCode, TypeName = t2.TypeName, AreaName = t3.Name, AreaId=t3.Id, X = t1.PosX, Y = t1.PosY, Z = t1.PosZ };

            if (query != null)
            {
                Result = query.ToList();
            }

            if (Result == null || Result.Count <= 0)
            {
                return;
            }

            foreach (NearbyDev item in Result)
            {
                lst2.Add(item);
            }

            return;
        }

        public List<NearbyDev> GetNearbyCamera_Alarm(int id, float fDis)
        {
            List<NearbyDev> lst = new List<NearbyDev>();
            List<NearbyDev> lst2 = new List<NearbyDev>();
            DbModel.Location.AreaAndDev.DevInfo dev = db.DevInfos.DbSet.Where(p => p.Id == id).FirstOrDefault();
            if (dev == null || dev.ParentId == null)
            {
                return lst;
            }

            int? AreadId = dev.ParentId;
            float PosX = dev.PosX;
            float PosY = dev.PosY;
            float PosZ = dev.PosZ;

            float PosX2 = 0;
            float PosY2 = 0;
            float PosZ2 = 0;

            float sqrtDistance = 0;
            float Distance = 0;

            var query = from t1 in db.DevAlarms.DbSet
                        join t2 in db.DevInfos.DbSet on t1.DevInfoId equals t2.Id
                        join t3 in db.DevTypes.DbSet on t2.Local_TypeCode equals t3.TypeCode
                        join t4 in db.Areas.DbSet on t2.ParentId equals t4.Id
                        where t2.ParentId == AreadId && (t2.Local_TypeCode == 3000201 || t2.Local_TypeCode == 14 || t2.Local_TypeCode == 3000610 || t2.Local_TypeCode == 1000102)
                        select new NearbyDev { id = t2.Id, Name = t2.Name, TypeCode = t2.Local_TypeCode, TypeName = t3.TypeName, AreaName = t4.Name, X = t2.PosX, Y = t2.PosY, Z = t2.PosZ };
            if (query != null)
            {
                lst2 = query.ToList();
            }

            foreach (NearbyDev item in lst2)
            {
                PosX2 = item.X - PosX;
                PosY2 = item.Y - PosY;
                PosZ2 = item.Z - PosZ;

                sqrtDistance = PosX2 * PosX2 + PosY2 * PosY2 + PosZ2 * PosZ2;
                Distance = (float)System.Math.Sqrt(sqrtDistance);
                item.Distance = Distance;
                if (Distance <= fDis)
                {
                    NearbyDev item2 = item.Clone();
                    if (item2 != null)
                    {
                        lst.Add(item2);
                    } 
                }


                PosX2 = 0;
                PosY2 = 0;
                PosZ2 = 0;
                sqrtDistance = 0;
                Distance = 0;
            }

            lst.Sort(new DevDistanceCompare());

            return lst;
        }

        public List<EntranceGuardActionInfo> GetEntranceActionInfoByPerson24Hours(int id)
        {
            DateTime dtNow = DateTime.Now;
            DateTime dtOld = DateTime.Now.AddHours(-24);

            long lNow = TimeConvert.ToStamp(dtNow);
            long lOld = TimeConvert.ToStamp(dtOld);

            List<EntranceGuardActionInfo> lst = new List<EntranceGuardActionInfo>();
            List<int> lst2 = db.EntranceGuardCardToPersonnels.DbSet.Where(p => p.PersonnelId == id).Select(p=>p.EntranceGuardCardId).ToList();
            List<DbModel.LocationHistory.AreaAndDev.DevEntranceGuardCardAction> lst3 = null;
            List<DbModel.Location.AreaAndDev.EntranceGuardCard> lst4 = db.EntranceGuardCards.ToList();
            List<DbModel.Location.AreaAndDev.DevInfo> lst5 = db.DevInfos.ToList();
            List<DbModel.Location.AreaAndDev.Area> lst6 = db.Areas.ToList();

            if (lst2 == null)
            {
                return lst;
            }

            var query = from t1 in db.DevEntranceGuardCardActions.DbSet
                        where lst2.Contains(t1.EntranceGuardCardId) && t1.OperateTimeStamp >= lOld && t1.OperateTimeStamp <= lNow
                        select t1;
            if (query == null)
            {
                return lst;
            }

            lst3 = query.ToList();

            foreach (DbModel.LocationHistory.AreaAndDev.DevEntranceGuardCardAction item in lst3)
            {
                int DevInfoId = item.DevInfoId;
                int EntranceGuardCardId = item.EntranceGuardCardId;
                DateTime? OperateTime = item.OperateTime;
                int nInOutState = item.nInOutState;

                var card = lst4.Find(p => p.Id == EntranceGuardCardId);
                if (card == null || card.Code == null || card.Code == "")
                {
                    continue;
                }

                var devinfo = lst5.Find(p => p.Id == DevInfoId);
                if (devinfo == null || devinfo.Name == null || devinfo.Name == "" || devinfo.ParentId == null)
                {
                    continue;
                }

                string Name = devinfo.Name;
                int AreaId = (int)devinfo.ParentId;
                var area = lst6.Find(p=>p.Id == AreaId);
                if (area == null || area.Name == "" || area.Name == null)
                {
                    continue;
                }

                EntranceGuardActionInfo ega = new EntranceGuardActionInfo();
                ega.Id = DevInfoId;
                ega.Name = Name;
                ega.AreadId = AreaId;
                ega.AreadName = area.Name;
                ega.Code = card.Code;
                ega.OperateTime = OperateTime;
                ega.nInOutState = nInOutState;

                lst.Add(ega);
            }
           
            return lst;
        }
    }
}
