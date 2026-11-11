using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OnlineBookShop.Models;

namespace OnlineBookShop.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;

    public HomeController(ILogger<HomeController> logger)
    {
        _logger = logger;
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [AllowAnonymous]
    public IActionResult Error()
    {
        var requestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
        
     
        _logger.LogError("An unhandled exception occurred. Request ID: {RequestId}", requestId);

        return View(new ErrorViewModel { RequestId = requestId });
    }
}