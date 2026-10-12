using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace canteen_management.Controllers;

[Authorize(AuthenticationSchemes = "Cookies", Roles = "Faculty")]
public class FacultyController : Controller
{
    public IActionResult Dashboard()
    {
        ViewBag.Title = "Faculty Dashboard";
        return View();
    }
}
