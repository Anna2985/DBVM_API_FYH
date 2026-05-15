using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json.Serialization;
using Basic;
using HIS_DB_Lib;

namespace DB2VM_API
{
    public class medPictureClass
    {
        [JsonPropertyName("imageBase64")]
        public string pic_base64 { get; set; }
        [JsonPropertyName("code")]
        public string 藥碼 { get; set; }
        [JsonPropertyName("fullName")]
        public string 藥名 { get; set; }

        static public medPictureClass get_pic(string code)
        {
            string url = $"http://192.168.16.230:8132/api/Medication/GetMedicationImage?Code={code}";
            returnData returnData = new returnData();
            string json_out = Net.WEBApiGet(url);
            medPictureClass medPicClass = json_out.JsonDeserializet<medPictureClass>();
            return medPicClass;
        }
    }
}
