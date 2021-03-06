﻿using System;
using System.Collections.Generic;
using Location.BLL.ServiceHelpers;
using Location.TModel.Location.AreaAndDev;
using LocationServices.Converters;
using LocationServices.Locations.Services;
using TModel.Location.Nodes;
using TModel.Location.AreaAndDev;
using TModel.Location.Person;
using System.Linq;
using DbModel.Location.AreaAndDev;
using BLL;
using System.Diagnostics;
using DbModel.Location.Alarm;
using DbModel.LocationHistory.Alarm;
using DbModel.Tools;
using Location.BLL.Tool;

namespace LocationServices.Locations
{
    //管理相关的接口
    public partial class LocationService : ILocationService, IDisposable
    {
        /// <summary>
        /// 获取物理逻辑拓扑
        /// </summary>
        /// <returns></returns>
        public IList<PhysicalTopology> GetPhysicalTopologyList(int view)
        {
            ShowLogEx(">>>>> GetPhysicalTopologyList");
            BLL.Bll dbpt = Bll.NewBllNoRelation();
            return new AreaService(dbpt).GetList(view);
        }

        public PhysicalTopology GetPhysicalTopology(string id, bool getChildren)
        {
            ShowLogEx(">>>>> GetPhysicalTopology id" + id);
            return new AreaService(db).GetEntity(id, getChildren);
        }

        public IList<PhysicalTopology> GetPhysicalTopologyListByName(string name)
        {
            ShowLogEx(">>>>> GetPhysicalTopologyListByName name" + name);
            return new AreaService(db).GetListByName(name);
        }

        public IList<PhysicalTopology> GetPhysicalTopologyListByPid(string pid)
        {
            ShowLogEx(">>>>> GetPhysicalTopologyListByPid pid" + pid);
            return new AreaService(db).GetListByPid(pid);
        }

        public AreaPoints GetPoints(int areaId)
        {
            return new AreaService(db).GetPoints(areaId + "");
        }

        public List<AreaPoints> GetPointsByPid(int pid)
        {
            return new AreaService(db).GetPointsByPid(pid + "");
        }

        /// <summary>
        /// 获取物理逻辑拓扑
        /// </summary>
        /// <returns></returns>
        public PhysicalTopology GetPhysicalTopologyTree(int view)
        {
            ShowLogEx(">>>>> GetPhysicalTopologyTree view=" + view);
            BLL.Bll dbpt = Bll.NewBllNoRelation();
            return new AreaService(dbpt).GetTree(view);
            //return null;
            //return new PhysicalTopology() { Id = 1, Name = "root" };
        }

        public AreaNode GetPhysicalTopologyTreeNode(int view)
        {
            ShowLogEx(">>>>> GetPhysicalTopologyTreeNode view=" + view);
            var result = new AreaService(db).GetBasicTree(view);
            ShowLogEx("<<<<< GetPhysicalTopologyTreeNode view=" + view);
            return result;
            //return null;
            //return new AreaNode() { Id = 1, Name = "root" };
        }

        public PhysicalTopology GetPhysicalTopologyTreeById(string id)
        {
            ShowLogEx(">>>>> GetPhysicalTopologyTreeById id=" + id);
            return new AreaService(db).GetTree(id);
        }

        public PhysicalTopology AddPhysicalTopology(string pid, PhysicalTopology item)
        {
            return new AreaService(db).Post(pid, item);
        }

        public PhysicalTopology AddPhysicalTopology(PhysicalTopology item)
        {
            return new AreaService(db).Post(item);
        }

        public PhysicalTopology EditPhysicalTopology(PhysicalTopology item)
        {
            return new AreaService(db).Put(item);
        }

        public PhysicalTopology RemovePhysicalTopology(string id)
        {
            return new AreaService(db).Delete(id);
        }

        public IList<PhysicalTopology> RemovePhysicalTopologyChildren(string pid)
        {
            return new AreaService(db).DeleteListByPid(pid);
        }

        /// <summary>
        /// 获取园区下的监控范围
        /// </summary>
        /// <returns></returns>
        public IList<PhysicalTopology> GetParkMonitorRange()
        {
            PhysicalTopologySP physicalTopologySp = new PhysicalTopologySP(db);
            var results = physicalTopologySp.GetParkMonitorRange();
            return results.ToWcfModelList();
        }

        /// <summary>
        /// 获取楼层下的监控范围
        /// </summary>
        /// <returns></returns>
        public IList<PhysicalTopology> GetFloorMonitorRange()
        {
            PhysicalTopologySP physicalTopologySp = new PhysicalTopologySP(db);
            var results = physicalTopologySp.GetFloorMonitorRange();
            return results.ToWcfModelList();
        }

        /// <summary>
        /// 根据PhysicalTopology的Id获取楼层以下级别的监控范围
        /// </summary>
        /// <returns></returns>
        public IList<PhysicalTopology> GetFloorMonitorRangeById(int id)
        {
            PhysicalTopologySP physicalTopologySp = new PhysicalTopologySP(db);
            var results = physicalTopologySp.GetFloorMonitorRange(id);
            return results.ToWcfModelList();
        }

        /// <summary>
        /// 根据节点修改监控范围
        /// </summary>
        public bool EditMonitorRange(PhysicalTopology pt)
        {
            try
            {
                Area area = db.Areas.Find((i) => i.Id == pt.Id);
                if (area != null && pt.InitBound != null)
                {
                    area.Name = pt.Name;//2019_07_18_cww:添加名称，区域名称可以修改的
                    var transform = pt.Transfrom;

                    //var parent = db.Areas.Find(area.ParentId);//父区域
                    //var parentBound = db.Bounds.Find(parent.InitBoundId);//父区域的范围
                    //if (parent.Type == AreaTypes.楼层 && pt.IsRelative)//加上偏移量
                    //{
                    //    transform.X += parentBound.MinX;
                    //    transform.Z += parentBound.MinY;
                    //}

                    pt.InitBound.SetInitBound(pt.Transfrom);
                    area.SetTransform(pt.Transfrom.ToDbModel());
                    db.Areas.Edit(area);

                    var InitBoundT = pt.InitBound.ToDbModel();
                    var InitBound = db.Bounds.Find(p => p.Id == InitBoundT.Id);
                    InitBound.SetInitBound(InitBoundT);
                    db.Bounds.Edit(InitBound);//修改

                    List<DbModel.Location.AreaAndDev.Point> lst = db.Points.FindAll(p => p.BoundId == InitBound.Id);
                    foreach (var item in InitBoundT.Points)
                    {
                        DbModel.Location.AreaAndDev.Point pi = lst.Find(p => p.Index == item.Index);
                        pi.SetPoint(item.X, item.Y, item.Z);
                    }

                    db.Points.EditRange(lst);

                    TagRelationBuffer.Instance().PuUpdateData();
                    BLL.Buffers.AuthorizationBuffer.Instance(db).PubUpdateData();
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Log.Error(LogTags.DbGet, "EditMonitorRange", "Exception:" + ex);
                return false;
            }

            return true;
        }

        private Location.TModel.Location.AreaAndDev.Bound NewBound(PhysicalTopology pt)
        {
            try
            {
                var bound = new Location.TModel.Location.AreaAndDev.Bound();
                bound.IsRelative = pt.IsRelative;
                bound.SetInitBound(pt.Transfrom);
                return bound;
            }
            catch (System.Exception ex)
            {
                Log.Error(LogTags.DbGet, "NewBound", "Exception:" + ex);
                return null;
            }
        }

        /// <summary>
        /// 根据节点添加子监控范围
        /// </summary>
        public PhysicalTopology AddMonitorRange(PhysicalTopology pt)
        {
            try
            {
                if (pt == null) return null;
                var transform = pt.Transfrom;

                //var parent = db.Areas.Find(pt.ParentId);//父区域
                //var parentBound = db.Bounds.Find(parent.InitBoundId);//父区域的范围
                //if (parent.Type == AreaTypes.楼层 && pt.IsRelative)//加上偏移量
                //{
                //    transform.X += parentBound.MinX;
                //    transform.Z += parentBound.MinY;
                //}

                //return db.Areas.Add(pt.ToDbModel());
                var areaNew = pt.ToDbModel();
                pt.InitBound = NewBound(pt);
                var newTransform = transform.ToDbModel();
                areaNew.SetTransform(newTransform);
                var newBound = pt.InitBound.ToDbModel();
                db.Bounds.Add(newBound);
                areaNew.SetBound(newBound);
                var points = areaNew.InitBound.Points;
                //db.Points.AddRange(points);

                var addR = db.Areas.Add(areaNew);

                if (addR)
                {
                    TagRelationBuffer.Instance().PuUpdateData();
                    BLL.Buffers.AuthorizationBuffer.Instance(db).PubUpdateData();

                    var result = areaNew.ToTModel();

                    //if (parent.Type == AreaTypes.楼层 && pt.IsRelative)//减去偏移量
                    //{
                    //    result.Transfrom.X -= parentBound.MinX;
                    //    result.Transfrom.Z -= parentBound.MinY;
                    //}
                    return result;
                }
                return null;
            }
            catch (Exception e)
            {
                Log.Error(LogTags.DbGet, "AddMonitorRange", e.ToString());
                return null;
            }
        }

        /// <summary>
        /// 删除区域范围
        /// </summary>
        public bool DeleteMonitorRange(PhysicalTopology pt)
        {
            try
            {
                if (pt == null) return false;
                int AreaId = pt.Id;
                Area aa = db.Areas.Find(p => p.Id == AreaId);
                if (aa != null)
                {
                    db.Areas.Remove(aa);
                }

                if (pt.InitBound != null)
                {
                    int BoundId = pt.InitBound.Id;

                    db.Bounds.DeleteById(BoundId);

                    List<DbModel.Location.AreaAndDev.Point> lst = db.Points.FindAll(p => p.BoundId == BoundId);
                    if (lst != null)
                    {
                        db.Points.RemoveList(lst);
                    }
                }

                TagRelationBuffer.Instance().PuUpdateData();
                BLL.Buffers.AuthorizationBuffer.Instance(db).PubUpdateData();
            }
            catch (Exception ex)
            {
                Log.Error(LogTags.DbGet, "DeleteMonitorRange", ex.ToString());
                return false;
            }

            return true;
        }

        /// <summary>
        /// 根据id，获取区域信息
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Area GetAreaById(int id)
        {
            try
            {
                //return db.Areas.Find(i => i.Id == id);
                AreaService asr = new AreaService();
                return asr.GetAreaById(id);
            }
            catch (System.Exception ex)
            {
                Log.Error(LogTags.DbGet, "GetAreaById", ex.ToString());
                return null;
            }
        }

        //AreaService asr = new AreaService();

        public AreaStatistics GetAreaStatistics(int id)
        {
            try
            {
                AreaService asr = new AreaService(db);
                return asr.GetAreaStatistics(id);
            }
            catch (System.Exception ex)
            {
                Log.Error(LogTags.DbGet, "GetAreaStatistics", ex.ToString());
                return null;
            }
        }

        public List<NearbyPerson> GetNearbyPerson_Currency(int id, float fDis)
        {
            try
            {
                PersonService ps = new PersonService();
                List<NearbyPerson> lst = ps.GetNearbyPerson_Currency(id, fDis);
                if (lst == null)
                {
                    lst = new List<NearbyPerson>();
                }

                return lst;
            }
            catch (System.Exception ex)
            {
                Log.Error(LogTags.DbGet, "GetNearbyPerson_Currency", ex.ToString());
                return null;
            }
        }

        public List<NearbyPerson> GetNearbyPerson_Alarm(int id, float fDis)
        {
            try
            {
                PersonService ps = new PersonService();
                List<NearbyPerson> lst = ps.GetNearbyPerson_Alarm(id, fDis);
                if (lst == null)
                {
                    lst = new List<NearbyPerson>();
                }

                return lst;
            }
            catch (System.Exception ex)
            {
                Log.Error(LogTags.DbGet, "GetNearbyPerson_Alarm", ex.ToString());
                return null;
            }
        }


        /// <summary>
        /// 获取品铂设置的定位区域
        /// </summary>
        /// <returns></returns>
        public IList<PhysicalTopology> GetSwitchAreas()
        {
            try
            {
                var switchAreas = db.bus_anchor_switch_area.ToList();

                var subSwitchAreas = switchAreas;

                int ShowFloor = 0;
                if (ShowFloor == 1)//1层
                {
                    subSwitchAreas = switchAreas.FindAll(i => i.min_z == 0 || i.min_z == 150);
                }
                else if (ShowFloor == 2)//2层
                {
                    subSwitchAreas = switchAreas.FindAll(i => i.min_z == 450 || i.min_z == 600);
                }
                else if (ShowFloor == 3)//3层
                {
                    subSwitchAreas = switchAreas.FindAll(i => i.min_z == 880);
                }
                else if (ShowFloor == 4)//4层
                {
                    subSwitchAreas = switchAreas.FindAll(i => i.min_z > 880);
                }
                else
                {

                }

                List<PhysicalTopology> areas = new List<PhysicalTopology>();
                float scale = 100.0f;
                foreach (var item in subSwitchAreas)
                {
                    var switchArea = new PhysicalTopology();
                    //todo:这部分的具体数值其他项目时需要调整。
                    float x1 = item.start_x / scale + 2059;
                    float x2 = item.end_x / scale + 2059;
                    float y1 = item.start_y / scale + 1565;
                    float y2 = item.end_y / scale + 1565;
                    float z1 = item.min_z / scale;
                    float z2 = item.max_z / scale;
                    switchArea.InitBound = new Location.TModel.Location.AreaAndDev.Bound(x1, y1, x2, y2, z1, z2, false);
                    //switchArea.Parent = area;
                    switchArea.Name = item.area_id;
                    switchArea.Type = AreaTypes.SwitchArea;

                    //AddAreaRect(switchArea, null, scale);
                    switchArea.SetTransformM();

                    areas.Add(switchArea);
                }

                return areas;
            }
            catch (System.Exception ex)
            {
                Log.Error(LogTags.DbGet, "GetSwitchAreas", "Exception:" + ex);
                return null;
            }

        }
    }
}
