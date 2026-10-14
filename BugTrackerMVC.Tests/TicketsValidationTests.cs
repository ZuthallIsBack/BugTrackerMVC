using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using BugTrackerMVC.Data;
using BugTrackerMVC.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BugTrackerMVC.Tests;

public sealed class TicketsValidationTests : IClassFixture<TicketApplicationFactory>
{
    private readonly TicketApplicationFactory _factory;
    private readonly int _projectId;
    private readonly int _categoryId;

    public TicketsValidationTests(TicketApplicationFactory factory)
    {
        _factory = factory;
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        context.Tickets.ExecuteDelete();
        _projectId = context.Projects.OrderBy(p => p.Id).First().Id;
        _categoryId = context.Categories.OrderBy(c => c.Id).First().Id;
    }

    public static IEnumerable<object?[]> InvalidRequests()
    {
        var cases = new (string Field, object? Value)[]
        {
            ("Title", null), ("Title", ""), ("Title", "     "),
            ("Title", new string('t', 4)), ("Title", new string('t', 121)),
            ("Description", null), ("Description", ""), ("Description", new string(' ', 20)),
            ("Description", new string('d', 19)), ("Description", new string('d', 4001)),
            ("Status", -1), ("Status", 4), ("Status", int.MaxValue),
            ("Priority", -1), ("Priority", 4), ("Priority", int.MaxValue),
            ("ProjectId", 0), ("ProjectId", -1), ("ProjectId", int.MaxValue),
            ("CategoryId", 0), ("CategoryId", -1), ("CategoryId", int.MaxValue)
        };
        foreach (var method in new[] { "POST", "PUT" })
            foreach (var (field, value) in cases)
                yield return new object?[] { method, field, value };
    }

    [Theory]
    [MemberData(nameof(InvalidRequests))]
    public async Task Invalid_api_request_returns_field_errors_without_saving(string method, string field, object? value)
    {
        var id = await SeedTicketAsync();
        var before = await SnapshotAsync();
        var payload = ValidPayload();
        payload[field] = value;
        using var client = _factory.CreateUserClient();

        using var response = await SendAsync(client, method, id, payload);

        await AssertValidationProblemAsync(response, field);
        Assert.Equal(before, await SnapshotAsync());
    }

    [Theory]
    [InlineData("POST", "Title")]
    [InlineData("POST", "Description")]
    [InlineData("POST", "ProjectId")]
    [InlineData("POST", "CategoryId")]
    [InlineData("PUT", "Title")]
    [InlineData("PUT", "Description")]
    [InlineData("PUT", "ProjectId")]
    [InlineData("PUT", "CategoryId")]
    public async Task Missing_required_field_returns_400(string method, string field)
    {
        var id = await SeedTicketAsync();
        var before = await SnapshotAsync();
        var payload = ValidPayload();
        payload.Remove(field);
        using var client = _factory.CreateUserClient();
        using var response = await SendAsync(client, method, id, payload);
        await AssertValidationProblemAsync(response, field);
        Assert.Equal(before, await SnapshotAsync());
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    public async Task Both_missing_relations_are_reported_together(string method)
    {
        var id = await SeedTicketAsync();
        var before = await SnapshotAsync();
        var payload = ValidPayload();
        payload["ProjectId"] = int.MaxValue;
        payload["CategoryId"] = int.MaxValue;
        using var client = _factory.CreateUserClient();
        using var response = await SendAsync(client, method, id, payload);
        await AssertValidationProblemAsync(response, "ProjectId", "CategoryId");
        Assert.Equal(before, await SnapshotAsync());
    }

    [Theory]
    [InlineData("POST", 5, 20, 0)]
    [InlineData("POST", 120, 4000, 3)]
    [InlineData("POST", 40, 100, 1)]
    [InlineData("POST", 40, 100, 2)]
    [InlineData("PUT", 5, 20, 0)]
    [InlineData("PUT", 120, 4000, 3)]
    [InlineData("PUT", 40, 100, 1)]
    [InlineData("PUT", 40, 100, 2)]
    public async Task Valid_requests_accept_boundaries_and_all_enum_values(
        string method, int titleLength, int descriptionLength, int enumValue)
    {
        var id = method == "PUT" ? await SeedTicketAsync() : 0;
        var previous = (await SnapshotAsync()).SingleOrDefault();
        var payload = ValidPayload();
        payload["Title"] = new string('t', titleLength);
        payload["Description"] = new string('d', descriptionLength);
        payload["Status"] = enumValue;
        payload["Priority"] = enumValue;
        payload["OwnerId"] = "attacker";
        using var client = _factory.CreateUserClient();

        using var response = await SendAsync(client, method, id, payload);

        Assert.Equal(method == "POST" ? HttpStatusCode.Created : HttpStatusCode.NoContent, response.StatusCode);
        var ticket = Assert.Single(await SnapshotAsync());
        Assert.Equal(payload["Title"], ticket.Title);
        Assert.Equal(payload["Description"], ticket.Description);
        Assert.Equal((TicketStatus)enumValue, ticket.Status);
        Assert.Equal((TicketPriority)enumValue, ticket.Priority);
        Assert.Equal(_projectId, ticket.ProjectId);
        Assert.Equal(_categoryId, ticket.CategoryId);
        Assert.Equal("owner", ticket.OwnerId);
        if (method == "POST")
        {
            Assert.Null(ticket.UpdatedAt);
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(ticket.Id, body.GetProperty("id").GetInt32());
            Assert.EndsWith($"/api/tickets/{ticket.Id}", response.Headers.Location!.ToString());
            using var get = await client.GetAsync(response.Headers.Location);
            Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        }
        else
        {
            Assert.Equal(id, ticket.Id);
            Assert.Equal(previous!.CreatedAt, ticket.CreatedAt);
            Assert.NotNull(ticket.UpdatedAt);
            Assert.True(ticket.UpdatedAt > previous.UpdatedAt);
        }
    }

    [Theory]
    [InlineData("POST", "null")]
    [InlineData("POST", "")]
    [InlineData("POST", "{")]
    [InlineData("PUT", "null")]
    [InlineData("PUT", "")]
    [InlineData("PUT", "{")]
    public async Task Invalid_json_body_returns_400_without_saving(string method, string json)
    {
        var id = await SeedTicketAsync();
        var before = await SnapshotAsync();
        using var client = _factory.CreateUserClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), ApiUrl(method, id))
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
        using var response = await client.SendAsync(request);
        await AssertValidationProblemAsync(response);
        Assert.Equal(before, await SnapshotAsync());
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    public async Task Anonymous_requests_remain_unauthorized(string method)
    {
        var id = await SeedTicketAsync();
        var before = await SnapshotAsync();
        using var client = _factory.CreateUserClient(userId: null);
        using var response = await SendAsync(client, method, id, ValidPayload());
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(before, await SnapshotAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Other_users_ticket_remains_forbidden_even_with_missing_relations(bool invalidRelations)
    {
        var id = await SeedTicketAsync("another-user");
        var before = await SnapshotAsync();
        var payload = ValidPayload();
        if (invalidRelations) payload["ProjectId"] = int.MaxValue;
        using var client = _factory.CreateUserClient();
        using var response = await SendAsync(client, "PUT", id, payload);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal(before, await SnapshotAsync());
    }

    [Fact]
    public async Task Admin_can_update_another_users_ticket_without_changing_ownership()
    {
        var id = await SeedTicketAsync("another-user");
        using var client = _factory.CreateUserClient("admin", isAdmin: true);
        using var response = await SendAsync(client, "PUT", id, ValidPayload());
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var ticket = Assert.Single(await SnapshotAsync());
        Assert.Equal("another-user", ticket.OwnerId);
        Assert.Equal("Valid updated title", ticket.Title);
    }

    [Theory]
    [InlineData("POST")]
    [InlineData("PUT")]
    public async Task Admin_is_also_subject_to_validation(string method)
    {
        var id = await SeedTicketAsync("another-user");
        var before = await SnapshotAsync();
        var payload = ValidPayload();
        payload["CategoryId"] = int.MaxValue;
        using var client = _factory.CreateUserClient("admin", isAdmin: true);
        using var response = await SendAsync(client, method, id, payload);
        await AssertValidationProblemAsync(response, "CategoryId");
        Assert.Equal(before, await SnapshotAsync());
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Missing_ticket_remains_404(bool invalidRelations)
    {
        var payload = ValidPayload();
        if (invalidRelations) payload["CategoryId"] = int.MaxValue;
        using var client = _factory.CreateUserClient();
        using var response = await SendAsync(client, "PUT", int.MaxValue, payload);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Empty(await SnapshotAsync());
    }

    [Theory]
    [InlineData(false, HttpStatusCode.Forbidden)]
    [InlineData(true, HttpStatusCode.NoContent)]
    public async Task Delete_remains_admin_only(bool isAdmin, HttpStatusCode expectedStatus)
    {
        var id = await SeedTicketAsync();
        using var client = _factory.CreateUserClient(isAdmin: isAdmin);
        using var response = await client.DeleteAsync($"/api/tickets/{id}");
        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.Equal(isAdmin ? 0 : 1, (await SnapshotAsync()).Length);
    }

    [Theory]
    [InlineData("Create", "Title")]
    [InlineData("Create", "Description")]
    [InlineData("Create", "Status")]
    [InlineData("Create", "Priority")]
    [InlineData("Create", "ProjectId")]
    [InlineData("Create", "CategoryId")]
    [InlineData("Edit", "Title")]
    [InlineData("Edit", "Description")]
    [InlineData("Edit", "Status")]
    [InlineData("Edit", "Priority")]
    [InlineData("Edit", "ProjectId")]
    [InlineData("Edit", "CategoryId")]
    public async Task Mvc_forms_reject_the_same_invalid_fields(string action, string field)
    {
        var id = await SeedTicketAsync();
        var before = await SnapshotAsync();
        var url = action == "Create" ? "/Tickets/Create" : $"/Tickets/Edit/{id}";
        using var client = _factory.CreateUserClient();
        var form = await ValidFormAsync(client, url);
        form[field] = field switch
        {
            "Title" => new string('t', 121),
            "Description" => new string('d', 4001),
            _ => int.MaxValue.ToString()
        };
        using var response = await client.PostAsync(url, new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("validation-summary-errors", html);
        Assert.Contains("data-val-length-max=\"120\"", html);
        Assert.Contains("data-val-length-min=\"5\"", html);
        Assert.Contains("data-val-length-max=\"4000\"", html);
        Assert.Contains("data-val-length-min=\"20\"", html);
        Assert.Equal(before, await SnapshotAsync());
    }

    [Theory]
    [InlineData("Create")]
    [InlineData("Edit")]
    public async Task Valid_mvc_forms_still_save(string action)
    {
        var id = action == "Edit" ? await SeedTicketAsync() : 0;
        var url = action == "Create" ? "/Tickets/Create" : $"/Tickets/Edit/{id}";
        using var client = _factory.CreateUserClient();
        var form = await ValidFormAsync(client, url);
        using var response = await client.PostAsync(url, new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var ticket = Assert.Single(await SnapshotAsync());
        Assert.Equal("Valid updated title", ticket.Title);
        Assert.Equal("owner", ticket.OwnerId);
    }

    private Dictionary<string, object?> ValidPayload() => new()
    {
        ["Title"] = "Valid updated title",
        ["Description"] = "A valid and sufficiently long description.",
        ["Status"] = (int)TicketStatus.InProgress,
        ["Priority"] = (int)TicketPriority.High,
        ["ProjectId"] = _projectId,
        ["CategoryId"] = _categoryId
    };

    private async Task<Dictionary<string, string>> ValidFormAsync(HttpClient client, string url)
    {
        using var response = await client.GetAsync(url);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        var input = Regex.Match(html, "<input[^>]*name=\"__RequestVerificationToken\"[^>]*>").Value;
        var token = Regex.Match(input, "value=\"([^\"]+)\"").Groups[1].Value;
        Assert.NotEmpty(token);
        var form = ValidPayload().ToDictionary(p => p.Key, p => p.Value!.ToString()!);
        form["__RequestVerificationToken"] = WebUtility.HtmlDecode(token);
        return form;
    }

    private static string ApiUrl(string method, int id) => method == "POST" ? "/api/tickets" : $"/api/tickets/{id}";

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, string method, int id,
        Dictionary<string, object?> payload)
    {
        using var request = new HttpRequestMessage(new HttpMethod(method), ApiUrl(method, id))
        {
            Content = JsonContent.Create(payload)
        };
        return await client.SendAsync(request);
    }

    private static async Task AssertValidationProblemAsync(HttpResponseMessage response, params string[] fields)
    {
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.NotNull(problem);
        Assert.Equal(400, problem.Status);
        Assert.False(string.IsNullOrWhiteSpace(problem.Title));
        Assert.NotEmpty(problem.Errors);
        foreach (var field in fields)
        {
            Assert.True(problem.Errors.TryGetValue(field, out var errors), $"Missing error for {field}");
            Assert.All(errors!, message => Assert.False(string.IsNullOrWhiteSpace(message)));
            Assert.NotEmpty(errors!);
        }
    }

    private async Task<int> SeedTicketAsync(string ownerId = "owner")
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ticket = new Ticket
        {
            Title = "Original ticket title", Description = "Original ticket description, before the request.",
            Status = TicketStatus.New, Priority = TicketPriority.Low,
            ProjectId = _projectId, CategoryId = _categoryId, OwnerId = ownerId,
            CreatedAt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            UpdatedAt = new DateTime(2025, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };
        context.Tickets.Add(ticket);
        await context.SaveChangesAsync();
        return ticket.Id;
    }

    private async Task<TicketSnapshot[]> SnapshotAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await context.Tickets.AsNoTracking().OrderBy(t => t.Id)
            .Select(t => new TicketSnapshot(t.Id, t.Title, t.Description, t.Status, t.Priority,
                t.ProjectId, t.CategoryId, t.OwnerId, t.CreatedAt, t.UpdatedAt)).ToArrayAsync();
    }

    private sealed record TicketSnapshot(int Id, string Title, string Description, TicketStatus Status,
        TicketPriority Priority, int ProjectId, int CategoryId, string OwnerId, DateTime CreatedAt, DateTime? UpdatedAt);
}
