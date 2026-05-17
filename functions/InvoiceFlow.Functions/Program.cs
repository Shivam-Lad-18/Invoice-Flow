using Azure.AI.DocumentIntelligence;
using Azure.Storage.Blobs;
using InvoiceFlow.Functions.Data;
using Microsoft.Azure.Functions.Worker;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        var config = context.Configuration;

        // Application Insights — enabled automatically by the Functions host when
        // APPLICATIONINSIGHTS_CONNECTION_STRING is set in app settings (production).
        // Not required for local development.

        // EF Core — same Azure SQL as the API
        services.AddDbContext<FunctionsDbContext>(options =>
            options.UseSqlServer(
                config["SqlConnectionString"]
                ?? throw new InvalidOperationException("SqlConnectionString setting is missing.")));

        // Azure Blob Storage — register the container client directly so functions
        // can download blobs without needing to know the container name separately.
        services.AddSingleton(_ =>
        {
            var connStr = config["BlobStorageConnection"]
                ?? throw new InvalidOperationException("BlobStorageConnection setting is missing.");
            var containerName = config["BlobContainerName"] ?? "invoices";
            return new BlobServiceClient(connStr).GetBlobContainerClient(containerName);
        });

        // Azure Document Intelligence
        services.AddSingleton(_ =>
        {
            var endpoint = config["DocumentIntelligenceEndpoint"]
                ?? throw new InvalidOperationException("DocumentIntelligenceEndpoint setting is missing.");
            var key = config["DocumentIntelligenceApiKey"]
                ?? throw new InvalidOperationException("DocumentIntelligenceApiKey setting is missing.");
            return new DocumentIntelligenceClient(new Uri(endpoint), new Azure.AzureKeyCredential(key));
        });
    })
    .Build();

host.Run();
