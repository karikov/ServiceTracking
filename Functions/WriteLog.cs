using System;
using System.IO;
using System.Text;

namespace Service.Functions
{
    public class Log
    {
        public static void Write(Exception exception)
        {
            if (exception == null) return;
            string path = Directory.GetCurrentDirectory() + "\\Logs";
            string fileName = "ExceptionLog_" + DateTime.Now.ToShortDateString() + ".txt";
            if (Directory.Exists(path) == false) Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\Logs");
            using var outfile = new StreamWriter(path + "\\" + fileName, true, Encoding.UTF8);
            outfile.WriteLine("\n***********************");
            outfile.WriteLine("\nDate: {0}", DateTime.Now);
            outfile.WriteLine();
            outfile.Write(exception.InnerException.Message);

        }
        public static void Write(string msg)
        {
            if (string.IsNullOrEmpty(msg)) return;
            string path = Directory.GetCurrentDirectory() + "\\Logs";
            string fileName = "MegssageLog_" + DateTime.Now.ToShortDateString() + ".txt";
            if (Directory.Exists(path) == false) Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\Logs");
            using var outfile = new StreamWriter(path + "\\" + fileName, true, Encoding.UTF8);
            outfile.WriteLine("\n***********************");
            outfile.WriteLine("\nDate: {0}", DateTime.Now);
            outfile.WriteLine();
            outfile.Write(msg);
        }
    }
}
