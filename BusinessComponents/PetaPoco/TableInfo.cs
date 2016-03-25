namespace PetaPoco
{
    using System;
    using System.Runtime.CompilerServices;

    public class TableInfo
    {
        public static TableInfo FromPoco(Type t)
        {
            TableInfo info = new TableInfo();
            object[] customAttributes = t.GetCustomAttributes(typeof(TableNameAttribute), true);
            info.TableName = (customAttributes.Length == 0) ? t.Name : (customAttributes[0] as TableNameAttribute).Value;
            customAttributes = t.GetCustomAttributes(typeof(PrimaryKeyAttribute), true);
            info.PrimaryKey = (customAttributes.Length == 0) ? "ID" : (customAttributes[0] as PrimaryKeyAttribute).Value;
            info.SequenceName = (customAttributes.Length == 0) ? null : (customAttributes[0] as PrimaryKeyAttribute).sequenceName;
            info.AutoIncrement = (customAttributes.Length != 0) && (customAttributes[0] as PrimaryKeyAttribute).autoIncrement;
            return info;
        }

        public bool AutoIncrement { get; set; }

        public string PrimaryKey { get; set; }

        public string SequenceName { get; set; }

        public string TableName { get; set; }
    }
}

