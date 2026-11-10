using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Nomadica.Domain.Entities;
using Nomadica.Domain.ValueObjects;

namespace Nomadica.Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    private static CategoryId CatId(int n) =>
        new CategoryId(Guid.Parse($"00000000-0000-0000-0000-{n:D12}"));

    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id)
            .HasConversion(id => id.Value, value => new CategoryId(value));

        builder.Property(c => c.Slug).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Icon).HasMaxLength(100);
        builder.Property(c => c.Color).HasMaxLength(20);

        builder.Property(c => c.ParentId)
            .HasConversion(
                id => id == null ? (Guid?)null : id.Value,
                value => value == null ? null : new CategoryId(value.Value));

        builder.Property(c => c.CreatedAt).HasColumnType("datetime2");
        builder.Property(c => c.UpdatedAt).HasColumnType("datetime2");

        builder.HasIndex(c => c.Slug).IsUnique();
        builder.HasIndex(c => c.ParentId);

        builder.HasOne<Category>()
            .WithMany()
            .HasForeignKey(c => c.ParentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        SeedData(builder);
    }

    private static void SeedData(EntityTypeBuilder<Category> builder)
    {
        var seed = DateTime.SpecifyKind(new DateTime(2024, 1, 1), DateTimeKind.Utc);

        // ── Parents ──────────────────────────────────────────────────────────
        var parents = new[]
        {
            (1,  "movement",       "Movement"),
            (2,  "events",         "Gatherings"),
            (3,  "volunteering",   "Volunteering"),
            (4,  "co-working",     "CoWorking"),
            (5,  "crafting",       "Crafting"),
            (6,  "healing",        "Healing"),
            (7,  "music-sound",    "Music & Sound"),
            (8,  "sexuality",      "Sexuality"),
            (9,  "spirituality",   "Spirituality"),
            (10, "wellness",       "Wellness"),
            (11, "nature",         "Nature"),
            (12, "ceremonies",     "Ceremonies"),
            (13, "markets",        "Markets"),
            (14, "food",           "Food"),
        };

        foreach (var (n, slug, name) in parents)
        {
            builder.HasData(new
            {
                Id = CatId(n),
                Slug = slug,
                Name = name,
                SortOrder = n,
                ParentId = (CategoryId?)null,
                Icon = (string?)null,
                Color = (string?)null,
                CreatedAt = seed,
                UpdatedAt = seed
            });
        }

        // ── Children ─────────────────────────────────────────────────────────
        var children = new (int ParentN, int ChildN, string Slug, string Name)[]
        {
            // Movement (parent 1)
            (1, 101, "yoga",                "Yoga"),
            (1, 102, "ecstatic-dance",      "Ecstatic Dance"),
            (1, 103, "qigong",              "Qigong"),
            (1, 104, "tai-chi",             "Tai Chi"),
            (1, 105, "breathwork",          "Breathwork"),
            (1, 106, "acro-yoga",           "Acro Yoga"),
            (1, 107, "martial-arts",        "Martial Arts"),
            (1, 108, "contact-improv",      "Contact Dance"),
            (1, 109, "circus-arts",         "Circus Arts"),

            // Events (parent 2)
            (2, 201, "festival",            "Festival"),
            (2, 202, "retreat",             "Retreat"),
            (2, 203, "workshop",            "Workshop"),
            (2, 204, "ceremony",            "Ceremony"),
            (2, 205, "gathering",           "Gathering"),
            (2, 206, "conference",          "Conference"),

            // Volunteering (parent 3)
            (3, 301, "wwoofing",            "WWOOFing"),
            (3, 302, "permaculture",        "Permaculture"),
            (3, 303, "community-garden",    "Community Garden"),
            (3, 304, "environmental-work",  "Environmental Work"),
            (3, 305, "co-living",           "Co-Living"),
            (3, 306, "skill-share",         "Skill Share"),

            // CoWorking (parent 4)
            (4, 401, "coworking-space",     "Co-Working Space"),
            (4, 402, "digital-nomad-hub",   "Digital Nomad Hub"),
            (4, 403, "hackerspace",         "Hackerspace"),
            (4, 404, "maker-space",         "Maker Space"),

            // Crafting (parent 5)
            (5, 501, "pottery",             "Pottery"),
            (5, 502, "textile-arts",        "Textile Arts"),
            (5, 503, "woodworking",         "Woodworking"),
            (5, 504, "jewelry-making",      "Jewelry Making"),
            (5, 505, "painting",            "Painting"),
            (5, 506, "natural-building",    "Natural Building"),
            // Workshops to learn macramé; buying finished pieces lives under Markets → "Jewelry & Macramé"
            (5, 507, "macrame-fiber-arts",  "Macramé & Fiber Arts"),

            // Healing (parent 6)
            (6, 601, "sound-healing",       "Sound Healing"),
            (6, 602, "energy-healing",      "Energy Healing"),
            (6, 603, "massage",             "Massage"),
            (6, 604, "reiki",               "Reiki"),
            (6, 605, "plant-medicine",      "Plant Medicine"),
            (6, 606, "somatic-therapy",     "Somatic Therapy"),
            (6, 607, "thai-massage",        "Thai Massage"),
            (6, 608, "tuina",               "Tuina"),
            (6, 609, "acupuncture",         "Acupuncture"),

            // Music & Sound (parent 7)
            (7, 701, "sound-bath",          "Sound Bath"),
            (7, 702, "kirtan",              "Kirtan"),
            (7, 703, "drumming-circle",     "Drumming Circle"),
            (7, 704, "live-music",          "Live Music"),
            (7, 705, "chanting",            "Chanting"),
            (7, 706, "dj-set",              "DJ Set"),
            (7, 707, "singing-circles",     "Singing Circles"),

            // Sexuality (parent 8)
            (8, 801, "tantra",              "Tantra"),
            (8, 802, "conscious-sexuality", "Conscious Sexuality"),
            (8, 803, "intimacy-workshop",   "Intimacy Workshop"),

            // Spirituality (parent 9)
            (9, 901, "meditation",          "Meditation"),
            (9, 902, "astrology",           "Astrology"),
            (9, 903, "shamanism",           "Shamanism"),
            (9, 904, "human-design",        "Human Design"),
            (9, 905, "neo-paganism",        "Neo-Paganism"),
            (9, 906, "sacred-ceremony",     "Sacred Ceremony"),
            (9, 907, "vipassana",           "Vipassana"),

            // Wellness (parent 10)
            (10, 1001, "nutrition",         "Nutrition"),
            (10, 1002, "ayurveda",          "Ayurveda"),
            (10, 1003, "herbalism",         "Herbalism"),
            (10, 1004, "yoga-therapy",      "Yoga Therapy"),
            (10, 1005, "fasting-detox",     "Fasting & Detox"),
            (10, 1006, "cold-therapy",      "Cold Therapy"),

            // Nature (parent 11)
            (11, 1101, "camping",           "Camping Areas"),
            (11, 1102, "farm-stay",         "Farms & Farm Stays"),
            (11, 1103, "hot-springs",       "Hot Springs"),
            (11, 1104, "hiking",            "Hiking & Trekking"),
            (11, 1105, "eco-village",       "Eco-Village"),

            // Ceremonies (parent 12)
            (12, 1201, "ayahuasca",         "Ayahuasca"),
            (12, 1202, "psilocybin",        "Mushroom Ceremonies"),
            (12, 1203, "kambo",             "Kambo"),
            (12, 1204, "cacao-ceremony",    "Cacao Ceremony"),
            (12, 1205, "sweat-lodge",       "Sweat Lodge / Temazcal"),
            (12, 1206, "bufo",              "Bufo / 5-MeO-DMT"),

            // Markets (parent 13)
            (13, 1301, "hippie-market",     "Hippie & Tribal Markets"),
            (13, 1302, "artisan-market",    "Artisan & Craft Markets"),
            (13, 1303, "spiritual-supplies","Spiritual Supplies"),
            (13, 1304, "tribal-clothing",   "Tribal Clothing & Pashminas"),
            (13, 1305, "jewelry-macrame",   "Jewelry & Macramé"),
            (13, 1306, "farmers-market",    "Farmers Market"),

            // Food (parent 14)
            (14, 1401, "vegan-restaurant",  "Vegan & Vegetarian"),
            (14, 1402, "indian-food",       "Indian Food"),
            (14, 1403, "health-food-cafe",  "Health Food Café"),
            (14, 1404, "raw-food",          "Raw & Living Food"),
            (14, 1405, "community-kitchen", "Community Kitchen & Dinners"),
            (14, 1406, "food-workshop",     "Cooking Workshops & Food Events"),
        };

        int sort = 0;
        foreach (var (parentN, childN, slug, name) in children)
        {
            builder.HasData(new
            {
                Id = CatId(childN),
                Slug = slug,
                Name = name,
                SortOrder = ++sort,
                ParentId = CatId(parentN),
                Icon = (string?)null,
                Color = (string?)null,
                CreatedAt = seed,
                UpdatedAt = seed
            });
        }
    }
}
