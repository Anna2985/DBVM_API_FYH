using Basic;
using Google.Protobuf.WellKnownTypes;
using HIS_DB_Lib;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyOffice;
using MySql.Data.MySqlClient;
using SQLUI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DBVM_API.Controller._API_EXCEL
{
    [Route("dbvm/[controller]")]
    [ApiController]
    public class inspection : ControllerBase
    {
        static private string API_Server = "http://192.168.12.164:4433/api/serversetting";
        [Route("excel_upload")]
        [HttpPost]
        public async Task<string> POST_excel([FromForm] IFormFile file, [FromForm] string IC_NAME, [FromForm] string PON, [FromForm] string CT)
        {
            try
            {
                var formFile = Request.Form.Files.FirstOrDefault();
                string filename = formFile.FileName;
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    await formFile.CopyToAsync(memoryStream);
                    byte[] file_bytes = memoryStream.ToArray();
                    List<string> names = new List<string>();
                    List<string> values = new List<string>();
                    names.Add("IC_NAME");
                    names.Add("PON");
                    names.Add("CT");
                    values.Add(IC_NAME);
                    values.Add(PON);
                    values.Add(CT);


                    string json_result = Basic.Net.WEBApiPost("http://192.168.12.164:4433/api/inspection/excel_upload", filename, file_bytes, names, values);
                    return json_result;

                }
            }
            catch (Exception e)
            {
                return $"{e.Message}";
            }

        }
        [Route("excel_download_by_IC_SN")]
        [HttpPost]
        public async Task<ActionResult> Post_excel_download_by_IC_SN([FromBody] returnData returnData)
        {
            MyTimerBasic myTimer = new MyTimerBasic();
            myTimer.StartTickTime(50000);

            List<sys_serverSettingClass> serverSettingClasses = sys_serverSettingClassMethod.WebApiGet($"{API_Server}");
            serverSettingClasses = serverSettingClasses.MyFind("Main", "網頁", "VM端");
            if (serverSettingClasses.Count == 0)
            {
                returnData.Code = -200;
                returnData.Result = $"找無Server資料!";
                return null;
            }
            string Server = serverSettingClasses[0].Server;
            string DB = serverSettingClasses[0].DBName;
            string UserName = serverSettingClasses[0].User;
            string Password = serverSettingClasses[0].Password;
            uint Port = (uint)serverSettingClasses[0].Port.StringToInt32();

            string json = POST_creat_get_by_IC_SN(returnData);
            returnData = json.JsonDeserializet<returnData>();

            if (returnData.Code != 200)
            {
                return null;
            }
            List<inspectionClass.creat> creats = returnData.Data.ObjToListClass<inspectionClass.creat>();
            inspectionClass.creat creat = creats[0];
            List<SheetClass> sheetClasses = new List<SheetClass>();
            SheetClass sheetClass = new SheetClass();
            sheetClass.AddNewCell_Webapi(0, 0, 0, 0, $"{"請購單號_項次"}", "微軟正黑體", 14, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Left, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);
            sheetClass.AddNewCell_Webapi(0, 0, 1, 1, $"{"驗收數量"}", "微軟正黑體", 14, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Left, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);
            sheetClass.AddNewCell_Webapi(0, 0, 2, 2, $"{"批號"}", "微軟正黑體", 14, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Left, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);
            sheetClass.AddNewCell_Webapi(0, 0, 3, 3, $"{"效期"}", "微軟正黑體", 14, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Left, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);
            int index = 1;
            for (int i = 0; i < creat.Contents.Count; i++)
            {
                for (int k = 0; k < creat.Contents[i].Sub_content.Count; k++)
                {
                    sheetClass.AddNewCell_Webapi(index, index, 0, 0, $"{creat.Contents[i].請購單號}", "微軟正黑體", 14, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Left, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);
                    sheetClass.AddNewCell_Webapi(index, index, 1, 1, $"{creat.Contents[i].Sub_content[k].實收數量}", "微軟正黑體", 14, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Left, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);
                    sheetClass.AddNewCell_Webapi(index, index, 2, 2, $"{creat.Contents[i].Sub_content[k].批號}", "微軟正黑體", 14, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Left, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);
                    sheetClass.AddNewCell_Webapi(index, index, 3, 3, $"{creat.Contents[i].Sub_content[k].效期.StringToDateTime().ToDateString().Replace("/", "")}", "微軟正黑體", 14, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Left, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);
                    index++;
                }

            }
            string xlsx_command = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            string xls_command = "application/vnd.ms-excel";

            byte[] excelData = sheetClass.NPOI_GetBytes(Excel_Type.xlsx);
            Stream stream = new MemoryStream(excelData);
            return await Task.FromResult(File(stream, xlsx_command, $"{DateTime.Now.ToDateString("-")}_驗收表.xlsx"));
        }
        [Route("creat_get_by_IC_SN")]
        [HttpPost]
        public string POST_creat_get_by_IC_SN([FromBody] returnData returnData)
        {
            string json_result = Basic.Net.WEBApiPostJson("http://192.168.12.164:4433/api/inspection/creat_get_by_IC_SN", returnData.JsonSerializationt());
            return json_result;

        }
    }
}
