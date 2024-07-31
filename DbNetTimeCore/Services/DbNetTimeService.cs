using DbNetTimeCore.Services.Interfaces;
using DbNetTimeCore.Repositories;
using System.Reflection;
using System.Text;
using DbNetTimeCore.Models;
using System.Data;
using DbNetTimeCore.Helpers;
using DbNetTimeCore.Enums;
using System.Text.Json;

namespace DbNetTimeCore.Services
{
    public class DbNetTimeService : IDbNetTimeService
    {
        private readonly IMSSQLRepository _msSqlRepository;
        private readonly ITimestreamRepository _timestreamRepository;
        private readonly ISQLiteRepository _sqliteRepository;
        private readonly RazorViewToStringRenderer _razorRendererService;
        private readonly IJSONRepository _jsonRepository;

        private HttpContext? _context = null;
        private string Handler => RequestHelper.QueryValue("handler", string.Empty, _context);
        private bool isAjaxCall => _context.Request.Headers["hx-request"] == "true";

        private DataSourceType dataSourceType => Enum.Parse<DataSourceType>(RequestHelper.FormValue("dataSourceType", string.Empty, _context));

        public DbNetTimeService(IMSSQLRepository msSqlRepository, RazorViewToStringRenderer razorRendererService, ITimestreamRepository timestreamRepository, ISQLiteRepository sqliteRepository, IJSONRepository jsonRepository)
        {
            _msSqlRepository = msSqlRepository;
            _razorRendererService = razorRendererService;
            _timestreamRepository = timestreamRepository;
            _sqliteRepository = sqliteRepository;
            _jsonRepository = jsonRepository;
        }

        public async Task<Byte[]> Process(HttpContext context, string page)
        {
            _context = context;
            switch (page.ToLower())
            {
                case "css":
                case "js":
                    return GetResources(page);
                case "gridcontrol":
                    GridModel gridModel = GetGridModel() ?? new GridModel();
                    return await GridView(gridModel);
            }

            return new byte[0];
        }

        private async Task<Byte[]> GridView(GridModel gridModel)
        {
            return await View("_gridMarkup", await GetGridViewModel(gridModel));
        }

        private async Task<Byte[]> View<TModel>(string viewName, TModel model)
        {
            return Encoding.UTF8.GetBytes(await _razorRendererService.RenderViewToStringAsync($"Views/{viewName}.cshtml", model));
        }

        private async Task<GridViewModel> GetGridViewModel(GridModel gridModel)
        {
            DataTable columns;
            DataTable data;

            switch (gridModel.DataSourceType)
            {
                case DataSourceType.Timestream:
                    columns = await _timestreamRepository.GetColumns(gridModel);
                    data = await _timestreamRepository.GetRecords(gridModel);
                    break;
                case DataSourceType.SQlite:
                    columns = await _sqliteRepository.GetColumns(gridModel);
                    data = await _sqliteRepository.GetRecords(gridModel);
                    break;
                case DataSourceType.JSON:
                    columns = await _jsonRepository.GetColumns(gridModel,_context);
                    data = await _jsonRepository.GetRecords(gridModel, _context);
                    break;
                default:
                    columns = await _msSqlRepository.GetColumns(gridModel);
                    data = await _msSqlRepository.GetRecords(gridModel);
                    break;
            }

            var unInitialisedColumns = new List<GridColumnModel>(gridModel.Columns.Where(c => c.Initialised == false));

            if (gridModel.Columns.Any() == false || unInitialisedColumns.Any())
            {
                if (gridModel.Columns.Any() == false)
                {
                    gridModel.Columns = columns.Columns.Cast<DataColumn>().Select(c => new GridColumnModel(c)).Cast<GridColumnModel>().ToList();
                }
                else
                {
                    var dataColumns = columns.Columns.Cast<DataColumn>().ToList();
                    for (var i = 0; i < dataColumns.Count; i++)
                    {
                        gridModel.Columns[i].Update(dataColumns[i]);
                    }
                }
                for (var i = 0; i < gridModel.Columns.Count; i++)
                {
                    gridModel.Columns[i].Ordinal = i + 1;
                }
            }

            return new GridViewModel(data, gridModel);
        }

        private GridModel? GetGridModel()
        {
            GridModel gridModel = JsonSerializer.Deserialize<GridModel>(RequestHelper.FormValue("model", string.Empty, _context)) ?? new GridModel();
            try
            {
                gridModel.CurrentPage = Convert.ToInt32(RequestHelper.QueryValue("page", "1", _context));
                gridModel.SearchInput = RequestHelper.FormValue("searchInput", string.Empty, _context);
                gridModel.SortKey = RequestHelper.FormValue("sortKey", string.Empty, _context);
                gridModel.CurrentSortKey = RequestHelper.FormValue("currentSortKey", string.Empty, _context);
                gridModel.CurrentSortAscending = Convert.ToBoolean(RequestHelper.FormValue("currentSortAscending", "false", _context));
            }
            catch
            {
                return null;
            }

            return gridModel;
        }

        private Byte[] GetResources(string type)
        {
            var resources = new string[] { };
            switch(type)
            {
                case "css":
                    resources = ["daisyui","gridcontrol"];
                    break;
                case "js":
                    resources = ["tailwindcss", "htmx.min", "surreal", "gridcontrol"];
                    break;
            }

            return GetResource(type, resources);
        }

        private Byte[] GetResource(string type, string[] resources)
        {
            byte[] bytes = new byte[0];
            var assembly = Assembly.GetExecutingAssembly();

            foreach (string resource in resources)
            {
                var resourceName = $"{assembly.FullName!.Split(",").First()}.Resources.{type.ToUpper()}.{resource}.{type}";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName) ?? new MemoryStream())
                using (var binaryReader = new BinaryReader(stream))
                {
                    bytes = CombineByteArrays(bytes,binaryReader.ReadBytes((int)stream.Length));
                    bytes = CombineByteArrays(bytes, Encoding.ASCII.GetBytes(Environment.NewLine));
                }
            }
            return bytes;
        }

        public static byte[] CombineByteArrays(byte[] first, byte[] second)
        {
            byte[] ret = new byte[first.Length + second.Length];
            Buffer.BlockCopy(first, 0, ret, 0, first.Length);
            Buffer.BlockCopy(second, 0, ret, first.Length, second.Length);
            return ret;
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

            formModel.SavedFormValues = new Dictionary<string, object>(formModel.FormValues((FormCollection)formValues));

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