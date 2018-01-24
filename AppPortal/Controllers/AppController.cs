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
using System.Data.Entity;
using System.Threading.Tasks;

namespace Portal.Controllers
{
    public class AppController : ApiController
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        private List<string> OtpAllowedServiceCodes = new List<string>() { /*"Soltan", */ "DonyayeAsatir", "MenchBaz", "Soraty", "DefendIran", "AvvalYad", "BehAmooz500" };
        private List<string> AppMessageAllowedServiceCode = new List<string>() { /*"Soltan",*/ "ShahreKalameh", "DonyayeAsatir", "Tamly", "JabehAbzar", "ShenoYad", "FitShow", "Takavar", "MenchBaz", "AvvalPod", "AvvalYad", "Soraty", "DefendIran", "TahChin", "Nebula", "Dezhban", "MusicYad", "Phantom", "Medio", "BehAmooz500", "ShenoYad500", "Tamly500", "AvvalPod500" };
        private List<string> VerificactionAllowedServiceCode = new List<string>() { /*"Soltan",*/ "ShahreKalameh", "DonyayeAsatir", "Tamly", "JabehAbzar", "ShenoYad", "FitShow", "Takavar", "MenchBaz", "AvvalPod", "AvvalYad", "Soraty", "DefendIran", "TahChin", "Nebula", "Dezhban", "MusicYad", "Phantom", "Medio", "BehAmooz500", "ShenoYad500", "Tamly500", "AvvalPod500" };
        private List<string> TimeBasedServices = new List<string>() { "ShahreKalameh", "Tamly", "JabehAbzar", "ShenoYad", "FitShow", "Takavar", "AvvalPod", "TahChin", "Nebula", "Dezhban", "MusicYad", "Phantom", "Medio", "ShenoYad500", "Tamly500", "AvvalPod500" };
        private List<string> PriceBasedServices = new List<string>() { /*"Soltan",*/ "DonyayeAsatir", "MenchBaz", "Soraty", "DefendIran", "AvvalYad", "BehAmooz500" };

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> OtpCharge([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            try
            {
                result.MobileNumber = messageObj.MobileNumber;
                var hash = SharedLibrary.Security.GetSha256Hash("OtpCharge" + messageObj.ServiceCode + messageObj.MobileNumber);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
                if (messageObj.ServiceCode == "NabardGah")
                    messageObj.ServiceCode = "Soltan";
                else if (!OtpAllowedServiceCodes.Contains(messageObj.ServiceCode) && messageObj.Price.Value > 7) // Hub use price 5 and 6 for sub and unsub
                    result.Status = "This ServiceCode does not have permission for OTP operation";
                else
                {
                    messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
                    if (messageObj.MobileNumber == "Invalid Mobile Number")
                        result.Status = "Invalid Mobile Number";
                    else
                    {
                        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
                        if (service == null)
                            result.Status = "Invalid serviceCode";
                        else
                        {
                            var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);
                            if (service.ServiceCode == "Soltan")
                            {
                                using (var entity = new SoltanLibrary.Models.SoltanEntities())
                                {
                                    var imiChargeCode = new SoltanLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else if (messageObj.Price.Value == -1)
                                    {
                                        messageObj.Price = 0;
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                    }
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new SoltanLibrary.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "MenchBaz")
                            {
                                using (var entity = new MenchBazLibrary.Models.MenchBazEntities())
                                {
                                    var imiChargeCode = new MenchBazLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else if (messageObj.Price.Value == -1)
                                    {
                                        messageObj.Price = 0;
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                    }
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new MenchBazLibrary.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "DonyayeAsatir")
                            {
                                using (var entity = new DonyayeAsatirLibrary.Models.DonyayeAsatirEntities())
                                {
                                    var imiChargeCode = new DonyayeAsatirLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else if (messageObj.Price.Value == -1)
                                    {
                                        messageObj.Price = 0;
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                    }
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new DonyayeAsatirLibrary.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "AvvalPod")
                            {
                                using (var entity = new AvvalPodLibrary.Models.AvvalPodEntities())
                                {
                                    var imiChargeCode = new AvvalPodLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else if (messageObj.Price.Value == -1)
                                    {
                                        messageObj.Price = 0;
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                    }
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new AvvalPodLibrary.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "AvvalPod500")
                            {
                                using (var entity = new AvvalPod500Library.Models.AvvalPod500Entities())
                                {
                                    var imiChargeCode = new AvvalPod500Library.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else if (messageObj.Price.Value == -1)
                                    {
                                        messageObj.Price = 0;
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                    }
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new AvvalPod500Library.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "AvvalYad")
                            {
                                using (var entity = new AvvalYadLibrary.Models.AvvalYadEntities())
                                {
                                    var imiChargeCode = new AvvalPodLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else if (messageObj.Price.Value == -1)
                                    {
                                        messageObj.Price = 0;
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                    }
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new AvvalYadLibrary.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "BehAmooz500")
                            {
                                using (var entity = new BehAmooz500Library.Models.BehAmooz500Entities())
                                {
                                    var imiChargeCode = new BehAmooz500Library.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else if (messageObj.Price.Value == -1)
                                    {
                                        messageObj.Price = 0;
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                    }
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new BehAmooz500Library.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "FitShow")
                            {
                                using (var entity = new FitShowLibrary.Models.FitShowEntities())
                                {
                                    var imiChargeCode = new FitShowLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else if (messageObj.Price.Value == -1)
                                    {
                                        messageObj.Price = 0;
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                    }
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new FitShowLibrary.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "JabehAbzar")
                            {
                                using (var entity = new JabehAbzarLibrary.Models.JabehAbzarEntities())
                                {
                                    var imiChargeCode = new JabehAbzarLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else if (messageObj.Price.Value == -1)
                                    {
                                        messageObj.Price = 0;
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                    }
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new JabehAbzarLibrary.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "ShenoYad")
                            {
                                using (var entity = new ShenoYadLibrary.Models.ShenoYadEntities())
                                {
                                    var imiChargeCode = new ShenoYadLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else if (messageObj.Price.Value == -1)
                                    {
                                        messageObj.Price = 0;
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                    }
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new ShenoYadLibrary.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "ShenoYad500")
                            {
                                using (var entity = new ShenoYad500Library.Models.ShenoYad500Entities())
                                {
                                    var imiChargeCode = new ShenoYad500Library.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else if (messageObj.Price.Value == -1)
                                    {
                                        messageObj.Price = 0;
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                    }
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new ShenoYad500Library.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Takavar")
                            {
                                using (var entity = new TakavarLibrary.Models.TakavarEntities())
                                {
                                    var imiChargeCode = new TakavarLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else if (messageObj.Price.Value == -1)
                                    {
                                        messageObj.Price = 0;
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                    }
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new TakavarLibrary.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Tamly")
                            {
                                using (var entity = new TamlyLibrary.Models.TamlyEntities())
                                {
                                    var imiChargeCode = new TamlyLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else if (messageObj.Price.Value == -1)
                                    {
                                        messageObj.Price = 0;
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                    }
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new TamlyLibrary.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Tamly500")
                            {
                                using (var entity = new Tamly500Library.Models.Tamly500Entities())
                                {
                                    var imiChargeCode = new Tamly500Library.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else if (messageObj.Price.Value == -1)
                                    {
                                        messageObj.Price = 0;
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                    }
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new Tamly500Library.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Dezhban")
                            {
                                using (var entity = new DezhbanLibrary.Models.DezhbanEntities())
                                {
                                    var imiChargeCode = new DezhbanLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else if (messageObj.Price.Value == -1)
                                    {
                                        messageObj.Price = 0;
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                    }
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new TamlyLibrary.Models.Singlecharge();
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "ShahreKalameh")
                            {
                                using (var entity = new ShahreKalamehLibrary.Models.ShahreKalamehEntities())
                                {
                                    var imiChargeCode = new ShahreKalamehLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj.Price = 5; //5 is Sub for hub
                                    else if (messageObj.Price.Value == -1)
                                        messageObj.Price = 6; //6 is Unsub for hub
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new ShahreKalamehLibrary.Models.Singlecharge();
                                        string aggregatorName = "Hub";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.HubOtpChargeRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Soraty")
                            {
                                using (var entity = new SoratyLibrary.Models.SoratyEntities())
                                {
                                    var imiChargeCode = new SoratyLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj.Price = 5; //5 is Sub for hub
                                    else if (messageObj.Price.Value == -1)
                                        messageObj.Price = 6; //6 is Unsub for hub
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new SoratyLibrary.Models.Singlecharge();
                                        string aggregatorName = "Hub";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.HubOtpChargeRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "DefendIran")
                            {
                                using (var entity = new DefendIranLibrary.Models.DefendIranEntities())
                                {
                                    var imiChargeCode = new SoratyLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj.Price = 5; //5 is Sub for hub
                                    else if (messageObj.Price.Value == -1)
                                        messageObj.Price = 6; //6 is Unsub for hub
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new SoratyLibrary.Models.Singlecharge();
                                        string aggregatorName = "Hub";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.HubOtpChargeRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "SepidRood")
                            {
                                using (var entity = new SepidRoodLibrary.Models.SepidRoodEntities())
                                {
                                    var imiChargeCode = new SepidRoodLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else if (messageObj.Price.Value == -1)
                                    {
                                        messageObj.Price = 0;
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                    }
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new SoratyLibrary.Models.Singlecharge();
                                        string aggregatorName = "PardisImi";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.PardisImiOtpChargeRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Nebula")
                            {
                                using (var entity = new NebulaLibrary.Models.NebulaEntities())
                                {
                                    var imiChargeCode = new NebulaLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else if (messageObj.Price.Value == -1)
                                    {
                                        messageObj.Price = 0;
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                    }
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new NebulaLibrary.Models.Singlecharge();
                                        string aggregatorName = "MobinOne";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.MobinOneOTPRequest(entity, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Phantom")
                            {
                                Type entityType = typeof(PhantomLibrary.Models.PhantomEntities);
                                using (var entity = new PhantomLibrary.Models.PhantomEntities())
                                {
                                    var imiChargeCode = new PhantomLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else if (messageObj.Price.Value == -1)
                                    {
                                        messageObj.Price = 0;
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                    }
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new PhantomLibrary.Models.Singlecharge();
                                        string aggregatorName = "MobinOneMapfa";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.MapfaOTPRequest(entityType, singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Medio")
                            {
                                using (var entity = new MedioLibrary.Models.MedioEntities())
                                {
                                    var imiChargeCode = new MedioLibrary.Models.ImiChargeCode();
                                    if (messageObj.Price.Value == 0)
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                                    else if (messageObj.Price.Value == -1)
                                    {
                                        messageObj.Price = 0;
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                                    }
                                    else
                                        messageObj = SharedLibrary.MessageHandler.SetImiChargeInfo(entity, imiChargeCode, messageObj, messageObj.Price.Value, 0, null);
                                    if (messageObj.Price == null)
                                        result.Status = "Invalid Price";
                                    else
                                    {
                                        var singleCharge = new MedioLibrary.Models.Singlecharge();
                                        string aggregatorName = "MobinOneMapfa";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.MapfaOTPRequest(typeof(MedioLibrary.Models.MedioEntities), singleCharge, messageObj, serviceAdditionalInfo);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else
                                result.Status = "Service does not defined";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Excepiton in OtpCharge method: ", e);
            }
            var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<HttpResponseMessage> OtpConfirm([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            try
            {
                result.MobileNumber = messageObj.MobileNumber;
                var hash = SharedLibrary.Security.GetSha256Hash("OtpConfirm" + messageObj.ServiceCode + messageObj.MobileNumber);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
                else
                {
                    if (messageObj.ServiceCode == "NabardGah")
                        messageObj.ServiceCode = "Soltan";
                    messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
                    if (messageObj.MobileNumber == "Invalid Mobile Number")
                        result.Status = "Invalid Mobile Number";
                    else
                    {

                        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
                        if (service == null)
                            result.Status = "Invalid serviceId";
                        else
                        {
                            var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id);
                            if (service.ServiceCode == "Soltan")
                            {
                                using (var entity = new SoltanLibrary.Models.SoltanEntities())
                                {
                                    var singleCharge = new SoltanLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "MenchBaz")
                            {
                                using (var entity = new MenchBazLibrary.Models.MenchBazEntities())
                                {
                                    var singleCharge = new MenchBazLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "AvvalPod")
                            {
                                using (var entity = new AvvalPodLibrary.Models.AvvalPodEntities())
                                {
                                    var singleCharge = new AvvalPodLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "AvvalPod500")
                            {
                                using (var entity = new AvvalPod500Library.Models.AvvalPod500Entities())
                                {
                                    var singleCharge = new AvvalPod500Library.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "AvvalYad")
                            {
                                using (var entity = new AvvalYadLibrary.Models.AvvalYadEntities())
                                {
                                    var singleCharge = new AvvalYadLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "BehAmooz500")
                            {
                                using (var entity = new BehAmooz500Library.Models.BehAmooz500Entities())
                                {
                                    var singleCharge = new BehAmooz500Library.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "DonyayeAsatir")
                            {
                                using (var entity = new DonyayeAsatirLibrary.Models.DonyayeAsatirEntities())
                                {
                                    var singleCharge = new DonyayeAsatirLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "FitShow")
                            {
                                using (var entity = new FitShowLibrary.Models.FitShowEntities())
                                {
                                    var singleCharge = new FitShowLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "JabehAbzar")
                            {
                                using (var entity = new JabehAbzarLibrary.Models.JabehAbzarEntities())
                                {
                                    var singleCharge = new JabehAbzarLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "ShenoYad")
                            {
                                using (var entity = new ShenoYadLibrary.Models.ShenoYadEntities())
                                {
                                    var singleCharge = new ShenoYadLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "ShenoYad500")
                            {
                                using (var entity = new ShenoYad500Library.Models.ShenoYad500Entities())
                                {
                                    var singleCharge = new ShenoYad500Library.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Takavar")
                            {
                                using (var entity = new TakavarLibrary.Models.TakavarEntities())
                                {
                                    var singleCharge = new TakavarLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Tamly")
                            {
                                using (var entity = new TamlyLibrary.Models.TamlyEntities())
                                {
                                    var singleCharge = new TamlyLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Tamly500")
                            {
                                using (var entity = new Tamly500Library.Models.Tamly500Entities())
                                {
                                    var singleCharge = new Tamly500Library.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Dezhban")
                            {
                                using (var entity = new DezhbanLibrary.Models.DezhbanEntities())
                                {
                                    var singleCharge = new DezhbanLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Telepromo";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.TelepromoOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "ShahreKalameh")
                            {
                                using (var entity = new ShahreKalamehLibrary.Models.ShahreKalamehEntities())
                                {
                                    var singleCharge = new ShahreKalamehLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Hub";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.HubOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Soraty")
                            {
                                using (var entity = new SoratyLibrary.Models.SoratyEntities())
                                {
                                    var singleCharge = new SoratyLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Hub";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.HubOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "DefendIran")
                            {
                                using (var entity = new DefendIranLibrary.Models.DefendIranEntities())
                                {
                                    var singleCharge = new DefendIranLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "Hub";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.HubOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "SepidRood")
                            {
                                using (var entity = new SepidRoodLibrary.Models.SepidRoodEntities())
                                {
                                    var singleCharge = new SepidRoodLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "PardisImi";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.PardisImiOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Nebula")
                            {
                                using (var entity = new NebulaLibrary.Models.NebulaEntities())
                                {
                                    var singleCharge = new NebulaLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "MobinOne";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.MobinOneOTPConfirm(entity, singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Phantom")
                            {
                                using (var entity = new PhantomLibrary.Models.PhantomEntities())
                                {
                                    var singleCharge = new PhantomLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "MobinOneMapfa";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.MapfaOTPConfirm(typeof(PhantomLibrary.Models.PhantomEntities), singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else if (service.ServiceCode == "Medio")
                            {
                                using (var entity = new MedioLibrary.Models.MedioEntities())
                                {
                                    var singleCharge = new MedioLibrary.Models.Singlecharge();
                                    singleCharge = SharedLibrary.MessageHandler.GetOTPRequestId(entity, messageObj);
                                    if (singleCharge == null)
                                        result.Status = "No Otp Request Found";
                                    else
                                    {
                                        string aggregatorName = "MobinOneMapfa";
                                        var serviceAdditionalInfo = SharedLibrary.ServiceHandler.GetAdditionalServiceInfoForSendingMessage(messageObj.ServiceCode, aggregatorName);
                                        singleCharge = await SharedLibrary.MessageSender.MapfaOTPConfirm(typeof(MedioLibrary.Models.MedioEntities), singleCharge, messageObj, serviceAdditionalInfo, messageObj.ConfirmCode);
                                        result.Status = singleCharge.Description;
                                    }
                                }
                            }
                            else
                                result.Status = "Service does not defined";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Excepiton in OtpConfirm method: ", e);
            }
            var json = JsonConvert.SerializeObject(result);
            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(json, System.Text.Encoding.UTF8, "text/plain");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage AppMessage([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            result.MobileNumber = messageObj.MobileNumber;
            try
            {
                var hash = SharedLibrary.Security.GetSha256Hash("AppMessage" + messageObj.ServiceCode + messageObj.MobileNumber);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
                else
                {
                    if (messageObj.ServiceCode == "NabardGah")
                        messageObj.ServiceCode = "Soltan";
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
                    if (messageObj.MobileNumber == "Invalid Mobile Number")
                        result.Status = "Invalid Mobile Number";
                    else if (!AppMessageAllowedServiceCode.Contains(messageObj.ServiceCode))
                        result.Status = "This ServiceCode does not have permission";
                    else
                    {
                        messageObj.ShortCode = SharedLibrary.MessageHandler.ValidateShortCode(messageObj.ShortCode);
                        messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-FromApp" : null;
                        SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                        result.Status = "Success";
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in AppMessage:" + e);
                result.Status = "General error occurred";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage IsUserSubscribed([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            result.MobileNumber = messageObj.MobileNumber;
            try
            {
                var hash = SharedLibrary.Security.GetSha256Hash("IsUserSubscribed" + messageObj.ServiceCode + messageObj.MobileNumber);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
                else
                {
                    if (messageObj.ServiceCode == "NabardGah")
                        messageObj.ServiceCode = "Soltan";
                    messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
                    if (messageObj.MobileNumber == "Invalid Mobile Number")
                        result.Status = "Invalid Mobile Number";
                    else if (!VerificactionAllowedServiceCode.Contains(messageObj.ServiceCode))
                        result.Status = "This ServiceCode does not have permission";
                    else
                    {
                        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
                        if (service == null)
                        {
                            result.Status = "Invalid ServiceCode";
                        }
                        else
                        {
                            var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(messageObj.MobileNumber, messageObj.ServiceId);
                            if (subscriber != null && subscriber.DeactivationDate == null)
                                result.Status = "Subscribed";
                            else
                                result.Status = "NotSubscribed";
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in IsUserSubscribed:" + e);
                result.Status = "General error occurred";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage Verification([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            result.MobileNumber = messageObj.MobileNumber;
            try
            {
                var hash = SharedLibrary.Security.GetSha256Hash("Verification" + messageObj.ServiceCode + messageObj.MobileNumber);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
                else
                {
                    if (messageObj.ServiceCode == "NabardGah")
                        messageObj.ServiceCode = "Soltan";
                    messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
                    if (messageObj.MobileNumber == "Invalid Mobile Number")
                        result.Status = "Invalid Mobile Number";
                    else if (!VerificactionAllowedServiceCode.Contains(messageObj.ServiceCode))
                        result.Status = "This ServiceCode does not have permission";
                    else
                    {
                        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
                        if (service == null)
                        {
                            result.Status = "Invalid ServiceCode";
                        }
                        else
                        {
                            messageObj.ServiceId = service.Id;
                            messageObj.ShortCode = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id).ShortCode;
                            var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(messageObj.MobileNumber, messageObj.ServiceId);
                            if (subscriber != null && subscriber.DeactivationDate == null)
                            {
                                Random random = new Random();
                                var verficationId = random.Next(1000, 9999).ToString();
                                messageObj.Content = "SendVerification-" + verficationId;
                                if (messageObj.ServiceCode == "Tamly500" || messageObj.ServiceCode == "ShenoYad500" || messageObj.ServiceCode == "AvvalPod500" || messageObj.ServiceCode == "BehAmooz500")
                                    messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-New500-AppVerification" : null;
                                else
                                    messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-AppVerification" : null;
                                SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                                result.Status = "Subscribed";
                                result.ActivationCode = verficationId;
                            }
                            else
                            {
                                if (service.ServiceCode == "DonyayeAsatir")
                                {
                                    var sub = SharedLibrary.HandleSubscription.GetSubscriber(messageObj.MobileNumber, 10004);
                                    if (sub != null && sub.DeactivationDate == null)
                                    {
                                        messageObj.ServiceId = 10004;
                                        messageObj.ShortCode = "307229";
                                        Random random = new Random();
                                        var verficationId = random.Next(1000, 9999).ToString();
                                        messageObj.Content = "SendVerification-" + verficationId;
                                        messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-AppVerification" : null;
                                        SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                                        result.Status = "Subscribed";
                                        result.ActivationCode = verficationId;
                                    }
                                    else
                                    {
                                        messageObj.Content = "SendServiceSubscriptionHelp";
                                        if (messageObj.ServiceCode == "Tamly500" || messageObj.ServiceCode == "ShenoYad500" || messageObj.ServiceCode == "AvvalPod500" || messageObj.ServiceCode == "BehAmooz500")
                                            messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-New500-Verification" : null;
                                        else
                                            messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-Verification" : null;
                                        SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                                        result.Status = "NotSubscribed";
                                        result.ActivationCode = null;
                                    }
                                }
                                else
                                {
                                    messageObj.Content = "SendServiceSubscriptionHelp";
                                    if (messageObj.ServiceCode == "Tamly500" || messageObj.ServiceCode == "ShenoYad500" || messageObj.ServiceCode == "AvvalPod500" || messageObj.ServiceCode == "BehAmooz500")
                                        messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-New500-Verification" : null;
                                    else
                                        messageObj.ReceivedFrom = HttpContext.Current != null ? HttpContext.Current.Request.UserHostAddress + "-Verification" : null;
                                    SharedLibrary.MessageHandler.SaveReceivedMessage(messageObj);
                                    result.Status = "NotSubscribed";
                                    result.ActivationCode = null;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in Verification:" + e);
                result.Status = "General error occurred";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage SubscriberStatus([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            result.MobileNumber = messageObj.MobileNumber;
            try
            {
                var hash = SharedLibrary.Security.GetSha256Hash("SubscriberStatus" + messageObj.ServiceCode + messageObj.MobileNumber);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
                else
                {
                    if (messageObj.ServiceCode == "NabardGah")
                        messageObj.ServiceCode = "Soltan";
                    messageObj.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(messageObj.MobileNumber);
                    if (messageObj.MobileNumber == "Invalid Mobile Number")
                        result.Status = "Invalid Mobile Number";
                    else if (!VerificactionAllowedServiceCode.Contains(messageObj.ServiceCode))
                        result.Status = "This ServiceCode does not have permission";
                    else
                    {
                        var service = SharedLibrary.ServiceHandler.GetServiceFromServiceCode(messageObj.ServiceCode);
                        if (service == null)
                        {
                            result.Status = "Invalid ServiceCode";
                        }
                        else
                        {
                            messageObj.ServiceId = service.Id;
                            messageObj.ShortCode = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(service.Id).ShortCode;
                            var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(messageObj.MobileNumber, messageObj.ServiceId);
                            var daysLeft = 0;
                            var pricePayed = -1;
                            if (messageObj.ServiceCode == "Soltan")
                            {
                                using (var entity = new SoltanLibrary.Models.SoltanEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                        pricePayed = -1;
                                    else
                                    {
                                        var originalPriceBalancedForInAppRequest = singlechargeInstallment.PriceBalancedForInAppRequest;
                                        if (singlechargeInstallment.PriceBalancedForInAppRequest == null)
                                            singlechargeInstallment.PriceBalancedForInAppRequest = 0;
                                        pricePayed = singlechargeInstallment.PricePayed - singlechargeInstallment.PriceBalancedForInAppRequest.Value;
                                        singlechargeInstallment.PriceBalancedForInAppRequest += pricePayed;
                                        if (singlechargeInstallment.PriceBalancedForInAppRequest != originalPriceBalancedForInAppRequest)
                                        {
                                            entity.Entry(singlechargeInstallment).State = EntityState.Modified;
                                            entity.SaveChanges();
                                        }
                                    }
                                }
                            }
                            else if (messageObj.ServiceCode == "DonyayeAsatir")
                            {
                                using (var entity = new DonyayeAsatirLibrary.Models.DonyayeAsatirEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                    {
                                        var singlecharge = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                        if (singlecharge != null)
                                        {
                                            pricePayed = 0;
                                        }
                                        else
                                            pricePayed = -1;
                                    }
                                    else
                                    {
                                        var originalPriceBalancedForInAppRequest = singlechargeInstallment.PriceBalancedForInAppRequest;
                                        if (singlechargeInstallment.PriceBalancedForInAppRequest == null)
                                            singlechargeInstallment.PriceBalancedForInAppRequest = 0;
                                        pricePayed = singlechargeInstallment.PricePayed - singlechargeInstallment.PriceBalancedForInAppRequest.Value;
                                        singlechargeInstallment.PriceBalancedForInAppRequest += pricePayed;
                                        if (singlechargeInstallment.PriceBalancedForInAppRequest != originalPriceBalancedForInAppRequest)
                                        {
                                            entity.Entry(singlechargeInstallment).State = EntityState.Modified;
                                            entity.SaveChanges();
                                        }
                                    }
                                }
                            }
                            else if (messageObj.ServiceCode == "MenchBaz")
                            {
                                using (var entity = new MenchBazLibrary.Models.MenchBazEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                        pricePayed = -1;
                                    else
                                    {
                                        var originalPriceBalancedForInAppRequest = singlechargeInstallment.PriceBalancedForInAppRequest;
                                        if (singlechargeInstallment.PriceBalancedForInAppRequest == null)
                                            singlechargeInstallment.PriceBalancedForInAppRequest = 0;
                                        pricePayed = singlechargeInstallment.PricePayed - singlechargeInstallment.PriceBalancedForInAppRequest.Value;
                                        singlechargeInstallment.PriceBalancedForInAppRequest += pricePayed;
                                        if (singlechargeInstallment.PriceBalancedForInAppRequest != originalPriceBalancedForInAppRequest)
                                        {
                                            entity.Entry(singlechargeInstallment).State = EntityState.Modified;
                                            entity.SaveChanges();
                                        }
                                    }
                                }
                            }
                            else if (messageObj.ServiceCode == "Soraty")
                            {
                                using (var entity = new SoratyLibrary.Models.SoratyEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                        pricePayed = -1;
                                    else
                                    {
                                        var originalPriceBalancedForInAppRequest = singlechargeInstallment.PriceBalancedForInAppRequest;
                                        if (singlechargeInstallment.PriceBalancedForInAppRequest == null)
                                            singlechargeInstallment.PriceBalancedForInAppRequest = 0;
                                        pricePayed = singlechargeInstallment.PricePayed - singlechargeInstallment.PriceBalancedForInAppRequest.Value;
                                        singlechargeInstallment.PriceBalancedForInAppRequest += pricePayed;
                                        if (singlechargeInstallment.PriceBalancedForInAppRequest != originalPriceBalancedForInAppRequest)
                                        {
                                            entity.Entry(singlechargeInstallment).State = EntityState.Modified;
                                            entity.SaveChanges();
                                        }
                                    }
                                }
                            }
                            else if (messageObj.ServiceCode == "DefendIran")
                            {
                                using (var entity = new DefendIranLibrary.Models.DefendIranEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                        pricePayed = -1;
                                    else
                                    {
                                        var originalPriceBalancedForInAppRequest = singlechargeInstallment.PriceBalancedForInAppRequest;
                                        if (singlechargeInstallment.PriceBalancedForInAppRequest == null)
                                            singlechargeInstallment.PriceBalancedForInAppRequest = 0;
                                        pricePayed = singlechargeInstallment.PricePayed - singlechargeInstallment.PriceBalancedForInAppRequest.Value;
                                        singlechargeInstallment.PriceBalancedForInAppRequest += pricePayed;
                                        if (singlechargeInstallment.PriceBalancedForInAppRequest != originalPriceBalancedForInAppRequest)
                                        {
                                            entity.Entry(singlechargeInstallment).State = EntityState.Modified;
                                            entity.SaveChanges();
                                        }
                                    }
                                }
                            }
                            else if (messageObj.ServiceCode == "ShahreKalameh")
                            {
                                using (var entity = new ShahreKalamehLibrary.Models.ShahreKalamehEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                    {
                                        var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                        if (installmentQueue != null)
                                            daysLeft = 30;
                                        else
                                            daysLeft = 0;
                                    }
                                    else
                                        daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                }
                            }
                            else if (messageObj.ServiceCode == "Tamly")
                            {
                                using (var entity = new TamlyLibrary.Models.TamlyEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                    {
                                        var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                        if (installmentQueue != null)
                                            daysLeft = 30;
                                        else
                                            daysLeft = 0;
                                    }
                                    else
                                        daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                }
                            }
                            else if (messageObj.ServiceCode == "Tamly500")
                            {
                                using (var entity = new Tamly500Library.Models.Tamly500Entities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                    {
                                        var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                        if (installmentQueue != null)
                                            daysLeft = 30;
                                        else
                                            daysLeft = 0;
                                    }
                                    else
                                        daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                }
                            }
                            else if (messageObj.ServiceCode == "Dezhban")
                            {
                                using (var entity = new DezhbanLibrary.Models.DezhbanEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now && o.IsUserCanceledTheInstallment != true).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                    {
                                        var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                        if (installmentQueue != null)
                                            daysLeft = 30;
                                        else
                                            daysLeft = 0;
                                    }
                                    else
                                        daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                }
                            }
                            else if (messageObj.ServiceCode == "TahChin")
                            {
                                using (var entity = new TahChinLibrary.Models.TahChinEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                    {
                                        var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                        if (installmentQueue != null)
                                            daysLeft = 30;
                                        else
                                            daysLeft = 0;
                                    }
                                    else
                                        daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                }
                            }
                            else if (messageObj.ServiceCode == "MusicYad")
                            {
                                using (var entity = new MusicYadLibrary.Models.MusicYadEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                    {
                                        var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                        if (installmentQueue != null)
                                            daysLeft = 30;
                                        else
                                            daysLeft = 0;
                                    }
                                    else
                                        daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                }
                            }
                            else if (messageObj.ServiceCode == "JabehAbzar")
                            {
                                using (var entity = new JabehAbzarLibrary.Models.JabehAbzarEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                    {
                                        var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                        if (installmentQueue != null)
                                            daysLeft = 30;
                                        else
                                            daysLeft = 0;
                                    }
                                    else
                                        daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                }
                            }
                            else if (messageObj.ServiceCode == "ShenoYad")
                            {
                                using (var entity = new ShenoYadLibrary.Models.ShenoYadEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                    {
                                        var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                        if (installmentQueue != null)
                                            daysLeft = 30;
                                        else
                                            daysLeft = 0;
                                    }
                                    else
                                        daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                }
                            }
                            else if (messageObj.ServiceCode == "ShenoYad500")
                            {
                                using (var entity = new ShenoYad500Library.Models.ShenoYad500Entities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                    {
                                        var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                        if (installmentQueue != null)
                                            daysLeft = 30;
                                        else
                                            daysLeft = 0;
                                    }
                                    else
                                        daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                }
                            }
                            else if (messageObj.ServiceCode == "FitShow")
                            {
                                using (var entity = new FitShowLibrary.Models.FitShowEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                    {
                                        var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                        if (installmentQueue != null)
                                            daysLeft = 30;
                                        else
                                            daysLeft = 0;
                                    }
                                    else
                                        daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                }
                            }
                            else if (messageObj.ServiceCode == "Takavar")
                            {
                                using (var entity = new TakavarLibrary.Models.TakavarEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                    {
                                        var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                        if (installmentQueue != null)
                                            daysLeft = 30;
                                        else
                                            daysLeft = 0;
                                    }
                                    else
                                        daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                }
                            }
                            else if (messageObj.ServiceCode == "AvvalPod")
                            {
                                using (var entity = new AvvalPodLibrary.Models.AvvalPodEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                    {
                                        var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                        if (installmentQueue != null)
                                            daysLeft = 30;
                                        else
                                            daysLeft = 0;
                                    }
                                    else
                                        daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                }
                            }
                            else if (messageObj.ServiceCode == "AvvalPod500")
                            {
                                using (var entity = new AvvalPod500Library.Models.AvvalPod500Entities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                    {
                                        var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                        if (installmentQueue != null)
                                            daysLeft = 30;
                                        else
                                            daysLeft = 0;
                                    }
                                    else
                                        daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                }
                            }
                            else if (messageObj.ServiceCode == "AvvalYad")
                            {
                                using (var entity = new AvvalYadLibrary.Models.AvvalYadEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                        pricePayed = -1;
                                    else
                                    {
                                        var originalPriceBalancedForInAppRequest = singlechargeInstallment.PriceBalancedForInAppRequest;
                                        if (singlechargeInstallment.PriceBalancedForInAppRequest == null)
                                            singlechargeInstallment.PriceBalancedForInAppRequest = 0;
                                        pricePayed = singlechargeInstallment.PricePayed - singlechargeInstallment.PriceBalancedForInAppRequest.Value;
                                        singlechargeInstallment.PriceBalancedForInAppRequest += pricePayed;
                                        if (singlechargeInstallment.PriceBalancedForInAppRequest != originalPriceBalancedForInAppRequest)
                                        {
                                            entity.Entry(singlechargeInstallment).State = EntityState.Modified;
                                            entity.SaveChanges();
                                        }
                                    }
                                }
                            }
                            else if (messageObj.ServiceCode == "BehAmooz500")
                            {
                                using (var entity = new BehAmooz500Library.Models.BehAmooz500Entities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                        pricePayed = -1;
                                    else
                                    {
                                        var originalPriceBalancedForInAppRequest = singlechargeInstallment.PriceBalancedForInAppRequest;
                                        if (singlechargeInstallment.PriceBalancedForInAppRequest == null)
                                            singlechargeInstallment.PriceBalancedForInAppRequest = 0;
                                        pricePayed = singlechargeInstallment.PricePayed - singlechargeInstallment.PriceBalancedForInAppRequest.Value;
                                        singlechargeInstallment.PriceBalancedForInAppRequest += pricePayed;
                                        if (singlechargeInstallment.PriceBalancedForInAppRequest != originalPriceBalancedForInAppRequest)
                                        {
                                            entity.Entry(singlechargeInstallment).State = EntityState.Modified;
                                            entity.SaveChanges();
                                        }
                                    }
                                }
                            }
                            else if (messageObj.ServiceCode == "Nebula")
                            {
                                using (var entity = new NebulaLibrary.Models.NebulaEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now && o.IsUserCanceledTheInstallment != true).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                    {
                                        var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                        if (installmentQueue != null)
                                            daysLeft = 30;
                                        else
                                            daysLeft = 0;
                                    }
                                    else
                                        daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                }
                            }
                            else if (messageObj.ServiceCode == "Phantom")
                            {
                                using (var entity = new PhantomLibrary.Models.PhantomEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                    {
                                        var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                        if (installmentQueue != null)
                                            daysLeft = 30;
                                        else
                                            daysLeft = 0;
                                    }
                                    else
                                        daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                }
                            }
                            else if (messageObj.ServiceCode == "Medio")
                            {
                                using (var entity = new MedioLibrary.Models.MedioEntities())
                                {
                                    var now = DateTime.Now;
                                    var singlechargeInstallment = entity.SinglechargeInstallments.Where(o => o.MobileNumber == messageObj.MobileNumber && DbFunctions.AddDays(o.DateCreated, 30) >= now).OrderByDescending(o => o.DateCreated).FirstOrDefault();
                                    if (singlechargeInstallment == null)
                                    {
                                        var installmentQueue = entity.SinglechargeWaitings.FirstOrDefault(o => o.MobileNumber == messageObj.MobileNumber);
                                        if (installmentQueue != null)
                                            daysLeft = 30;
                                        else
                                            daysLeft = 0;
                                    }
                                    else
                                        daysLeft = 30 - now.Subtract(singlechargeInstallment.DateCreated).Days;
                                }
                            }
                            if (daysLeft > 0 || pricePayed > -1)
                            {
                                result.Status = "Active";
                                if (daysLeft > 0)
                                    result.DaysLeft = daysLeft;
                                else
                                    result.PricePayed = pricePayed;
                            }
                            else
                            {
                                result.Status = "NotActive";
                                result.DaysLeft = null;
                                result.PricePayed = null;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SubscriberStatus:" + e);
                result.Status = "General error occurred";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage GetIrancellOtpUrl([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            try
            {
                var hash = SharedLibrary.Security.GetSha256Hash("GetIrancellOtpUrl" + messageObj.ServiceCode);
                if (messageObj.AccessKey != hash)
                    result.Status = "You do not have permission";
                else
                {
                    if (messageObj.ServiceCode != null && (messageObj.ServiceCode == "TahChin" || messageObj.ServiceCode == "MusicYad"))
                    {
                        string timestampParam = DateTime.Now.ToString("yyyyMMddhhmmss");
                        string requestIdParam = Guid.NewGuid().ToString();
                        var callBackParam = messageObj.Address;
                        var price = "3000";
                        var modeParam = "1"; //Web
                        var pageNo = 0;
                        var authKey = "393830313130303036333739";
                        var sign = "";
                        var cpId = "980110006379";
                        if (messageObj.ServiceCode == "TahChin")
                        {
                            var serviceId = SharedLibrary.ServiceHandler.GetServiceId(messageObj.ServiceCode).Value;
                            var serviceInfo = SharedLibrary.ServiceHandler.GetServiceInfoFromServiceId(serviceId);
                            pageNo = 146;
                            sign = SharedLibrary.HelpfulFunctions.IrancellSignatureGenerator(authKey, cpId, serviceInfo.AggregatorServiceId, price, timestampParam, requestIdParam);
                        }

                        var url = string.Format(@"http://92.42.51.91/CGGateway/Default.aspx?Timestamp={0}&RequestID={1}&pageno={2}&Callback={3}&Sign={4}&mode={5}"
                                                , timestampParam, requestIdParam, pageNo, callBackParam, sign, modeParam);
                        result.Status = "Success";
                        result.Description = url;
                    }
                    else if (messageObj.From != null)
                    {
                        result.Status = "Invalid Service Code";
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in GetIrancellOtpUrl:" + e);
                result.Status = "Error";
                result.Description = "General error occurred";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }

        [HttpPost]
        [AllowAnonymous]
        public HttpResponseMessage DecryptIrancellMessage([FromBody]MessageObject messageObj)
        {
            dynamic result = new ExpandoObject();
            try
            {
                result.Status = "Error";
                result.Description = "General error occurred";
                var authKey = "393830313130303036333739";
                var message = SharedLibrary.HelpfulFunctions.IrancellEncryptedResponse(messageObj.Content, authKey);
                var splitedMessage = message.Split('&');
                foreach (var item in splitedMessage)
                {
                    if (item.Contains("msisdn"))
                        result.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(item.Remove(0, 7));
                    else if (item.Contains("status"))
                    {
                        var status = item.Remove(0, 7);
                        if (status == "00000000")
                        {
                            result.Status = "Success";
                            result.Description = "Successful Subscription";
                        }
                        else if (status == "22007201" || status == "22007238")
                        {
                            result.Status = "Error";
                            result.Description = "Already Subscribed";
                        }
                        else if (status == "10001211")
                        {
                            result.Status = "Error";
                            result.Description = "ServiceID + IP not whitelisted or used only 3G services service ID";
                        }
                        else if (status == "22007306")
                        {
                            result.Status = "Error";
                            result.Description = "MSISDN Blacklist";
                        }
                        else if (status == "22007230")
                        {
                            result.Status = "Error";
                            result.Description = "cannot be subscribed to by a third party";
                        }
                        else if (status == "22007330")
                        {
                            result.Status = "Error";
                            result.Description = "The account balance is Insufficient.";
                        }
                        else if (status == "22007306")
                        {
                            result.Status = "Error";
                            result.Description = "The user is blacklisted and cannot Subscribe to the product.";
                        }
                        else if (status == "99999999")
                        {
                            result.Status = "Error";
                            result.Description = "Subscription attempt failed. Please try again.";
                        }
                        else if (status == "88888888")
                        {
                            result.Status = "Error";
                            result.Description = "Cancel Button Clicked";
                        }
                        else
                        {
                            result.Status = "Error";
                            result.Description = "Unknown status code: " + status;
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in DecryptIrancellMessage:" + e);
                result.Status = "Error";
                result.Description = "General error occurred";
            }
            var json = JsonConvert.SerializeObject(result);
            var response = Request.CreateResponse(HttpStatusCode.OK);
            response.Content = new StringContent(json, Encoding.UTF8, "application/json");
            return response;
        }
    }
}
