using FolderExplorer.Data;
using FolderExplorer.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FolderExplorer.Controllers
{
    public class FoldersController : Controller
    {
        private static FoldersDAL _foldersDAL = new FoldersDAL();
        private IWebHostEnvironment _webHostEnvironment;

        public FoldersController(IWebHostEnvironment webHostEnvironment)
        {
            _webHostEnvironment = webHostEnvironment;
        }

        // GET: FoldersController
        public ActionResult Index(int id = 1)
        {
            return View(_foldersDAL.GetFolderViewModel(id));
        }

        // GET: FoldersController/Create
        public ActionResult Create(int parentId)
        {
            return View();
        }

        // POST: FoldersController/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                int parentId = int.Parse(collection["ParentId"]);
                string name = MakeUniqueName(parentId, collection["Name"]);
                _foldersDAL.CreateFolder(parentId, name);
                return RedirectToAction(nameof(Index), new { id = collection["ParentId"] });
            }
            catch
            {
                return View();
            }
        }

        // GET: FoldersController/Edit/5
        public ActionResult Edit(int id)
        {
            return View(_foldersDAL.GetFolder(id));
        }

        // POST: FoldersController/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            var folder = _foldersDAL.GetFolder(id);
            if (folder == null)
                return NotFound();
            var parentId = _foldersDAL.GetFolder(id).ParentId;
            string name = MakeUniqueName(parentId, collection["Name"]);
            _foldersDAL.RenameFolder(id, name);
            return RedirectToAction(nameof(Index), new {id = parentId });
        }

        // GET: FoldersController/Delete/5
        public ActionResult Delete(int id)
        {
            return View(_foldersDAL.GetFolder(id));
        }

        // POST: FoldersController/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            var folder = _foldersDAL.GetFolder(id);
            if (folder == null)
                return NotFound();
            if (_foldersDAL.DeleteFolder(id))
                return RedirectToAction(nameof(Index), new { id = folder.ParentId });
            else
                return NotFound();
        }

        public ActionResult Back(int id)
        {
            var folder = _foldersDAL.GetFolder(id);
            if (folder == null)
                return NotFound();
            return RedirectToAction(nameof(Index), new { id = id });
        }

        public ActionResult ExportFolder(int id)
        {
            var exportModel = ExportFolderModel(id);
            string fileName = $"{exportModel.Name}.json";
            string fullPath = GetPathToExportImport(fileName);
            string json = JsonSerializer.Serialize(exportModel, new JsonSerializerOptions()
            {
                WriteIndented = true
            });
            System.IO.File.WriteAllText(fullPath, json);
            return PhysicalFile(fullPath, "text/json", fileName);
        }

        // GET: FoldersController/ImportFolder
        public ActionResult ImportFolder(int parentId)
        {
            return View(new Folder() { ParentId = parentId });
        }

        // POST: FoldersController/ImportFolder
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ImportFolder(IFormCollection collection)
        {
            int parentId = int.Parse(collection["ParentId"]);
            try
            {
                var formFile = Request.Form.Files[0];
                string fullPath = GetPathToExportImport(formFile.Name);
                using var fileStream = System.IO.File.Create(fullPath + ".json");
                formFile.CopyTo(fileStream);
                string json = System.IO.File.ReadAllText(fullPath);
                var folderModel = JsonSerializer.Deserialize<ExportFolderModel>(json);
                ImportFolderModel(folderModel, parentId/*_lastImportParentId*/);
            }
            catch { }
            return RedirectToAction(nameof(Index), new { id = parentId/*_lastImportParentId*/ });
        }

        private string GetPathToExportImport(string fileName)
        {
            return Path.Combine(_webHostEnvironment.WebRootPath, "export-import", fileName);
        }

        private ExportFolderModel ExportFolderModel(int id)
        {
            var folder = _foldersDAL.GetFolder(id);
            if (folder == null)
                return null;
            var subfolders = _foldersDAL.GetSubfolders(folder)
                .Select(sf => ExportFolderModel(sf.Id));
            return new ExportFolderModel()
            {
                Name = folder.Name,
                Subfolders = subfolders.ToList(),
            };
        }

        private void ImportFolderModel(ExportFolderModel folderModel, int parentId)
        {
            string name = MakeUniqueName(parentId, folderModel.Name);
            _foldersDAL.CreateFolder(parentId, name);
            int folderId = _foldersDAL.GetSubfolders(parentId)
                .Where(f => f.Name == name)
                .Select(f => f.Id)
                .First();
            foreach (var subfolder in folderModel.Subfolders)
            {
                ImportFolderModel(subfolder, folderId);
            }
        }

        private string MakeUniqueName(int parentId, string name)
        {
            var names = _foldersDAL.GetSubfolders(parentId).Select(f => f.Name);
            while (names.Contains(name))
            {
                name += "1";
            }
            return name;
        }
    }
}
