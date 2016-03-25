namespace PetaPoco
{
    using PetaPoco.Internal;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    public class Sql
    {
        private object[] _args;
        private object[] _argsFinal;
        private Sql _rhs;
        private string _sql;
        private string _sqlFinal;

        public Sql()
        {
        }

        public Sql(string sql, params object[] args)
        {
            this._sql = sql;
            this._args = args;
        }

        public Sql Append(Sql sql)
        {
            if (this._rhs != null)
            {
                this._rhs.Append(sql);
            }
            else
            {
                this._rhs = sql;
            }
            return this;
        }

        public Sql Append(string sql, params object[] args)
        {
            return this.Append(new Sql(sql, args));
        }

        private void Build()
        {
            if (this._sqlFinal == null)
            {
                StringBuilder sb = new StringBuilder();
                List<object> args = new List<object>();
                this.Build(sb, args, null);
                this._sqlFinal = sb.ToString();
                this._argsFinal = args.ToArray();
            }
        }

        private void Build(StringBuilder sb, List<object> args, Sql lhs)
        {
            if (!string.IsNullOrEmpty(this._sql))
            {
                if (sb.Length > 0)
                {
                    sb.Append("\n");
                }
                string str = ParametersHelper.ProcessParams(this._sql, this._args, args);
                if (Is(lhs, "WHERE ") && Is(this, "WHERE "))
                {
                    str = "AND " + str.Substring(6);
                }
                if (Is(lhs, "ORDER BY ") && Is(this, "ORDER BY "))
                {
                    str = ", " + str.Substring(9);
                }
                sb.Append(str);
            }
            if (this._rhs != null)
            {
                this._rhs.Build(sb, args, this);
            }
        }

        public Sql From(params object[] tables)
        {
            return this.Append(new Sql("FROM " + string.Join(", ", (from x in tables select x.ToString()).ToArray<string>()), new object[0]));
        }

        public Sql GroupBy(params object[] columns)
        {
            return this.Append(new Sql("GROUP BY " + string.Join(", ", (from x in columns select x.ToString()).ToArray<string>()), new object[0]));
        }

        public SqlJoinClause InnerJoin(string table)
        {
            return this.Join("INNER JOIN ", table);
        }

        private static bool Is(Sql sql, string sqltype)
        {
            return (((sql != null) && (sql._sql != null)) && sql._sql.StartsWith(sqltype, StringComparison.InvariantCultureIgnoreCase));
        }

        private SqlJoinClause Join(string JoinType, string table)
        {
            return new SqlJoinClause(this.Append(new Sql(JoinType + table, new object[0])));
        }

        public SqlJoinClause LeftJoin(string table)
        {
            return this.Join("LEFT JOIN ", table);
        }

        public Sql OrderBy(params object[] columns)
        {
            return this.Append(new Sql("ORDER BY " + string.Join(", ", (from x in columns select x.ToString()).ToArray<string>()), new object[0]));
        }

        public Sql Select(params object[] columns)
        {
            return this.Append(new Sql("SELECT " + string.Join(", ", (from x in columns select x.ToString()).ToArray<string>()), new object[0]));
        }

        public Sql Where(string sql, params object[] args)
        {
            return this.Append(new Sql("WHERE (" + sql + ")", args));
        }

        public object[] Arguments
        {
            get
            {
                this.Build();
                return this._argsFinal;
            }
        }

        public static Sql Builder
        {
            get
            {
                return new Sql();
            }
        }

        public string SQL
        {
            get
            {
                this.Build();
                return this._sqlFinal;
            }
        }

        public class SqlJoinClause
        {
            private readonly Sql _sql;

            public SqlJoinClause(Sql sql)
            {
                this._sql = sql;
            }

            public Sql On(string onClause, params object[] args)
            {
                return this._sql.Append("ON " + onClause, args);
            }
        }
    }
}

