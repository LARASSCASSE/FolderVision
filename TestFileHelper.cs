using System;
using System.Collections.Generic;
using FolderVision.Utils;

namespace FolderVision
{
    public class TestFileHelper
    {
        public static void RunTests()
        {
            Console.WriteLine("=== Testing FileHelper.CreateScanFolderName ===");
            Console.WriteLine();

            // Test single drive scan
            TestScenario("Single drive C:\\", new List<string> { "C:\\" });
            TestScenario("Single drive D:\\", new List<string> { "D:\\" });

            // Test single folder scans
            TestScenario("Games folder", new List<string> { "D:\\Games" });
            TestScenario("Documents folder", new List<string> { "C:\\Users\\Documents" });
            TestScenario("Steam folder", new List<string> { "D:\\My Games\\Steam" });
            TestScenario("Program Files", new List<string> { "C:\\Program Files (x86)\\Microsoft\\Office" });

            // Test multiple paths
            TestScenario("Multiple drives", new List<string> { "C:\\", "D:\\" });
            TestScenario("Multiple folders", new List<string> { "C:\\Users", "D:\\Games", "E:\\Projects" });

            // Test edge cases
            TestScenario("Empty list", new List<string>());
            TestScenario("UNC path", new List<string> { "\\\\server\\share\\folder" });
            TestScenario("Path with special chars", new List<string> { "C:\\My Projects (Work)\\Test [2024]" });

            Console.WriteLine();
            Console.WriteLine("=== All tests completed ===");
        }

        private static void TestScenario(string description, List<string> paths)
        {
            Console.WriteLine($"Test: {description}");
            Console.WriteLine($"Input: {string.Join(", ", paths)}");

            var result = FileHelper.CreateScanFolderName(paths, new DateTime(2025, 9, 23, 14, 30, 22));
            Console.WriteLine($"Output: {result}");
            Console.WriteLine();
        }
    }
}