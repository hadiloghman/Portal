﻿using System.Web.Http;
using SharedLibrary.Models;
using System.Web;
using System.Net.Http;
using System.Net;

namespace Portal.Controllers
{
    public class ReceiveController : ApiController
    {
        // /Receive/Message?mobileNumber=09125612694&shortCode=2050&content=hi&receiveTime=22&messageId=45
        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Message([FromUri]MessageObject messageObj)
        {
            if (messageObj.Address != null)
            {
                messageObj.MobileNumber = messageObj.Address;
                messageObj.Content = messageObj.Message;
            }
            else if (messageObj.From != null)
            {
                messageObj.MobileNumber = messageObj.From;
                messageObj.ShortCode = messageObj.To;
            }
            messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
            string result = "";
            if (messageObj.MobileNumber == "Invalid Mobile Number")
                result = "-1";
            else
            {
                messageObj.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(messageObj.ShortCode);
                messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
                SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                result = "1";
            }
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        

        // /Receive/ReceiveMessage?mobileNumber=09125612694&shortCode=2050&content=hi&receiveTime=22&messageId=45
        [HttpPost]
        [AllowAnonymous]
        public string ReceiveMessage([FromBody]MessageObject messageObj)
        {
            if (messageObj.Address != null)
            {
                messageObj.MobileNumber = messageObj.Address;
                messageObj.Content = messageObj.Message;
            }
            else if (messageObj.From != null)
            {
                messageObj.MobileNumber = messageObj.From;
                messageObj.ShortCode = messageObj.To;
            }
            messageObj.ShortCode = "307229";
            messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
            if (messageObj.MobileNumber == "Invalid Mobile Number")
                return "-1";
            messageObj.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(messageObj.ShortCode);
            messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
            SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
            return "1";
        }

        // /Receive/ChargeUser?MobileNumber=09125612694&ShortCode=3071171&Content=1
        [HttpGet]
        [AllowAnonymous]
        public string ChargeUser([FromUri]MessageObject message)
        {
            message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
            if (message.ReceivedFrom == "138.68.38.140" || message.ReceivedFrom == "31.187.71.85" || message.ReceivedFrom == "138.68.152.71" || message.ReceivedFrom == "138.68.140.120" || message.ReceivedFrom == "178.62.51.95" || message.ReceivedFrom == "188.166.173.46")
            {
                message.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(message.MobileNumber);
                if (message.MobileNumber == "Invalid Mobile Number")
                    return "-1";
                message.ShortCode = "307229";
                message = SharedLibrary.MessageHandler.ValidateMessage(message);
                message.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress : null;
                message.ProcessStatus = (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend;
                message.MessageType = (int)SharedLibrary.MessageHandler.MessageType.OnDemand;
                message.IsReceivedFromIntegratedPanel = false;
                var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode("Soltan");
                if (service == null)
                    return "-2";
                message.ServiceId = service.Id;
                var singlecharge = SoltanLibrary.HandleMo.ReceivedMessageForSingleCharge(message, service);
                if (singlecharge == null)
                    return "-3";
                using (var entity = new SoltanLibrary.Models.SoltanEntities())
                {
                    entity.Singlecharges.Attach(singlecharge);
                    singlecharge.IsCalledFromInAppPurchase = true;
                    entity.Entry(singlecharge).State = System.Data.Entity.EntityState.Modified;
                    entity.SaveChanges();
                }
                if (singlecharge.IsSucceeded == true)
                    return "1";
                else if (singlecharge.IsSucceeded == false && singlecharge.Description.Contains("Insufficient balance"))
                    return "-6";
                else
                    return "-4";
            }
            else
                return "-5";
        }
    }
}