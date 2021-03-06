﻿using System.Collections.Generic;
using TEntity = Location.TModel.Location.Data.TagPosition;
using BLL;
using BLL.Blls.Location;
using LocationServices.Converters;
using DbModel.Tools;
using TModel.Tools;
using System;
using Location.TModel.LocationHistory.Data;
using System.Linq;
using System.Runtime.InteropServices.ComTypes;
using Location.TModel.Tools;
using Location.BLL.Tool;

namespace LocationServices.Locations.Services
{
    public interface IPosService : INameEntityService<TEntity>
    {
        IList<TEntity> GetListByPerson(string person);

        IList<TEntity> GetListByArea(string area);
    }
    public class PosService : IPosService
    {
        private Bll db;

        private LocationCardPositionBll dbSet;

        public PosService()
        {
            db = new Bll();
            dbSet = db.LocationCardPositions;
        }

        public PosService(Bll bll)
        {
            this.db = bll;
            dbSet = db.LocationCardPositions;
        }

        public static string tag = "PosService";

        public TEntity Delete(string id)
        {
            try
            {
                var item = dbSet.DeleteById(id);
                return item.ToTModel();
            }
            catch (System.Exception ex)
            {
                Log.Error(tag, "Delete", "Exception:" + ex);
                return null;
            }
        }

        public TEntity GetEntity(string id)
        {
            try
            {
                var item = dbSet.Find(id);

                var tItem = item.ToTModel();
                tItem.Area = db.Areas.Find(item.AreaId).ToTModel();
                var tag = db.LocationCards.FirstOrDefault(i => i.Code == item.Id);
                var tagToPerson = db.LocationCardToPersonnels.FirstOrDefault(i => i.LocationCardId == tag.Id);
                if (tagToPerson != null)
                {
                    tItem.Person = tagToPerson.Personnel.ToTModel();
                    tItem.PersonId = tagToPerson.PersonnelId;
                }
                return tItem;
            }
            catch (System.Exception ex)
            {
                Log.Error(tag, "GetEntity", "Exception:" + ex);
                return null;
            }
        }

        public List<TEntity> GetList()
        {
            try
            {
                var list = dbSet.ToList().ToTModel();
                foreach (var item in list)
                {
                    if (item.Archors != null && item.Archors.Count == 0)
                    {
                        item.Archors = null;
                    }
                }
                return list.ToWCFList();
            }
            catch (Exception ex)
            {
                Log.Error(tag, "GetList", "Exception:" + ex);
                return null;
            }
        }

        public IList<TEntity> GetListByName(string tagCode)
        {
            try
            {
                var devInfoList = dbSet.GetListByTagCode(tagCode).ToTModel();
                return devInfoList.ToWCFList();
            }
            catch (System.Exception ex)
            {
                Log.Error(tag, "GetListByName", "Exception:" + ex);
                return null;
            }
        }

        public IList<TEntity> GetListByPerson(string person)
        {
            try
            {
                var relation = db.LocationCardToPersonnels.DbSet.FirstOrDefault(i => i.PersonnelId + "" == person);
                if (relation == null) return null;
                var tag = db.LocationCards.Find(relation.LocationCardId);
                var devInfoList = dbSet.GetListByTagCode(tag.Code).ToTModel();
                return devInfoList.ToWCFList();
            }
            catch (System.Exception ex)
            {
                Log.Error(tag, "GetListByPerson", "Exception:" + ex);
                return null;
            }
        }

        public IList<TEntity> GetListByArea(string area)
        {
            try
            {
                var devInfoList = dbSet.GetListByArea(area.ToInt()).ToTModel();
                return devInfoList.ToWCFList();
            }
            catch (System.Exception ex)
            {
                Log.Error(tag, "GetListByArea", "Exception:" + ex);
                return null;
            }
        }

        public TEntity Post(TEntity item)
        {
            try
            {
                var dbItem = item.ToDbModel();
                var result = dbSet.Add(dbItem);
                return result ? dbItem.ToTModel() : null;
            }
            catch (System.Exception ex)
            {
                Log.Error(tag, "Post", "Exception:" + ex);
                return null;
            }
        }

        public TEntity Put(TEntity item)
        {
            try
            {
                var dbItem = item.ToDbModel();
                var result = dbSet.Edit(dbItem);
                return result ? dbItem.ToTModel() : null;
            }
            catch (System.Exception ex)
            {
                Log.Error(tag, "Put", "Exception:" + ex);
                return null;
            }
        }

        public bool PutRange(List<TEntity> list)
        {
            try
            {
                var list2 = list.ToDbModel();
                var result = dbSet.EditRange(list2);
                return result;
            }
            catch (System.Exception ex)
            {
                Log.Error(tag, "PutRange", "Exception:" + ex);
                return false;
            }
        }

        /// <summary>
        /// 获取标签实时位置
        /// </summary>
        /// <returns></returns>
        public IList<TEntity> GetRealPositonsByTags(List<string> tagCodes)
        {
            try
            {
                var list = dbSet.DbSet.Where(tag => tagCodes.Contains(tag.Id)).ToList();
                return list.ToWcfModelList();
            }
            catch (Exception ex)
            {
                Log.Error(tag, "GetRealPositonsByTags", "Exception:" + ex);
                return null;
            }
        }
    }
}
