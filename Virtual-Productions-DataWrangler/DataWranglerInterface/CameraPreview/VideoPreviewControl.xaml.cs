using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace DataWranglerInterface.ShotRecording
{
    /// <summary>
    /// Interaction logic for VideoPreviewControl.xaml
    /// </summary>
    public partial class VideoPreviewControl : UserControl
    {
        private readonly WriteableBitmap?[] m_bitmaps = new WriteableBitmap[2];
        private int m_currentBackBuffer = 0;

        public VideoPreviewControl()
        {
            InitializeComponent();
        }

        public void OnVideoFrameUpdated(int a_frameWidth, int a_frameHeight, PixelFormat a_pixelFormat,
            IntPtr a_framePixelData, int a_framePixelDataLength, int a_framePixelDataStride)
        {
            WriteableBitmap? targetBitmap = m_bitmaps[m_currentBackBuffer];
            if (targetBitmap == null || targetBitmap.PixelWidth != a_frameWidth || targetBitmap.PixelHeight != a_frameHeight)
            {
                    m_bitmaps[0] = new WriteableBitmap(a_frameWidth, a_frameHeight, 96, 96, a_pixelFormat, null);
                    m_bitmaps[1] = new WriteableBitmap(a_frameWidth, a_frameHeight, 96, 96, a_pixelFormat, null);

                m_currentBackBuffer = 0;
                targetBitmap = m_bitmaps[m_currentBackBuffer];
            }

            targetBitmap!.Lock();
            targetBitmap.WritePixels(new Int32Rect(0, 0, a_frameWidth, a_frameHeight), a_framePixelData, a_framePixelDataLength, a_framePixelDataStride);
            targetBitmap.Unlock();

            int imageToDisplay = m_currentBackBuffer;
            DisplayImage.Source = m_bitmaps[imageToDisplay];

            m_currentBackBuffer = ((m_currentBackBuffer + 1) % m_bitmaps.Length);
        }
    }
}
