using BabaTune.Application.ServiceProviderExtensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Scalar.AspNetCore;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplicationContext(builder.Configuration.GetConnectionString("DefaultConnection"));

builder.Services.AddUnitOfWork();
builder.Services.AddApplicationServices();

var firebaseSection = builder.Configuration.GetSection("Firebase")
	?? throw new InvalidOperationException("Section 'Firebase' not found");

builder.Services.AddStorageService(firebaseSection);

builder.Services.AddControllers()
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
	});

builder.Services.AddAuthentication(options =>
{
	options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
	options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
	options.TokenValidationParameters = new TokenValidationParameters
	{
		ValidateIssuer = true,
		ValidateAudience = true,
		ValidateLifetime = true,
		ValidateIssuerSigningKey = true,
		ValidIssuer = builder.Configuration["Jwt:Issuer"],
		ValidAudience = builder.Configuration["Jwt:Audience"],
		IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Secret"]!))
	};
});

builder.Services.AddOpenApi(options =>
{

	options.AddDocumentTransformer(( document, context, cancellationToken ) =>
	{
		document.Info.Title = "BabaTune API";
		document.Info.Version = "v1";

		document.Components ??= new OpenApiComponents();
		document.Components.SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
		{
			["Bearer"] = new OpenApiSecurityScheme
			{
				Type = SecuritySchemeType.Http,
				Scheme = "bearer",
				BearerFormat = "JWT",
				In = ParameterLocation.Header
			}
		};

		foreach(var operation in document.Paths.Values.SelectMany(p => p.Operations))
		{
			operation.Value.Security ??= [];
			operation.Value.Security.Add(new OpenApiSecurityRequirement
			{
				[new OpenApiSecuritySchemeReference("Bearer", document)] = []
			});
		}

		return Task.CompletedTask;
	});
});

builder.Services.AddCors(options =>
{
	options.AddDefaultPolicy(policy =>
	{
		policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
	});
});

var app = builder.Build();

if(app.Environment.IsDevelopment())
{
	app.MapOpenApi();
	app.MapScalarApiReference(options => options
		.WithTitle("BabaTune API")
		.AddPreferredSecuritySchemes("Bearer")
		.AddHttpAuthentication("Bearer", auth =>
		{
			auth.Token = "";
		})
	);
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseCors();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.Run();