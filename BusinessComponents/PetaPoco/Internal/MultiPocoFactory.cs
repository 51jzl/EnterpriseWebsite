namespace PetaPoco.Internal
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Linq.Expressions;
    using System.Reflection;
    using System.Reflection.Emit;

    internal class MultiPocoFactory
    {
        private List<Delegate> _delegates;
        private static Cache<ArrayKey<Type>, object> AutoMappers = new Cache<ArrayKey<Type>, object>();
        private static Cache<Tuple<Type, ArrayKey<Type>, string, string>, object> MultiPocoFactories = new Cache<Tuple<Type, ArrayKey<Type>, string, string>, object>();

        private static Func<IDataReader, object, TRet> CreateMultiPocoFactory<TRet>(Type[] types, string ConnectionString, string sql, IDataReader r)
        {
            DynamicMethod method = new DynamicMethod("petapoco_multipoco_factory", typeof(TRet), new Type[] { typeof(MultiPocoFactory), typeof(IDataReader), typeof(object) }, typeof(MultiPocoFactory));
            ILGenerator iLGenerator = method.GetILGenerator();
            iLGenerator.Emit(OpCodes.Ldarg_2);
            List<Delegate> list = new List<Delegate>();
            int pos = 0;
            for (int i = 0; i < types.Length; i++)
            {
                Delegate item = FindSplitPoint(types[i], ((i + 1) < types.Length) ? types[i + 1] : null, ConnectionString, sql, r, ref pos);
                list.Add(item);
                iLGenerator.Emit(OpCodes.Ldarg_0);
                iLGenerator.Emit(OpCodes.Ldc_I4, i);
                iLGenerator.Emit(OpCodes.Callvirt, typeof(MultiPocoFactory).GetMethod("GetItem"));
                iLGenerator.Emit(OpCodes.Ldarg_1);
                MethodInfo meth = item.GetType().GetMethod("Invoke");
                iLGenerator.Emit(OpCodes.Callvirt, meth);
            }
            iLGenerator.Emit(OpCodes.Callvirt, Expression.GetFuncType(types.Concat<Type>(new Type[] { typeof(TRet) }).ToArray<Type>()).GetMethod("Invoke"));
            iLGenerator.Emit(OpCodes.Ret);
            MultiPocoFactory target = new MultiPocoFactory {
                _delegates = list
            };
            return (Func<IDataReader, object, TRet>) method.CreateDelegate(typeof(Func<IDataReader, object, TRet>), target);
        }

        private static Delegate FindSplitPoint(Type typeThis, Type typeNext, string ConnectionString, string sql, IDataReader r, ref int pos)
        {
            if (typeNext == null)
            {
                return PocoData.ForType(typeThis).GetFactory(sql, ConnectionString, pos, r.FieldCount - pos, r);
            }
            PocoData data = PocoData.ForType(typeThis);
            PocoData data2 = PocoData.ForType(typeNext);
            int firstColumn = pos;
            Dictionary<string, bool> dictionary = new Dictionary<string, bool>();
            while (pos < r.FieldCount)
            {
                string name = r.GetName(pos);
                if (dictionary.ContainsKey(name) || (!data.Columns.ContainsKey(name) && data2.Columns.ContainsKey(name)))
                {
                    return data.GetFactory(sql, ConnectionString, firstColumn, pos - firstColumn, r);
                }
                dictionary.Add(name, true);
                pos++;
            }
            throw new InvalidOperationException(string.Format("Couldn't find split point between {0} and {1}", typeThis, typeNext));
        }

        internal static void FlushCaches()
        {
            MultiPocoFactories.Flush();
            AutoMappers.Flush();
        }

        public static object GetAutoMapper(Type[] types)
        {
            ArrayKey<Type> key = new ArrayKey<Type>(types);
            return AutoMappers.Get(key, delegate {
                DynamicMethod method = new DynamicMethod("petapoco_automapper", types[0], types, true);
                ILGenerator iLGenerator = method.GetILGenerator();
                Func<PropertyInfo, bool> predicate = null;
                for (int i = 1; i < types.Length; i++)
                {
                    bool flag = false;
                    for (int k = i - 1; k >= 0; k--)
                    {
                        if (predicate == null)
                        {
                            predicate = p => p.PropertyType == types[i];
                        }
                        IEnumerable<PropertyInfo> source = types[k].GetProperties().Where<PropertyInfo>(predicate);
                        if (source.Count<PropertyInfo>() != 0)
                        {
                            if (source.Count<PropertyInfo>() > 1)
                            {
                                throw new InvalidOperationException(string.Format("Can't auto join {0} as {1} has more than one property of type {0}", types[i], types[k]));
                            }
                            iLGenerator.Emit(OpCodes.Ldarg_S, k);
                            iLGenerator.Emit(OpCodes.Ldarg_S, i);
                            iLGenerator.Emit(OpCodes.Callvirt, source.First<PropertyInfo>().GetSetMethod(true));
                            flag = true;
                        }
                    }
                    if (!flag)
                    {
                        throw new InvalidOperationException(string.Format("Can't auto join {0}", types[i]));
                    }
                }
                iLGenerator.Emit(OpCodes.Ldarg_0);
                iLGenerator.Emit(OpCodes.Ret);
                return method.CreateDelegate(Expression.GetFuncType(types.Concat<Type>(types.Take<Type>(1)).ToArray<Type>()));
            });
        }

        public static Func<IDataReader, object, TRet> GetFactory<TRet>(Type[] types, string ConnectionString, string sql, IDataReader r)
        {
            Tuple<Type, ArrayKey<Type>, string, string> key = Tuple.Create<Type, ArrayKey<Type>, string, string>(typeof(TRet), new ArrayKey<Type>(types), ConnectionString, sql);
            return (Func<IDataReader, object, TRet>) MultiPocoFactories.Get(key, () => CreateMultiPocoFactory<TRet>(types, ConnectionString, sql, r));
        }

        public Delegate GetItem(int index)
        {
            return this._delegates[index];
        }
    }
}

