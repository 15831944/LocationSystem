﻿using DAL;
using DbModel.Location.AreaAndDev;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace BLL.Blls.Location
{
    public class PostBll : BaseBll<Post, LocationDb>
    {
        public PostBll():base()
        {

        }
        public PostBll(LocationDb db) : base(db)
        {

        }

        protected override void InitDbSet()
        {
            DbSet = Db.Posts;
        }


        public List<Post> GetList()
        {
            return DbSet.ToList();
        }
    }
}
