using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using QuestPDF.Infrastructure;
using LibraTrack.Data;
using LibraTrack.Services;

// Configure QuestPDF community license
QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// 1. Add Database Context (SQLite)
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("LibraryDb") ?? "Data Source=libratrack.db"));

// 2. Add Cookie-based Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
    });

builder.Services.AddAuthorization();

// 3. Register Application Services
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IReservationService, ReservationService>();
builder.Services.AddSingleton<IBookMetadataProvider, LocalMockBookMetadataProvider>();
builder.Services.AddHostedService<LoanNotificationBackgroundService>();

// 4. Add MVC Controllers & Views
builder.Services.AddControllersWithViews();

// 5. Add In-Memory Cache and Session State for Self-Service Kiosk
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(10);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".LibraTrack.Session";
});

// 6. Add CORS Policy (GET only as requested)
builder.Services.AddCors(options =>
{
    options.AddPolicy("PublicApiCorsPolicy", policy =>
    {
        policy.AllowAnyOrigin()
              .WithMethods("GET")
              .WithHeaders("X-Api-Key", "Content-Type", "Accept");
    });
});

// 7. Add Basic Rate Limiting
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("api-limiter", opt =>
    {
        opt.PermitLimit = 60;
        opt.Window = TimeSpan.FromMinutes(1);
        opt.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
        opt.QueueLimit = 0;
    });
});

// 8. Add Swagger / OpenAPI Documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "LibraTrack Public Catalog REST API",
        Version = "v1",
        Description = "Secure, read-only public REST API providing access to library catalog items, categories, branch locations, and system status."
    });

    // Only document the REST API endpoints in Swagger
    c.DocInclusionPredicate((docName, apiDesc) =>
    {
        return apiDesc.RelativePath != null && apiDesc.RelativePath.StartsWith("api/", StringComparison.OrdinalIgnoreCase);
    });

    c.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
    {
        Description = "API key required for all requests. Header format: 'X-Api-Key: {your-key}'",
        In = ParameterLocation.Header,
        Name = "X-Api-Key",
        Type = SecuritySchemeType.ApiKey
    });

    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("ApiKey"),
            new List<string>()
        }
    });
});

var app = builder.Build();

// 4. Auto-initialize & Seed Database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<LibraryDbContext>();
        bool resetDb = app.Environment.IsDevelopment() && app.Configuration.GetValue<bool>("ResetDatabaseOnStart");
        if (resetDb)
        {
            logger.LogInformation("ResetDatabaseOnStart is true in development mode: Re-creating SQLite database and seeding fresh data.");
        }
        DbInitializer.Initialize(context, resetDb);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred during database migration or seeding.");
    }
}

// 5. Configure HTTP Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Enable CORS for GET only public API
app.UseCors("PublicApiCorsPolicy");

// Enable Rate Limiting
app.UseRateLimiter();

app.UseSession();

app.UseAuthentication();
app.UseAuthorization();

// Enable Swagger / OpenAPI
app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "LibraTrack Public Catalog API v1");
    c.RoutePrefix = "swagger";
});

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Catalog}/{action=Index}/{id?}");

app.Run();
