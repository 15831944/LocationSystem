﻿using CommunicationClass;
using CommunicationClass.SihuiThermalPowerPlant;
using CommunicationClass.SihuiThermalPowerPlant.Models;
using DbModel.BaseData;
using Location.BLL.Tool;
using LocationServer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TModel.BaseData;

namespace WebApiLib.Clients
{
    /// <summary>
    /// 具体从基础信息平台获取信息的代码，从BaseDataClient分离出来
    /// </summary>
    public class BaseDataInnerClient
    {
        private string Message;

        public string BaseUri { get; set; }

        public BaseDataInnerClient(string baseUri)
        {
//#if DEBUG
//            if (System.Diagnostics.Debugger.IsAttached)
//            {
//                baseUri = string.Format("http://{0}:{1}/datacase/api/", AppContext.CurrentHost, AppContext.CurrentPort);
//            }
//#endif

            BaseUri = baseUri;
        }

        public BaseTran<T> GetEntityList<T>(string url,bool nullable=false) where T :new()
        {
            var recv = new BaseTran<T>();
            try
            {
                recv = WebApiHelper.GetEntity<BaseTran<T>>(url);
                if (recv == null)
                {
                    Log.Info(LogTags.BaseData, "BaseDataInnerClient.GetEntityList recv==null:" + url );
                    //return null;
                    recv= new BaseTran<T>();
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                Log.Error(LogTags.BaseData, "BaseDataInnerClient.GetEntityList:"+url+"\n"+Message);
                recv= new BaseTran<T>();
            }

            if (nullable == false)//列表不能为null，因为返回后有要放到foreach里面
            {
                if (recv.data == null)
                {
                    recv.data = new List<T>();
                }
            }

            return recv;
        }

        public List<user> GetUserList()
        {
            BaseTran<user> recv = new BaseTran<user>();
            string path = "users";
            string url = BaseUri + path;
            recv = GetEntityList<user>(url);
            return recv.data;
        }

        public List<zone> GetZoneList()
        {
            string path = "zones?struct=LIST";
            string url = BaseUri + path;
            BaseTran<zone> recv = new BaseTran<zone>();
            recv = GetEntityList<zone>(url);
            if (recv.data == null)
            {
                recv.data = new List<zone>();
            }
            return recv.data;
        }

        public List<org> GetOrgList()
        {
            BaseTran<org> recv = new BaseTran<org>();
            string path = "orgs";
            string url = BaseUri + path;
            recv = GetEntityList<org>(url);
            if (recv.data == null)
            {
                recv.data = new List<org>();
            }
            return recv.data;
        }

        public List<device> GetDeviceList(string types, string code, string name)
        {
            BaseTran<device> recv = new BaseTran<device>();
            string path = "devices";
            string url = BaseUri + path;

            if(types==null && code ==null && name == null)
            {

            }
            else
            {
                if (types != null)
                {
                    url += "?types=" + types;
                }
                else
                {
                    url += "?types";
                }

                if (code == null && name == null)
                {

                }
                else
                {
                    if (code != null)
                    {
                        url += "&code=" + code;
                    }
                    else
                    {
                        url += "&code";
                    }

                    if (name != null)
                    {
                        url += "&name=" + name;
                    }
                    else
                    {
                        url += "&name";
                    }
                }


            }
            

            recv = GetEntityList<device>(url);
            if (recv.data == null)
            {
                recv.data = new List<device>();
            }

            return recv.data;
        }

       


        public T GetEntityDetail<T>(string url)
        {
            T recv = default(T);
            try
            {
                recv = WebApiHelper.GetEntity<T>(url);
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                return default(T);
            }
            return recv;
        }

        /// <summary>
        /// 获取指定区域下设备列表
        /// </summary>
        /// <param name="zoneId"></param>
        /// <returns></returns>
        public List<device> GetZoneDeviceList(int zoneId)
        {
            BaseTran<device> recv = new BaseTran<device>();
            string path = "api/zones/" + zoneId + "/devices";
            string url = BaseUri + path;
            recv = GetEntityList<device>(url);

            if (recv.data == null)
            {
                recv.data = new List<device>();
            }

            return recv.data;
        }

        /// <summary>
        /// 获取设备详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public device GetDeviceDetail(int id)
        {
            device recv = new device();
            string path = "api/devices/" + id;
            string url = BaseUri + path;
            recv = GetEntityDetail<device>(url);
            return recv;
        }

        /// <summary>
        /// 获取指定的两票详情
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public tickets GetTicketsDetail(int id, string begin_date, string end_date)
        {
            tickets recv = new tickets();

            try
            {
                string path = "api/tickets/" + id;
                string url = BaseUri + path;
                if (begin_date != null)
                {
                    url += "?begin_date=" + begin_date;
                }
                else
                {
                    url += "?begin_date";
                }

                if (end_date != null)
                {
                    url += "&end_date=" + end_date;
                }
                else
                {
                    url += "&end_date";
                }

                return GetEntityDetail<tickets>(url);
            }
            catch (Exception ex)
            {
                string messgae = ex.Message;
            }

            return recv;
        }

        /// <summary>
        /// 获取区域详情
        /// </summary>
        /// <param name="id"></param>
        /// <param name="view"></param>
        /// <returns></returns>
        public zone GetZoneDetail(int id, int view)
        {
            zone recv = new zone();
            string path = "zones/" + id + "?view=" + view;
            string url = BaseUri + path;
            recv = GetEntityDetail<zone>(url);

            if (recv == null)
            {
                recv = new zone();
            }

            if (recv.zones == null)
            {
                recv.zones = new List<zone>();
            }

            if (recv.devices == null)
            {
                recv.devices = new List<device>();
            }
            return recv;
        }

        /// <summary>
        /// 获取Sis数据
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public SisData_Compact GetSisData(string url)
        {
            var recv = new SisData_Compact();
            try
            {
                recv = WebApiHelper.GetEntity<SisData_Compact>(url);
                if (recv == null) return null;
                if (recv.schema == null)
                {
                    recv.schema = new sis_compact();
                }

                if (recv.data == null)
                {
                    recv.data = new List<List<string>>();
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                return null;
            }
            return recv;
        }

        /// <summary>
        /// 获取门禁卡列表
        /// </summary>
        /// <returns></returns>
        public List<cards> GetCardList()
        {
            string path = "cards";
            string url = BaseUri + path;
            BaseTran<cards> recv = new BaseTran<cards>();
            recv = GetEntityList<cards>(url);
            return recv.data;
        }

        /// <summary>
        /// 获取门禁卡操作历史
        /// </summary>
        /// <param name="id"></param>
        /// <param name="begin_date"></param>
        /// <param name="end_date"></param>
        /// <returns></returns>
        public List<cards_actions> GetCardActions(int id, string begin_date, string end_date)
        {
            BaseTran<cards_actions> recv = new BaseTran<cards_actions>();
            string path = "cards/" + Convert.ToString(id) + "/actions";
            string url = BaseUri + path;
            if (begin_date != null)
            {
                url += "?begin_date=" + begin_date;
            }
            else
            {
                url += "?begin_date";
            }

            if (end_date != null)
            {
                url += "&end_date=" + end_date;
            }
            else
            {
                url += "&end_date";
            }

            recv = GetEntityList<cards_actions>(url);

            if (recv.data == null)
            {
                recv.data = new List<cards_actions>();
            }

            return recv.data;
        }

        /// <summary>
        /// 获取单台设备操作历史
        /// </summary>
        /// <param name="id"></param>
        /// <param name="begin_date"></param>
        /// <param name="end_date"></param>
        /// <returns></returns>
        public List<devices_actions> GetDeviceActions(int id, string begin_date, string end_date)
        {
            string path = "api/devices/" + id + "/actions";
            string url = BaseUri + path;
            if (begin_date != null)
            {
                url += "?begin_date=" + begin_date;
            }
            else
            {
                url += "?begin_date";
            }

            if (end_date != null)
            {
                url += "&end_date=" + end_date;
            }
            else
            {
                url += "&end_date";
            }

            BaseTran<devices_actions> recv = new BaseTran<devices_actions>();
            recv = GetEntityList<devices_actions>(url);

            if (recv.data == null)
            {
                recv.data = new List<devices_actions>();
            }

            return recv.data;
        }

        private string ParkName = "";
        /// <summary>
        /// 获取SIS传感数据
        /// </summary>
        /// <param name="strTags"></param>
        /// <returns></returns>
        public List<sis> GetSisList(string strTags)
        {
            //string path = "api/rt/sis?kks=" + kks;
            //string url = BaseUri + path;
            //string[] sArray = BaseUri.Split(new string[] { "api" }, StringSplitOptions.RemoveEmptyEntries);
            //string BaseUri2 = sArray[0];
            // BaseUri2 += "api-viz/";
            ParkName = AppContext.ParkName;
            if (ParkName == "中山嘉明电厂")
            {
                string url = BaseUri + "Sis/list/kks/" + strTags;
                int length = url.Length;//max:2083
                List<sis> list = GetEntityList2<sis>(url);
                return list;
            }
            else
            {
                BaseTran<sis> recv = new BaseTran<sis>();
                //  string url = BaseUri + "rt/sis/" + strTags;
                string url = BaseUri + "Sis/list";
                int length = url.Length;//max:2083
                recv = GetEntityList<sis>(url, true);
                //if (recv.data == null)
                //{
                //    recv.data = new List<sis>();
                //}
                return recv.data;
            }
           
        }

        public string GetSisUrl(string tags)
        {
            // string url = BaseUri + "rt/sis/" + tags;
            string url = BaseUri + "Sis/list/kks/";
            return url;
        }

        /// <summary>
        /// 获取告警事件列表
        /// </summary>
        /// <param name="src"></param>
        /// <param name="level"></param>
        /// <param name="begin_t"></param>
        /// <param name="end_t"></param>
        /// <returns></returns>
        public List<events> GetEventList(int? src, int? level, long? begin_t, long? end_t)
        {
            BaseTran<events> recv = new BaseTran<events>();
            string path = "events";
            string url = BaseUri + path;
            if (src != null)
            {
                url += "?src=" + Convert.ToString(src);
            }
            else
            {
                url += "?src";
            }

            if (level != null)
            {
                url += "&level=" + Convert.ToString(level);
            }
            else
            {
                url += "&level";
            }

            if (begin_t != null && end_t != null)
            {
                url += "&begin_t=" + Convert.ToString(begin_t) + "&end_t=" + Convert.ToString(end_t);
            }
            else
            {
                url += "&begin_t" + "&end_t";
            }

            recv = GetEntityList<events>(url);

            if (recv.data == null)
            {
                recv.data = new List<events>();
            }
            return recv.data;
        }

        /// <summary>
        /// 获取两票列表
        /// </summary>
        /// <param name="type">1:操作票；2:工作票</param>
        /// <param name="begin_date">格式：yyyyMMdd 默认为当天</param>
        /// <param name="end_date">跨度最大一个月</param>
        /// <returns></returns>
        public BaseTran<tickets> GetTicketsList(string type, string begin_date, string end_date)
        {
            BaseTran<tickets> recv = new BaseTran<tickets>();
            string path = "api/tickets";
            string url = BaseUri + path;
            QueryArg query = new QueryArg();
            query.Add("type", type);
            query.Add("begin_date", begin_date);
            query.Add("end_date", end_date);
            url += query.GetQueryString();
            recv = GetEntityList<tickets>(url);
            if (recv.data == null)
            {
                recv.data = new List<tickets>();
            }
            return recv;
        }

        #region 巡检
        /// <summary>
        /// 获取巡检节点列表
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public patrols GetPatrolDetail(int patrolId)
        {
            /*
            {"createTime":1568736088,"startTime":1568736000,"endTime":1568764200,"id":"2340","code":"巡检单-14270","name":"#1炉机务巡检路线（运行）","state":"执行中","route":[{"staffCode":"82000007","staffName":"","kksCode":"20190514RHUU","deviceCode":"#1炉辅机间0m区域","deviceName":"","deviceId":110864},{"staffCode":"82000007","staffName":"","kksCode":"201905142V55","deviceCode":"#1炉定排坑区域","deviceName":"","deviceId":110781},{"staffCode":"82000007","staffName":"","kksCode":"20190514WLUU","deviceCode":"#1炉辅机间2层平台","deviceName":"","deviceId":110888},{"staffCode":"82000007","staffName":"","kksCode":"20190514J4YJ","deviceCode":"#1炉辅机间3层平台","deviceName":"","deviceId":110887},{"staffCode":"82000007","staffName":"","kksCode":"20190514OVHO","deviceCode":"#1炉辅机间4层平台","deviceName":"","deviceId":110785},{"staffCode":"82000007","staffName":"","kksCode":"2019051447P1","deviceCode":"#1炉29.48m平台区域","deviceName":"","deviceId":110780},{"staffCode":"82000007","staffName":"","kksCode":"201905141961","deviceCode":"#1炉0m区域","deviceName":"","deviceId":110890}]}
            */
            patrols recv = new patrols();

            try
            {
                string path = "patrols/" + patrolId;
                string url = BaseUri + path;

                recv = GetPatrols(url);

            }
            catch (Exception ex)
            {
                string messgae = ex.Message;
            }

            return recv;

        }

        public patrols GetPatrols(string url)
        {
            patrols recv = new patrols();
            try
            {
                recv = WebApiHelper.GetEntity<patrols>(url);
                if (recv == null) return null;
                if (recv.route == null)
                {
                    recv.route = new List<checkpoints>();
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                return null;
            }
            return recv;

        }

        /// <summary>
        /// 获取巡检结果列表
        /// </summary>
        /// <param name="patrolId"></param>
        /// <param name="deviceCode"></param>
        /// <returns></returns>
        public checkresults Getcheckresults(int patrolId, string deviceId)
        {
            checkresults recv = new checkresults();

            try
            {
                string path = "patrols/" + Convert.ToString(patrolId) + "/checkpoints/" + deviceId + "/results";
                string url = BaseUri + path;

                recv = GetCheckResults(url);
            }
            catch (Exception ex)
            {
                string messgae = ex.Message;
            }

            return recv;

        }

        public checkresults GetCheckResults(string url)
        {
            checkresults recv = new checkresults();
            try
            {
                recv = WebApiHelper.GetEntity<checkresults>(url);
                if (recv == null) return null;
                if (recv.checks == null)
                {
                    recv.checks = new List<results>();
                }
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                return null;
            }
            return recv;

        }


        public checkpoints GetCheckPoints(string url)
        {
            checkpoints recv = new checkpoints();
            try
            {
                recv = WebApiHelper.GetEntity<checkpoints>(url);
                if (recv == null) return null;
                //if (recv.checks == null)
                //{
                //    recv.checks = new List<results>();
                //}
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                return null;
            }
            return recv;

        }

        public List<T> GetEntityList2<T>(string url)
        {
            var recv = new List<T>();
            try
            {
                recv = WebApiHelper.GetEntity<List<T>>(url);
            }
            catch (Exception ex)
            {
                Message = ex.Message;
                return null;
            }
            return recv;
        }

        /// <summary>
        /// 获取巡检轨迹列表
        /// </summary>
        /// <param name="dtBegin"></param>
        /// <param name="dtEnd"></param>
        /// <param name="byTime">值为True，按时间获取，值为false,获取全部</param>
        /// <returns></returns>
        public List<patrols> GetPatrolList(long lBegin, long lEnd, bool byTime)
        {
            List<patrols> recv = new List<patrols>();

            try
            {
                string path = "Patrols/list";
                string url = BaseUri + path;
                //if (byTime)
                //{
                //    url += "?startDate=" + lBegin;
                //    url += "&endDate=" + lEnd;
                //}
                recv = GetEntityList2<patrols>(url);
                recv.Sort((a, b) =>
                {
                    return b.createTimes.CompareTo(a.createTimes);
                });
            }
            catch (Exception ex)
            {
                Message = ex.Message;
            }

            return recv;

        }

        #endregion
    }
}
