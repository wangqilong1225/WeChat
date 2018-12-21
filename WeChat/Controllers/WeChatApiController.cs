using System;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using System.Web;
using System.Xml;
using System.IO;

namespace WeChat.Controllers
{
    public class WeChatApiController : Controller
    {
        private static string TOKEN = "wangqilong"; //后期采用配置文件
        // GET: WeChatApi
        public ActionResult Index()
        {
            return View();
        }

        /// <summary>
        /// 服务器接入接口验证
        /// </summary>
        /// <param name="signature">微信加密签名</param>
        /// <param name="timestamp">时间戳</param>
        /// <param name="nonce">随机数</param>
        /// <param name="echostr">随机字符串</param>
        [HttpGet]
        public void Wx(string signature, string timestamp, string nonce, string echostr)
        {
            Console.WriteLine(signature + "," + timestamp + "," + nonce + "," + echostr);
            //效验请求
            bool signatureFlag = CheckWeChatServer(signature, timestamp, nonce, echostr);
            if (signatureFlag)
            {
                System.Web.HttpContext.Current.Response.Write(echostr);
                System.Web.HttpContext.Current.Response.End();
            }
            else
            {
                System.Web.HttpContext.Current.Response.Write("签名验证错误！");
                System.Web.HttpContext.Current.Response.End();
            }

          // ProcessRequest(System.Web.HttpContext.Current);
           // Stream s = VqiRequest.GetInputStream();
        }

        /// <summary>
        /// 服务器接入签名计算和比对
        /// </summary>
        public bool CheckWeChatServer(string signature, string timestamp, string nonce, string echostr)
        {
            //将token、timestamp、nonce三个参数进行字典序排序
            string[] strs = new string[] { TOKEN, timestamp, nonce };
            Array.Sort(strs);

            //将三个参数字符串拼接成一个字符串进行sha1加密 
            string str = strs[0] + strs[1] + strs[2];
            string mySignature = Sha1(str);
            if (signature == mySignature)
            {
                return true;
            }
            else
            {
                Console.WriteLine(mySignature);
                return false;
            }
        }

        #region sha1加密
        /// <summary>
        /// sha1加密
        /// </summary>
        public string Sha1(string str)
        {
            var buffer = Encoding.UTF8.GetBytes(str);
            var data = SHA1.Create().ComputeHash(buffer);
            var sb = new StringBuilder();
            foreach (var t in data)
            {
                //x2 签名全部为小写
                sb.Append(t.ToString("x2"));
            }

            return sb.ToString();
        }
        #endregion

        //[HttpPost]
        //[ActionName("Wx")]
        //public void ProcessRequest(HttpContext context)
        //{
        //    context.Response.ContentType = "text/plain";
        //    string postString = string.Empty;
        //    if (System.Web.HttpContext.Current.Request.HttpMethod.ToUpper() == "POST")
        //    {
        //        using (Stream stream = System.Web.HttpContext.Current.Request.InputStream)
        //        {
        //            Byte[] postBytes = new Byte[stream.Length];
        //            stream.Read(postBytes, 0, (Int32)stream.Length);
        //            postString = Encoding.UTF8.GetString(postBytes);
        //            Handle(postString);
        //        }
        //    }
        //}
        ///// <summary>
        ///// 处理信息并应答
        ///// </summary>
        //private void Handle(string postStr)
        //{
        //    messageHelp help = new messageHelp();
        //    string responseContent = help.ReturnMessage(postStr);
        //    System.Web.HttpContext.Current.Response.ContentEncoding = Encoding.UTF8;
        //    System.Web.HttpContext.Current.Response.Write(responseContent);
        //}

        //public class messageHelp
        //{
        //    public string ReturnMessage(string postStr)
        //    {
        //        string responseContent = "";
        //        XmlDocument xmldoc = new XmlDocument();
        //        xmldoc.Load(new System.IO.MemoryStream(System.Text.Encoding.GetEncoding("UTF-8").GetBytes(postStr)));
        //        XmlNode MsgType = xmldoc.SelectSingleNode("/xml/MsgType");
        //        if (MsgType != null)
        //        {
        //            switch (MsgType.InnerText)
        //            {
        //                case "event":
        //                    responseContent = EventHandle(xmldoc);//事件处理
        //                    break;
        //                    //case "text":
        //                    //    responseContent = TextHandle(xmldoc);//接受文本消息处理break;
        //            }
        //        }
        //        return responseContent;
        //    }
        //    //事件
        //    public string EventHandle(XmlDocument xmldoc)
        //    {
        //        string responseContent = "";
        //        XmlNode Event = xmldoc.SelectSingleNode("/xml/Event");
        //        XmlNode EventKey = xmldoc.SelectSingleNode("/xml/EventKey");
        //        XmlNode ToUserName = xmldoc.SelectSingleNode("/xml/ToUserName");
        //        XmlNode FromUserName = xmldoc.SelectSingleNode("/xml/FromUserName");
        //        if (Event != null)
        //        {
        //            //菜单单击事件
        //            //if (Event.InnerText.Equals("CLICK"))
        //            //{
        //            //    Helper.GetUserDetail(Helper.IsExistAccess_Token(), FromUserName.InnerText);//获取用户基本信息
        //            //    if (EventKey.InnerText.Equals("12"))
        //            //    {
        //            //        responseContent = string.Format(ReplyType.Message_Text,
        //            //            FromUserName.InnerText,
        //            //            ToUserName.InnerText,
        //            //            DateTime.Now.Ticks,
        //            //            "欢迎查看工作动态");
        //            //    }
        //            //}
        //            //else if (Event.InnerText.Equals("subscribe"))//关注公众号时推送消息
        //            //{
        //            //    Helper.GetUserDetail(Helper.IsExistAccess_Token(), FromUserName.InnerText);//获取用户基本信息
        //            //    responseContent = string.Format(ReplyType.Message_Text,
        //            //        FromUserName.InnerText,
        //            //        ToUserName.InnerText,
        //            //        DateTime.Now.Ticks,
        //            //        "欢迎关注XX公司");
        //            //}
        //        }
        //        return responseContent;
        //    }
        //    //接受文本消息
        //    public string TextHandle(XmlDocument xmldoc)
        //    {
        //        string responseContent = "";
        //        XmlNode ToUserName = xmldoc.SelectSingleNode("/xml/ToUserName");//接收方帐号（收到的OpenID）
        //        XmlNode FromUserName = xmldoc.SelectSingleNode("/xml/FromUserName");//开发者微信号
        //        XmlNode Content = xmldoc.SelectSingleNode("/xml/Content");
        //        if (Content != null)
        //        {
        //            responseContent = string.Format(ReplyType.Message_Text,
        //               FromUserName.InnerText,
        //                ToUserName.InnerText,
        //                DateTime.Now.Ticks,
        //                "欢迎使用微信公众号，如有任何疑问请联系我们客服人员！");
        //        }
        //        return responseContent;
        //    }
        //    //回复类型
        //    public class ReplyType
        //    {
        //        /// <summary>
        //        /// 普通文本消息
        //        /// </summary>
        //        public static string Message_Text
        //        {
        //            get
        //            {
        //                return @"<xml>
        //                    <ToUserName><![CDATA[{0}]]></ToUserName>
        //                    <FromUserName><![CDATA[{1}]]></FromUserName>
        //                    <CreateTime>{2}</CreateTime>
        //                    <MsgType><![CDATA[text]]></MsgType>
        //                    <Content><![CDATA[{3}]]></Content>
        //                    </xml>";
        //            }
        //        }
        //    }
        //}


        
        //public void Wx2(string signature, string timestamp, string nonce, string echostr)
        //{
        //    Console.WriteLine(signature + "," + timestamp + "," + nonce + "," + echostr);
        //    //效验请求
        //    bool signatureFlag = CheckWeChatServer(signature, timestamp, nonce, echostr);
        //    if (signatureFlag)
        //    {
        //        System.Web.HttpContext.Current.Response.Write(echostr);
        //        System.Web.HttpContext.Current.Response.End();
        //    }
        //    else
        //    {
        //        System.Web.HttpContext.Current.Response.Write("签名验证错误！");
        //        System.Web.HttpContext.Current.Response.End();
        //    }

        //    // ProcessRequest(System.Web.HttpContext.Current);
        //    // Stream s = VqiRequest.GetInputStream();
        //}
    }
}