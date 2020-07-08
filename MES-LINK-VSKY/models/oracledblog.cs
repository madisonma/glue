using SQLBuilder.Core.Repositories;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

namespace MES_LINK_VSKY.models
{
    public class oracledblog
    {
        private static string oracleConnectionStr = Appsetting.ConnectionStrings.mesoracle;
        public static bool oraclelog(string terminal_id, string work_order, string log_detail, string log_status)
        {
            var dbSearch = new OracleRepository(oracleConnectionStr);
            string sql = "insert into vsky.tsc_log(terminal_id,time,work_order,logdetail,status) values(:terminal_id,SYSDATE,:work_order,:log_detail,:log_status)";
            var td = dbSearch.ExecuteBySql(sql, new { terminal_id = terminal_id, work_order= work_order, log_detail = log_detail, log_status = log_status});
            //int num = td.Rows.Count;
            if (td == 0)
            { return false; }
            else
            { return true; }
        }
    }
}
