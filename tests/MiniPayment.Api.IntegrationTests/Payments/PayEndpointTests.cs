using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using MiniPayment.Api.IntegrationTests.Infrastructure;

namespace MiniPayment.Api.IntegrationTests.Payments;

[Collection("PaymentApi")]
public class PayEndpointTests(PaymentApiFactory factory) : IClassFixture<PaymentApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();
    private readonly string _token = TokenHelper.GenerateTestToken();

    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Unauthenticated_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/pay", ValidRequest("ORD-AUTH-1", 10.00m));
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Amount_10_00_ReturnsApproved()
    {
        var response = await PostAuthorized(ValidRequest("ORD-INT-APPROVED", 10.00m));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        body.GetProperty("status").GetString().Should().Be("APPROVED");
        body.GetProperty("response_code").GetString().Should().Be("00");
        body.GetProperty("transaction_id").GetString().Should().NotBeNullOrEmpty();
        body.GetProperty("acquirer_reference").GetString().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Amount_10_05_ReturnsDeclined()
    {
        var response = await PostAuthorized(ValidRequest("ORD-INT-DECLINED", 10.05m));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        body.GetProperty("status").GetString().Should().Be("DECLINED");
        body.GetProperty("response_code").GetString().Should().Be("05");
    }

    [Fact]
    public async Task InvalidCard_Returns422()
    {
        var req = ValidRequest("ORD-VAL-1", 10.00m);
        req["card_number"] = "1234567890123456"; // fails Luhn
        var response = await PostAuthorized(req);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task ExpiredCard_Returns422()
    {
        var req = ValidRequest("ORD-VAL-2", 10.00m);
        req["expiry_date"] = "01/20"; // past date
        var response = await PostAuthorized(req);
        response.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task DuplicateOrderNumber_ReturnsSameTransactionId()
    {
        var req = ValidRequest("ORD-IDEM-INT", 10.00m);

        var first = await PostAuthorized(req);
        first.StatusCode.Should().Be(HttpStatusCode.OK);
        var firstBody = await first.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var firstTxId = firstBody.GetProperty("transaction_id").GetString();

        // Reset request stream for second call
        var second = await PostAuthorized(req);
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var secondBody = await second.Content.ReadFromJsonAsync<JsonElement>(JsonOpts);
        var secondTxId = secondBody.GetProperty("transaction_id").GetString();

        secondTxId.Should().Be(firstTxId);
    }

    private async Task<HttpResponseMessage> PostAuthorized(Dictionary<string, object?> body)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/pay")
        {
            Content = JsonContent.Create(body)
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
        return await _client.SendAsync(request);
    }

    private static Dictionary<string, object?> ValidRequest(string orderNumber, decimal amount) =>
        new()
        {
            ["order_number"] = orderNumber,
            ["card_number"] = "4111111111111111",
            ["expiry_date"] = "12/29",
            ["cvv"] = "123",
            ["currency"] = "USD",
            ["cardholder_name"] = "Jane Smith",
            ["email"] = "jane@example.com",
            ["amount"] = amount
        };
}
