using System.Security.Claims;
using System.Text;
using System.Text.Json.Serialization;
using Lms.Modules.Assessments;
using Lms.Modules.Assessments.Infrastructure;
using Lms.Modules.Content;
using Lms.Modules.Content.Infrastructure;
using Lms.Modules.Flashcards;
using Lms.Modules.Flashcards.Infrastructure;
using Lms.Modules.Courses;
using Lms.Modules.Courses.Infrastructure;
using Lms.Modules.Enrollment;
using Lms.Modules.Enrollment.Infrastructure;
using Lms.Modules.Identity;
using Lms.Modules.Identity.Infrastructure;
using Lms.Modules.LiveClasses;
using Lms.Modules.LiveClasses.Infrastructure;
using Lms.Modules.Platform;
using Lms.Modules.Platform.Infrastructure;
using Lms.Modules.Progress;
using Lms.Modules.Progress.Infrastructure;
using Lms.Modules.SyllabusMentor;
using Lms.Modules.SyllabusMentor.Application;
using Lms.Modules.SyllabusMentor.Infrastructure;
using Lms.Modules.QnA;
using Lms.Modules.QnA.Infrastructure;
using Lms.Api.Filters;
using Lms.Api.Middleware;
using Lms.Api.Seeders;
using Lms.Shared;
using QuestPDF.Infrastructure;
using Lms.Shared.Auth;
using Lms.Shared.Modules;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

QuestPDF.Settings.License = LicenseType.Community;

var builder = WebApplication.CreateBuilder(args);

// Shared kernel: tenant context, current user, event bus, file storage.
builder.Services.AddSharedKernel(builder.Configuration);

// Register feature modules (modular monolith). Add new modules here.
var moduleAssemblies = builder.Services.RegisterModules(
    builder.Configuration,
    new IdentityModule(),
    new CoursesModule(),
    new ContentModule(),
    new AssessmentsModule(),
    new FlashcardsModule(),
    new EnrollmentModule(),
    new ProgressModule(),
    new PlatformModule(),
    new LiveClassesModule(),
    new SyllabusMentorModule(),
    new QnAModule());

// Controllers from the host + every module assembly (plug-and-play).
var mvc = builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
    .AddMvcOptions(o => o.Filters.Add<ErrorTraceResultFilter>());
foreach (var assembly in moduleAssemblies)
    mvc.AddApplicationPart(assembly);

// JWT authentication.
var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.MapInboundClaims = true;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.SigningKey)),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (!context.Request.Path.StartsWithSegments("/api/v1/files"))
                    return Task.CompletedTask;

                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                    context.Token = accessToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorizationBuilder()
    .AddPolicy("SuperAdmin", p => p.RequireRole(Roles.SuperAdmin))
    .AddPolicy("PlatformStaff", p => p.RequireRole(Roles.SuperAdmin, Roles.Support))
    .AddPolicy("InstituteAdmin", p => p.RequireRole(Roles.SuperAdmin, Roles.InstituteAdmin))
    .AddPolicy("Teacher", p => p.RequireRole(Roles.SuperAdmin, Roles.InstituteAdmin, Roles.Teacher));

var corsOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>()
    ?? new[] { "http://localhost:3000" };
var allowSubdomainOrigins = builder.Configuration.GetValue("Cors:AllowSubdomainOrigins", true);
builder.Services.AddCors(options =>
    options.AddPolicy("frontend", p =>
    {
        p.SetIsOriginAllowed(origin =>
        {
            if (corsOrigins.Contains(origin, StringComparer.OrdinalIgnoreCase)) return true;
            if (!allowSubdomainOrigins) return false;
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri)) return false;
            return uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase)
                   && (uri.Port is -1 or 3000);
        });
        p.AllowAnyHeader().AllowAnyMethod();
    }));

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "White-Label LMS API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter the JWT access token"
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Apply module migrations on startup (dev convenience).
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var sp = scope.ServiceProvider;
    var identityDb = sp.GetRequiredService<IdentityDbContext>();
    await identityDb.Database.MigrateAsync();
    await IdentitySeeder.SeedAsync(identityDb, sp.GetRequiredService<Lms.Modules.Identity.Infrastructure.IPasswordHasher>());

    var coursesDb = sp.GetRequiredService<CoursesDbContext>();
    await coursesDb.Database.MigrateAsync();
    await coursesDb.Database.ExecuteSqlRawAsync("""
        IF COL_LENGTH('courses.Bundles', 'VideosOnly') IS NULL
            ALTER TABLE courses.Bundles ADD VideosOnly bit NOT NULL CONSTRAINT DF_Bundles_VideosOnly DEFAULT 0;
        """);
    await Lms.Modules.Courses.Application.SubjectCatalogSeeder.EnsureDefaultTenantAsync(coursesDb);
    await CourseSeeder.SeedAsync(coursesDb);
    await Lms.Modules.Courses.Application.SubjectCatalogMigrator.MigrateUnlinkedSubjectsAsync(coursesDb);

    var topics = await coursesDb.Topics
        .Select(t => new ValueTuple<Guid, string>(t.Id, t.Title))
        .ToListAsync();

    var contentDb = sp.GetRequiredService<ContentDbContext>();
    await contentDb.Database.MigrateAsync();
    await ContentSeeder.SeedAsync(contentDb, topics);

    var assessmentsDb = sp.GetRequiredService<AssessmentsDbContext>();
    await assessmentsDb.Database.MigrateAsync();
    await AssessmentSeeder.SeedAsync(assessmentsDb, topics);

    var enrollmentDb = sp.GetRequiredService<EnrollmentDbContext>();
    await enrollmentDb.Database.MigrateAsync();
    await MockExamSeeder.SeedAsync(
        assessmentsDb,
        coursesDb,
        identityDb,
        enrollmentDb);

    var flashcardsDb = sp.GetRequiredService<FlashcardsDbContext>();
    await flashcardsDb.Database.MigrateAsync();
    await FlashcardSeeder.SeedAsync(flashcardsDb, topics);

    var progressDb = sp.GetRequiredService<ProgressDbContext>();
    await progressDb.Database.MigrateAsync();

    var platformDb = sp.GetRequiredService<PlatformDbContext>();
    await platformDb.Database.MigrateAsync();
    await platformDb.Database.ExecuteSqlRawAsync("""
        IF COL_LENGTH('platform.Tenants', 'StorageUsedBytes') IS NULL
            ALTER TABLE platform.Tenants ADD StorageUsedBytes bigint NOT NULL CONSTRAINT DF_Tenants_StorageUsedBytes DEFAULT 0;
        IF COL_LENGTH('platform.Tenants', 'StorageQuotaBytesOverride') IS NULL
            ALTER TABLE platform.Tenants ADD StorageQuotaBytesOverride bigint NULL;
        IF COL_LENGTH('platform.Tenants', 'StorageQuotaBypass') IS NULL
            ALTER TABLE platform.Tenants ADD StorageQuotaBypass bit NOT NULL CONSTRAINT DF_Tenants_StorageQuotaBypass DEFAULT 0;
        IF OBJECT_ID('platform.TenantStorageObjects', 'U') IS NULL
        BEGIN
            CREATE TABLE platform.TenantStorageObjects (
                Id uniqueidentifier NOT NULL PRIMARY KEY,
                TenantId uniqueidentifier NOT NULL,
                StorageKey nvarchar(512) NOT NULL,
                Folder nvarchar(64) NOT NULL,
                SizeBytes bigint NOT NULL,
                CreatedAt datetime2 NOT NULL);
            CREATE UNIQUE INDEX IX_TenantStorageObjects_TenantId_StorageKey
                ON platform.TenantStorageObjects (TenantId, StorageKey);
        END
        """);
    await PlatformSeeder.SeedAsync(platformDb);

    var catalogTenants = await platformDb.Tenants.AsNoTracking()
        .Select(t => new { t.Id, t.ProductProfile })
        .ToListAsync();
    foreach (var tenant in catalogTenants)
    {
        await Lms.Modules.Courses.Application.SubjectCatalogSeeder.SeedForTenantAsync(
            coursesDb, tenant.Id, tenant.ProductProfile);
    }
    await Lms.Modules.Courses.Application.SubjectCatalogMigrator.MigrateUnlinkedSubjectsAsync(coursesDb);

    var liveDb = sp.GetRequiredService<LiveClassesDbContext>();
    await liveDb.Database.MigrateAsync();

    var mentorDb = sp.GetRequiredService<SyllabusMentorDbContext>();
    await mentorDb.Database.MigrateAsync();

    var qnaDb = sp.GetRequiredService<QnADbContext>();
    await qnaDb.Database.MigrateAsync();

    var mentor = sp.GetRequiredService<ISyllabusMentorService>();
    foreach (var topic in topics)
    {
        try
        {
            await mentor.IngestAsync(new IngestRequest(topic.Item1, null), default);
        }
        catch
        {
            /* seed ingest is best-effort */
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("frontend");
app.UseMiddleware<TraceIdMiddleware>();
app.UseMiddleware<TenantResolutionMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();
