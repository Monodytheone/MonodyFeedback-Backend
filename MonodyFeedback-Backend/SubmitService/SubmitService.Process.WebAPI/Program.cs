using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Zack.JWT;
using CommonInfrastructure.Filters;
using CommonInfrastructure.Filters.JWTRevoke;
using SubmitService.Domain;
using SubmitService.Infrastructure;
using Microsoft.Data.SqlClient;

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

// DI服务注册
builder.Services.AddScoped<SubmitDomainService>();
builder.Services.AddScoped<ISubmitRepository, SubmitRepository>();


// 筛选器
builder.Services.Configure<MvcOptions>(options =>
{
    options.Filters.Add<ExceptionFilter>();  // 异常筛选器，根据运行环境的不同返回不同的错误信息
    options.Filters.Add<JWTVersionCheckFilter>();  // 判断JWT是否失效的筛选器
});

// FluentValidation

// 配置

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
