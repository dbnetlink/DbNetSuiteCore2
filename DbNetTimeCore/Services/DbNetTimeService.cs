using DbNetTimeCore.Services.Interfaces;
using DbNetTimeCore.Repositories;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Reflection;
using System.Text;
using DbNetTimeCore.Pages;
using DbNetTimeCore.Models;
using System.Data;

namespace DbNetTimeCore.Services
{
    public class DbNetTimeService : IDbNetTimeService
    {   
        private readonly IDbNetTimeRepository _dbNetTimeRepository;
        private readonly RazorViewToStringRenderer _razorRendererService;
        private HttpContext? _context = null;
        private bool isAjaxCall => _context.Request.Headers["hx-request"] == "true";

        public DbNetTimeService(IDbNetTimeRepository dbNetTimeRepository, RazorViewToStringRenderer razorRendererService)  
        {
            _dbNetTimeRepository = dbNetTimeRepository;
            _razorRendererService = razorRendererService;
        }

        public async Task<Byte[]> Process(HttpContext context, string page)
        {
            _context = context;
            switch (page.ToLower())
            {
                case "index":
                    return await IndexPage();
                case "customers":
                    return await CustomersPage();
                case "films":
                    return await FilmsPage();
                case "actors":
                    return await ActorsPage();
                case "users":
                    return await UsersPage();
            }

            return GetResource(page);
        }

        private async Task<Byte[]> IndexPage()
        {
            var model = new IndexModel();

            model.CustomersGrid = CustomersDataGrid();
            model.FilmsGrid = FilmsDataGrid();
            model.ActorsGrid = ActorsDataGrid();

            return await Page("index", model);
        }
   
        private async Task<Byte[]> CustomersPage()
        {
            var gridParameters = GetGridParameters();
            if (gridParameters.Handler == "edit")
            {
                return await View("_formMarkup", CustomerEditForm(gridParameters));
            }
            else
            {
                return await View("_gridMarkup", CustomersDataGrid(gridParameters));
            }
        }

        private async Task<Byte[]> FilmsPage()
        {
            var gridParameters = GetGridParameters();
            if (gridParameters.Handler == "edit")
            {
                return await View("_formMarkup", FilmEditForm(gridParameters));
            }
            else
            {
                return await View("_gridMarkup", FilmsDataGrid(gridParameters));
            }
        }

        private async Task<Byte[]> ActorsPage()
        {
            var gridParameters = GetGridParameters();
            if (gridParameters.Handler == "edit")
            {
                return await View("_formMarkup", ActorEditForm(gridParameters));
            }
            else
            {
                return await View("_gridMarkup", ActorsDataGrid(gridParameters));
            }
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

        private DataGrid CustomersDataGrid(GridParameters? gridParameters = null)
        {
            gridParameters = gridParameters ?? new GridParameters();
            DataTable customers = _dbNetTimeRepository.GetCustomers(gridParameters);
            return new DataGrid(customers, "customers", gridParameters);
        }

        private DataGrid CustomerEditForm(GridParameters? gridParameters = null)
        {
            gridParameters = gridParameters ?? new GridParameters();
            DataTable customers = _dbNetTimeRepository.GetCustomer(gridParameters);
            return new DataGrid(customers, "customers", gridParameters);
        }
       

        private DataGrid FilmsDataGrid(GridParameters? gridParameters = null)
        {
            gridParameters = gridParameters ?? new GridParameters();
            DataTable films = _dbNetTimeRepository.GetFilms(gridParameters);
            return new DataGrid(films, "films", gridParameters);
        }

        private DataGrid FilmEditForm(GridParameters? gridParameters = null)
        {
            gridParameters = gridParameters ?? new GridParameters();
            DataTable films = _dbNetTimeRepository.GetFilm(gridParameters);
            return new DataGrid(films, "films", gridParameters);
        }

        private DataGrid ActorsDataGrid(GridParameters? gridParameters = null)
        {
            gridParameters = gridParameters ?? new GridParameters();
            DataTable actors = _dbNetTimeRepository.GetActors(gridParameters);
            return new DataGrid(actors, "actors", gridParameters);
        }

        private DataGrid ActorEditForm(GridParameters? gridParameters = null)
        {
            gridParameters = gridParameters ?? new GridParameters();
            DataTable actors = _dbNetTimeRepository.GetActor(gridParameters);
            return new DataGrid(actors, "actors", gridParameters);
        }
        private GridParameters GetGridParameters()
        {
            GridParameters gridParameters = new GridParameters();
            try
            {
                gridParameters.CurrentPage = Convert.ToInt32(QueryValue("page","1"));
                gridParameters.SearchInput = FormValue("searchInput", string.Empty);
                gridParameters.SortKey = FormValue("sortKey", string.Empty);
                gridParameters.CurrentSortKey = FormValue("currentSortKey", string.Empty);
                gridParameters.CurrentSortAscending = Convert.ToBoolean(FormValue("currentSortAscending", "0"));
                gridParameters.Handler = QueryValue("handler", string.Empty);
                gridParameters.PrimaryKey = QueryValue("pk", string.Empty);
                gridParameters.ColSpan = Convert.ToInt32(FormValue("colSPan", "0"));
            }
            catch
            {
            }

            return gridParameters;
        }

        private string QueryValue(string key, string defaultValue)
        {
            return _context.Request.Query.ContainsKey(key) ? _context.Request.Query[key].ToString() : defaultValue;
        }

        private string FormValue(string key, string defaultValue)
        {
            return _context.Request.Form.ContainsKey(key) ? _context.Request.Form[key].ToString() : defaultValue;
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
    }
}