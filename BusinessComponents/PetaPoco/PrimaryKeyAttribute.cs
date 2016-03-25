namespace PetaPoco
{
    using System;
    using System.Runtime.CompilerServices;

    [AttributeUsage(AttributeTargets.Class)]
    public class PrimaryKeyAttribute : Attribute
    {
        public PrimaryKeyAttribute(string primaryKey)
        {
            this.Value = primaryKey;
            this.autoIncrement = true;
        }

        public bool autoIncrement { get; set; }

        public string sequenceName { get; set; }

        public string Value { get; private set; }
    }
}

