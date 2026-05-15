using System;
using Basic;
using Oracle.ManagedDataAccess.Client;

namespace ConsoleApp_medPageCloud
{
    class Program
    {
        static void Main(string[] args)
        {
            string connString =
            "User Id=hson_kutech;" +
            "Password=\"3edc#$56^YHN\";" +
            "Data Source=192.168.120.123:1521/sisdcp;" +   // 這裡用小寫 sisdcp
            "Connection Timeout=60;";

            try
            {
                Console.WriteLine("測試連線");

                using (var conn = new OracleConnection(connString))
                {
                    conn.Open();
                }
                Console.WriteLine($"Oracle Connecting success! , {connString}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Oracle Connecting failed! , {ex.GetType().Name} : {ex.Message}");
            }

        }
    }
}
