using Microsoft.EntityFrameworkCore;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.Enums;
using BearNecessities.Domain.ValueObjects;
using System.Text.Json;

namespace BearNecessities.Infrastructure.Persistence;

public static class DemoDataSeeder
{
    public static async Task SeedDemoDataAsync(BearNecessitiesDbContext context)
    {
        // Seed only into an empty database — never wipe user-generated data
        var userCount = await context.Users.CountAsync();
        if (userCount > 0)
        {
            Console.WriteLine("Users exist, skipping demo seed");
            return;
        }

        try
        {
            var demoUser = User.Create("demo@bearnecessities.app", "Bear Necessities Demo");
            demoUser.SetPasswordHash(new Services.BcryptPasswordHasher().Hash("Demo1234!"));
            demoUser.AddRole(UserRole.Traveler);
            demoUser.AddRole(UserRole.Organizer);
            demoUser.AddRole(UserRole.Admin);
            demoUser.ApproveMembership();

            context.Users.Add(demoUser);
            await context.SaveChangesAsync();

            // Get or create demo categories
            var categories = await LoadCategoriesAsync(context);

            // Create 10 demo places
            var places = CreateDemoPlaces(demoUser.Id, categories);
            foreach (var place in places)
                context.Places.Add(place);

            await context.SaveChangesAsync();

            // Create 3 demo events
            var events = CreateDemoEvents(demoUser.Id, places, categories);
            foreach (var evt in events)
                context.Events.Add(evt);

            await context.SaveChangesAsync();

            Console.WriteLine("Demo data seeded: 10 places, 3 events");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error seeding demo data: {ex.Message}");
            throw;
        }
    }

    // The category tree is seeded by migrations; demo listings pick from its subcategories by name
    private static async Task<Dictionary<string, Category>> LoadCategoriesAsync(BearNecessitiesDbContext context)
    {
        var categories = await context.Categories.ToListAsync();
        return categories
            .GroupBy(category => category.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);
    }

    // A typo here would otherwise leave a demo listing out of every filter without anyone noticing
    private static Category Subcategory(Dictionary<string, Category> categories, string name) =>
        categories.TryGetValue(name, out var category)
            ? category
            : throw new InvalidOperationException($"Demo data names an unknown category '{name}'.");

    private static List<Place> CreateDemoPlaces(UserId ownerId, Dictionary<string, Category> categories)
    {
        var places = new List<Place>
        {
            CreatePlace("Wonderfruit Festival Grounds", 12.9236m, 100.8825m, "Pattaya, Thailand",
                "An annual music and arts festival in Thailand featuring world-class performers and spiritual workshops.",
                ownerId, new[] { "Festival", "Live Music" }, categories, isPermanent: false),

            CreatePlace("The Yoga Barn", -8.5069m, 115.2625m, "Ubud, Bali, Indonesia",
                "A yoga and wellness sanctuary nestled in the heart of Bali's spiritual center with daily classes and retreats.",
                ownerId, new[] { "Yoga", "Sound Healing" }, categories, isPermanent: true),

            CreatePlace("Earthship Biotecture", 36.4072m, -105.5731m, "Taos, New Mexico, USA",
                "Innovative sustainable community combining passive solar design with volunteer opportunities and collaborative living.",
                ownerId, new[] { "Natural Building", "Eco-Village", "Skill Share" }, categories, isPermanent: true),

            CreatePlace("Osho Meditation Resort", 18.5369m, 73.8886m, "Pune, India",
                "A world-renowned meditation resort offering transformative experiences and spiritual growth programs.",
                ownerId, new[] { "Meditation", "Retreat" }, categories, isPermanent: true),

            CreatePlace("Playa del Carmen Ecstatic Dance", 20.6296m, -87.0739m, "Playa del Carmen, Mexico",
                "A thriving movement and ecstatic dance community on the Caribbean coast with daily dance sessions and workshops.",
                ownerId, new[] { "Ecstatic Dance" }, categories, isPermanent: true),

            CreatePlace("Rainbow Serpent Festival", -37.1m, 143.4m, "Victoria, Australia",
                "Australia's premier electronic music and arts festival celebrating consciousness, community, and creativity.",
                ownerId, new[] { "Festival", "DJ Set" }, categories, isPermanent: false),

            CreatePlace("Tamera Peace Research Village", 37.6748m, -8.3706m, "Alentejo, Portugal",
                "An intentional community pioneering sustainable and cooperative living with opportunities for volunteers and researchers.",
                ownerId, new[] { "Eco-Village", "Co-Living", "Permaculture" }, categories, isPermanent: true),

            CreatePlace("Esalen Institute", 36.1388m, -121.6497m, "Big Sur, California, USA",
                "A legendary retreat center on the cliffs above the Pacific Ocean offering transformative workshops and wellness programs.",
                ownerId, new[] { "Retreat", "Hot Springs", "Workshop" }, categories, isPermanent: true),

            CreatePlace("Sagrada Familia Tantra Temple", 41.4036m, 2.1744m, "Barcelona, Spain",
                "An intimate sacred space for tantra teachings, conscious sexuality exploration, and spiritual intimacy practices.",
                ownerId, new[] { "Tantra", "Conscious Sexuality" }, categories, isPermanent: true),

            CreatePlace("Vipassana Meditation Center", 20.4500m, 75.0100m, "Dhamma Giri, India",
                "A traditional Vipassana meditation center offering 10-day silent retreat courses for spiritual development.",
                ownerId, new[] { "Vipassana", "Retreat" }, categories, isPermanent: true)
        };

        return places;
    }

    private static Place CreatePlace(string name, decimal lat, decimal lng, string location, string description,
        UserId ownerId, string[] categoryNames, Dictionary<string, Category> categories, bool isPermanent)
    {
        var place = Place.Create(name, lat, lng, "UTC", ownerId);
        place.SetDescription(description);
        place.SetAddress(null, location.Split(',')[0].Trim(), location.Split(',').Last().Trim(), null);
        place.SetIsPermanent(isPermanent);
        place.UpdateCoordinates(lat, lng,
            $"h3_res8_{lat:F4}_{lng:F4}",
            $"h3_res5_{lat:F2}_{lng:F2}");
        place.Submit();
        place.Approve(ownerId);

        foreach (var categoryName in categoryNames)
            place.AddCategory(Subcategory(categories, categoryName));

        return place;
    }

    private static List<Event> CreateDemoEvents(UserId ownerId, List<Place> places, Dictionary<string, Category> categories)
    {
        var events = new List<Event>();

        // Weekly Ecstatic Dance @ The Yoga Barn
        var yogaBarn = places.FirstOrDefault(p => p.Name.Contains("Yoga Barn"));
        if (yogaBarn != null)
        {
            var evt1 = Event.Create("Weekly Ecstatic Dance @ The Yoga Barn", EventType.Recurring, "UTC", ownerId);
            evt1.SetDescription("Join us every Sunday morning for ecstatic dance and movement celebration.");
            evt1.SetRRule("FREQ=WEEKLY;BYDAY=SU");
            evt1.SetPlaceId(yogaBarn.Id);
            evt1.Submit();
            evt1.Approve(ownerId);
            evt1.AddCategory(Subcategory(categories, "Ecstatic Dance"));
            events.Add(evt1);
        }

        // Vipassana 10-Day Course
        var vipassana = places.FirstOrDefault(p => p.Name.Contains("Vipassana"));
        if (vipassana != null)
        {
            var evt2 = Event.Create("Vipassana 10-Day Course", EventType.OneTime, "UTC", ownerId);
            evt2.SetDescription("An intensive 10-day silent meditation retreat for deep spiritual practice.");
            evt2.SetPlaceId(vipassana.Id);
            evt2.SetStartAt(DateTime.UtcNow.AddMonths(1));
            evt2.SetEndAt(DateTime.UtcNow.AddMonths(1).AddDays(10));
            evt2.Submit();
            evt2.Approve(ownerId);
            evt2.AddCategory(Subcategory(categories, "Vipassana"));
            events.Add(evt2);
        }

        // Sound Healing Circle @ Esalen
        var esalen = places.FirstOrDefault(p => p.Name.Contains("Esalen"));
        if (esalen != null)
        {
            var evt3 = Event.Create("Sound Healing Circle @ Esalen", EventType.Recurring, "UTC", ownerId);
            evt3.SetDescription("Monthly sound healing sessions using gongs, singing bowls, and voice for transformation.");
            evt3.SetRRule("FREQ=MONTHLY;BYDAY=1SA");
            evt3.SetPlaceId(esalen.Id);
            evt3.Submit();
            evt3.Approve(ownerId);
            evt3.AddCategory(Subcategory(categories, "Sound Healing"));
            events.Add(evt3);
        }

        return events;
    }

}
