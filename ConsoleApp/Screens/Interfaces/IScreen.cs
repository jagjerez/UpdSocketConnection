using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ConsoleApp.Screens.Interfaces
{
    public interface IScreen
    {
        string DrawScreen();
        IScreen ExcecuteOptionMenu(string pCaracters, CancellationTokenSource pCancellationToken);
    }
}
