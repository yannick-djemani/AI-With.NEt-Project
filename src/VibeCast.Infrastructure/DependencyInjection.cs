using Azure.AI.OpenAI;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using VibeCast.Application.Abstractions.Jobs;
using VibeCast.Application.Abstractions.Storage;
using VibeCast.Application.Episodes;
using VibeCast.Application.Media;
using VibeCast.Application.Validation;
using VibeCast.Infrastructure.Data;
using VibeCast.Infrastructure.Jobs;
using VibeCast.Infrastructure.Options;
using VibeCast.Infrastructure.Storage;

namespace VibeCast.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddVibeCastInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("VibeCast")
            ?? "Data Source=.vibecast/vibecast.db";

        services.AddDbContextFactory<VibeCastDbContext>(options => options.UseSqlite(connectionString));

        services.AddOptions<BlobStorageOptions>()
            .Bind(configuration.GetSection(BlobStorageOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IEpisodeConceptGenerator, FoundryEpisodeConceptGenerator>();

        services.AddOptions<FoundryOptions>()
            .Bind(configuration.GetSection(FoundryOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<AzureOpenAIClient>(serviceProvider =>
        {
            var foundryOptions = serviceProvider.GetRequiredService<IOptions<FoundryOptions>>().Value;
            var openAIClient = new AzureOpenAIClient(
                new Uri(foundryOptions.ProjectEndpoint, UriKind.Absolute),
                new Azure.AzureKeyCredential(foundryOptions.Apipkey ?? string.Empty)
            );
            return openAIClient;        
        });

        services.AddSingleton<IChatClient>(serviceProvider =>
        {
            var foundryOptions = serviceProvider.GetRequiredService<IOptions<FoundryOptions>>().Value;
            var openAIClient = serviceProvider.GetRequiredService<AzureOpenAIClient>();
            return openAIClient
                .GetChatClient(foundryOptions.ChatModelDeployment)
                .AsIChatClient();
        });

        services.AddOptions<BackgroundJobsOptions>()
            .Bind(configuration.GetSection(BackgroundJobsOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IBlobStorage, LocalBlobStorage>();
        services.AddSingleton<IBackgroundJobQueue, ChannelBackgroundJobQueue>();
        services.AddHostedService<BackgroundJobWorker>();

        services.AddSingleton<IValidator<CreateEpisodeRequest>, EpisodeDraftValidator>();
        services.AddSingleton<IValidator<MediaUploadRequest>, MediaUploadValidator>();

        return services;
    }
}
