﻿//------------------------------------------------------------------------------
// <auto-generated>
//     此代码已从模板生成。
//
//     手动更改此文件可能导致应用程序出现意外的行为。
//     如果重新生成代码，将覆盖对此文件的手动更改。
// </auto-generated>
//------------------------------------------------------------------------------

namespace Location.Model.topviewxp
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Runtime.Serialization;
    [DataContract]
	public partial class t_SetModel
    {
        [DataMember]
        [Key]
        public string RecordID { get; set; }
        [DataMember]
        public string Name { get; set; }
        [DataMember]
        public string strText { get; set; }
        [DataMember]
        public string ID { get; set; }
        [DataMember]
        public string strType { get; set; }
        [DataMember]
        public string Visible { get; set; }
        [DataMember]
        public string Items { get; set; }
        [DataMember]
        public string Class { get; set; }
        [DataMember]
        public string Obligate1 { get; set; }
        [DataMember]
        public string Obligate2 { get; set; }
    }
}
