namespace PetaPoco.Internal
{
    using System;
    using System.Collections.Generic;

    internal static class EnumMapper
    {
        private static Cache<Type, Dictionary<string, object>> _types = new Cache<Type, Dictionary<string, object>>();

        public static object EnumFromString(Type enumType, string value)
        {
            return _types.Get(enumType, delegate {
                Array values = Enum.GetValues(enumType);
                Dictionary<string, object> dictionary = new Dictionary<string, object>(values.Length, StringComparer.InvariantCultureIgnoreCase);
                foreach (object obj2 in values)
                {
                    dictionary.Add(obj2.ToString(), obj2);
                }
                return dictionary;
            })[value];
        }
    }
}

