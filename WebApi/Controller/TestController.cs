using Basic;
using HIS_DB_Lib;
using IBM.Data.DB2.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Configuration.Provider;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
namespace DB2VM
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        
        // GET api/values
        [HttpGet]
        public string Get()
        {


            string connString =
            "User Id=hson_kutech;" +
            "Password=\"uZ2cVm3NT3zv\";" +
            "Data Source=192.168.48.250:1521/sisdcp;" +   // 這裡用小寫 sisdcp
            "Connection Timeout=60;";

            try
            {
                using (var conn = new OracleConnection(connString))
                {
                    conn.Open();
                }

                return $"Oracle Connecting success! , {connString}";
            }
            catch (Exception ex)
            {
                return $"Oracle Connecting failed! , {ex.GetType().Name} : {ex.Message}";
            }


        }
        

    }
}
