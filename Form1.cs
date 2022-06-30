using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace RTS_1000_Test_Tool
{
    public partial class Form1 : Form
    {
        private WebSocketWrapper[] wsw = new WebSocketWrapper[8];
        private bool _onClosing;


        public Form1()
        {
            InitializeComponent();
            _onClosing = false;
        }


        private void button2_Click(object sender, EventArgs e)
        {
            // Crea le connessioni websocket verso le 7 periferiche e riporta il colore del pallino verde se la connessione va a buon fine
            // e rosso se fallisce
            wsw[1] = WebSocketWrapper.Create(textBox2.Text,1);
            wsw[2] = WebSocketWrapper.Create(textBox3.Text,2);
            wsw[3] = WebSocketWrapper.Create(textBox4.Text,3);
            wsw[4] = WebSocketWrapper.Create(textBox5.Text,4);
            wsw[5] = WebSocketWrapper.Create(textBox6.Text,5);
            wsw[6] = WebSocketWrapper.Create(textBox7.Text,6);
            wsw[7] = WebSocketWrapper.Create(textBox8.Text,7);
            for (int i = 1; i < wsw.Length; i++)
            {
                wsw[i] = wsw[i].Connect();
                wsw[i].OnConnect(ClientConnected);
                wsw[i].OnDisconnect(ClientDisconnected);
                wsw[i].OnMessage(ServerMessage);
                wsw[i].OnException(ConnectionException);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            wsw[0] = WebSocketWrapper.Create(textBox1.Text,0);
            wsw[0] = wsw[0].Connect();
            wsw[0].OnConnect(ClientConnected);
            wsw[0].OnDisconnect(ClientDisconnected);
            wsw[0].OnMessage(ServerMessage);
            wsw[0].OnException(ConnectionException);

            string jsonString = "{\"header\":{\"name\":" + "\"" + "ServicePublisher.GetServices" + "\"" + "," +
                      "\"requestId\": " + Convert.ToString(RequestId.NewID()) + "," +
                      "\"type\":\"command\"" + "}" +
                      ",\"payload\":{ \"timeout\": 5000}" + "}";

            textBox10.AppendText(">>> Sending to Framework" + Environment.NewLine);
            textBox10.AppendText(JsonPrettify(jsonString)+Environment.NewLine);

            wsw[0].SendMessage(jsonString);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            string jsonString = "{\"header\":{\"name\":" + "\"" + "Common.Cancel" + "\"" + "," +
                      "\"requestId\": " + Convert.ToString(RequestId.NewID()) + "," +
                      "\"type\":\"command\"" + "}" +
                      ",\"payload\":{ \"timeout\": 5000" + "," + "\"requestIds\":" + "[" + Convert.ToString(numericUpDown1.Value) + "]" + "}" + "}";
            textBox10.AppendText(">>> Sending to " + (string)comboBox1.SelectedItem + Environment.NewLine);
            textBox10.AppendText(JsonPrettify(jsonString) + Environment.NewLine);
            wsw[comboBox1.SelectedIndex + 1].SendMessage(jsonString);
        }

        public void ClientConnected(int id)
        {
            if (textBox10.InvokeRequired)
            {
                textBox10.Invoke((Action)(() => ClientConnected(id)));
                return;
            }
            switch (id)
            {
                case 1: pictureBox1.Image = Properties.Resources.Greendot; break;
                case 2: pictureBox2.Image = Properties.Resources.Greendot; break;
                case 3: pictureBox3.Image = Properties.Resources.Greendot; break;
                case 4: pictureBox4.Image = Properties.Resources.Greendot; break;
                case 5: pictureBox5.Image = Properties.Resources.Greendot; break;
                case 6: pictureBox6.Image = Properties.Resources.Greendot; break;
                case 7: pictureBox7.Image = Properties.Resources.Greendot; break;
            }
        }

        public void ClientDisconnected(int id)
        {
            if (textBox10.InvokeRequired)
            {
                textBox10.Invoke((Action)(() => ClientDisconnected(id)));
                return;
            }
            switch (id)
            {
                case 1: pictureBox1.Image = Properties.Resources.Reddot; break;
                case 2: pictureBox2.Image = Properties.Resources.Reddot; break;
                case 3: pictureBox3.Image = Properties.Resources.Reddot; break;
                case 4: pictureBox4.Image = Properties.Resources.Reddot; break;
                case 5: pictureBox5.Image = Properties.Resources.Reddot; break;
                case 6: pictureBox6.Image = Properties.Resources.Reddot; break;
                case 7: pictureBox7.Image = Properties.Resources.Reddot; break;
            }
        }

        public void ConnectionException(int id, string error)
        {
            if (textBox10.InvokeRequired)
            {
                textBox10.Invoke((Action)(() => ConnectionException(id,error)));
                return;
            }
            switch (id)
            {
                case 1: pictureBox1.Image = Properties.Resources.Reddot; break;
                case 2: pictureBox2.Image = Properties.Resources.Reddot; break;
                case 3: pictureBox3.Image = Properties.Resources.Reddot; break;
                case 4: pictureBox4.Image = Properties.Resources.Reddot; break;
                case 5: pictureBox5.Image = Properties.Resources.Reddot; break;
                case 6: pictureBox6.Image = Properties.Resources.Reddot; break;
                case 7: pictureBox7.Image = Properties.Resources.Reddot; break;
            }
            if (id == 0) textBox10.AppendText("<<< Error from: Framework: ");
            else textBox10.AppendText("<<< Error from: " + (string)comboBox1.Items[id - 1]+": ");
            textBox10.AppendText(error+Environment.NewLine);

        }

        public void ServerMessage(int id, string jsonmsg)
        {
            if (textBox10.InvokeRequired)
            {
                textBox10.Invoke((Action)(() => ServerMessage(id,jsonmsg)));
                return;
            }

            if (id == 0) textBox10.AppendText("=== from: Framework" + Environment.NewLine);
            else textBox10.AppendText("=== from: " + (string)comboBox1.Items[id-1] + Environment.NewLine);
            textBox10.AppendText(JsonPrettify(jsonmsg));
        }

        public static string JsonPrettify(string json)
        {
            using (var stringReader = new StringReader(json))
            using (var stringWriter = new StringWriter())
            {
                var jsonReader = new JsonTextReader(stringReader);
                var jsonWriter = new JsonTextWriter(stringWriter) { Formatting = Formatting.Indented };
                jsonWriter.WriteToken(jsonReader);
                return stringWriter.ToString();
            }
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            comboBox2.Items.Clear();
            switch (comboBox1.SelectedIndex)
            {
                case 0: //Barcode Reader
                    comboBox2.Items.Add("Common.Status");       
                    comboBox2.Items.Add("BarcodeReader.Read");
                    comboBox2.Items.Add("BarcodeReader.Reset");
                    break;
                case 1: //Lights
                    comboBox2.Items.Add("Common.Status");
                    comboBox2.Items.Add("Lights.SetLight");
                    break;
                case 2: // Auxiliaries
                    comboBox2.Items.Add("Common.Status");
                    comboBox2.Items.Add("Auxiliaries.SetAuxiliaries");
                    break;
                case 3: // Receipt Printer
                    comboBox2.Items.Add("Common.Status");
                    comboBox2.Items.Add("Printer.Reset");
                    comboBox2.Items.Add("Printer.PrintForm");
                    comboBox2.Items.Add("Printer.PrintNative");
                    comboBox2.Items.Add("Printer.ControlMedia");
                    break;
                case 4: // Ticket Printer
                    comboBox2.Items.Add("Common.Status");
                    comboBox2.Items.Add("Printer.Reset");
                    comboBox2.Items.Add("Printer.PrintForm");
                    comboBox2.Items.Add("Printer.PrintNative");
                    comboBox2.Items.Add("Printer.ControlMedia");
                    break;
                case 5: // Credit Card Pay
                    comboBox2.Items.Add("Common.Status");
                    comboBox2.Items.Add("CCPay.RequestPayment");
                    break;
                case 6: // NFC Reader
                    comboBox2.Items.Add("Common.Status");
                    comboBox2.Items.Add("NfcReader.Reset");
                    comboBox2.Items.Add("NfcReader.SendFile");
                    comboBox2.Items.Add("NfcReader.ReadRawData");
                    break;
            }
          
        }

        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string Folder = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);
                textBox9.Text = File.ReadAllText(Path.Combine(Folder,comboBox1.Text + "_" + comboBox2.Text + ".txt"));
            }
            catch { }

        }
        private void button3_Click(object sender, EventArgs e)
        {
            int rid = RequestId.NewID();
            
            if ((comboBox1.Text == "") || (comboBox2.Text == ""))
            {
                MessageBox.Show("invalid command", "ERROR!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            numericUpDown1.Value = rid;
            string jsonString ="{\"header\":{\"name\":" + "\"" + comboBox2.Text + "\"" + "," +
                                  "\"requestId\": "+ Convert.ToString(rid) + ","+
                                  "\"type\":\"command\"" + "}" +
                                  ",\"payload\":"+textBox9.Text + "}";

            textBox10.AppendText(">>> Sending to " + (string)comboBox1.SelectedItem + Environment.NewLine);
            textBox10.AppendText(JsonPrettify(jsonString) + Environment.NewLine);
            wsw[comboBox1.SelectedIndex + 1].SendMessage(jsonString);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            _onClosing = true;

            try
            {

                for (int i = 0; i < wsw.Length; i++)
                {
                    if (wsw[i] != null && wsw[i].SocketStatus == System.Net.WebSockets.WebSocketState.Open)
                    {
                        Task task = wsw[i].InstanceObject.CloseAsync(System.Net.WebSockets.WebSocketCloseStatus.NormalClosure, "", CancellationToken.None);
                        task.Wait(); task.Dispose();
                    }
                }
            }
            catch (Exception)
            { }

        }
    }
}
