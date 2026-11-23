using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace BearNecessities.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class MergeNicheSubcategories : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // qigong -> tai-chi, intimacy-workshop -> conscious-sexuality, singing-circles -> drumming-circle,
            // digital-nomad-hub -> coworking-space, tuina -> acupuncture, fasting-detox -> nutrition,
            // yoga-therapy -> yoga, raw-food -> vegan-restaurant, jewelry-macrame -> tribal-clothing
            CategoryLinkMoves.MoveAndDrop(migrationBuilder,
                moves: [(103, 104), (803, 802), (707, 703), (402, 401), (608, 609),
                        (1005, 1001), (1004, 101), (1404, 1401), (1305, 1304)],
                drops: []);

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000103"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000402"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000608"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000707"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000803"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001004"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001005"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001305"));

            migrationBuilder.DeleteData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001404"));

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000104"),
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "Tai Chi & Qigong", 3 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000105"),
                column: "SortOrder",
                value: 53);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000106"),
                column: "SortOrder",
                value: 4);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000107"),
                column: "SortOrder",
                value: 5);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000108"),
                column: "SortOrder",
                value: 8);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000109"),
                column: "SortOrder",
                value: 9);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000110"),
                column: "SortOrder",
                value: 10);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000111"),
                column: "SortOrder",
                value: 6);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000112"),
                column: "SortOrder",
                value: 7);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000201"),
                column: "SortOrder",
                value: 11);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000202"),
                column: "SortOrder",
                value: 12);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000203"),
                column: "SortOrder",
                value: 13);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000204"),
                column: "SortOrder",
                value: 14);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000205"),
                column: "SortOrder",
                value: 15);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000301"),
                column: "SortOrder",
                value: 16);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000302"),
                column: "SortOrder",
                value: 17);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000303"),
                column: "SortOrder",
                value: 18);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000304"),
                column: "SortOrder",
                value: 19);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000305"),
                column: "SortOrder",
                value: 21);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000401"),
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "Coworking & Nomad Hubs", 20 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000404"),
                column: "SortOrder",
                value: 22);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000501"),
                column: "SortOrder",
                value: 23);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000503"),
                column: "SortOrder",
                value: 24);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000504"),
                column: "SortOrder",
                value: 25);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000505"),
                column: "SortOrder",
                value: 26);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000506"),
                column: "SortOrder",
                value: 27);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000507"),
                column: "SortOrder",
                value: 28);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000601"),
                column: "SortOrder",
                value: 29);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000602"),
                column: "SortOrder",
                value: 30);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000604"),
                column: "SortOrder",
                value: 31);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000606"),
                column: "SortOrder",
                value: 32);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000607"),
                column: "SortOrder",
                value: 33);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000609"),
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "Acupuncture, Tuina & Chinese Medicine", 34 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000610"),
                column: "SortOrder",
                value: 35);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000611"),
                column: "SortOrder",
                value: 36);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000612"),
                column: "SortOrder",
                value: 37);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000702"),
                column: "SortOrder",
                value: 38);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000703"),
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "Drum & Song Circles", 39 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000704"),
                column: "SortOrder",
                value: 40);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000706"),
                column: "SortOrder",
                value: 41);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000801"),
                column: "SortOrder",
                value: 42);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000802"),
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "Conscious Intimacy", 43 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000804"),
                column: "SortOrder",
                value: 44);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000901"),
                column: "SortOrder",
                value: 45);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000902"),
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "Astrology, Numerology, Tarot & Divination", 46 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000903"),
                column: "SortOrder",
                value: 65);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000906"),
                column: "SortOrder",
                value: 47);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000907"),
                column: "SortOrder",
                value: 48);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001001"),
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "Nutrition & Detox", 49 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001002"),
                column: "SortOrder",
                value: 50);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001003"),
                column: "SortOrder",
                value: 51);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001006"),
                column: "SortOrder",
                value: 52);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001101"),
                column: "SortOrder",
                value: 54);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001102"),
                column: "SortOrder",
                value: 55);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001103"),
                column: "SortOrder",
                value: 56);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001104"),
                column: "SortOrder",
                value: 57);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001105"),
                column: "SortOrder",
                value: 58);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001201"),
                column: "SortOrder",
                value: 59);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001202"),
                column: "SortOrder",
                value: 60);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001203"),
                column: "SortOrder",
                value: 61);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001204"),
                column: "SortOrder",
                value: 62);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001205"),
                column: "SortOrder",
                value: 63);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001206"),
                column: "SortOrder",
                value: 64);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001301"),
                column: "SortOrder",
                value: 66);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001302"),
                column: "SortOrder",
                value: 67);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001303"),
                column: "SortOrder",
                value: 68);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001304"),
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "Boho Clothing & Jewelry", 69 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001306"),
                column: "SortOrder",
                value: 70);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001401"),
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "Vegan, Vegetarian & Raw", 71 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001402"),
                column: "SortOrder",
                value: 72);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001403"),
                column: "SortOrder",
                value: 73);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001405"),
                column: "SortOrder",
                value: 74);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001406"),
                column: "SortOrder",
                value: 75);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000104"),
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "Tai Chi", 4 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000105"),
                column: "SortOrder",
                value: 60);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000106"),
                column: "SortOrder",
                value: 5);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000107"),
                column: "SortOrder",
                value: 6);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000108"),
                column: "SortOrder",
                value: 9);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000109"),
                column: "SortOrder",
                value: 10);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000110"),
                column: "SortOrder",
                value: 11);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000111"),
                column: "SortOrder",
                value: 7);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000112"),
                column: "SortOrder",
                value: 8);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000201"),
                column: "SortOrder",
                value: 12);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000202"),
                column: "SortOrder",
                value: 13);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000203"),
                column: "SortOrder",
                value: 14);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000204"),
                column: "SortOrder",
                value: 15);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000205"),
                column: "SortOrder",
                value: 16);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000301"),
                column: "SortOrder",
                value: 17);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000302"),
                column: "SortOrder",
                value: 18);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000303"),
                column: "SortOrder",
                value: 19);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000304"),
                column: "SortOrder",
                value: 20);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000305"),
                column: "SortOrder",
                value: 23);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000401"),
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "Co-Working Space", 21 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000404"),
                column: "SortOrder",
                value: 24);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000501"),
                column: "SortOrder",
                value: 25);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000503"),
                column: "SortOrder",
                value: 26);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000504"),
                column: "SortOrder",
                value: 27);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000505"),
                column: "SortOrder",
                value: 28);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000506"),
                column: "SortOrder",
                value: 29);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000507"),
                column: "SortOrder",
                value: 30);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000601"),
                column: "SortOrder",
                value: 31);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000602"),
                column: "SortOrder",
                value: 32);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000604"),
                column: "SortOrder",
                value: 33);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000606"),
                column: "SortOrder",
                value: 34);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000607"),
                column: "SortOrder",
                value: 35);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000609"),
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "Acupuncture", 37 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000610"),
                column: "SortOrder",
                value: 38);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000611"),
                column: "SortOrder",
                value: 39);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000612"),
                column: "SortOrder",
                value: 40);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000702"),
                column: "SortOrder",
                value: 41);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000703"),
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "Drumming Circle", 42 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000704"),
                column: "SortOrder",
                value: 43);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000706"),
                column: "SortOrder",
                value: 44);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000801"),
                column: "SortOrder",
                value: 46);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000802"),
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "Conscious Sexuality", 47 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000804"),
                column: "SortOrder",
                value: 49);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000901"),
                column: "SortOrder",
                value: 50);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000902"),
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "Astrology", 51 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000903"),
                column: "SortOrder",
                value: 72);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000906"),
                column: "SortOrder",
                value: 52);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000000907"),
                column: "SortOrder",
                value: 53);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001001"),
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "Nutrition", 54 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001002"),
                column: "SortOrder",
                value: 55);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001003"),
                column: "SortOrder",
                value: 56);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001006"),
                column: "SortOrder",
                value: 59);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001101"),
                column: "SortOrder",
                value: 61);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001102"),
                column: "SortOrder",
                value: 62);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001103"),
                column: "SortOrder",
                value: 63);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001104"),
                column: "SortOrder",
                value: 64);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001105"),
                column: "SortOrder",
                value: 65);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001201"),
                column: "SortOrder",
                value: 66);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001202"),
                column: "SortOrder",
                value: 67);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001203"),
                column: "SortOrder",
                value: 68);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001204"),
                column: "SortOrder",
                value: 69);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001205"),
                column: "SortOrder",
                value: 70);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001206"),
                column: "SortOrder",
                value: 71);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001301"),
                column: "SortOrder",
                value: 73);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001302"),
                column: "SortOrder",
                value: 74);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001303"),
                column: "SortOrder",
                value: 75);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001304"),
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "Tribal Clothing & Pashminas", 76 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001306"),
                column: "SortOrder",
                value: 78);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001401"),
                columns: new[] { "Name", "SortOrder" },
                values: new object[] { "Vegan & Vegetarian", 79 });

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001402"),
                column: "SortOrder",
                value: 80);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001403"),
                column: "SortOrder",
                value: 81);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001405"),
                column: "SortOrder",
                value: 83);

            migrationBuilder.UpdateData(
                table: "Categories",
                keyColumn: "Id",
                keyValue: new Guid("00000000-0000-0000-0000-000000001406"),
                column: "SortOrder",
                value: 84);

            migrationBuilder.InsertData(
                table: "Categories",
                columns: new[] { "Id", "Color", "CreatedAt", "Icon", "Name", "ParentId", "Slug", "SortOrder", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("00000000-0000-0000-0000-000000000103"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Qigong", new Guid("00000000-0000-0000-0000-000000000001"), "qigong", 3, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000402"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Digital Nomad Hub", new Guid("00000000-0000-0000-0000-000000000004"), "digital-nomad-hub", 22, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000608"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Tuina", new Guid("00000000-0000-0000-0000-000000000006"), "tuina", 36, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000707"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Singing Circles", new Guid("00000000-0000-0000-0000-000000000007"), "singing-circles", 45, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000000803"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Intimacy Workshop", new Guid("00000000-0000-0000-0000-000000000008"), "intimacy-workshop", 48, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000001004"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Yoga Therapy", new Guid("00000000-0000-0000-0000-000000000010"), "yoga-therapy", 57, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000001005"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Fasting & Detox", new Guid("00000000-0000-0000-0000-000000000010"), "fasting-detox", 58, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000001305"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Jewelry & Macramé", new Guid("00000000-0000-0000-0000-000000000013"), "jewelry-macrame", 77, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) },
                    { new Guid("00000000-0000-0000-0000-000000001404"), null, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Raw & Living Food", new Guid("00000000-0000-0000-0000-000000000014"), "raw-food", 82, new DateTime(2024, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc) }
                });
        }
    }
}
