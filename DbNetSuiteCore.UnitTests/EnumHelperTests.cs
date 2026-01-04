using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.UnitTests
{


    public class EnumHelperTests
    {
        public enum MaritalStatusEnum
        {
            [System.ComponentModel.Description("Married")]
            M,
            [System.ComponentModel.Description("Single")]
            S
        }

        public enum PaymentEnum
        {
            [System.ComponentModel.Description("By Month")]
            Monthly = 2,
            [System.ComponentModel.Description("By Week")]
            Weekly = 1
        }

        public enum ShipperEnum
        {
            SpeedyExpress = 1,
            UnitedPackage = 2,
            FederalShipping = 3
        }

        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void HelperGetEnumOptionsTests()
        {
            List<KeyValuePair<string,string>> options = EnumHelper.GetEnumOptions(typeof(MaritalStatusEnum), typeof(String));

            Assert.True(options.Count == 2);
            Assert.True(options.First().Value == "Married");
            Assert.True(options.First().Key == "M");

            options = EnumHelper.GetEnumOptions(typeof(MaritalStatusEnum), typeof(Int32));

            Assert.True(options.Count == 2);
            Assert.True(options.First().Value == "Married");
            Assert.True(options.First().Key == "0");

            options = EnumHelper.GetEnumOptions(typeof(PaymentEnum), typeof(Int32));

            Assert.True(options.Count == 2);
            Assert.True(options.First().Value == "By Month");
            Assert.True(options.First().Key == "2");

            options = EnumHelper.GetEnumOptions(typeof(ShipperEnum), typeof(Int32));

            Assert.True(options.Count == 3);
            Assert.True(options.Last().Value == "UnitedPackage");
            Assert.True(options.Last().Key == "2");

            Assert.True(EnumHelper.EnumHasDescription(typeof(MaritalStatusEnum)));
            Assert.False(EnumHelper.EnumHasDescription(typeof(ShipperEnum)));


            Assert.True(EnumHelper.GetEnumDescription(PaymentEnum.Weekly) == "By Week");

        }
    }
}