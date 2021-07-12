using ConsoleApp.Screens.Interfaces;
using Infraestructura;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp.Screens
{
    public class ScreenFactory
    {
        //internal readonly UDPSocketFactory udpSocket;
        internal readonly IServiceProvider serviceProvider;
        private Dictionary<string,IScreen> screens = new Dictionary<string, IScreen>();
        public ScreenFactory(IServiceProvider pServiceProvider)
        {
            //udpSocket = pServiceProvider.GetService<UDPSocketFactory>();
            serviceProvider = pServiceProvider;
        }
        public IScreen CreateScreenInstance<Screen>()
            where Screen : IScreen
        {
            IScreen screenResult = null;
            string typeName = typeof(Screen).Name;
            screenResult = (IScreen)InstanceClass(typeName, typeof(Screen));
            return screenResult;
        }
        public void DeleteScreenInstance<Screen>()
            where Screen : IScreen
        {
            string typeName = typeof(Screen).Name;
            if (screens.ContainsKey(typeName))
            {
                screens.Remove(typeName);
            }
        }
        private object InstanceClass(string ClassName,Type type)
        {
            object screen = null; ;
            if (!screens.ContainsKey(ClassName))
            {
                screen = serviceProvider.GetService(type);
                screens.Add(ClassName, (IScreen)screen);
            }
            else
            {
                screen = screens[ClassName];
            }
            return screen;
        }
    }
}
