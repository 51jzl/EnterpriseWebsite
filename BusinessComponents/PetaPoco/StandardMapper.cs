namespace PetaPoco
{
    using System;
    using System.Reflection;

    public class StandardMapper : IMapper
    {
        public ColumnInfo GetColumnInfo(PropertyInfo pocoProperty)
        {
            return ColumnInfo.FromProperty(pocoProperty);
        }

        public Func<object, object> GetFromDbConverter(PropertyInfo TargetProperty, Type SourceType)
        {
            return null;
        }

        public TableInfo GetTableInfo(Type pocoType)
        {
            return TableInfo.FromPoco(pocoType);
        }

        public Func<object, object> GetToDbConverter(PropertyInfo SourceProperty)
        {
            return null;
        }
    }
}

