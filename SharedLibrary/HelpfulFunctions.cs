﻿using ICSharpCode.SharpZipLib.BZip2;
using Newtonsoft.Json;
using SharedLibrary.Models;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace SharedLibrary
{
    public class HelpfulFunctions
    {
        static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        public static string GetAllTheNumbersFromComplexString(string input)
        {
            return new String(input.Where(Char.IsDigit).ToArray());
        }

        public static bool IsPropertyExist(dynamic dynamicObject, string propertyName)
        {
            if (dynamicObject is ExpandoObject)
                return ((IDictionary<string, object>)dynamicObject).ContainsKey(propertyName);

            return dynamicObject.GetType().GetProperty(propertyName) != null;
        }

        public static bool DownloadFileFromWeb(string uri, string filePath)
        {
            var fileName = uri.Remove(0, uri.LastIndexOf('/'));
            try
            {
                WebClient Client = new WebClient();
                Client.DownloadFile(uri, filePath + fileName);
                return true;
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    var response = e.Response as HttpWebResponse;
                    if (response != null)
                        if (response.StatusCode == HttpStatusCode.NotFound)
                            return true;
                }
                logs.Error("WebException in DownloadFileFromWeb-" + uri + ":", e);
                return false;
            }
            catch (Exception e)
            {
                logs.Error("Exception in DownloadFileFromWeb-" + uri + ":", e);
                return false;
            }
        }

        public static bool DeleteFile(string fileUri)
        {
            try
            {
                File.Delete(fileUri);
                return true;
            }
            catch (Exception e)
            {
                logs.Error("Exception in DeleteFile-" + fileUri + ":", e);
                return false;
            }
        }

        public static bool DecompressFromBZ2File(string fileUri)
        {
            FileInfo zipFile = new FileInfo(fileUri);
            using (FileStream fileToDecompressAsStream = zipFile.OpenRead())
            {
                string decompressedFileName = fileUri.Replace(".bz2", "");
                using (FileStream decompressedStream = File.Create(decompressedFileName))
                {
                    try
                    {
                        BZip2.Decompress(fileToDecompressAsStream, decompressedStream, true);
                        return true;
                    }
                    catch (Exception e)
                    {
                        logs.Error("Exception in DecompressFromBZ2File-" + fileUri + ":", e);
                        return false;
                    }
                }
            }
        }

        public static List<ImiData> ReadImiDataFile(string fileUri)
        {
            var preparedLine = "";
            var imiDataList = new List<ImiData>();
            try
            {
                var file = File.ReadAllLines(fileUri);
                foreach (var line in file)
                {
                    preparedLine = "";
                    preparedLine = line.Replace("trans-id", "transId");
                    preparedLine = preparedLine.Replace("base-price-point", "basePricePoint");
                    preparedLine = preparedLine.Replace("next_renewal_date", "nextRenewalDate");
                    preparedLine = preparedLine.Replace("trans-status", "transStatus");
                    preparedLine = preparedLine.Replace("billed-price-point", "billedPricePoint");
                    preparedLine = preparedLine.Replace("event-type", "eventType");
                    var imiDataObj = JsonConvert.DeserializeObject<ImiData>(preparedLine);
                    imiDataObj.keyword = HttpUtility.UrlDecode(imiDataObj.keyword, System.Text.UnicodeEncoding.Default);
                    imiDataList.Add(imiDataObj);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ReadImiDataFile-" + fileUri + ":", e);
            }
            return imiDataList;
        }

        public static Dictionary<string, int> GetIncomeAndSubscriptionsFromImiDataFile(List<ImiData> imiDatas)
        {
            Dictionary<string, int> result = new Dictionary<string, int>();
            result["prepaidSubscriptions"] = 0;
            result["postpaidSubscriptions"] = 0;
            result["prepaidUnsubscriptions"] = 0;
            result["postpaidUnsubscriptions"] = 0;
            result["prepaidCharges"] = 0;
            result["postpaidCharges"] = 0;
            result["sumOfCharges"] = 0;
            try
            {
                foreach (var data in imiDatas)
                {
                    if (data.eventType == "1.5")
                    {
                        if (data.status != 0)
                            continue;
                        var message = new MessageObject();
                        message.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
                        message = SharedLibrary.MessageHandler.GetSubscriberOperatorInfo(message);
                        if (message.OperatorPlan == (int)MessageHandler.OperatorPlan.Postpaid)
                            result["postpaidCharges"] += data.billedPricePoint.Value;
                        else
                            result["prepaidCharges"] += data.billedPricePoint.Value;
                        result["sumOfCharges"] += data.billedPricePoint.Value;
                    }
                    else if (data.eventType == "1.2")
                    {
                        var message = new MessageObject();
                        message.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
                        message = SharedLibrary.MessageHandler.GetSubscriberOperatorInfo(message);
                        if (message.OperatorPlan == (int)MessageHandler.OperatorPlan.Postpaid)
                            result["postpaidUnsubscriptions"] += 1;
                        else
                            result["prepaidUnsubscriptions"] += 1;
                    }
                    else if (data.eventType == "1.1")
                    {
                        var message = new MessageObject();
                        message.MobileNumber = SharedLibrary.MessageHandler.ValidateNumber(data.msisdn);
                        message = SharedLibrary.MessageHandler.GetSubscriberOperatorInfo(message);
                        if (message.OperatorPlan == (int)MessageHandler.OperatorPlan.Postpaid)
                            result["postpaidSubscriptions"] += 1;
                        else
                            result["prepaidSubscriptions"] += 1;
                    }
                }
                if (result["postpaidCharges"] > 0)
                    result["postpaidCharges"] /= 10;
                if (result["prepaidCharges"] > 0)
                    result["prepaidCharges"] /= 10;
                if (result["sumOfCharges"] > 0)
                    result["sumOfCharges"] /= 10;
            }
            catch (Exception e)
            {
                logs.Error("Exception in GetIncomeAndSubscriptionsFromImiDataFile:", e);
                result["prepaidSubscriptions"] = 0;
                result["postpaidSubscriptions"] = 0;
                result["prepaidUnsubscriptions"] = 0;
                result["postpaidUnsubscriptions"] = 0;
                result["prepaidCharges"] = 0;
                result["postpaidCharges"] = 0;
                result["sumOfCharges"] = 0;
            }
            return result;
        }

        public static bool IsPropertyExistInDynamicObject(dynamic dynamicObject, string propertyName)
        {
            if (dynamicObject is ExpandoObject)
                return ((IDictionary<string, object>)dynamicObject).ContainsKey(propertyName);

            return dynamicObject.GetType().GetProperty(propertyName) != null;
        }

        public static string IrancellSignatureGenerator(string authorizationKey, string cpId, string serviceId, string price, string timestamp, string requestId)
        {
            string result = "";
            try
            {
                System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                var key = cpId + serviceId + price + timestamp + requestId;
                logs.Info(key);
                key = key.ToLower();
                HMACSHA1 hmac = new HMACSHA1(ConvertHexStringToByteArray(authorizationKey));
                hmac.Initialize();
                byte[] buffer1 = encoding.GetBytes(key.ToLower());
                result = BitConverter.ToString(hmac.ComputeHash(buffer1)).Replace("-", "").ToLower();
            }
            catch (Exception e)
            {
                logs.Error("Exception in IrancellSignatureGenerator: ", e);
            }
            return result;
        }

        public static string IrancellEncryptedResponse(string encryptedText, string authorizationKey)
        {
            string cryptTxt = "";
            try
            {
                cryptTxt = encryptedText.Replace(" ", "+");
                byte[] bytesBuff = Convert.FromBase64String(cryptTxt);

                string key = authorizationKey;

                using (Aes aes = Aes.Create())
                {
                    Rfc2898DeriveBytes crypto = new Rfc2898DeriveBytes(key, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    aes.Key = crypto.GetBytes(32); aes.IV = crypto.GetBytes(16);
                    using (MemoryStream mStream = new MemoryStream())
                    {
                        using (CryptoStream cStream = new CryptoStream(mStream, aes.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cStream.Write(bytesBuff, 0, bytesBuff.Length); cStream.Close();
                        }
                        cryptTxt = Encoding.Unicode.GetString(mStream.ToArray());
                    }
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in IrancellEncryptedResponse: ", e);
            }
            return cryptTxt;
        }

        public static byte[] ConvertHexStringToByteArray(string hexString)
        {
            byte[] HexAsBytes = null;
            try
            {
                if (hexString.Length % 2 != 0)
                {
                    throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", hexString));
                }
                HexAsBytes = new byte[hexString.Length / 2];
                for (int index = 0; index < HexAsBytes.Length; index++)
                {
                    string byteValue = hexString.Substring(index * 2, 2);
                    HexAsBytes[index] = byte.Parse(byteValue, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                }
            }
            catch (Exception e)
            {
                logs.Error("Exception in ConvertHexStringToByteArray: ", e);
            }
            return HexAsBytes;
        }

    }
}
