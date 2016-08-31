using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;

namespace EmbeddedMVC
{
    class CachedDocument
    {
        public CachedDocument()
        {
        }

        public CachedDocument(Image img)
        {
            _mimeType = "image/png";
            _bytes = ImageToBytePng(img);
        }

        public CachedDocument(string text)
        {
            _mimeType = "text/plain";
            _bytes = Encoding.UTF8.GetBytes(text);
        }

        private byte[] _bytes;
        public byte[] Bytes
        {
            get { return _bytes; }
            set { _bytes = value; }
        }

        private string _mimeType;
        public string MimeType
        {
            get { return _mimeType; }
            set { _mimeType = value; }
        }

        private static byte[] ImageToBytePng(Image img)
        {
            byte[] byteArray;
            using (MemoryStream stream = new MemoryStream())
            {
                img.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                stream.Flush();
                byteArray = stream.ToArray();
            }
            return byteArray;
        }

    }
}
