using ConsoleApp.Screens.Interfaces;
using Infraestructura;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ConsoleApp.Screens
{
    public class WriteMessageForSend : IScreen
    {
        internal readonly ILogger<WriteMessageForSend> logger;
        internal readonly ScreenFactory screenFactory;
        internal readonly SocketCustom socket;
        public WriteMessageForSend(IServiceProvider pServiceProvider)
        {
            logger = pServiceProvider.GetService<ILogger<WriteMessageForSend>>();
            UDPSocketFactory uDPSocketFactory = pServiceProvider.GetService<UDPSocketFactory>();
            screenFactory = pServiceProvider.GetService<ScreenFactory>();
            socket = uDPSocketFactory.ConfigureSocket();
            socket.DataReceived += UDPSocketFactory_DataReceived;
            
        }

        private void UDPSocketFactory_DataReceived(byte[] data, System.Net.Sockets.AddressFamily addressFamily)
        {
            try
            {
                string messageData = SocketCustom.Create(data, "Data received", addressFamily);

                logger.Log(LogLevel.Warning, "{0}", messageData);
                //afterMessage = messageData;
                //Console.Clear();

            }
            catch (Exception ex)
            {
                logger.LogError(ex.Message);
            }
        }

        public string DrawScreen()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Menu: Envio de mensajes");
            sb.AppendLine("Opciones:");
            sb.AppendLine("b -> regresar al menu de inicio.");
            return sb.ToString();
        }

        public IScreen ExcecuteOptionMenu(string pCaracters, CancellationTokenSource pCancellationToken)
        {
            IScreen screenResult = null;
            bool volverAtras = false;
            bool limpiarPantalla = false;
            switch (pCaracters)
            {
                case "B":
                    volverAtras = true;
                    limpiarPantalla = true;
                    break;
                case "b":
                    volverAtras = true;
                    limpiarPantalla = true;
                    break;
                default:
                    screenResult = screenFactory.CreateScreenInstance<WriteMessageForSend>();
                    socket.Send(Encoding.ASCII.GetBytes(pCaracters));

                    break;
            }
            if (volverAtras)
            {
                socket.DataReceived -= UDPSocketFactory_DataReceived;
                screenFactory.DeleteScreenInstance<WriteMessageForSend>();
                screenResult = screenFactory.CreateScreenInstance<MenuInit>();

            }
            if (limpiarPantalla)
            {
                Console.Clear();
            }
            return screenResult;
        }
    }
}
