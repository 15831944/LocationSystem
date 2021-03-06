﻿using DbModel.Tools;
using Location.BLL.Tool;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Coldairarrow.Util.Sockets
{
    public class BUDPGram
    {
        public Byte[] data;
        public IPEndPoint iep;

        public BUDPGram()
        {
            data = null;
            iep = new IPEndPoint(IPAddress.Any, 0);
        }

        public BUDPGram(Byte[] data, IPEndPoint iep)
        {
            this.data = data;
            this.iep = iep;
        }
    }

    public delegate void DGramRecievedHandle(object sender, BUDPGram dgram);

    public delegate void DGramListRecievedHandle(object sender, List<BUDPGram> dgram);

    /// <summary>
    /// 多播的地址是特定的，D类地址用于多播，即224.0.0.0至239.255.255.255之间的IP地址，并被划分为局部连接多播地址、预留多播地址和管理权限多播地址3类：
    /// 1、局部多播地址：在224.0.0.0～224.0.0.255之间，这是为路由协议和其他用途保留的地址，路由器并不转发属于此范围的IP包。
    /// 2、预留多播地址：在224.0.1.0～238.255.255.255之间，可用于全球范围（如Internet）或网络协议。
    /// 3、管理权限多播地址：在239.0.0.0～239.255.255.255之间，可供组织内部使用，类似于私有IP地址，不能用于Internet，可限制多播范围。
    /// </summary>
    public class LightUDP
    {
        public bool Inited
        {
            get { return udpc != null; }
        }
        private UdpClient udpc = null;
        // private IPAddress localIP = IPAddress.Any;
      //  private IPAddress localIP = IPAddress.Parse("127.0.0.1");
        private int localPort = 0;
        private BackgroundWorker recieveBW = null;
        private IPEndPoint broadCastEp = null;

        //接收间隔（毫秒）
        public int recvInterval = 50;

        //所有的IP包都有一个“生存时间”（time-to-live）,TTL
        //TTL指定一个包到达目的地之前跳过网络（路由）的最大次数
        //设置为1表示只能发到本地网络的计算机，设置为2表示只能穿过一个路由
        //默认值128
        public short Ttl
        {
            get
            {
                if (udpc != null)
                {
                    return udpc.Ttl;
                }
                else
                {
                    return 0;
                }
            }
            set
            {
                if (udpc != null)
                {
                    udpc.Ttl = value;
                }
            }
        }

        public event DGramRecievedHandle DGramRecieved;  //接收到信息的事件

        public event DGramListRecievedHandle DGramListRecieved;  //接收到信息的事件

        public LightUDP(string ip,int port):this(IPAddress.Parse(ip),port)
        {

        }

        /// <summary>
        /// 绑定监听地址，目前默认本地IP127.0.0.1
        /// </summary>
        /// <param name="localPort"></param>
        public LightUDP(IPAddress localIP,  int localPort)
        {
            Init(localIP, localPort);
        }

        public LightUDP()
        {
            
        }

        public static LightUDP Create(string ip, int port)
        {
            LightUDP udp = new LightUDP();
            for (int i = 0; i < 10; i++)
            {
                bool r = udp.Init(IPAddress.Parse(ip), port + i);
                if (r == true)
                {
                    return udp;
                }
            }
            return udp;

        }

        public static LightUDP Create(IPAddress localIP, int localPort)
        {
            LightUDP udp = new LightUDP();
            for (int i = 0; i < 10; i++)
            {
                bool r = udp.Init(localIP, localPort+i);
                if(r== true)
                {
                    return udp;
                }
            }
            return udp;
            
        }

        public bool Init(IPAddress localIP, int localPort)
        {
            try
            {
                this.localPort = localPort;
                IPEndPoint localEp = new IPEndPoint(localIP, localPort);
                udpc = new UdpClient(localEp);
                broadCastEp = new IPEndPoint(IPAddress.Broadcast, 0);
                InitReceive();
                return true;
            }
            catch (Exception ex)
            {
                Log.Error("LightUDP:"+ ex);
                return false;
            }
        }

        public void InitReceive()
        {
            recieveBW = new BackgroundWorker();
            recieveBW.WorkerSupportsCancellation = true;
            recieveBW.WorkerReportsProgress = true;
            recieveBW.DoWork += new DoWorkEventHandler(recieveBW_DoWork);

            recieveBW.ProgressChanged += new ProgressChangedEventHandler(recieveBW_ProgressChanged);
            recieveBW.RunWorkerCompleted += RecieveBW_RunWorkerCompleted;
            recieveBW.RunWorkerAsync();
        }

        private void RecieveBW_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //Log.Info("RecieveBW_RunWorkerCompleted");
            IsReceiving = false;
        }

        //收到信息触发事件
        void recieveBW_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 1)
            {
                if (DGramRecieved != null)
                {
                    DGramRecieved(this, (BUDPGram)e.UserState);
                }
            }
            if (e.ProgressPercentage == 2)
            {
                if (DGramListRecieved != null)
                {
                    DGramListRecieved(this, (List<BUDPGram>)e.UserState);
                }
            }
        }

        public bool IsReceiving { get; set; }

        //后台接收信息线程
        void recieveBW_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                IsReceiving = true;
                while (udpc.Client != null && !recieveBW.CancellationPending)
                {
                    try
                    {
                        List<BUDPGram> list = new List<BUDPGram>();
                        //DateTime start = DateTime.Now;
                        while (udpc.Available > 0)  //如果接收缓冲区有信息才接收，防止接收线程阻塞
                        {
                            BUDPGram budpg = new BUDPGram();
                            budpg.data = udpc.Receive(ref budpg.iep);
                            list.Add(budpg);
                            recieveBW.ReportProgress(1, budpg);
                        }//这里要用while，接收直到全部收完，而不是收一条，等100ms
                        if (list.Count > 0)
                        {
                            //TimeSpan time = DateTime.Now - start;
                            recieveBW.ReportProgress(2, list);
                            //list.Clear();
                        }
                        System.Threading.Thread.Sleep(recvInterval);
                    }
                    catch (Exception ex)
                    {
                        System.Threading.Thread.Sleep(recvInterval);
                    }
                }
            }
            catch (Exception  EX )
            {
                IsReceiving = false;
                //MessageBox.Show(EX.Message);
            }
            
        }

        public int Send(string txt,string ip,int port)
        {
            if (udpc == null) return -1;//设置的ip地址本机没有的话为null
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            var bytes = Encoding.UTF8.GetBytes(txt);
            return udpc.Send(bytes, bytes.Length, ipEndPoint);
        }

        public int Send(Byte[] sendData, IPEndPoint remoteEp)
        {
            int nReturn = -1;
            try
            {
                if (udpc == null) return -1;//设置的ip地址本机没有的话为null
                nReturn = udpc.Send(sendData, sendData.Length, remoteEp);
            }
            catch (Exception ex)
            {
                nReturn = -1;
            }

            return nReturn;
            
        }

        public int SendHex(string hexString,IPEndPoint remoteEp)
        {
            if (udpc == null) return -1;//设置的ip地址本机没有的话为null
            if (string.IsNullOrEmpty(hexString)) return 0;
            var bytes = ByteHelper.HexToBytes(hexString);
            return Send(bytes, remoteEp);
        }

        //所有局域网内监听指定port的UDPClient都可以收到
        public int BroadCast(Byte[] sendData, int port)
        {
            if (udpc == null) return -1;//设置的ip地址本机没有的话为null
            broadCastEp.Port = port;
            return udpc.Send(sendData, sendData.Length, broadCastEp);
        }

        public void Close()
        {
            //停止接收线程
            if(recieveBW!=null)
                recieveBW.CancelAsync();
            if(udpc!=null)//设置的ip地址本机没有的话为null
                udpc.Close();
        }
    }
}
