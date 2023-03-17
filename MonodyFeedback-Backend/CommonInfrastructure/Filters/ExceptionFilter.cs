using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Hosting;

namespace CommonInfrastructure.Filters;

public class ExceptionFilter : IAsyncExceptionFilter
{
    private readonly IWebHostEnvironment _webHostEnvironment;

    public ExceptionFilter(IWebHostEnvironment webHostEnvironment)
    {
        _webHostEnvironment = webHostEnvironment;
    }

    public Task OnExceptionAsync(ExceptionContext context)
    {
        string message;
        if (_webHostEnvironment.IsDevelopment())
        {
            message = context.Exception.ToString();
        }
        else
        {
            message = "服务端错误";
        }
        context.Result = new ObjectResult(message) { StatusCode = 500 };
        return Task.CompletedTask;
    }
}
