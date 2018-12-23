using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace WeChat.Controllers
{
    public class WeChatConfigController : Controller
    {
        // GET: WeChatConfig
        public ActionResult Index()
        {
            return View();
        }

        #region 进入管理后台页面页面
        public ActionResult loginFrom(string name,string pwd)
        {
            if (name == "王旗龙" && pwd == "wangqilong")
            {
                return View();
            }
            else {
                return null;
            }
        }
        #endregion
    }
}