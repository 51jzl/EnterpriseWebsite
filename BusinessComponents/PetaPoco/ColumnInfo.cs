namespace PetaPoco
{
    using System;
    using System.Reflection;
    using System.Runtime.CompilerServices;

    public class ColumnInfo
    {
        public static ColumnInfo FromProperty(PropertyInfo pi)
        {
            bool flag = pi.DeclaringType.GetCustomAttributes(typeof(ExplicitColumnsAttribute), true).Length > 0;
            object[] customAttributes = pi.GetCustomAttributes(typeof(ColumnAttribute), true);
            if (flag)
            {
                if (customAttributes.Length == 0)
                {
                    return null;
                }
            }
            else if (pi.GetCustomAttributes(typeof(IgnoreAttribute), true).Length != 0)
            {
                return null;
            }
            ColumnInfo info = new ColumnInfo();
            if (customAttributes.Length > 0)
            {
                ColumnAttribute attribute = (ColumnAttribute) customAttributes[0];
                info.ColumnName = (attribute.Name == null) ? pi.Name : attribute.Name;
                info.ForceToUtc = attribute.ForceToUtc;
                if (attribute is ResultColumnAttribute)
                {
                    info.ResultColumn = true;
                }
                return info;
            }
            info.ColumnName = pi.Name;
            info.ForceToUtc = false;
            info.ResultColumn = false;
            return info;
        }

        public string ColumnName { get; set; }

        public bool ForceToUtc { get; set; }

        public bool ResultColumn { get; set; }
    }
}

