using System;
using System.Threading.Tasks;
using FolderVision.Ui;
using FolderVision.Core;
using FolderVision.Models;
using FolderVision.Exporters;

namespace FolderVision
{
    class Program
    {
        static async Task Main(string[] args)
        {
            try
            {
                var consoleUI = new ConsoleUI();
                await consoleUI.RunAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An unexpected error occurred: {ex.Message}");
                Console.WriteLine("Press any key to exit...");
                Console.ReadKey();
            }
        }
    }
}