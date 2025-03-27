using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Npgsql;

namespace HydroNode.Core
{
    public class DatabaseService
    {
#if DEBUG
        private readonly string _connStr =
            "Host=202.68.227.105;Port=54132;Username=postgres;Password=321qwedsa$RF%^YTH;Database=DroughtDiagnosis;Search Path=asan;";
#else
        private readonly string _connStr = 
            "Host=202.68.227.105;Port=54132;Username=postgres;Password=321qwedsa$RF%^YTH;Database=DroughtDiagnosis;Search Path=asan;";
#endif

        public async Task InsertDataAsync(string sensorDate, string sensorId, float waterLevel, float rainfall)
        {
            await using var conn = new NpgsqlConnection(_connStr);
            await conn.OpenAsync();

            await using var transaction = await conn.BeginTransactionAsync();

            try
            {
                DateTime baseDate = DateTime.ParseExact(sensorDate, "yyyyMMddHHmmss", CultureInfo.InvariantCulture);

                // Insert into tbl_get_wl_log (수위)
                var insertWlCmd = new NpgsqlCommand(@"
                    INSERT INTO tbl_get_wl_log (base_date, sensor_id, sensor_data, input_date)
                    VALUES (@base_date, @sensor_id, @sensor_data, now())", 
                    conn, transaction);

                insertWlCmd.Parameters.AddWithValue("base_date", baseDate);
                insertWlCmd.Parameters.AddWithValue("sensor_id", sensorId);
                insertWlCmd.Parameters.AddWithValue("sensor_data", waterLevel);

                await insertWlCmd.ExecuteNonQueryAsync();

                // Insert into tbl_get_rain_log (강우)
                var insertRainCmd = new NpgsqlCommand(@"
                    INSERT INTO tbl_get_rain_log (base_date, sensor_id, sensor_data, input_date)
                    VALUES (@base_date, @sensor_id, @sensor_data, now())", 
                    conn, transaction);

                insertRainCmd.Parameters.AddWithValue("base_date", baseDate);
                insertRainCmd.Parameters.AddWithValue("sensor_id", sensorId);
                insertRainCmd.Parameters.AddWithValue("sensor_data", rainfall);

                await insertRainCmd.ExecuteNonQueryAsync();

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                Console.WriteLine($"[ERROR] 트랜잭션 롤백됨: {ex.Message}");
                throw;
            }
        }
    }
}
