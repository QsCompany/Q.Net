using System;

namespace Common.Utils
{
    public static class ErrorHandlerReporter
    {
        public static void WriteSeparator(string title = "", char sep = '*')
        {
            if (string.IsNullOrEmpty(title))
            {
                int c = (80 - title.Length - 6) / 2;
                Ionsole.WriteLine(new string(sep, c) + " " + title + new string(sep, c));
            }
            Ionsole.WriteLine(new string(sep, 80));
        }
        public static void StartDebugging()
        {
            StopDebugging();            
            ///FIX:            Application.ThreadException += Application_ThreadException;
            AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        }
        public static void StopDebugging()
        {
            ///FIX:            Application.ThreadException -= Application_ThreadException;
            AppDomain.CurrentDomain.FirstChanceException -= CurrentDomain_FirstChanceException;
            AppDomain.CurrentDomain.UnhandledException -= CurrentDomain_UnhandledException;
        }
        private static bool @internal;
        private static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (@internal) return;
            @internal = true;

            try
            {
                WriteSeparator();
                Ionsole.WriteLine("UnHandled Exception");
                Ionsole.WriteLine("Exception Object : " + e.ExceptionObject?.ToString());
                Ionsole.WriteLine("Exception IsTerminate : " + e.IsTerminating.ToString());
                WriteSeparator();
            }
            catch { }
            @internal = false;
        }

        private static void CurrentDomain_FirstChanceException(object sender, System.Runtime.ExceptionServices.FirstChanceExceptionEventArgs e)
        {
            if (@internal) return;
            @internal = true;

            try
            {
                WriteSeparator("Handled Exception", '-');
                Ionsole.Write(e.Exception);
                WriteSeparator(null, '-');
            }
            catch { }
            @internal = false;

        }

        public static void Application_ThreadException(object sender, System.Threading.ThreadExceptionEventArgs e)
        {
            if (@internal) return;
            @internal = true;
            try
            {
                Ionsole.Write(e.Exception);
            }
            catch { }
            @internal = false;
        }
    }
}
