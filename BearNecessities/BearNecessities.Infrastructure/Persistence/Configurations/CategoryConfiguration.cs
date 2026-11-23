using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using BearNecessities.Domain.Entities;
using BearNecessities.Domain.ValueObjects;

namespace BearNecessities.Infrastructure.Persistence.Configurations;

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
            (1, 104, "tai-chi",             "Tai Chi & Qigong"),
            (1, 106, "acro-yoga",           "Acro Yoga"),
            // Kalaripayattu, Muay Thai, kung fu; capoeira and aikido have their own
            (1, 107, "martial-arts",        "Traditional Martial Arts"),
            (1, 111, "capoeira",            "Capoeira"),
            (1, 112, "aikido",              "Aikido"),
            (1, 108, "contact-improv",      "Contact Improvisation"),
            (1, 109, "circus-arts",         "Circus Arts"),
            // Mobility, handstands, floor work and Ido Portal-style movement practice
            (1, 110, "movement-flow",       "Movement & Flow"),

            // Events (parent 2)
            (2, 201, "festival",            "Festival"),
            (2, 202, "retreat",             "Retreat"),
            (2, 203, "workshop",            "Workshop"),
            (2, 204, "ceremony",            "Ceremony"),
            (2, 205, "gathering",           "Gathering"),

            // Volunteering (parent 3)
            (3, 301, "wwoofing",            "WWOOFing"),
            (3, 302, "permaculture",        "Permaculture"),
            (3, 303, "community-garden",    "Community Garden"),
            (3, 304, "environmental-work",  "Environmental Work"),

            // CoWorking (parent 4); co-living keeps its 3xx id from when it sat under Volunteering
            (4, 401, "coworking-space",     "Coworking & Nomad Hubs"),
            (4, 305, "co-living",           "Co-Living"),
            (4, 404, "maker-space",         "Maker Space"),

            // Crafting (parent 5)
            (5, 501, "pottery",             "Pottery"),
            (5, 503, "woodworking",         "Woodcraft & Carving"),
            (5, 504, "jewelry-making",      "Jewelry Making"),
            (5, 505, "painting",            "Painting"),
            (5, 506, "natural-building",    "Natural Building & Bamboo"),
            // Weaving, dyeing, macramé, basketry; buying finished pieces lives under Markets → "Boho Clothing & Jewelry"
            (5, 507, "macrame-fiber-arts",  "Weaving, Macramé & Fiber Arts"),

            // Healing (parent 6); sound baths count as sound healing
            (6, 601, "sound-healing",       "Sound Healing"),
            (6, 602, "energy-healing",      "Energy Healing & Cleansing"),
            (6, 604, "reiki",               "Reiki"),
            (6, 606, "somatic-therapy",     "Somatic Therapy"),
            (6, 607, "thai-massage",        "Thai Massage"),
            (6, 609, "acupuncture",         "Acupuncture, Tuina & Chinese Medicine"),
            // Named bodywork styles instead of a generic "Massage", which pulled in ordinary spas
            (6, 610, "watsu",               "Watsu"),
            (6, 611, "lomi-lomi",           "Lomi Lomi"),
            (6, 612, "shiatsu",             "Shiatsu"),

            // Music & Sound (parent 7)
            // Chanting of every tradition; a separate "Chanting" overlapped almost entirely
            (7, 702, "kirtan",              "Kirtan & Mantra"),
            (7, 703, "drumming-circle",     "Drum & Song Circles"),
            (7, 704, "live-music",          "Live Music"),
            (7, 706, "dj-set",              "DJ Set"),

            // Sexuality (parent 8)
            (8, 801, "tantra",              "Tantra"),
            (8, 802, "conscious-sexuality", "Conscious Intimacy"),
            (8, 804, "shibari",             "Shibari"),

            // Spirituality (parent 9)
            (9, 901, "meditation",          "Meditation"),
            (9, 902, "astrology",           "Astrology, Numerology, Tarot & Divination"),
            (9, 906, "sacred-ceremony",     "Sacred Sites & Rituals"),
            (9, 907, "vipassana",           "Vipassana"),

            // Wellness (parent 10)
            (10, 1001, "nutrition",         "Nutrition & Detox"),
            (10, 1002, "ayurveda",          "Ayurveda"),
            // Also takes in the old "Plant Medicine", a name the scene reads as psychedelic ceremonies
            (10, 1003, "herbalism",         "Herbalism & Traditional Medicine"),
            (10, 1006, "cold-therapy",      "Cold Therapy"),
            // Wim Hof, rebirthing, holotropic; keeps its 1xx id from when it sat under Movement
            (10, 105,  "breathwork",        "Breathwork"),

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
            // Keeps its 9xx id from when it sat under Spirituality
            (12, 903,  "shamanism",         "Shamanism"),

            // Markets (parent 13)
            (13, 1301, "hippie-market",     "Hippie & Tribal Markets"),
            (13, 1302, "artisan-market",    "Artisan & Craft Markets"),
            (13, 1303, "spiritual-supplies","Spiritual Supplies"),
            (13, 1304, "tribal-clothing",   "Boho Clothing & Jewelry"),
            (13, 1306, "farmers-market",    "Farmers Market"),

            // Food (parent 14)
            (14, 1401, "vegan-restaurant",  "Vegan, Vegetarian & Raw"),
            (14, 1402, "indian-food",       "Indian Food"),
            (14, 1403, "health-food-cafe",  "Health Food Café"),
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
