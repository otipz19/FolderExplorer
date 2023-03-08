using Microsoft.Data.SqlClient;

namespace FolderExplorer.Data
{
    public static class DBStartUp
    {
        public static void StartUp()
        {
            var backupFilePath = Path.Combine(AppContext.BaseDirectory, "FolderExplorer.bak");
            if (File.Exists(backupFilePath))
            {
                using var connection = new SqlConnection(new SqlConnectionStringBuilder()
                {
                    DataSource = "(localdb)\\mssqllocaldb",
                    IntegratedSecurity = true,
                }.ConnectionString);
                connection.Open();
                string sql = $"USE master RESTORE DATABASE FolderExplorer FROM DISK='{backupFilePath}'";
                using var command = new SqlCommand(sql, connection);
                command.ExecuteNonQuery();
                connection.Close();
                File.Delete(backupFilePath);
            }
        }
    }
}
