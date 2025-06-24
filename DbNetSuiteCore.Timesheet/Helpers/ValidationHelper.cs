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
        public static void AssignOptions(DbNetSuiteCoreOptions options)
        {
            options.FormUpdateValidationDelegate = async (formModel, httpContext, configuration) =>
            {
                return ValidateFormUpdate(formModel, httpContext, configuration);
            };
            options.FormDeleteValidationDelegate = async (formModel, httpContext, configuration) =>
            {
                return ValidateFormDelete(formModel, httpContext, configuration);
            };
            options.FormInsertValidationDelegate = async (formModel, httpContext, configuration) =>
            {
                return ValidateFormInsert(formModel, httpContext, configuration);
            };

        }
        public static bool ValidateFormUpdate(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            switch (formModel.TableName)
            {
                case "AspNetUsers":
                    if (formModel.FormValues.ContainsKey("Email"))
                    {
                        if (IsValidEmail(formModel.FormValues["Email"]) == false)
                        {
                            formModel.Message = "Format of email address is not valid";
                            formModel.Columns.First(c => c.Name == "Email").InError = true;
                            return false;
                        }
                    }
                    break;
            }
            return true;
        }


        public static bool ValidateFormDelete(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            switch (formModel.TableName)
            {
                case "AspNetUsers":
                    formModel.Message = "Cannot delete User";
                    return false;
            }
            return true;
        }

        public static bool ValidateFormInsert(FormModel formModel, HttpContext httpContext, IConfiguration configuration)
        {
            switch (formModel.TableName)
            {
                case "AspNetUserRoles":
                    formModel.FormValues["userid"] = TextHelper.DeobfuscateString(formModel.FormValues["userid"]);
                    break;
            }
            return true;
        }

        private static bool IsValidEmail(string email)
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