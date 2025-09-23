using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace FolderVision.Utils
{
    public static class FileHelper
    {
        public static bool IsValidPath(string path)
        {
            throw new NotImplementedException();
        }

        public static bool DirectoryExists(string path)
        {
            throw new NotImplementedException();
        }

        public static bool FileExists(string path)
        {
            throw new NotImplementedException();
        }

        public static string FormatFileSize(long bytes)
        {
            throw new NotImplementedException();
        }

        public static string GetRelativePath(string basePath, string fullPath)
        {
            throw new NotImplementedException();
        }

        public static string SanitizeFileName(string fileName)
        {
            throw new NotImplementedException();
        }

        public static string GetSafeFilePath(string directory, string fileName)
        {
            throw new NotImplementedException();
        }

        public static async Task<bool> IsAccessibleAsync(string path)
        {
            throw new NotImplementedException();
        }

        public static bool IsHiddenFile(string filePath)
        {
            throw new NotImplementedException();
        }

        public static bool IsSystemFile(string filePath)
        {
            throw new NotImplementedException();
        }

        public static bool IsSymbolicLink(string path)
        {
            throw new NotImplementedException();
        }

        public static string GetFileExtension(string filePath)
        {
            throw new NotImplementedException();
        }

        public static IEnumerable<string> GetSubDirectories(string path)
        {
            throw new NotImplementedException();
        }

        public static IEnumerable<string> GetFiles(string path)
        {
            throw new NotImplementedException();
        }

        public static long GetFileSize(string filePath)
        {
            throw new NotImplementedException();
        }

        public static DateTime GetLastWriteTime(string path)
        {
            throw new NotImplementedException();
        }

        public static DateTime GetCreationTime(string path)
        {
            throw new NotImplementedException();
        }

        public static bool HasReadPermission(string path)
        {
            throw new NotImplementedException();
        }

        public static string NormalizePath(string path)
        {
            throw new NotImplementedException();
        }

        public static bool MatchesPattern(string fileName, string pattern)
        {
            throw new NotImplementedException();
        }
    }
}