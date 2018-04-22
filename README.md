# Qapper - .net Data Mapping tools for Q
[![GitHub license](https://img.shields.io/badge/license-LGPL%20v3-blue.svg)](https://github.com/machonky/qapper/blob/master/LICENSE)
[![Nuget](https://img.shields.io/nuget/v/Qapper.svg)](https://www.nuget.org/packages/qapper)
[![NuGet](https://img.shields.io/nuget/dt/Qapper.svg)](https://www.nuget.org/packages/qapper)
[![GitHub issues](https://img.shields.io/github/issues/machonky/Qapper.svg)](https://github.com/machonky/Qapper/issues)
[![GitHub forks](https://img.shields.io/github/forks/machonky/Qapper.svg?style=social&label=Fork)](https://github.com/machonky/Qapper/network)
[![GitHub stars](https://img.shields.io/github/stars/machonky/Qapper.svg?style=social&label=Star)](https://github.com/machonky/Qapper/stargazers)

Qapper (pronounced 'kwapper') is a super simple object mapper for kdb+\q modelled after [Dapper](https://github.com/StackExchange/Dapper)

> ### DISCLAIMER
> **IMPORTANT:** The current state of this toolkit is **PRE-ALPHA/Development**. Please consider it version a foundational version. Many areas could be improved and change significantly while refactoring current code and implementing new features. 

## Example

Given a plain C# class decorated with [QSchema](https://github.com/dotnetq/QSchema) attributes... 

```cs
namespace Auth
{
    public class User
    {
        [Key]
        public string Id { get; set; }
        [Unique]
        public string Login { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        // We'll store the various roles when we deserialize
        [Ignore] public string[] PrincipalIds { get; set; }
    }
}    
```

... and an active corresponding kdb+ database built with ```QSchema.SchemaBuilder```, we can create an instance of the ```User``` class ```userInstance``` and store it in the database with:

```cs
    var entities = new Auth.User[]{userInstance};
    var qTable = QMapper.ConvertToQTable(entities);
    var tableName = SchemaBuilder.GetQTableName(typeof(Auth.User));
    connection.Sync("upsert", tableName, qTable);
```

Where the ```connection``` instance above is an active ```QConnection``` to a kdb+ database.

We can retrieve a collection of ```User``` objects from the database with a single line instruction:

```cs
IEnumerable<Auth.User> users = connection.QueryObjects<Auth.User>("select from .auth.user");
```

Qapper will automatically convert the returned 'q' table into an enumerable collection of object instances. The object properties of each object instance have been mapped to the columns of the table, subject to a simple naming convention, so each object instance will correspond to a row from the table. This will work with both keyed and unkeyed tables.

It really is that simple!

Take a look at the worked [Example](https://github.com/dotnetq/Example) for more detail.
