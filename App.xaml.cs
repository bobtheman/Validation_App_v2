namespace AccreditValidation
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();

            var mainPage = new MainPage();

            NavigationPage.SetHasNavigationBar(mainPage, false);

            MainPage = new NavigationPage(mainPage)
            {
                BarBackgroundColor = Color.FromArgb("#407cc9"),
                BarTextColor = Colors.White,
            };
        }
    }
}
