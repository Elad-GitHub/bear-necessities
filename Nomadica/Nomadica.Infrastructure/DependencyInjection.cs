using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Nomadica.Application.Interfaces;
using Nomadica.Application.UseCases.Auth.Login;
using Nomadica.Application.UseCases.Auth.RefreshToken;
using Nomadica.Application.UseCases.Auth.Register;
using Nomadica.Application.UseCases.Categories.GetCategoriesTree;
using Nomadica.Application.UseCases.Events.ApproveEvent;
using Nomadica.Application.UseCases.Events.CancelEvent;
using Nomadica.Application.UseCases.Events.CreateEvent;
using Nomadica.Application.UseCases.Events.GetEventById;
using Nomadica.Application.UseCases.Events.GetEventsByPlace;
using Nomadica.Application.UseCases.Events.GetEventsByViewport;
using Nomadica.Application.UseCases.Events.GetMyEvents;
using Nomadica.Application.UseCases.Groups.CreateGroup;
using Nomadica.Application.UseCases.Groups.CreateGroupPost;
using Nomadica.Application.UseCases.Groups.GetGroupById;
using Nomadica.Application.UseCases.Groups.GetGroupPosts;
using Nomadica.Application.UseCases.Groups.GetGroups;
using Nomadica.Application.UseCases.Groups.GetMyGroups;
using Nomadica.Application.UseCases.Groups.JoinGroup;
using Nomadica.Application.UseCases.Groups.LeaveGroup;
using Nomadica.Application.UseCases.Trips.GetTripSuggestions;
using Nomadica.Application.UseCases.Trips.PlanItinerary;
using Nomadica.Application.Services.Trips;
using Nomadica.Application.UseCases.Users.GetPendingMembers;
using Nomadica.Application.UseCases.Users.ReviewMembership;
using Nomadica.Application.UseCases.Events.GetPendingEvents;
using Nomadica.Application.UseCases.Events.RejectEvent;
using Nomadica.Application.UseCases.Events.UpdateEvent;
using Nomadica.Application.UseCases.Reviews.AdminRemoveReview;
using Nomadica.Application.UseCases.Reviews.CreateReview;
using Nomadica.Application.UseCases.Reviews.FlagReview;
using Nomadica.Application.UseCases.Reviews.GetMyReviews;
using Nomadica.Application.UseCases.Reviews.GetReviewsByTarget;
using Nomadica.Application.UseCases.Reviews.SubmitCounterReview;
using Nomadica.Application.UseCases.Media.ConfirmUpload;
using Nomadica.Application.UseCases.Media.DeleteMedia;
using Nomadica.Application.UseCases.Media.GetMediaByOwner;
using Nomadica.Application.UseCases.Media.GetMediaContent;
using Nomadica.Application.UseCases.Media.ReorderMedia;
using Nomadica.Application.UseCases.Media.RequestUpload;
using Nomadica.Application.UseCases.Rsvps.CancelRsvp;
using Nomadica.Application.UseCases.Rsvps.CreateOrUpdateRsvp;
using Nomadica.Application.UseCases.Rsvps.GetEventRsvps;
using Nomadica.Application.UseCases.Rsvps.GetMyRsvps;
using Nomadica.Application.UseCases.Messages.DeleteMessage;
using Nomadica.Application.UseCases.Messages.GetConversations;
using Nomadica.Application.UseCases.Messages.GetMessages;
using Nomadica.Application.UseCases.Messages.GetUnreadCount;
using Nomadica.Application.UseCases.Messages.SendMessage;
using Nomadica.Application.UseCases.SavedItems.GetSavedItems;
using Nomadica.Application.UseCases.SavedItems.ToggleSave;
using Nomadica.Application.UseCases.Telemetry.IngestTelemetry;
using Nomadica.Infrastructure.Telemetry;
using Nomadica.Application.UseCases.Places.ApprovePlace;
using Nomadica.Application.UseCases.Places.CreatePlace;
using Nomadica.Application.UseCases.Places.DeletePlace;
using Nomadica.Application.UseCases.Places.GetMyPlaces;
using Nomadica.Application.UseCases.Places.GetPendingPlaces;
using Nomadica.Application.UseCases.Places.GetPlaceById;
using Nomadica.Application.UseCases.Places.GetPlacesByViewport;
using Nomadica.Application.UseCases.Places.RejectPlace;
using Nomadica.Application.UseCases.Places.SearchPlaces;
using Nomadica.Application.UseCases.Places.UpdatePlace;
using Nomadica.Domain.Interfaces;
using Nomadica.Infrastructure.Persistence;
using Nomadica.Infrastructure.Repositories;
using Nomadica.Infrastructure.Services;
using Nomadica.Shared.Constants;
using Microsoft.Extensions.Logging;

namespace Nomadica.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<NomadiaDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.Configure<JwtSettings>(configuration.GetSection("JwtSettings"));

        var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>()
            ?? throw new InvalidOperationException("JwtSettings configuration is missing.");

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.MapInboundClaims = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                    ValidateIssuer = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });

        services.AddAuthorization();

        services.AddScoped<IPlaceRepository, PlaceRepository>();
        services.AddScoped<IEventRepository, EventRepository>();
        services.AddScoped<IEventOccurrenceRepository, EventOccurrenceRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IMediaRepository, MediaRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IListingChangeLogRepository, ListingChangeLogRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IReportRepository, ReportRepository>();
        services.AddScoped<IRsvpRepository, RsvpRepository>();
        services.AddScoped<IConversationRepository, ConversationRepository>();
        services.AddScoped<ISavedItemRepository, SavedItemRepository>();
        services.AddScoped<IGroupRepository, GroupRepository>();

        // Bulk import pipeline (OSM + CSV → shared importer)
        services.AddScoped<IPlaceImportService, Imports.PlaceImportService>();
        services.AddScoped<Imports.CsvImportService>();
        services.AddHttpClient<Imports.OsmImportService>(client =>
        {
            client.Timeout = TimeSpan.FromMinutes(3);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Nomadica/1.0 (community discovery; import)");
        });
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IOccurrenceExpander, OccurrenceExpander>();
        services.AddScoped<IOccurrenceRefreshService, OccurrenceRefreshService>();
        services.AddHostedService<OccurrenceRefreshBackgroundService>();

        services.AddScoped<GetPlacesByViewportHandler>();
        services.AddScoped<GetPlaceByIdHandler>();
        services.AddScoped<GetCategoriesTreeHandler>();
        services.AddScoped<CreatePlaceHandler>();
        services.AddScoped<UpdatePlaceHandler>();
        services.AddScoped<ApprovePlaceHandler>();
        services.AddScoped<RejectPlaceHandler>();
        services.AddScoped<DeletePlaceHandler>();
        services.AddScoped<GetPendingPlacesHandler>();
        services.AddScoped<SearchPlacesHandler>();
        services.AddScoped<GetMyPlacesHandler>();
        services.AddScoped<RegisterHandler>();
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefreshTokenHandler>();

        services.AddScoped<CreateEventHandler>();
        services.AddScoped<UpdateEventHandler>();
        services.AddScoped<ApproveEventHandler>();
        services.AddScoped<RejectEventHandler>();
        services.AddScoped<CancelEventHandler>();
        services.AddScoped<GetEventByIdHandler>();
        services.AddScoped<GetPendingEventsHandler>();
        services.AddScoped<GetMyEventsHandler>();

        services.AddScoped<CreateGroupHandler>();
        services.AddScoped<GetGroupsHandler>();
        services.AddScoped<GetGroupByIdHandler>();
        services.AddScoped<GetMyGroupsHandler>();
        services.AddScoped<JoinGroupHandler>();
        services.AddScoped<LeaveGroupHandler>();
        services.AddScoped<GetGroupPostsHandler>();
        services.AddScoped<CreateGroupPostHandler>();

        services.AddScoped<GetPendingMembersHandler>();
        services.AddScoped<ReviewMembershipHandler>();
        services.AddScoped<GetTripSuggestionsHandler>();
        services.AddScoped<PlanItineraryHandler>();
        services.AddSingleton<IItineraryPlanner, NearestNeighborItineraryPlanner>();
        services.AddScoped<GetEventsByViewportHandler>();
        services.AddScoped<GetEventsByPlaceHandler>();

        var storageSettings = configuration.GetSection("Storage").Get<StorageSettings>() ?? new StorageSettings();
        services.AddSingleton(storageSettings);

        if (storageSettings.Provider == "Mock")
            services.AddSingleton<IStorageService, MockStorageService>();
        else
            services.AddSingleton<IStorageService, R2StorageService>();

        services.AddScoped<CreateReviewHandler>();
        services.AddScoped<SubmitCounterReviewHandler>();
        services.AddScoped<GetReviewsByTargetHandler>();
        services.AddScoped<GetMyReviewsHandler>();
        services.AddScoped<FlagReviewHandler>();
        services.AddScoped<AdminRemoveReviewHandler>();

        services.AddScoped<RequestUploadHandler>();
        services.AddScoped<ConfirmUploadHandler>();
        services.AddScoped<DeleteMediaHandler>();
        services.AddScoped<GetMediaByOwnerHandler>();
        services.AddScoped<ReorderMediaHandler>();
        services.AddScoped<GetMediaContentHandler>();

        services.AddScoped<CreateOrUpdateRsvpHandler>();
        services.AddScoped<CancelRsvpHandler>();
        services.AddScoped<GetEventRsvpsHandler>();
        services.AddScoped<GetMyRsvpsHandler>();

        services.AddScoped<SendMessageHandler>();
        services.AddScoped<GetConversationsHandler>();
        services.AddScoped<GetMessagesHandler>();
        services.AddScoped<DeleteMessageHandler>();
        services.AddScoped<GetUnreadCountHandler>();

        services.AddScoped<ToggleSaveHandler>();
        services.AddScoped<GetSavedItemsHandler>();

        // Telemetry: generic sink (structured logs by default) + ingest handler
        services.AddSingleton<ITelemetrySink, SerilogTelemetrySink>();
        services.AddScoped<IngestTelemetryHandler>();

        return services;
    }
}
