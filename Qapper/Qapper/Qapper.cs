using qSharp;
using QTools.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Qapper
{
    public static class QMapper
    {
        public static IEnumerable<T> QueryObjects<T>(this QConnection connection, string query)
        {
            return FromQTable<T>(connection.Sync(query));
        }

        public static T[] FromQTable<T>(object o)
        {
            var properties = MappedTypes[typeof(T)];

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

        private static readonly PropertyMap MappedTypes = new PropertyMap(MappingFactory);

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

        private static bool HasAttribute<T>(this MemberInfo info) where T:Attribute
        {
            return info.GetCustomAttribute<T>() != null;
        }

        public static IQTable ConvertToQTable<T>(IEnumerable<T> dataModels)
        {
            var propertiesDict = MappedTypes[typeof(T)];

            var keyedProperties = new Dictionary<string, PropertyInfo>();
            var unkeyedProperties = new Dictionary<string, PropertyInfo>();

            foreach (var prop in propertiesDict)
            {
                if (prop.Value.HasAttribute<KeyAttribute>())
                {
                    keyedProperties.Add(prop.Key, prop.Value);
                }
                else
                {
                    unkeyedProperties.Add(prop.Key, prop.Value);
                }
            }

            var keyedTable = keyedProperties.Count == 0 ? null : ConvertToQTable(dataModels, keyedProperties);
            var unkeyedTable = ConvertToQTable(dataModels, unkeyedProperties);

            if (keyedTable != null)
            {
                return new QKeyedTable(keyedTable, unkeyedTable);
            }

            return unkeyedTable;
        }

        private static QTable ConvertToQTable<T>(IEnumerable<T> dataModels, Dictionary<string, PropertyInfo> propertiesDict)
        {
            var rowCount = dataModels.Count();

            var columns = propertiesDict.Keys.ToArray();
            var propertiesArray = propertiesDict.Values.ToArray();

            var data = new object[propertiesDict.Count];
            for (int i = 0; i < data.Length; ++i)
            {
                data[i] = Array.CreateInstance(ToQType(propertiesArray[i].PropertyType), rowCount);
            }

            int rowIndex = 0;
            var result = new QTable(columns, data);
            foreach (var dataModel in dataModels)
            {
                for (int propIndex = 0; propIndex < propertiesArray.Length; propIndex++)
                {
                    var propInfo = propertiesArray[propIndex];
                    var dataModelValue = propInfo.GetValue(dataModel);
                    ((Array)data.GetValue(propIndex)).SetValue(ToQObject(dataModelValue, propInfo), rowIndex);
                }

                rowIndex++;
            }
            return result;
        }

        private static Type ToQType(Type propertyType)
        {
            return propertyType;
        }

        private static object ToQObject(object dataModelValue, PropertyInfo propInfo)
        {
            return dataModelValue;
        }
    }
}

