﻿using Location.BLL.Blls;
using Location.BLL.topviewxpBlls;
using Location.DAL;
using Location.Model;
using Location.Model.topviewxp;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Serialization.Formatters;
using System.Text;
using System.Threading.Tasks;

namespace Location.BLL
{

    //修改历史数据和实时数据用的代码
    public partial class TestBll
    {

        /// <summary>
        /// 根据标签信息初始化实时位置信息表，这样跟定位引擎对接时就不用每次都判断是否是新增还是修改了
        /// </summary>
        public List<Tag> InitTagPosition(int mockPowerCount)
        {
            List<Tag> tags = Tags.ToList();
            if (tags == null) tags = new List<Tag>();

             var mockTags = GetMockTags(mockPowerCount, tags); //生成模拟数据，测试大数据量，mockPowerCount = 100的话，2个变成200个
            tags.AddRange(mockTags);

            AddTagPositionsByTags(tags);

            return tags;
        }

        public void AddTagPositionsByTags(List<Tag> tags)
        {
            List<TagPosition> tagPosList = TagPositions.ToList();//事先取出全部到内存中，比每次都到数据库中查询快很多。 100个从6.4s->1.8s,1.8s中主要是第一次查询的一些初始工作
            List<TagPosition> newPosList = new List<TagPosition>();

            foreach (Tag tag in tags)
            {
                //TagPosition tagPos = TagPositions.FindByCode(tag.Code);//100个要2s
                TagPosition tagPos = tagPosList.Find(i => i.Tag == tag.Code);//判断是否存在实时数据
                if (tagPos == null)
                {
                    TagPosition tagPosition = new TagPosition(tag.Code);
                    newPosList.Add(tagPosition);
                }
            }

            //TagPositions.Db.BulkInsert(newPosList);//插件Z.EntityFramework.Extensions功能
            //TagPositions.Db.BulkSaveChanges();

            foreach (TagPosition tp in newPosList)
            {
                TagPositions.Add(tp);
            }
        }

        /// <summary>
        /// 生成模拟数据，测试大数据量，mockPowerCount=100的话，2个变成200个
        /// </summary>
        /// <param name="mockPowerCount"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        private static List<Tag> GetMockTags(int mockPowerCount, List<Tag> tags)
        {
            List<Tag> mockTags = new List<Tag>();
            if (tags == null) return mockTags;
            foreach (Tag tag in tags)
            {
                for (int i = 0; i < mockPowerCount; i++)
                {
                    Tag mockTag = new Tag();
                    mockTag.Code = tag.Code + i;
                    mockTag.Name = tag.Name + i;
                    mockTag.Describe = "模拟数据" + i;
                    mockTags.Add(mockTag);
                }
            }
            return mockTags;
        }

        public bool AddPositions(List<Position> positions)
        {
            bool r = true;
            try
            {
                if (positions == null || positions.Count == 0) return false;              

                //string sql = GetInsertSql(positions);
                //if (!string.IsNullOrEmpty(sql))
                //    DbHistory.Database.ExecuteSqlCommand(sql);

                //List<TagPosition> tagPosList = EditTagPositionList(positions);
                //string sql2 = GetUpdateSql(tagPosList);
                //if (!string.IsNullOrEmpty(sql2))
                //    Db.Database.ExecuteSqlCommand(sql2);

                //todo:获取位置信息参与计算的基站
                foreach (Position position in positions)
                {
                    if (position.Archors != null)
                    {
                        List<Archor> archorList = Archors.FindByCodes(position.Archors);
                        foreach (Archor archor in archorList)
                        {
                            //基站位置和Position位置相等（0.1是为了应对Double类型比较，可能出现的误差）
                            if (Math.Abs(archor.Y - position.Y) < 0.1f)
                            {
                                //找到对应ID,不往后找
                                position.TopoNodeId = archor.Dev.ParentId;
                                break;
                            }
                            //if (!position.TopoNodes.Contains(archor.Dev.ParentId))
                            //    position.TopoNodes.Add(archor.Dev.ParentId);
                        }
                    }
                    else
                    {
                        position.TopoNodeId = null;
                    }
                    //Todo:找不到合适的ID,需要处理一下


                    //foreach (string code in position.Archors)
                    //{
                    //    Archor archor=Archors.FindByCode(code);
                    //    if(!position.TopoNodes.Contains(archor.Dev.ParentId))
                    //        position.TopoNodes.Add(archor.Dev.ParentId);
                    //}
                }
                //1.批量插入历史数据数据
                DbHistory.BulkInsert(positions);//插件Z.EntityFramework.Extensions功能
                //2.获取并修改列表
                List<TagPosition> tagPosList = EditTagPositionList(positions);
                //3.更新列表
                TagPositions.Db.BulkUpdate(tagPosList);//插件Z.EntityFramework.Extensions功能
            }
            catch (Exception ex)
            {
                r = false;
            }
            return r;
        }

        private string GetInsertSql(List<Position> positions)
        {
            string sql = "";
            foreach (Position p in positions)
            {
                if (p == null) continue;
                sql +=
                    string.Format(
                        "insert into Positions (Tag,X,Y,Z,Time,Power,Number,Flag) values ( '{0}','{1}','{2}','{3}','{4}','{5}','{6}','{7}') ",
                        p.Tag, p.X, p.Y, p.Z, p.Time, p.Power, p.Number, p.Flag);
            }
            return sql;
        }

        private string GetUpdateSql(List<TagPosition> positions)
        {
            string sql = "";
            foreach (TagPosition p in positions)
            {
                if (p == null) continue;
                sql +=
                    string.Format(
                        "UPDATE TagPositions SET X = '{1}',Y ='{2}',Z='{3}',Time='{4}',Power='{5}',Number='{6}',Flag='{7}' where Tag='{0}' ",
                        p.Tag, p.X, p.Y, p.Z, p.Time, p.Power, p.Number, p.Flag);
            }
            return sql;
        }

        public async Task AddPositionsAsyc(List<Position> positions)
        {

            string sql = GetInsertSql(positions);
            if (!string.IsNullOrEmpty(sql))
                await DbHistory.Database.ExecuteSqlCommandAsync(sql);
            List<TagPosition> tagPosList = EditTagPositionList(positions);
            string sql2 = GetUpdateSql(tagPosList);
            if(!string.IsNullOrEmpty(sql2))
                await Db.Database.ExecuteSqlCommandAsync(sql2);

            //批量插入历史数据数据
            //await DbHistory.BulkInsertAsync(positions); //插件Z.EntityFramework.Extensions功能

            //获取并修改列表
            //List<TagPosition> tagPosList = EditTagPositionList(positions);

            ////更新列表
            //await TagPositions.Db.BulkUpdateAsync(tagPosList);//插件Z.EntityFramework.Extensions功能
        }

        private List<TagPosition> EditTagPositionList(List<Position> positions)
        {
            //1.获取列表
            List<TagPosition> tagPosList = TagPositions.ToList();
            List<TagPosition> changedTagPosList = new List<TagPosition>();
            //2.修改数据
            for (int i = 0; i < positions.Count; i++)
            {
                Position position = positions[i];
                if (position == null) continue;//位置信息可能有null
                TagPosition tagPos = tagPosList.Find(item => item.Tag == position.Tag);
                if (tagPos == null) continue;
                tagPos.Edit(position);
                if (!changedTagPosList.Contains(tagPos))
                {
                    changedTagPosList.Add(tagPos);
                }
            }
            return changedTagPosList;
        }

        public bool EditTagPositionEx(Position position)
        {
            TagPosition tagPos = TagPositions.FindByCode(position.Tag);//判断是否存在实时数据
            if (tagPos == null)
            {
                TagPosition tagPosition = new TagPosition(position);
                if (TagPositions.Add(tagPosition))//添加新的实时数据
                {
                    return true;
                }
                else
                {
                    ErrorMessage = Position.ErrorMessage;
                    return false;
                }
            }
            else
            {
                tagPos.Edit(position);
                if (TagPositions.Edit(tagPos, false))//修改实时数据
                {
                    return true;
                }
                else
                {
                    ErrorMessage = Position.ErrorMessage;
                    return false;
                }
            }
        }

        public bool AddPosition(Position position)
        {
            bool r = false;
            if (position == null)
            {
                r = false;
            }
            if (Position.Add(position))//添加历史数据
            {
                r = EditTagPosition(position);//修改实时数据
                //return true;
            }
            else
            {
                ErrorMessage = Position.ErrorMessage;
                r = false;
            }
            //return EditTagPosition(position);//修改实时数据
            return r;
        }

        public bool EditTagPosition(Position position)
        {
            TagPosition tagPos = TagPositions.FindByCode(position.Tag);//判断是否存在实时数据
            if (tagPos == null)
            {
                TagPosition tagPosition = new TagPosition(position);
                if (TagPositions.Add(tagPosition))//添加新的实时数据
                {
                    return true;
                }
                else
                {
                    ErrorMessage = Position.ErrorMessage;
                    return false;
                }
            }
            else
            {
                tagPos.Edit(position);
                if (TagPositions.Edit(tagPos))//修改实时数据
                {
                    return true;
                }
                else
                {
                    ErrorMessage = Position.ErrorMessage;
                    return false;
                }
            }
        }

        public string ErrorMessage { get; set; }
        
    }
}
