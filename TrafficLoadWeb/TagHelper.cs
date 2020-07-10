using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.Linq;

namespace TrafficLoadWeb
{
    public static class HtmlHelperExtensions
    {
        public static string ActiveClass(this IHtmlHelper htmlHelper, string route, string inject = "active")
        {
            var routeData = htmlHelper.ViewContext.RouteData;
            var pageRoute = routeData.Values["page"].ToString();
            return route == pageRoute ? inject : "";
        }
    }

    public class CustomRouteModelConvention : IPageRouteModelConvention
    {
        public void Apply(PageRouteModel model)
        {
            List<SelectorModel> selectorModels = new List<SelectorModel>();
            foreach (var selector in model.Selectors.ToList())
            {
                var template = selector.AttributeRouteModel.Template;
                selectorModels.Add(new SelectorModel()
                {
                    AttributeRouteModel = new AttributeRouteModel
                    {
                        Template = "/{status?}" + "/" + template
                    }
                });
            }
            foreach (var m in selectorModels)
            {
                model.Selectors.Add(m);
            }
        }
    }
}
