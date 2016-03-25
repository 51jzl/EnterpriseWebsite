namespace PetaPoco
{
    using System;
    using System.Reflection;

    public interface IMapper
    {
        ColumnInfo GetColumnInfo(PropertyInfo pocoProperty);
        Func<object, object> GetFromDbConverter(PropertyInfo TargetProperty, Type SourceType);
        TableInfo GetTableInfo(Type pocoType);
        Func<object, object> GetToDbConverter(PropertyInfo SourceProperty);
    }
}

