﻿using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Routing;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TrafficLoadWeb.Models;

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

                selectorModels.Add(new SelectorModel() { AttributeRouteModel = new AttributeRouteModel { Template = "/{status:TrafficLight}/" + template } });
                selectorModels.Add(new SelectorModel() { AttributeRouteModel = new AttributeRouteModel { Template = "/{date:DateTime}/" + template } });
                selectorModels.Add(new SelectorModel() { AttributeRouteModel = new AttributeRouteModel { Template = "/{line}/" + template } });
                selectorModels.Add(new SelectorModel() { AttributeRouteModel = new AttributeRouteModel { Template = "/{status:TrafficLight}/{date:DateTime}/" + template } });
                selectorModels.Add(new SelectorModel() { AttributeRouteModel = new AttributeRouteModel { Template = "/{status:TrafficLight}/{line}/" + template } });
                selectorModels.Add(new SelectorModel() { AttributeRouteModel = new AttributeRouteModel { Template = "/{date:DateTime}/{line}" + template } });
                selectorModels.Add(new SelectorModel() {AttributeRouteModel = new AttributeRouteModel{ Template = "/{status:TrafficLight}/{date:DateTime}/{line}/" + template }});
            }
            foreach (var m in selectorModels)
            {
                model.Selectors.Add(m);
            }
        }
    }

    class TrafficLightStatusConstraint : IRouteConstraint
    {

        public bool Match(HttpContext httpContext, IRouter route, string routeKey,
                          RouteValueDictionary values, RouteDirection routeDirection)
        {
            if (values.TryGetValue(routeKey, out object value))
            {
                var parameterValueString = Convert.ToString(value, CultureInfo.InvariantCulture);
                if (parameterValueString == null)
                {
                    return false;
                }
                Enum.TryParse(parameterValueString, out TrafficLightStatus myStatus);

                return myStatus != 0;
            }

            return false;
        }
    }
}
