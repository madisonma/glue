using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MES_LINK_VSKY.models.GlueMachine
{
    public class Input
    {
        public int CMD { get; set; }
        public string FunctionSwitch { get; set; }
        public string Description { get; set; }
        public string Work_Order { get; set; }
        public string Emp_ID { get; set; }
        public string Reel_ID { get; set; }
        public string Part_NO { get; set; }
        public string Tool_ID { get; set; }
        public string Machine_ID { get; set; }
        public string Machine_Name { get; set; }
        public string Vender { get; set; }
        public string Machine_Type { get; set; }
        public string Machine_SN { get; set; }
        public string Recipe_Name { get; set; }
        public string Tooling_SN { get; set; }
        public string Panel_SN { get; set; }
        public double ReduceCount { get; set; }
        public string ParameterA { get; set; }
        public string  ParameterB { get; set; }
        public string ParameterC { get; set; }
        public string ParameterD { get; set; }
        public string ParameterE{ get; set; }
        public string ParameterF{ get; set; }
        public string ParameterG { get; set; }
        public string ParameterH { get; set; }
        public string ParameterI{ get; set; }

    }
}
