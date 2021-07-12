using ConsoleApp.Screens.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ConsoleApp.Screens
{
    public class MenuInit:IScreen
    {
        internal readonly ScreenFactory screenFactory;
        public MenuInit(IServiceProvider pServiceProvider)
        {
            screenFactory = pServiceProvider.GetService<ScreenFactory>();
        }
        public string DrawScreen()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("Menu: Inicio");
            sb.AppendLine("Opciones:");
            sb.AppendLine("1 -> para enviar un mensaje.");
            sb.AppendLine("b -> para salir.");
            return sb.ToString();
        }

        public IScreen ExcecuteOptionMenu(string pCaracters,CancellationTokenSource pCancellationToken)
        {
            IScreen screenResult = null;
            bool volverAtras = false;
            bool limpiarPantalla = false;
            switch (pCaracters)
            {
                case "1":
                    screenResult = screenFactory.CreateScreenInstance<WriteMessageForSend>();
                    limpiarPantalla = true;
                    break;
                case "B":
                    volverAtras = true;
                    limpiarPantalla = true;
                    break;
                case "b":
                    volverAtras = true;
                    limpiarPantalla = true;
                    break;
            }
            if (volverAtras)
            {
                pCancellationToken.Cancel();
                
            }
            if (limpiarPantalla)
            {
                Console.Clear();
            }
            return screenResult;
        }
    }
}
