using System;
using System.Collections.Generic;
using System.Text;

namespace LibTests
{
    class Program
    {
        [System.STAThread]
        public static void Main(string[] args)
        {
            NUnit.Gui.AppEntry.Main(new string[] { System.Reflection.Assembly.GetExecutingAssembly().Location });
        }
    }
}
