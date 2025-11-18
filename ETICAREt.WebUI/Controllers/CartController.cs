using ETICARET.Business.Abstract;
using ETICARET.Entities;
using ETICARET.WebUI.Extensions;
using ETICARET.WebUI.Identity;
using ETICARET.WebUI.Models;
using Iyzipay;
using Iyzipay.Model;
using Iyzipay.Request;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Security.Cryptography.Xml;

namespace ETICARET.WebUI.Controllers
{
    public class CartController : Controller
    {
        private readonly ICartService _cartService;
        private readonly IProductService _productService;
        private readonly IOrderService _orderService;
        private readonly UserManager<ApplicationUser> _userManager;

        public CartController(ICartService cartService, IProductService productService, IOrderService orderService, UserManager<ApplicationUser> userManager)
        {
            _cartService = cartService;
            _productService = productService;
            _orderService = orderService;
            _userManager = userManager;
        }

        public IActionResult Index()
        {
            var cart = _cartService.GetCartByUserId(_userManager.GetUserId(User));

            if(cart == null)
            {
                _cartService.InitialCart(_userManager.GetUserId(User));
                cart = _cartService.GetCartByUserId(_userManager.GetUserId(User));
            }

            return View(
                new CartModel()
                {
                    CartId = cart.Id,
                    CartItems = cart.CartItems.Select(ci => new CartItemModel()
                    {
                        CartItemId = ci.Id,
                        ProductId = ci.Product.Id,
                        Name = ci.Product.Name,
                        ImageUrl = ci.Product.Images[0].ImageUrl,
                        Price = ci.Product.Price,
                        Quantity = ci.Quantity
                    }).ToList()
                });
        }

        public IActionResult AddToCart(int productId, int quantity)
        {
            _cartService.AddToCart(_userManager.GetUserId(User), productId, quantity);
            return RedirectToAction("Index");
        }

        public IActionResult DeleteFromCart(int productId)
        {
            _cartService.DeleteFromCart(_userManager.GetUserId(User), productId);
            return RedirectToAction("Index");
        }

        public IActionResult Checkout()
        {
            var cart = _cartService.GetCartByUserId(_userManager.GetUserId(User));

            OrderModel orderModel = new OrderModel();

            orderModel.CartModel = new CartModel()
            {
                CartId = cart.Id,
                CartItems = cart.CartItems.Select(ci => new CartItemModel()
                {
                    CartItemId = ci.Id,
                    ProductId = ci.Product.Id,
                    Name = ci.Product.Name,
                    ImageUrl = ci.Product.Images[0].ImageUrl,
                    Price = ci.Product.Price,
                    Quantity = ci.Quantity
                }).ToList()
            };

            return View(orderModel);
        }

        [HttpPost]
        public IActionResult Checkout(OrderModel model,string paymentMethod)
        {
            ModelState.Remove("CartModel");

            if (ModelState.IsValid)
            {
                var userId = _userManager.GetUserId(User);

                var cart = _cartService.GetCartByUserId(userId);

                model.CartModel = new CartModel()
                {
                    CartId = cart.Id,
                    CartItems = cart.CartItems.Select(ci => new CartItemModel()
                    {
                        CartItemId = ci.Id,
                        ProductId = ci.Product.Id,
                        Name = ci.Product.Name,
                        ImageUrl = ci.Product.Images[0].ImageUrl,
                        Price = ci.Product.Price,
                        Quantity = ci.Quantity
                    }).ToList(),
                };

                if (paymentMethod == "credit")
                {
                    var payment = PaymentProcess(model);
                    if (payment.Result.Status == "success")
                    {
                        SaveOrder(model, payment, userId);
                        ClearCart(cart.Id);
                        TempData.Put("message", new ResultModel()
                        {
                            Title = "Sipariş Tamamlandı",
                            Message = "Siparişiniz Alınmıştır. Teşekkür Ederiz.",
                            Css = "success"
                        });
                    }
                }

                else
                {
                    SaveOrder(model, userId);
                    ClearCart(cart.Id);

                    TempData.Put("message", new ResultModel()
                    {
                        Title = "Sipariş Tamamlandı",
                        Message = "Siparişiniz Alınmıştır. Teşekkür Ederiz.",
                        Css = "success"
                    });
                }
            }

            return View(model);
        }

        private void ClearCart(int cartId)
        {
            _cartService.ClearCart(cartId);
        }

        // EFT 
        private void SaveOrder(OrderModel model, string userId)
        {
            Order order = new Order()
            {
                OrderNumber = Guid.NewGuid().ToString(),
                OrderState = EnumOrderState.waiting,
                PaymentTypes = EnumPaymentTypes.Eft,
                PaymentToken = Guid.NewGuid().ToString(),
                ConversionId = Guid.NewGuid().ToString(),
                PaymentId = Guid.NewGuid().ToString(),
                FirstName = model.FirstName,
                LastName = model.LastName,
                Address = model.Address,
                City = model.City,
                Phone = model.Phone,
                Email = model.Email,
                OrderNote = model.OrderNote,
                UserId = userId,
                OrderDate = DateTime.Now,
            };

            foreach (var cartItem in model.CartModel.CartItems)
            {
                var orderItem = new Entities.OrderItem()
                {
                    Price = cartItem.Price,
                    Quantity = cartItem.Quantity,
                    ProductId = cartItem.ProductId,
                };

                order.OrderItems.Add(orderItem); 
            }

            _orderService.Create(order);
        }

        // Credit Card
        private void SaveOrder(OrderModel model, Task<Payment> payment,string userId)
        {
            Order order = new Order()
            {
                OrderNumber = Guid.NewGuid().ToString(),
                OrderState = EnumOrderState.completed,
                PaymentTypes = EnumPaymentTypes.CreditCard,
                PaymentToken = Guid.NewGuid().ToString(),
                ConversionId = Guid.NewGuid().ToString(),
                PaymentId = Guid.NewGuid().ToString(),
                FirstName = model.FirstName,
                LastName = model.LastName,
                Address = model.Address,
                City = model.City,
                Phone = model.Phone,
                Email = model.Email,
                OrderNote = model.OrderNote,
                UserId = userId,
                OrderDate = DateTime.Now,
            };


            foreach (var cartItem in model.CartModel.CartItems)
            {
                var orderItem = new Entities.OrderItem()
                {
                    Price = cartItem.Price,
                    Quantity = cartItem.Quantity,
                    ProductId = cartItem.ProductId,
                };

                order.OrderItems.Add(orderItem);
            }

            _orderService.Create(order);
        }

        private async Task<Payment> PaymentProcess(OrderModel model)
        {
            Options options = new Options()
            {
                BaseUrl = "https://sandbox-api.iyzipay.com",
                ApiKey = "sandbox-cNnJEaoyNt0sCREL4nOq8PajTLQwWeXz",
                SecretKey = "sandbox-cmJxJfaGlVarqNV3c5ZQcMTwVNh8qswx"
            };

            string externalIpString = new WebClient().DownloadString("https://icanhazip.com").Replace("\\r\\n", "").Replace("\\n", "").Trim();
            var externalIp = IPAddress.Parse(externalIpString);

            CreatePaymentRequest request = new CreatePaymentRequest();
            request.Locale = Locale.TR.ToString();
            request.ConversationId = Guid.NewGuid().ToString();
            request.Price = model.CartModel.TotalPrice().ToString().Split(',')[0];
            request.PaidPrice = model.CartModel.TotalPrice().ToString().Split(',')[0];
            request.Currency = Currency.TRY.ToString();
            request.Installment = 1;
            request.BasketId = model.CartModel.CartId.ToString();
            request.PaymentChannel = PaymentChannel.WEB.ToString();
            request.PaymentGroup = PaymentGroup.PRODUCT.ToString();

            PaymentCard paymentCard = new PaymentCard()
            {
                CardHolderName = model.CardName,
                CardNumber = model.CardNumber,
                ExpireMonth = model.ExprationMonth,
                ExpireYear = model.ExprationYear,
                Cvc = model.Cvv,
                RegisterCard = 0
            };
            
            request.PaymentCard = paymentCard;

            Buyer buyer = new Buyer()
            {
                Id = _userManager.GetUserId(User),
                Name = model.FirstName,
                Surname = model.LastName,
                GsmNumber = model.Phone,
                Email = model.Email,
                IdentityNumber = "11111111111",
                RegistrationAddress = model.Address,
                Ip = externalIp.ToString(),
                City = model.City,  
                Country = "TURKEY",
                ZipCode = "34000"
            };

            request.Buyer = buyer;

            Address address = new Address()
            {
                ContactName = model.FirstName + " " + model.LastName,
                City = model.City,
                Country = "Turkey",
                Description = model.Address,
                ZipCode = "34000"
            };

            request.BillingAddress = address;
            request.ShippingAddress = address;

            List<BasketItem> basketItems = new List<BasketItem>();
            BasketItem basketItem;

            foreach(var cartItem in model.CartModel.CartItems)
            {
                basketItem = new BasketItem()
                {
                    Id = cartItem.ProductId.ToString(),
                    Name = cartItem.Name,
                    Category1 = _productService.GetProductDetails(cartItem.ProductId).ProductCategories.FirstOrDefault().CategoryId.ToString(),
                    ItemType = BasketItemType.PHYSICAL.ToString(),
                    Price = (cartItem.Price * cartItem.Quantity).ToString().Split(",")[0]
                };
                basketItems.Add(basketItem);
            }


            request.BasketItems = basketItems;

            Payment payment = await Payment.Create(request, options);

            return payment;

        }

        public IActionResult GetOrders()
        {
            var userId = _userManager.GetUserId(User);
            var orders = _orderService.GetOrders(userId);

            var orderListModel = new List<OrderListModel>();
            OrderListModel orderModel;

            foreach (var order in orders)
            {
                orderModel = new OrderListModel();
                orderModel.OrderId = order.Id;
                orderModel.OrderNumber = order.OrderNumber;
                orderModel.OrderDate = order.OrderDate;
                orderModel.FirstName = order.FirstName;
                orderModel.LastName = order.LastName;
                orderModel.Address = order.Address;
                orderModel.City = order.City;
                orderModel.Phone = order.Phone;
                orderModel.Email = order.Email;
                orderModel.OrderState = order.OrderState;
                orderModel.PaymentTypes = order.PaymentTypes;
                orderModel.OrderItems = order.OrderItems.Select(oi => new OrderItemModel()
                {
                    OrderItemId = oi.OrderId,
                    Name = oi.Product.Name,
                    Price = oi.Product.Price,
                    Quantity = oi.Quantity,
                    ImageUrl = oi.Product.Images[0].ImageUrl
                }).ToList();

                orderListModel.Add(orderModel);

            }

            return View(orderListModel);
        }
    }
}
