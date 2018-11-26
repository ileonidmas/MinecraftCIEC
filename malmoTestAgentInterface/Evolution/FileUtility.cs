using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;
using SharpNeat.Genomes.Neat;
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
        private readonly static string candidatePath = System.AppDomain.CurrentDomain.BaseDirectory + "/EvolutionDB/Candidates";

        public static string CreateOutputDirectory(string username, string foldername)
        {
            string fullPath = resultsPath + "/" + username + "/" + foldername + "/";

            if (Directory.Exists(fullPath))
            {
                DeleteDirectory(fullPath);
            }

            Directory.CreateDirectory(fullPath);
            return fullPath;
        }


        public static string GetVideoPathWithoutDecoding(string username, string foldername)
        {

            string destinationFolder = resultsPath + "/" + username + "/" + foldername + "/" + "data";
            string videoPath = Directory.GetDirectories(destinationFolder)[0] + "/video.mp4";
            return videoPath;
        }

        public static string DecodeArchiveAndGetVideoPath(string username, string foldername)
        {
            string fullPath = resultsPath + "/" + username + "/" + foldername + "/";
            string destinationFolder = resultsPath + "/" + username + "/" + foldername + "/" + "data";
            if (!File.Exists(fullPath + "data.tgz"))
                return GetVideoPathWithoutDecoding(username, foldername);

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

        public static string GetUserDBVideoPath(string username, string foldername)
        {
            string extraFolderName = Path.GetFileName(Directory.GetDirectories(candidatePath + "/" + username + "/" + foldername + "/0/data")[0]);
            string path =username + "/" + foldername + "/0/data/" + extraFolderName + "/video.mp4";
            return path;
        }

        public static void CopyFilesToUserFolder(string filesLocation, string username)
        {
            string source = candidatePath + "/" + username+ "/" + Path.GetFileName(filesLocation);
            string dest = resultsPath + "/" + username;
            //Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(source, "*",
                SearchOption.AllDirectories))
                Directory.CreateDirectory(dirPath.Replace(source, dest));

            //Copy all the files & Replaces any files with the same name
            foreach (string newPath in Directory.GetFiles(source, "*.*",
                SearchOption.AllDirectories))
                File.Copy(newPath, newPath.Replace(source, dest), true);
        }

        public static bool IsFirstIteration(string username, string foldername)
        {
            string fullPath = resultsPath + "/" + username;
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);                
            }
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
                DeleteDirectory(fullPath);
        }

        public static void CopyCanditateToParentFolder(string username, string foldername)
        {
            if (foldername == "0")
                return;
            string fullPath = resultsPath + "/" + username + "/" + foldername;
            string oldPath = resultsPath + "/" + username + "/0" ;
            DeleteDirectory(oldPath);
            Directory.Move(fullPath, oldPath);
        }

        public static void CopyCanditateToProperFolder(string username, string currentfoldername, string newfoldername)
        {
            string source = resultsPath + "/" + username + "/" + currentfoldername;
            string dest = resultsPath + "/" + username + "/" + newfoldername;
            if(Directory.Exists(dest))
                DeleteDirectory(dest);
            Directory.Move(source, dest);
        }

        public static void CreateUserFolder(string username)
        {
            string userPath = resultsPath + "/" + username;
            if (Directory.Exists(userPath))
                DeleteDirectory(userPath);

            while (!Directory.Exists(userPath))
            {
                Directory.CreateDirectory(userPath);
            }
        }

        public static string SaveCurrentProgressAndReturnPath(string username)
        {
            string userPath = candidatePath + "/" + username;
            int count = 0;
            //check if exists and then create if doesnt
            if (Directory.Exists(userPath))
                count = Directory.GetDirectories(userPath).Length;
            else
                Directory.CreateDirectory(userPath);

            string generationPath = resultsPath + "/" + username;

            string finalPath = userPath + "/" + count.ToString();
            Directory.Move(generationPath, finalPath);

            //return path evolution

            return finalPath;
        }

        public static string GetVideoPathFromEvolutionPath(string evolutionPath)
        {

            string extraFolderName = Path.GetFileName(Directory.GetDirectories(evolutionPath + "/0/data")[0]);

            return evolutionPath + "/0/data/"+ extraFolderName + "/video.mp4" ;
        }


        public static void SaveCurrentStructure(string username, string foldername, bool[] structure)
        {
            string path = resultsPath + "/" + username + "/" + foldername + "/structure.txt";

            //Create the file and save all values to the file
            using (StreamWriter sw = new StreamWriter(path))
            {
                // Run through the structure grid and save all values to the file
                for (int j = 0; j < structure.Length; j++)
                {
                    if (structure[j])
                        sw.Write("1");
                    else
                        sw.Write("0");
                }
                sw.WriteLine();
            }
        }

        public static void SaveCurrentGenome(string username, string foldername, NeatGenome genome)
        {
            List<NeatGenome> list = new List<NeatGenome>();
            list.Add(genome);
            string path = resultsPath + "/" + username + "/" + foldername + "/genome.xml";
            var doc = NeatGenomeXmlIO.SaveComplete(list, false);
            doc.Save(path);
        }

        public static void SaveNovelStructure(string username, string foldername)
        {
            string source = resultsPath + "/" + username + "/" + foldername + "/structure.txt";
            string dest = resultsPath + "/" + username + "/" + "/structureArchive.txt";

            string structure = "";

            using(StreamReader sr = new StreamReader(source))
            {
                structure = sr.ReadLine();
            }

            using(StreamWriter sw = new StreamWriter(dest, true))
            {
                sw.WriteLine(structure);
            }
        }

        public static List<bool[]> LoadStructures(string username)
        {
            string path = resultsPath + "/" + username + "/structureArchive.txt";
            List<bool[]> structures = new List<bool[]>();
            using (StreamReader sr = new StreamReader(path))
            {
                string line;
                while ((line = sr.ReadLine()) != null)
                {
                    bool[] structure = line.Select(c => c == '1').ToArray();
                    structures.Add(structure);
                }

            }
            return structures;

        }




        public static void DeleteDirectory(string target_dir)
        {
            string[] files = Directory.GetFiles(target_dir);
            string[] dirs = Directory.GetDirectories(target_dir);

            foreach (string file in files)
            {
                File.SetAttributes(file, FileAttributes.Normal);
                File.Delete(file);
            }

            foreach (string dir in dirs)
            {
                DeleteDirectory(dir);
            }

            Directory.Delete(target_dir, false);
        }
    }
}
