using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TModModpackHelper
{
    public static class MiscUtil
    {
        public static void CopyDirectory(string source, string target)
        {
            if (Directory.Exists(target))
            {
                DeleteDirectoryOnlyFiles(target);
            }
            else
            {
                Directory.CreateDirectory(target);
            }
            DirectoryInfo directory = new DirectoryInfo(source);
            foreach (FileInfo file in directory.EnumerateFiles())
            {
                file.CopyTo(Path.Combine(target, file.Name));
            }
            foreach (DirectoryInfo file in directory.EnumerateDirectories())
            {
                CopyDirectory(Path.Combine(source, file.Name), Path.Combine(target, file.Name));
            }
        }
        public static void DeleteDirectoryOnlyFiles(string path)
        {
            DirectoryInfo directory = new DirectoryInfo(path);
            foreach (FileInfo file in directory.EnumerateFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo file in directory.EnumerateDirectories())
            {
                DeleteDirectoryOnlyFiles(file.FullName);
            }
        }
    }
}
