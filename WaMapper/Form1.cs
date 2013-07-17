using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Drawing2D;

namespace WaMapper
{
    public partial class Form1 : Form
    {
        private const string header = "Wa!Mapper Ver. 0.1";
        private const string filter = "マップデータファイル(*.map)|*.map";
        private const string imageFilter = "画像ファイル(*.bmp)|*.bmp";

        private Pen gridPen;                        // グリッド用ペン
        private int grid = 8;                       // グリッドの幅

        private Bitmap mapImage;                    // 編集するマップの画像
        private string mapFileName = "map.txt";     // マップファイル名
        private int mapWidth = 64;                  // マップチップの横方向の個数
        private int mapHeight = 64;                 // マップチップの縦方向の個数
        private int scaling = 1;                    // スケーリング
        private int mapImageWidth;                  // マップ画像の幅
        private int mapImageHeight;                 // マップ画像の高さ
        private Bitmap tempBitmap;                  // 一時画像]
        private int[,] map;                         // マップ
        private bool putFlag;                       // マップに貼り付け中か？

        private Bitmap chipImage;                   // マップチップ画像
        private string chipFileName = "map.bmp";    // マップファイル名
        private int chipWidth = 8;                  // マップチップの幅
        private int chipHeight = 8;                 // マップチップの高さ
        private int chipCountX;                     // １行のチップ数
        private int chipCountY;                     // １列のチップ数
        private bool selectFlag;                    // チップ選択中か？
        private Point selectedStartChip;            // 選択されたチップ
        private Point selectedEndChip;              // 選択されたチップ
        private Rectangle selectedRect;             // 選択されたチップ範囲
        private Pen whitePen;                       // 選択用ペン

        /// <summary>
        /// コンストラクタ。
        /// </summary>
        public Form1()
        {
            InitializeComponent();
        }

        /// <summary>
        /// フォームを初期化する。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Form1_Load(object sender, EventArgs e)
        {
            // ダブルバッファリングする。
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true); 
            gridPen = new Pen(Color.Black, 1);
            gridPen.DashStyle = DashStyle.Dash;
            mapImageWidth = mapWidth * chipWidth;
            mapImageHeight = mapHeight * chipHeight;
            chipCountX = 1;
            chipCountY = 1;
            whitePen = new Pen(Color.White, 1);

            map = new int[mapHeight,mapWidth];
            for (int i = 0; i < mapHeight; i++)
            {
                for (int j = 0; j < mapWidth; j++)
                {
                    map[i, j] = 0;
                }
            }

            // スクロールバーを設定する。
            SetScrollBarValues();
            // 一時ビットマップを作成する。
            tempBitmap = new Bitmap(mapImageWidth, mapImageHeight);
        }
 
        /// <summary>
        /// マップチップ画像をロードする。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loadChipToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // 「ファイルを開く」ダイアログを表示する。
            openFileDialog1.Filter = imageFilter;
            DialogResult ret = openFileDialog1.ShowDialog();
            if (ret == System.Windows.Forms.DialogResult.OK)
            {
                // ダイアログからファイル名を取り出す。
                chipFileName = openFileDialog1.FileName;
                // 元の画像を消す。
                if (chipImage != null)
                {
                    chipImage.Dispose();
                    chipImage = null;
                }
                // 画像をロードする。
                chipImage = new Bitmap(chipFileName);
                // ラベルにファイル名を表示する。
                chipFileLabel.Text = chipFileName;
                // スクロールバーを設定する。
                //SetScrollBarValues2();
                chipCountX = chipImage.Width / chipWidth;
                chipCountY = chipImage.Height / chipHeight;

                DrawMapImage();
                // ピクチャボックスに表示する。
                chipPictureBox.Refresh();
                mapPictureBox.Refresh();
            }
        }

        /// <summary>
        /// 終了する。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        /// <summary>
        /// マップデータをロードする。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loadMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // ファイルを開くダイアログの表示
            openFileDialog1.Filter = filter;
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                // ファイルを開く
                using (FileStream fs = new FileStream(openFileDialog1.FileName, FileMode.Open, FileAccess.Read))
                {
                    // 現在のファイルを記憶しておく
                    mapFileName = openFileDialog1.FileName;
                    using (StreamReader sr = new StreamReader(mapFileName))
                    {
                        String line;
                        if ((line = sr.ReadLine()) != null)
                        {
                            string[] str = line.Split(',');
                            mapWidth = int.Parse(str[0]);
                            mapHeight = int.Parse(str[1]);
                        }
                        map = new int[mapHeight, mapWidth];
                        int i = 0;
                        while ((line = sr.ReadLine()) != null)
                        {
                            string[] str = line.Split(',');
                            for (int j = 0; j < mapWidth; j++)
                            {
                                map[i, j] = int.Parse(str[j]);
                            }
                            i++;
                        }
                    }
                }

                // マップイメージを更新
                DrawMapImage();
                // マップ画面更新
                mapPictureBox.Refresh();

                Form1.ActiveForm.Text = header + " " + mapFileName;
            }
        }

        /// <summary>
        /// マップを保存する。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (mapFileName != null)
            {
                // ファイルを作る
                using (StreamWriter sw = new StreamWriter(mapFileName))
                {
                    // 幅と高さを書き込む
                    sw.WriteLine(mapWidth + "," + mapHeight);

                    // データをすべて書き込む
                    for (int i = 0; i < mapHeight; i++)
                    {
                        for (int j = 0; j < mapWidth; j++)
                        {
                            // チップデータを書き込む
                            sw.Write(map[i, j] + ",");
                        }
                        sw.WriteLine();
                    }
                }
            }
            // 名前を付けて保存
            else
            {
                saveAsMapToolStripMenuItem_Click(sender, e);
            }
        }

        /// <summary>
        /// マップを名前を付けて保存する。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void saveAsMapToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (chipImage != null)
            {
                saveFileDialog1.Filter = filter;
                DialogResult ret = saveFileDialog1.ShowDialog();
                if (ret == System.Windows.Forms.DialogResult.OK)
                {
                    // ファイルを作る
                    mapFileName = saveFileDialog1.FileName;
                    saveMapToolStripMenuItem_Click(sender, e);
                }
            }
        }


        private void SetScrollBarValues()
        {
            // 最大値、最小値、大きな変更、小さな変更プロパティを設定する。
            vScrollBar1.Minimum = 0;
            hScrollBar1.Minimum = 0;
            vScrollBar1.Maximum = 0;
            hScrollBar1.Maximum = 0;

            // 画像の幅がピクチャボックスより大きいならその値を設定する。
            if (mapImageWidth > mapPictureBox.ClientSize.Width)
            {
                hScrollBar1.Maximum = mapImageWidth - mapPictureBox.ClientSize.Width;
            }
            // 垂直スクロールバーが可視なら、水平スクロールバーの最大値を
            // 垂直スクロールバーの幅を加算する。
            if (vScrollBar1.Visible)
            {
                hScrollBar1.Maximum += vScrollBar1.Width;
            }
            hScrollBar1.LargeChange = hScrollBar1.Maximum / 10;
            hScrollBar1.SmallChange = hScrollBar1.Maximum / 20;

            // ユーザ操作に対応できるように最大値を生の最大値に調整する。
            hScrollBar1.Maximum += hScrollBar1.LargeChange;

            // 画像の高さがピクチャボックスより大きいならその値を設定する。
            if (mapImageHeight > mapPictureBox.ClientSize.Height)
            {
                vScrollBar1.Maximum = mapImageHeight - mapPictureBox.ClientSize.Height;
            }

            // 水平スクロールバーが可視なら、水平スクロールバーの最大値を
            // 水平スクロールバーの高さを加算する。
            if (hScrollBar1.Visible)
            {
                vScrollBar1.Maximum += hScrollBar1.Height;
            }
            vScrollBar1.LargeChange = vScrollBar1.Maximum / 10;
            vScrollBar1.SmallChange = vScrollBar1.Maximum / 20;

            // ユーザ操作に対応できるように最大値を生の最大値に調整する。
            vScrollBar1.Maximum += vScrollBar1.LargeChange;
        }

        private void vScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            HandleScroll(sender, e);
        }

        private void hScrollBar1_Scroll(object sender, ScrollEventArgs e)
        {
            HandleScroll(sender, e);
        }
        private void HandleScroll(Object sender, ScrollEventArgs se)
        {
            mapPictureBox.Refresh();
        }
        private void SetScrollBarValues2()
        {
            // 最大値、最小値、大きな変更、小さな変更プロパティを設定する。
            vScrollBar1.Minimum = 0;
            hScrollBar1.Minimum = 0;
            vScrollBar1.Maximum = 0;
            hScrollBar1.Maximum = 0;

            // 画像の幅がピクチャボックスより大きいならその値を設定する。
            if (chipImage.Width > chipPictureBox.ClientSize.Width)
            {
                hScrollBar1.Maximum = chipImage.Width * scaling - mapPictureBox.ClientSize.Width;
            }
            // 垂直スクロールバーが可視なら、水平スクロールバーの最大値を
            // 垂直スクロールバーの幅を加算する。
            if (vScrollBar1.Visible)
            {
                hScrollBar1.Maximum += vScrollBar1.Width;
            }
            hScrollBar1.LargeChange = hScrollBar1.Maximum / 10;
            hScrollBar1.SmallChange = hScrollBar1.Maximum / 20;

            // ユーザ操作に対応できるように最大値を生の最大値に調整する。
            hScrollBar1.Maximum += hScrollBar1.LargeChange;

            // 画像の高さがピクチャボックスより大きいならその値を設定する。
            if (chipImage.Height > chipPictureBox.ClientSize.Height)
            {
                vScrollBar1.Maximum = chipImage.Height * scaling - mapPictureBox.ClientSize.Height;
            }

            // 水平スクロールバーが可視なら、水平スクロールバーの最大値を
            // 水平スクロールバーの高さを加算する。
            if (hScrollBar1.Visible)
            {
                vScrollBar1.Maximum += hScrollBar1.Height;
            }
            vScrollBar1.LargeChange = vScrollBar1.Maximum / 10;
            vScrollBar1.SmallChange = vScrollBar1.Maximum / 20;

            // ユーザ操作に対応できるように最大値を生の最大値に調整する。
            vScrollBar1.Maximum += vScrollBar1.LargeChange;
        }

        /// <summary>
        /// マップ画像を描画するイベントを処理する。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mapPictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (e != null)
            {
                Graphics g = e.Graphics;
                g.DrawImage(tempBitmap, 0, 0);
            }
        }

        /// <summary>
        /// マップ画像全体を描画する。
        /// </summary>
        private void DrawMapImage()
        {
            if (chipImage != null)
            {
                //Graphics g = mapPictureBox.CreateGraphics();
                Graphics g = Graphics.FromImage(tempBitmap);
                for (int i = 0; i < mapHeight; i++)
                {
                    for (int j = 0; j < mapWidth; j++)
                    {
                        // 描画対象位置を計算する。
                        Rectangle rect = new Rectangle(j * chipWidth, i * chipHeight,
                            chipWidth, chipHeight);
                        // マップデータからチップ画像の位置を計算する。
                        int sx = map[i, j] % chipCountX;
                        int sy = map[i, j] / chipCountX;
                        // マップ画像にチップを描画する。
                        g.DrawImage(chipImage, rect,
                            sx * chipWidth, sy * chipHeight,
                            chipWidth, chipHeight, GraphicsUnit.Pixel);
                    }
                }
                // グリッドを描画する。
                if (grid > 0)
                {
                    for (int i = 0; i < mapPictureBox.Width; i += grid)
                    {
                        g.DrawLine(gridPen, i, 0, i, mapImageHeight);
                    }
                    for (int i = 0; i < mapPictureBox.Height; i += grid)
                    {
                        g.DrawLine(gridPen, 0, i, mapImageWidth, i);
                    }
                }
            }
        }

        /// <summary>
        /// マップ画像に１つのチップを描画する。
        /// </summary>
        /// <param name="dx">描画対象マップXインデックス</param>
        /// <param name="dy">描画対象マップYインデックス</param>
        /// <param name="sx">描画チップXインデックス</param>
        /// <param name="sy">描画チップYインデックス</param>
        private void DrawMapImage(int dx, int dy, int sx, int sy)
        {
            if (chipImage != null)
            {
                //Graphics g = mapPictureBox.CreateGraphics();
                Graphics g = Graphics.FromImage(tempBitmap);
                // 描画対象座標を計算する。
                Rectangle rect = new Rectangle(dx * chipWidth, dy * chipHeight,
                    chipWidth, chipHeight);
                // 一時画像に描画する。
                g.DrawImage(chipImage, rect,
                    sx * chipWidth, sy * chipHeight,
                    chipWidth, chipHeight, GraphicsUnit.Pixel);
                // グリッドを描画する。
                if (grid > 0)
                {
                    for (int i = 0; i < mapPictureBox.Width; i += grid)
                    {
                        g.DrawLine(gridPen, i, 0, i, mapImageHeight);
                    }
                    for (int i = 0; i < mapPictureBox.Height; i += grid)
                    {
                        g.DrawLine(gridPen, 0, i, mapImageWidth, i);
                    }
                }
            }
            mapPictureBox.Refresh();
        }

        /// <summary>
        /// マップデータ上でマウスが押された。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mapPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            // 選択されたマップインデックスを計算する。
            int dx = e.X / chipWidth;
            int dy = e.Y / chipHeight;
            // 選択されたチップでマップを更新する。
            for (int i = selectedStartChip.Y; i <= selectedEndChip.Y; i++)
            {
                for (int j = selectedStartChip.X; j <= selectedEndChip.X; j++)
                {
                    try
                    {
                        map[dy, dx] = i * chipCountX + j;
                        DrawMapImage(dx, dy, j, i);
                    }
                    catch
                    {
                        // 何もしない。
                    }
                    dx++;
                }
                dy++;
                dx = e.X / chipWidth;
            }
            putFlag = true;
        }

        /// <summary>
        /// マップ画像上でマウスが移動した。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mapPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (putFlag)
            {
                /*
                int dx = e.X / chipWidth;
                int dy = e.Y / chipHeight;
                try
                {
                    map[dy, dx] = selectedStartChip.Y * chipCountX + selectedStartChip.X;
                    DrawMapImage(dx, dy, selectedStartChip.X, selectedStartChip.Y);
                }
                catch
                {
                    putFlag = false;
                }
                 */
            }
        }

        /// <summary>
        /// マップ画像上でマウスが離された。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void mapPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            putFlag = false;
        }

        /// <summary>
        /// マップチップ上でマウスが押された。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chipPictureBox_MouseDown(object sender, MouseEventArgs e)
        {
            selectedStartChip.X = e.X / chipWidth;
            selectedStartChip.Y = e.Y / chipHeight;
            selectedRect.X = selectedStartChip.X * chipWidth;
            selectedRect.Y = selectedStartChip.Y * chipHeight;
            selectFlag = true;
        }

        /// <summary>
        /// マップチップ上でマウスが離された。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chipPictureBox_MouseUp(object sender, MouseEventArgs e)
        {
            // 念のためMouseMoveと同じ処理をしておく。
            selectedRect.Width = (e.X / chipWidth) * chipWidth - selectedRect.X;
            selectedRect.Height = (e.Y / chipHeight) * chipHeight - selectedRect.Y;
            selectedEndChip.X = (selectedRect.X + selectedRect.Width - 1) / chipWidth;
            selectedEndChip.Y = (selectedRect.Y + selectedRect.Height - 1) / chipHeight;
            // 選択画像を描画する。
            DrawSelectedImage();
            selectFlag = false;
        }

        /// <summary>
        /// チップ画像上でマウスが動いた。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chipPictureBox_MouseMove(object sender, MouseEventArgs e)
        {
            if (selectFlag)
            {
                selectedRect.Width = (e.X / chipWidth) * chipWidth - selectedRect.X;
                selectedRect.Height = (e.Y / chipHeight) * chipHeight - selectedRect.Y;
                selectedEndChip.X = (selectedRect.X + selectedRect.Width - 1) / chipWidth;
                selectedEndChip.Y = (selectedRect.Y + selectedRect.Height - 1) / chipHeight;
                toolStripStatusLabel1.Text = "Select (" + selectedStartChip.X + "," + selectedStartChip.Y
                    + ")-(" + selectedEndChip.X + "," + selectedEndChip.Y + ")";
                chipPictureBox.Refresh();
            }
        }

        /// <summary>
        /// チップ画像を描画する。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void chipPictureBox_Paint(object sender, PaintEventArgs e)
        {
            if (e != null)
            {
                Graphics g = e.Graphics;
                Rectangle rect;
                // 実際のイメージを描画する。
                if (chipImage != null)
                {
                    rect = new Rectangle(0, 0, chipImage.Width * scaling, chipImage.Height * scaling);
                    g.DrawImage(chipImage, rect, hScrollBar1.Value, vScrollBar1.Value, chipImage.Width, chipImage.Height, GraphicsUnit.Pixel);
                    // グリッドを描画する。
                    if (grid > 0)
                    {
                        for (int i = 0; i < chipPictureBox.Width && i < chipImage.Width * scaling; i += grid * scaling)
                        {
                            g.DrawLine(gridPen, i - hScrollBar1.Value, 0, i - hScrollBar1.Value, chipImage.Height * scaling);
                        }
                        for (int i = 0; i < chipPictureBox.Height && i < chipImage.Height * scaling; i += grid * scaling)
                        {
                            g.DrawLine(gridPen, 0, i - vScrollBar1.Value, chipImage.Width * scaling, i - vScrollBar1.Value);
                        }
                    }
                    if (selectFlag)
                    {
                        g.DrawRectangle(whitePen, selectedRect);
                    }
                }
            }
        }

        /// <summary>
        /// 選択されたマップチップ画像を描画する。
        /// </summary>
        private void DrawSelectedImage()
        {
            if (chipImage != null)
            {
                Graphics g = selectedPictureBox.CreateGraphics();
                g.Clear(Color.White);
                Rectangle rect = new Rectangle(0, 0,
                    selectedRect.Width, selectedRect.Height);
                g.DrawImage(chipImage, rect, selectedRect, GraphicsUnit.Pixel);
                //                selectedPictureBox.Refresh();
            }
        }
    }
}