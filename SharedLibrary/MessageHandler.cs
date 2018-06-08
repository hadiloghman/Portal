﻿using System;
using System.Collections.Generic;
using System.Linq;
using SharedLibrary.Models;
using System.Text.RegularExpressions;
using System.Collections;
using System.Data.Entity;
using System.Threading.Tasks;
using System.Xml;
using System.Net;
using System.IO;

namespace SharedLibrary
{
    public class MessageHandler
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static void SaveReceivedMessage(MessageObject message)
        {
            using (var entity = new PortalEntities())
            {
                try
                {
                    var mo = new ReceievedMessage()
                    {
                        MobileNumber = message.MobileNumber,
                        ShortCode = message.ShortCode,
                        ReceivedTime = DateTime.Now,
                        PersianReceivedTime = Date.GetPersianDateTime(DateTime.Now),
                        MessageId = message.MessageId,
                        Content = (message.Content == null) ? " " : message.Content,
                        IsProcessed = false,
                        IsReceivedFromIntegratedPanel = (message.IsReceivedFromIntegratedPanel == null) ? false : message.IsReceivedFromIntegratedPanel,
                        IsReceivedFromWeb = (message.IsReceivedFromWeb == null) ? false : message.IsReceivedFromWeb,
                        ReceivedFrom = message.ReceivedFrom
                    };
                    entity.ReceievedMessages.Add(mo);
                    entity.SaveChanges();
                }
                catch (Exception e)
                {
                    logs.Error("Exception in SaveReceivedMessage: " + e);
                }
            }
        }

        public static long GetAggregatorId(MessageObject message)
        {
            using (var entity = new PortalEntities())
                return entity.ServiceInfoes.Where(o => o.ShortCode == message.ShortCode).FirstOrDefault().AggregatorId;
        }

        public static long GetAggregatorIdFromConfig(string AggregatorName)
        {
            using (var entity = new PortalEntities())
                return entity.Aggregators.Where(o => o.AggregatorName == AggregatorName).FirstOrDefault().Id;
        }

        public static void SaveDeliveryStatus(DeliveryObject deliveryObj)
        {
            using (var entity = new PortalEntities())
            {
                var delivery = new Delivery();
                delivery.ReferenceId = deliveryObj.ReferenceId;
                delivery.Status = deliveryObj.Status;
                delivery.Description = deliveryObj.ErrorMessage;
                delivery.DeliveryTime = DateTime.Now;
                delivery.IsProcessed = false;
                delivery.AggregatorId = deliveryObj.AggregatorId;
                entity.Deliveries.Add(delivery);
                entity.SaveChanges();
            }
        }

        public static MessageObject ValidateMessage(MessageObject message, List<SharedLibrary.Models.OperatorsPrefix> prefixs)
        {
            message.ShortCode = ValidateShortCode(message.ShortCode);
            message.Content = NormalizeContent(message.Content);
            message = GetSubscriberOperatorInfo(message, prefixs);
            return message;
        }

        public static string NormalizeContent(string content)
        {
            if (content == null)
                content = "";
            else
            {
                content = content.Trim();
                content = Regex.Replace(content, @"\s+", " ");
                content = content.ToLower();
                content = content.Replace('ك', 'ک');
                content = content.Replace('ي', 'ی');
                content = content.Replace("‏۱", "1");
                content = content.Replace('۱', '1');
                content = content.Replace('١', '1');
                content = content.Replace('٢', '2');
                content = content.Replace('۲', '2');
                content = content.Replace('۳', '3');
                content = content.Replace('٣', '3');
                content = content.Replace("‏۳", "3");
                content = content.Replace("‏‏٣", "3");
                content = content.Replace("‏۴", "4");
                content = content.Replace('۴', '4');
                content = content.Replace('٤', '4');
                content = content.Replace("‏۵", "5");
                content = content.Replace('۵', '5');
                content = content.Replace('٥', '5');
                content = content.Replace('۶', '6');
                content = content.Replace("‏۶", "6");
                content = content.Replace('٦', '6');
                content = content.Replace('٧', '7');
                content = content.Replace('۷', '7');
                content = content.Replace('٨', '8');
                content = content.Replace('۸', '8');
                content = content.Replace('۹', '9');
                content = content.Replace('٩', '9');
                content = content.Replace('٠', '0');
                content = content.Replace('۰', '0');
            }
            return content;
        }

        public static void InsertMessageToQueue(Type entityType, MessageObject message, Type autocharge, Type eventBase, Type onDemand)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                message.Content = HandleSpecialStrings(message.Content, message.Point, message.MobileNumber, message.ServiceId);
                if (message.MessageType == (int)SharedLibrary.MessageHandler.MessageType.AutoCharge)
                {
                    var messageBuffer = Convert.ChangeType(CreateMessageBuffer(autocharge, message), autocharge);
                    entity.AutochargeMessagesBuffers.Add(messageBuffer);
                }
                else if (message.MessageType == (int)SharedLibrary.MessageHandler.MessageType.EventBase)
                {
                    var messageBuffer = Convert.ChangeType(CreateMessageBuffer(eventBase, message), eventBase);
                    entity.EventbaseMessagesBuffers.Add(messageBuffer);
                }
                else
                {
                    var messageBuffer = Convert.ChangeType(CreateMessageBuffer(onDemand, message), onDemand);
                    entity.OnDemandMessagesBuffers.Add(messageBuffer);
                }
                entity.SaveChanges();
            }
        }

        public static void InsertBulkMessagesToQueue(Type entityType, List<MessageObject> messages, Type autocharge, Type eventBase, Type onDemand)
        {
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                int counter = 0;
                foreach (var message in messages)
                {
                    if (message.MessageType == (int)SharedLibrary.MessageHandler.MessageType.AutoCharge)
                    {
                        var messageBuffer = Convert.ChangeType(CreateMessageBuffer(autocharge, message), autocharge);
                        entity.AutochargeMessagesBuffers.Add(messageBuffer);
                    }
                    else if (message.MessageType == (int)SharedLibrary.MessageHandler.MessageType.EventBase)
                    {
                        var messageBuffer = Convert.ChangeType(CreateMessageBuffer(eventBase, message), eventBase);
                        entity.EventbaseMessagesBuffers.Add(messageBuffer);
                    }
                    else
                    {
                        var messageBuffer = Convert.ChangeType(CreateMessageBuffer(onDemand, message), onDemand);
                        entity.OnDemandMessagesBuffers.Add(messageBuffer);
                    }
                    if (counter > 1000)
                    {
                        counter = 0;
                        entity.SaveChanges();
                    }
                    counter++;
                }
                entity.SaveChanges();
            }
        }

        public static dynamic CreateMessageBuffer(Type messageType, MessageObject message)
        {
            if (message.AggregatorId == 0)
                message.AggregatorId = SharedLibrary.MessageHandler.GetAggregatorId(message);

            dynamic messageBuffer = Activator.CreateInstance(messageType);
            messageBuffer.Content = message.Content;
            messageBuffer.ContentId = message.ContentId;
            messageBuffer.ImiChargeCode = message.ImiChargeCode;
            messageBuffer.ImiChargeKey = message.ImiChargeKey;
            messageBuffer.ImiMessageType = message.ImiMessageType;
            messageBuffer.MobileNumber = message.MobileNumber;
            messageBuffer.MessagePoint = message.Point;
            messageBuffer.MessageType = message.MessageType;
            messageBuffer.ProcessStatus = message.ProcessStatus;
            messageBuffer.ServiceId = message.ServiceId;
            messageBuffer.DateAddedToQueue = DateTime.Now;
            messageBuffer.SubUnSubMoMssage = (message.SubUnSubMoMssage == null || message.SubUnSubMoMssage == "") ? "0" : message.SubUnSubMoMssage;
            messageBuffer.SubUnSubType = (message.SubUnSubType == null) ? 0 : message.SubUnSubType;
            messageBuffer.AggregatorId = message.AggregatorId;
            messageBuffer.Tag = message.Tag;
            messageBuffer.SubscriberId = message.SubscriberId == null ? SharedLibrary.HandleSubscription.GetSubscriberId(message.MobileNumber, message.ServiceId) : message.SubscriberId;
            messageBuffer.PersianDateAddedToQueue = SharedLibrary.Date.GetPersianDateTime(DateTime.Now);
            messageBuffer.Price = message.Price;
            return messageBuffer;
        }

        public static string HandleSpecialStrings(string content, int point, string mobileNumber, long serviceId)
        {
            using (var entity = new PortalEntities())
            {
                if (content == null || content == "")
                    return content;
                if (content.Contains("{SPOINT}"))
                {
                    var subscriberPoint = SharedLibrary.MessageHandler.GetSubscriberPoint(mobileNumber, serviceId);
                    subscriberPoint += point;
                    content = content.Replace("{SPOINT}", subscriberPoint.ToString());
                }
                if (content.Contains("{TPOINT}"))
                {
                    var subscriberPoint = SharedLibrary.MessageHandler.GetSubscriberPoint(mobileNumber, null);
                    subscriberPoint += point;
                    content = content.Replace("{TPOINT}", subscriberPoint.ToString());
                }
            }
            return content;
        }

        public static bool IsInBlackList(string mobileNumber, long serviceId)
        {
            bool result = false;
            try
            {
                using (var entity = new PortalEntities())
                {
                    var mobile = entity.BlackLists.Where(o => o.MobileNumber == mobileNumber).ToList();
                    if (mobile.FirstOrDefault(o => o.ServiceId == 0 || o.ServiceId == serviceId) != null)
                        result = true;
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in IsInBlackList: ", e);
            }
            return result;
        }

        public static string PrepareGeneralOffMessage(MessageObject message, List<Service> servicesThatUserSubscribedOnShortCode)
        {
            if (servicesThatUserSubscribedOnShortCode.Count == 0)
                message.Content = "کاربر گرامی شما عضو هیچ سرویسی بر روی سرشماره " + message.ShortCode + " نمی باشید.";
            if (servicesThatUserSubscribedOnShortCode.Count == 1)
            {
                var offKeyword = SharedLibrary.ServiceHandler.getFirstOnKeywordOfService(servicesThatUserSubscribedOnShortCode[0].OnKeywords) + " off";
                message.Content = "کاربر گرامی شما عضو سرویس " + servicesThatUserSubscribedOnShortCode[0].Name + " می باشید." + Environment.NewLine + "برای غیر فعال سازی سرویس کلمه " + offKeyword + " را به شماره " + message.ShortCode + " ارسال نمایید.";
            }
            else
            {
                message.Content = "کاربر گرامی شما عضو سرویس های";
                foreach (var service in servicesThatUserSubscribedOnShortCode)
                {
                    message.Content += " " + service.Name + "،";
                }
                message.Content = message.Content.TrimEnd('،');
                message.Content += " می باشید." + Environment.NewLine + "برای غیر فعال سازی سرویس";
                foreach (var service in servicesThatUserSubscribedOnShortCode)
                {
                    var offKeyword = SharedLibrary.ServiceHandler.getFirstOnKeywordOfService(service.OnKeywords) + " off";
                    message.Content += " " + service.Name + " کلمه " + offKeyword + "، ";
                }
                message.Content = message.Content.TrimEnd(' ');
                message.Content = message.Content.TrimEnd('،');
                message.Content += " را به شماره " + message.ShortCode + " ارسال نمایید.";
            }
            return message.Content;
        }

        public static long GetServiceIdFromUserMessage(string content, string shortCode)
        {
            long serviceId = 0;
            try
            {
                using (var entity = new PortalEntities())
                {
                    var serviceKeywords = entity.ServiceKeywords.FirstOrDefault(o => o.Keyword == content && o.ShortCode == shortCode);
                    if (serviceKeywords != null)
                        serviceId = serviceKeywords.ServiceId;
                    else
                    {
                        var services = (from s in entity.Services join sInfo in entity.ServiceInfoes on s.Id equals sInfo.ServiceId where sInfo.ShortCode == shortCode select new { ServiceId = s.Id, ServiceOnKeywords = s.OnKeywords }).AsEnumerable().ToList();
                        foreach (var service in services)
                        {
                            var serviceOnKeywords = service.ServiceOnKeywords.Split(',');
                            foreach (var onKeyword in serviceOnKeywords)
                            {
                                var trimmedOnKeyword = onKeyword.Trim();
                                if (content == trimmedOnKeyword)
                                    return service.ServiceId;
                                else
                                {
                                    var offkewords = SharedLibrary.ServiceHandler.ServiceOffKeywords();
                                    foreach (var offkeyword in offkewords)
                                    {
                                        if (content.Contains(offkeyword) && content.Contains(trimmedOnKeyword))
                                            return service.ServiceId;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in GetServiceIdFromUserMessage: " + e);
            }
            return serviceId;
        }

        public static string ValidateShortCode(string shortCode)
        {
            shortCode = shortCode.Replace(" ", "");
            if (shortCode.StartsWith("+"))
                shortCode = shortCode.TrimStart('+');
            if (shortCode.StartsWith("98"))
                shortCode = shortCode.Remove(0, 2);
            return shortCode;
        }

        public static string ValidateNumber(string mobileNumber)
        {
            if (mobileNumber == null)
                mobileNumber = "Invalid Mobile Number";
            if (mobileNumber.StartsWith("+"))
                mobileNumber = mobileNumber.TrimStart('+');
            if (mobileNumber.StartsWith("98"))
                mobileNumber = mobileNumber.Remove(0, 2);
            if (!mobileNumber.StartsWith("0"))
                mobileNumber = "0" + mobileNumber;
            if (!mobileNumber.StartsWith("09") || mobileNumber.Length != 11)
                mobileNumber = "Invalid Mobile Number";

            return mobileNumber;
        }

        public static string ValidateLandLineNumber(string number)
        {
            if (number == null)
                number = "Invalid Number";
            if (number.StartsWith("+"))
                number = number.TrimStart('+');
            if (number.StartsWith("98"))
                number = number.Remove(0, 2);
            if (!number.StartsWith("0"))
                number = "0" + number;
            if (number.StartsWith("09") || number.Length > 11)
                number = "Invalid Number";

            return number;
        }
        public static MessageObject GetSubscriberOperatorInfo(MessageObject message, List<SharedLibrary.Models.OperatorsPrefix> operatorPrefixes)
        {
            foreach (var operatorPrefixe in operatorPrefixes)
            {
                if (message.MobileNumber.StartsWith(operatorPrefixe.Prefix))
                {
                    message.MobileOperator = operatorPrefixe.OperatorId;
                    message.OperatorPlan = operatorPrefixe.OperatorPlan;
                    break;
                }
                else
                {
                    message.MobileOperator = (int)MobileOperators.Mci;
                    message.OperatorPlan = (int)OperatorPlan.Prepaid;
                }
            }
            return message;
        }

        public static MessageObject CreateMessage(Subscriber subscriber, string content, long contentId, MessageType messageType, ProcessStatus processStatus, int ImiMessageType, dynamic ImiChargeObject, long AggregatorId, int messagePoint, int? tag, int price)
        {
            var message = new MessageObject();
            message.Content = content;
            message.MobileNumber = subscriber.MobileNumber;
            message.SubscriberId = subscriber.Id;
            message.ContentId = contentId;
            message.MessageType = (int)messageType;
            message.ProcessStatus = (int)processStatus;
            message.ServiceId = subscriber.ServiceId;
            message.OperatorPlan = subscriber.OperatorPlan;
            message.MobileOperator = subscriber.MobileOperator;
            message.ImiMessageType = ImiMessageType;
            message.ImiChargeCode = ImiChargeObject.ChargeCode;
            message.ImiChargeKey = ImiChargeObject.ChargeKey;
            message.AggregatorId = AggregatorId;
            message.Point = messagePoint;
            message.Tag = tag;
            message.Price = price;
            return message;
        }

        public static MessageObject CreateMessageFromMessageBuffer(long? subscriberId, string mobileNumber, long serviceId, string content, long? contentId, MessageType messageType, ProcessStatus processStatus, int? ImiMessageType, int? imiChargeCode, string imiChargeKey, long AggregatorId, int messagePoint, int? tag, byte? subUnSubType, string subUnSubMoMessage, int price)
        {
            var message = new MessageObject();
            message.Content = content;
            message.MobileNumber = mobileNumber;
            message.SubscriberId = subscriberId;
            message.ContentId = contentId;
            message.MessageType = (int)messageType;
            message.ProcessStatus = (int)processStatus;
            message.ServiceId = serviceId;
            message.ImiMessageType = ImiMessageType;
            message.ImiChargeCode = imiChargeCode;
            message.ImiChargeKey = imiChargeKey;
            message.AggregatorId = AggregatorId;
            message.Point = messagePoint;
            message.Tag = tag;
            message.SubUnSubType = subUnSubType;
            message.SubUnSubMoMssage = subUnSubMoMessage;
            message.Price = price;
            return message;
        }
        public static int GetSubscriberPoint(string mobileNumber, long? serviceId)
        {
            int point = 0;
            using (var entity = new PortalEntities())
            {
                if (serviceId != null)
                    point = entity.SubscribersPoints.FirstOrDefault(o => o.MobileNumber == mobileNumber && o.ServiceId == serviceId).Point;
                else
                    point = entity.SubscribersPoints.Where(o => o.MobileNumber == mobileNumber).Select(o => o.Point).DefaultIfEmpty(0).Sum();
            }
            return point;
        }

        public static void SetSubscriberPoint(string mobileNumber, long serviceId, int point)
        {
            try
            {
                using (var entity = new PortalEntities())
                {
                    var pointObj = entity.SubscribersPoints.FirstOrDefault(o => o.MobileNumber == mobileNumber && o.ServiceId == serviceId);
                    pointObj.Point += point;
                    entity.Entry(pointObj).State = System.Data.Entity.EntityState.Modified;
                    entity.SaveChanges();
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in SetSubscriberPoint: ", e);
            }
        }

        public static dynamic GetOTPRequestId(dynamic entity, MessageObject message)
        {
            try
            {
                var singlecharge = ((IEnumerable<dynamic>)entity.Singlecharges).Where(o => o.MobileNumber == message.MobileNumber && o.Description == "SUCCESS-Pending Confirmation").OrderByDescending(o => o.DateCreated).FirstOrDefault();
                if (singlecharge != null)
                    return singlecharge;
                else
                    return null;
            }
            catch (Exception e)
            {
                logs.Error("Exception in GetOTPRequestId: ", e);
            }
            return null;
        }

        public static MessageObject SetImiChargeInfo(dynamic entity, dynamic imiChargeCode, MessageObject message, int price, int messageType, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState? subscriberState)
        {
            if (subscriberState == null && price > 0)
                imiChargeCode = ((IEnumerable<dynamic>)entity.ImiChargeCodes).FirstOrDefault(o => o.Price == price);
            else if (subscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Activated)
                imiChargeCode = ((IEnumerable<dynamic>)entity.ImiChargeCodes).FirstOrDefault(o => o.Price == price && o.Description == "Register");
            else if (subscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Deactivated)
                imiChargeCode = ((IEnumerable<dynamic>)entity.ImiChargeCodes).FirstOrDefault(o => o.Price == price && o.Description == "UnSubscription");
            else if (subscriberState == SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Renewal)
                imiChargeCode = ((IEnumerable<dynamic>)entity.ImiChargeCodes).FirstOrDefault(o => o.Price == price && o.Description == "Renewal");
            else
                imiChargeCode = ((IEnumerable<dynamic>)entity.ImiChargeCodes).FirstOrDefault(o => o.Price == price && o.Description == "Free");

            if (imiChargeCode != null)
            {
                message.ImiChargeCode = imiChargeCode.ChargeCode;
                message.ImiChargeKey = imiChargeCode.ChargeKey;
                message.ImiMessageType = messageType;
                message.Price = price;
            }
            return message;
        }

        public static MessageObject SendServiceSubscriptionHelp(dynamic entity, dynamic imiChargeCodes, MessageObject message, dynamic messagesTemplate)
        {
            message = SetImiChargeInfo(entity, imiChargeCodes, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
            message.Content = ((IEnumerable<dynamic>)messagesTemplate).Where(o => o.Title == "SendServiceSubscriptionHelp").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static MessageObject SendServiceOTPHelp(dynamic entity, dynamic imiChargeCodes, MessageObject message, dynamic messagesTemplate)
        {
            message = SetImiChargeInfo(entity, imiChargeCodes, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
            message.Content = ((IEnumerable<dynamic>)messagesTemplate).Where(o => o.Title == "SendServiceOTPHelp").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static MessageObject SendServiceOTPRequestExists(dynamic entity, dynamic imiChargeCodes, MessageObject message, dynamic messagesTemplate)
        {
            message = SetImiChargeInfo(entity, imiChargeCodes, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.Unspecified);
            message.Content = ((IEnumerable<dynamic>)messagesTemplate).Where(o => o.Title == "SendServiceOTPRequestExists").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static MessageObject EmptyContentWhenNotSubscribed(dynamic entity, dynamic imiChargeCodes, MessageObject message, dynamic messagesTemplate)
        {
            message = SetImiChargeInfo(entity, imiChargeCodes, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);
            message.Content = ((IEnumerable<dynamic>)messagesTemplate).Where(o => o.Title == "EmptyContentWhenNotSubscribed").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static MessageObject EmptyContentWhenSubscribed(dynamic entity, dynamic imiChargeCodes, MessageObject message, dynamic messagesTemplate)
        {
            message = SetImiChargeInfo(entity, imiChargeCodes, message, 0, 0, SharedLibrary.HandleSubscription.ServiceStatusForSubscriberState.InvalidContentWhenNotSubscribed);
            message.Content = ((IEnumerable<dynamic>)messagesTemplate).Where(o => o.Title == "EmptyContentWhenSubscribed").Select(o => o.Content).FirstOrDefault();
            return message;
        }

        public static dynamic GetUnprocessedMessages(Type entityType, MessageType messageType, int readSize)
        {
            var today = DateTime.Now.Date;
            var maxRetryCount = SharedLibrary.MessageSender.retryCountMax;
            var retryPauseBeforeSendByMinute = SharedLibrary.MessageSender.retryPauseBeforeSendByMinute;
            var retryTimeOut = DateTime.Now.AddMinutes(retryPauseBeforeSendByMinute);
            using (dynamic entity = Activator.CreateInstance(entityType))
            {
                entity.Configuration.AutoDetectChangesEnabled = false;

                if (messageType == MessageType.AutoCharge)
                    return ((IEnumerable<dynamic>)entity.AutochargeMessagesBuffers).Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend /*&& DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today*/ && (o.RetryCount == null || o.RetryCount <= maxRetryCount) && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)).Take(readSize).ToList();
                else if (messageType == MessageType.EventBase)
                    return ((IEnumerable<dynamic>)entity.EventbaseMessagesBuffers).Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend /*&& DbFunctions.TruncateTime(o.DateAddedToQueue).Value == today*/ && (o.RetryCount == null || o.RetryCount <= maxRetryCount) && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)).Take(readSize).ToList();
                else if (messageType == MessageType.OnDemand)
                    return ((IEnumerable<dynamic>)entity.OnDemandMessagesBuffers).Where(o => o.ProcessStatus == (int)SharedLibrary.MessageHandler.ProcessStatus.TryingToSend && (o.RetryCount == null || o.RetryCount <= maxRetryCount) && (o.DateLastTried == null || o.DateLastTried < retryTimeOut)).Take(readSize).ToList();
                else
                    return new List<dynamic>();
            }
        }

        public static void SendSelectedMessages(Type entityType, dynamic messages, int[] skip, int[] take, Dictionary<string, string> serviceAdditionalInfo, string aggregatorName)
        {
            if (((IEnumerable<dynamic>)messages).Count() == 0)
                return;

            List<Task> TaskList = new List<Task>();
            for (int i = 0; i < take.Length; i++)
            {
                var chunkedMessages = ((IEnumerable<dynamic>)messages).Skip(skip[i]).Take(take[i]).ToList();
                if (aggregatorName == "Hamrahvas")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToHamrahvas(entityType, chunkedMessages, serviceAdditionalInfo));
                else if (aggregatorName == "PardisImi")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToPardisImi(entityType, chunkedMessages, serviceAdditionalInfo));
                else if (aggregatorName == "Telepromo")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToTelepromo(entityType, chunkedMessages, serviceAdditionalInfo));
                else if (aggregatorName == "Hub")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToHub(entityType, chunkedMessages, serviceAdditionalInfo));
                else if (aggregatorName == "PardisPlatform")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToPardisPlatform(entityType, chunkedMessages, serviceAdditionalInfo));
                else if (aggregatorName == "MobinOneMapfa")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToPardisPlatform(entityType, chunkedMessages, serviceAdditionalInfo));
                else if (aggregatorName == "MTN")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToMtn(entityType, chunkedMessages, serviceAdditionalInfo));
                else if (aggregatorName == "MobinOne")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToMobinOne(entityType, chunkedMessages, serviceAdditionalInfo));
                else if (aggregatorName == "MciDirect")
                    TaskList.Add(SharedLibrary.MessageSender.SendMesssagesToMci(entityType, chunkedMessages, serviceAdditionalInfo));
            }
            Task.WaitAll(TaskList.ToArray());
        }

        public static Dictionary<string, int[]> CalculateServiceSendMessageThreadNumbers(int readSize, int takeSize)
        {
            if (readSize < takeSize)
            {
                int[] takes = new int[1];
                int[] skips = new int[1];
                skips[0] = 0;
                takes[0] = takeSize;
                return new Dictionary<string, int[]>() { { "take", takes }, { "skip", skips } };
            }
            int[] take = new int[(readSize / takeSize) + 1];
            int[] skip = new int[(readSize / takeSize) + 1];
            skip[0] = 0;
            take[0] = takeSize;
            for (int i = 1; i < take.Length; i++)
            {
                take[i] = takeSize;
                skip[i] = skip[i - 1] + takeSize;
            }
            return new Dictionary<string, int[]>() { { "take", take }, { "skip", skip } };
        }

        public static Dictionary<string, int[]> CalculateServiceSendMessageThreadNumbersByTps(int readSize, int threadNumbers)
        {
            if (readSize <= threadNumbers)
            {
                int[] takes = new int[1];
                int[] skips = new int[1];
                skips[0] = 0;
                takes[0] = threadNumbers;
                return new Dictionary<string, int[]>() { { "take", takes }, { "skip", skips } };
            }
            var takeSize = readSize / threadNumbers;

            int[] take = new int[threadNumbers + 1];
            int[] skip = new int[threadNumbers + 1];
            skip[0] = 0;
            take[0] = takeSize;
            for (int i = 1; i < take.Length; i++)
            {
                take[i] = takeSize;
                skip[i] = skip[i - 1] + takeSize;
            }
            return new Dictionary<string, int[]>() { { "take", take }, { "skip", skip } };
        }

        public static string PrepareMTNMobileNumbers(List<string> mobileNumbers)
        {
            string stringedMobileNumbers = "tel:";
            for (var i = 0; i < mobileNumbers.Count; i++)
            {
                if (mobileNumbers[i].Length == 11)
                    stringedMobileNumbers += "98" + mobileNumbers[i].TrimStart('0') + ",";
                else
                    stringedMobileNumbers += mobileNumbers[i] + ",";
            }
            stringedMobileNumbers = stringedMobileNumbers.TrimEnd(',');
            return stringedMobileNumbers;
        }

        public static string CreateMtnSoapEnvelopeString(string agggregatorServiceId, string timeStamp, string mobileNumbers, string shortCode, string messageContent, string innerServiceId)
        {
            var spId = "980110006379";
            var deliveryUrl = "http://79.175.164.51:200/api/Mtn/Delivery";
            string xmlString = string.Format(@"
<soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:v2=""http://www.huawei.com.cn/schema/common/v2_1"" xmlns:loc=""http://www.csapi.org/schema/parlayx/sms/send/v2_2/local"">
    <soapenv:Header>
        <v2:RequestSOAPHeader>
            <v2:spId>{6}</v2:spId>
            <v2:serviceId>{0}</v2:serviceId>
            <v2:timeStamp>{1}</v2:timeStamp>
        </v2:RequestSOAPHeader>
    </soapenv:Header>
    <soapenv:Body>
        <loc:sendSms>
            <loc:addresses>{2}</loc:addresses>
            <loc:senderName>{3}</loc:senderName>
            <loc:message>{4}</loc:message>
            <loc:receiptRequest>
                <endpoint>{7}</endpoint>
                <interfaceName>SmsNotification</interfaceName>
                <correlator>{5}</correlator>
            </loc:receiptRequest>
        </loc:sendSms>
    </soapenv:Body>
</soapenv:Envelope>"
, agggregatorServiceId, timeStamp, mobileNumbers, shortCode, messageContent, innerServiceId, spId, deliveryUrl);
            return xmlString;
        }

        public static void InsertSoapEnvelopeIntoWebRequest(XmlDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {
            using (Stream stream = webRequest.GetRequestStream())
            {
                soapEnvelopeXml.Save(stream);
            }
        }


        public enum MessageType
        {
            OnDemand = 1,
            EventBase = 2,
            AutoCharge = 3,
        }

        public enum ProcessStatus
        {
            InQueue = 1,
            TryingToSend = 2,
            Success = 3,
            Failed = 4,
            Finished = 5,
            Paused = 6,
        }

        public enum MobileOperators
        {
            Mci = 1,
            Irancell = 2,
            Rightel = 3,
            TCT = 4
        }

        public enum OperatorPlan
        {
            Unspecified = 0,
            Postpaid = 1,
            Prepaid = 2
        }

        public enum MapfaChannels
        {
            SMS = 1,
            USSD = 2,
            MMS = 3,
            IVR = 4,
            ThreeG = 5
        }
    }
}