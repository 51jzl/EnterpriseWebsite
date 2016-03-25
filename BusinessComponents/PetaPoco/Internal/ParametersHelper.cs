namespace PetaPoco.Internal
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    internal static class ParametersHelper
    {
        private static Regex rxParams = new Regex(@"(?<!@)@\w+", RegexOptions.Compiled);

        public static string ProcessParams(string sql, object[] args_src, List<object> args_dest)
        {
            return rxParams.Replace(sql, delegate (Match m) {
                object obj2;
                int num;
                string s = m.Value.Substring(1);
                if (int.TryParse(s, out num))
                {
                    if ((num < 0) || (num >= args_src.Length))
                    {
                        throw new ArgumentOutOfRangeException(string.Format("Parameter '@{0}' specified but only {1} parameters supplied (in `{2}`)", num, args_src.Length, sql));
                    }
                    obj2 = args_src[num];
                }
                else
                {
                    bool flag = false;
                    obj2 = null;
                    foreach (object obj3 in args_src)
                    {
                        PropertyInfo property = obj3.GetType().GetProperty(s);
                        if (property != null)
                        {
                            obj2 = property.GetValue(obj3, null);
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                    {
                        throw new ArgumentException(string.Format("Parameter '@{0}' specified but none of the passed arguments have a property with this name (in '{1}')", s, sql));
                    }
                }
                if (((obj2 is IEnumerable) && !(obj2 is string)) && !(obj2 is byte[]))
                {
                    StringBuilder builder = new StringBuilder();
                    foreach (object obj4 in obj2 as IEnumerable)
                    {
                        builder.Append(((builder.Length == 0) ? "@" : ",@") + args_dest.Count.ToString());
                        args_dest.Add(obj4);
                    }
                    return builder.ToString();
                }
                args_dest.Add(obj2);
                int num4 = args_dest.Count - 1;
                return "@" + num4.ToString();
            });
        }
    }
}

