using Dashboard.Net.AI.Extensions;

namespace Dashboard.Net.AI
{
    public class Program
    {
        public static void Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            _ = builder.Services.AddAuthorization();
            _ = builder.Services.AddControllers();
            _ = builder.Services.AddEndpointsApiExplorer();
            _ = builder.Services.AddSwaggerGen();
            _ = builder.Services.AddApplicationServices();

            _ = builder.Configuration.AddUserSecrets<Program>();

            WebApplication app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                _ = app.UseHttpsRedirection();
            }

            _ = app.UseAuthentication();
            _ = app.UseAuthorization();

            if (app.Environment.IsDevelopment())
            {
                _ = app.UseSwagger();
                _ = app.UseSwaggerUI();
            }

            _ = app.UseHttpsRedirection();
            _ = app.UseAuthorization();
            _ = app.MapControllers();

            app.Run();
        }
    }
}
