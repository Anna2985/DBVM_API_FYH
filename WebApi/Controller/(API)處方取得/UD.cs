using Basic;
using HIS_DB_Lib;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using SQLUI;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DBVM_API.Controller._API_處方取得
{
    [Route("api/[controller]")]
    [ApiController]
    public class UD : ControllerBase
    {
        public enum enum_急診藥袋
        {
            本次領藥號,
            看診日期,
            病歷號,
            序號,
            頻率,
            途徑,
            總量,
            前次領藥號,
            本次醫令序號,
        }
        private static readonly string conn_str = "Data Source=192.168.166.220:1521/sisdcp;User ID=hson_kutech;Password=6w1xPDQnsnw3kO;";
        private string API_Server = "http://192.168.12.164:4433";
        private List<string> 大瓶藥排除 = new List<string>() { "IISOT","IALB","IMAN","IROL","ISOD2"};
        [HttpGet]
        public string Get(string? datetime)
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
                        Logger.Log($"{ex.Message}, HIS系統連接失敗");
                        return $"{ex.Message},HIS系統連接失敗!";
                    }

                    //===============================
                    // 2. 解析條碼 → CommandText
                    //===============================
                    string today = DateTime.Now.ToString("yyyyMMdd");
                    string commandText = $"SELECT * from  phaadcal WHERE  PAC_TYPE = 'L' AND PAC_DIACODE != 'XX88' AND PAC_VISITDT = '{today}' AND PAC_BEDNO NOT LIKE 'DH%'";
                    if (datetime.StringIsEmpty() == false)
                    {
                        commandText = $"SELECT * from  phaadcal WHERE  PAC_TYPE = 'L' AND PAC_DIACODE != 'XX88' AND PAC_VISITDT = '{datetime}' AND PAC_BEDNO LIKE 'DH%'";
                    }

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
                    List<OrderClass> orderClasses = new List<OrderClass>();

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
                                OrderClass orderClass = new OrderClass();

                                //====== 藥袋類型 ======
                                string type = SafeGet(reader, "PAC_TYPE");
                                orderClass.藥袋類型 = type switch
                                {
                                    "E" => "急診",
                                    "S" => "住院ST",
                                    "B" => "住院首日量",
                                    "O" => "門診",
                                    "M" => "出院帶藥",
                                    "L" => "住院配藥車",
                                    _ => ""
                                };
                                string 藥包機 = SafeGet(reader, "PAC_MACHINE") == "Y" ? "藥包機" : "";
                                //====== 基本欄位 ======
                                //orderClass.藥袋條碼 = datetime.StringIsEmpty()? ;
                                orderClass.住院序號 = SafeGet(reader, "PAC_SEQ");
                                orderClass.藥品碼 = SafeGet(reader, "PAC_DIACODE");
                                orderClass.藥品名稱 = SafeGet(reader, "PAC_DIANAME");
                                orderClass.病人姓名 = SafeGet(reader, "PAC_PATNAME");
                                orderClass.病歷號 = SafeGet(reader, "PAC_PATID");
                                orderClass.領藥號 = SafeGet(reader, "PAC_DRUGNO");
                                orderClass.科別 = SafeGet(reader, "PAC_SECTNAME");
                                orderClass.醫師代碼 = SafeGet(reader, "PAC_DOCNAME");
                                orderClass.頻次 = SafeGet(reader, "PAC_FEQNO");
                                orderClass.天數 = SafeGet(reader, "PAC_DAYS");
                                orderClass.單次劑量 = SafeGet(reader, "PAC_QTYPERTIME");
                                orderClass.劑量單位 = SafeGet(reader, "PAC_UNIT");
                                orderClass.費用別 = SafeGet(reader, "PAC_PAYCD") == "Y" ? "自費" : "健保";
                                orderClass.批序 = SafeGet(reader, "PAC_ORDERSEQ");
                                orderClass.病房 = SafeGet(reader, "PAC_BEDNO").Split("-")[0];
                                orderClass.床號 = SafeGet(reader, "PAC_BEDNO").Split("-").Length >=2 ? SafeGet(reader, "PAC_BEDNO").Split("-")[1]:"";
                                orderClass.備註 = 藥包機;
                                if (orderClass.病房.StartsWith("3")) orderClass.藥局代碼 = "3B";
                                if (orderClass.病房.StartsWith("6")) orderClass.藥局代碼 = "6B";
                                if (orderClass.病房.StartsWith("70")) orderClass.藥局代碼 = "RCW";
                                if (orderClass.病房.StartsWith("71")) orderClass.藥局代碼 = "RCW";
                                if (orderClass.病房.StartsWith("72")) orderClass.藥局代碼 = "RCW";
                                if (orderClass.病房.StartsWith("73")) orderClass.藥局代碼 = "RCW";
                                if (orderClass.病房.StartsWith("74")) orderClass.藥局代碼 = "RCW";
                                if (orderClass.病房.StartsWith("7") && orderClass.藥局代碼.StringIsEmpty()) orderClass.藥局代碼 = "7B";
                                if (orderClass.病房.StartsWith("8")) orderClass.藥局代碼 = "8B";
                                if (orderClass.病房.StartsWith("22")) orderClass.藥局代碼 = "2A";
                                if (orderClass.病房.StartsWith("A2")) orderClass.藥局代碼 = "2A";
                                if (orderClass.病房.StartsWith("A3")) orderClass.藥局代碼 = "3A";
                                if (orderClass.病房.StartsWith("ICU")) orderClass.藥局代碼 = "ICU";
                                if (orderClass.病房.StartsWith("DH")) orderClass.藥局代碼 = "DH";



                                //====== 就醫時間 ======
                                string visit = SafeGet(reader, "PAC_VISITDT");
                                if (visit.Length == 8)
                                    orderClass.就醫時間 = $"{visit[..4]}-{visit[4..6]}-{visit[6..8]}";

                                //====== 開方日期 ======
                                string 時間 = SafeGet(reader, "PAC_PROCDTTM");
                                if (時間.Length == 14)
                                {
                                    orderClass.開方日期 =
                                        $"{時間[..4]}/{時間[4..6]}/{時間[6..8]} " +
                                        $"{時間[8..10]}:{時間[10..12]}:{時間[12..14]}";
                                }

                                //====== 交易量（負值） ======
                                double sumQTY = SafeDouble(reader, "PAC_SUMQTY");
                                orderClass.交易量 = (-sumQTY).ToString();

                                //====== PRI_KEY ======
                                string key = $"{orderClass.頻次}{orderClass.天數}{orderClass.單次劑量}{orderClass.劑量單位}";
                                orderClass.PRI_KEY = $"{時間}-{orderClass.病歷號}-{orderClass.藥品碼}{orderClass.交易量}-{key}";
                                if (orderClass.藥品碼.StartsWith("XXFD") == false && 大瓶藥排除.Contains(orderClass.藥品碼) == false && (orderClass.藥品碼.StartsWith("I") || orderClass.藥品碼.StartsWith("O") || orderClass.藥品碼.StartsWith("E")))
                                    orderClasses.Add(orderClass);
                            }
                            catch (Exception ex)
                            {
                                return $"HIS系統資料解析異常 (Row)：{ex.Message}";
                            }
                        }
                    }

                    //===============================
                    // 4. 無資料處理
                    //===============================
                    if (orderClasses.Count == 0)
                    {
                        returnData rd = new returnData()
                        {
                            Code = -200,
                            TimeTaken = myTimer_total.ToString(),
                            Result = "無此藥袋資料!"
                        };
                        return rd.JsonSerializationt(true);
                    }

                    //===============================
                    // 5. 寫入資料庫
                    //===============================
                    MyTimerBasic t_db = new MyTimerBasic();
                    List<List<OrderClass>> orders = orderClasses
                    .GroupBy(x => x.病歷號)
                    .Select(g => g.ToList())
                    .ToList();
                    List<OrderClass> add = new List<OrderClass>();
                    foreach(var item in orders)
                    {
                        List<OrderClass> classes = item
                         .Where(x => x.藥品碼.StringIsEmpty() == false)
                         .GroupBy(x => new { x.藥品碼, x.頻次, x.途徑 })
                         .Select(g =>
                         {
                             var first = g.First(); // 取一筆當作其他欄位來源
                             double 交易量 = g.Sum(x => x.交易量.StringToDouble());
                             first.交易量 = 交易量.ToString("0.00");
                             return first;
                         })
                         .ToList();
                        add.AddRange(classes);
                    }
                    //if (add.Count > 0) return add.JsonSerializationt(true);
                    var returnData_order = OrderClass.update_UDorder_list("http://127.0.0.1:4433", add);
                    DB寫入時間 = t_db.ToString();

                    //returnData_order.Value = BarCode;
                    returnData_order.TimeTaken += $"{myTimer_total}";
                    returnData_order.Result += $"，HIS連線時間:{HIS連線時間}，取得HIS資料:{HISData時間}，DB寫入時間:{DB寫入時間}";

                    string json = returnData_order.JsonSerializationt(true);
                    //Logger.Log(json);
                    conn_oracle.Close();
                    return json;
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Exception :{ex.Message}");
                if (ex.Message.Contains("ORA-01000"))
                {
                    OracleConnection.ClearAllPools();
                    Logger.Log($" OracleConnection.ClearAllPools() ,清除所有DB連線");
                }
                return $"Exception : {ex.Message}";
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

        private static double SafeDouble(OracleDataReader r, string col)
        {
            try
            {
                double.TryParse(r[col]?.ToString(), out double v);
                return v;
            }
            catch
            {
                return 0;
            }
        }

       
    }
}
