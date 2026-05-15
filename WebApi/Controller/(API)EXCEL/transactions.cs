using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Basic;
using MyOffice;
using System.IO;
using NPOI;
using HIS_DB_Lib;
using System.Text.RegularExpressions;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DB2VM_API.Controller._API_EXCEL下載
{
    [Route("dbvm/[controller]")]
    [ApiController]
    public class transactions : ControllerBase
    {
        private static string API_Server = "http://127.0.0.1:4433";
        [HttpPost]
        public string get_datas_sheet([FromBody] returnData returnData)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            returnData.Method = "get_datas_sheet";
            try
            {

                if (returnData.ValueAry == null)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 無傳入資料";
                    return returnData.JsonSerializationt(true);
                }
                if (returnData.ValueAry.Count != 5)
                {
                    returnData.Code = -200;
                    returnData.Result = $"returnData.ValueAry 內容應為[藥碼][起始時間][結束時間][ServerName1,ServerName2][ServerType1,ServerType2]";
                    return returnData.JsonSerializationt(true);
                }
                string[] 藥碼Ary = returnData.ValueAry[0].Split(",");
                string 起始時間 = returnData.ValueAry[1];
                string 結束時間 = returnData.ValueAry[2];
                string serverName = returnData.ValueAry[3];
                string serverType = returnData.ValueAry[4];

                string[] ServerNames = serverName.Split(',');
                string[] ServerTypes = serverType.Split(',');
                if (藥碼Ary.Length == 0)
                {
                    returnData.Code = -200;
                    returnData.Result = $"[藥碼] 欄位異常 ,請用','分隔需搜尋藥碼";
                    return returnData.JsonSerializationt(true);

                }
                if (起始時間.Check_Date_String() == false)
                {
                    returnData.Code = -200;
                    returnData.Result = $"[起始時間] 為非法格式";
                    return returnData.JsonSerializationt(true);
                }
                if (結束時間.Check_Date_String() == false)
                {
                    returnData.Code = -200;
                    returnData.Result = $"[結束時間] 為非法格式";
                    return returnData.JsonSerializationt(true);
                }
                if (ServerNames.Length != ServerTypes.Length)
                {
                    returnData.Code = -200;
                    returnData.Result = $"ServerNames及ServerTypes長度不同";
                    return returnData.JsonSerializationt(true);
                }
                DateTime dateTime_st = 起始時間.StringToDateTime();
                DateTime dateTime_end = 結束時間.StringToDateTime();
                List<medClass> medClasses = medClass.get_med_cloud(API_Server);

                List<SheetClass> sheetClasses = new List<SheetClass>();
                List<List<transactionsClass>> list_transactionsClasses = new List<List<transactionsClass>>();
                藥碼Ary = 藥碼Ary.Distinct().ToArray();

                for (int k = 0; k < 藥碼Ary.Length; k++)
                {
                    string 藥碼 = 藥碼Ary[k];
                    
                    List<transactionsClass> transactionsClasses = transactionsClass.get_datas_by_code(API_Server, 藥碼, serverName, serverType);

                    transactionsClasses = (from temp in transactionsClasses
                                           where temp.操作時間.StringToDateTime() >= dateTime_st
                                           where temp.操作時間.StringToDateTime() <= dateTime_end
                                           select temp).ToList();

                    List<medClass> medClasses_buf = new List<medClass>();

                    medClasses_buf = (from value in medClasses
                                      where value.藥品碼.ToUpper() == 藥碼.ToUpper()
                                      select value).ToList();
                    if (medClasses_buf.Count == 0)
                    {
                        if (returnData.Value.StringIsEmpty())
                        {
                            return null;
                        }
                    }

                    string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "excel_emg_tradding.xlsx");
                    string loadText = MyOffice.ExcelClass.NPOI_LoadSheetsToJson_PreserveStyle(path);
                    List<SheetClass> sheetClasslist = loadText.JsonDeserializet<List<SheetClass>>();
                    SheetClass sheetClass = sheetClasslist[0];

                    //Logger.Log("excel_emg_tradding", $"{sheetClass.JsonSerializationt(true)}");

                    //Console.WriteLine($"取得creats {myTimerBasic.ToString()}");
                    int row_max = 60000;

                    //SheetClass sheetClass = loadText.JsonDeserializet<SheetClass>();
                    int 消耗量 = 0;
                    int NumOfRow = -1;
                    if (NumOfRow >= row_max || NumOfRow == -1)
                    {

                        sheetClass.Name = $"{藥碼}";
                        sheetClass.Rows[1].Cell[1].Text = $"{medClasses_buf[0].藥品碼}";
                        sheetClass.Rows[1].Cell[4].Text = $"{medClasses_buf[0].包裝單位}";
                        sheetClass.Rows[1].Cell[8].Text = $"{medClasses_buf[0].管制級別}";
                        sheetClass.Rows[1].Cell[10].Text = $"{起始時間}";
                        sheetClass.Rows[2].Cell[1].Text = $"{medClasses_buf[0].藥品名稱}";
                        sheetClass.Rows[2].Cell[8].Text = $"{medClasses_buf[0].藥品許可證號}";
                        sheetClass.Rows[2].Cell[10].Text = $"{結束時間}";

                        sheetClass.Rows[3].Cell[1].Text = $"{medClasses_buf[0].藥品學名}";
                        sheetClass.Rows[3].Cell[8].Text = $"{medClasses_buf[0].廠牌}";

                        sheetClass.Rows[4].Cell[2].Text = $"{medClasses_buf[0].供貨廠商}";
                        sheetClass.Rows[4].Cell[8].Text = $"{medClasses_buf[0].供貨商證字號}";

                        sheetClasses.Add(sheetClass);
                        NumOfRow = 0;
                    }

                   

                    for (int i = 0; i < transactionsClasses.Count; i++)
                    {
                        if (transactionsClasses[i].收支原因 == "交班對點") continue;
                        string 交易量 = transactionsClasses[i].交易量;
                        string 支出數 = "";
                        string 收入數 = "";
                        if (交易量.Contains("-"))
                        {
                            支出數 = 交易量.Replace("-", "");
                        }
                        else
                        {
                            收入數 = 交易量;
                        }
                        List<personPageClass> personPageClasses = personPageClass.get_all(API_Server);
                        personPageClass person1 = personPageClasses.searchByName(transactionsClasses[i].操作人);
                        personPageClass person2 = personPageClasses.searchByName(transactionsClasses[i].覆核藥師);
                        string person1_藥師證字號 = string.Empty;
                        string person2_藥師證字號 = string.Empty;
                        if (person1 != null && person1.藥師證字號.StringIsEmpty() == false) person1_藥師證字號 = $"({person1.藥師證字號})";
                        if (person2 != null && person2.藥師證字號.StringIsEmpty() == false) person2_藥師證字號 = $"({person2.藥師證字號})"; ;

                        消耗量 += transactionsClasses[i].交易量.StringToInt32();
                        sheetClass.AddNewCell_Webapi(NumOfRow + 6, 0, $"{i + 1}", "微軟正黑體", 12, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Center, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);
                        sheetClass.AddNewCell_Webapi(NumOfRow + 6, 1, $"{transactionsClasses[i].操作時間}", "微軟正黑體", 12, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Left, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);
                        sheetClass.AddNewCell_Webapi(NumOfRow + 6, 2, $"{transactionsClasses[i].收支原因}", "微軟正黑體", 12, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Center, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);
                        sheetClass.AddNewCell_Webapi(NumOfRow + 6, 3, $"{transactionsClasses[i].領藥號}", "微軟正黑體", 12, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Center, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);

                        sheetClass.AddNewCell_Webapi(NumOfRow + 6, 4, $"{transactionsClasses[i].病人姓名}", "微軟正黑體", 12, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Left, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);
                        sheetClass.AddNewCell_Webapi(NumOfRow + 6, 5, $"{transactionsClasses[i].病歷號}", "微軟正黑體", 12, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Center, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);

                        sheetClass.AddNewCell_Webapi(NumOfRow + 6, 6, $"{transactionsClasses[i].操作人}{person1_藥師證字號}", "微軟正黑體", 12, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Center, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);
                        //sheetClass.AddNewCell_Webapi(NumOfRow + 6, 7, $"{transactionsClasses[i].覆核藥師}({person2_藥師證字號})", "微軟正黑體", 14, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Center, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);
                        sheetClass.AddNewCell_Webapi(NumOfRow + 6, 7, $"{收入數}", "微軟正黑體", 12, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Center, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);
                        sheetClass.AddNewCell_Webapi(NumOfRow + 6, 8, $"{支出數}", "微軟正黑體", 12, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Center, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);
                        sheetClass.AddNewCell_Webapi(NumOfRow + 6, 9, $"{transactionsClasses[i].結存量}", "微軟正黑體", 12, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Center, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);
                        sheetClass.AddNewCell_Webapi(NumOfRow + 6, 10, $"{transactionsClasses[i].備註}", "微軟正黑體", 10, false, NPOI_Color.BLACK, 430, NPOI.SS.UserModel.HorizontalAlignment.Left, NPOI.SS.UserModel.VerticalAlignment.Bottom, NPOI.SS.UserModel.BorderStyle.Thin);
                        NumOfRow++;
                    }
                    string 上期結存 = string.Empty;
                    if (transactionsClasses.Count > 0)
                    {
                        上期結存 = transactionsClasses[0].庫存量;
                    }
                    sheetClass.Rows[1].Cell[6].Text = 上期結存;

                }

                returnData.Code = 200;
                returnData.Result = "Sheet取得成功!";
                returnData.Data = sheetClasses;
                return returnData.JsonSerializationt();
            }
            catch (Exception e)
            {
                returnData.Code = -200;
                returnData.Result = e.Message;
                return returnData.JsonSerializationt();
            }

        }
        /// <summary>
        /// 取得收支結存報表(Excel)(多台合併)
        /// </summary>
        /// <remarks>
        ///  --------------------------------------------<br/> 
        /// 以下為範例JSON範例
        /// <code>
        ///   {
        ///     "Data": 
        ///     {
        ///        
        ///     },
        ///     "ValueAry" : 
        ///     [
        ///       "藥碼1,藥碼2,藥碼",
        ///       "起始時間",
        ///       "結束時間",
        ///       "口服1,口服2",
        ///       "調劑台,調劑台"
        ///     ]
        ///   }
        /// </code>
        /// </remarks>
        /// <param name="returnData">共用傳遞資料結構</param>
        /// <returns>[returnData.Data]為交易紀錄結構</returns>
        [Route("download_cdmis_datas_excel")]
        [HttpPost]
        public async Task<ActionResult> download_cdmis_datas_excel([FromBody] returnData returnData)
        {
            try
            {
                MyTimerBasic myTimerBasic = new MyTimerBasic();
                myTimerBasic.StartTickTime(50000);

                returnData = get_datas_sheet(returnData).JsonDeserializet<returnData>();
                if (returnData.Code != 200)
                {
                    return null;
                }
                string jsondata = returnData.Data.JsonSerializationt();

                List<SheetClass> sheetClasses = jsondata.JsonDeserializet<List<SheetClass>>();

                string xlsx_command = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                string xls_command = "application/vnd.ms-excel";

                byte[] excelData = sheetClasses.NPOI_GetBytes(Excel_Type.xlsx);
                Stream stream = new MemoryStream(excelData);
                return await Task.FromResult(File(stream, xlsx_command, $"{DateTime.Now.ToDateString("-")}_收支結存簿冊.xlsx"));
            }
            catch
            {
                return null;
            }

        }
        private (string 效期, string 批號) spliteNote(string note)
        {
            var match = Regex.Match(note, @"\[效期\]:(?<exp>[^,\[\]]+),\[批號\]:(?<lot>[^,\[\]]+)");
            string 效期 = "";
            string 批號 = "";
            if (match.Success)
            {
                效期 = match.Groups["exp"].Value;
                批號 = match.Groups["lot"].Value;
            }
            效期 = $"[效期]:{效期}";
            return (效期, 批號);
        }
    }
}
