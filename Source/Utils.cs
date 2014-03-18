
// GaGa.


using System;
using System.Drawing;
using System.IO;
using System.Reflection;


namespace GaGa
{
    static class Utils
    {
        public static Icon LoadIconFromResource(String resource)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            using (Stream stream = assembly.GetManifestResourceStream(resource))
            {
                return new Icon(stream);
            }
        }
    }
}

