﻿using CommonInfrastructure.Filters.JWTRevoke;
using CommonInfrastructure.Filters;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.IdentityModel.Tokens;
using SubmitService.Domain;
using SubmitService.Infrastructure;
using System.Text;
using Zack.JWT;
using MediatR;
using Zack.Commons;
using Microsoft.EntityFrameworkCore;
using Zack.ASPNETCore;
using FluentValidation;
using SubmitService.Submit.WebAPI.Controllers.Requests;
using Microsoft.Extensions.DependencyInjection;
using CommonInfrastructure.TencentCOS;

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
builder.Services.AddDbContext<SubmitDbContext>(optionsBuilder =>
{
    string connStr = builder.Configuration.GetConnectionString("MonodyFeedBackDB");
    optionsBuilder.UseSqlServer(connStr);
});

// DI服务注册
builder.Services.AddScoped<SubmitDomainService>();
builder.Services.AddScoped<ISubmitRepository, SubmitRepository>();
builder.Services.AddScoped<IJWTVersionTool, JWTVersionToolForOtherServices>();  // JWTVersion筛选器获取服务端JWT的工具
builder.Services.AddHttpClient();  // 为了将IHttpClientFactory注入进JWTVersionToolForOtherServices
builder.Services.AddScoped<COSService>();

// 筛选器
builder.Services.Configure<MvcOptions>(options =>
{
    options.Filters.Add<UnitOfWorkFilter>();  // 在Action方法执行结束后统一SaveChangesAsync
    options.Filters.Add<ExceptionFilter>();  // 异常筛选器，根据运行环境的不同返回不同的错误信息
    options.Filters.Add<JWTVersionCheckFilter>();  // 判断JWT是否失效的筛选器
});

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<SubmitRequestValidator>();

// 配置
builder.Services.Configure<COSPictureOptions>(builder.Configuration.GetSection("COSPicture"));

// MediatR
builder.Services.AddMediatR(ReflectionHelper.GetAllReferencedAssemblies().ToArray());

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
