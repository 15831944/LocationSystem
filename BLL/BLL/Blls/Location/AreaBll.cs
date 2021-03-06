﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Core.Objects;
using System.Data.Entity.Infrastructure;
using System.Linq;
using DAL;
using DbModel.Location.AreaAndDev;
using Location.BLL.Tool;

namespace BLL.Blls.Location
{
    public class AreaBll : BaseBll<Area, LocationDb>
    {
        public AreaBll():base()
        {

        }
        public AreaBll(LocationDb db) : base(db)
        {

        }

        protected override void InitDbSet()
        {
            DbSet = Db.Areas;
        }

        public Area GetRoot()
        {
            return DbSet.FirstOrDefault();
        }

        public Area FindByName(string name)
        {
            return DbSet.FirstOrDefault(i => i.Name == name);
        }

        public List<Area> FindListByName(string name)
        {
            var list = DbSet.Where(i => i.Name.Contains(name)).ToList();
            foreach(var item in list)
            {
                item.Children = null;//todo:Find一个不会获取Children，但是用Contains查找会有，为什么呢？要再看看EF的书。
            }
            return list;
        }

        public List<Area> FindListByPid(int pid)
        {
            return DbSet.Where(i => i.ParentId==pid).ToList();
        }

        public List<Area> GetWithBoundPoints(bool withPoints)
        {
            //var bounds = Db.Bounds.ToList();
            var bounds = Db.Bounds.AsNoTracking().ToList();

            List<Area> list = ToList();
            foreach (var area in list)
            {
                if (area.InitBound == null)
                { area.InitBound = bounds.Find(i => i.Id == area.InitBoundId); }
                if (area.EditBound == null)
                { area.EditBound = bounds.Find(i => i.Id == area.EditBoundId); }
                if (area.Children == null)
                { area.Children = list.FindAll(i => i.ParentId == area.Id); }
                if (area.Parent == null)
                {
                    area.Parent = list.Find(i => i.Id == area.ParentId);
                }
            }

            if (withPoints)
            {
                var points = Db.Points.ToList();
                foreach (var bound in bounds)
                {
                    if (bound.Points == null)
                        bound.Points = points.FindAll(i => i.BoundId == bound.Id);
                }
            }
            return list;
        }

        /// <summary>
        /// 获取所有的子区域，和自身
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<int?> GetAllSubAreas(int id)
        {
            List<int?> lst = new List<int?>();
            var areaList = ToList();
            var a1 = areaList.Find(p => p.Id == id);
            if (a1 == null)
            {
                return lst;
            }

            lst.Add(a1.Id);
            List<int?> lstRecv = GetSubAreas(a1.Id, areaList);
            if (lstRecv != null || lstRecv.Count > 0)
            {
                lst.AddRange(lstRecv);
            }
            return lst;
        }
        private List<int?> GetSubAreas(int id, List<Area> areaList)
        {
            List<int?> lst = new List<int?>();
            List<Area> alist2 = areaList.FindAll(p => p.ParentId == id);
            List<int?> lstRecv;
            if (alist2 == null || alist2.Count <= 0)
            {
                return lst;
            }

            foreach (Area item in alist2)
            {
                lst.Add(item.Id);
                lstRecv = GetSubAreas(item.Id, areaList);
                if (lstRecv != null || lstRecv.Count > 0)
                {
                    lst.AddRange(lstRecv);
                }
            }

            return lst;
        }

        //public override bool Edit(Area newArea, bool isSave = true)
        //{
        //    try
        //    {
        //        ErrorMessage = "";
        //        if (DbSet == null) return false;

        //        var bound = newArea.InitBound;

        //        newArea.InitBound = null;

        //        //RemoveHoldingEntityInContext(entity);//不知道为什么不起效果
        //        DbEntityEntry<Area> entryArea = Db.Entry<Area>(newArea);
        //        var state1 = entryArea.State;

        //        entryArea.State = EntityState.Modified;
               
        //        var state2 = entryArea.State;
        //        var result = Save(isSave);
        //        ObjectContext objContext = ((IObjectContextAdapter)Db).ObjectContext;
        //        objContext.Detach(newArea);//修改完后就从Context中删除

        //        var state3 = entryArea.State;

        //        if (bound != null)
        //        {
        //            DbEntityEntry<Bound> entryBound = Db.Entry<Bound>(bound);
        //            var state12 = entryBound.State;

        //            entryBound.State = EntityState.Modified;

        //            var result2 = Save(isSave);
        //            var state22 = entryBound.State;

        //            objContext.Detach(bound);//修改完后就从Context中删除
        //            var state33 = entryBound.State;
        //        }

        //        return result;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log.Error("BaseBll.Edit", ex);
        //        ErrorMessage = ex.ToString();
        //        return false;
        //    }
        //}
    }
}
