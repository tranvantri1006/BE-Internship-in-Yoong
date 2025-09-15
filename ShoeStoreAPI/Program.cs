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
using System.Linq;
using Microsoft.AspNetCore.Http;
using System.Text.Json;
using CsvHelper;
using System.Globalization;
using Google.Apis.Auth;
using Microsoft.Extensions.FileProviders;


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
app.UseStaticFiles(); // Cho phép truy cập wwwroot
// Endpoint test
app.MapGet("/ping", () => "pong").AllowAnonymous();


// Seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
    Seed.EnsureSeed(db);
}

var users = new List<User>
{
    new User { Id = 1, Email = "john@example.com", PasswordHash = "123456", Role = "User" },
    new User { Id = 2, Email = "admin@example.com", PasswordHash = "abcdef", Role = "Admin" }
};


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
// Danh sách token bị revoke (blacklist)
List<string> revokedTokens = new List<string>();

// API Logout
app.MapPost("/api/logout", (HttpRequest request) =>
{
    var authHeader = request.Headers["Authorization"].ToString();
    if (string.IsNullOrEmpty(authHeader) || !authHeader.StartsWith("Bearer "))
    {
        return Results.Unauthorized();
    }

    var token = authHeader.Substring("Bearer ".Length).Trim();

    // Lưu token vào danh sách bị revoke
    revokedTokens.Add(token);

    return Results.Ok(new { message = "Đăng xuất thành công" });
});
app.MapPost("/api/upload", async (IFormFile file) =>
{
    if (file == null || file.Length == 0)
        return Results.BadRequest("No file uploaded.");

    // Thư mục lưu file
    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads");

    if (!Directory.Exists(uploadPath))
    {
        Directory.CreateDirectory(uploadPath);
    }

    // Tạo tên file duy nhất
    var fileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
    var filePath = Path.Combine(uploadPath, fileName);

    using (var stream = new FileStream(filePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }

    // Trả về URL để client dùng
    var fileUrl = $"/uploads/{fileName}";
    return Results.Ok(new { Url = fileUrl });
});
// API Export sản phẩm ra JSON file


// API Import sản phẩm từ JSON file
app.MapPost("/api/products/import", () =>
{
    if (!File.Exists("products_export.json"))
        return Results.NotFound("Không tìm thấy file products_export.json để import");

    var json = File.ReadAllText("products_export.json");
    var imported = JsonSerializer.Deserialize<List<Product>>(json);

    if (imported == null)
        return Results.BadRequest("File JSON không hợp lệ");

    
    return Results.Ok(new { Message = "Import thành công", Count = imported.Count });
});
// API Import sản phẩm từ file CSV
app.MapPost("/api/products/import-csv", () =>
{
    if (!File.Exists("products_export.csv"))
        return Results.NotFound("Không tìm thấy file products_export.csv để import");

    using var reader = new StreamReader("products_export.csv");
    using var csv = new CsvReader(reader, CultureInfo.InvariantCulture);
    var imported = csv.GetRecords<Product>().ToList();


    return Results.Ok(new { Message = "Import CSV thành công", Count = imported.Count });
});
app.MapGet("/api/users", () =>
{
    var result= users.Select(u => new
    {
        u.Id,
        u.Email,
        u.Role
    });

    return Results.Ok(result);
});
// Cập nhật thông tin cá nhân User
app.MapPut("/api/users/{id}", (int id, User updatedUser) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user == null)
        return Results.NotFound("User không tồn tại");

    // Cập nhật Email (nếu có truyền)
    if (!string.IsNullOrEmpty(updatedUser.Email))
        user.Email = updatedUser.Email;

    // Cập nhật mật khẩu (nếu có truyền)
    if (!string.IsNullOrEmpty(updatedUser.PasswordHash))
        user.PasswordHash = updatedUser.PasswordHash;

    // Cập nhật Avatar (nếu có truyền)
   

    // Không cho người dùng tự cập nhật Role (chỉ Admin mới có quyền)
    // => Nếu muốn cho phép thì mở comment dòng sau
    // user.Role = updatedUser.Role;

    return Results.Ok(new
    {
        Message = "Cập nhật thông tin thành công",
        User = user
    });
});
// API Login Google
app.MapPost("/api/auth/google", async (HttpRequest request) =>
{
    try
    {
        // Lấy token từ body
        using var reader = new StreamReader(request.Body);
        var body = await reader.ReadToEndAsync();
        var token = System.Text.Json.JsonDocument.Parse(body)
                                                .RootElement
                                                .GetProperty("idToken")
                                                .GetString();

        if (string.IsNullOrEmpty(token))
            return Results.BadRequest("Thiếu Google ID Token");

        // ✅ Verify token với Google
        var payload = await GoogleJsonWebSignature.ValidateAsync(token);

        // Kiểm tra user trong hệ thống
        var user = users.FirstOrDefault(u => u.Email == payload.Email);
        if (user == null)
        {
            // Nếu user chưa có thì tạo mới
            user = new User
            {
                Id = users.Count + 1,
                Email = payload.Email,
                PasswordHash = "", // không cần vì login Google
                Role = "User"
            };
            users.Add(user);
        }

        // Trả về thông tin user + payload
        return Results.Ok(new
        {
            message = "Đăng nhập Google thành công",
            user = new { user.Id, user.Email, user.Role },
            googleInfo = payload
        });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { error = ex.Message });
    }
});
// API Dashboard
app.MapGet("/api/dashboard", async (AppDbContext db) =>
{
    var totalUsers = await db.Users.CountAsync();
    var totalProducts = await db.Products.CountAsync();
    var totalOrders = await db.Orders.CountAsync();
   

    return Results.Ok(new
    {
        TotalUsers = totalUsers,
        TotalProducts = totalProducts,
        TotalOrders = totalOrders,
        
    });
});

app.MapDelete("/api/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    if (user == null)
    {
        return Results.NotFound(new { message = "Không tìm thấy user" });
    }

    users.Remove(user);

    return Results.Ok(new { message = $"User {id} đã được xóa thành công" });
});
//Lấy User theo Id   
app.MapGet("/api/users/{id}", (int id) =>
{
    var user = users.FirstOrDefault(u => u.Id == id);
    return user is null
        ? Results.NotFound(new { message = "Không tìm thấy user" })
        : Results.Ok(user);
});
//Tạo User  
app.MapPost("/api/users", (User newUser) =>
{
    newUser.Id = users.Count > 0 ? users.Max(u => u.Id) + 1 : 1;
    users.Add(newUser);
    return Results.Created($"/api/users/{newUser.Id}", newUser);
});
// Search user theo email
app.MapGet("/api/users/search-by-email", (string email) =>
{
    var user = users.FirstOrDefault(u =>
        u.Email.Equals(email, StringComparison.OrdinalIgnoreCase));

    return user is null
        ? Results.NotFound(new { message = "Không tìm thấy user với email này" })
        : Results.Ok(user);
});





app.Run();








