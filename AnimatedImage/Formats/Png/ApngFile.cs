// Copyright (c) 2013 Amemiya
// Licensed under the MIT License.
// Ported from: https://github.com/xupefei/APNG.NET

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using AnimatedImage.Formats.Png.Chunks;

namespace AnimatedImage.Formats.Png
{
    internal class ApngFile
    {
        public ApngFile(string fileName) : this(File.ReadAllBytes(fileName))
        { }

        public ApngFile(byte[] fileBytes) : this(new MemoryStream(fileBytes))
        { }


        public ApngFile(Stream stream)
        {
            // check file signature.
            if (!Helper.IsBytesEqual(stream.ReadBytes(ApngFrame.Signature.Length), ApngFrame.Signature))
                throw new PngDecoderException("File signature incorrect.");


            var chunkStream = new ChunkStream(stream);

            if (chunkStream.ReadChunkType() != "IHDR")
                throw new PngDecoderException("IHDR chunk must located before any other chunks.");

            // Read IHDR chunk.
            IHDRChunk = new IHDRChunk(chunkStream);


            bool isSimple = false;

            // Now let's loop in chunks

            ApngFrame? defaultImage = null;
            ApngFrame? current = null;
            var frames = new List<ApngFrame>();

            bool isIENDParsed = false;

            /*
             * Simple PNG image
             * 
             * IHDR -> IDAT -> IEND
             * IHDR -> fdAT -> fdAT ... -> IEND
             * IHDR -> PLTE -> some DAT (IDAT or fdAT) -> IEND
             * 
             * 
             * Animation PNG image
             * 
             * IHDR -> acTL -> fcTL -> iDAT or fdAT* -> fcTL -> fdAT* -> IEND
             * 
             */

            do
            {
                var chunkType = chunkStream.ReadChunkType();

                Debug.Print(chunkType);
                switch (chunkType)
                {
                    case null:
                        throw new PngDecoderException("IEND chunk expected.");

                    case IHDRChunk.ChunkType:
                        throw new PngDecoderException("Only single IHDR is allowed.");

                    case acTLChunk.ChunkType:
                        if (isSimple)
                            throw new PngDecoderException("acTL chunk must located before any IDAT and fdAT");

                        acTLChunk = new acTLChunk(chunkStream);
                        break;

                    case IDATChunk.ChunkType:
                        // To be an APNG, acTL must located before any IDAT and fdAT.
                        if (acTLChunk is null)
                            isSimple = true;

                        if (current is null)
                        {
                            current = new ApngFrame(IHDRChunk);

                            if (!isSimple)
                                frames.Add(current);
                        }
                        if (defaultImage is null)
                        {
                            defaultImage = current;
                        }

                        current.AddIDATChunk(new IDATChunk(chunkStream));
                        break;

                    case fcTLChunk.ChunkType:
                        // To be an APNG, acTL must located before any IDAT and fdAT.
                        if (acTLChunk is null)
                            isSimple = true;

                        if (current is not null && current.IDATChunks.Count == 0)
                            throw new PngDecoderException("One frame must have only one fcTL chunk.");

                        current = new ApngFrame(IHDRChunk, new fcTLChunk(chunkStream));

                        if (!isSimple)
                            frames.Add(current);

                        if (defaultImage is null)
                        {
                            defaultImage = current;
                        }
                        break;

                    case fdATChunk.ChunkType:
                        if (acTLChunk is null)
                            isSimple = true;

                        if (current is null)
                        {
                            current = new ApngFrame(IHDRChunk);

                            if (!isSimple)
                                frames.Add(current);
                        }
                        if (defaultImage is null)
                        {
                            defaultImage = current;
                        }

                        current.AddIDATChunk(new fdATChunk(chunkStream).ToIDATChunk());
                        break;

                    case PLTEChunk.ChunkType:
                        PLTEChunk = new PLTEChunk(chunkStream);
                        break;

                    case tRNSChunk.ChunkType:
                        tRNSChunk = tRNSChunk.Create(IHDRChunk, chunkStream);
                        break;

                    case IENDChunk.ChunkType:
                        // register last frame object
                        new IENDChunk(chunkStream);
                        isIENDParsed = true;
                        break;

                    default:
                        new Chunk(chunkStream);
                        break;
                }

            } while (!isIENDParsed);


            if (defaultImage is null)
                throw new PngDecoderException("has no image");

            IsSimplePNG = isSimple;
            DefaultImage = defaultImage;
            Frames = frames.AsReadOnly();
        }

        /// <summary>
        ///     Indicate whether the file is a simple PNG.
        /// </summary>
        public bool IsSimplePNG { get; }

        /// <summary>
        ///     Gets the base image.
        ///     If IsSimplePNG = True, returns the only image;
        ///     if False, returns the default image
        /// </summary>
        public ApngFrame DefaultImage { get; }

        /// <summary>
        ///     Gets the frame array.
        ///     If IsSimplePNG = True, returns empty
        /// </summary>
        public ReadOnlyCollection<ApngFrame> Frames { get; }

        /// <summary>
        ///     Gets the IHDR Chunk
        /// </summary>
        public IHDRChunk IHDRChunk { get; }

        /// <summary>
        ///     Gets the acTL Chunk
        /// </summary>
        public acTLChunk? acTLChunk { get; }

        public PLTEChunk? PLTEChunk { get; }

        public tRNSChunk? tRNSChunk { get; }
    }
}