using Microsoft.Data.Sqlite;
using PLC_Inovance.Models;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PLC_Inovance.Services
{
    internal class DatabaseService
    {
        private readonly string _connectionString =
            "Data Source=plc.db";
       
        public void Init()
        {
            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS PlcLog (
                    Id          INTEGER PRIMARY KEY AUTOINCREMENT,
                    TimeStamp   TEXT NOT NULL,
                    PLC_IP      TEXT NOT NULL DEFAULT '',
                    X0          INTEGER NOT NULL DEFAULT 0,
                    X1          INTEGER NOT NULL DEFAULT 0,
                    X2          INTEGER NOT NULL DEFAULT 0,
                    X3          INTEGER NOT NULL DEFAULT 0,
                    X4          INTEGER NOT NULL DEFAULT 0,
                    X5          INTEGER NOT NULL DEFAULT 0,
                    X6          INTEGER NOT NULL DEFAULT 0,
                    X7          INTEGER NOT NULL DEFAULT 0,
                    D0          INTEGER,
                    D1          INTEGER,
                    D2          INTEGER,
                    D3          INTEGER,
                    D4          INTEGER,
                    D5          INTEGER,
                    D6          INTEGER,
                    D7          INTEGER,
                                IsRunning   INTEGER NOT NULL DEFAULT 0,
                                HasAlarm    INTEGER NOT NULL DEFAULT 0
                );
            ";
            cmd.ExecuteNonQuery();

            // Create indexes separately
            cmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_timestamp ON PlcLog(TimeStamp);";
            cmd.ExecuteNonQuery();

            cmd.CommandText = "CREATE INDEX IF NOT EXISTS idx_plc_ip ON PlcLog(PLC_IP, TimeStamp);";
            cmd.ExecuteNonQuery();
        }

        public void Insert(PlcData data)
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            if (string.IsNullOrWhiteSpace(data.PLC_IP))
                throw new ArgumentException("PLC_IP không được để trống");
            if (data.X == null || data.X.Length < 8)
                throw new ArgumentException("data.X phải có ít nhất 8 phần tử");
            if (data.D == null || data.D.Length < 8)
                throw new ArgumentException("data.D phải có ít nhất 8 phần tử");

            try
            {
                using var conn = new SqliteConnection(_connectionString);
                conn.Open();

                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    INSERT INTO PlcLog (
                        TimeStamp, PLC_IP,
                        X0, X1, X2, X3, X4, X5, X6, X7,
                        D0, D1, D2, D3, D4, D5, D6, D7,
                        IsRunning, HasAlarm
                    ) VALUES (
                        $time, $plc_ip,
                        $x0, $x1, $x2, $x3, $x4, $x5, $x6, $x7,
                        $d0, $d1, $d2, $d3, $d4, $d5, $d6, $d7,
                        $running, $alarm
                    );
                ";

                cmd.Parameters.AddWithValue("$time", data.TimeStamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                cmd.Parameters.AddWithValue("$plc_ip", data.PLC_IP);

                cmd.Parameters.AddWithValue("$x0", data.X[0] ? 1 : 0);
                cmd.Parameters.AddWithValue("$x1", data.X[1] ? 1 : 0);
                cmd.Parameters.AddWithValue("$x2", data.X[2] ? 1 : 0);
                cmd.Parameters.AddWithValue("$x3", data.X[3] ? 1 : 0);
                cmd.Parameters.AddWithValue("$x4", data.X[4] ? 1 : 0);
                cmd.Parameters.AddWithValue("$x5", data.X[5] ? 1 : 0);
                cmd.Parameters.AddWithValue("$x6", data.X[6] ? 1 : 0);
                cmd.Parameters.AddWithValue("$x7", data.X[7] ? 1 : 0);

                cmd.Parameters.AddWithValue("$d0", data.D[0]);
                cmd.Parameters.AddWithValue("$d1", data.D[1]);
                cmd.Parameters.AddWithValue("$d2", data.D[2]);
                cmd.Parameters.AddWithValue("$d3", data.D[3]);
                cmd.Parameters.AddWithValue("$d4", data.D[4]);
                cmd.Parameters.AddWithValue("$d5", data.D[5]);
                cmd.Parameters.AddWithValue("$d6", data.D[6]);
                cmd.Parameters.AddWithValue("$d7", data.D[7]);

                cmd.Parameters.AddWithValue("$running", data.IsRunning ? 1 : 0);
                cmd.Parameters.AddWithValue("$alarm", data.HasAlarm ? 1 : 0);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Lỗi insert DB: {ex.Message}");
                // Có thể throw hoặc log vào file/rtbLog tùy nhu cầu
            }
        }

        public List<PLClOG> GetAllLogs()
        {
            var list = new List<PLClOG>();

            using var conn = new SqliteConnection(_connectionString);
            conn.Open();

            string sql = "SELECT * FROM PlcLog ORDER BY Id DESC";

            using var cmd = new SqliteCommand(sql, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new PLClOG
                {
                    TimeStamp = DateTime.Parse(reader["TimeStamp"].ToString()),
                    PLC_IP = reader["PLC_IP"].ToString(),

                    X0 = Convert.ToInt32(reader["X0"]) == 1,
                    X1 = Convert.ToInt32(reader["X1"]) == 1,
                    X2 = Convert.ToInt32(reader["X2"]) == 1,
                    X3 = Convert.ToInt32(reader["X3"]) == 1,
                    X4 = Convert.ToInt32(reader["X4"]) == 1,
                    X5 = Convert.ToInt32(reader["X5"]) == 1,
                    X6 = Convert.ToInt32(reader["X6"]) == 1,
                    X7 = Convert.ToInt32(reader["X7"]) == 1,

                    D0 = reader["D0"] as int?,
                    D1 = reader["D1"] as int?,
                    D2 = reader["D2"] as int?,
                    D3 = reader["D3"] as int?,
                    D4 = reader["D4"] as int?,
                    D5 = reader["D5"] as int?,
                    D6 = reader["D6"] as int?,
                    D7 = reader["D7"] as int?,

                    IsRunning = Convert.ToInt32(reader["IsRunning"]) == 1,
                    HasAlarm = Convert.ToInt32(reader["HasAlarm"]) == 1
                });
            }

            return list;
        }
    }
}
