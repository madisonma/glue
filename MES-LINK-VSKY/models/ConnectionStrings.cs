using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MES_LINK_VSKY.models
{
    public class ConnectionStrings
    {
        public string NewMesTest { get; set; }
        public string fujioracle { get; set; }
        public string traceoracle { get; set; }
        public string mesoracle { get;  set; }
        public string A8mesoracle { get; set; }
        public string DEPT_MES { get; set; }
        public string SettingExpireTime { get; set; }
    }
}
