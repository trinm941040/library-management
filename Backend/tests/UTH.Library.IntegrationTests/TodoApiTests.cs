using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using UTH.Library.Application.Abstractions.Persistence;
using UTH.Library.Domain.Entities;

namespace UTH.Library.IntegrationTests;

public sealed class TodoApiTests : IClassFixture<TodoApiFactory>
{
    private readonly HttpClient client;

    public TodoApiTests(TodoApiFactory factory) => client = factory.CreateClient();

    [Fact]
    public async Task CreateTodo_ValidRequest_ReturnsCreatedResponse()
    {
        var response = await client.PostAsJsonAsync("/api/todos", new { title = "Read" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.Contains("Read", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task GetTodo_UnknownId_ReturnsNotFound()
    {
        var response = await client.GetAsync($"/api/todos/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}

public sealed class TodoApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            foreach (var descriptor in services.Where(descriptor => descriptor.ServiceType == typeof(ITodoRepository)).ToList())
            {
                services.Remove(descriptor);
            }

            services.AddSingleton<ITodoRepository, FakeTodoRepository>();
        });
    }

    private sealed class FakeTodoRepository : ITodoRepository
    {
        private readonly List<TodoItem> todos = [];

        public Task AddAsync(TodoItem todo, CancellationToken cancellationToken)
        {
            todos.Add(todo);
            return Task.CompletedTask;
        }

        public Task<TodoItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(todos.SingleOrDefault(todo => todo.Id == id));

        public Task SaveChangesAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}