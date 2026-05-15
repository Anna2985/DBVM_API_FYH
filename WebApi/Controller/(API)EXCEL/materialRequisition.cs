using Basic;
using Google.Protobuf.WellKnownTypes;
using HIS_DB_Lib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyOffice;
using MySql.Data.MySqlClient;
using NPOI;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using SQLUI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ubiety.Dns.Core;


// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DBVM_API.Controller._API_EXCEL
{
    [Route("dbvm/[controller]")]
    [ApiController]
    public class materialRequisition : ControllerBase
    {
        
        [HttpPost]
        public async Task<ActionResult> Post_download_excel_by_requestTime([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            returnData.Method = "download_excel_by_requestTime";

            //try
            //{
            if (returnData.ValueAry == null || returnData.ValueAry.Count != 2)
            {
                returnData.Code = -200;
                returnData.Result = $"returnData.ValueAry 內容應為[起始時間][結束時間]";
                return BadRequest(returnData);
            }

            string 起始時間 = returnData.ValueAry[0];
            string 結束時間 = returnData.ValueAry[1];

            if (!起始時間.Check_Date_String() || !結束時間.Check_Date_String())
            {
                returnData.Code = -200;
                returnData.Result = $"時間範圍格式錯誤";
                return BadRequest(returnData);
            }

            DateTime dateTime_st = 起始時間.StringToDateTime();
            DateTime dateTime_end = 結束時間.StringToDateTime();

            List<medClass> med_cloud = medClass.get_med_cloud("http://127.0.0.1:4433");
            List<medClass> med_cloud_buf = new List<medClass>();
            Dictionary<string, List<medClass>> keyValuePairs_cloud = med_cloud.CoverToDictionaryByCode();

            List<materialRequisitionClass> materialRequisitionClasses = materialRequisitionClass.get_by_requestTime("http://127.0.0.1:4433", dateTime_st, dateTime_end);

            List<object[]> list_materialRequisitionClasses = new List<object[]>();
            for (int i = 0; i < materialRequisitionClasses.Count; i++)
            {
                object[] value = new object[new enum_materialRequisition_Excel_exprot().GetLength()];
                med_cloud_buf = keyValuePairs_cloud.SortDictionaryByCode(materialRequisitionClasses[i].藥碼);
                value[(int)enum_materialRequisition_Excel_exprot.物品代碼] = materialRequisitionClasses[i].藥碼;

                if (med_cloud_buf.Count > 0)
                {
                    value[(int)enum_materialRequisition_Excel_exprot.物品代碼] = med_cloud_buf[0].料號;
                }

                value[(int)enum_materialRequisition_Excel_exprot.請領數量] = materialRequisitionClasses[i].實撥量;
                list_materialRequisitionClasses.Add(value);
            }

            System.Data.DataTable dataTable = list_materialRequisitionClasses.ToDataTable(new enum_materialRequisition_Excel_exprot());
            string xlsx_command = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

            byte[] excelData = dataTable.NPOI_GetBytes(Excel_Type.xlsx, new[] { (int)enum_materialRequisition_Excel_exprot.請領數量 });
            Stream stream = new MemoryStream(excelData);

            return File(stream, xlsx_command, $"{DateTime.Now.ToDateString("-")}_申領明細.xlsx");
            //}
            //catch (Exception ex)
            //{
            //    return StatusCode(500, new
            //    {
            //        Code = -500,
            //        Result = $"例外錯誤：{ex.Message}"
            //    });
            //}
        }
    }
    public enum enum_materialRequisition_Excel_exprot
    {
        物品代碼,
        請領數量,
    }
    
}
