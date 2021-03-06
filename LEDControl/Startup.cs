using System;
using LEDControl.Database;
using LEDControl.Hubs;
using LEDControl.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;

namespace LEDControl
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        private IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "LEDControl", Version = "v1" });
            });

            services.AddSignalR();

            services.AddDbContext<DataContext>(options => 
                options.UseMySql(Configuration["ConnectionString"], 
                    new MariaDbServerVersion(new Version(10, 5, 12))));

            services.AddSingleton<DeviceService>();
            services.AddSingleton<ProgramService>();
            services.AddSingleton<SettingsService>();
            services.AddHostedService<ConvertService>();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, DeviceService _)
        {
            var baseUrl = Configuration["BaseUrl"];
            if (!string.IsNullOrEmpty(baseUrl))
            {
                baseUrl = baseUrl.Trim('/');
                app.UsePathBase("/" + baseUrl);
            }
            
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LEDControl v1"));
            }

            app.UseCors(p => p
                .WithOrigins("http://localhost:4200")
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials()
            );

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<LightHub>("hubs/light");
                endpoints.MapHub<ConvertHub>("hubs/convert");
                endpoints.MapHub<MusicHub>("hubs/music");
                endpoints.MapControllers();
            });
        }
    }
}