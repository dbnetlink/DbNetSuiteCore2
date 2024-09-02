using System.ComponentModel;

namespace DbNetSuiteCore.Web.Enums
{
    public enum PaymentEnum
    {
        [Description("By Month")]
        Monthly = 2,
        [Description("By Week")]
        Weekly = 1
    }
}
