using System;
using Xamarin.Forms;
using Xamarin.Forms.Xaml;

[assembly: XamlCompilation(XamlCompilationOptions.Compile)]
namespace XamarinVideoPlayer
{
    public partial class App : Application
    {
        public App()
        {
            InitializeCommon();
            MainPage = new MainPage();
        }

        public App(int fd)
        {
            InitializeCommon();
            MainPage = new MainPage(fd);
        }

        private void InitializeCommon()
        {
            InitializeComponent();
        }

        protected override void OnStart()
        {
            // Handle when your app starts
        }

        protected override void OnSleep()
        {
            // Handle when your app sleeps
        }

        protected override void OnResume()
        {
            // Handle when your app resumes
        }
    }
}
