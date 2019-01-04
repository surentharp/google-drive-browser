using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using WebApplication1.Models;
using Newtonsoft.Json;

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        static string[] Scopes = { DriveService.Scope.DriveReadonly };
        static string ApplicationName = "Drive API .NET Quickstart";

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult drive()
        {
            return View(new List<string>());
        }

        public JsonResult GetFolders()
        {
            MyStringModel ss = new MyStringModel();

            string filePath = Server.MapPath(Url.Content("~/Content/credentials.json"));

            UserCredential credential;

            using (var stream =
                new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = Server.MapPath(Url.Content("~/Content/token.json"));
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                //Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            List<FolderListClass> _folders = FolderStructureMethod(service);

            return this.Json(_folders, JsonRequestBehavior.AllowGet);
        }

        private List<FolderListClass> FolderStructureMethod(DriveService service)
        {
            List<FolderTempClass> _foldersObject = new List<FolderTempClass>();

            FilesResource.ListRequest listRequest2 = service.Files.List();
            listRequest2.PageSize = 10;
            listRequest2.Fields = "nextPageToken, files(id, name, parents, shared)";
            listRequest2.Q = "'root' in parents";

            String rootID = listRequest2.Execute().Files[0].Parents[0];

            bool getFiles = true;
            string pageToken = "";

            while (getFiles)
            {
                // Define parameters of request.
                FilesResource.ListRequest listRequest = service.Files.List();
                listRequest.PageSize = 1000;
                listRequest.Fields = "nextPageToken, files(id, name, parents, shared, mimeType)";
                listRequest.Q = "mimeType = 'application/vnd.google-apps.folder'";

                if (pageToken != "")
                    listRequest.PageToken = pageToken;

                // List files.
                IList<Google.Apis.Drive.v3.Data.File> files = listRequest.Execute()
                    .Files;

                pageToken = listRequest.Execute().NextPageToken;

                pageToken = pageToken ?? "";

                pageToken = pageToken.Trim();

                if (files != null && files.Count > 0)
                {
                    foreach (var file in files)
                    {
                        if (file.Parents != null)
                        {
                            //Console.WriteLine("{0} ({1}) =>{2}<= Parent: {3}", file.Name, file.Id, file.Shared, file.Parents[file.Parents.Count - 1]);
                            _foldersObject.Add(new FolderTempClass { FolderID = file.Id, FolderName = file.Name, ParentID = file.Parents[0], MimeType = file.MimeType });
                        }
                    }
                }

                if (pageToken == "")
                    getFiles = false;
                else
                    getFiles = true;
            }

            List<FolderListClass> _folders = new List<FolderListClass>();

            _folders.Add(new FolderListClass { id = "root", text = "root" });

            foreach (var Res in ResFromFolder(_foldersObject, rootID).ToList())
                getHierarchy(Res, _folders[0].children, _foldersObject);
            return _folders;
        }

        public ActionResult GetAllFiles(string id)
        {
            List<string> returnedFiles = new List<string>();

            string filePath = Server.MapPath(Url.Content("~/Content/credentials.json"));

            UserCredential credential;

            using (var stream =
                new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                // The file token.json stores the user's access and refresh tokens, and is created
                // automatically when the authorization flow completes for the first time.
                string credPath = Server.MapPath(Url.Content("~/Content/token.json"));
                credential = GoogleWebAuthorizationBroker.AuthorizeAsync(
                    GoogleClientSecrets.Load(stream).Secrets,
                    Scopes,
                    "user",
                    CancellationToken.None,
                    new FileDataStore(credPath, true)).Result;
                //Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });


            List<string> _files = new List<string>();

            foreach (var Res in ResFromFiles(service, id).ToList())
                getHierarchyFiles(Res, service, _files);

            var jj = JsonConvert.SerializeObject(_files);

            return View(_files);
        }

        #region Old Logic

        //public List<Google.Apis.Drive.v3.Data.File> ResFromFolder(DriveService service, string folderId)
        //{
        //    var request = service.Files.List();
        //    request.PageSize = 1000;
        //    request.Fields = "nextPageToken, files(id, name, parents, shared, mimeType)";
        //    request.Q = String.Format("'{0}' in parents", folderId);

        //    List<Google.Apis.Drive.v3.Data.File> TList = new List<Google.Apis.Drive.v3.Data.File>();
        //    do
        //    {
        //        var children = request.Execute();
        //        foreach (var child in children.Files)
        //        {
        //            TList.Add(service.Files.Get(child.Id).Execute());
        //        }
        //        request.PageToken = children.NextPageToken;
        //    } while (!String.IsNullOrEmpty(request.PageToken));

        //    return TList;
        //}

        //private void getHierarchy(Google.Apis.Drive.v3.Data.File Res, DriveService driveService, StringBuilder sb)
        //{
        //    if (Res.MimeType == "application/vnd.google-apps.folder")
        //    {
        //        sb.Append(intend + Res.Name + " :" + Environment.NewLine);
        //        intend += "     ";

        //        foreach (var res in ResFromFolder(driveService, Res.Id).ToList())
        //            getHierarchy(res, driveService, sb);

        //        intend = intend.Remove(intend.Length - 5);
        //    }
        //    else
        //    {
        //        sb.Append(intend + Res.Name + Environment.NewLine);
        //    }
        //}


        #endregion

        #region New Logic

        public List<FolderTempClass> ResFromFolder(List<FolderTempClass> _folderObject, string folderId)
        {

            List<FolderTempClass> TList = new List<FolderTempClass>();

            var children = (from k in _folderObject where k.ParentID == folderId select k);
            foreach (var child in children)
            {
                //TList.Add(service.Files.Get(child.Id).Execute());
                TList.Add(new FolderTempClass { FolderID = child.FolderID, ParentID = child.ParentID, FolderName = child.FolderName, MimeType = child.MimeType });
            }

            return TList;
        }

        private void getHierarchy(FolderTempClass Res, List<FolderListClass> _folders, List<FolderTempClass> _folderObject)
        {
            if (Res.MimeType == "application/vnd.google-apps.folder")
            {
                _folders.Add(new FolderListClass { id = Res.FolderID, text = Res.FolderName });

                foreach (var res in ResFromFolder(_folderObject, Res.FolderID))
                {
                    List<FolderListClass> _tempList = (from k in _folders where k.id == Res.FolderID select k.children).Single();
                    getHierarchy(res, _tempList, _folderObject);
                }

            }

        }

        public static List<Google.Apis.Drive.v3.Data.File> ResFromFiles(DriveService service, string folderId)
        {
            //var request = service.Files.List(folderId);
            var request = service.Files.List();
            request.PageSize = 1000;
            request.Fields = "nextPageToken, files(id, name, parents, mimeType)";
            request.Q = String.Format("'{0}' in parents", folderId);

            List<Google.Apis.Drive.v3.Data.File> TList = new List<Google.Apis.Drive.v3.Data.File>();
            do
            {
                var children = request.Execute();
                foreach (var child in children.Files)
                {
                    //TList.Add(service.Files.Get(child.Id).Execute());
                    TList.Add(child);
                }
                request.PageToken = children.NextPageToken;
            } while (!String.IsNullOrEmpty(request.PageToken));

            return TList;
        }



        private static void getHierarchyFiles(Google.Apis.Drive.v3.Data.File Res, DriveService driveService, List<string> _files)
        {
            if (Res.MimeType == "application/vnd.google-apps.folder")
            {
                _files.Add(Res.Name + " - [Folder]");
            }
            else
            {
                _files.Add(Res.Name);
            }
        }

        #endregion

    }

    public class FolderListClass
    {
        public string id { get; set; }
        public string text { get; set; }
        public List<FolderListClass> children { get; set; }

        public FolderListClass()
        {
            this.children = new List<FolderListClass>();
        }
    }

    public class CustomJsonFolderModel
    {
        public string id { get; set; }
        public string text { get; set; }
    }

    public class FolderTempClass
    {
        public string FolderID { get; set; }
        public string FolderName { get; set; }
        public string ParentID { get; set; }
        public string MimeType { get; set; }
    }
}