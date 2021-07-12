using Infraestructura;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Application1
{
    public class Worker : BackgroundService
    {
        internal readonly ILogger<Worker> _logger;
        internal readonly UDPSocketFactory socketFactory;
        internal SocketCustom socket;
        internal const string CONST_FILE_OUT = "fileOut.txt";
        internal const string CONST_FILE_IN = "fileIn.txt";
        internal string afterMessage = "";
        public Worker(IServiceProvider pProveedorServicios)
        {
            _logger = pProveedorServicios.GetService<ILogger<Worker>>();
            socketFactory = pProveedorServicios.GetService<UDPSocketFactory>();
            socket = socketFactory.ConfigureSocket();

            socket.DataReceived += Socket_DataReceived;
            if (!File.Exists(CONST_FILE_OUT))
            {
                File.Create(CONST_FILE_OUT);
            }
        }

        private void Socket_DataReceived(byte[] data, AddressFamily addressFamily)
        {
            try
            {
                string messageData = SocketCustom.Create(data, "Data received", addressFamily);
                File.WriteAllText(CONST_FILE_OUT, messageData);
                _logger.Log(LogLevel.Warning, "{0}", messageData);
                afterMessage = messageData;
                //Console.Clear();

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            byte[] data;
            if (!File.Exists(CONST_FILE_IN))
            {
                File.Create(CONST_FILE_IN);
            }
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {

                    data = File.ReadAllBytes(CONST_FILE_IN);
                    if (data.Length > 0)
                    {
                        socket.Send(data);
                    }
                    if (!string.IsNullOrEmpty(afterMessage))
                    {
                        _logger.Log(LogLevel.Warning, "{0}", afterMessage);
                        afterMessage = "";
                    }
                }
                catch (Exception ex)
                {
                    _logger.Log(LogLevel.None, "Error en el socket: {0}", ex.Message);
                }
                _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);

                await Task.Delay(5000, stoppingToken);
                Console.Clear();


            }
        }
    }
}
