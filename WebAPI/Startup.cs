using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.OData;
using Microsoft.AspNetCore.OData.Batch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace WebApplication1
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
            services.AddLogging(cfg =>
            {
                cfg.AddConsole();
                cfg.AddDebug();
            });

            services.AddAuthentication(cfg =>
                    {
                        cfg.RequireAuthenticatedSignIn = true;
                        cfg.DefaultScheme = CertificateAuthenticationDefaults.AuthenticationScheme;
                    })
                    .AddCertificate(ao =>
                    {
                        ao.AllowedCertificateTypes = CertificateTypes.All;
                        ao.Events = new CertificateAuthenticationEvents
                        {
                            OnAuthenticationFailed = context =>
                            {
                                Console.WriteLine("Authentication failed !");
                                System.Diagnostics.Debug.WriteLine("Authentication failed !");
                                return Task.CompletedTask;
                            },
                            OnCertificateValidated = context =>
                            {
                                if (context.ClientCertificate.Thumbprint.Equals("df74ee9108fd3a258511ed2d287e195ce6a50b0d", StringComparison.OrdinalIgnoreCase))
                                {
                                    Console.WriteLine("Certificate authentication passed !");
                                    System.Diagnostics.Debug.WriteLine("Certificate authentication passed !");
                                    var claims = new[]
                                    {
                                        new Claim(
                                            ClaimTypes.NameIdentifier,
                                            context.ClientCertificate.Subject,
                                            ClaimValueTypes.String,
                                            context.Options.ClaimsIssuer),
                                        new Claim(ClaimTypes.Name,
                                            context.ClientCertificate.Subject,
                                            ClaimValueTypes.String,
                                            context.Options.ClaimsIssuer)
                                    };

                                    context.Principal = new ClaimsPrincipal(
                                        new ClaimsIdentity(claims, context.Scheme.Name));

                                    context.Success();
                                }
                                else
                                {
                                    Console.WriteLine("Certificate authentication failed !");
                                    System.Diagnostics.Debug.WriteLine("Certificate authentication failed !");
                                    context.Fail("invalid cert");
                                }

                                return Task.CompletedTask;
                            }
                        };
                    });
            services.AddAuthorization();

            services.AddControllers()
                    .AddOData(opt =>
            {
                opt.EnableQueryFeatures(null)
                   .AddRouteComponents("odata", GetEdmModel());
            });
        }

        private IEdmModel GetEdmModel()
        {
            var builder = new ODataConventionModelBuilder();
            builder.EntitySet<WeatherForecast>("WeatherForecast");

            return builder.GetEdmModel();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseODataRouteDebug();
            app.UseODataQueryRequest();
            app.UseODataBatching();

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();
            
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
