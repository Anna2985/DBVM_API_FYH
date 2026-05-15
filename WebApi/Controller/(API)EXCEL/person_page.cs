using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HIS_DB_Lib;
using Basic;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text;
using MyOffice;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DBVM_API.Controller._API_EXCEL
{
    [Route("api/[controller]")]
    [ApiController]
    public class person_page : ControllerBase
    {
        private static string API_Server = "http://127.0.0.1:4433";
        [Route("excel_upload")]
        [HttpPost]
        public async Task<string> excel_upload([FromForm] IFormFile file)
        {
            returnData returnData = new returnData();
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            myTimerBasic.StartTickTime(50000);
            try
            {

                returnData.Method = "excel_upload";
                var formFile = Request.Form.Files.FirstOrDefault();

                if (formFile == null)
                {
                    returnData.Code = -200;
                    returnData.Result = "文件不得為空";
                    return returnData.JsonSerializationt(true);
                }

                string extension = Path.GetExtension(formFile.FileName); // 获取文件的扩展名
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                List<personPageClass> personPageClasses = personPageClass.get_all(API_Server);
                List<personPageClass> personPageClasses_add = new List<personPageClass>();

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    await formFile.CopyToAsync(memoryStream);
                    System.Data.DataTable dt = ExcelClass.NPOI_LoadFile(memoryStream.ToArray(), extension);
                    dt = dt.ReorderTable(new enum_人員());
                    if (dt == null)
                    {
                        returnData.Code = -200;
                        returnData.Result = "上傳文件表頭無效!";
                        return returnData.JsonSerializationt(true);
                    }
                    List<object[]> list_value = dt.DataTableToRowList();
                    if (list_value.Count == 0)
                    {
                        returnData.Code = -200;
                        returnData.Result = $"文件內容不得為空";
                        return returnData.JsonSerializationt(true);
                    }
                    for (int i = 0; i < list_value.Count; i++)
                    {
                        string id = list_value[i][(int)enum_人員.員工編號].ObjectToString();
                        personPageClass personPage_buff = personPageClasses.FirstOrDefault(x => x.ID == id);
                        if (personPage_buff == null)
                        {
                            personPageClass personPageClass = new personPageClass();
                            personPageClass.ID = id;
                            personPageClass.密碼 = id;
                            personPageClass.姓名 = list_value[i][(int)enum_人員.姓名].ObjectToString();
                            personPageClass.單位 = "藥劑科";
                            //personPageClass.藥師證字號 = list_value[i][(int)enum_人員.藥師證書字號].ObjectToString();
                            personPageClasses_add.Add(personPageClass);
                        }
                        //else
                        //{
                        //    personPage_buff.藥師證字號 = list_value[i][(int)enum_人員.藥師證書字號].ObjectToString();
                        //    personPageClasses_add.Add(personPage_buff);
                        //}
                    }
                }
                return personPageClass.add(API_Server, personPageClasses_add).JsonSerializationt(true);

            }

            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = $"{e.Message}";
                return returnData.JsonSerializationt(true);
            }
        }
    }
    public enum enum_人員
    {
        姓名,
        員工編號,
    }
}
