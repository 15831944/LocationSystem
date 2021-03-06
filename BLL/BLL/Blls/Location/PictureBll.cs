﻿using DAL;
using DbModel.Location.AreaAndDev;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BLL.Blls.Location
{
    public class PictureBll : BaseBll<Picture, LocationDb>
    {
        public PictureBll():base()
        {

        }
        public PictureBll(LocationDb db) : base(db)
        {

        }

        protected override void InitDbSet()
        {
            DbSet = Db.Pictures;
        }

        public bool Update(string name,byte[] info)
        {
            bool bReturn = false;
            var pc2 = DbSet.FirstOrDefault(p => p.Name == name);
            if (pc2 == null)
            {
                pc2 = new Picture();
                pc2.Name = name;
                pc2.Info = info;

                bReturn = Add(pc2);
            }
            else
            {
                pc2.Info = info;
                bReturn = Edit(pc2);
            }
            return bReturn;
        }
    }
}
