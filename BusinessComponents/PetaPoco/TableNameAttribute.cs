namespace PetaPoco
{
    using System;
    using System.Runtime.CompilerServices;

    [AttributeUsage(AttributeTargets.Class)]
    public class TableNameAttribute : Attribute
    {
        public TableNameAttribute(string tableName)
        {
            this.Value = tableName;
        }

        public string Value { get; private set; }
    }
}

