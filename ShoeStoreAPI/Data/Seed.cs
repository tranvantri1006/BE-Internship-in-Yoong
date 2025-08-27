using ShoeStoreAPI.Data;
using ShoeStoreAPI.Models;



    public static class Seed
    {
        public static void EnsureSeed(AppDbContext db)
        {
            if (!db.Users.Any())
            {
                db.Users.Add(new User
                {
                    Email = "admin@shop.local",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                    Role = "admin"
                });
                db.Users.Add(new User
                {
                    Email = "user@shop.local",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("User@123"),
                    Role = "customer"
                });
                db.SaveChanges();
            }

            if (!db.Products.Any())
            {
                db.Products.AddRange(
                    new Product { Name = "Nike Air Zoom", Price = 120, Stock = 10 },
                    new Product { Name = "Adidas Ultraboost", Price = 150, Stock = 8 }
                );
                db.SaveChanges();

            }
        }
    }



