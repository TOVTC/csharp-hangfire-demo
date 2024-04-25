using Hangfire;
using HangfireBasicAuthenticationFilter;

namespace HangfireDemo
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            // Add Hangfire using AddHangfire method
            builder.Services.AddHangfire((sp, config) =>
            {
                // Specify and use the database connection string for Hangfire
                var connectionString = sp.GetRequiredService<IConfiguration>().GetConnectionString("DbConnection");
                config.UseSqlServerStorage(connectionString);
                // Before starting the application, manually create the HangfireDemo.Dev database using SSMS
                // Hangfire will automatically create the necessary tables  within the database
            });
            // Ensure the Hangfire server is added to dependency services
            builder.Services.AddHangfireServer();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            // The Hangfire dashboard allows us to view the details of our jobs
            // Access using /hangfire as the default location
            // You can view the jobs being executed in real time in the graph on the home page, view jobs by their statuses, and view additional details about recurring jobs, such as its cron statement
            // You can also customize the Hangfire dashboard by passing additional parameters (e.g. a custom path, setting a custom title, disable dark mode, hide the Database name in the footer, enabling authentication, etc.)
            app.UseHangfireDashboard("/test/job-dashboard", new DashboardOptions
            {
                DashboardTitle = "Hangfire Job Demo Application",
                DarkModeEnabled = false,
                DisplayStorageConnectionString = false,
                // To enable authorization, install the Hangfire.Dashboard.Basic.Authentication library
                // This allows you to specify a username and password for your dashboard
                Authorization = new[]
                {
                    new HangfireCustomBasicAuthenticationFilter
                    {
                        User = "admin",
                        Pass = "admin123"
                    }
                }
            });

            app.Run();
        }
    }
}
