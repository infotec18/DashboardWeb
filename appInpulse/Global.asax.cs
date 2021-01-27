﻿using appBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;

namespace appInpulse
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
           
            Scripts.DefaultTagFormat = @"<script src=""{0}"" defer></script>";

            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);            

            System.Web.WebPages.WebPageHttpHandler.RegisterExtension("html");
            var languages = System.Web.Razor.RazorCodeLanguage.Languages;
            languages.Add("html", languages["cshtml"]);
        }
    }
}
