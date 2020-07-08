using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MES_LINK_VSKY.models.GlueMachine;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MES_LINK_VSKY.Controllers
{
    [ApiExplorerSettings(GroupName = "Glue")]
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class Glue : ControllerBase
    {
        // GET: api/GlueMachine
        [HttpGet]
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/GlueMachine/5
        [HttpGet("{id}", Name = "Get")]
        public string Get(int id)
        {
            return "value";
        }

        /// <summary>
        /// HTTP Post接口---用于上传参数给Mes
        /// </summary>
        /// <remarks>参数范例：{“CMD”:1,” Description”:” Process End”,"Work_Order":" ST100-190800235","Emp_ID":" HA50628","Glue_ID":" 134-2233-1000303","Valve_ID":" 2134-2233-1000303","Machine_ID":" GL1000303","Recipe_Name":" RETTEA18888","Panel_SN":" W1A10003","Dot_Weight":0.2,"Current_Pressure":0.5,"Fluid_Pressure":0.5,"Valve_Pressure":0.5,"Temp":24.4}</remarks>
        /// <param name="[FromBody] Input">Input类</param>
        /// <returns>MesReponse</returns>
        [HttpPost]
        public Responce machinestart([FromBody] Input Input)
        {

            Excute Inter=new Excute ();
            Responce Res=Inter.HandleInfo  (Input);
            return  Res;
        }

        // PUT: api/GlueMachine/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody] string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
