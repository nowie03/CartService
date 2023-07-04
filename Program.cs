using CartService.BackgroundServices;
using CartService.Context;
using CartService.MessageBroker;
using Microsoft.EntityFrameworkCore;

namespace CartService
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddDbContext<ServiceContext>(options => options.UseSqlServer(
                builder.Configuration.GetConnectionString("local-server")));

            builder.Services.AddScoped<IMessageSender, MessageSender>();
            builder.Services.AddScoped<IMessageReceiver, MessageReceiver>();

            builder.Services.AddSingleton<MessageProcessingService>();
            builder.Services.AddHostedService<MessageProcessingService>(
                provider => provider.GetRequiredService<MessageProcessingService>());

            builder.Services.AddSingleton<PublishMessageToQueue>();
            builder.Services.AddHostedService<PublishMessageToQueue>(
                provider => provider.GetRequiredService<PublishMessageToQueue>());

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseSwagger();
            app.UseSwaggerUI();


            app.UseHttpsRedirection();

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }
}