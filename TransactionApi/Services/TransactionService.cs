using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json; // Import Newtonsoft.Json
using TransactionApi.Models;
using TransactionApi.Function; // Import the DiscountCalculator class
using log4net;
using log4net.Config;
using System.IO;
using System.Reflection;
using Microsoft.VisualStudio.TestPlatform.Utilities;

namespace TransactionApi.Services
{


    public class TransactionService
    {

        public TransactionService()
        {
            var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
            log4net.Config.XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
        }
        private static readonly ILog log = LogManager.GetLogger(typeof(TransactionService));

        private readonly Dictionary<string, string> _allowedPartners = new()
        {
            { "FG-00001", "FAKEPASSWORD1234" },
            { "FG-00002", "FAKEPASSWORD4578" }
        };

        public TransactionResponse ValidateAndProcessTransaction(TransactionRequest request)
        {
            // Convert object to JSON and print
            string jsonRequest = JsonConvert.SerializeObject(request, Formatting.Indented);
            log.Info("Request :" + jsonRequest);

            var response = new TransactionResponse();

            log.Info("Starting transaction validation.");

            try
            {
                // Validate mandatory parameters
                if (string.IsNullOrEmpty(request.PartnerRefNo))
                {
                    log.Warn("Validation failed: PartnerRefNo is missing.");
                    response.Result = 0;
                    response.ResultMessage = "partnerrefno is Required.";
                    // Convert object to JSON and print
                    string jsonResponse = JsonConvert.SerializeObject(response, Formatting.Indented);
                    log.Info("Response :" + jsonResponse);
                    return response;
                }

                log.Info("Signature validated successfully.");

                if (string.IsNullOrEmpty(request.PartnerKey))
                {

                    log.Warn("Validation failed: PartnerRefNo is missing.");
                    response.Result = 0;
                    response.ResultMessage = "partnerKey is Required.";
                    // Convert object to JSON and print
                    string jsonResponse = JsonConvert.SerializeObject(response, Formatting.Indented);
                    log.Info("Response :" + jsonResponse);
                    return response;
                }

                log.Info("Timestamp validated successfully.");

                if (string.IsNullOrEmpty(request.Timestamp))
                {
                    log.Warn("Validation failed: PartnerRefNo is missing.");
                    response.Result = 0;
                    response.ResultMessage = "timestamp is Required.";
                    // Convert object to JSON and print
                    string jsonResponse = JsonConvert.SerializeObject(response, Formatting.Indented);
                    log.Info("Response :" + jsonResponse);
                    return response;
                }

                log.Info("Timestamp validated successfully.");


                if (string.IsNullOrEmpty(request.Sig))
                {
                    log.Warn("Validation failed: Signature is missing.");
                    response.Result = 0;
                    response.ResultMessage = "sig is Required.";
                    // Convert object to JSON and print
                    string jsonResponse = JsonConvert.SerializeObject(response, Formatting.Indented);
                    log.Info("Response :" + jsonResponse);
                    return response;
                }

                log.Info("Sig validated successfully.");

                // Validate PartnerKey and PartnerPassword
                if (!_allowedPartners.TryGetValue(request.PartnerRefNo, out var expectedPassword))
                {
                    log.Warn($"Access Denied: PartnerRefNo {request.PartnerRefNo} is not recognized.");
                    response.Result = 0;
                    response.ResultMessage = "Access Denied!";
                    // Convert object to JSON and print
                    string jsonResponse = JsonConvert.SerializeObject(response, Formatting.Indented);
                    log.Info("Response :" + jsonResponse);
                    return response;
                }

                try
                {
                    var decodedPassword = Encoding.UTF8.GetString(Convert.FromBase64String(request.PartnerPassword));
                    if (decodedPassword != expectedPassword)
                    {
                        log.Warn($"Access Denied: Invalid password for PartnerRefNo {request.PartnerRefNo}.");
                        response.Result = 0;
                        response.ResultMessage = "Access Denied!";
                        // Convert object to JSON and print
                        string jsonResponse = JsonConvert.SerializeObject(response, Formatting.Indented);
                        log.Info("Response :" + jsonResponse);
                        return response;
                    }
                }
                catch (FormatException ex)
                {
                    log.Error("FormatException occurred during transaction validation.", ex);
                    response.Result = 0;
                    response.ResultMessage = "Invalid Base-64 string format.";
                    // Convert object to JSON and print
                    string jsonResponse = JsonConvert.SerializeObject(response, Formatting.Indented);
                    log.Info("Response :" + jsonResponse);
                }

                // Validate Timestamp
                var requestTime = DateTime.Parse(request.Timestamp, null, System.Globalization.DateTimeStyles.AdjustToUniversal);
                var serverTime = DateTime.UtcNow;
                if (Math.Abs((serverTime - requestTime).TotalMinutes) > 5)
                {
                    log.Warn($"Timestamp expired: Request time {requestTime}, Server time {serverTime}");
                    response.Result = 0;
                    response.ResultMessage = "Timestamp Expired";
                    // Convert object to JSON and print
                    string jsonResponse = JsonConvert.SerializeObject(response, Formatting.Indented);
                    log.Info("Response :" + jsonResponse);

                    return response;
                }

                // Validate Signature
                var sigTimestamp = DateTime.Parse(request.Timestamp, null, System.Globalization.DateTimeStyles.AdjustToUniversal).ToString("yyyyMMddHHmmss");  // DateTime.Parse(request.Timestamp).ToString("yyyyMMddHHmmss");
                var sigString = $"{sigTimestamp}{request.PartnerKey}{request.PartnerRefNo}{request.TotalAmount}{request.PartnerPassword}";
                var sigHash = SHA256.HashData(Encoding.UTF8.GetBytes(sigString));
                var sigBase64 = Convert.ToBase64String(sigHash);

                if (sigBase64 != request.Sig)
                {
                    log.Warn($"Signature validation failed for PartnerRefNo {request.PartnerRefNo}.");
                    response.Result = 0;
                    response.ResultMessage = "Access Denied!";
                    // Convert object to JSON and print
                    string jsonResponse = JsonConvert.SerializeObject(response, Formatting.Indented);
                    log.Info("Response :" + jsonResponse);

                    return response;
                }
                log.Info("Signature validated successfully.");


                // Validate Items and Total Amount
                if (request.Items != null)
                {
                    long calculatedTotalAmount = 0;
                    foreach (var item in request.Items)
                    {
                        if (string.IsNullOrEmpty(item.PartnerItemRef) || string.IsNullOrEmpty(item.Name) ||
                            item.Qty < 1 || item.UnitPrice < 1)
                        {
                            log.Warn($"Invalied Item Details for PartnerRefNo {request.PartnerRefNo}.");
                            response.Result = 0;
                            response.ResultMessage = "Invalid Item Details!";
                            // Convert object to JSON and print
                            string jsonResponse = JsonConvert.SerializeObject(response, Formatting.Indented);
                            log.Info("Response :" + jsonResponse);

                            return response;
                        }
                        calculatedTotalAmount += item.Qty * item.UnitPrice;
                    }

                    if (calculatedTotalAmount != request.TotalAmount)
                    {
                        log.Warn($"Invalied Total Amount for PartnerRefNo {request.PartnerRefNo}.");
                        response.Result = 0;
                        response.ResultMessage = "Invalid Total Amount.";

                        // Convert object to JSON and print
                        string jsonResponse = JsonConvert.SerializeObject(response, Formatting.Indented);
                        log.Info("Response :" + jsonResponse);

                        return response;
                    }
                }


                // Calculate Base Discount
                var (totalDiscount, finalAmount) = DiscountCalculator.CalculateDiscount(request.TotalAmount);

                return new TransactionResponse
                {
                    Result = 1,
                    TotalAmount = request.TotalAmount,
                    TotalDiscount = totalDiscount,
                    FinalAmount = finalAmount
                };

                

                if (response.ResultMessage == "")
                {
                    response.ResultMessage = "Success";
                }



            }
            catch (Exception ex)
            {
                log.Error("An error occurred during transaction validation.", ex);
                response.Result = 0;
                response.ResultMessage = "An unexpected error occurred.";
            }
            finally
            {

                // Convert object to JSON and print
                string jsonResponse = JsonConvert.SerializeObject(response, Formatting.Indented);
                log.Info("Response :" + jsonResponse);
            }
            return response;
        }
 

    }
}
