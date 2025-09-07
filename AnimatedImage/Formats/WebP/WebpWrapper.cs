using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace AnimatedImage.Formats.WebP
{
    internal class WebpWrapper
    {
        #region value & structure

        /// <summary>Describes the byte-ordering of packed samples in memory</summary>
        public enum WEBP_CSP_MODE
        {
            /// <summary>Byte-order: R,G,B,R,G,B,..</summary>
            MODE_RGB = 0,
            /// <summary>Byte-order: R,G,B,A,R,G,B,A,..</summary>
            MODE_RGBA = 1,
            /// <summary>Byte-order: B,G,R,B,G,R,..</summary>
            MODE_BGR = 2,
            /// <summary>Byte-order: B,G,R,A,B,G,R,A,..</summary>
            MODE_BGRA = 3,
            /// <summary>Byte-order: A,R,G,B,A,R,G,B,..</summary>
            MODE_ARGB = 4,
            /// <summary>Byte-order: RGB-565: [a4 a3 a2 a1 a0 r5 r4 r3], [r2 r1 r0 g4 g3 g2 g1 g0], ...
            /// WEBP_SWAP_16BITS_CSP is defined, 
            /// Byte-order: RGB-565: [a4 a3 a2 a1 a0 b5 b4 b3], [b2 b1 b0 g4 g3 g2 g1 g0], ..</summary>
            MODE_RGBA_4444 = 5,
            /// <summary>Byte-order: RGB-565: [r4 r3 r2 r1 r0 g5 g4 g3], [g2 g1 g0 b4 b3 b2 b1 b0], ...
            /// WEBP_SWAP_16BITS_CSP is defined, 
            /// Byte-order: [b3 b2 b1 b0 a3 a2 a1 a0], [r3 r2 r1 r0 g3 g2 g1 g0], ..</summary>
            MODE_RGB_565 = 6,
            /// <summary>RGB-premultiplied transparent modes (alpha value is preserved)</summary>
            MODE_rgbA = 7,
            /// <summary>RGB-premultiplied transparent modes (alpha value is preserved)</summary>
            MODE_bgrA = 8,
            /// <summary>RGB-premultiplied transparent modes (alpha value is preserved)</summary>
            MODE_Argb = 9,
            /// <summary>RGB-premultiplied transparent modes (alpha value is preserved)</summary>
            MODE_rgbA_4444 = 10,
            /// <summary>YUV 4:2:0</summary>
            MODE_YUV = 11,
            /// <summary>YUV 4:2:0</summary>
            MODE_YUVA = 12,
            /// <summary>MODE_LAST -> 13</summary>
            MODE_LAST = 13,
        }

        /// <summary>Anim decoder options</summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WebPAnimDecoderOptions
        {
            /// <summary>Output colorspace. Only the following modes are supported:
            /// MODE_RGBA, MODE_BGRA, MODE_rgbA and MODE_bgrA.</summary>
            public WEBP_CSP_MODE color_mode;
            /// <summary>If true, use multi-threaded decoding</summary>
            public int use_threads;
            /// <summary>Padding for later use</summary>
            private readonly UInt32 pad1;
            /// <summary>Padding for later use</summary>
            private readonly UInt32 pad2;
            /// <summary>Padding for later use</summary>
            private readonly UInt32 pad3;
            /// <summary>Padding for later use</summary>
            private readonly UInt32 pad4;
            /// <summary>Padding for later use</summary>
            private readonly UInt32 pad5;
            /// <summary>Padding for later use</summary>
            private readonly UInt32 pad6;
            /// <summary>Padding for later use</summary>
            private readonly UInt32 pad7;
        };

        /// <summary>
        /// Data type used to describe 'raw' data, e.g., chunk data
        /// (ICC profile, metadata) and WebP compressed image data.
        /// 'bytes' memory must be allocated using WebPMalloc() and such.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WebPData
        {
            public IntPtr data;
            public UIntPtr size;
        }

        /// <summary>Main opaque object.</summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WebPAnimDecoder
        {
            public IntPtr decoder;
        }

        /// <summary>Global information about the animation</summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WebPAnimInfo
        {
            public UInt32 canvas_width;
            public UInt32 canvas_height;
            public UInt32 loop_count;
            public UInt32 bgcolor;
            public UInt32 frame_count;
            /// <summary>Padding for later use</summary>
            private readonly UInt32 pad1;
            /// <summary>Padding for later use</summary>
            private readonly UInt32 pad2;
            /// <summary>Padding for later use</summary>
            private readonly UInt32 pad3;
            /// <summary>Padding for later use</summary>
            private readonly UInt32 pad4;
        }

        private static readonly int WEBP_DEMUX_ABI_VERSION = 0x0107;

        /// <summary>
        ///  Frame iteration.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct WebPIterator
        {
            public Int32 frame_num;
            public Int32 num_frames;                     // equivalent to WEBP_FF_FRAME_COUNT.
            public Int32 x_offset, y_offset;             // offset relative to the canvas.
            public Int32 width, height;                  // dimensions of this frame.
            public Int32 duration;                       // display duration in milliseconds.
            public Int32 dispose_method;    // dispose method for the frame.
            public Int32 complete;          // true if 'fragment' contains a full frame. partial images
                                            // may still be decoded with the WebP incremental decoder.
            WebPData fragment;              // The frame given by 'frame_num'. Note for historical
                                            // reasons this is called a fragment.
            public Int32 has_alpha;         // True if the frame contains transparency.
            public Int32 blend_method;      // Blend operation for the frame.

            /// <summary>Padding for later use</summary>
            private readonly UInt32 pad1;
            /// <summary>Padding for later use</summary>
            private readonly UInt32 pad2;

            private IntPtr private_;   // for internal use only.
        };

        #endregion

        #region initialize

        static WebpWrapper()
        {
            var asm = Assembly.GetCallingAssembly();
            var dllDir = Path.GetDirectoryName(asm.Location)!;

#if NETFRAMEWORK
            if (Environment.Is64BitProcess)
            {
                Load(Path.Combine(dllDir, "runtimes/win-x64/native/libsharpyuv.dll"));
                Load(Path.Combine(dllDir, "runtimes/win-x64/native/libwebp.dll"));
                Load(Path.Combine(dllDir, "runtimes/win-x64/native/libwebpdemux.dll"));
            }
            else
            {
                Load(Path.Combine(dllDir, "runtimes/win-x86/native/libsharpyuv.dll"));
                Load(Path.Combine(dllDir, "runtimes/win-x86/native/libwebp.dll"));
                Load(Path.Combine(dllDir, "runtimes/win-x86/native/libwebpdemux.dll"));
            }
#elif NETCOREAPP
            NativeLibrary.SetDllImportResolver(
                asm,
                (libnm, requestingAsm, path) =>
                {
                    if (libnm != "libwebp" && libnm != "libwebpdemux")
                    {
                        return IntPtr.Zero;
                    }
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                    {
                        return Environment.Is64BitProcess ?
                            NativeLibrary.Load(Path.Combine(dllDir, $"runtimes/win-x64/native/{libnm}.dll")) :
                            NativeLibrary.Load(Path.Combine(dllDir, $"runtimes/win-x86/native/{libnm}.dll"));
                    }
                    if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                    {
                        return Environment.Is64BitProcess ?
                            NativeLibrary.Load(Path.Combine(dllDir, $"runtimes/linux-x64/native/{libnm}.dll")) :
                            IntPtr.Zero;
                    }
                    return IntPtr.Zero;
                });
#endif
        }

#if NETFRAMEWORK
        private static void Load(string dllpath)
        {
            IntPtr Handle = LoadLibrary(Path.GetFullPath(dllpath));
            if (Handle == IntPtr.Zero)
            {
                int errorCode = Marshal.GetLastWin32Error();
                throw new Exception(string.Format("Failed to load library (ErrorCode: {0})", errorCode));
            }
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string libname);
#endif

        #endregion

        /// <summary>Should always be called, to initialize a fresh WebPAnimDecoderOptions
        /// structure before modification. Returns false in case of version mismatch.
        /// WebPAnimDecoderOptionsInit() must have succeeded before using the
        /// 'dec_options' object.</summary>
        /// <param name="dec_options">(in/out) options used for decoding animation</param>
        /// <returns>true/false - success/error</returns>
        internal static bool WebPAnimDecoderOptionsInit(ref WebPAnimDecoderOptions dec_options)
        {
            return WebPAnimDecoderOptionsInitInternal(ref dec_options, WEBP_DEMUX_ABI_VERSION) == 1;
        }
        //[DllImport("libwebpdemux.dll", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderOptionsInitInternal")]
        //private static extern int WebPAnimDecoderOptionsInitInternal_x86(ref WebPAnimDecoderOptions dec_options, int WEBP_DEMUX_ABI_VERSION);
        [DllImport("libwebpdemux", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderOptionsInitInternal")]
        private static extern int WebPAnimDecoderOptionsInitInternal(ref WebPAnimDecoderOptions dec_options, int WEBP_DEMUX_ABI_VERSION);


        /// <summary>
        /// Creates and initializes a WebPAnimDecoder object.
        /// </summary>
        /// <param name="webp_data">(in) WebP bitstream. This should remain unchanged during the 
        ///     lifetime of the output WebPAnimDecoder object.</param>
        /// <param name="dec_options">(in) decoding options. Can be passed NULL to choose 
        ///     reasonable defaults (in particular, color mode MODE_RGBA 
        ///     will be picked).</param>
        /// <returns>A pointer to the newly created WebPAnimDecoder object, or NULL in case of
        ///     parsing error, invalid option or memory error.</returns>
        internal static WebPAnimDecoder WebPAnimDecoderNew(ref WebPData webp_data, ref WebPAnimDecoderOptions dec_options)
        {
            IntPtr ptr = WebPAnimDecoderNewInternal(ref webp_data, ref dec_options, WEBP_DEMUX_ABI_VERSION);
            WebPAnimDecoder decoder = new WebPAnimDecoder() { decoder = ptr };
            return decoder;
        }
        [DllImport("libwebpdemux", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderNewInternal")]
        private static extern IntPtr WebPAnimDecoderNewInternal(ref WebPData webp_data, ref WebPAnimDecoderOptions dec_options, int WEBP_DEMUX_ABI_VERSION);


        /// <summary>Get global information about the animation.</summary>
        /// <param name="dec">(in) decoder instance to get information from.</param>
        /// <param name="info">(out) global information fetched from the animation.</param>
        /// <returns>True on success.</returns>
        internal static bool WebPAnimDecoderGetInfo(IntPtr dec, out WebPAnimInfo info)
        {
            return WebPAnimDecoderGetInfoInternal(dec, out info) == 1;
        }
        [DllImport("libwebpdemux", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderGetInfo")]
        private static extern int WebPAnimDecoderGetInfoInternal(IntPtr dec, out WebPAnimInfo info);

        /// <summary>Check if there are more frames left to decode.</summary>
        /// <param name="dec">(in) decoder instance to be checked.</param>
        /// <returns>
        /// True if 'dec' is not NULL and some frames are yet to be decoded.
        /// Otherwise, returns false.
        /// </returns>
        internal static bool WebPAnimDecoderHasMoreFrames(WebPAnimDecoder dec)
        {
            return WebPAnimDecoderHasMoreFramesInternal(dec) == 1;
        }
        [DllImport("libwebpdemux", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderHasMoreFrames")]
        private static extern int WebPAnimDecoderHasMoreFramesInternal(WebPAnimDecoder dec);


        /// <summary>
        /// Fetch the next frame from 'dec' based on options supplied to
        /// WebPAnimDecoderNew(). This will be a fully reconstructed canvas of size
        /// 'canvas_width * 4 * canvas_height', and not just the frame sub-rectangle. The
        /// returned buffer 'buf' is valid only until the next call to
        /// WebPAnimDecoderGetNext(), WebPAnimDecoderReset() or WebPAnimDecoderDelete().
        /// </summary>
        /// <param name="dec">(in/out) decoder instance from which the next frame is to be fetched.</param>
        /// <param name="buf">(out) decoded frame.</param>
        /// <param name="timestamp">(out) timestamp of the frame in milliseconds.</param>
        /// <returns>
        /// False if any of the arguments are NULL, or if there is a parsing or
        /// decoding error, or if there are no more frames. Otherwise, returns true.
        /// </returns>
        internal static bool WebPAnimDecoderGetNext(IntPtr dec, ref IntPtr buf, ref int timestamp)
        {
            return WebPAnimDecoderGetNextInternal(dec, ref buf, ref timestamp) == 1;
        }
        [DllImport("libwebpdemux", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderGetNext")]
        private static extern int WebPAnimDecoderGetNextInternal(IntPtr dec, ref IntPtr buf, ref int timestamp);

        /// <summary>
        /// Resets the WebPAnimDecoder object, so that next call to
        /// WebPAnimDecoderGetNext() will restart decoding from 1st frame. This would be
        /// helpful when all frames need to be decoded multiple times (e.g.
        /// info.loop_count times) without destroying and recreating the 'dec' object.
        /// </summary>
        /// <param name="dec">(in/out) decoder instance to be reset</param>
        internal static void WebPAnimDecoderReset(WebPAnimDecoder dec)
        {
            WebPAnimDecoderResetInternal(dec);
        }
        [DllImport("libwebpdemux", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderReset")]
        private static extern void WebPAnimDecoderResetInternal(WebPAnimDecoder dec);


        /// <summary>Deletes the WebPAnimDecoder object.</summary>
        /// <param name="decoder">(in/out) decoder instance to be deleted</param>
        internal static void WebPAnimDecoderDelete(WebPAnimDecoder decoder)
        {
            WebPAnimDecoderDeleteInternal(decoder);
        }
        [DllImport("libwebpdemux", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderDelete")]
        private static extern void WebPAnimDecoderDeleteInternal(WebPAnimDecoder dec);

        /// <summary>
        /// Grab the internal demuxer object.
        ///  Getting the demuxer object can be useful if one wants to use operations only
        ///  available through demuxer; e.g. to get XMP/EXIF/ICC metadata. The returned
        ///  demuxer object is owned by 'dec' and is valid only until the next call to
        ///  WebPAnimDecoderDelete().
        /// </summary>
        /// <param name="dec">(in) decoder instance from which the demuxer object is to be fetched.</param>
        /// <returns></returns>
        [DllImport("libwebpdemux", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPAnimDecoderGetDemuxer")]
        public static extern IntPtr WebPAnimDecoderGetDemuxer(WebPAnimDecoder dec);

        /// <summary>
        ///  Retrieves frame 'frame_number' from 'dmux'.
        /// 'iter->fragment' points to the frame on return from this function.
        /// Setting 'frame_number' equal to 0 will return the last frame of the image.
        /// 
        /// Call WebPDemuxReleaseIterator() when use of the iterator is complete.
        /// NOTE: 'dmux' must persist for the lifetime of 'iter'.
        /// </summary>
        /// <param name="dmux"></param>
        /// <param name="frame_number"></param>
        /// <param name="iter"></param>
        /// <returns>return false if 'dmux' is NULL or frame 'frame_number' is not present.</returns>
        internal static bool WebPDemuxGetFrame(IntPtr dmux, Int32 frame_number, ref WebPIterator iter)
        {
            return WebPDemuxGetFrameInternal(dmux, frame_number, ref iter) != 0;
        }
        /// 
        [DllImport("libwebpdemux", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPDemuxGetFrame")]
        public static extern int WebPDemuxGetFrameInternal(IntPtr dmux, Int32 frame_number, ref WebPIterator iter);

        /// <summary>
        /// Sets 'iter->fragment' to point to the next ('iter->frame_num' + 1) or
        /// previous ('iter->frame_num' - 1) frame. These functions do not loop.
        /// </summary>
        /// <param name="iter"></param>
        /// <returns>Returns true on success, false otherwise.</returns>
        internal static bool WebPDemuxNextFrame(ref WebPIterator iter)
        {
            return WebPDemuxNextFrameInternal(ref iter) == 1;
        }

        [DllImport("libwebpdemux", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPDemuxNextFrame")]
        private static extern int WebPDemuxNextFrameInternal(ref WebPIterator iter);


        /// <summary>
        /// Releases any memory associated with 'iter'.
        /// Must be called before any subsequent calls to WebPDemuxGetChunk() on the same
        /// iter. Also, must be called before destroying the associated WebPDemuxer with
        /// WebPDemuxDelete().
        /// </summary>
        /// <param name="iter"></param>
        [DllImport("libwebpdemux", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPDemuxReleaseIterator")]
        public static extern void WebPDemuxReleaseIterator(ref WebPIterator iter);

        internal static IntPtr WebPMalloc(int size)
        {
            return WebPMalloc_x64(size);
        }
        [DllImport("libwebp", CallingConvention = CallingConvention.Cdecl, EntryPoint = "WebPMalloc")]
        private static extern IntPtr WebPMalloc_x64(int size);

        [DllImport("libwebp", CallingConvention = CallingConvention.Cdecl)]
        static extern int WebPFree(IntPtr p);


    }
}
