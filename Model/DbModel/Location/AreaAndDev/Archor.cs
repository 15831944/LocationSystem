﻿using DbModel.Tools;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;
using Location.TModel.Tools;
using Location.IModel;

namespace DbModel.Location.AreaAndDev
{
    /// <summary>
    /// 基站 //todo:英文写错了，应该是anchor
    /// </summary>
    public class Archor:IEntity
    {
        /// <summary>
        /// 基站Id
        /// </summary>
        [DataMember]
        [Display(Name = "基站Id")]
        public int Id { get; set; }

        /// <summary>
        /// 基站编号
        /// </summary>
        [DataMember]
        [Display(Name = "基站编号")]        
        [MaxLength(16)]
        public string Code { get; set; }

        public string GetCode()
        {
            if (string.IsNullOrEmpty(Code))
            {
                return "Code_" + Id;
            }
            return Code;
        }

        /// <summary>
        /// 基站IP
        /// </summary>
        [DataMember]
        [Display(Name = "基站IP")]
        [MaxLength(16)]
        public string Ip { get; set; }

        /// <summary>
        /// 基站名
        /// </summary>
        [DataMember]
        [Display(Name = "基站名")]
        [Required]
        [MaxLength(128)]
        public string Name { get; set; }

        ///// <summary>
        ///// 区域
        ///// </summary>
        //[DataMember]
        //[Display(Name = "区域")]
        //public int AreaId { get; set; }
        //public virtual Area Area { get; set; }

        /// <summary>
        /// 所在区域的Id,要和DevInfo.ParentId相同
        /// </summary>
        [DataMember]
        public int? ParentId { get; set; }

        [NotMapped]
        public Area Parent { get; set; }

        /// <summary>
        /// 位置X
        /// </summary>
        [DataMember]
        [Display(Name = "位置X")]
        public double X { get; set; }

        /// <summary>
        /// 位置Y
        /// </summary>
        [DataMember]
        [Display(Name = "位置Y")]
        public double Y { get; set; }

        /// <summary>
        /// 位置Z (?)
        /// </summary>
        [DataMember]
        [Display(Name = "位置Z")]
        public double Z { get; set; }

        /// <summary>
        /// 类型 : 0 副、1 主
        /// </summary>
        [DataMember]
        [Display(Name = "基站类型")]
        public ArchorTypes Type { get; set; }

        /// <summary>
        /// 自动获取IP
        /// </summary>
        [DataMember]
        [Display(Name = "自动获取IP")]
        public bool IsAutoIp { get; set; }

        /// <summary>
        /// 服务器IP
        /// </summary>
        [DataMember]
        [Display(Name = "服务器IP")]
        [MaxLength(16)]
        public string ServerIp { get; set; }

        /// <summary>
        /// 端口
        /// </summary>
        [DataMember]
        [Display(Name = "端口")]
        public int ServerPort { get; set; }

        /// <summary>
        /// 发射功率
        /// </summary>
        [DataMember]
        [Display(Name = "发射功率")]
        public double Power { get; set; }

        /// <summary>
        /// 心跳间隔
        /// </summary>
        [DataMember]
        [Display(Name = "心跳间隔")]
        public double AliveTime { get; set; }

        /// <summary>
        /// 是否启动，0否，1是
        /// </summary>
        [DataMember]
        [Display(Name = "是否启动")]
        public IsStart Enable { get; set; }

        /// <summary>
        /// 基站对应的设备主键Id
        /// </summary>
        [DataMember]
        [Display(Name = "基站对应的设备主键Id")]
        public int DevInfoId { get; set; }

        [Display(Name = "基站对应的设备")]
        public virtual DevInfo DevInfo { get; set; }

        public override string ToString()
        {
            return Name;
        }

        public Archor Clone()
        {
            Archor copy = new Archor();
            copy = this.CloneObjectByBinary();
            if (this.DevInfo != null)
            {
                copy.DevInfo = this.DevInfo;
            }

            return copy;
        }

        public Archor()
        {

        }

        public Archor(DevInfo dev)
        {
            //_archor = new Archor();
            Code = "Dev_" + dev.Id;
            Id = dev.Id;
            Name = dev.Name;
            ParentId = dev.ParentId;
            X = dev.PosX;
            Y = dev.PosY;
            Z = dev.PosZ;
            DevInfoId = dev.Id;
        }
    }

    public class ArchorInfo
    {
        public int Id { get; set; }

        public string Code { get; set; }

        public string Ip { get; set; }

        public int? ParentId { get; set; }

        public Area Parent { get; set; }

        public int? DevId { get; set; }
    }
}
