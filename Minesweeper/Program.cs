using System;
using System.Windows.Forms;

namespace Minesweeper
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            //Nguyễn Tuấn Anh-20206114
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Minesweeper());
        }
    }
}
