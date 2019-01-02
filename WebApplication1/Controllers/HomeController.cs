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

namespace WebApplication1.Controllers
{
    public class HomeController : Controller
    {
        static string intend = "     ";
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
                Console.WriteLine("Credential file saved to: " + credPath);
            }

            // Create Drive API service.
            var service = new DriveService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = ApplicationName,
            });

            StringBuilder sb = new StringBuilder();

            foreach (var Res in ResFromFolder(service, "root").ToList())
                getHierarchy(Res, service, sb);

            ss.MyString = sb.ToString();

            return View(ss);
        }

        public List<Google.Apis.Drive.v3.Data.File> ResFromFolder(DriveService service, string folderId)
        {
            var request = service.Files.List();
            request.PageSize = 1000;
            request.Fields = "nextPageToken, files(id, name, parents, shared, mimeType)";
            request.Q = String.Format("'{0}' in parents", folderId);

            List<Google.Apis.Drive.v3.Data.File> TList = new List<Google.Apis.Drive.v3.Data.File>();
            do
            {
                var children = request.Execute();
                foreach (var child in children.Files)
                {
                    TList.Add(service.Files.Get(child.Id).Execute());
                }
                request.PageToken = children.NextPageToken;
            } while (!String.IsNullOrEmpty(request.PageToken));

            return TList;
        }

        private void getHierarchy(Google.Apis.Drive.v3.Data.File Res, DriveService driveService, StringBuilder sb)
        {
            if (Res.MimeType == "application/vnd.google-apps.folder")
            {
                sb.Append(intend + Res.Name + " :" + Environment.NewLine);
                intend += "     ";

                foreach (var res in ResFromFolder(driveService, Res.Id).ToList())
                    getHierarchy(res, driveService, sb);

                intend = intend.Remove(intend.Length - 5);
            }
            else
            {
                sb.Append(intend + Res.Name + Environment.NewLine);
            }
        }

    }
}