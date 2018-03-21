using qSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Qapper
{
    public static class Qapper
    {
        public static IEnumerable<T> QueryObjects<T>(this QConnection connection, string query)
        {
            return FromQTable<T>(connection.Sync(query));
        }

        public static T[] FromQTable<T>(object o)
        {
            var properties = _propertyMap[typeof(T)];

            if (o is QTable table)
            {
                return FromQTable<T>(properties, table);
            }

            if (o is QKeyedTable keyedTable)
            {
                return FromQKeyedTable<T>(properties, keyedTable);
            }

            return null;
        }

        private class PropertyMap : ConcurrentObjectFactoryCache<Type, Dictionary<string, PropertyInfo>>
        {
            public PropertyMap(Func<Type, Dictionary<string, PropertyInfo>> mappingFactory) : base(mappingFactory)
            { }
        }

        private static readonly Func<Type, Dictionary<string, PropertyInfo>> MappingFactory =
            t => t.GetProperties(BindingFlags.Instance | BindingFlags.Public).ToDictionary(p => LeadingLowercase(p.Name));

        private static string LeadingLowercase(string value) => string.Concat(value[0].ToString().ToLower(), value.Substring(1));

        private static readonly PropertyMap _propertyMap = new PropertyMap(MappingFactory);

        private static T[] FromQKeyedTable<T>(Dictionary<string, PropertyInfo> properties, QKeyedTable keyedTable)
        {
            var result = new T[keyedTable.RowsCount];
            for (int row = 0; row < result.Length; ++row)
            {
                result[row] = Activator.CreateInstance<T>();
                AssignToPropertyFromQColumn(properties, keyedTable.Keys, result, row);
                AssignToPropertyFromQColumn(properties, keyedTable.Values, result, row);
            }
            return result;
        }

        private static T[] FromQTable<T>(Dictionary<string, PropertyInfo> properties, QTable table)
        {
            var result = new T[table.RowsCount];
            for (int row = 0; row < result.Length; ++row)
            {
                result[row] = Activator.CreateInstance<T>();
                AssignToPropertyFromQColumn(properties, table, result, row);
            }

            return result;
        }

        private static void AssignToPropertyFromQColumn<T>(Dictionary<string, PropertyInfo> properties, QTable table, T[] result, int row)
        {
            for (int col = 0; col < table.Data.Length; ++col)
            {
                Array x = (Array)table.Data.GetValue(col);
                properties[table.Columns[col]].SetValue(result[row], x.GetValue(row));
            }
        }
    }
}

