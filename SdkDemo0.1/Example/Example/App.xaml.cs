using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Tego.Devices.Simulated;
using Tego.Devices.TechnologySolutions;
using Tego.Devices.Zeti;
using Tego.IO;
using Tego.Rfid.Gen2;
using Xamarin.Forms;

namespace Example
{
	public partial class App : Application
	{
        private ReaderManager readerManager;

        public static Reader Reader;

		public App ()
		{
			InitializeComponent();
			MainPage = new Example.MainPage();
        }

        private void WriteResourceToFile(string resourceName, string fileName)
        {
            using (var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                using (var file = new FileStream(fileName, FileMode.Create, FileAccess.Write))
                {
                    resource.CopyTo(file);
                    resource.Dispose();
                    file.Flush();
                    file.Dispose();
                }
            }
        }

        protected override void OnStart ()
		{
            // Copy Tego License
			WriteResourceToFile("Example.TegoOS.Lic", FileHelper.Combine(FileHelper.GetLocalDirectory(), "TegoOS.Lic"));

            // Remember to add Bluetooth permission in package manifest, info.plist, etc. for each platform's app
            readerManager = new ReaderManager();

            Reader = readerManager.CreateReader(typeof(SimulatedReader));
            SimulatedTagPopulation.AddUniqueTag("E2817000");
            SimulatedTagPopulation.AddUniqueTag("E2817000");

            //Reader = readerManager.CreateReader(typeof(TechnologySolutionsReader));

            //Reader = readerManager.CreateReader(typeof(ZetiReader));

            ConnectReader();

        }

		protected override void OnSleep ()
		{
            // Disconnect reader as Bluetooth link will drop on some platforms
            DisconnectReader();
		}

		protected override void OnResume ()
		{
            // Connect reader as Bluetooth link will have dropped on some platforms
            ConnectReader();
        }

        private void ConnectReader()
        {
            if (!Reader.Implementation.IsConnected)
            {
                // To do: catch exception, alert user, exit app
                Reader.Connect(null);
                // Change any reader settings here
                Reader.Initialize();
            }
        }

        private void DisconnectReader()
        {
            if (Reader.Implementation.IsConnected)
            {
                Reader.Disconnect();
            }
        }
	}
}
