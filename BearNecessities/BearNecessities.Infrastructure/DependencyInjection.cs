using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using BearNecessities.Application.Interfaces;
using BearNecessities.Application.UseCases.Auth.Login;
using BearNecessities.Application.UseCases.Auth.RefreshToken;
using BearNecessities.Application.UseCases.Auth.Register;
using BearNecessities.Application.UseCases.Categories.GetCategoriesTree;
using BearNecessities.Application.UseCases.Events.ApproveEvent;
using BearNecessities.Application.UseCases.Events.CancelEvent;
using BearNecessities.Application.UseCases.Events.CreateEvent;
using BearNecessities.Application.UseCases.Events.GetEventById;
using BearNecessities.Application.UseCases.Events.GetEventsByPlace;
using BearNecessities.Application.UseCases.Events.GetEventsByViewport;
using BearNecessities.Application.UseCases.Events.GetMyEvents;
using BearNecessities.Application.UseCases.Events.SearchEvents;
using BearNecessities.Application.UseCases.Groups.CreateGroup;
using BearNecessities.Application.UseCases.Groups.CreateGroupPost;
using BearNecessities.Application.UseCases.Groups.GetGroupById;
using BearNecessities.Application.UseCases.Groups.GetGroupPosts;
using BearNecessities.Application.UseCases.Groups.GetGroups;
using BearNecessities.Application.UseCases.Groups.GetMyGroups;
using BearNecessities.Application.UseCases.Groups.JoinGroup;
using BearNecessities.Application.UseCases.Groups.LeaveGroup;
using BearNecessities.Application.UseCases.Trips.GetTripSuggestions;
using BearNecessities.Application.UseCases.Trips.PlanItinerary;
using BearNecessities.Application.Services.Trips;
using BearNecessities.Application.UseCases.Users.GetPendingMembers;
using BearNecessities.Application.UseCases.Users.ReviewMembership;
using BearNecessities.Application.UseCases.Events.GetPendingEvents;
using BearNecessities.Application.UseCases.Events.RejectEvent;
using BearNecessities.Application.UseCases.Events.UpdateEvent;
using BearNecessities.Application.UseCases.Reviews.AdminRemoveReview;
using BearNecessities.Application.UseCases.Reviews.CreateReview;
using BearNecessities.Application.UseCases.Reviews.FlagReview;
using BearNecessities.Application.UseCases.Reviews.GetMyReviews;
using BearNecessities.Application.UseCases.Reviews.GetReviewsByTarget;
using BearNecessities.Application.UseCases.Reviews.SubmitCounterReview;
using BearNecessities.Application.UseCases.Media.ConfirmUpload;
using BearNecessities.Application.UseCases.Media.DeleteMedia;
using BearNecessities.Application.UseCases.Media.GetMediaByOwner;
using BearNecessities.Application.UseCases.Media.GetMediaContent;
using BearNecessities.Application.UseCases.Media.ReorderMedia;
using BearNecessities.Application.UseCases.Media.RequestUpload;
using BearNecessities.Application.UseCases.Rsvps.CancelRsvp;
using BearNecessities.Application.UseCases.Rsvps.CreateOrUpdateRsvp;
using BearNecessities.Application.UseCases.Rsvps.GetEventRsvps;
using BearNecessities.Application.UseCases.Rsvps.GetMyRsvps;
using BearNecessities.Application.UseCases.Messages.DeleteMessage;
using BearNecessities.Application.UseCases.Messages.GetConversations;
using BearNecessities.Application.UseCases.Messages.GetMessages;
using BearNecessities.Application.UseCases.Messages.GetUnreadCount;
using BearNecessities.Application.UseCases.Messages.SendMessage;
using BearNecessities.Application.UseCases.SavedItems.GetSavedItems;
using BearNecessities.Application.UseCases.SavedItems.ToggleSave;
using BearNecessities.Application.UseCases.Telemetry.IngestTelemetry;
using BearNecessities.Infrastructure.Telemetry;
using BearNecessities.Application.UseCases.Places.ApprovePlace;
using BearNecessities.Application.UseCases.Places.CreatePlace;
using BearNecessities.Application.UseCases.Places.DeletePlace;
using BearNecessities.Application.UseCases.Places.GetMyPlaces;
using BearNecessities.Application.UseCases.Places.GetPendingPlaces;
using BearNecessities.Application.UseCases.Places.GetPlaceById;
using BearNecessities.Application.UseCases.Places.GetPlacesByViewport;
using BearNecessities.Application.UseCases.Places.GetPlaceDensity;
using BearNecessities.Application.UseCases.Places.RejectPlace;
using BearNecessities.Application.UseCases.Places.SearchPlaces;
using BearNecessities.Application.UseCases.Places.UpdatePlace;
using BearNecessities.Domain.Interfaces;
using BearNecessities.Infrastructure.Persistence;
using BearNecessities.Infrastructure.Repositories;
using BearNecessities.Infrastructure.Services;
using BearNecessities.Shared.Constants;
using Microsoft.Extensions.Logging;

namespace BearNecessities.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<BearNecessitiesDbContext>(options =>
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
            client.DefaultRequestHeaders.UserAgent.ParseAdd("BearNecessities/1.0 (community discovery; import)");
        });
        services.AddHttpClient<Imports.PlacePhotoService>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.UserAgent.ParseAdd("BearNecessities/1.0 (community discovery; photo lookup)");
        });
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<IOccurrenceExpander, OccurrenceExpander>();
        services.AddScoped<IOccurrenceRefreshService, OccurrenceRefreshService>();
        services.AddHostedService<OccurrenceRefreshBackgroundService>();

        services.AddScoped<GetPlacesByViewportHandler>();
        services.AddScoped<GetPlaceDensityHandler>();
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
        services.AddScoped<SearchEventsHandler>();

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
