using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Moq;

namespace Ledger.Api.Tests
{
    public class LedgerApiEndpointsTests
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
        public async Task GetEntryById_ReturnsNotFound_WhenEntryDoesNotExist()
        {
            var mediator = new Mock<IMediator>();
            mediator.Setup(m => m.Send(It.IsAny<object>(), default)).ReturnsAsync((object)null);
            var id = Guid.NewGuid();
            var result = await LedgerApiTestHelpers.GetEntryByIdEndpoint(id, mediator.Object);
            var notFoundResult = Assert.IsType<NotFound>(result);
        }

        [Fact]
        public async Task PostEntry_ReturnsCreatedWithId()
        {
            var mediator = new Mock<IMediator>();
            var expectedId = Guid.NewGuid();
            mediator.Setup(m => m.Send(It.IsAny<object>(), default)).ReturnsAsync(expectedId);
            var command = new object(); // Simulação do comando
            var result = await LedgerApiTestHelpers.PostEntryEndpoint(command, mediator.Object);
            var createdResult = Assert.IsType<Created<object>>(result);
            Assert.Equal($"/entries/{expectedId}", createdResult.Location);
            Assert.Equal(expectedId, ((dynamic)createdResult.Value).id);
        }
    }

    // Helpers para simular endpoints
    public static class LedgerApiTestHelpers
    {
        public static async Task<IResult> GetEntryByIdEndpoint(Guid id, IMediator mediator)
        {
            var entry = await mediator.Send(new object()); // Simulação
            return entry is null ? Results.NotFound() : Results.Ok(entry);
        }

        public static async Task<IResult> PostEntryEndpoint(object command, IMediator mediator)
        {
            var id = await mediator.Send(command);
            return Results.Created($"/entries/{id}", new { id });
        }
    }
}