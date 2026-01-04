using DbNetSuiteCore.Helpers;
using DbNetSuiteCore.Models;

namespace DbNetSuiteCore.UnitTests
{
    public class ColumnsHelperTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void MoveDataOnlyColumnsTest()
        {
            List<GridColumn> gridColumns = new List<GridColumn>()
            {
                new GridColumn("Column3") {DataOnly = true },
                new GridColumn("Column1"),
                new GridColumn("Column2") {InitialSortOrder = Enums.SortOrder.Desc }
            };

            gridColumns = ColumnsHelper.MoveDataOnlyColumnsToEnd(gridColumns).Cast<GridColumn>().ToList();

            Assert.True(gridColumns.First().DataOnly == false);
            Assert.True(gridColumns.Last().DataOnly);
            Assert.True(gridColumns.First(c => c.Expression == "Column2").InitialSortOrder == Enums.SortOrder.Desc);

            List<SelectColumn> selectColumns = new List<SelectColumn>()
            {
                new SelectColumn("Column3") {DataOnly = true },
                new SelectColumn("Column1"),
                new SelectColumn("Column2")
            };

            selectColumns = ColumnsHelper.MoveDataOnlyColumnsToEnd(selectColumns).Cast<SelectColumn>().ToList();

            Assert.True(selectColumns.First().DataOnly == false);
            Assert.True(selectColumns.Last().DataOnly);
        }
    }
}