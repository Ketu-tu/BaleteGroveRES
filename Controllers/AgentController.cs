using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using BaleteGroveRES.Models;
using BaleteGroveRES.Models.Admin;
using BaleteGroveRES.Data;
using BaleteGroveRES.Services;
using Microsoft.EntityFrameworkCore;

namespace BaleteGroveRES.Controllers
{
    [Authorize(Roles = "Agent")]
    public class AgentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IBrevoEmailService _emailService;
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public AgentController(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IBrevoEmailService emailService, IConfiguration config, IWebHostEnvironment env)
        {
            _userManager = userManager;
            _context = context;
            _emailService = emailService;
            _config = config;
            _env = env;
        }

        public async Task<IActionResult> AgentDashboard()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            
            var totalInquiries = await _context.Clients.CountAsync(c => c.AgentUserId == currentUser.Id);
            var activeClients = await _context.Clients.CountAsync(c => c.AgentUserId == currentUser.Id && (c.Status == "Processing" || c.Status == "Payment Pending"));
            var closedDeals = await _context.TransactionLedgers.CountAsync(t => t.AgentUserId == currentUser.Id);

            ViewBag.TotalInquiries = totalInquiries;
            ViewBag.ActiveClients = activeClients;
            ViewBag.ClosedDeals = closedDeals;

            var recentInquiries = await _context.Clients
                .Include(c => c.Inquiry)
                .ThenInclude(i => i.Property)
                .Where(c => c.AgentUserId == currentUser.Id)
                .OrderByDescending(c => c.DateAccepted)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentInquiries = recentInquiries;

            var commissions = await _context.TransactionLedgers
                .Include(t => t.Property)
                .Include(t => t.Agent)
                .Where(t => t.AgentUserId == currentUser.Id)
                .OrderByDescending(t => t.TransactionDate)
                .ToListAsync();

            ViewBag.Commissions = commissions;

            decimal pendingCommissions = commissions.Where(c => !c.IsCommissionPaid).Sum(c => c.CommissionAmount);
            decimal paidCommissions = commissions.Where(c => c.IsCommissionPaid).Sum(c => c.CommissionAmount);
            
            var currentMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            decimal currentMonthSales = commissions
                .Where(c => c.TransactionDate >= currentMonthStart)
                .Sum(c => c.SaleAmount);

            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
            decimal quota = profile?.MonthlySalesQuota ?? 10000000m;
            bool isOnLeave = profile?.IsOnLeave ?? false;

            ViewBag.PendingCommissions = pendingCommissions;
            ViewBag.PaidCommissions = paidCommissions;
            ViewBag.CurrentMonthSales = currentMonthSales;
            ViewBag.MonthlySalesQuota = quota;
            ViewBag.IsOnLeave = isOnLeave;

            var allPropsList = await _context.Properties.ToListAsync();
            var allStatuses = await _context.PropertyStatuses.ToListAsync();
            
            var availablePropertiesData = allPropsList.Where(p => 
            {
                var stat = allStatuses.FirstOrDefault(s => s.PropertyId == p.Id);
                return stat == null || stat.Status == "Available";
            }).OrderByDescending(p => p.TotalClicks).ToList();

            ViewBag.AvailablePropertiesData = availablePropertiesData;
            ViewBag.AvailableProperties = availablePropertiesData.Count;

            return View();
        }

        [HttpGet]
        public async Task<IActionResult> CommissionSlip(int ledgerId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var ledger = await _context.TransactionLedgers
                .Include(t => t.Property)
                .Include(t => t.Agent)
                .FirstOrDefaultAsync(t => t.Id == ledgerId && t.AgentUserId == currentUser.Id);

            if (ledger == null) return NotFound();

            return View(ledger);
        }

        public async Task<IActionResult> Inquiries()
        {
            var poolInquiries = await _context.Inquiries
                .Include(i => i.Property)
                .Where(i => i.Status == "Pending")
                .ToListAsync();
                
            var currentUser = await _userManager.GetUserAsync(User);
            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == currentUser.Id);
            ViewBag.IsOnLeave = profile?.IsOnLeave ?? false;

            var pastInquiries = await _context.Inquiries
                .Include(i => i.Property)
                .Where(i => i.Status == "Completed" || i.Status == "Canceled")
                .OrderByDescending(i => i.DateSubmitted)
                .ToListAsync();

            ViewBag.PastInquiries = pastInquiries;

            return View(poolInquiries); 
        }

        [HttpPost]
        public async Task<IActionResult> AcceptInquiry(int id)
        {
            var inquiry = await _context.Inquiries.Include(i => i.Property).FirstOrDefaultAsync(i => i.Id == id);
            if (inquiry == null || inquiry.Status != "Pending") return NotFound();

            var currentUser = await _userManager.GetUserAsync(User);
            
            inquiry.Status = "Accepted";
            inquiry.AgentUserId = currentUser.Id;

            var client = new Client
            {
                InquiryId = inquiry.Id,
                AgentUserId = currentUser.Id,
                DateAccepted = DateTime.Now,
                Status = "Processing"
            };
            _context.Clients.Add(client);

            await _context.SaveChangesAsync();

            var adminEmail = _config["Brevo:SenderEmail"];
            if (!string.IsNullOrEmpty(adminEmail))
            {
                await _emailService.SendAgentAcceptedInquiryToAdminAsync(adminEmail, currentUser.UserName, inquiry);
            }
            await _emailService.SendVisitorInfoToAgentAsync(currentUser.Email, inquiry);

            if (!string.IsNullOrEmpty(inquiry.Email))
            {
                await _emailService.SendInquiryAcceptedToVisitorAsync(inquiry.Email, inquiry, currentUser.UserName);
            }

            await LogActivityAsync("Inquiries", "Accept", $"Accepted inquiry from {inquiry.FullName} for property ID: {inquiry.PropertyId}");

            return RedirectToAction("Clients");
        }

        public async Task<IActionResult> Clients()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var clients = await _context.Clients
                .Include(c => c.Inquiry)
                .ThenInclude(i => i.Property)
                .Where(c => c.AgentUserId == currentUser.Id && c.Status == "Processing")
                .ToListAsync();

            return View(clients);
        }

        [HttpPost]
        public async Task<IActionResult> ScheduleVisitation(int clientId)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client != null)
            {
                client.DateVisitationScheduled = DateTime.Now;
                await _context.SaveChangesAsync();
                await LogActivityAsync("Clients", "Schedule Visitation", $"Scheduled visitation for client ID: {client.Id}");
            }
            return RedirectToAction("Clients");
        }

        [HttpPost]
        public async Task<IActionResult> FinishVisitation(int clientId)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client != null)
            {
                client.DateVisitationFinished = DateTime.Now;
                await _context.SaveChangesAsync();
                await LogActivityAsync("Clients", "Finish Visitation", $"Completed visitation for client ID: {client.Id}");
            }
            return RedirectToAction("Clients");
        }

        [HttpPost]
        public async Task<IActionResult> CancelClient(int clientId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var client = await _context.Clients
                .Include(c => c.Inquiry)
                .FirstOrDefaultAsync(c => c.Id == clientId && c.AgentUserId == currentUser.Id);

            if (client != null && client.Status != "Paid" && client.Status != "Canceled")
            {
                client.Status = "Canceled";
                if (client.Inquiry != null)
                {
                    client.Inquiry.Status = "Canceled";
                    var propStatus = await _context.PropertyStatuses.FirstOrDefaultAsync(ps => ps.PropertyId == client.Inquiry.PropertyId);
                    if (propStatus != null)
                    {
                        propStatus.Status = "Available";
                    }
                }
                await _context.SaveChangesAsync();
                await LogActivityAsync("Clients", "Cancel Engagement", $"Canceled engagement with client ID: {client.Id}");
            }
            return RedirectToAction("Clients");
        }

        public async Task<IActionResult> Payment(int clientId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var client = await _context.Clients
                .Include(c => c.Inquiry)
                .ThenInclude(i => i.Property)
                .FirstOrDefaultAsync(c => c.Id == clientId && c.AgentUserId == currentUser.Id && c.Status == "Processing");

            if (client == null) return NotFound();

            return View(client);
        }

        [HttpPost]
        public async Task<IActionResult> ProcessPayment(int clientId)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var client = await _context.Clients
                .Include(c => c.Inquiry)
                .ThenInclude(i => i.Property)
                .FirstOrDefaultAsync(c => c.Id == clientId);
            
            if (client == null || client.AgentUserId != currentUser.Id) return NotFound();

            client.Status = "Payment Pending";

            await _context.SaveChangesAsync();
            
            var adminEmail = _config["Brevo:SenderEmail"];
            if (!string.IsNullOrEmpty(adminEmail))
            {
                await _emailService.SendPaymentPendingToAdminAsync(adminEmail, currentUser.UserName, client.Inquiry);
            }

            await LogActivityAsync("Payments", "Submit", $"Submitted payment for verification for client ID: {client.Id}");

            return RedirectToAction("TransactionHistory");


        }

        public IActionResult Properties()
        {
            return View();
        }

        public async Task<IActionResult> TransactionHistory()
        {
            var currentUser = await _userManager.GetUserAsync(User);
            var history = await _context.Clients
                .Include(c => c.Inquiry)
                .ThenInclude(i => i.Property)
                .Where(c => c.AgentUserId == currentUser.Id)
                .OrderByDescending(c => c.DateAccepted)
                .ToListAsync();

            return View(history);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleLeaveStatus()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
            if (profile == null)
            {
                profile = new UserProfile { UserId = user.Id };
                _context.UserProfiles.Add(profile);
            }

            profile.IsOnLeave = !profile.IsOnLeave;
            await _context.SaveChangesAsync();

            TempData["Success"] = profile.IsOnLeave ? "You are now on leave." : "You are now active.";
            return RedirectToAction("Profile");
        }

        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.FirstOrDefault() ?? "N/A";

            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            string? firstName = profile?.FirstName;
            string? lastName = profile?.LastName;

            if (profile == null)
            {
                var nameParts = (user.UserName ?? "").Split(' ', 2);
                firstName = nameParts.Length > 0 ? nameParts[0] : null;
                lastName = nameParts.Length > 1 ? nameParts[1] : null;
            }

            var vm = new AdminProfileViewModel
            {
                UserId = user.Id,
                Email = user.Email ?? "",
                Role = role,
                DateCreated = user.DateCreated,
                FirstName = firstName,
                LastName = lastName,
                PhoneNumber = profile?.PhoneNumber ?? user.PhoneNumber,
                DateOfBirth = profile?.DateOfBirth,
                ProfilePhotoPath = profile?.ProfilePhotoPath,
                IsActive = true,
                IsOnLeave = profile?.IsOnLeave ?? false,
                MonthlySalesQuota = profile?.MonthlySalesQuota ?? 10000000m
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAgentProfile(AdminProfileViewModel vm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            var profile = await _context.UserProfiles
                .FirstOrDefaultAsync(p => p.UserId == user.Id);

            if (profile == null)
            {
                profile = new UserProfile { UserId = user.Id };
                _context.UserProfiles.Add(profile);
            }

            if (Request.Form.ContainsKey("FirstName"))
            {
                profile.FirstName = vm.FirstName;
                profile.LastName = vm.LastName;
                profile.PhoneNumber = vm.PhoneNumber;
                profile.DateOfBirth = vm.DateOfBirth;
            }

            if (vm.PhotoFile != null && vm.PhotoFile.Length > 0)
            {
                var allowed = new[] { "image/jpeg", "image/png", "image/gif", "image/webp" };
                if (!allowed.Contains(vm.PhotoFile.ContentType))
                {
                    TempData["ErrorMessage"] = "Only image files are allowed.";
                    return RedirectToAction("Profile");
                }

                var folder = Path.Combine(_env.WebRootPath, "uploads", "profiles");
                Directory.CreateDirectory(folder);

                if (!string.IsNullOrEmpty(profile.ProfilePhotoPath))
                {
                    var oldPath = Path.Combine(_env.WebRootPath, profile.ProfilePhotoPath.TrimStart('/'));
                    if (System.IO.File.Exists(oldPath))
                        System.IO.File.Delete(oldPath);
                }

                var fileName = $"{user.Id}_{Path.GetRandomFileName()}{Path.GetExtension(vm.PhotoFile.FileName)}";
                var filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await vm.PhotoFile.CopyToAsync(stream);

                profile.ProfilePhotoPath = $"/uploads/profiles/{fileName}";
            }

            if (!string.IsNullOrWhiteSpace(vm.NewPassword))
            {
                if (vm.NewPassword != vm.ConfirmPassword)
                {
                    TempData["ErrorMessage"] = "New passwords do not match.";
                    return RedirectToAction("Profile");
                }

                var passwordCheck = await _userManager.CheckPasswordAsync(user, vm.CurrentPassword ?? "");
                if (!passwordCheck)
                {
                    TempData["ErrorMessage"] = "Current password is incorrect.";
                    return RedirectToAction("Profile");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, vm.NewPassword);

                if (!result.Succeeded)
                {
                    TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
                    return RedirectToAction("Profile");
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile updated successfully!";
            return RedirectToAction("Profile");
        }

        private async Task LogActivityAsync(string module, string action, string details)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user != null)
            {
                _context.SystemLogs.Add(new SystemLog
                {
                    UserId = user.Id,
                    Email = user.Email ?? "Unknown",
                    Module = module,
                    Action = action,
                    Details = details,
                    Timestamp = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }
        }
    }
}