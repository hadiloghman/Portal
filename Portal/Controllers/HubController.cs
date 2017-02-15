﻿using System.Web.Http;
using SharedLibrary.Models;
using System.Web;
using System.Net.Http;
using System.Net;
using System.Collections.Generic;
using System.Linq;
using System.Dynamic;
using System.Text;
using Newtonsoft.Json;
using System;

namespace Portal.Controllers
{
    public class HubController : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage Message(string text, string from, string to, string smsId, string userId)
        {
            var messageObj = new MessageObject();
            messageObj.MobileNumber = from;
            messageObj.ShortCode = to;
            messageObj.Content = text;
            messageObj.MessageId = smsId;

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

        [HttpGet]
        [AllowAnonymous]
        public HttpResponseMessage SinglechargeDelivery(string ChargeId, string StatusId, string Recipient, string AppliedPrice, string TransactionCode, string description)
        {
            var singlechargeDelivery = new SinglechargeDelivery();
            singlechargeDelivery.DateReceived = DateTime.Now;
            singlechargeDelivery.MobileNumber = "0" + Recipient;
            singlechargeDelivery.ReferenceId = ChargeId;
            singlechargeDelivery.Status = StatusId;
            singlechargeDelivery.Description = description;
            singlechargeDelivery.IsProcessed = false;
            using(var entity = new PortalEntities())
            {
                entity.SinglechargeDeliveries.Add(singlechargeDelivery);
                entity.SaveChanges();
            }
            var result = "1";
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }
    }
}