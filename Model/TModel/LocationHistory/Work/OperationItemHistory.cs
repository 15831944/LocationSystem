﻿using DbModel.Tools;
using System;
using System.Runtime.Serialization;
using Location.TModel.Tools;

namespace TModel.LocationHistory.Work
{
    [DataContract]
    public class OperationItemHistory
    {
        /// <summary>
        /// 操作项Id
        /// </summary>
        [DataMember]
        //[Display(Name = "操作项Id")]
        public int Id { get; set; }

        /// <summary>
        /// 操作票Id
        /// </summary>
        [DataMember]
        //[Display(Name = "操作票Id")]
        public int? TicketId { get; set; }

        /// <summary>
        /// 操作时间
        /// </summary>
        [DataMember]
        //[Display(Name = "操作时间")]
        public DateTime OperationTime { get; set; }

        /// <summary>
        /// 记号
        /// </summary>
        [DataMember]
        //[Display(Name = "记号")]
        public bool Mark { get; set; }

        /// <summary>
        /// 顺序
        /// </summary>
        [DataMember]
        //[Display(Name = "顺序")]
        public int OrderNum { get; set; }

        /// <summary>
        /// 操作项目
        /// </summary>
        [DataMember]
        //[Display(Name = "操作项目")]
        public string Item { get; set; }

        public OperationItemHistory Clone()
        {
            OperationItemHistory copy = new OperationItemHistory();
            copy = this.CloneObjectByBinary();

            return copy;
        }
    }
}
