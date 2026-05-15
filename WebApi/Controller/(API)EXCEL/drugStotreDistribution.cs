using Basic;
using Google.Protobuf.WellKnownTypes;
using HIS_DB_Lib;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MyOffice;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DBVM_API.Controller._API_EXCEL
{
    [Route("dbvm/[controller]")]
    [ApiController]
    public class drugStotreDistribution : ControllerBase
    {
        [Route("excel_upload")]
        [HttpPost]
        public async Task<string> POST_excel_upload([FromForm] IFormFile file, [FromForm] string op_name)
        {
            returnData returnData = new returnData();
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            try
            {


                List<medClass> medClasses = medClass.get_med_cloud("http://127.0.0.1:4433");
                if (medClasses == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "ServerSetting VM端設定異常!";
                    return returnData.JsonSerializationt(true);
                }

                returnData.Method = "POST_excel_upload";
                var formFile = Request.Form.Files.FirstOrDefault();

                if (formFile == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "文件不得為空";
                    return returnData.JsonSerializationt(true);
                }

                string extension = Path.GetExtension(formFile.FileName); // 获取文件的扩展名
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                inventoryClass.creat creat = new inventoryClass.creat();
                string error = "";
                List<drugStotreDistributionClass> drugStotreDistributionClasses = new List<drugStotreDistributionClass>();
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    await formFile.CopyToAsync(memoryStream);
                    System.Data.DataTable dt = ExcelClass.NPOI_LoadFile(memoryStream.ToArray(), extension);
                    //dt = dt.ReorderTable(new enum_撥補單上傳_Excel());
                    //if (dt == null)
                    //{
                    //    returnData.Code = -200;
                    //    returnData.Result = "上傳文件表頭無效!";
                    //    return returnData.JsonSerializationt(true);
                    //}
                    List<object[]> list_value = dt.DataTableToRowList();


                    for (int i = 0; i < list_value.Count; i++)
                    {
                        string 藥碼 = list_value[i][1].ObjectToString();
                        medClass medClass = medClasses.FirstOrDefault(temp => temp.藥品碼 == 藥碼 || temp.料號 == 藥碼);
                        if (medClass == null) continue;
                        drugStotreDistributionClass rowvalue = new drugStotreDistributionClass
                        {
                            來源庫別 = "藥庫",
                            目的庫別 = "藥局",
                            藥碼 = medClass.藥品碼,
                            藥名 = medClass.藥品名稱,
                            包裝單位 = medClass.最小包裝單位,
                            撥發量 = list_value[i][4].ObjectToString(),
                            報表名稱 = op_name,
                            狀態 = "等待過帳"
                        };
                        drugStotreDistributionClasses.Add(rowvalue);
                    }
                }
                drugStotreDistributionClass.add("http://127.0.0.1:4433", drugStotreDistributionClasses);
                returnData.Result = "資料新增成功";
                returnData.Code = 200;
                returnData.Data = drugStotreDistributionClasses;
                return returnData.JsonSerializationt(true);

            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = $"{e.Message}";
                return returnData.JsonSerializationt(true);
            }
        }
        [Route("download_excel_by_addTime")]
        [HttpPost]
        public async Task<ActionResult> download_excel_by_addTime([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            returnData.Method = "download_excel_by_addTime";
            try
            {
                if (returnData.ValueAry.Count != 2)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[起始時間][結束時間]";
                    return null;
                }
                string 起始時間 = returnData.ValueAry[0];
                string 結束時間 = returnData.ValueAry[1];
                if (起始時間.Check_Date_String() == false || 結束時間.Check_Date_String() == false)
                {
                    returnData.Code = -200;
                    returnData.Result = $"時間範圍格式錯誤";
                    return null;
                }
                DateTime dateTime_st = 起始時間.StringToDateTime();
                DateTime dateTime_end = 結束時間.StringToDateTime();
                List<medClass> med_cloud = medClass.get_med_cloud("http://127.0.0.1:4433");
                //List<medClass> med_cloud = medClass.get_med_cloud("https://pharma-cetrlm.tph.mohw.gov.tw:4443");
                List<medClass> med_cloud_buf = new List<medClass>();
                Dictionary<string, List<medClass>> keyValuePairs_cloud = med_cloud.CoverToDictionaryByCode();
                //List<drugStotreDistributionClass> drugStotreDistributionClasses = drugStotreDistributionClass.get_by_addedTime("https://pharma-cetrlm.tph.mohw.gov.tw:4443", dateTime_st, dateTime_end);
                List<drugStotreDistributionClass> drugStotreDistributionClasses = drugStotreDistributionClass.get_by_addedTime("http://127.0.0.1:4433", dateTime_st, dateTime_end);
                List<object[]> list_drugStotreDistributionClasses = new List<object[]>();
                for (int i = 0; i < drugStotreDistributionClasses.Count; i++)
                {
                    object[] value = new object[new enum_drugStotreDistribution_Excel_exprot().GetLength()];
                    med_cloud_buf = keyValuePairs_cloud.SortDictionaryByCode(drugStotreDistributionClasses[i].藥碼);
                    value[(int)enum_drugStotreDistribution_Excel_exprot.物品代碼] = drugStotreDistributionClasses[i].藥碼;
                    if (med_cloud_buf.Count > 0)
                    {
                        value[(int)enum_drugStotreDistribution_Excel_exprot.物品代碼] = med_cloud_buf[0].料號;
                    }

                    value[(int)enum_drugStotreDistribution_Excel_exprot.撥補數量] = drugStotreDistributionClasses[i].實撥量;
                    list_drugStotreDistributionClasses.Add(value);
                }

                System.Data.DataTable dataTable = list_drugStotreDistributionClasses.ToDataTable(new enum_drugStotreDistribution_Excel_exprot());
                string xlsx_command = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                string xls_command = "application/vnd.ms-excel";

                byte[] excelData = dataTable.NPOI_GetBytes(Excel_Type.xlsx, new[] { (int)enum_drugStotreDistribution_Excel_exprot.撥補數量 });
                Stream stream = new MemoryStream(excelData);
                return await Task.FromResult(File(stream, xlsx_command, $"{DateTime.Now.ToDateString("-")}_申領明細.xlsx"));
            }
            catch (Exception ex)
            {
                return null;
            }
            finally
            {

            }

        }
    }
    public enum enum_drugStotreDistribution_Excel_exprot
    {
        物品代碼,
        撥補數量,
    }
    
}
