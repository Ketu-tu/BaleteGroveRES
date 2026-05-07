using System.Diagnostics;
using BaleteGroveRES.Models;
using BaleteGroveRES.Models.Admin; 
using BaleteGroveRES.Data;        
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; 
using Microsoft.AspNetCore.Identity;
using BaleteGroveRES.Services;

namespace BaleteGroveRES.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context; 
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBrevoEmailService _emailService;
        private readonly IConfiguration _config;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context, UserManager<ApplicationUser> userManager, IBrevoEmailService emailService, IConfiguration config) 
        {
            _logger = logger;
            _context = context;
            _userManager = userManager;
            _emailService = emailService;
            _config = config;
        }

        
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult AboutUs()
        {
            return View();
        }
        private const int PageSize = 10;



        public IActionResult Gallery(int page = 1)
        {
            var totalImages = _context.GalleryImages.Count();
            var totalPages = (int)Math.Ceiling(totalImages / (double)PageSize);

            page = Math.Max(1, Math.Min(page, totalPages == 0 ? 1 : totalPages));

            var images = _context.GalleryImages
                .OrderByDescending(g => g.DateAdded)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;

            return View(images);
        }

        
        public async Task<IActionResult> PropertyListings()
        {
            var properties = await _context.Properties
                .OrderByDescending(p => p.DateCreated)
                .ToListAsync();

            var statuses = await _context.PropertyStatuses.ToDictionaryAsync(ps => ps.PropertyId, ps => ps.Status);
            ViewBag.PropertyStatuses = statuses;

            return View(properties);
        }

        [HttpPost]
        public async Task<IActionResult> TrackClick(int id)
        {
            var property = await _context.Properties.FindAsync(id);

            if (property == null)
                return NotFound();

            property.TotalClicks++;

            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpPost]
        public async Task<IActionResult> SubmitInquiry([FromForm] Inquiry model)
        {
            try
            {
                var property = await _context.Properties.FindAsync(model.PropertyId);
                if (property == null)
                    return Json(new { success = false, message = "Property not found." });

                var status = await _context.PropertyStatuses.FirstOrDefaultAsync(ps => ps.PropertyId == model.PropertyId);
                
                if (status != null && status.Status == "Owned")
                {
                    return Json(new { success = false, message = "This property is no longer available." });
                }

                if (status == null)
                {
                    status = new PropertyStatus { PropertyId = model.PropertyId, Status = "Reserved" };
                    _context.PropertyStatuses.Add(status);
                }
                else
                {
                    status.Status = "Reserved";
                }

                model.Status = "Admin Review";
                model.DateSubmitted = DateTime.Now;
                model.Property = property;

                _context.Inquiries.Add(model);
                await _context.SaveChangesAsync();

                var adminEmail = _config["Brevo:SenderEmail"];
                if (!string.IsNullOrEmpty(adminEmail))
                {
                    await _emailService.SendNewInquiryNotificationToAdminAsync(adminEmail, model);
                }

                return Json(new { success = true });
            }
            catch(Exception ex)
            {
                _logger.LogError(ex, "Error submitting inquiry.");
                return Json(new { success = false, message = "An error occurred while processing your request." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetPropertyDetails(int id)
        {
            var property = await _context.Properties
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.PropertyName,
                    p.PropertyImage,
                    p.ExtraImage1,
                    p.ExtraImage2,
                    p.ExtraImage3,
                    p.Type,
                    p.Price,
                    p.Details,
                    p.DateCreated
                })
                .FirstOrDefaultAsync();

            if (property == null)
                return NotFound();

            return Json(property);
        }
        
        public IActionResult Location()
        {
            return View();
        }

        public async Task<IActionResult> News()
        {
            var newsList = await _context.CommunityNews
                .OrderByDescending(n => n.DateAdded)
                .ToListAsync();

            return View(newsList); 
        }
        public async Task<IActionResult> FAQs()
        {
            var faqs = await _context.SiteSettingFAQs
                .OrderBy(f => f.DateAdded) 
                .ToListAsync();

            return View(faqs);
        }

        
        public IActionResult Privacy()
        {
            return View();
        }
        public IActionResult Terms()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}