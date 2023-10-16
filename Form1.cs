using DTCChecker.Items;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using System.Data.SQLite;

namespace DTCChecker
{
    public partial class Form1 : Form
    {  
        private static Form1 cd = new Form1();
        private static NetworkStream stream;
        
        private List<DTCval> dtcvalues = new List<DTCval>();
        //private String str = "server=localhost\\SQLEXPRESS;database=master;Integrated Security=SSPI;TrustServerCertificate = True";
        private String str = "Data Source=C:\\Users\\ADV ELEC (ASSY ME)\\Downloads\\WindowsFormsApp1\\DTCChecker\\localsqlite.sqlite";
        private static SQLiteConnection connection;



        public Form1()
        {   
            InitializeComponent();   
        }


        private async void connect()
        {
            connection = new SQLiteConnection(str);
            connection.Open();
            recentdtc();
            while (true)
            {
                using(TcpClient client = new TcpClient())
                {
                    try
                    {
                        string ip = "127.0.0.1";
                        //string ip = "192.168.0.10";                //Set Desired IP Address (192.168.0.10)
                        int port = 35000;
                        await client.ConnectAsync(IPAddress.Parse(ip), port);
                        stream = client.GetStream();
                        //stream.ReadTimeout = 2000;
                        //SetELMConfigs();
                        int buffsize = client.ReceiveBufferSize;
                        send("07" + "\r", buffsize);                

                    }
                    catch (SocketException)
                    {

                        continue;
                    }
                    catch (Exception)
                    {
                        continue;
                    }
                    await Task.Delay(1000);
                }

            }
            
        }
        private void send(String msg, int recbuffsize)
        {
            
            listView1.Items.Clear();
           
            byte[] sendbuffer = Encoding.ASCII.GetBytes(msg);       //Convert String to bytes
            stream.Write(sendbuffer,0, sendbuffer.Length);      //Sening msg = 07/r to Device
            
            bool forcestop = false;
            string data = "";
            int k = 0;
            while (!forcestop)
            {
                try
                {
                    byte[] buffer = new byte[recbuffsize];

                    int bytesRead = stream.Read(buffer, 0, buffer.Length);      //Read all data until ">" is received

                    byte[] mesajj = new byte[bytesRead];

                    for (int i = 0; i < bytesRead; i++)
                    {
                        mesajj[i] = buffer[i];
                    }

                    k++;
                    data +=Encoding.Default.GetString(mesajj, 0, bytesRead).Replace("\r"," ");          

                    byte[] temp2 = Encoding.ASCII.GetBytes("\r");

                    if (data.EndsWith(">") || data.Length > 128 || k > 10)
                    {

                        k = 0;
                        if (data.Length > 128)
                        {
                            stream.Write(temp2, 0, temp2.Length);           //If data being sent exceeds 128 /r is sent to Device to get rest of the data
                        }

                        listconvert(data);
                        forcestop = true;


                        data = "";
                    }
                }
                catch (Exception)
                {
                    forcestop= true;
                    
                }
            }
        }

        public async void listconvert(string recmsg)
        {
            label1.Text = recmsg;
            //string[] recarray = recmsg.Split(' ');

            List<String> list1 = new List<string>((recmsg.Length - 1) / 2);

            //Data can be received in 2 formats: (i) 07 47 02 04 06 05 3A      >
            //                                   (ii) 07 008 01: 47 04........   >


            if (recmsg.Contains("0:"))
            {
                for (int i = 11; i < recmsg.Length - 1; i = i + 1)
                {
                    char c = recmsg[i];
                    if (c == ':')
                    {
                        continue;
                    }
                    else if (recmsg[i+3] == ':')
                    {  
                        string conc = "P" + recmsg[i] + recmsg[i + 1] + recmsg[i+4] + recmsg[i+5];
                        list1.Add(conc.Trim());
                        i = i + 5;
                    }
                    else if (c == 'A' && recmsg[i + 1] == 'A')
                    {
                        break;
                    }
                    else if ((i+5)< recmsg.Length && recmsg[i+5] == ':')
                    {
                        try
                        {
                            string conc = "P" + recmsg[i] + recmsg[i + 1] + recmsg[i + 2] + recmsg[i + 3];
                            list1.Add(conc.Trim());
                            i = i + 5;
                        }
                        catch (IndexOutOfRangeException) { }

                    }
                    
                    else
                    {
                        string conc = "P" + recmsg[i] + recmsg[i + 1] + recmsg[i+2] + recmsg[i+3];
                        list1.Add(conc.Trim());
                        i = i + 3;
                    }

                }
            }
            else
            {
                for (int i = 6; i < recmsg.Length - 1; i = i + 1)
                {

                    if (recmsg[i] == 'A' && recmsg[i + 1] == 'A')
                    {
                        break;
                    }
                    else if (recmsg[i] == '>')
                    {
                        break;
                    }
                    else
                    {
                            string conc = "P" + recmsg[i] + recmsg[i + 1] + recmsg[i+2] + recmsg[i+3];
                            list1.Add(conc.Trim());
                            i = i + 3;

                    }

                }
            }

            

            List<string> list2 = new List<string>();

            for (int p = 0; p < list1.Count; p++)
            {
                string ele = list1[p];
                var dtcval = dtcvalues.Where(h => h.DTCCodes == ele).LastOrDefault();
                if (dtcval != null)
                {
                    list2.Add(dtcval.DTCDetails);
                }
                else if(dtcval == null)
                {
                    list2.Add("NULL");
                }

            }

            string alldtc = "";

            for (int i = 0; i < list1.Count; i++)
            {
                ListViewItem item = new ListViewItem(new string[] { list1[i], list2[i] });                           
                listView1.Items.Add(item);
                alldtc = alldtc + list1[i] + ",";

            }

            DateTime now = DateTime.Now;
            string dt = now.ToString();
            string[] currdt = dt.Split(' ');
            string sc = "INSERT INTO DTCCodes(Date, Time, DTCCodes) values(@date1,@time1,@pcode)";
            string ctq = "CREATE TABLE IF NOT EXISTS DTCCodes(Date varchar(10), Time time(0), DTCCodes varchar(100))";
            using (SQLiteCommand cmd = new SQLiteCommand(ctq, connection))
            {
                cmd.ExecuteNonQuery();

            }
            SQLiteCommand si = new SQLiteCommand(sc, connection);
            si.Parameters.AddWithValue("@date1", currdt[0]);
            si.Parameters.AddWithValue("@time1", currdt[1]);
            si.Parameters.AddWithValue("@pcode", alldtc);
            si.ExecuteNonQuery();
            recentdtc();
            stream.Close();
            await Task.Delay(20000);
            connect();

        }

        
        private void SetELMConfigs()
        {
            byte[] temp = new byte[1024];
           
            byte[] x = Encoding.ASCII.GetBytes("AT SP 0" + "\r");
            stream.Write(x, 0, x.Length);
            //stream.Read(temp, 0, temp.Length);
            
            //byte[] h = Encoding.ASCII.GetBytes("AT H1" + "\r");
            //stream.Write(h, 0, h.Length);
            //stream.Read(temp, 0, temp.Length);
            
            byte[] sh2 = Encoding.ASCII.GetBytes("AT SH 7FF" + "\r");
            stream.Write(sh2, 0, sh2.Length);
            //stream.Read(temp, 0, temp.Length);
            
        }
        void listView1_SelectedIndexChanged_1(object sender, EventArgs e)
        {
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            dtcvalues = Helpers.Helper.ReadJsonConfiguration(Helpers.Helper.ReadResource("dtclist"));
            connect();
        }

        
        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        public void recentdtc()
        {
            listView2.Items.Clear();
            List<string> columndata = new List<string>();
            
            string query = "SELECT * FROM DTCCodes ORDER BY Date DESC,Time DESC LIMIT 10";
            SQLiteDataAdapter adapter = new SQLiteDataAdapter(query, connection);
            DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);

            foreach (DataRow row in dataTable.Rows)
            {
                ListViewItem item = new ListViewItem(row["Date"].ToString());
                string a = row["Time"].ToString();
                string[] qw = a.Split(' ');
                item.SubItems.Add(qw[1]);
                item.SubItems.Add(row["DTCCodes"].ToString());

                listView2.Items.Add(item);
            }
              
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void label4_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            listView3.Items.Clear();
            string fromdate = dateTimePicker2.Value.ToString().Split(' ')[0];
            string todate = dateTimePicker1.Value.ToString().Split(' ')[0];
            List<string> columndata = new List<string>();

            string query = "SELECT * FROM DTCCodes WHERE Date > @fdt AND Date < @fdtt OR Date = @fdt OR Date = @fdtt";
            SQLiteCommand command = new SQLiteCommand(query, connection);
            command.Parameters.Add(new SQLiteParameter("@fdt", DbType.String));
            command.Parameters["@fdt"].Value = fromdate;
            command.Parameters.Add(new SQLiteParameter("@fdtt", DbType.String));
            command.Parameters["@fdtt"].Value = todate;

            SQLiteDataAdapter adapter = new SQLiteDataAdapter(command); // Pass the command to the adapter
            DataTable dataTable = new DataTable();
            adapter.Fill(dataTable);

            foreach (DataRow row in dataTable.Rows)
            {
                ListViewItem item = new ListViewItem(row["Date"].ToString());
                string a = row["Time"].ToString();
                string[] qw = a.Split(' ');
                item.SubItems.Add(qw[1]);
                item.SubItems.Add(row["DTCCodes"].ToString());

                listView3.Items.Add(item);
            }

        }

        private void listView2_SelectedIndexChanged_1(object sender, EventArgs e)
        {

        }


        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            listView3.Items.Clear();
            string query = "SELECT * FROM DTCCodes WHERE DTCCodes LIKE @txtval";
            string txtval = textBox1.Text;

            using(SQLiteCommand cmd = new SQLiteCommand(query,connection))
            {
                cmd.Parameters.AddWithValue("@txtval", "%"+txtval+"%");
                using (SQLiteDataAdapter adapter = new SQLiteDataAdapter(cmd))
                {
                    DataTable datatable = new DataTable();
                    adapter.Fill(datatable);

                    foreach (DataRow row in datatable.Rows)
                    {
                        ListViewItem item = new ListViewItem(row["Date"].ToString());
                        string a = row["Time"].ToString();
                        string[] qw = a.Split(' ');
                        item.SubItems.Add(qw[1]);
                        item.SubItems.Add(txtval.ToString());

                        listView3.Items.Add(item);
                    }

                    int count = datatable.Rows.Count;
                    label6.Text = count.ToString();
                }

                
            }


        }

        private void label5_Click(object sender, EventArgs e)
        {

        }
    }
}
