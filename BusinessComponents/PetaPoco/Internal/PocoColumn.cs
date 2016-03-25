namespace PetaPoco.Internal
{
    using PetaPoco;
    using System;
    using System.Reflection;

    internal class PocoColumn
    {
        public string ColumnName;
        public bool ForceToUtc = true;
        public System.Reflection.PropertyInfo PropertyInfo;
        public bool ResultColumn;
        private SqlBehaviorFlags sqlBehavior = SqlBehaviorFlags.All;

        public virtual object ChangeType(object val)
        {
            return Convert.ChangeType(val, this.PropertyInfo.PropertyType);
        }

        public virtual object GetValue(object target)
        {
            return this.PropertyInfo.GetValue(target, null);
        }

        public virtual void SetValue(object target, object val)
        {
            this.PropertyInfo.SetValue(target, val, null);
        }

        public SqlBehaviorFlags SqlBehavior
        {
            get
            {
                return this.sqlBehavior;
            }
            set
            {
                this.sqlBehavior = value;
            }
        }
    }
}

