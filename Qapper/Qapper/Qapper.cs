using qSharp;
using DotnetQ.QSchema.Attributes;
using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using FastMember;

namespace DotnetQ.Qapper
{
    public static class QMapper
    {
        public static IEnumerable<T> QueryObjects<T>(this QConnection connection, string query)
        {
            return FromQTable<T>(connection.Sync(query));
        }

        public static T[] FromQTable<T>(object o)
        {
            var cxt = MappedTypes[typeof(T)];

            if (o is QTable table)
            {
                return FromQTable<T>(cxt, table);
            }

            if (o is QKeyedTable keyedTable)
            {
                return FromQKeyedTable<T>(cxt, keyedTable);
            }

            return null;
        }

        private class MappingContext
        {
            public TypeAccessor TypeAccessor { get; set; }
            public Dictionary<string, PropertyInfo> ColumnsToProperties { get; set; }
        }

        private class PropertyMap : ConcurrentObjectFactoryCache<Type, MappingContext>
        {
            public PropertyMap(Func<Type, MappingContext> mappingFactory) : base(mappingFactory)
            { }
        }

        private static readonly Func<Type, MappingContext> MappingFactory =
            t => new MappingContext
            {
                TypeAccessor = TypeAccessor.Create(t),
                ColumnsToProperties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public)
                        .Where(p => !p.HasAttribute<IgnoreAttribute>())
                        .ToDictionary(p => ColumnName(p))
            };

        private static string LeadingLowercase(string value) => string.Concat(value[0].ToString().ToLower(), value.Substring(1));

        private static string ColumnName(MemberInfo info)
        {
            var overrideColName = info.GetCustomAttribute<ColNameAttribute>();
            if (overrideColName != null)
            {
                return overrideColName.Name;
            }
            return LeadingLowercase(info.Name);
        }

        private static readonly PropertyMap MappedTypes = new PropertyMap(MappingFactory);

        private static T[] FromQKeyedTable<T>(MappingContext cxt, QKeyedTable keyedTable)
        {
            var result = new T[keyedTable.RowsCount];
            for (int row = 0; row < result.Length; ++row)
            {
                result[row] = Activator.CreateInstance<T>();
                AssignToPropertyFromQColumn(cxt, keyedTable.Keys, result, row);
                AssignToPropertyFromQColumn(cxt, keyedTable.Values, result, row);
            }
            return result;
        }

        private static T[] FromQTable<T>(MappingContext cxt, QTable table)
        {
            var result = new T[table.RowsCount];
            for (int row = 0; row < result.Length; ++row)
            {
                result[row] = Activator.CreateInstance<T>();
                AssignToPropertyFromQColumn(cxt, table, result, row);
            }

            return result;
        }

        private static void AssignToPropertyFromQColumn<T>(MappingContext cxt, QTable table, T[] result, int row)
        {
            for (int col = 0; col < table.Data.Length; ++col)
            {
                Array x = (Array)table.Data.GetValue(col);
                var propName = cxt.ColumnsToProperties[table.Columns[col]];
                cxt.TypeAccessor[result[row], propName.Name] = x.GetValue(row);
            }
        }

        private static bool HasAttribute<T>(this MemberInfo info) where T:Attribute
        {
            return info.GetCustomAttribute<T>() != null;
        }

        public static IQTable ConvertToQTable<T>(IEnumerable<T> dataModels)
        {
            var cxt = MappedTypes[typeof(T)];

            var keyedProperties = new Dictionary<string, PropertyInfo>();
            var unkeyedProperties = new Dictionary<string, PropertyInfo>();

            foreach (var prop in cxt.ColumnsToProperties)
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

            var keyedTable = keyedProperties.Count == 0 ? null : ConvertToQTable(dataModels, keyedProperties, cxt);
            var unkeyedTable = ConvertToQTable(dataModels, unkeyedProperties, cxt);

            if (keyedTable != null)
            {
                return new QKeyedTable(keyedTable, unkeyedTable);
            }

            return unkeyedTable;
        }

        private static QTable ConvertToQTable<T>(IEnumerable<T> dataModels, Dictionary<string, PropertyInfo> propertiesDict, MappingContext cxt)
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
                    var dataModelValue = cxt.TypeAccessor[dataModel, propInfo.Name];
                    ((Array)data.GetValue(propIndex)).SetValue(ToQObject(dataModelValue, propInfo), rowIndex);
                }

                rowIndex++;
            }
            return result;
        }

        private static Type ToQType(Type propertyType)
        {
            // TODO: Extend the implementation to incorporate types from QSharp
            return propertyType;
        }

        private static object ToQObject(object dataModelValue, PropertyInfo propInfo)
        {
            // TODO: Extend the implementation to incorporate types from QSharp
            return dataModelValue;
        }
    }
}

