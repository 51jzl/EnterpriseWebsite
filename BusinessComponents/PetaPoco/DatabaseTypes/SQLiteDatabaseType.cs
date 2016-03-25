namespace PetaPoco.DatabaseTypes
{
    using PetaPoco;
    using PetaPoco.Internal;
    using System;
    using System.Data;

    internal class SQLiteDatabaseType : DatabaseType
    {
        public override object ExecuteInsert(Database db, IDbCommand cmd, string PrimaryKeyName)
        {
            if (PrimaryKeyName != null)
            {
                cmd.CommandText = cmd.CommandText + ";\nSELECT last_insert_rowid();";
                return db.ExecuteScalarHelper(cmd);
            }
            db.ExecuteNonQueryHelper(cmd);
            return -1;
        }

        public override string GetExistsSql()
        {
            return "SELECT EXISTS (SELECT 1 FROM {0} WHERE {1})";
        }

        public override object MapParameterValue(object value)
        {
            if (value.GetType() == typeof(uint))
            {
                return (long) ((uint) value);
            }
            return base.MapParameterValue(value);
        }
    }
}

