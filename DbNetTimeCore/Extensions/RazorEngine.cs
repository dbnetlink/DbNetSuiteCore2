using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Mvc.Controllers;
using System.Collections;

namespace DbNetTimeCore.Extensions
{
    public class RazorEngine
    {
        private IRazorViewEngine viewEngine;
        private IOptions<MvcViewOptions> viewOptions;

        public RazorEngine(IRazorViewEngine viewEngine, IOptions<MvcViewOptions> viewOptions)
        {
            this.viewEngine = viewEngine;
            this.viewOptions = viewOptions;
        }

        public async Task RenderAsync(HttpContext httpContext, string viewName, object model, Stream s)
        {
            var view = viewEngine.GetView(null, viewName, false);
            var writer = new StreamWriter(s);
            var viewContext = CreateViewContext(httpContext, view.View as RazorView, writer);
            viewContext.ViewData.Model = model;
            await view.View.RenderAsync(viewContext);
            writer.Flush();
        }

        public async Task<string> RenderAsync(HttpContext httpContext, string viewName, object model)
        {
            var ms = new MemoryStream();
            await this.RenderAsync(httpContext, viewName, model, ms);
            ms.Seek(offset: 0, loc: SeekOrigin.Begin);
            using (var reader = new StreamReader(ms))
            {
                var content = reader.ReadToEnd();
                return content;
            }
        }

        private ViewContext CreateViewContext(HttpContext httpContext, RazorView view, StreamWriter writer)
        {
            var actionContext = new ActionContext(httpContext, new RouteData(), new ControllerActionDescriptor());
            var controllerContext = new ControllerContext(actionContext);
            var viewContext = new ViewContext(
              actionContext: controllerContext,
              view: view,
              viewData: new ViewDataDictionary(new EmptyModelMetadataProvider(), controllerContext.ModelState),
              tempData: new FakeTempDataDictionary(),
              writer: writer,
              htmlHelperOptions: viewOptions.Value.HtmlHelperOptions);
            return viewContext;
        }

        class FakeTempDataDictionary : ITempDataDictionary
        {
            public object this[string key]
            {
                get
                {
                    throw new NotImplementedException();
                }

                set
                {
                    throw new NotImplementedException();
                }
            }

            public int Count
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public ICollection<string> Keys
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public ICollection<object> Values
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public void Add(KeyValuePair<string, object> item)
            {
                throw new NotImplementedException();
            }

            public void Add(string key, object value)
            {
                throw new NotImplementedException();
            }

            public void Clear()
            {
                throw new NotImplementedException();
            }

            public bool Contains(KeyValuePair<string, object> item)
            {
                throw new NotImplementedException();
            }

            public bool ContainsKey(string key)
            {
                throw new NotImplementedException();
            }

            public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
            {
                throw new NotImplementedException();
            }

            public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
            {
                throw new NotImplementedException();
            }

            public void Keep()
            {
                throw new NotImplementedException();
            }

            public void Keep(string key)
            {
                throw new NotImplementedException();
            }

            public void Load()
            {
                throw new NotImplementedException();
            }

            public object Peek(string key)
            {
                throw new NotImplementedException();
            }

            public bool Remove(KeyValuePair<string, object> item)
            {
                throw new NotImplementedException();
            }

            public bool Remove(string key)
            {
                throw new NotImplementedException();
            }

            public void Save()
            {
                throw new NotImplementedException();
            }

            public bool TryGetValue(string key, out object value)
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                throw new NotImplementedException();
            }
        }
    }
}