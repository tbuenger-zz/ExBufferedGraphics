using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace System.Drawing
{
    /// <summary>
    /// Provides a graphics buffer for efficient drawing and blitting. 
    /// </summary>
    public sealed class ExBufferedGraphics : IDisposable
    {
        #region Fields
        private BufferedGraphics Buffer;
        private BufferedGraphicsContext Context;
        private bool IsDisposed = false;
        private Size _size = new Size(1, 1);
        #endregion

        const double Sqrt2 = 1.4142135;

        #region Properties
        /// <summary>
        /// Gets or sets the size, in pixels, of this Buffer.
        /// </summary>
        public Size Size
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(this.ToString());
                return this._size;
            }
            set
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(this.ToString());
                if (value.Width * value.Height == 0)
                    throw new ArgumentException();

                this._size = value;
                Size maximumSize = this.Context.MaximumBuffer;
                if (this.Size.Width > maximumSize.Width || this.Size.Height > maximumSize.Height)
                {
                    ReAlloc(new Size(
                        Math.Max((int)(maximumSize.Width * Sqrt2), this.Width),
                        Math.Max((int)(maximumSize.Height * Sqrt2), this.Height)));
                }
                this.Buffer.Graphics.Clip = new Region(new Rectangle(Point.Empty, this.Size));
            }
        }
        /// <summary>
        /// Gets a Graphics object that outputs to the graphics buffer.
        /// </summary>
        public Graphics Graphics
        {
            get
            {
                if (IsDisposed)
                    throw new ObjectDisposedException(this.ToString());
                return this.Buffer.Graphics;
            }

        }

        /// <summary>
        /// Gets the width, in pixels, of this Buffer.
        /// </summary>
        public int Width { get { return this.Size.Width; } }

        /// <summary>
        /// Gets the height, in pixels, of this Buffer.
        /// </summary>
        public int Height { get { return this.Size.Height; } }
        #endregion

        #region Constructors
        /// <summary>
        /// Initializes a new instance of the ExBufferedGraphics class compatible with the specified Graphics object.
        /// </summary>
        /// <param name="g">Reference Graphics object</param>
        public ExBufferedGraphics(Graphics g)
        {
            this.Init(g);
        }

        /// <summary>
        /// Initializes a new instance of the ExBufferedGraphics class compatible with the specified Control object.
        /// </summary>
        /// <param name="g">Reference Control object</param>
        public ExBufferedGraphics(System.Windows.Forms.Control c)
        {
            using (Graphics g = c.CreateGraphics())
            {
                this.Init(g);
            }
        }
        #endregion

        #region BufferManagement
        private void Init(Graphics g)
        {
            Alloc(g, this.Size);
        }

        private void Alloc(Graphics g, Size s)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(this.ToString());

            this.Context = new BufferedGraphicsContext();
            this.Context.MaximumBuffer = s;
            this.Buffer = this.Context.Allocate(g, new Rectangle(Point.Empty, s));
        }

        private void ReAlloc(Size s)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(this.ToString());

            using (BufferedGraphicsContext oldContext = this.Context)
            using (BufferedGraphics oldBuffer = this.Buffer)
            {
                Alloc(oldBuffer.Graphics, s);
            }
        }
        #endregion

        #region Disposing
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ExBufferedGraphics()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    if (this.Buffer != null)
                        this.Buffer.Dispose();
                    if (this.Context != null)
                        this.Context.Dispose();
                }
                IsDisposed = true;
            }
        }
        #endregion

        #region Rendering
        /// <summary>
        /// Writes the contents of the graphics buffer to the specified graphics object.
        /// </summary>
        /// <param name="g">A graphics object to which to write the contents of the graphics buffer.</param>
        public void Render(Graphics target)
        {
            Render(target, 0, 0);
        }

        /// <summary>
        /// Writes the contents of the graphics buffer to the specified graphics object at the specified location. 
        /// </summary>
        /// <param name="target">A graphics object to which to write the contents of the graphics buffer.</param>
        /// <param name="targetLocation">Point structure that represents the location of the upper-left corner of the drawn image.</param>
        public void Render(Graphics target, Point targetLocation)
        {
            Render(target, targetLocation.X, targetLocation.Y);
        }

        /// <summary>
        /// Writes the contents of a section of the graphics buffer to the specified Graphics object at the specified location. 
        /// </summary>
        /// <param name="target">A graphics object to which to write the contents of the graphics buffer.</param>
        /// <param name="targetLocation">Point structure that represents the location of the upper-left corner of the drawn image.</param>
        /// <param name="sourceSection">Rectangle structure that represents the section of the graphics buffer to be drawn.</param>
        public void Render(Graphics target, Point targetLocation, Rectangle sourceSection)
        {
            Render(target, targetLocation.X, targetLocation.Y, sourceSection.Width, sourceSection.Height, sourceSection.X, sourceSection.Y);
        }


        /// <summary>
        /// Writes the contents of the graphics buffer to the specified Graphics object at the location specified by a coordinate pair.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="targetXLocation">The x-coordinate of the upper-left corner of the drawn image. </param>
        /// <param name="targetYLocation">The y-coordinate of the upper-left corner of the drawn image. </param>
        public void Render(Graphics target, int targetXLocation, int targetYLocation)
        {
            Render(target, targetXLocation, targetYLocation, 0, 0, this.Width, this.Height);
        }

        /// <summary>
        /// Writes the contents of a section of the graphics buffer to the specified Graphics object at the location specified by a coordinate pair.
        /// </summary>
        /// <param name="target"></param>
        /// <param name="targetXLocation">The x-coordinate of the upper-left corner of the drawn image. </param>
        /// <param name="targetYLocation">The y-coordinate of the upper-left corner of the drawn image. </param>
        /// <param name="sourceXLocation">The x-coordinate of the upper-left corner of the section of the graphics buffer that is drawn.</param>
        /// <param name="sourceYLocation">The y-coordinate of the upper-left corner of the section of the graphics buffer that is drawn.</param>
        /// <param name="width">The width of the section of the graphics buffer that is drawn.</param>
        /// <param name="height">The height of the section of the graphics buffer that is drawn.</param>
        public void Render(Graphics target, int targetXLocation, int targetYLocation, int sourceXLocation, int sourceYLocation, int width, int height)
        {
            RenderScaled(target, targetXLocation, targetYLocation, width, height, sourceXLocation, sourceYLocation, width, height, InterpolationMode.Invalid);
        }

        /// <summary>
        /// Writes and scales the contents of the graphics buffer into a section of the specified Graphics object. 
        /// </summary>
        /// <param name="target">A graphics object to which to write the contents of the graphics buffer.</param>
        /// <param name="targetSection">Rectangle structure that represents the section of the specified graphics where the image is drawn into.</param>
        /// <param name="interpolationMode">A InterpolationMode enumeration that specifies the interpolation mode for scaling.</param>
        public void RenderScaled(Graphics target, Rectangle targetSection, InterpolationMode interpolationMode)
        {
            RenderScaled(target, targetSection, new Rectangle(Point.Empty, this.Size), interpolationMode);
        }

        /// <summary>
        /// Writes and scales the contents of the graphics buffer into a section of the specified Graphics object. 
        /// </summary>
        /// <param name="target">A graphics object to which to write the contents of the graphics buffer.</param>
        /// <param name="targetXLocation">The x-coordinate of the upper-left corner of the drawn image.</param>
        /// <param name="targetYLocation">The y-coordinate of the upper-left corner of the drawn image.</param>
        /// <param name="targetWidth">The width of the drawn image.</param>
        /// <param name="targetHeight">The height of the drawn image.</param>
        /// <param name="interpolationMode">A InterpolationMode enumeration that specifies the interpolation mode for scaling.</param>
        public void RenderScaled(Graphics target, int targetXLocation, int targetYLocation, int targetWidth, int targetHeight, InterpolationMode mode)
        {
            RenderScaled(target, targetXLocation, targetYLocation, targetWidth, targetHeight, 0, 0, this.Width, this.Height, mode);
        }

        /// <summary>
        /// Writes and scales the contents of a section of the graphics buffer into a section of the specified Graphics object. 
        /// </summary>
        /// <param name="target">A graphics object to which to write the contents of the graphics buffer.</param>
        /// <param name="targetSection">Rectangle structure that represents the section of the specified graphics where the image is drawn into.</param>
        /// <param name="sourceSection">Rectangle structure that represents the section of the graphics buffer that is drawn.</param>
        /// <param name="interpolationMode">A InterpolationMode enumeration that specifies the interpolation mode for scaling.</param>
        public void RenderScaled(Graphics target, Rectangle targetSection, Rectangle sourceSection, InterpolationMode mode)
        {
            RenderScaled(
                target,
                targetSection.X, targetSection.Y, targetSection.Width, targetSection.Height,
                sourceSection.X, sourceSection.Y, sourceSection.Width, sourceSection.Height,
                mode);
        }

        /// <summary>
        /// Writes and scales the contents of a section of the graphics buffer into a section of the specified Graphics object. 
        /// </summary>
        /// <param name="target">A graphics object to which to write the contents of the graphics buffer.</param>
        /// <param name="targetXLocation">The x-coordinate of the upper-left corner of the drawn image.</param>
        /// <param name="targetYLocation">The y-coordinate of the upper-left corner of the drawn image.</param>
        /// <param name="targetWidth">The width of the drawn image.</param>
        /// <param name="targetHeight">The height of the drawn image.</param>
        /// <param name="sourceXLocation">The x-coordinate of the upper-left corner of the section of the graphics buffer that is drawn.</param>
        /// <param name="sourceYLocation">The y-coordinate of the upper-left corner of the section of the graphics buffer that is drawn.</param>
        /// <param name="sourceWidth">The width of the section of the graphics buffer that is drawn.</param>
        /// <param name="sourceHeight">The height of the section of the graphics buffer that is drawn.</param>
        /// <param name="interpolationMode">A InterpolationMode enumeration that specifies the interpolation mode for scaling.</param>
        public void RenderScaled(Graphics target,
            int targetXLocation, int targetYLocation, int targetWidth, int targetHeight,
            int sourceXLocation, int sourceYLocation, int sourceWidth, int sourceHeight, InterpolationMode mode)
        {
            if (IsDisposed)
                throw new ObjectDisposedException(this.ToString());
            if (target == null)
                throw new ArgumentNullException("target");

            using (DCHandle hdcTarget = new DCHandle(target))
            using (DCHandle hdcSource = new DCHandle(this.Graphics))
            {

                if (sourceWidth == targetWidth && targetHeight == sourceHeight)
                {
                    if(!BitBlt(
                        hdcTarget,
                        targetXLocation, targetYLocation, targetWidth, targetHeight,
                        hdcSource,
                        sourceXLocation, sourceYLocation,
                        TernaryRasterOperations.SRCCOPY))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
                else
                {
                    SetStretchBltMode(hdcTarget, mode);
                    if(!StretchBlt(
                        hdcTarget,
                        targetXLocation, targetYLocation, targetWidth, targetHeight,
                        hdcSource,
                        sourceXLocation, sourceYLocation, sourceWidth, sourceHeight,
                        TernaryRasterOperations.SRCCOPY))
                    {
                        throw new Win32Exception(Marshal.GetLastWin32Error());
                    }
                }
            }
        }

        private void SetStretchBltMode(DCHandle hdcTarget, InterpolationMode interpolationMode)
        {
            StretchMode sm;
            switch (interpolationMode)
            {
                case InterpolationMode.Bilinear:
                case InterpolationMode.Bicubic:
                case InterpolationMode.High:
                case InterpolationMode.HighQualityBilinear:
                case InterpolationMode.HighQualityBicubic:
                    sm = StretchMode.STRETCH_HALFTONE;
                    break;

                case InterpolationMode.Default:
                case InterpolationMode.Low:
                case InterpolationMode.NearestNeighbor:
                    sm = StretchMode.STRETCH_DELETESCANS;
                    break;

                default:
                    throw new ArgumentException("interpolationMode");
            }
            if (SetStretchBltMode(hdcTarget, sm) == StretchMode.INVALID)
            {
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }
        }

        #endregion

        #region Native GDI

        private class DCHandle : SafeHandle
        {
            Graphics graphics;
            internal DCHandle(Graphics graphics)
                : base(graphics.GetHdc(), true)
            {
                this.graphics = graphics;
            }

            protected override bool ReleaseHandle()
            {
                this.graphics.ReleaseHdc(this.handle);
                this.handle = IntPtr.Zero;
                return true;
            }

            public override bool IsInvalid { get { return IsClosed || handle == IntPtr.Zero; } }
        }

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        static extern bool BitBlt(DCHandle hdc, int nXDest, int nYDest, int nWidth,
            int nHeight, DCHandle hdcSrc, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        static extern bool StretchBlt(DCHandle hdcDest, int nXOriginDest, int nYOriginDest,
            int nWidthDest, int nHeightDest,
            DCHandle hdcSrc, int nXOriginSrc, int nYOriginSrc, int nWidthSrc, int nHeightSrc,
            TernaryRasterOperations dwRop);

        [DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        static extern StretchMode SetStretchBltMode(DCHandle hdc, StretchMode iStretchMode);

        enum StretchMode
        {
            INVALID = 0,
            STRETCH_ANDSCANS = 1,
            STRETCH_ORSCANS = 2,
            STRETCH_DELETESCANS = 3,
            STRETCH_HALFTONE = 4,
        }

        enum TernaryRasterOperations : uint
        {
            SRCCOPY = 0x00CC0020, /* dest = source*/
            SRCPAINT = 0x00EE0086, /* dest = source OR dest*/
            SRCAND = 0x008800C6, /* dest = source AND dest*/
            SRCINVERT = 0x00660046, /* dest = source XOR dest*/
            SRCERASE = 0x00440328, /* dest = source AND (NOT dest )*/
            NOTSRCCOPY = 0x00330008, /* dest = (NOT source)*/
            NOTSRCERASE = 0x001100A6, /* dest = (NOT src) AND (NOT dest) */
            MERGECOPY = 0x00C000CA, /* dest = (source AND pattern)*/
            MERGEPAINT = 0x00BB0226, /* dest = (NOT source) OR dest*/
            PATCOPY = 0x00F00021, /* dest = pattern*/
            PATPAINT = 0x00FB0A09, /* dest = DPSnoo*/
            PATINVERT = 0x005A0049, /* dest = pattern XOR dest*/
            DSTINVERT = 0x00550009, /* dest = (NOT dest)*/
            BLACKNESS = 0x00000042, /* dest = BLACK*/
            WHITENESS = 0x00FF0062, /* dest = WHITE*/
        };
        #endregion
    }
}