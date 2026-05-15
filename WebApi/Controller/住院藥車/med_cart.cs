using Basic;
using HIS_DB_Lib;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using SQLUI;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DBVM_API.Controller.住院藥車
{
    [Route("api/[controller]")]
    [ApiController]
    public class med_cart : ControllerBase
    {
        static private string API01 = "http://127.0.0.1:4433";
        private static readonly string conn_str = "Data Source=192.168.166.220:1521/sisdcp;User ID=hson_kutech;Password=6w1xPDQnsnw3kO;";
        private string API_Server = "http://192.168.12.164:4433";
        private List<string> 大瓶藥 = new List<string>() { "IBFL", "IDEX", "IDEX1", "IGLU", "IGLU5", "ILIPO", "IMAN1","INOR", "INOR1", "INOR3", "INOR9", "INS2", "INS21","INS3"
        ,"ISOD3","ISOR","ISOT","ISOT2","ISOT3","ITAI","ITAI5"};

        private string PHAOPDSOA(string PATID)
        {
            MyTimerBasic myTimer_total = new MyTimerBasic();
            string HIS連線時間 = "";
            string HISData時間 = "";
            string DB寫入時間 = "";

            try
            {
                using (var conn_oracle = new OracleConnection(conn_str))
                {
                    //===============================
                    // 1. 連線至 HIS
                    //===============================
                    try
                    {
                        MyTimerBasic t1 = new MyTimerBasic();
                        conn_oracle.Open();
                        HIS連線時間 = t1.ToString();
                    }
                    catch (Exception ex)
                    {
                        return $"{ex.Message},HIS系統連接失敗!";
                    }

                    //===============================
                    // 2. 解析條碼 → CommandText
                    //===============================
                    string today = DateTime.Now.ToString("yyyyMMdd");
                    string commandText = $"SELECT * from  PHAOPDSOA WHERE  SOA_PATID = '{PATID}' AND SOA_VISITDT = '{today}'";

                    if (commandText.StringIsEmpty())
                    {
                        returnData rd = new returnData()
                        {
                            Code = -200,
                            Result = "BarCode 格式無法解析!"
                        };
                        return rd.JsonSerializationt(true);
                    }

                    //===============================
                    // 3. 執行查詢（強化版，不洩漏 cursor）
                    //===============================
                    List<diseaseClass> diseaseClasses = new List<diseaseClass>();

                    using (var cmd = new OracleCommand(commandText, conn_oracle))
                    using (var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                    {
                        MyTimerBasic t_query = new MyTimerBasic();
                        HISData時間 = t_query.ToString();

                        while (true)
                        {
                            bool hasRow = false;

                            //--- 防止 Read() 拋例外造成 Cursor 卡在 HIS
                            try
                            {
                                hasRow = reader.Read();
                            }
                            catch (Exception ex)
                            {
                                return $"HIS系統資料讀取異常 (Read)：{ex.Message}";
                            }

                            if (!hasRow) break;

                            //--- 單筆資料解析（不可拋例外）
                            try
                            {
                                diseaseClass disease = new diseaseClass();


                                //====== 基本欄位 ======
                                string 疾病 = SafeGet(reader, "SOA_CONTENT");
                                disease.疾病代碼 = 疾病.Split("_").Length >= 2 ? 疾病.Split("_")[0] : "";
                                disease.中文說明 = 疾病.Split("_").Length >= 2 ? 疾病.Split("_")[1] : "";




                                diseaseClasses.Add(disease);
                            }
                            catch (Exception ex)
                            {
                                return $"HIS系統資料解析異常 (Row)：{ex.Message}";
                            }
                        }
                    }                                     
                    conn_oracle.Close();
                    return diseaseClasses.JsonSerializationt(true);
                }
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("ORA-01000"))
                {
                    OracleConnection.ClearAllPools();
                }
                return $"Exception : {ex.Message}";
            }
        }
        [HttpGet("GetUD_Data")]
        public async Task<string> GetUD_Data()
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            try
            {

                //DateTime date = DateTime.Now.AddDays(-1); ;
                DateTime date = DateTime.Now;

                DateTime start = date.GetStartDate();
                DateTime end = date.GetEndDate();


                List<OrderClass> orderClasses = OrderClass.get_by_creat_time_st_end(API01, start, end);
                List<List<OrderClass>> orders = orderClasses.GroupBy(s => s.藥局代碼).Select(g => g.ToList()).ToList();

                foreach (var phar_order in orders)
                {
                    //string 藥局 = getPharName(phar_order[0].藥局代碼);
                    //string 護理站 = getCartName(phar_order[0].藥局代碼);
                    string 藥局 = "住院藥局";
                    string 護理站 = phar_order[0].藥局代碼;
                    if (藥局.StringIsEmpty() || 護理站.StringIsEmpty()) continue;
                    List<patientInfoClass> patientInfoClasses = new List<patientInfoClass>();
                    Dictionary<string, List<OrderClass>> dic_order = ToDictByPatient(phar_order);
                    List<List<OrderClass>> order_buff = phar_order.GroupBy(s => s.病歷號).Select(g => g.ToList()).ToList();
                    foreach (var list_order in order_buff)
                    {
                        string 床號 = list_order[0].床號.Trim();
                        if (床號.StringIsEmpty()) 床號 = list_order[0].領藥號.Trim();
                        patientInfoClass patientInfoClass = new patientInfoClass();
                        patientInfoClass.GUID = Guid.NewGuid().ToString();
                        patientInfoClass.更新時間 = DateTime.Now.ToDateTimeString();
                        patientInfoClass.調劑時間 = DateTime.MinValue.ToDateTimeString();
                        patientInfoClass.PRI_KEY = list_order[0].病歷號.Trim();
                        patientInfoClass.藥局 = 藥局;
                        patientInfoClass.護理站 = 護理站;
                        patientInfoClass.床號 = $"{list_order[0].病房}-{床號}";
                        patientInfoClass.病歷號 = list_order[0].病歷號.Trim();
                        patientInfoClass.住院號 = list_order[0].住院序號.Trim();
                        patientInfoClass.姓名 = list_order[0].病人姓名.Trim();
                        patientInfoClass.入院日期 = list_order[0].開方日期.Trim();
                        patientInfoClass.檢驗數值異常 = "||";

                        patientInfoClass.占床狀態 = "已佔床";
                        string 疾病 = PHAOPDSOA(patientInfoClass.病歷號);
                        List<diseaseClass> diseaseClasses = 疾病.JsonDeserializet<List<diseaseClass>>();
                        if (diseaseClasses != null) 
                        {
                            List<string> 疾病代碼 = diseaseClasses.Select(x => x.疾病代碼).ToList();
                            List<string> 中文說明 = diseaseClasses.Select(x => x.中文說明).ToList();
                            patientInfoClass.疾病代碼 = string.Join(";", 疾病代碼);
                            patientInfoClass.疾病說明 = string.Join(";", 中文說明);

                        }


                        patientInfoClasses.Add(patientInfoClass);
                    }
                    returnData returnData_update_patient = patientInfoClass.update_patientInfo(API01, patientInfoClasses);
                    if (returnData_update_patient == null || returnData_update_patient.Code != 200) return $"{藥局} {護理站}處方取得失敗";
                    List<patientInfoClass> update_patient = returnData_update_patient.Data.ObjToClass<List<patientInfoClass>>();
                    List<medCpoeClass> medCpoeClasses = new List<medCpoeClass>();
                    for (int i = 0; i < update_patient.Count(); i++)
                    {
                        List<OrderClass> orders_ = GetByPatient(dic_order, update_patient[i].病歷號);
                        string now = DateTime.Now.ToDateTimeString();
                        for (int j = 0; j < orders_.Count(); j++)
                        {
                            medCpoeClass medCpoeClass = new medCpoeClass();
                            medCpoeClass.GUID = Guid.NewGuid().ToString();

                            medCpoeClass.Master_GUID = update_patient[i].GUID;
                            medCpoeClass.PRI_KEY = orders_[j].批序;
                            medCpoeClass.序號 = orders_[j].批序;
                            medCpoeClass.更新時間 = now;
                            medCpoeClass.藥局 = update_patient[i].藥局;
                            medCpoeClass.護理站 = update_patient[i].護理站;
                            medCpoeClass.姓名 = update_patient[i].姓名;
                            medCpoeClass.床號 = update_patient[i].床號.Trim();
                            medCpoeClass.病歷號 = update_patient[i].病歷號.Trim();
                            medCpoeClass.住院號 = update_patient[i].住院號.Trim();
                            medCpoeClass.開始時間 = orders_[j].開方日期;
                            medCpoeClass.結束時間 = orders_[j].開方日期;
                            medCpoeClass.藥碼 = orders_[j].藥品碼;
                            medCpoeClass.頻次 = orders_[j].頻次;
                            medCpoeClass.藥品名 = orders_[j].藥品名稱;
                            medCpoeClass.途徑 = orders_[j].途徑;
                            medCpoeClass.數量 = orders_[j].交易量.Replace("-", "").StringToDouble().ToString();
                            medCpoeClass.劑量 = orders_[j].單次劑量;
                            medCpoeClass.單位 = orders_[j].劑量單位;
                            if (medCpoeClass.藥碼.StartsWith("O")) 
                            { 
                                medCpoeClass.排序 = $"A{medCpoeClass.藥碼}";
                                medCpoeClass.備註 = "手包";

                            }
                            if (medCpoeClass.藥碼.StartsWith("E")) medCpoeClass.排序 = $"B{medCpoeClass.藥碼}";
                            if (medCpoeClass.藥碼.StartsWith("I")) medCpoeClass.排序 = $"C{medCpoeClass.藥碼}";
                            if (medCpoeClass.藥碼.StartsWith("O") && orders_[j].備註.StringIsEmpty() == false && orders_[j].備註.Contains("藥包機")) 
                            {
                                medCpoeClass.排序 = $"D{medCpoeClass.藥碼}";
                                medCpoeClass.備註 = "";
                            }
                           
                            if (medCpoeClass.藥碼.StartsWith("I") && 大瓶藥.Contains(medCpoeClass.藥碼)) medCpoeClass.排序 = $"E{medCpoeClass.藥碼}";

                            if (medCpoeClass.排序.StringIsEmpty()) medCpoeClass.排序 = $"Z{medCpoeClass.藥碼}";



                            medCpoeClasses.Add(medCpoeClass);

                        }
                    }
                    //if (medCpoeClasses.Count > 0) return medCpoeClasses.JsonSerializationt(true);
                    returnData returnData_medCpoeClass = medCpoeClass.update_med_cpoe(API01, medCpoeClasses);
                    if (returnData_medCpoeClass == null || returnData_medCpoeClass.Code != 200)
                    {
                        returnData_medCpoeClass.Data = medCpoeClasses;
                        return returnData_medCpoeClass.JsonSerializationt(true);
                    }
                }
                return $"資料更新 {myTimerBasic.ToString()}";

            }
            catch (Exception ex)
            {
                // 可依需要記錄 Log 或回傳錯誤訊息
                return $"Error: {ex.Message}";
            }
        }
        [HttpGet("GetUD")]
        public async Task<string> GetUD(string? datetime)
        {
            MyTimerBasic myTimerBasic = new MyTimerBasic();
            try
            {

                
                string apiUrl = Basic.Net.WEBApiGet($"http://127.0.0.1:4434/api/UD?datetime={datetime}");
                Console.WriteLine($"取得order結束 \n {apiUrl}");
                Console.WriteLine("寫入UD開始");

                string UD = Basic.Net.WEBApiGet("http://127.0.0.1:4434/api/med_Cart/GetUD_Data");
                Console.WriteLine($"取得order結束 \n {UD}");

                return $"取得藥車資料成功";

            }
            catch (Exception ex)
            {
                // 可依需要記錄 Log 或回傳錯誤訊息
                return $"取得藥車紀錄失敗";
            }
        }


        static public Dictionary<string, List<OrderClass>> ToDictByPatient(List<OrderClass> order)
        {
            Dictionary<string, List<OrderClass>> dictionary = new Dictionary<string, List<OrderClass>>();
            foreach (var item in order)
            {
                if (dictionary.TryGetValue(item.病歷號, out List<OrderClass> list))
                {
                    list.Add(item);
                }
                else
                {
                    dictionary[item.病歷號] = new List<OrderClass> { item };
                }
            }
            return dictionary;
        }
        static public List<OrderClass> GetByPatient(Dictionary<string, List<OrderClass>> dict, string patient)
        {
            if (dict.TryGetValue(patient, out List<OrderClass> OrderClasses))
            {
                return OrderClasses;
            }
            else
            {
                return new List<OrderClass>();
            }
        }
        private static string SafeGet(OracleDataReader r, string col)
        {
            try
            {
                return r[col]?.ToString()?.Trim() ?? "";
            }
            catch
            {
                return "";
            }
        }
    }
}
