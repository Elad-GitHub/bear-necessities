using Microsoft.EntityFrameworkCore.Migrations;

namespace BearNecessities.Infrastructure.Persistence.Migrations;

/// <summary>
/// Moves place and event links off seeded categories that a migration is about to delete,
/// so merging two categories never leaves a place without the surviving one.
/// </summary>
internal static class CategoryLinkMoves
{
    private static readonly (string Table, string OwnerColumn)[] LinkTables =
        [("PlaceCategories", "PlaceId"), ("EventCategories", "EventId")];

    public static void MoveAndDrop(MigrationBuilder migrationBuilder, (int From, int To)[] moves, int[] drops)
    {
        var moveRows = string.Join(", ", moves.Select(move => $"('{SeedId(move.From)}', '{SeedId(move.To)}')"));
        var removed = string.Join(", ", moves.Select(move => move.From).Concat(drops).Select(id => $"'{SeedId(id)}'"));

        foreach (var (table, owner) in LinkTables)
        {
            if (moves.Length > 0)
                migrationBuilder.Sql($@"
                    INSERT INTO {table} ({owner}, CategoryId)
                    SELECT DISTINCT link.{owner}, moves.ToId
                    FROM {table} link
                    JOIN (VALUES {moveRows}) moves(FromId, ToId) ON link.CategoryId = moves.FromId
                    WHERE NOT EXISTS (SELECT 1 FROM {table} existing
                                      WHERE existing.{owner} = link.{owner} AND existing.CategoryId = moves.ToId);");
            migrationBuilder.Sql($"DELETE FROM {table} WHERE CategoryId IN ({removed});");
        }
    }

    // Same id scheme as CategoryConfiguration's seed
    private static string SeedId(int number) => $"00000000-0000-0000-0000-{number:D12}";
}
