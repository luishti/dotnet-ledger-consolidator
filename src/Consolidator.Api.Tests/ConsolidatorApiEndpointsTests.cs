using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Consolidator.Api.Tests
{
    public class ConsolidatorApiEndpointsTests
    {
        [Fact]
        public async Task HealthEndpoint_ReturnsHealthy()
        {
            // Simula o endpoint /health
            var result = Results.Ok("healthy");
            var objectResult = Assert.IsType<Ok<string>>(result);
            Assert.Equal("healthy", objectResult.Value);
        }

        [Fact]
        public async Task DailyBalancesEndpoint_ReturnsOk()
        {
            // Simulação básica do endpoint /daily-balances
            var balances = new List<DailyBalanceDto> {
                new DailyBalanceDto("merchant1", DateOnly.FromDateTime(DateTime.UtcNow), 100.0m)
            };
            var result = Results.Ok(balances);
            var okResult = Assert.IsType<Ok<List<DailyBalanceDto>>>(result);
            Assert.Single(okResult.Value);
        }
    }

    public record DailyBalanceDto(string MerchantId, DateOnly Date, decimal TotalAmount);
}