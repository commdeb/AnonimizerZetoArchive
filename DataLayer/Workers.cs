using Microsoft.Data.Sqlite;
using System.Data;
using System.Transactions;

namespace DataLayer.Workers
{
    public class SqLiteUnitOfWork : UnitOfWork
    {
        //Deafult connection string
        public static readonly string DefaultSqLiteConnectionString = "Data Source=./Data/anonimizer_zeto_archive.db;Cache=Shared;Mode=ReadWriteCreate;";


        private readonly SqliteConnection _connection;
        private readonly string _connectionString;
        private SqliteCommand _command;


        public SqLiteUnitOfWork(SqliteConnection connection, SqliteCommand command) : base(new SqLiteCommandWrapper(command))
        {
            _connection = connection;
            _command = command;
            _command.Connection = _connection;
            _connectionString = connection.ConnectionString;
        }

        public SqLiteUnitOfWork(string connectionString, SqliteCommand command) : this(new SqliteConnection(connectionString), command){ }

        public override ICommand Command { get => new SqLiteCommandWrapper(_command); set => _command = ((SqLiteCommandWrapper) value).InnerCommand; }

        public override void BeginTransaction()
        {
            throw new NotImplementedException();
        }

        public override void CommitTransaction()
        {
            throw new NotImplementedException();
        }

        public override void RollbackTransaction()
        {
            throw new NotImplementedException();
        }

        public override void Dispose()
        {
           _command?.Dispose();
           _connection?.Dispose(); 
        }

        public override void ExecuteCommand() 
        {
            try
            {
                int returnCode = ExecuteSql();
                if (returnCode < 0)
                    throw new SqliteException($"Error executing SQL command:{Environment.NewLine}{_command.CommandText}", returnCode);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing SQL command:{Environment.NewLine}{_command.CommandText}", ex);
            }
            finally
            {
                _connection.Close();
            }
        }

        public override DT ExecuteCommand<DT>() 
        {
            DT dt;
            try
            {
                dt = ExecuteSqlReturnDataTable<DT>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing SQL command:{Environment.NewLine}{_command.CommandText}", ex);
            }
            finally
            {
                _connection.Close();
            }
            return dt;
        }

        public override async Task ExecuteCommandAsync()
        {
            try
            {
                var task = Task.Run(() => ExecuteSql());
                int returnCode = await task;

                if(task.IsFaulted) throw task.Exception;

                if (returnCode < 0)
                    throw new SqliteException($"Error executing SQL command:{Environment.NewLine}{_command.CommandText}", returnCode);
            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing SQL command:{Environment.NewLine}{_command.CommandText}", ex);
            }
            finally
            {
                _connection.Close();
            }
        }

        public override async Task<DT> ExecuteCommandAsync<DT>() 
        {
            DT dt;
            try
            {
                var task = Task.Run(() => ExecuteSqlReturnDataTable<DT>());
                dt = await task;

                if (task.IsFaulted) throw task.Exception;

            }
            catch (Exception ex)
            {
                throw new Exception($"Error executing SQL command:{Environment.NewLine}{_command.CommandText}", ex);
            }
            finally
            {
                _connection.Close();
            }

            return dt;
        } 
        
        
        
        public int ExecuteSql(string sql = "", CommandType commandType = CommandType.Text, Dictionary<string, object>? parameters = null)
        {
            _command.Parameters.Clear();

            if (!string.IsNullOrWhiteSpace(sql))
                _command.CommandText = sql;

            _command.CommandType = commandType;

            if(parameters != null)
                foreach (var param in parameters)
                {
                    _command.Parameters.AddWithValue(param.Key, param.Value);
                }

            _command.Prepare();
            _connection.Open();
            int returnCode = _command.ExecuteNonQuery();
            _connection.Close();

            return returnCode;
        }

        public DT ExecuteSqlReturnDataTable<DT>(string sql = "", CommandType commandType = CommandType.Text, Dictionary<string, object>? parameters = null) where DT : DataTable
        {
            _command.Parameters.Clear();
            
            if(!string.IsNullOrWhiteSpace(sql))
                _command.CommandText = sql;

            _command.CommandType = commandType;
           
            if (parameters != null)
                foreach (var param in parameters)
                {
                    _command.Parameters.AddWithValue(param.Key, param.Value);
                }

            _command.Prepare();
            _connection.Open();
            using var reader = _command.ExecuteReader();
            DataTable table = new DataTable();
            table.Load(reader);
            _connection.Close();
            return (DT) table;
        }

        public struct SqLiteCommandWrapper(SqliteCommand command) : ICommand<DataTable> 
        {
            public SqliteCommand InnerCommand => command; 
        }
        
    }
}
