namespace PetaPoco
{
    using System;

    [AttributeUsage(AttributeTargets.Property)]
    public class ResultColumnAttribute : ColumnAttribute
    {
        public ResultColumnAttribute()
        {
        }

        public ResultColumnAttribute(string name) : base(name)
        {
        }
    }
}

