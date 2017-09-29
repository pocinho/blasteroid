using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Media;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Media;
using System.Xml;

namespace Jogo
{
    public partial class FormJogo : Form
    {
        private Random rnd = new Random();

        // opções de jogo
        public int dificuldade = 2;
        public int numero_de_bots = 5;
        public int numero_de_asteroides = 5;
        public int numero_de_rochedos = 1;

        // sons
        private SoundPlayer sp_som = new SoundPlayer();
        public MediaPlayer mp_musica = new MediaPlayer();

        private string musica_01 = Application.StartupPath + "\\Resources\\nihil_loop.wav";
        private string musica_02 = Application.StartupPath + "\\Resources\\circuit_beat.wav";

        // definicoes de programa
        private string ficheiro_scores = "scores.txt";
        private string ficheiro_config = "config.xml";

        // numero de bots a abater antes do boss
        private const int boss_threshold = 20;
        private int bots_abatidos = 0;

        // espaco para alocacar dinamicamente pb's
        private List<PictureBox> disparos_jogador = new List<PictureBox>();
        private List<PictureBox> asteroids = new List<PictureBox>();
        private List<PictureBox> rochedos = new List<PictureBox>();
        private List<PictureBox> bots = new List<PictureBox>();
        private List<PictureBox> disparos_inimigo = new List<PictureBox>();
        private List<PictureBox> explosoes_boss = new List<PictureBox>();

        private bool bots_atacam = false;
        private bool mostra_explosoes = false;

        // timer para gif explosao
        Stopwatch stw_explosao_m01 = new Stopwatch();

        // timer de tempo de jogo
        Stopwatch stw_tempo_de_jogo = new Stopwatch();

        // timer de tempo para tiro OP do boss
        Stopwatch stw_disparo_boss = new Stopwatch();

        // pausa entre level ups
        long pausa_inicio = 0;
        long pausa_fim = 0;

        bool pausa_levelup = false;

        // controlo de movimentos
        private bool nave_muda_imagem = false;
        private bool nave_move_cima = false;
        private bool nave_move_baixo = false;
        private bool nave_move_esq = false;
        private bool nave_move_dir = false;
        private bool boss_move_esq = false;

        // variaveis de jogo
        private int vidas = 3;
        private const int pontos_meteorito_01 = 30;
        private const int pontos_asteroids = 20;
        private int vidas_meteorito_01 = 2;
        private int boss_hp = 100;

        public FormJogo()
        {
            InitializeComponent();
        }

        private void FormJogo_Load(object sender, EventArgs e)
        {
            InicializarOpcoes();

            // tocar musica em background com loop
            mp_musica.Open(new Uri(musica_01));
            mp_musica.MediaEnded += new EventHandler(PlaybackEnded);
            mp_musica.Play();

            lbl_game_time.Parent = pb_background;

            pb_gameover.Parent = pb_background;
            pb_gameover.Top = (pb_background.Bottom - 62) / 2;
            pb_gameover.Left = (pb_background.Right - 427) / 2;

            pb_level_up.Parent = pb_background;
            pb_level_up.Top = (pb_background.Bottom - 72) / 2;
            pb_level_up.Left = (pb_background.Right - 330) / 2;

            pb_titulo.Parent = pb_background;
            pb_titulo.Top = (pb_background.Bottom - 63) / 2;
            pb_titulo.Left = (pb_background.Right - 435) / 2;

            pb_nave.Parent = pb_background;

            pb_explosao_m01.Parent = pb_background;

            lbl_static_score.Parent = pb_background;
            lbl_static_nome.Parent = pb_background;
            lbl_score.Parent = pb_background;
            lbl_nome_jogador.Parent = pb_background;
            pb_vida_1.Parent = pb_background;
            pb_vida_2.Parent = pb_background;
            pb_vida_3.Parent = pb_background;

            lbl_static_nome.Top = pb_background.Bottom - 50;
            lbl_static_score.Top = pb_background.Bottom - 50;
            lbl_score.Top = pb_background.Bottom - 50;
            lbl_nome_jogador.Top = pb_background.Bottom - 50;

            pb_xplosion_01.Parent = pb_background;
            pb_xplosion_02.Parent = pb_background;
            pb_xplosion_03.Parent = pb_background;
            pb_xplosion_04.Parent = pb_background;
            pb_xplosion_05.Parent = pb_background;

            pb_boss.Parent = pb_background;
            pb_boss.Top = pb_background.Top - 700;
            pb_boss.Left = (pb_background.Right - 340) / 2;
            lbl_boss_hp.Parent = pb_background;

            pb_tiro_boss.Parent = pb_background;

            CarregarObjetos();
        }

        private void InicializarOpcoes()
        {
            if (File.Exists(ficheiro_config))
            {
                XmlDocument config = new XmlDocument();
                config.Load(ficheiro_config);

                dificuldade = Convert.ToInt32(config.SelectSingleNode("//local/jogo/opcoes/dificuldade").InnerText);
                numero_de_bots = Convert.ToInt32(config.SelectSingleNode("//local/jogo/opcoes/numero_de_bots").InnerText);
                numero_de_asteroides = Convert.ToInt32(config.SelectSingleNode("//local/jogo/opcoes/numero_de_asteroides").InnerText);
                numero_de_rochedos = Convert.ToInt32(config.SelectSingleNode("//local/jogo/opcoes/numero_de_rochedos").InnerText);
            }
            else
            {
                try
                {
                    StreamWriter w = new StreamWriter(ficheiro_config);
                    w.WriteLine("<local>");
                    w.WriteLine("    <jogo>");
                    w.WriteLine("        <opcoes>");
                    w.WriteLine("            <dificuldade>2</dificuldade>");
                    w.WriteLine("            <numero_de_bots>5</numero_de_bots>");
                    w.WriteLine("            <numero_de_asteroides>5</numero_de_asteroides>");
                    w.WriteLine("            <numero_de_rochedos>1</numero_de_rochedos>");
                    w.WriteLine("        </opcoes>");
                    w.WriteLine("    </jogo>");
                    w.WriteLine("</local>");
                    w.Close();
                }
                catch (Exception e)
                {
                    // escrever para consola log de erros (o jogo tem de ser lancado no cmd para tal efeito)
                    Console.WriteLine(e.Message);
                    Console.WriteLine("Nao foi possivel gerar ficheiro \"" + ficheiro_config + "\"");
                }
            }
        }

        // evento que toma conta do loop do mediaplayer
        private void PlaybackEnded(object sender, EventArgs e)
        {
            mp_musica.Position = TimeSpan.Zero;
            mp_musica.Play();
        }

        private void FormJogo_FormClosed(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void FormJogo_KeyDown(object sender, KeyEventArgs e)
        {

            switch (e.KeyCode)
            {
                case Keys.Left:
                    nave_move_esq = true;
                    break;
                case Keys.Right:
                    nave_move_dir = true;
                    break;
                case Keys.Up:
                    nave_move_cima = true;
                    break;
                case Keys.Down:
                    nave_move_baixo = true;
                    break;
                case Keys.Space:
                    Disparo();
                    break;
            }

            MoverNave();
            timer_nave.Start();
        }

        private void Disparo()
        {
            sp_som.Stream = Properties.Resources.laser_8_bit;
            sp_som.Play();
            PictureBox pb_tiro = new PictureBox();
            pb_tiro.Image = Properties.Resources.beam_red;
            pb_tiro.BackColor = System.Drawing.Color.Transparent;
            pb_tiro.SizeMode = PictureBoxSizeMode.AutoSize;
            pb_tiro.Parent = pb_background;
            pb_tiro.Top = pb_nave.Top;
            pb_tiro.Left = pb_nave.Left + 18;
            disparos_jogador.Add(pb_tiro);
        }

        private void DisparoInimigo(PictureBox pb)
        {
            sp_som.Stream = Properties.Resources.shoot;
            sp_som.Play();
            PictureBox pb_tiro = new PictureBox();
            pb_tiro.Image = Properties.Resources.beam_blue;
            pb_tiro.BackColor = System.Drawing.Color.Transparent;
            pb_tiro.SizeMode = PictureBoxSizeMode.AutoSize;
            pb_tiro.Parent = pb_background;
            pb_tiro.Top = pb.Top;
            pb_tiro.Left = pb.Left + 25;
            disparos_inimigo.Add(pb_tiro);
        }

        private void MoverNaveBaixo()
        {
            if (pb_nave.Bottom < (pb_background.Bottom - 80))
                pb_nave.Top = pb_nave.Top + 10;
        }

        private void MoverNaveCima()
        {
            if (pb_nave.Top > 20)
                pb_nave.Top = pb_nave.Top - 10;
        }

        private void MoverNaveDir()
        {
            if (nave_muda_imagem)
            {
                pb_nave.Image = Properties.Resources.smallfighter0011;
                nave_muda_imagem = false;
            }

            if (pb_nave.Left < (pb_background.Right - 100))
                pb_nave.Left = pb_nave.Left + 10;
        }

        private void MoverNaveEsq()
        {
            if (nave_muda_imagem)
            {
                pb_nave.Image = Properties.Resources.smallfighter0001;
                nave_muda_imagem = false;
            }

            if (pb_nave.Left > 10)
                pb_nave.Left = pb_nave.Left - 10;
        }

        private void FormJogo_KeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyValue)
            {
                case (int)Keys.Left:
                    nave_move_esq = false;
                    break;
                case (int)Keys.Right:
                    nave_move_dir = false;
                    break;
                case (int)Keys.Up:
                    nave_move_cima = false;
                    break;
                case (int)Keys.Down:
                    nave_move_baixo = false;
                    break;
            }
            timer_nave.Stop();
            pb_nave.Image = new Bitmap(Properties.Resources.smallfighter0006);
            nave_muda_imagem = true;
        }

        private void novoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            frm_novo = new FormNovo();
            frm_novo.frm_jogo = this;
            frm_novo.Show();
            this.Hide();
        }


        public void InicializarJogo()
        {
            lbl_game_time.Visible = true;
            lbl_game_time.Text = "00:00:00";

            if (musicaToolStripMenuItem.Checked)
            {
                mp_musica.Open(new Uri(musica_01));
                mp_musica.Play();
            }

            lbl_nome_jogador.Text = frm_novo.tb_nome_jogador.Text;
            lbl_score.Text = 0.ToString();

            pb_titulo.Visible = false;
            pb_gameover.Visible = false;
            pb_level_up.Visible = false;

            vidas = 3;
            pb_vida_1.Visible = true;
            pb_vida_2.Visible = true;
            pb_vida_3.Visible = true;

            bots_abatidos = 0;
            bots_atacam = false;

            timer_boss.Stop();

            pb_boss.Top = pb_background.Top - 700;
            boss_hp = 100;
            lbl_boss_hp.Visible = false;
            lbl_boss_hp.Text = "HP: 100%";

            pb_explosao_m01.Parent = pb_background;

            pb_nave.Top = pb_background.Bottom - 200;
            pb_nave.Left = pb_background.Right / 2;
            pb_nave.Visible = true;

            lbl_nome_jogador.Visible = true;
            lbl_score.Visible = true;
            lbl_static_nome.Visible = true;
            lbl_static_score.Visible = true;

            pb_xplosion_01.Visible = false;
            pb_xplosion_02.Visible = false;
            pb_xplosion_03.Visible = false;
            pb_xplosion_04.Visible = false;
            pb_xplosion_05.Visible = false;

            CarregarObjetos();

            // colocar dinamicamente rochedos
            foreach (PictureBox p in rochedos)
            {
                p.Top = rnd.Next(-500, -100);
                p.Left = rnd.Next(0, pb_background.Right - 100);
                p.Visible = true;
            }

            // colocar dinamicamente asteroids
            foreach (PictureBox p in asteroids)
            {
                p.Top = rnd.Next(-500, -100);
                p.Left = rnd.Next(0, pb_background.Right - 100);
            }

            // colocar dinamicamente bots
            foreach (PictureBox p in bots)
            {
                p.Top = rnd.Next(-500, -100);
                p.Left = rnd.Next(0, pb_background.Right - 100);
            }

            // limpar tiros do jogador, se existirem
            for (int i = 0; i < disparos_jogador.Count; ++i)
            {
                disparos_jogador[i].Visible = false;
            }
            disparos_jogador.Clear();

            // limpar tiros do inimigo, se existirem
            for (int i = 0; i < disparos_inimigo.Count; ++i)
            {
                disparos_inimigo[i].Visible = false;
            }
            disparos_inimigo.Clear();

            timer_asteroids.Start();
            stw_tempo_de_jogo.Start();
        }

        private void CarregarObjetos()
        {
            // reset dados dinamicos, opcoes podem ter mudado
            rochedos.Clear();
            asteroids.Clear();
            bots.Clear();

            // carregar rochedos dinamicamente:
            for (int i = 0; i < numero_de_rochedos; ++i)
            {
                PictureBox pb = new PictureBox();
                pb.BackColor = System.Drawing.Color.Transparent;
                pb.Parent = pb_background;
                pb.SizeMode = PictureBoxSizeMode.CenterImage;
                pb.Image = Properties.Resources.Meteor_1;
                pb.Width = 141;
                pb.Height = 157;
                pb.Top = rnd.Next(-600, -200);
                pb.Left = rnd.Next(0, pb_background.Right - 100);
                rochedos.Add(pb);
            }

            //carregar asteroids dinamicamente:
            for (int i = 0; i < numero_de_asteroides; ++i)
            {
                PictureBox pb = new PictureBox();
                pb.BackColor = System.Drawing.Color.Transparent;
                pb.Parent = pb_background;
                pb.SizeMode = PictureBoxSizeMode.StretchImage;
                pb.Width = 30;
                pb.Height = 30;
                pb.Top = rnd.Next(-600, -100);
                pb.Left = rnd.Next(0, pb_background.Right - 100);
                asteroids.Add(pb);
            }
            // temos pelo menos 3 asteroides diferentes
            asteroids[0].Image = new Bitmap(Properties.Resources.asteroid_01);
            asteroids[1].Image = new Bitmap(Properties.Resources.asteroid_02);
            asteroids[2].Image = new Bitmap(Properties.Resources.asteroid_03);
            if (asteroids.Count == 5)
            {
                asteroids[3].Image = new Bitmap(Properties.Resources.asteroid_04);
                asteroids[4].Image = new Bitmap(Properties.Resources.asteroid_05);
            }
            else if (asteroids.Count == 8)
            {
                asteroids[3].Image = new Bitmap(Properties.Resources.asteroid_04);
                asteroids[4].Image = new Bitmap(Properties.Resources.asteroid_05);
                asteroids[5].Image = new Bitmap(Properties.Resources.asteroid_01);
                asteroids[6].Image = new Bitmap(Properties.Resources.asteroid_02);
                asteroids[7].Image = new Bitmap(Properties.Resources.asteroid_03);
            }

            // carregar bots dinamicamente:
            for (int i = 0; i < numero_de_bots; ++i)
            {
                PictureBox pb = new PictureBox();
                pb.BackColor = System.Drawing.Color.Transparent;
                pb.Parent = pb_background;
                pb.SizeMode = PictureBoxSizeMode.StretchImage;
                pb.Image = new Bitmap(Properties.Resources.bot);
                pb.Width = 60;
                pb.Height = 60;
                pb.Top = rnd.Next(-600, -100);
                pb.Left = (i * 20) + 30;
                bots.Add(pb);
            }
        }

        private void timer_asteroids_Tick(object sender, EventArgs e)
        {
            lbl_game_time.Text = stw_tempo_de_jogo.Elapsed.ToString(@"mm\:ss\:ff");

            if (pausa_levelup)
            {
                pausa_fim = stw_tempo_de_jogo.ElapsedMilliseconds;
                if (pausa_fim - pausa_inicio > 10000)
                {
                    pausa_levelup = false;
                    InicializarLevelUp();
                }
            }
            else
            {
                // verificar colisoes com a nave do jogador
                foreach (PictureBox p in asteroids)
                {
                    if (p.Bounds.IntersectsWith(pb_nave.Bounds))
                    {
                        Colisao(pb_nave);
                        p.Top = rnd.Next(-600, -100);
                        p.Left = rnd.Next(0, pb_background.Right);
                    }
                    p.Top = p.Top + 10;
                    if (p.Bottom > (pb_background.Bottom + 200))
                        p.Location = new Point(rnd.Next(0, pb_background.Right - 100), rnd.Next(-600, -100));
                }

                foreach (PictureBox p in rochedos)
                {
                    if (p.Bounds.IntersectsWith(pb_nave.Bounds))
                    {
                        Colisao(pb_nave);
                        p.Top = rnd.Next(-600, -200);
                        p.Left = rnd.Next(0, pb_background.Right - 100);
                        vidas_meteorito_01 = 2;
                    }
                    p.Top = p.Top + 8;
                    if (p.Bottom > (pb_background.Bottom + 200))
                    {
                        p.Location = new Point(rnd.Next(0, pb_background.Right - 100), rnd.Next(-600, -200));
                        vidas_meteorito_01 = 2;
                    }
                }

                // verificar disparos do jogador
                if (disparos_jogador.Count > 0)
                {
                    for (int i = 0; i < disparos_jogador.Count; ++i)
                    {
                        disparos_jogador[i].Top = disparos_jogador[i].Top - 20;
                        if (disparos_jogador[i].Bottom < -5)
                            disparos_jogador.Remove(disparos_jogador[i]);

                        // verificar rochedos
                        for (int r = 0; r < rochedos.Count; ++r)
                        {
                            if (i < disparos_jogador.Count && disparos_jogador[i].Bounds.IntersectsWith(rochedos[r].Bounds))
                            {
                                disparos_jogador[i].Enabled = false;
                                disparos_jogador[i].Visible = false;
                                disparos_jogador.Remove(disparos_jogador[i]);
                                TiroNoAlvo(rochedos[r]);
                            }
                        }

                        // verificar asteroids
                        for (int a = 0; a < asteroids.Count; ++a)
                        {
                            if (i < disparos_jogador.Count && disparos_jogador[i].Bounds.IntersectsWith(asteroids[a].Bounds))
                            {
                                disparos_jogador[i].Enabled = false;
                                disparos_jogador[i].Visible = false;
                                disparos_jogador.Remove(disparos_jogador[i]);
                                TiroNoAlvo(asteroids[a]);
                            }
                        }

                        // verificar bots
                        for (int b = 0; b < bots.Count; ++b)
                        {
                            if (i < disparos_jogador.Count && disparos_jogador[i].Bounds.IntersectsWith(bots[b].Bounds))
                            {
                                disparos_jogador[i].Enabled = false;
                                disparos_jogador[i].Visible = false;
                                disparos_jogador.Remove(disparos_jogador[i]);
                                TiroNoAlvo(bots[b]);
                                bots_abatidos++;
                            }
                        }
                    }
                }


                if (stw_explosao_m01.ElapsedMilliseconds > 600)
                {
                    stw_explosao_m01.Stop();
                    stw_explosao_m01.Reset();
                    pb_explosao_m01.Enabled = false;
                    pb_explosao_m01.Visible = false;
                }

                VerificarBotsAtacam();
            }
        }

        private void VerificarBotsAtacam()
        {
            // Score que inicia ataque dos bots
            if (!bots_atacam && Convert.ToInt32(lbl_score.Text) > 500)
            {
                bots_atacam = true;
                timer_bots.Start();
            }
        }

        private void TiroNoAlvo(PictureBox pb)
        {
            int pontos = Convert.ToInt32(lbl_score.Text);

            sp_som.Stream = Properties.Resources.explosion_asteroid;
            sp_som.Play();

            // BUG: explosoes nao aparecem para rochedos > 1
            // TODO: Melhorar este metodo
            foreach (PictureBox p in rochedos)
            {
                if (pb.Equals(p))
                {
                    pb_explosao_m01.Top = pb.Top;
                    pb_explosao_m01.Left = pb.Left;
                    pb_explosao_m01.Width = 200;
                    pb_explosao_m01.Height = 282;
                    pontos += pontos_meteorito_01;
                    vidas_meteorito_01 -= 1;
                    if (vidas_meteorito_01 <= 0)
                    {
                        pb.Top = rnd.Next(-600, -100);
                        pb.Left = rnd.Next(0, pb_background.Right - 200);
                        vidas_meteorito_01 = 2;
                    }
                }
                else
                {
                    // fazer exposao mais pequena
                    pb_explosao_m01.Top = pb.Top - 20;
                    pb_explosao_m01.Left = pb.Left - 30;
                    pb_explosao_m01.Width = 100;
                    pb_explosao_m01.Height = 141;

                    pb.Top = rnd.Next(-500, -100);
                    pb.Left = rnd.Next(0, pb_background.Right - 200);

                    pontos += pontos_asteroids;
                }
            }

            pb_explosao_m01.Enabled = true;
            pb_explosao_m01.Visible = true;
            stw_explosao_m01.Start();

            lbl_score.Text = pontos.ToString();
        }

        private void TiraVidas()
        {
            vidas -= 1;
            pb_nave.Left = pb_background.Right / 2;
            pb_nave.Top = pb_background.Bottom - 200;

            if (vidas == 2)
                pb_vida_1.Visible = false;
            else if (vidas == 1)
                pb_vida_2.Visible = false;
            else if (vidas == 0)
                pb_vida_3.Visible = false;
            else if (vidas < 0)
                FimDeJogo();
        }

        private void FimDeJogo()
        {
            pb_gameover.Visible = true;
            pb_nave.Visible = false;
            pb_explosao_m01.Visible = false;
            stw_explosao_m01.Stop();
            stw_explosao_m01.Reset();
            stw_tempo_de_jogo.Stop();
            timer_asteroids.Stop();
            timer_bots.Stop();
            timer_boss.Stop();
            bots_atacam = false;
            EscreverScores();
            stw_tempo_de_jogo.Reset();
            MostrarTop10();
        }

        private void MostrarTop10()
        {
            StringBuilder msg = new StringBuilder();
            List<Score> top10 = LerScores();
            string[] entrada = new string[3];

            msg.Append("Posição\tTempo\tPontos\tNome\n\n");

            for (int i = 0; i < top10.Count(); ++i)
            {
                entrada = top10[i].ToString().Split(';');
                msg.Append((i + 1) + "\t" + entrada[1] + "\t" + entrada[0] + "\t" + entrada[2] + "\n");
            }

            MessageBox.Show(msg.ToString(), "Top 10", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void EscreverScores()
        {
            List<Score> top10 = LerScores();

            top10.Add(new Score(Convert.ToInt32(lbl_score.Text), stw_tempo_de_jogo.Elapsed.ToString(@"mm\:ss\:ff"), lbl_nome_jogador.Text));

            OrdenarScores(top10);

            //limitar ao top 10 de high scores
            if (top10.Count > 10)
                top10.RemoveRange(10, top10.Count - 10);

            FileInfo fi = new FileInfo(ficheiro_scores);
            StreamWriter w = fi.CreateText();
            try
            {
                foreach (Score s in top10)
                    w.WriteLine(s);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Nao foi possivel escrever no ficheiro \"" + ficheiro_scores + "\"");
            }
            finally
            {
                w.Close();
            }
        }

        // BubbleSort
        private void OrdenarScores(List<Score> scores)
        {
            Score temp;
            for (int i = scores.Count - 1; i >= 0; --i)
            {
                for (int j = 0; j < i; ++j)
                {
                    if (scores[j].ScoreTotal < scores[j + 1].ScoreTotal)
                    {
                        temp = scores[j];
                        scores[j] = scores[j + 1];
                        scores[j + 1] = temp;
                    }
                }
            }
        }

        private List<Score> LerScores()
        {
            List<Score> top10 = new List<Score>();

            if (File.Exists(ficheiro_scores))
            {
                string[] entrada = new string[3];
                Score score_jogador;
                StreamReader str = File.OpenText(ficheiro_scores);
                string read = null;

                while ((read = str.ReadLine()) != null)
                {
                    entrada = read.Split(';');
                    score_jogador = new Score(Convert.ToInt32(entrada[0]), entrada[1], entrada[2]);
                    top10.Add(score_jogador);
                }
                str.Close();
            }
            return top10;
        }

        private void Colisao(PictureBox pb)
        {
            sp_som.Stream = Properties.Resources.explosion_ship;
            sp_som.Play();

            pb_explosao_m01.Width = 200;
            pb_explosao_m01.Height = 282;

            pb_explosao_m01.Top = pb.Top;
            pb_explosao_m01.Left = pb.Left;
            pb_explosao_m01.Enabled = true;
            pb_explosao_m01.Visible = true;
            stw_explosao_m01.Start();
            TiraVidas();
        }

        private void sairToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void timer_nave_Tick(object sender, EventArgs e)
        {
            MoverNave();
        }

        private void MoverNave()
        {
            if (nave_move_baixo) MoverNaveBaixo();
            if (nave_move_cima) MoverNaveCima();
            if (nave_move_esq) MoverNaveEsq();
            if (nave_move_dir) MoverNaveDir();
        }

        private void FormJogo_Resize(object sender, EventArgs e)
        {
            if (pb_background.Bottom > 712 || pb_background.Right > 1278)
                pb_background.Image = Properties.Resources.starfield_1080p;

            if (pb_background.Bottom < 712 && pb_background.Right < 1278)
                pb_background.Image = Properties.Resources.bg_starfield;

            pb_titulo.Top = (pb_background.Bottom - 63) / 2;
            pb_titulo.Left = (pb_background.Right - 435) / 2;

            pb_gameover.Top = (pb_background.Bottom - 62) / 2;
            pb_gameover.Left = (pb_background.Right - 427) / 2;

            pb_level_up.Top = (pb_background.Bottom - 72) / 2;
            pb_level_up.Left = (pb_background.Right - 330) / 2;

            lbl_static_nome.Top = pb_background.Bottom - 50;
            lbl_static_score.Top = pb_background.Bottom - 50;
            lbl_score.Top = pb_background.Bottom - 50;
            lbl_nome_jogador.Top = pb_background.Bottom - 50;

            pb_vida_1.Top = pb_background.Bottom - 70;
            pb_vida_2.Top = pb_background.Bottom - 70;
            pb_vida_3.Top = pb_background.Bottom - 70;

            pb_vida_1.Left = pb_background.Right - 130;
            pb_vida_2.Left = pb_background.Right - 100;
            pb_vida_3.Left = pb_background.Right - 70;

            if (pb_nave.Right > pb_background.Right)
                pb_nave.Left = pb_background.Right - 200;
            if (pb_nave.Bottom > pb_background.Bottom)
                pb_nave.Top = pb_background.Bottom - 200;

            lbl_game_time.Left = pb_background.Right - lbl_game_time.Right + lbl_game_time.Left;

        }

        private void verScoresToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MostrarTop10();
        }

        private void controlosToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            FormControlos fc = new FormControlos();
            fc.StartPosition = FormStartPosition.Manual;
            fc.Location = new Point(this.Location.X + (this.Width - fc.Width) / 2, this.Location.Y + (this.Height - fc.Height) / 2);
            fc.Show(this);
        }

        private void acercaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormAcerca fa = new FormAcerca();
            fa.fj = this;
            fa.StartPosition = FormStartPosition.Manual;
            fa.Location = new Point(this.Location.X + (this.Width - fa.Width) / 2, this.Location.Y + (this.Height - fa.Height) / 2);
            fa.Show(this);
        }

        private void musicaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (musicaToolStripMenuItem.Checked)
            {
                mp_musica.Stop();
                musicaToolStripMenuItem.Checked = false;
                musicaToolStripMenuItem.Text = "Musica Off";
            }
            else
            {
                mp_musica.Play();
                musicaToolStripMenuItem.Checked = true;
                musicaToolStripMenuItem.Text = "Musica On";
            }

        }

        private void timer_bots_Tick(object sender, EventArgs e)
        {
            lbl_game_time.Text = stw_tempo_de_jogo.Elapsed.ToString(@"mm\:ss\:ff");

            if (bots_abatidos > 10)
                InicializarBoss();

            // verificar disparos dos bots
            if (disparos_inimigo.Count > 0)
            {
                for (int i = 0; i < disparos_inimigo.Count; ++i)
                {
                    disparos_inimigo[i].Top = disparos_inimigo[i].Top + 20;

                    if (disparos_inimigo[i].Top > pb_background.Bottom + 30)
                    {
                        disparos_inimigo[i].Visible = false;
                        disparos_inimigo[i].Enabled = false;
                        disparos_inimigo.Remove(disparos_inimigo[i]);
                    }

                    if (i < disparos_inimigo.Count && disparos_inimigo[i].Bounds.IntersectsWith(pb_nave.Bounds))
                    {
                        disparos_inimigo[i].Enabled = false;
                        disparos_inimigo[i].Visible = false;
                        disparos_inimigo.Remove(disparos_inimigo[i]);
                        Colisao(pb_nave);
                    }
                }
            }

            for (int i = 0; i < bots.Count; ++i)
            {
                if (i == 0)
                {
                    if (bots[i].Left > pb_nave.Right)
                        bots[i].Left -= 10;

                    else if (bots[i].Left < pb_nave.Left)
                        bots[i].Left += 10;

                    if (bots[i].Top <= pb_nave.Top)
                        bots[i].Top += 10;

                    else if (bots[i].Top >= pb_nave.Top)
                        bots[i].Top -= 10;
                }
                else
                {
                    if (bots[i].Left > pb_nave.Right && !bots[i - 1].Bounds.IntersectsWith(bots[i].Bounds))
                        bots[i].Left -= 10;

                    else if (bots[i].Left < pb_nave.Left && !bots[i - 1].Bounds.IntersectsWith(bots[i].Bounds))
                        bots[i].Left += 10;

                    if (bots[i].Top <= pb_nave.Top && !bots[i - 1].Bounds.IntersectsWith(bots[i].Bounds))
                        bots[i].Top += 10;

                    else if (bots[i].Top >= pb_nave.Top && !bots[i - 1].Bounds.IntersectsWith(bots[i].Bounds))
                        bots[i].Top -= 10;
                }

                if (bots[i].Bounds.IntersectsWith(pb_nave.Bounds))
                {
                    bots[i].Top = rnd.Next(-600, -100);
                    bots[i].Left = rnd.Next(0, pb_background.Right - 100);
                    Colisao(pb_nave);
                }

                int dispara = rnd.Next(0, 2);
                if (dispara > 0 && disparos_inimigo.Count < i)
                {
                    DisparoInimigo(bots[i]);
                }
            }
        }

        private void InicializarBoss()
        {
            // mudar musica
            if (musicaToolStripMenuItem.Checked)
            {
                mp_musica.Open(new Uri(musica_02));
                mp_musica.Play();
            }

            // parar timers anteriores
            timer_asteroids.Stop();
            timer_bots.Stop();

            // limpar lasers no jogo
            for (int i = 0; i < disparos_inimigo.Count; ++i)
            {
                disparos_inimigo[i].Visible = false;
                disparos_inimigo[i].Enabled = false;
            }
            disparos_inimigo.Clear();

            for (int i = 0; i < disparos_jogador.Count; ++i)
            {
                disparos_jogador[i].Visible = false;
                disparos_jogador[i].Enabled = false;
            }
            disparos_jogador.Clear();

            // rebentar e remover todos os bots
            for (int i = 0; i < bots.Count; ++i)
            {
                bots[i].Top = rnd.Next(-300, -100);
                bots[i].Left = rnd.Next(0, pb_background.Right - 50);
            }

            // rebentar e remover todos os asteroids
            for (int i = 0; i < asteroids.Count; ++i)
            {
                asteroids[i].Top = rnd.Next(-300, -100);
                asteroids[i].Left = rnd.Next(0, pb_background.Right - 50);
            }

            // rebentar e remover todos os rochedos
            for (int i = 0; i < rochedos.Count; ++i)
            {
                rochedos[i].Top = rnd.Next(-600, -200);
                rochedos[i].Left = rnd.Next(0, pb_background.Right - 100);
            }

            pb_explosao_m01.Visible = false;

            // mostrar explosões:
            MostrarExplosoes();

            pb_boss.Visible = true;
            lbl_boss_hp.Visible = true;

            // comecar timer boss
            timer_boss.Start();
        }

        private void MostrarExplosoes()
        {
            mostra_explosoes = true;

            pb_xplosion_01.Top = pb_background.Bottom / 2;
            pb_xplosion_01.Left = pb_background.Left + 20;
            pb_xplosion_01.Visible = true;

            pb_xplosion_02.Top = pb_background.Bottom / 2;
            pb_xplosion_02.Left = pb_xplosion_01.Right + 20;
            pb_xplosion_02.Visible = true;

            pb_xplosion_03.Top = pb_background.Bottom / 2;
            pb_xplosion_03.Left = pb_xplosion_02.Right + 20;
            pb_xplosion_03.Visible = true;

            pb_xplosion_04.Top = pb_background.Bottom / 2;
            pb_xplosion_04.Left = pb_xplosion_03.Right + 20;
            pb_xplosion_04.Visible = true;

            pb_xplosion_05.Top = pb_background.Bottom / 2;
            pb_xplosion_05.Left = pb_xplosion_04.Right + 20;
            pb_xplosion_05.Visible = true;

            // TODO: escolher som mais comprido.
            sp_som.Stream = Properties.Resources.explosion_ship;
            sp_som.Play();
        }

        private void timer_boss_Tick(object sender, EventArgs e)
        {
            lbl_game_time.Text = stw_tempo_de_jogo.Elapsed.ToString(@"mm\:ss\:ff");

            if (mostra_explosoes && pb_xplosion_01.Bottom > pb_background.Top - 30)
            {
                pb_xplosion_01.Top -= 20;
                pb_xplosion_02.Top -= 20;
                pb_xplosion_03.Top -= 20;
                pb_xplosion_04.Top -= 20;
                pb_xplosion_05.Top -= 20;
            }
            else
            {
                pb_xplosion_01.Visible = false;
                pb_xplosion_02.Visible = false;
                pb_xplosion_03.Visible = false;
                pb_xplosion_04.Visible = false;
                pb_xplosion_05.Visible = false;
                mostra_explosoes = false;
            }

            // morre quando toca no boss
            if (pb_nave.Bounds.IntersectsWith(pb_boss.Bounds))
                Colisao(pb_nave);

            if (pb_boss.Top < pb_background.Top + 50)
            {
                pb_boss.Top += 10;
            }
            else
            {
                if (boss_move_esq)
                    boss_move_esq = BossMoveEsq();
                else
                    boss_move_esq = BossMoveDir();
            }

            if (disparos_jogador.Count > 0)
            {
                for (int i = 0; i < disparos_jogador.Count; ++i)
                {
                    disparos_jogador[i].Top = disparos_jogador[i].Top - 20;
                    if (disparos_jogador[i].Top < -30)
                        disparos_jogador.Remove(disparos_jogador[i]);

                    if (i > 0 && i < disparos_jogador.Count && disparos_jogador[i].Bounds.IntersectsWith(pb_boss.Bounds))
                    {
                        disparos_jogador[i].Enabled = false;
                        disparos_jogador[i].Visible = false;
                        disparos_jogador.Remove(disparos_jogador[i]);
                        TiroNoBoss();
                    }
                }
            }

            if (stw_explosao_m01.ElapsedMilliseconds > 600)
            {
                stw_explosao_m01.Stop();
                stw_explosao_m01.Reset();
                pb_explosao_m01.Visible = false;
            }

            if (disparos_inimigo.Count == 0)
            {
                DisparoBoss(pb_boss);
            }
            else
            {
                for (int i = 0; i < disparos_inimigo.Count; ++i)
                {
                    disparos_inimigo[i].Top += 20;
                    disparos_inimigo[i].Height += 10;

                    if (disparos_inimigo[i].Top > pb_background.Bottom + 300)
                        disparos_inimigo.Remove(disparos_inimigo[i]);
                }
            }

            if (disparos_inimigo.Count > 0)
            {
                foreach (PictureBox p in disparos_inimigo)
                {
                    if (disparos_inimigo[0].Bounds.IntersectsWith(pb_nave.Bounds))
                        Colisao(pb_nave);
                }
            }

        }

        private void DisparoBoss(PictureBox origem)
        {
            // TODO: som de disparo para o boss
            sp_som.Stream = Properties.Resources.shoot;
            sp_som.Play();
            PictureBox pb_tiro = new PictureBox();
            pb_tiro.Image = Properties.Resources.laser_beam_op;
            pb_tiro.SizeMode = PictureBoxSizeMode.StretchImage;
            pb_tiro.BackColor = System.Drawing.Color.Transparent;
            pb_tiro.Parent = pb_background;
            pb_tiro.Width = 200;
            pb_tiro.Height = 50;
            pb_tiro.Top = origem.Top + 50;
            pb_tiro.Left = origem.Left + 100;
            disparos_inimigo.Add(pb_tiro);

            // BUG: tiro não está centrado no meio do boss
            /*
            // replica para aparecer a frente do boss
            PictureBox pb_tiro_frente = new PictureBox();
            pb_tiro_frente.Image = Properties.Resources.laser_beam_op;
            pb_tiro_frente.SizeMode = PictureBoxSizeMode.StretchImage;
            pb_tiro_frente.BackColor = System.Drawing.Color.Transparent;
            pb_tiro_frente.Parent = pb_boss;
            pb_tiro_frente.Width = 200;
            pb_tiro_frente.Height = 50;
            pb_tiro_frente.Top = origem.Top + 50;
            pb_tiro_frente.Left = origem.Left; 
            disparos_inimigo.Add(pb_tiro_frente);
            */
        }

        private void TiroNoBoss()
        {
            pb_explosao_m01.Parent = pb_boss;
            pb_explosao_m01.Enabled = true;
            pb_explosao_m01.Visible = true;
            pb_explosao_m01.Width = 100;
            pb_explosao_m01.Height = 141;
            pb_explosao_m01.Top = pb_boss.Top + 50;
            pb_explosao_m01.Left = pb_boss.Left + 50;  // BUG: A explosão nao está centrada no boss

            stw_explosao_m01.Start();

            boss_hp -= 2;
            lbl_boss_hp.Text = "HP: " + boss_hp.ToString() + "%";
            if (boss_hp <= 0)
                GanhouNivel();
        }

        private void GanhouNivel()
        {
            if (musicaToolStripMenuItem.Checked)
            {
                mp_musica.Open(new Uri(Application.StartupPath + "\\Resources\\win.wav"));
                mp_musica.Play();
            }

            lbl_score.Text = Convert.ToString(Convert.ToInt32(lbl_score.Text) + 1000);
            // limpar jogo
            timer_boss.Stop();
            pb_explosao_m01.Visible = false;
            stw_explosao_m01.Stop();
            stw_explosao_m01.Reset();

            // limpar lasers no jogo
            for (int i = 0; i < disparos_inimigo.Count; ++i)
            {
                disparos_inimigo[i].Visible = false;
                disparos_inimigo[i].Enabled = false;
            }
            disparos_inimigo.Clear();

            for (int i = 0; i < disparos_jogador.Count; ++i)
            {
                disparos_jogador[i].Visible = false;
                disparos_jogador[i].Enabled = false;
            }
            disparos_jogador.Clear();

            bots_abatidos = 0;
            bots_atacam = false;

            pb_boss.Top = pb_background.Top - 700;
            boss_hp = 100;
            lbl_boss_hp.Visible = false;
            lbl_boss_hp.Text = "HP: 100%";

            pb_explosao_m01.Parent = pb_background;

            pb_nave.Top = pb_background.Bottom - 200;
            pb_nave.Left = pb_background.Right / 2;
            pb_nave.Visible = true;

            pb_level_up.Visible = true;
            pb_nave.Visible = false;

            pausa_levelup = true;
            pausa_inicio = stw_tempo_de_jogo.ElapsedMilliseconds;
            timer_asteroids.Start();
        }

        private bool BossMoveDir()
        {
            bool resultado = false;

            if (pb_boss.Right < pb_background.Right - 20)
                pb_boss.Left += 10;

            if (pb_boss.Right >= pb_background.Right - 20)
                resultado = true;

            return resultado;
        }

        private bool BossMoveEsq()
        {
            bool resultado = true;

            if (pb_boss.Left > pb_background.Left + 20)
                pb_boss.Left -= 10;

            if (pb_boss.Left <= pb_background.Left + 20)
                resultado = false;

            return resultado;
        }

        public void InicializarLevelUp()
        {
            pb_nave.Visible = true;

            if (musicaToolStripMenuItem.Checked)
            {
                mp_musica.Open(new Uri(musica_01));
                mp_musica.Play();
            }

            pb_level_up.Visible = false;

            lbl_nome_jogador.Visible = true;
            lbl_score.Visible = true;
            lbl_static_nome.Visible = true;
            lbl_static_score.Visible = true;

            // colocar dinamicamente rochedos
            foreach (PictureBox p in rochedos)
            {
                p.Top = rnd.Next(-600, -100);
                p.Left = rnd.Next(0, pb_background.Right - 100);
                p.Visible = true;
            }

            // colocar dinamicamente asteroids
            foreach (PictureBox p in asteroids)
            {
                p.Top = rnd.Next(-500, -100);
                p.Left = rnd.Next(0, pb_background.Right - 50);
            }

            // colocar dinamicamente bots
            foreach (PictureBox p in bots)
            {
                p.Top = rnd.Next(-500, -100);
                p.Left = rnd.Next(0, pb_background.Right - 50);
            }

            // limpar tiros do jogador, se existirem
            for (int i = 0; i < disparos_jogador.Count; ++i)
            {
                disparos_jogador[i].Visible = false;
            }
            disparos_jogador.Clear();

            // limpar tiros do inimigo, se existirem
            for (int i = 0; i < disparos_inimigo.Count; ++i)
            {
                disparos_inimigo[i].Visible = false;
            }
            disparos_inimigo.Clear();
        }

        private void alterarOpcoesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FormOpcoes fo = new FormOpcoes();
            fo.frm_jogo = this;
            fo.Show();
            this.Hide();
        }

        private void FormJogo_FormClosing(object sender, FormClosingEventArgs e)
        {
            GravarOpcoes();
        }

        private void GravarOpcoes()
        {
            try
            {
                StreamWriter w = new StreamWriter(ficheiro_config);
                w.WriteLine("<local>");
                w.WriteLine("    <jogo>");
                w.WriteLine("        <opcoes>");
                w.WriteLine("            <dificuldade>" + dificuldade + "</dificuldade>");
                w.WriteLine("            <numero_de_bots>" + numero_de_bots + "</numero_de_bots>");
                w.WriteLine("            <numero_de_asteroides>" + numero_de_asteroides + "</numero_de_asteroides>");
                w.WriteLine("            <numero_de_rochedos>" + numero_de_rochedos + "</numero_de_rochedos>");
                w.WriteLine("        </opcoes>");
                w.WriteLine("    </jogo>");
                w.WriteLine("</local>");
                w.Close();
            }
            catch (Exception e)
            {
                // escrever para consola log de erros (o jogo tem de ser lancado no cmd para tal efeito)
                Console.WriteLine(e.Message);
                Console.WriteLine("Nao foi possivel gerar ficheiro \"" + ficheiro_config + "\"");
            }
        }
    }
}
