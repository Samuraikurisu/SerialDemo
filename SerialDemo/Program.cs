using System;
using System.IO.Ports;
using System.IO;
using System.Text;
using System.Threading;

using Microsoft.SPOT;
using Microsoft.SPOT.IO;
using Microsoft.SPOT.Input;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;

using GHIElectronics.NETMF.Hardware;
using GHIElectronics.NETMF.IO;


namespace SerialDemo
{
    public class Program : Microsoft.SPOT.Application
    {
        public static string message;
        public static Text text;
        public static bool isMounted; //Checks for mounted drive
        public static void Main()
        {
            //Program myApplication = new Program();
            //Window mainWindow = myApplication.CreateWindow();
            //myApplication.Run(mainWindow);
            SerialTest();







        }

        /*private Window mainWindow;

        public Window CreateWindow()
        {
            // Create a window object and set its size to the
            // size of the display.
            mainWindow = new Window();
            mainWindow.Height = SystemMetrics.ScreenHeight;
            mainWindow.Width = SystemMetrics.ScreenWidth;

            // Create a single text control.
            text = new Text();

            text.Font = Resources.GetFont(Resources.FontResources.small);
            text.TextContent = message;
            text.HorizontalAlignment = Microsoft.SPOT.Presentation.HorizontalAlignment.Center;
            text.VerticalAlignment = Microsoft.SPOT.Presentation.VerticalAlignment.Center;

            // Add the text control to the window.
            mainWindow.Child = text;

            // Set the window visibility to visible.
            mainWindow.Visibility = Visibility.Visible;

            // Attach the button focus to the window.
            Buttons.Focus(mainWindow);

            return mainWindow;
        }*/

        public static void SerialTest()
        {
            SerialPort serialPort = new SerialPort("COM2", 9600, Parity.None); //Sets Serial port for 9600Baud
            serialPort.ReadTimeout = 0;
            serialPort.Open();
            serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialPortReceived); 

            byte[] inbuff = new byte[32];

            while (true) //Keeps the connection always listing for bytes to read
            {
                
                if (serialPort.BytesToRead>=10)
                {
                    int count = serialPort.Read(inbuff, 0, inbuff.Length);
                    char[] chars = Encoding.UTF8.GetChars(inbuff);
                    string message = new string(chars, 0, count);
                    Debug.Print(message);
                   // text.TextContent = message;
                    // Window mainWindow = myApplication.CreateWindow();
                    //myApplication.Run(mainWindow);   
                } Thread.Sleep(25);

            }
        }
        public static void RemoveableMedia_Insert(object sender, MediaEventArgs e) //Custom event that checks for inserting a SD card. Typically this is handled by checking the status of a switch manually, but the namespace provides for this event
        {
            Debug.Print("SD Card Inserted");
            if (e.Volume.IsFormatted) //Checks to see formatting of SD volume
            {
                Debug.Print("Available folders:");
                string[] strfolders = Directory.GetDirectories(e.Volume.RootDirectory); //Checks for folders
                for (int i = 0; i < strfolders.Length; i++)
                {
                    Debug.Print(strfolders[i]); 
                }
                Debug.Print("Available files:");
                string[] strfiles = Directory.GetFiles(e.Volume.RootDirectory); //Checks for existing files
                for (int k = 0; k < strfiles.Length; k++)
                {
                    Debug.Print(strfiles[k]);
                }
            }
            else
            {
                Debug.Print("SD card is not formatted.");
            }
        }

        public static void RemoveableMedia_Eject(object sender, MediaEventArgs e) //Event to check for removal of SD card
        {
            Debug.Print("SD Card Ejected");
        }

        public static void SDMountThreadMethod() //This method will try to mount the storage for writing and reading
        {
            Debug.Print("Probe1");
            PersistentStorage sdPS = null; //Assumes storage is not there before attempting to see if it is
            Debug.Print("Probe2");
            const int Poll_Time = 500; //Thread sleep time
            bool sdExists;
            while (true)
            {
                try
                {
                    sdExists = PersistentStorage.DetectSDCard(); //Returns boolean of whether the system detects the SD card; this needs to be attempted at the start
                    if (sdExists)
                    {
                        Thread.Sleep(50);
                        sdExists = PersistentStorage.DetectSDCard(); 
                    }
                    if (sdExists && sdPS == null) //If the SD card is in the slot, but has not yet been made into persistent storage, make it persistant and try to mount the storage
                    {
                        sdPS = new PersistentStorage("SD");
                        sdPS.MountFileSystem();
                        isMounted = true;
                    }
                    else if (!sdExists && sdPS != null) //If the SD card is removed, unmount and dispose of the resources that were used by the storage
                    {
                        sdPS.UnmountFileSystem();
                        sdPS.Dispose();
                        isMounted = false;
                        sdPS = null;
                    }
                    if (isMounted == true)
                    {
                        string rootDirectory = VolumeInfo.GetVolumes()[0].RootDirectory;
                        FileStream FileHandle = new FileStream(rootDirectory + @"\test.txt", FileMode.Create);
                        byte[] data = Encoding.UTF8.GetBytes(message);
                        byte[] otherdata = Encoding.UTF8.GetBytes("my other message."); //This is a placeholder in case the main source of data doesn't come in. It is to test the mounting code's functionality
                        //Debug.Print("Writing " + message + " on the SD Card.");
                        FileHandle.Write(data, 0, data.Length);
                        FileHandle.Write(otherdata, 0, otherdata.Length);
                        FileHandle.Close();
                    }
                }
                catch
                {
                    if (sdPS != null) //clears the resources for persistent variable, but does not effect the card once it has been mounted
                    {
                        sdPS.Dispose();
                        sdPS = null;
                    }
                }
                Thread.Sleep(Poll_Time);
            }
        }

        }
    }
}
