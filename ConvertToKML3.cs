using System;
using System.Text;
using System.IO;
using System.Runtime.InteropServices;


    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct GPS290Format
    {
        public short year;       // 0
        public short month;      // 2
        public short day;        // 4
        public short hour;       // 6
        public short minute;     // 8
        public short second;     // 10
        public float unknown1;   // 12
        public float hdop;       // 16
        public float unknown2;   // 20
        public float latitude;   // 24
        public float longitude;  // 28
        public float altitude;   // 32
        public float unknown3;   // 36
        public float speed;      // 40
        public float bearing;    // 44
    }

public struct LineStyle
{
    public int SpeedLimit;
    public string Name;
    public string Description;
    public string Colour;
    public string FontColour;
    private double TotalSec;

    public LineStyle(int SpeedLimit, string Name, string Description, string Colour, string FontColour)
    {
        this.SpeedLimit = SpeedLimit;
        this.Name = Name;
        this.Description = Description;
        this.Colour = Colour;
        this.FontColour = FontColour;
        this.TotalSec = 0;
    }

    public void TotalSecondsAdd(Double Seconds)
    {
        this.TotalSec += Seconds;
    }
    public Double TotalSecondsView()
    {
        return this.TotalSec;
    }
    public void TotalSecondsReset()
    {
        this.TotalSec = 0;
    }
}

 
	public struct Config
	{
        public double LonVariance;
        public double LatVariance;
        public LineStyle[] ConfigLineStyle;
    }
 
 
 
namespace GPSConvertToKML
{
    
    class Program
    {

        public const string FILENOTCREATED = "ERROR no file created.";
        public const string SPEEDBAND = "speedband";
        public const string TOTALSPEEDBANDS = "numbands";
        public const string LONGITUDEVARIANCE = "longitudevariance";
        public const string LATITUDEVARIANCE = "latitudevariance";
		
        // Returns the linestyle of a speedband depending on the speed provided
        public static int SpeedCategory(float speed , LineStyle[] SpeedLineStyle)
        {
            for (int i = 0; i <= SpeedLineStyle.Length-2; i++)
            {
                if (speed < SpeedLineStyle[i].SpeedLimit)
                {
                    return i;
                }
            }
            // Default value is last speed
            return SpeedLineStyle.Length-1;
        }

        // Reads the speedband configuration from the INI file or creates a new INI file
        // with default values if one does not exist
        public static Config ReadINI(string FileName)
        {
            Config NewConfig = new Config();
            NewConfig.LonVariance = 0.0004; // i.e. 112ish km/hr
            NewConfig.LatVariance = 0.0004;
            LineStyle[] linestyle = new LineStyle[0];
            String fileline;
            string[] SpeedVar = new string[5]; // Speed variables 
            string[] param; // list of variables provided on a line
            int item = 0; // Current speedband
            int NumBands = 0;
            string FontColour;

			// Read Ini file
            try
            {
                if (!File.Exists(FileName)) 
                {
                    Console.WriteLine("Configuration file does not exist. Creating " + FileName);
                    // Create INI file if it doesn't exist
                    using (StreamWriter LineINI = File.CreateText(FileName))
                    {
                        LineINI.WriteLine("[Comments]");
						LineINI.WriteLine(": LongitudeVariance - Degrees per second. Used to ignore longitude jumps greater than this variance");
						LineINI.WriteLine(": LatitudeVariance - Degrees per second. Used to ignore latitude jumps greater than this variance");
                        LineINI.WriteLine(": Note: 1 (degree) latitude = 111.12 kilometers or 69.047 miles");
                        LineINI.WriteLine(":       so .00027 (deegrees per second) = 112ish km/hr ");
                        LineINI.WriteLine(": Speedband=[Speed],[Label],[Description],[Colour],[Font Colour]");
                        LineINI.WriteLine(": Speed -       Speedbands must be sorted in order of speed. Note: the last band does not require a speed value.");
                        LineINI.WriteLine(": Label -       Providing a label makes reading the KML file easier only");
                        LineINI.WriteLine(": Description - The description for the speedband is displayed in GoogleEarth");
                        LineINI.WriteLine(": Colour -      The colour of the speed band line. One method to determine the");
                        LineINI.WriteLine(":               colour code is to change the properties of a speedband in");
                        LineINI.WriteLine(":               GoogleEarth, using right-click, to change the colour of and");
                        LineINI.WriteLine(":               existing speedband. Save the modified KML file, right-click on");
                        LineINI.WriteLine(":               trip, and view the colour code in the resulting KML file");
                        LineINI.WriteLine(":  FontColour - Colour code for Line Description in KML file. This makes it easier to determine what the");
                        LineINI.WriteLine(":               speedband colour codes our. FontColour uses HTML colour codes not Google Earth colour codes.");
                        LineINI.WriteLine(":               If no colour code is provided the font colour will default to black");
                        LineINI.WriteLine("");
                        LineINI.WriteLine("[Processing]");
                        LineINI.WriteLine("LONGITUDEVARIANCE=" + NewConfig.LonVariance);
                        LineINI.WriteLine("LATITUDEVARIANCE=" + NewConfig.LatVariance);
                        LineINI.WriteLine("");
						LineINI.WriteLine("[LineStyle]");
						LineINI.WriteLine("NUMBANDS=6");
                        LineINI.WriteLine("SPEEDBAND=3,STOP,Stopped - 3km/hr or less,00000000,000000");
                        LineINI.WriteLine("SPEEDBAND=25,JAM,Traffic Jam - 25km/hr or less,ff000000,000000");
                        LineINI.WriteLine("SPEEDBAND=55,SLOW,Slow - 55km/hr or less,ff006f38,336600");
                        LineINI.WriteLine("SPEEDBAND=85,RESTRICTED,Restricted - 85km/hr or less,a6ffaa55,6699ff");
                        LineINI.WriteLine("SPEEDBAND=105,FAST,Fast - 105km/hr or less,a67f0000,000066");
                        LineINI.WriteLine("SPEEDBAND=9999,SPEED,Speeding - Greater than 105km/hr,a60000ff,ff0000");
                        LineINI.Close();
                    }
                }

                StreamReader sr = new StreamReader(new FileStream(FileName, FileMode.Open, FileAccess.Read));                             
                fileline = sr.ReadLine();
                while (fileline != null)
                {
                    param = fileline.Split('=');

                    switch (param[0].ToLower())
                    {
						case LONGITUDEVARIANCE:
                            NewConfig.LonVariance = Convert.ToDouble(param[1]);
							break;
                        case LATITUDEVARIANCE:
                            NewConfig.LatVariance = Convert.ToDouble(param[1]);
							break;
						case TOTALSPEEDBANDS:
                            NumBands = Convert.ToInt32(param[1]);
                            linestyle = new LineStyle[NumBands];
                            break;
                        case SPEEDBAND:
                            if (item < NumBands)
                            {
                                SpeedVar = param[1].Split(',');
                                // If no Font Colour was provided then default to black
                                if (SpeedVar.Length < 5)
                                {
                                    FontColour = "000000";
                                }
                                else FontColour = SpeedVar[4];

                                linestyle[item] = new LineStyle(Convert.ToInt32(SpeedVar[0]), SpeedVar[1], SpeedVar[2], SpeedVar[3], FontColour);
                                item++;
                            }
                            break;
                    }
                    fileline = sr.ReadLine();
                }
                sr.Close();
                NewConfig.ConfigLineStyle = linestyle;
                return NewConfig;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error processing configuration file: " + e.Message + ". Using default values.");
                // Default values if error in INI file 
                linestyle = new LineStyle[6]{
                    new LineStyle(3,"STOP","Stopped - 3km/hr or less","00ffffff","000000"),
                    new LineStyle(25,"JAM","Traffic Jam - 25km/hr or less","ff000000","000000"),
                    new LineStyle(55,"SLOW","Slow - 55km/hr or less","ff006f38","336600"),
                    new LineStyle(85,"RESTRICTED","Restricted - 85km/hr or less","a6ffaa55","6699ff"),
                    new LineStyle(105,"FAST","Fast - 105km/hr or less","a67f0000","000066"),
                    new LineStyle(999,"SPEED","Speeding - Greater than 105km/hr","a60000ff","ff0000"), // this is used by default rather than checking that it is really under 999
                    };
                NewConfig.ConfigLineStyle = linestyle;
                return NewConfig;
            }
            
        }

		
		// Allow GPS data to be accessed using GPS290Format structure by overlaying structure on GPS data in memory 
		// This is quicker that moving the GPS data into the GPS290Format structure
        public static T ReadStruct<T>(FileStream fs)
        {
            byte[] buffer = new byte[Marshal.SizeOf(typeof(T))];
            fs.Read(buffer, 0, Marshal.SizeOf(typeof(T)));
            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            T temp = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
            handle.Free();
            return temp;
        }

        public static Boolean SameLocation(GPS290Format curloc, GPS290Format oldloc) 
        {
            return  (curloc.latitude == oldloc.latitude) &&
                 (curloc.longitude == oldloc.longitude);
        }

        public static string FilePath(string FullPathFilename)
        {
            string ApplicationPath = "";
            int FileNameStart = FullPathFilename.LastIndexOf("\\");
            if (FileNameStart > 0)  ApplicationPath = FullPathFilename.Substring(0, FileNameStart + 1);
            return ApplicationPath;
        }

        public static string FileName(string FullPathFilename, string Extension)
        {
            int FileNameStart = FullPathFilename.LastIndexOf("\\");
            if (FileNameStart < 0) FileNameStart = 1;
 
            int FileNameEnd = FullPathFilename.LastIndexOf(".");
            if (FileNameEnd < 0) { FileNameEnd = FullPathFilename.Length; }

            return "RoadTrip_" + FullPathFilename.Substring(FileNameStart + 1, FileNameEnd - FileNameStart - 1) + Extension;
        }



        static void Main(string[] args)
        {
            string gpsfilename = "";
            string kmlfilename = "";
            string inifilename = FilePath(System.Reflection.Assembly.GetExecutingAssembly().Location) + "Roadtrip.ini";
            int NumberGPSFiles = args.Length;
            
            DateTime LocationTime;
            String UTCFormat = "yyyy-MM-ddThh:mm:ssZ";

            DateTime TripStartTime = new DateTime();
            DateTime TripEndTime = new DateTime();
            String TripDateFormat = "dd-mm-yyyy hh:mm:ss";

            DateTime SegmentStartTime = new DateTime();
            DateTime SegmentEndTime = new DateTime();
            String SegmentDateFormat = "hh:mm:ss";

            TimeSpan DateDiff;
            double SecondsDiff;
            
            Config NewConfig = ReadINI(inifilename);
            LineStyle[] SpeedLineStyle = NewConfig.ConfigLineStyle; // ReadINI(inifilename);
            
            // If no GPS filenames were provided, either through the command line or using
            // drag and drop, then use the default GPS filename i.e. there is always 1 GPS 
            // file to process
            if (NumberGPSFiles < 1) NumberGPSFiles = 1;

            for (int iFile = 0; iFile < NumberGPSFiles; iFile++)
            {
                if (args.Length < 1) {
                    gpsfilename = "gps.txt"; // default file if no parameters provided
                } else {
                    gpsfilename = args[iFile];
                }
                long kmlloccount = 0;
                long totalspeedcount = 0;
                double SignalLossTime = 0;

                float totalspeed = 0;
                float topspeed = 0;
                float highestaltitude = 0;
                float lowestaltitude = -1; // Set to -1 so we can overwrite with the first altitude value
                long fileLimit = 0;
                int oldspeedcategory = 1;
                int speedcategory = 1;
                
                float Segmenttotalspeed = 0;
                long Segmentspeedcount = 0;
                float Segmenthighestaltitude = 0;
                float Segmentlowestaltitude = 0;
                float Segmenttopspeed = 0;
                float Segmentlowspeed = 0;
                float GPXLatMin = 0;
                float GPXLatMax = 0;
                float GPXLonMin = 0;
                float GPXLonMax = 0 ;
  
                FileInfo FileKML = null;
                StreamWriter LineKML = null;

                FileInfo FileGPX = null;
                StreamWriter LineGPX = null;

                try
                {
                    
                    FileStream fs = new FileStream(gpsfilename, FileMode.Open, FileAccess.Read);
                    Boolean ignoreloc = false;
                    Boolean forcenewline = false;
                    long fileOffset = 0; // Current position in the file
                    fileLimit = fs.Length / Marshal.SizeOf(typeof(GPS290Format)); // Calculate number of GPS records in the file
                    GPS290Format oldgps = new GPS290Format(); // Used to remember the previous line when comparing if there are duplicate lines in the file
                    GPS290Format gps = ReadStruct<GPS290Format>(fs);                    
                    
                    // Create kml file
                    FileKML = new FileInfo(FilePath(gpsfilename) + FileName(gpsfilename, ".kml"));
                    LineKML = FileKML.CreateText();
                    kmlfilename = FileKML.FullName;
                    LineKML.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    LineKML.WriteLine("<kml creater=\"RoadTrip\" xmlns=\"http://earth.google.com/kml/2.0\">");
                    LineKML.WriteLine("  <Document>");
                    LineKML.WriteLine("    <name>" + FileName(gpsfilename,".kml") + "</name>");                    

                    // Create GPX file
                    FileGPX = new FileInfo(FilePath(gpsfilename) + FileName(gpsfilename, ".gpx"));
                    LineGPX = FileGPX.CreateText();
					LineGPX.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
                    LineGPX.WriteLine("<gpx");
                    LineGPX.WriteLine("   version=\"1.0\"");
                    LineGPX.WriteLine("   creator=\"RoadTrip\"");
                    LineGPX.WriteLine("   xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\"");
                    LineGPX.WriteLine("   xmlns=\"http://www.topografix.com/GPX/1/0\"");
                    LineGPX.WriteLine("   xsi:schemaLocation=\"http://www.topografix.com/GPX/1/0 http://www.topografix.com/GPX/1/0/gpx.xsd\">");                
                    LineGPX.WriteLine("   <trk>");
                    LineGPX.WriteLine("      <trkseg>");
                   
				   for (int i = 0; i < SpeedLineStyle.Length; i++)
                    {
                        // Write header information to degine line styles for the different speed bands
                        LineKML.WriteLine("    <Style id=\"" + SpeedLineStyle[i].Name + "\">");
                        LineKML.WriteLine("		<LineStyle>");
                        LineKML.WriteLine("			<color>" + SpeedLineStyle[i].Colour + "</color>");
                        LineKML.WriteLine("			<width>4</width>");
                        LineKML.WriteLine("		</LineStyle>");
                        LineKML.WriteLine("	 </Style>");
                    }

                    speedcategory = SpeedCategory(gps.speed, SpeedLineStyle);

                    Console.WriteLine("");
                    Console.WriteLine("");
                    Console.WriteLine("RoadTrip");
                    Console.WriteLine("========");
                    Console.WriteLine("");
                    Console.WriteLine("%    Longitude    Latitude    Altitude  Speed");
                    Console.WriteLine("----------------------------------------------");

                    while (fileOffset < fileLimit)
                    {
                        ignoreloc = false;
                        // Ignore lines that have no gps data. This often occurs at start up 
                        // and when gps signal is lost e.g. when going under a bridge 
                        // 
                        // Note: I added the check for year's less than zero (and more than 
                        // two years greater than the previous record as I was getting years 
                        // of 7000). I have had two gps files created by mapview 0.5 that have 
                        // created records of 62 bytes (the last 16 bytes were repeated) near 
                        // the end of the file. Below is the example when the year was 7000ish
                        //  d7 07 09 00 19 00 05 00 07 00 2a 00 00 00 00 00 
                        //  d7 07 09 00 19 00 05 00 07 00 2a 00 00 00 00 00 // looks like this line should be the 4th line not the second
                        //  00 00 80 3f 01 02 00 00 2d 91 13 c2 6e ba 2e 43 
                        //  b8 1e 31 42 00 00 00 00 ec 51 b8 3e 52 38 e7 42 
                        //  00 00 80 3f 01 02 00 00 2d 91 13 c2 6e ba 2e 43 
                        //  b8 1e 31 42 00 00 00 00 ec 51 b8 3e 52 38 e7 42
                        // I believe this is caused by the following scenario.
                        // a) You cannot turn off recording when the GPS has lost a loc on any satellites
                        // b) MapThis wont allow you stop stop recording (left arrow) until 
                        //    change GPS Mode to on (square button)
                        // c) When GPS Mode is back on you then turn off recording.

                        if (gps.latitude == 0 || gps.longitude == 0 || gps.year < 0 || (gps.year > 5000))
                        {
                            ignoreloc = true;
                            gps.latitude = 0;
                            gps.longitude = 0;
                        }
                        else
                        {
                            if (oldgps.latitude != 0 && oldgps.longitude != 0)
                            {
                                // Ignore large jumps as this is probably due to poor signal rather
                                // than supersonic driving abilities. Need to check difference in times between
                                // recordings as large jumps are possible if we haven't been recording for a while
                                // or the supersonic driving continued for even a short lenght of time
                                DateDiff = new DateTime(gps.year, gps.month, gps.day, gps.hour, gps.minute, gps.second) - new DateTime(oldgps.year, oldgps.month, oldgps.day, oldgps.hour, oldgps.minute, oldgps.second);
                                // Sometimes the old and new gps times are the same i.e. difference is less than 1 second                                                                                        
                                
                                if (DateDiff.TotalSeconds == 0)
                                {
                                    SecondsDiff = 1;
                                }
                                else
                                {
                                    SecondsDiff = DateDiff.TotalSeconds;
                                }
                                if ((Math.Abs(gps.latitude - oldgps.latitude) > (NewConfig.LatVariance * SecondsDiff)) ||
                                   (Math.Abs(gps.longitude - oldgps.longitude) > (NewConfig.LonVariance * SecondsDiff)) ||
                                   SecondsDiff < 0)
                                {         
                                    ignoreloc = true;
                                    forcenewline = true;
                                }
                            }
                        }

                        // Ignore the blank gps records where the GPS device
                        // was trying to get a signal. Until a loc is made the
                        // correct data and time is not captured in the file
                        if (!ignoreloc)
                        {
                            
                            // If the current location is the same as the last on then ignore it
                            // this often happens when stuck at traffic lights ;-)
                            if (!SameLocation(gps, oldgps))
                            {
                                // If this is the first line that actually has some location details
                                // then create the header details. My GPS unit takes a while to warm
                                // up and loc on so there may not be any GPS info for quiet a while through the file
                                if (kmlloccount == 0)
                                {
                                    speedcategory = SpeedCategory(gps.speed, SpeedLineStyle);
                                    TripStartTime = new DateTime(gps.year, gps.month, gps.day, gps.hour, gps.minute, gps.second); 
                                    GPXLatMin = gps.latitude;
                                    GPXLatMax = gps.latitude;
                                    GPXLonMin = gps.longitude;
                                    GPXLonMax = gps.longitude;
                                }

								// Start a new line string after GPS signal loss, 
                                // so that a straight line is not drawn directly between
                                // where the signal was lost and then regained,
                                // or when the speed band changes. Also, change line colours if
                                // the speed drops or goes above the current speed band
                                if (oldgps.latitude == 0 || oldspeedcategory != speedcategory || forcenewline)
                                {                                    
                                    if (kmlloccount >= 1)
                                    {
                                        SegmentEndTime = new DateTime(gps.year, gps.month, gps.day, gps.hour, gps.minute, gps.second);
                                        DateDiff = SegmentEndTime - SegmentStartTime;
                                        if (DateDiff.TotalSeconds > 80) {
                                            ignoreloc = ignoreloc;
                                    }
                                        SpeedLineStyle[oldspeedcategory].TotalSecondsAdd(DateDiff.TotalSeconds);

                                        LineKML.WriteLine("        </coordinates>");
                                        LineKML.WriteLine("      </LineString>");
                                        LineKML.WriteLine("      </MultiGeometry>");
                                        LineKML.WriteLine("      <description><![CDATA[");
                                        //LineKML.WriteLine("Start time:       " + SegmentStartTime.ToString(SegmentDateFormat));
                                        LineKML.WriteLine("Time:       " + SegmentStartTime.ToString(SegmentDateFormat) + " - " + SegmentEndTime.ToString(SegmentDateFormat));
                                        LineKML.WriteLine("<BR>Traveling Time:    " + DateDiff.TotalSeconds + " seconds");
                                        LineKML.WriteLine("<BR>Speed:       " + Segmentlowspeed.ToString("f2") + " - " + Segmenttopspeed.ToString("f2"));
                                        LineKML.WriteLine("<BR> Average Speed:       " + (Segmenttotalspeed / Segmentspeedcount).ToString("f2"));
                                        LineKML.WriteLine("<BR>Altitude:       " + Segmentlowestaltitude + " - " + Segmenthighestaltitude);
                                        LineKML.WriteLine("      ]]></description>");
                                        LineKML.WriteLine("    </Placemark>");

                                    }
                                    totalspeed += Segmenttotalspeed;
                                    totalspeedcount += Segmentspeedcount;
                                    if (Segmenthighestaltitude > highestaltitude) highestaltitude = Segmenthighestaltitude;
                                    if (lowestaltitude == -1 || (Segmentlowestaltitude < lowestaltitude)) lowestaltitude = Segmentlowestaltitude;
                                    if (Segmenttopspeed > topspeed) topspeed = Segmenttopspeed;
                                    
                                    Segmenttotalspeed = 0;
                                    Segmentspeedcount = 0;
                                    Segmenthighestaltitude = gps.altitude;
                                    Segmentlowestaltitude = gps.altitude;
                                    Segmenttopspeed = gps.speed;
                                    Segmentlowspeed = gps.speed;
                                    SegmentStartTime = new DateTime(gps.year, gps.month, gps.day, gps.hour, gps.minute, gps.second);
                                    TripEndTime = new DateTime(gps.year, gps.month, gps.day, gps.hour, gps.minute, gps.second);
                                    
                                    LineKML.WriteLine("    <Placemark>");
                                    LineKML.WriteLine("      <name><![CDATA[<span style=\"color:#" + SpeedLineStyle[speedcategory].FontColour + "\">" + SpeedLineStyle[speedcategory].Description + "</span>]]></name>");
                                    LineKML.WriteLine("      <styleUrl>#" + SpeedLineStyle[speedcategory].Name + "</styleUrl> ");
                                    LineKML.WriteLine("      <MultiGeometry>");
                                    LineKML.WriteLine("      <LineString>");
                                    LineKML.WriteLine("        <extrude>0</extrude>");
                                    LineKML.WriteLine("        <tessellate>1</tessellate>");
                                    LineKML.WriteLine("        <altitudeMode>clampedToGround</altitudeMode>");
                                    LineKML.WriteLine("        <coordinates>");

                                    // Assumes that when latitude and longitude are both 0 then the there is no GPS recording. 
                                    if (oldgps.latitude != 0 && oldgps.longitude != 0)
                                    {
                                        // Do not join this new line with the old line if there was a large jump between the points
                                        if (!forcenewline)
                                        {
                                            LineKML.WriteLine("         " + Convert.ToDouble(oldgps.longitude).ToString("f6") + "," +
                                           Convert.ToDouble(oldgps.latitude).ToString("f6") + "," + oldgps.altitude.ToString());
                                        }
                                        forcenewline = false;
                                        // Create a new segment every minute. GoogleEarth doesn't show anything if there is only
                                        // one trkpt per segment for some reason
                                        if (oldgps.minute != gps.minute)
                                        {
                                            LineGPX.WriteLine("      </trkseg>");
                                            LineGPX.WriteLine("   </trk>");
                                            LineGPX.WriteLine("   <trk>");
                                            LineGPX.WriteLine("      <trkseg>");
                                        }
                                        LocationTime = new DateTime(oldgps.year, oldgps.month, oldgps.day, oldgps.hour, oldgps.minute, oldgps.second);
                                        LineGPX.WriteLine("         <trkpt lat=\"" + Convert.ToDouble(oldgps.latitude).ToString("f6") + "\" lon=\"" + Convert.ToDouble(oldgps.longitude).ToString("f6") + "\">");
                                        //LineGPX.WriteLine("            <name>" + LocationTime.ToString(UTCFormat) + "</name>");
                                        //LineGPX.WriteLine("            <type>" + SpeedLineStyle[speedcategory].Description + "</type>");
                                        LineGPX.WriteLine("            <time>" + LocationTime.ToString(UTCFormat) + "</time>");
                                        LineGPX.WriteLine("            <speed>" + oldgps.speed.ToString() + "</speed>");
                                        LineGPX.WriteLine("            <ele>" + oldgps.altitude.ToString() + "</ele>");
                                        LineGPX.WriteLine("         </trkpt>");
                                    }
                                }
                                
                                kmlloccount++;
                                    
                                Segmenttotalspeed += gps.speed;
                                Segmentspeedcount++;
                                if (gps.altitude > Segmenthighestaltitude) Segmenthighestaltitude = gps.altitude;
                                if (lowestaltitude == -1 || (gps.altitude < Segmentlowestaltitude)) Segmentlowestaltitude = gps.altitude;
                                if (gps.speed > Segmenttopspeed) Segmenttopspeed = gps.speed;
                                if (gps.speed < Segmentlowspeed) Segmentlowspeed = gps.speed;
                                SegmentEndTime = new DateTime(gps.year, gps.month, gps.day, gps.hour, gps.minute, gps.second); 
                                
                                LineKML.WriteLine("         " + Convert.ToDouble(gps.longitude).ToString("f6") + "," + 
                                       Convert.ToDouble(gps.latitude).ToString("f6") + "," + gps.altitude.ToString());
                                
                                // Used to define border in GPX file
                                if ((gps.latitude != 0) && (gps.longitude != 0)) {
                                    if (GPXLatMin > gps.latitude) GPXLatMin = gps.latitude;
                                    if (GPXLatMax < gps.latitude) GPXLatMax = gps.latitude;
                                    if (GPXLonMin > gps.longitude) GPXLonMin = gps.longitude;
                                    if (GPXLonMax < gps.longitude) GPXLonMax = gps.longitude;
                                }
                                // Create a new segment every minute. GoogleEarth doesn't show anything if there is only
                                // one trkpt per segment for some reason
                                if (oldgps.minute != gps.minute) {
                                    LineGPX.WriteLine("      </trkseg>");
                                    LineGPX.WriteLine("   </trk>");
                                    LineGPX.WriteLine("   <trk>");
                                    LineGPX.WriteLine("      <trkseg>");
                                }
                                LocationTime = new DateTime(gps.year, gps.month, gps.day, gps.hour, gps.minute, gps.second);
                                LineGPX.WriteLine("         <trkpt lat=\"" + Convert.ToDouble(gps.latitude).ToString("f6") + "\" lon=\"" + Convert.ToDouble(gps.longitude).ToString("f6") + "\">");
                                //LineGPX.WriteLine("            <name>" + LocationTime.ToString(UTCFormat) + "</name>");
                                //LineGPX.WriteLine("            <type>" + SpeedLineStyle[speedcategory].Description + "</type>");
                                LineGPX.WriteLine("            <time>" + LocationTime.ToString(UTCFormat) + "</time>");
                                LineGPX.WriteLine("            <speed>" + gps.speed.ToString() + "</speed>");
                                LineGPX.WriteLine("            <ele>" + gps.altitude.ToString() + "</ele>");
                                LineGPX.WriteLine("         </trkpt>");

                                // Show the current processed record to the user
                                Console.WriteLine((fileOffset * 1.00 / fileLimit * 100).ToString("f0").PadLeft(3) + "% " +
                                    Convert.ToDouble(gps.longitude).ToString("f6") + ",  " +
                                    Convert.ToDouble(gps.latitude).ToString("f6") + ", " +
                                    gps.altitude.ToString("f2") + ",    " + gps.speed.ToString("00.000") + ",    " );
                            }
                            totalspeed += gps.speed;
                            totalspeedcount++;

                            oldgps = gps;
                            oldspeedcategory = speedcategory;
                        }
                        gps = ReadStruct<GPS290Format>(fs);
                        speedcategory = SpeedCategory(gps.speed, SpeedLineStyle);
                        fileOffset++;

                    }
                    fs.Close();

                    Console.WriteLine("----------------------------------------------");
                    Console.WriteLine("%    Longitude    Latitude    Altitude  Speed");


                    // Include some interesting statics on the trip
                    if (kmlloccount > 0)
                    {
                        totalspeed += Segmenttotalspeed;
                        totalspeedcount += Segmentspeedcount;
                        if (Segmenthighestaltitude > highestaltitude) highestaltitude = Segmenthighestaltitude;
                        if (lowestaltitude == -1 || (Segmentlowestaltitude < lowestaltitude)) lowestaltitude = Segmentlowestaltitude;
                        if (Segmenttopspeed > topspeed) topspeed = Segmenttopspeed;

                        DateDiff = SegmentEndTime - SegmentStartTime;
                                        
                        LineKML.WriteLine("        </coordinates>");
                        LineKML.WriteLine("      </LineString>");
                        LineKML.WriteLine("      </MultiGeometry>");
                        LineKML.WriteLine("      <description><![CDATA[");
                        LineKML.WriteLine("Start time:       " + SegmentStartTime.ToString(SegmentDateFormat));
                        LineKML.WriteLine("<BR>Traveling Time:    " + DateDiff.TotalSeconds + " seconds");
                        LineKML.WriteLine("<BR>Speed:       " + Segmentlowspeed.ToString("f2") + " - " + Segmenttopspeed.ToString("f2"));
                        LineKML.WriteLine("<BR> Average Speed:       " + (Segmenttotalspeed / Segmentspeedcount).ToString("f2"));
                        LineKML.WriteLine("<BR>Altitude:       " + Segmentlowestaltitude + " - " + Segmenthighestaltitude);
                        LineKML.WriteLine("      ]]></description>");
                        LineKML.WriteLine("    </Placemark>");
                    }
                    DateDiff = TripEndTime - TripStartTime;
                    LineKML.WriteLine("    <description>");
                    LineKML.WriteLine("    <![CDATA[");
                    LineKML.WriteLine("    MapThis! gps file converted by RoadTrip.");
                    LineKML.WriteLine("<TABLE cellspacing=0 cellpadding=0>");
                    LineKML.WriteLine("<TR><TD>Trip start time:</TD><TD align=right colspan=\"2\">" + TripStartTime.ToString("r") + "</TD></TR>");
                    LineKML.WriteLine("<TR><TD>Trip end time:</TD><TD align=right colspan=\"2\">" + TripEndTime.ToString("r") + "</TD></TR>");
                    LineKML.WriteLine("<TR><TD>Traveling time:</TD><TD align=right colspan=\"2\">" + DateDiff.TotalMinutes.ToString("0,0") + " minutes</TD></TR>");
                    LineKML.WriteLine("<TR><TD></TD><TD colspan=\"2\"></TD></TR>");
                    LineKML.WriteLine("<TR><TD>Speed Category</TD><TD align=right>Traveling Time (mins)</TD><TD align=right>% of Trip</TD></TR>");
                    for (int i = 0; i <= SpeedLineStyle.Length-1; i++)
                    {
                        LineKML.WriteLine("<TR><TD>&nbsp;&nbsp;&nbsp;" + SpeedLineStyle[i].Description + "</TD><TD align=right>" + Convert.ToDouble(SpeedLineStyle[i].TotalSecondsView() / 60).ToString("0.0") + "</TD><TD align=right>" + Convert.ToDouble((SpeedLineStyle[i].TotalSecondsView() / DateDiff.TotalSeconds)).ToString("0.0%") + "</TD></TR>");
                        SpeedLineStyle[i].TotalSecondsReset();
                    }
                    LineKML.WriteLine("<TR><TD>&nbsp;&nbsp;&nbsp;Signal Loss</TD><TD>" + "</TD><TD>" + "</TD></TR>");

                    LineKML.WriteLine("<TR><TD></TD><TD colspan=\"2\"></TD></TR>");
                    LineKML.WriteLine("<TR><TD>Top Speed:</TD><TD align=right colspan=\"2\">" + topspeed.ToString("f2") + "</TD></TR>");
                    LineKML.WriteLine("<TR><TD>Average Speed:</TD><TD align=right colspan=\"2\">" + (totalspeed / totalspeedcount).ToString("f2") + "</TD></TR>");

                    LineKML.WriteLine("<TR><TD></TD><TD colspan=\"2\"></TD></TR>");
                    LineKML.WriteLine("<TR><TD>Highest Altitude:</TD><TD align=right colspan=\"2\">" + highestaltitude + "</TD></TR>");
                    LineKML.WriteLine("<TR><TD>Lowest Altitude:</TD><TD align=right colspan=\"2\">" + lowestaltitude + "</TD></TR>");
                    LineKML.WriteLine("</TABLE><BR>   <I>Brought to you by RJ</I>");
                    LineKML.WriteLine("    ]]>");
                    LineKML.WriteLine("  </description>");
                    LineKML.WriteLine("  </Document>");
                    LineKML.WriteLine("</kml>");
                    LineKML.Close();

                    LineGPX.WriteLine("      </trkseg>");
                    LineGPX.WriteLine("   </trk>");
                    LineGPX.WriteLine("   <bounds minlat=\"" + GPXLatMin + "\" minlon=\"" + GPXLonMin + "\"" + 
                        " maxlat=\"" + GPXLatMax + "\" maxlon=\"" + GPXLonMax + "\"/>");

                    LineGPX.WriteLine("</gpx>");
                    LineGPX.Close();

                }
                catch (Exception e)
                {
                    gpsfilename = "ERROR - " + e.Message;
                    kmlfilename = FILENOTCREATED;

                }
                Console.WriteLine("");
                Console.WriteLine("");
                Console.WriteLine("RoadTrip");
                Console.WriteLine("========");
                Console.WriteLine("By RJ, New Zealand.  31-Jan-2008 Version 0.6");
                Console.WriteLine("");
                Console.WriteLine("RoadTrip converts MapThis! PSP290 format gps.txt file to Google's KML format.");
                Console.WriteLine("o Mapthis! deniska.dcemu.co.uk");
                Console.WriteLine("o GoogleEarth earth.google.com");
                Console.WriteLine("");
                if (kmlfilename == FILENOTCREATED)
                {
                    Console.WriteLine("usage: ROADTRIP [GPS filename 1], [GPS filename 2], [GPS filename n] ");
                    Console.WriteLine("  or drag and drop a single or mulitple GPS files onto RoadTrip.exe");
                    Console.WriteLine("");
                    Console.WriteLine("");
                }
                
				//   Console.WriteLine("Ini file name:        " + inifilename);
                Console.WriteLine("Input file name:        " + gpsfilename);
                Console.WriteLine("Input location points:  " + fileLimit);
                Console.WriteLine("Output file name:       " + kmlfilename);
                Console.WriteLine("Config file name:       " + inifilename);
                if (kmlfilename != FILENOTCREATED)
                {
                    Console.WriteLine("Output location points: " + kmlloccount);
                    Console.WriteLine("Trip start time:        " + TripStartTime.ToString(TripDateFormat));
                    Console.WriteLine("Trip end time:          " + TripEndTime.ToString(TripDateFormat));
                    Console.WriteLine("Top Speed:              " + (topspeed).ToString("f2"));
                    Console.WriteLine("Average Speed:          " + (totalspeed / totalspeedcount).ToString("f2"));
                    Console.WriteLine("Highest Altitude:       " + highestaltitude);
                    Console.WriteLine("Lowest Altitude:        " + lowestaltitude);
                }
                Console.WriteLine("");
                Console.WriteLine("");
            }
            Console.WriteLine("Press any key to continue");
            Console.ReadKey();
        }
    }
}


