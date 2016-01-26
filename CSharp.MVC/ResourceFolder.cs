using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;

namespace EmbeddedMVC
{
    class ResourceFolder
    {
        public ResourceFolder(ResourceManager manager, string folderName)
        {
            _manager = manager;
            _folderName = folderName;
        }

        private ResourceManager _manager;
        public ResourceManager Manager
        {
            get { return _manager; }
        }


        private string _folderName;
        public string FolderName
        {
            get { return _folderName; }
        }
    }
}
