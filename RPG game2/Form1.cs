using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RPG_game2
{
    public partial class Form1 : Form
    {
        public Game game;
        public Form1()
        {
            InitializeComponent();
            game = new Game(this);
            Width = 1280 ;
            Height = 720;
        }

        private void Form1_KeyDown(object sender, KeyEventArgs e)
        {
            game.HandleKeyPress(e);
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
