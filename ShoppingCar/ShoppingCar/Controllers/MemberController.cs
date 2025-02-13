﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using ShoppingCar.Models; //表單驗證

namespace ShoppingCar.Controllers
{
    [Authorize]
    public class MemberController : Controller
    {
        private ShoppingCarContext db = new ShoppingCarContext();
        public ActionResult Index()
        {
            //查詢全部商品
            var products = db.Products.OrderByDescending(m => m.Id).ToList();

            if (products == null)
                return HttpNotFound();

            //使用_LayoutMember
            return View("..\\Home\\Index", "_LayoutMember", products);
        }

        public ActionResult Logout()
        {
            FormsAuthentication.SignOut();
            return RedirectToAction("index", "Home");
        }

        public ActionResult ShoppingCar()
        {
            string UserId = User.Identity.Name;
            //查詢購物車清單(未核准狀態的明細)
            var OrderDetails = db.OrderDetails.Where(m => m.UserId == UserId && m.IsApproved == "NO").ToList();

            if (OrderDetails == null)
                return HttpNotFound();

            return View(OrderDetails);
        }

        public ActionResult AddCar(string PId)
        {
            if (PId == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            //購物車加入商品
            string UserId = User.Identity.Name;
            var CerrentCarDetail = db.OrderDetails.Where(m => m.UserId == UserId && m.PId == PId && m.IsApproved == "NO").FirstOrDefault();

            bool IsOrder = CerrentCarDetail != null;

            //在購物車中  數量+1
            if (IsOrder)
            {
                CerrentCarDetail.Qty += 1;
            }
            //不再購物車中  新增一筆
            else
            {
                var product = db.Products.Where(m => m.PId == PId).FirstOrDefault();
                OrderDetail OrderDetial = new OrderDetail();
                OrderDetial.UserId = UserId;
                OrderDetial.PId = product.PId;
                OrderDetial.Name = product.Name;
                OrderDetial.Price = product.Price;
                OrderDetial.Qty = 1;
                OrderDetial.IsApproved = "NO";
                db.OrderDetails.Add(OrderDetial);
            }

            db.SaveChanges();
            return RedirectToAction("ShoppingCar");

        }

        public ActionResult DeleteCar(int Id)
        {
            //查詢要刪除的購物車明細
            var OrderDetail = db.OrderDetails.Where(m => m.Id == Id).FirstOrDefault();

            db.OrderDetails.Remove(OrderDetail);
            db.SaveChanges();
            return RedirectToAction("ShoppingCar");
        }

        public ActionResult PlaceOrder()
        {
            if (Is_the_shopping_cart_empty())
            {
                //停留在購物車
                return RedirectToAction("ShoppingCar");
            }
            else
            {
                //進入訂單頁面
                return View();
            }
        }

        /// <summary>
        /// 判斷購物車是不是空的
        /// </summary>
        /// <returns></returns>
        bool Is_the_shopping_cart_empty()
        {
            string UserId = User.Identity.Name;
            var OrderDetails = db.OrderDetails.Where(m => m.UserId == UserId && m.IsApproved == "NO").ToList();

            return OrderDetails.Count <= 0;        
        }

        [HttpPost]
        public ActionResult PlaceOrder(string fReceiver, string fEmail, string fAddress)
        {
            if (fReceiver == null || fEmail == null || fAddress == null)
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            string UserId = User.Identity.Name;
            //訂單與明細，用GUID來產生關聯
            string guid = Guid.NewGuid().ToString();

            var OrderDetails = db.OrderDetails.Where(m => m.UserId == UserId && m.IsApproved == "NO").ToList();
            //將購物車狀態的明細，改成訂單狀態的明細
            foreach (var item in OrderDetails)
            {
                item.OrderGuid = guid;
                item.IsApproved = "YES";
            }

            //創建訂單
            Order order = new Order();
            order.OrderGuid = guid;
            order.UserId = UserId;
            order.Receiver = fReceiver;
            order.Email = fEmail;
            order.Address = fAddress;
            order.Date = DateTime.Now;
            db.Orders.Add(order);

            db.SaveChanges();
            return RedirectToAction("OrderList");
        }

        public ActionResult OrderList()
        {
            string UserId = User.Identity.Name;
            //訂單清單
            var orders = db.Orders.Where(m => m.UserId == UserId).OrderByDescending(m => m.Date).ToList();
            return View(orders);
        }

        public ActionResult OrderDetail(string fOrderGuid)
        {
            //訂單明細
            var orderDetails = db.OrderDetails.Where(m => m.OrderGuid == fOrderGuid).ToList();
            return View(orderDetails);
        }

        public ActionResult MemberInfo()
        {
            //顯示會員資料
            string name = User.Identity.Name;
            var member = db.Members.Where(m => m.UserId == name).ToList();

            return View(member);

        }

        public ActionResult EditMember()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditMember([Bind(Include = "Name,Email")] Member NewMemberInfo)
        {
            //編輯會員資料
            string ID = User.Identity.Name;
            var Member = db.Members.Where(m => m.UserId == ID).FirstOrDefault();

            Member.Name = NewMemberInfo.Name;
            Member.Email = NewMemberInfo.Email;

            //改名後重設歡迎詞
            Session["Welcome"] = Member.Name + "歡迎光臨";

            db.SaveChanges();

            return RedirectToAction("MemberInfo");
        }

        public ActionResult DeleteMember()
        {
            //刪除帳號
            string ID = User.Identity.Name;
            var Member = db.Members.Where(m => m.UserId == ID).FirstOrDefault();

            db.Members.Remove(Member);
            db.SaveChanges();

            //導向登出Action
            return RedirectToAction("Logout");
        }

        public ActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword([Bind(Include = "Pwd")] Member NewMemberInfo)
        {
            //變更密碼
            string ID = User.Identity.Name;
            var Member = db.Members.Where(m => m.UserId == ID).FirstOrDefault();

            Member.Pwd = NewMemberInfo.Pwd;

            db.SaveChanges();

            return RedirectToAction("MemberInfo");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}