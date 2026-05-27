namespace CarLine.DataCleanUp.FSharp

open CarLine.Common.DependencyInjection
open CarLine.DataCleanUp.FSharp
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting

module Program =
    let exitCode = 0

    [<EntryPoint>]
    let main args =

        let builder = WebApplication.CreateBuilder(args)

        // Infrastructure registrations
        // In F#, every expression must be used. If a function returns a value and you don't use it, the compiler gives a warning (or error).
        builder.Services.AddCarLineMongoClient(builder.Configuration, allowLocalFallback = false)      |> ignore 
        builder.Services.AddCarLineBlobStorage(builder.Configuration)                                  |> ignore
        builder.Services.AddCarLineElasticsearch(builder.Configuration, disableDirectStreaming = true) |> ignore

        // Application services
        builder.Services.AddScoped<DataCleanupService>()  |> ignore
        builder.Services.AddHostedService<Worker>()       |> ignore
        builder.Services.AddControllers()                 |> ignore

        let app = builder.Build()
        app.MapControllers() |> ignore
        app.Run()

        exitCode