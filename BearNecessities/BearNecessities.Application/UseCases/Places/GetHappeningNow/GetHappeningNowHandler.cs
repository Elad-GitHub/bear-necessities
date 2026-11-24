using BearNecessities.Application.Exceptions;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.Services.Places;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Application.UseCases.Places.GetHappeningNow;

public sealed record GetHappeningNowRequest(decimal Latitude, decimal Longitude, double? RadiusKm = null);

/// <summary>An event at a nearby place that is on now or starts within the next few hours (StartAt is UTC).</summary>
public sealed record HappeningEventDto(
    Guid Id, string Title, Guid PlaceId, string PlaceName, decimal Latitude, decimal Longitude,
    double DistanceKm, DateTime StartAt, bool IsOngoing);

/// <summary>A nearby place open right now; ClosesAt is its local closing time ("18:00"), null when it never closes.</summary>
public sealed record OpenPlaceDto(
    Guid Id, string Name, decimal Latitude, decimal Longitude, double DistanceKm,
    string? ClosesAt, List<Guid> CategoryIds, string? ThumbnailUrl);

/// <summary>PlacesWithHours says how many nearby places list their hours, so the page can be honest about gaps.</summary>
public sealed record GetHappeningNowResponse(
    List<HappeningEventDto> Events, List<OpenPlaceDto> OpenPlaces, int PlacesNearby, int PlacesWithHours);

/// <summary>"Happening now near me": places open at this moment and events on now or soon, closest first.</summary>
public sealed class GetHappeningNowHandler : IRequestHandler<GetHappeningNowRequest, GetHappeningNowResponse>
{
    public const double DefaultRadiusKm = 25;
    public const double MaxRadiusKm = 50;
    public const int MaxOpenPlaces = 40;
    public static readonly TimeSpan UpcomingWindow = TimeSpan.FromHours(12);

    private readonly IUnitOfWork _unitOfWork;
    private readonly ITimeZoneLookup _timeZones;
    private readonly TimeProvider _clock;

    public GetHappeningNowHandler(IUnitOfWork unitOfWork, ITimeZoneLookup timeZones, TimeProvider clock)
    {
        _unitOfWork = unitOfWork;
        _timeZones = timeZones;
        _clock = clock;
    }

    public async Task<GetHappeningNowResponse> HandleAsync(GetHappeningNowRequest request, CancellationToken cancellationToken = default)
    {
        var center = ToCenter(request);
        var radiusKm = Math.Clamp(request.RadiusKm ?? DefaultRadiusKm, 1, MaxRadiusKm);
        var nowUtc = _clock.GetUtcNow().UtcDateTime;

        var nearby = (await _unitOfWork.Places.FindNearbyAsync(center, radiusKm, cancellationToken))
            .Select(place => (Place: place, DistanceKm: center.DistanceKmTo(Coordinates.Create(place.Latitude, place.Longitude))))
            .Where(item => item.DistanceKm <= radiusKm)
            .ToList();

        var nextEvents = await _unitOfWork.Events.FindNextEventByPlacesAsync(
            nearby.Select(item => item.Place.Id), nowUtc, nowUtc + UpcomingWindow, cancellationToken);

        var events = nearby
            .Where(item => nextEvents.TryGetValue(item.Place.Id, out var next) && next.StartAt is not null)
            .Select(item =>
            {
                var next = nextEvents[item.Place.Id];
                return new HappeningEventDto(next.EventId.Value, next.Title, item.Place.Id.Value, item.Place.Name,
                    item.Place.Latitude, item.Place.Longitude, Math.Round(item.DistanceKm, 1), next.StartAt!.Value,
                    next.StartAt <= nowUtc);
            })
            .OrderBy(happening => happening.StartAt)
            .ToList();

        var withHours = nearby
            .Select(item => (item.Place, item.DistanceKm, Schedule: OpeningHoursSchedule.Parse(item.Place.OpeningHours)))
            .Where(item => item.Schedule is not null)
            .ToList();

        var openPlaces = withHours
            .Select(item => (item.Place, item.DistanceKm,
                State: item.Schedule!.StateAt(_timeZones.ToLocalTime(nowUtc, item.Place.Latitude, item.Place.Longitude))))
            .Where(item => item.State.IsOpen)
            .OrderBy(item => item.DistanceKm)
            .Take(MaxOpenPlaces)
            .Select(item => ToOpenPlace(item.Place, item.DistanceKm, item.State))
            .ToList();

        return new GetHappeningNowResponse(events, openPlaces, nearby.Count, withHours.Count);
    }

    private static Coordinates ToCenter(GetHappeningNowRequest request)
    {
        try
        {
            return Coordinates.Create(request.Latitude, request.Longitude);
        }
        catch (ArgumentOutOfRangeException ex)
        {
            throw new ValidationException(new Dictionary<string, string[]> { ["location"] = [ex.Message] });
        }
    }

    private static OpenPlaceDto ToOpenPlace(Place place, double distanceKm, OpenState state) =>
        new(place.Id.Value, place.Name, place.Latitude, place.Longitude, Math.Round(distanceKm, 1),
            state.ClosesAt?.ToString("HH:mm"),
            place.Categories.Select(category => category.Id.Value).ToList(),
            place.PhotoUrl);
}
