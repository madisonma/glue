using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MES_LINK_VSKY.models
{
    public class Appsetting
    {
        private static ConnectionStrings _ConnectionStrings;
        public static ConnectionStrings ConnectionStrings { get { return _ConnectionStrings; } }
        /// <summary>
        /// 将配置项的值赋值给属性
        /// </summary>
        /// <param name="configuration"></param>
        public void Initial(IConfiguration configuration)
        {
            ConnectionStrings conn = new ConnectionStrings();
            //注意：可以使用冒号来获取内层的配置项
            conn.mesoracle = configuration["ConnectionStrings:mesoracle"];
            conn.fujioracle = configuration["ConnectionStrings:fujioracle"];
            conn.traceoracle= configuration["ConnectionStrings:traceoracle"];
            conn.NewMesTest = configuration["ConnectionStrings:NewMesTest"];
            conn.A8mesoracle = configuration["ConnectionStrings:A8mesoracle"];
            conn.DEPT_MES  = configuration["ConnectionStrings:DEPT_MES"];
            conn.SettingExpireTime = configuration["ExpireTimeSetting:GlueTime1"];
            _ConnectionStrings = conn;
        }
    }
}
