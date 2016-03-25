namespace PetaPoco
{
    using System;
    using System.Runtime.CompilerServices;

    [AttributeUsage(AttributeTargets.Property)]
    public class ColumnAttribute : Attribute
    {
        public ColumnAttribute()
        {
            this.ForceToUtc = false;
        }

        public ColumnAttribute(string Name)
        {
            this.Name = Name;
            this.ForceToUtc = false;
        }

        public bool ForceToUtc { get; set; }

        public string Name { get; set; }
    }
}

