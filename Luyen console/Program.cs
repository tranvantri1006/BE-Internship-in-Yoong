using System;
using System.Collections.Generic;

class Program
{
    // Lớp sản phẩm
    class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public double Price { get; set; }
    }

    static List<Product> products = new List<Product>
    {
        new Product { Id = 1, Name = "Nike Air", Price = 200 },
        new Product { Id = 2, Name = "Adidas Superstar", Price = 180 },
        new Product { Id = 3, Name = "Puma Classic", Price = 150 }
    };

    static List<Product> cart = new List<Product>();

    static void ShowProducts()
    {
        Console.WriteLine("=== Danh sách giày ===");
        foreach (var p in products)
        {
            Console.WriteLine($"{p.Id}. {p.Name} - ${p.Price}");
        }
        Console.WriteLine("=====================");
    }

    static void AddToCart()
    {
        Console.Write("Nhập ID giày muốn mua: ");
        int id;
        if (int.TryParse(Console.ReadLine(), out id))
        {
            var product = products.Find(p => p.Id == id);
            if (product != null)
            {
                cart.Add(product);
                Console.WriteLine($"Đã thêm {product.Name} vào giỏ hàng!");
            }
            else
            {
                Console.WriteLine("Sản phẩm không tồn tại!");
            }
        }
        else
        {
            Console.WriteLine("ID không hợp lệ!");
        }
    }

    static void ViewCart()
    {
        Console.WriteLine("=== Giỏ hàng ===");
        double total = 0;
        foreach (var item in cart)
        {
            Console.WriteLine($"{item.Name} - ${item.Price}");
            total += item.Price;
        }
        Console.WriteLine($"Tổng: ${total}");
        Console.WriteLine("================");
    }

    static void Checkout()
    {
        if (cart.Count == 0)
        {
            Console.WriteLine("Giỏ hàng trống, không thể thanh toán!");
            return;
        }

        double total = 0;
        Console.WriteLine("=== Hóa đơn thanh toán ===");
        foreach (var item in cart)
        {
            Console.WriteLine($"{item.Name} - ${item.Price}");
            total += item.Price;
        }
        Console.WriteLine($"Tổng tiền cần thanh toán: ${total}");
        Console.WriteLine("Thanh toán thành công, cảm ơn bạn đã mua hàng!");

        cart.Clear(); // Xóa giỏ hàng sau khi thanh toán
    }
    static void SearchProduct()
    {
        Console.Write("Nhập tên giày cần tìm: ");
        string keyword = Console.ReadLine().ToLower();

        var results = products.FindAll(p => p.Name.ToLower().Contains(keyword));

        if (results.Count > 0)
        {
            Console.WriteLine("=== Kết quả tìm kiếm ===");
            foreach (var p in results)
            {
                Console.WriteLine($"{p.Id}. {p.Name} - ${p.Price}");
            }
        }
        else
        {
            Console.WriteLine("Không tìm thấy sản phẩm nào!");
        }
    } 


    static void Main(string[] args)
    {
        while (true)
        {
            Console.WriteLine("\n=== App Console Bán Giày ===");
            Console.WriteLine("1. Xem sản phẩm");
            Console.WriteLine("2. Thêm vào giỏ hàng");
            Console.WriteLine("3. Xem giỏ hàng");
            Console.WriteLine("4. Thoát");
            Console.WriteLine("5. Thanh toán");
            Console.WriteLine("6. Tìm kiếm sản phẩm");
            Console.Write("Chọn chức năng: ");
             
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ShowProducts();
                    break;
                case "2":
                    AddToCart();
                    break;
                case "3":
                    ViewCart();
                    break;
                case "4":
                    Console.WriteLine("Cảm ơn bạn đã sử dụng app!");
                    return;
                case "5":
                    Checkout();
                    break;
                case "6":
                    SearchProduct();
                    break;
            
                     
                default:
                    Console.WriteLine("Chọn chức năng không hợp lệ!");
                    break;
            }
        }
    }
}
