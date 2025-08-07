using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Monitor_Tecnico
{
    public partial class Form1 : Form
    {
        private PictureBox pictureBox;
        private TcpListener listener;
        private Thread listenerThread;
        private int controle = 0;

        private Dictionary<string, TcpClient> clients = new Dictionary<string, TcpClient>();
        private Dictionary<string, NetworkStream> streams = new Dictionary<string, NetworkStream>();
        private Dictionary<string, Image> ultimasImagens = new Dictionary<string, Image>();

        private string selectedClient = null;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Size = new Size(1024, 768);

            //CarregarIPsConectados();
            ReconectarClientes();
            IniciaListening();

            listenerThread = new Thread(Monitoramento);
            listenerThread.IsBackground = true;
            listenerThread.Start();

            //comboBox1.Items.Add("192.168.3.35");
            //comboBox1.Items.Add("192.168.7.4");
            //comboBox1.Items.Add("192.168.8.3"); //apenas para teste 
        }

        private void IniciaListening()
        {
            listener = new TcpListener(IPAddress.Any, 8888);
            listener.Start();
        }

        private void ReconectarClientes()
        {
            foreach (var clientId in comboBox1.Items)
            {
                try
                {
                    var client = new TcpClient(clientId.ToString(), 8888);
                    var stream = client.GetStream();

                    clients[clientId.ToString()] = client;
                    streams[clientId.ToString()] = stream;
                }
                catch
                {
                    // Ignorar erros de reconexão
                }
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Atualizar cliente selecionado
            selectedClient = comboBox1.SelectedItem?.ToString();

            // Atualizar a tela imediatamente
            if (selectedClient != null && ultimasImagens.ContainsKey(selectedClient))
            {
                pictureBox1.Image = (Image)ultimasImagens[selectedClient].Clone();
                try
                {
                    new Thread(() =>
                    {

                    }).Start();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao atualizar a tela do cliente {selectedClient}: {ex.Message}");
                }
            }
            else
            {
                pictureBox1.Image = null; // Limpar a imagem se o cliente não estiver disponível
            }
        }

        private void Monitoramento()
        {
            while (true)
            {
                var client = listener.AcceptTcpClient();
                var stream = client.GetStream();
                //stream.ReadTimeout = 5000;

                //===============Novos Ajustes========================== 
                byte[] nomeLengthBuffer = new byte[4];
                stream.Read(nomeLengthBuffer, 0, 4);

                int nomeLength = BitConverter.ToInt32(nomeLengthBuffer, 0);
                byte[] nomeBuffer = new byte[nomeLength];
                stream.Read(nomeBuffer, 0, nomeLength);
                string nomeUsuario = Encoding.UTF8.GetString(nomeBuffer);
                //======================================================

                try
                {
                    // Identificar o cliente (exemplo: Nome + IP)
                    string clientId = ((IPEndPoint)client.Client.RemoteEndPoint).ToString(); // IP:Porta
                    string clientKey = $"{nomeUsuario} ({clientId})";

                    // Adicionar cliente à lista
                    this.Invoke((MethodInvoker)(() =>
                    {
                        if (!clients.ContainsKey(clientKey))
                        {
                            clients[clientKey] = client;
                            streams[clientKey] = stream;
                            comboBox1.Items.Add(clientKey);
                            listBoxLog.Items.Add($"{DateTime.Now:HH:mm:ss} - Cliente conectado: {clientKey}");
                            if (comboBox1.SelectedItem == null)
                                comboBox1.SelectedItem = clientKey;
                        }
                        else
                        {
                            // Atualize apenas o stream e o client se já existir
                            clients[clientKey] = client;
                            streams[clientKey] = stream;
                        }
                    }));

                    // Iniciar thread para receber imagens do cliente
                    new Thread(() =>
                    {
                        while (true)
                        {
                            try
                            {
                                byte[] lengthBuffer = new byte[4];
                                stream.Read(lengthBuffer, 0, 4);
                                int length = BitConverter.ToInt32(lengthBuffer, 0);

                                byte[] imageBuffer = new byte[length];
                                int read = 0;

                                while (read < length)
                                    read += stream.Read(imageBuffer, read, length - read);

                                using (MemoryStream ms = new MemoryStream(imageBuffer))
                                {
                                    var img = Image.FromStream(ms);

                                    lock (ultimasImagens)
                                    {
                                        if (ultimasImagens.ContainsKey(clientKey))
                                        {
                                            ultimasImagens[clientKey].Dispose();
                                        }

                                        ultimasImagens[clientKey] = (Image)img.Clone();
                                    }

                                    // Exibir imagem apenas se o cliente estiver selecionado
                                    if (selectedClient == clientKey)
                                    {
                                        this.Invoke((MethodInvoker)(() =>
                                        {
                                            pictureBox1.Image = img;
                                        }));
                                    }
                                }
                            }
                            catch
                            {
                                //Remover cliente em caso de erro
                                this.Invoke((MethodInvoker)(() =>
                                {
                                    clients.Remove(clientKey); // pode ser aqui o erro e ele apenas recebe 2 clientes. É erro do Remoto
                                    streams.Remove(clientKey);

                                    comboBox1.Items.Remove(clientKey);
                                    //comboBox1.SelectedIndex = -1;
                                    //comboBox1.Text = "";

                                    listBoxLog.Items.Add($"{DateTime.Now:HH:mm:ss} - Cliente desconectado: {clientKey}");

                                    if (selectedClient == clientKey)
                                    {
                                        pictureBox1.Image = null;
                                        selectedClient = null;
                                    }
                                }));

                                break;
                            }
                        }
                    }).Start();
                }
                catch
                {
                    // Ignorar erros de conexão
                }
            }
        }
    }
}