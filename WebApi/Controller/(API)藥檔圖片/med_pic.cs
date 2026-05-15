using Basic;
using HIS_DB_Lib;
using Microsoft.AspNetCore.Mvc;
using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess.Client;
using SQLUI;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DB2VM_API.Controller._API_藥檔圖片
{
    [Route("api/[controller]")]
    [ApiController]
    public class med_pic : ControllerBase
    {
        static private string API_Server = "http://127.0.0.1:4433";
        static private MySqlSslMode SSLMode = MySqlSslMode.None;

        [HttpGet]
        public string get()
        {
            MyTimerBasic timerTotal = new MyTimerBasic();
            string HIS連線時間 = "";
            string HISData時間 = "";
            string DB寫入時間 = "";

            string conn_str = "Data Source=192.168.48.250:1521/sisdcp;User ID=hson_kutech;Password=uZ2cVm3NT3zv;";
            List<medClass> medClasses = medClass.get_med_cloud(API_Server);
            Dictionary<string, List<medClass>> keyValuePairs_med_cloud = medClasses.CoverToDictionaryByCode();
            try
            {
                using (var conn_oracle = new OracleConnection(conn_str))
                {
                    //===============================
                    // 1. 連線 HIS
                    //===============================
                    try
                    {
                        MyTimerBasic t = new MyTimerBasic();
                        conn_oracle.Open();
                        HIS連線時間 = t.ToString();
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"{ex.Message}, HIS系統連線失敗");
                        return $"{ex.Message},HIS系統連線失敗";
                    }

                    //===============================
                    // 2. 解析條碼 → 產生 SQL
                    //===============================
                    string commandText = "select * from v_meddsc";



                    //===============================
                    // 3. 執行查詢
                    //===============================
                    List<OrderClass> orderClasses = new List<OrderClass>();
                    List<medPicClass> localList = new List<medPicClass>();

                    using (var cmd = new OracleCommand(commandText, conn_oracle))
                    using (var reader = cmd.ExecuteReader())
                    {

                        try
                        {
                            while (reader.Read())
                            {
                                MyTimerBasic t2 = new MyTimerBasic();
                                HISData時間 = t2.ToString();
                                string MED_DIACODE = reader["MED_DIACODE"].ToString().Trim();

                                medClass _medClass = keyValuePairs_med_cloud.SortDictionaryByCode(MED_DIACODE).FirstOrDefault();
                                if (_medClass != null)
                                {
                                    byte[] blobData = null;

                                    int blobIndex = reader.GetOrdinal("MED_GRAPHIC");

                                    if (!reader.IsDBNull(blobIndex))
                                    {
                                        blobData = (byte[])reader.GetValue(blobIndex);
                                        string base64 = Convert.ToBase64String(blobData);

                                        medPicClass medPicClass = new medPicClass
                                        {
                                            藥碼 = MED_DIACODE,
                                            藥名 = _medClass.藥品名稱,
                                            副檔名 = "jpg",
                                            pic_base64 = base64,
                                        };
                                        localList.Add(medPicClass);

                                    }

                                }

                            }
                            medPicClass.init(API_Server);
                            for (int i = 0; i < localList.Count; i++)
                            {
                                medPicClass.add(API_Server, localList[i]);
                            }

                            returnData rd = new returnData()
                            {
                                Code = -200,
                                TimeTaken = timerTotal.ToString(),
                                Result = $"完成藥品圖片新增 共<{localList.Count}>筆"
                            };
                            return rd.JsonSerializationt(true);

                        }
                        catch (Exception e1)
                        {
                            returnData rd = new returnData()
                            {
                                Code = -200,
                                TimeTaken = timerTotal.ToString(),
                                Result = "HIS系統回傳資料異常!"
                            };
                            Logger.Log($"HIS資料讀取錯誤 {e1.Message}");
                            return rd.JsonSerializationt(true);
                        }
                    }

                    
                }
            }
            catch (Exception ex)
            {
                Logger.Log($" Exception: {ex.Message}");
                if (ex.Message.Contains("ORA-01000"))
                {
                    OracleConnection.ClearAllPools();
                    Logger.Log($" OracleConnection.ClearAllPools() ,清除所有DB連線");
                }
                return $"Exception: {ex.Message}";
            }
        }
    }
}
