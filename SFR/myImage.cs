using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Xml.Serialization;

namespace SFR
{
    public class myImage
    {
        [XmlElement("imageSource")]
        public ImageSource imageSource{ get; set; }
        public myImage(){}
        public myImage(ImageSource imageSource)
        {
            this.imageSource = imageSource;
        }

    }
}
