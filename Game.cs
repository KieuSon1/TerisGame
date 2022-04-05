using System;
using System.Data.SqlClient;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Media;
using System.Runtime.Remoting.Messaging;

namespace Tetris
{
    public partial class Game : Form
    {




        //struct Point { public int x; public int y; } //mỗi điểm đều có x và y
        /// <summary>
        /// _ _ _ _ _x
        /// |
        /// |
        /// |
        /// |y
        /// </summary>
        const int M = 20;//max cao
        const int N = 10;//max rộng

        int[,] field = new int[M, N]{
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1},
            {-1,-1,-1,-1,-1,-1,-1,-1,-1,-1}
        };//lưu cache của tile khi tile đứng trên 1 tile khác hoặc đứng dưới đáy, nói gọn lại là lưu tile khi đã hint lên bảng

        int[,] figures = new int[7,4]// các loại tile gồm 7 loại mỗi loại có 4 ô vuông
        {
            { 1,3,5,7 }, // I
            { 2,4,5,7 }, // Z
            { 3,5,4,6 }, // S
            { 3,5,4,7 }, // T
            { 2,3,5,7 }, // L
            { 3,5,7,6 }, // J
            { 2,3,4,5 }, // O
        };

        /// <summary> 
        /// Ví dụ cho chữ J { 3,5,7,6 } các số trong ngoặc tương ứng với vị trí của mỗi Tile 
        /// [0][1]                           [ ][ ]   
        /// [2][3]  chữ J có  { 3,5,7,6 } => [ ][*]
        /// [4][5]                           [ ][*]
        /// [6][7]                           [*][*]
        /// hay chữ L { 2,3,5,7 }
        /// [0][1]                           [ ][ ]   
        /// [2][3]  chữ L có  { 2,3,5,7 } => [*][*]
        /// [4][5]                           [ ][*]
        /// [6][7]                           [ ][*]
        /// các chữ còn lại tương tự
        /// </summary>



        int dx = 0;//di chuyển trái phải của tile "dx = -1" là qua trái và ngược lại
        bool rotate = false;//lật tile
        int colorNum = 1;//màu của Tile khi được xuất ra


        int n = 0; // số chữ sẽ xuất ra 
        /// <summary>
        /// Ví dụ n = 0 thì sẽ xuất ra chữ I 
        /// Ví dụ n = 1 thì sẽ xuất ra chữ Z
        /// Theo thứ tự của figures[n,]
        /// </summary>

        Tiles.Point[] a = new Tiles.Point[4];//điểm của Tile khi đang trôi xuống đáy
        Tiles.Point[] b = new Tiles.Point[4];//điểm của Tile được lưu lại để gọi khi Tile bị lỗi


        Tiles.Point[] newTile = new Tiles.Point[4];
        int newColor;
        int nextN = -1;
        int eat = 0;

        /// Hình ảnh
        public Image image = Image.FromFile("./images/tiles.png");//ảnh Tile 18x18px mỗi ô, tổng 18x144px
        public Image frame = Image.FromFile("./images/frame1.png");
        
        /// Âm thanh
        public SoundPlayer sound;
        public SoundPlayer soundSync;
        public string menusound = @".\menu.wav";
        public string gamesound = @".\gameplay.wav";
        public string buttonclicksound = @".\click.wav";
        public string gameoversound = @".\gameover.wav";
        public string pointsound = @".\point.wav";

        
        public void sound_playlooping(string path)
        {
            sound = new SoundPlayer();
            sound.SoundLocation = path.ToString();
            sound.PlayLooping();
        }

        public void sound_play(string path)
        {
            sound = new SoundPlayer();
            sound.SoundLocation = path.ToString();
            sound.Play();
        }





        public Game()
        {
            InitializeComponent();
                       
        }

        private void Game_Load(object sender, EventArgs e)
        {
            btn_back.Visible = false;
            if (panel1.Visible)
            {
                timer1.Enabled = false;
                sound_playlooping(menusound);
            }
           
            //this.Size = new System.Drawing.Size(320, 480);//size màn hình game(cái này sửa trực tiếp trong properties)
            this.Text = "Tetris";//tên game
            label1.Text = $"{eat}";
            
            Random rnd = new Random();
            colorNum = rnd.Next(7);//random MÀU lần đầu tiên khi vào game
            n = rnd.Next(7);//random CHỮ lần đầu tiên khi vào game

            for (int i = 0; i < 4; i++)
            {
                a[i].x = 4+figures[n, i] % 2;
                a[i].y = figures[n, i] / 2;
            }
            createNewTile();
            /// <summary>
            /// Mỗi chữ có 4 ô nên for 4 vòng
            /// chạy gán a[i] cho figures[n, i]
            /// 
            /// tới đây gán a[i].x cho figures[n, i] % 2 và  a[i].y cho figures[n, i] / 2
            /// Mỗi Chữ(Tile) sẽ có 4 Điểm(Point) tương ứng với 4 ô vuông
            /// Ví dụ tính chữ L { 2,3,5,7 }
            /// Vị trí muốn vẽ là
            /// [ ][ ]
            /// [*][*]
            /// [ ][*]
            /// [ ][*]
            /// 
            /// tính bằng cách lấy figures[4(vị trí của chữ L trong mảng figures),lặp 4 lần i tương ứng 4 điểm trong mảng 2 chiều] 
            /// vòng for 0
            /// lấy giá trị x: figures[4, 0] % 2 tương ứng với 2 % 2 
            /// lấy giá trị y: figures[4, 0] / 2 tương ứng với 2 / 2
            /// vòng for 0 lấy được x = 0, y = 1
            /// 
            /// vòng for 1
            /// lấy giá trị x: figures[4, 1] % 2 tương ứng với 3 % 2 
            /// lấy giá trị y: figures[4, 1] / 2 tương ứng với 3 / 2
            /// vòng for 1 lấy được x = 1, y = 1
            /// 
            /// vòng for 2
            /// lấy giá trị x: figures[4, 2] % 2 tương ứng với 5 % 2 
            /// lấy giá trị y: figures[4, 2] / 2 tương ứng với 5 / 2
            /// vòng for 2 lấy được x = 1, y = 2
            /// 
            /// vòng for 3
            /// lấy giá trị x: figures[4, 3] % 2 tương ứng với 7 % 2 
            /// lấy giá trị y: figures[4, 3] / 2 tương ứng với 7 / 2
            /// vòng for 3 lấy được x = 1, y = 3
            /// 
            /// Ta lấy được kết quả 
            /// A[0] = [0,1]
            /// A[1] = [1,1]
            /// A[2] = [1,2]
            /// A[3] = [1,3]
            /// 
            /// 
            /// vẽ lên đồ thị sẽ được chữ L
            /// vì mình lật ngược map nên đồ thị sẽ ngược xuống dưới
            ///    0  1  ....x
            /// 0 [ ][ ] 
            /// 1 [*][*]
            /// 2 [ ][*]
            /// 3 [ ][*]
            /// ...y
            /// 
            /// </summary>
            /// 

        }
        private void checkLine()
        {
            ///<summary>
            /// mô tả cơ bản về hàm check
            /// vì là đồ thị ngược nên M lớn nhất nằm dưới cùng
            /// _ _ _ _ _N
            /// |
            /// |
            /// |
            /// |M
            /// gán cho k bằng với số dòng thứ cuối cùng của map( field[dòng, cột] => field[M, N] ) vì ở trên M được gán là 20 thì số dòng bắt đầu từ 0-19(20 giá trị)
            /// để check dòng thì hàm chạy sẽ check từ dưới lên trên
            /// 
            /// khai báo biến đếm bằng 0 khi bắt đầu check một dòng M mới có N giá trị(N là số ô trong dòng)
            /// vì bảng map tile mặc định không màu = -1, nếu ô trong map tile khác -1 thì ô đó được gán màu nếu được gán màu thì count +1
            /// 
            /// 
            /// dòng này rất là quan trọng này
            /// *******     field[k, j] = field[i, j]; *******
            /// 
            /// dòng này sẽ chạy theo 2 kiểu
            /// 
            /// ----kiểu thứ nhất là check KHÔNG có dòng nào đủ count == N, chạy bình thường không ăn dòng nào và K luôn luôn chạy = i
            /// ----kiểu thứ hai là check dòng dủ đủ count và dồn hàng trên xuống hàng count đủ để xóa đi, chạy luôn luôn K > i
            /// 
            /// 
            /// 
            /// 
            /// </summary>

            int k = M - 1; //M cao là 20 - 1 => k = 19
            for (int i = M - 1; i > 0; i--)
            {
                int count = 0;// đếm màu trong dòng N
                for (int j = 0; j < N; j++)//chạy vòng for cho N dòng
                {
                    if (field[i, j] != -1) count++;// vì bảng map tile mặc định không màu = -1, nếu ô trong map tile khác -1 thì ô đó được gán màu nếu được gán màu thì count +1
                    field[k, j] = field[i, j];
                }
                if (count == N)
                {
                    eat++;
                    label1.Text = $"{eat}";
                }
                if (count < N)
                {
                    k--;
                }
            }
        }


        private void MoveTile()
        {
            
            if (dx == 0) return;
            for (int i = 0; i < 4; i++)
            {
                b[i] = a[i];
                a[i].x += dx;         
            }
            if (!check())
            {
                for (int i = 0; i < 4; i++)
                {
                    a[i] = b[i];
                }
            }
            dx = 0;
            Invalidate();
        }

        private void Rotate()
        {
            if (rotate == true)
            {
                rotate = false;
                Tiles.Point p = a[1];
                for(int i = 0; i < 4; i++)
                {
                    int x = a[i].y - p.y;
                    int y = a[i].x - p.x;

                    a[i].x = p.x - x;
                    a[i].y = p.y + y;;
                }
            }
            if (!check())//check chạm cạnh hay chạm tile sẽ trả về b đã lưu
            {
                for (int i = 0; i < 4; i++)
                {
                    a[i] = b[i];
                }
            }
        }

        private void Game_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Up || e.KeyCode == Keys.W)
            {
                rotate = true;
                Rotate();
            }else if(e.KeyCode == Keys.Down || e.KeyCode == Keys.S)
            {
                timer1.Interval = 10;

            }else if (e.KeyCode == Keys.Left || e.KeyCode == Keys.A)
            {
                dx = -1;
                MoveTile();
            }
            else if (e.KeyCode == Keys.Right || e.KeyCode == Keys.D)
            {
                dx = 1;
                MoveTile();
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            for (int i = 0; i <4; i++)
            {
                b[i] = a[i];
                a[i].y += 1;      
            }
            pictureBox1.Refresh();
            Invalidate();
            
        }

        private bool check()
        {
            for (int i = 0; i < 4; i++)
            {
                if (a[i].x < 0 || a[i].x >= N || a[i].y >= M)
                {
                    timer1.Interval = 200;
                    return false;

                }
                else if (field[a[i].y, a[i].x] != -1 )
                {
                    timer1.Interval = 200;
                    if (a[i].y <= 2)
                    {
                        timer1.Interval = 1;
                        timer1.Enabled = false;
                        sound_play(gameoversound);

                        var result = MessageBox.Show($"\t GAME OVER \n\t SCORE: {eat}\n\t RETURN TO MENU", "Tetris", MessageBoxButtons.YesNo);
                        if (result == DialogResult.No)
                        {
                            // cancel the closure of the form.
                            Application.Exit();
                        }
                        else if (result == DialogResult.Yes)
                        {
                            // cancel the closure of the form.

                            Game gamenew = new Game();
                            gamenew.Show();
                            this.Dispose(false);
                            
                            
                        }
                        return false;
                    }
                    return false;
                }
            }
            return true;
        }

        private void DrawLine(PaintEventArgs e)
        {
            
            for (int i = 0; i < M; i++)
            {
                for (int j = 0; j < N; j++)
                {
                    if (field[i,j]<0) continue;
                    Rectangle crop = new Rectangle(j*18+2,i*18+2, 18, 18);
                    e.Graphics.DrawImage(image, crop, field[i, j] * 18, 0, 18, 18, GraphicsUnit.Pixel);

                }
            }
            Invalidate();
        }
        private void DrawTiles(Graphics g)
        {
            for (int i = 0; i < 4; i++)
            {
                Rectangle crop = new Rectangle(a[i].x * 18 + 2, a[i].y * 18 + 2, 18, 18);
                g.DrawImage(image, crop, colorNum * 18, 0, 18, 18, GraphicsUnit.Pixel);
            }
        }

        private void pictureBox3_Paint(object sender, PaintEventArgs e)
        {

            for (int i = 0; i < 4; i++)
            {
                Rectangle crop = new Rectangle((newTile[i].x-3) *18, (newTile[i].y) *18, 18, 18);
                e.Graphics.DrawImage(image, crop, newColor * 18, 0, 18, 18, GraphicsUnit.Pixel);
            }
        }


        private void createNewTile()
        {
            if (nextN != -1)
            {
                for (int i = 0; i < 4; i++)
                {
                    a[i] = newTile[i];
                }
                colorNum = newColor;
                n = nextN;
            }

            Random rnd = new Random();
            newColor = rnd.Next(7);
            nextN = rnd.Next(7);
            for (int i = 0; i < 4; i++)
            {
                newTile[i].x = 4 + figures[nextN, i] % 2;
                newTile[i].y = figures[nextN, i] / 2;
            }
            pictureBox3.Refresh();
        }
        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            // Vẽ lên màn hình
            DrawMenu(e);//menu
            if (!check())
            {
                for (int i = 0; i < 4; i++)
                {
                    field[b[i].y, b[i].x] = colorNum;
                }
                createNewTile();
            }

            DrawTiles(e.Graphics);
            checkLine();
            DrawLine(e);
            pictureBox1.Update();
        }

        private void DrawMenu(PaintEventArgs e)
        {
            Rectangle crop = new Rectangle(0, 0, 305, 441);
            e.Graphics.DrawImage(frame, crop,0, 0, 305, 441, GraphicsUnit.Pixel);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            panel1.Visible = false;
            panel1.Enabled = false;
            timer1.Enabled = true;
        }

        

        

        private void pictureBox3_Click(object sender, EventArgs e)
        {

        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {

        }

        private void btn_Play_Click(object sender, EventArgs e)
        {
            panel1.Visible = false;
            panel1.Enabled = false;
            timer1.Enabled = true;
            sound_playlooping(gamesound);         

        }
        //private void btn_credit_Click(object sender, EventArgs e)
        //{
        //    if (btn_back.Visible=true)
        //    {
        //        btn_credits.Visible = false;
        //        btn_Play.Visible = false;
        //        panel2.Visible = true;
        //    }
        //    else
        //    {
        //        panel2.Visible = false;                
        //        btn_Play.Visible = true;
        //        btn_back.Visible = false;
        //richTextBox1.Text = "2020 Tetris\nXây dựng bởi: \n-Đông Gia Huy\n-Kiều Hải Sơn\n-Lê Kim Tân\n Dựa trên trò chơi TETRIS phát triển năm 1984 bởi  Alexey Pajitnov.";
        //        richTextBox1.SelectAll();
        //        richTextBox1.SelectionAlignment = HorizontalAlignment.Center;
        //    }

        //}
        private void btn_credits_Click(object sender, EventArgs e)
        {
                btn_back.Visible = true;
                panel2.Visible = true;
                btn_credits.Visible = false;
                btn_Play.Visible = false;
                richTextBox1.Text = "2020 Tetris\nXây dựng bởi: \n-Đông Gia Huy\n-Kiều Hải Sơn\n-Lê Kim Tân\n Dựa trên trò chơi TETRIS phát triển năm 1984 bởi  Alexey Pajitnov.";
                richTextBox1.SelectAll();
                richTextBox1.SelectionAlignment = HorizontalAlignment.Center;
        }

        private void btn_back_Click(object sender, EventArgs e)
        {
            panel2.Visible = false;
            btn_credits.Visible = true;
            btn_Play.Visible = true;
            btn_back.Visible = false;
        }
    }
}
