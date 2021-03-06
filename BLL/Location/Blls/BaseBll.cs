﻿using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.Validation;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Location.BLL.Tool;
using Location.DAL;

namespace Location.BLL
{
    public class BaseBll<T>:IDisposable where T : DbContext
    {
        public T Db { get; set; }

        //public BaseBll()
        //{
        //    Db=new LocationDb();
        //}

        public BaseBll(T db)
        {
            Db = db;
        }

        public void Dispose()
        {
            Db.Dispose();
        }
    }

    public abstract class BaseBll<T,TDb>: BaseBll<TDb> 
        where T :class , new()
        where TDb : DbContext, new()
    {
        public DbSet<T> DbSet { get; set; }

        protected abstract void InitDbSet();

        public BaseBll() : base(new TDb())
        {
            InitDbSet();
        }

        public BaseBll(TDb db) : base(db)
        {
            InitDbSet();
        }

        public virtual bool Add(T item,bool isSave=true)
        {
            if (DbSet == null) return false;

            DbSet.Add(item);
            if (isSave)
            {
                return Save();
            }
            else
            {
                return true;
            }
        }

        public bool Save()
        {
            try
            {
                int r = Db.SaveChanges();
                return r > 0;
            }
            catch (DbEntityValidationException ex)
            {
                StringBuilder errors = new StringBuilder();
                IEnumerable<DbEntityValidationResult> validationResult = ex.EntityValidationErrors;
                foreach (DbEntityValidationResult result in validationResult)
                {
                    ICollection<DbValidationError> validationError = result.ValidationErrors;
                    foreach (DbValidationError err in validationError)
                    {
                        errors.Append(err.PropertyName + ":" + err.ErrorMessage + "\r\n");
                    }
                }
                ErrorMessage = errors.ToString();
                //Console.WriteLine(ErrorMessage);
                Log.Error("BaseBll.Save DbEntityValidationException:\n" + ErrorMessage);
                //简写
                //var validerr = ex.EntityValidationErrors.First().ValidationErrors.First();
                //Console.WriteLine(validerr.PropertyName + ":" + validerr.ErrorMessage);

                return false;
            }
            catch (DbUpdateException ex)
            {
                Exception innerEx = ex.InnerException;
                while (innerEx is DbUpdateException)
                {
                    innerEx = ex.InnerException;
                }
                Log.Error("BaseBll.Save DbUpdateException", innerEx);
                ErrorMessage = innerEx.ToString();
                return false;
            }
            catch (Exception ex)
            {
                Log.Error("BaseBll.Save Exception", ex);
                ErrorMessage = ex.ToString();
                return false;
            }
        }

        public string ErrorMessage { get; set; }

        public virtual bool AddRange(List<T> list)
        {
            if (DbSet == null) return false;
            //DbSet.AddRange(list);
            //return Save();
            return AddRange(Db, list);
        }

        public virtual bool AddRange(params T[] list)
        {
            if (DbSet == null) return false;
            //DbSet.AddRange(list);
            //return Save();
            return AddRange(Db, list);
        }

        public virtual T Find(object id)
        {
            if (DbSet == null) return default(T);

            if (id == null)
            {
                return default(T);
            }
            try
            {
                T obj = DbSet.Find(id);
                return obj;
            }
            catch (Exception ex)
            {
                Log.Error("BaseBll.Find", ex);
                return default(T);
            }
        }

        public virtual List<T> ToList(bool isTracking=false)
        {
            if (DbSet == null) return null;
            try
            {
                List<T> list = null;
                if (isTracking)
                {
                    list = DbSet.ToList();
                }
                else
                {
                    list = DbSet.AsNoTracking().ToList();
                }
                return list;
            }
            catch (Exception ex)
            {
                Log.Error("BaseBll.ToList", ex);
                return null;
            }
        }

        public virtual bool DeleteById(object id)
        {
            if (id == null) return false;
            if (DbSet == null) return false;
            try
            {
                T obj = DbSet.Find(id);
                if (obj == null) return false;
                DbSet.Remove(obj);
                return Save();
            }
            catch (Exception ex)
            {
                Log.Error("BaseBll.DeleteById", ex);
                return false;
            }
            
        }

        public bool Remove(T obj,bool isSave=true)
        {
            try
            {
                DbSet.Remove(obj);
                if (isSave)
                {
                    return Save();
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error("BaseBll.Remove", ex);
                return false;
            }
        }

        public bool Clear()
        {
            try
            {
                List<T> list = ToList(true);
                //foreach (T item in list)
                //{
                //    Remove(item, false);
                //}
                //return Save();
                Db.BulkDelete(list);
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("BaseBll.Clear", ex);
                return false;
            }
            
        }

        public virtual bool Edit(T entity,bool isSave=true)
        {
            try
            {
                if (DbSet == null) return false;
                DbEntityEntry<T> entry = Db.Entry<T>(entity);
                entry.State = EntityState.Modified;
                if (isSave)
                {
                    return Save();
                }
                else
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                Log.Error("BaseBll.Edit", ex);
                return false;
            }

        }

        public virtual bool AddRange(TDb Db, IEnumerable<T> list)
        {
            if (list == null)
            {
                return false;
            }
            if (Db == null)
            {
                return false;
            }
            try
            {
                Db.BulkInsert(list);
                
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("BaseBll.AddRange.BulkInsert,Type:{0},Count:{1}", typeof(T),list.Count()), ex);

                try
                {
                    Thread.Sleep(100);
                    Db.BulkInsert(list);
                }
                catch (Exception ex2)
                {
                    Log.Error(string.Format("BaseBll.AddRange.BulkInsert2,Type:{0},Count:{1}", typeof(T), list.Count()), ex2);
                }
                

                return false;
            }

            try
            {
                Db.BulkSaveChanges();
            }
            catch (Exception ex)
            {
                Log.Error(string.Format("BaseBll.AddRange.BulkSaveChanges,Type:{0},Count:{1}", typeof(T), list.Count()), ex);
                return false;
            }
            return true;
        }

        public virtual bool EditRange(TDb Db, List<T> list)
        {
            try
            {
                if (Db == null)
                {
                    return false;
                }

                Db.BulkUpdate(list);
                Db.BulkSaveChanges();

                return true;
            }
            catch (Exception ex)
            {
                Log.Error("BaseBll.EditRange", ex);
                return false;
            }
        }
    }
}
