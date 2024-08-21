using System.Collections.Generic;

namespace UI.MVVM.Models.Engine
{
    internal class EngineInfo
    {
        public string name = "";
        public string path = "";
        public string author = "";
        public List<EngineOption> options = new List<EngineOption>();

        public EngineInfo(string name, string path, string author, List<EngineOption> options)
        {
            this.name = name;
            this.path = path;
            this.author = author;
            this.options = options;
        }

        public EngineInfo()
        {

        }

        public List<string> genOptionCommands()
        {
            return null;
        }
    }
}
