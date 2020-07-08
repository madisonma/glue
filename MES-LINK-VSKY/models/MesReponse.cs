using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MES_LINK_VSKY.models
{
    
    /// <summary>
    /// 通用响应
    /// </summary>
    public class MesReponse
    {
        /// <summary>
        /// 200 成功 500异常
        /// </summary>
        public int status { get; set; }
        public object data { get; set; }
        public string msg { get; set; }
    }
}
