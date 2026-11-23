using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.Services.Trips;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.ValueObjects;
using BearNecessities.Infrastructure.Trips;

namespace BearNecessities.Infrastructure.Tests.Trips;

public class TripPlannerTests
{
    private readonly Category _yoga = Category.Create("yoga", "Yoga");

    private sealed class FakeGemini : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _answerJson;
        public int Calls { get; private set; }
        public string? LastBody { get; private set; }
        public string? LastKeyHeader { get; private set; }

        public FakeGemini(HttpStatusCode status, string answerJson = "{}")
        {
            _status = status;
            _answerJson = answerJson;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastBody = await request.Content!.ReadAsStringAsync(cancellationToken);
            LastKeyHeader = request.Headers.TryGetValues("x-goog-api-key", out var values) ? values.Single() : null;
            var envelope = new JsonObject
            {
                ["candidates"] = new JsonArray(new JsonObject
                {
                    ["content"] = new JsonObject { ["parts"] = new JsonArray(new JsonObject { ["text"] = _answerJson }) }
                })
            };
            return new HttpResponseMessage(_status) { Content = new StringContent(envelope.ToJsonString(), Encoding.UTF8, "application/json") };
        }
    }

    private List<Place> Places(int count) => Enumerable.Range(1, count).Select(i =>
    {
        var place = Place.Create($"Shala {i}", 38m + i * 0.01m, 23m, "UTC", new UserId(Guid.NewGuid()));
        place.AddCategory(_yoga);
        return place;
    }).ToList();

    private ItineraryPlanningInput Input(List<Place> places, int days = 2, int stopsPerDay = 2) =>
        new(places, [_yoga.Id.Value], Coordinates.Create(38m, 23m), days, stopsPerDay, "quiet yoga days");

    private static FallbackItineraryPlanner Planner(FakeGemini fake, string? apiKey = "test-key", int maxPerMinute = 10)
    {
        var settings = Options.Create(new GeminiSettings { ApiKey = apiKey, MaxRequestsPerMinute = maxPerMinute });
        var gemini = new GeminiItineraryPlanner(new HttpClient(fake), settings);
        return new FallbackItineraryPlanner(gemini, new SmartItineraryPlanner(), new AiPlannerBudget(TimeProvider.System),
            settings, NullLogger<FallbackItineraryPlanner>.Instance);
    }

    [Fact]
    public async Task PlanAsync_GeminiAnswer_ShouldKeepOnlyValidUnusedPlaces()
    {
        // 9 doesn't exist, 2 repeats, and day one lists three stops where two are allowed
        const string answer = """
            {"days":[
              {"title":"Shala morning","stops":[{"place":1,"reason":"Gentle start"},{"place":9,"reason":"x"},{"place":2,"reason":"Calm"},{"place":3,"reason":"Too many"}]},
              {"title":"Day two","stops":[{"place":2,"reason":"Repeat"},{"place":4,"reason":"Sunset class"}]}
            ]}
            """;
        var fake = new FakeGemini(HttpStatusCode.OK, answer);
        var places = Places(4);

        var plan = await Planner(fake).PlanAsync(Input(places));

        plan.Planner.Should().Be("gemini");
        plan.Days[0].Title.Should().Be("Shala morning");
        plan.Days[0].Stops.Select(stop => stop.Place.Name).Should().Equal("Shala 1", "Shala 2");
        plan.Days[1].Stops.Select(stop => stop.Place.Name).Should().Equal("Shala 4");
        plan.Days[1].Stops[0].Reason.Should().Be("Sunset class");
        fake.LastKeyHeader.Should().Be("test-key");
        fake.LastBody.Should().Contain("quiet yoga days").And.Contain("responseJsonSchema");
    }

    [Fact]
    public async Task PlanAsync_GeminiRateLimited_ShouldFallBackToSmartPlanner()
    {
        var plan = await Planner(new FakeGemini(HttpStatusCode.TooManyRequests)).PlanAsync(Input(Places(4)));

        plan.Planner.Should().Be("smart");
        plan.Days.SelectMany(day => day.Stops).Should().HaveCount(4);
    }

    [Fact]
    public async Task PlanAsync_UnusableAnswer_ShouldFallBackToSmartPlanner()
    {
        var plan = await Planner(new FakeGemini(HttpStatusCode.OK, """{"days":[{"title":"x","stops":[{"place":99,"reason":"x"}]}]}"""))
            .PlanAsync(Input(Places(4)));

        plan.Planner.Should().Be("smart");
    }

    [Fact]
    public async Task PlanAsync_NoApiKey_ShouldNotCallGemini()
    {
        var fake = new FakeGemini(HttpStatusCode.OK);

        var plan = await Planner(fake, apiKey: null).PlanAsync(Input(Places(4)));

        plan.Planner.Should().Be("smart");
        fake.Calls.Should().Be(0);
    }

    [Fact]
    public async Task PlanAsync_OverTheMinuteBudget_ShouldUseSmartPlanner()
    {
        const string answer = """{"days":[{"title":"t","stops":[{"place":1,"reason":"r"}]}]}""";
        var fake = new FakeGemini(HttpStatusCode.OK, answer);
        var planner = Planner(fake, maxPerMinute: 1);

        var first = await planner.PlanAsync(Input(Places(4)));
        var second = await planner.PlanAsync(Input(Places(4)));

        first.Planner.Should().Be("gemini");
        second.Planner.Should().Be("smart");
        fake.Calls.Should().Be(1);
    }
}
