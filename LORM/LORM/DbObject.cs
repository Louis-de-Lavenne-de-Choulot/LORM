using System.Reflection;

namespace LORM
{

    public class DbObject<T> : BetterDisposable
    {
        private Dictionary<String, Action<object>[]>? attributeFunctionMap { get; set; }
        private Dictionary<String, String>? columnNames { get; set; }
        public static string? BoundName { get; set; }
        public static string? PrimaryKey { get; set; }

        public T? Value { get; set; }

        public string GetBoundName()
        {
            return BoundName;
        }

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
            var properties = entity.GetType().GetProperties().Where(p => p.Name != PrimaryKey);
            var columnNamesList = new List<string>();
            var parameterNamesList = new List<string>();
            var parameters = new Dictionary<string, object>();

            foreach (var property in properties)
            {
                var propertyName = property.Name;
                if (attributeFunctionMap.ContainsKey(propertyName))
                {
                    //run the function mapped to the attribute
                    var funcs = attributeFunctionMap[propertyName];
                    foreach (var func in funcs)
                    {
                        func(property.GetValue(entity));
                    }
                }

                if (columnNames.ContainsKey(propertyName))
                {
                    propertyName = columnNames[propertyName];
                }
                var parameterName = $"@{propertyName}";
                var propertyValue = property.GetValue(entity);

                if (propertyValue != null)
                {
                    columnNamesList.Add(propertyName);
                    parameterNamesList.Add(parameterName);
                    parameters.Add(parameterName, propertyValue);
                }
            }

            var cNames = string.Join(", ", columnNamesList);
            var parameterNames = string.Join(", ", parameterNamesList);

            string query = $"INSERT INTO {BoundName} ({cNames}) VALUES ({parameterNames})";
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

            foreach (var entity in entities)
            {
                var parN = new List<string>();
                foreach (var property in properties)
                {
                    var propertyName = property.Name;
                    if (attributeFunctionMap.ContainsKey(propertyName))
                    {
                        //run the function mapped to the attribute
                        var funcs = attributeFunctionMap[propertyName];
                        foreach (var func in funcs)
                        {
                            func(property.GetValue(entity));
                        }
                    }
                    if (columnNames.ContainsKey(propertyName))
                    {
                        propertyName = columnNames[propertyName];
                    }
                    var parameterName = $"@{propertyName}{i}";
                    var propertyValue = property.GetValue(entity);

                    if (propertyValue != null && propertyName != PrimaryKey)
                    {
                        parN.Add(parameterName);
                        parameters.Add(parameterName, propertyValue);
                        if (i == 0)
                        {
                            columnNamesList.Add(propertyName);
                        }
                    }
                }
                parameterNamesList.Add("(" + string.Join(", ", parN) + ")");
                i++;
            }


            var cNames = string.Join(", ", columnNamesList);
            var parameterNames = string.Join(", ", parameterNamesList);

            string query = $"INSERT INTO {BoundName} ({cNames}) VALUES {parameterNames}";
            GenericDB.Instance.ExecuteNonQuery(query, parameters);
        }


        public void Update(T entity)
        {
            var properties = entity.GetType().GetProperties().Where(p => p.Name != PrimaryKey);

            foreach (var property in properties)
            {
                var propertyName = property.Name;
                if (attributeFunctionMap.ContainsKey(propertyName))
                {
                    //run the function mapped to the attribute
                    var funcs = attributeFunctionMap[propertyName];
                    foreach (var func in funcs)
                    {
                        func(property.GetValue(entity));
                    }
                }
            }
            var primaryKeyValue = GetValueOfPrimaryKey(entity);
            string setClause = string.Join(", ", properties.Select(p => $"{(columnNames.ContainsKey(p.Name) ? columnNames[p.Name] : p.Name)}=@{p.Name}"));

            string query = $"UPDATE {BoundName} SET {setClause} WHERE {PrimaryKey}=@PrimaryKey";
            var parameters = new Dictionary<string, object>();
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

                if (columnNames.ContainsKey(propertyName))
                {
                    propertyName = columnNames[propertyName];
                }
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
}

