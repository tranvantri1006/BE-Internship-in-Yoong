
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using UIShoeStoreAPI.Models;
namespace UIShoeStoreAPI.Controllers
{
    public class ProductController:Controller
    {
        // Fake DB (demo), có thể thay bằng Entity Framework + SQL
        private static List<Product> products = new List<Product>
        {
            new Product { Id = 1, Name = "Nike Air", Price = 200, ImageUrl = "/Content/images/nike.jpg", Description="Giày Nike Air êm ái" },
            new Product { Id = 2, Name = "Adidas Ultraboost", Price = 250, ImageUrl = "/Content/images/adidas.jpg", Description="Giày Adidas chạy bộ" },
            new Product { Id = 3, Name = "Puma RS-X", Price = 180, ImageUrl = "/Content/images/puma.jpg", Description="Giày Puma trẻ trung" }
        };

        // Danh sách sản phẩm
        public IActionResult Index()
        {
            return View(products);
        }

        // Chi tiết sản phẩm
        public IActionResult Details(int id)
        {
            var product = products.FirstOrDefault(p => p.Id == id);
            if (product == null)
            {
                return NotFound();
            }
            return View(product);
        }

    }
}
