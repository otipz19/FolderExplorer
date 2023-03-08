using FolderExplorer.Data;
using System.Runtime.Serialization;

namespace FolderExplorer.Models
{
    [Serializable]
    public class ExportFolderModel
    {
        public string Name { get; set; }
        public List<ExportFolderModel> Subfolders { get; set; }
    }
}
