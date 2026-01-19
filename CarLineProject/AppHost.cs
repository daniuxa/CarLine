using Azure.Provisioning.Storage;
using Projects;

var builder = DistributedApplication.CreateBuilder(args);

// Databases - Configure with persistent named volumes
var carsNoSqlDb = builder.AddMongoDB("mongodb")
    .WithDataVolume("carline-mongodb-data") // Named volume for persistence
    .WithLifetime(ContainerLifetime.Persistent)
    .AddDatabase("carsnosql");

// SQL Server for subscriptions
var subscriptionsSql = builder.AddSqlServer("sqlserver")
    .WithDataVolume("carline-sqlserver-data")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithHostPort(54040)
    .AddDatabase("subscriptionsdb");

// Elasticsearch - Configure with persistent named volume
var elasticsearch = builder.AddElasticsearch("elasticsearch")
    .WithDataVolume("carline-elasticsearch-data")
    .WithLifetime(ContainerLifetime.Persistent);

// Azure Blob Storage - Configure with persistent named volume
var azureBlobStorage = builder.AddAzureStorage("azureblobstorage")
    .RunAsEmulator(emulator =>
    {
        emulator.WithDataVolume("carline-azurite-data"); // Named volume for persistence
        emulator.WithLifetime(ContainerLifetime.Persistent);
    });

var modelsContainer = azureBlobStorage.AddBlobs("modelscontainer");

// ----- Services and Applications -----
// External Car Seller Stub - for simulating external data source
var externalCarSellerStub = builder.AddProject<CarLine_ExternalCarSellerStub>("externalcarsellerstub");

// Car Crawler Service - ingests data from external car sellers stub into NoSQL DB
var carCrawlerService = builder.AddProject<CarLine_Crawler>("carcrawler")
    .WithReference(carsNoSqlDb)
    .WithReference(externalCarSellerStub) // Reference to external car seller stub
    .WaitFor(carsNoSqlDb)
    .WaitFor(externalCarSellerStub);

// Data Cleanup Service - processes and cleans data in NoSQL DB, indexes into Elasticsearch, stores cleaned csv in Blob Storage
var dataCleanupService = builder.AddProject<CarLine_DataCleanUp>("datacleanup")
    .WithReference(carsNoSqlDb)
    .WithReference(elasticsearch)
    .WithReference(modelsContainer)
    .WaitFor(carsNoSqlDb)
    .WaitFor(elasticsearch)
    .WaitFor(modelsContainer);

// Training Model Service - trains ML models using data from Blob Storage
var trainingModelService = builder.AddAzureFunctionsProject<CarLine_TrainingFunction>("trainingmodel")
    .WithHostStorage(azureBlobStorage)
    .WithRoleAssignments(azureBlobStorage, StorageBuiltInRole.StorageBlobDataOwner)
    .WithReference(modelsContainer)
    .WaitFor(modelsContainer);

// ML Inference Service - provides ML model inference capabilities
var carLineMlInferenceService = builder.AddProject<CarLine_MLInterferenceService>("carlinemlinferenceservice")
    .WithReference(modelsContainer)
    .WaitFor(modelsContainer);

var subscriptionService = builder.AddProject<CarLine_SubscriptionService>("subscriptionservice")
    .WithReference(carsNoSqlDb)
    .WithReference(subscriptionsSql)
    .WaitFor(carsNoSqlDb)
    .WaitFor(subscriptionsSql);

// Car Line API - main API service for Car Line application
var carLineApi = builder.AddProject<CarLine_API>("carlineapi")
    .WithReference(carsNoSqlDb)
    .WithReference(elasticsearch)
    .WithReference(modelsContainer)
    .WithReference(carLineMlInferenceService)
    .WithReference(subscriptionService)
    .WaitFor(carsNoSqlDb)
    .WaitFor(elasticsearch)
    .WaitFor(modelsContainer)
    .WaitFor(carLineMlInferenceService)
    .WaitFor(subscriptionService);

// Price Classification Service - classifies car prices using ML Inference Service
var priceClassificationService = builder.AddProject<CarLine_PriceClassificationService>("priceclassificationservice")
    .WithReference(carsNoSqlDb)
    .WithReference(elasticsearch)
    .WithReference(carLineMlInferenceService)
    .WaitFor(carsNoSqlDb)
    .WaitFor(elasticsearch)
    .WaitFor(carLineMlInferenceService);

// Car Line Web - front-end web application for Car Line
var carLineWeb = builder.AddViteApp("carlineweb", "../CarLine.Web/car-line-web")
    .WithReference(carLineApi)
    .WaitFor(carLineApi)
    .WithNpmPackageInstallation();

builder.Build().Run();