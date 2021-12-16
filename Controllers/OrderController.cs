﻿using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ITGoShop_F_Ver2.Models;
using MyCardSession.Helpers;
using Microsoft.AspNetCore.Http;

namespace ITGoShop_F_Ver2.Controllers
{
    public class OrderController : Controller
    {
        public IActionResult my_orders()
        {
            int customerId = Convert.ToInt32(HttpContext.Session.GetInt32("customerId"));
            if (customerId != 0) // Nếu customer đã đăng nhập
            {
                /*===Cái này để load layout ===*/
                ITGoShopContext context = HttpContext.RequestServices.GetService(typeof(ITGoShop_F_Ver2.Models.ITGoShopContext)) as ITGoShopContext;
                var linqContext = new ITGoShopLINQContext();
                ViewBag.AllCategory = linqContext.getAllCategory();
                ViewBag.AllBrand = linqContext.getAllBrand();
                ViewBag.AllSubBrand = linqContext.getAllSubBrand();
                /*======*/

                ViewBag.OrderList = linqContext.getOrderListOfCustomer(customerId);
                return View();
            }
            return RedirectToAction("login", "Home");
        }
        public IActionResult order_detail(int orderId)
        {
            /*===Cái này để load layout ===*/
            ITGoShopContext context = HttpContext.RequestServices.GetService(typeof(ITGoShop_F_Ver2.Models.ITGoShopContext)) as ITGoShopContext;
            ViewBag.AllCategory = context.getAllCategory();
            ViewBag.AllBrand = context.getAllBrand();
            ViewBag.AllSubBrand = context.getAllSubBrand();
            /*======*/
            var linqContext = new ITGoShopLINQContext();
            int customerId = Convert.ToInt32(HttpContext.Session.GetInt32("customerId"));
            ViewBag.DefaultShippingAddress = context.getDefaultShippingAddress(customerId);
            ViewBag.OrderInfo = linqContext.getOrderInfo(orderId);
            ViewBag.OrderDetail = context.getOrderDetail(orderId);
            return View();
        }

        public IActionResult create_order(Order order)
        {
            var cart = SessionHelper.GetObjectFromJson<List<CartItem>>(HttpContext.Session, "cart");
            ITGoShopLINQContext linqContext = new ITGoShopLINQContext();
            ITGoShopContext context = HttpContext.RequestServices.GetService(typeof(ITGoShop_F_Ver2.Models.ITGoShopContext)) as ITGoShopContext;
            //ShipMethod shipMethod = linqContext.getShipMethod(ShipMethodId);
            order.Total = order.ShipFee + cart.Sum(item => item.Product.Price * item.Quantity);
            order.UserId = Convert.ToInt32(HttpContext.Session.GetInt32("customerId"));
            order.DateUpdate = DateTime.Now;
            order.OrderDate = DateTime.Now;
            order.OrderStatus = "Đặt hàng thành công";
            order.PaymentStatus = "Chờ thanh toán";
            int numberOfProduct = cart.Sum(item => item.Quantity);
            order.Description = cart.First().Product.ProductName;
            if(numberOfProduct > 1)
                order.Description +=" và " + (numberOfProduct - 1) + " sản phẩm khác";
            int orderId = linqContext.createOrder(order);
            // Thêm theo dõi đơn hàng
            linqContext.addOrderTracking(orderId, "Đặt hàng thành công");

            foreach (var item in cart)
            {
                OrderDetail orderDetail = new OrderDetail() 
                { 
                    OrderQuantity = item.Quantity, 
                    ProductId = item.Product.ProductId, 
                    UnitPrice = item.Product.Price,
                    OrderId = order.OrderId
                };
                linqContext.saveOrderDetail(orderDetail);

                Product productInfo = context.getProductInfo(item.Product.ProductId);
                //Update thông tin doanh thu lên bảng statistic
                Statistic statistic = linqContext.getStatistic(DateTime.Now);
                Statistic newStatisticInfo = new Statistic()
                {
                    StatisticDate = DateTime.Now,
                    Sales = (int)(item.Quantity * item.Product.Price),
                    Profit = (int)((item.Product.Price - item.Product.Cost) * item.Quantity)
                };
                if (statistic != null)
                {
                    // Nếu đã có dòng thống kê doanh thu cho hôm nay thì cập nhật dữ liệu
                    linqContext.updateStatistic(newStatisticInfo);
                }
                else
                {
                    // Nếu chưa có dòng thống kê cho hôm nay thì tạo mới
                    linqContext.addStatistic(newStatisticInfo);
                }

                // Trừ số lượng tồn kho
                linqContext.updateSoldProduct(productInfo.ProductId, item.Quantity);
               
            }
            return RedirectToAction("order_detail", new { orderId = orderId });
        }
        public void cancel_order(int orderId)
        {
            ITGoShopContext context = HttpContext.RequestServices.GetService(typeof(ITGoShop_F_Ver2.Models.ITGoShopContext)) as ITGoShopContext;
            ITGoShopLINQContext linqContext = new ITGoShopLINQContext();
            linqContext.updateOrderStatus(orderId, "Đã hủy");
            List<object> orderDetail = context.getOrderDetail(orderId);

            // Cập nhật số lượng tồn kho và đã bán của các sản phẩm trong đơn hàng
            linqContext.updateSoldProduct(orderDetail);
        }
        
        public int is_rating_exit(int ProductId)
        {
            int customerId = Convert.ToInt32(HttpContext.Session.GetInt32("customerId"));
            ITGoShopLINQContext linqContext = new ITGoShopLINQContext();
            return linqContext.isRatingeExit(ProductId, customerId);
        }

        public void add_rating(int ProductId, string Title, string Content, int Rating)
        {
            int customerId = Convert.ToInt32(HttpContext.Session.GetInt32("customerId"));
            ITGoShopLINQContext linqContext = new ITGoShopLINQContext();
            linqContext.addRating(ProductId, Title, Content, Rating, customerId);
        }

        public string get_product(int ProductId)
        {
            ITGoShopLINQContext linqContext = new ITGoShopLINQContext();
            ITGoShopContext context = HttpContext.RequestServices.GetService(typeof(ITGoShop_F_Ver2.Models.ITGoShopContext)) as ITGoShopContext;
            Product product = context.getProductInfo(ProductId);
            string output = $"<p><b style='font-size:20px'>ĐÁNH GIÁ SẢN PHẨM #{product.ProductId}</b></p><img src='/public/images_upload/product/{product.ProductImage}'  style='margin: auto; max-width: 100px; max-height: 80px; width: auto; height: auto;'/>";
            output += $"<p style='display:inline-block; margin-left:10px'>{product.ProductName}</p>";
            output += $"<input type='hidden' value='{product.ProductId}</p>";
            return output;
        }
    }
}
