using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace WindowsFormsApplication2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            NvrVideoPlayer.VideoForm video = new NvrVideoPlayer.VideoForm();
            video.Init("d54126364d7c245d");
            this.Controls.Add(video);
        }
    }
}
