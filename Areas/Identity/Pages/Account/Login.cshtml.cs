

#nullable disable

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;

namespace BaleteGroveRES.Areas.Identity.Pages.Account
{
    public class LoginModel : PageModel
    {
        private readonly SignInManager<BaleteGroveRES.Models.ApplicationUser> _signInManager;
        private readonly ILogger<LoginModel> _logger;
        private readonly IConfiguration _configuration;

        public LoginModel(SignInManager<BaleteGroveRES.Models.ApplicationUser> signInManager, ILogger<LoginModel> logger, IConfiguration configuration)
        {
            _signInManager = signInManager;
            _logger = logger;
            _configuration = configuration;
        }

        
        
        
        
        [BindProperty]
        public InputModel Input { get; set; }

        
        
        
        
        public IList<AuthenticationScheme> ExternalLogins { get; set; }

        
        
        
        
        public string ReturnUrl { get; set; }

        
        
        
        
        [TempData]
        public string ErrorMessage { get; set; }

        
        
        
        
        public class InputModel
        {
            
            
            
            
            [Required]
            [EmailAddress]
            public string Email { get; set; }

            
            
            
            
            [Required]
            [DataType(DataType.Password)]
            public string Password { get; set; }

            
            
            
            
            [Display(Name = "Remember me?")]
            public bool RememberMe { get; set; }
        }

        public async Task OnGetAsync(string returnUrl = null)
        {
            if (!string.IsNullOrEmpty(ErrorMessage))
            {
                ModelState.AddModelError(string.Empty, ErrorMessage);
            }

            returnUrl ??= Url.Content("~/");

            
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            ReturnUrl = returnUrl;
        }

        public async Task<IActionResult> OnPostAsync(string returnUrl = null)
        {
            returnUrl ??= Url.Content("~/");

            ExternalLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync()).ToList();

            var turnstileResponse = Request.Form["cf-turnstile-response"];
            var secretKey = _configuration["Brevo:Turnstile:SecretKey"];
            
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true,
                SslProtocols = System.Security.Authentication.SslProtocols.Tls12 | System.Security.Authentication.SslProtocols.Tls13
            };
            using var client = new HttpClient(handler);
            
            var content = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("secret", secretKey),
                new KeyValuePair<string, string>("response", turnstileResponse)
            });

            var response = await client.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", content);
            var jsonResponse = await response.Content.ReadAsStringAsync();
            if (!jsonResponse.Contains("\"success\":true") && !jsonResponse.Contains("\"success\": true"))
            {
                ModelState.AddModelError(string.Empty, "Captcha verification failed. Please check the security box.");
                return Page();
            }

            if (ModelState.IsValid)
            {
       
                
                var userAccount = await _signInManager.UserManager.FindByEmailAsync(Input.Email);
                if (userAccount == null)
                {
                    ModelState.AddModelError(string.Empty, $"Account with email {Input.Email} was not found in the database.");
                    return Page();
                }

                if (userAccount.LockoutEnabled && userAccount.LockoutEnd != null && userAccount.LockoutEnd > DateTimeOffset.UtcNow)
                {
                    ModelState.AddModelError(string.Empty, "This account is inactive. Please contact an administrator.");
                    return Page();
                }
                
                var result = await _signInManager.CheckPasswordSignInAsync(userAccount, Input.Password, lockoutOnFailure: true);
                if (result.Succeeded)
                {
                    
                    var freshUser = await _signInManager.UserManager.FindByIdAsync(userAccount.Id);
                    if(freshUser != null)
                    {
                        await _signInManager.UserManager.UpdateSecurityStampAsync(freshUser);
                        await _signInManager.SignInAsync(freshUser, Input.RememberMe);
                    }

                    _logger.LogInformation("User logged in.");
                    
                    var roles = await _signInManager.UserManager.GetRolesAsync(userAccount);

                    if (roles.Contains("SuperAdmin") || roles.Contains("Admin"))
                    {
                        return LocalRedirect("~/Admin/Dashboard");
                    }
                    else if (roles.Contains("Agent"))
                    {
                        return LocalRedirect("~/Agent/AgentDashboard");
                    }

                    return LocalRedirect(returnUrl);
                }
                if (result.RequiresTwoFactor)
                {
                    return RedirectToPage("./LoginWith2fa", new { ReturnUrl = returnUrl, RememberMe = Input.RememberMe });
                }
                if (result.IsLockedOut)
                {
                    _logger.LogWarning("User account locked out.");
                    return RedirectToPage("./Lockout");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Incorrect password provided.");
                    return Page();
                }
            }

            
            return Page();
        }
    }
}
