using System;
using System.Threading.Tasks;
using Moq;
using Xunit;
using TransactionApi.Services; // Ensure this matches your namespace
using TransactionApi.Models;
using Xunit.Abstractions;
using Microsoft.VisualStudio.TestPlatform.Utilities;

using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using log4net.Config;
using log4net;
using System.Reflection;

public class TransactionServiceTests
{
    private readonly ITestOutputHelper _output;
    private readonly TransactionService _transactionService;
    private static readonly ILog log = LogManager.GetLogger(typeof(TransactionService));


    public TransactionServiceTests(ITestOutputHelper output)
    {
        _output = output;
        _transactionService = new TransactionService();

        // Initialize log4net (add this)
        var logRepository = LogManager.GetRepository(Assembly.GetEntryAssembly());
        log4net.Config.XmlConfigurator.Configure(logRepository, new FileInfo("log4net.config"));
       
    }

    /*VALIDATION SUCCESS*/
    [Fact]
    public void ValidateTransaction_Success_Example()
    {
        _output.WriteLine("TEST CASE: NORMAL Success Example");
        log.Info("TEST CASE: NORMAL Success Example");

        // Arrange
        // Arrange
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff") + "Z";
        var partnerKey = "FAKEGOOGLE";
        var partnerRefNo = "FG-00001";
        var partnerPassword = "RkFLRVBBU1NXT1JEMTIzNA==";
        var totalAmount = 100000;

        // Generate the signature dynamically
        var signature = GenerateSignature(timestamp, partnerKey, partnerRefNo, totalAmount, partnerPassword);

        var request = new TransactionRequest
        {
            PartnerKey = partnerKey,
            PartnerRefNo = partnerRefNo,
            PartnerPassword = partnerPassword,
            TotalAmount = totalAmount,
            Items = new List<ItemDetail>
        {
            new ItemDetail { PartnerItemRef = "i-00001", Name = "Pen", Qty = 4, UnitPrice = 20000 },
            new ItemDetail { PartnerItemRef = "i-00002", Name = "Ruler", Qty = 2, UnitPrice = 10000 }
        },
            Timestamp = timestamp,
            Sig = signature
        };

        _output.WriteLine($"Generated Timestamp: {timestamp}");
        _output.WriteLine($"Generated Signature: {signature}");

        string jsonrequest = JsonConvert.SerializeObject(request, Formatting.Indented);
        _output.WriteLine(jsonrequest);

        // Act
        var result = _transactionService.ValidateAndProcessTransaction(request);

        // Convert object to JSON and print
        string jsonResponse = JsonConvert.SerializeObject(result, Formatting.Indented);
        _output.WriteLine(jsonResponse);

        // Log response (Visible in test output)
        _output.WriteLine($"Result: {result.Result}, Message: {result.ResultMessage}");
        // Assert
        Assert.Equal(1, result.Result); // ✅ Expect success (Result = 1)

    }

    /*TimeStamp Expired*/
    [Fact]
    public void ValidateTransaction_ExpiredTimestamp_ReturnsFailure()
    {

        _output.WriteLine($"TEST CASE : EXPIRED TIMESTAMP");
        log.Info("TEST CASE : EXPIRED TIMESTAMP");
        // Arrange
        var request = new TransactionRequest
        {
            PartnerKey = "FAKEGOOGLE",
            PartnerRefNo = "FG-00001",
            PartnerPassword = "RkFLRVBBU1NXT1JEMTIzNA==",
            TotalAmount = 100000,
            Items = new List<ItemDetail>
        {
            new ItemDetail { PartnerItemRef = "i-00001", Name = "Pen", Qty = 4, UnitPrice = 20000 },
            new ItemDetail { PartnerItemRef = "i-00002", Name = "Ruler", Qty = 2, UnitPrice = 10000 }
        },

            Timestamp = "2024-03-22T07:30:43.7900257Z", // ⏳ Expired timestamp (1 year ago)
            Sig = "PIk5t0B51nZgptLJcSO+Nx6QfApFR7zRnQjwrMVNhkA=" // Fake but correct format
        };

        string jsonrequest = JsonConvert.SerializeObject(request, Formatting.Indented);
        _output.WriteLine(jsonrequest);

        // Act
        var result = _transactionService.ValidateAndProcessTransaction(request);

        // Convert response to JSON and print
        string jsonResponse = JsonConvert.SerializeObject(result, Formatting.Indented);
        // _output.WriteLine(jsonResponse);

        //// Log response (Visible in test output)
        //_output.WriteLine($"Result: {result.Result}, Message: {result.ResultMessage}");

        // Assert: Expect failure due to expired timestamp
        Assert.Equal(0, result.Result); // ❌ Expect failure (Result = 0)
        Assert.Equal("Timestamp Expired", result.ResultMessage); // ❌ Expected expiration error message
    }

    /*INVALID USERNAME*/
    [Fact]
    public void ValidateTransaction_InvalidUsername_ReturnsAccessDenied()
    {

        _output.WriteLine($"TEST CASE : INCORRECT PartnerPassword");
        log.Info("TEST CASE : INCORRECT PartnerPassword");


        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff") + "Z";
        var partnerKey = "FAKEGOOGLE";
        var partnerRefNo = "FG-00001";
        var partnerPassword = "XXRkFLRVBBU1NXT1JEMTIzNA==";
        var totalAmount = 90000;

        // Generate the signature dynamically
        var signature = GenerateSignature(timestamp, partnerKey, partnerRefNo, totalAmount, partnerPassword);


        // Arrange
        var request = new TransactionRequest
        {
            PartnerKey = "FAKEGOOGLE", // ✅ Valid PartnerKey
            PartnerRefNo = "FG-00001", // ❌ Incorrect PartnerRefNo (Username)
            PartnerPassword = "XXRkFLRVBBU1NXT1JEMTIzNA==", // ✅ Base64-encoded "FAKEPASSWORD1234"
            TotalAmount = 100000,
            Items = new List<ItemDetail>
            {
                new ItemDetail { PartnerItemRef = "i-00001", Name = "Pen", Qty = 4, UnitPrice = 20000 },
                new ItemDetail { PartnerItemRef = "i-00002", Name = "Ruler", Qty = 2, UnitPrice = 10000 }
            },
            Timestamp = timestamp,
            Sig = signature
        };

        string jsonrequest = JsonConvert.SerializeObject(request, Formatting.Indented);
        _output.WriteLine(jsonrequest);

        // Act
        var result = _transactionService.ValidateAndProcessTransaction(request);
        // Convert object to JSON and print
        string jsonResponse = JsonConvert.SerializeObject(result, Formatting.Indented);
        _output.WriteLine(jsonResponse);

        // Log response (Visible in test output)
        _output.WriteLine($"Result: {result.Result}, Message: {result.ResultMessage}");

        // Assert
        Assert.Equal(0, result.Result); // ❌ Expect failure (Result = 0)
        Assert.Equal("Access Denied!", result.ResultMessage); // ❌ Expect correct error message
    }

    /*INVALID TOTAL AMOUNT*/
    [Fact]
    public void ValidateTransaction_InvalidTotalAmount_ReturnsFailure()
    {
        _output.WriteLine($"TEST CASE : INVALID TOTAL AMOUNT");
        log.Info("TEST CASE : INVALID TOTAL AMOUNT");

        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff") + "Z";
        var partnerKey = "FAKEGOOGLE";
        var partnerRefNo = "FG-00001";
        var partnerPassword = "RkFLRVBBU1NXT1JEMTIzNA==";
        var totalAmount = 90000;

        // Generate the signature dynamically
        var signature = GenerateSignature(timestamp, partnerKey, partnerRefNo, totalAmount, partnerPassword);


        // Arrange
        var request = new TransactionRequest
        {
            PartnerKey = "FAKEGOOGLE",
            PartnerRefNo = "FG-00001",
            PartnerPassword = "RkFLRVBBU1NXT1JEMTIzNA==",
            TotalAmount = 90000, // ❌ Incorrect total (should be 100000)
            Items = new List<ItemDetail>
        {
            new ItemDetail { PartnerItemRef = "i-00001", Name = "Pen", Qty = 4, UnitPrice = 20000 }, // 4 * 20000 = 80000
            new ItemDetail { PartnerItemRef = "i-00002", Name = "Ruler", Qty = 2, UnitPrice = 10000 } // 2 * 10000 = 20000
        },

            Timestamp = timestamp,
            Sig = signature
        };

        string jsonrequest = JsonConvert.SerializeObject(request, Formatting.Indented);
        _output.WriteLine(jsonrequest);

        // Act
        var result = _transactionService.ValidateAndProcessTransaction(request);

        // Convert response to JSON and print
        string jsonResponse = JsonConvert.SerializeObject(result, Formatting.Indented);
        _output.WriteLine(jsonResponse);

        // Log response (Visible in test output)
        _output.WriteLine($"Result: {result.Result}, Message: {result.ResultMessage}");

        // Assert: Expect failure due to mismatched total amount
        Assert.Equal(0, result.Result); // ❌ Expect failure (Result = 0)
        Assert.Equal("Invalid Total Amount.", result.ResultMessage); // ❌ Expected error message
    }

    /*DISCOUNT*/
    [Fact]
    public void ValidateTransaction_Success_Discount_Scenario1()
    {
        _output.WriteLine("TEST CASE: Scenario 1 - Base Discount 10%, No Conditional Discount");
        log.Info("TEST CASE: Scenario 1 - Base Discount 10%, No Conditional Discount");

        // Arrange
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff") + "Z";
        var partnerKey = "FAKEGOOGLE";
        var partnerRefNo = "FG-00001";
        var partnerPassword = "RkFLRVBBU1NXT1JEMTIzNA==";
        var totalAmount = 100000;

        var signature = GenerateSignature(timestamp, partnerKey, partnerRefNo, totalAmount, partnerPassword);

        var request = new TransactionRequest
        {
            PartnerKey = partnerKey,
            PartnerRefNo = partnerRefNo,
            PartnerPassword = partnerPassword,
            TotalAmount = totalAmount,
            Items = new List<ItemDetail>
                {
                    new ItemDetail { PartnerItemRef = "i-00001", Name = "Notebook", Qty = 5, UnitPrice = 20000 }
                },
            Timestamp = timestamp,
            Sig = signature
        };

        string jsonrequest = JsonConvert.SerializeObject(request, Formatting.Indented);
        _output.WriteLine(jsonrequest);

        // Act
        var result = _transactionService.ValidateAndProcessTransaction(request);
        string jsonResponse = JsonConvert.SerializeObject(result, Formatting.Indented);
        var errorDetails = JsonConvert.DeserializeObject<ApiErrorResponse>(jsonResponse);

        if (result.Result == 0)
        {
            _output.WriteLine("Transaction failed. Checking error details...");

            if (errorDetails != null)
            {
                _output.WriteLine($"Error Title: {errorDetails.Title}");
                _output.WriteLine($"Status: {errorDetails.Status}");
                _output.WriteLine($"TraceId: {errorDetails.TraceId}");

                foreach (var error in errorDetails.Errors)
                {
                    _output.WriteLine($"Field: {error.Key}");
                    foreach (var message in error.Value)
                    {
                        _output.WriteLine($"  - {message}");
                    }
                }
            }

        }
    }

    [Fact]
    public void ValidateTransaction_Success_Discount_Scenario2()
    {
        _output.WriteLine("TEST CASE: Scenario 2 - Base Discount 15%, Conditional Discount 10%, Cap at 20%");
        log.Info("TEST CASE: Scenario 2 - Base Discount 15%, Conditional Discount 10%, Cap at 20%");

        // Arrange
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffff") + "Z";
        var partnerKey = "FAKEGOOGLE";
        var partnerRefNo = "FG-00001";
        var partnerPassword = "RkFLRVBBU1NXT1JEMTIzNA==";
        var totalAmount = 120500;

        var signature = GenerateSignature(timestamp, partnerKey, partnerRefNo, totalAmount, partnerPassword);

        var request = new TransactionRequest
        {
            PartnerKey = partnerKey,
            PartnerRefNo = partnerRefNo,
            PartnerPassword = partnerPassword,
            TotalAmount = totalAmount,
            Items = new List<ItemDetail>
        {
            new ItemDetail { PartnerItemRef = "i-00001", Name = "Vacuum", Qty = 5, UnitPrice = 24100 }
        },
            Timestamp = timestamp,
            Sig = signature
        };

        string jsonrequest = JsonConvert.SerializeObject(request, Formatting.Indented);
        _output.WriteLine(jsonrequest);

        // Act
        var result = _transactionService.ValidateAndProcessTransaction(request);

        string jsonResponse = JsonConvert.SerializeObject(result, Formatting.Indented);
        var errorDetails = JsonConvert.DeserializeObject<ApiErrorResponse>(jsonResponse);

        if (result.Result == 0)
        {
            _output.WriteLine("Transaction failed. Checking error details...");

            if (errorDetails != null)
            {
                _output.WriteLine($"Error Title: {errorDetails.Title}");
                _output.WriteLine($"Status: {errorDetails.Status}");
                _output.WriteLine($"TraceId: {errorDetails.TraceId}");

                foreach (var error in errorDetails.Errors)
                {
                    _output.WriteLine($"Field: {error.Key}");
                    foreach (var message in error.Value)
                    {
                        _output.WriteLine($"  - {message}");
                    }
                }
            }

            //Assert.Fail("Transaction validation failed. Check logs for details.");
        }

        //string jsonResponse = JsonConvert.SerializeObject(result, Formatting.Indented);
        //var errorDetails = JsonConvert.DeserializeObject<ApiErrorResponse>(jsonResponse);
        //_output.WriteLine($"Error: {errorDetails.Title}");
        //_output.WriteLine($"Status: {errorDetails.Status}");
        //_output.WriteLine($"TraceId: {errorDetails.TraceId}");

        //// Assert
        //Assert.Equal(1, result.Result);  // Ensure transaction was successful
        //Assert.Equal(120500, result.TotalAmount);  // Verify total amount
        //Assert.Equal(24100, result.TotalDiscount); // Verify the 20% discount
        //Assert.Equal(96400, result.FinalAmount);   // Ensure correct final amount after discount
    }

    //gen signature to compare
    public static string GenerateSignature(string timestamp, string partnerKey, string partnerRefNo, long totalAmount, string partnerPassword)
    {
        TimeZoneInfo.ClearCachedData(); // Refresh timezone settings
        TimeZoneInfo utcZone = TimeZoneInfo.Utc;

        Console.WriteLine($"Local Time: {DateTime.Now}"); // This is system local time
        Console.WriteLine($"UTC Time: {DateTime.UtcNow}"); // This should match Web API

        // Convert local time to UTC (if needed)
        DateTime localTime = DateTime.Now;
        DateTime convertedToUtc = TimeZoneInfo.ConvertTimeToUtc(localTime);
        Console.WriteLine($"Converted UTC Time: {convertedToUtc}");
        Console.WriteLine($"timestamp UTC Time: {convertedToUtc.ToString("yyyy-MM-ddTHH:mm:ss.fffffff") + "Z"}");



        var sigTimestamp = convertedToUtc.ToString("yyyyMMddHHmmss");
        var sigString = $"{sigTimestamp}{partnerKey}{partnerRefNo}{totalAmount}{partnerPassword}";
        Console.WriteLine($"Before Conversion :{sigString}");
        var sigHash = SHA256.HashData(Encoding.UTF8.GetBytes(sigString));
        var sigBase64 = Convert.ToBase64String(sigHash);
        return sigBase64;
    }

    class ApiErrorResponse
    {
        public string Type { get; set; }
        public string Title { get; set; }
        public int Status { get; set; }
        public string TraceId { get; set; }
        public Dictionary<string, List<string>> Errors { get; set; }
    }
}
