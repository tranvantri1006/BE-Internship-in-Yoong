using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore; 
using ShoeStoreAPI.Models;
using ShoeStoreAPI.Data; 
using Microsoft.AspNetCore.Authorization; 
using Microsoft.AspNetCore.Mvc;
using static Microsoft.Extensions.Logging.EventSource.LoggingEventSource;
using Microsoft.AspNetCore.Authentication;

var builder = WebApplication.CreateBuilder(args);

// SQLite
builder.Services.AddDbContext<AppDbContext>(opt =>
    opt.UseSqlite("Data Source=shoeshop.db"));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(
    c =>
    {
        c.SwaggerDoc("v1", new() { Title = "Shoe Store API", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
        {
            In = Microsoft.OpenApi.Models.ParameterLocation.Header,
            Description = "Please enter token",
            Name = "Authorization",
            Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
            BearerFormat = "JWT",
            Scheme = "bearer"
        });
    }
);
var secretKey = "THIS_IS_MY_DEMO_SECRET_KEY_123456";

// 🔑 Custom Authentication ngay trong Program.cs
builder.Services.AddAuthentication("CustomJwt")
    .AddScheme<AuthenticationSchemeOptions, CustomJwtHandler>("CustomJwt", null);


builder.Services.AddAuthorization(); 

var app = builder.Build();
//if (app.Environment.IsDevelopment())
//{
//app.UseSwagger();
//app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Shoe Store API V1"));
//}   
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Shoe Store API V1"));

app.UseHttpsRedirection();
// ✅ Middleware order: Authentication trước, Authorization sau
app.UseAuthentication();
app.UseAuthorization();
// Endpoint test
app.MapGet("/ping", () => "pong").AllowAnonymous();


// Seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    Seed.EnsureSeed(db);
}



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
//app.MapGet("/products", async (AppDbContext db) => await db.Products.ToListAsync());


app.MapGet("/products", async (AppDbContext db, ClaimsPrincipal user) =>
{
    if (user.Identity?.IsAuthenticated != true)
        return Results.Unauthorized();

    try
    {
        var products = await db.Products.ToListAsync();
        return Results.Ok(products);
    }
    catch (Exception ex)
    {
        return Results.Problem($"An error occurred while fetching products: {ex.Message}");
    }
});


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
//Hàm Đăng ký User  
app.MapPost("/register", async (UserRegisterRequest req, AppDbContext db) =>
{
    // Check nếu email đã tồn tại
    if (await db.Users.AnyAsync(u => u.Email == req.Email))
    {
        return Results.BadRequest(new { message = "Email đã tồn tại" });
    }

    var user = new User
    {
        Email = req.Email,
        PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
        Role = "customer"
    };

    db.Users.Add(user);
    await db.SaveChangesAsync();

    return Results.Ok(new { message = "Đăng ký thành công", user.Email });
});
//Hàm Thêm sản phẩm (Create)
app.MapPost("/products", async (Product req, AppDbContext db) =>
{
    db.Products.Add(req);
    await db.SaveChangesAsync();
    return Results.Created($"/products/{req.Id}", req);
});
//Lấy danh sách sản phẩm (Read All)
//app.MapGet("/products", async (AppDbContext db) =>
//{
//    return await db.Products.ToListAsync();
//});
//Lấy 1 sản phẩm theo Id (Read by Id)
app.MapGet("/products/{id}", async (int id, AppDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
return product is not null ? Results.Ok(product) : Results.NotFound();
});
//Cập nhật sản phẩm (Update)
app.MapPut("/products/{id}", async (int id, Product req, AppDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
if (product is null) return Results.NotFound();

product.Name = req.Name;
product.Description = req.Description;
product.Price = req.Price;
product.Stock = req.Stock;

await db.SaveChangesAsync();
return Results.Ok(product);
});
//Xóa sản phẩm (Delete)
app.MapDelete("/products/{id}", async (int id, AppDbContext db) =>
{
    var product = await db.Products.FindAsync(id);
    if (product is null) return Results.NotFound();

    db.Products.Remove(product);
    await db.SaveChangesAsync();
    return Results.Ok(new { message = "Đã xóa sản phẩm" });
});
//Tìm kiếm sản phẩm theo tên (Search)
app.MapGet("/products/search/{keyword}", async (string keyword, AppDbContext db) =>
{
    var products = await db.Products
        .Where(p => p.Name.Contains(keyword) || p.Description.Contains(keyword))
        .ToListAsync();

return Results.Ok(products);
});



app.Run();  




