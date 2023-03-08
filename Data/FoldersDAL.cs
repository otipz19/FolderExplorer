using System.Data;
using Microsoft.Data.SqlClient;
using FolderExplorer.Models;

namespace FolderExplorer.Data
{
    public class FoldersDAL
    {
        private string _connectionString;
        private SqlConnection _connection;

        public FoldersDAL(): this(new SqlConnectionStringBuilder()
        {
            DataSource = "(localdb)\\mssqllocaldb",
            IntegratedSecurity = true,
            InitialCatalog = "FolderExplorer",
        }.ConnectionString) { }

        public FoldersDAL(string connectionString)
        {
            _connectionString = connectionString;
        }

        public void OpenConnection()
        {
            _connection = new SqlConnection(_connectionString);
            _connection.Open();
        }

        public void CloseConnection()
        {
            if(_connection != null && _connection.State != ConnectionState.Closed)
            {
                _connection.Close();
            }
        }

        public Folder GetFolder(int id)
        {
            return QueryFolders($"SELECT * FROM Folders WHERE Id = @id",
                new SqlParameter("@id", id))
                .FirstOrDefault();
        }

        public List<Folder> GetSubfolders(Folder folder)
        {
            return GetSubfolders(folder.Id);
        }

        public List<Folder> GetSubfolders(int id)
        {
            return QueryFolders($"SELECT * FROM Folders WHERE ParentId = @parentId",
                new SqlParameter("@parentId", id))
                .ToList();
        }

        public FolderViewModel GetFolderViewModel(int id)
        {
            var folder = GetFolder(id);
            return new FolderViewModel()
            {
                Folder = folder,
                Subfolders = GetSubfolders(folder)
            };
        }

        public void CreateFolder(int parentId, string name)
        {
            OpenConnection();
            using var command = new SqlCommand($"INSERT INTO Folders (ParentId, Name) VALUES(@parentId, @name)", _connection);
            command.Parameters.Add(new SqlParameter("@parentId", parentId));
            command.Parameters.Add(new SqlParameter("@name", name));
            try
            {
                command.ExecuteNonQuery();
            }
            catch(SqlException ex)
            {
                throw ex;
            }
            finally
            {
                CloseConnection();
            }
        }

        public void RenameFolder(int id, string name)
        {
            OpenConnection();
            using var command = new SqlCommand($"UPDATE Folders SET Name = @name WHERE Id = @id", _connection);
            command.Parameters.Add(new SqlParameter("@name", name));
            command.Parameters.Add(new SqlParameter("@id", id));
            command.ExecuteNonQuery();
            CloseConnection();
        }

        /// <returns>False if deletion was aborted</returns>
        public bool DeleteFolder(int id)
        {
            bool result = true;
            var subfolders = GetSubfolders(id);
            if(subfolders.Count == 0)
            {
                OpenConnection();
                using var command = new SqlCommand($"DELETE FROM Folders WHERE Id = @id", _connection);
                command.Parameters.Add(new SqlParameter("@id", id));
                try
                {
                    command.ExecuteNonQuery();
                }
                catch (SqlException ex)
                {
                    return false;
                }
                finally 
                {
                    CloseConnection();
                }
                return true;
            }
            else
            {
                foreach(var subfolder in subfolders)
                {
                    result &= DeleteFolder(subfolder.Id);
                }
                DeleteFolder(id);
                return result;
            }
        }

        private IEnumerable<Folder> QueryFolders(string query, params SqlParameter[] parameters)
        {
            OpenConnection();
            using var command = new SqlCommand(query, _connection);
            if(parameters != null && parameters.Length > 0)
                command.Parameters.AddRange(parameters);
            using var dataReader = command.ExecuteReader();
            while (dataReader.Read())
            {
                yield return new Folder()
                {
                    Id = ConvertFromDBVal<int>(dataReader["Id"]),
                    ParentId = ConvertFromDBVal<int>(dataReader["ParentId"]),
                    Name = ConvertFromDBVal<string>(dataReader["Name"]),
                };
            }
            CloseConnection();
        }

        private T ConvertFromDBVal<T>(object obj)
        {
            if (obj == null || obj == DBNull.Value)
            {
                return default(T);
            }
            return (T)obj;
        }
    }
}
