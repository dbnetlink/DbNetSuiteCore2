using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Middleware;
using DbNetSuiteCore.Models;
using DbNetSuiteCore.Repositories;
using DbNetSuiteCore.Timesheet.Constants;
using System.Data;

namespace DbNetSuiteCore.Timesheet.Helpers
{
    public static class ValidationHelper
    {
        public static bool IsValidEmail(string email)
        {
            var trimmedEmail = email.Trim();

            if (trimmedEmail.EndsWith("."))
            {
                return false;
            }
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == trimmedEmail;
            }
            catch
            {
                return false;
            }
        }
    }
}