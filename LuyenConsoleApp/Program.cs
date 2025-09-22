/*using System;
using System.Net;
using System.Text;

class Program
{
    static void Main()
    {
        HttpListener listener = new HttpListener();
        listener.Prefixes.Add("http://localhost:5000/");
        listener.Start();
        Console.WriteLine("Server chạy tại http://localhost:5000 ...");

        while (true)
        {
            HttpListenerContext context = listener.GetContext();
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            string responseString;

            if (request.HttpMethod == "POST")
            {
                // Đọc dữ liệu từ form
                System.IO.Stream body = request.InputStream;
                System.IO.StreamReader reader = new System.IO.StreamReader(body, request.ContentEncoding);
                string data = reader.ReadToEnd();
                reader.Close();
                body.Close();

                responseString = $"<html><body><h2>Bạn đã nhập:</h2><p>{data}</p><a href='/'>Quay lại</a></body></html>";
            }
            else
            {
                // Hiển thị form
                responseString = @"
                <html>
                <body>
                    <h2>Form demo trong Console App</h2>
                    <form method='POST' action='/'>
                        Nhập tên: <input type='text' name='name' />
                        <input type='submit' value='Gửi' />
                    </form>
                </body>
                </html>";
            }

            byte[] buffer = Encoding.UTF8.GetBytes(responseString);
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer, 0, buffer.Length);
            response.OutputStream.Close();
        }
    }
}*/
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.IO;
using System.Linq;
using System.Web; // Add reference to System.Web if needed (for HttpUtility). Alternatively use WebUtility.

namespace ShoeStoreConsoleWebHost
{
    class Program
    {
        // In-memory "user database" (username -> password). For demo only.
        static Dictionary<string, string> users = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // pre-create a demo account
            { "demo", "demo123" }
        };
        class ShopInfo
        {
            public string Name { get; set; } = "ShoeStore Hạ Long";
            public string Address { get; set; } = "Số 123, Đường Hạ Long, Bãi Cháy, Hạ Long, Quảng Ninh";
            public string Email { get; set; } = "contact@shoestore.com";
            public string Phone { get; set; } = "+84 913 456 789";
            public string Image { get; set; } = "data:image/jpeg;base64,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";
        }
        class Product
        {
            public int Id { get; set; }
            public string Name { get; set; }
            public double  Price { get; set; }
            public string ImageUrl { get; set; }
        }
        static ShopInfo shopInfo = new ShopInfo();
        static List<Product> products = new List<Product>()
    {
        new Product { Id=1, Name="Giày Nike Air", Price=2000000, ImageUrl="data:image/jpeg;base64,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" },
        new Product { Id=2, Name="Giày Adidas UltraBoost", Price=2500000, ImageUrl="data:image/jpeg;base64,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" }
    };

        static void Main(string[] args)
        {
            

            string prefix = "http://localhost:5000/";
            HttpListener listener = new HttpListener();
            listener.Prefixes.Add(prefix);

            try
            {
                listener.Start();
            }
            catch (HttpListenerException ex)
            {
                Console.WriteLine("Không thể khởi động HttpListener. Thử chạy Visual Studio với quyền Administrator hoặc cấu hình urlacl.");
                Console.WriteLine("Lỗi: " + ex.Message);
                return;
            }

            Console.WriteLine("Server chạy tại " + prefix);
            Console.WriteLine("Mở trình duyệt vào http://localhost:5000/");

            while (true)
            {
                HttpListenerContext context = listener.GetContext();
                ThreadPool.QueueUserWorkItem((_) => HandleRequest(context));
            }
        }

        static void HandleRequest(HttpListenerContext context)
        {
            try
            {
                string path = context.Request.Url.AbsolutePath;
                if (path == "/") ShowLogin(context);
                else if (path.Equals("/login", StringComparison.OrdinalIgnoreCase))
                {
                    if (context.Request.HttpMethod == "POST") ProcessLogin(context);
                    else Redirect(context, "/");
                }
                else if (path.Equals("/register", StringComparison.OrdinalIgnoreCase))
                {
                    if (context.Request.HttpMethod == "POST") ProcessRegister(context);
                    else ShowRegister(context);
                }
                else if (path.Equals("/dashboard", StringComparison.OrdinalIgnoreCase))
                {
                    ShowDashboard(context);
                }
                else if (path == "/about")
                {

                    if (context.Request.HttpMethod == "GET")
                    {
                        ShowAbout(context); // hiển thị trang giới thiệu + thông tin shop
                    }
                    else if (context.Request.HttpMethod == "POST")
                    {
                        UpdateShopInfo(context); // cập nhật thông tin shop
                        ShowAbout(context);      // hiển thị lại trang sau khi cập nhật
                    }
                }

                /*else if (path.Equals("/products",StringComparison.OrdinalIgnoreCase))
                {
                    ShowProducts(context);
                }*/

                else if (path == "/products")
                {
                    ShowProducts(context);
                }
                else if (path == "/addproduct" && context.Request.HttpMethod == "GET")
                {
                    ShowAddProductForm(context);
                }
                else if (path == "/addproduct" && context.Request.HttpMethod == "POST")
                {
                    HandleAddProduct(context);
                }
                else if (path == "/edit-product")
                {
                    string idStr = context.Request.QueryString["id"];
                    int id = string.IsNullOrEmpty(idStr) ? -1 : int.Parse(idStr);

                    if (context.Request.HttpMethod == "GET")
                        ShowEditProductForm(context, id);
                    else if (context.Request.HttpMethod == "POST")
                        HandleEditProduct(context, id);
                }
                else if (path == "/delete-product")
                {
                    string idStr = context.Request.QueryString["id"];
                    int id = string.IsNullOrEmpty(idStr) ? -1 : int.Parse(idStr);

                    HandleDeleteProduct(context);
                }
                else if (path == "/search")
                {
                    HandleSearchProduct(context);
                }

                else

                {
                    NotFound(context);
                }
            }


            catch (Exception ex)
            {
                Console.WriteLine("HandleRequest error: " + ex.Message);
                SendString(context, "<h1>Server lỗi</h1><p>" + WebUtility.HtmlEncode(ex.Message) + "</p>", 500);
            }
        }

        #region Pages
        static void ShowLogin(HttpListenerContext ctx)
        {
            string message = "";
            // optional message from querystring
            var q = ctx.Request.Url.Query;
            if (!string.IsNullOrEmpty(q))
            {
                var qs = HttpUtility.ParseQueryString(q);
                if (!string.IsNullOrEmpty(qs["msg"])) message = $"<p style='color:red'>{WebUtility.HtmlEncode(qs["msg"])}</p>";
            }

            string html = $@"
<!doctype html>
<html>
<head>
  <meta charset='utf-8'>
  <title>Login - ShoeStore</title>
  <style>
    body {{ font-family: Arial, sans-serif; background:#f7f7f7; }}
    .card {{ width:360px; margin:60px auto; padding:20px; background:white; border-radius:6px; box-shadow:0 2px 8px rgba(0,0,0,0.1); }}
    input[type=text], input[type=password] {{ width:100%; padding:8px; margin:6px 0 12px; box-sizing:border-box; }}
    button {{ padding:10px 16px; border:none; background:#1976d2; color:white; cursor:pointer; border-radius:4px; }}
    .linkbtn {{ background:transparent; color:#1976d2; border:1px solid #1976d2; margin-left:8px; }}
    .small {{ font-size:0.9em; color:#555; }}
  </style>
</head>
<body>
  <div class='card'>
    <h2>Đăng nhập - ShoeStore</h2>
    {message}
    <form method='post' action='/login'>
      <label>Tên đăng nhập</label><br/>
      <input type='text' name='username' required /><br/>
      <label>Mật khẩu</label><br/>
      <input type='password' name='password' required /><br/>
      <button type='submit'>Đăng nhập</button>
      <a href='/register'><button type='button' class='linkbtn'>Bạn chưa có tài khoản? Đăng ký</button></a>
    </form>
    <p class='small'>Demo account: <b>demo</b> / <b>demo123</b></p>
  </div>
</body>
</html>";
            SendString(ctx, html);
        }

        static void ShowRegister(HttpListenerContext ctx)
        {
            string html = @"
<!doctype html>
<html>
<head>
  <meta charset='utf-8'>
  <title>Đăng ký - ShoeStore</title>
  <style>
    body { font-family: Arial, sans-serif; background:#f7f7f7; }
    .card { width:360px; margin:60px auto; padding:20px; background:white; border-radius:6px; box-shadow:0 2px 8px rgba(0,0,0,0.1); }
    input[type=text], input[type=password] { width:100%; padding:8px; margin:6px 0 12px; box-sizing:border-box; }
    button { padding:10px 16px; border:none; background:#388e3c; color:white; cursor:pointer; border-radius:4px; }
    .link { display:block; margin-top:10px; }
  </style>
</head>
<body>
  <div class='card'>
    <h2>Đăng ký tài khoản</h2>
    <form method='post' action='/register'>
      <label>Tên đăng nhập</label><br/>
      <input type='text' name='username' required /><br/>
      <label>Mật khẩu</label><br/>
      <input type='password' name='password' required /><br/>
      <button type='submit'>Đăng ký</button>
    </form>
    <a class='link' href='/'>Quay về Đăng nhập</a>
  </div>
</body>
</html>";
            SendString(ctx, html);
        }

        static void ShowDashboard(HttpListenerContext ctx)
        {
            string username = GetUsernameFromCookie(ctx);
            if (string.IsNullOrEmpty(username))
            {
                Redirect(ctx, "/?msg=Vui+long+dang+nhap+truoc.");
                return;
            }

            string html = $@"
<!doctype html>
<html>
<head><meta charset='utf-8'><title>Dashboard - ShoeStore</title>
<style>
body {{ font-family: Arial, sans-serif; margin:20px; }}
nav a {{ margin-right:15px; }}
</style>
</head>
<body>
  <nav>
    <a href='/dashboard'>Trang chủ</a>
    <a href='/about'>Giới thiệu</a>
    <a href='/'>Đăng xuất</a>
  </nav>
  <h2>Chào mừng, {WebUtility.HtmlEncode(username)}!</h2>
  <p>Bạn đang ở trang Dashboard. Từ đây bạn có thể truy cập mục <b>Giới thiệu</b> để xem thông tin về hệ thống bán giày.</p>
</body>
</html>";
            SendString(ctx, html);
        }
        static void ShowAbout(HttpListenerContext ctx)
        {
            string username = GetUsernameFromCookie(ctx);
            if (string.IsNullOrEmpty(username))
            {
                Redirect(ctx, "/?msg=Vui+long+dang+nhap+truoc.");
                return;
            }

            string html = $@"
<!doctype html>
<html>
<head><meta charset='utf-8'><title>Giới thiệu - ShoeStore</title>
<style>
body {{ font-family: Arial, sans-serif; margin:20px; }}
nav a {{ margin-right:15px; }}  
img {{ max-width:300px; display:block; margin:15px 0; }}
</style>
</head>
<body>
  <nav>
    <a href='/dashboard'>Trang chủ</a>
    <a href='/about'>Giới thiệu</a>
    <a href='/'>Đăng xuất</a>
  </nav>
  <h2>Giới thiệu hệ thống bán giày</h2>
  <p><b>{shopInfo.Name}</b> là hệ thống thương mại điện tử giúp khách hàng lựa chọn và mua các sản phẩm giày nhanh chóng, tiện lợi.</p> 
   <p><b>Địa chỉ:</b> {shopInfo.Address}</p>
  <p><b>Email:</b> {shopInfo.Email}</p>
  <p><b>Số điện thoại:</b> {shopInfo.Phone}</p>

  <img src='{shopInfo.Image}' alt='Ảnh Shop'/>

  <h3>Cập nhật thông tin shop</h3>
  <form method='post' action='/about'>
      Tên shop: <input type='text' name='name' value='{shopInfo.Name}'/><br/>
      Địa chỉ: <input type='text' name='address' value='{shopInfo.Address}'/><br/>
      Email: <input type='text' name='email' value='{shopInfo.Email}'/><br/>
      SĐT: <input type='text' name='phone' value='{shopInfo.Phone}'/><br/>
      Link ảnh: <input type='text' name='image' value='{shopInfo.Image}'/><br/>
      <button type='submit'>Cập nhật</button>
  </form>
</body>
</html>";
            SendString(ctx, html);
        }
        static void ShowProducts(HttpListenerContext ctx)
        {
            string username = GetUsernameFromCookie(ctx);
            if (string.IsNullOrEmpty(username))
            {
                Redirect(ctx, "/?msg=Vui+long+dang+nhap+truoc.");
                return;
            }

            string html = @"
<!doctype html>
<html>
<head><meta charset='utf-8'><title>Sản phẩm - ShoeStore</title>
<style>
body { font-family: Arial, sans-serif; margin:20px; }
nav a { margin-right:15px; }
.products { display:flex; flex-wrap:wrap; gap:20px; margin-top:20px; }
.card {
    border:1px solid #ccc; border-radius:8px;
    padding:10px; width:200px; text-align:center;
    box-shadow:2px 2px 6px rgba(0,0,0,0.1);
}
.card img { width:180px; height:120px; object-fit:cover; border-radius:5px; }
.card h3 { margin:10px 0 5px; font-size:16px; }
.card p { margin:0; color:#555; }
</style>
</head>
<body>
  <nav>
    <a href='/dashboard'>Trang chủ</a>
    <a href='/about'>Giới thiệu</a>
    <a href='/products'>Sản phẩm</a>
    <a href='/add-product'>Thêm sản phẩm</a>
    <a href='/'>Đăng xuất</a>
  </nav>
  <h2>Danh sách sản phẩm</h2>  
   <form method='get' action='/search' class='search-box'>
    <input type='text' name='q' placeholder='Nhập tên sản phẩm...'/>
    <button type='submit'>Tìm</button>
  </form>
  <div class='products'>";

            // ✅ render sản phẩm từ list products
            foreach (var p in products)
            {
                html += $@"
    <div class='card'>
      <img src='{p.ImageUrl}' alt='{p.Name}'/>
      <h3>{p.Name}</h3>
      <p>Giá: {p.Price:N0}đ</p>   
      <a href='/edit-product?id={products.IndexOf(p)}'>Cập nhật</a>  
      <a href='/delete-product?id={p.Id}' onclick='return confirm(""Bạn có chắc muốn xoá?"");'>Xoá</a>
    </div>";
            }

            html += @"
  </div>
</body>
</html>";

            SendString(ctx, html);
        }

        static void UpdateShopInfo(HttpListenerContext context)
        {
            var req = context.Request;
            if (!req.HasEntityBody) return;

            using (var body = req.InputStream)
            using (var reader = new System.IO.StreamReader(body, req.ContentEncoding))
            {
                string raw = reader.ReadToEnd();
                var pairs = raw.Split('&');
                foreach (var pair in pairs)
                {
                    var kv = pair.Split('=');
                    if (kv.Length == 2)
                    {
                        string key = WebUtility.UrlDecode(kv[0]);
                        string val = WebUtility.UrlDecode(kv[1]);
                        switch (key)
                        {
                            case "name": shopInfo.Name = val; break;
                            case "address": shopInfo.Address = val; break;
                            case "email": shopInfo.Email = val; break;
                            case "phone": shopInfo.Phone = val; break;
                            case "image": shopInfo.Image = val; break;
                        }
                    }
                }
            }
        }
        // Hàm hiển thị form thêm sản phẩm
        static void ShowAddProductForm(HttpListenerContext context)
        {
            string html = @"
    <!doctype html>
    <html>
    <head><meta charset='utf-8'><title>Thêm sản phẩm</title></head>
    <body>
        <h2>Thêm sản phẩm mới</h2>
        <form method='post' action='/addproduct'>
            Tên giày: <input type='text' name='name' required/><br/>
    Giá: <input type='number' step='0.01' name='price' required/><br/>
    Ảnh (URL): <input type='text' name='imageUrl'/><br/>
    <button type='submit'>Thêm</button>
  </form>
  <a href='/products'>Quay lại danh sách</a>
    </body>
    </html>";
            SendString(context, html);
        }
        // Hàm xử lý thêm sản phẩm
        static void HandleAddProduct(HttpListenerContext context)
        {
            using (var reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding))
            {
                string body = reader.ReadToEnd();
                var data = HttpUtility.ParseQueryString(body);

                string name = data["name"];
                double price = double.Parse(data["price"]);
                string imageUrl = data["imageUrl"];

                int newId = products.Count > 0 ? products.Max(p => p.Id) + 1 : 1;

                products.Add(new Product
                {
                    Id = newId,
                    Name = name,
                    Price = price,
                    ImageUrl = string.IsNullOrEmpty(imageUrl) ? "https://via.placeholder.com/150" : imageUrl
                });
            }

            // Redirect về danh sách sản phẩm
            Redirect(context, "/products");
        }
        static void ShowEditProductForm(HttpListenerContext ctx, int id)
        {
            if (id < 0 || id >= products.Count)
            {
                SendString(ctx, "<h3>Sản phẩm không tồn tại!</h3>");
                return;
            }

            var p = products[id];

            string html = $@"
    <html>
    <head><meta charset='utf-8'><title>Cập nhật sản phẩm</title></head>
    <body>
        <h2>Cập nhật sản phẩm</h2>
        <form method='post' action='/edit-product?id={id}'>
            <label>Tên sản phẩm:</label><br/>
            <input type='text' name='name' value='{p.Name}' required/><br/>
            <label>Giá:</label><br/>
            <input type='number' name='price' value='{p.Price}' required/><br/>
            <label>Ảnh (URL):</label><br/>
            <input type='text' name='image' value='{p.ImageUrl}' required/><br/><br/>
            <button type='submit'>Cập nhật</button>
        </form>
        <a href='/products'>Quay lại danh sách</a>
    </body>
    </html>";

            SendString(ctx, html);
        }
        static void HandleEditProduct(HttpListenerContext ctx, int id)
        {
            if (id < 0 || id >= products.Count)
            {
                SendString(ctx, "<h3>Sản phẩm không tồn tại!</h3>");
                return;
            }

            using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding))
            {
                string body = reader.ReadToEnd();
                var data = HttpUtility.ParseQueryString(body);

                products[id].Name = data["name"];
                products[id].Price = double.Parse(data["price"]);
                products[id].ImageUrl = data["image"];
            }

            Redirect(ctx, "/products");
        }
        static void HandleDeleteProduct(HttpListenerContext context)
        {
            // parse id an toàn
            string idStr = context.Request.QueryString["id"];
            if (!int.TryParse(idStr, out int id))
            {
                SendString(context, "<h3>ID không hợp lệ!</h3>");
                return;
            }

            // tìm product theo Id
            var product = products.Find(p => p.Id == id);
            if (product == null)
            {
                SendString(context, "<h3>Sản phẩm không tồn tại hoặc đã bị xóa.</h3>");
                return;
            }

            // xóa
            products.Remove(product);

            // redirect về products để thấy thay đổi
            Redirect(context, "/products");
        }
        static void HandleSearchProduct(HttpListenerContext context)
        {
            string keyword = context.Request.QueryString["q"];
            if (string.IsNullOrEmpty(keyword)) keyword = "";

            var results = products
                .Where(p => p.Name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                .ToList();

            string html = $@"
<!doctype html>
<html>
<head><meta charset='utf-8'><title>Tìm kiếm sản phẩm</title>
<style>
body {{ font-family: Arial, sans-serif; margin:20px; }}
nav a {{ margin-right:15px; }}
.products {{ display:flex; flex-wrap:wrap; gap:20px; margin-top:20px; }}
.card {{
    border:1px solid #ccc; border-radius:8px;
    padding:10px; width:200px; text-align:center;
    box-shadow:2px 2px 6px rgba(0,0,0,0.1);
}}
.card img {{ width:180px; height:120px; object-fit:cover; border-radius:5px; }}
.card h3 {{ margin:10px 0 5px; font-size:16px; }}
.card p {{ margin:0; color:#555; }}
.search-box {{ margin-top:20px; }}
</style>
</head>
<body>
  <nav>
    <a href='/dashboard'>Trang chủ</a>
    <a href='/about'>Giới thiệu</a>
    <a href='/products'>Sản phẩm</a>
    <a href='/search'>Tìm kiếm</a>
    <a href='/'>Đăng xuất</a>
  </nav>

  <h2>Kết quả tìm kiếm cho: '{keyword}'</h2>
  <form method='get' action='/search' class='search-box'>
    <input type='text' name='q' value='{keyword}' placeholder='Nhập tên sản phẩm...'/>
    <button type='submit'>Tìm</button>
  </form>

  <div class='products'>";

            if (results.Count == 0)
            {
                html += "<p>Không tìm thấy sản phẩm nào.</p>";
            }
            else
            {
                foreach (var p in results)
                {
                    html += $@"
        <div class='card'>
            <img src='{p.ImageUrl}' alt='{p.Name}'/>
            <h3>{p.Name}</h3>
            <p>Giá: {p.Price} đ</p>
            <a href='/edit-product?id={p.Id}'>Sửa</a> |
            <a href='/delete-product?id={p.Id}' onclick='return confirm(""Xoá sản phẩm này?"");'>Xoá</a>
        </div>";
                }
            }

            html += "</div></body></html>";

            SendString(context, html);
        }












        static void ProcessLogin(HttpListenerContext ctx)
        {
            string body = ReadRequestBody(ctx.Request);
            var form = ParseForm(body);

            string username = form.ContainsKey("username") ? form["username"].Trim() : "";
            string password = form.ContainsKey("password") ? form["password"] : "";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Redirect(ctx, "/?msg=Thieu+ten+hoac+mat+khau");
                return;
            }

            if (users.TryGetValue(username, out string stored) && stored == password)
            {
                // Set simple cookie for session (insecure demo)
                var cookie = new Cookie("user", Uri.EscapeDataString(username));
                cookie.Path = "/";
                ctx.Response.SetCookie(cookie);

                Redirect(ctx, "/dashboard");
            }
            else
            {
                Redirect(ctx, "/?msg=Dang+nhap+khong+chinh+xac");
            }
        }

        static void ProcessRegister(HttpListenerContext ctx)
        {
            string body = ReadRequestBody(ctx.Request);
            var form = ParseForm(body);

            string username = form.ContainsKey("username") ? form["username"].Trim() : "";
            string password = form.ContainsKey("password") ? form["password"] : "";

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                Redirect(ctx, "/register?msg=Vui+long+dien+day+du");
                return;
            }

            lock (users)
            {
                if (users.ContainsKey(username))
                {
                    Redirect(ctx, "/register?msg=Tai+khoan+da+ton+tai");
                    return;
                }
                users[username] = password;
            }

            // Auto-login: set cookie
            var cookie = new Cookie("user", Uri.EscapeDataString(username));
            cookie.Path = "/";
            ctx.Response.SetCookie(cookie);

            Redirect(ctx, "/dashboard");
        }
        #endregion

        #region Helpers
        static void SendString(HttpListenerContext ctx, string html, int statusCode = 200)
        {
            byte[] buffer = Encoding.UTF8.GetBytes(html);
            ctx.Response.StatusCode = statusCode;
            ctx.Response.ContentType = "text/html; charset=utf-8";
            ctx.Response.ContentLength64 = buffer.Length;
            ctx.Response.OutputStream.Write(buffer, 0, buffer.Length);
            ctx.Response.OutputStream.Close();
        }

        static void Redirect(HttpListenerContext ctx, string url)
        {
            ctx.Response.Redirect(url);
            ctx.Response.OutputStream.Close();
        }

        static void NotFound(HttpListenerContext ctx)
        {
            SendString(ctx, "<h1>404 - Not Found</h1>", 404);
        }

        static string ReadRequestBody(HttpListenerRequest req)
        {
            if (!req.HasEntityBody) return "";
            using (var ms = new MemoryStream())
            {
                req.InputStream.CopyTo(ms);
                return Encoding.UTF8.GetString(ms.ToArray());
            }
        }

        // Very small form parser: application/x-www-form-urlencoded
        static Dictionary<string, string> ParseForm(string body)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(body)) return dict;
            var pairs = body.Split('&');
            foreach (var p in pairs)
            {
                var idx = p.IndexOf('=');
                if (idx >= 0)
                {
                    string k = WebUtility.UrlDecode(p.Substring(0, idx));
                    string v = WebUtility.UrlDecode(p.Substring(idx + 1));
                    dict[k] = v;
                }
            }
            return dict;
        }

        static string GetUsernameFromCookie(HttpListenerContext ctx)
        {
            var cookies = ctx.Request.Cookies;
            var cookie = cookies["user"];
            if (cookie == null) return null;
            return Uri.UnescapeDataString(cookie.Value);
        }
        #endregion
    }
}