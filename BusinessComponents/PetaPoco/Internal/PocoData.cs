namespace PetaPoco.Internal
{
    using PetaPoco;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Dynamic;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Runtime.CompilerServices;

    internal class PocoData
    {
        private static List<Func<object, object>> _converters = new List<Func<object, object>>();
        private static Cache<Type, PocoData> _pocoDatas = new Cache<Type, PocoData>();
        private static FieldInfo fldConverters = typeof(PocoData).GetField("_converters", BindingFlags.GetField | BindingFlags.NonPublic | BindingFlags.Static);
        private static MethodInfo fnGetValue = typeof(IDataRecord).GetMethod("GetValue", new Type[] { typeof(int) });
        private static MethodInfo fnInvoke = typeof(Func<object, object>).GetMethod("Invoke");
        private static MethodInfo fnIsDBNull = typeof(IDataRecord).GetMethod("IsDBNull");
        private static MethodInfo fnListGetItem = typeof(List<Func<object, object>>).GetProperty("Item").GetGetMethod();
        private Cache<Tuple<string, string, int, int>, Delegate> PocoFactories;
        public Type type;

        public PocoData()
        {
            this.PocoFactories = new Cache<Tuple<string, string, int, int>, Delegate>();
        }

        public PocoData(Type t)
        {
            this.PocoFactories = new Cache<Tuple<string, string, int, int>, Delegate>();
            this.type = t;
            IMapper mapper = Mappers.GetMapper(t);
            this.TableInfo = mapper.GetTableInfo(t);
            this.Columns = new Dictionary<string, PocoColumn>(StringComparer.OrdinalIgnoreCase);
            foreach (PropertyInfo info in t.GetProperties())
            {
                if (info.CanWrite && info.CanRead)
                {
                    ColumnInfo columnInfo = mapper.GetColumnInfo(info);
                    if (columnInfo != null)
                    {
                        PocoColumn column = new PocoColumn {
                            PropertyInfo = info,
                            ColumnName = columnInfo.ColumnName,
                            ResultColumn = columnInfo.ResultColumn,
                            ForceToUtc = columnInfo.ForceToUtc
                        };
                        object[] customAttributes = info.GetCustomAttributes(typeof(SqlBehaviorAttribute), true);
                        if (customAttributes.Length > 0)
                        {
                            SqlBehaviorAttribute attribute = customAttributes[0] as SqlBehaviorAttribute;
                            if (attribute != null)
                            {
                                column.SqlBehavior = attribute.Behavior;
                            }
                        }
                        this.Columns.Add(column.ColumnName, column);
                    }
                }
            }
            this.QueryColumns = (from c in this.Columns
                where !c.Value.ResultColumn
                select c.Key).ToArray<string>();
        }

        private static void AddConverterToStack(ILGenerator il, Func<object, object> converter)
        {
            if (converter != null)
            {
                int count = _converters.Count;
                _converters.Add(converter);
                il.Emit(OpCodes.Ldsfld, fldConverters);
                il.Emit(OpCodes.Ldc_I4, count);
                il.Emit(OpCodes.Callvirt, fnListGetItem);
            }
        }

        internal static void FlushCaches()
        {
            _pocoDatas.Flush();
        }

        public static PocoData ForObject(object o, string primaryKeyName)
        {
            Type t = o.GetType();
            if (!(t == typeof(ExpandoObject)))
            {
                return ForType(t);
            }
            PocoData data = new PocoData {
                TableInfo = new PetaPoco.TableInfo(),
                Columns = new Dictionary<string, PocoColumn>(StringComparer.OrdinalIgnoreCase)
            };
            ExpandoColumn column2 = new ExpandoColumn {
                ColumnName = primaryKeyName
            };
            data.Columns.Add(primaryKeyName, column2);
            data.TableInfo.PrimaryKey = primaryKeyName;
            data.TableInfo.AutoIncrement = true;
            foreach (string str in (o as IDictionary<string, object>).Keys)
            {
                if (str != primaryKeyName)
                {
                    ExpandoColumn column = new ExpandoColumn {
                        ColumnName = str
                    };
                    data.Columns.Add(str, column);
                }
            }
            return data;
        }

        public static PocoData ForType(Type t)
        {
            if (t == typeof(ExpandoObject))
            {
                throw new InvalidOperationException("Can't use dynamic types with this method");
            }
            return _pocoDatas.Get(t, () => new PocoData(t));
        }

        private static Func<object, object> GetConverter(IMapper mapper, PocoColumn pc, Type srcType, Type dstType)
        {
            Func<object, object> func2 = null;
            Func<object, object> func3 = null;
            Func<object, object> func4 = null;
            Func<object, object> fromDbConverter = null;
            if (pc != null)
            {
                fromDbConverter = mapper.GetFromDbConverter(pc.PropertyInfo, srcType);
                if (fromDbConverter != null)
                {
                    return fromDbConverter;
                }
            }
            if ((((pc != null) && pc.ForceToUtc) && (srcType == typeof(DateTime))) && ((dstType == typeof(DateTime)) || (dstType == typeof(DateTime?))))
            {
                return src => new DateTime(((DateTime) src).Ticks, DateTimeKind.Utc);
            }
            if (dstType.IsEnum && IsIntegralType(srcType))
            {
                if (srcType != typeof(int))
                {
                    return src => Convert.ChangeType(src, typeof(int), null);
                }
            }
            else if (!dstType.IsAssignableFrom(srcType))
            {
                if (dstType.IsEnum && (srcType == typeof(string)))
                {
                    if (func2 == null)
                    {
                        func2 = src => EnumMapper.EnumFromString(dstType, (string) src);
                    }
                    return func2;
                }
                if (dstType.Equals(typeof(bool)))
                {
                    return delegate (object src) {
                        if (src.ToString() == "0")
                        {
                            return false;
                        }
                        return true;
                    };
                }
                if (dstType.Equals(typeof(Guid)))
                {
                    return src => new Guid(src.ToString());
                }
                if (dstType.IsGenericType && (dstType.GetGenericTypeDefinition() == typeof(Nullable<>)))
                {
                    if (func3 == null)
                    {
                        func3 = src => Convert.ChangeType(src, Nullable.GetUnderlyingType(dstType));
                    }
                    return func3;
                }
                if (func4 == null)
                {
                    func4 = src => Convert.ChangeType(src, dstType, null);
                }
                return func4;
            }
            return null;
        }

        public Delegate GetFactory(string sql, string connString, int firstColumn, int countColumns, IDataReader r)
        {
            Tuple<string, string, int, int> key = Tuple.Create<string, string, int, int>(sql, connString, firstColumn, countColumns);
            return this.PocoFactories.Get(key, delegate {
                DynamicMethod method = new DynamicMethod("petapoco_factory_" + this.PocoFactories.Count.ToString(), this.type, new Type[] { typeof(IDataReader) }, true);
                ILGenerator il = method.GetILGenerator();
                IMapper mapper = Mappers.GetMapper(this.type);
                if (this.type == typeof(object))
                {
                    il.Emit(OpCodes.Newobj, typeof(ExpandoObject).GetConstructor(Type.EmptyTypes));
                    MethodInfo meth = typeof(IDictionary<string, object>).GetMethod("Add");
                    for (int k = firstColumn; k < (firstColumn + countColumns); k++)
                    {
                        Type sourceType = r.GetFieldType(k);
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Ldstr, r.GetName(k));
                        Func<object, object> fromDbConverter = mapper.GetFromDbConverter(null, sourceType);
                        AddConverterToStack(il, fromDbConverter);
                        il.Emit(OpCodes.Ldarg_0);
                        il.Emit(OpCodes.Ldc_I4, k);
                        il.Emit(OpCodes.Callvirt, fnGetValue);
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Isinst, typeof(DBNull));
                        Label label = il.DefineLabel();
                        il.Emit(OpCodes.Brfalse_S, label);
                        il.Emit(OpCodes.Pop);
                        if (fromDbConverter != null)
                        {
                            il.Emit(OpCodes.Pop);
                        }
                        il.Emit(OpCodes.Ldnull);
                        if (fromDbConverter != null)
                        {
                            Label label2 = il.DefineLabel();
                            il.Emit(OpCodes.Br_S, label2);
                            il.MarkLabel(label);
                            il.Emit(OpCodes.Callvirt, fnInvoke);
                            il.MarkLabel(label2);
                        }
                        else
                        {
                            il.MarkLabel(label);
                        }
                        il.Emit(OpCodes.Callvirt, meth);
                    }
                }
                else if ((this.type.IsValueType || (this.type == typeof(string))) || (this.type == typeof(byte[])))
                {
                    Type fieldType = r.GetFieldType(0);
                    Func<object, object> converter = GetConverter(mapper, null, fieldType, this.type);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Callvirt, fnIsDBNull);
                    Label label3 = il.DefineLabel();
                    il.Emit(OpCodes.Brfalse_S, label3);
                    il.Emit(OpCodes.Ldnull);
                    Label label4 = il.DefineLabel();
                    il.Emit(OpCodes.Br_S, label4);
                    il.MarkLabel(label3);
                    AddConverterToStack(il, converter);
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldc_I4_0);
                    il.Emit(OpCodes.Callvirt, fnGetValue);
                    if (converter != null)
                    {
                        il.Emit(OpCodes.Callvirt, fnInvoke);
                    }
                    il.MarkLabel(label4);
                    il.Emit(OpCodes.Unbox_Any, this.type);
                }
                else
                {
                    il.Emit(OpCodes.Newobj, this.type.GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null));
                    for (int i = firstColumn; i < (firstColumn + countColumns); i++)
                    {
                        PocoColumn column;
                        if (this.Columns.TryGetValue(r.GetName(i), out column))
                        {
                            Type srcType = r.GetFieldType(i);
                            Type dstType = column.PropertyInfo.PropertyType;
                            il.Emit(OpCodes.Ldarg_0);
                            il.Emit(OpCodes.Ldc_I4, i);
                            il.Emit(OpCodes.Callvirt, fnIsDBNull);
                            Label label5 = il.DefineLabel();
                            il.Emit(OpCodes.Brtrue_S, label5);
                            il.Emit(OpCodes.Dup);
                            Func<object, object> func3 = GetConverter(mapper, column, srcType, dstType);
                            bool flag = false;
                            if (func3 == null)
                            {
                                MethodInfo info2 = typeof(IDataRecord).GetMethod("Get" + srcType.Name, new Type[] { typeof(int) });
                                if (((info2 != null) && (info2.ReturnType == srcType)) && ((info2.ReturnType == dstType) || (info2.ReturnType == Nullable.GetUnderlyingType(dstType))))
                                {
                                    il.Emit(OpCodes.Ldarg_0);
                                    il.Emit(OpCodes.Ldc_I4, i);
                                    il.Emit(OpCodes.Callvirt, info2);
                                    if (Nullable.GetUnderlyingType(dstType) != null)
                                    {
                                        il.Emit(OpCodes.Newobj, dstType.GetConstructor(new Type[] { Nullable.GetUnderlyingType(dstType) }));
                                    }
                                    il.Emit(OpCodes.Callvirt, column.PropertyInfo.GetSetMethod(true));
                                    flag = true;
                                }
                            }
                            if (!flag)
                            {
                                AddConverterToStack(il, func3);
                                il.Emit(OpCodes.Ldarg_0);
                                il.Emit(OpCodes.Ldc_I4, i);
                                il.Emit(OpCodes.Callvirt, fnGetValue);
                                if (func3 != null)
                                {
                                    il.Emit(OpCodes.Callvirt, fnInvoke);
                                }
                                il.Emit(OpCodes.Unbox_Any, column.PropertyInfo.PropertyType);
                                il.Emit(OpCodes.Callvirt, column.PropertyInfo.GetSetMethod(true));
                            }
                            il.MarkLabel(label5);
                        }
                    }
                    MethodInfo info3 = RecurseInheritedTypes<MethodInfo>(this.type, x => x.GetMethod("OnLoaded", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new Type[0], null));
                    if (info3 != null)
                    {
                        il.Emit(OpCodes.Dup);
                        il.Emit(OpCodes.Callvirt, info3);
                    }
                }
                il.Emit(OpCodes.Ret);
                return method.CreateDelegate(Expression.GetFuncType(new Type[] { typeof(IDataReader), this.type }));
            });
        }

        private static bool IsIntegralType(Type t)
        {
            TypeCode typeCode = Type.GetTypeCode(t);
            return ((typeCode >= TypeCode.SByte) && (typeCode <= TypeCode.UInt64));
        }

        private static T RecurseInheritedTypes<T>(Type t, Func<Type, T> cb)
        {
            while (t != null)
            {
                T local = cb(t);
                if (local != null)
                {
                    return local;
                }
                t = t.BaseType;
            }
            return default(T);
        }

        public Dictionary<string, PocoColumn> Columns { get; private set; }

        public string[] QueryColumns { get; private set; }

        public PetaPoco.TableInfo TableInfo { get; private set; }
    }
}

