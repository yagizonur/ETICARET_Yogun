using ETICARET.Business.Abstract;
using ETICARET.DataAccess.Abstract;
using ETICARET.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ETICARET.Business.Concrete
{
    public class CartManager : ICartService
    {
        private readonly ICartDal _cartDal;

        public CartManager(ICartDal cartDal)
        {
            _cartDal = cartDal;
        }

        public void AddToCart(string userId, int productId, int quantity)
        {
            var cart = GetCartByUserId(userId);

            if(cart is not null)
            {
                var index = cart.CartItems.FindIndex(i => i.ProductId == productId);

                if(index < 0) // Eğer ürün sepette hiç yoksa sepete ürün ekle
                {
                    cart.CartItems.Add(
                        new CartItem()
                        {
                            ProductId = productId,
                            Quantity = quantity,
                            CartId = cart.Id
                        });
                }

                else // Eğer ürün sepette varsa sepetteki ürünün sayısını arttır.
                {
                    cart.CartItems[index].Quantity += quantity;
                }

                _cartDal.Update(cart);
            }
        }

        public void ClearCart(int cartId)
        {
            _cartDal.ClearCart(cartId);
        }

        public void DeleteFromCart(string userId, int productId)
        {
            var cart = GetCartByUserId(userId);

            if(cart != null)
            {
                _cartDal.DeleteFromCart(cart.Id, productId);
            }
        }

        public Cart GetCartByUserId(string userId)
        {
            return _cartDal.GetCartByUserId(userId);
        }

        public void InitialCart(string userId)
        {
            _cartDal.Create(new Cart() { UserId = userId });
        }
    }
}
