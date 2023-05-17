using Microsoft.Extensions.Configuration;
using Microsoft.Data.SqlClient;
using Zack.JWT;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using FAQService.Infrastructure;
using CommonInfrastructure.Filters.JWTRevoke;
using FAQService.Domain;
using Microsoft.AspNetCore.Mvc;
using Zack.ASPNETCore;
using CommonInfrastructure.Filters.Transaction;
using CommonInfrastructure.Filters;
using FluentValidation;
using FAQService.WebAPI.Controllers;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// 数据库配置源Zack.AnyDBConfigProvider
builder.WebHost.ConfigureAppConfiguration((hostCtx, configBuilder) =>
{
    string connStr = Environment.GetEnvironmentVariable("ConnectionStrings:MonodyFeedBackDB")!;
    configBuilder.AddDbConfiguration(() => new SqlConnection(connStr), reloadOnChange: true, reloadInterval: TimeSpan.FromSeconds(2));
});

// JWT
JWTOptions jwtOptions = builder.Configuration.GetSection("JWT").Get<JWTOptions>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(jwtBearerOpt =>
{
    byte[] keyBytes = Encoding.UTF8.GetBytes(jwtOptions.Key);
    var secKey = new SymmetricSecurityKey(keyBytes);
    jwtBearerOpt.TokenValidationParameters = new()
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = secKey,
    };
});

// DbContext
builder.Services.AddDbContext<FAQDbContext>(optionsBuilder =>
{
    string connStr = builder.Configuration.GetConnectionString("MonodyFeedBackDB");
    optionsBuilder.UseSqlServer(connStr);
});

// DI服务注册
builder.Services.AddScoped<IJWTVersionTool, JWTVersionToolForOtherServices>();  // JWTVersion筛选器获取服务端JWT的工具
builder.Services.AddHttpClient();  // 为了将IHttpClientFactory注入进JWTVersionToolForOtherServices
builder.Services.AddScoped<FAQDomainService>();
builder.Services.AddScoped<IFAQRepository, FAQRepository>();

// 筛选器
builder.Services.Configure<MvcOptions>(options =>
{
    options.Filters.Add<UnitOfWorkFilter>();
    options.Filters.Add<TransactionScopeFilter>();
    options.Filters.Add<ExceptionFilter>();
    options.Filters.Add<JWTVersionCheckFilter>();
});

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<AccessController>();

// 跨域
var urls = new string[] { builder.Configuration.GetSection("CORSUrl").Value };
builder.Services.AddCors(options => options.AddDefaultPolicy(builder => builder.WithOrigins(urls).AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));


var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
