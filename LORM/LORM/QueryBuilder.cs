namespace LORM {
        public class QueryBuilder<T> : BetterDisposable
        {
            private string _query;
            private Dictionary<string, object> _parameters;
            private int _parameterCount;
            string BoundName { get; set; }



            public QueryBuilder(DbObject<T> obj)
            {
                //get private BoundName
                BoundName = obj.GetBoundName();
                _query = "";
                _parameters = new Dictionary<string, object>();
                _parameterCount = 0;

            }

            public List<T> Select(bool distinct = false)
            {
                if (distinct)
                    _query = $"SELECT DISTINCT * FROM {BoundName} {_query}";
                else
                    _query = $"SELECT * FROM {BoundName} {_query}";
                return GenericDB.Instance.ExecuteQuery<T>(_query, _parameters);
            }

            public List<T> Select(string Elm, bool distinct = false)
            {
                if (distinct)
                    _query = $"SELECT DISTINCT {Elm} FROM {BoundName} {_query}";
                else
                    _query = $"SELECT {Elm} FROM {BoundName} {_query}";
                return GenericDB.Instance.ExecuteQuery<T>(_query, _parameters);
            }

            public void Delete()
            {
                _query = $"DELETE FROM {BoundName} {_query}";
                GenericDB.Instance.ExecuteQuery<T>(_query, _parameters);
            }
            

            public QueryBuilder<T> Where(string Elm)
            {
                //use @ and add to parameters
                _query += $" WHERE {Elm}";
                return this;
            }

            public QueryBuilder<T> WhereNot(string Elm)
            {
                _query += $" WHERE NOT {Elm}";
                return this;
            }
            public QueryBuilder<T> And(string Elm)
            {
                _query += $" AND {Elm}";
                return this;
            }

            public QueryBuilder<T> AndNot(string Elm)
            {
                _query += $" AND NOT {Elm}";
                return this;
            }

            public QueryBuilder<T> Or(string Elm)
            {
                _query += $" OR {Elm}";
                return this;
            }

            public QueryBuilder<T> OrNot(string Elm)
            {
                _query += $" OR NOT {Elm}";
                return this;
            }

            public QueryBuilder<T> OrderBy(string Elm)
            {
                _query += $" ORDER BY {Elm}";
                return this;
            }

            public QueryBuilder<T> Limit(int Elm)
            {
                _query += $" LIMIT @Elm{_parameterCount}";
                _parameters.Add($"@Elm{_parameterCount}", Elm);
                _parameterCount++;
                return this;
            }

            public QueryBuilder<T> Offset(int Elm)
            {
                _query += $" OFFSET @Elm{_parameterCount}";
                _parameters.Add($"@Elm{_parameterCount}", Elm);
                _parameterCount++;
                return this;
            }

            public QueryBuilder<T> Equals(object Elm)
            {
                _query += $" = @Elm{_parameterCount}";
                _parameters.Add($"@Elm{_parameterCount}", Elm);
                _parameterCount++;
                return this;
            }

            public QueryBuilder<T> GreaterThan(object Elm)
            {
                _query += $" > @Elm{_parameterCount}";
                _parameters.Add($"@Elm{_parameterCount}", Elm);
                _parameterCount++;
                return this;
            }

            public QueryBuilder<T> LessThan(object Elm)
            {
                _query += $" < @Elm{_parameterCount}";
                _parameters.Add($"@Elm{_parameterCount}", Elm);
                _parameterCount++;
                return this;
            }

            public QueryBuilder<T> GreaterThanOrEqual(object Elm)
            {
                _query += $" >= @Elm{_parameterCount}";
                _parameters.Add($"@Elm{_parameterCount}", Elm);
                _parameterCount++;
                return this;
            }

            public QueryBuilder<T> LessThanOrEqual(object Elm)
            {
                _query += $" <= @Elm{_parameterCount}";
                _parameters.Add($"@Elm{_parameterCount}", Elm);
                _parameterCount++;
                return this;
            }

            public QueryBuilder<T> In(object Elm)
            {
                _query += $" IN @Elm{_parameterCount}";
                _parameters.Add($"@Elm{_parameterCount}", Elm);
                _parameterCount++;
                return this;
            }

            public QueryBuilder<T> Like(object Elm)
            {
                _query += $" LIKE @Elm{_parameterCount}";
                _parameters.Add($"@Elm{_parameterCount}", Elm);
                _parameterCount++;
                return this;
            }

            public QueryBuilder<T> IsNull()
            {
                _query += $" IS NULL";
                return this;
            }

            public QueryBuilder<T> Between(object Elm1, object Elm2)
            {
                _query += $" BETWEEN @Elm{_parameterCount} AND @Elm{_parameterCount + 1}";
                _parameters.Add($"@Elm{_parameterCount}", Elm1);
                _parameters.Add($"@Elm{_parameterCount + 1}", Elm2);
                _parameterCount += 2;
                return this;
            }

            public QueryBuilder<T> GroupBy(string Elm)
            {
                _query += $" GROUP BY @Elm{_parameterCount}";
                _parameters.Add($"@Elm{_parameterCount}", Elm);
                _parameterCount++;
                return this;
            }

            public QueryBuilder<T> Having(string Elm)
            {
                _query += $" HAVING @Elm{_parameterCount}";
                _parameters.Add($"@Elm{_parameterCount}", Elm);
                _parameterCount++;
                return this;
            }

            public QueryBuilder<T> Join(string Elm)
            {
                _query += $" JOIN @Elm{_parameterCount}";
                _parameters.Add($"@Elm{_parameterCount}", Elm);
                _parameterCount++;
                return this;
            }

            public QueryBuilder<T> InnerJoin(string Elm)
            {
                _query += $" INNER JOIN @Elm{_parameterCount}";
                _parameters.Add($"@Elm{_parameterCount}", Elm);
                _parameterCount++;
                return this;
            }

            public QueryBuilder<T> LeftJoin(string Elm)
            {
                _query += $" LEFT JOIN @Elm{_parameterCount}";
                _parameters.Add($"@Elm{_parameterCount}", Elm);
                _parameterCount++;
                return this;
            }

            public QueryBuilder<T> RightJoin(string Elm)
            {
                _query += $" RIGHT JOIN @Elm{_parameterCount}";
                _parameters.Add($"@Elm{_parameterCount}", Elm);
                _parameterCount++;
                return this;
            }

            public QueryBuilder<T> FullJoin(string Elm)
            {
                _query += $" FULL JOIN @Elm{_parameterCount}";
                _parameters.Add($"@Elm{_parameterCount}", Elm);
                _parameterCount++;
                return this;
            }

            public QueryBuilder<T> CrossJoin(string Elm)
            {
                _query += $" CROSS JOIN @Elm{_parameterCount}";
                _parameters.Add($"@Elm{_parameterCount}", Elm);
                _parameterCount++;
                return this;
            }
        }
}