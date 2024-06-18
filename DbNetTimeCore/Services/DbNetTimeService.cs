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

            model.CustomersGrid = await CustomersDataGrid();
            model.FilmsGrid = await FilmsDataGrid();
            model.ActorsGrid = await ActorsDataGrid();

            return await Page("index", model);
        }
   
        private async Task<Byte[]> CustomersPage()
        {
            var gridParameters = GetGridParameters();
            switch (gridParameters.Handler)
            {
                case "edit":
                    return await View("_formMarkup", await CustomerEditForm(gridParameters));
                case "save":
                    await _dbNetTimeRepository.SaveCustomer(gridParameters);
                    return await View("_formMarkup", await CustomerEditForm(gridParameters));
                default:
                    return await View("_gridMarkup", await CustomersDataGrid(gridParameters));
            }
        }

        private async Task<Byte[]> FilmsPage()
        {
            var gridParameters = GetGridParameters();
            switch (gridParameters.Handler)
            {
                case "edit":
                    return await View("_formMarkup", await FilmEditForm(gridParameters));
                case "save":
                    await _dbNetTimeRepository.SaveFilm(gridParameters);
                    return await View("_formMarkup", await FilmEditForm(gridParameters));
                default:
                    return await View("_gridMarkup", await FilmsDataGrid(gridParameters));
            }
        }

        private async Task<Byte[]> ActorsPage()
        {
            var gridParameters = GetGridParameters();
            switch (gridParameters.Handler)
            {
                case "edit":
                    return await View("_formMarkup", await ActorEditForm(gridParameters));
                case "save":
                    await _dbNetTimeRepository.SaveActor(gridParameters);
                    return await View("_formMarkup", await ActorEditForm(gridParameters));
                default:
                    return await View("_gridMarkup", await ActorsDataGrid(gridParameters));
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

        private async Task<DataGrid> CustomersDataGrid(GridParameters? gridParameters = null)
        {
            gridParameters = gridParameters ?? new GridParameters();
            DataTable customers = await _dbNetTimeRepository.GetCustomers(gridParameters);
            return new DataGrid(customers, "customers", gridParameters);
        }

        private async Task<DataGrid> CustomerEditForm(GridParameters? gridParameters = null)
        {
            gridParameters = gridParameters ?? new GridParameters();
            DataTable customers = await _dbNetTimeRepository.GetCustomer(gridParameters);
            return new DataGrid(customers, "customers", gridParameters);
        }
       

        private async Task<DataGrid> FilmsDataGrid(GridParameters? gridParameters = null)
        {
            gridParameters = gridParameters ?? new GridParameters();
            DataTable films = await _dbNetTimeRepository.GetFilms(gridParameters);
            return new DataGrid(films, "films", gridParameters);
        }

        private async Task<DataGrid> FilmEditForm(GridParameters? gridParameters = null)
        {
            gridParameters = gridParameters ?? new GridParameters();
            DataTable films = await _dbNetTimeRepository.GetFilm(gridParameters);
            return new DataGrid(films, "films", gridParameters);
        }

        private async Task<DataGrid> ActorsDataGrid(GridParameters? gridParameters = null)
        {
            gridParameters = gridParameters ?? new GridParameters();
            DataTable actors =await _dbNetTimeRepository.GetActors(gridParameters);
            return new DataGrid(actors, "actors", gridParameters);
        }

        private async Task<DataGrid> ActorEditForm(GridParameters? gridParameters = null)
        {
            gridParameters = gridParameters ?? new GridParameters();
            DataTable actors = await _dbNetTimeRepository.GetActor(gridParameters);
            return new DataGrid(actors, "actors", gridParameters);
        }
        private GridParameters GetGridParameters()
        {
            GridParameters gridParameters = new GridParameters();
            try
            {
                gridParameters.CurrentPage = Convert.ToInt32(RequestHelper.QueryValue("page","1", _context));
                gridParameters.SearchInput = RequestHelper.FormValue("searchInput", string.Empty, _context);
                gridParameters.SortKey = RequestHelper.FormValue("sortKey", string.Empty, _context);
                gridParameters.CurrentSortKey = RequestHelper.FormValue("currentSortKey", string.Empty, _context);
                gridParameters.CurrentSortAscending = Convert.ToBoolean(RequestHelper.FormValue("currentSortAscending", "false", _context));
                gridParameters.Handler = RequestHelper.QueryValue("handler", string.Empty, _context);
                gridParameters.PrimaryKey = RequestHelper.QueryValue("pk", string.Empty, _context);
                gridParameters.ColSpan = Convert.ToInt32(RequestHelper.FormValue("colSpan", "0", _context));
            }
            catch
            {
            }

            return gridParameters;
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