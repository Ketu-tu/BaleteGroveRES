using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using BaleteGroveRES.Models.Admin;

namespace BaleteGroveRES.Services
{
    public class BrevoEmailService : IBrevoEmailService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _config;
        private readonly ILogger<BrevoEmailService> _logger;

        public BrevoEmailService(HttpClient httpClient, IConfiguration config, ILogger<BrevoEmailService> logger)
        {
            _httpClient = httpClient;
            _config = config;
            _logger = logger;
            _httpClient.BaseAddress = new Uri("https://api.brevo.com/v3/");
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlContent)
        {
            var apiKey = _config["Brevo:ApiKey"];
            var senderEmail = _config["Brevo:SenderEmail"];
            var senderName = _config["Brevo:SenderName"];

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(senderEmail))
            {
                _logger.LogWarning("Brevo API Key or Sender Email is not configured.");
                return;
            }

            var payload = new
            {
                sender = new { name = senderName, email = senderEmail },
                to = new[] { new { email = toEmail } },
                subject = subject,
                htmlContent = htmlContent
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, "smtp/email");
            request.Headers.Add("api-key", apiKey);
            request.Content = content;

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Failed to send email. Status: {response.StatusCode}. Response: {responseContent}");
            }
        }

        private string GetStyledHtml(string title, string content)
        {
            return $@"
            <!DOCTYPE html>
            <html>
            <head>
                <style>
                    body {{
                        font-family: 'Arial', sans-serif;
                        background-color: #f4f5f0;
                        margin: 0;
                        padding: 0;
                    }}
                    .email-container {{
                        max-width: 600px;
                        margin: 40px auto;
                        background: #ffffff;
                        border-radius: 12px;
                        box-shadow: 0 4px 20px rgba(0,0,0,0.08);
                        overflow: hidden;
                        border: 1px solid #e1e1e1;
                    }}
                    .email-header {{
                        background: #366642;
                        padding: 20px;
                        text-align: center;
                    }}
                    .email-header h1 {{
                        color: #ffffff;
                        margin: 0;
                        font-size: 24px;
                        letter-spacing: 1px;
                    }}
                    .email-body {{
                        padding: 30px;
                        color: #2e4633;
                        line-height: 1.6;
                        font-size: 15px;
                    }}
                    .email-body h2 {{
                        color: #2d5234;
                        margin-top: 0;
                    }}
                    .email-footer {{
                        background: #f4f5f0;
                        padding: 15px;
                        text-align: center;
                        font-size: 12px;
                        color: #888;
                        border-top: 1px solid #e1e1e1;
                    }}
                    .detail-box {{
                        background: #fcfcfc;
                        border-left: 4px solid #b8c795;
                        padding: 15px;
                        margin: 20px 0;
                        border-radius: 0 4px 4px 0;
                    }}
                    ul {{
                        padding-left: 20px;
                        margin: 10px 0;
                    }}
                    li {{
                        margin-bottom: 8px;
                    }}
                </style>
            </head>
            <body>
                <div class='email-container'>
                    <div class='email-header'>
                        <h1>BALETE GROVE</h1>
                    </div>
                    <div class='email-body'>
                        <h2>{title}</h2>
                        {content}
                    </div>
                    <div class='email-footer'>
                        &copy; {DateTime.Now.Year} Balete Grove Real Estate System. All rights reserved.<br>
                        <em>This is an automated system notification.</em>
                    </div>
                </div>
            </body>
            </html>";
        }

        public async Task SendNewInquiryNotificationToAgentAsync(string agentEmail, Inquiry inquiry)
        {
            var content = $@"
                <p>A new visitor has inquired about a property block.</p>
                <div class='detail-box'>
                    <strong>Lot Name:</strong> {inquiry.Property?.PropertyName}
                </div>
                <p>Please log into your Agent portal as soon as possible to claim this inquiry and view the client's contact information.</p>";

            var styledHtml = GetStyledHtml("New Property Inquiry Available!", content);
            await SendEmailAsync(agentEmail, "New Property Inquiry Available", styledHtml);
        }

        public async Task SendNewInquiryNotificationToAdminAsync(string adminEmail, Inquiry inquiry)
        {
            var content = $@"
                <p>A visitor has submitted a new inquiry.</p>
                <div class='detail-box'>
                    <strong>Lot Name:</strong> {inquiry.Property?.PropertyName}<br><br>
                    <strong>Visitor Stated Reason:</strong><br>
                    <i>{inquiry.Reason}</i>
                </div>
                <p>This inquiry is now pending in the agent pool waiting to be claimed.</p>";

            var styledHtml = GetStyledHtml("Admin Notice: New Property Inquiry", content);
            await SendEmailAsync(adminEmail, "Admin Notice: New Property Inquiry", styledHtml);
        }

        public async Task SendAgentAcceptedInquiryToAdminAsync(string adminEmail, string agentName, Inquiry inquiry)
        {
            var content = $@"
                <p>An inquiry has been officially claimed by an agent and moved into active processing.</p>
                <div class='detail-box'>
                    <strong>Agent:</strong> {agentName}<br>
                    <strong>Lot Name:</strong> {inquiry.Property?.PropertyName}
                </div>";

            var styledHtml = GetStyledHtml("Inquiry Claimed", content);
            await SendEmailAsync(adminEmail, $"Inquiry Claimed by {agentName}", styledHtml);
        }

        public async Task SendVisitorInfoToAgentAsync(string agentEmail, Inquiry inquiry)
        {
            var content = $@"
                <p>You have successfully claimed the inquiry for the property lot: <b>{inquiry.Property?.PropertyName}</b>.</p>
                <p>Below are the contact details of the client so you may reach out and schedule a visitation:</p>
                <div class='detail-box'>
                    <ul style='list-style: none; padding-left: 0;'>
                        <li><strong>Name:</strong> {inquiry.FullName}</li>
                        <li><strong>Email:</strong> {inquiry.Email}</li>
                        <li><strong>Inquiry Reason:</strong> {inquiry.Reason}</li>
                    </ul>
                </div>
                <p>Remember to update the client's status in the <strong>My Clients</strong> tab of your Agent portal once visitation has been scheduled.</p>";

            var styledHtml = GetStyledHtml("Inquiry Claimed Successfully", content);
            await SendEmailAsync(agentEmail, "Visitor Details for Claimed Inquiry", styledHtml);
        }

        public async Task SendInquiryAcceptedToVisitorAsync(string visitorEmail, Inquiry inquiry, string agentName)
        {
            var content = $@"
                <p>Hello {inquiry.FullName},</p>
                <p>Your inquiry for the property lot: <b>{inquiry.Property?.PropertyName}</b> has been taken by one of our agents!</p>
                <div class='detail-box'>
                    <strong>Agent Name:</strong> {agentName}<br>
                    <strong>Lot Name:</strong> {inquiry.Property?.PropertyName}
                </div>
                <p>Visitation would be in a scheduled date. Your designated agent will reach out to you shortly to coordinate.</p>";

            var styledHtml = GetStyledHtml("Inquiry Accepted", content);
            await SendEmailAsync(visitorEmail, "Your Inquiry has been Accepted", styledHtml);
        }

        public async Task SendPaymentPendingToAdminAsync(string adminEmail, string agentName, Inquiry inquiry)
        {
            var content = $@"
                <p>Agent <b>{agentName}</b> has submitted a payment confirmation for an inquiry.</p>
                <div class='detail-box'>
                    <strong>Client Name:</strong> {inquiry.FullName}<br>
                    <strong>Lot Name:</strong> {inquiry.Property?.PropertyName}<br>
                    <strong>Lot Price:</strong> ₱{inquiry.Property?.Price.ToString("N2")}
                </div>
                <p>Please log in to the Admin Dashboard and navigate to the Payments section to officially confirm or cancel this payment submission.</p>";

            var styledHtml = GetStyledHtml("Payment Pending Verification", content);
            await SendEmailAsync(adminEmail, "Action Required: Pending Payment Verification", styledHtml);
        }

        public async Task SendWelcomeToVisitorAsync(string visitorEmail, Inquiry inquiry, decimal amountPaid, string referenceNumber)
        {
            var content = $@"
                <p>Dear {inquiry.FullName},</p>
                <p>Congratulations! Your payment for the property lot has been successfully verified.</p>
                <p><b>Welcome to Balete Grove!</b></p>
                <div class='detail-box'>
                    <strong>Lot Name:</strong> {inquiry.Property?.PropertyName}<br>
                    <strong>Amount Paid:</strong> ₱{amountPaid.ToString("N2")}<br>
                    <strong>Reference Number:</strong> {referenceNumber}
                </div>
                <h3>Next Steps</h3>
                <p>Please prepare the following documents for our upcoming meet and orientation:</p>
                <ul>
                    <li>Two (2) Valid Identifications (Primary IDs)</li>
                    <li>Proof of Address (Utility bill, etc.)</li>
                    <li>Copy of this Payment Confirmation Slip</li>
                </ul>
                <p>Our sales administration team and your designated agent will follow up with you within 24 to 48 hours for finalizing your turnover documents. If you have any immediate concerns, please reply to this email.</p>
                <p>We are thrilled to be part of your home-finding journey.</p>";

            var styledHtml = GetStyledHtml("Payment Confirmed: Welcome to Balete Grove", content);
            await SendEmailAsync(visitorEmail, "Payment Confirmed: Welcome to Balete Grove!", styledHtml);
        }
    }
}
