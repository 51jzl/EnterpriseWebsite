namespace PetaPoco
{
    using System;
    using System.Data.Common;
    using System.Reflection;

    public class OracleProvider : DbProviderFactory
    {
        private const string _assemblyName = "Oracle.DataAccess";
        private static Type _commandType = TypeFromAssembly("Oracle.DataAccess.Client.OracleCommand", "Oracle.DataAccess");
        private const string _commandTypeName = "Oracle.DataAccess.Client.OracleCommand";
        private static Type _connectionType = TypeFromAssembly("Oracle.DataAccess.Client.OracleConnection", "Oracle.DataAccess");
        private const string _connectionTypeName = "Oracle.DataAccess.Client.OracleConnection";
        public static OracleProvider Instance = new OracleProvider();

        public OracleProvider()
        {
            if (_connectionType == null)
            {
                throw new InvalidOperationException("Can't find Connection type: Oracle.DataAccess.Client.OracleConnection");
            }
        }

        public override DbCommand CreateCommand()
        {
            DbCommand command = (DbCommand) Activator.CreateInstance(_commandType);
            _commandType.GetProperty("BindByName").SetValue(command, true, null);
            return command;
        }

        public override DbConnection CreateConnection()
        {
            return (DbConnection) Activator.CreateInstance(_connectionType);
        }

        public static Type TypeFromAssembly(string typeName, string assemblyName)
        {
            try
            {
                Type type = Type.GetType(typeName);
                if (type == null)
                {
                    if (assemblyName == null)
                    {
                        throw new TypeLoadException("Could not load type " + typeName + ". Possible cause: no assembly name specified.");
                    }
                    Assembly assembly = Assembly.Load(assemblyName);
                    if (assembly == null)
                    {
                        throw new InvalidOperationException("Can't find assembly: " + assemblyName);
                    }
                    type = assembly.GetType(typeName);
                    if (type == null)
                    {
                        return null;
                    }
                }
                return type;
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

