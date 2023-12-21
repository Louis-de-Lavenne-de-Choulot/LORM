
using System.Data;
using System.Data.Common;
using System.Reflection;

namespace LORM
{
    public class GenericDB
    {
        private static GenericDB _instance;
        private DbConnection _connection;

        internal static GenericDB Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GenericDB();
                return _instance;
            }
        }

        internal void SetConnection(DbConnection connection)
        {
            _connection = connection;
        }

        internal void EndTransaction()
        {
            _connection.Close();
        }

        internal T? ExecuteQueryFirst<T>(string query, object parameters = null)
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

        internal IEnumerable<DbDataReader> Query(string query, object parameters = null)
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

        internal void NonQuery(string query, object parameters = null)
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

        internal List<T> ExecuteQuery<T>(string query, object parameters = null)
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

        internal int ExecuteNonQuery(string query, object parameters = null)
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
                    //get map column name, if value is not null, use it, otherwise use the property name
                    var mapColumnName = prop.GetCustomAttribute<ColumnNameAttribute>()?.Name ?? prop.Name;
                    if (mapColumnName.ToLower() == columnName.ToLower())
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