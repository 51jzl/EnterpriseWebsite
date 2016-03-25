namespace PetaPoco.DatabaseTypes
{
    using PetaPoco;
    using PetaPoco.Internal;
    using System;
    using System.Data;

    internal class PostgreSQLDatabaseType : DatabaseType
    {
        public override string EscapeSqlIdentifier(string str)
        {
            return string.Format("\"{0}\"", str);
        }

        public override object ExecuteInsert(Database db, IDbCommand cmd, string PrimaryKeyName)
        {
            if (PrimaryKeyName != null)
            {
                cmd.CommandText = cmd.CommandText + string.Format("returning {0} as NewID", this.EscapeSqlIdentifier(PrimaryKeyName));
                return db.ExecuteScalarHelper(cmd);
            }
            db.ExecuteNonQueryHelper(cmd);
            return -1;
        }

        public override object MapParameterValue(object value)
        {
            if (value.GetType() == typeof(bool))
            {
                return value;
            }
            return base.MapParameterValue(value);
        }
    }
}

