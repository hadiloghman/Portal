﻿using SharedLibrary.Models;
using ShahreKalamehLibrary.Models;
using System;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;

namespace ShahreKalamehLibrary
{
    public class HandleMo
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public async static Task<bool> ReceivedMessage(MessageObject message, Service service)
        {
            bool isSucceeded = true;
            try
            {
                using (var entity = new ShahreKalamehEntities())
                {
                    var content = message.Content;
                    message.ServiceCode = service.ServiceCode;
                    message.ServiceId = service.Id;
                    var isUserWantsToUnsubscribe = ServiceHandler.CheckIfUserWantsToUnsubscribe(message.Content);
                    var isUserSendsSubscriptionKeyword = ServiceHandler.CheckIfUserSendsSubscriptionKeyword(message.Content, service);

                    var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate();
                    if (message.ReceivedFrom.Contains("FromApp") && !message.Content.All(char.IsDigit))
                    {
                        message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
                        MessageHandler.InsertMessageToQueue(message);
                        return isSucceeded;
                    }
                    else if (message.ReceivedFrom.Contains("AppVerification") && message.Content.Contains("sendverification"))
                    {
                        var verficationMessage = message.Content.Split('-');
                        message.Content = messagesTemplate.Where(o => o.Title == "VerificationMessage").Select(o => o.Content).FirstOrDefault();
                        message.Content = message.Content.Replace("{CODE}", verficationMessage[1]);
                        message = MessageHandler.SetImiChargeInfo(message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenSubscribed);
                        MessageHandler.InsertMessageToQueue(message);
                        return isSucceeded;
                    }
                    else if (message.Content == "00" || message.Content.ToLower().Contains("abc"))
                    {
                        var logId = MessageHandler.OtpLog(message.MobileNumber, "request", message.Content);
                        var result = await SharedLibrary.UsefulWebApis.MciOtpSendActivationCode(message.ServiceCode, message.MobileNumber, "0");
                        MessageHandler.OtpLogUpdate(logId, result.Status.ToString());
                        if (result.Status != "SUCCESS-Pending Confirmation")
                        {
                            if (result.Status == "Error" || result.Status == "Exception")
                                isSucceeded = false;
                            else
                            {
                                message.Content = "لطفا بعد از 5 دقیقه دوباره تلاش کنید.";
                                MessageHandler.InsertMessageToQueue(message);
                            }
                        }
                        return isSucceeded;
                    }
                    else if (message.Content.Length == 4 && message.Content.All(char.IsDigit))
                    {
                        var confirmCode = message.Content;
                        var logId = MessageHandler.OtpLog(message.MobileNumber, "confirm", confirmCode);
                        var result = await SharedLibrary.UsefulWebApis.MciOtpSendConfirmCode(message.ServiceCode, message.MobileNumber, confirmCode);
                        MessageHandler.OtpLogUpdate(logId, result.Status.ToString());
                        if (result.Status == "Error" || result.Status == "Exception")
                            isSucceeded = false;
                        else if (result.Status.ToString().Contains("NOT FOUND IN LAST 5MINS") || result.Status == "No Otp Request Found")
                        {
                            var logId2 = MessageHandler.OtpLog(message.MobileNumber, "request", message.Content);
                            var result2 = await SharedLibrary.UsefulWebApis.MciOtpSendActivationCode(message.ServiceCode, message.MobileNumber, "0");
                            MessageHandler.OtpLogUpdate(logId2, result2.Status.ToString());
                            message.Content = messagesTemplate.Where(o => o.Title == "WrongOtpConfirm").Select(o => o.Content).FirstOrDefault();
                            MessageHandler.InsertMessageToQueue(message);
                        }
                        else if (result.Status.ToString().Contains("PIN DOES NOT MATCH"))
                        {
                            message.Content = messagesTemplate.Where(o => o.Title == "WrongOtpConfirm").Select(o => o.Content).FirstOrDefault();
                            MessageHandler.InsertMessageToQueue(message);
                        }
                        return isSucceeded;
                    }

                    if (message.ReceivedFrom.Contains("Notify-Register"))
                        isUserSendsSubscriptionKeyword = true;
                    else if (message.ReceivedFrom.Contains("Notify-Unsubscription") || message.IsReceivedFromIntegratedPanel == true)
                        isUserWantsToUnsubscribe = true;

                    //if (isUserWantsToUnsubscribe == true)
                    //    SharedLibrary.HandleSubscription.UnsubscribeUserFromHubService(service.Id, message.MobileNumber);

                    if (isUserSendsSubscriptionKeyword == true || isUserWantsToUnsubscribe == true)
                    {
                        //if (isUserSendsSubscriptionKeyword == true && isUserWantsToUnsubscribe == false)
                        //{
                        //    var user = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                        //    if (user != null && user.DeactivationDate == null)
                        //    {
                        //        message = MessageHandler.SendServiceHelp(message, messagesTemplate);
                        //        MessageHandler.InsertMessageToQueue(message);
                        //        return;
                        //    }
                        //}
                        if (service.Enable2StepSubscription == true && isUserSendsSubscriptionKeyword == true)
                        {
                            string subscriberdUsedKeyword = ShahreKalamehLibrary.ServiceHandler.IsUserVerifedTheSubscription(message.MobileNumber, message.ServiceId, content);
                            if (subscriberdUsedKeyword == "")
                            {
                                //message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                                //message.Content = messagesTemplate.Where(o => o.Title == "SendVerifySubscriptionMessage").Select(o => o.Content).FirstOrDefault();
                                //MessageHandler.InsertMessageToQueue(message);
                                return isSucceeded;
                            }
                            else
                                content = message.Content = subscriberdUsedKeyword;
                        }
                        var serviceStatusForSubscriberState = SharedLibrary.HandleSubscription.HandleSubscriptionContent(message, service, isUserWantsToUnsubscribe);
                        if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                        {
                            if (message.IsReceivedFromIntegratedPanel)
                            {
                                message.SubUnSubMoMssage = "ارسال درخواست از طریق پنل تجمیعی غیر فعال سازی";
                                message.SubUnSubType = 2;
                            }
                            else
                            {
                                message.SubUnSubMoMssage = message.Content;
                                message.SubUnSubType = 1;
                            }
                        }
                        var subsciber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                        if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated)
                        {
                            Subscribers.CreateSubscriberAdditionalInfo(message.MobileNumber, service.Id);
                            Subscribers.AddSubscriptionPointIfItsFirstTime(message.MobileNumber, service.Id);
                            message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                            ContentManager.AddSubscriberToSinglechargeQueue(message.MobileNumber, content);
                        }
                        else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                        {
                            ContentManager.DeleteFromSinglechargeQueue(message.MobileNumber);
                            ServiceHandler.CancelUserInstallments(message.MobileNumber);
                            var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                            message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                        }
                        else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                        {
                            message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                            var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                            //Subscribers.SetIsSubscriberSendedOffReason(subscriberId.Value, false);
                            ContentManager.AddSubscriberToSinglechargeQueue(message.MobileNumber, content);
                        }
                        else
                            message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);

                        message.Content = MessageHandler.PrepareSubscriptionMessage(messagesTemplate, serviceStatusForSubscriberState);
                        MessageHandler.InsertMessageToQueue(message);
                        //if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                        //{
                        //    message.Content = content;
                        //    ContentManager.HandleSinglechargeContent(message, service, subsciber, messagesTemplate);
                        //}
                        return isSucceeded;
                    }
                    var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

                    if (subscriber == null)
                    {
                        message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                        MessageHandler.InsertMessageToQueue(message);
                        return isSucceeded;
                    }
                    message.SubscriberId = subscriber.Id;
                    if (subscriber.DeactivationDate != null)
                    {
                        message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                        MessageHandler.InsertMessageToQueue(message);
                        return isSucceeded;
                    }
                    message.Content = content;
                    ContentManager.HandleContent(message, service, subscriber, messagesTemplate);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ShahreKalameh ReceivedMessage:", e);
            }
            return isSucceeded;
        }

        public static Singlecharge ReceivedMessageForSingleCharge(MessageObject message, Service service)
        {
            message.Content = message.Price.ToString();
            var content = message.Content;
            var singlecharge = new Singlecharge();
            if (message.Content.All(char.IsDigit))
            {
                var price = Convert.ToInt32(message.Content);
                var imiObject = MessageHandler.GetImiChargeObjectFromPrice(price, null);
                message.Content = imiObject.ChargeCode.ToString();
            }
            var messagesTemplate = ServiceHandler.GetServiceMessagesTemplate();
            var isUserSendsSubscriptionKeyword = ServiceHandler.CheckIfUserSendsSubscriptionKeyword(message.Content, service);
            var isUserWantsToUnsubscribe = ServiceHandler.CheckIfUserWantsToUnsubscribe(message.Content);
            var subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

            if (subscriber == null)
                isUserSendsSubscriptionKeyword = true;
            if (isUserSendsSubscriptionKeyword == true || isUserWantsToUnsubscribe == true)
            {
                //if (isUserSendsSubscriptionKeyword == true && isUserWantsToUnsubscribe == false)
                //{
                //    var user = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                //    if (user != null && user.DeactivationDate == null)
                //    {
                //        message.Content = content;
                //        singlecharge = ContentManager.HandleSinglechargeContent(message, service, user, messagesTemplate);
                //        return singlecharge;
                //    }
                //}
                var serviceStatusForSubscriberState = SharedLibrary.HandleSubscription.HandleSubscriptionContent(message, service, isUserWantsToUnsubscribe);
                if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                {
                    if (message.IsReceivedFromIntegratedPanel)
                    {
                        message.SubUnSubMoMssage = "ارسال درخواست از طریق پنل تجمیعی غیر فعال سازی";
                        message.SubUnSubType = 2;
                    }
                    else
                    {
                        message.SubUnSubMoMssage = message.Content;
                        message.SubUnSubType = 1;
                    }
                }
                var subsciber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);
                if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated)
                {
                    Subscribers.CreateSubscriberAdditionalInfo(message.MobileNumber, service.Id);
                    Subscribers.AddSubscriptionPointIfItsFirstTime(message.MobileNumber, service.Id);
                    message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                }
                else if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                {
                    var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                    message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated);
                }
                else
                {
                    message = MessageHandler.SetImiChargeInfo(message, 0, 21, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated);
                    var subscriberId = SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId);
                    //Subscribers.SetIsSubscriberSendedOffReason(subscriberId.Value, false);
                }
                //message.Content = MessageHandler.PrepareSubscriptionMessage(messagesTemplate, serviceStatusForSubscriberState);
                //MessageHandler.InsertMessageToQueue(message);
                if (serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated || serviceStatusForSubscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                {
                    message.Content = content;
                    singlecharge = ContentManager.HandleSinglechargeContent(message, service, subsciber, messagesTemplate);
                }
                return singlecharge;
            }
            subscriber = SharedLibrary.HandleSubscription.GetSubscriber(message.MobileNumber, message.ServiceId);

            if (subscriber == null)
            {
                message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                MessageHandler.InsertMessageToQueue(message);
                return null;
            }
            message.SubscriberId = subscriber.Id;
            if (subscriber.DeactivationDate != null)
            {
                message = MessageHandler.InvalidContentWhenNotSubscribed(message, messagesTemplate);
                MessageHandler.InsertMessageToQueue(message);
                return null;
            }
            message.Content = content;
            singlecharge = ContentManager.HandleSinglechargeContent(message, service, subscriber, messagesTemplate);
            return singlecharge;
        }
    }
}