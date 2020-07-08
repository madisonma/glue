using SQLBuilder.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MES_LINK_VSKY.models.Glue
{
    public class CheckMachineParamsClass
    {
        //数据库连接读取配置
        private string NewMesTest = Appsetting.ConnectionStrings.mesoracle;
        public string CheckMachineParamsByID(string MachineID,string reel_id)
        {
            try
            {
                string sqlGetID = "select TERMINAL_NAME from SYS_TERMINAL where TERMINAL_ID='"+ MachineID+"'";
                var dbSearch = new OracleRepository(NewMesTest);
                DataTable dtMachineName = dbSearch.FindTable(sqlGetID);
                if(dtMachineName.Rows .Count ==0)
                {
                    LogHelper.Info("NG:根据机台ID查找不到机台名称"+ MachineID);
                    return "NG:根据机台ID查找不到机台名称";
                }
                string MachineType = dtMachineName.Rows[0][0].ToString ();
                //判断机台类型，分为Masking,Sensor和VCN1,VCN2
                if(MachineType.Contains ("Masking"))
                {
                    MachineType = "Masking";
                }
                else if(MachineType.Contains("Glue staking"))
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
                DataTable dtMachineParam = dbSearch.FindTable(sqlGetID);
                if (dtMachineParam.Rows.Count == 0)
                {
                    LogHelper.Info("NG:根据机台名称查找不到机台胶水料号" + MachineID);
                    return "NG:根据机台名称查找不到机台胶水料号";
                }
                LogHelper.Info("查询胶水料号成功;" + dtMachineParam.Rows[0][0].ToString()+";"+ MachineID);
                reel_id = reel_id.Substring(0,14);//截取前十四位位胶水料号
                if(reel_id!= dtMachineParam.Rows[0][0].ToString())
                {
                    LogHelper.Info("NG:当前机台应该使用的胶水料号不正确" + MachineID);
                    return "NG:当前机台使用的胶水料号不正确";
                }
                return dtMachineParam.Rows[0][0].ToString ();

            }
            catch (Exception ex)
            {
                LogHelper.Info("NG:查询机台胶水料号异常"+ex.ToString ()+"请联系系统工程师解决");
                return "NG:查询机台胶水料号异常" + ex.ToString() + "请联系系统工程师解决";
            }
        }
    }
}
