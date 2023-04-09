using CommonInfrastructure.Filters;
using CommonInfrastructure.Filters.JWTRevoke;
using CommonInfrastructure.Filters.Transaction;
using CommonInfrastructure.TencentCOS;
using FluentValidation;
using IdentityService.Domain;
using IdentityService.Domain.Entities;
using IdentityService.Infrastructure;
using IdentityService.WebAPI.Controllers.Requests;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using Zack.JWT;

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

// 标识框架
builder.Services.AddDbContext<IdDbContext>(optionsBuilder =>
{
    string connStr = builder.Configuration.GetConnectionString("MonodyFeedBackDB");
    optionsBuilder.UseSqlServer(connStr, sqlOptions =>
    {
        // retry logic
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: new List<int> { 19 }
        );
    });
});
builder.Services.AddDataProtection();
builder.Services.AddIdentityCore<User>(options =>
{
    options.Password.RequiredLength = 8;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;

    options.Lockout.MaxFailedAccessAttempts = 5;  // 登录失败5次锁定

    options.Tokens.PasswordResetTokenProvider = TokenOptions.DefaultEmailProvider;
    options.Tokens.EmailConfirmationTokenProvider = TokenOptions.DefaultEmailProvider;
});
IdentityBuilder identityBuilder = new(typeof(User), typeof(Role), builder.Services);
identityBuilder.AddEntityFrameworkStores<IdDbContext>()
    .AddDefaultTokenProviders()
    .AddUserManager<UserManager<User>>()
    .AddRoleManager<RoleManager<Role>>();
// ↑↑↑↑↑↑标识框架配置结束↑↑↑↑↑↑

// JWT
JWTOptions jwtOptions = builder.Configuration.GetSection("JWT").Get<JWTOptions>();
//builder.Services.AddJWTAuthentication(jwtOptions);
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(jwtBearerOpt =>
{
    byte[] keyBytes = Encoding.UTF8.GetBytes(jwtOptions.Key);
    var secKey = new SymmetricSecurityKey(keyBytes);
    jwtBearerOpt.TokenValidationParameters = new()
    {
        ValidateIssuer = false,
        ValidateAudience = false,  // !!!!!!!!!!问题就出在这里!!!!!!!!!!
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = secKey,
    };
});

// 筛选器
builder.Services.Configure<MvcOptions>(options =>
{
    // 不要对标识框架的DbContext乱搞什么异常筛选器事务筛选器，它自己会干的
    options.Filters.Add<ExceptionFilter>();  // 异常筛选器，根据运行环境的不同返回不同的错误信息
    options.Filters.Add<JWTVersionCheckFilter>();  // 判断JWT是否失效的筛选器
});

// DI服务注册
builder.Services.AddScoped<IIdentityRepository, IdentityRepository>();
builder.Services.AddScoped<IdentityDomainService>();
builder.Services.AddScoped<TokenService>();
builder.Services.AddHttpContextAccessor();// 注册HttpContextAccessor以通过依赖注入的方式拿到HttpContext
builder.Services.AddScoped<COSService>();
builder.Services.AddScoped<IJWTVersionTool, JWTVersionTool>();

// FluentValidation
builder.Services.AddValidatorsFromAssemblyContaining<SignUpRequest>();

// 配置
builder.Services.Configure<JWTOptions>(builder.Configuration.GetSection("JWT"));
builder.Services.Configure<COSAvatarOptions>(builder.Configuration.GetSection("COSAvatar"));

// 跨域
var urls = new string[] { builder.Configuration.GetSection("CORSUrl").Value };
builder.Services.AddCors(options => options.AddDefaultPolicy(builder => builder.WithOrigins(urls).AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod()/*.AllowCredentials()*/));

// 让Swagger中带上Authorization报文头
builder.Services.AddSwaggerGen(opt =>
{
    OpenApiSecurityScheme scheme = new()
    {
        Description = "Authorization报文头. \r\n例如：Bearer ey234927349dhhsdid",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Authorization" },
        Scheme = "oauth2",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
    };
    opt.AddSecurityDefinition("Authorization", scheme);
    OpenApiSecurityRequirement requirement = new();
    requirement[scheme] = new List<string>();
    opt.AddSecurityRequirement(requirement);
});



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
