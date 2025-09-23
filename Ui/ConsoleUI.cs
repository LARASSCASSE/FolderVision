using System;
using System.Threading.Tasks;
using FolderVision.Core;
using FolderVision.Models;
using FolderVision.Exporters;

namespace FolderVision.Ui
{
    public class ConsoleUI
    {
        private readonly ProgressDisplay _progressDisplay;

        public ConsoleUI()
        {
            _progressDisplay = new ProgressDisplay();
        }

        public async Task RunAsync()
        {
            throw new NotImplementedException();
        }

        public void ShowWelcomeMessage()
        {
            throw new NotImplementedException();
        }

        public void ShowMainMenu()
        {
            throw new NotImplementedException();
        }

        public string GetFolderPath()
        {
            throw new NotImplementedException();
        }

        public ScanSettings GetScanSettings()
        {
            throw new NotImplementedException();
        }

        public void DisplayScanResult(ScanResult result)
        {
            throw new NotImplementedException();
        }

        public void DisplayError(string message)
        {
            throw new NotImplementedException();
        }

        public void DisplayWarning(string message)
        {
            throw new NotImplementedException();
        }

        public void DisplayInfo(string message)
        {
            throw new NotImplementedException();
        }

        public void DisplaySuccess(string message)
        {
            throw new NotImplementedException();
        }

        public bool ConfirmAction(string message)
        {
            throw new NotImplementedException();
        }

        public string GetExportPath()
        {
            throw new NotImplementedException();
        }

        public void WaitForKey()
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            Console.Clear();
        }

        private void SetConsoleColor(ConsoleColor color)
        {
            Console.ForegroundColor = color;
        }

        private void ResetConsoleColor()
        {
            Console.ResetColor();
        }
    }
}