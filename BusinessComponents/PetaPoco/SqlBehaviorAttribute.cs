namespace PetaPoco
{
    using System;
    using System.Runtime.CompilerServices;

    [AttributeUsage(AttributeTargets.Property)]
    public class SqlBehaviorAttribute : Attribute
    {
        public SqlBehaviorAttribute(SqlBehaviorFlags behavior)
        {
            this.Behavior = behavior;
        }

        public SqlBehaviorFlags Behavior { get; private set; }
    }
}

