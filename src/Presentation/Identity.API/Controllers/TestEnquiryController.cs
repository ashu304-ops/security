using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Identity.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] // Enforces valid JWT token
public class EnquiryController : ControllerBase
{
    [HttpGet]
    public IActionResult GetEnquiries()
    {
        return Ok(new
        {
            success = true,
            message = "CORS and JWT Validation successful across ports!",
            data = new[]
            {
                new { id = 1, studentName = "Rahul Sharma", course = "PG-DAC", status = "Pending" },
                new { id = 2, studentName = "Priya Patel", course = "DevOps", status = "Contacted" }
            }
        });
    }
}