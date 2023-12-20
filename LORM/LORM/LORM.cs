using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Reflection;



namespace LORM
{
    public class BetterDisposable : IDisposable
    {
        private bool _disposed = false;
        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                // TODO: dispose managed state (managed objects).
            }

            // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
            // TODO: set large fields to null.

            _disposed = true;
        }
    }


    public class DbObject<T> : BetterDisposable
    {
        public static string? BoundName { get; set; }
        public static string? PrimaryKey { get; set; }
        public T? Value { get; set; }

        public T? GetElementById(int id)
        {
            string query = $"SELECT * FROM {BoundName} WHERE {PrimaryKey}=@id";
            return GenericDB.Instance.ExecuteQueryFirst<T>(query, new { id });
        }

        public List<T> ExecuteQuery(string query, object parameters = null)
        {
            return GenericDB.Instance.ExecuteQuery<T>(query, parameters);
        }

        public void Insert(T entity)
        {
            var properties = entity.GetType().GetProperties();
            var columnNamesList = new List<string>();
            var parameterNamesList = new List<string>();
            var parameters = new Dictionary<string, object>();

            foreach (var property in properties)
            {
                var propertyName = property.Name;
                var parameterName = $"@{propertyName}";
                var propertyValue = property.GetValue(entity);

                if (propertyValue != null && propertyName != PrimaryKey)
                {
                    Console.WriteLine($"Property name: {propertyName}, property value: {propertyValue}, primary key: {PrimaryKey}");
                    columnNamesList.Add(propertyName);
                    parameterNamesList.Add(parameterName);
                    parameters.Add(parameterName, propertyValue);
                }
            }

            var columnNames = string.Join(", ", columnNamesList);
            var parameterNames = string.Join(", ", parameterNamesList);

            string query = $"INSERT INTO {BoundName} ({columnNames}) VALUES ({parameterNames})";
            GenericDB.Instance.ExecuteNonQuery(query, parameters);
        }

        public void BulkInsert(List<T> entities)
        {
            // get the properties of the first entity in the list without primary key
            var properties = entities[0].GetType().GetProperties().Where(p => p.Name != PrimaryKey);
            var columnNamesList = new List<string>();
            var parameterNamesList = new List<string>();
            var parameters = new Dictionary<string, object>();
            int i = 0;
            foreach (var property in properties)
            {
                var propertyName = property.Name;
                columnNamesList.Add(propertyName);
            }

            foreach (var entity in entities)
            {
                var parN = new List<string>();
                foreach (var property in properties)
                {
                    var propertyName = property.Name;
                    var parameterName = $"@{propertyName}{i}";
                    var propertyValue = property.GetValue(entity);

                    if (propertyValue != null && propertyName != PrimaryKey)
                    {
                        parN.Add(parameterName);
                        parameters.Add(parameterName, propertyValue);
                    }
                }
                parameterNamesList.Add("(" + string.Join(", ", parN) + ")");
                i++;
            }

            var columnNames = string.Join(", ", columnNamesList);
            var parameterNames = string.Join(", ", parameterNamesList);

            string query = $"INSERT INTO {BoundName} ({columnNames}) VALUES {parameterNames}";
            Console.WriteLine(query);
            GenericDB.Instance.ExecuteNonQuery(query, parameters);
        }


        public void Update(T entity)
        {
            var properties = entity.GetType().GetProperties().Where(p => p.Name != PrimaryKey);
            var primaryKeyValue = GetValueOfPrimaryKey(entity);
            string setClause = string.Join(", ", properties.Select(p => $"{p.Name}=@{p.Name}"));

            string query = $"UPDATE {BoundName} SET {setClause} WHERE {PrimaryKey}=@PrimaryKey";
            var parameters = new Dictionary<string, object>();
            Console.WriteLine($"Query: {query}");
            foreach (var property in properties)
            {
                var value = property.GetValue(entity);
                parameters.Add(property.Name, value);
            }
            parameters.Add("PrimaryKey", primaryKeyValue);

            GenericDB.Instance.ExecuteNonQuery(query, parameters);
        }

        public void Delete(T entity)
        {
            string query = $"DELETE FROM {BoundName} WHERE {PrimaryKey}=@{PrimaryKey}";
            GenericDB.Instance.ExecuteNonQuery(query, new { id = GetValueOfPrimaryKey(entity) });
        }

        public List<T> Fetch(object conditions)
        {
            string query = $"SELECT * FROM {BoundName} WHERE ";
            var parameters = new Dictionary<string, object>();
            var conditionProperties = conditions.GetType().GetProperties();

            for (int i = 0; i < conditionProperties.Length; i++)
            {
                var property = conditionProperties[i];
                var propertyName = property.Name;
                var parameterName = $"@{propertyName}";
                var propertyValue = property.GetValue(conditions);

                query += $"{propertyName}={parameterName}";

                parameters.Add(parameterName, propertyValue);

                if (i < conditionProperties.Length - 1)
                {
                    query += " AND ";
                }
            }

            return GenericDB.Instance.ExecuteQuery<T>(query, parameters);
        }

        public int Count()
        {
            string query = $"SELECT COUNT(*) FROM {BoundName}";
            return GenericDB.Instance.ExecuteScalar<int>(query);
        }

        public T GetLatest()
        {
            string query = $"SELECT * FROM {BoundName} ORDER BY {PrimaryKey} DESC LIMIT 1";
            return GenericDB.Instance.ExecuteQueryFirst<T>(query);
        }

        public List<T> Join(string joinQuery)
        {
            return GenericDB.Instance.ExecuteQuery<T>(joinQuery);
        }

        public List<T> GetPage(int pageNumber, int pageSize)
        {
            int offset = (pageNumber - 1) * pageSize;
            string query = $"SELECT * FROM {BoundName} LIMIT {pageSize} OFFSET {offset}";
            return GenericDB.Instance.ExecuteQuery<T>(query);
        }

        private int GetValueOfPrimaryKey(T entity)
        {
            var property = entity.GetType().GetProperties().FirstOrDefault(p => p.Name == PrimaryKey);
            //if no primary key is found, throw an exception
            if (property == null)
                throw new Exception($"Could not find primary key with name {PrimaryKey} in {entity.GetType().Name}, if your primary key is not named {PrimaryKey}, please set the PrimaryKey property of the DbObject to the name of your primary key. Use the [NoKey] attribute to affirm there is no key.");
            return (int)property.GetValue(entity);
        }
    }

    public class DBInstance : BetterDisposable
    {
        private static GenericDB _instance;

        public static GenericDB Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GenericDB();
                return _instance;
            }
        }

        public DBInstance(string dbType, string connectionString)
        {
            if (dbType == "mysql")
            {
                var connection = new MySqlConnection(connectionString);
                GenericDB.Instance.SetConnection(connection);
                _instance = GenericDB.Instance;
            }
            else if (dbType == "postgres")
            {
                var connection = new NpgsqlConnection(connectionString);
                GenericDB.Instance.SetConnection(connection);
                _instance = GenericDB.Instance;
            }
            else if (dbType == "oracle")
            {
                var connection = new OracleConnection(connectionString);
                GenericDB.Instance.SetConnection(connection);
                _instance = GenericDB.Instance;
            }
            else if (dbType == "sqlite")
            {
                var connection = new SqliteConnection(connectionString);
                GenericDB.Instance.SetConnection(connection);
                _instance = GenericDB.Instance;
            }
            else if (dbType == "sqlserver")
            {
                var connection = new SqlConnection(connectionString);
                GenericDB.Instance.SetConnection(connection);
                _instance = GenericDB.Instance;
            }
            else
            {
                throw new Exception($"Could not find DB Type with type {dbType}");
            }

            mapTables();
        }

        private void mapTables()
        {
            List<string> tables = _instance.GetKeys();
            var dbObjects = GetCallingChildClass();
            foreach (var property in dbObjects)
            {
                //instantiates the object
                property.SetValue(this, Activator.CreateInstance(property.PropertyType));

                // Get the child elm "Value" and its type
                var tValue = property.PropertyType.GetProperty("Value");
                var tPK = property.PropertyType.GetProperty("PrimaryKey");
                var tBN = property.PropertyType.GetProperty("BoundName");
                if (tValue != null)
                {
                    var fieldType = tValue.PropertyType.Name;
                    foreach (var table in tables)
                    {
                        if (table.ToLower() == fieldType.ToLower())
                        {
                            var primaryKey = "id";

                            //check if property has a children with [Key] attribute
                            var children = tValue.PropertyType.GetProperties();
                            foreach (var child in children)
                            {
                                var attributes = child.GetCustomAttributes(true);
                                foreach (var attribute in attributes)
                                {
                                    if (attribute.GetType().Name == "KeyAttribute")
                                    {
                                        primaryKey = child.Name;
                                    }
                                }
                            }
                            //set primary key
                            tPK.SetValue(property.GetValue(this), primaryKey);
                            //set bound name
                            tBN.SetValue(property.GetValue(this), table.ToLower());
                        }
                    }
                }
            }
        }

        public void NonQuery(string query)
        {
            _instance.NonQuery(query);
        }

        public IEnumerable<DbDataReader> Query(string query)
        {
            return _instance.Query(query);
        }

        public DbTransaction BeginTransaction()
        {
            return GenericDB.Instance.BeginTransaction();
        }

        public void EndTransaction()
        {
            GenericDB.Instance.EndTransaction();
        }


        private List<PropertyInfo>? GetCallingChildClass()
        {
            // Get the stack trace
            StackTrace stackTrace = new StackTrace();

            // Get the calling method
            StackFrame callingFrame = stackTrace.GetFrame(3);
            var callingMethod = callingFrame.GetMethod();

            // Get the calling class
            var callingType = callingMethod.DeclaringType;

            return callingType.GetProperties()
            .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbObject<>))
            .ToList();
        }
    }


    public class GenericDB
    {
        private static GenericDB _instance;
        private DbConnection _connection;

        public static GenericDB Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GenericDB();
                return _instance;
            }
        }

        public void SetConnection(DbConnection connection)
        {
            _connection = connection;
        }

        public void EndTransaction()
        {
            _connection.Close();
        }

        public T? ExecuteQueryFirst<T>(string query, object parameters = null)
        {
            OpenConn();
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = query;
                if (parameters != null)
                    AddParameters(cmd, parameters);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return MapToObject<T>(reader, true);
                    }
                }
            }
            _connection.Close();

            return default(T);
        }

        public IEnumerable<DbDataReader> Query(string query, object parameters = null)
        {
            OpenConn();
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = query;
                if (parameters != null)
                    AddParameters(cmd, parameters);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        yield return reader;
                    }
                }
            }
            _connection.Close();
        }

        public void NonQuery(string query, object parameters = null)
        {
            OpenConn();
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = query;
                if (parameters != null)
                    AddParameters(cmd, parameters);

                cmd.ExecuteNonQuery();
            }
            _connection.Close();
        }

        public List<T> ExecuteQuery<T>(string query, object parameters = null)
        {
            var results = new List<T>();

            OpenConn();
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = query;
                if (parameters != null)
                    AddParameters(cmd, parameters);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        results.Add(MapToObject<T>(reader));
                    }
                }
            }
            _connection.Close();

            return results;
        }

        public int ExecuteNonQuery(string query, object parameters = null)
        {
            OpenConn();
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = query;
                if (parameters != null)
                    AddParameters(cmd, parameters);

                int rowsAffected = cmd.ExecuteNonQuery();
                _connection.Close();

                return rowsAffected;
            }
        }

        public T ExecuteScalar<T>(string query, object parameters = null)
        {
            OpenConn();
            using (var cmd = _connection.CreateCommand())
            {
                cmd.CommandText = query;
                if (parameters != null)
                    AddParameters(cmd, parameters);

                object result = cmd.ExecuteScalar();
                _connection.Close();

                return (T)Convert.ChangeType(result, typeof(T));
            }
        }

        public DbTransaction BeginTransaction()
        {
            OpenConn();
            return _connection.BeginTransaction();
        }


        private T MapToObject<T>(DbDataReader reader, bool closeConnection = false)
        {
            T result = Activator.CreateInstance<T>();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                //get the property name from the column name but all in lowercase
                string columnName = reader.GetName(i).ToLower();
                //for each property in the object, match it to its lowercase version then try to find the column name based on the lowercase version
                PropertyInfo[] properties = result.GetType().GetProperties();
                PropertyInfo property = null;
                foreach (PropertyInfo prop in properties)
                {
                    if (prop.Name.ToLower() == columnName)
                    {
                        property = prop;
                        break;
                    }
                }

                if (property != null)
                {
                    object value = reader.GetValue(i);
                    property.SetValue(result, value);
                }
            }
            if (closeConnection)
                _connection.Close();
            return result;
        }

        private void AddParameters(DbCommand cmd, object parameters)
        {
            //if the parameters is a dictionary, then add each key/value pair as a parameter
            if (parameters is Dictionary<string, object>)
            {
                foreach (var parameter in (Dictionary<string, object>)parameters)
                {
                    var param = cmd.CreateParameter();
                    param.ParameterName = parameter.Key;
                    param.Value = parameter.Value;
                    cmd.Parameters.Add(param);
                }
            }
            //if the parameters is an object, then add each property as a parameter
            else if (parameters is object)
            {
                foreach (var property in parameters.GetType().GetProperties())
                {
                    var param = cmd.CreateParameter();
                    param.ParameterName = property.Name;
                    param.Value = property.GetValue(parameters);
                    cmd.Parameters.Add(param);
                }
            }


        }


        public List<string> GetKeys()
        {
            List<string> keys = new List<string>();
            OpenConn();
            string query = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
            var cmd = _connection.CreateCommand();
            cmd.CommandText = query;


            DbDataReader reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                string tableName = reader["TABLE_NAME"].ToString();
                keys.Add(tableName);
            }

            reader.Close();
            _connection.Close();
            return keys;
        }

        private void OpenConn()
        {
            if (_connection.State == ConnectionState.Closed)
            {
                _connection.Open();
            }
        }

    }
}


