using UIKit;

namespace De.Openelp.Feuerwehr.App.iOS
{
    /// <summary>
    /// Provides the iOS application entry point.
    /// </summary>
    public class Application
    {
        // This is the main entry point of the application.
        static void Main(string[] args)
        {
            // if you want to use a different Application Delegate class from "AppDelegate"
            // you can specify it here.
            UIApplication.Main(args, null, typeof(AppDelegate));
        }
    }
}

