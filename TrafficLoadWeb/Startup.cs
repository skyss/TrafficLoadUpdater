using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.EntityFrameworkCore;
using TrafficLoadWeb.Data;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Identity;
using TrafficLoadWeb.Models;

namespace TrafficLoadWeb
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;


        }

        public static readonly ILoggerFactory factory
            = LoggerFactory.Create(builder => { builder.AddConsole(); });

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddRazorPages().AddRazorRuntimeCompilation().AddRazorPagesOptions(opts =>
            {
                opts.Conventions.Add(new CustomRouteModelConvention());
            });                

            services.AddDbContext<TrafficLoadContext>(
                options => options.UseSqlServer(Configuration.GetConnectionString("SQLConnectionString")).UseLoggerFactory(factory));

            services.AddHttpContextAccessor();

            services.AddScoped<ITurModelHelper, TurModelHelper>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }
    }
}
