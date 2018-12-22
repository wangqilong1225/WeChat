using System;
using System.Security.Cryptography;
using System.Text;
using System.Web.Mvc;
using System.Web;
using System.Xml;
using System.IO;
using WeChat.Models;

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
        public void Wx(string signature, string timestamp, string nonce, string echostr)
        {
            //Get请求处理
            if (System.Web.HttpContext.Current.Request.HttpMethod.ToUpper() == "Get")
            {
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
            }
            //Post请求处理
            
            if (System.Web.HttpContext.Current.Request.HttpMethod.ToUpper() == "POST")
            {
                Stream requestStream = System.Web.HttpContext.Current.Request.InputStream;
                byte[] requestByte = new byte[requestStream.Length];
                requestStream.Read(requestByte, 0, (Int32)requestStream.Length);
                string requestStr = Encoding.UTF8.GetString(requestByte);

                if (!string.IsNullOrEmpty(requestStr))
                {
                    //封装请求类
                    XmlDocument requestDocXml = new XmlDocument();
                    requestDocXml.LoadXml(requestStr);
                    XmlElement rootElement = requestDocXml.DocumentElement;
                    NewsModel newsModel = new NewsModel();
                    newsModel.ToUserName = rootElement.SelectSingleNode("ToUserName").InnerText;
                    newsModel.FromUserName = rootElement.SelectSingleNode("FromUserName").InnerText;
                    newsModel.CreateTime = rootElement.SelectSingleNode("CreateTime").InnerText;
                    newsModel.MsgType = rootElement.SelectSingleNode("MsgType").InnerText;
                    switch (newsModel.MsgType)
                    {
                        case "text"://文本
                            newsModel.Content = rootElement.SelectSingleNode("Content").InnerText;
                            break;
                        case "image"://图片
                            newsModel.PicUrl = rootElement.SelectSingleNode("PicUrl").InnerText;
                            break;
                        case "event"://事件
                            newsModel.Event = rootElement.SelectSingleNode("Event").InnerText;
                            if (newsModel.Event == "subscribe")//关注类型
                            {
                                newsModel.EventKey = rootElement.SelectSingleNode("EventKey").InnerText;
                            }
                            break;
                        default:
                            break;
                    }
                    ResponseXML(newsModel);//回复消息
                    //bs.Close();
                    //return Encoding.UTF8.GetString(buff);
                }

            }
        }

        #region 服务器接入签名计算和比对
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
        #endregion

        #region sha1加密    
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

        #region 消息回复
        private void ResponseXML(NewsModel newsModel)
        {
            string XML = "";
            switch (newsModel.MsgType)
            {
                case "text"://文本回复
                    XML = ReText(newsModel.FromUserName, newsModel.ToUserName, newsModel.Content);                   
                    break;
                case "image"://图片回复
                    XML = ReImage(newsModel.FromUserName, newsModel.ToUserName, newsModel.PicUrl);
                    break;
                case "event"://事件回复
                    XML = ReEvent(newsModel.FromUserName, newsModel.ToUserName, newsModel.Event);
                    break;
                default://默认回复
                    break;
            }
            System.Web.HttpContext.Current.Response.Write(XML);
            System.Web.HttpContext.Current.Response.End();
        }
        #endregion

        #region 文本格式
        /// <summary>
        /// 回复文本
        /// </summary>
        /// <param name="FromUserName">发送给谁(openid)</param>
        /// <param name="ToUserName">来自谁(公众账号ID)</param>
        /// <param name="Content">回复类型文本</param>
        /// <returns>拼凑的XML</returns>
        public static string ReText(string FromUserName, string ToUserName, string Content)
        {
            string XML = "<xml><ToUserName><![CDATA[" + FromUserName + "]]></ToUserName><FromUserName><![CDATA[" + ToUserName + "]]></FromUserName>";//发送给谁(openid)，来自谁(公众账号ID)
            XML += "<CreateTime>" + ConvertDateTimeInt(DateTime.Now) + "</CreateTime>";//回复时间戳
            XML += "<MsgType><![CDATA[text]]></MsgType>";//回复类型文本
            XML += "<Content><![CDATA[" + Content + "]]></Content><FuncFlag>0</FuncFlag></xml>";//回复内容 FuncFlag设置为1的时候，自动星标刚才接收到的消息，适合活动统计使用
            return XML;
        }
        #endregion

        #region 图片格式
        public static string ReImage(string FromUserName, string ToUserName, string PicUrl)
        {
            //回复用户图片信息，需要使用素材管理，必须含有参数 access_token，media_id
            //现在公众号接收到消息，先回复文本消息。
            string XML = "<xml><ToUserName><![CDATA[" + FromUserName + "]]></ToUserName><FromUserName><![CDATA[" + ToUserName + "]]></FromUserName>";//发送给谁(openid)，来自谁(公众账号ID)
            XML += "<CreateTime>" + ConvertDateTimeInt(DateTime.Now) + "</CreateTime>";//回复时间戳
            //XML += "<MsgType><![CDATA[image]]></MsgType>";//回复类型文本
            //XML += "<PicUrl><![CDATA[" + PicUrl + "]]></PicUrl>";
            XML += "<MsgType><![CDATA[text]]></MsgType>";
            XML += "<Content><![CDATA[亲，已经接收到您的图片，图片消息回复接口暂时未开通。]]></Content>";
            XML += "<FuncFlag>0</FuncFlag></xml>";//回复内容 FuncFlag设置为1的时候，自动星标刚才接收到的消息，适合活动统计使用
            return XML;
        }
        #endregion

        #region 事件格式
        public static string ReEvent(string FromUserName, string ToUserName, string Event)
        {
            
            string XML = "<xml><ToUserName><![CDATA[" + FromUserName + "]]></ToUserName><FromUserName><![CDATA[" + ToUserName + "]]></FromUserName>";//发送给谁(openid)，来自谁(公众账号ID)
            XML += "<CreateTime>" + ConvertDateTimeInt(DateTime.Now) + "</CreateTime>";//回复时间戳
            XML += "<MsgType><![CDATA[text]]></MsgType>";//回复类型文本
            if (Event == "subscribe")
            {
                XML += "<Content><![CDATA[感谢关注，茫茫人海中，能与您在此相识是我的荣幸。]]></Content><FuncFlag>0</FuncFlag></xml>";//回复内容 FuncFlag设置为1的时候，自动星标刚才接收到的消息，适合活动统计使用
            }
            else {
                XML += "<Content><![CDATA[谢谢您的包容，祝您生活愉快，身体健康。]]></Content><FuncFlag>0</FuncFlag></xml>";
            }
            return XML;
        } 
        #endregion

        #region 时间戳处理
        /// <summary>
        /// DateTime时间格式转换为Unix时间戳格式
        /// </summary>
        /// <param name="time"> DateTime时间格式</param>
        /// <returns>Unix时间戳格式</returns>
        public static int ConvertDateTimeInt(System.DateTime time)
        {
            System.DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1));
            return (int)(time - startTime).TotalSeconds;
        } 
        #endregion

        #region 暂时放弃/参考的代码片
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
        #endregion


    }
}