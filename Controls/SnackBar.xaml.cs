using Microsoft.Maui.Controls;
using System;
using System.Threading.Tasks;

namespace InventorySystem.Controls
{
    public partial class SnackBar : ContentView
    {
        public SnackBar()
        {
            InitializeComponent();
        }

        public async Task Show(string message)
        {
            MessageLabel.Text = message;
            await SnackBarFrame.FadeTo(1, 250);
            await Task.Delay(3000);
            await SnackBarFrame.FadeTo(0, 250);
        }
    }
}
