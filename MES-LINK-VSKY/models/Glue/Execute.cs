using Dapper;
using Microsoft.AspNetCore.Mvc;
using Nancy.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SQLBuilder.Core.Repositories;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MES_LINK_VSKY.models.GlueMachine
{
    public class Excute
    {
        //数据库连接读取配置
        private string A2oracleConnectionStr = Appsetting.ConnectionStrings.mesoracle;
        private string NewMesTest = Appsetting.ConnectionStrings.NewMesTest;
        private string A8oracleConnectionStr = Appsetting.ConnectionStrings.A8mesoracle ;
        private string DEPT_MES = Appsetting.ConnectionStrings.DEPT_MES;
        /// <summary>
        ///对于机台上传的参数进行处理
        /// </summary>
        public Responce HandleInfo([FromBody]Input Input)
        {
            int cmd=Input.CMD;
            //首次生产前
            if(cmd==1)
            {
                Responce Res=FistStart(Input);
                return Res;
            }
            //进板前判断
            else if(cmd == 2)
            {
                Responce Res=SecondStart(Input);
                return Res;
            }
            //生产结束
            else if (cmd == 3)
            {
                Responce Res=End(Input);
                return Res;
            }
            //未知命令，返回错误提示
            else
            {
                //记录Log
                JavaScriptSerializer js = new JavaScriptSerializer();
                LogHelper.Info(js.Serialize(Input));
                return new Responce { Result = false, Status = "NG", Message = "CMD命令传入错误"+"当前传入为:"+ cmd };                
            }
        }

        /// <summary>
        ///首次生产前
        /// </summary>
        public Responce FistStart(Input Input)
        {
            try
            {
                
                //记录日志
                JavaScriptSerializer js = new JavaScriptSerializer();
                LogHelper.Info(js.Serialize(Input));
                //定义参数
                string OpenReelExpireTime = "";
                string UnopenExpireTime = "";
                string Quantity = "";
                //解析传入的功能开关命令,0为关,1为开,共配置7位(1:是否过站/2:是否检查工单在BOM/3:是否检查胶水未开封保质期/4:是否检查胶水开封保质期/5:是否检查胶水数量/6:是否扣数/7:是否解绑载具)
                string[] FunctionSwith;
                if (string.IsNullOrEmpty(Input.FunctionSwitch))
                {
                    LogHelper.Info("cmd1:" + "请配置功能开关");
                    return new Responce { Result = false, Status = "", Message = "请配置功能开关" };
                }
                else
                {
                    FunctionSwith = Input.FunctionSwitch.Split(',');
                    if (FunctionSwith.Length != 7)
                    {
                        LogHelper.Info("cmd1:"+ "配置的功能开关长度不正确,应配置7位");
                        return new Responce { Result = false, Status = "", Message = "配置的功能开关长度不正确,应配置7位" };
                    }
                }
                //检查Reel_ID是否被禁用
                if(!CheckReelForbidden(Input .Reel_ID ,out string CheckReelMessage))
                {
                    return new Responce { Result = false, Status = Input.Reel_ID, Message = CheckReelMessage };
                }
                //检查胶水料号是否在工单BOM里面
                if (FunctionSwith[1]=="1")
                {
                    if (!CheckWo(Input.Work_Order, out string WoMessage))
                    {
                        return new Responce { Result = false, Status = Input.Work_Order, Message = WoMessage };
                    }
                    //检查胶水料号是否在工单BOM
                    if (!CheckReelInBom(Input.Reel_ID, Input.Work_Order, out string ReelInBomMessage))
                    {
                        return new Responce { Result = false, Status = Input.Reel_ID, Message = ReelInBomMessage };
                    }
                }
                
                //检查胶水是否过未开封保质期
                if(FunctionSwith[2]=="1")
                {
                    if (!CheckUnOpenReelExpireTime(Input.Reel_ID, out  UnopenExpireTime, out string UnOpenReelExpireTimeMesage))
                    {
                        return new Responce { Result = false, Status = Input.Reel_ID, Message = UnOpenReelExpireTimeMesage };
                    }
                }
               
                //检查胶水是否过开封保质期
                if(FunctionSwith[3] == "1")
                {
                    //检查胶水是否过开封保质期
                    if (!CheckOpenReelExpireTime(Input.Reel_ID, out OpenReelExpireTime, out string OpenReelExpireTimeMesage))
                    {
                        return new Responce { Result = false, Status = Input.Reel_ID, Message = OpenReelExpireTimeMesage };
                    }
                }
                
                //检查胶水数量
                if(FunctionSwith[4] == "1")
                {
                    if (!CheckReelQuantity(Input.Reel_ID, out Quantity, out string ReelQuantityMessage))
                    {
                        return new Responce { Result = false, Status = Input.Reel_ID+ ";Quantity:"+ Quantity, Message = ReelQuantityMessage };
                    }
                }
                //检查User
                if (!CheckEmp(Input.Emp_ID, out string CheckEmpMessage))
                {
                    return new Responce { Result = false, Status = Input.Emp_ID, Message = CheckEmpMessage };
                }
                //检查上传的Part_NO与Reel_ID是否匹配
                if(!CheckPartAndReel (Input.Reel_ID ,Input.Part_NO ,out string CheckPartAndReelMessage))
                {
                    return new Responce { Result = false, Status = Input.Reel_ID+";"+ Input.Part_NO, Message = CheckPartAndReelMessage };
                }
                //检查胶水是否处于领用状态
                if(!CheckGlueReceive (Input.Reel_ID ,out string CheckGlueReceiveMessage))
                {
                    return new Responce { Result = false, Status = Input.Reel_ID , Message = CheckGlueReceiveMessage };
                }
                //首次检查时将Reel_ID存入数据库
                if(!InsertReel_ID (Input .Reel_ID ,out string InsertReel_IDMessage))
                {
                    return new Responce { Result = false, Status = Input.Reel_ID , Message = InsertReel_IDMessage };
                }
                return new Responce { Result = true, Status = "UnOpenExpireTime:"+UnopenExpireTime+"天" +";OpenExpireTime:"+ OpenReelExpireTime+";Quantity:"+ Quantity, Message = "首次生产前参数确认OK" };
            }
            catch (Exception ex)
            {
                JavaScriptSerializer js = new JavaScriptSerializer();
                LogHelper.Info("cmd1异常;异常信息"+ex.ToString ()+ (js.Serialize(Input)));
                return new Responce { Result = false, Status = "查询异常", Message = ex.ToString () };
            }
            


        }

        /// <summary>
        ///开始作业前确认
        /// </summary>
        public Responce SecondStart(Input Input)
        {
            try
            {
                //记录日志
                JavaScriptSerializer js = new JavaScriptSerializer();
                LogHelper.Info(js.Serialize(Input));
                //定义参数
                string OpenReelExpireTime = "";
                string UnopenExpireTime = "";
                string Quantity = "";
                //解析传入的功能开关命令,0为关,1为开,共配置7位(1:是否过站/2:是否检查工单在BOM/3:是否检查胶水未开封保质期/4:是否检查胶水开封保质期/5:是否检查胶水数量/6:是否扣数/7:是否解绑载具)
                string[] FunctionSwith;
                if (string.IsNullOrEmpty(Input.FunctionSwitch))
                {
                    LogHelper.Info("cmd2"+ "请配置功能开关");
                    return new Responce { Result = false, Status = "", Message = "请配置功能开关" };
                }
                else
                {
                    FunctionSwith = Input.FunctionSwitch.Split(',');
                    if (FunctionSwith.Length != 7)
                    {
                        LogHelper.Info("cmd2" + "配置的功能开关长度不正确,应配置7位");
                        return new Responce { Result = false, Status = "", Message = "配置的功能开关长度不正确,应配置7位" };
                    }
                }
                //根据载具码查询小板sn
                if (!GetSNByCarrier (Input.Tooling_SN, out DataTable dt,out string GetSNByCarrierMessage))
                {
                    return new Responce { Result = false, Status = Input.Tooling_SN, Message = GetSNByCarrierMessage };
                }
                //检查途程是否正确
                if(!CheckRoute(Input.Machine_ID, dt,out string CheckRouteMessage))
                {
                    return new Responce { Result = false, Status = Input.Machine_ID, Message = CheckRouteMessage };
                }
                if(FunctionSwith[2]=="1")
                {
                    //检查胶水是否过未开封保质期
                    if (!CheckUnOpenReelExpireTime(Input.Reel_ID, out UnopenExpireTime, out string UnOpenReelExpireTimeMesage))
                    {
                        return new Responce { Result = false, Status = Input.Emp_ID, Message = UnOpenReelExpireTimeMesage };
                    }
                } 
                if(FunctionSwith [3]=="1")
                {
                    //检查胶水是否过开封保质期
                    if (!CheckOpenReelExpireTime(Input.Reel_ID, out OpenReelExpireTime, out string OpenReelExpireTimeMesage))
                    {
                        return new Responce { Result = false, Status = Input.Emp_ID, Message = OpenReelExpireTimeMesage };
                    }
                }
                if(FunctionSwith [4]=="1")
                {
                    //检查胶水数量
                    if (!CheckReelQuantity(Input.Reel_ID, out Quantity, out string ReelQuantityMessage))
                    {
                        return new Responce { Result = false, Status = Input.Emp_ID, Message = ReelQuantityMessage };
                    }
                }               
                return new Responce { Result = true, Status = "UnOpenExpireTime:" + UnopenExpireTime + "天" + ";OpenExpireTime:" + OpenReelExpireTime +";Quantity:"+ Quantity, Message = "进板状态检查成功" };
            }
            catch (Exception ex)
            {
                return new Responce { Result = false , Status = "", Message = "检查状态异常:"+ex.ToString () };
                throw;
            }
            
        }

        /// <summary>
        ///结束作业后
        /// </summary>
        public Responce End(Input Input)
        {
            try
            {
                //记录日志
                JavaScriptSerializer js = new JavaScriptSerializer();
                LogHelper.Info(js.Serialize(Input));
                //定义参数
                double NewQuantity = 9999;
                //解析传入的功能开关命令,0为关,1为开,共配置7位(1:是否过站/2:是否检查工单在BOM/3:是否检查胶水未开封保质期/4:是否检查胶水开封保质期/5:是否检查胶水数量/6:是否扣数/7:是否解绑载具)
                string[] FunctionSwith;
                if (string.IsNullOrEmpty (Input.FunctionSwitch))
                {
                    LogHelper.Info("cmd3:" + "请配置功能开关");
                    return new Responce { Result = false, Status = "", Message = "请配置功能开关" };
                }
                else 
                {
                    FunctionSwith = Input.FunctionSwitch.Split(',');
                    if(FunctionSwith.Length !=7)
                    {
                        LogHelper.Info("cmd3:" + "请配置功能开关");
                        return new Responce { Result = false, Status = "", Message = "配置的功能开关长度不正确,应配置7位" };
                    }
                }
                
                //根据载具码查询小板sn
                if (!GetSNByCarrier(Input.Tooling_SN, out DataTable dt, out string GetSNByCarrierMessage))
                {
                    return new Responce { Result = false, Status = Input.Tooling_SN, Message = GetSNByCarrierMessage };
                }
                //不过站只存储参数
                if (FunctionSwith[0]=="0")
                {
                    //数据存入数据库
                    if(!InsrtParam (Input,dt,out string InsrtMessageParam1))
                    {
                        return new Responce { Result = true, Status = "", Message = InsrtMessageParam1 };
                    }
                    return new Responce { Result = true, Status = GetSNByCarrierMessage, Message = "数据上传成功" };
                }

                //过站
                for (int i=0;i< dt.Rows .Count;i++)
                {
                    if(!PassStation(Input.Machine_ID, dt.Rows[i]["UNIT_SN"].ToString(), Input.Emp_ID, out string Tres))
                    {
                        return new Responce { Result = false, Status = dt.Rows[i]["UNIT_SN"].ToString(), Message = Tres };
                    }
                }    
                //数据存入数据库
               if(!InsrtParam (Input,dt,out string InsrtParamMessage2))
                {
                    return new Responce { Result = false, Status = "", Message = InsrtParamMessage2 };
                }
                //解绑载具
                if (FunctionSwith[5] == "1")//1为解绑,0为不解绑
                {
                    if (!UnbindTooling(Input.Tooling_SN, out string UnbindToolingMessage))
                    {
                        return new Responce { Result = false, Status = Input.Tooling_SN, Message = UnbindToolingMessage };
                    }
                }
                if (FunctionSwith [6]=="1")
                {
                    //检查胶水Reel_ID是否上传
                    if (string.IsNullOrEmpty(Input.Reel_ID))
                    {
                        LogHelper.Info("cmd3:" + "未上传胶水Reel_ID");
                        return new Responce { Result = false, Status = Input.Emp_ID, Message = "未上传胶水Reel_ID或者每小板扣数量" };
                    }
                    //过站成功Reel扣数
                    if (!ReduceQuantity(Input.Reel_ID, Input.ReduceCount, dt.Rows.Count, out NewQuantity, out string QuantityMessage))
                    {
                        return new Responce { Result = false, Status = Input.Reel_ID, Message = QuantityMessage };
                    }
                }             
                return new Responce { Result = true, Status = "UnOpenExpireTime:" + "" + "Day" + ";OpenExpireTime:" + "" + "Hour" + ";Quantity:" + NewQuantity, Message = "出板状态检查成功" };
            }
            catch (Exception ex)
            {
                LogHelper.Info("End;"+"异常信息:"+ex.ToString ());
                return new Responce { Result = false , Status = "NG", Message = "数据上传异常"+ex.ToString()};
            }
        }

        //检查工号
        public bool CheckEmp(string EMp,out string CheckEmpMessage)
        {
            try
            {
                LogHelper.Info("CheckEmp:工号"+ EMp);
                string SqlCheckEmp = @" select case ENABLED
 when 'Y' then 'OK'
 WHEN 'N' then 'NG' 
 END  STATUS,emp_no,emp_name,ENABLED from  sajet.sys_emp where emp_no="+"'"+ EMp+"'"+" AND ROWNUM = 1";
                var dbSearch = new OracleRepository(A8oracleConnectionStr);
                DataTable dt = dbSearch.FindTable(SqlCheckEmp);
                if(dt.Rows .Count ==0)
                {
                    CheckEmpMessage = "工号不存在;工号:"+ EMp;
                    LogHelper.Info("CheckEmp:"+ CheckEmpMessage);
                    return false ;
                }
                CheckEmpMessage = "检查工号成功;工号:" + EMp;
                LogHelper.Info("CheckEmp:" + CheckEmpMessage);
                return true;
            }
            catch (Exception ex)
            {
                CheckEmpMessage = "检查工号异常;工号:" + EMp+";异常信息:"+ex.ToString ();
                LogHelper.Info("CheckEmp:" + CheckEmpMessage);
                return false;
            }
        }
        /// <summary>
        /// 检查载具码查询小板SN 参数范例ToolingSN="H19-MLB-Coating-DIG-2010"
        /// </summary>
        ///  <remarks>注解 </remarks>
        /// <param name="ToolingSN">传入参数</param>
        /// <returns>true/false/DataTable</returns>
        public bool GetSNByCarrier(string ToolingSN, out DataTable dt,out string GetSNMessage)
        {
            try
            {
                LogHelper.Info("GetSNByCarrier:" + "载具码" + ToolingSN);
                if (string.IsNullOrEmpty (ToolingSN))
                {
                    dt = null;
                    GetSNMessage = "没有上传载具码;";
                    LogHelper.Info("GetSNByCarrier:"+ GetSNMessage);         
                    return false;
                }
                //取出当前载具最新绑定的sn
                string SqlGetSn = "select UNIT_SN from(select * from sajet.G_CARRIER_BINDING where CARRIER_NO=" + "'" + ToolingSN + "' order by LINK_TIME desc)where ROWNUM <= 12 order by SERSION";
                var dbSearch = new OracleRepository(A8oracleConnectionStr);
                dt = dbSearch.FindTable(SqlGetSn);
                if (dt.Rows.Count == 0)
                {
                    GetSNMessage = "根据载具码没有查找到小板SN;" + "载具码:" + ToolingSN;
                    LogHelper.Info("GetSNByCarrier:" +GetSNMessage);
                    return false ;
                }
                //else if (dt.Rows.Count != 4)
                //{
                //    return "NG 根据载具码查找到对应小板sn数量异常" + dt.Rows.Count;
                //}
                string js = JsonConvert.SerializeObject(dt);
                GetSNMessage = js;
                LogHelper.Info("GetSNByCarrier:" + "根据载具码查询小板SN成功" + ToolingSN+";小板码:"+js);
                return true;
            }
            catch (Exception ex)
            {
                dt = null;
                GetSNMessage = "根据载具码查询小板SN异常;异常信息:" + ex.ToString();
                LogHelper.Info("GetSNByCarrier:" + GetSNMessage);
                return false ;
            }

        }

        //根据Pannel码获取小板sn,返回string类型数据
        public string PanelGetsn(string PannelSN, out string LocationNum)
        {
            try
            {
                string SqlGetSn = "select UNIT_SN from sajet.g_sn_status where panel_no=" + "'" + PannelSN + "'" + "order by out_process_time desc";
                var dbSearch = new OracleRepository(A2oracleConnectionStr);
                DataTable dt = new DataTable();
                int Location;
                ArrayList alLocation = new ArrayList();
                dt = dbSearch.FindTable(SqlGetSn);
                if (dt.Rows.Count == 0)
                {
                    LocationNum = "";
                    return "NG: 根据载具码没有查找到对应小板sn";
                }
                else
                {
                    for(int i=0;i< dt.Rows .Count;i++)
                    {
                        string seri_num = dt.Rows[i]["Serial_number"].ToString();
                        Location=int.Parse (seri_num.Substring (seri_num.Length  - 2, 2));
                        alLocation.Add(Location);
                    }
                }
                alLocation.Sort();//排序
                LocationNum = "";
                string tempInfo = "";
                foreach (int num in alLocation)
                {
                    LocationNum += num.ToString()+",";//位置参数
                    //返回序列号排序
                    for (int i = 0; i < dt.Rows.Count; i++)
                    {
                        string seri_num = dt.Rows[i]["Serial_number"].ToString();
                         Location = int.Parse(seri_num.Substring(seri_num.Length - 2, 2));
                        if(Location==num)
                        {
                            //不跳过的数据
                            tempInfo = "{"+"BlockNumber" + Location + "," + "BoardBarcode:" + seri_num + "," + "Skip:" + "N"+"}"+",";
                        }                    
                    }
                    //跳过的数据
                    tempInfo = "{" + "BlockNumber" + "" + "," + "BoardBarcode:" + "" + "," + "Skip:" + "Y" + "}" + ",";
                    tempInfo += tempInfo;
                }
                LocationNum=LocationNum.Substring(0, LocationNum.LastIndexOf(","));
                tempInfo= tempInfo.Substring(0, LocationNum.LastIndexOf(","));
                return tempInfo; 
            }
            catch (Exception ex)
            {
                LocationNum ="";
                return "NG:" + ex.ToString();
            }

        }
        //Qtime卡控
        public string Qtime(string BoardBarcode)
        {
            try
            {
                string SqlGetLastStationtime = "";
                return "";
            }
            catch (Exception ex)
            {

                throw;
            }
        }
        /// <summary>
        /// 过站 参数范例Terminalid="100036179",SN="GFH95240NKVJCD32Y",Emp="12723717"
        /// </summary>
        ///  <remarks>注解 </remarks>
        /// <param name="Terminalid,SN,Emp(工号)">传入参数</param>
        /// <returns>true/false/PassStationMesssage</returns>
        public bool PassStation(string Terminalid, string SN, string Emp, out string PassStationMesssage)
        {
            try
            {
                LogHelper.Info("PassStation;Terminalid:" + Terminalid+ "SN:"+ SN+";Emp:"+ Emp);
                DateTime time = DateTime.Now;
                var param = new DynamicParameters();
                var dbSearch = new OracleRepository(A8oracleConnectionStr);
                param.Add(":TTERMINALID", Terminalid, DbType.String, ParameterDirection.Input);
                param.Add(":TSN", SN, DbType.String, ParameterDirection.Input);
                param.Add(":TNOW", time, DbType.Date , ParameterDirection.Input);
                param.Add(":TRES", "", DbType.String, ParameterDirection.Output);
                param.Add(":TEMP", Emp, DbType.String, ParameterDirection.Input);
                dbSearch.ExecuteByProc("SAJET.SJ_GO", param);
                var res = param.Get<string>(":TRES");
                if (res.Substring(0, 2) != "OK")
                {
                    PassStationMesssage = "NG:" + res;
                }
                PassStationMesssage = "OK:" + SN + "过站成功";
                LogHelper.Info("PassStation:" + PassStationMesssage);
                return true;
            }
            catch (Exception ex)
            {
                PassStationMesssage = "过站异常;Terminalid"+ Terminalid+ ";SN" + SN +"工号"+ Emp +"异常信息:"+ ex.ToString();
                LogHelper.Info("PassStation" + PassStationMesssage);
                return false;
            }
        }

        /// <summary>
        /// 检查途程是否正确 参数范例ToolingSN="100036179"
        /// </summary>
        ///  <remarks>注解 </remarks>
        /// <param name="Terminalid">传入参数</param>
        /// <returns>true/false/DataTable</returns>
        public bool CheckRoute(string Terminalid,  DataTable dt,out string CheckRouteMessage)
        {
            string js = "";
            try
            {
                js = JsonConvert.SerializeObject(dt);
                LogHelper.Info("CheckRoute;站点号:"+ Terminalid+";小板码:"+ js);
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string Tsn = dt.Rows[i]["UNIT_SN"].ToString();
                    var param = new DynamicParameters();
                    var dbSearch = new OracleRepository(A8oracleConnectionStr );
                    param.Add(":TERMINALID", Terminalid, DbType.String, ParameterDirection.Input);
                    param.Add(":TSN", Tsn, DbType.String, ParameterDirection.Input);
                    param.Add(":TRES", "", DbType.String, ParameterDirection.Output);
                    dbSearch.ExecuteByProc("SAJET.SJ_CKRT_ROUTE", param);
                    var res = param.Get<string>(":TRES");
                    if (res.Substring(0, 2) != "OK")
                    {
                        CheckRouteMessage = "NG:当前站点不正确,无法过站;" + res + ":" + Tsn;
                        LogHelper.Info("CheckRoute" + CheckRouteMessage);
                        return false;
                    }
                }
                
                CheckRouteMessage = "OK:" + js + "途程正确";
                LogHelper.Info("CheckRoute:"+ CheckRouteMessage);
                return true;
            }
            catch (Exception ex)
            {
                CheckRouteMessage = "NG检查小板sn途程出现异常;站点号"+ Terminalid +";小板码:"+js+ ";异常信息:" + ex.ToString();
                LogHelper.Info("CheckRoute" + CheckRouteMessage);
                return false ;
            }
        }
        //检查胶水料号是否正确
        public string CheckMachineParamsByID(int MachineID, string reel_id)
        {
            try
            {
                string sqlGetID = "select TERMINAL_NAME from sajet.SYS_TERMINAL where TERMINAL_ID='" + MachineID + "'";
                var dbSearchMachineName = new OracleRepository(A8oracleConnectionStr);
                DataTable dtMachineName = dbSearchMachineName.FindTable(sqlGetID);
                if (dtMachineName.Rows.Count == 0)
                {
                    LogHelper.Info("NG:根据机台ID查找不到机台名称" + MachineID);
                    return "NG:根据机台ID查找不到机台名称";
                }
                string MachineType = dtMachineName.Rows[0][0].ToString();
                //判断机台类型，分为Masking,Sensor和VCN1,VCN2
                if (MachineType.Contains("Masking"))
                {
                    MachineType = "Masking";
                }
                else if (MachineType.Contains("Glue staking"))
                {
                    MachineType = "Sensor";
                }
                else if (MachineType.Contains("VCN1"))
                {
                    MachineType = "VCN1Coating";
                }
                else if (MachineType.Contains("VCN2"))
                {
                    MachineType = "VCN2Coating";
                }
                string sqlGetParams = "select A as MachineParam from sajet.machine_name where MACHINE_NAME='" + MachineType + "'";
                var dbSearchParam = new OracleRepository(NewMesTest);
                DataTable dtMachineParam = dbSearchParam.FindTable(sqlGetParams);
                if (dtMachineParam.Rows.Count == 0)
                {
                    LogHelper.Info("NG:根据机台名称查找不到机台胶水料号" + MachineID);
                    return "NG:根据机台名称查找不到机台胶水料号";
                }
                LogHelper.Info("查询胶水料号成功;" + dtMachineParam.Rows[0][0].ToString() + ";" + MachineID);
                if(string.IsNullOrEmpty (reel_id)|| reel_id.Length <14)
                {
                    LogHelper.Info("NG:没有输入胶水料号或者料号长度不正确" + reel_id);
                    return "NG:没有输入胶水料号或者料号长度不正确" + reel_id;
                }
                reel_id = reel_id.Substring(0, 14);//截取前十四位位胶水料号
                if (reel_id != dtMachineParam.Rows[0][0].ToString())
                {
                    LogHelper.Info("NG:当前机台应该使用的胶水料号不正确" + MachineID);
                    return "NG:当前机台使用的胶水料号不正确";
                }
                return dtMachineParam.Rows[0][0].ToString();

            }
            catch (Exception ex)
            {
                LogHelper.Info("NG:查询机台胶水料号异常" + ex.ToString() + "请联系系统工程师解决");
                return "NG:查询机台胶水料号异常" + ex.ToString() + "请联系系统工程师解决";
            }
        }
        /// <summary>
        /// 检查胶水Reel_ID是否在工单BOM 参数范例Reel_ID="095-0009-F047H202000009",Wo="VN104-200400026"
        /// </summary>
        ///  <remarks>注解 </remarks>
        /// <param name="Reel_ID,Wo">传入参数</param>
        /// <returns>true/false/Message</returns>
        public bool CheckReelInBom(string Reel_ID,string Wo, out string Message)
        {
            try
            {
                LogHelper.Info("CheckReelInBom;工单号:" + Wo +";Reel_ID:"+ Reel_ID);
                if(string.IsNullOrEmpty (Reel_ID))
                {
                    Message = "未上传Reel_ID";
                    LogHelper.Info("CheckReelInBom;" + Message);
                    return false;
                }
                var dbSearch = new OracleRepository(A8oracleConnectionStr);
                //在sajet.g_material中根据Reel_ID查询PART_ID
                string SqlMeterrial = "select PART_ID from sajet.g_material where REEL_NO='"+ Reel_ID+ "' and PART_ID is not null";
                DataTable dtMeterrial = dbSearch.FindTable(SqlMeterrial);
                if(dtMeterrial.Rows .Count ==0)
                {
                    Message = "在表sajet.G_Material中找不到当前Reel_ID的PART_ID;Reel_ID:"+ Reel_ID;
                    LogHelper.Info("CheckReelInBom;" + Message);
                    return false;
                }
                //在sajet.g_wo_bom中根据WORK_ORDER查询ITEM_PART_ID
                string SqlBOM = "select ITEM_PART_ID from sajet.g_wo_bom where WORK_ORDER='" + Wo + "' and ITEM_PART_ID is not null";
                DataTable dtBOM = dbSearch.FindTable(SqlBOM);
                if (dtBOM.Rows.Count == 0)
                {
                    Message = "在表sajet.G_WO_BOM中找不到当前Reel_ID的ITEM_PART_ID;Reel_ID:" + Reel_ID+";工单号:"+ Wo;
                    LogHelper.Info("CheckReelInBom;"+ Message);
                    return false;
                }
                //循环判断料号是否在工单BOM里面
                for(int i=0;i< dtBOM.Rows.Count;i++)
                {
                    //去除空格再比较
                    if(dtMeterrial.Rows[0]["PART_ID"].ToString ().Trim () == dtBOM.Rows[i]["ITEM_PART_ID"].ToString().Trim())
                    {
                        Message = "CheckReelInBom成功" + ";Reel_ID:" + Reel_ID + ";工单号:" + Wo;
                        return true;
                    }
                }
                Message = "当前Reel_ID不在工单BOM里面" + ";Reel_ID:" + Reel_ID + ";工单号:" + Wo;
                LogHelper.Info("CheckReelInBom;" + Message);
                return false;
            }
            catch (Exception ex)
            {
                Message = "查询Reel_ID是否在工单BOM里面出现异常" + ";Reel_ID:" + Reel_ID + ";工单号:" + Wo+";异常信息:"+ex.ToString ();
                LogHelper.Info("CheckReelInBom;" + Message);
                return false;
            }
            
        }
        /// <summary>
        /// 检查胶水工单 参数范例Wo="VN104-200400026"
        /// </summary>
        ///  <remarks>注解 </remarks>
        /// <param name="Wo">传入参数</param>
        /// <returns>true/false/Message</returns>
        public bool CheckWo(string Wo, out string Message)
        {
            try
            {
                LogHelper.Info("CheckWo;工单号" + Wo);
                if(string.IsNullOrEmpty (Wo))
                {
                    Message = "未上传工单号";
                    LogHelper.Info("CheckWo;" + Message);
                    return false;
                }
                string procedure = "SAJET.SJ_CHK_WO_INPUT";//检查工单存储过程
                var param = new DynamicParameters();
                var dbSearch = new OracleRepository(A8oracleConnectionStr);
                param.Add(":TREV", Wo, DbType.String, ParameterDirection.Input);
                param.Add(":TRES", "", DbType.String, ParameterDirection.Output);
                dbSearch.ExecuteByProc(procedure, param);
                var res = param.Get<string>(":TRES");
                if (res.Substring(0, 2) != "OK")
                {
                    Message = "检查工单出错,错误信息:" + res + ";工单:" + Wo;
                    LogHelper.Info("CheckWo;" +Message);
                    return false ;
                }
                Message = "OK:工单正确;" + Wo;
                LogHelper.Info("CheckWo;" + Message);
                return true ;
            }
            catch (Exception ex)
            {

                Message = "NG:检查工单异常;" +"工单;"+Wo +";异常信息:"+ex.ToString ();
                LogHelper.Info("CheckWo;"  + Message);
                return false;
            }
            
        }
        /// <summary>
        /// 检查胶水数量 参数范例Reel_ID="095-0009-F047H202000009"
        /// </summary>
        ///  <remarks>注解 </remarks>
        /// <param name="Reel_ID">传入参数</param>
        /// <returns>true/false/Message</returns>
        public bool CheckReelQuantity(string Reel_ID,out string Quantity, out string Message)
        {
            try
            {
                LogHelper.Info("CheckReelQuantity;Reel_ID:" + Reel_ID);
                if(string.IsNullOrEmpty (Reel_ID))
                {
                    Quantity = "";
                    Message = "没有上传Reel_ID";
                    LogHelper.Info("CheckReelQuantity;" + Message);
                    return false;
                }
                string ReelQuantity = "select REEL_QTY from sajet.g_material where REEL_NO='"+ Reel_ID+ "' and REEL_QTY is not null";
                var dbSearch = new OracleRepository(A8oracleConnectionStr);
                DataTable dt=dbSearch.FindTable(ReelQuantity);
                if (dt.Rows .Count ==0)
                {
                    Quantity = "";
                    Message = "没有查找到当前Reel剩余数量" + ":" + Reel_ID;
                    LogHelper.Info("CheckReelQuantity;"+ Message);
                    return false;
                }
                if(double.Parse(dt.Rows[0]["REEL_QTY"].ToString ())<=0)
                {
                    Quantity = "0";
                    Message = "当前Reel数量已经使用完" + ":" + Reel_ID;
                    LogHelper.Info("CheckReelQuantity;" + Message);
                    return false;
                }
                Quantity = dt.Rows[0]["REEL_QTY"].ToString();
                Quantity = double.Parse(Quantity).ToString("0.00");//保留小数点后两位
                Message = "检查Reel数量成功;" + Reel_ID+";剩余数量:"+ Quantity;
                LogHelper.Info("CheckReelQuantity;" + Message);
                return true;
            }
            catch (Exception ex)
            {
                Quantity = "";
                Message = "检查Reel数量异常;" + "Reel_ID:" + Reel_ID+";异常信息:"+ ex.ToString();
                LogHelper.Info("CheckReelQuantity;" + Message);
                return false;
            }

        }
        /// <summary>
        /// 检查胶水是否过未开封保质期 参数范例Reel_ID="095-0009-F047H202000009"
        /// </summary>
        ///  <remarks>注解 </remarks>
        /// <param name="Reel_ID">传入参数</param>
        /// <returns>true/false/Message/UnOpenExpireTime</returns>
        public bool CheckUnOpenReelExpireTime(string Reel_ID,out string UnOpenReelExpireTime, out string Message)
        {
            try
            {
                LogHelper.Info("CheckUnOpenReelExpireTime;Reel_ID:" + Reel_ID);
                if(string.IsNullOrEmpty (Reel_ID))
                {
                    Message = "未上传Reel_ID";
                    UnOpenReelExpireTime = "";
                    LogHelper.Info("CheckUnOpenReelExpireTime;" + Message);
                    return false;
                }
                string ReelExpireTime = "select EXP_DATE from sajet.g_material where REEL_NO='" + Reel_ID + "' and EXP_DATE is not null";
                var dbSearch = new OracleRepository(A8oracleConnectionStr);
                DataTable dt = dbSearch.FindTable(ReelExpireTime);
                if (dt.Rows.Count == 0)
                {
                    Message = "没有查找到当前Reel的EXP_DATE(未开封过期时间);Reel_ID:" + Reel_ID;
                    UnOpenReelExpireTime = "";
                    LogHelper.Info("CheckUnOpenReelExpireTime;" + Message);
                    return false;
                }
                //将获取到的时间转换成DateTime类型,获取的数据格式为:20200211
                string Time=dt.Rows[0]["EXP_DATE"].ToString();
                int years = int.Parse( Time.Substring(0, 4));
                int month = int.Parse(Time.Substring(4, 2));
                int day = int.Parse(Time.Substring(6, 2));
                DateTime s = new DateTime(years,month, day);

                DateTime lastTime = Convert.ToDateTime(s);
                TimeSpan span = lastTime - DateTime.Now;  //剩余时间
                if(span.TotalSeconds <= 0)
                {
                    Message = "当前Reel已过未开封保质期,无法使用;Reel_ID:" + Reel_ID+"剩余时间:0";
                    LogHelper.Info("CheckUnOpenReelExpireTime;" + Message);
                    UnOpenReelExpireTime = "0";
                    return false;
                }
                Message = "检查Reel未开封保质期成功;Reel_ID:" + Reel_ID;
                LogHelper.Info("CheckUnOpenReelExpireTime;" + Message);
                UnOpenReelExpireTime = span.TotalDays.ToString().Substring (0,5);
                return true;
            }
            catch (Exception ex)
            {

                Message = "检查Reel未开封保质期异常;" + "Reel_ID:" + Reel_ID + ";异常信息:" + ex.ToString();
                LogHelper.Info("CheckUnOpenReelExpireTime;" + Message);
                UnOpenReelExpireTime = "";
                return false;
            }

        }
        /// <summary>
        /// 检查胶水是否过开封后保质期 参数范例Reel_ID="095-0009-F047H202000009"
        /// </summary>
        ///  <remarks>注解 </remarks>
        /// <param name="Reel_ID">传入参数</param>
        /// <returns>true/false/Message/OpenExpireTime</returns>
        public bool CheckOpenReelExpireTime(string Reel_ID, out string OpenReelExpireTime, out string Message)
        {
            try
            {
                LogHelper.Info("CheckOpenReelExpireTime;Reel_ID:" + Reel_ID);
                if(string.IsNullOrEmpty (Reel_ID))
                {
                    Message = "未上传Reel_ID";
                    OpenReelExpireTime = "";
                    LogHelper.Info("CheckUnOpenReelExpireTime;" + Message);
                    return false;
                }
                //读取配置文件,配置为appsettings.jason文件中的ExpireTimeSetting属性
                string SettingExpireTime = Appsetting.ConnectionStrings.SettingExpireTime;
                //获取Reel_ID的第一次上传时间，第一次上传时间视为开封时间
                var dbSearch = new OracleRepository(DEPT_MES );
                TimeSpan span = new TimeSpan();
                string SqlFirstSendTime = "select TIMESTAMP from mes.machine_param_traceability where B='"+Reel_ID+ "' order by TIMESTAMP asc";
                DataTable dt = dbSearch.FindTable(SqlFirstSendTime);
                if(dt.Rows .Count !=0)
                {
                    DateTime  DT = (DateTime )dt.Rows[0][0];
                    span = (TimeSpan)(DateTime.Now - DT);
                    if (span.TotalHours >= double.Parse (SettingExpireTime))
                    {
                        Message = "Reel已过开封卡控时间;卡控时长:" + SettingExpireTime + "小时" +";Reel开封时间:"+ DT + ";Reel_ID:" + Reel_ID;
                        OpenReelExpireTime = "";
                        LogHelper.Info("CheckUnOpenReelExpireTime;" + Message);
                        return false;
                    }
                    OpenReelExpireTime = (double.Parse(SettingExpireTime) - span.TotalHours ).ToString("0.00");//获取剩余小时数
                    string[] strsTime = OpenReelExpireTime.Split('.');
                    OpenReelExpireTime = strsTime[0] + "小时" + (double.Parse ("0."+strsTime[1])*60).ToString ().Split('.')[0] + "分钟";//修改为XX小时XX分钟的格式
                    Message = "检查Reel开封保质期成功;Reel_ID:" + Reel_ID + ";开封后剩余时间:" + strsTime[0] + "小时"+ (double.Parse("0." + strsTime[1]) * 60).ToString().Split('.')[0] + "分钟";
                    LogHelper.Info("CheckOpenReelExpireTime;" + Message);
                    return true;
                }
                //没有查询到上传记录视为第一次上传
                OpenReelExpireTime = SettingExpireTime + "小时" + "0分钟";
                Message = "检查Reel开封保质期成功;Reel_ID:" + Reel_ID+";开封后剩余时间:"+ SettingExpireTime + "小时"+"0分钟";
                LogHelper.Info("CheckUnOpenReelExpireTime;" + Message);              
                return true;
            }
            catch (Exception ex)
            {
                Message = "检查Reel开封保质期异常;" + "Reel_ID:" + Reel_ID + ";异常信息:" + ex.ToString();
                LogHelper.Info("CheckUnOpenReelExpireTime;" + Message);
                OpenReelExpireTime = "";
                return false;
            }

        }
        /// <summary>
        /// 胶水扣数 参数范例Reel_ID="095-0009-F047H202000009,PecsCout="10",Count=12
        /// </summary>
        ///  <remarks>注解 </remarks>
        /// <param name="Reel_ID,PecsCout,Count">传入参数</param>
        /// <returns>true/false/Message/newQty</returns>
        public bool ReduceQuantity(string Reel_ID, double PecsCout, int Count,out double newQty, out string Message)
        {
            try
            {
                LogHelper.Info("ReduceQuantity;Reel_ID:" + Reel_ID + ";每PECS扣数" + PecsCout+";数量"+ Count);
                if(string.IsNullOrEmpty (PecsCout.ToString ()))
                {
                    Message = "未上传每pecs扣数重量;Reel_ID:" + Reel_ID;
                    newQty = 0;
                    LogHelper.Info("ReduceQuantity;" + Message);
                    return false;
                }
                if(PecsCout<0)
                {
                    Message = "扣数量不能小于0;Reel_ID:" + Reel_ID;
                    newQty = 0;
                    LogHelper.Info("ReduceQuantity;" + Message);
                    return false;
                }
                //扣数前先查询剩余数量
                string ReelQty = "select   REEL_QTY from sajet.g_material where REEL_NO='" + Reel_ID+"'"+ "and REEL_QTY is not null";
                var dbSearch = new OracleRepository(A8oracleConnectionStr);
                DataTable dt = dbSearch.FindTable(ReelQty);
                if (dt.Rows.Count == 0)
                {
                    Message = "没有查找到当前Reel的数量;Reel_ID:" + Reel_ID;
                    newQty = 0;
                    LogHelper.Info("ReduceQuantity;" + Message);
                    return false;
                }
                //扣数
                newQty = double.Parse((double .Parse (dt.Rows[0]["REEL_QTY"].ToString ())- PecsCout* Count).ToString ("0.000"));
                string ReduceQty = "update  sajet.g_material set REEL_QTY="+newQty+" where REEL_NO='" + Reel_ID + "'";
                dbSearch.ExecuteBySql(ReduceQty);
                newQty=double .Parse (newQty.ToString ("0.000"));
                Message = "Reel扣数成功;Reel_ID:" + Reel_ID+";剩余数量:"+ newQty;
                LogHelper.Info("ReduceQuantity;" + Message);
                return true;
            }
            catch (Exception ex)
            {
                Message = "Reel扣数异常;" + "Reel_ID:" + Reel_ID + ";异常信息:" + ex.ToString();
                newQty = 0;
                LogHelper.Info("ReduceQuantity;" + Message);
                return false;
            }

        }
        /// <summary>
        /// 参数存入数据库 Reel_ID="095-0009-F047H202000009……
        /// </summary>
        ///  <remarks>注解 </remarks>
        /// <param name="Input,dt">传入参数</param>
        /// <returns>true/false/Message</returns>
        public bool InsrtParam(Input input,DataTable dt,out string InsrtParamMessage)
        {
            try
            {
                LogHelper.Info("InsrtParam:Begain");
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    string sql = @"insert into MES.MACHINE_PARAM_TRACEABILITY (machine_name,machine_type,vender,machine_sn,SERIAL_NUM,emp_no,terminal_id,A,B,C,D,E,F,G,H,I,J,K,L,M,N,O,P) values ("
                    + "'" + input.Machine_Name + "'" + "," + "'" + input.Machine_Type + "'" + "," + "'" + input.Vender + "'" + "," + "'" + input.Machine_SN + "'" + "," + "'" + dt.Rows[i]["UNIT_SN"] + "'" + "," + "'" + input.Emp_ID + "'" + ","
                    + "'" + input.Machine_ID + "'" + "," + "'" + input.Work_Order + "'" + "," + "'" + input.Reel_ID + "'" + "," + "'" + input.Tool_ID + "'" + "," + "'" + input.Recipe_Name + "'" + ","
                    + "'" + input.Tooling_SN + "'" + "," + "'" + input.Panel_SN + "'" + "," + "'" + input.ParameterA + "'" + "," + "'" + input.ParameterB + "'" + ","
                    + "'" + input.ParameterC + "'" + "," + "'" + input.ParameterD + "'" + "," + "'" + input.ParameterE + "'" + "," + "'" + input.ParameterF + "'" +
                    "," + "'" + input.ParameterG + "'" + "," + "'" + input.ParameterH + "'" + "," + "'" + input.ParameterI + "'" + "," + "'" + "SERSION#" + (i + 1) + "'" + ")";
                    var dbSearch = new OracleRepository(DEPT_MES );
                    int tr = dbSearch.ExecuteBySql(sql);
                }
                LogHelper.Info("InsrtParam:End");
                InsrtParamMessage = "";
                return true;
            }
            catch (Exception ex)
            {
                InsrtParamMessage = "参数存入系统异常:异常信息:" + ex.ToString();
                LogHelper.Info("InsrtParam;" + InsrtParamMessage);
                return false ;
            }
        }
        /// <summary>
        /// 首次查询时将Reel_ID存入数据库 Reel_ID="095-0009-F047H202000009
        /// </summary>
        ///  <remarks>注解 </remarks>
        /// <param name="Reel_ID">传入参数</param>
        /// <returns>true/false/Message</returns>
        public bool InsertReel_ID(string Reel_ID, out string InsrtReel_IDMessage)
        {
            try
            {
                LogHelper.Info("InsertReel_ID:Begain;Reel_ID:"+ Reel_ID);
                if(string.IsNullOrEmpty (Reel_ID ))
                {
                    InsrtReel_IDMessage = "没有上传Reel_ID";
                    LogHelper.Info("InsertReel_ID;" + InsrtReel_IDMessage);
                    return false;
                }
                //数据存入数据库
                string sql = "insert into mes.machine_param_traceability (B) values ('"+ Reel_ID+"')";
                var dbSearch = new OracleRepository(DEPT_MES );
                int tr = dbSearch.ExecuteBySql(sql);
                InsrtReel_IDMessage = "首次查询时将Reel_ID插入数据库成功";
                LogHelper.Info("InsertReel_ID:End;首次查询时将Reel_ID插入数据库成功");
                return true;
            }
            catch (Exception ex)
            {
                InsrtReel_IDMessage = "首次查询时将Reel_ID存入系统异常:异常信息:" + ex.ToString();
                LogHelper.Info("InsrtReel_ID;" + InsrtReel_IDMessage);
                return false;
            }
        }
        /// <summary>
        /// 过站后解绑载具 tooling_SN="W1ATFRY0704"
        /// </summary>
        ///  <remarks>注解 </remarks>
        /// <param name="tooling_SN">传入参数</param>
        /// <returns>true/false/Message</returns>
        public bool UnbindTooling(string tooling_SN,out string UnbindToolingMessage)
        {
            try
            {
                LogHelper.Info("UnbindTooling;载具码:"+ tooling_SN);
                string SqlUnbindTooling = "delete sajet.g_carrier_binding where CARRIER_NO='"+ tooling_SN+"'";
                var dbSearch = new OracleRepository(A8oracleConnectionStr);
                int tr = dbSearch.ExecuteBySql(SqlUnbindTooling);
                UnbindToolingMessage = "载具解绑成功;解绑载具:"+ tooling_SN;
                LogHelper.Info("UnbindTooling:"+ UnbindToolingMessage);
                return true;
            }
            catch (Exception ex)
            {
                UnbindToolingMessage = "载具解绑异常;解绑载具:" + tooling_SN+";异常信息:"+ex.ToString ();
                LogHelper.Info("UnbindTooling:" + UnbindToolingMessage);
                return false;
            }
        }
        /// <summary>
        /// 检查上传的Part_NO与上传的Reel_ID是否匹配 Reel_ID="095-0009-F047H202000009,Part_NO="095-0009-F047H"
        /// </summary>
        ///  <remarks>注解 </remarks>
        /// <param name="Reel_ID,Part_NO">传入参数</param>
        /// <returns>true/false/Message</returns>
        public bool CheckPartAndReel(string Reel_ID, string Part_NO, out string CheckPartAndReelMessage)
        {
            try
            {
                LogHelper.Info("CheckPartAndReel;Reel_ID:" + Reel_ID+ ";Part_NO:"+ Part_NO);
                if(string.IsNullOrEmpty (Reel_ID))
                {
                    CheckPartAndReelMessage = "未上传Reel_ID";
                    LogHelper.Info("CheckPartAndReel:" + CheckPartAndReelMessage);
                    return false;
                }
                if(string.IsNullOrEmpty(Part_NO))
                {
                    CheckPartAndReelMessage = "未上传Part_NO(料号)";
                    LogHelper.Info("CheckPartAndReel:" + CheckPartAndReelMessage);
                    return false;
                }
                string SqlPartNO = "select B.part_NO from  sajet.g_material A,sajet.sys_part B where A.Reel_NO='" + Reel_ID + "'"+"and A.part_ID=B.part_ID";
                var dbSearch = new OracleRepository(A8oracleConnectionStr);
                DataTable dt = dbSearch.FindTable (SqlPartNO);
                if(dt.Rows .Count ==0)
                {
                    CheckPartAndReelMessage = "当前上传Part_NO(料号)与Reel_ID不匹配:Part_NO:" + Part_NO + ";Reel_ID:" + Reel_ID;
                    LogHelper.Info("CheckPartAndReel:" + CheckPartAndReelMessage);
                    return false;
                }
                if(dt.Rows[0]["part_NO"].ToString ().Trim ()!= Part_NO)
                {
                    CheckPartAndReelMessage = "当前上传Part_NO(料号)与Reel_ID不匹配:Part_NO:" + Part_NO + ";Reel_ID:" + Reel_ID;
                    LogHelper.Info("CheckPartAndReel:" + CheckPartAndReelMessage);
                    return false;
                }
                CheckPartAndReelMessage = "检查Part_NO与Reel_ID是否匹配成功:Part_NO:" + Part_NO+ ";Reel_ID:" + Reel_ID;
                LogHelper.Info("CheckPartAndReel:" + CheckPartAndReelMessage);
                return true;
            }
            catch (Exception ex)
            {
                CheckPartAndReelMessage = "检查Part_NO与Reel_ID是否匹配异常:Part_NO:" + Part_NO + ";Reel_ID:" + Reel_ID+";异常信息:" + ex.ToString();
                LogHelper.Info("CheckPartAndReel:" + CheckPartAndReelMessage);
                return false;
            }
        }
        /// <summary>
        /// 检查上传Reel_ID是否被禁用 Reel_ID="095-0009-F047H202000009
        /// </summary>
        ///  <remarks>注解 </remarks>
        /// <param name="Reel_ID,Part_NO">传入参数</param>
        /// <returns>true/false/Message</returns>
        public bool CheckReelForbidden(string Reel_ID, out string CheckReelForbiddenMessage)
        {
            try
            {
                LogHelper.Info("CheckReelForbidden;Reel_ID:" + Reel_ID);
                if (string.IsNullOrEmpty(Reel_ID))
                {
                    CheckReelForbiddenMessage = "未上传Reel_ID";
                    LogHelper.Info("CheckReelForbidden:" + CheckReelForbiddenMessage);
                    return false;
                }
                string SqlReelForbidden = "select ENABLE,CREATE_USER,REMARKS from  sajet.g_reel_forbidden  where REEL_NO='" + Reel_ID + "'";
                var dbSearch = new OracleRepository(A8oracleConnectionStr);
                DataTable dtReel = dbSearch.FindTable(SqlReelForbidden);
                if (dtReel.Rows.Count != 0&& dtReel.Rows [0]["ENABLE"].ToString ().Trim ()== "Y")
                {
                    string SqlName = "select EMP_NAME from sajet.sys_emp where EMP_NO='"+ dtReel.Rows [0]["CREATE_USER"]+"'";
                    DataTable dtName = dbSearch.FindTable(SqlName);
                    CheckReelForbiddenMessage = "当前Reel_ID被禁用" + ";Reel_ID:" + Reel_ID+";禁用人:"+ dtName.Rows [0]["EMP_NAME"]+ ";REMARKS:"+ dtReel.Rows[0]["REMARKS"];
                    LogHelper.Info("CheckReelForbidden:" + CheckReelForbiddenMessage);
                    return false;
                }
                CheckReelForbiddenMessage = "当前Reel_ID没有被禁用"+ ";Reel_ID:" + Reel_ID;
                LogHelper.Info("CheckReelForbidden:" + CheckReelForbiddenMessage);
                return true;
            }
            catch (Exception ex)
            {
                CheckReelForbiddenMessage = "检查Reel_ID是否被禁用异常;" + ";Reel_ID:" + Reel_ID + ";异常信息:" + ex.ToString();
                LogHelper.Info("CheckReelForbidden:" + CheckReelForbiddenMessage);
                return false;
            }
        }
        /// <summary>
        /// 检查上传胶水是否处于领用状态 Reel_ID="095-0009-F047H202000009
        /// </summary>
        ///  <remarks>注解 </remarks>
        /// <param name="Reel_ID">传入参数</param>
        /// <returns>true/false/Message</returns>
        public bool CheckGlueReceive(string Reel_ID, out string CheckGlueReceiveMessage)
        {
            try
            {
                LogHelper.Info("CheckGlueReceive;Reel_ID:" + Reel_ID);
                if (string.IsNullOrEmpty(Reel_ID))
                {
                    CheckGlueReceiveMessage = "未上传Reel_ID";
                    LogHelper.Info("CheckReelForbidden:" + CheckGlueReceiveMessage);
                    return false;
                }
                string SqlGlueReceiveMessage = "select CURRENT_STATUS from  sajet.g_material_glue  where REEL_NO='" + Reel_ID + "'";
                var dbSearch = new OracleRepository(A8oracleConnectionStr);
                DataTable dtReel = dbSearch.FindTable(SqlGlueReceiveMessage);
                if (dtReel.Rows.Count == 0 )
                {
                    CheckGlueReceiveMessage = "当前胶水状态不正确,无法使用" + ";Reel_ID:" + Reel_ID;
                    LogHelper.Info("CheckReelForbidden:" + CheckGlueReceiveMessage);
                    return false;
                }
                if(dtReel.Rows[0]["CURRENT_STATUS"].ToString ().Trim ()!= "P")
                {
                    CheckGlueReceiveMessage = "当前胶水状态不正确,无法使用" + ";Reel_ID:" + Reel_ID;
                    LogHelper.Info("CheckReelForbidden:" + CheckGlueReceiveMessage);
                    return false;
                }
                CheckGlueReceiveMessage = "检查当前胶水是否处于领用状态成功" + ";Reel_ID:" + Reel_ID;
                LogHelper.Info("CheckReelForbidden:" + CheckGlueReceiveMessage);
                return true;
            }
            catch (Exception ex)
            {
                CheckGlueReceiveMessage = "检查当前胶水是否处于领用状态异常;" + ";Reel_ID:" + Reel_ID + ";异常信息:" + ex.ToString();
                LogHelper.Info("CheckReelForbidden:" + CheckGlueReceiveMessage);
                return false;
            }
        }
        

    }

}
