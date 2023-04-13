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
using Zack.Commons;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Zack.ASPNETCore;
using CommonInfrastructure.TencentCOS;
using CommonInfrastructure.Filters.Transaction;
using FluentValidation;
using SubmitService.Process.WebAPI.Controllers.Requests;
using SubmitService.Process.WebAPI;
using SubmitService.Process.WebAPI.Hubs;
using Microsoft.Extensions.Primitives;

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

    // SignalR身份认证：
    jwtBearerOpt.Events = new JwtBearerEvents
    {
        // WebSocket不支持自定义报文头
        // 所以需要把JWT通过Url的QueryString传递
        // 然后在服务端的OnMessageReceived中，把QueryString中的JWT读出来，赋给context.Token
        // 这样后续中间件才能从context.Token中解析出Token
        OnMessageReceived = context =>
        {
            StringValues accessToken = context.Request.Query["access_token"];
            PathString path = context.HttpContext.Request.Path;
            if (string.IsNullOrEmpty(accessToken) == false 
                && (path.StartsWithSegments("/SubmitterHub") || path.StartsWithSegments("/CommonHub")))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
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
    options.Filters.Add<TransactionScopeFilter>();  // 自动启用事务管理的筛选器(发生异常后回滚数据库)
    options.Filters.Add<ExceptionFilter>();  // 异常筛选器，根据运行环境的不同返回不同的错误信息
    options.Filters.Add<JWTVersionCheckFilter>();  // 判断JWT是否失效的筛选器
});

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<ProcessRequestValidator>();

// 配置
builder.Services.Configure<COSPictureOptions>(builder.Configuration.GetSection("COSPicture"));

// MediatR
builder.Services.AddMediatR(ReflectionHelper.GetAllReferencedAssemblies().ToArray());

// 跨域
var urls = new string[] { builder.Configuration.GetSection("CORSUrl").Value };
builder.Services.AddCors(options => options.AddDefaultPolicy(builder => builder.WithOrigins(urls).AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()));

// 托管服务
builder.Services.AddHostedService<AutoCloseHostedService>();

// SignalR
builder.Services.AddSignalR();


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

app.MapHub<CommonHub>("/CommonHub");
app.MapHub<SubmitterHub>("/SubmitterHub");
app.MapControllers();

app.Run();
