using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LEDControl.Database;
using LEDControl.Hubs;
using LEDControl.Services;
using LEDControl.Services.Mqtt;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;

namespace LEDControl
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

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
                options.UseMySql("server=192.168.178.8;user=led;password=Menschen7;database=LEDControl", 
                    new MariaDbServerVersion(new Version(10, 5, 12))));

            services.AddMqttService(options =>
            {
                options.Port = 12000;
            });
            
            services.AddSingleton<ProgramService>();
            services.AddSingleton<SettingsService>();
            services.AddSingleton<ConvertService>();
            services.AddHostedService(provider => provider.GetService<ConvertService>());
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, MqttService mqttService)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "LEDControl v1"));
            }

            //app.UseHttpsRedirection();

            app.UseCors(options =>
            {
                options.WithOrigins("http://localhost:4200")
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
            });

            app.UseRouting();

            app.UseAuthorization();

            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHub<LightHub>("hubs/light");
                endpoints.MapControllers();
            });
        }
    }
}