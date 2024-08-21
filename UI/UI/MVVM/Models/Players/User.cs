using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Windows.Media;
using UI.MVVM.Models.Engine;

namespace UI.MVVM.Models.Players
{
    internal static class User
    {
        private static BrushConverter converter = new BrushConverter();

        public static List<EngineInfo> _Engines = new List<EngineInfo>();

        public static readonly string rootFolder;
        public static readonly string assetsFolder;


        public static List<EngineInfo> Engines
        {
            get { return _Engines; }
            set { }
        }

        public static List<string> EngineName
        {
            get
            {
                List<string> list = new List<string>();
                foreach (EngineInfo engine in _Engines)
                {
                    list.Add(engine.name);
                }
                return list;
            }
            set { }
        }

        public static string UserName { get; set; }
        public static string flag { get; set; }

        public static UISettings Settings { get; set; }

        static User()
        {
            UserName = "Anon";
            flag = "Default";
            Settings = new UISettings();
            rootFolder = Directory.GetParent(System.IO.Directory.GetCurrentDirectory()).Parent.Parent.Parent.Parent.FullName;
            string appFolderPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
            assetsFolder = System.IO.Path.Combine(Directory.GetParent(appFolderPath).Parent.Parent.FullName, "Assets");
            string EngineString = System.IO.Path.Combine(new string[] { rootFolder, "Engine", "Engine", "bin", "Release", "Engine.exe" });

            EngineInfo temp = new EngineInfo("NEA Chess Ai", EngineString, "Harry Foster", new List<EngineOption>());

            _Engines.Add(temp);
            Settings.DefaultEngine = 0;

        }
    }
}
