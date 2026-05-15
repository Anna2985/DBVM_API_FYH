using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using IBM.Data.DB2.Core;
using System.Data;
using System.Configuration;
using Basic;
using Oracle.ManagedDataAccess.Client;
using System.Text;
using HIS_DB_Lib;
using System.Linq.Expressions;



// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace DB2VM_API.Controller
{
    [Route("dbvm/[controller]")]
    [ApiController]
    public class BBCM : ControllerBase
    {
        static public string API_Server = "http://127.0.0.1:4433";


        [HttpGet]
        public string Get(string? Code)
        {
            try
            {
                string conn_str = "Data Source=192.168.48.250:1521/sisdcp;User ID=hson_kutech;Password=uZ2cVm3NT3zv;";
                OracleConnection conn_oracle = new OracleConnection(conn_str);
                conn_oracle.Open();
                string commandText = "";
                if (Code.StringIsEmpty()) commandText = $"select * from v_hisdrugdia";
                else commandText = $"select * from v_hisdrugdia where DIA_DIACODE='{Code}'";
                OracleCommand cmd = new OracleCommand(commandText, conn_oracle);
                List<object[]> list_v_hisdrugdia = new List<object[]>();
                var reader = cmd.ExecuteReader();
                List<string> columnNames = new List<string>();
                //for (int i = 0; i < reader.FieldCount; i++)
                //{
                //    string columnName = reader.GetName(i);
                //    columnNames.Add(columnName);
                //}

                List<medClass> medClasses_his = new List<medClass>();
                while (reader.Read())
                {
                    object[] value = new object[new enum_雲端藥檔().GetLength()];

                    medClass medClass = new medClass();

                    medClass.藥品碼 = reader["DIA_DIACODE"].ToString().Trim();
                    medClass.料號 = reader["DIA_SKDIACODE"].ToString().Trim();
                    medClass.中文名稱 = ConvertToBig5(reader["DIA_CNAME"].ToString().Trim());
                    medClass.藥品名稱 = reader["DIA_EGNAME"].ToString().Trim();
                    medClass.藥品學名 = reader["DIA_CHNAME"].ToString().Trim();
                    medClass.健保碼 = reader["DIA_INSCODE"].ToString().Trim();
                    medClass.包裝單位 = reader["DIA_ATTACHUNIT"].ToString().Trim();
                    medClass.最小包裝單位 = reader["DIA_UNIT"].ToString().Trim();
                    medClass.警訊藥品 = (reader["MED_HWARNING"].ToString().Trim() == "Y") ? "True" : "False";
                    medClass.管制級別 = reader["DIA_RESTRIC"].ToString().Trim();
                    medClass.類別 = reader["DIA_DRUGKINDNAME"].ToString().Trim();
                    //medClass.ATC = reader["DIA_ATCCODE"].ToString().Trim();
                    //medClass.懷孕用藥級別 = reader["MED_PREGNANCY"].ToString().Trim();
                    if (medClass.類別 == "中醫飲片" || medClass.類別 == "中藥" || medClass.類別 == "中藥錠劑" || medClass.類別 == "外用中藥")
                    {
                        medClass.中西藥 = "中藥";
                    }
                    else
                    {
                        medClass.中西藥 = "西藥";
                    }

                    medClasses_his.Add(medClass);

                }
                cmd.Dispose();
                conn_oracle.Close();
                conn_oracle.Dispose();
                returnData returnData_medCloud = medClass.add_med_clouds(API_Server, medClasses_his);

                return returnData_medCloud.JsonSerializationt(true);
            }
            catch(Exception ex)
            {
                return ex.Message;
            }
            
          

        }
        public static string ConvertToBig5(string input)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            // 將字串轉換為位元組陣列
            byte[] bytes = Encoding.Default.GetBytes(input);
            // 將位元組由預設編碼轉換為 BIG5 編碼
            byte[] big5Bytes = Encoding.Convert(Encoding.Default, Encoding.GetEncoding("BIG5"), bytes);
            // 取得轉換後的字串

            return Encoding.GetEncoding("BIG5").GetString(big5Bytes);
        }
    }
}
