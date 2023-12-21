using LORM;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data.Common;

public class Users2
{
    [Key]
    public int Id { get; set; }

    public string FirstName { get; set; }

    [Nullable(false)]
    public string Surname { get; set; }

    [StringLength(50)]
    public string Email { get; set; }

    public string Phone_Number { get; set; }

    public int Role_Id { get; set; }

    public string Password { get; set; }

    [ColumnName("isReal")]
    public bool Realtable { get; set; }
}

public class TestContext : DBInstance
{
    public DbObject<Users2> Users { get; set; }

    public TestContext(string dbType = "mysql", string connStr = "Server=localhost;Port=8080;Database=Test;User Id=Toto;Password=1234;")
        : base(dbType, connStr)
    {
    }
}

public class Program
{
    static void Main(string[] args)
    {
        using (var dbCtx = new TestContext())
        {
            // Additional ORM functions:

            // Insert a new user
            var newUser = new Users2
            {
                FirstName = "JDF",
                Surname = "Doeuktgcfgbgbcgncgncgn",
                Email = "johndoe@example.com",
                Phone_Number = "1234567890",
                Role_Id = 1,
                Password = "1234",
                Realtable = true
            };
            dbCtx.Users.Insert(newUser);

            // Update an existing user
            var user = dbCtx.Users.GetElementById(171);

            if (user != null)
            {
                Console.WriteLine($"User: {user.FirstName} {user.Surname}, role_id: {user.Role_Id}");
                user.FirstName = "PNL";
                dbCtx.Users.Update(user);
            }
            else
            {
                Console.WriteLine("User not found.");
            }

            // Delete a user
            dbCtx.Users.Delete(dbCtx.Users.GetLatest());

            // Bulk insert
            var usrs = new List<Users2>
            {
                new Users2
                {
                    FirstName = "tst1",
                    Surname = "Test1",
                    Email = "test1@example.com",
                    Phone_Number = "1111111111",
                    Role_Id = 1,
                    Password = "1234"
                },
                new Users2
                {
                    FirstName = "tst2",
                    Surname = "Test2",
                    Email = "test2@example.com",
                    Phone_Number = "2222222222",
                    Role_Id = 1,
                    Password = "453"
                }
            };
            dbCtx.Users.BulkInsert(usrs);

            // Execute a custom query
            var query = "SELECT * FROM Users WHERE user_id = 1";
            var result = dbCtx.Query(query);

            // Fetch multiple users based on conditions
            var condition = new { Role_Id = 1 };
            var users = dbCtx.Users.Fetch(condition);

            foreach (var u in users)
            {
                Console.WriteLine($"ID: {u.Id} User: {u.FirstName} {u.Surname}, role_id: {u.Role_Id}");
            }
            Console.WriteLine("--------------------------------------------------");



            var qB = new QueryBuilder<Users2>(dbCtx.Users);
            users = qB.Where("Role_Id").Equals(1).AndNot("surname").Equals("Test1").Select();

            foreach (var u in users)
            {
                Console.WriteLine($"ID: {u.Id} User: {u.FirstName} {u.Surname}, role_id: {u.Role_Id}");
            }

            Console.WriteLine("--------------------------------------------------");

            // // Count the number of users
            var count = dbCtx.Users.Count();

            // Join operation example
            var joinQuery = $"SELECT * FROM Users INNER JOIN Orders ON Users.Id = Orders.UserId";
            var joinResult = dbCtx.Query(joinQuery);

            // Pagination example
            var pageNumber = 1;
            var pageSize = 10;
            var usersPage1 = dbCtx.Users.GetPage(pageNumber, pageSize);

            // Print users and count
            foreach (var u in users)
            {
                Console.WriteLine($"User: {u.FirstName} {u.Surname}, role_id: {u.Role_Id}");
            }
            Console.WriteLine($"Count: {count}");
        }
    }
}