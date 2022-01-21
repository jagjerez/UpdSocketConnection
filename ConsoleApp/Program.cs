using ConsoleApp.Screens;
using ConsoleApp.Screens.Interfaces;
using Infraestructura;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Configuration;
using System.Threading;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            
            IServiceCollection serviceDescriptors = new ServiceCollection();


            serviceDescriptors.AddSingleton<IConfiguration>(sp =>
            {
                var builder = new ConfigurationBuilder()
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

                return builder.Build();
            });
            serviceDescriptors.AddTransient<UDPSocketFactory>((sp) => UDPSocketFactory.CrearInstance(sp));
            serviceDescriptors.AddTransient<ScreenFactory>();
            serviceDescriptors.AddTransient<MenuInit>();
            serviceDescriptors.AddTransient<WriteMessageForSend>();
            serviceDescriptors.AddLogging(sp => sp.AddConsole());
            //serviceDescriptors.ConfigureOptions()
            //ServiceProviderOptions serviceProviderOptions = new ServiceProviderOptions();
            
            IServiceProvider serviceProvider = serviceDescriptors.BuildServiceProvider();
            
            ILogger<Program> logger = serviceProvider.GetService<ILogger<Program>>();
            ScreenFactory udpSocket = serviceProvider.GetService<ScreenFactory>();
            CancellationTokenSource cancelSource = new CancellationTokenSource();
            CancellationToken cancellationToken = cancelSource.Token;
            IScreen screenActive = null;
            screenActive = udpSocket.CreateScreenInstance<MenuInit>();
            
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        string menssage = "";
                        if (screenActive == null)
                        {
                            cancelSource.Cancel();
                            continue;
                        }
                        do
                        {
                            Console.Write(screenActive.DrawScreen());
                            menssage = Console.ReadLine();
                        } while (string.IsNullOrEmpty(menssage));

                        screenActive = screenActive.ExcecuteOptionMenu(menssage, cancelSource);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.Message);
                        //throw;
                    }


                }
            }
            catch (OperationCanceledException ex)
            {
                logger.LogError(ex.Message);
            }
        }
    }
}
