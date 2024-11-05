using Reveal.Sdk;
using Reveal.Sdk.Data;
using Reveal.Sdk.Dom;
using RevealSdk.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers().AddReveal(builder =>
{
    builder
        // ****
        // Set your license here or in a file in your home directory
        // https://help.revealbi.io/web/adding-license-key/
        //
        //.AddSettings(settings =>
        //{
        //    settings.License = "eyJhbGciOicCI6IkpXVCJ9.e";
        //})

        // ***
        // required 
        .AddAuthenticationProvider<AuthenticationProvider>()
        .AddDataSourceProvider<DataSourceProvider>()
        // optional 
        .AddUserContextProvider<UserContextProvider>()
        // optional
        .AddObjectFilter<ObjectFilterProvider>()
        // optional 
        //.AddDashboardProvider<DashboardProvider>()

        // Required.  Register any data source connector you are using.
        .DataSources.RegisterMicrosoftSqlServer();
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
      builder => builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()
    );
});

var app = builder.Build();
app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//// ****
//// This API will get a thumbnail for a dashboard using the Reveal client API
//// ****
app.MapGet("/dashboards/{name}/thumbnail", async (string name) =>
{
    try
    {
        var path = Path.Combine("dashboards", $"{name}.rdash");

        if (!File.Exists(path))
        {
            return Results.NotFound(new { Message = $"Dashboard '{name}' not found." });
        }

        var dashboard = new Dashboard(path);
        var info = await dashboard.GetInfoAsync(Path.GetFileNameWithoutExtension(path));

        if (info == null)
        {
            return Results.Problem("Failed to retrieve dashboard info.", statusCode: 500);
        }

        return TypedResults.Ok(info);
    }
    catch (FileNotFoundException ex)
    {
        Console.Error.WriteLine($"FileNotFoundException: {ex.Message}");
        return Results.NotFound(new { Message = $"Dashboard '{name}' not found." });
    }
    catch (UnauthorizedAccessException ex)
    {
        Console.Error.WriteLine($"UnauthorizedAccessException: {ex.Message}");
        return Results.Problem("Unauthorized access to the dashboard file.", statusCode: 403);
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine($"Exception: {ex.Message}");
        return Results.Problem("An unexpected error occurred while processing your request.", statusCode: 500);
    }
});


// ****
// This API uses the Reveal SDK DOM library to get the names + titles of the dashboards
// It is important to note that the Dashboard Name and Dashboard Title can be different
// which is why we use the RdashDocument.Load to get the title
// ****
app.MapGet("/dashboards/names", () =>
{
    try
    {
        string folderPath = Path.Combine(Directory.GetCurrentDirectory(), "Dashboards");
        var files = Directory.GetFiles(folderPath);
        Random rand = new();

        var fileNames = files.Select(file =>
        {
            try
            {
                return new DashboardNames
                {
                    DashboardFileName = Path.GetFileNameWithoutExtension(file),
                    DashboardTitle = RdashDocument.Load(file).Title
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error Reading FileData {file}: {ex.Message}");
                return null;
            }
        }).Where(fileData => fileData != null).ToList();

        return Results.Ok(fileNames);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error Reading Directory : {ex.Message}");
        return Results.Problem("An unexpected error occurred while processing the request.");
    }

}).Produces<IEnumerable<DashboardNames>>(StatusCodes.Status200OK)
.Produces(StatusCodes.Status404NotFound)
.ProducesProblem(StatusCodes.Status500InternalServerError);

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();