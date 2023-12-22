# LORM - Legendary Object-Relational Mapping

LORM (Legendary Object-Relational Mapping) is a simple ORM (Object-Relational Mapping) library for C#. It provides a convenient way to interact with a database using object-oriented programming principles. This README file explains how to initialize and use LORM in your C# projects.

## Table of Contents
- [LORM - Legendary Object-Relational Mapping](#lorm---legendary-object-relational-mapping)
  - [Table of Contents](#table-of-contents)
  - [Installation](#installation)
  - [Usage](#usage)
    - [Initializing the Database Connection](#initializing-the-database-connection)

## Installation

To use LORM in your C# project, you can either download the source code and build it yourself, or you can add it as a NuGet package. To install LORM via NuGet, follow these steps:

1. Open the NuGet Package Manager in Visual Studio Community

2. Add as source in the options the path to the LORM folder (unzip release and point to it)

3. Select the LORM package and install the latest version

## Usage

[Example_File](https://github.com/Louis-de-Lavenne-de-Choulot/LORM/blob/main/UsageExample.cs)

### Initializing the Database Connection

To start using LORM, you need to initialize the database connection. The `DBInstance` class is responsible for managing the database connection. Here's an example of how to initialize the `DBInstance`:

```csharp
using LORM;

public class TestContext : DBInstance
{
    public TestContext(string dbType = "mysql", string connStr = "Server=localhost;Port=8080;Database=Test;User Id=Username;Password=1234;")
        : base(dbType, connStr)
    {
    }
}
```
In the example above, we create a derived class TestContext from DBInstance and pass the database type and connection string to the base constructor. You can modify the values of the dbType and connStr parameters according to your specific database configuration.

the possible databases are:
- mysql
- postgres
- oracle
- sqlserver
- sqlite


Defining Database Objects
LORM provides the DbObject<T> class, which represents a table or object in the database. You can create your database objects by defining classes that inherit from DbObject<T>. Here's an example of a Users2 class representing a table in the database:

```csharp
using LORM;
using System.ComponentModel.DataAnnotations;

public class Users2 : DbObject<Users2>
{
    [Key]
    public int Id { get; set; }
    public string FirstName { get; set; }
    public int Role_Id { get; set; }
    public string Password { get; set; }
}
```

In the example above, the Users2 class inherits from DbObject<Users2>, where Users2 is the type of the database object. You can define properties that represent the columns in the table, and decorate the primary key property with the [Key] attribute.

Performing CRUD Operations
Once you have defined your database objects, you can perform CRUD (Create, Read, Update, Delete) operations using the DbObject<T> methods. Here are some examples:

```csharp
using LORM;

// ...

using (var dbCtx = new TestContext())
{
    // Retrieve a specific user by ID
    var user = dbCtx.Users.GetElementById(32);

    // Update an existing user
    user.FirstName = "PNL";
    dbCtx.Users.Update(user);

    // Insert a new user
    var newUser = new Users2 { FirstName = "JDF", Role_Id = 1, Password = "1234" };
    dbCtx.Users.Insert(newUser);

    // Delete a user
    dbCtx.Users.Delete(dbCtx.Users.GetLatest());
}
```

The example above demonstrates how to retrieve a user by ID, update an existing user, insert a new user, and delete a user using the DbObject<T> methods.

Executing Custom Queries
LORM allows you to execute custom SQL queries using the Query method provided by the DBInstance class. Here's an example:*

```csharp
using LORM;

// ...

using (var dbCtx = new TestContext())
{
    var query = "SELECT * FROM Users WHERE user_id = 1";
    var result = dbCtx.Query(query);
}
```

In the example above, we execute a custom SQL query and store the result in the result variable.

Pro No SQL Queries
LORM supports pro no SQL queries using the QueryBuilder method provided by LORM. Here's an example:

```csharp
var qB = new QueryBuilder<Users2>(dbCtx.Users);
users = qB.Where("Id").GreaterThan(100).AndNot("surname").Equals("Test1").Select();

foreach (var u in users)
{
    Console.WriteLine($"ID: {u.Id} User: {u.FirstName} {u.Surname}, role_id: {u.Role_Id}");
}
```


Joining Tables
LORM supports joining tables using custom SQL queries. Here's an example:

```csharp
using LORM;

// ...

using (var dbCtx = new TestContext())
{
    var joinQuery = "SELECT * FROM Users INNER JOIN Orders ON Users.Id= Orders.UserId";
    var joinResult = dbCtx.Query(joinQuery);
}
```

In the example above, we join the Users and Orders tables using a custom SQL query and store the result in the joinResult variable.

Pagination
LORM provides pagination support for retrieving data in chunks. Here's an example:

```csharp
using LORM;

// ...

using (var dbCtx = new TestContext())
{
    var pageNumber = 1;
    var pageSize = 10;
    var usersPage1 = dbCtx.Users.GetPage(pageNumber, pageSize);
}
```

In the example above, we retrieve the first page of users, where each page contains 10 users.

License
LORM is licensed under the apache License 2.0. See the LICENSE file for more details.
