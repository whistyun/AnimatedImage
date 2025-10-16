using AnimatedImage.Formats.WebP;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace AnimatedImage
{
    /// <summary>
    /// The utility class for locating and loading native libraries used by AnimatedImage.
    /// </summary>
    public static class NativeLibsLocator
    {
        private static bool _tooLate;

        private static string? _RIDDirectory;
        private static string? _ext;

        /// <summary>
        /// Gets the directory path for the native libraries specific to the current Runtime Identifier (RID).
        /// </summary>
        public static string RIDNativeDirectory => _RIDDirectory ??= SolveRIDNativeDirectory();

        /// <summary>
        /// Gets the directory path for loading native library. 
        /// If null, <see cref="RIDNativeDirectory"/> and <see cref="AppContext.BaseDirectory"/> are used.
        /// </summary>
        public static string? NativeLibraryPath { get; private set; }

        /// <summary>
        /// Sets the directory path for loading native library. 
        /// If null, <see cref="RIDNativeDirectory"/> and <see cref="AppContext.BaseDirectory"/> are used instead.
        /// </summary>
        /// <exception cref="InvalidOperationException">
        /// If native libraries have already been loaded.
        /// </exception>
        public static void SetNativeLibraryPath(string llibraryPath)
        {
            if (_tooLate)
                throw new InvalidOperationException("Native libraries already loaded.");

            NativeLibraryPath = llibraryPath;
        }

        private static string Ext => _ext ??= SolveExt();

        private static string SolveRIDNativeDirectory()
        {
            var baseDir = AppContext.BaseDirectory;
            if (string.IsNullOrEmpty(baseDir))
                return "";


#if NETCOREAPP
            var osname = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
                         RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
                         RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" :
                         string.Empty;

            if (string.IsNullOrEmpty(osname))
            {
                Debug.Print("AnimatedImage: unsupport platform " + RuntimeInformation.OSDescription);
                return "";
            }

            var arch = RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(osname))
            {
                Debug.Print("AnimatedImage: unsupport architecture " + RuntimeInformation.ProcessArchitecture);
                return "";
            }

            var relpath = $"runtimes/{osname}-{arch}/native";
#elif NET472_OR_GREATER
            var relpath =  RuntimeInformation.ProcessArchitecture switch
            {
                Architecture.X86 => "x86",
                Architecture.X64 => "x64",
                Architecture.Arm64 => "arm64",
                _ => string.Empty
            };
#elif NETFRAMEWORK
            var relpath =  Environment.Is64BitProcess ?
                        "x64" :
                        "x86";
#endif

            var libdir = Path.Combine(baseDir, relpath);
            return Directory.Exists(libdir) ? libdir : "";
        }

        private static string SolveExt()
        {
#if NETCOREAPP || NET472_OR_GREATER
            return RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".dll" :
                   RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? ".so" :
                   RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? ".dylib" :
                   string.Empty;
#else
            return ".dll";
#endif
        }

        internal static bool TryLoad(params string[] libraryNames)
        {
            _tooLate = true;

            var nativeLibraryPath = NativeLibraryPath;
            if (!string.IsNullOrEmpty(nativeLibraryPath))
            {
                // solve specified path
                return TryLoad(nativeLibraryPath, libraryNames);
            }
            else
            {
                var basedir = AppContext.BaseDirectory;
                var ridndir = RIDNativeDirectory;

                return (ridndir != "" && TryLoad(ridndir, libraryNames))
                    || (basedir != "" && TryLoad(basedir, libraryNames));
            }
        }

        private static bool TryLoad(string basedir, params string[] libraryNames)
        {
            foreach (var libraryName in libraryNames)
            {
                var libpath = Directory.GetFiles(basedir, libraryName + ".*")
                                       .Where(f => Path.GetExtension(f) == Ext)
                                       .OrderBy(s => s.Length)
                                       .FirstOrDefault();

                if (libpath is null)
                {
                    Debug.Print($"AnimatedImage: {libraryName} not founds. Please add AnimatedImage.Native");
                    return false;
                }

                if (!PrivateTryLoad(libpath))
                {
                    Debug.Print($"AnimatedImage: {libraryName} load failed.");
                    return false;
                }
            }

            return true;
        }

#if NETCOREAPP
        private static bool PrivateTryLoad(string dllPath)
        {
            try
            {
                NativeLibrary.Load(dllPath);
                return true;
            }
            catch
            {
                if (Debugger.IsAttached) throw;
                return false;
            }
        }
#elif NETFRAMEWORK
        private static bool PrivateTryLoad(string dllPath)
        {
            if (LoadLibrary(Path.GetFullPath(dllPath)) == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();

                var errMsg = string.Format("Failed to load library (ErrorCode: {0})", errorCode);
                if (Debugger.IsAttached)
                {
                    throw new Exception(errMsg);
                }
                return false;
            }
            return true;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string libname);
#endif
    }
}
