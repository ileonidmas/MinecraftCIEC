using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace RunMission.Evolution
{
    public static class FileUtility
    {

        //private readonly static string resultsPath = "";
        private readonly static string resultsPath = System.AppDomain.CurrentDomain.BaseDirectory + "/Results/Users";


        public static string CreateOutputDirectory(string username, string foldername)
        {
            string fullPath = resultsPath + "/" +  username + "/" + foldername + "/";

            if (Directory.Exists(fullPath))
            {
                Directory.Delete(fullPath, true);
            }

            Directory.CreateDirectory(fullPath);

            return fullPath;
        }


        public static string GetParentVideo(string username, string foldername)
        {

            string destinationFolder = resultsPath + "/" + username + "/" + foldername + "/" + "data";
            string videoPath = Directory.GetDirectories(destinationFolder)[0] + "/video.mp4";
            return videoPath;
        }

        public static string DecodeArchiveAndGetVideoPath(string username, string foldername)
        {
            string fullPath = resultsPath + "/" + username + "/" + foldername + "/";
            string destinationFolder = resultsPath + "/" + username + "/" + foldername + "/" + "data";

            var inStream = new FileStream(fullPath + "data.tgz", FileMode.Open);
            Stream gzipStream = new GZipInputStream(inStream);

            TarArchive tarArchive = TarArchive.CreateInputTarArchive(gzipStream);
            tarArchive.ExtractContents(destinationFolder);
            tarArchive.Close();

            gzipStream.Close();
            inStream.Close();

            //remove archive

            File.Delete(fullPath + "data.tgz");


            string videoPath = Directory.GetDirectories(destinationFolder)[0] + "/video.mp4";

            return videoPath;

        }


        public static string GetUserResultPath(string username)
        {
            return resultsPath +"/"+ username + "/" ;
        }

        public static string GetUserResultVideoPath(string username, string foldername)
        {
            string extraFolderName =Path.GetFileName( Directory.GetDirectories(GetUserResultPath(username) + "/" + foldername + "/data")[0]);
            return username + "/" + foldername + "/data/"+ extraFolderName + "/video.mp4";
        }

        public static bool IsFirstIteration(string username, string foldername)
        {
            string fullPath = resultsPath + "/" + username;
            var directories = Directory.GetDirectories(fullPath);
            foreach (var folder in directories)
            {
                var name = Path.GetFileName(folder);
                if (name == foldername)
                    return false;
            }
            return true;
        }

        public static void RemoveOldFolder(string username, string foldername)
        {
            string fullPath = resultsPath + "/" + username + "/" + foldername;
            if(Directory.Exists(fullPath))
                Directory.Delete(fullPath, true);
        }

        public static void CopyCanditateToParentFolder(string username, string foldername)
        {
            string fullPath = resultsPath + "/" + username + "/" + foldername;
            string oldPath = resultsPath + "/" + username + "/0" ;
            Directory.Delete(oldPath, true);
            Directory.Move(fullPath, oldPath);
        }

        public static void CreateUserFolder(string username)
        {
            string userPath = resultsPath + "/" + username;
            if (Directory.Exists(userPath))
                Directory.Delete(userPath, true);
            Directory.CreateDirectory(userPath);
        }

    }
}
