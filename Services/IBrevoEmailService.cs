using BaleteGroveRES.Models.Admin;

namespace BaleteGroveRES.Services
{
    public interface IBrevoEmailService
    {
        Task SendNewInquiryNotificationToAgentAsync(string agentEmail, Inquiry inquiry);
        Task SendNewInquiryNotificationToAdminAsync(string adminEmail, Inquiry inquiry);
        Task SendAgentAcceptedInquiryToAdminAsync(string adminEmail, string agentName, Inquiry inquiry);
        Task SendVisitorInfoToAgentAsync(string agentEmail, Inquiry inquiry);
        Task SendInquiryAcceptedToVisitorAsync(string visitorEmail, Inquiry inquiry, string agentName);
        Task SendPaymentPendingToAdminAsync(string adminEmail, string agentName, Inquiry inquiry);
        Task SendWelcomeToVisitorAsync(string visitorEmail, Inquiry inquiry, decimal amountPaid, string referenceNumber);
    }
}
