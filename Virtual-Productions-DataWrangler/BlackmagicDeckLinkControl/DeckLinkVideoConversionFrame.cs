using DeckLinkAPI;
using System.Runtime.InteropServices;

namespace BlackmagicDeckLinkControl
{
    public class DeckLinkVideoConversionFrame: IDeckLinkVideoFrame, IDisposable
    {
        public readonly int Width;
        public readonly int Height;
        public readonly int Stride;
        public readonly _BMDPixelFormat PixelFormat;

        private IntPtr m_frameData;

        public DeckLinkVideoConversionFrame(int a_width, int a_height, _BMDPixelFormat a_targetFormat)
        {
            int bitsPerPixel = 4;

            Width = a_width;
            Height = a_height;
            Stride = Width * bitsPerPixel;
            PixelFormat = a_targetFormat;

            m_frameData = Marshal.AllocHGlobal(Width * Height * bitsPerPixel);
        }

        public void Dispose()
        {
            Marshal.FreeHGlobal(m_frameData);
        }

        public int GetWidth()
        {
            return Width;
        }

        public int GetHeight()
        {
            return Height;
        }

        public int GetRowBytes()
        {
            return Stride;
        }

        public _BMDPixelFormat GetPixelFormat()
        {
            return PixelFormat;
        }

        public _BMDFrameFlags GetFlags()
        {
            return _BMDFrameFlags.bmdFrameFlagDefault;
        }

        public void GetBytes(out IntPtr buffer)
        {
            buffer = m_frameData;
        }

        public void GetTimecode(_BMDTimecodeFormat format, out IDeckLinkTimecode timecode)
        {
            throw new NotImplementedException();
        }

        public void GetAncillaryData(out IDeckLinkVideoFrameAncillary ancillary)
        {
            throw new NotImplementedException();
        }
    }
}
