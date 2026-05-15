using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using HIS_DB_Lib;
using Basic;

namespace DB2VM_API
{
    public class medicineClass
    {

        [JsonPropertyName("stockCode")]
        public string 藥品碼 { get; set; }
        [JsonPropertyName("chiName")]
        public string 中文名稱 { get; set; }
        [JsonPropertyName("fullName")]
        public string 藥品名稱 { get; set; }
        [JsonPropertyName("medicationName")]
        public string 藥品學名 { get; set; }
        [JsonPropertyName("unit")]
        public string 最小包裝單位 { get; set; }
        [JsonPropertyName("isAlarm")]
        public bool 警訊藥品 { get; set; }
        [JsonPropertyName("controlLevel")]
        public int 管制級別 { get; set; }
        [JsonPropertyName("stopFlag")]
        public string 開檔狀態 { get; set; }
        [JsonPropertyName("code")]
        public string 料號 { get; set; }
        [JsonPropertyName("healthInsurance")]
        public HealthInsurance healthInsurance { get; set; }
        //[JsonPropertyName("self")]
        //public List<Self> self { get; set; }
        static public List<medicineClass> get_med(string code)
        {
            string url = $"http://192.168.16.230:8132/api/Medication/GetMedicationList?stockcode={code}";
            returnData returnData = new returnData();
            string json_out = Net.WEBApiGet(url);
            List<medicineClass> medicineClasses = json_out.JsonDeserializet<List<medicineClass>>();

            return medicineClasses;
        } 
    }
    public class HealthInsurance
    {
        [JsonPropertyName("price")]
        public double 成本價 { get; set; }
        [JsonPropertyName("atc7")]
        public string ATC { get; set; }
    }
    public class Self
    {
        [JsonPropertyName("price")]
        public double 售價 { get; set; }
    }

}
