using Microsoft.AspNetCore.Mvc;
using BaleteGroveRES.Data;
using BaleteGroveRES.Models.Admin;    
using BaleteGroveRES.Models;         
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;
using System.IO;

namespace BaleteGroveSystem.Controllers
{
    [Authorize(Roles = "Admin,SuperAdmin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public AdminController(
                    ApplicationDbContext context,
                    IWebHostEnvironment env,
                    UserManager<ApplicationUser> userManager,
                    RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _env = env;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task<IActionResult> Dashboard()
        {
            int totalProperties = await _context.Properties.CountAsync();
            ViewBag.TotalProperties = totalProperties;
            ViewBag.ActiveInquiries = await _context.Inquiries.Where(i => i.Status == "Pending").CountAsync();
            
            var allStatuses = await _context.PropertyStatuses.ToListAsync();
            int soldCount = allStatuses.Count(s => s.Status == "Owned");
            int reservedCount = allStatuses.Count(s => s.Status == "Reserved");
            int availableCount = totalProperties - soldCount - reservedCount;

            int soldPercent = totalProperties == 0 ? 0 : (int)Math.Round((double)soldCount / totalProperties * 100);
            int reservedPercent = totalProperties == 0 ? 0 : (int)Math.Round((double)reservedCount / totalProperties * 100);
            int availablePercent = totalProperties == 0 ? 0 : 100 - soldPercent - reservedPercent;

            ViewBag.SoldPercent = soldPercent;
            ViewBag.ReservedPercent = reservedPercent;
            ViewBag.AvailablePercent = availablePercent;

            var grossRevenue = await _context.TransactionLedgers.SumAsync(l => l.SaleAmount);
            ViewBag.GrossRevenue = grossRevenue;

            var totalCommissions = await _context.TransactionLedgers.SumAsync(t => t.CommissionAmount);

            var companyExpenses = await _context.CompanyExpenses.SumAsync(e => e.Amount);
            ViewBag.TotalExpenses = companyExpenses + totalCommissions;

            ViewBag.NetProfit = grossRevenue - totalCommissions - companyExpenses;
            
            var agents = await _userManager.GetUsersInRoleAsync("Agent");
            ViewBag.TotalAgents = agents.Count;

            var pendingPayments = await _context.Clients
                .Include(c => c.Inquiry)
                .ThenInclude(i => i.Property)
                .Include(c => c.Agent)
                .Where(c => c.Status == "Payment Pending")
                .ToListAsync();

            ViewBag.PendingPayments = pendingPayments;

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmPayment(int clientId)
        {
            var client = await _context.Clients
                .Include(c => c.Inquiry)
                .ThenInclude(i => i.Property)
                .Include(c => c.Agent)
                .FirstOrDefaultAsync(c => c.Id == clientId);

            if (client == null || client.Status != "Payment Pending") return NotFound();

            client.Status = "Paid";
            client.DatePaid = DateTime.Now;
            client.Inquiry.Status = "Completed";

            var propStatus = await _context.PropertyStatuses.FirstOrDefaultAsync(ps => ps.PropertyId == client.Inquiry.PropertyId);
            if (propStatus == null)
            {
                propStatus = new PropertyStatus { PropertyId = client.Inquiry.PropertyId, Status = "Owned" };
                _context.PropertyStatuses.Add(propStatus);
            }
            else
            {
                propStatus.Status = "Owned";
            }

            var refNumber = "TR-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();
            var saleAmount = client.Inquiry.Property.Price;
            var commission = saleAmount * 0.05m;   

            var ledger = new TransactionLedger
            {
                PropertyId = client.Inquiry.PropertyId,
                AgentUserId = client.AgentUserId,
                BuyerName = client.Inquiry.FullName,
                SaleAmount = saleAmount,
                CommissionAmount = commission,
                ReferenceNumber = refNumber,
                TransactionDate = DateTime.Now
            };
            _context.TransactionLedgers.Add(ledger);

            await _context.SaveChangesAsync();

            var emailService = HttpContext.RequestServices.GetService<BaleteGroveRES.Services.IBrevoEmailService>();
            if (emailService != null && !string.IsNullOrEmpty(client.Inquiry.Email))
            {
                await emailService.SendWelcomeToVisitorAsync(client.Inquiry.Email, client.Inquiry, saleAmount, refNumber);
            }

            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        public async Task<IActionResult> CancelPayment(int clientId)
        {
            var client = await _context.Clients.FindAsync(clientId);
            if (client != null && client.Status == "Payment Pending")
            {
                client.Status = "Processing";      
                await _context.SaveChangesAsync();
            }
            return RedirectToAction("Dashboard");
        }

        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> UserManagement()
        {
            var users = await _userManager.Users.ToListAsync();
            var model = new UserManagementViewModel();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                
                var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == user.Id);
                var fullName = user.UserName;
                if (profile != null && (!string.IsNullOrWhiteSpace(profile.FirstName) || !string.IsNullOrWhiteSpace(profile.LastName)))
                {
                    fullName = $"{profile.FirstName} {profile.LastName}".Trim();
                }

                model.Users.Add(new UserDisplayInfo
                {
                    Id = user.Id,
                    Email = user.Email,
                    FullName = fullName,
                    Role = roles.FirstOrDefault() ?? "No Role",
                    IsActive = !user.LockoutEnabled || (user.LockoutEnd == null || user.LockoutEnd < DateTime.UtcNow),
                    DateCreated = user.DateCreated,
                    ProfilePhotoPath = profile?.ProfilePhotoPath
                });
            }

            return View(model);
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            if (ModelState.IsValid)
            {
                var user = new ApplicationUser
                {
                    UserName = model.FullName,
                    Email = model.Email,
                    EmailConfirmed = true,
                    DateCreated = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(user, model.Password);

                if (result.Succeeded)
                {

                    if (!await _roleManager.RoleExistsAsync(model.Role))
                    {
                        await _roleManager.CreateAsync(new IdentityRole(model.Role));
                    }
                    await _userManager.AddToRoleAsync(user, model.Role);

                    var nameParts = model.FullName.Trim().Split(' ', 2);
                    _context.UserProfiles.Add(new UserProfile
                    {
                        UserId = user.Id,
                        FirstName = nameParts[0],
                        LastName = nameParts.Length > 1 ? nameParts[1] : ""
                    });
                    await _context.SaveChangesAsync();

                    await LogActivityAsync("User Management", "Create", $"Created user: {user.UserName} with role: {model.Role}");

                    return RedirectToAction("UserManagement");
                }

                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
                TempData["ErrorMessage"] = string.Join("; ", result.Errors.Select(e => e.Description));
            }
            else
            {
                TempData["ErrorMessage"] = "Validation failed for the submitted user data.";
            }
            return RedirectToAction("UserManagement");
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> ResetUserPassword(string userId, string newPassword, string adminPassword)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null) return Unauthorized();

            if (string.IsNullOrEmpty(adminPassword) || !await _userManager.CheckPasswordAsync(currentUser, adminPassword))
            {
                TempData["ErrorMessage"] = "Password reset failed: Incorrect Admin password provided.";
                return RedirectToAction("UserManagement");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, token, newPassword);

            if (result.Succeeded)
            {
                await LogActivityAsync("User Management", "Password Reset", $"Reset password for user: {user.UserName}");
                TempData["SuccessMessage"] = "Password successfully reset.";
            }
            else
            {
                TempData["ErrorMessage"] = "Password reset failed: " + string.Join("; ", result.Errors.Select(e => e.Description));
            }

            return RedirectToAction("UserManagement");
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> EditUser(string userId, string fullName, string email, string role)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            user.UserName = fullName;
            user.Email = email;
            var updateResult = await _userManager.UpdateAsync(user);

            var profile = await _context.UserProfiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile != null)
            {
                var nameParts = fullName.Trim().Split(' ', 2);
                profile.FirstName = nameParts[0];
                profile.LastName = nameParts.Length > 1 ? nameParts[1] : "";
                await _context.SaveChangesAsync();
            }

            if (updateResult.Succeeded && !string.IsNullOrEmpty(role))
            {
                var currentRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, currentRoles);
                
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                }
                await _userManager.AddToRoleAsync(user, role);
            }

            await LogActivityAsync("User Management", "Update", $"Updated user details for: {user.UserName}");

            return RedirectToAction("UserManagement");
        }

        [HttpPost]
        [Authorize(Roles = "SuperAdmin")]
        public async Task<IActionResult> DeactivateUser(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null) return NotFound();

            if (user.LockoutEnabled && user.LockoutEnd != null && user.LockoutEnd > DateTime.UtcNow)
            {
                user.LockoutEnd = null;
            }
            else
            {
                user.LockoutEnabled = true;
                user.LockoutEnd = DateTimeOffset.MaxValue;
            }

            await _userManager.UpdateAsync(user);

            await LogActivityAsync("User Management", "Deactivate/Reactivate", $"Toggled lock status for user: {user.UserName}");

            return RedirectToAction("UserManagement");
        }

        public async Task<IActionResult> PropertyManagement(string search = "", int page = 1, int pageSize = 5)
        {
            var query = _context.Properties.Where(p => !p.IsArchived).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(p => p.PropertyName.Contains(search));
            }

            int totalItems = await query.CountAsync();

            var properties = await query
                .OrderByDescending(p => p.DateCreated)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.PageSize = pageSize;
            ViewBag.Search = search;

            ViewBag.ArchivedProperties = await _context.Properties
                .Where(p => p.IsArchived)
                .OrderByDescending(p => p.DateCreated)
                .ToListAsync();

            return View(properties);
        }
        [HttpPost]
        public async Task<IActionResult> CreateProperty(
    Property model,
    IFormFile mainImage,
    IFormFile extra1,
    IFormFile extra2,
    IFormFile extra3)
        {
            if (mainImage != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(mainImage.FileName);
                string path = Path.Combine(_env.WebRootPath, "uploads", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await mainImage.CopyToAsync(stream);
                }

                model.PropertyImage = "/uploads/" + fileName;
            }

            if (extra1 != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(extra1.FileName);
                string path = Path.Combine(_env.WebRootPath, "uploads", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await extra1.CopyToAsync(stream);
                }

                model.ExtraImage1 = "/uploads/" + fileName;
            }

            if (extra2 != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(extra2.FileName);
                string path = Path.Combine(_env.WebRootPath, "uploads", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await extra2.CopyToAsync(stream);
                }

                model.ExtraImage2 = "/uploads/" + fileName;
            }

            if (extra3 != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(extra3.FileName);
                string path = Path.Combine(_env.WebRootPath, "uploads", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await extra3.CopyToAsync(stream);
                }

                model.ExtraImage3 = "/uploads/" + fileName;
            }

            model.DateCreated = DateTime.Now;

            _context.Properties.Add(model);
            await _context.SaveChangesAsync();

            await LogActivityAsync("Property", "Create", $"Created property: {model.PropertyName}");

            return RedirectToAction("PropertyManagement");
        }
        [HttpPost]
        public async Task<IActionResult> EditProperty(
    Property model,
    IFormFile mainImage,
    IFormFile extra1,
    IFormFile extra2,
    IFormFile extra3)
        {
            var property = await _context.Properties.FindAsync(model.Id);

            if (property == null) return NotFound();

            property.PropertyName = model.PropertyName;
            property.Type = model.Type;
            property.Price = model.Price;
            property.Details = model.Details;

            if (mainImage != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(mainImage.FileName);
                string path = Path.Combine(_env.WebRootPath, "uploads", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await mainImage.CopyToAsync(stream);
                }

                property.PropertyImage = "/uploads/" + fileName;
            }

            if (extra1 != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(extra1.FileName);
                string path = Path.Combine(_env.WebRootPath, "uploads", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await extra1.CopyToAsync(stream);
                }

                property.ExtraImage1 = "/uploads/" + fileName;
            }

            if (extra2 != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(extra2.FileName);
                string path = Path.Combine(_env.WebRootPath, "uploads", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await extra2.CopyToAsync(stream);
                }

                property.ExtraImage2 = "/uploads/" + fileName;
            }

            if (extra3 != null)
            {
                string fileName = Guid.NewGuid().ToString() + Path.GetExtension(extra3.FileName);
                string path = Path.Combine(_env.WebRootPath, "uploads", fileName);

                using (var stream = new FileStream(path, FileMode.Create))
                {
                    await extra3.CopyToAsync(stream);
                }

                property.ExtraImage3 = "/uploads/" + fileName;
            }

            await _context.SaveChangesAsync();

            await LogActivityAsync("Property", "Update", $"Updated property details for: {property.PropertyName}");

            return RedirectToAction("PropertyManagement");
        }
        [HttpPost]
        public async Task<IActionResult> ArchiveProperty(int id)
        {
            var property = await _context.Properties.FindAsync(id);

            if (property != null)
            {
                var targetName = property.PropertyName;
                property.IsArchived = true;
                await _context.SaveChangesAsync();
                
                await LogActivityAsync("Property", "Archive", $"Archived property: {targetName}");
            }

            return RedirectToAction("PropertyManagement");
        }

        [HttpPost]
        public async Task<IActionResult> UnarchiveProperty(int id)
        {
            var property = await _context.Properties.FindAsync(id);

            if (property != null)
            {
                var targetName = property.PropertyName;
                property.IsArchived = false;
                await _context.SaveChangesAsync();
                
                await LogActivityAsync("Property", "Restore", $"Restored archived property: {targetName}");
            }

            return RedirectToAction("PropertyManagement");
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

        public async Task<IActionResult> History(int page = 1, int pageSize = 10, string search = "")
        {
            var query = _context.SystemLogs.Include(s => s.User).AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                string s = search.ToLower();
                query = query.Where(l =>
                    l.Email.ToLower().Contains(s) ||
                    l.Module.ToLower().Contains(s) ||
                    l.Action.ToLower().Contains(s) ||
                    l.Details.ToLower().Contains(s));
            }

            query = query.OrderByDescending(s => s.Timestamp);

            bool isSearching = !string.IsNullOrEmpty(search);
            int totalItems = await query.CountAsync();

            List<SystemLog> logs;

            if (isSearching)
            {
                logs = await query.ToListAsync(); 
            }
            else
            {
                logs = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
            }

            var userIds = logs.Select(l => l.UserId).Distinct().ToList();
            var userProfiles = await _context.UserProfiles
                .Where(p => userIds.Contains(p.UserId))
                .ToListAsync();

            ViewBag.UserAvatars = userProfiles.ToDictionary(p => p.UserId, p => p.ProfilePhotoPath);
            ViewBag.UserNames = userProfiles.ToDictionary(p => p.UserId, p => $"{p.FirstName} {p.LastName}".Trim());
            ViewBag.UserPhones = userProfiles.ToDictionary(p => p.UserId, p => p.PhoneNumber);
            ViewBag.UserDOBs = userProfiles.ToDictionary(p => p.UserId, p => p.DateOfBirth);
            ViewBag.UserLeaveStatus = userProfiles.ToDictionary(p => p.UserId, p => p.IsOnLeave);
            
            var userLockouts = logs.Select(l => l.User).DistinctBy(u => u.Id).ToDictionary(
                u => u.Id, 
                u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow
            );
            ViewBag.UserLockouts = userLockouts;

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);
            ViewBag.SearchQuery = search;
            ViewBag.IsSearching = isSearching;   
            ViewBag.PageSize = pageSize;
            ViewBag.TotalItems = totalItems;
            return View(logs);
        }
        public async Task<IActionResult> Inquiries(int page = 1, int pageSize = 10)
        {
            var query = _context.Inquiries
                .Include(i => i.Property)
                .Include(i => i.Agent)
                .OrderByDescending(i => i.DateSubmitted);

            int totalItems = await query.CountAsync();
            var inquiries = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            return View(inquiries);
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmInquiry(int id)
        {
            var inquiry = await _context.Inquiries.Include(i => i.Property).FirstOrDefaultAsync(i => i.Id == id);
            if (inquiry == null || inquiry.Status != "Admin Review") return NotFound();

            inquiry.Status = "Pending";
            await _context.SaveChangesAsync();

            var emailService = HttpContext.RequestServices.GetService<BaleteGroveRES.Services.IBrevoEmailService>();
            if (emailService != null)
            {
                var agents = await _userManager.GetUsersInRoleAsync("Agent");
                foreach (var agent in agents)
                {
                    if (!string.IsNullOrEmpty(agent.Email))
                    {
                        await emailService.SendNewInquiryNotificationToAgentAsync(agent.Email, inquiry);
                    }
                }
            }

            await LogActivityAsync("Inquiries", "Confirm", $"Confirmed inquiry {id} for property {inquiry.Property?.PropertyName}");
            return RedirectToAction("Inquiries");
        }

        [HttpPost]
        public async Task<IActionResult> CancelInquiry(int id)
        {
            var inquiry = await _context.Inquiries.FindAsync(id);
            if (inquiry == null || inquiry.Status != "Admin Review") return NotFound();

            inquiry.Status = "Canceled";
            
            var propStatus = await _context.PropertyStatuses.FirstOrDefaultAsync(ps => ps.PropertyId == inquiry.PropertyId);
            if (propStatus != null && propStatus.Status == "Reserved")
            {
                propStatus.Status = "Available";
            }

            await _context.SaveChangesAsync();
            await LogActivityAsync("Inquiries", "Cancel", $"Canceled admin review inquiry {id}");
            return RedirectToAction("Inquiries");
        }

        [HttpPost]
        public async Task<IActionResult> RevokeInquiry(int id)
        {
            var inquiry = await _context.Inquiries.FindAsync(id);
            if (inquiry == null || (inquiry.Status != "Accepted" && inquiry.Status != "Processing" && inquiry.Status != "Payment Pending"))
                return NotFound();

            var client = await _context.Clients.FirstOrDefaultAsync(c => c.InquiryId == id);
            if (client != null)
            {
                _context.Clients.Remove(client);
            }

            inquiry.Status = "Pending";
            inquiry.AgentUserId = null;

            await _context.SaveChangesAsync();
            await LogActivityAsync("Inquiries", "Revoke", $"Revoked inquiry {id} from agent and returned to pool");
            return RedirectToAction("Inquiries");
        }

        public async Task<IActionResult> TransactionLedger()
        {
            var ledgers = await _context.TransactionLedgers
                .Include(l => l.Property)
                .Include(l => l.Agent)
                .OrderByDescending(l => l.TransactionDate)
                .ToListAsync();

            return View(ledgers);
        }

        public async Task<IActionResult> AgentPerformance()
        {
            var agents = await _userManager.GetUsersInRoleAsync("Agent");
            var agentIds = agents.Select(a => a.Id).ToList();

            var profiles = await _context.UserProfiles
                .Where(p => agentIds.Contains(p.UserId))
                .ToDictionaryAsync(p => p.UserId);

            var currentMonthStart = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            
            var salesData = await _context.TransactionLedgers
                .Where(t => agentIds.Contains(t.AgentUserId) && t.TransactionDate >= currentMonthStart)
                .GroupBy(t => t.AgentUserId)
                .Select(g => new { AgentId = g.Key, TotalSales = g.Sum(t => t.SaleAmount) })
                .ToDictionaryAsync(g => g.AgentId, g => g.TotalSales);

            ViewBag.Agents = agents;
            ViewBag.Profiles = profiles;
            ViewBag.SalesData = salesData;

            return View();
        }

        public async Task<IActionResult> StatementOfAccount(int id)
        {
            var ledger = await _context.TransactionLedgers
                .Include(l => l.Property)
                .Include(l => l.Agent)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (ledger == null) return NotFound();

            return View(ledger);
        }

        public async Task<IActionResult> CommunityNews()
        {
            var news = await _context.CommunityNews
                .OrderByDescending(n => n.DateAdded)
                .ToListAsync();

            return View(news);
        }
        [HttpPost]
        public async Task<IActionResult> Create(CommunityNews model, IFormFile imageFile)
        {
            if (ModelState.IsValid)
            {
                if (imageFile != null && imageFile.Length > 0)
                {
                    string folder = Path.Combine(_env.WebRootPath, "uploads/news");
                    Directory.CreateDirectory(folder);

                    string fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                    string filePath = Path.Combine(folder, fileName);

                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await imageFile.CopyToAsync(stream);
                    }

                    model.ImagePath = "/uploads/news/" + fileName;
                }

                model.DateAdded = DateTime.UtcNow;

                _context.CommunityNews.Add(model);
                await _context.SaveChangesAsync();
                
                await LogActivityAsync("Community News", "Create", $"Created news headline: {model.Headline}");
            }

            return RedirectToAction("CommunityNews");
        }
        [HttpPost]
        public async Task<IActionResult> Edit(CommunityNews model, IFormFile imageFile)
        {
            var news = await _context.CommunityNews.FindAsync(model.Id);
            if (news == null) return NotFound();

            news.Headline = model.Headline;
            news.Information = model.Information;

            if (imageFile != null && imageFile.Length > 0)
            {
                string folder = Path.Combine(_env.WebRootPath, "uploads/news");
                Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                news.ImagePath = "/uploads/news/" + fileName;
            }

            await _context.SaveChangesAsync();
            
            await LogActivityAsync("Community News", "Update", $"Updated news headline: {news.Headline}");
            
            return RedirectToAction("CommunityNews");
        }
        [HttpPost]
        public async Task<IActionResult> Delete(int Id)
        {
            var news = await _context.CommunityNews.FindAsync(Id);
            if (news == null) return NotFound();

            var headlineName = news.Headline;
            
            if (!string.IsNullOrEmpty(news.ImagePath))
            {
                var oldPath = Path.Combine(_env.WebRootPath, news.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            _context.CommunityNews.Remove(news);
            await _context.SaveChangesAsync();

            await LogActivityAsync("Community News", "Delete", $"Deleted news headline: {headlineName}");

            return RedirectToAction("CommunityNews");
        }

        public async Task<IActionResult> ImageGallery()
        {
            var images = await _context.GalleryImages
                .OrderByDescending(x => x.DateAdded)
                .ToListAsync();

            return View(images);
        }
        [HttpPost]
        public async Task<IActionResult> AddGallery(IFormFile imageFile)
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                string folder = Path.Combine(_env.WebRootPath, "uploads/gallery");
                Directory.CreateDirectory(folder);

                string fileName = Guid.NewGuid() + Path.GetExtension(imageFile.FileName);
                string filePath = Path.Combine(folder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                var image = new GalleryImage
                {
                    ImagePath = "/uploads/gallery/" + fileName,
                    DateAdded = DateTime.UtcNow
                };

                _context.GalleryImages.Add(image);
                await _context.SaveChangesAsync();
                
                await LogActivityAsync("Gallery", "Upload", $"Uploaded a new gallery photo.");
            }

            return RedirectToAction("ImageGallery");
        }
        [HttpPost]
        public async Task<IActionResult> DeleteGallery(int Id)
        {
            var image = await _context.GalleryImages.FindAsync(Id);
            if (image == null) return NotFound();

            if (!string.IsNullOrEmpty(image.ImagePath))
            {
                var oldPath = Path.Combine(_env.WebRootPath, image.ImagePath.TrimStart('/'));
                if (System.IO.File.Exists(oldPath))
                    System.IO.File.Delete(oldPath);
            }

            _context.GalleryImages.Remove(image);
            await _context.SaveChangesAsync();

            await LogActivityAsync("Gallery", "Delete", $"Deleted a gallery photo.");

            return RedirectToAction("ImageGallery");
        }

        public async Task<IActionResult> SiteSettings()
        {
            var faqs = await _context.SiteSettingFAQs
                .OrderByDescending(f => f.DateAdded)
                .ToListAsync();

            return View(faqs);
        }
        [HttpPost]
        public async Task<IActionResult> CreateFAQ(SiteSettingFAQ model)
        {
            if (ModelState.IsValid)
            {
                model.DateAdded = DateTime.UtcNow;

                _context.SiteSettingFAQs.Add(model);
                await _context.SaveChangesAsync();
                
                await LogActivityAsync("Site Settings", "Setup FAQ", $"Created new FAQ item.");
            }

            return RedirectToAction("SiteSettings");
        }
        [HttpPost]
        public async Task<IActionResult> EditFAQ(SiteSettingFAQ model)
        {
            var faq = await _context.SiteSettingFAQs.FindAsync(model.Id);
            if (faq == null) return NotFound();

            faq.Question = model.Question;
            faq.Answer = model.Answer;

            await _context.SaveChangesAsync();

            return RedirectToAction("SiteSettings");
        }
        [HttpPost]
        public async Task<IActionResult> DeleteFAQ(int Id)
        {
            var faq = await _context.SiteSettingFAQs.FindAsync(Id);
            if (faq == null) return NotFound();

            _context.SiteSettingFAQs.Remove(faq);
            await _context.SaveChangesAsync();

            await LogActivityAsync("Site Settings", "Delete FAQ", $"Deleted FAQ item.");

            return RedirectToAction("SiteSettings");
        }

        public async Task<IActionResult> AdminProfile()
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
                IsActive = true
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditAdminProfile(AdminProfileViewModel vm)
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
                    return RedirectToAction("AdminProfile");
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
                    return RedirectToAction("AdminProfile");
                }

                var passwordCheck = await _userManager.CheckPasswordAsync(user, vm.CurrentPassword ?? "");
                if (!passwordCheck)
                {
                    TempData["ErrorMessage"] = "Current password is incorrect.";
                    return RedirectToAction("AdminProfile");
                }

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var result = await _userManager.ResetPasswordAsync(user, token, vm.NewPassword);

                if (!result.Succeeded)
                {
                    TempData["ErrorMessage"] = string.Join(" ", result.Errors.Select(e => e.Description));
                    return RedirectToAction("AdminProfile");
                }
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "Profile updated successfully.";
            return RedirectToAction("AdminProfile");
        }


        [Authorize(Roles = "SuperAdmin, Admin")]
        public async Task<IActionResult> Expenses()
        {
            var expenses = await _context.CompanyExpenses.OrderByDescending(e => e.DateIncurred).ToListAsync();
            return View(expenses);
        }

        [Authorize(Roles = "SuperAdmin, Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddExpense(CompanyExpense model)
        {
            if (ModelState.IsValid)
            {
                _context.CompanyExpenses.Add(model);
                await _context.SaveChangesAsync();
                await LogActivityAsync("Finance Module", "Logged Expense", $"Logged an expense of {model.Amount:C} for {model.Category}.");
            }
            return RedirectToAction(nameof(Expenses));
        }

        [Authorize(Roles = "SuperAdmin, Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditExpense(int id, string category, string description, decimal amount)
        {
            var expense = await _context.CompanyExpenses.FindAsync(id);
            if (expense != null)
            {
                expense.Category = category;
                expense.Description = description;
                expense.Amount = amount;
                await _context.SaveChangesAsync();
                await LogActivityAsync("Finance Module", "Edited Expense", $"Edited an expense ID #{id}.");
            }
            return RedirectToAction(nameof(Expenses));
        }

        [Authorize(Roles = "SuperAdmin, Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            var expense = await _context.CompanyExpenses.FindAsync(id);
            if (expense != null)
            {
                _context.CompanyExpenses.Remove(expense);
                await _context.SaveChangesAsync();
                await LogActivityAsync("Finance Module", "Deleted Expense", $"Deleted an expense ID #{id}.");
            }
            return RedirectToAction(nameof(Expenses));
        }

        [Authorize(Roles = "SuperAdmin, Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkCommissionPaid(int ledgerId)
        {
            var ledger = await _context.TransactionLedgers.FindAsync(ledgerId);
            if (ledger != null && !ledger.IsCommissionPaid)
            {
                ledger.IsCommissionPaid = true;
                ledger.DateCommissionPaid = DateTime.Now;
                await _context.SaveChangesAsync();
                await LogActivityAsync("Transaction Ledger", "Disbursed Commission", $"Marked commission as paid for ledger #{ledgerId}.");
            }
            return RedirectToAction(nameof(TransactionLedger));
        }

        [HttpPost]
        public IActionResult Logout()
        {
            return RedirectToPage("/Account/Login", new { area = "Identity" });
        }
    }
}