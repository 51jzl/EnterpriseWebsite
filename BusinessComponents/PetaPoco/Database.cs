using PetaPoco.Internal;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using Victornet;
namespace PetaPoco
{
    /// <summary>
    /// 对PetaPoco.Database进行封装，以便于使用
    /// </summary>
    /// <summary>
    /// The main PetaPoco Database class.  You can either use this class directly, or derive from it.
    /// </summary>
    public class Database : System.IDisposable
    {
        private object syncObj = new object();
        private static Regex rxParamsPrefix = new Regex("(?<!@)@\\w+", RegexOptions.Compiled);
        internal DatabaseType _dbType;
        private string _connectionString;
        private string _providerName;
        private DbProviderFactory _factory;
        private IDbConnection _sharedConnection;
        private IDbTransaction _transaction;
        private int _sharedConnectionDepth;
        private int _transactionDepth;
        private bool _transactionCancelled;
        private string _lastSql;
        private object[] _lastArgs;
        private string _paramPrefix;
        /// <summary>
        /// When set to true the first opened connection is kept alive until this object is disposed
        /// </summary>
        public bool KeepConnectionAlive
        {
            get;
            set;
        }
        /// <summary>
        /// Provides access to the currently open shared connection (or null if none)
        /// </summary>
        public IDbConnection Connection
        {
            get
            {
                return this._sharedConnection;
            }
        }
        /// <summary>
        /// Retrieves the SQL of the last executed statement
        /// </summary>
        public string LastSQL
        {
            get
            {
                return this._lastSql;
            }
        }
        /// <summary>
        /// Retrieves the arguments to the last execute statement
        /// </summary>
        public object[] LastArgs
        {
            get
            {
                return this._lastArgs;
            }
        }
        /// <summary>
        /// Returns a formatted string describing the last executed SQL statement and it's argument values
        /// </summary>
        public string LastCommand
        {
            get
            {
                return this.FormatCommand(this._lastSql, this._lastArgs);
            }
        }
        /// <summary>
        /// When set to true, PetaPoco will automatically create the "SELECT columns" part of any query that looks like it needs it
        /// </summary>
        public bool EnableAutoSelect
        {
            get;
            set;
        }
        /// <summary>
        /// When set to true, parameters can be named ?myparam and populated from properties of the passed in argument values.
        /// </summary>
        public bool EnableNamedParams
        {
            get;
            set;
        }
        /// <summary>
        /// Sets the timeout value for all SQL statements.
        /// </summary>
        public int CommandTimeout
        {
            get;
            set;
        }
        /// <summary>
        /// Sets the timeout value for the next (and only next) SQL statement
        /// </summary>
        public int OneTimeCommandTimeout
        {
            get;
            set;
        }
        /// <summary>
        /// 获取PetaPoco.Database的实例
        /// </summary>
        public static Database CreateInstance(string connectionStringName = null)
        {
            return new Database(connectionStringName)
            {
                EnableAutoSelect = true,
                EnableNamedParams = true
            };
        }
        /// <summary>
        /// 批量执行sql
        /// </summary>
        /// <param name="sqls"></param>
        /// <returns></returns>
        public int Execute(System.Collections.Generic.IEnumerable<Sql> sqls)
        {
            int result;
            try
            {
                this.OpenSharedConnection();
                try
                {
                    lock (this.syncObj)
                    {
                        int num = 0;
                        foreach (Sql current in sqls)
                        {
                            using (IDbCommand dbCommand = this.CreateCommand(this._sharedConnection, current.SQL, current.Arguments))
                            {
                                num += dbCommand.ExecuteNonQuery();
                                this.OnExecutedCommand(dbCommand);
                            }
                        }
                        result = num;
                    }
                }
                finally
                {
                    this.CloseSharedConnection();
                }
            }
            catch (System.Exception x)
            {
                this.OnException(x);
                throw;
            }
            return result;
        }
        /// <summary>
        /// 获取第一列组成的集合
        /// </summary>
        /// <param name="sql">PetaPoco.Sql</param>
        public System.Collections.Generic.IEnumerable<object> FetchFirstColumn(Sql sql)
        {
            return this.FetchFirstColumn(sql.SQL, sql.Arguments);
        }
        /// <summary>
        /// 获取第一列组成的集合
        /// </summary>
        public System.Collections.Generic.IEnumerable<object> FetchFirstColumn(string sql, params object[] args)
        {
            this.OpenSharedConnection();
            System.Collections.Generic.List<object> list = new System.Collections.Generic.List<object>();
            try
            {
                lock (this.syncObj)
                {
                    using (IDbCommand dbCommand = this.CreateCommand(this._sharedConnection, sql, args))
                    {
                        using (IDataReader dataReader = dbCommand.ExecuteReader())
                        {
                            this.OnExecutedCommand(dbCommand);
                            while (dataReader.Read())
                            {
                                list.Add(dataReader[0]);
                            }
                            dataReader.Close();
                        }
                    }
                }
            }
            finally
            {
                this.CloseSharedConnection();
            }
            return list;
        }
        /// <summary>
        /// 获取可分页的主键集合
        /// </summary>
        /// <typeparam name="TEntity">实体</typeparam>
        /// <param name="maxRecords">最大返回记录数</param>
        /// <param name="pageSize">每页记录数</param>
        /// <param name="pageIndex">当前页码(从1开始)</param>
        /// <param name="sql">PetaPoco.Sql</param>
        /// <returns>可分页的实体Id集合</returns>
        public PagingEntityIdCollection FetchPagingPrimaryKeys<TEntity>(long maxRecords, int pageSize, int pageIndex, Sql sql) where TEntity : IEntity
        {
            string sQL = sql.SQL;
            object[] arguments = sql.Arguments;
            string sql2;
            string sql3;
            this.BuildPagingPrimaryKeyQueries<TEntity>(maxRecords, (long)((pageIndex - 1) * pageSize), (long)pageSize, sQL, ref arguments, out sql2, out sql3);
            long totalRecords = this.ExecuteScalar<long>(sql2, arguments);
            System.Collections.Generic.List<object> entityIds = this.FetchFirstColumn(sql3, arguments).ToList<object>();
            return new PagingEntityIdCollection(entityIds, totalRecords);
        }
        /// <summary>
        /// 获取可分页的主键集合
        /// </summary>
        /// <typeparam name="TEntity">实体</typeparam>
        /// <param name="maxRecords">最大返回记录数</param>
        /// <param name="pageSize">每页记录数</param>
        /// <param name="pageIndex">当前页码(从1开始)</param>
        /// <param name="primaryKey">主键</param>
        /// <param name="sql">PetaPoco.Sql <remarks>要求必须是完整的sql语句</remarks></param>
        /// <returns>可分页的实体Id集合</returns>
        public PagingEntityIdCollection FetchPagingPrimaryKeys(long maxRecords, int pageSize, int pageIndex, string primaryKey, Sql sql)
        {
            string sQL = sql.SQL;
            object[] arguments = sql.Arguments;
            string sql2;
            string sql3;
            this.BuildPagingPrimaryKeyQueries(maxRecords, (long)((pageIndex - 1) * pageSize), (long)pageSize, primaryKey, sQL, ref arguments, out sql2, out sql3);
            long totalRecords = this.ExecuteScalar<long>(sql2, arguments);
            System.Collections.Generic.List<object> entityIds = this.FetchFirstColumn(sql3, arguments).ToList<object>();
            return new PagingEntityIdCollection(entityIds, totalRecords);
        }
        /// <summary>
        /// 获取前topNumber条记录
        /// </summary>
        /// <param name="topNumber">前多少条数据</param>
        /// <param name="sql">PetaPoco.Sql</param>
        public System.Collections.Generic.IEnumerable<object> FetchTopPrimaryKeys<TEntity>(int topNumber, Sql sql) where TEntity : IEntity
        {
            string sQL = sql.SQL;
            object[] arguments = sql.Arguments;
            string sql2 = this.BuildTopSql<TEntity>(topNumber, sQL);
            return this.FetchFirstColumn(sql2, arguments);
        }
        /// <summary>
        /// 获取前topNumber条记录
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="topNumber">前多少条数据</param>
        /// <param name="sql">PetaPoco.Sql<remarks>要求必须是完整的sql语句</remarks></param>
        public System.Collections.Generic.IEnumerable<T> FetchTop<T>(int topNumber, Sql sql)
        {
            string sQL = sql.SQL;
            object[] arguments = sql.Arguments;
            string sql2 = this.BuildTopSql(topNumber, sQL);
            return this.Fetch<T>(sql2, arguments);
        }
        public System.Collections.Generic.IEnumerable<T> FetchByPrimaryKeys<T>(System.Collections.Generic.IEnumerable<object> primaryKeys)
        {
            if (primaryKeys == null || primaryKeys.Count<object>() == 0)
            {
                return new System.Collections.Generic.List<T>();
            }
            string arg = this._dbType.EscapeSqlIdentifier(PocoData.ForType(typeof(T)).TableInfo.PrimaryKey);
            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder("WHERE ");
            int num = 0;
            foreach (object arg_52_0 in primaryKeys)
            {
                stringBuilder.AppendFormat("{0} = @{1} or ", arg, num);
                num++;
            }
            stringBuilder.Remove(stringBuilder.Length - 4, 3);
            return this.Fetch<T>(stringBuilder.ToString(), primaryKeys.ToArray<object>());
        }
        /// <summary>
        /// 创建分页的SQL语句
        /// </summary>
        protected void BuildPagingPrimaryKeyQueries<T>(long maxRecords, long skip, long take, string sql, ref object[] args, out string sqlCount, out string sqlPage)
        {
            PocoData pocoData = PocoData.ForType(typeof(T));
            string primaryKey = string.Empty;
            if (sql.Contains(pocoData.TableInfo.TableName))
            {
                primaryKey = pocoData.TableInfo.TableName + "." + pocoData.TableInfo.PrimaryKey;
            }
            else
            {
                primaryKey = pocoData.TableInfo.PrimaryKey;
            }
            if (this.EnableAutoSelect)
            {
                sql = AutoSelectHelper.AddSelectClause<T>(this._dbType, sql, primaryKey);
            }
            this.BuildPagingPrimaryKeyQueries(maxRecords, skip, take, primaryKey, sql, ref args, out sqlCount, out sqlPage);
        }
        /// <summary>
        /// 创建分页的SQL语句
        /// </summary>
        protected void BuildPagingPrimaryKeyQueries(long maxRecords, long skip, long take, string primaryKey, string sql, ref object[] args, out string sqlCount, out string sqlPage)
        {
            string sqlSelectRemoved;
            string sqlOrderBy;
            if (!this.SplitSqlForPagingOptimized(maxRecords, sql, primaryKey, out sqlCount, out sqlSelectRemoved, out sqlOrderBy))
            {
                throw new System.Exception("Unable to parse SQL statement for paged query");
            }
            sqlPage = this._dbType.BuildPageQuery(skip, take, new PagingHelper.SQLParts
            {
                sql = sql,
                sqlCount = sqlCount,
                sqlSelectRemoved = sqlSelectRemoved,
                sqlOrderBy = sqlOrderBy
            }, ref args, primaryKey);
        }
        /// <summary>
        /// 切割sql数据
        /// </summary>
        /// <param name="maxRecords"></param>
        /// <param name="sql"></param>
        /// <param name="primaryKey"></param>
        /// <param name="sqlCount"></param>
        /// <param name="sqlSelectRemoved"></param>
        /// <param name="sqlOrderBy"></param>
        /// <returns></returns>
        protected bool SplitSqlForPagingOptimized(long maxRecords, string sql, string primaryKey, out string sqlCount, out string sqlSelectRemoved, out string sqlOrderBy)
        {
            sqlSelectRemoved = null;
            sqlCount = null;
            sqlOrderBy = null;
            Match match = PagingHelper.rxColumns.Match(sql);
            if (!match.Success)
            {
                return false;
            }
            Group group = match.Groups[1];
            sqlSelectRemoved = sql.Substring(group.Index);
            if (PagingHelper.rxDistinct.IsMatch(sqlSelectRemoved))
            {
                sqlCount = string.Concat(new string[]
				{
					sql.Substring(0, group.Index),
					"COUNT(",
					match.Groups[1].ToString().Trim(),
					") ",
					sql.Substring(group.Index + group.Length)
				});
            }
            else if (maxRecords > 0L)
            {
                if (this._providerName.StartsWith("MySql"))
                {
                    sqlCount = string.Concat(new object[]
					{
						"select count(*) from (",
						sql,
						" limit ",
						maxRecords,
						" ) as TempCountTable"
					});
                }
                else
                {
                    sqlCount = string.Concat(new object[]
					{
						"select count(*) from (",
						sql.Substring(0, group.Index),
						" top ",
						maxRecords,
						" ",
						primaryKey,
						" ",
						sql.Substring(group.Index + group.Length),
						" ) as TempCountTable"
					});
                }
            }
            else
            {
                sqlCount = sql.Substring(0, group.Index) + "COUNT(*) " + sql.Substring(group.Index + group.Length);
            }
            match = PagingHelper.rxOrderBy.Match(sqlCount);
            if (!match.Success)
            {
                sqlOrderBy = null;
            }
            else
            {
                group = match.Groups[0];
                sqlOrderBy = group.ToString();
                sqlCount = sqlCount.Substring(0, group.Index) + sqlCount.Substring(group.Index + group.Length);
            }
            return true;
        }
        /// <summary>
        /// 构建获取前topNumber记录的SQL
        /// </summary>
        protected string BuildTopSql<T>(int topNumber, string sql)
        {
            PocoData pocoData = PocoData.ForType(typeof(T));
            string primaryKey = pocoData.TableInfo.TableName + "." + pocoData.TableInfo.PrimaryKey;
            if (this.EnableAutoSelect)
            {
                sql = AutoSelectHelper.AddSelectClause<T>(this._dbType, sql, primaryKey);
            }
            return this.BuildTopSql(topNumber, sql);
        }
        /// <summary>
        /// 构建获取前topNumber记录的SQL
        /// </summary>
        protected string BuildTopSql(int topNumber, string sql)
        {
            Match match = PagingHelper.rxColumns.Match(sql);
            if (!match.Success)
            {
                return null;
            }
            Group group = match.Groups[1];
            string result;
            if (this._providerName.StartsWith("MySql"))
            {
                result = sql + " limit " + topNumber;
            }
            else
            {
                result = string.Concat(new object[]
				{
					sql.Substring(0, group.Index),
					" top ",
					topNumber,
					" ",
					group.Value,
					" ",
					sql.Substring(group.Index + group.Length)
				});
            }
            return result;
        }
        /// <summary>
        /// Construct a database using a supplied IDbConnection
        /// </summary>
        /// <param name="connection">The IDbConnection to use</param>
        /// <remarks>
        /// The supplied IDbConnection will not be closed/disposed by PetaPoco - that remains
        /// the responsibility of the caller.
        /// </remarks>
        public Database(IDbConnection connection)
        {
            this._sharedConnection = connection;
            this._connectionString = connection.ConnectionString;
            this._sharedConnectionDepth = 2;
            this.CommonConstruct();
        }
        /// <summary>
        /// Construct a database using a supplied connections string and optionally a provider name
        /// </summary>
        /// <param name="connectionString">The DB connection string</param>
        /// <param name="providerName">The name of the DB provider to use</param>
        /// <remarks>
        /// PetaPoco will automatically close and dispose any connections it creates.
        /// </remarks>
        public Database(string connectionString, string providerName)
        {
            this._connectionString = connectionString;
            this._providerName = providerName;
            this.CommonConstruct();
        }
        /// <summary>
        /// Construct a Database using a supplied connection string and a DbProviderFactory
        /// </summary>
        /// <param name="connectionString">The connection string to use</param>
        /// <param name="provider">The DbProviderFactory to use for instantiating IDbConnection's</param>
        public Database(string connectionString, DbProviderFactory provider)
        {
            this._connectionString = connectionString;
            this._factory = provider;
            this.CommonConstruct();
        }
        /// <summary>
        /// Construct a Database using a supplied connectionString Name.  The actual connection string and provider will be 
        /// read from app/web.config.
        /// </summary>
        /// <param name="connectionStringName">The name of the connection</param>
        public Database(string connectionStringName)
        {
            if (string.IsNullOrEmpty(connectionStringName))
            {
                int count = ConfigurationManager.ConnectionStrings.Count;
                if (count <= 0)
                {
                    throw new System.InvalidOperationException("Can't find a connection string '");
                }
                connectionStringName = ConfigurationManager.ConnectionStrings[count - 1].Name;
            }
            string providerName = "System.Data.SqlClient";
            if (ConfigurationManager.ConnectionStrings[connectionStringName] != null)
            {
                if (!string.IsNullOrEmpty(ConfigurationManager.ConnectionStrings[connectionStringName].ProviderName))
                {
                    providerName = ConfigurationManager.ConnectionStrings[connectionStringName].ProviderName;
                }
                this._connectionString = ConfigurationManager.ConnectionStrings[connectionStringName].ConnectionString;
                this._providerName = providerName;
                this.CommonConstruct();
                return;
            }
            throw new System.InvalidOperationException("Can't find a connection string with the name '" + connectionStringName + "'");
        }
        /// <summary>
        /// Provides common initialization for the various constructors
        /// </summary>
        private void CommonConstruct()
        {
            this._transactionDepth = 0;
            this.EnableAutoSelect = true;
            this.EnableNamedParams = true;
            if (this._providerName != null)
            {
                this._factory = DbProviderFactories.GetFactory(this._providerName);
            }
            string name = ((this._factory == null) ? this._sharedConnection.GetType() : this._factory.GetType()).Name;
            this._dbType = DatabaseType.Resolve(name, this._providerName);
            this._paramPrefix = this._dbType.GetParameterPrefix(this._connectionString);
        }
        /// <summary>
        /// Automatically close one open shared connection 
        /// </summary>
        public void Dispose()
        {
            this.CloseSharedConnection();
        }
        /// <summary>
        /// Open a connection that will be used for all subsequent queries.
        /// </summary>
        /// <remarks>
        /// Calls to Open/CloseSharedConnection are reference counted and should be balanced
        /// </remarks>
        public void OpenSharedConnection()
        {
            lock (this.syncObj)
            {
                if (this._sharedConnectionDepth == 0)
                {
                    this._sharedConnection = this._factory.CreateConnection();
                    this._sharedConnection.ConnectionString = this._connectionString;
                    if (this._sharedConnection.State == ConnectionState.Broken)
                    {
                        this._sharedConnection.Close();
                    }
                    if (this._sharedConnection.State == ConnectionState.Closed)
                    {
                        this._sharedConnection.Open();
                    }
                    this._sharedConnection = this.OnConnectionOpened(this._sharedConnection);
                    if (this.KeepConnectionAlive)
                    {
                        this._sharedConnectionDepth++;
                    }
                }
                this._sharedConnectionDepth++;
            }
        }
        /// <summary>
        /// Releases the shared connection
        /// </summary>
        public void CloseSharedConnection()
        {
            lock (this.syncObj)
            {
                if (this._sharedConnectionDepth > 0)
                {
                    this._sharedConnectionDepth--;
                    if (this._sharedConnectionDepth == 0)
                    {
                        this.OnConnectionClosing(this._sharedConnection);
                        this._sharedConnection.Dispose();
                        this._sharedConnection = null;
                    }
                }
            }
        }
        /// <summary>
        /// Starts or continues a transaction.
        /// </summary>
        /// <returns>An ITransaction reference that must be Completed or disposed</returns>
        /// <remarks>
        /// This method makes management of calls to Begin/End/CompleteTransaction easier.  
        ///
        /// The usage pattern for this should be:
        ///
        /// using (var tx = db.GetTransaction())
        /// {
        /// 	// Do stuff
        /// 	db.Update(...);
        ///
        ///     // Mark the transaction as complete
        ///     tx.Complete();
        /// }
        ///
        /// Transactions can be nested but they must all be completed otherwise the entire
        /// transaction is aborted.
        /// </remarks>
        public ITransaction GetTransaction()
        {
            return new Transaction(this);
        }
        /// <summary>
        /// Called when a transaction starts.  Overridden by the T4 template generated database
        /// classes to ensure the same DB instance is used throughout the transaction.
        /// </summary>
        public virtual void OnBeginTransaction()
        {
        }
        /// <summary>
        /// Called when a transaction ends.
        /// </summary>
        public virtual void OnEndTransaction()
        {
        }
        /// <summary>
        /// Starts a transaction scope, see GetTransaction() for recommended usage
        /// </summary>
        public void BeginTransaction()
        {
            this._transactionDepth++;
            if (this._transactionDepth == 1)
            {
                this.OpenSharedConnection();
                this._transaction = this._sharedConnection.BeginTransaction();
                this._transactionCancelled = false;
                this.OnBeginTransaction();
            }
        }
        /// <summary>
        /// Internal helper to cleanup transaction
        /// </summary>
        private void CleanupTransaction()
        {
            this.OnEndTransaction();
            if (this._transactionCancelled)
            {
                this._transaction.Rollback();
            }
            else
            {
                this._transaction.Commit();
            }
            this._transaction.Dispose();
            this._transaction = null;
            this.CloseSharedConnection();
        }
        /// <summary>
        /// Aborts the entire outer most transaction scope 
        /// </summary>
        /// <remarks>
        /// Called automatically by Transaction.Dispose()
        /// if the transaction wasn't completed.
        /// </remarks>
        public void AbortTransaction()
        {
            this._transactionCancelled = true;
            if (--this._transactionDepth == 0)
            {
                this.CleanupTransaction();
            }
        }
        /// <summary>
        /// Marks the current transaction scope as complete.
        /// </summary>
        public void CompleteTransaction()
        {
            if (--this._transactionDepth == 0)
            {
                this.CleanupTransaction();
            }
        }
        /// <summary>
        /// Add a parameter to a DB command
        /// </summary>
        /// <param name="cmd">A reference to the IDbCommand to which the parameter is to be added</param>
        /// <param name="value">The value to assign to the parameter</param>
        /// <param name="pi">Optional, a reference to the property info of the POCO property from which the value is coming.</param>
        private void AddParam(IDbCommand cmd, object value, System.Reflection.PropertyInfo pi)
        {
            if (pi != null)
            {
                IMapper mapper = Mappers.GetMapper(pi.DeclaringType);
                Func<object, object> toDbConverter = mapper.GetToDbConverter(pi);
                if (toDbConverter != null)
                {
                    value = toDbConverter(value);
                }
            }
            IDbDataParameter dbDataParameter = value as IDbDataParameter;
            if (dbDataParameter != null)
            {
                dbDataParameter.ParameterName = string.Format("{0}{1}", this._paramPrefix, cmd.Parameters.Count);
                cmd.Parameters.Add(dbDataParameter);
                return;
            }
            IDbDataParameter dbDataParameter2 = cmd.CreateParameter();
            dbDataParameter2.ParameterName = string.Format("{0}{1}", this._paramPrefix, cmd.Parameters.Count);
            if (value == null)
            {
                dbDataParameter2.Value = System.DBNull.Value;
            }
            else
            {
                value = this._dbType.MapParameterValue(value);
                System.Type type = value.GetType();
                if (type.IsEnum)
                {
                    dbDataParameter2.Value = (int)value;
                }
                else if (type == typeof(System.Guid))
                {
                    dbDataParameter2.Value = value.ToString();
                    dbDataParameter2.DbType = DbType.String;
                    dbDataParameter2.Size = 40;
                }
                else if (type == typeof(string))
                {
                    if ((value as string).Length + 1 > 4000 && dbDataParameter2.GetType().Name == "SqlCeParameter")
                    {
                        dbDataParameter2.GetType().GetProperty("SqlDbType").SetValue(dbDataParameter2, SqlDbType.NText, null);
                    }
                    dbDataParameter2.Size = System.Math.Max((value as string).Length + 1, 4000);
                    dbDataParameter2.Value = value;
                }
                else if (type == typeof(AnsiString))
                {
                    dbDataParameter2.Size = System.Math.Max((value as AnsiString).Value.Length + 1, 4000);
                    dbDataParameter2.Value = (value as AnsiString).Value;
                    dbDataParameter2.DbType = DbType.AnsiString;
                }
                else if (value.GetType().Name == "SqlGeography")
                {
                    dbDataParameter2.GetType().GetProperty("UdtTypeName").SetValue(dbDataParameter2, "geography", null);
                    dbDataParameter2.Value = value;
                }
                else if (value.GetType().Name == "SqlGeometry")
                {
                    dbDataParameter2.GetType().GetProperty("UdtTypeName").SetValue(dbDataParameter2, "geometry", null);
                    dbDataParameter2.Value = value;
                }
                else
                {
                    dbDataParameter2.Value = value;
                }
            }
            cmd.Parameters.Add(dbDataParameter2);
        }
        public IDbCommand CreateCommand(IDbConnection connection, string sql, params object[] args)
        {
            if (this.EnableNamedParams)
            {
                System.Collections.Generic.List<object> list = new System.Collections.Generic.List<object>();
                sql = ParametersHelper.ProcessParams(sql, args, list);
                args = list.ToArray();
            }
            if (this._paramPrefix != "@")
            {
                sql = Database.rxParamsPrefix.Replace(sql, (Match m) => this._paramPrefix + m.Value.Substring(1));
            }
            sql = sql.Replace("@@", "@");
            IDbCommand dbCommand = connection.CreateCommand();
            dbCommand.Connection = connection;
            dbCommand.CommandText = sql;
            dbCommand.Transaction = this._transaction;
            object[] array = args;
            for (int i = 0; i < array.Length; i++)
            {
                object value = array[i];
                this.AddParam(dbCommand, value, null);
            }
            this._dbType.PreExecute(dbCommand);
            if (!string.IsNullOrEmpty(sql))
            {
                this.DoPreExecute(dbCommand);
            }
            return dbCommand;
        }
        /// <summary>
        /// Called if an exception occurs during processing of a DB operation.  Override to provide custom logging/handling.
        /// </summary>
        /// <param name="x">The exception instance</param>
        /// <returns>True to re-throw the exception, false to suppress it</returns>
        public virtual bool OnException(System.Exception x)
        {
            return true;
        }
        /// <summary>
        /// Called when DB connection opened
        /// </summary>
        /// <param name="conn">The newly opened IDbConnection</param>
        /// <returns>The same or a replacement IDbConnection</returns>
        /// <remarks>
        /// Override this method to provide custom logging of opening connection, or
        /// to provide a proxy IDbConnection.
        /// </remarks>
        public virtual IDbConnection OnConnectionOpened(IDbConnection conn)
        {
            return conn;
        }
        /// <summary>
        /// Called when DB connection closed
        /// </summary>
        /// <param name="conn">The soon to be closed IDBConnection</param>
        public virtual void OnConnectionClosing(IDbConnection conn)
        {
        }
        /// <summary>
        /// Called just before an DB command is executed
        /// </summary>
        /// <param name="cmd">The command to be executed</param>
        /// <remarks>
        /// Override this method to provide custom logging of commands and/or
        /// modification of the IDbCommand before it's executed
        /// </remarks>
        public virtual void OnExecutingCommand(IDbCommand cmd)
        {
        }
        /// <summary>
        /// Called on completion of command execution
        /// </summary>
        /// <param name="cmd">The IDbCommand that finished executing</param>
        public virtual void OnExecutedCommand(IDbCommand cmd)
        {
        }
        /// <summary>
        /// Executes a non-query command
        /// </summary>
        /// <param name="sql">The SQL statement to execute</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL</param>
        /// <returns>The number of rows affected</returns>
        public int Execute(string sql, params object[] args)
        {
            int result;
            try
            {
                this.OpenSharedConnection();
                try
                {
                    lock (this.syncObj)
                    {
                        using (IDbCommand dbCommand = this.CreateCommand(this._sharedConnection, sql, args))
                        {
                            int num = dbCommand.ExecuteNonQuery();
                            this.OnExecutedCommand(dbCommand);
                            result = num;
                        }
                    }
                }
                finally
                {
                    this.CloseSharedConnection();
                }
            }
            catch (System.Exception x)
            {
                if (this.OnException(x))
                {
                    throw;
                }
                result = -1;
            }
            return result;
        }
        /// <summary>
        /// Executes a non-query command
        /// </summary>
        /// <param name="sql">An SQL builder object representing the query and it's arguments</param>
        /// <returns>The number of rows affected</returns>
        public int Execute(Sql sql)
        {
            return this.Execute(sql.SQL, sql.Arguments);
        }
        /// <summary>
        /// Executes a query and return the first column of the first row in the result set.
        /// </summary>
        /// <typeparam name="T">The type that the result value should be cast to</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL</param>
        /// <returns>The scalar value cast to T</returns>
        public T ExecuteScalar<T>(string sql, params object[] args)
        {
            T result;
            try
            {
                this.OpenSharedConnection();
                try
                {
                    lock (this.syncObj)
                    {
                        using (IDbCommand dbCommand = this.CreateCommand(this._sharedConnection, sql, args))
                        {
                            object obj2 = dbCommand.ExecuteScalar();
                            this.OnExecutedCommand(dbCommand);
                            System.Type underlyingType = System.Nullable.GetUnderlyingType(typeof(T));
                            if (underlyingType != null && obj2 == null)
                            {
                                result = default(T);
                            }
                            else
                            {
                                result = (T)((object)System.Convert.ChangeType(obj2, (underlyingType == null) ? typeof(T) : underlyingType));
                            }
                        }
                    }
                }
                finally
                {
                    this.CloseSharedConnection();
                }
            }
            catch (System.Exception x)
            {
                if (this.OnException(x))
                {
                    throw;
                }
                result = default(T);
            }
            return result;
        }
        /// <summary>
        /// Executes a query and return the first column of the first row in the result set.
        /// </summary>
        /// <typeparam name="T">The type that the result value should be cast to</typeparam>
        /// <param name="sql">An SQL builder object representing the query and it's arguments</param>
        /// <returns>The scalar value cast to T</returns>
        public T ExecuteScalar<T>(Sql sql)
        {
            return this.ExecuteScalar<T>(sql.SQL, sql.Arguments);
        }
        /// <summary>
        /// Runs a query and returns the result set as a typed list
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="sql">The SQL query to execute</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL</param>
        /// <returns>A List holding the results of the query</returns>
        public System.Collections.Generic.List<T> Fetch<T>(string sql, params object[] args)
        {
            return this.Query<T>(sql, args).ToList<T>();
        }
        /// <summary>
        /// Runs a query and returns the result set as a typed list
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="sql">An SQL builder object representing the query and it's arguments</param>
        /// <returns>A List holding the results of the query</returns>
        public System.Collections.Generic.List<T> Fetch<T>(Sql sql)
        {
            return this.Fetch<T>(sql.SQL, sql.Arguments);
        }
        /// <summary>
        /// Starting with a regular SELECT statement, derives the SQL statements required to query a 
        /// DB for a page of records and the total number of records
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="skip">The number of rows to skip before the start of the page</param>
        /// <param name="take">The number of rows in the page</param>
        /// <param name="sql">The original SQL select statement</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL</param>
        /// <param name="sqlCount">Outputs the SQL statement to query for the total number of matching rows</param>
        /// <param name="sqlPage">Outputs the SQL statement to retrieve a single page of matching rows</param>
        private void BuildPageQueries<T>(long skip, long take, string sql, ref object[] args, out string sqlCount, out string sqlPage)
        {
            if (this.EnableAutoSelect)
            {
                sql = AutoSelectHelper.AddSelectClause<T>(this._dbType, sql, null);
            }
            PagingHelper.SQLParts parts;
            if (!PagingHelper.SplitSQL(sql, out parts))
            {
                throw new System.Exception("Unable to parse SQL statement for paged query");
            }
            sqlPage = this._dbType.BuildPageQuery(skip, take, parts, ref args, null);
            sqlCount = parts.sqlCount;
        }
        /// <summary>
        /// Retrieves a page of records	and the total number of available records
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="page">The 1 based page number to retrieve</param>
        /// <param name="itemsPerPage">The number of records per page</param>
        /// <param name="sqlCount">The SQL to retrieve the total number of records</param>
        /// <param name="countArgs">Arguments to any embedded parameters in the sqlCount statement</param>
        /// <param name="sqlPage">The SQL To retrieve a single page of results</param>
        /// <param name="pageArgs">Arguments to any embedded parameters in the sqlPage statement</param>
        /// <returns>A Page of results</returns>
        /// <remarks>
        /// This method allows separate SQL statements to be explicitly provided for the two parts of the page query.
        /// The page and itemsPerPage parameters are not used directly and are used simply to populate the returned Page object.
        /// </remarks>
        public Page<T> Page<T>(long page, long itemsPerPage, string sqlCount, object[] countArgs, string sqlPage, object[] pageArgs)
        {
            int oneTimeCommandTimeout = this.OneTimeCommandTimeout;
            Page<T> page2 = new Page<T>
            {
                CurrentPage = page,
                ItemsPerPage = itemsPerPage,
                TotalItems = this.ExecuteScalar<long>(sqlCount, countArgs)
            };
            page2.TotalPages = page2.TotalItems / itemsPerPage;
            if (page2.TotalItems % itemsPerPage != 0L)
            {
                page2.TotalPages += 1L;
            }
            this.OneTimeCommandTimeout = oneTimeCommandTimeout;
            page2.Items = this.Fetch<T>(sqlPage, pageArgs);
            return page2;
        }
        /// <summary>
        /// Retrieves a page of records	and the total number of available records
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="page">The 1 based page number to retrieve</param>
        /// <param name="itemsPerPage">The number of records per page</param>
        /// <param name="sql">The base SQL query</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL statement</param>
        /// <returns>A Page of results</returns>
        /// <remarks>
        /// PetaPoco will automatically modify the supplied SELECT statement to only retrieve the
        /// records for the specified page.  It will also execute a second query to retrieve the
        /// total number of records in the result set.
        /// </remarks>
        public Page<T> Page<T>(long page, long itemsPerPage, string sql, params object[] args)
        {
            string sqlCount;
            string sqlPage;
            this.BuildPageQueries<T>((page - 1L) * itemsPerPage, itemsPerPage, sql, ref args, out sqlCount, out sqlPage);
            return this.Page<T>(page, itemsPerPage, sqlCount, args, sqlPage, args);
        }
        /// <summary>
        /// Retrieves a page of records	and the total number of available records
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="page">The 1 based page number to retrieve</param>
        /// <param name="itemsPerPage">The number of records per page</param>
        /// <param name="sql">An SQL builder object representing the base SQL query and it's arguments</param>
        /// <returns>A Page of results</returns>
        /// <remarks>
        /// PetaPoco will automatically modify the supplied SELECT statement to only retrieve the
        /// records for the specified page.  It will also execute a second query to retrieve the
        /// total number of records in the result set.
        /// </remarks>
        public Page<T> Page<T>(long page, long itemsPerPage, Sql sql)
        {
            return this.Page<T>(page, itemsPerPage, sql.SQL, sql.Arguments);
        }
        /// <summary>
        /// Retrieves a page of records	and the total number of available records
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="page">The 1 based page number to retrieve</param>
        /// <param name="itemsPerPage">The number of records per page</param>
        /// <param name="sqlCount">An SQL builder object representing the SQL to retrieve the total number of records</param>
        /// <param name="sqlPage">An SQL builder object representing the SQL to retrieve a single page of results</param>
        /// <returns>A Page of results</returns>
        /// <remarks>
        /// This method allows separate SQL statements to be explicitly provided for the two parts of the page query.
        /// The page and itemsPerPage parameters are not used directly and are used simply to populate the returned Page object.
        /// </remarks>
        public Page<T> Page<T>(long page, long itemsPerPage, Sql sqlCount, Sql sqlPage)
        {
            return this.Page<T>(page, itemsPerPage, sqlCount.SQL, sqlCount.Arguments, sqlPage.SQL, sqlPage.Arguments);
        }
        /// <summary>
        /// Retrieves a page of records (without the total count)
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="page">The 1 based page number to retrieve</param>
        /// <param name="itemsPerPage">The number of records per page</param>
        /// <param name="sql">The base SQL query</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL statement</param>
        /// <returns>A List of results</returns>
        /// <remarks>
        /// PetaPoco will automatically modify the supplied SELECT statement to only retrieve the
        /// records for the specified page.
        /// </remarks>
        public System.Collections.Generic.List<T> Fetch<T>(long page, long itemsPerPage, string sql, params object[] args)
        {
            return this.SkipTake<T>((page - 1L) * itemsPerPage, itemsPerPage, sql, args);
        }
        /// <summary>
        /// Retrieves a page of records (without the total count)
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="page">The 1 based page number to retrieve</param>
        /// <param name="itemsPerPage">The number of records per page</param>
        /// <param name="sql">An SQL builder object representing the base SQL query and it's arguments</param>
        /// <returns>A List of results</returns>
        /// <remarks>
        /// PetaPoco will automatically modify the supplied SELECT statement to only retrieve the
        /// records for the specified page.
        /// </remarks>
        public System.Collections.Generic.List<T> Fetch<T>(long page, long itemsPerPage, Sql sql)
        {
            return this.SkipTake<T>((page - 1L) * itemsPerPage, itemsPerPage, sql.SQL, sql.Arguments);
        }
        /// <summary>
        /// Retrieves a range of records from result set
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="skip">The number of rows at the start of the result set to skip over</param>
        /// <param name="take">The number of rows to retrieve</param>
        /// <param name="sql">The base SQL query</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL statement</param>
        /// <returns>A List of results</returns>
        /// <remarks>
        /// PetaPoco will automatically modify the supplied SELECT statement to only retrieve the
        /// records for the specified range.
        /// </remarks>
        public System.Collections.Generic.List<T> SkipTake<T>(long skip, long take, string sql, params object[] args)
        {
            string text;
            string sql2;
            this.BuildPageQueries<T>(skip, take, sql, ref args, out text, out sql2);
            return this.Fetch<T>(sql2, args);
        }
        /// <summary>
        /// Retrieves a range of records from result set
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="skip">The number of rows at the start of the result set to skip over</param>
        /// <param name="take">The number of rows to retrieve</param>
        /// <param name="sql">An SQL builder object representing the base SQL query and it's arguments</param>
        /// <returns>A List of results</returns>
        /// <remarks>
        /// PetaPoco will automatically modify the supplied SELECT statement to only retrieve the
        /// records for the specified range.
        /// </remarks>
        public System.Collections.Generic.List<T> SkipTake<T>(long skip, long take, Sql sql)
        {
            return this.SkipTake<T>(skip, take, sql.SQL, sql.Arguments);
        }
        /// <summary>
        /// Runs an SQL query, returning the results as an IEnumerable collection
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="sql">The SQL query</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL statement</param>
        /// <returns>An enumerable collection of result records</returns>
        /// <remarks>
        /// For some DB providers, care should be taken to not start a new Query before finishing with
        /// and disposing the previous one. In cases where this is an issue, consider using Fetch which
        /// returns the results as a List rather than an IEnumerable.
        /// </remarks>
        public System.Collections.Generic.IEnumerable<T> Query<T>(string sql, params object[] args)
        {
            if (this.EnableAutoSelect)
            {
                sql = AutoSelectHelper.AddSelectClause<T>(this._dbType, sql, null);
            }
            System.Collections.Generic.List<T> list = new System.Collections.Generic.List<T>();
            PocoData pocoData = PocoData.ForType(typeof(T));
            this.OpenSharedConnection();
            try
            {
                lock (this.syncObj)
                {
                    using (IDbCommand dbCommand = this.CreateCommand(this._sharedConnection, sql, args))
                    {
                        using (IDataReader dataReader = dbCommand.ExecuteReader())
                        {
                            this.OnExecutedCommand(dbCommand);
                            Func<IDataReader, T> func = pocoData.GetFactory(dbCommand.CommandText, this._sharedConnection.ConnectionString, 0, dataReader.FieldCount, dataReader) as Func<IDataReader, T>;
                            while (dataReader.Read())
                            {
                                T item = func(dataReader);
                                list.Add(item);
                            }
                            dataReader.Close();
                        }
                    }
                }
            }
            finally
            {
                this.CloseSharedConnection();
            }
            return list;
        }
        /// <summary>
        /// Runs an SQL query, returning the results as an IEnumerable collection
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="sql">An SQL builder object representing the base SQL query and it's arguments</param>
        /// <returns>An enumerable collection of result records</returns>
        /// <remarks>
        /// For some DB providers, care should be taken to not start a new Query before finishing with
        /// and disposing the previous one. In cases where this is an issue, consider using Fetch which
        /// returns the results as a List rather than an IEnumerable.
        /// </remarks>
        public System.Collections.Generic.IEnumerable<T> Query<T>(Sql sql)
        {
            return this.Query<T>(sql.SQL, sql.Arguments);
        }
        /// <summary>
        /// Checks for the existance of a row matching the specified condition
        /// </summary>
        /// <typeparam name="T">The Type representing the table being queried</typeparam>
        /// <param name="sqlCondition">The SQL expression to be tested for (ie: the WHERE expression)</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL statement</param>
        /// <returns>True if a record matching the condition is found.</returns>
        public bool Exists<T>(string sqlCondition, params object[] args)
        {
            TableInfo tableInfo = PocoData.ForType(typeof(T)).TableInfo;
            return this.ExecuteScalar<int>(string.Format(this._dbType.GetExistsSql(), tableInfo.TableName, sqlCondition), args) != 0;
        }
        /// <summary>
        /// Checks for the existance of a row with the specified primary key value.
        /// </summary>
        /// <typeparam name="T">The Type representing the table being queried</typeparam>
        /// <param name="primaryKey">The primary key value to look for</param>
        /// <returns>True if a record with the specified primary key value exists.</returns>
        public bool Exists<T>(object primaryKey)
        {
            return this.Exists<T>(string.Format("{0}=@0", this._dbType.EscapeSqlIdentifier(PocoData.ForType(typeof(T)).TableInfo.PrimaryKey)), new object[]
			{
				primaryKey
			});
        }
        /// <summary>
        /// Returns the record with the specified primary key value
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="primaryKey">The primary key value of the record to fetch</param>
        /// <returns>The single record matching the specified primary key value</returns>
        /// <remarks>
        /// Throws an exception if there are zero or more than one record with the specified primary key value.
        /// </remarks>
        public T Single<T>(object primaryKey)
        {
            return this.Single<T>(string.Format("WHERE {0}=@0", this._dbType.EscapeSqlIdentifier(PocoData.ForType(typeof(T)).TableInfo.PrimaryKey)), new object[]
			{
				primaryKey
			});
        }
        /// <summary>
        /// Returns the record with the specified primary key value, or the default value if not found
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="primaryKey">The primary key value of the record to fetch</param>
        /// <returns>The single record matching the specified primary key value</returns>
        /// <remarks>
        /// If there are no records with the specified primary key value, default(T) (typically null) is returned.
        /// </remarks>
        public T SingleOrDefault<T>(object primaryKey)
        {
            return this.SingleOrDefault<T>(string.Format("WHERE {0}=@0", this._dbType.EscapeSqlIdentifier(PocoData.ForType(typeof(T)).TableInfo.PrimaryKey)), new object[]
			{
				primaryKey
			});
        }
        /// <summary>
        /// Runs a query that should always return a single row.
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="sql">The SQL query</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL statement</param>
        /// <returns>The single record matching the specified primary key value</returns>
        /// <remarks>
        /// Throws an exception if there are zero or more than one matching record
        /// </remarks>
        public T Single<T>(string sql, params object[] args)
        {
            return this.Query<T>(sql, args).Single<T>();
        }
        /// <summary>
        /// Runs a query that should always return either a single row, or no rows
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="sql">The SQL query</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL statement</param>
        /// <returns>The single record matching the specified primary key value, or default(T) if no matching rows</returns>
        public T SingleOrDefault<T>(string sql, params object[] args)
        {
            return this.Query<T>(sql, args).SingleOrDefault<T>();
        }
        /// <summary>
        /// Runs a query that should always return at least one return
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="sql">The SQL query</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL statement</param>
        /// <returns>The first record in the result set</returns>
        public T First<T>(string sql, params object[] args)
        {
            return this.Query<T>(sql, args).First<T>();
        }
        /// <summary>
        /// Runs a query and returns the first record, or the default value if no matching records
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="sql">The SQL query</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL statement</param>
        /// <returns>The first record in the result set, or default(T) if no matching rows</returns>
        public T FirstOrDefault<T>(string sql, params object[] args)
        {
            return this.Query<T>(sql, args).FirstOrDefault<T>();
        }
        /// <summary>
        /// Runs a query that should always return a single row.
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="sql">An SQL builder object representing the query and it's arguments</param>
        /// <returns>The single record matching the specified primary key value</returns>
        /// <remarks>
        /// Throws an exception if there are zero or more than one matching record
        /// </remarks>
        public T Single<T>(Sql sql)
        {
            return this.Query<T>(sql).Single<T>();
        }
        /// <summary>
        /// Runs a query that should always return either a single row, or no rows
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="sql">An SQL builder object representing the query and it's arguments</param>
        /// <returns>The single record matching the specified primary key value, or default(T) if no matching rows</returns>
        public T SingleOrDefault<T>(Sql sql)
        {
            return this.Query<T>(sql).SingleOrDefault<T>();
        }
        /// <summary>
        /// Runs a query that should always return at least one return
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="sql">An SQL builder object representing the query and it's arguments</param>
        /// <returns>The first record in the result set</returns>
        public T First<T>(Sql sql)
        {
            return this.Query<T>(sql).First<T>();
        }
        /// <summary>
        /// Runs a query and returns the first record, or the default value if no matching records
        /// </summary>
        /// <typeparam name="T">The Type representing a row in the result set</typeparam>
        /// <param name="sql">An SQL builder object representing the query and it's arguments</param>
        /// <returns>The first record in the result set, or default(T) if no matching rows</returns>
        public T FirstOrDefault<T>(Sql sql)
        {
            return this.Query<T>(sql).FirstOrDefault<T>();
        }
        /// <summary>
        /// Performs an SQL Insert
        /// </summary>
        /// <param name="tableName">The name of the table to insert into</param>
        /// <param name="primaryKeyName">The name of the primary key column of the table</param>
        /// <param name="poco">The POCO object that specifies the column values to be inserted</param>
        /// <returns>The auto allocated primary key of the new record</returns>
        public object Insert(string tableName, string primaryKeyName, object poco)
        {
            return this.Insert(tableName, primaryKeyName, true, poco);
        }
        /// <summary>
        /// Performs an SQL Insert
        /// </summary>
        /// <param name="tableName">The name of the table to insert into</param>
        /// <param name="primaryKeyName">The name of the primary key column of the table</param>
        /// <param name="autoIncrement">True if the primary key is automatically allocated by the DB</param>
        /// <param name="poco">The POCO object that specifies the column values to be inserted</param>
        /// <returns>The auto allocated primary key of the new record, or null for non-auto-increment tables</returns>
        /// <remarks>Inserts a poco into a table.  If the poco has a property with the same name 
        /// as the primary key the id of the new record is assigned to it.  Either way,
        /// the new id is returned.</remarks>
        public object Insert(string tableName, string primaryKeyName, bool autoIncrement, object poco)
        {
            object result;
            try
            {
                this.OpenSharedConnection();
                try
                {
                    lock (this.syncObj)
                    {
                        using (IDbCommand dbCommand = this.CreateCommand(this._sharedConnection, "", new object[0]))
                        {
                            PocoData pocoData = PocoData.ForObject(poco, primaryKeyName);
                            System.Collections.Generic.List<string> list = new System.Collections.Generic.List<string>();
                            System.Collections.Generic.List<string> list2 = new System.Collections.Generic.List<string>();
                            int num = 0;
                            foreach (System.Collections.Generic.KeyValuePair<string, PocoColumn> current in pocoData.Columns)
                            {
                                if (!current.Value.ResultColumn && (SqlBehaviorFlags.Insert & current.Value.SqlBehavior) != (SqlBehaviorFlags)0)
                                {
                                    if (autoIncrement && primaryKeyName != null && string.Compare(current.Key, primaryKeyName, true) == 0)
                                    {
                                        string autoIncrementExpression = this._dbType.GetAutoIncrementExpression(pocoData.TableInfo);
                                        if (autoIncrementExpression != null)
                                        {
                                            list.Add(current.Key);
                                            list2.Add(autoIncrementExpression);
                                        }
                                    }
                                    else
                                    {
                                        list.Add(this._dbType.EscapeSqlIdentifier(current.Key));
                                        list2.Add(string.Format("{0}{1}", this._paramPrefix, num++));
                                        this.AddParam(dbCommand, current.Value.GetValue(poco), current.Value.PropertyInfo);
                                    }
                                }
                            }
                            string text = string.Empty;
                            if (autoIncrement)
                            {
                                text = this._dbType.GetInsertOutputClause(primaryKeyName);
                            }
                            dbCommand.CommandText = string.Format("INSERT INTO {0} ({1}){2} VALUES ({3})", new object[]
							{
								this._dbType.EscapeTableName(tableName),
								string.Join(",", list.ToArray()),
								text,
								string.Join(",", list2.ToArray())
							});
                            if (!autoIncrement)
                            {
                                this.DoPreExecute(dbCommand);
                                dbCommand.ExecuteNonQuery();
                                this.OnExecutedCommand(dbCommand);
                                PocoColumn pocoColumn;
                                if (primaryKeyName != null && pocoData.Columns.TryGetValue(primaryKeyName, out pocoColumn))
                                {
                                    result = pocoColumn.GetValue(poco);
                                }
                                else
                                {
                                    result = null;
                                }
                            }
                            else
                            {
                                object obj2 = this._dbType.ExecuteInsert(this, dbCommand, primaryKeyName);
                                PocoColumn pocoColumn2;
                                if (primaryKeyName != null && pocoData.Columns.TryGetValue(primaryKeyName, out pocoColumn2))
                                {
                                    pocoColumn2.SetValue(poco, pocoColumn2.ChangeType(obj2));
                                }
                                result = obj2;
                            }
                        }
                    }
                }
                finally
                {
                    this.CloseSharedConnection();
                }
            }
            catch (System.Exception x)
            {
                if (this.OnException(x))
                {
                    throw;
                }
                result = null;
            }
            return result;
        }
        /// <summary>
        /// Performs an SQL Insert
        /// </summary>
        /// <param name="poco">The POCO object that specifies the column values to be inserted</param>
        /// <returns>The auto allocated primary key of the new record, or null for non-auto-increment tables</returns>
        /// <remarks>The name of the table, it's primary key and whether it's an auto-allocated primary key are retrieved
        /// from the POCO's attributes</remarks>
        public object Insert(object poco)
        {
            PocoData pocoData = PocoData.ForType(poco.GetType());
            return this.Insert(pocoData.TableInfo.TableName, pocoData.TableInfo.PrimaryKey, pocoData.TableInfo.AutoIncrement, poco);
        }
        /// <summary>
        /// Performs an SQL update
        /// </summary>
        /// <param name="tableName">The name of the table to update</param>
        /// <param name="primaryKeyName">The name of the primary key column of the table</param>
        /// <param name="poco">The POCO object that specifies the column values to be updated</param>
        /// <param name="primaryKeyValue">The primary key of the record to be updated</param>
        /// <returns>The number of affected records</returns>
        public int Update(string tableName, string primaryKeyName, object poco, object primaryKeyValue)
        {
            return this.Update(tableName, primaryKeyName, poco, primaryKeyValue, null);
        }
        /// <summary>
        /// Performs an SQL update
        /// </summary>
        /// <param name="tableName">The name of the table to update</param>
        /// <param name="primaryKeyName">The name of the primary key column of the table</param>
        /// <param name="poco">The POCO object that specifies the column values to be updated</param>
        /// <param name="primaryKeyValue">The primary key of the record to be updated</param>
        /// <param name="columns">The column names of the columns to be updated, or null for all</param>
        /// <returns>The number of affected rows</returns>
        public int Update(string tableName, string primaryKeyName, object poco, object primaryKeyValue, System.Collections.Generic.IEnumerable<string> columns)
        {
            int result;
            try
            {
                this.OpenSharedConnection();
                try
                {
                    lock (this.syncObj)
                    {
                        using (IDbCommand dbCommand = this.CreateCommand(this._sharedConnection, "", new object[0]))
                        {
                            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
                            int num = 0;
                            PocoData pocoData = PocoData.ForObject(poco, primaryKeyName);
                            if (columns == null)
                            {
                                using (System.Collections.Generic.Dictionary<string, PocoColumn>.Enumerator enumerator = pocoData.Columns.GetEnumerator())
                                {
                                    while (enumerator.MoveNext())
                                    {
                                        System.Collections.Generic.KeyValuePair<string, PocoColumn> current = enumerator.Current;
                                        if (string.Compare(current.Key, primaryKeyName, true) == 0)
                                        {
                                            if (primaryKeyValue == null)
                                            {
                                                primaryKeyValue = current.Value.GetValue(poco);
                                            }
                                        }
                                        else if (!current.Value.ResultColumn && (SqlBehaviorFlags.Update & current.Value.SqlBehavior) != (SqlBehaviorFlags)0)
                                        {
                                            if (num > 0)
                                            {
                                                stringBuilder.Append(", ");
                                            }
                                            stringBuilder.AppendFormat("{0} = {1}{2}", this._dbType.EscapeSqlIdentifier(current.Key), this._paramPrefix, num++);
                                            this.AddParam(dbCommand, current.Value.GetValue(poco), current.Value.PropertyInfo);
                                        }
                                    }
                                    goto IL_1CB;
                                }
                            }
                            foreach (string current2 in columns)
                            {
                                PocoColumn pocoColumn = pocoData.Columns[current2];
                                if (num > 0)
                                {
                                    stringBuilder.Append(", ");
                                }
                                stringBuilder.AppendFormat("{0} = {1}{2}", this._dbType.EscapeSqlIdentifier(current2), this._paramPrefix, num++);
                                this.AddParam(dbCommand, pocoColumn.GetValue(poco), pocoColumn.PropertyInfo);
                            }
                            if (primaryKeyValue == null)
                            {
                                PocoColumn pocoColumn2 = pocoData.Columns[primaryKeyName];
                                primaryKeyValue = pocoColumn2.GetValue(poco);
                            }
                        IL_1CB:
                            System.Reflection.PropertyInfo pi = null;
                            if (primaryKeyName != null)
                            {
                                pi = pocoData.Columns[primaryKeyName].PropertyInfo;
                            }
                            dbCommand.CommandText = string.Format("UPDATE {0} SET {1} WHERE {2} = {3}{4}", new object[]
							{
								this._dbType.EscapeTableName(tableName),
								stringBuilder.ToString(),
								this._dbType.EscapeSqlIdentifier(primaryKeyName),
								this._paramPrefix,
								num++
							});
                            this.AddParam(dbCommand, primaryKeyValue, pi);
                            this.DoPreExecute(dbCommand);
                            int num2 = dbCommand.ExecuteNonQuery();
                            this.OnExecutedCommand(dbCommand);
                            result = num2;
                        }
                    }
                }
                finally
                {
                    this.CloseSharedConnection();
                }
            }
            catch (System.Exception x)
            {
                if (this.OnException(x))
                {
                    throw;
                }
                result = -1;
            }
            return result;
        }
        /// <summary>
        /// Performs an SQL update
        /// </summary>
        /// <param name="tableName">The name of the table to update</param>
        /// <param name="primaryKeyName">The name of the primary key column of the table</param>
        /// <param name="poco">The POCO object that specifies the column values to be updated</param>
        /// <returns>The number of affected rows</returns>
        public int Update(string tableName, string primaryKeyName, object poco)
        {
            return this.Update(tableName, primaryKeyName, poco, null);
        }
        /// <summary>
        /// Performs an SQL update
        /// </summary>
        /// <param name="tableName">The name of the table to update</param>
        /// <param name="primaryKeyName">The name of the primary key column of the table</param>
        /// <param name="poco">The POCO object that specifies the column values to be updated</param>
        /// <param name="columns">The column names of the columns to be updated, or null for all</param>
        /// <returns>The number of affected rows</returns>
        public int Update(string tableName, string primaryKeyName, object poco, System.Collections.Generic.IEnumerable<string> columns)
        {
            return this.Update(tableName, primaryKeyName, poco, null, columns);
        }
        /// <summary>
        /// Performs an SQL update
        /// </summary>
        /// <param name="poco">The POCO object that specifies the column values to be updated</param>
        /// <param name="columns">The column names of the columns to be updated, or null for all</param>
        /// <returns>The number of affected rows</returns>
        public int Update(object poco, System.Collections.Generic.IEnumerable<string> columns)
        {
            return this.Update(poco, null, columns);
        }
        /// <summary>
        /// Performs an SQL update
        /// </summary>
        /// <param name="poco">The POCO object that specifies the column values to be updated</param>
        /// <returns>The number of affected rows</returns>
        public int Update(object poco)
        {
            return this.Update(poco, null, null);
        }
        /// <summary>
        /// Performs an SQL update
        /// </summary>
        /// <param name="poco">The POCO object that specifies the column values to be updated</param>
        /// <param name="primaryKeyValue">The primary key of the record to be updated</param>
        /// <returns>The number of affected rows</returns>
        public int Update(object poco, object primaryKeyValue)
        {
            return this.Update(poco, primaryKeyValue, null);
        }
        /// <summary>
        /// Performs an SQL update
        /// </summary>
        /// <param name="poco">The POCO object that specifies the column values to be updated</param>
        /// <param name="primaryKeyValue">The primary key of the record to be updated</param>
        /// <param name="columns">The column names of the columns to be updated, or null for all</param>
        /// <returns>The number of affected rows</returns>
        public int Update(object poco, object primaryKeyValue, System.Collections.Generic.IEnumerable<string> columns)
        {
            PocoData pocoData = PocoData.ForType(poco.GetType());
            return this.Update(pocoData.TableInfo.TableName, pocoData.TableInfo.PrimaryKey, poco, primaryKeyValue, columns);
        }
        /// <summary>
        /// Performs an SQL update
        /// </summary>
        /// <typeparam name="T">The POCO class who's attributes specify the name of the table to update</typeparam>
        /// <param name="sql">The SQL update and condition clause (ie: everything after "UPDATE tablename"</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL</param>
        /// <returns>The number of affected rows</returns>
        public int Update<T>(string sql, params object[] args)
        {
            PocoData pocoData = PocoData.ForType(typeof(T));
            return this.Execute(string.Format("UPDATE {0} {1}", this._dbType.EscapeTableName(pocoData.TableInfo.TableName), sql), args);
        }
        /// <summary>
        /// Performs an SQL update
        /// </summary>
        /// <typeparam name="T">The POCO class who's attributes specify the name of the table to update</typeparam>
        /// <param name="sql">An SQL builder object representing the SQL update and condition clause (ie: everything after "UPDATE tablename"</param>
        /// <returns>The number of affected rows</returns>
        public int Update<T>(Sql sql)
        {
            PocoData pocoData = PocoData.ForType(typeof(T));
            return this.Execute(new Sql(string.Format("UPDATE {0}", this._dbType.EscapeTableName(pocoData.TableInfo.TableName)), new object[0]).Append(sql));
        }
        /// <summary>
        /// Performs and SQL Delete
        /// </summary>
        /// <param name="tableName">The name of the table to delete from</param>
        /// <param name="primaryKeyName">The name of the primary key column</param>
        /// <param name="poco">The POCO object whose primary key value will be used to delete the row</param>
        /// <returns>The number of rows affected</returns>
        public int Delete(string tableName, string primaryKeyName, object poco)
        {
            return this.Delete(tableName, primaryKeyName, poco, null);
        }
        /// <summary>
        /// Performs and SQL Delete
        /// </summary>
        /// <param name="tableName">The name of the table to delete from</param>
        /// <param name="primaryKeyName">The name of the primary key column</param>
        /// <param name="poco">The POCO object whose primary key value will be used to delete the row (or null to use the supplied primary key value)</param>
        /// <param name="primaryKeyValue">The value of the primary key identifing the record to be deleted (or null, or get this value from the POCO instance)</param>
        /// <returns>The number of rows affected</returns>
        public int Delete(string tableName, string primaryKeyName, object poco, object primaryKeyValue)
        {
            if (primaryKeyValue == null)
            {
                PocoData pocoData = PocoData.ForObject(poco, primaryKeyName);
                PocoColumn pocoColumn;
                if (pocoData.Columns.TryGetValue(primaryKeyName, out pocoColumn))
                {
                    primaryKeyValue = pocoColumn.GetValue(poco);
                }
            }
            string sql = string.Format("DELETE FROM {0} WHERE {1}=@0", this._dbType.EscapeTableName(tableName), this._dbType.EscapeSqlIdentifier(primaryKeyName));
            return this.Execute(sql, new object[]
			{
				primaryKeyValue
			});
        }
        /// <summary>
        /// Performs an SQL Delete
        /// </summary>
        /// <param name="poco">The POCO object specifying the table name and primary key value of the row to be deleted</param>
        /// <returns>The number of rows affected</returns>
        public int Delete(object poco)
        {
            PocoData pocoData = PocoData.ForType(poco.GetType());
            return this.Delete(pocoData.TableInfo.TableName, pocoData.TableInfo.PrimaryKey, poco);
        }
        /// <summary>
        /// Performs an SQL Delete
        /// </summary>
        /// <typeparam name="T">The POCO class whose attributes identify the table and primary key to be used in the delete</typeparam>
        /// <param name="pocoOrPrimaryKey">The value of the primary key of the row to delete</param>
        /// <returns></returns>
        public int Delete<T>(object pocoOrPrimaryKey)
        {
            if (pocoOrPrimaryKey.GetType() == typeof(T))
            {
                return this.Delete(pocoOrPrimaryKey);
            }
            PocoData pocoData = PocoData.ForType(typeof(T));
            return this.Delete(pocoData.TableInfo.TableName, pocoData.TableInfo.PrimaryKey, null, pocoOrPrimaryKey);
        }
        /// <summary>
        /// Performs an SQL Delete
        /// </summary>
        /// <typeparam name="T">The POCO class who's attributes specify the name of the table to delete from</typeparam>
        /// <param name="sql">The SQL condition clause identifying the row to delete (ie: everything after "DELETE FROM tablename"</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL</param>
        /// <returns>The number of affected rows</returns>
        public int Delete<T>(string sql, params object[] args)
        {
            PocoData pocoData = PocoData.ForType(typeof(T));
            return this.Execute(string.Format("DELETE FROM {0} {1}", this._dbType.EscapeTableName(pocoData.TableInfo.TableName), sql), args);
        }
        /// <summary>
        /// Performs an SQL Delete
        /// </summary>
        /// <typeparam name="T">The POCO class who's attributes specify the name of the table to delete from</typeparam>
        /// <param name="sql">An SQL builder object representing the SQL condition clause identifying the row to delete (ie: everything after "UPDATE tablename"</param>
        /// <returns>The number of affected rows</returns>
        public int Delete<T>(Sql sql)
        {
            PocoData pocoData = PocoData.ForType(typeof(T));
            return this.Execute(new Sql(string.Format("DELETE FROM {0}", this._dbType.EscapeTableName(pocoData.TableInfo.TableName)), new object[0]).Append(sql));
        }
        /// <summary>
        /// Check if a poco represents a new row
        /// </summary>
        /// <param name="primaryKeyName">The name of the primary key column</param>
        /// <param name="poco">The object instance whose "newness" is to be tested</param>
        /// <returns>True if the POCO represents a record already in the database</returns>
        /// <remarks>This method simply tests if the POCO's primary key column property has been set to something non-zero.</remarks>
        public bool IsNew(string primaryKeyName, object poco)
        {
            PocoData pocoData = PocoData.ForObject(poco, primaryKeyName);
            PocoColumn pocoColumn;
            object value;
            if (pocoData.Columns.TryGetValue(primaryKeyName, out pocoColumn))
            {
                value = pocoColumn.GetValue(poco);
            }
            else
            {
                if (poco.GetType() == typeof(ExpandoObject))
                {
                    return true;
                }
                System.Reflection.PropertyInfo property = poco.GetType().GetProperty(primaryKeyName);
                if (property == null)
                {
                    throw new System.ArgumentException(string.Format("The object doesn't have a property matching the primary key column name '{0}'", primaryKeyName));
                }
                value = property.GetValue(poco, null);
            }
            if (value == null)
            {
                return true;
            }
            System.Type type = value.GetType();
            if (!type.IsValueType)
            {
                return value == null;
            }
            if (type == typeof(long))
            {
                return (long)value == 0L;
            }
            if (type == typeof(ulong))
            {
                return (ulong)value == 0uL;
            }
            if (type == typeof(int))
            {
                return (int)value == 0;
            }
            if (type == typeof(uint))
            {
                return (uint)value == 0u;
            }
            if (type == typeof(System.Guid))
            {
                return (System.Guid)value == default(System.Guid);
            }
            return value == System.Activator.CreateInstance(value.GetType());
        }
        /// <summary>
        /// Check if a poco represents a new row
        /// </summary>
        /// <param name="poco">The object instance whose "newness" is to be tested</param>
        /// <returns>True if the POCO represents a record already in the database</returns>
        /// <remarks>This method simply tests if the POCO's primary key column property has been set to something non-zero.</remarks>
        public bool IsNew(object poco)
        {
            PocoData pocoData = PocoData.ForType(poco.GetType());
            if (!pocoData.TableInfo.AutoIncrement)
            {
                throw new System.InvalidOperationException("IsNew() and Save() are only supported on tables with auto-increment/identity primary key columns");
            }
            return this.IsNew(pocoData.TableInfo.PrimaryKey, poco);
        }
        /// <summary>
        /// Saves a POCO by either performing either an SQL Insert or SQL Update
        /// </summary>
        /// <param name="tableName">The name of the table to be updated</param>
        /// <param name="primaryKeyName">The name of the primary key column</param>
        /// <param name="poco">The POCO object to be saved</param>
        public void Save(string tableName, string primaryKeyName, object poco)
        {
            if (this.IsNew(primaryKeyName, poco))
            {
                this.Insert(tableName, primaryKeyName, true, poco);
                return;
            }
            this.Update(tableName, primaryKeyName, poco);
        }
        /// <summary>
        /// Saves a POCO by either performing either an SQL Insert or SQL Update
        /// </summary>
        /// <param name="poco">The POCO object to be saved</param>
        public void Save(object poco)
        {
            PocoData pocoData = PocoData.ForType(poco.GetType());
            this.Save(pocoData.TableInfo.TableName, pocoData.TableInfo.PrimaryKey, poco);
        }
        /// <summary>
        /// Perform a multi-poco fetch
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="TRet">The returned list POCO type</typeparam>
        /// <param name="cb">A callback function to connect the POCO instances, or null to automatically guess the relationships</param>
        /// <param name="sql">The SQL query to be executed</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL</param>
        /// <returns>A collection of POCO's as a List</returns>
        public System.Collections.Generic.List<TRet> Fetch<T1, T2, TRet>(Func<T1, T2, TRet> cb, string sql, params object[] args)
        {
            return this.Query<T1, T2, TRet>(cb, sql, args).ToList<TRet>();
        }
        /// <summary>
        /// Perform a multi-poco fetch
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="T3">The third POCO type</typeparam>
        /// <typeparam name="TRet">The returned list POCO type</typeparam>
        /// <param name="cb">A callback function to connect the POCO instances, or null to automatically guess the relationships</param>
        /// <param name="sql">The SQL query to be executed</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL</param>
        /// <returns>A collection of POCO's as a List</returns>
        public System.Collections.Generic.List<TRet> Fetch<T1, T2, T3, TRet>(Func<T1, T2, T3, TRet> cb, string sql, params object[] args)
        {
            return this.Query<T1, T2, T3, TRet>(cb, sql, args).ToList<TRet>();
        }
        /// <summary>
        /// Perform a multi-poco fetch
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="T3">The third POCO type</typeparam>
        /// <typeparam name="T4">The fourth POCO type</typeparam>
        /// <typeparam name="TRet">The returned list POCO type</typeparam>
        /// <param name="cb">A callback function to connect the POCO instances, or null to automatically guess the relationships</param>
        /// <param name="sql">The SQL query to be executed</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL</param>
        /// <returns>A collection of POCO's as a List</returns>
        public System.Collections.Generic.List<TRet> Fetch<T1, T2, T3, T4, TRet>(Func<T1, T2, T3, T4, TRet> cb, string sql, params object[] args)
        {
            return this.Query<T1, T2, T3, T4, TRet>(cb, sql, args).ToList<TRet>();
        }
        /// <summary>
        /// Perform a multi-poco query
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="TRet">The type of objects in the returned IEnumerable</typeparam>
        /// <param name="cb">A callback function to connect the POCO instances, or null to automatically guess the relationships</param>
        /// <param name="sql">The SQL query to be executed</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL</param>
        /// <returns>A collection of POCO's as an IEnumerable</returns>
        public System.Collections.Generic.IEnumerable<TRet> Query<T1, T2, TRet>(Func<T1, T2, TRet> cb, string sql, params object[] args)
        {
            return this.Query<TRet>(new System.Type[]
			{
				typeof(T1),
				typeof(T2)
			}, cb, sql, args);
        }
        /// <summary>
        /// Perform a multi-poco query
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="T3">The third POCO type</typeparam>
        /// <typeparam name="TRet">The type of objects in the returned IEnumerable</typeparam>
        /// <param name="cb">A callback function to connect the POCO instances, or null to automatically guess the relationships</param>
        /// <param name="sql">The SQL query to be executed</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL</param>
        /// <returns>A collection of POCO's as an IEnumerable</returns>
        public System.Collections.Generic.IEnumerable<TRet> Query<T1, T2, T3, TRet>(Func<T1, T2, T3, TRet> cb, string sql, params object[] args)
        {
            return this.Query<TRet>(new System.Type[]
			{
				typeof(T1),
				typeof(T2),
				typeof(T3)
			}, cb, sql, args);
        }
        /// <summary>
        /// Perform a multi-poco query
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="T3">The third POCO type</typeparam>
        /// <typeparam name="T4">The fourth POCO type</typeparam>
        /// <typeparam name="TRet">The type of objects in the returned IEnumerable</typeparam>
        /// <param name="cb">A callback function to connect the POCO instances, or null to automatically guess the relationships</param>
        /// <param name="sql">The SQL query to be executed</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL</param>
        /// <returns>A collection of POCO's as an IEnumerable</returns>
        public System.Collections.Generic.IEnumerable<TRet> Query<T1, T2, T3, T4, TRet>(Func<T1, T2, T3, T4, TRet> cb, string sql, params object[] args)
        {
            return this.Query<TRet>(new System.Type[]
			{
				typeof(T1),
				typeof(T2),
				typeof(T3),
				typeof(T4)
			}, cb, sql, args);
        }
        /// <summary>
        /// Perform a multi-poco fetch
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="TRet">The returned list POCO type</typeparam>
        /// <param name="cb">A callback function to connect the POCO instances, or null to automatically guess the relationships</param>
        /// <param name="sql">An SQL builder object representing the query and it's arguments</param>
        /// <returns>A collection of POCO's as a List</returns>
        public System.Collections.Generic.List<TRet> Fetch<T1, T2, TRet>(Func<T1, T2, TRet> cb, Sql sql)
        {
            return this.Query<T1, T2, TRet>(cb, sql.SQL, sql.Arguments).ToList<TRet>();
        }
        /// <summary>
        /// Perform a multi-poco fetch
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="T3">The third POCO type</typeparam>
        /// <typeparam name="TRet">The returned list POCO type</typeparam>
        /// <param name="cb">A callback function to connect the POCO instances, or null to automatically guess the relationships</param>
        /// <param name="sql">An SQL builder object representing the query and it's arguments</param>
        /// <returns>A collection of POCO's as a List</returns>
        public System.Collections.Generic.List<TRet> Fetch<T1, T2, T3, TRet>(Func<T1, T2, T3, TRet> cb, Sql sql)
        {
            return this.Query<T1, T2, T3, TRet>(cb, sql.SQL, sql.Arguments).ToList<TRet>();
        }
        /// <summary>
        /// Perform a multi-poco fetch
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="T3">The third POCO type</typeparam>
        /// <typeparam name="T4">The fourth POCO type</typeparam>
        /// <typeparam name="TRet">The returned list POCO type</typeparam>
        /// <param name="cb">A callback function to connect the POCO instances, or null to automatically guess the relationships</param>
        /// <param name="sql">An SQL builder object representing the query and it's arguments</param>
        /// <returns>A collection of POCO's as a List</returns>
        public System.Collections.Generic.List<TRet> Fetch<T1, T2, T3, T4, TRet>(Func<T1, T2, T3, T4, TRet> cb, Sql sql)
        {
            return this.Query<T1, T2, T3, T4, TRet>(cb, sql.SQL, sql.Arguments).ToList<TRet>();
        }
        /// <summary>
        /// Perform a multi-poco query
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="TRet">The type of objects in the returned IEnumerable</typeparam>
        /// <param name="cb">A callback function to connect the POCO instances, or null to automatically guess the relationships</param>
        /// <param name="sql">An SQL builder object representing the query and it's arguments</param>
        /// <returns>A collection of POCO's as an IEnumerable</returns>
        public System.Collections.Generic.IEnumerable<TRet> Query<T1, T2, TRet>(Func<T1, T2, TRet> cb, Sql sql)
        {
            return this.Query<TRet>(new System.Type[]
			{
				typeof(T1),
				typeof(T2)
			}, cb, sql.SQL, sql.Arguments);
        }
        /// <summary>
        /// Perform a multi-poco query
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="T3">The third POCO type</typeparam>
        /// <typeparam name="TRet">The type of objects in the returned IEnumerable</typeparam>
        /// <param name="cb">A callback function to connect the POCO instances, or null to automatically guess the relationships</param>
        /// <param name="sql">An SQL builder object representing the query and it's arguments</param>
        /// <returns>A collection of POCO's as an IEnumerable</returns>
        public System.Collections.Generic.IEnumerable<TRet> Query<T1, T2, T3, TRet>(Func<T1, T2, T3, TRet> cb, Sql sql)
        {
            return this.Query<TRet>(new System.Type[]
			{
				typeof(T1),
				typeof(T2),
				typeof(T3)
			}, cb, sql.SQL, sql.Arguments);
        }
        /// <summary>
        /// Perform a multi-poco query
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="T3">The third POCO type</typeparam>
        /// <typeparam name="T4">The fourth POCO type</typeparam>
        /// <typeparam name="TRet">The type of objects in the returned IEnumerable</typeparam>
        /// <param name="cb">A callback function to connect the POCO instances, or null to automatically guess the relationships</param>
        /// <param name="sql">An SQL builder object representing the query and it's arguments</param>
        /// <returns>A collection of POCO's as an IEnumerable</returns>
        public System.Collections.Generic.IEnumerable<TRet> Query<T1, T2, T3, T4, TRet>(Func<T1, T2, T3, T4, TRet> cb, Sql sql)
        {
            return this.Query<TRet>(new System.Type[]
			{
				typeof(T1),
				typeof(T2),
				typeof(T3),
				typeof(T4)
			}, cb, sql.SQL, sql.Arguments);
        }
        /// <summary>
        /// Perform a multi-poco fetch
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <param name="sql">The SQL query to be executed</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL</param>
        /// <returns>A collection of POCO's as a List</returns>
        public System.Collections.Generic.List<T1> Fetch<T1, T2>(string sql, params object[] args)
        {
            return this.Query<T1, T2>(sql, args).ToList<T1>();
        }
        /// <summary>
        /// Perform a multi-poco fetch
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="T3">The third POCO type</typeparam>
        /// <param name="sql">The SQL query to be executed</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL</param>
        /// <returns>A collection of POCO's as a List</returns>
        public System.Collections.Generic.List<T1> Fetch<T1, T2, T3>(string sql, params object[] args)
        {
            return this.Query<T1, T2, T3>(sql, args).ToList<T1>();
        }
        /// <summary>
        /// Perform a multi-poco fetch
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="T3">The third POCO type</typeparam>
        /// <typeparam name="T4">The fourth POCO type</typeparam>
        /// <param name="sql">The SQL query to be executed</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL</param>
        /// <returns>A collection of POCO's as a List</returns>
        public System.Collections.Generic.List<T1> Fetch<T1, T2, T3, T4>(string sql, params object[] args)
        {
            return this.Query<T1, T2, T3, T4>(sql, args).ToList<T1>();
        }
        /// <summary>
        /// Perform a multi-poco query
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <param name="sql">The SQL query to be executed</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL</param>
        /// <returns>A collection of POCO's as an IEnumerable</returns>
        public System.Collections.Generic.IEnumerable<T1> Query<T1, T2>(string sql, params object[] args)
        {
            return this.Query<T1>(new System.Type[]
			{
				typeof(T1),
				typeof(T2)
			}, null, sql, args);
        }
        /// <summary>
        /// Perform a multi-poco query
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="T3">The third POCO type</typeparam>
        /// <param name="sql">The SQL query to be executed</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL</param>
        /// <returns>A collection of POCO's as an IEnumerable</returns>
        public System.Collections.Generic.IEnumerable<T1> Query<T1, T2, T3>(string sql, params object[] args)
        {
            return this.Query<T1>(new System.Type[]
			{
				typeof(T1),
				typeof(T2),
				typeof(T3)
			}, null, sql, args);
        }
        /// <summary>
        /// Perform a multi-poco query
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="T3">The third POCO type</typeparam>
        /// <typeparam name="T4">The fourth POCO type</typeparam>
        /// <param name="sql">The SQL query to be executed</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL</param>
        /// <returns>A collection of POCO's as an IEnumerable</returns>
        public System.Collections.Generic.IEnumerable<T1> Query<T1, T2, T3, T4>(string sql, params object[] args)
        {
            return this.Query<T1>(new System.Type[]
			{
				typeof(T1),
				typeof(T2),
				typeof(T3),
				typeof(T4)
			}, null, sql, args);
        }
        /// <summary>
        /// Perform a multi-poco fetch
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <param name="sql">An SQL builder object representing the query and it's arguments</param>
        /// <returns>A collection of POCO's as a List</returns>
        public System.Collections.Generic.List<T1> Fetch<T1, T2>(Sql sql)
        {
            return this.Query<T1, T2>(sql.SQL, sql.Arguments).ToList<T1>();
        }
        /// <summary>
        /// Perform a multi-poco fetch
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="T3">The third POCO type</typeparam>
        /// <param name="sql">An SQL builder object representing the query and it's arguments</param>
        /// <returns>A collection of POCO's as a List</returns>
        public System.Collections.Generic.List<T1> Fetch<T1, T2, T3>(Sql sql)
        {
            return this.Query<T1, T2, T3>(sql.SQL, sql.Arguments).ToList<T1>();
        }
        /// <summary>
        /// Perform a multi-poco fetch
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="T3">The third POCO type</typeparam>
        /// <typeparam name="T4">The fourth POCO type</typeparam>
        /// <param name="sql">An SQL builder object representing the query and it's arguments</param>
        /// <returns>A collection of POCO's as a List</returns>
        public System.Collections.Generic.List<T1> Fetch<T1, T2, T3, T4>(Sql sql)
        {
            return this.Query<T1, T2, T3, T4>(sql.SQL, sql.Arguments).ToList<T1>();
        }
        /// <summary>
        /// Perform a multi-poco query
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <param name="sql">An SQL builder object representing the query and it's arguments</param>
        /// <returns>A collection of POCO's as an IEnumerable</returns>
        public System.Collections.Generic.IEnumerable<T1> Query<T1, T2>(Sql sql)
        {
            return this.Query<T1>(new System.Type[]
			{
				typeof(T1),
				typeof(T2)
			}, null, sql.SQL, sql.Arguments);
        }
        /// <summary>
        /// Perform a multi-poco query
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="T3">The third POCO type</typeparam>
        /// <param name="sql">An SQL builder object representing the query and it's arguments</param>
        /// <returns>A collection of POCO's as an IEnumerable</returns>
        public System.Collections.Generic.IEnumerable<T1> Query<T1, T2, T3>(Sql sql)
        {
            return this.Query<T1>(new System.Type[]
			{
				typeof(T1),
				typeof(T2),
				typeof(T3)
			}, null, sql.SQL, sql.Arguments);
        }
        /// <summary>
        /// Perform a multi-poco query
        /// </summary>
        /// <typeparam name="T1">The first POCO type</typeparam>
        /// <typeparam name="T2">The second POCO type</typeparam>
        /// <typeparam name="T3">The third POCO type</typeparam>
        /// <typeparam name="T4">The fourth POCO type</typeparam>
        /// <param name="sql">An SQL builder object representing the query and it's arguments</param>
        /// <returns>A collection of POCO's as an IEnumerable</returns>
        public System.Collections.Generic.IEnumerable<T1> Query<T1, T2, T3, T4>(Sql sql)
        {
            return this.Query<T1>(new System.Type[]
			{
				typeof(T1),
				typeof(T2),
				typeof(T3),
				typeof(T4)
			}, null, sql.SQL, sql.Arguments);
        }
        /// <summary>
        /// Performs a multi-poco query
        /// </summary>
        /// <typeparam name="TRet">The type of objects in the returned IEnumerable</typeparam>
        /// <param name="types">An array of Types representing the POCO types of the returned result set.</param>
        /// <param name="cb">A callback function to connect the POCO instances, or null to automatically guess the relationships</param>
        /// <param name="sql">The SQL query to be executed</param>
        /// <param name="args">Arguments to any embedded parameters in the SQL</param>
        /// <returns>A collection of POCO's as an IEnumerable</returns>
        public System.Collections.Generic.IEnumerable<TRet> Query<TRet>(System.Type[] types, object cb, string sql, params object[] args)
        {
            this.OpenSharedConnection();
            try
            {
                using (IDbCommand dbCommand = this.CreateCommand(this._sharedConnection, sql, args))
                {
                    IDataReader dataReader;
                    try
                    {
                        dataReader = dbCommand.ExecuteReader();
                        this.OnExecutedCommand(dbCommand);
                    }
                    catch (System.Exception x)
                    {
                        if (this.OnException(x))
                        {
                            throw;
                        }
                        //位置报错 未处理
                        this.Dispose();
                        //base.System.IDisposable.Dispose();
                        goto IL_20A;
                    }
                    Func<IDataReader, object, TRet> factory = MultiPocoFactory.GetFactory<TRet>(types, this._sharedConnection.ConnectionString, sql, dataReader);
                    if (cb == null)
                    {
                        cb = MultiPocoFactory.GetAutoMapper(types.ToArray<System.Type>());
                    }
                    bool flag = false;
                    using (dataReader)
                    {
                        while (true)
                        {
                            TRet tRet;
                            try
                            {
                                if (!dataReader.Read())
                                {
                                    break;
                                }
                                tRet = factory(dataReader, cb);
                            }
                            catch (System.Exception x2)
                            {
                                if (this.OnException(x2))
                                {
                                    throw;
                                }
                                this.Dispose();
                                //base.System.IDisposable.Dispose();
                                goto IL_20A;
                            }
                            if (tRet != null)
                            {
                                yield return tRet;
                            }
                            else
                            {
                                flag = true;
                            }
                        }
                        if (flag)
                        {
                            TRet tRet2 = (TRet)((object)(cb as System.Delegate).DynamicInvoke(new object[types.Length]));
                            if (tRet2 == null)
                            {
                                yield break;
                            }
                            yield return tRet2;
                        }
                    }
                }
            }
            finally
            {
                this.CloseSharedConnection();
            }
        IL_20A:
            yield break;
        }
        /// <summary>
        /// Formats the contents of a DB command for display
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public string FormatCommand(IDbCommand cmd)
        {
            return this.FormatCommand(cmd.CommandText, (
                from IDataParameter parameter in cmd.Parameters
                select parameter.Value).ToArray<object>());
        }
        /// <summary>
        /// Formats an SQL query and it's arguments for display
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public string FormatCommand(string sql, object[] args)
        {
            System.Text.StringBuilder stringBuilder = new System.Text.StringBuilder();
            if (sql == null)
            {
                return "";
            }
            stringBuilder.Append(sql);
            if (args != null && args.Length > 0)
            {
                stringBuilder.Append("\n");
                for (int i = 0; i < args.Length; i++)
                {
                    stringBuilder.AppendFormat("\t -> {0}{1} [{2}] = \"{3}\"\n", new object[]
					{
						this._paramPrefix,
						i,
						args[i].GetType().Name,
						args[i]
					});
                }
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }
            return stringBuilder.ToString();
        }
        internal void ExecuteNonQueryHelper(IDbCommand cmd)
        {
            this.DoPreExecute(cmd);
            cmd.ExecuteNonQuery();
            this.OnExecutedCommand(cmd);
        }
        internal object ExecuteScalarHelper(IDbCommand cmd)
        {
            this.DoPreExecute(cmd);
            object result = cmd.ExecuteScalar();
            this.OnExecutedCommand(cmd);
            return result;
        }
        internal void DoPreExecute(IDbCommand cmd)
        {
            if (this.OneTimeCommandTimeout != 0)
            {
                cmd.CommandTimeout = this.OneTimeCommandTimeout;
                this.OneTimeCommandTimeout = 0;
            }
            else if (this.CommandTimeout != 0)
            {
                cmd.CommandTimeout = this.CommandTimeout;
            }
            this.OnExecutingCommand(cmd);
            this._lastSql = cmd.CommandText;
            this._lastArgs = (
                from IDataParameter parameter in cmd.Parameters
                select parameter.Value).ToArray<object>();
        }
    }
}
