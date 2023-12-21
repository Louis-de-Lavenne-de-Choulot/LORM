using Microsoft.Data.Sqlite;
using MySql.Data.MySqlClient;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using Org.BouncyCastle.Tls;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;



namespace LORM
{
    public class DBInstance : BetterDisposable
    {
        private static GenericDB _instance;

        private static GenericDB Instance
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
            DbConnection connection;
            switch (dbType)
            {
                case "mysql":
                    connection = new MySqlConnection(connectionString);
                    break;
                case "postgres":
                    connection = new NpgsqlConnection(connectionString);
                    break;
                case "oracle":
                    connection = new OracleConnection(connectionString);
                    break;
                case "sqlite":
                    connection = new SqliteConnection(connectionString);
                    break;
                case "sqlserver":
                    connection = new SqlConnection(connectionString);
                    break;
                default:
                    throw new Exception($"Could not find DB Type with type {dbType}");
            }
            GenericDB.Instance.SetConnection(connection);
            _instance = GenericDB.Instance;

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
                var aFM = property.PropertyType.GetProperty("attributeFunctionMap", BindingFlags.NonPublic | BindingFlags.Instance);
                var cN = property.PropertyType.GetProperty("columnNames", BindingFlags.NonPublic | BindingFlags.Instance);
                if (tValue != null)
                {
                    var fieldType = tValue.PropertyType.Name;
                    var attributes = property.GetCustomAttributes(true);
                    foreach (var attribute in attributes)
                    {
                        if (attribute.GetType().Name == "TableNameAttribute")
                        {
                            fieldType = ((TableNameAttribute)attribute).Name;
                            break;
                        }
                    }
                    foreach (var table in tables)
                    {
                        if (table.ToLower() == fieldType.ToLower())
                        {
                            var primaryKey = "ID";
                            Dictionary<String, Action<object>[]> attrfm = new Dictionary<string, Action<object>[]>();
                            Dictionary<String, String> cNMap = new Dictionary<string, string>();
                            //check if property has a children with [Key] attribute
                            var children = tValue.PropertyType.GetProperties();
                            foreach (var child in children)
                            {
                                attributes = child.GetCustomAttributes(true);
                                foreach (var attribute in attributes)
                                {
                                    switch (attribute.GetType().Name)
                                    {
                                        case "ColumnNameAttribute":
                                            //map the string to ValidateLength function
                                            cNMap.Add(child.Name, ((ColumnNameAttribute)attribute).Name);
                                            break;
                                        case "NullableAttribute":
                                            //map the string to ValidateNullable function
                                            var action = new Action<object>[] { (object value) => ValidateNullable(value, ((NullableAttribute)attribute).IsNullable, child.Name) }; 

                                            attrfm.Add(child.Name, action);
                                            break;
                                        case "StringLengthAttribute":
                                            //map the string to ValidateStringLength function 
                                            action = new Action<object>[] { (object value) => ValidateLength((string)value, ((StringLengthAttribute)attribute).MaxLength) };

                                            attrfm.Add(child.Name, action);
                                            break;
                                        case "KeyAttribute":
                                            primaryKey = child.Name;
                                            break;
                                    }
                                }
                            }
                            //set primary key
                            tPK.SetValue(property.GetValue(this), primaryKey);
                            //set attribute function map
                            aFM.SetValue(property.GetValue(this), attrfm);
                            //set column name map
                            cN.SetValue(property.GetValue(this), cNMap);
                            //set bound name
                            tBN.SetValue(property.GetValue(this), table.ToLower());
                        }
                    }
                }
            }
        }

        internal void ValidateLength(string value, int maxLength)
        {
            if (value.Length > maxLength)
            {
                throw new Exception($"Length of '{value}' is greater than {maxLength}");
            }
        }

        internal void ValidateNullable(object value, bool isNullable, string propertyName)
        {
            if (value == null && !isNullable)
            {
                throw new Exception($"Value of {propertyName} cannot be null");
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


        internal List<PropertyInfo>? GetCallingChildClass()
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
}


