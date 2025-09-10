using Microsoft.AspNetCore.Mvc;
using System.Diagnostics; 
using UIShoeStroreAPI.Models;

namespace UIShoeStroreAPI.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
        // Trang giới thiệu
        public IActionResult About()
        {
            ViewData["Message"] = "Giới thiệu cửa hàng bán giày.";
            return View();
        }

        // Trang liên hệ
        public IActionResult Contact()
        {
            ViewData["Message"] = "Thông tin liên hệ.";
            return View();
        }

        // Trang Privacy (mặc định có sẵn khi tạo project)
        public IActionResult Privacy()
        {
            return View();
        }

        // Xử lý lỗi
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
