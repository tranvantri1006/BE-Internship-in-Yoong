using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ShoeStoreAPI.Models;
using ShoeStoreAPI.Data;

var builder = WebApplication.CreateBuilder(args);

// SQLite
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=shoeshop.db"));

var app = builder.Build();

// Seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
    Seed.EnsureSeed(db);
}

// JWT Secret
var secretKey = "THIS_IS_MY_DEMO_SECRET_KEY_123456";

// DTOs

// LOGIN
app.MapPost("/login", async (LoginRequest req, AppDbContext db) =>
{
    var user = await db.Users.FirstOrDefaultAsync(u => u.Email == req.Email);
    if (user is null || !BCrypt.Net.BCrypt.Verify(req.Password, user.PasswordHash))
        return Results.Unauthorized();

    var claims = new[]
    {
        new Claim(ClaimTypes.Name, user.Email),
        new Claim("uid", user.Id.ToString()),
        new Claim(ClaimTypes.Role, user.Role)
    };

    var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
    var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

    var token = new JwtSecurityToken(
        issuer: "demo-app",
        audience: "demo-client",
        claims: claims,
        expires: DateTime.UtcNow.AddHours(1),
        signingCredentials: creds
    );

    var tokenString = new JwtSecurityTokenHandler().WriteToken(token);
    return Results.Ok(new { token = tokenString, email = user.Email, role = user.Role });
});

// Middleware validate JWT
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/login")) { await next(); return; }

    var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
    if (authHeader != null && authHeader.StartsWith("Bearer "))
    {
        var tokenStr = authHeader.Substring("Bearer ".Length).Trim();
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(secretKey);

            var principal = tokenHandler.ValidateToken(tokenStr, new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "demo-app",
                ValidateAudience = true,
                ValidAudience = "demo-client",
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            }, out _);

            context.User = principal;
            await next();
            return;
        }
        catch
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Invalid or expired token");
            return;
        }
    }

    context.Response.StatusCode = 401;
    await context.Response.WriteAsync("Missing Authorization header");
});

// API Products
app.MapGet("/products", async (AppDbContext db) => await db.Products.ToListAsync());

// API Create Order
app.MapPost("/orders", async (OrderRequest req, AppDbContext db, ClaimsPrincipal user) =>
{
    var email = user.Identity?.Name;
    var customer = await db.Users.FirstAsync(u => u.Email == email);

    var order = new Order { UserId = customer.Id, Items = new List<OrderItem>() };

    foreach (var item in req.Items)
    {
        var product = await db.Products.FindAsync(item.ProductId);
        if (product is null || product.Stock < item.Quantity)
            return Results.BadRequest($"Sản phẩm {item.ProductId} không đủ hàng");

        product.Stock -= item.Quantity;
        order.Items.Add(new OrderItem { ProductId = product.Id, Quantity = item.Quantity });
    }

    db.Orders.Add(order);
    await db.SaveChangesAsync();

    return Results.Ok(order);
});

// API My Orders
app.MapGet("/orders", async (AppDbContext db, ClaimsPrincipal user) =>
{
    var email = user.Identity?.Name;
    return await db.Orders
        .Include(o => o.Items).ThenInclude(i => i.Product)
        .Include(o => o.User)
        .Where(o => o.User.Email == email)
        .ToListAsync();
});
app.Run(); 
public record LoginRequest(string Email, string Password);
public record OrderRequest(List<(int ProductId, int Quantity)> Items);




