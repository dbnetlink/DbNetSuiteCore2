using DbNetTimeCore.Services.Interfaces;
using DbNetTimeCore.Repositories;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Reflection;
using System.Text;
using DbNetTimeCore.Pages;
using DbNetTimeCore.Models;
using System.Data;
using DbNetTimeCore.Helpers;

namespace DbNetTimeCore.Services
{
    public class DbNetTimeService : IDbNetTimeService
    {
        private readonly IDbNetTimeRepository _dbNetTimeRepository;
        private readonly ITimestreamRepository _timestreamRepository;
        private readonly RazorViewToStringRenderer _razorRendererService;
        private HttpContext? _context = null;
        private string Handler => RequestHelper.QueryValue("handler", string.Empty, _context);
        private bool isAjaxCall => _context.Request.Headers["hx-request"] == "true";

        public DbNetTimeService(IDbNetTimeRepository dbNetTimeRepository, RazorViewToStringRenderer razorRendererService, ITimestreamRepository timestreamRepository)
        {
            _dbNetTimeRepository = dbNetTimeRepository;
            _razorRendererService = razorRendererService;
            _timestreamRepository = timestreamRepository;
        }

        public async Task<Byte[]> Process(HttpContext context, string page)
        {
            _context = context;
            switch (page.ToLower())
            {
                case "index":
                    return await IndexPage();
                case "timestream":
                    return await TimestreamPage();
                case "customers":
                    return await CustomersView();
                case "films":
                    return await FilmsView();
                case "actors":
                    return await ActorsView();
                case "users":
                    return await UsersPage();
                case "mt1_in_reg":
                case "mt2_in_raw":
                    return await TimestreamView("Kensa-Development", page);
            }

            return GetResource(page);
        }

        private async Task<Byte[]> IndexPage()
        {
            var model = new IndexModel();

            model.CustomersGrid = await CustomerGridViewModel(new GridModel() { Columns = ColumnInfoHelper.CustomerGridColumns().Cast<ColumnModel>().ToList() });
            model.FilmsGrid = await FilmsGridViewModel(new GridModel() { Columns = ColumnInfoHelper.FilmGridColumns().Cast<ColumnModel>().ToList() });
            model.ActorsGrid = await ActorsGridViewModel(new GridModel() { Columns = ColumnInfoHelper.ActorGridColumns().Cast<ColumnModel>().ToList() });

            return await Page("index", model);
        }

        private async Task<Byte[]> TimestreamPage()
        {
            var model = new TimestreamModel();

            model.MT2Grid = await TimestreamGridViewModel("Kensa-Development", "MT2_IN_RAW");
            model.MT1Grid = await TimestreamGridViewModel("Kensa-Development", "MT1_IN_REG");

            return await Page("timestream", model);
        }

        private async Task<Byte[]> CustomersView()
        {
            switch (Handler)
            {
                case "edit":
                case "save":
                    return await CustomersFormView();
                default:
                    return await CustomersGridView();
            }
        }

        private async Task<Byte[]> CustomersGridView()
        {
            var gridModel = GetGridModel();
            gridModel.Columns = ColumnInfoHelper.CustomerGridColumns().Cast<ColumnModel>().ToList();
            return await View("_gridMarkup", await CustomerGridViewModel(gridModel));
        }

        private async Task<Byte[]> CustomersFormView()
        {
            var formModel = GetFormModel();
            formModel.Columns = ColumnInfoHelper.CustomerEditColumns().Cast<ColumnModel>().ToList();
            if (Handler == "save")
            {
                if (ValidateCustomerEditForm(formModel))
                {
                    await _dbNetTimeRepository.SaveCustomer(formModel);
                }
                else
                {
                    var formViewModel = await CustomerFormViewModel(formModel);
                     return await View("_formMarkup", formViewModel);
                }
            }

            return await View("_formMarkup", await CustomerFormViewModel(formModel));
        }

        private async Task<Byte[]> FilmsView()
        {
            switch (Handler)
            {
                case "edit":
                case "save":
                    return await FilmsFormView();
                default:
                    return await FilmsGridView();
            }
        }

        private async Task<Byte[]> FilmsGridView()
        {
            var gridModel = GetGridModel();
            gridModel.Columns = ColumnInfoHelper.FilmGridColumns().Cast<ColumnModel>().ToList();
            return await View("_gridMarkup", await FilmsGridViewModel(gridModel));
        }

        private async Task<Byte[]> FilmsFormView()
        {
            var formModel = GetFormModel();
            formModel.Columns = ColumnInfoHelper.FilmEditColumns().Cast<ColumnModel>().ToList();
            if (Handler == "save")
            {
                if (ValidateFilmEditForm(formModel))
                {
                    await _dbNetTimeRepository.SaveFilm(formModel);
                }
                else
                {
                    var formViewModel = await FilmFormViewModel(formModel);
                     return await View("_formMarkup", formViewModel);
                }
            }
            return await View("_formMarkup", await FilmFormViewModel(formModel));
        }

        private async Task<Byte[]> ActorsView()
        {
            switch (Handler)
            {
                case "edit":
                case "save":
                    return await ActorsFormView();
                default:
                    return await ActorsGridView();
            }
        }

        private async Task<Byte[]> TimestreamView(string databaseName, string tableName)
        {
            var gridModel = GetGridModel();
            return await View("_gridMarkup", await TimestreamGridViewModel(databaseName, tableName));
        }


        private async Task<Byte[]> ActorsGridView()
        {
            var gridModel = GetGridModel();
            return await View("_gridMarkup", await ActorsGridViewModel(gridModel));
        }

        private async Task<Byte[]> ActorsFormView()
        {
            var formModel = GetFormModel();
            formModel.Columns = ColumnInfoHelper.ActorEditColumns().Cast<ColumnModel>().ToList();

            if (Handler == "save")
            {
                if (ValidateActorEditForm(formModel))
                {
                    await _dbNetTimeRepository.SaveActor(formModel);
                }
                else
                {
                    var formViewModel = await ActorFormViewModel(formModel);
                     return await View("_formMarkup", formViewModel);
                }
            }
            return await View("_formMarkup", await ActorFormViewModel(formModel));
        }

        private async Task<Byte[]> UsersPage()
        {
            return await Page("users", new UsersModel());
        }

        private async Task<Byte[]> Page(string pageName, PageModel pageModel)
        {
            return Encoding.UTF8.GetBytes(await _razorRendererService.RenderPageToStringAsync<PageModel>($"Pages/{pageName}.cshtml", pageModel));
        }

        private async Task<Byte[]> View<TModel>(string viewName, TModel model)
        {
            return Encoding.UTF8.GetBytes(await _razorRendererService.RenderViewToStringAsync($"Pages/Shared/{viewName}.cshtml", model));
        }

        private async Task<GridViewModel> CustomerGridViewModel(GridModel? gridModel = null)
        {
            gridModel = gridModel ?? new GridModel();
            DataTable customers = await _dbNetTimeRepository.GetCustomers(gridModel);
            return new GridViewModel(customers, "customers", gridModel);
        }

        private async Task<FormViewModel> CustomerFormViewModel(FormModel? formModel = null)
        {
            formModel = formModel ?? new FormModel();
            DataTable customers = await _dbNetTimeRepository.GetCustomer(formModel);
            return new FormViewModel(customers, "customers", formModel);
        }

        private async Task<GridViewModel> FilmsGridViewModel(GridModel? gridModel = null)
        {
            gridModel = gridModel ?? new GridModel();
            DataTable films = await _dbNetTimeRepository.GetFilms(gridModel);
            return new GridViewModel(films, "films", gridModel);
        }

        private async Task<FormViewModel> FilmFormViewModel(FormModel? gridModel = null)
        {
            gridModel = gridModel ?? new FormModel();
            DataTable films = await _dbNetTimeRepository.GetFilm(gridModel);
            return new FormViewModel(films, "films", gridModel);
        }

        private async Task<GridViewModel> ActorsGridViewModel(GridModel? gridModel = null)
        {
            gridModel = gridModel ?? new GridModel();
            gridModel.Columns = ColumnInfoHelper.ActorGridColumns().Cast<ColumnModel>().ToList();
            DataTable actors = await _dbNetTimeRepository.GetActors(gridModel);
            return new GridViewModel(actors, "actors", gridModel);
        }

        private async Task<GridViewModel> TimestreamGridViewModel(string databaseName, string tableName)
        {
            GridModel gridModel = GetGridModel() ?? new GridModel();
            var columns = await _timestreamRepository.GetColumns(databaseName, tableName);
            gridModel.Columns = columns.Columns.Cast<DataColumn>().Select(c => new GridColumnModel(c.ColumnName) { DataType = c.DataType, Searchable = c.DataType == typeof(string) }).Cast<ColumnModel>().ToList();
            DataTable data = await _timestreamRepository.GetRecords(databaseName, tableName, gridModel);
            return new GridViewModel(data, tableName, gridModel);
        }

        private async Task<FormViewModel> ActorFormViewModel(FormModel? formModel = null)
        {
            formModel = formModel ?? new FormModel();
            DataTable actors = await _dbNetTimeRepository.GetActor(formModel);
            return new FormViewModel(actors, "actors", formModel);
        }
        private GridModel? GetGridModel()
        {
            GridModel gridModel = new GridModel();
            try
            {
                gridModel.CurrentPage = Convert.ToInt32(RequestHelper.QueryValue("page", "1", _context));
                gridModel.SearchInput = RequestHelper.FormValue("searchInput", string.Empty, _context);
                gridModel.SortKey = RequestHelper.FormValue("sortKey", string.Empty, _context);
                gridModel.CurrentSortKey = RequestHelper.FormValue("currentSortKey", string.Empty, _context);
                gridModel.CurrentSortAscending = Convert.ToBoolean(RequestHelper.FormValue("currentSortAscending", "false", _context));
                gridModel.PrimaryKey = RequestHelper.QueryValue("pk", string.Empty, _context);
            }
            catch
            {
                return null;
            }

            return gridModel;
        }

        private FormModel GetFormModel()
        {
            FormModel formModel = new FormModel();
            try
            {
                formModel.PrimaryKey = RequestHelper.QueryValue("pk", string.Empty, _context);
                formModel.ColSpan = Convert.ToInt32(RequestHelper.FormValue("colSpan", "0", _context));
            }
            catch
            {
            }

            return formModel;
        }

        private Byte[] GetResource(string resource)
        {
            string folder = resource.Split(".").Last().ToUpper();
            var assembly = Assembly.GetExecutingAssembly();

            var resourceName = $"DbNetTimeCore.Resources.{folder}.{resource}";

            byte[] bytes = new byte[0];

            using (Stream stream = assembly.GetManifestResourceStream(resourceName) ?? new MemoryStream())
            using (var binaryReader = new BinaryReader(stream))
            {
                bytes = binaryReader.ReadBytes((int)stream.Length);
            }

            return bytes;
        }

        private bool ValidateFilmEditForm(FormModel formModel)
        {
            return StandardFormValidation(formModel);
        }

        private bool ValidateCustomerEditForm(FormModel formModel)
        {
            return StandardFormValidation(formModel);
        }

        private bool ValidateActorEditForm(FormModel formModel)
        {
            return StandardFormValidation(formModel);
        }

        private bool StandardFormValidation(FormModel formModel)
        {
            var formValues = (FormCollection)_context.Request.Form;

            formModel.SavedFormValues = new Dictionary<string,object>(formModel.FormValues((FormCollection)formValues));

            foreach (var column in formModel.EditColumns)
            {
                if (column.Required && string.IsNullOrEmpty(formValues[column.Name]))
                {
                    formModel.Message = "Highlighted column is required";
                    column.Invalid = true;
                    break;
                }
            }

            if (formModel.EditColumns.Any(c => c.Invalid))
            {
                return false;
            }

            foreach (var column in formModel.EditColumns)
            {
                var value = formValues[column.Name].ToString();
                if (string.IsNullOrEmpty(value) || column.DataType == typeof(bool))
                {
                    continue;
                }

                try
                {
                    var convertedValue = Convert.ChangeType(value, column.DataType);
                }
                catch
                {
                    formModel.Message = "Highlighted column is not in correct format";
                    column.Invalid = true;
                    break;
                }
            }

            var inValid = formModel.EditColumns.Any(c => c.Invalid);
            return inValid == false;
        }
    }
}