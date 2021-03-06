﻿using CommunicationClass.ExtremeVision;
using Location.TModel.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using Location.BLL.Tool;

namespace WebApiCommunication.ExtremeVision
{
    /// <summary>
    /// 摄像头行为分析告警
    /// </summary>
    [DataContract]
    public class CameraAlarmInfo:IComparable<CameraAlarmInfo>
    {

        [DataMember]
        [NotMapped]
        public string DevName { get; set; }

        [DataMember]
        [NotMapped]
        public string DevIp { get; set; }

        [DataMember]
        [NotMapped]
        public int DevID { get; set; }
        /// <summary>
        /// 告警类型:1:安全帽 2:火焰 3:烟雾
        /// </summary>
        [DataMember]
        [NotMapped]
        public int AlarmType { get; set; }

        /// <summary>
        /// id
        /// </summary>
        //[JsonIgnore]//客户端需要这个，作为唯一告警ID
        [DataMember]
        public int id { get; set; }

        /// <summary>
        /// 相同告警合并，但是记录一下一系列告警的开始的数据，最终给的是结束的数据
        /// </summary>
        [JsonIgnore]
        [DataMember]
        public CameraAlarmInfo startInfo { get; set; }


        [JsonIgnore]
        [DataMember]
        public DateTime time { get; set; }

        /// <summary>
        /// 算法ID，由极视角定义的算法ID，只读
        /// </summary>
        [JsonProperty("aid")]//因为三维客户端那边自动生成的类里面没有JsonProperty特性，属性名还是和json中的一样好了
        [DataMember]
        public int aid { get; set; }

        /// <summary>
        /// 摄像头ID
        /// 存在两种情况：
        /// 1. 由算法配置工具自动生成，不建议修改；
        /// 2. 算法内自定义的摄像头ID，采用string格式，可以自定义，建议使用数字
        /// </summary>
        [JsonProperty("cid")]
        [DataMember]
        [MaxLength(256)]
        public string cid { get; set; }

        /// <summary>
        /// 摄像头对应拉流地址/rtsp
        /// </summary>
        [JsonProperty("cid_url")]
        [DataMember]
        [MaxLength(512)]
        public string cid_url { get; set; }

        /// <summary>
        /// 算法服务器时间戳，unix标准时间戳格式
        /// </summary>
        [JsonProperty("time_stamp")]
        [DataMember]
        public long time_stamp { get; set; }


        /// <summary>
        /// 状态值，0/无报警，1/有报警
        /// </summary>
        [JsonProperty("status")]
        [DataMember]
        public int status { get; set; }

        /// <summary>
        /// 报警图片命名
        /// 格式为“时间（精确到秒）_us（微秒）_cid（摄像头ID）_fix（输入或输出）.jpg”，例：20180719121005_266236_3_out.jpg
        /// </summary>
        [JsonProperty("pic_name")]
        [DataMember]
        [MaxLength(256)]
        public string pic_name { get; set; }

        /// <summary>
        /// 报警结果图片，base64格式编码
        /// </summary>
        [JsonProperty("pic_data")]
        [DataMember]
        //[NotMapped]
        public string pic_data { get; set; }

        /// <summary>
        /// 算法返回的数据
        /// </summary>
        [JsonProperty("data")]
        [NotMapped]
        public object data { get; set; }


        public int ParseType()
        {
            string json = data + "";
            if (AlarmType == 0)
            {
                if (json.Contains("headInfo"))
                {
                    AlarmType = 1;
                }
                else if (json.Contains("flameInfo"))
                {
                    AlarmType = 2;
                }
                else if (json.Contains("smogInfo"))
                {
                    AlarmType = 3;
                }
                else
                {
                    AlarmType = 1;//这里也可以是0
                }
            }
            return AlarmType;
        }

        public int ParseType1()
        {
            int id = aid;
            if (id == 9510)
            {
                AlarmType = 1;
            }
            else if (id == 9610)
            {
                AlarmType = 2;
            }
            else if (id == 9620)
            {
                AlarmType = 3;

            }
            else
            {
                AlarmType = 1;
            }
            return AlarmType;
        }



        [DataMember]
        public FlameData FlameData { get; set; }

        [DataMember]
        public HeadData HeadData { get; set; }

        [DataMember]
        public SmogData SmogData { get; set; }

        [DataMember]
        [NotMapped]
        public string Error { get; set; }

        public override string ToString()
        {
            return string.Format("Id:{0},Cid:{1} time:{2}", aid, cid,time);
        }
        public static string RecordErrorStr = "";
        public static string RecordCorrectStr = "";
        public FlameData GetFlameData()
        {
            try
            {
                var data = GetData<FlameData>();
                if (data == null)
                {
                    data = new FlameData();//客户端靠这个判断，就算解析错了也不能是空
                }
                else if (data.flameInfo == null)
                {
                    var data1 = GetData<FlameNewData>();
                    if (data1 == null)
                    {
                        Log.Error("GetFlameData", "data1(FlameNewData) == null:" + data);
                    }
                    else
                    {
                        data.alertFlag = data1.alert_flag;
                        data.numOfFlameRects = 1;
                        data.message = "";
                        List<RectInfo> list = new List<RectInfo>();
                        List<RectOneInfo> newList = data1.alert_info;
                        if (newList!=null&&newList.Count > 0)
                        {
                            for (int i = 0; i < newList.Count; i++)
                            {
                                RectOneInfo info1 = newList[i];
                                if (info1 == null) continue;
                                RectInfo info2 = new RectInfo();
                                info2.x = info1.x;
                                info2.y = info1.y;
                                info2.width = info1.width;
                                info2.height = info1.width;
                                list.Add(info2);
                            }
                            data.flameInfo = list;
                        }
                        else
                        {

                        }
                    }

                }
                return data;
            }
            catch (Exception ex)
            {
                Log.Error("GetFlameData",ex.ToString());
                return new FlameData();
            }
            
        }


        public SmogData GetSmogData()
        {
            var data = GetData<SmogData>();
            if (data == null)
            {
                data = new SmogData();//客户端靠这个判断，就算解析错了也不能是空
            }
            return data;
        }

        private bool IsHeadInfo()
        {
            if (data is JArray)//9510安全帽识别算法回调参数说明.md 协议
            {
                JArray arr = data as JArray;
                foreach (JToken child in arr.First.Children())
                {
                    var property = child as JProperty;
                    if (property == null) continue;
                    //var str = property + "";
                    var name = (property).Name;
                    if (name == "headInfo")
                    {
                        return true;
                    }
                }
                return false;
            }
            else if (data is JObject)//实际收到的
            {
                JObject obj = data as JObject;
                foreach (JToken child in obj.Children())
                {
                    var property = child as JProperty;
                    if (property == null) continue;
                    //var str = property + "";
                    var name = (property).Name;
                    if (name == "headInfo")
                    {
                        return true;
                    }
                }
                return false;

            }
            return false;
        }

        private T GetData<T>()
        {
            try
            {

                if (data is JArray)
                {
                    JArray obj = data as JArray;
                    var result = obj.ToObject<T[]>();
                    return result[0];
                }
                else if (data is JObject)
                {
                    JObject obj = data as JObject;
                    var result = obj.ToObject<T>();
                    return result;
                }
            }
            catch (Exception ex)
            {
                Error = ex.ToString();
                return default(T);
            }
            return default(T);
        }

        public HeadData GetHeadData()
        {
            var data= GetData<HeadData>();
            if (data == null)
            {
                data=new HeadData();//客户端靠这个判断，就算解析错了也不能是空
            }
            return data;
        }

        public void ParseData()
        {
            if (AlarmType == 0)//以前的没有获取过告警信息的，另外AlarmType是不存到数据库里面的
            {

            }

            if (AlarmType==1)//判断是不是头盔告警
            {
                HeadData = GetHeadData();//解析头盔数据
            }
            else if (AlarmType == 2)
            {
                FlameData = GetFlameData();//解析火焰数据
            }
            else if (AlarmType == 3)
            {
                SmogData = GetSmogData();
            }
            else
            {
                HeadData = GetHeadData();//解析头盔数据
            }
        }

        public static CameraAlarmInfo Parse(string json)
        {
            var info = JsonConvert.DeserializeObject<CameraAlarmInfo>(json);
            //string dataText = info.data + "";

            info.ParseType1();
            info.ParseData();
            
            //info.time = info.time_stamp.ToDateTime(true);
            return info;
        }

        public int CompareTo(CameraAlarmInfo other)
        {
            return other.GetCompareId().CompareTo(this.GetCompareId());
        }

        public string GetCompareId()
        {
            return ("" + time_stamp);
        }
    }
}
