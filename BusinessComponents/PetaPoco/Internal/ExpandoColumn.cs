namespace PetaPoco.Internal
{
    using System;
    using System.Collections.Generic;

    internal class ExpandoColumn : PocoColumn
    {
        public override object ChangeType(object val)
        {
            return val;
        }

        public override object GetValue(object target)
        {
            object obj2 = null;
            (target as IDictionary<string, object>).TryGetValue(base.ColumnName, out obj2);
            return obj2;
        }

        public override void SetValue(object target, object val)
        {
            (target as IDictionary<string, object>)[base.ColumnName] = val;
        }
    }
}

