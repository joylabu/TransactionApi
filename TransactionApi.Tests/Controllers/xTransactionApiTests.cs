using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Xunit;

public class xTransactionApiTests
{
    private readonly HttpClient _client;

    public xTransactionApiTests()
    {
        _client = new HttpClient { BaseAddress = new Uri("https://localhost:7049/") };
    }

    //[Fact]
    //public async Task SubmitTransaction_ReturnsSuccess()
    //{
    //    var jsonRequest = @"
    //    {
    //        ""partnerkey"": ""FAKEGOOGLE"",
    //        ""partnerrefno"": ""FG-00001"",
    //        ""partnerpassword"": ""RkFLRVBBU1NXT1JEMTIzNA=="",
    //        ""totalamount"": 100000,
    //        ""items"": [
    //            { ""partneritemref"": ""i-00001"", ""name"": ""Pen"", ""qty"": 4, ""unitprice"": 20000 },
    //            { ""partneritemref"": ""i-00002"", ""name"": ""Ruler"", ""qty"": 2, ""unitprice"": 10000 }
    //        ],
    //        ""timestamp"": ""2025-03-22T07:30:43.7900257Z"",
    //        ""sig"": ""PIk5t0B51nZgptLJcSO+Nx6QfApFR7zRnQjwrMVNhkA=""
    //    }";

    //    var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

    //    var response = await _client.PostAsync("api/Transaction/submittrxmessage", content);

    //    response.EnsureSuccessStatusCode();
    //}
}
