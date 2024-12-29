using DbNetSuiteCore.Enums;
using DbNetSuiteCore.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace DbNetSuiteCore.Web.Pages
{
    [IgnoreAntiforgeryToken]
    public class AccountsModel : PageModel
    {
        private string _activeTab = string.Empty;

        private DataSourceType _dateSourceType = DataSourceType.SQLite;


        [FromQuery(Name = "financialyear")]
        public int FinancialYear { get; set; } = DateTime.Now.Year;
        [FromQuery(Name = "activetab")]
        public string ActiveTab 
        { 
            get { return TabIds.Contains(_activeTab) ? _activeTab : TabIds.First(); }
            set { _activeTab = value; } 
        }
        public List<string> Tabs = new List<string> { "Journals", "Chart Of Accounts", "Account Types", "Settings", "Year End Report", "Users" };
        public IEnumerable<string> TabIds => Tabs.Select(t => t.Replace(" ", ""));
        public void OnGet() { }
        public void OnPost(int financialYear, string activeTab) 
        {
            FinancialYear = financialYear;
            ActiveTab = activeTab;
        }

        public FormModel? GetFormModel()
        {
            FormModel? formModel = null;
            switch (ActiveTab)
            {
                case "Journals":
                    formModel = new FormModel(_dateSourceType, "dbnetlink", "JournalEntry") { Insert = true, Delete = true, LayoutColumns = 6 };
                    formModel.Columns = new List<FormColumn>()
                    {
                        new FormColumn("Id") {PrimaryKey = true, ForeignKey = true, DataOnly = true},
                        new FormColumn("Description") { Suggest = true },
                        new FormColumn("TransactionDate","Date") { DataType = typeof(DateTime)},
                        new FormColumn("AccountID", "Account"){Lookup = new Lookup("ChartOfAccounts","Id","Description")},
                        new FormColumn("Debit") { Format = "c" },
                        new FormColumn("Credit") { Format = "c" }
                    };
                    formModel.ClientEvents[FormClientEvent.ValidateUpdate] = "validateJournalEntry";
                    break;
                case "ChartOfAccounts":
                    formModel = new FormModel(_dateSourceType, "dbnetlink", "ChartOfAccounts") { Insert = true, Delete = true };
                    formModel.Columns = new List<FormColumn>
                    {
                        new FormColumn("Id") {PrimaryKey = true, ForeignKey = true, DataOnly = true},
                        new FormColumn("Description"),
                        new FormColumn("AccountTypeId", "Account Type") {Lookup = new Lookup("AccountType","Id","Description")}
                    };
                    break;
                case "AccountTypes":
                    formModel = new FormModel(_dateSourceType, "dbnetlink", "AccountType") { Insert = true, Delete = true };
                    formModel.Columns = new List<FormColumn>
                    {
                        new FormColumn("Id") {PrimaryKey = true, ForeignKey = true, DataOnly = true},
                        new FormColumn("Description")
                    };
                    break;
                case "Settings":
                    formModel = new FormModel(_dateSourceType, "dbnetlink", "Settings") { Insert = true};
                    formModel.Columns = new List<FormColumn>
                    {
                        new FormColumn("Id") { DataOnly = true},
                        new FormColumn("OpeningBalance") {Format = "c", DataType = typeof(decimal)}
                    };
                    break;
                case "YearEndReport":
                    break;
                case "Users":
                    formModel = new FormModel(_dateSourceType, "dbnetlink", "Users") { Insert = true, Delete = true };
                    formModel.Columns = new List<FormColumn>
                    {
                        new FormColumn("Id") { PrimaryKey = true, ForeignKey = true, DataOnly = true},
                        new FormColumn("Name") { ReadOnly = ReadOnlyMode.UpdateOnly},
                        new FormColumn("RoleId", "Role") { Lookup = new Lookup("Roles", "Id", "Name")},
                        new FormColumn("Password") { ControlType = FormControlType.Password, Pattern = "^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d).{8,}$", HelpText = "At least 8 chars, 1 number and 1 upper and lowercase character", HashPassword = true}
                    };
                    formModel.ClientEvents[FormClientEvent.ValidateDelete] = "validateUserDelete";
                    break;
            }

            return formModel;
        }

        public GridModel? GetGridModel()
        {
            GridModel? gridModel = null;
            switch (ActiveTab)
            {
                case "Journals":
                    gridModel = new GridModel(_dateSourceType, "dbnetlink", "JournalView");
                    gridModel.Columns = new List<GridColumn>()
                    {
                        new GridColumn("Id") {PrimaryKey = true},
                        new GridColumn("Description"),
                        new GridColumn("TransactionDate","Date") { InitialSortOrder = SortOrder.Desc},
                        new GridColumn("AccountID","Account"){Lookup = new Lookup("ChartOfAccounts","Id","Description")},
                        new GridColumn("Debit") { Format = "c" },
                        new GridColumn("Credit") { Format = "c" },
                        new GridColumn("CurrentBalance") { Format = "c", DataType = typeof(Decimal) },
                    };
                    AddFinancialYearFilter(gridModel);
                    break;
                case "ChartOfAccounts":
                    gridModel = new GridModel(_dateSourceType, "dbnetlink", "ChartOfAccounts");
                    gridModel.Columns = new List<GridColumn>()
                    {
                        new GridColumn("Id"),
                        new GridColumn("Description"),
                        new GridColumn("AccountTypeId", "Account Type") {Lookup = new Lookup("AccountType","Id","Description")}
                    };
                     break;
                case "AccountTypes":
                    gridModel = new GridModel(_dateSourceType, "dbnetlink", "AccountType");
                    gridModel.Columns = new List<GridColumn>()
                    {
                        new GridColumn("Id"),
                        new GridColumn("Description")
                    };
                    break;
                case "Settings":
                    break;
                case "YearEndReport":
                    gridModel = new GridModel(_dateSourceType, "dbnetlink", "JournalEntry");
                    gridModel.Columns = new List<GridColumn>()
                    {
                        new GridColumn("AccountID","Account"){Lookup = new Lookup("ChartOfAccounts","Id","Description")},
                        new GridColumn("coalesce(Debit,0) - coalesce(Credit,0)", "Amount") {Aggregate = AggregateType.Sum, Format = "c" }
                    };
                    AddFinancialYearFilter(gridModel);
                    break;
                case "Users":
                    gridModel = new GridModel(_dateSourceType, "dbnetlink", "Users") {};
                    gridModel.Columns = new List<GridColumn>
                    {
                        new GridColumn("Id") { DataOnly = true},
                        new GridColumn("Name") {},
                        new GridColumn("RoleId", "Role") { Lookup = new Lookup("Roles", "Id", "Name")}
                    };
                    break;
            }

            return gridModel;
        }

        private void AddFinancialYearFilter(GridModel gridModel)
        {
            gridModel.FixedFilter = "TransactionDate between @startDate and @endDate";
            gridModel.FixedFilterParameters.Add(new DbParameter("@startDate", new DateTime(FinancialYear, 4, 1)));
            gridModel.FixedFilterParameters.Add(new DbParameter("@endDate", new DateTime(FinancialYear + 1, 3, 31)));
        }
    }
}
