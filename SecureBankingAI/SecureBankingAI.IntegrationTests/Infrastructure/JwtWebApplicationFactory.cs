
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using SecureBankingAI.IntegrationTests.Infrastructure;

namespace SecureBankingAI.IntegrationTests.Api;

public sealed class JwtWebApplicationFactory
    : CustomWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Retain the shared database and test configuration.
        base.ConfigureWebHost(builder);

        // Restore real JWT authentication for these tests.
        builder.ConfigureServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultAuthenticateScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultChallengeScheme =
                    JwtBearerDefaults.AuthenticationScheme;

                options.DefaultForbidScheme =
                    JwtBearerDefaults.AuthenticationScheme;
            });
        });
    }
}
