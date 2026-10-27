using Nomadica.Application.DTOs.Groups;
using Nomadica.Domain.Entities;

namespace Nomadica.Application.Common;

public static class GroupMapping
{
    public static GroupSummaryDto ToSummaryDto(CommunityGroup group, GroupMembership? membership)
        => ToSummaryDto(group, membership is not null, membership?.Role.ToString());

    public static GroupSummaryDto ToSummaryDto(CommunityGroup group, bool isMember, string? myRole)
        => new(
            Id: group.Id.Value,
            Slug: group.Slug,
            Name: group.Name,
            Description: group.Description,
            City: group.City,
            Country: group.Country,
            Latitude: group.Latitude,
            Longitude: group.Longitude,
            MemberCount: group.MemberCount,
            IsMember: isMember,
            MyRole: myRole);
}
